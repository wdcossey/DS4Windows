using System.Windows.Media;

namespace DS4WinWPF.DS4Forms.ViewModels;

public class BindingWindowViewModel
{
    public bool Using360Mode { get; }
    public int DeviceNum { get; }
    public OutBinding CurrentOutBind { get; }
    public OutBinding ShiftOutBind { get; }
    public OutBinding ActionBinding { get; set; }
    public bool ShowShift { get; set; }
    public bool RumbleActive { get; set; }
    public DS4ControlSettings Settings { get; }

    public BindingWindowViewModel(int deviceNum, DS4ControlSettings settings)
    {
        DeviceNum = deviceNum;
        Using360Mode = Global.outDevTypeTemp[deviceNum] == OutContType.X360;
        Settings = settings;
        CurrentOutBind = new OutBinding();
        ShiftOutBind = new OutBinding { ShiftBind = true };
        PopulateCurrentBinds();
    }

    public void PopulateCurrentBinds()
    {
        DS4ControlSettings setting = Settings;
        bool sc = setting.keyType.HasFlag(DS4KeyType.ScanCode);
        bool toggle = setting.keyType.HasFlag(DS4KeyType.Toggle);
        CurrentOutBind.input = setting.control;
        ShiftOutBind.input = setting.control;
        if (setting.actionType != DS4ControlSettings.ActionType.Default)
        {
            switch(setting.actionType)
            {
                case DS4ControlSettings.ActionType.Button:
                    CurrentOutBind.OutputType = OutBinding.OutType.Button;
                    CurrentOutBind.Control = (X360Controls)setting.action.actionBtn;
                    break;
                case DS4ControlSettings.ActionType.Default:
                    CurrentOutBind.OutputType = OutBinding.OutType.Default;
                    break;
                case DS4ControlSettings.ActionType.Key:
                    CurrentOutBind.OutputType = OutBinding.OutType.Key;
                    CurrentOutBind.OutKey = setting.action.actionKey;
                    CurrentOutBind.HasScanCode = sc;
                    CurrentOutBind.Toggle = toggle;
                    break;
                case DS4ControlSettings.ActionType.Macro:
                    CurrentOutBind.OutputType = OutBinding.OutType.Macro;
                    CurrentOutBind.macro = (int[])setting.action.actionMacro;
                    CurrentOutBind.MacroType = Settings.keyType;
                    CurrentOutBind.HasScanCode = sc;
                    break;
            }
        }
        else
        {
            CurrentOutBind.OutputType = OutBinding.OutType.Default;
        }

        if (!string.IsNullOrEmpty(setting.extras))
        {
            CurrentOutBind.ParseExtras(setting.extras);
        }

        if (setting.shiftActionType != DS4ControlSettings.ActionType.Default)
        {
            sc = setting.shiftKeyType.HasFlag(DS4KeyType.ScanCode);
            toggle = setting.shiftKeyType.HasFlag(DS4KeyType.Toggle);
            ShiftOutBind.ShiftTrigger = setting.shiftTrigger;
            switch (setting.shiftActionType)
            {
                case DS4ControlSettings.ActionType.Button:
                    ShiftOutBind.OutputType = OutBinding.OutType.Button;
                    ShiftOutBind.Control = (X360Controls)setting.shiftAction.actionBtn;
                    break;
                case DS4ControlSettings.ActionType.Default:
                    ShiftOutBind.OutputType = OutBinding.OutType.Default;
                    break;
                case DS4ControlSettings.ActionType.Key:
                    ShiftOutBind.OutputType = OutBinding.OutType.Key;
                    ShiftOutBind.OutKey = setting.shiftAction.actionKey;
                    ShiftOutBind.HasScanCode = sc;
                    ShiftOutBind.Toggle = toggle;
                    break;
                case DS4ControlSettings.ActionType.Macro:
                    ShiftOutBind.OutputType = OutBinding.OutType.Macro;
                    ShiftOutBind.macro = (int[])setting.shiftAction.actionMacro;
                    ShiftOutBind.MacroType = setting.shiftKeyType;
                    ShiftOutBind.HasScanCode = sc;
                    break;
            }
        }

        if (!string.IsNullOrEmpty(setting.shiftExtras))
        {
            ShiftOutBind.ParseExtras(setting.shiftExtras);
        }
    }

    public void WriteBinds()
    {
        CurrentOutBind.WriteBind(Settings);
        ShiftOutBind.WriteBind(Settings);
    }

