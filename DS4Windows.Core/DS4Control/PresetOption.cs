namespace DS4WinWPF.DS4Control
{
    public abstract class PresetOption
    {
        protected readonly IControlService ControlService;

        public PresetOption(IControlService controlService, string name, string description, bool outputControllerChoice, OutputContChoice outputCont)
        {
            ControlService = controlService;
            Name = name;
            Description = description;
            OutputControllerChoice = outputControllerChoice;
            OutputCont = outputCont;
        }
        
        public enum OutputContChoice : ushort
        {
            None,
            Xbox360,
            DualShock4,
        }
        
        public string Name { get; protected set; }
        public string Description { get; protected set; }
        public bool OutputControllerChoice { get; protected set; }
        public OutputContChoice OutputCont { get; set;}

        public abstract void ApplyPreset(int idx);
    }

    public class GamepadPreset : PresetOption
    {
        public GamepadPreset(IControlService controlService, string name, string description, bool outputControllerChoice, OutputContChoice outputCont = OutputContChoice.Xbox360) 
            : base(controlService, name, description, outputControllerChoice, outputCont) { }

        public override void ApplyPreset(int idx)
        {
            if (OutputCont == OutputContChoice.Xbox360)
            {
                Global.LoadBlankDevProfile(idx, false, ControlService, false);
            }
            else if (OutputCont == OutputContChoice.DualShock4)
            {
                Global.LoadBlankDS4Profile(idx, false, ControlService, false);
            }
        }
    }

    public class GamepadGyroCamera : PresetOption
    {
        public GamepadGyroCamera(IControlService controlService, string name, string description, bool outputControllerChoice, OutputContChoice outputCont = OutputContChoice.Xbox360) 
            : base(controlService, name, description, outputControllerChoice, outputCont) { }

        public override void ApplyPreset(int idx)
        {
            if (OutputCont == OutputContChoice.Xbox360)
            {
                Global.LoadDefaultGamepadGyroProfile(idx, false, ControlService, false);
            }
            else if (OutputCont == OutputContChoice.DualShock4)
            {
                Global.LoadDefaultDS4GamepadGyroProfile(idx, false, ControlService, false);
            }
        }
    }

    public class MixedPreset : PresetOption
    {
        public MixedPreset(IControlService controlService, string name, string description, bool outputControllerChoice, OutputContChoice outputCont = OutputContChoice.Xbox360) 
            : base(controlService, name, description, outputControllerChoice, outputCont) { }

        public override void ApplyPreset(int idx)
        {
            if (OutputCont == OutputContChoice.Xbox360)
            {
                Global.LoadDefaultMixedControlsProfile(idx, false, ControlService, false);
            }
            else if (OutputCont == OutputContChoice.DualShock4)
            {
                Global.LoadDefaultMixedControlsProfile(idx, false, ControlService, false);
            }
        }
    }

    public class MixedGyroMousePreset : PresetOption
    {
        public MixedGyroMousePreset(IControlService controlService, string name, string description, bool outputControllerChoice, OutputContChoice outputCont = OutputContChoice.Xbox360) 
            : base(controlService, name, description, outputControllerChoice, outputCont) { }

        public override void ApplyPreset(int idx)
        {
            if (OutputCont == OutputContChoice.Xbox360)
            {
                Global.LoadDefaultMixedGyroMouseProfile(idx, false, ControlService, false);
            }
            else if (OutputCont == OutputContChoice.DualShock4)
            {
                Global.LoadDefaultDS4MixedGyroMouseProfile(idx, false, ControlService, false);
            }
        }
    }

    public class KBMPreset : PresetOption
    {
        public KBMPreset(IControlService controlService, string name, string description, bool outputControllerChoice = false, OutputContChoice outputCont = OutputContChoice.None) 
            : base(controlService, name, description, outputControllerChoice, outputCont) { }

        public override void ApplyPreset(int idx)
        {
            Global.LoadDefaultKBMProfile(idx, false, ControlService, false);
        }
    }

    public class KBMGyroMouse : PresetOption
    {
        public KBMGyroMouse(IControlService controlService, string name, string description, bool outputControllerChoice = false, OutputContChoice outputCont = OutputContChoice.None) 
            : base(controlService, name, description, outputControllerChoice, outputCont) { }

        public override void ApplyPreset(int idx)
        {
            Global.LoadDefaultKBMGyroMouseProfile(idx, false, ControlService, false);
        }
    }
}
