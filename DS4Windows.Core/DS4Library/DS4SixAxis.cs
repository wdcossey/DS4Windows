using System.Diagnostics;

namespace DS4Windows;


public class DS4SixAxis
{
    //public event EventHandler<SixAxisEventArgs> SixAccelMoved = null;
    public event SixAxisHandler<SixAxisEventArgs>? SixAccelMoved = null;
    private SixAxis sPrev = null, now = null;
    private CalibrationData[] calibrationData = new CalibrationData[6] 
        { new (), new (), new (), new (), new (), new () };
    
    private bool _calibrationDone;

    // for continuous calibration (JoyShockLibrary)
    private const int NumGyroAverageWindows = 3;
    private int _gyroAverageWindowFrontIndex = 0;
    private const int GyroAverageWindowMs = 5000;
    private readonly GyroAverageWindow[] _gyroAverageWindow = new GyroAverageWindow[NumGyroAverageWindows];
    private int _gyroOffsetX = 0;
    private int _gyroOffsetY = 0;
    private int _gyroOffsetZ = 0;
    private double _gyroAccelMagnitude = 1.0f;
    private readonly Stopwatch _gyroAverageTimer = new Stopwatch();
    public long CntCalibrating
    {
        get
        {
            return _gyroAverageTimer.IsRunning ? _gyroAverageTimer.ElapsedMilliseconds : 0;
        }
    }

    public DS4SixAxis()
    {
        sPrev = new SixAxis(0, 0, 0, 0, 0, 0, 0.0);
        now = new SixAxis(0, 0, 0, 0, 0, 0, 0.0);
        StartContinuousCalibration();
    }

    int temInt = 0;
    public void setCalibrationData(ref byte[] calibData, bool useAltGyroCalib)
    {
        int pitchPlus, pitchMinus, yawPlus, yawMinus, rollPlus, rollMinus,
            accelXPlus, accelXMinus, accelYPlus, accelYMinus, accelZPlus, accelZMinus,
            gyroSpeedPlus, gyroSpeedMinus;

        calibrationData[0].Bias = (short)((ushort)(calibData[2] << 8) | calibData[1]);
        calibrationData[1].Bias = (short)((ushort)(calibData[4] << 8) | calibData[3]);
        calibrationData[2].Bias = (short)((ushort)(calibData[6] << 8) | calibData[5]);

        if (!useAltGyroCalib)
        {
            pitchPlus = temInt = (short)((ushort)(calibData[8] << 8) | calibData[7]);
            yawPlus = temInt = (short)((ushort)(calibData[10] << 8) | calibData[9]);
            rollPlus = temInt = (short)((ushort)(calibData[12] << 8) | calibData[11]);
            pitchMinus = temInt = (short)((ushort)(calibData[14] << 8) | calibData[13]);
            yawMinus = temInt = (short)((ushort)(calibData[16] << 8) | calibData[15]);
            rollMinus = temInt = (short)((ushort)(calibData[18] << 8) | calibData[17]);
        }
        else
        {
            pitchPlus = temInt = (short)((ushort)(calibData[8] << 8) | calibData[7]);
            pitchMinus = temInt = (short)((ushort)(calibData[10] << 8) | calibData[9]);
            yawPlus = temInt = (short)((ushort)(calibData[12] << 8) | calibData[11]);
            yawMinus = temInt = (short)((ushort)(calibData[14] << 8) | calibData[13]);
            rollPlus = temInt = (short)((ushort)(calibData[16] << 8) | calibData[15]);
            rollMinus = temInt = (short)((ushort)(calibData[18] << 8) | calibData[17]);
        }

        gyroSpeedPlus = temInt = (short)((ushort)(calibData[20] << 8) | calibData[19]);
        gyroSpeedMinus = temInt = (short)((ushort)(calibData[22] << 8) | calibData[21]);
        accelXPlus = temInt = (short)((ushort)(calibData[24] << 8) | calibData[23]);
        accelXMinus = temInt = (short)((ushort)(calibData[26] << 8) | calibData[25]);

        accelYPlus = temInt = (short)((ushort)(calibData[28] << 8) | calibData[27]);
        accelYMinus = temInt = (short)((ushort)(calibData[30] << 8) | calibData[29]);

        accelZPlus = temInt = (short)((ushort)(calibData[32] << 8) | calibData[31]);
        accelZMinus = temInt = (short)((ushort)(calibData[34] << 8) | calibData[33]);

        var gyroSpeed2x = temInt = (gyroSpeedPlus + gyroSpeedMinus);
        calibrationData[0].SensNumerator = gyroSpeed2x* SixAxis.GYRO_RES_IN_DEG_SEC;
        calibrationData[0].SensDenominator = pitchPlus - pitchMinus;

        calibrationData[1].SensNumerator = gyroSpeed2x* SixAxis.GYRO_RES_IN_DEG_SEC;
        calibrationData[1].SensDenominator = yawPlus - yawMinus;

        calibrationData[2].SensNumerator = gyroSpeed2x* SixAxis.GYRO_RES_IN_DEG_SEC;
        calibrationData[2].SensDenominator = rollPlus - rollMinus;

        var accelRange = temInt = accelXPlus - accelXMinus;
        calibrationData[3].Bias = accelXPlus - accelRange / 2;
        calibrationData[3].SensNumerator = 2 * SixAxis.ACC_RES_PER_G;
        calibrationData[3].SensDenominator = accelRange;

        accelRange = temInt = accelYPlus - accelYMinus;
        calibrationData[4].Bias = accelYPlus - accelRange / 2;
        calibrationData[4].SensNumerator = 2 * SixAxis.ACC_RES_PER_G;
        calibrationData[4].SensDenominator = accelRange;

        accelRange = temInt = accelZPlus - accelZMinus;
        calibrationData[5].Bias = accelZPlus - accelRange / 2;
        calibrationData[5].SensNumerator = 2 * SixAxis.ACC_RES_PER_G;
        calibrationData[5].SensDenominator = accelRange;

        // Check that denom will not be zero.
        _calibrationDone = calibrationData[0].SensDenominator != 0 &&
                          calibrationData[1].SensDenominator != 0 &&
                          calibrationData[2].SensDenominator != 0 &&
                          accelRange != 0;
    }

