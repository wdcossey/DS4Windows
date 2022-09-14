using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace DS4WinWPF.DS4Forms.ViewModels;

public class ControllerListViewModel
{
    //private object _colLockobj = new object();
    private readonly ReaderWriterLockSlim colListLocker = new();
    public ObservableCollection<CompositeDeviceModel> ControllerCol { get; } = new();
    
    private readonly IControlService controlService;
    private readonly IProfileListService profileListService;
    private int currentIndex;
    public int CurrentIndex { get => currentIndex; set => currentIndex = value; }
    public CompositeDeviceModel CurrentItem {
        get
        {
            if (currentIndex == -1) return null;
            ControllerDict.TryGetValue(currentIndex, out CompositeDeviceModel item);
            return item;
        }
    }

    public Dictionary<int, CompositeDeviceModel> ControllerDict { get; set; } = new();

    //public ControllerListViewModel(Tester tester, ProfileList profileListHolder)
    public ControllerListViewModel(IControlService controlService, IProfileListService profileListService)
    {
        this.controlService = controlService;
        this.profileListService = profileListService;
        controlService.ServiceStarted += ControllersChanged;
        controlService.PreServiceStop += ClearControllerList;
        controlService.HotplugController += Service_HotplugController;
        //tester.StartControllers += ControllersChanged;
        //tester.ControllersRemoved += ClearControllerList;
        
        foreach (var (index, device) in controlService.SlotManager.ControllerColl.Select((device, index) => (index, device)))
        {
            var temp = new CompositeDeviceModel(controlService, device, index, Global.ProfilePath[index], profileListService.ProfileList);
            ControllerCol.Add(temp);
            ControllerDict.Add(index, temp);
            device.Removal += Controller_Removal;
        }

        //BindingOperations.EnableCollectionSynchronization(controllerCol, _colLockobj);
        BindingOperations.EnableCollectionSynchronization(ControllerCol, colListLocker, ColLockCallback);
    }

    private void ColLockCallback(IEnumerable collection, object context, Action accessMethod, bool writeAccess)
    {
        if (writeAccess)
        {
            using var locker = new WriteLocker(colListLocker);
            accessMethod?.Invoke();
        }
        else
        {
            using var locker = new ReadLocker(colListLocker);
            accessMethod?.Invoke();
        }
    }

    private void Service_HotplugController(ControlService sender, DS4Device device, int index)
    {
        // Engage write lock pre-maturely
        using (var _ = new WriteLocker(colListLocker))
        {
            // Look if device exists. Also, check if disconnect might be occurring
            if (ControllerDict.ContainsKey(index) || device.IsRemoving)
                return;

            var temp = new CompositeDeviceModel(controlService, device, index, Global.ProfilePath[index],
                profileListService.ProfileList);
            ControllerCol.Add(temp);
            ControllerDict.Add(index, temp);

            device.Removal += Controller_Removal;
        }
    }

    private void ClearControllerList(object sender, EventArgs e)
    {
        colListLocker.EnterReadLock();
        foreach (var temp in ControllerCol)
            temp.Device.Removal -= Controller_Removal;
        
        colListLocker.ExitReadLock();
        colListLocker.EnterWriteLock();
        ControllerCol.Clear();
        ControllerDict.Clear();
        colListLocker.ExitWriteLock();
    }

    private void ControllersChanged(object sender, EventArgs e)
    {
        //IEnumerable<DS4Device> devices = DS4Windows.DS4Devices.getDS4Controllers();
        using (var _ = new ReadLocker(controlService.SlotManager.CollectionLocker))
        {
            foreach (var currentDev in controlService.SlotManager.ControllerColl)
            {
                colListLocker.EnterReadLock();
                var found = ControllerCol.Any(temp => temp.Device == currentDev);
                colListLocker.ExitReadLock();

                // Check for new device. Also, check if disconnect might be occurring
                if (found || currentDev.IsRemoving)
                    continue;

                //int idx = controllerCol.Count;
                colListLocker.EnterWriteLock();
                var idx = controlService.SlotManager.ReverseControllerDict[currentDev];
                var deviceModel = new CompositeDeviceModel(controlService, currentDev, idx, Global.ProfilePath[idx], profileListService.ProfileList);
                ControllerCol.Add(deviceModel);
                ControllerDict.Add(idx, deviceModel);
                colListLocker.ExitWriteLock();

                currentDev.Removal += Controller_Removal;
            }
        }
    }

