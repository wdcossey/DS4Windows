using System.Collections.Generic;
using System.IO;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using NonFormTimer = System.Timers.Timer;
using System.ComponentModel;

namespace DS4WinWPF.DS4Forms;

/// <summary>
/// Interaction logic for ProfileEditor.xaml
/// </summary>
public partial class ProfileEditor : UserControl
{
    private class HoverImageInfo
    {
        public Point Point;
        public Size Size;
    }

    private readonly IServiceProvider _serviceProvider;
    private readonly IBindingWindowFactory _bindingWindowFactory;
    private readonly ISpecialActionEditorFactory _specialActionEditorFactory;
    private readonly IControlService _controlService;
    private readonly IProfileListService _profileListService;
    private readonly ProfileSettingsViewModel _profileSettingsViewModel;
        
    private int _deviceNum;
        
    private readonly MappingListViewModel _mappingListViewModel;
    private ProfileEntity _currentProfile;
    private readonly SpecialActionsListViewModel _specialActionsViewModel;

    public event EventHandler Closed;
    public delegate void CreatedProfileHandler(ProfileEditor sender, string profile);
    public event CreatedProfileHandler CreatedProfile;

    private readonly Dictionary<Button, ImageBrush> _hoverImages = new();
    private readonly Dictionary<Button, HoverImageInfo> _hoverLocations = new();
    private readonly Dictionary<Button, int> _hoverIndexes = new();
    private readonly Dictionary<int, Button> _reverseHoverIndexes = new();

    private bool _keepSize;
    private bool _controllerReadingsTabActive = false;
    public bool KeepSize => _keepSize;
    public int DeviceNum => _deviceNum;

    private readonly NonFormTimer _inputTimer;
        
    public ProfileEditor(
        IServiceProvider serviceProvider,
        IControlService controlService,
        ProfileSettingsViewModel profileSettingsViewModel,
        MappingListViewModel mappingListViewModel,
        SpecialActionsListViewModel specialActionsListViewModel,
        IBindingWindowFactory bindingWindowFactory, 
        ISpecialActionEditorFactory specialActionEditorFactory,
        IProfileListService profileListService,
        int device)
    {
        _serviceProvider = serviceProvider;
        _bindingWindowFactory = bindingWindowFactory;
        _specialActionEditorFactory = specialActionEditorFactory;
        _controlService = controlService;
        _profileSettingsViewModel = profileSettingsViewModel;
        _mappingListViewModel = mappingListViewModel;
        _specialActionsViewModel = specialActionsListViewModel;
        _profileListService = profileListService;
            
        InitializeComponent();
            
        _deviceNum = device;
        emptyColorGB.Visibility = Visibility.Collapsed;
            
        picBoxHover.Visibility = Visibility.Hidden;
        picBoxHover2.Visibility = Visibility.Hidden;
            
        RemoveHoverBtnText();
        PopulateHoverImages();
        PopulateHoverLocations();
        PopulateHoverIndexes();
        PopulateReverseHoverIndexes();

        AssignTiltAssociation();
        AssignSwipeAssociation();
        AssignTriggerFullPullAssociation();
        AssignStickOuterBindAssociation();
        AssignGyroSwipeAssociation();

        _inputTimer = new NonFormTimer(100);
        _inputTimer.Elapsed += InputDs4;
        SetupEvents();
    }

    private void SetupEvents()
    {
        gyroOutModeCombo.SelectionChanged += GyroOutModeCombo_SelectionChanged;
        outConTypeCombo.SelectionChanged += OutConTypeCombo_SelectionChanged;
        mappingListBox.SelectionChanged += MappingListBox_SelectionChanged;
        Closed += ProfileEditor_Closed;

        _profileSettingsViewModel.LSDeadZoneChanged += UpdateReadingsLsDeadZone;
        _profileSettingsViewModel.RSDeadZoneChanged += UpdateReadingsRsDeadZone;
        _profileSettingsViewModel.L2DeadZoneChanged += UpdateReadingsL2DeadZone;
        _profileSettingsViewModel.R2DeadZoneChanged += UpdateReadingsR2DeadZone;
        _profileSettingsViewModel.SXDeadZoneChanged += UpdateReadingsSxDeadZone;
        _profileSettingsViewModel.SZDeadZoneChanged += UpdateReadingsSzDeadZone;
    }

    private void UpdateReadingsSzDeadZone(object sender, EventArgs e) => 
        conReadingsUserCon.SixAxisZDead = _profileSettingsViewModel.SZDeadZone;

    private void UpdateReadingsSxDeadZone(object sender, EventArgs e) => 
        conReadingsUserCon.SixAxisXDead = _profileSettingsViewModel.SXDeadZone;

    private void UpdateReadingsR2DeadZone(object sender, EventArgs e) => 
        conReadingsUserCon.R2Dead = _profileSettingsViewModel.R2DeadZone;

    private void UpdateReadingsL2DeadZone(object sender, EventArgs e) => 
        conReadingsUserCon.L2Dead = _profileSettingsViewModel.L2DeadZone;

    private void UpdateReadingsLsDeadZone(object sender, EventArgs e)
    {
        conReadingsUserCon.LsDeadX = _profileSettingsViewModel.LSDeadZone;
        conReadingsUserCon.LsDeadY = _profileSettingsViewModel.LSDeadZone;
    }

    private void UpdateReadingsLsDeadZoneX(object sender, EventArgs e) => 
        conReadingsUserCon.LsDeadX = axialLSStickControl.AxialVM.DeadZoneX;

    private void UpdateReadingsLsDeadZoneY(object sender, EventArgs e) => 
        conReadingsUserCon.LsDeadY = axialLSStickControl.AxialVM.DeadZoneY;

    private void UpdateReadingsRsDeadZone(object sender, EventArgs e)
    {
        conReadingsUserCon.RsDeadX = _profileSettingsViewModel.RSDeadZone;
        conReadingsUserCon.RsDeadY = _profileSettingsViewModel.RSDeadZone;
    }

    private void UpdateReadingsRsDeadZoneX(object sender, EventArgs e) => 
        conReadingsUserCon.RsDeadX = axialRSStickControl.AxialVM.DeadZoneX;

    private void UpdateReadingsRsDeadZoneY(object sender, EventArgs e) => 
        conReadingsUserCon.RsDeadY = axialRSStickControl.AxialVM.DeadZoneY;

    private void AssignTiltAssociation()
    {
        gyroZNLb.DataContext = _mappingListViewModel.ControlMap[DS4Controls.GyroZNeg];
        gyroZPLb.DataContext = _mappingListViewModel.ControlMap[DS4Controls.GyroZPos];
        gyroXNLb.DataContext = _mappingListViewModel.ControlMap[DS4Controls.GyroXNeg];
        gyroXLb.DataContext = _mappingListViewModel.ControlMap[DS4Controls.GyroXPos];
    }

    private void AssignSwipeAssociation()
    {
        swipeUpLb.DataContext = _mappingListViewModel.ControlMap[DS4Controls.SwipeUp];
        swipeDownLb.DataContext = _mappingListViewModel.ControlMap[DS4Controls.SwipeDown];
        swipeLeftLb.DataContext = _mappingListViewModel.ControlMap[DS4Controls.SwipeLeft];
        swipeRightLb.DataContext = _mappingListViewModel.ControlMap[DS4Controls.SwipeRight];
    }

    private void AssignTriggerFullPullAssociation()
    {
        l2FullPullLb.DataContext = _mappingListViewModel.ControlMap[DS4Controls.L2FullPull];
        r2FullPullLb.DataContext = _mappingListViewModel.ControlMap[DS4Controls.R2FullPull];
    }

    private void AssignStickOuterBindAssociation()
    {
        lsOuterBindLb.DataContext = _mappingListViewModel.ControlMap[DS4Controls.LSOuter];
        rsOuterBindLb.DataContext = _mappingListViewModel.ControlMap[DS4Controls.RSOuter];
    }

    private void AssignGyroSwipeAssociation()
    {
        gyroSwipeLeftLb.DataContext = _mappingListViewModel.ControlMap[DS4Controls.GyroSwipeLeft];
        gyroSwipeRightLb.DataContext = _mappingListViewModel.ControlMap[DS4Controls.GyroSwipeRight];
        gyroSwipeUpLb.DataContext = _mappingListViewModel.ControlMap[DS4Controls.GyroSwipeUp];
        gyroSwipeDownLb.DataContext = _mappingListViewModel.ControlMap[DS4Controls.GyroSwipeDown];
    }

