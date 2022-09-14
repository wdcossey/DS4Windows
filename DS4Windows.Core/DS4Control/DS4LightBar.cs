using System.Drawing;
using static System.Math;
using static DS4Windows.Global;
using System.Diagnostics;

namespace DS4Windows;

public class DS4LightBar
{
    private static readonly byte[/* Light On duration */, /* Light Off duration */] BatteryIndicatorDurations =
    {
        { 28, 252 }, // on 10% of the time at 0
        { 28, 252 },
        { 56, 224 },
        { 84, 196 },
        { 112, 168 },
        { 140, 140 },
        { 168, 112 },
        { 196, 84 },
        { 224, 56 }, // on 80% of the time at 80, etc.
        { 252, 28 }, // on 90% of the time at 90
        { 0, 0 }     // use on 100%. 0 is for "charging" OR anything sufficiently-"charged"
    };

    static double[] counters = new double[Global.MAX_DS4_CONTROLLER_COUNT] { 0, 0, 0, 0, 0, 0, 0, 0 };
    public static Stopwatch[] fadewatches = new Stopwatch[Global.MAX_DS4_CONTROLLER_COUNT]
    {
        new(), new(), new(), new(),
        new(), new(), new(), new(),
    };

    static bool[] fadedirection = new bool[Global.MAX_DS4_CONTROLLER_COUNT] { false, false, false, false, false, false, false, false };
    static DateTime[] oldnow = new DateTime[Global.MAX_DS4_CONTROLLER_COUNT]
    {
        DateTime.UtcNow, DateTime.UtcNow, DateTime.UtcNow, DateTime.UtcNow,
        DateTime.UtcNow, DateTime.UtcNow, DateTime.UtcNow, DateTime.UtcNow,
    };

    public static bool[] forcelight = new bool[Global.MAX_DS4_CONTROLLER_COUNT] { false, false, false, false, false, false, false, false };
    public static DS4Color[] forcedColor = new DS4Color[Global.MAX_DS4_CONTROLLER_COUNT];
    public static byte[] forcedFlash = new byte[Global.MAX_DS4_CONTROLLER_COUNT];
    internal const int PULSE_FLASH_DURATION = 2000;
    internal const double PULSE_FLASH_SEGMENTS = PULSE_FLASH_DURATION / 40;
    internal const int PULSE_CHARGING_DURATION = 4000;
    internal const double PULSE_CHARGING_SEGMENTS = (PULSE_CHARGING_DURATION / 40) - 2;

