namespace DS4Windows;

public struct DS4HapticState : IEquatable<DS4HapticState>
{
    public DS4LightbarState LightbarState;
    public DS4ForceFeedbackState RumbleState;

    public bool Equals(DS4HapticState other)
    {
        return LightbarState.Equals(other.LightbarState) &&
               RumbleState.Equals(other.RumbleState);
    }

    public bool IsLightBarSet() => LightbarState.IsLightBarSet();

    public bool IsRumbleSet()
    {
        const byte zero = 0;
        return RumbleState.RumbleMotorsExplicitlyOff || RumbleState.RumbleMotorStrengthLeftHeavySlow != zero || RumbleState.RumbleMotorStrengthRightLightFast != zero;
    }
}

/*
* The haptics engine uses a stack of these states representing the light bar and rumble motor settings.
* It (will) handle composing them and the details of output report management.
*/
public struct DS4ForceFeedbackState : IEquatable<DS4ForceFeedbackState>
{
    public byte RumbleMotorStrengthLeftHeavySlow, RumbleMotorStrengthRightLightFast;
    public bool RumbleMotorsExplicitlyOff;

    public bool Equals(DS4ForceFeedbackState other)
    {
        return RumbleMotorStrengthLeftHeavySlow == other.RumbleMotorStrengthLeftHeavySlow &&
               RumbleMotorStrengthRightLightFast == other.RumbleMotorStrengthRightLightFast &&
               RumbleMotorsExplicitlyOff == other.RumbleMotorsExplicitlyOff;
    }

    public bool IsRumbleSet()
    {
        const byte zero = 0;
        return RumbleMotorsExplicitlyOff || RumbleMotorStrengthLeftHeavySlow != zero || RumbleMotorStrengthRightLightFast != zero;
    }
}

public struct DS4LightbarState : IEquatable<DS4LightbarState>
{
    public DS4Color LightBarColor;
    public bool LightBarExplicitlyOff;
    public byte LightBarFlashDurationOn, LightBarFlashDurationOff;

    public bool Equals(DS4LightbarState other)
    {
        return LightBarColor.Equals(other.LightBarColor) &&
               LightBarExplicitlyOff == other.LightBarExplicitlyOff &&
               LightBarFlashDurationOn == other.LightBarFlashDurationOn &&
               LightBarFlashDurationOff == other.LightBarFlashDurationOff;
    }

    public bool IsLightBarSet()
    {
        return LightBarExplicitlyOff || LightBarColor.Red != 0 || LightBarColor.Green != 0 || LightBarColor.Blue != 0;
    }
}