    public void StartForcedColor(Color color)
    {
        if (DeviceNum < IControlService.CURRENT_DS4_CONTROLLER_LIMIT)
        {
            DS4Color dcolor = new DS4Color() { Red = color.R, Green = color.G, Blue = color.B };
            DS4LightBar.forcedColor[DeviceNum] = dcolor;
            DS4LightBar.forcedFlash[DeviceNum] = 0;
            DS4LightBar.forcelight[DeviceNum] = true;
        }
    }

    public void EndForcedColor()
    {
        if (DeviceNum < IControlService.CURRENT_DS4_CONTROLLER_LIMIT)
        {
            DS4LightBar.forcedColor[DeviceNum] = new DS4Color(0, 0, 0);
            DS4LightBar.forcedFlash[DeviceNum] = 0;
            DS4LightBar.forcelight[DeviceNum] = false;
        }
    }

    public void UpdateForcedColor(Color color)
    {
        if (DeviceNum < IControlService.CURRENT_DS4_CONTROLLER_LIMIT)
        {
            DS4Color dcolor = new DS4Color() { Red = color.R, Green = color.G, Blue = color.B };
            DS4LightBar.forcedColor[DeviceNum] = dcolor;
            DS4LightBar.forcedFlash[DeviceNum] = 0;
            DS4LightBar.forcelight[DeviceNum] = true;
        }
    }
}

public class BindAssociation
{
    public enum OutType : uint
    {
        Default,
        Key,
        Button,
        Macro
    }

    public OutType outputType;
    public X360Controls control;
    public int outkey;

    public bool IsMouse()
    {
        return outputType == OutType.Button && (control >= X360Controls.LeftMouse && control < X360Controls.Unbound);
    }

    public static bool IsMouseRange(X360Controls control)
    {
        return control >= X360Controls.LeftMouse && control < X360Controls.Unbound;
    }
}

public class OutBinding
{
    public enum OutType : uint
    {
        Default,
        Key,
        Button,
        Macro
    }

    public DS4Controls input;
    public OutType OutputType;
    
    public int[] macro;
    public DS4KeyType MacroType;
    public X360Controls Control;
    
    private int heavyRumble = 0;
    private int lightRumble = 0;
    private int flashRate;
    private int mouseSens = 25;
    private DS4Color extrasColor = new (255,255,255);

    public int OutKey { get; set; }
    public bool ShiftBind { get; set; }
    public bool HasScanCode { get; set; }
    public bool Toggle { get; set; }
    public int ShiftTrigger { get; set; }