    private void applyCalibs(ref int yaw, ref int pitch, ref int roll,
        ref int accelX, ref int accelY, ref int accelZ)
    {
        var current = calibrationData[0];
        temInt = pitch - current.Bias;
        pitch = temInt = (int)(temInt * (current.SensNumerator / (float)current.SensDenominator));

        current = calibrationData[1];
        temInt = yaw - current.Bias;
        yaw = temInt = (int)(temInt * (current.SensNumerator / (float)current.SensDenominator));

        current = calibrationData[2];
        temInt = roll - current.Bias;
        roll = temInt = (int)(temInt * (current.SensNumerator / (float)current.SensDenominator));

        current = calibrationData[3];
        temInt = accelX - current.Bias;
        accelX = temInt = (int)(temInt * (current.SensNumerator / (float)current.SensDenominator));

        current = calibrationData[4];
        temInt = accelY - current.Bias;
        accelY = temInt = (int)(temInt * (current.SensNumerator / (float)current.SensDenominator));

        current = calibrationData[5];
        temInt = accelZ - current.Bias;
        accelZ = temInt = (int)(temInt * (current.SensNumerator / (float)current.SensDenominator));
    }

    public unsafe void handleSixaxis(byte* gyro, byte* accel, DS4State state,
        double elapsedDelta)
    {
        unchecked
        {
            int currentYaw = (short)((ushort)(gyro[3] << 8) | gyro[2]);
            int currentPitch = (short)((ushort)(gyro[1] << 8) | gyro[0]);
            int currentRoll = (short)((ushort)(gyro[5] << 8) | gyro[4]);
            int AccelX = (short)((ushort)(accel[1] << 8) | accel[0]);
            int AccelY = (short)((ushort)(accel[3] << 8) | accel[2]);
            int AccelZ = (short)((ushort)(accel[5] << 8) | accel[4]);

            //Console.WriteLine("AccelZ: {0}", AccelZ);

            if (_calibrationDone)
                applyCalibs(ref currentYaw, ref currentPitch, ref currentRoll, ref AccelX, ref AccelY, ref AccelZ);

            if (_gyroAverageTimer.IsRunning)
            {
                CalcSensorCamples(ref currentYaw, ref currentPitch, ref currentRoll, ref AccelX, ref AccelY, ref AccelZ);
            }

            currentYaw -= _gyroOffsetX;
            currentPitch -= _gyroOffsetY;
            currentRoll -= _gyroOffsetZ;

            SixAxisEventArgs args = null;
            if (AccelX != 0 || AccelY != 0 || AccelZ != 0)
            {
                if (SixAccelMoved == null) 
                    return;
                
                sPrev.Copy(now);
                now.Populate(currentYaw, currentPitch, currentRoll,
                    AccelX, AccelY, AccelZ, elapsedDelta, sPrev);

                args = new SixAxisEventArgs(state.ReportTimeStamp, now);
                state.Motion = now;
                SixAccelMoved(this, args);
            }
        }
    }

