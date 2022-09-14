﻿using System.Windows.Input;

namespace DS4Windows;

public class MacroParser
{
    private bool _loaded;
    private readonly List<MacroStep> _macroSteps;
    private readonly int[] _inputMacro;
    private readonly Dictionary<int, bool> _keydown = new();
    
    public static readonly Dictionary<int, string> MacroInputNames = new()
    {
        [256] = "Left Mouse Button", [257] = "Right Mouse Button",
        [258] = "Middle Mouse Button", [259] = "4th Mouse Button",
        [260] = "5th Mouse Button",
        [261] = "A Button",
        [262] = "B Button", [263] = "X Button",
        [264] = "Y Button", [265] = "Start",
        [266] = "Back", [267] = "Up Button",
        [268] = "Down Button", [269] = "Left Button",
        [270] = "Right Button", [271] = "Guide",
        [272] = "Left Bumper", [273] = "Right Bumper",
        [274] = "Left Trigger", [275] = "Right Trigger",
        [276] = "Left Stick", [277] = "Right Stick",
        [278] = "LS Right", [279] = "LS Left",
        [280] = "LS Down", [281] = "LS Up",
        [282] = "RS Right", [283] = "RS Left",
        [284] = "RS Down", [285] = "RS Up",
        [286] = "Touchpad Click",
    };

    public List<MacroStep> MacroSteps { get => _macroSteps; }

    public MacroParser(int[] macro)
    {
        _macroSteps = new List<MacroStep>();
        _inputMacro = macro;
    }

    public void LoadMacro()
    {
        if (_loaded)
            return;

        _keydown.Clear();
        foreach (var value in _inputMacro)
        {
            var step = ParseStep(value);
            _macroSteps.Add(step);
        }

        _loaded = true;
    }

    public List<string> GetMacroStrings()
    {
        if (!_loaded)
            LoadMacro();

        return _macroSteps.Select(step => step.Name).ToList();
    }

    private MacroStep ParseStep(int value)
    {
        string name = string.Empty;
        var type = MacroStep.StepType.ActDown;
        var outType = MacroStep.StepOutput.Key;

        if (value >= 1000000000)
        {
            outType = MacroStep.StepOutput.Lightbar;
            if (value > 1000000000)
            {
                type = MacroStep.StepType.ActDown;
                var lb = value.ToString().Substring(1);
                var r = (byte)(int.Parse(lb[0].ToString()) * 100 + int.Parse(lb[1].ToString()) * 10 + int.Parse(lb[2].ToString()));
                var g = (byte)(int.Parse(lb[3].ToString()) * 100 + int.Parse(lb[4].ToString()) * 10 + int.Parse(lb[5].ToString()));
                var b = (byte)(int.Parse(lb[6].ToString()) * 100 + int.Parse(lb[7].ToString()) * 10 + int.Parse(lb[8].ToString()));
                name = $"Lightbar Color: {r},{g},{b}";
            }
            else
            {
                type = MacroStep.StepType.ActUp;
                name = "Reset Lightbar";
            }
        }
        else if (value >= 1000000)
        {
            outType = MacroStep.StepOutput.Rumble;
            if (value > 1000000)
            {
                type = MacroStep.StepType.ActDown;
                var r = value.ToString().Substring(1);
                var heavy = (byte)(int.Parse(r[0].ToString()) * 100 + int.Parse(r[1].ToString()) * 10 + int.Parse(r[2].ToString()));
                var light = (byte)(int.Parse(r[3].ToString()) * 100 + int.Parse(r[4].ToString()) * 10 + int.Parse(r[5].ToString()));
                name = $"Rumble {heavy}, {light} ({Math.Round((heavy * .75f + light * .25f) / 2.55f, 1)}%)";
            }
            else
            {
                type = MacroStep.StepType.ActUp;
                name = "Stop Rumble";
            }
        }
        else if (value >= 300) // ints over 300 used to delay
        {
            type = MacroStep.StepType.Wait;
            outType = MacroStep.StepOutput.None;
            name = $"Wait {(value - 300).ToString()} ms";
        }
        else
        {
            // anything above 255 is not a key value
            outType = value <= 255 ? MacroStep.StepOutput.Key : MacroStep.StepOutput.Button;
            _keydown.TryGetValue(value, out var isdown);
            if (!isdown)
            {
                type = MacroStep.StepType.ActDown;
                _keydown.Add(value, true);
                if (outType == MacroStep.StepOutput.Key)
                {
                    name = KeyInterop.KeyFromVirtualKey(value).ToString();
                }
                else
                {
                    MacroInputNames.TryGetValue(value, out name);
                }
            }
            else
            {
                type = MacroStep.StepType.ActUp;
                _keydown.Remove(value);
                if (outType == MacroStep.StepOutput.Key)
                {
                    name = KeyInterop.KeyFromVirtualKey(value).ToString();
                }
                else
                {
                    MacroInputNames.TryGetValue(value, out name);
                }
            }
        }

        var step = new MacroStep(value, name, type, outType);
        return step;
    }

    public void Reset()
    {
        _loaded = false;
    }
}

public class MacroStep
{
    public enum StepType : uint
    {
        ActDown,
        ActUp,
        Wait
    }

    public enum StepOutput : uint
    {
        None,
        Key,
        Button,
        Rumble,
        Lightbar
    }

    private string _name;
    private int _value;

    public string Name
    {
        get => _name;
        set
        {
            _name = value;
            NameChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    public event EventHandler NameChanged;
    public int Value
    {
        get => _value;
        set
        {
            _value = value;
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    public event EventHandler? ValueChanged;
    
    public StepType ActType { get; }

    public StepOutput OutputType { get; }

    public MacroStep(int value, string name, StepType act, StepOutput output)
    {
        _value = value;
        _name = name;
        ActType = act;
        OutputType = output;

        ValueChanged += MacroStep_ValueChanged;
    }

    private void MacroStep_ValueChanged(object? sender, EventArgs e)
    {
        if (ActType == StepType.Wait)
        {
            Name = $"Wait {_value-300}ms";
        }
        else if (OutputType == StepOutput.Rumble)
        {
            var result = _value;
            result -= 1000000;
            var curHeavy = result / 1000;
            var curLight = result - (curHeavy * 1000);
            Name = $"Rumble {curHeavy},{curLight}";
        }
        else if (OutputType == StepOutput.Lightbar)
        {
            var temp = _value - 1000000000;
            var r = temp / 1000000;
            temp -= (r * 1000000);
            var g = temp / 1000;
            temp -= (g * 1000);
            var b = temp;
            Name = $"Lightbar Color: {r},{g},{b}";
        }
    }
}