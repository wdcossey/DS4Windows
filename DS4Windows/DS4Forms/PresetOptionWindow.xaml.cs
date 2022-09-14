﻿namespace DS4WinWPF.DS4Forms;

/// <summary>
/// Interaction logic for PresetOptionWindow.xaml
/// </summary>
public partial class PresetOptionWindow : Window
{
    private MessageBoxResult result = MessageBoxResult.Cancel;
    public MessageBoxResult Result { get => result; }

    private int deviceIndex;
    private PresetOptionViewModel presetOptionVM;
    private bool delayPresetApply;
    public bool DelayPresetApply { get => delayPresetApply; set => delayPresetApply = value; }

    public PresetOptionWindow(IServiceProvider serviceProvider)
    {
        InitializeComponent();

        presetOptionVM = serviceProvider.GetRequiredService<PresetOptionViewModel>();

        // Hide tab headers. Tab content will still be visible
        IntroTab.Visibility = Visibility.Collapsed;
        PresetListTab.Visibility = Visibility.Collapsed;
    }

    public void SetupData(int index)
    {
        deviceIndex = index;
        PresetListTab.DataContext = presetOptionVM;
    }

    public void ToPresetsScreen()
    {
        screensTabControl.SelectedItem = PresetListTab;
    }

    public void ApplyPreset()
    {
        presetOptionVM.ApplyPreset(deviceIndex);
    }

    private void NoPresetBtn_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void YesPresetBtn_Click(object sender, RoutedEventArgs e)
    {
        screensTabControl.SelectedItem = PresetListTab;
        //PresetListTab.DataContext = presetOptionVM;
    }

    private void PresetCancelBtn_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void PresetApplyBtn_Click(object sender, RoutedEventArgs e)
    {
        if (!delayPresetApply)
        {
            presetOptionVM.ApplyPreset(deviceIndex);
        }

        result = MessageBoxResult.OK;
        Close();
    }
}