    // Entry point to run continuous calibration for non-DS4 input devices
    public unsafe void PrepareNonDS4SixAxis(ref int currentYaw, ref int currentPitch, ref int currentRoll,
        ref int accelX, ref int accelY, ref int accelZ)
    {
        unchecked
        {
            if (_gyroAverageTimer.IsRunning)
            {
                CalcSensorCamples(ref currentYaw, ref currentPitch, ref currentRoll, ref accelX, ref accelY, ref accelZ);
            }

            currentYaw -= _gyroOffsetX;
            currentPitch -= _gyroOffsetY;
            currentRoll -= _gyroOffsetZ;
        }
    }

    private unsafe void CalcSensorCamples(ref int currentYaw, ref int currentPitch, ref int currentRoll, ref int accelX, ref int accelY, ref int accelZ)
    {
        unchecked
        {
            var accelMag = Math.Sqrt(accelX * accelX + accelY * accelY + accelZ * accelZ);
            PushSensorSamples(currentYaw, currentPitch, currentRoll, (float)accelMag);
            if (_gyroAverageTimer.ElapsedMilliseconds > 5000L)
            {
                _gyroAverageTimer.Stop();
                AverageGyro(ref _gyroOffsetX, ref _gyroOffsetY, ref _gyroOffsetZ, ref _gyroAccelMagnitude);
#if DEBUG
                Console.WriteLine("AverageGyro {0} {1} {2} {3}", _gyroOffsetX, _gyroOffsetY, _gyroOffsetZ, _gyroAccelMagnitude);
#endif
            }
        }
    }

    public bool FixupInvertedGyroAxis()
    {
        var result = false;
        // Some, not all, DS4 rev1 gamepads have an inverted YAW gyro axis calibration value (sensNumber>0 but at the same time sensDenom value is <0 while other two axies have both attributes >0).
        // If this gamepad has YAW calibration with weird mixed values then fix it automatically to workaround inverted YAW axis problem.
        if (calibrationData[1].SensNumerator > 0 && calibrationData[1].SensDenominator < 0 &&
            calibrationData[0].SensDenominator > 0 && calibrationData[2].SensDenominator > 0)
        {
            calibrationData[1].SensDenominator *= -1;
            result = true; // Fixed inverted axis
        }
        return result;
    }

    public void FireSixAxisEvent(SixAxisEventArgs args) => 
        SixAccelMoved?.Invoke(this, args);

    public void StartContinuousCalibration()
    {
        for (var i = 0; i < _gyroAverageWindow.Length; i++) _gyroAverageWindow[i] = new GyroAverageWindow();
        _gyroAverageTimer.Start();
    }

    public void StopContinuousCalibration()
    {
        _gyroAverageTimer.Stop();
        _gyroAverageTimer.Reset();
        foreach (var gyroAverageWindow in _gyroAverageWindow)
            gyroAverageWindow.Reset();
    }