    public int HeavyRumble { get => heavyRumble; set => heavyRumble = value; }
    public int LightRumble { get => lightRumble; set => lightRumble = value; }
    public int FlashRate
    {
        get => flashRate;
        set
        {
            flashRate = value;
            FlashRateChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    public event EventHandler FlashRateChanged;

    public int MouseSens
    {
        get => mouseSens;
        set
        {
            mouseSens = value;
            MouseSensChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    public event EventHandler MouseSensChanged;

    private bool useMouseSens;
    public bool UseMouseSens
    {
        get => useMouseSens;
        set
        {
            useMouseSens = value;
            UseMouseSensChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    public event EventHandler UseMouseSensChanged;

    private bool useExtrasColor;
    public bool UseExtrasColor {
        get => useExtrasColor;
        set
        {
            useExtrasColor = value;
            UseExtrasColorChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    public event EventHandler UseExtrasColorChanged;

    public int ExtrasColorR
    {
        get => extrasColor.Red;
        set
        {
            extrasColor.Red = (byte)value;
            ExtrasColorRChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    public event EventHandler ExtrasColorRChanged;

    public string ExtrasColorRString
    {
        get
        {
            string temp = $"#{extrasColor.Red:X2}FF0000";
            return temp;
        }
    }
    public event EventHandler ExtrasColorRStringChanged;
    public int ExtrasColorG
    {
        get => extrasColor.Green;
        set
        {
            extrasColor.Green = (byte)value;
            ExtrasColorGChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    public event EventHandler ExtrasColorGChanged;

    public string ExtrasColorGString
    {
        get
        {
            var temp = $"#{extrasColor.Green:X2}00FF00";
            return temp;
        }
    }
    public event EventHandler ExtrasColorGStringChanged;

    public int ExtrasColorB
    {
        get => extrasColor.Blue;
        set
        {
            extrasColor.Blue = (byte)value;
            ExtrasColorBChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    public event EventHandler ExtrasColorBChanged;

    public string ExtrasColorBString
    {
        get
        {
            string temp = $"#{extrasColor.Blue:X2}0000FF";
            return temp;
        }
    }
    public event EventHandler ExtrasColorBStringChanged;

    public string ExtrasColorString => $"#FF{extrasColor.Red:X2}{extrasColor.Green:X2}{extrasColor.Blue:X2}";
    public event EventHandler ExtrasColorStringChanged;

    public Color ExtrasColorMedia =>
        new Color()
        {
            A = 255,
            R = extrasColor.Red,
            B = extrasColor.Blue,
            G = extrasColor.Green
        };

    private int shiftTriggerIndex;
    public int ShiftTriggerIndex { get => shiftTriggerIndex; set => shiftTriggerIndex = value; }

    public string DefaultColor
    {
        get
        {
            string color = string.Empty;
            if (OutputType == OutType.Default)
            {
                color =  Colors.LimeGreen.ToString();
            }
            else
            {
                color = Application.Current.FindResource("SecondaryColor").ToString();
                //color = SystemColors.ControlBrush.Color.ToString();
            }

            return color;
        }
    }

    public string UnboundColor
    {
        get
        {
            string color = string.Empty;
            if (OutputType == OutType.Button && Control == X360Controls.Unbound)
            {
                color = Colors.LimeGreen.ToString();
            }
            else
            {
                color = Application.Current.FindResource("SecondaryColor").ToString();
                //color = SystemColors.ControlBrush.Color.ToString();
            }

            return color;
        }
    }

    public string DefaultBtnString
    {
        get
        {
            string result = "Default";
            if (ShiftBind)
            {
                result = Localization.FallBack;
            }

            return result;
        }
    }

    public Visibility MacroLbVisible
    {
        get
        {
            return OutputType == OutType.Macro ? Visibility.Visible : Visibility.Hidden;
        }
    }

    public OutBinding()
    {
        ExtrasColorRChanged += OutBinding_ExtrasColorRChanged;
        ExtrasColorGChanged += OutBinding_ExtrasColorGChanged;
        ExtrasColorBChanged += OutBinding_ExtrasColorBChanged;
        UseExtrasColorChanged += OutBinding_UseExtrasColorChanged;
    }

    private void OutBinding_ExtrasColorBChanged(object sender, EventArgs e)
    {
        ExtrasColorStringChanged?.Invoke(this, EventArgs.Empty);
        ExtrasColorBStringChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OutBinding_ExtrasColorGChanged(object sender, EventArgs e)
    {
        ExtrasColorStringChanged?.Invoke(this, EventArgs.Empty);
        ExtrasColorGStringChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OutBinding_ExtrasColorRChanged(object sender, EventArgs e)
    {
        ExtrasColorStringChanged?.Invoke(this, EventArgs.Empty);
        ExtrasColorRStringChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OutBinding_UseExtrasColorChanged(object sender, EventArgs e)
    {
        if (!useExtrasColor)
        {
            ExtrasColorR = 255;
            ExtrasColorG = 255;
            ExtrasColorB = 255;
        }
    }

    public bool IsShift()
    {
        return ShiftBind;
    }

    public bool IsMouse()
    {
        return OutputType == OutType.Button && (Control >= X360Controls.LeftMouse && Control < X360Controls.Unbound);
    }

    public static bool IsMouseRange(X360Controls control)
    {
        return control >= X360Controls.LeftMouse && control < X360Controls.Unbound;
    }

    public void ParseExtras(string extras)
    {
        string[] temp = extras.Split(',');
        if (temp.Length == 9)
        {
            int.TryParse(temp[0], out heavyRumble);
            int.TryParse(temp[1], out lightRumble);
            int.TryParse(temp[2], out int useColor);
            if (useColor == 1)
            {
                useExtrasColor = true;
                byte.TryParse(temp[3], out extrasColor.Red);
                byte.TryParse(temp[4], out extrasColor.Green);
                byte.TryParse(temp[5], out extrasColor.Blue);
                int.TryParse(temp[6], out flashRate);
            }
            else
            {
                useExtrasColor = false;
                extrasColor.Red = extrasColor.Green = extrasColor.Blue = 255;
                flashRate = 0;
            }

            int.TryParse(temp[7], out int useM);
            if (useM == 1)
            {
                useMouseSens = true;
                int.TryParse(temp[8], out mouseSens);
            }
            else
            {
                useMouseSens = false;
                mouseSens = 25;
            }
        }
    }

    public string CompileExtras()
    {
        string result = $"{heavyRumble},{lightRumble},";
        if (useExtrasColor)
        {
            result += $"1,{extrasColor.Red},{extrasColor.Green},{extrasColor.Blue},{flashRate},";
        }
        else
        {
            result += "0,0,0,0,0,";
        }

        if (useMouseSens)
        {
            result += $"1,{mouseSens}";
        }
        else
        {
            result += "0,0";
        }

        return result;
    }

    public bool IsUsingExtras()
    {
        var result = false;
        result = result || (heavyRumble != 0);
        result = result || (lightRumble != 0);
        result = result || useExtrasColor;
        result = result ||
                 (extrasColor.Red != 255 && extrasColor.Green != 255 &&
                  extrasColor.Blue != 255);

        result = result || (flashRate != 0);
        result = result || useMouseSens;
        result = result || (mouseSens != 25);
        return result;
    }

    public void WriteBind(DS4ControlSettings settings)
    {
        if (!ShiftBind)
        {
            settings.keyType = DS4KeyType.None;

            if (OutputType == OutType.Default)
            {
                settings.action.actionKey = 0;
                settings.actionType = DS4ControlSettings.ActionType.Default;
            }
            else if (OutputType == OutType.Button)
            {
                settings.action.actionBtn = Control;
                settings.actionType = DS4ControlSettings.ActionType.Button;
                if (Control == X360Controls.Unbound)
                {
                    settings.keyType |= DS4KeyType.Unbound;
                }
            }
            else if (OutputType == OutType.Key)
            {
                settings.action.actionKey = OutKey;
                settings.actionType = DS4ControlSettings.ActionType.Key;
                if (HasScanCode)
                {
                    settings.keyType |= DS4KeyType.ScanCode;
                }

                if (Toggle)
                {
                    settings.keyType |= DS4KeyType.Toggle;
                }
            }
            else if (OutputType == OutType.Macro)
            {
                settings.action.actionMacro = macro;
                settings.actionType = DS4ControlSettings.ActionType.Macro;
                if (MacroType.HasFlag(DS4KeyType.HoldMacro))
                {
                    settings.keyType |= DS4KeyType.HoldMacro;
                }
                else
                {
                    settings.keyType |= DS4KeyType.Macro;
                }

                if (HasScanCode)
                {
                    settings.keyType |= DS4KeyType.ScanCode;
                }
            }

            settings.extras = IsUsingExtras() ? CompileExtras() : string.Empty;

            Global.RefreshActionAlias(settings, ShiftBind);
        }
        else
        {
            settings.shiftKeyType = DS4KeyType.None;
            settings.shiftTrigger = ShiftTrigger;

            if (OutputType == OutType.Default || ShiftTrigger == 0)
            {
                settings.shiftAction.actionKey = 0;
                settings.shiftActionType = DS4ControlSettings.ActionType.Default;
            }
            else if (OutputType == OutType.Button)
            {
                settings.shiftAction.actionBtn = Control;
                settings.shiftActionType = DS4ControlSettings.ActionType.Button;
                if (Control == X360Controls.Unbound)
                {
                    settings.shiftKeyType |= DS4KeyType.Unbound;
                }
            }
            else if (OutputType == OutType.Key)
            {
                settings.shiftAction.actionKey = OutKey;
                settings.shiftActionType = DS4ControlSettings.ActionType.Key;
                if (HasScanCode)
                {
                    settings.shiftKeyType |= DS4KeyType.ScanCode;
                }

                if (Toggle)
                {
                    settings.shiftKeyType |= DS4KeyType.Toggle;
                }
            }
            else if (OutputType == OutType.Macro)
            {
                settings.shiftAction.actionMacro = macro;
                settings.shiftActionType = DS4ControlSettings.ActionType.Macro;

                if (MacroType.HasFlag(DS4KeyType.HoldMacro))
                {
                    settings.shiftKeyType |= DS4KeyType.HoldMacro;
                }
                else
                {
                    settings.shiftKeyType |= DS4KeyType.Macro;
                }

                if (HasScanCode)
                {
                    settings.shiftKeyType |= DS4KeyType.ScanCode;
                }
            }

            settings.shiftExtras = IsUsingExtras() ? CompileExtras() : string.Empty;

            Global.RefreshActionAlias(settings, ShiftBind);
        }
    }

    public void UpdateExtrasColor(Color color)
    {
        ExtrasColorR = color.R;
        ExtrasColorG = color.G;
        ExtrasColorB = color.B;
    }
}