using System.Collections.Generic;
using DS4WinWPF.DS4Control;
using DS4WinWPF.DS4Forms.ViewModels.Util;

namespace DS4WinWPF.DS4Forms.ViewModels;

public class PresetOptionViewModel
{
    private int _presetIndex;
    private PresetOption.OutputContChoice _controllerChoice =
        PresetOption.OutputContChoice.Xbox360;

    public int PresetIndex
    {
        get => _presetIndex;
        set
        {
            if (_presetIndex == value) return;
            _presetIndex = value;
            PresetIndexChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    public event EventHandler PresetIndexChanged;

    private readonly List<PresetOption> _presetList;
    public List<PresetOption> PresetsList => _presetList;

    public string PresetDescription => _presetList[_presetIndex].Description;
        
    public event EventHandler PresetDescriptionChanged;

    public bool PresetDisplayOutputCont => _presetList[_presetIndex].OutputControllerChoice;
        
    public event EventHandler PresetDisplayOutputContChanged;

    public PresetOption.OutputContChoice ControllerChoice
    {
        get => _controllerChoice;
        set => _controllerChoice = value;
    }

    private List<EnumChoiceSelection<PresetOption.OutputContChoice>> outputChoices =
        new()
        {
            new EnumChoiceSelection<PresetOption.OutputContChoice>("Xbox 360", PresetOption.OutputContChoice.Xbox360),
            new EnumChoiceSelection<PresetOption.OutputContChoice>("DualShock 4", PresetOption.OutputContChoice.DualShock4),
        };

    public List<EnumChoiceSelection<PresetOption.OutputContChoice>> OutputChoices { get => outputChoices; }

    public PresetOptionViewModel(IEnumerable<PresetOption> presetOptions)
    {
        _presetList = new List<PresetOption>(presetOptions);
        PresetIndexChanged += PresetOptionViewModel_PresetIndexChanged;
    }

    private void PresetOptionViewModel_PresetIndexChanged(object sender, EventArgs e)
    {
        PresetDescriptionChanged?.Invoke(this, EventArgs.Empty);
        PresetDisplayOutputContChanged?.Invoke(this, EventArgs.Empty);
    }

    public void ApplyPreset(int index)
    {
        if (_presetIndex >= 0)
        {
            var current = _presetList[_presetIndex];
            if (current.OutputControllerChoice &&
                _controllerChoice != PresetOption.OutputContChoice.None)
            {
                current.OutputCont = _controllerChoice;
            }

            current.ApplyPreset(index);
        }
    }
}