    private void Controller_Removal(object sender, EventArgs e)
    {
        var currentDev = sender as DS4Device;
        colListLocker.EnterReadLock();
        var found = ControllerCol.FirstOrDefault(temp => temp.Device == currentDev);
        colListLocker.ExitReadLock();

        if (found == null) 
            return;
        
        colListLocker.EnterWriteLock();
        ControllerCol.Remove(found);
        ControllerDict.Remove(found.DevIndex);
        Application.Current.Dispatcher.Invoke(() =>
        {
            Global.Save();
        });
        Global.linkedProfileCheck[found.DevIndex] = false;
        colListLocker.ExitWriteLock();
    }
}

public class CompositeDeviceModel
{
    private readonly IControlService controlService;
    private DS4Device device;
    private string selectedProfile;
    private ProfileList profileListHolder;
    private ProfileEntity selectedEntity;
    private int selectedIndex = 1;
    private int devIndex;
        
    public DS4Device Device { get => device; set => device = value; }
    public string SelectedProfile { get => selectedProfile; set => selectedProfile = value; }
    public ProfileList ProfileEntities { get => profileListHolder; set => profileListHolder = value; }
    public ObservableCollection<ProfileEntity> ProfileListCol => profileListHolder.ProfileListCol;

    public string LightColor
    {
        get
        {
            DS4Color color;
            if (Global.LightbarSettingsInfo[devIndex].ds4winSettings.useCustomLed)
            {
                color = Global.LightbarSettingsInfo[devIndex].ds4winSettings.m_CustomLed; //Global.CustomColor[devIndex];
            }
            else
            {
                color = Global.LightbarSettingsInfo[devIndex].ds4winSettings.m_Led;
            }
            return $"#FF{color.Red.ToString("X2")}{color.Green.ToString("X2")}{color.Blue.ToString("X2")}";
        }
    }

    public event EventHandler LightColorChanged;

    public Color CustomLightColor
    {
        get
        {
            DS4Color color;
            color = Global.LightbarSettingsInfo[devIndex].ds4winSettings.m_CustomLed;
            return new Color() { R = color.Red, G = color.Green, B = color.Blue, A = 255 };
        }
    }

    public string BatteryState
    {
        get
        {
            string temp = $"{device.Battery}%{(device.Charging ? "+" : "")}";
            return temp;
        }
    }
    public event EventHandler BatteryStateChanged;