    private void MappingListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_mappingListViewModel.SelectedIndex >= 0 && 
            _reverseHoverIndexes.TryGetValue(_mappingListViewModel.SelectedIndex, out var tempBtn))
            InputControlHighlight(tempBtn);
    }

    private void PopulateReverseHoverIndexes()
    {
        foreach(var pair in _hoverIndexes) 
            _reverseHoverIndexes.Add(pair.Value, pair.Key);
    }

    private void PopulateHoverIndexes()
    {
        _hoverIndexes[crossConBtn] = 0;
        _hoverIndexes[circleConBtn] = 1;
        _hoverIndexes[squareConBtn] = 2;
        _hoverIndexes[triangleConBtn] = 3;
        _hoverIndexes[optionsConBtn] = 4;
        _hoverIndexes[shareConBtn] = 5;
        _hoverIndexes[upConBtn] = 6;
        _hoverIndexes[downConBtn] = 7;
        _hoverIndexes[leftConBtn] = 8;
        _hoverIndexes[rightConBtn] = 9;
        _hoverIndexes[guideConBtn] = 10;
        _hoverIndexes[muteConBtn] = 11;
        _hoverIndexes[l1ConBtn] = 12;
        _hoverIndexes[r1ConBtn] = 13;
        _hoverIndexes[l2ConBtn] = 14;
        _hoverIndexes[r2ConBtn] = 15;
        _hoverIndexes[l3ConBtn] = 16;
        _hoverIndexes[r3ConBtn] = 17;

        _hoverIndexes[leftTouchConBtn] = _mappingListViewModel.ControlIndexMap[DS4Controls.TouchLeft]; // 21
        _hoverIndexes[rightTouchConBtn] = _mappingListViewModel.ControlIndexMap[DS4Controls.TouchRight]; // 22
        _hoverIndexes[multiTouchConBtn] = _mappingListViewModel.ControlIndexMap[DS4Controls.TouchMulti]; // 23
        _hoverIndexes[topTouchConBtn] = _mappingListViewModel.ControlIndexMap[DS4Controls.TouchUpper]; // 24

        _hoverIndexes[lsuConBtn] = 25;
        _hoverIndexes[lsdConBtn] = 26;
        _hoverIndexes[lslConBtn] = 27;
        _hoverIndexes[lsrConBtn] = 28;

        _hoverIndexes[rsuConBtn] = 29;
        _hoverIndexes[rsdConBtn] = 30;
        _hoverIndexes[rslConBtn] = 31;
        _hoverIndexes[rsrConBtn] = 32;

        _hoverIndexes[gyroZNBtn] = 33;
        _hoverIndexes[gyroZPBtn] = 34;
        _hoverIndexes[gyroXNBtn] = 35;
        _hoverIndexes[gyroXPBtn] = 36;

        _hoverIndexes[swipeUpBtn] = 37;
        _hoverIndexes[swipeDownBtn] = 38;
        _hoverIndexes[swipeLeftBtn] = 39;
        _hoverIndexes[swipeRightBtn] = 40;
    }

    private void PopulateHoverLocations()
    {
        _hoverLocations[crossConBtn] = new HoverImageInfo() { Point = new Point(Canvas.GetLeft(crossConBtn), Canvas.GetTop(crossConBtn)),
            Size = new Size(crossConBtn.Width, crossConBtn.Height) };
        _hoverLocations[circleConBtn] = new HoverImageInfo() { Point = new Point(Canvas.GetLeft(circleConBtn), Canvas.GetTop(circleConBtn)),
            Size = new Size(circleConBtn.Width, circleConBtn.Height) };
        _hoverLocations[squareConBtn] = new HoverImageInfo() { Point = new Point(Canvas.GetLeft(squareConBtn), Canvas.GetTop(squareConBtn)),
            Size = new Size(squareConBtn.Width, squareConBtn.Height) };
        _hoverLocations[triangleConBtn] = new HoverImageInfo() { Point = new Point(Canvas.GetLeft(triangleConBtn), Canvas.GetTop(triangleConBtn)),
            Size = new Size(triangleConBtn.Width, triangleConBtn.Height) };
        _hoverLocations[l1ConBtn] = new HoverImageInfo() { Point = new Point(Canvas.GetLeft(l1ConBtn), Canvas.GetTop(l1ConBtn)),
            Size = new Size(l1ConBtn.Width, l1ConBtn.Height) };
        _hoverLocations[r1ConBtn] = new HoverImageInfo() { Point = new Point(Canvas.GetLeft(r1ConBtn), Canvas.GetTop(r1ConBtn)),
            Size = new Size(r1ConBtn.Width, r1ConBtn.Height) };
        _hoverLocations[l2ConBtn] = new HoverImageInfo() { Point = new Point(Canvas.GetLeft(l2ConBtn), Canvas.GetTop(l2ConBtn)),
            Size = new Size(l2ConBtn.Width, l2ConBtn.Height) };
        _hoverLocations[r2ConBtn] = new HoverImageInfo() { Point = new Point(Canvas.GetLeft(r2ConBtn), Canvas.GetTop(r2ConBtn)),
            Size = new Size(r2ConBtn.Width, r2ConBtn.Height) };
        _hoverLocations[shareConBtn] = new HoverImageInfo() { Point = new Point(Canvas.GetLeft(shareConBtn), Canvas.GetTop(shareConBtn)),
            Size = new Size(shareConBtn.Width, shareConBtn.Height) };
        _hoverLocations[optionsConBtn] = new HoverImageInfo() { Point = new Point(Canvas.GetLeft(optionsConBtn), Canvas.GetTop(optionsConBtn)),
            Size = new Size(optionsConBtn.Width, optionsConBtn.Height) };
        _hoverLocations[guideConBtn] = new HoverImageInfo() { Point = new Point(Canvas.GetLeft(guideConBtn), Canvas.GetTop(guideConBtn)),
            Size = new Size(guideConBtn.Width, guideConBtn.Height) };
        _hoverLocations[muteConBtn] = new HoverImageInfo()
        {
            Point = new Point(Canvas.GetLeft(muteConBtn), Canvas.GetTop(muteConBtn)),
            Size = new Size(muteConBtn.Width, muteConBtn.Height)
        };

        _hoverLocations[leftTouchConBtn] = new HoverImageInfo() { Point = new Point(144, 44), Size = new Size(140, 98) };
        _hoverLocations[multiTouchConBtn] = new HoverImageInfo() { Point = new Point(143, 42), Size = new Size(158, 100) };
        _hoverLocations[rightTouchConBtn] = new HoverImageInfo() { Point = new Point(156, 47), Size = new Size(146, 94) };
        _hoverLocations[topTouchConBtn] = new HoverImageInfo() { Point = new Point(155, 6), Size = new Size(153, 114) };

        _hoverLocations[l3ConBtn] = new HoverImageInfo() { Point = new Point(Canvas.GetLeft(l3ConBtn), Canvas.GetTop(l3ConBtn)),
            Size = new Size(l3ConBtn.Width, l3ConBtn.Height) };
        _hoverLocations[lsuConBtn] = new HoverImageInfo() { Point = new Point(Canvas.GetLeft(l3ConBtn), Canvas.GetTop(l3ConBtn)),
            Size = new Size(l3ConBtn.Width, l3ConBtn.Height) };
        _hoverLocations[lsrConBtn] = new HoverImageInfo() { Point = new Point(Canvas.GetLeft(l3ConBtn), Canvas.GetTop(l3ConBtn)),
            Size = new Size(l3ConBtn.Width, l3ConBtn.Height) };
        _hoverLocations[lsdConBtn] = new HoverImageInfo() { Point = new Point(Canvas.GetLeft(l3ConBtn), Canvas.GetTop(l3ConBtn)),
            Size = new Size(l3ConBtn.Width, l3ConBtn.Height) };
        _hoverLocations[lslConBtn] = new HoverImageInfo() { Point = new Point(Canvas.GetLeft(l3ConBtn), Canvas.GetTop(l3ConBtn)),
            Size = new Size(l3ConBtn.Width, l3ConBtn.Height) };

        _hoverLocations[r3ConBtn] = new HoverImageInfo() { Point = new Point(Canvas.GetLeft(r3ConBtn), Canvas.GetTop(r3ConBtn)),
            Size = new Size(r3ConBtn.Width, r3ConBtn.Height) };
        _hoverLocations[rsuConBtn] = new HoverImageInfo() { Point = new Point(Canvas.GetLeft(r3ConBtn), Canvas.GetTop(r3ConBtn)),
            Size = new Size(r3ConBtn.Width, r3ConBtn.Height) };
        _hoverLocations[rsrConBtn] = new HoverImageInfo() { Point = new Point(Canvas.GetLeft(r3ConBtn), Canvas.GetTop(r3ConBtn)),
            Size = new Size(r3ConBtn.Width, r3ConBtn.Height) };
        _hoverLocations[rsdConBtn] = new HoverImageInfo() { Point = new Point(Canvas.GetLeft(r3ConBtn), Canvas.GetTop(r3ConBtn)),
            Size = new Size(r3ConBtn.Width, r3ConBtn.Height) };
        _hoverLocations[rslConBtn] = new HoverImageInfo() { Point = new Point(Canvas.GetLeft(r3ConBtn), Canvas.GetTop(r3ConBtn)),
            Size = new Size(r3ConBtn.Width, r3ConBtn.Height) };

        _hoverLocations[upConBtn] = new HoverImageInfo() { Point = new Point(Canvas.GetLeft(upConBtn), Canvas.GetTop(upConBtn)),
            Size = new Size(upConBtn.Width, upConBtn.Height) };
        _hoverLocations[rightConBtn] = new HoverImageInfo() { Point = new Point(Canvas.GetLeft(rightConBtn), Canvas.GetTop(rightConBtn)),
            Size = new Size(rightConBtn.Width, rightConBtn.Height) };
        _hoverLocations[downConBtn] = new HoverImageInfo() { Point = new Point(Canvas.GetLeft(downConBtn), Canvas.GetTop(downConBtn)),
            Size = new Size(downConBtn.Width, downConBtn.Height) };
        _hoverLocations[leftConBtn] = new HoverImageInfo() { Point = new Point(Canvas.GetLeft(leftConBtn), Canvas.GetTop(leftConBtn)),
            Size = new Size(leftConBtn.Width, leftConBtn.Height) };
    }

    private void RemoveHoverBtnText()
    {
        crossConBtn.Content = "";
        circleConBtn.Content = "";
        squareConBtn.Content = "";
        triangleConBtn.Content = "";
        l1ConBtn.Content = "";
        r1ConBtn.Content = "";
        l2ConBtn.Content = "";
        r2ConBtn.Content = "";
        shareConBtn.Content = "";
        optionsConBtn.Content = "";
        guideConBtn.Content = "";
        muteConBtn.Content = "";
        leftTouchConBtn.Content = "";
        multiTouchConBtn.Content = "";
        rightTouchConBtn.Content = "";
        topTouchConBtn.Content = "";

        l3ConBtn.Content = "";
        lsuConBtn.Content = "";
        lsrConBtn.Content = "";
        lsdConBtn.Content = "";
        lslConBtn.Content = "";

        r3ConBtn.Content = "";
        rsuConBtn.Content = "";
        rsrConBtn.Content = "";
        rsdConBtn.Content = "";
        rslConBtn.Content = "";

        upConBtn.Content = "";
        rightConBtn.Content = "";
        downConBtn.Content = "";
        leftConBtn.Content = "";
    }

    private void PopulateHoverImages()
    {
        var sourceConverter = new ImageSourceConverter();

        var temp = sourceConverter.
            ConvertFromString($"{Global.ASSEMBLY_RESOURCE_PREFIX}component/Resources/DS4-Config_Cross.png") as ImageSource;
        var crossHover = new ImageBrush(temp);

        temp = sourceConverter.
            ConvertFromString($"{Global.ASSEMBLY_RESOURCE_PREFIX}component/Resources/DS4-Config_Circle.png") as ImageSource;
        var circleHover = new ImageBrush(temp);

        temp = sourceConverter.
            ConvertFromString($"{Global.ASSEMBLY_RESOURCE_PREFIX}component/Resources/DS4-Config_Square.png") as ImageSource;
        var squareHover = new ImageBrush(temp);

        temp = sourceConverter.
            ConvertFromString($"{Global.ASSEMBLY_RESOURCE_PREFIX}component/Resources/DS4-Config_Triangle.png") as ImageSource;
        var triangleHover = new ImageBrush(temp);

        temp = sourceConverter.
            ConvertFromString($"{Global.ASSEMBLY_RESOURCE_PREFIX}component/Resources/DS4-Config_L1.png") as ImageSource;
        var l1Hover = new ImageBrush(temp);

        temp = sourceConverter.
            ConvertFromString($"{Global.ASSEMBLY_RESOURCE_PREFIX}component/Resources/DS4-Config_R1.png") as ImageSource;
        var r1Hover = new ImageBrush(temp);

        temp = sourceConverter.
            ConvertFromString($"{Global.ASSEMBLY_RESOURCE_PREFIX}component/Resources/DS4-Config_L2.png") as ImageSource;
        var l2Hover = new ImageBrush(temp);

        temp = sourceConverter.
            ConvertFromString($"{Global.ASSEMBLY_RESOURCE_PREFIX}component/Resources/DS4-Config_R2.png") as ImageSource;
        var r2Hover = new ImageBrush(temp);

        temp = sourceConverter.
            ConvertFromString($"{Global.ASSEMBLY_RESOURCE_PREFIX}component/Resources/DS4-Config_Share.png") as ImageSource;
        var shareHover = new ImageBrush(temp);

        temp = sourceConverter.
            ConvertFromString($"{Global.ASSEMBLY_RESOURCE_PREFIX}component/Resources/DS4-Config_options.png") as ImageSource;
        var optionsHover = new ImageBrush(temp);

        temp = sourceConverter.
            ConvertFromString($"{Global.ASSEMBLY_RESOURCE_PREFIX}component/Resources/DS4-Config_PS.png") as ImageSource;
        var guideHover = new ImageBrush(temp);

        temp = sourceConverter.
            ConvertFromString($"{Global.ASSEMBLY_RESOURCE_PREFIX}component/Resources/DS4-Config_TouchLeft.png") as ImageSource;
        var leftTouchHover = new ImageBrush(temp);

        temp = sourceConverter.
            ConvertFromString($"{Global.ASSEMBLY_RESOURCE_PREFIX}component/Resources/DS4-Config_TouchMulti.png") as ImageSource;
        var multiTouchTouchHover = new ImageBrush(temp);

        temp = sourceConverter.
            ConvertFromString($"{Global.ASSEMBLY_RESOURCE_PREFIX}component/Resources/DS4-Config_TouchRight.png") as ImageSource;
        var rightTouchHover = new ImageBrush(temp);

        temp = sourceConverter.
            ConvertFromString($"{Global.ASSEMBLY_RESOURCE_PREFIX}component/Resources/DS4-Config_TouchUpper.png") as ImageSource;
        var topTouchHover = new ImageBrush(temp);


        temp = sourceConverter.
            ConvertFromString($"{Global.ASSEMBLY_RESOURCE_PREFIX}component/Resources/DS4-Config_LS.png") as ImageSource;
        var l3Hover = new ImageBrush(temp);

        temp = sourceConverter.
            ConvertFromString($"{Global.ASSEMBLY_RESOURCE_PREFIX}component/Resources/DS4-Config_LS.png") as ImageSource;
        var lsuHover = new ImageBrush(temp);

        temp = sourceConverter.
            ConvertFromString($"{Global.ASSEMBLY_RESOURCE_PREFIX}component/Resources/DS4-Config_LS.png") as ImageSource;
        var lsrHover = new ImageBrush(temp);

        temp = sourceConverter.
            ConvertFromString($"{Global.ASSEMBLY_RESOURCE_PREFIX}component/Resources/DS4-Config_LS.png") as ImageSource;
        var lsdHover = new ImageBrush(temp);

        temp = sourceConverter.
            ConvertFromString($"{Global.ASSEMBLY_RESOURCE_PREFIX}component/Resources/DS4-Config_LS.png") as ImageSource;
        var lslHover = new ImageBrush(temp);


        temp = sourceConverter.
            ConvertFromString($"{Global.ASSEMBLY_RESOURCE_PREFIX}component/Resources/DS4-Config_RS.png") as ImageSource;
        var r3Hover = new ImageBrush(temp);

        temp = sourceConverter.
            ConvertFromString($"{Global.ASSEMBLY_RESOURCE_PREFIX}component/Resources/DS4-Config_RS.png") as ImageSource;
        var rsuHover = new ImageBrush(temp);

        temp = sourceConverter.
            ConvertFromString($"{Global.ASSEMBLY_RESOURCE_PREFIX}component/Resources/DS4-Config_RS.png") as ImageSource;
        var rsrHover = new ImageBrush(temp);

        temp = sourceConverter.
            ConvertFromString($"{Global.ASSEMBLY_RESOURCE_PREFIX}component/Resources/DS4-Config_RS.png") as ImageSource;
        var rsdHover = new ImageBrush(temp);

        temp = sourceConverter.
            ConvertFromString($"{Global.ASSEMBLY_RESOURCE_PREFIX}component/Resources/DS4-Config_RS.png") as ImageSource;
        var rslHover = new ImageBrush(temp);


        temp = sourceConverter.
            ConvertFromString($"{Global.ASSEMBLY_RESOURCE_PREFIX}component/Resources/DS4-Config_Up.png") as ImageSource;
        var upHover = new ImageBrush(temp);

        temp = sourceConverter.
            ConvertFromString($"{Global.ASSEMBLY_RESOURCE_PREFIX}component/Resources/DS4-Config_Right.png") as ImageSource;
        var rightHover = new ImageBrush(temp);

        temp = sourceConverter.
            ConvertFromString($"{Global.ASSEMBLY_RESOURCE_PREFIX}component/Resources/DS4-Config_Down.png") as ImageSource;
        var downHover = new ImageBrush(temp);

        temp = sourceConverter.
            ConvertFromString($"{Global.ASSEMBLY_RESOURCE_PREFIX}component/Resources/DS4-Config_Left.png") as ImageSource;
        var leftHover = new ImageBrush(temp);

        _hoverImages[crossConBtn] = crossHover;
        _hoverImages[circleConBtn] = circleHover;
        _hoverImages[squareConBtn] = squareHover;
        _hoverImages[triangleConBtn] = triangleHover;
        _hoverImages[l1ConBtn] = l1Hover;
        _hoverImages[r1ConBtn] = r1Hover;
        _hoverImages[l2ConBtn] = l2Hover;
        _hoverImages[r2ConBtn] = r2Hover;
        _hoverImages[shareConBtn] = shareHover;
        _hoverImages[optionsConBtn] = optionsHover;
        _hoverImages[guideConBtn] = guideHover;
        _hoverImages[muteConBtn] = guideHover;

        _hoverImages[leftTouchConBtn] = leftTouchHover;
        _hoverImages[multiTouchConBtn] = multiTouchTouchHover;
        _hoverImages[rightTouchConBtn] = rightTouchHover;
        _hoverImages[topTouchConBtn] = topTouchHover;
        _hoverImages[l3ConBtn] = l3Hover;
        _hoverImages[lsuConBtn] = lsuHover;
        _hoverImages[lsrConBtn] = lsrHover;
        _hoverImages[lsdConBtn] = lsdHover;
        _hoverImages[lslConBtn] = lslHover;
        _hoverImages[r3ConBtn] = r3Hover;
        _hoverImages[rsuConBtn] = rsuHover;
        _hoverImages[rsrConBtn] = rsrHover;
        _hoverImages[rsdConBtn] = rsdHover;
        _hoverImages[rslConBtn] = rslHover;

        _hoverImages[upConBtn] = upHover;
        _hoverImages[rightConBtn] = rightHover;
        _hoverImages[downConBtn] = downHover;
        _hoverImages[leftConBtn] = leftHover;
    }

    public void Reload(int device, ProfileEntity profile = null)
    {
        profileSettingsTabCon.DataContext = null;
        mappingListBox.DataContext = null;
        specialActionsTab.DataContext = null;
        lightbarRect.DataContext = null;

        _deviceNum = device;
        if (profile != null)
        {
            _currentProfile = profile;
            if (device == Global.TEST_PROFILE_INDEX)
            {
                Global.ProfilePath[Global.TEST_PROFILE_INDEX] = profile.Name;
            }

            Global.LoadProfile(device, false, _controlService, false);
            profileNameTxt.Text = profile.Name;
            profileNameTxt.IsEnabled = false;
            applyBtn.IsEnabled = true;
        }
        else
        {
            _currentProfile = null;
            var presetWin = new PresetOptionWindow(_serviceProvider);
            presetWin.SetupData(_deviceNum);
            presetWin.ShowDialog();
            if (presetWin.Result == MessageBoxResult.Cancel)
            {
                Global.LoadBlankDevProfile(device, false, _controlService, false);
            }
        }

        ColorByBatteryPerCheck();

        if (device < Global.TEST_PROFILE_INDEX)
        {
            useControllerUD.Value = device + 1;
            conReadingsUserCon.UseDevice(device, device);
            contReadingsTab.IsEnabled = true;
        }
        else
        {
            useControllerUD.Value = 1;
            conReadingsUserCon.UseDevice(0, Global.TEST_PROFILE_INDEX);
            contReadingsTab.IsEnabled = true;
        }

        conReadingsUserCon.EnableControl(false);
        axialLSStickControl.UseDevice(Global.LSModInfo[device]);
        axialRSStickControl.UseDevice(Global.RSModInfo[device]);

        _specialActionsViewModel.LoadActions(_currentProfile == null);
        _mappingListViewModel.UpdateMappings();
        _profileSettingsViewModel.UpdateLateProperties();
        _profileSettingsViewModel.PopulateTouchDisInver(touchDisInvertBtn.ContextMenu);
        _profileSettingsViewModel.PopulateGyroMouseTrig(gyroMouseTrigBtn.ContextMenu);
        _profileSettingsViewModel.PopulateGyroMouseStickTrig(gyroMouseStickTrigBtn.ContextMenu);
        _profileSettingsViewModel.PopulateGyroSwipeTrig(gyroSwipeTrigBtn.ContextMenu);
        _profileSettingsViewModel.PopulateGyroControlsTrig(gyroControlsTrigBtn.ContextMenu);
        profileSettingsTabCon.DataContext = _profileSettingsViewModel;
        mappingListBox.DataContext = _mappingListViewModel;
        specialActionsTab.DataContext = _specialActionsViewModel;
        lightbarRect.DataContext = _profileSettingsViewModel;

        var lsMod = Global.LSModInfo[device];
        if (lsMod.deadzoneType == StickDeadZoneInfo.DeadZoneType.Radial)
        {
            conReadingsUserCon.LsDeadX = _profileSettingsViewModel.LSDeadZone;
            conReadingsUserCon.LsDeadY = _profileSettingsViewModel.LSDeadZone;
        }
        else if (lsMod.deadzoneType == StickDeadZoneInfo.DeadZoneType.Axial)
        {
            conReadingsUserCon.LsDeadX = axialLSStickControl.AxialVM.DeadZoneX;
            conReadingsUserCon.LsDeadY = axialLSStickControl.AxialVM.DeadZoneY;
        }

        var rsMod = Global.RSModInfo[device];
        if (rsMod.deadzoneType == StickDeadZoneInfo.DeadZoneType.Radial)
        {
            conReadingsUserCon.RsDeadX = _profileSettingsViewModel.RSDeadZone;
            conReadingsUserCon.RsDeadY = _profileSettingsViewModel.RSDeadZone;
        }
        else if (rsMod.deadzoneType == StickDeadZoneInfo.DeadZoneType.Axial)
        {
            conReadingsUserCon.RsDeadX = axialRSStickControl.AxialVM.DeadZoneX;
            conReadingsUserCon.RsDeadY = axialRSStickControl.AxialVM.DeadZoneY;
        }

        conReadingsUserCon.L2Dead = _profileSettingsViewModel.L2DeadZone;
        conReadingsUserCon.R2Dead = _profileSettingsViewModel.R2DeadZone;
        conReadingsUserCon.SixAxisXDead = _profileSettingsViewModel.SXDeadZone;
        conReadingsUserCon.SixAxisZDead = _profileSettingsViewModel.SZDeadZone;

        axialLSStickControl.AxialVM.DeadZoneXChanged += UpdateReadingsLsDeadZoneX;
        axialLSStickControl.AxialVM.DeadZoneYChanged += UpdateReadingsLsDeadZoneY;
        axialRSStickControl.AxialVM.DeadZoneXChanged += UpdateReadingsRsDeadZoneX;
        axialRSStickControl.AxialVM.DeadZoneYChanged += UpdateReadingsRsDeadZoneY;

        // Sort special action list by action name
        var view = (CollectionView)CollectionViewSource.GetDefaultView(_specialActionsViewModel.ActionCol);
        view.SortDescriptions.Clear();
        view.SortDescriptions.Add(new SortDescription("ActionName", ListSortDirection.Ascending));
        view.Refresh();

        if (_profileSettingsViewModel.UseControllerReadout)
        {
            _inputTimer.Start();
        }
    }

    private void StopEditorBindings()
    {
        profileSettingsTabCon.DataContext = null;
        mappingListBox.DataContext = null;
        specialActionsTab.DataContext = null;
        lightbarRect.DataContext = null;
    }

    private void RefreshEditorBindings()
    {
        _specialActionsViewModel.LoadActions(_currentProfile == null);
        _mappingListViewModel.UpdateMappings();
        _profileSettingsViewModel.UpdateLateProperties();
        _profileSettingsViewModel.PopulateTouchDisInver(touchDisInvertBtn.ContextMenu);
        _profileSettingsViewModel.PopulateGyroMouseTrig(gyroMouseTrigBtn.ContextMenu);
        _profileSettingsViewModel.PopulateGyroMouseStickTrig(gyroMouseStickTrigBtn.ContextMenu);
        _profileSettingsViewModel.PopulateGyroSwipeTrig(gyroSwipeTrigBtn.ContextMenu);
        _profileSettingsViewModel.PopulateGyroControlsTrig(gyroControlsTrigBtn.ContextMenu);
        profileSettingsTabCon.DataContext = _profileSettingsViewModel;
        mappingListBox.DataContext = _mappingListViewModel;
        specialActionsTab.DataContext = _specialActionsViewModel;
        lightbarRect.DataContext = _profileSettingsViewModel;

        conReadingsUserCon.LsDeadX = _profileSettingsViewModel.LSDeadZone;
        conReadingsUserCon.RsDeadX = _profileSettingsViewModel.RSDeadZone;
        conReadingsUserCon.L2Dead = _profileSettingsViewModel.L2DeadZone;
        conReadingsUserCon.R2Dead = _profileSettingsViewModel.R2DeadZone;
        conReadingsUserCon.SixAxisXDead = _profileSettingsViewModel.SXDeadZone;
        conReadingsUserCon.SixAxisZDead = _profileSettingsViewModel.SZDeadZone;
    }

    private void CancelBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_profileSettingsViewModel.FuncDevNum < IControlService.CURRENT_DS4_CONTROLLER_LIMIT)
        {
            _controlService.SetRumble(0, 0, _profileSettingsViewModel.FuncDevNum);
        }
        Global.outDevTypeTemp[_deviceNum] = OutContType.X360;
        Global.LoadProfile(_deviceNum, false, _controlService);
        Closed?.Invoke(this, EventArgs.Empty);
    }

    private void HoverConBtn_Click(object sender, RoutedEventArgs e)
    {
        var mpControl = _mappingListViewModel.Mappings[_mappingListViewModel.SelectedIndex];
        var window = _bindingWindowFactory.Create(_deviceNum, mpControl.Setting);
        window.Owner = Application.Current.MainWindow;
        window.ShowDialog();
        mpControl.UpdateMappingName();
        UpdateHighlightLabel(mpControl);
        Global.CacheProfileCustomsFlags(_profileSettingsViewModel.Device);
    }

    private void InputControlHighlight(Button control)
    {
        if (_hoverImages.TryGetValue(control, out var tempBrush))
        {
            picBoxHover.Source = tempBrush.ImageSource;
            //picBoxHover.Width = tempBrush.ImageSource.Width * .8;
            //picBoxHover.Height = tempBrush.ImageSource.Height * .8;
            //control.Background = tempBrush;
            //control.Background = new SolidColorBrush(Colors.Green);
            //control.Width = tempBrush.ImageSource.Width;
            //control.Height = tempBrush.ImageSource.Height;
        }

        if (_hoverLocations.TryGetValue(control, out var tempInfo))
        {
            Canvas.SetLeft(picBoxHover, tempInfo.Point.X);
            Canvas.SetTop(picBoxHover, tempInfo.Point.Y);
            picBoxHover.Width = tempInfo.Size.Width;
            picBoxHover.Height = tempInfo.Size.Height;
            picBoxHover.Stretch = Stretch.Fill;
            picBoxHover.Visibility = Visibility.Visible;
        }

        if (_hoverIndexes.TryGetValue(control, out var tempIndex))
        {
            _mappingListViewModel.SelectedIndex = tempIndex;
            mappingListBox.ScrollIntoView(mappingListBox.SelectedItem);
            var mapped = _mappingListViewModel.Mappings[tempIndex];
            UpdateHighlightLabel(mapped);
        }
    }

    private void UpdateHighlightLabel(MappedControl mapped)
    {
        var display = $"{mapped.ControlName}: {mapped.MappingName}";
        if (mapped.HasShiftAction())
        {
            display += "\nShift: ";
            display += mapped.ShiftMappingName;
        }

        highlightControlDisplayLb.Content = display;
    }

    private void ContBtn_MouseEnter(object sender, MouseEventArgs e)
    {
        var control = sender as Button;
        InputControlHighlight(control);
    }

    private void ContBtn_MouseLeave(object sender, MouseEventArgs e)
    {
        //Button control = sender as Button;
        //control.Background = new SolidColorBrush(Colors.Transparent);
        Canvas.SetLeft(picBoxHover, 0);
        Canvas.SetTop(picBoxHover, 0);
        picBoxHover.Visibility = Visibility.Hidden;
    }

    private void GyroOutModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var idx = gyroOutModeCombo.SelectedIndex;
        if (idx < 0) 
            return;
            
        if (_deviceNum < IControlService.CURRENT_DS4_CONTROLLER_LIMIT)
            _controlService.TouchPad[_deviceNum]?.ResetToggleGyroModes();
    }

    private void SetLateProperties(bool fullSave = true)
    {
        Global.BTPollRate[_deviceNum] = _profileSettingsViewModel.TempBTPollRateIndex;
        Global.OutContType[_deviceNum] = _profileSettingsViewModel.TempConType;
        if (fullSave) 
            Global.outDevTypeTemp[_deviceNum] = OutContType.X360;
    }

    private void SaveBtn_Click(object sender, RoutedEventArgs e)
    {
        var saved = ApplyProfileStep(false);
        if (saved) 
            Closed?.Invoke(this, EventArgs.Empty);
    }

    private bool ApplyProfileStep(bool fullSave = true)
    {
        var result = false;
        if (_profileSettingsViewModel.FuncDevNum < IControlService.CURRENT_DS4_CONTROLLER_LIMIT)
            _controlService.SetRumble(0, 0, _profileSettingsViewModel.FuncDevNum);

        var temp = profileNameTxt.Text;
        if (!string.IsNullOrWhiteSpace(temp) &&
            temp.IndexOfAny(Path.GetInvalidFileNameChars()) == -1)
        {
            SetLateProperties(false);
            Global.ProfilePath[_deviceNum] =
                Global.OlderProfilePath[_deviceNum] = temp;

            if (_currentProfile != null)
            {
                if (temp != _currentProfile.Name)
                {
                    //File.Delete(DS4Windows.Global.appdatapath + @"\Profiles\" + currentProfile.Name + ".xml");
                    _currentProfile.DeleteFile();
                    _currentProfile.Name = temp;
                }
            }

            if (_currentProfile != null)
            {
                _currentProfile.SaveProfile(_deviceNum);
                _currentProfile.FireSaved();
                result = true;
            }
            else
            {
                var tempprof = Global.appdatapath + @"\Profiles\" + temp + ".xml";
                if (!File.Exists(tempprof))
                {
                    Global.SaveProfile(_deviceNum, temp);
                    CreatedProfile?.Invoke(this, temp);
                    result = true;
                }
                else
                {
                    MessageBox.Show(Localization.ValidName, Localization.NotValid,
                        MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }
            }
        }
        else
        {
            MessageBox.Show(Localization.ValidName, Localization.NotValid,
                MessageBoxButton.OK, MessageBoxImage.Exclamation);
        }

        return result;
    }

    private void KeepSizeBtn_Click(object sender, RoutedEventArgs e)
    {
        _keepSize = true;
        var c = new ImageSourceConverter();
        sizeImage.Source = c.ConvertFromString($"{Global.ASSEMBLY_RESOURCE_PREFIX}component/Resources/checked.png") as ImageSource;
    }

    public void Close()
    {
        if (_profileSettingsViewModel.FuncDevNum < IControlService.CURRENT_DS4_CONTROLLER_LIMIT)
        {
            _controlService.SetRumble(0, 0, _profileSettingsViewModel.FuncDevNum);
        }

        Closed?.Invoke(this, EventArgs.Empty);
    }

    private void ColorByBatteryPerCk_Click(object sender, RoutedEventArgs e) => 
        ColorByBatteryPerCheck();

    private void ColorByBatteryPerCheck()
    {
        var state = _profileSettingsViewModel.ColorBatteryPercent;
        if (state)
        {
            colorGB.Header = Strings.Full;
            emptyColorGB.Visibility = Visibility.Visible;
        }
        else
        {
            colorGB.Header = Strings.Color;
            emptyColorGB.Visibility = Visibility.Hidden;
        }
    }

    private void FlashColorBtn_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ColorPickerWindow();
        dialog.Owner = Application.Current.MainWindow;
        var tempcolor = _profileSettingsViewModel.FlashColorMedia;
        dialog.colorPicker.SelectedColor = tempcolor;
        _profileSettingsViewModel.StartForcedColor(tempcolor);
        dialog.ColorChanged += (_, color) =>
        {
            _profileSettingsViewModel.UpdateForcedColor(color);
        };
        dialog.ShowDialog();
        _profileSettingsViewModel.EndForcedColor();
        _profileSettingsViewModel.UpdateFlashColor(dialog.colorPicker.SelectedColor.GetValueOrDefault());
    }

    private void LowColorBtn_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ColorPickerWindow();
        dialog.Owner = Application.Current.MainWindow;
        var tempColor = _profileSettingsViewModel.LowColorMedia;
        dialog.colorPicker.SelectedColor = tempColor;
        _profileSettingsViewModel.StartForcedColor(tempColor);
        dialog.ColorChanged += (sender2, color) =>
        {
            _profileSettingsViewModel.UpdateForcedColor(color);
        };
        dialog.ShowDialog();
        _profileSettingsViewModel.EndForcedColor();
        _profileSettingsViewModel.UpdateLowColor(dialog.colorPicker.SelectedColor.GetValueOrDefault());
    }

    private void HeavyRumbleTestBtn_Click(object sender, RoutedEventArgs e)
    {
        var deviceNum = _profileSettingsViewModel.FuncDevNum;
        if (deviceNum < IControlService.CURRENT_DS4_CONTROLLER_LIMIT)
        {
            var d = _controlService.DS4Controllers[deviceNum];
            if (d != null)
            {
                var rumbleActive = _profileSettingsViewModel.HeavyRumbleActive;
                if (!rumbleActive)
                {
                    _profileSettingsViewModel.HeavyRumbleActive = true;
                    d.SetRumble(d.RightLightFastRumble,
                        (byte)Math.Min(255, 255 * _profileSettingsViewModel.RumbleBoost / 100));
                    heavyRumbleTestBtn.Content = Localization.StopHText;
                }
                else
                {
                    _profileSettingsViewModel.HeavyRumbleActive = false;
                    d.SetRumble(d.RightLightFastRumble, 0);
                    heavyRumbleTestBtn.Content = Localization.TestHText;
                }
            }
        }
    }

    private void LightRumbleTestBtn_Click(object sender, RoutedEventArgs e)
    {
        var deviceNum = _profileSettingsViewModel.FuncDevNum;
            
        if (deviceNum >= IControlService.CURRENT_DS4_CONTROLLER_LIMIT)
            return;
            
        var device = _controlService.DS4Controllers[deviceNum];
            
        if (device is null) 
            return;
            
        var rumbleActive = _profileSettingsViewModel.LightRumbleActive;
        if (!rumbleActive)
        {
            _profileSettingsViewModel.LightRumbleActive = true;
            device.SetRumble((byte)Math.Min(255, 255 * _profileSettingsViewModel.RumbleBoost / 100),
                device.LeftHeavySlowRumble);
            lightRumbleTestBtn.Content = Localization.StopLText;
        }
        else
        {
            _profileSettingsViewModel.LightRumbleActive = false;
            device.SetRumble(0, device.LeftHeavySlowRumble);
            lightRumbleTestBtn.Content = Localization.TestLText;
        }
    }

    private void CustomEditorBtn_Click(object sender, RoutedEventArgs e)
    {
        var btn = (sender as Button)!;
        var tag = btn.Tag.ToString();
        if (tag == "LS") LaunchCurveEditor(_profileSettingsViewModel.LSCustomCurve);
        else if (tag == "RS") LaunchCurveEditor(_profileSettingsViewModel.RSCustomCurve);
        else if (tag == "L2") LaunchCurveEditor(_profileSettingsViewModel.L2CustomCurve);
        else if (tag == "R2") LaunchCurveEditor(_profileSettingsViewModel.R2CustomCurve);
        else if (tag == "SX") LaunchCurveEditor(_profileSettingsViewModel.SXCustomCurve);
        else if (tag == "SZ") LaunchCurveEditor(_profileSettingsViewModel.SZCustomCurve);
    }

    private void LaunchCurveEditor(string customDefinition)
    {
        _profileSettingsViewModel.LaunchCurveEditor(customDefinition);
    }

    private void LaunchProgBrowseBtn_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Multiselect = false,
            AddExtension = true,
            DefaultExt = ".exe",
            Filter = "Program (*.exe)|*.exe",
            Title = "Select Program",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)
        };

        if (dialog.ShowDialog() is true)
            _profileSettingsViewModel.UpdateLaunchProgram(dialog.FileName);
    }

    private void FrictionUD_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (_deviceNum < IControlService.CURRENT_DS4_CONTROLLER_LIMIT)
        {
            _controlService.TouchPad[_deviceNum]?.ResetTrackAccel(frictionUD.Value.GetValueOrDefault());
        }
    }

    private void RainbowBtn_Click(object sender, RoutedEventArgs e)
    {
        var active = _profileSettingsViewModel.Rainbow != 0.0;
        if (active)
        {
            _profileSettingsViewModel.Rainbow = 0.0;
            colorByBatteryPerCk.Content = Localization.ColorByBattery;
            colorGB.IsEnabled = true;
            emptyColorGB.IsEnabled = true;
        }
        else
        {
            _profileSettingsViewModel.Rainbow = 5.0;
            colorByBatteryPerCk.Content = Localization.DimByBattery;
            colorGB.IsEnabled = false;
            emptyColorGB.IsEnabled = false;
        }
    }

    private void ChargingColorBtn_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ColorPickerWindow();
        dialog.Owner = Application.Current.MainWindow;
        var tempcolor = _profileSettingsViewModel.ChargingColorMedia;
        dialog.colorPicker.SelectedColor = tempcolor;
        _profileSettingsViewModel.StartForcedColor(tempcolor);
        dialog.ColorChanged += (sender2, color) =>
        {
            _profileSettingsViewModel.UpdateForcedColor(color);
        };
        dialog.ShowDialog();
        _profileSettingsViewModel.EndForcedColor();
        _profileSettingsViewModel.UpdateChargingColor(dialog.colorPicker.SelectedColor.GetValueOrDefault());
    }

    private void SteeringWheelEmulationCalibrateBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_profileSettingsViewModel.SASteeringWheelEmulationAxisIndex <= 0) 
            return;
            
        var d = _controlService.DS4Controllers[_profileSettingsViewModel.FuncDevNum];
        if (d is not null)
        {
            var origWheelCenterPoint = new System.Drawing.Point(d.WheelCenterPoint.X, d.WheelCenterPoint.Y);
            var origWheel90DegPointLeft = new System.Drawing.Point(d.Wheel90DegPointLeft.X, d.Wheel90DegPointLeft.Y);
            var origWheel90DegPointRight = new System.Drawing.Point(d.Wheel90DegPointRight.X, d.Wheel90DegPointRight.Y);

            d.WheelRecalibrateActiveState = 1;

            var result = MessageBox.Show($"{Localization.SASteeringWheelEmulationCalibrate}.\n\n" +
                                         $"{Localization.SASteeringWheelEmulationCalibrateInstruction1}.\n" +
                                         $"{Localization.SASteeringWheelEmulationCalibrateInstruction2}.\n" +
                                         $"{Localization.SASteeringWheelEmulationCalibrateInstruction3}.\n\n" +
                                         $"{Localization.SASteeringWheelEmulationCalibrateInstruction}.\n",
                Localization.SASteeringWheelEmulationCalibrate, MessageBoxButton.OKCancel, MessageBoxImage.Information, MessageBoxResult.OK);

            if (result == MessageBoxResult.OK)
            {
                // Accept new calibration values (State 3 is "Complete calibration" state)
                d.WheelRecalibrateActiveState = 3;
            }
            else
            {
                // Cancel calibration and reset back to original calibration values
                d.WheelRecalibrateActiveState = 4;

                d.WheelFullTurnCount = 0;
                d.WheelCenterPoint = origWheelCenterPoint;
                d.Wheel90DegPointLeft = origWheel90DegPointLeft;
                d.Wheel90DegPointRight = origWheel90DegPointRight;
            }
        }
        else
        {
            MessageBox.Show($"{Localization.SASteeringWheelEmulationCalibrateNoControllerError}.");
        }
    }

    private void TouchDisInvertBtn_Click(object sender, RoutedEventArgs e) => 
        touchDisInvertBtn.ContextMenu.IsOpen = true;

    private void TouchDisInvertMenuItem_Click(object sender, RoutedEventArgs e) => 
        _profileSettingsViewModel.UpdateTouchDisInvert(touchDisInvertBtn.ContextMenu);

    private void GyroMouseTrigMenuItem_Click(object sender, RoutedEventArgs e)
    {
        var menu = gyroMouseTrigBtn.ContextMenu;
        var itemCount = menu.Items.Count;
        var alwaysOnItem = menu.Items[itemCount - 1] as MenuItem;

        _profileSettingsViewModel.UpdateGyroMouseTrig(menu, e.OriginalSource == alwaysOnItem);
    }

    private void GyroMouseStickTrigMenuItem_Click(object sender, RoutedEventArgs e)
    {
        var menu = gyroMouseStickTrigBtn.ContextMenu;
        var itemCount = menu.Items.Count;
        var alwaysOnItem = menu.Items[itemCount - 1] as MenuItem;

        _profileSettingsViewModel.UpdateGyroMouseStickTrig(menu, e.OriginalSource == alwaysOnItem);
    }

    private void GyroMouseTrigBtn_Click(object sender, RoutedEventArgs e) => 
        gyroMouseTrigBtn.ContextMenu.IsOpen = true;

    private void GyroMouseStickTrigBtn_Click(object sender, RoutedEventArgs e) => 
        gyroMouseStickTrigBtn.ContextMenu.IsOpen = true;

    private void OutConTypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var index = outConTypeCombo.SelectedIndex;
        if (index >= 0)
        {
            _mappingListViewModel.UpdateMappingDevType(_profileSettingsViewModel.TempConType);
        }
    }

    private void NewActionBtn_Click(object sender, RoutedEventArgs e)
    {
        baseSpeActPanel.Visibility = Visibility.Collapsed;
        var actEditor = _specialActionEditorFactory.Create(_deviceNum, _profileListService.ProfileList, null);
        specialActionDockPanel.Children.Add(actEditor);
        actEditor.Visibility = Visibility.Visible;
        actEditor.Cancel += (sender2, args) =>
        {
            specialActionDockPanel.Children.Remove(actEditor);
            baseSpeActPanel.Visibility = Visibility.Visible;
        };
        actEditor.Saved += (sender2, actionName) =>
        {
            var action = Global.GetAction(actionName);
            var newitem = _specialActionsViewModel.CreateActionItem(action);
            newitem.Active = true;
            var lastIdx = _specialActionsViewModel.ActionCol.Count;
            newitem.Index = lastIdx;
            _specialActionsViewModel.ActionCol.Add(newitem);
            specialActionDockPanel.Children.Remove(actEditor);
            baseSpeActPanel.Visibility = Visibility.Visible;

            _specialActionsViewModel.ExportEnabledActions();
            Global.CacheExtraProfileInfo(_profileSettingsViewModel.Device);
        };
    }

    private void EditActionBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_specialActionsViewModel.SpecialActionIndex < 0) 
            return;
            
        var item = _specialActionsViewModel.CurrentSpecialActionItem;
        var currentIndex = item.Index;
        //int viewIndex = specialActionsVM.SpecialActionIndex;
        //int currentIndex = specialActionsVM.ActionCol[viewIndex].Index;
        //SpecialActionItem item = specialActionsVM.ActionCol[currentIndex];
        baseSpeActPanel.Visibility = Visibility.Collapsed;
                
        var actionEditor = _specialActionEditorFactory.Create(_deviceNum, _profileListService.ProfileList, item.SpecialAction);
        specialActionDockPanel.Children.Add(actionEditor);
        actionEditor.Visibility = Visibility.Visible;
        actionEditor.Cancel += (_, _) =>
        {
            specialActionDockPanel.Children.Remove(actionEditor);
            baseSpeActPanel.Visibility = Visibility.Visible;
        };
        actionEditor.Saved += (_, actionName) =>
        {
            var action = Global.GetAction(actionName);
            var newActionItem = _specialActionsViewModel.CreateActionItem(action);
            newActionItem.Active = item.Active;
            newActionItem.Index = currentIndex;
            _specialActionsViewModel.ActionCol.RemoveAt(currentIndex);
            _specialActionsViewModel.ActionCol.Insert(currentIndex, newActionItem);
            specialActionDockPanel.Children.Remove(actionEditor);
            baseSpeActPanel.Visibility = Visibility.Visible;
            Global.CacheExtraProfileInfo(_profileSettingsViewModel.Device);
        };
    }

    private void RemoveActionBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_specialActionsViewModel.SpecialActionIndex < 0) 
            return;
            
        var item = _specialActionsViewModel.CurrentSpecialActionItem;
        //int currentIndex = specialActionsVM.ActionCol[specialActionsVM.SpecialActionIndex].Index;
        //SpecialActionItem item = specialActionsVM.ActionCol[currentIndex];
        _specialActionsViewModel.RemoveAction(item);
        Global.CacheExtraProfileInfo(_profileSettingsViewModel.Device);
    }

    private void SpecialActionCheckBox_Click(object sender, RoutedEventArgs e) => 
        _specialActionsViewModel.ExportEnabledActions();

    private void Ds4LightbarColorBtn_MouseEnter(object sender, MouseEventArgs e) => 
        highlightControlDisplayLb.Content = "Click the lightbar for color picker";

    private void Ds4LightbarColorBtn_MouseLeave(object sender, MouseEventArgs e) => 
        highlightControlDisplayLb.Content = "";

    private void Ds4LightbarColorBtn_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ColorPickerWindow();
        dialog.Owner = Application.Current.MainWindow;
        var tempcolor = _profileSettingsViewModel.MainColor;
        dialog.colorPicker.SelectedColor = tempcolor;
        _profileSettingsViewModel.StartForcedColor(tempcolor);
        dialog.ColorChanged += (sender2, color) =>
        {
            _profileSettingsViewModel.UpdateForcedColor(color);
        };
        dialog.ShowDialog();
        _profileSettingsViewModel.EndForcedColor();
        _profileSettingsViewModel.UpdateMainColor(dialog.colorPicker.SelectedColor.GetValueOrDefault());
    }

    private void InputDs4(object sender, System.Timers.ElapsedEventArgs e)
    {
        _inputTimer.Stop();

        var activeWin = false;
        var tempDeviceNum = 0;
        Dispatcher.Invoke(() =>
        {
            activeWin = Application.Current.MainWindow.IsActive;
            tempDeviceNum = _profileSettingsViewModel.FuncDevNum;
        });

        if (activeWin && _profileSettingsViewModel.UseControllerReadout)
        {
            var index = -1;
            switch(_controlService.GetActiveInputControl(tempDeviceNum))
            {
                case DS4Controls.None: break;
                case DS4Controls.Cross: index = 0; break;
                case DS4Controls.Circle: index = 1; break;
                case DS4Controls.Square: index = 2; break;
                case DS4Controls.Triangle: index = 3; break;
                case DS4Controls.Options: index = 4; break;
                case DS4Controls.Share: index = 5; break;
                case DS4Controls.DpadUp: index = 6; break;
                case DS4Controls.DpadDown: index = 7; break;
                case DS4Controls.DpadLeft: index = 8; break;
                case DS4Controls.DpadRight: index = 9; break;
                case DS4Controls.PS: index = 10; break;
                case DS4Controls.Mute: index = 11; break;
                case DS4Controls.L1: index = 12; break;
                case DS4Controls.R1: index = 13; break;
                case DS4Controls.L2: index = 14; break;
                case DS4Controls.R2: index = 15; break;
                case DS4Controls.L3: index = 16; break;
                case DS4Controls.R3: index = 17; break;
                case DS4Controls.TouchLeft: index = 18; break;
                case DS4Controls.TouchRight: index = 19; break;
                case DS4Controls.TouchMulti: index = 20; break;
                case DS4Controls.TouchUpper: index = 21; break;
                case DS4Controls.LYNeg: index = 22; break;
                case DS4Controls.LYPos: index = 23; break;
                case DS4Controls.LXNeg: index = 24; break;
                case DS4Controls.LXPos: index = 25; break;
                case DS4Controls.RYNeg: index = 26; break;
                case DS4Controls.RYPos: index = 27; break;
                case DS4Controls.RXNeg: index = 28; break;
                case DS4Controls.RXPos: index = 29; break;
                default: break;
            }

            if (index >= 0)
            {
                Dispatcher.BeginInvoke((Action)(() =>
                {
                    _mappingListViewModel.SelectedIndex = index;
                    ShowControlBindingWindow();
                }));
            }
        }

        if (_profileSettingsViewModel.UseControllerReadout)
        {
            _inputTimer.Start();
        }
    }
    private void ProfileEditor_Closed(object sender, EventArgs e)
    {
        _profileSettingsViewModel.UseControllerReadout = false;
        _inputTimer.Stop();
        conReadingsUserCon.EnableControl(false);
        Global.CacheExtraProfileInfo(_profileSettingsViewModel.Device);
    }

    private void UseControllerReadoutCk_Click(object sender, RoutedEventArgs e)
    {
        if (_profileSettingsViewModel.UseControllerReadout && _profileSettingsViewModel.Device < IControlService.CURRENT_DS4_CONTROLLER_LIMIT)
        {
            _inputTimer.Start();
        }
        else
        {
            _inputTimer.Stop();
        }
    }

    private void ShowControlBindingWindow()
    {
        var mpControl = _mappingListViewModel.Mappings[_mappingListViewModel.SelectedIndex];
        var window = _bindingWindowFactory.Create(_deviceNum, mpControl.Setting);
        window.Owner = Application.Current.MainWindow;
        window.ShowDialog();
        mpControl.UpdateMappingName();
        UpdateHighlightLabel(mpControl);
        Global.CacheProfileCustomsFlags(_profileSettingsViewModel.Device);
    }

    private void MappingListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (_mappingListViewModel.SelectedIndex >= 0)
        {
            ShowControlBindingWindow();
        }
    }

    private void SidebarTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sidebarTabControl.SelectedItem == contReadingsTab)
        {
            _controllerReadingsTabActive = true;
            conReadingsUserCon.EnableControl(true);
        }
        else if (_controllerReadingsTabActive)
        {
            _controllerReadingsTabActive = false;
            conReadingsUserCon.EnableControl(false);
        }
    }

    private void TiltControlsButton_Click(object sender, RoutedEventArgs e)
    {
        var btn = sender as Button;
        var control = (DS4Controls)Convert.ToInt32(btn.Tag);
        var mpControl = _mappingListViewModel.ControlMap[control];
        var window = _bindingWindowFactory.Create(_deviceNum, mpControl.Setting);
        window.Owner = Application.Current.MainWindow;
        window.ShowDialog();
        mpControl.UpdateMappingName();
        UpdateHighlightLabel(mpControl);
        Global.CacheProfileCustomsFlags(_profileSettingsViewModel.Device);
    }

    private void SwipeControlsButton_Click(object sender, RoutedEventArgs e)
    {
        var btn = sender as Button;
        var control = (DS4Controls)Convert.ToInt32(btn.Tag);
        var mpControl = _mappingListViewModel.ControlMap[control];
        var window = _bindingWindowFactory.Create(_deviceNum, mpControl.Setting);
        window.Owner = Application.Current.MainWindow;
        window.ShowDialog();
        mpControl.UpdateMappingName();
        UpdateHighlightLabel(mpControl);
        Global.CacheProfileCustomsFlags(_profileSettingsViewModel.Device);
    }

    private void ConBtn_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        var btn = sender as Button;
        var mpControl = _mappingListViewModel.Mappings[_mappingListViewModel.SelectedIndex];
        _profileSettingsViewModel.PresetMenuUtil.SetHighlightControl(mpControl.Control);
        var cm = conCanvas.FindResource("presetMenu") as ContextMenu;
        var temp = cm.Items[0] as MenuItem;
        temp.Header = _profileSettingsViewModel.PresetMenuUtil.PresetInputLabel;
        cm.PlacementTarget = btn;
        cm.IsOpen = true;
    }

    private void PresetMenuItem_Click(object sender, RoutedEventArgs e)
    {
        var item = sender as MenuItem;
        var baseTag = Convert.ToInt32(item.Tag);
        var subTag = Convert.ToInt32(item.CommandParameter);
        if (baseTag >= 0 && subTag >= 0)
        {
            var controls =
                _profileSettingsViewModel.PresetMenuUtil.ModifySettingWithPreset(baseTag, subTag);
            foreach(var control in controls)
            {
                var mpControl = _mappingListViewModel.ControlMap[control];
                mpControl.UpdateMappingName();
            }

            Global.CacheProfileCustomsFlags(_profileSettingsViewModel.Device);
            highlightControlDisplayLb.Content = "";
        }
    }

    private void PresetBtn_Click(object sender, RoutedEventArgs e)
    {
        sidebarTabControl.SelectedIndex = 0;

        var presetWin = new PresetOptionWindow(_serviceProvider);
        presetWin.SetupData(_deviceNum);
        presetWin.ToPresetsScreen();
        presetWin.DelayPresetApply = true;
        presetWin.ShowDialog();

        if (presetWin.Result == MessageBoxResult.OK)
        {
            StopEditorBindings();
            presetWin.ApplyPreset();
            RefreshEditorBindings();
        }
    }

    private void ApplyBtn_Click(object sender, RoutedEventArgs e)
    {
        ApplyProfileStep();
    }

    private void TriggerFullPullBtn_Click(object sender, RoutedEventArgs e)
    {
        var btn = sender as Button;
        var tag = Convert.ToInt32(btn.Tag);
        var ds4Control = (DS4Controls)tag;
        if (ds4Control == DS4Controls.None)
        {
            return;
        }

        //DS4ControlSettings setting = Global.getDS4CSetting(tag, ds4control);
        var mpControl = _mappingListViewModel.ControlMap[ds4Control];
        var window = _bindingWindowFactory.Create(_deviceNum, mpControl.Setting);
        window.Owner = Application.Current.MainWindow;
        window.ShowDialog();
        mpControl.UpdateMappingName();
        Global.CacheProfileCustomsFlags(_profileSettingsViewModel.Device);
    }

    private void GyroCalibration_Click(object sender, RoutedEventArgs e)
    {
        var deviceNum = _profileSettingsViewModel.FuncDevNum;
        if (deviceNum < IControlService.CURRENT_DS4_CONTROLLER_LIMIT)
        {
            var d = _controlService.DS4Controllers[deviceNum];
            d.SixAxis.ResetContinuousCalibration();
            if (d.JointDeviceSlotNumber != DS4Device.DEFAULT_JOINT_SLOT_NUMBER)
            {
                var tempDev = _controlService.DS4Controllers[d.JointDeviceSlotNumber];
                tempDev?.SixAxis.ResetContinuousCalibration();
            }
        }
    }

    private void GyroSwipeTrigBtn_Click(object sender, RoutedEventArgs e)
    {
        gyroSwipeTrigBtn.ContextMenu.IsOpen = true;
    }

    private void GyroSwipeTrigMenuItem_Click(object sender, RoutedEventArgs e)
    {
        var menu = gyroSwipeTrigBtn.ContextMenu;
        var itemCount = menu.Items.Count;
        var alwaysOnItem = menu.Items[itemCount - 1] as MenuItem;

        _profileSettingsViewModel.UpdateGyroSwipeTrig(menu, e.OriginalSource == alwaysOnItem);
    }

    private void GyroSwipeControlsBtn_Click(object sender, RoutedEventArgs e)
    {
        var btn = sender as Button;
        var control = (DS4Controls)Convert.ToInt32(btn.Tag);
        var mpControl = _mappingListViewModel.ControlMap[control];
        var window = _bindingWindowFactory.Create(_deviceNum, mpControl.Setting);
        window.Owner = Application.Current.MainWindow;
        window.ShowDialog();
        mpControl.UpdateMappingName();
        Global.CacheProfileCustomsFlags(_profileSettingsViewModel.Device);
    }

    private void GyroControlsTrigBtn_Click(object sender, RoutedEventArgs e)
    {
        gyroControlsTrigBtn.ContextMenu.IsOpen = true;
    }

    private void GyroControlsMenuItem_Click(object sender, RoutedEventArgs e)
    {
        var menu = gyroControlsTrigBtn.ContextMenu;
        var itemCount = menu.Items.Count;
        var alwaysOnItem = menu.Items[itemCount - 1] as MenuItem;

        _profileSettingsViewModel.UpdateGyroControlsTrig(menu, e.OriginalSource == alwaysOnItem);
    }

    private void StickOuterBindButton_Click(object sender, RoutedEventArgs e)
    {
        var btn = sender as Button;
        var tag = Convert.ToInt32(btn.Tag);
        var ds4Control = (DS4Controls)tag;
        if (ds4Control == DS4Controls.None)
        {
            return;
        }

        //DS4ControlSettings setting = Global.getDS4CSetting(tag, ds4control);
        var mpControl = _mappingListViewModel.ControlMap[ds4Control];
        var window = _bindingWindowFactory.Create(_deviceNum, mpControl.Setting);
        window.Owner = Application.Current.MainWindow;
        window.ShowDialog();
        mpControl.UpdateMappingName();
        Global.CacheProfileCustomsFlags(_profileSettingsViewModel.Device);
    }
}