    public static void updateLightBar(DS4Device device, int deviceNum)
    {
        var color = new DS4Color();
        var useForceLight = forcelight[deviceNum];
        var lightbarSettingInfo = getLightbarSettingsInfo(deviceNum);
        var lightModeInfo = lightbarSettingInfo.ds4winSettings;
        var useLightRoutine = lightbarSettingInfo.mode == LightbarMode.DS4Win;
        //bool useLightRoutine = false;
        if (!defaultLight && !useForceLight && useLightRoutine)
        {
            if (lightModeInfo.useCustomLed)
            {
                if (lightModeInfo.ledAsBattery)
                {
                    ref var fullColor = ref lightModeInfo.m_CustomLed; // ref getCustomColor(deviceNum);
                    ref var lowColor = ref lightModeInfo.m_LowLed; //ref getLowColor(deviceNum);
                    color = getTransitionedColor(ref lowColor, ref fullColor, device.GetBattery());
                }
                else
                    color = lightModeInfo.m_CustomLed; //getCustomColor(deviceNum);
            }
            else
            {
                var rainbow = lightModeInfo.rainbow;// getRainbow(deviceNum);
                if (rainbow > 0)
                {
                    // Display rainbow
                    var now = DateTime.UtcNow;
                    if (now >= oldnow[deviceNum] + TimeSpan.FromMilliseconds(10)) //update by the millisecond that way it's a smooth transtion
                    {
                        oldnow[deviceNum] = now;
                        if (device.IsCharging())
                            counters[deviceNum] -= 1.5 * 3 / rainbow;
                        else
                            counters[deviceNum] += 1.5 * 3 / rainbow;
                    }

                    if (counters[deviceNum] < 0)
                        counters[deviceNum] = 180000;
                    else if (counters[deviceNum] > 180000)
                        counters[deviceNum] = 0;

                    var maxSat = lightModeInfo.maxRainbowSat; // GetMaxSatRainbow(deviceNum);
                    if (lightModeInfo.ledAsBattery)
                    {
                        var useSat = (byte)(maxSat == 1.0 ?
                            device.GetBattery() * 2.55 :
                            device.GetBattery() * 2.55 * maxSat);
                        color = HuetoRGB((float)counters[deviceNum] % 360, useSat);
                    }
                    else
                        color = HuetoRGB((float)counters[deviceNum] % 360,
                            (byte)(maxSat == 1.0 ? 255 : 255 * maxSat));

                }
                else if (lightModeInfo.ledAsBattery)
                {
                    ref var fullColor = ref lightModeInfo.m_Led; //ref getMainColor(deviceNum);
                    ref var lowColor = ref lightModeInfo.m_LowLed; //ref getLowColor(deviceNum);
                    color = getTransitionedColor(ref lowColor, ref fullColor, device.GetBattery());
                }
                else
                {
                    color = getMainColor(deviceNum);
                }
            }

            if (device.GetBattery() <= lightModeInfo.flashAt && !defaultLight && !device.IsCharging())
            {
                ref var flashColor = ref lightModeInfo.m_FlashLed; //ref getFlashColor(deviceNum);
                if (!(flashColor.Red == 0 &&
                      flashColor.Green == 0 &&
                      flashColor.Blue == 0))
                    color = flashColor;

                if (lightModeInfo.flashType == 1)
                {
                    var ratio = 0.0;

                    if (!fadewatches[deviceNum].IsRunning)
                    {
                        var temp = fadedirection[deviceNum];
                        fadedirection[deviceNum] = !temp;
                        fadewatches[deviceNum].Restart();
                        ratio = temp ? 100.0 : 0.0;
                    }
                    else
                    {
                        var elapsed = fadewatches[deviceNum].ElapsedMilliseconds;

                        if (fadedirection[deviceNum])
                        {
                            if (elapsed < PULSE_FLASH_DURATION)
                            {
                                elapsed = elapsed / 40;
                                ratio = 100.0 * (elapsed / PULSE_FLASH_SEGMENTS);
                            }
                            else
                            {
                                ratio = 100.0;
                                fadewatches[deviceNum].Stop();
                            }
                        }
                        else
                        {
                            if (elapsed < PULSE_FLASH_DURATION)
                            {
                                elapsed = elapsed / 40;
                                ratio = (0 - 100.0) * (elapsed / PULSE_FLASH_SEGMENTS) + 100.0;
                            }
                            else
                            {
                                ratio = 0.0;
                                fadewatches[deviceNum].Stop();
                            }
                        }
                    }

                    var tempCol = new DS4Color(0, 0, 0);
                    color = getTransitionedColor(ref color, ref tempCol, ratio);
                }
            }

            var idleDisconnectTimeout = getIdleDisconnectTimeout(deviceNum);
            if (idleDisconnectTimeout > 0 && lightModeInfo.ledAsBattery &&
                (!device.IsCharging() || device.GetBattery() >= 100))
            {
                // Fade lightbar by idle time
                var timeratio = new TimeSpan(DateTime.UtcNow.Ticks - device.lastActive.Ticks);
                var botratio = timeratio.TotalMilliseconds;
                var topratio = TimeSpan.FromSeconds(idleDisconnectTimeout).TotalMilliseconds;
                double ratio = 100.0 * (botratio / topratio), elapsed = ratio;
                if (ratio >= 50.0 && ratio < 100.0)
                {
                    var emptyCol = new DS4Color(0, 0, 0);
                    color = getTransitionedColor(ref color, ref emptyCol,
                        (uint)(-100.0 * (elapsed = 0.02 * (ratio - 50.0)) * (elapsed - 2.0)));
                }
                else if (ratio >= 100.0)
                {
                    var emptyCol = new DS4Color(0, 0, 0);
                    color = getTransitionedColor(ref color, ref emptyCol, 100.0);
                }
                        
            }

            if (device.IsCharging() && device.GetBattery() < 100)
            {
                switch (lightModeInfo.chargingType)
                {
                    case 1:
                    {
                        var ratio = 0.0;

                        if (!fadewatches[deviceNum].IsRunning)
                        {
                            var temp = fadedirection[deviceNum];
                            fadedirection[deviceNum] = !temp;
                            fadewatches[deviceNum].Restart();
                            ratio = temp ? 100.0 : 0.0;
                        }
                        else
                        {
                            var elapsed = fadewatches[deviceNum].ElapsedMilliseconds;

                            if (fadedirection[deviceNum])
                            {
                                if (elapsed < PULSE_CHARGING_DURATION)
                                {
                                    elapsed = elapsed / 40;
                                    if (elapsed > PULSE_CHARGING_SEGMENTS)
                                        elapsed = (long)PULSE_CHARGING_SEGMENTS;
                                    ratio = 100.0 * (elapsed / PULSE_CHARGING_SEGMENTS);
                                }
                                else
                                {
                                    ratio = 100.0;
                                    fadewatches[deviceNum].Stop();
                                }
                            }
                            else
                            {
                                if (elapsed < PULSE_CHARGING_DURATION)
                                {
                                    elapsed = elapsed / 40;
                                    if (elapsed > PULSE_CHARGING_SEGMENTS)
                                        elapsed = (long)PULSE_CHARGING_SEGMENTS;
                                    ratio = (0 - 100.0) * (elapsed / PULSE_CHARGING_SEGMENTS) + 100.0;
                                }
                                else
                                {
                                    ratio = 0.0;
                                    fadewatches[deviceNum].Stop();
                                }
                            }
                        }

                        var emptyCol = new DS4Color(0, 0, 0);
                        color = getTransitionedColor(ref color, ref emptyCol, ratio);
                        break;
                    }
                    case 2:
                    {
                        counters[deviceNum] += 0.167;
                        color = HuetoRGB((float)counters[deviceNum] % 360, 255);
                        break;
                    }
                    case 3:
                    {
                        color = lightModeInfo.m_ChargingLed; //getChargingColor(deviceNum);
                        break;
                    }
                    default: break;
                }
            }
        }
        else if (useForceLight)
        {
            color = forcedColor[deviceNum];
            useLightRoutine = true;
        }
        else if (shuttingdown)
        {
            color = new DS4Color(0, 0, 0);
            useLightRoutine = true;
        }
        else if (useLightRoutine)
        {
            if (device.GetConnectionType() == ConnectionType.BT)
                color = new DS4Color(32, 64, 64);
            else
                color = new DS4Color(0, 0, 0);
        }

        if (useLightRoutine)
        {
            var distanceprofile = DistanceProfiles[deviceNum] || tempprofileDistance[deviceNum];
            //distanceprofile = (ProfilePath[deviceNum].ToLower().Contains("distance") || tempprofilename[deviceNum].ToLower().Contains("distance"));
            if (distanceprofile && !defaultLight)
            {
                // Thing I did for Distance
                var rumble = device.GetLeftHeavySlowRumble() / 2.55f;
                var max = Max(color.Red, Max(color.Green, color.Blue));
                if (device.GetLeftHeavySlowRumble() > 100)
                {
                    var maxCol = new DS4Color(max, max, 0);
                    var redCol = new DS4Color(255, 0, 0);
                    color = getTransitionedColor(ref maxCol, ref redCol, rumble);
                }
                else
                {
                    var maxCol = new DS4Color(max, max, 0);
                    var redCol = new DS4Color(255, 0, 0);
                    var tempCol = getTransitionedColor(ref maxCol,
                        ref redCol, 39.6078f);
                    color = getTransitionedColor(ref color, ref tempCol,
                        device.GetLeftHeavySlowRumble());
                }
            }

            /*DS4HapticState haptics = new DS4HapticState
            {
                LightBarColor = color
            };
            */
            var lightState = new DS4LightbarState
            {
                LightBarColor = color,
            };

            if (lightState.IsLightBarSet())
            {
                if (useForceLight && forcedFlash[deviceNum] > 0)
                {
                    lightState.LightBarFlashDurationOff = lightState.LightBarFlashDurationOn = (byte)(25 - forcedFlash[deviceNum]);
                    lightState.LightBarExplicitlyOff = true;
                }
                else if (device.GetBattery() <= lightModeInfo.flashAt && lightModeInfo.flashType == 0 && !defaultLight && !device.IsCharging())
                {
                    var level = device.GetBattery() / 10;
                    if (level >= 10)
                        level = 10; // all values of >~100% are rendered the same

                    lightState.LightBarFlashDurationOn = BatteryIndicatorDurations[level, 0];
                    lightState.LightBarFlashDurationOff = BatteryIndicatorDurations[level, 1];
                }
                else if (distanceprofile && device.GetLeftHeavySlowRumble() > 155) //also part of Distance
                {
                    lightState.LightBarFlashDurationOff = lightState.LightBarFlashDurationOn = (byte)((-device.GetLeftHeavySlowRumble() + 265));
                    lightState.LightBarExplicitlyOff = true;
                }
                else
                {
                    //haptics.LightBarFlashDurationOff = haptics.LightBarFlashDurationOn = 1;
                    lightState.LightBarFlashDurationOff = lightState.LightBarFlashDurationOn = 0;
                    lightState.LightBarExplicitlyOff = true;
                }
            }
            else
            {
                lightState.LightBarExplicitlyOff = true;
            }

            var tempLightBarOnDuration = device.getLightBarOnDuration();
            if (tempLightBarOnDuration != lightState.LightBarFlashDurationOn && tempLightBarOnDuration != 1 && lightState.LightBarFlashDurationOn == 0)
                lightState.LightBarFlashDurationOff = lightState.LightBarFlashDurationOn = 1;

            device.SetLightbarState(ref lightState);
            //device.SetHapticState(ref haptics);
            //device.pushHapticState(ref haptics);
        }
    }

    public static bool defaultLight = false, shuttingdown = false;
      
    public static DS4Color HuetoRGB(float hue, byte sat)
    {
        var c = sat;
        var x = (int)((c * (float)(1 - Math.Abs((hue / 60) % 2 - 1))));
        return hue switch
        {
            >= 0 and < 60 => new DS4Color(c, (byte)x, 0),
            >= 60 and < 120 => new DS4Color((byte)x, c, 0),
            >= 120 and < 180 => new DS4Color(0, c, (byte)x),
            >= 180 and < 240 => new DS4Color(0, (byte)x, c),
            >= 240 and < 300 => new DS4Color((byte)x, 0, c),
            >= 300 and < 360 => new DS4Color(c, 0, (byte)x),
            _ => new DS4Color(Color.Red)
        };
    }
}