    public int SelectedIndex
    {
        get => selectedIndex;
        set
        {
            if (selectedIndex == value) return;
            selectedIndex = value;
            SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    public event EventHandler SelectedIndexChanged;

    public string StatusSource
    {
        get
        {
            string imgName = (string)Application.Current.FindResource(device.ConnectionType == ConnectionType.USB ? "UsbImg" : "BtImg");
            string source = $"/DS4Windows;component/Resources/{imgName}";
            return source;
        }
    }

    public string ExclusiveSource
    {
        get
        {
            string imgName = (string)Application.Current.FindResource("CancelImg");
            string source = $"/DS4Windows;component/Resources/{imgName}";
            switch(device.CurrentExclusiveStatus)
            {
                case DS4Device.ExclusiveStatus.Exclusive:
                    imgName = (string)Application.Current.FindResource("CheckedImg");
                    source = $"/DS4Windows;component/Resources/{imgName}";
                    break;
                case DS4Device.ExclusiveStatus.HidHideAffected:
                case DS4Device.ExclusiveStatus.HidGuardAffected:
                    imgName = (string)Application.Current.FindResource("KeyImageImg");
                    source = $"/DS4Windows;component/Resources/{imgName}";
                    break;
                default:
                    break;
            }

            return source;
        }
    }

    public bool LinkedProfile
    {
        get
        {
            return Global.linkedProfileCheck[devIndex];
        }
        set
        {
            bool temp = Global.linkedProfileCheck[devIndex];
            if (temp == value) return;
            Global.linkedProfileCheck[devIndex] = value;
            SaveLinked(value);
        }
    }

    public int DevIndex => devIndex;
    public int DisplayDevIndex => devIndex + 1;

    public string TooltipIDText
    {
        get
        {
            string temp = string.Format(Localization.InputDelay, device.Latency);
            return temp;
        }
    }

    public event EventHandler TooltipIDTextChanged;

    private bool useCustomColor;
    public bool UseCustomColor { get => useCustomColor; set => useCustomColor = value; }

    private ContextMenu lightContext;
    public ContextMenu LightContext { get => lightContext; set => lightContext = value; }

    public string IdText => $"{device.DisplayName} ({device.MacAddress})";
    public event EventHandler IdTextChanged;

    public string IsExclusiveText
    {
        get
        {
            return device.CurrentExclusiveStatus switch
            {
                DS4Device.ExclusiveStatus.Exclusive => Strings.ExclusiveAccess,
                DS4Device.ExclusiveStatus.HidHideAffected => Strings.HidHideAccess,
                DS4Device.ExclusiveStatus.HidGuardAffected => Strings.HidGuardianAccess,
                _ => Strings.SharedAccess
            };
        }
    }

    public bool PrimaryDevice => device.PrimaryDevice;

    public delegate void CustomColorHandler(CompositeDeviceModel sender);
    public event CustomColorHandler RequestColorPicker;
        
    public CompositeDeviceModel(IControlService controlService, DS4Device device, int devIndex, string profile,
        ProfileList collection)
    {
        this.controlService = controlService;
        this.device = device;
        device.BatteryChanged += (_, e) => BatteryStateChanged?.Invoke(this, e);
        device.ChargingChanged += (_, e) => BatteryStateChanged?.Invoke(this, e);
        device.MacAddressChanged += (_, e) => IdTextChanged?.Invoke(this, e);
        this.devIndex = devIndex;
        selectedProfile = profile;
        profileListHolder = collection;
        if (!string.IsNullOrEmpty(selectedProfile))
        {
            selectedEntity = profileListHolder.ProfileListCol.SingleOrDefault(x => x.Name == selectedProfile);
        }

        if (selectedEntity != null)
        {
            selectedIndex = profileListHolder.ProfileListCol.IndexOf(selectedEntity);
            HookEvents(true);
        }

        useCustomColor = Global.LightbarSettingsInfo[devIndex].ds4winSettings.useCustomLed;
    }

    public void ChangeSelectedProfile()
    {
        if (selectedEntity != null)
        {
            HookEvents(false);
        }

        var profileName = Global.ProfilePath[devIndex] = ProfileListCol[selectedIndex].Name;
        if (LinkedProfile)
        {
            Global.changeLinkedProfile(device.GetMacAddress(), Global.ProfilePath[devIndex]);
            Global.SaveLinkedProfiles();
        }
        else
        {
            Global.OlderProfilePath[devIndex] = Global.ProfilePath[devIndex];
        }

        //Global.Save();
        Global.LoadProfile(devIndex, true, controlService);
        var prolog = string.Format(Localization.UsingProfile, (devIndex + 1).ToString(), profileName, $"{device.Battery}");
        AppLogger.LogToGui(prolog, false);

        selectedProfile = profileName;
        selectedEntity = profileListHolder.ProfileListCol.SingleOrDefault(x => x.Name == profileName);
        if (selectedEntity != null)
        {
            selectedIndex = profileListHolder.ProfileListCol.IndexOf(selectedEntity);
            HookEvents(true);
        }

        LightColorChanged?.Invoke(this, EventArgs.Empty);
    }

    private void HookEvents(bool state)
    {
        if (state)
        {
            selectedEntity.ProfileSaved += SelectedEntity_ProfileSaved;
            selectedEntity.ProfileDeleted += SelectedEntity_ProfileDeleted;
        }
        else
        {
            selectedEntity.ProfileSaved -= SelectedEntity_ProfileSaved;
            selectedEntity.ProfileDeleted -= SelectedEntity_ProfileDeleted;
        }
    }

    private void SelectedEntity_ProfileDeleted(object sender, EventArgs e)
    {
        HookEvents(false);
        var entity = profileListHolder.ProfileListCol.FirstOrDefault();
        if (entity != null)
        {
            SelectedIndex = profileListHolder.ProfileListCol.IndexOf(entity);
        }
    }

    private void SelectedEntity_ProfileSaved(object sender, EventArgs e)
    {
        Global.LoadProfile(devIndex, false, controlService);
        LightColorChanged?.Invoke(this, EventArgs.Empty);
    }

    public void RequestUpdatedTooltipID() => 
        TooltipIDTextChanged?.Invoke(this, EventArgs.Empty);

    private void SaveLinked(bool status)
    {
        if (device == null || !device.isSynced()) 
            return;
        
        if (status)
        {
            if (device.isValidSerial())
            {
                Global.changeLinkedProfile(device.GetMacAddress(), Global.ProfilePath[devIndex]);
            }
        }
        else
        {
            Global.removeLinkedProfile(device.GetMacAddress());
            Global.ProfilePath[devIndex] = Global.OlderProfilePath[devIndex];
        }

        Global.SaveLinkedProfiles();
    }

    public void AddLightContextItems()
    {
        var thing = new MenuItem { Header = "Use Profile Color", IsChecked = !useCustomColor };
        thing.Click += ProfileColorMenuClick;
        lightContext.Items.Add(thing);
        thing = new MenuItem { Header = "Use Custom Color", IsChecked = useCustomColor };
        thing.Click += CustomColorItemClick;
        lightContext.Items.Add(thing);
    }

    private void ProfileColorMenuClick(object _, RoutedEventArgs e)
    {
        useCustomColor = false;
        RefreshLightContext();
        Global.LightbarSettingsInfo[devIndex].ds4winSettings.useCustomLed = false;
        LightColorChanged?.Invoke(this, EventArgs.Empty);
    }

    private void CustomColorItemClick(object _, RoutedEventArgs e)
    {
        useCustomColor = true;
        RefreshLightContext();
        Global.LightbarSettingsInfo[devIndex].ds4winSettings.useCustomLed = true;
        LightColorChanged?.Invoke(this, EventArgs.Empty);
        RequestColorPicker?.Invoke(this);
    }

    private void RefreshLightContext()
    {
        (lightContext.Items[0] as MenuItem)!.IsChecked = !useCustomColor;
        (lightContext.Items[1] as MenuItem)!.IsChecked = useCustomColor;
    }

    public void UpdateCustomLightColor(Color color)
    {
        Global.LightbarSettingsInfo[devIndex].ds4winSettings.m_CustomLed = new DS4Color() { Red = color.R, Green = color.G, Blue = color.B };
        LightColorChanged?.Invoke(this, EventArgs.Empty);
    }

    public void ChangeSelectedProfile(string loadprofile)
    {
        var temp = profileListHolder.ProfileListCol.SingleOrDefault(x => x.Name == loadprofile);
        if (temp != null)
        {
            SelectedIndex = profileListHolder.ProfileListCol.IndexOf(temp);
        }
    }

    public void RequestDisconnect()
    {
        if (!device.Synced || device.Charging) 
            return;

        switch (device.ConnectionType)
        {
            case ConnectionType.BT:
                //device.StopUpdate();
                device.queueEvent(() => device.DisconnectBT());
                break;
            case ConnectionType.SONYWA:
                device.DisconnectDongle();
                break;
            case ConnectionType.USB:
            default:
                break;
        }
    }
}