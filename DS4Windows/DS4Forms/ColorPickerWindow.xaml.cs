﻿using System.Windows.Media;

namespace DS4WinWPF.DS4Forms;

/// <summary>
/// Interaction logic for ColorPickerWindow.xaml
/// </summary>
public partial class ColorPickerWindow : Window
{
    public delegate void ColorChangedHandler(ColorPickerWindow sender, Color color);
    public event ColorChangedHandler ColorChanged;

    public ColorPickerWindow()
    {
        InitializeComponent();
    }

    private void ColorPicker_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
    {
        ColorChanged?.Invoke(this, e.NewValue.GetValueOrDefault());
    }
}