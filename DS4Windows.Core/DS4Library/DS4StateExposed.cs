namespace DS4Windows;

public class DS4StateExposed
{
    private readonly DS4State _state;

    public DS4StateExposed() => _state = new DS4State();

    public DS4StateExposed(DS4State state) => _state = state;

    bool Square => _state.Square;
    bool Triangle => _state.Triangle;
    bool Circle => _state.Circle;
    bool Cross => _state.Cross;
    bool DpadUp => _state.DpadUp;
    bool DpadDown => _state.DpadDown;
    bool DpadLeft => _state.DpadLeft;
    bool DpadRight => _state.DpadRight;
    bool L1 => _state.L1;
    bool L3 => _state.L3;
    bool R1 => _state.R1;
    bool R3 => _state.R3;
    bool Share => _state.Share;
    bool Options => _state.Options;
    bool PS => _state.PS;
    bool Touch1 => _state.Touch1;
    bool Touch2 => _state.Touch2;
    bool TouchButton => _state.TouchButton;
    bool Touch1Finger => _state.Touch1Finger;
    bool Touch2Fingers => _state.Touch2Fingers;
    byte LX => _state.LX;
    byte RX => _state.RX;
    byte LY => _state.LY;
    byte RY => _state.RY;
    byte L2 => _state.L2;
    byte R2 => _state.R2;
    int Battery => _state.Battery;

    public SixAxis Motion => _state.Motion;

    public int GyroYaw => _state.Motion.GyroYaw;

    public int GetGyroYaw()
    {
        return _state.Motion.GyroYaw;
    }

    public int GyroPitch => _state.Motion.GyroPitch;

    public int GetGyroPitch() => _state.Motion.GyroPitch;

    public int GyroRoll => _state.Motion.GyroRoll;

    public int GetGyroRoll() => _state.Motion.GyroRoll;

    public int AccelX => _state.Motion.AccelX;

    public int GetAccelX() => _state.Motion.AccelX;

    public int AccelY => _state.Motion.AccelY;

    public int GetAccelY() => _state.Motion.AccelY;

    public int AccelZ => _state.Motion.AccelZ;

    public int GetAccelZ() => _state.Motion.AccelZ;

    public int OutputAccelX => _state.Motion.OutputAccelX;

    public int GetOutputAccelX() => _state.Motion.OutputAccelX;

    public int OutputAccelY => _state.Motion.OutputAccelY;

    public int GetOutputAccelY() => _state.Motion.OutputAccelY;

    public int OutputAccelZ => _state.Motion.OutputAccelZ;
    
    public int GetOutputAccelZ() => _state.Motion.OutputAccelZ;
}