public class ControlIndexCheck
{
    public int TiltUp { get => (int)DS4Controls.GyroZNeg; }
    public int TiltDown { get => (int)DS4Controls.GyroZPos; }
    public int TiltLeft { get => (int)DS4Controls.GyroXPos; }
    public int TiltRight { get => (int)DS4Controls.GyroXNeg; }

    public int SwipeUp { get => (int)DS4Controls.SwipeUp; }
    public int SwipeDown { get => (int)DS4Controls.SwipeDown; }
    public int SwipeLeft { get => (int)DS4Controls.SwipeLeft; }
    public int SwipeRight { get => (int)DS4Controls.SwipeRight; }
    public int L2FullPull { get => (int)DS4Controls.L2FullPull; }
    public int R2FullPull { get => (int)DS4Controls.R2FullPull; }

    public int LsOuterBind { get => (int)DS4Controls.LSOuter; }
    public int RsOuterBind { get => (int)DS4Controls.RSOuter; }

    public int GyroSwipeLeft { get => (int)DS4Controls.GyroSwipeLeft; }
    public int GyroSwipeRight { get => (int)DS4Controls.GyroSwipeRight; }
    public int GyroSwipeUp { get => (int)DS4Controls.GyroSwipeUp; }
    public int GyroSwipeDown { get => (int)DS4Controls.GyroSwipeDown; }
}