using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Controls;

namespace DS4WinWPF.DS4Forms.ViewModels;

public class TrayIconViewModel
{
    private string tooltipText = "DS4Windows";
    private string iconSource;
    public const string ballonTitle = "DS4Windows";
    public static string trayTitle = $"DS4Windows v{Global.exeversion}";
    private ContextMenu contextMenu;
    private MenuItem changeServiceItem;
    private MenuItem openItem;
    private MenuItem minimizeItem;
    private MenuItem openProgramItem;
    private MenuItem closeItem;


    public string TooltipText { 
        get => tooltipText;
        set
        {
            var temp = value;
            if (value.Length > 63) temp = value[..63];
            if (tooltipText == temp) 
                return;
            tooltipText = temp;
            TooltipTextChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    public event EventHandler TooltipTextChanged;

    public string IconSource { get => iconSource;
        set
        {
            if (iconSource == value) return;
            iconSource = value;
            IconSourceChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public ContextMenu ContextMenu { get => contextMenu; }

    public event EventHandler IconSourceChanged;
    public event EventHandler RequestShutdown;
    public event EventHandler RequestOpen;
    public event EventHandler RequestMinimize;
    public event EventHandler RequestServiceChange;

    private ReaderWriterLockSlim _colLocker = new();
    private List<ControllerHolder> controllerList = new();
    private readonly IControlService controlService;
    private readonly IProfileListService profileListService;

    public delegate void ProfileSelectedHandler(TrayIconViewModel sender,
        ControllerHolder item, string profile);
    public event ProfileSelectedHandler ProfileSelected;

    //public TrayIconViewModel(Tester tester)
    public TrayIconViewModel(IControlService controlService, IProfileListService profileListService)
    {
        this.profileListService = profileListService;
        this.controlService = controlService;
        contextMenu = new ContextMenu();
        iconSource = Global.iconChoiceResources[Global.UseIconChoice];
        changeServiceItem = new MenuItem { Header = "Start" };
        changeServiceItem.Click += ChangeControlServiceItem_Click;
        changeServiceItem.IsEnabled = false;

        openItem = new MenuItem { Header = "Open", FontWeight = FontWeights.Bold };
        openItem.Click += OpenMenuItem_Click;
        minimizeItem = new MenuItem { Header = "Minimize" };
        minimizeItem.Click += MinimizeMenuItem_Click;
        openProgramItem = new MenuItem { Header = "Open Program Folder" };
        openProgramItem.Click += OpenProgramFolderItem_Click;
        closeItem = new MenuItem { Header = "Exit (Middle Mouse)" };
        closeItem.Click += ExitMenuItem_Click;

        PopulateControllerList();
        PopulateToolText();
        PopulateContextMenu();
        SetupEvents();
        profileListService.ProfileList.ProfileListCol.CollectionChanged += ProfileListCol_CollectionChanged;

        this.controlService.ServiceStarted += BuildControllerList;
        this.controlService.ServiceStarted += HookEvents;
        this.controlService.ServiceStarted += StartPopulateText;
        this.controlService.PreServiceStop += ClearToolText;
        this.controlService.PreServiceStop += UnhookEvents;
        this.controlService.PreServiceStop += ClearControllerList;
        this.controlService.RunningChanged += Service_RunningChanged;
        this.controlService.HotplugController += Service_HotplugController;
        /*tester.StartControllers += HookBatteryUpdate;
        tester.StartControllers += StartPopulateText;
        tester.PreRemoveControllers += ClearToolText;
        tester.HotplugControllers += HookBatteryUpdate;
        tester.HotplugControllers += StartPopulateText;
        */
    }

    private void Service_RunningChanged(object sender, EventArgs e)
    {
        var serviceStatus = controlService.IsRunning ? "Stop" : "Start";
        Application.Current?.Dispatcher?.BeginInvoke(() =>
        {
            changeServiceItem.Header = serviceStatus;
            changeServiceItem.IsEnabled = true;
        });
    }

    private void ClearControllerList(object sender, EventArgs e)
    {
        _colLocker.EnterWriteLock();
        controllerList.Clear();
        _colLocker.ExitWriteLock();
    }

    private void UnhookEvents(object sender, EventArgs e)
    {
        _colLocker.EnterReadLock();
        foreach (var currentDev in controllerList.Select(holder => holder.Device))
            RemoveDeviceEvents(currentDev);
        _colLocker.ExitReadLock();
    }

    private void Service_HotplugController(ControlService sender, DS4Device device, int index)
    {
        SetupDeviceEvents(device);
        _colLocker.EnterWriteLock();
        controllerList.Add(new ControllerHolder(device, index));
        _colLocker.ExitWriteLock();
    }

    private void ProfileListCol_CollectionChanged(object sender,
        System.Collections.Specialized.NotifyCollectionChangedEventArgs e) => PopulateContextMenu();

    private void BuildControllerList(object sender, EventArgs e) => PopulateControllerList();

    public void PopulateContextMenu()
    {
        contextMenu.Items.Clear();
        var items = contextMenu.Items;
        MenuItem item;

        using (var _ = new ReadLocker(_colLocker))
        {
            foreach (var (_, idx) in controllerList.Select((holder, idx) => (holder.Device, idx)))
            {
                item = new MenuItem
                {
                    Header = $"Controller {idx + 1}",
                    Tag = idx
                };
                //item.ContextMenu = new ContextMenu();
                var subItems = item.Items;
                var currentProfile = Global.ProfilePath[idx];
                foreach (var entry in profileListService.ProfileList.ProfileListCol)
                {
                    // Need to escape profile name to disable Access Keys for control
                    var name = entry.Name;
                    name = Regex.Replace(name, "_{1}", "__");
                    var temp = new MenuItem
                    {
                        Header = name,
                        Tag = idx,
                    };
                    temp.Click += ProfileItem_Click;
                    if (entry.Name == currentProfile)
                    {
                        temp.IsChecked = true;
                    }

                    subItems.Add(temp);
                }

                items.Add(item);
            }

            item = new MenuItem { Header = "Disconnect Menu" };
            
            foreach (var (device, idx) in controllerList.Select((holder, idx) => (holder.Device, idx)))
            {
                if (!device.Synced || device.Charging) 
                    continue;
                
                var subitem = new MenuItem() { Header = $"Disconnect Controller {idx + 1}" };
                subitem.Click += DisconnectMenuItem_Click;
                subitem.Tag = idx;
                
                item.IsEnabled = idx != 0;
                item.Items.Add(subitem);
            }
        }

        items.Add(item);
        items.Add(new Separator());
        PopulateStaticItems();
    }

    private void ChangeControlServiceItem_Click(object sender, RoutedEventArgs e)
    {
        changeServiceItem.IsEnabled = false;
        RequestServiceChange?.Invoke(this, EventArgs.Empty);
    }

    private void OpenProgramFolderItem_Click(object sender, RoutedEventArgs e)
    {
        var startInfo = new ProcessStartInfo(Global.exedirpath)
        {
            UseShellExecute = true
        };
        using (var _ = Process.Start(startInfo))
        {
        }
    }

    private void OpenMenuItem_Click(object sender, RoutedEventArgs e) => 
        RequestOpen?.Invoke(this, EventArgs.Empty);

    private void MinimizeMenuItem_Click(object sender, RoutedEventArgs e) => 
        RequestMinimize?.Invoke(this, EventArgs.Empty);

    private void ProfileItem_Click(object sender, RoutedEventArgs e)
    {
        var item = (sender as MenuItem)!;
        var idx = Convert.ToInt32(item.Tag);
        var holder = controllerList[idx];
        // Un-escape underscores is MenuItem header. Header holds the profile name
        var tempProfileName = Regex.Replace(item.Header.ToString()!,
            "_{2}", "_");
        ProfileSelected?.Invoke(this, holder, tempProfileName);
    }

    private void DisconnectMenuItem_Click(object sender,
        RoutedEventArgs e)
    {
        var item = (sender as MenuItem)!;
        var idx = Convert.ToInt32(item.Tag);
        var holder = controllerList[idx];
        var tempDev = holder?.Device;
        if (tempDev is not { Synced: true, Charging: false }) 
            return;
        
        switch (tempDev.ConnectionType)
        {
            case ConnectionType.BT:
                //tempDev.StopUpdate();
                tempDev.DisconnectBT();
                break;
            case ConnectionType.SONYWA:
                tempDev.DisconnectDongle();
                break;
        }

        //controllerList[idx] = null;
    }

    private void PopulateControllerList()
    {
        //IEnumerable<DS4Device> devices = DS4Devices.getDS4Controllers();
        var idx = 0;
        _colLocker.EnterWriteLock();
        foreach (var currentDev in controlService.SlotManager.ControllerColl)
        {
            controllerList.Add(new ControllerHolder(currentDev, idx));
            idx++;
        }
        _colLocker.ExitWriteLock();
    }

    private void StartPopulateText(object sender, EventArgs e)
    {
        PopulateToolText();
        //PopulateContextMenu();
    }

    private void PopulateToolText()
    {
        var items = new List<string>();
        items.Add(trayTitle);
        //IEnumerable<DS4Device> devices = DS4Devices.getDS4Controllers();
        var idx = 1;
        //foreach (DS4Device currentDev in devices)
        _colLocker.EnterReadLock();
        foreach (var holder in controllerList)
        {
            var currentDev = holder.Device;
            items.Add($"{idx}: {currentDev.ConnectionType} {currentDev.Battery}%{(currentDev.Charging ? "+" : "")}");
            idx++;
        }
        _colLocker.ExitReadLock();

        TooltipText = string.Join("\n", items);
    }

    private void SetupEvents()
    {
        //IEnumerable<DS4Device> devices = DS4Devices.getDS4Controllers();
        //foreach (DS4Device currentDev in devices)
        _colLocker.EnterReadLock();
        foreach (var holder in controllerList)
        {
            var currentDev = holder.Device;
            SetupDeviceEvents(currentDev);
        }
        _colLocker.ExitReadLock();
    }

    private void SetupDeviceEvents(DS4Device device)
    {
        device.BatteryChanged += UpdateForBattery;
        device.ChargingChanged += UpdateForBattery;
        device.Removal += CurrentDev_Removal;
    }

    private void RemoveDeviceEvents(DS4Device device)
    {
        device.BatteryChanged -= UpdateForBattery;
        device.ChargingChanged -= UpdateForBattery;
        device.Removal -= CurrentDev_Removal;
    }

    private void CurrentDev_Removal(object sender, EventArgs e)
    {
        var currentDev = sender as DS4Device;
        ControllerHolder item = null;
        var idx = 0;

        using (var _ = new WriteLocker(_colLocker))
        {
            foreach (var holder in controllerList)
            {
                if (currentDev == holder.Device)
                {
                    item = holder;
                    break;
                }

                idx++;
            }

            if (item != null)
            {
                controllerList.RemoveAt(idx);
                RemoveDeviceEvents(currentDev);
            }
        }

        PopulateToolText();
    }

    private void HookEvents(object sender, EventArgs e) => SetupEvents();

    private void UpdateForBattery(object sender, EventArgs e) => PopulateToolText();

    private void ClearToolText(object sender, EventArgs e)
    {
        TooltipText = "DS4Windows";
        //contextMenu.Items.Clear();
    }

    private void PopulateStaticItems()
    {
        var items = contextMenu.Items;
        items.Add(changeServiceItem);
        items.Add(openItem);
        items.Add(minimizeItem);
        items.Add(openProgramItem);
        items.Add(new Separator());
        items.Add(closeItem);
    }

    public void ClearContextMenu()
    {
        contextMenu.Items.Clear();
        PopulateStaticItems();
    }

    private void ExitMenuItem_Click(object sender, RoutedEventArgs e) => 
        RequestShutdown?.Invoke(this, EventArgs.Empty);
}

public class ControllerHolder
{
    private DS4Device device;
    private int index;
    public DS4Device Device { get => device; }
    public int Index { get => index; }

    public ControllerHolder(DS4Device device, int index)
    {
        this.device = device;
        this.index = index;
    }
}