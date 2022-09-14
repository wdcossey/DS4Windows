﻿using System.Windows.Controls;

namespace DS4WinWPF.DS4Forms;

/// <summary>
/// Interaction logic for OutputSlotManager.xaml
/// </summary>
public partial class OutputSlotManagerControl : UserControl
{
    private IControlService controlService;
    private CurrentOutDeviceViewModel currentOutDevVM;
    //private PermanentOutDevViewModel permanentDevVM;

    public OutputSlotManagerControl()
    {
        InitializeComponent();
    }

    public void SetupDataContext(IControlService controlService,
        DS4Windows.OutputSlotManager outputMan)
    {
        this.controlService = controlService;

        currentOutDevVM = new CurrentOutDeviceViewModel(controlService, outputMan);
        currentOutDevVM.SelectedIndexChanged += CurrentOutDevVM_SelectedIndexChanged;
        currentOutDevLV.DataContext = currentOutDevVM;
        sideStackPanel.DataContext = currentOutDevVM;
        plugDevStackPanel.DataContext = currentOutDevVM;
        outSlotStackPanel.DataContext = null;

        //permanentDevVM = new PermanentOutDevViewModel(controlService, outputMan);
        //permanentOutDevLV.DataContext = permanentDevVM;
    }

    private void CurrentOutDevVM_SelectedIndexChanged(object sender, EventArgs e)
    {
        int idx = currentOutDevVM.SelectedIndex;
        if (idx >= 0)
        {
            outSlotStackPanel.DataContext = currentOutDevVM.SlotDeviceEntries[idx];
        }
        else
        {
            outSlotStackPanel.DataContext = null;
        }
    }

    public void SetupLateEvents()
    {

    }

    private void PluginBtn_Click(object sender, RoutedEventArgs e)
    {
        int idx = currentOutDevVM.SelectedIndex;
        SlotDeviceEntry tempEntry = null;
        if (idx >= 0)
        {
            tempEntry = currentOutDevVM.SlotDeviceEntries[idx];
        }

        if (tempEntry != null &&
            tempEntry.OutSlotDevice.CurrentReserveStatus ==
            DS4Control.OutSlotDevice.ReserveStatus.Permanent &&
            tempEntry.OutSlotDevice.PermanentType != DS4Windows.OutContType.None)
        {
            tempEntry.OutSlotDevice.CurrentType = tempEntry.OutSlotDevice.PermanentType;
            tempEntry.RequestPlugin();
        }
        else
        {
            PluginOutDevWindow devWindow = new PluginOutDevWindow();
            devWindow.ShowDialog();
            MessageBoxResult result = devWindow.Result;
            if (result == MessageBoxResult.OK)
            {
                tempEntry.OutSlotDevice.CurrentType = devWindow.ContType;
                tempEntry.OutSlotDevice.CurrentReserveStatus = devWindow.ReserveType;
                if (tempEntry.OutSlotDevice.CurrentReserveStatus ==
                    DS4Control.OutSlotDevice.ReserveStatus.Permanent)
                {
                    tempEntry.OutSlotDevice.PermanentType = devWindow.ContType;
                }

                tempEntry.RequestPlugin();
            }
        }
    }

    private void UnplugBtn_Click(object sender, RoutedEventArgs e)
    {
        int idx = currentOutDevVM.SelectedIndex;
        if (idx >= 0)
        {
            currentOutDevVM.SlotDeviceEntries[idx].RequestUnplug();
        }
    }

    private void SlotChangeAcceptBtn_Click(object sender, RoutedEventArgs e)
    {
        int idx = currentOutDevVM.SelectedIndex;
        if (idx >= 0)
        {
            currentOutDevVM.SlotDeviceEntries[idx].ApplyChanges();
        }
    }
}