    public void ResetContinuousCalibration()
    {
        // Potential race condition with CalcSensorCamples() since this method is called after checking gyroAverageTimer.IsRunning == true
        StopContinuousCalibration();
        StartContinuousCalibration();
    }

    public unsafe void PushSensorSamples(int x, int y, int z, double accelMagnitude)
    {
        // push samples
        var windowPointer = _gyroAverageWindow[_gyroAverageWindowFrontIndex];

        if (windowPointer.StopIfElapsed(GyroAverageWindowMs))
        {
            Console.WriteLine("GyroAvg[{0}], numSamples: {1}", _gyroAverageWindowFrontIndex,
                windowPointer.NumSamples);

            // next
            _gyroAverageWindowFrontIndex = (_gyroAverageWindowFrontIndex + NumGyroAverageWindows - 1) % NumGyroAverageWindows;
            windowPointer = _gyroAverageWindow[_gyroAverageWindowFrontIndex];
            windowPointer.Reset();
        }
        // accumulate
        windowPointer.NumSamples++;
        windowPointer.X += x;
        windowPointer.Y += y;
        windowPointer.Z += z;
        windowPointer.AccelMagnitude += accelMagnitude;
    }

    public void AverageGyro(ref int x, ref int y, ref int z, ref double accelMagnitude)
    {
        var weight = 0.0;
        var totalX = 0.0;
        var totalY = 0.0;
        var totalZ = 0.0;
        var totalAccelMagnitude = 0.0;

        var wantedMs = 5000;
        for (var i = 0; i < NumGyroAverageWindows && wantedMs > 0; i++)
        {
            var cycledIndex = (i + _gyroAverageWindowFrontIndex) % NumGyroAverageWindows;
            var windowPointer = _gyroAverageWindow[cycledIndex];
            if (windowPointer.NumSamples == 0 || windowPointer.DurationMs == 0) continue;

            double thisWeight;
            double fNumSamples = windowPointer.NumSamples;
            if (wantedMs < windowPointer.DurationMs)
            {
                thisWeight = (float)wantedMs / windowPointer.DurationMs;
                wantedMs = 0;
            }
            else
            {
                thisWeight = windowPointer.GetWeight(GyroAverageWindowMs);
                wantedMs -= windowPointer.DurationMs;
            }

            totalX += (windowPointer.X / fNumSamples) * thisWeight;
            totalY += (windowPointer.Y / fNumSamples) * thisWeight;
            totalZ += (windowPointer.Z / fNumSamples) * thisWeight;
            totalAccelMagnitude += (windowPointer.AccelMagnitude / fNumSamples) * thisWeight;
            weight += thisWeight;
        }

        if (weight > 0.0)
        {
            x = (int)(totalX / weight);
            y = (int)(totalY / weight);
            z = (int)(totalZ / weight);
            accelMagnitude = totalAccelMagnitude / weight;
        }
    }
}

public class SixAxis
{
    public const int ACC_RES_PER_G = 8192;
    public const float F_ACC_RES_PER_G = ACC_RES_PER_G;
    public const int GYRO_RES_IN_DEG_SEC = 16;
    public const float F_GYRO_RES_IN_DEG_SEC = GYRO_RES_IN_DEG_SEC;

    public int GyroYaw, GyroPitch, GyroRoll, AccelX, AccelY, AccelZ;
    public int OutputAccelX, OutputAccelY, OutputAccelZ;
    public bool OutputGyroControls;
    public double AccelXg, AccelYg, AccelZg;
    public double AngVelYaw, AngVelPitch, AngVelRoll;
    public int GyroYawFull, GyroPitchFull, GyroRollFull;
    public int AccelXFull, AccelYFull, AccelZFull;
    public double Elapsed;
    public SixAxis? PreviousAxis = null;

    private double tempDouble = 0d;

    public SixAxis(int x, int y, int z, int aX, int aY, int aZ, double elapsedDelta, SixAxis? prevAxis = null) =>
        Populate(x, y, z, aX, aY, aZ, elapsedDelta, prevAxis);

