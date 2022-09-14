using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using System.Windows.Interop;
using System.Diagnostics;
using System.IO;
using System.Management;
using NonFormTimer = System.Timers.Timer;
using System.Runtime.InteropServices;
using System.ComponentModel;
using Microsoft.Extensions.Options;

namespace DS4WinWPF.DS4Forms;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
[System.Security.SuppressUnmanagedCodeSecurity]
public partial class MainWindow : Window
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IControlService _controlService;
    private readonly IProfileListService _profileListService;
    private const int DEFAULT_PROFILE_EDITOR_WIDTH = 1000;
    private const int DEFAULT_PROFILE_EDITOR_HEIGHT = 650;

    private const int POWER_RESUME = 7;
    private const int POWER_SUSPEND = 4;

    private readonly MainWindowsViewModel mainWinVM;
    private StatusLogMsg lastLogMsg = new StatusLogMsg();
    private LogViewModel logvm;
    private ControllerListViewModel conLvViewModel;
    private TrayIconViewModel trayIconVM;
    private SettingsViewModel settingsWrapVM;
    private IntPtr regHandle = new IntPtr();
    private bool showAppInTaskbar = false;
    private ManagementEventWatcher managementEvWatcher;
    private bool wasrunning = false;
    private AutoProfileHolder autoProfileHolder;
    private NonFormTimer hotkeysTimer;
    private NonFormTimer autoProfilesTimer;
    private AutoProfileChecker autoprofileChecker;
    private ProfileEditor editor;
    private bool preserveSize = true;
    private Size oldSize;
    private bool contextclose;

    public MainWindow(IServiceProvider serviceProvider, IControlService controlService, IProfileListService profileListService, IOptions<ArgumentParser> argsOptions)
    {
        this._serviceProvider = serviceProvider;
        this._controlService = controlService;
        this._profileListService = profileListService;
        InitializeComponent();
            
        mainWinVM = serviceProvider.GetRequiredService<MainWindowsViewModel>();
        DataContext = mainWinVM;

        var root = Application.Current as App;
        settingsWrapVM =  serviceProvider.GetRequiredService<SettingsViewModel>();
        settingsTab.DataContext = settingsWrapVM;
        logvm = serviceProvider.GetRequiredService<LogViewModel>();
        //logListView.ItemsSource = logvm.LogItems;
        logListView.DataContext = logvm;
        lastMsgLb.DataContext = lastLogMsg;
            
        profilesListBox.ItemsSource = profileListService.ProfileList.ProfileListCol;

        StartStopBtn.Content = this._controlService.IsRunning ? Strings.StopText :
            Strings.StartText;

        conLvViewModel = this._serviceProvider.GetRequiredService<ControllerListViewModel>();
            
        //conLvViewModel = this.serviceProvider.GetRequiredService<IControllerListViewModelFactory>().Create(profileListHolder);
        controllerLV.DataContext = conLvViewModel;
        controllerLV.ItemsSource = conLvViewModel.ControllerCol;
        ChangeControllerPanel();

        // Sort device by input slot number
        var view = (CollectionView)CollectionViewSource.GetDefaultView(controllerLV.ItemsSource);
        view.SortDescriptions.Clear();
        view.SortDescriptions.Add(new SortDescription("DevIndex", ListSortDirection.Ascending));
        view.Refresh();

        trayIconVM = this._serviceProvider.GetRequiredService<TrayIconViewModel>();

        // Need to define before calling TaskbarIcon.ForceCreate
        notifyIcon.DataContext = trayIconVM;
        notifyIcon.CustomName = Global.exelocation;

        // Remove TaskbarIcon from visual tree so Loaded and Unloaded events
        // are not fired for TaskbarIcon instance. Ignores early Dispose calls
        // when scaling changes or an RDP session is activated
        var parent = VisualTreeHelper.GetParent(notifyIcon) as Panel;
        if (parent != null)
        {
            parent.Children.Remove(notifyIcon);
            // Since Loaded event will not get fired from Window, need to
            // create the tray icon explicitly here
            try
            {
                // Loaded event handler has enablesEfficiencyMode default to false so
                // do the same here
                notifyIcon.ForceCreate(enablesEfficiencyMode: false);
            }
            catch (Exception)
            {
                // Ignore exception
            }
        }

        if (Global.StartMinimized || argsOptions.Value.Mini)
        {
            WindowState = WindowState.Minimized;
        }

        var isElevated = Global.IsAdministrator();
        if (isElevated)
        {
            uacImg.Visibility = Visibility.Collapsed;
        }

        Width = Global.FormWidth;
        Height = Global.FormHeight;
        WindowStartupLocation = WindowStartupLocation.Manual;
        Left = Global.FormLocationX;
        Top = Global.FormLocationY;
        noContLb.Content = string.Format(Strings.NoControllersConnected,
            IControlService.CURRENT_DS4_CONTROLLER_LIMIT);

        autoProfileHolder = autoProfControl.AutoProfileHolder;
        autoProfControl.SetupDataContext(this._profileListService.ProfileList);

        autoprofileChecker = this._serviceProvider.GetRequiredService<IAutoProfileCheckerFactory>().Create(autoProfileHolder);

        slotManControl.SetupDataContext(controlService: this._controlService, this._controlService.OutputslotMan);

        SetupEvents();

        // Don't tie timers to main thread
        var timerThread = new Thread(() =>
        {
            hotkeysTimer = new NonFormTimer();
            hotkeysTimer.Interval = 20;
            hotkeysTimer.AutoReset = false;

            autoProfilesTimer = new NonFormTimer();
            autoProfilesTimer.Interval = 1000;
            autoProfilesTimer.AutoReset = false;
        });
        timerThread.IsBackground = true;
        timerThread.Priority = ThreadPriority.Lowest;
        timerThread.Start();
        // Wait for thread tasks to finish before continuing
        timerThread.Join();
    }

    public async Task LateChecksAsync(ArgumentParser parser)
    {
        var tempTask = Task.Run(() =>
        {
            mainWinVM.CheckDrivers();
            if (!parser.Stop)
            {
                Dispatcher.BeginInvoke((Action)(() =>
                {
                    StartStopBtn.IsEnabled = false;
                }));
                Thread.Sleep(1000);
                _controlService.Start();
                //root.rootHubtest.Start();
            }
        });

        // Log exceptions that might occur
        Util.LogAssistBackgroundTask(tempTask);

        tempTask = Task.Delay(100).ContinueWith(async (t) =>
        {
            var checkwhen = Global.CheckWhen;
            if (checkwhen > 0 && DateTime.Now >= Global.LastChecked + TimeSpan.FromHours(checkwhen))
            {
                await mainWinVM.DownloadUpstreamVersionInfoAsync();
                CheckVersionAsync();

                Global.LastChecked = DateTime.Now;
            }
        });
        Util.LogAssistBackgroundTask(tempTask);
    }

    private async void CheckVersionAsync(bool showStatus = false)
    {
        var version = Global.exeversion;
        var newVersion = string.Empty;
        var versionFilePath = Path.Combine(Global.appdatapath, "version.txt");
        var lastVersionNum = Global.LastVersionCheckedNum;
        //ulong lastVersion = Global.CompileVersionNumberFromString("2.1.1");

        var versionFileExists = File.Exists(versionFilePath);
        if (versionFileExists)
        {
            newVersion = (await File.ReadAllTextAsync(versionFilePath)).Trim();
            //newversion = "2.1.3";
        }

        var newversionNum = !string.IsNullOrEmpty(newVersion) ?
            Global.CompileVersionNumberFromString(newVersion) : 0;

        if (!string.IsNullOrWhiteSpace(newVersion) && version.CompareTo(newVersion) != 0 &&
            lastVersionNum < newversionNum)
        {
            var result = MessageBoxResult.No;
            Dispatcher.Invoke(() =>
            {
                var updaterWindow = _serviceProvider.GetRequiredService<IUpdaterWindowFactory>().Create(newVersion);
                updaterWindow.ShowDialog();
                result = updaterWindow.Result;
            });

            if (result == MessageBoxResult.Yes)
            {
                var (shouldLaunch, upstreamVersion) = await mainWinVM.RunUpdaterCheckAsync();

                if (shouldLaunch)
                {
                    shouldLaunch = mainWinVM.LauchDS4Updater();
                }

                if (shouldLaunch)
                {
                    Dispatcher.BeginInvoke(() =>
                    {
                        contextclose = true;
                        Close();
                    });
                }
                else
                {
                    Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show(Localization.PleaseDownloadUpdater);
                        if (!string.IsNullOrEmpty(upstreamVersion))
                        {
                            Util.StartProcessHelper($"https://github.com/Ryochan7/DS4Updater/releases/tag/v{upstreamVersion}/{mainWinVM.updaterExe}");
                        }
                    });
                }
            }
            else
            {
                if (versionFileExists)
                    File.Delete(versionFilePath);
            }
        }
        else
        {
            if (versionFileExists)
                File.Delete(versionFilePath);

            if (showStatus)
            {
                Dispatcher.Invoke(() => MessageBox.Show(Localization.UpToDate, "DS4Windows Updater"));
            }
        }
    }

    private void TrayIconVM_RequestMinimize(object sender, EventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void TrayIconVM_ProfileSelected(TrayIconViewModel sender,
        ControllerHolder item, string profile)
    {
        var idx = item.Index;
        var devitem = conLvViewModel.ControllerDict[idx];
        if (devitem != null)
        {
            devitem.ChangeSelectedProfile(profile);
        }
    }

    private void ShowNotification(object sender, DebugEventArgs e)
    {
        Dispatcher.BeginInvoke((Action)(() =>
        {

            if (!IsActive && (Global.Notifications == 2 ||
                              (Global.Notifications == 1 && e.Warning)))
            {
                notifyIcon.ShowNotification(TrayIconViewModel.ballonTitle,
                    e.Data, !e.Warning ? H.NotifyIcon.Core.NotificationIcon.Info :
                        H.NotifyIcon.Core.NotificationIcon.Warning);
            }
        }));
    }

    private void SetupEvents()
    {
        _controlService.ServiceStarted += ControlServiceStarted;
        _controlService.RunningChanged += ControlServiceChanged;
        _controlService.PreServiceStop += PrepareForServiceStop;
        //root.rootHubtest.RunningChanged += ControlServiceChanged;
        conLvViewModel.ControllerCol.CollectionChanged += ControllerCol_CollectionChanged;
        AppLogger.TrayIconLog += ShowNotification;
        AppLogger.GuiLog += UpdateLastStatusMessage;
        logvm.LogItems.CollectionChanged += LogItems_CollectionChanged;
        _controlService.Debug += UpdateLastStatusMessage;
        trayIconVM.RequestShutdown += TrayIconVM_RequestShutdown;
        trayIconVM.ProfileSelected += TrayIconVM_ProfileSelected;
        trayIconVM.RequestMinimize += TrayIconVM_RequestMinimize;
        trayIconVM.RequestOpen += TrayIconVM_RequestOpen;
        trayIconVM.RequestServiceChange += TrayIconVM_RequestServiceChange;
        settingsWrapVM.IconChoiceIndexChanged += SettingsWrapVM_IconChoiceIndexChanged;
        settingsWrapVM.AppChoiceIndexChanged += SettingsWrapVM_AppChoiceIndexChanged;

        autoProfControl.AutoDebugChanged += AutoProfControl_AutoDebugChanged;
        autoprofileChecker.RequestServiceChange += AutoprofileChecker_RequestServiceChange;
        autoProfileHolder.AutoProfileColl.CollectionChanged += AutoProfileColl_CollectionChanged;
        //autoProfControl.AutoProfVM.AutoProfileSystemChange += AutoProfVM_AutoProfileSystemChange;
        mainWinVM.FullTabsEnabledChanged += MainWinVM_FullTabsEnabledChanged;

        var wmiConnected = false;
        var q = new WqlEventQuery();
        var scope = new ManagementScope("root\\CIMV2");
        q.EventClassName = "Win32_PowerManagementEvent";

        try
        {
            scope.Connect();
        }
        catch (COMException) { }
        catch (ManagementException) { }

        if (scope.IsConnected)
        {
            wmiConnected = true;
            managementEvWatcher = new ManagementEventWatcher(scope, q);
            managementEvWatcher.EventArrived += PowerEventArrive;
            try
            {
                managementEvWatcher.Start();
            }
            catch (ManagementException) { wmiConnected = false; }
        }

        if (!wmiConnected)
        {
            AppLogger.LogToGui(@"Could not connect to Windows Management Instrumentation service.
Suspend support not enabled.", true);
        }
    }

    private void SettingsWrapVM_AppChoiceIndexChanged(object sender, EventArgs e)
    {
        var choice = Global.UseCurrentTheme;
        var current = Application.Current as App;
        current.ChangeTheme(choice);
        trayIconVM.PopulateContextMenu();
    }

    private void SettingsWrapVM_IconChoiceIndexChanged(object sender, EventArgs e)
    {
        trayIconVM.IconSource = Global.iconChoiceResources[Global.UseIconChoice];
    }

    private void MainWinVM_FullTabsEnabledChanged(object sender, EventArgs e)
    {
        settingsWrapVM.ViewEnabled = mainWinVM.FullTabsEnabled;
    }

    private void TrayIconVM_RequestServiceChange(object sender, EventArgs e)
    {
        ChangeService();
    }

    private void LogItems_CollectionChanged(object sender,
        System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
        {
            Dispatcher.BeginInvoke((Action)(() =>
            {
                var count = logListView.Items.Count;
                if (count > 0)
                {
                    logListView.ScrollIntoView(logvm.LogItems[count - 1]);
                }
            }));
        }
    }

    private void ControlServiceStarted(object sender, EventArgs e)
    {
        if (Global.SwipeProfiles)
        {
            ChangeHotkeysStatus(true);
        }

        CheckAutoProfileStatus();
    }

    private void AutoprofileChecker_RequestServiceChange(AutoProfileChecker sender, bool state)
    {
        Dispatcher.BeginInvoke((Action)(() =>
        {
            ChangeService();
        }));
    }

    private void AutoProfVM_AutoProfileSystemChange(AutoProfilesViewModel sender, bool state)
    {
        if (state)
        {
            ChangeAutoProfilesStatus(true);
            autoProfileHolder.AutoProfileColl.CollectionChanged += AutoProfileColl_CollectionChanged;
        }
        else
        {
            ChangeAutoProfilesStatus(false);
            autoProfileHolder.AutoProfileColl.CollectionChanged -= AutoProfileColl_CollectionChanged;
        }
    }

    private void AutoProfileColl_CollectionChanged(object sender,
        System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        CheckAutoProfileStatus();
    }

    private void AutoProfControl_AutoDebugChanged(object sender, EventArgs e)
    {
        autoprofileChecker.AutoProfileDebugLogLevel = autoProfControl.AutoDebug == true ? 1 : 0;
    }

    private void PowerEventArrive(object sender, EventArrivedEventArgs e)
    {
        var evType = Convert.ToInt16(e.NewEvent.GetPropertyValue("EventType"));
        switch (evType)
        {
            // Wakeup from Suspend
            case POWER_RESUME:
            {
                DS4LightBar.shuttingdown = false;
                _controlService.IsSuspending = false;

                if (wasrunning)
                {
                    wasrunning = false;
                    //Thread.Sleep(16000);
                    Dispatcher.Invoke(() =>
                    {
                        StartStopBtn.IsEnabled = false;
                    });

                    _controlService.Start();
                }
            }

                break;
            // Entering Suspend
            case POWER_SUSPEND:
            {
                DS4LightBar.shuttingdown = true;
                _controlService.IsSuspending = true;

                if (_controlService.IsRunning)
                {
                    Dispatcher.Invoke(() =>
                    {
                        StartStopBtn.IsEnabled = false;
                    });

                    _controlService.Stop(immediateUnplug: true);
                    wasrunning = true;

                    Thread.Sleep(1000);
                }
            }

                break;

            default: break;
        }
    }

    private void ChangeHotkeysStatus(bool state)
    {
        if (state)
        {
            hotkeysTimer.Elapsed += HotkeysTimer_Elapsed;
            hotkeysTimer.Start();
        }
        else
        {
            hotkeysTimer.Stop();
            hotkeysTimer.Elapsed -= HotkeysTimer_Elapsed;
        }
    }

    private void HotkeysTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
    {
        hotkeysTimer.Stop();

        if (Global.SwipeProfiles)
        {
            foreach (var item in conLvViewModel.ControllerCol)
                //for (int i = 0; i < 4; i++)
            {
                var slide = _controlService.TouchpadSlide(item.DevIndex);
                if (slide == "left")
                {
                    //int ind = i;
                    Dispatcher.BeginInvoke((Action)(() =>
                    {
                        if (item.SelectedIndex <= 0)
                        {
                            item.SelectedIndex = item.ProfileListCol.Count - 1;
                        }
                        else
                        {
                            item.SelectedIndex--;
                        }
                    }));
                }
                else if (slide == "right")
                {
                    //int ind = i;
                    Dispatcher.BeginInvoke((Action)(() =>
                    {
                        if (item.SelectedIndex == (item.ProfileListCol.Count - 1))
                        {
                            item.SelectedIndex = 0;
                        }
                        else
                        {
                            item.SelectedIndex++;
                        }
                    }));
                }

                if (slide.Contains("t"))
                {
                    //int ind = i;
                    Dispatcher.BeginInvoke((Action)(() =>
                    {
                        var temp = string.Format(Localization.UsingProfile, (item.DevIndex + 1).ToString(), item.SelectedProfile, $"{item.Device.Battery}");
                        ShowHotkeyNotification(temp);
                    }));
                }
            }
        }

        hotkeysTimer.Start();
    }

    private void ShowHotkeyNotification(string message)
    {
        if (!IsActive && (Global.Notifications == 2))
        {
            notifyIcon.ShowNotification(TrayIconViewModel.ballonTitle,
                message, H.NotifyIcon.Core.NotificationIcon.Info);
        }
    }

    private void PrepareForServiceStop(object sender, EventArgs e)
    {
        Dispatcher.BeginInvoke((Action)(() =>
        {
            trayIconVM.ClearContextMenu();
        }));

        ChangeHotkeysStatus(false);
    }

    private void TrayIconVM_RequestOpen(object sender, EventArgs e)
    {
        if (!showAppInTaskbar)
        {
            Show();
        }

        WindowState = WindowState.Normal;
    }

    private void TrayIconVM_RequestShutdown(object sender, EventArgs e)
    {
        contextclose = true;
        Close();
    }

    private void UpdateLastStatusMessage(object sender, DebugEventArgs e)
    {
        lastLogMsg.Message = e.Data;
        lastLogMsg.Warning = e.Warning;
    }

    private void ChangeControllerPanel()
    {
        if (conLvViewModel.ControllerCol.Count == 0)
        {
            controllerLV.Visibility = Visibility.Hidden;
            noContLb.Visibility = Visibility.Visible;
        }
        else
        {
            controllerLV.Visibility = Visibility.Visible;
            noContLb.Visibility = Visibility.Hidden;
        }
    }

    private void ChangeAutoProfilesStatus(bool state)
    {
        if (state)
        {
            autoProfilesTimer.Elapsed += AutoProfilesTimer_Elapsed;
            autoProfilesTimer.Start();
            autoprofileChecker.IsRunning = true;
        }
        else
        {
            autoProfilesTimer.Stop();
            autoProfilesTimer.Elapsed -= AutoProfilesTimer_Elapsed;
            autoprofileChecker.IsRunning = false;
        }
    }

    private void CheckAutoProfileStatus()
    {
        var pathCount = autoProfileHolder.AutoProfileColl.Count;
        var timerEnabled = autoprofileChecker.IsRunning;
        if (pathCount > 0 && !timerEnabled)
        {
            ChangeAutoProfilesStatus(true);
        }
        else if (pathCount == 0 && timerEnabled)
        {
            ChangeAutoProfilesStatus(false);
        }
    }

    private void AutoProfilesTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
    {
        autoProfilesTimer.Stop();
        //Console.WriteLine("Event triggered");
        autoprofileChecker.Process();

        if (autoprofileChecker.IsRunning)
        {
            autoProfilesTimer.Start();
        }
    }

    private void ControllerCol_CollectionChanged(object sender,
        System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        Dispatcher.BeginInvoke((Action)(() =>
        {
            ChangeControllerPanel();
            var newitems = e.NewItems;
            if (newitems != null)
            {
                foreach (CompositeDeviceModel item in newitems)
                {
                    item.LightContext = new ContextMenu();
                    item.AddLightContextItems();
                    item.Device.SyncChange += DS4Device_SyncChange;
                    item.RequestColorPicker += Item_RequestColorPicker;
                    //item.LightContext.Items.Add(new MenuItem() { Header = "Use Profile Color", IsChecked = !item.UseCustomColor });
                    //item.LightContext.Items.Add(new MenuItem() { Header = "Use Custom Color", IsChecked = item.UseCustomColor });
                }
            }

            if (_controlService.IsRunning)
                trayIconVM.PopulateContextMenu();
        }));
    }

    private void Item_RequestColorPicker(CompositeDeviceModel sender)
    {
        var dialog = new ColorPickerWindow();
        dialog.Owner = this;
        dialog.colorPicker.SelectedColor = sender.CustomLightColor;
        dialog.ColorChanged += (sender2, color) =>
        {
            sender.UpdateCustomLightColor(color);
        };
        dialog.ShowDialog();
    }

    private void DS4Device_SyncChange(object sender, EventArgs e)
    {
        Dispatcher.BeginInvoke((Action)(() =>
        {
            trayIconVM.PopulateContextMenu();
        }));
    }

    private void ControlServiceChanged(object sender, EventArgs e)
    {
        //Tester service = sender as Tester;
        var service = sender as IControlService;
        Dispatcher.BeginInvoke((Action)(() =>
        {
            if (service.IsRunning)
            {
                StartStopBtn.Content = Strings.StopText;
            }
            else
            {
                StartStopBtn.Content = Strings.StartText;
            }

            StartStopBtn.IsEnabled = true;
            slotManControl.IsEnabled = service.IsRunning;
        }));
    }

    private void AboutBtn_Click(object sender, RoutedEventArgs e)
    {
        var aboutWin = new About();
        aboutWin.Owner = this;
        aboutWin.ShowDialog();
    }

    private void StartStopBtn_Click(object sender, RoutedEventArgs e)
    {
        ChangeService();
    }

    private async void ChangeService()
    {
        StartStopBtn.IsEnabled = false;
        var root = Application.Current as App;
        //Tester service = root.rootHubtest;
        var service = _controlService;
        var serviceTask = Task.Run(() =>
        {
            if (service.IsRunning)
                service.Stop(immediateUnplug: true);
            else
                service.Start();
        });

        // Log exceptions that might occur
        Util.LogAssistBackgroundTask(serviceTask);
        await serviceTask;
    }

    private void LogListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        var idx = logListView.SelectedIndex;
        if (idx > -1)
        {
            var temp = logvm.LogItems[idx];
            var msgBox = new LogMessageDisplay(temp.Message);
            msgBox.Owner = this;
            msgBox.ShowDialog();
            //MessageBox.Show(temp.Message, "Log");
        }
    }

    private void ClearLogBtn_Click(object sender, RoutedEventArgs e)
    {
        logvm.LogItems.Clear();
    }

    private void MainTabCon_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (mainTabCon.SelectedIndex == 4)
        {
            lastMsgLb.Visibility = Visibility.Hidden;
        }
        else
        {
            lastMsgLb.Visibility = Visibility.Visible;
        }
    }

    private void ProfilesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        newProfListBtn.IsEnabled = true;
        editProfBtn.IsEnabled = true;
        deleteProfBtn.IsEnabled = true;
        renameProfBtn.IsEnabled = true;
        dupProfBtn.IsEnabled = true;
        importProfBtn.IsEnabled = true;
        exportProfBtn.IsEnabled = true;
    }

    private void RunAtStartCk_Click(object sender, RoutedEventArgs e)
    {
        settingsWrapVM.ShowRunStartPanel = runAtStartCk.IsChecked == true ? Visibility.Visible :
            Visibility.Collapsed;
    }

    private void ContStatusImg_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        var img = sender as Image;
        var tag = Convert.ToInt32(img.Tag);
        conLvViewModel.CurrentIndex = tag;
        var item = conLvViewModel.CurrentItem;
        //CompositeDeviceModel item = conLvViewModel.ControllerDict[tag];
        if (item != null)
        {
            item.RequestDisconnect();
        }
    }

    private void ExportLogBtn_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog();
        dialog.AddExtension = true;
        dialog.DefaultExt = ".txt";
        dialog.Filter = "Text Documents (*.txt)|*.txt";
        dialog.Title = "Select Export File";
        // TODO: Expose config dir
        dialog.InitialDirectory = Global.appdatapath;
        if (dialog.ShowDialog() == true)
        {
            var logWriter = new LogWriter(dialog.FileName, logvm.LogItems.ToList());
            logWriter.Process();
        }
    }

    private void IdColumnTxtB_ToolTipOpening(object sender, ToolTipEventArgs e)
    {
        var statusBk = sender as TextBlock;
        var idx = Convert.ToInt32(statusBk.Tag);
        if (idx >= 0)
        {
            var item = conLvViewModel.ControllerDict[idx];
            item.RequestUpdatedTooltipID();
        }
    }

    /// <summary>
    /// Clear and re-populate tray context menu
    /// </summary>
    private void NotifyIcon_TrayRightMouseUp(object sender, RoutedEventArgs e)
    {
        notifyIcon.ContextMenu = trayIconVM.ContextMenu;
    }

    /// <summary>
    /// Change profile based on selection
    /// </summary>
    private void SelectProfCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var box = sender as ComboBox;
        var idx = Convert.ToInt32(box.Tag);
        if (idx > -1 && conLvViewModel.ControllerDict.ContainsKey(idx))
        {
            var item = conLvViewModel.ControllerDict[idx];
            if (item.SelectedIndex > -1)
            {
                item.ChangeSelectedProfile();
                trayIconVM.PopulateContextMenu();
            }
        }
    }

    private void CustomColorPick_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
    {

    }

    private void LightColorBtn_Click(object sender, RoutedEventArgs e)
    {
        var button = sender as Button;
        var idx = Convert.ToInt32(button.Tag);
        var item = conLvViewModel.ControllerDict[idx];
        //(button.ContextMenu.Items[0] as MenuItem).IsChecked = conLvViewModel.ControllerCol[idx].UseCustomColor;
        //(button.ContextMenu.Items[1] as MenuItem).IsChecked = !conLvViewModel.ControllerCol[idx].UseCustomColor;
        button.ContextMenu = item.LightContext;
        button.ContextMenu.IsOpen = true;
    }

    private void MainDS4Window_Closing(object sender, CancelEventArgs e)
    {
        if (editor != null)
        {
            editor.Close();
            e.Cancel = true;
            return;
        }
        else if (contextclose)
        {
            return;
        }
        else if (Global.CloseMini)
        {
            WindowState = WindowState.Minimized;
            e.Cancel = true;
            return;
        }

        // If this method was called directly without sender object then skip the confirmation dialogbox
        if (sender != null && conLvViewModel.ControllerCol.Count > 0)
        {
            var result = MessageBox.Show(Localization.CloseConfirm, Localization.Confirm,
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.No)
            {
                e.Cancel = true;
                return;
            }
        }
    }

    private void MainDS4Window_Closed(object sender, EventArgs e)
    {
        hotkeysTimer.Stop();
        autoProfilesTimer.Stop();
        //autoProfileHolder.Save();
        Util.UnregisterNotify(regHandle);
        Application.Current.Shutdown();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        var source = PresentationSource.FromVisual(this) as HwndSource;
        HookWindowMessages(source);
        source.AddHook(WndProc);
    }

    private bool inHotPlug = false;
    private int hotplugCounter = 0;
    private object hotplugCounterLock = new object();
    private const int DBT_DEVNODES_CHANGED = 0x0007;
    private const int DBT_DEVICEARRIVAL = 0x8000;
    private const int DBT_DEVICEREMOVECOMPLETE = 0x8004;
    public const int WM_COPYDATA = 0x004A;
    private const int HOTPLUG_CHECK_DELAY = 2000;

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam,
        IntPtr lParam, ref bool handled)
    {
        // Handle messages...
        switch (msg)
        {
            case Util.WM_DEVICECHANGE:
            {
                if (Global.runHotPlug)
                {
                    var Type = wParam.ToInt32();
                    if (Type == DBT_DEVICEARRIVAL ||
                        Type == DBT_DEVICEREMOVECOMPLETE)
                    {
                        lock (hotplugCounterLock)
                        {
                            hotplugCounter++;
                        }

                        if (!inHotPlug)
                        {
                            inHotPlug = true;
                            var hotplugTask = Task.Run(() => { InnerHotplug2(); });
                            // Log exceptions that might occur
                            Util.LogAssistBackgroundTask(hotplugTask);
                        }
                    }
                }
                break;
            }
            case WM_COPYDATA:
            {
                // Received InterProcessCommunication (IPC) message. DS4Win command is embedded as a string value in lpData buffer
                try
                {
                    var cds = (User32.COPYDATASTRUCT)Marshal.PtrToStructure(lParam, typeof(User32.COPYDATASTRUCT));
                    if (cds.cbData >= 4 && cds.cbData <= 256)
                    {
                        var tdevice = -1;

                        var buffer = new byte[cds.cbData];
                        Marshal.Copy(cds.lpData, buffer, 0, cds.cbData);
                        var strData = Encoding.ASCII.GetString(buffer).Split('.');

                        if (strData.Length >= 1)
                        {
                            strData[0] = strData[0].ToLower();

                            if (strData[0] == "start")
                            { 
                                if(!_controlService.IsRunning) 
                                    ChangeService();
                            }
                            else if (strData[0] == "stop")
                            {    
                                if (_controlService.IsRunning)
                                    ChangeService();
                            }
                            else if (strData[0] == "cycle")
                            {
                                ChangeService();
                            }
                            else if (strData[0] == "shutdown")
                            {
                                // Force disconnect all gamepads before closing the app to avoid "Are you sure you want to close the app" messagebox
                                if (_controlService.IsRunning)
                                    ChangeService();

                                // Call closing method and let it to close editor wnd (if it is open) before proceeding to the actual "app closed" handler
                                MainDS4Window_Closing(null, new CancelEventArgs());
                                MainDS4Window_Closed(this, new EventArgs());
                            }
                            else if (strData[0] == "disconnect")
                            {
                                // Command syntax: Disconnect[.device#] (fex Disconnect.1)
                                // Disconnect all wireless controllers. ex. (Disconnect)
                                if (strData.Length == 1)
                                {
                                    // Attempt to disconnect all wireless controllers
                                    // Opt to make copy of Dictionary before iterating over contents
                                    var dictCopy = new Dictionary<int, CompositeDeviceModel>(conLvViewModel.ControllerDict);
                                    foreach(var pair in dictCopy)
                                    {
                                        pair.Value.RequestDisconnect();
                                    }
                                }
                                else
                                {
                                    // Attempt to disconnect one wireless controller
                                    if (int.TryParse(strData[1], out tdevice)) tdevice--;

                                    if (conLvViewModel.ControllerDict.TryGetValue(tdevice, out var model))
                                    {
                                        model.RequestDisconnect();
                                    }
                                }
                            }
                            else if ((strData[0] == "changeledcolor") && strData.Length >= 5)
                            {
                                // Command syntax: changeledcolor.device#.red.gree.blue (ex changeledcolor.1.255.0.0)
                                if (int.TryParse(strData[1], out tdevice))
                                    tdevice--;
                                if (tdevice >= 0 && tdevice < IControlService.MAX_DS4_CONTROLLER_COUNT)
                                {
                                    byte.TryParse(strData[2], out var red);
                                    byte.TryParse(strData[3], out var green);
                                    byte.TryParse(strData[4], out var blue);

                                    conLvViewModel.ControllerCol[tdevice].UpdateCustomLightColor(Color.FromRgb(red, green, blue));
                                }

                            }
                            else if ((strData[0] == "loadprofile" || strData[0] == "loadtempprofile") && strData.Length >= 3)
                            {
                                // Command syntax: LoadProfile.device#.profileName (fex LoadProfile.1.GameSnake or LoadTempProfile.1.WebBrowserSet)
                                if (int.TryParse(strData[1], out tdevice)) tdevice--;

                                if (tdevice >= 0 && tdevice < IControlService.MAX_DS4_CONTROLLER_COUNT &&
                                    File.Exists(Global.appdatapath + "\\Profiles\\" + strData[2] + ".xml"))
                                {
                                    if (strData[0] == "loadprofile")
                                    {
                                        var idx = _profileListService.ProfileList.ProfileListCol.Select((item, index) => new { item, index }).
                                            Where(x => x.item.Name == strData[2]).Select(x => x.index).DefaultIfEmpty(-1).First();

                                        if (idx >= 0 && tdevice < conLvViewModel.ControllerCol.Count)
                                        {
                                            conLvViewModel.ControllerCol[tdevice].ChangeSelectedProfile(strData[2]);
                                        }
                                        else
                                        {
                                            // Preset profile name for later loading
                                            Global.ProfilePath[tdevice] = strData[2];
                                            //Global.LoadProfile(tdevice, true, Program.rootHub);
                                        }
                                    }
                                    else
                                    {
                                        Global.LoadTempProfile(tdevice, strData[2], true, _controlService);
                                    }

                                    var device = conLvViewModel.ControllerCol[tdevice].Device;
                                    if (device != null)
                                    {
                                        var prolog = string.Format(Localization.UsingProfile, (tdevice + 1).ToString(), strData[2], $"{device.Battery}");
                                        _controlService.LogDebug(prolog);
                                    }
                                }
                            }
                            else if (strData[0] == "outputslot" && strData.Length >= 3)
                            {
                                // Command syntax: 
                                //    OutputSlot.slot#.Unplug
                                //    OutputSlot.slot#.PlugDS4
                                //    OutputSlot.slot#.PlugX360
                                if (int.TryParse(strData[1], out tdevice))
                                    tdevice--;

                                if (tdevice >= 0 && tdevice < IControlService.MAX_DS4_CONTROLLER_COUNT)
                                {
                                    strData[2] = strData[2].ToLower();
                                    var slotDevice = _controlService.OutputslotMan.OutputSlots[tdevice];
                                    if (strData[2] == "unplug")
                                        _controlService.DetachUnboundOutDev(slotDevice);
                                    else if (strData[2] == "plugds4")
                                        _controlService.AttachUnboundOutDev(slotDevice, OutContType.DS4);
                                    else if (strData[2] == "plugx360")
                                        _controlService.AttachUnboundOutDev(slotDevice, OutContType.X360);
                                }
                            }
                            else if (strData[0] == "query" && strData.Length >= 3)
                            {
                                string propName;
                                var propValue = String.Empty;

                                // Command syntax: QueryProfile.device#.Name (fex "Query.1.ProfileName" would print out the name of the active profile in controller 1)
                                if (int.TryParse(strData[1], out tdevice))
                                    tdevice--;

                                if (tdevice >= 0 && tdevice < IControlService.MAX_DS4_CONTROLLER_COUNT)
                                {
                                    // Name of the property to query from a profile or DS4Windows app engine
                                    propName = strData[2].ToLower();

                                    if (propName == "profilename")
                                    {
                                        if (Global.useTempProfile[tdevice])
                                            propValue = Global.tempprofilename[tdevice];
                                        else
                                            propValue = Global.ProfilePath[tdevice];
                                    }
                                    else if (propName == "outconttype")
                                        propValue = Global.OutContType[tdevice].ToString();
                                    else if (propName == "activeoutdevtype")
                                        propValue = Global.activeOutDevType[tdevice].ToString();
                                    else if (propName == "usedinputonly")
                                        propValue = Global.useDInputOnly[tdevice].ToString();

                                    else if (propName == "devicevidpid" && _controlService.DS4Controllers[tdevice] != null)
                                        propValue = $"VID={_controlService.DS4Controllers[tdevice].HidDevice.Attributes.VendorHexId}, PID={_controlService.DS4Controllers[tdevice].HidDevice.Attributes.ProductHexId}";
                                    else if (propName == "devicepath" && _controlService.DS4Controllers[tdevice] != null)
                                        propValue = _controlService.DS4Controllers[tdevice].HidDevice.DevicePath;
                                    else if (propName == "macaddress" && _controlService.DS4Controllers[tdevice] != null)
                                        propValue = _controlService.DS4Controllers[tdevice].MacAddress;
                                    else if (propName == "displayname" && _controlService.DS4Controllers[tdevice] != null)
                                        propValue = _controlService.DS4Controllers[tdevice].DisplayName;
                                    else if (propName == "conntype" && _controlService.DS4Controllers[tdevice] != null)
                                        propValue = _controlService.DS4Controllers[tdevice].ConnectionType.ToString();
                                    else if (propName == "exclusivestatus" && _controlService.DS4Controllers[tdevice] != null)
                                        propValue = _controlService.DS4Controllers[tdevice].CurrentExclusiveStatus.ToString();
                                    else if (propName == "battery" && _controlService.DS4Controllers[tdevice] != null)
                                        propValue = _controlService.DS4Controllers[tdevice].Battery.ToString();
                                    else if (propName == "charging" && _controlService.DS4Controllers[tdevice] != null)
                                        propValue = _controlService.DS4Controllers[tdevice].Charging.ToString();
                                    else if (propName == "outputslottype")
                                        propValue = _controlService.OutputslotMan.OutputSlots[tdevice].CurrentType.ToString();
                                    else if (propName == "outputslotpermanenttype")
                                        propValue = _controlService.OutputslotMan.OutputSlots[tdevice].PermanentType.ToString();
                                    else if (propName == "outputslotattachedstatus")
                                        propValue = _controlService.OutputslotMan.OutputSlots[tdevice].CurrentAttachedStatus.ToString();
                                    else if (propName == "outputslotinputbound")
                                        propValue = _controlService.OutputslotMan.OutputSlots[tdevice].CurrentInputBound.ToString();

                                    else if (propName == "apprunning")
                                        propValue = _controlService.IsRunning.ToString(); // Controller idx value is ignored, but it still needs to be in 1..4 range in a cmdline call
                                }

                                // Write out the property value to MMF result data file and notify a client process that the data is available
                                ((Application.Current) as App).WriteIPCResultDataMMF(propValue);
                            }
                        }
                    }
                }
                catch
                {
                    // Eat all exceptions in WM_COPYDATA because exceptions here are not fatal for DS4Windows background app
                }
                break;
            }
            default: break;
        }

        return IntPtr.Zero;
    }

    private void InnerHotplug2()
    {
        inHotPlug = true;

        var loopHotplug = false;
        lock (hotplugCounterLock)
        {
            loopHotplug = hotplugCounter > 0;
        }

        _controlService.UpdateHidHiddenAttributes();
        while (loopHotplug == true)
        {
            Thread.Sleep(HOTPLUG_CHECK_DELAY);
            _controlService.HotPlug();
            lock (hotplugCounterLock)
            {
                hotplugCounter--;
                loopHotplug = hotplugCounter > 0;
            }
        }

        inHotPlug = false;
    }

    private void HookWindowMessages(HwndSource source)
    {
        var hidGuid = new Guid();
        Hid.HidD_GetHidGuid(ref hidGuid);
        var result = Util.RegisterNotify(source.Handle, hidGuid, ref regHandle);
        if (!result)
        {
            Application.Current.Shutdown();
        }
    }

    private void ProfEditSBtn_Click(object sender, RoutedEventArgs e)
    {
        var temp = sender as Control;
        var idx = Convert.ToInt32(temp.Tag);
        controllerLV.SelectedIndex = idx;
        var item = conLvViewModel.CurrentItem;

        if (item != null)
        {
            var entity = _profileListService.ProfileList.ProfileListCol[item.SelectedIndex];
            ShowProfileEditor(idx, entity);
            mainTabCon.SelectedIndex = 1;
        }
    }

    private void NewProfBtn_Click(object sender, RoutedEventArgs e)
    {
        var temp = sender as Control;
        var idx = Convert.ToInt32(temp.Tag);
        controllerLV.SelectedIndex = idx;
        ShowProfileEditor(idx, null);
        mainTabCon.SelectedIndex = 1;
        //controllerLV.Focus();
    }

    // Ex Mode Re-Enable
    private async void HideDS4ContCk_Click(object sender, RoutedEventArgs e)
    {
        StartStopBtn.IsEnabled = false;
        //bool checkStatus = hideDS4ContCk.IsChecked == true;
        hideDS4ContCk.IsEnabled = false;
        var serviceTask = Task.Run(() =>
        {
            _controlService.Stop();
            _controlService.Start();
        });

        // Log exceptions that might occur
        Util.LogAssistBackgroundTask(serviceTask);
        await serviceTask;

        hideDS4ContCk.IsEnabled = true;
        StartStopBtn.IsEnabled = true;
    }

    private void UseOscServerCk_Click(object sender, RoutedEventArgs e)
    {
        var status = useOscServerCk.IsChecked == true;
        _controlService.ChangeOSCListenerStatus(status);
    }

    private void UseOscSenderCk_Click(object sender, RoutedEventArgs e)
    {
        var status = useOscSenderCk.IsChecked == true;
        _controlService.ChangeOSCSenderStatus(status);
    }

    private async void UseUdpServerCk_Click(object sender, RoutedEventArgs e)
    {
        var status = useUdpServerCk.IsChecked == true;
        if (!status)
        {
            _controlService.ChangeMotionEventStatus(status);
            await Task.Delay(100).ContinueWith((t) =>
            {
                _controlService.ChangeUDPStatus(status);
            });
        }
        else
        {
            _controlService.ChangeUDPStatus(status);
            await Task.Delay(100).ContinueWith((t) =>
            {
                _controlService.ChangeMotionEventStatus(status);
            });
        }
    }

    private void ProfFolderBtn_Click(object sender, RoutedEventArgs e)
    {
        var startInfo = new ProcessStartInfo(Global.appdatapath + "\\Profiles");
        startInfo.UseShellExecute = true;
        try
        {
            using (var temp = Process.Start(startInfo))
            {
            }
        }
        catch { }
    }

    private void ControlPanelBtn_Click(object sender, RoutedEventArgs e)
    {
        Process.Start("control", "joy.cpl");
    }

    private async void DriverSetupBtn_Click(object sender, RoutedEventArgs e)
    {
        StartStopBtn.IsEnabled = false;
        await Task.Run(() =>
        {
            if (_controlService.IsRunning)
                _controlService.Stop();
        });

        StartStopBtn.IsEnabled = true;
        var startInfo = new ProcessStartInfo();
        startInfo.FileName = Global.exelocation;
        startInfo.Arguments = "-driverinstall";
        startInfo.Verb = "runas";
        startInfo.UseShellExecute = true;
        try
        {
            using (var temp = Process.Start(startInfo))
            {
                temp.WaitForExit();
                Global.RefreshHidHideInfo();
                Global.RefreshFakerInputInfo();
                _controlService.RefreshOutputKBMHandler();

                settingsWrapVM.DriverCheckRefresh();
            }
        }
        catch { }
    }

    private async void CheckUpdatesBtn_Click(object sender, RoutedEventArgs e)
    {
        await mainWinVM.DownloadUpstreamVersionInfoAsync();
        CheckVersionAsync(true);
    }

    private void ImportProfBtn_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog();
        dialog.AddExtension = true;
        dialog.DefaultExt = ".xml";
        dialog.Filter = "DS4Windows Profile (*.xml)|*.xml";
        dialog.Title = "Select Profile to Import File";
        if (Global.appdatapath != Global.exedirpath)
            dialog.InitialDirectory = Path.Combine(Global.appDataPpath, "Profiles");
        else
            dialog.InitialDirectory = Global.exedirpath + @"\Profiles\";

        if (dialog.ShowDialog() == true)
        {
            var files = dialog.FileNames;
            for (int i = 0, arlen = files.Length; i < arlen; i++)
            {
                var profilename = Path.GetFileName(files[i]);
                var basename = Path.GetFileNameWithoutExtension(files[i]);
                File.Copy(dialog.FileNames[i], Global.appdatapath + "\\Profiles\\" + profilename, true);
                _profileListService.ProfileList.AddProfileSort(basename);
            }
        }
    }

    private void ExportProfBtn_Click(object sender, RoutedEventArgs e)
    {
        if (profilesListBox.SelectedIndex >= 0)
        {
            var dialog = new SaveFileDialog();
            dialog.AddExtension = true;
            dialog.DefaultExt = ".xml";
            dialog.Filter = "DS4Windows Profile (*.xml)|*.xml";
            dialog.Title = "Select Profile to Export File";
            Stream stream;
            var idx = profilesListBox.SelectedIndex;
            var profile = new StreamReader(Global.appdatapath + "\\Profiles\\" + _profileListService.ProfileList.ProfileListCol[idx].Name + ".xml").BaseStream;
            if (dialog.ShowDialog() == true)
            {
                if ((stream = dialog.OpenFile()) != null)
                {
                    profile.CopyTo(stream);
                    profile.Close();
                    stream.Close();
                }
            }
        }
    }

    private void DupProfBtn_Click(object sender, RoutedEventArgs e)
    {
        var filename = "";
        if (profilesListBox.SelectedIndex >= 0)
        {
            var idx = profilesListBox.SelectedIndex;
            filename = _profileListService.ProfileList.ProfileListCol[idx].Name;
            dupBox.OldFilename = filename;
            dupBoxBar.Visibility = Visibility.Visible;
            dupBox.Save -= DupBox_Save;
            dupBox.Cancel -= DupBox_Cancel;
            dupBox.Save += DupBox_Save;
            dupBox.Cancel += DupBox_Cancel;
        }
    }

    private void DupBox_Cancel(object sender, EventArgs e)
    {
        dupBoxBar.Visibility = Visibility.Collapsed;
    }

    private void DupBox_Save(DupBox sender, string profilename)
    {
        _profileListService.ProfileList.AddProfileSort(profilename);
        dupBoxBar.Visibility = Visibility.Collapsed;
    }

    private void DeleteProfBtn_Click(object sender, RoutedEventArgs e)
    {
        if (profilesListBox.SelectedIndex >= 0)
        {
            var idx = profilesListBox.SelectedIndex;
            var entity = _profileListService.ProfileList.ProfileListCol[idx];
            var filename = entity.Name;
            if (MessageBox.Show(Localization.ProfileCannotRestore.Replace("*Profile name*", "\"" + filename + "\""),
                    Localization.DeleteProfile,
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                entity.DeleteFile();
                _profileListService.ProfileList.ProfileListCol.RemoveAt(idx);
            }
        }
    }

    private void SelectProfCombo_KeyDown(object sender, KeyEventArgs e)
    {
        e.Handled = true;
    }

    private void MainDS4Window_StateChanged(object _sender, EventArgs _e)
    {
        CheckMinStatus();
    }

    public void CheckMinStatus()
    {
        var minToTask = Global.MinToTaskbar;
        if (WindowState == WindowState.Minimized && !minToTask)
        {
            Hide();
            showAppInTaskbar = false;
        }
        else if (WindowState == WindowState.Normal && !minToTask)
        {
            Show();
            showAppInTaskbar = true;
        }
    }

    private void MainDS4Window_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (WindowState != WindowState.Minimized && preserveSize)
        {
            Global.FormWidth = Convert.ToInt32(Width);
            Global.FormHeight = Convert.ToInt32(Height);
        }
    }

    private void MainDS4Window_LocationChanged(object sender, EventArgs e)
    {
        int left = Convert.ToInt32(Left), top = Convert.ToInt32(Top);
        if (left >= 0 && top >= 0)
        {
            Global.FormLocationX = left;
            Global.FormLocationY = top;
        }
    }

    private void NotifyIcon_TrayMiddleMouseDown(object sender, RoutedEventArgs e)
    {
        contextclose = true;
        Close();
    }

    private void SwipeTouchCk_Click(object sender, RoutedEventArgs e)
    {
        var status = swipeTouchCk.IsChecked == true;
        ChangeHotkeysStatus(status);
    }

    private void EditProfBtn_Click(object sender, RoutedEventArgs e)
    {
        if (profilesListBox.SelectedIndex >= 0)
        {
            var entity = _profileListService.ProfileList.ProfileListCol[profilesListBox.SelectedIndex];
            ShowProfileEditor(Global.TEST_PROFILE_INDEX, entity);
        }
    }

    private void ProfileEditor_Closed(object sender, EventArgs e)
    {
        profDockPanel.Children.Remove(editor);
        profOptsToolbar.Visibility = Visibility.Visible;
        profilesListBox.Visibility = Visibility.Visible;
        preserveSize = true;
        if (!editor.KeepSize)
        {
            Width = oldSize.Width;
            Height = oldSize.Height;
        }
        else
        {
            oldSize = new Size(Width, Height);
        }

        editor = null;
        mainTabCon.SelectedIndex = 0;
        mainWinVM.FullTabsEnabled = true;
        //Task.Run(() => GC.Collect(0, GCCollectionMode.Forced, false));
    }

    private void NewProfListBtn_Click(object sender, RoutedEventArgs e)
    {
        ShowProfileEditor(Global.TEST_PROFILE_INDEX, null);
    }

    private void ShowProfileEditor(int device, ProfileEntity entity = null)
    {
        if (editor == null)
        {
            profOptsToolbar.Visibility = Visibility.Collapsed;
            profilesListBox.Visibility = Visibility.Collapsed;
            mainWinVM.FullTabsEnabled = false;

            preserveSize = false;
            oldSize.Width = Width;
            oldSize.Height = Height;
            if (Width < DEFAULT_PROFILE_EDITOR_WIDTH)
            {
                Width = DEFAULT_PROFILE_EDITOR_WIDTH;
            }

            if (Height < DEFAULT_PROFILE_EDITOR_HEIGHT)
            {
                Height = DEFAULT_PROFILE_EDITOR_HEIGHT;
            }

            editor = _serviceProvider.GetRequiredService<IProfileEditorFactory>().Create(device);
            editor.CreatedProfile += Editor_CreatedProfile;
            editor.Closed += ProfileEditor_Closed;
            profDockPanel.Children.Add(editor);
            editor.Reload(device, entity);
        }
            
    }

    private void Editor_CreatedProfile(ProfileEditor sender, string profile)
    {
        _profileListService.ProfileList.AddProfileSort(profile);
        var devnum = sender.DeviceNum;
        if (devnum >= 0 && devnum+1 <= conLvViewModel.ControllerCol.Count)
        {
            conLvViewModel.ControllerCol[devnum].ChangeSelectedProfile(profile);
        }
    }

    private void NotifyIcon_TrayMouseDoubleClick(object sender, RoutedEventArgs e)
    {
        if (!showAppInTaskbar)
        {
            Show();
        }

        WindowState = WindowState.Normal;
    }

    private void ProfilesListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (profilesListBox.SelectedIndex >= 0)
        {
            var entity = _profileListService.ProfileList.ProfileListCol[profilesListBox.SelectedIndex];
            ShowProfileEditor(Global.TEST_PROFILE_INDEX, entity);
        }
    }

    private void Html5GameBtn_Click(object sender, RoutedEventArgs e)
    {
        Util.StartProcessHelper("https://gamepad-tester.com/");
    }

    private void HidHideBtn_Click(object sender, RoutedEventArgs e)
    {
        var path = Util.GetHidHideClientPath();
        if (!string.IsNullOrEmpty(path))
        {
            try
            {
                var startInfo = new ProcessStartInfo(path);
                startInfo.UseShellExecute = true;
                using (var proc = Process.Start(startInfo)) { }
            }
            catch { }
        }
    }

    private void FakeExeNameExplainBtn_Click(object sender, RoutedEventArgs e)
    {
        var message = Strings.CustomExeNameInfo;
        MessageBox.Show(message, "Custom Exe Name Info", MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void XinputCheckerBtn_Click(object sender, RoutedEventArgs e)
    {
        var path = Path.Combine(Global.exedirpath, "Tools",
            "XInputChecker", "XInputChecker.exe");

        if (File.Exists(path))
        {
            try
            {
                using (var proc = Process.Start(path)) { }
            }
            catch { }
        }
    }

    private void ChecklogViewBtn_Click(object sender, RoutedEventArgs e)
    {
        var changelogWin =_serviceProvider.GetRequiredService<ChangelogWindow>();
        changelogWin.ShowDialog();
    }

    private void DeviceOptionSettingsBtn_Click(object sender, RoutedEventArgs e)
    {
        var optsWindow =
            new ControllerRegisterOptionsWindow(_controlService.DeviceOptions, _controlService);

        optsWindow.Owner = this;
        optsWindow.Show();
    }

    private void RenameProfBtn_Click(object sender, RoutedEventArgs e)
    {
        if (profilesListBox.SelectedIndex >= 0)
        {
            var idx = profilesListBox.SelectedIndex;
            var entity = _profileListService.ProfileList.ProfileListCol[idx];
            var filename = Path.Combine(Global.appdatapath,
                "Profiles", $"{entity.Name}.xml");

            // Disallow renaming Default profile
            if (entity.Name != "Default" &&
                File.Exists(filename))
            {
                var renameWin = new RenameProfileWindow();
                renameWin.ChangeProfileName(entity.Name);
                var result = renameWin.ShowDialog();
                if (result.HasValue && result.Value)
                {
                    entity.RenameProfile(renameWin.RenameProfileVM.ProfileName);
                    trayIconVM.PopulateContextMenu();
                }
            }
        }
    }
}

public class ImageLocationPaths
{
    public string NewProfile { get => $"/DS4Windows;component/Resources/{Application.Current.FindResource("NewProfileImg")}"; }
    public event EventHandler NewProfileChanged;

    public string EditProfile { get => $"/DS4Windows;component/Resources/{Application.Current.FindResource("EditImg")}"; }
    public event EventHandler EditProfileChanged;

    public string DeleteProfile { get => $"/DS4Windows;component/Resources/{Application.Current.FindResource("DeleteImg")}"; }
    public event EventHandler DeleteProfileChanged;

    public string DuplicateProfile { get => $"/DS4Windows;component/Resources/{Application.Current.FindResource("CopyImg")}"; }
    public event EventHandler DuplicateProfileChanged;

    public string ExportProfile { get => $"/DS4Windows;component/Resources/{Application.Current.FindResource("ExportImg")}"; }
    public event EventHandler ExportProfileChanged;

    public string ImportProfile { get => $"/DS4Windows;component/Resources/{Application.Current.FindResource("ImportImg")}"; }
    public event EventHandler ImportProfileChanged;

    public ImageLocationPaths()
    {
        var current = Application.Current as App;
        if (current != null)
        {
            current.ThemeChanged += Current_ThemeChanged;
        }
    }

    private void Current_ThemeChanged(object sender, EventArgs e)
    {
        NewProfileChanged?.Invoke(this, EventArgs.Empty);
        EditProfileChanged?.Invoke(this, EventArgs.Empty);
        DeleteProfileChanged?.Invoke(this, EventArgs.Empty);
        DuplicateProfileChanged?.Invoke(this, EventArgs.Empty);
        ExportProfileChanged?.Invoke(this, EventArgs.Empty);
        ImportProfileChanged?.Invoke(this, EventArgs.Empty);
    }
}