    public void Copy(SixAxis source)
    {
        GyroYaw = source.GyroYaw;
        GyroPitch = source.GyroPitch;
        GyroRoll = source.GyroRoll;

        GyroYawFull = source.GyroYawFull;
        AccelXFull = source.AccelXFull; AccelYFull = source.AccelYFull; AccelZFull = source.AccelZFull;

        AngVelYaw = source.AngVelYaw;
        AngVelPitch = source.AngVelPitch;
        AngVelRoll = source.AngVelRoll;

        AccelXg = source.AccelXg;
        AccelYg = source.AccelYg;
        AccelZg = source.AccelZg;

        // Put accel ranges between 0 - 128 abs
        AccelX = source.AccelX;
        AccelY = source.AccelY;
        AccelZ = source.AccelZ;
        OutputAccelX = AccelX;
        OutputAccelY = AccelY;
        OutputAccelZ = AccelZ;

        Elapsed = source.Elapsed;
        PreviousAxis = source.PreviousAxis;
        OutputGyroControls = source.OutputGyroControls;
    }

    public void Populate(int x, int y, int z, int aX, int aY, int aZ, double elapsedDelta, SixAxis? prevAxis = null)
    {
        GyroYaw = -x / 256;
        GyroPitch = y / 256;
        GyroRoll = -z / 256;

        GyroYawFull = -x; GyroPitchFull = y; GyroRollFull = -z;
        AccelXFull = -aX; AccelYFull = -aY; AccelZFull = aZ;

        AngVelYaw = GyroYawFull / F_GYRO_RES_IN_DEG_SEC;
        AngVelPitch = GyroPitchFull / F_GYRO_RES_IN_DEG_SEC;
        AngVelRoll = GyroRollFull / F_GYRO_RES_IN_DEG_SEC;

        AccelXg = tempDouble = AccelXFull / F_ACC_RES_PER_G;
        AccelYg = tempDouble = AccelYFull / F_ACC_RES_PER_G;
        AccelZg = tempDouble = AccelZFull / F_ACC_RES_PER_G;

        // Put accel ranges between 0 - 128 abs
        AccelX = -aX / 64;
        AccelY = -aY / 64;
        AccelZ = aZ / 64;

        // Leave blank and have mapping routine alter values as needed
        OutputAccelX = 0;
        OutputAccelY = 0;
        OutputAccelZ = 0;
        OutputGyroControls = false;

        Elapsed = elapsedDelta;
        PreviousAxis = prevAxis;
    }
}

internal class CalibrationData
{
    public int Bias;
    public int SensNumerator;
    public int SensDenominator;
    public const int GyroPitchIdx = 0, GyroYawIdx = 1, GyroRollIdx = 2, AccelXIdx = 3, AccelYIdx = 4, AccelZIdx = 5;
}

public class GyroAverageWindow
{
    public int X;
    public int Y;
    public int Z;
    public double AccelMagnitude;
    public int NumSamples;
    public DateTime Start;
    public DateTime Stop;

    public int DurationMs // property
    {
        get
        {
            var timeDiff = Stop - Start;
            return Convert.ToInt32(timeDiff.TotalMilliseconds);
        }
    }

    public GyroAverageWindow()
    {
        Reset();
    }

    public void Reset()
    {
        X = Y = Z = NumSamples = 0;
        AccelMagnitude = 0.0;
        Start = Stop = DateTime.UtcNow;
    }

    public bool StopIfElapsed(int ms)
    {
        var end = DateTime.UtcNow;
        var timeDiff = end - Start;
        var shouldStop = Convert.ToInt32(timeDiff.TotalMilliseconds) >= ms;
        if (!shouldStop) Stop = end;
        return shouldStop;
    }
    public double GetWeight(int expectedMs)
    {
        if (expectedMs == 0) return 0;
        return Math.Min(1.0, DurationMs / expectedMs);
    }
}

public class SixAxisEventArgs : EventArgs
{
    public SixAxis SixAxis { get; }
    public DateTime TimeStamp { get; }
    
    public SixAxisEventArgs(DateTime utcTimestamp, SixAxis sixAxis)
    {
        SixAxis = sixAxis;
        TimeStamp = utcTimestamp;
    }
}