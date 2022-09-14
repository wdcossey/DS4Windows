using System.Runtime.InteropServices;
using System.Security;

namespace DS4Windows.DS4Control
{
    [SuppressUnmanagedCodeSecurity]
    public class SendInputHandler : VirtualKBMBase
    {
        public const string DISPLAY_NAME = "SendInput";
        public const string IDENTIFIER = "sendinput";
        private const double ABSOLUTE_MOUSE_COOR_MAX = 65535.0;

        public SendInputHandler()
        {
            fakeKeyRepeat = true;
        }

        public override bool Connect()
        {
            return true;
        }

        public override bool Disconnect()
        {
            return true;
        }

        public override void MoveRelativeMouse(int x, int y)
        {
            if (x != 0 || y != 0)
            {
                User32.INPUT[] tempInput = new User32.INPUT[1];
                ref User32.INPUT temp = ref tempInput[0];
                temp.Type = User32.INPUT_MOUSE;
                temp.Data.Mouse.ExtraInfo = IntPtr.Zero;
                temp.Data.Mouse.Flags = User32.MOUSEEVENTF_MOVE;
                temp.Data.Mouse.MouseData = 0;
                temp.Data.Mouse.Time = 0;
                temp.Data.Mouse.X = x;
                temp.Data.Mouse.Y = y;
                uint result = User32.SendInput(1, tempInput, Marshal.SizeOf(tempInput[0]));
            }
        }

        /// <summary>
        /// Move the mouse cursor to an absolute position on the virtual desktop
        /// </summary>
        /// <param name="x">X coordinate in range of [0.0, 1.0]. 0.0 for left. 1.0 for far right</param>
        /// <param name="y">Y coordinate in range of [0.0, 1.0]. 0.0 for top. 1.0 for bottom</param>
        public override void MoveAbsoluteMouse(double x, double y)
        {
            User32.INPUT[] tempInput = new User32.INPUT[1];
            ref User32.INPUT temp = ref tempInput[0];
            temp.Type = User32.INPUT_MOUSE;
            temp.Data.Mouse.ExtraInfo = IntPtr.Zero;
            temp.Data.Mouse.Flags = User32.MOUSEEVENTF_MOVE | User32.MOUSEEVENTF_VIRTUALDESK | User32.MOUSEEVENTF_ABSOLUTE;
            temp.Data.Mouse.MouseData = 0;
            temp.Data.Mouse.Time = 0;
            temp.Data.Mouse.X = (int)(x * ABSOLUTE_MOUSE_COOR_MAX);
            temp.Data.Mouse.Y = (int)(y * ABSOLUTE_MOUSE_COOR_MAX);
            uint result = User32.SendInput(1, tempInput, Marshal.SizeOf(tempInput[0]));
        }

        public override void PerformMouseButtonEvent(uint mouseButton)
        {
            User32.INPUT[] tempInput = new User32.INPUT[1];
            ref User32.INPUT temp = ref tempInput[0];
            temp.Type = User32.INPUT_MOUSE;
            temp.Data.Mouse.ExtraInfo = IntPtr.Zero;
            temp.Data.Mouse.Flags = mouseButton;
            temp.Data.Mouse.MouseData = 0;
            temp.Data.Mouse.Time = 0;
            temp.Data.Mouse.X = 0;
            temp.Data.Mouse.Y = 0;
            uint result = User32.SendInput(1, tempInput, Marshal.SizeOf(tempInput[0]));
        }

        public override void PerformMouseWheelEvent(int vertical, int horizontal)
        {
            User32.INPUT[] tempInput = new User32.INPUT[2];
            uint inputs = 0;
            ref User32.INPUT temp = ref tempInput[inputs];
            if (vertical != 0)
            {
                temp.Type = User32.INPUT_MOUSE;
                temp.Data.Mouse.ExtraInfo = IntPtr.Zero;
                temp.Data.Mouse.Flags = User32.MOUSEEVENTF_WHEEL;
                temp.Data.Mouse.MouseData = (uint)vertical;
                temp.Data.Mouse.Time = 0;
                temp.Data.Mouse.X = 0;
                temp.Data.Mouse.Y = 0;
                inputs++;
            }

            if (horizontal != 0)
            {
                temp = ref tempInput[inputs];
                temp.Type = User32.INPUT_MOUSE;
                temp.Data.Mouse.ExtraInfo = IntPtr.Zero;
                temp.Data.Mouse.Flags = User32.MOUSEEVENTF_HWHEEL;
                temp.Data.Mouse.MouseData = (uint)horizontal;
                temp.Data.Mouse.Time = 0;
                temp.Data.Mouse.X = 0;
                temp.Data.Mouse.Y = 0;
                inputs++;
            }

            User32.SendInput(inputs, tempInput, (int)inputs * Marshal.SizeOf(tempInput[0]));
        }

        public override void PerformMouseButtonEventAlt(uint mouseButton, int type)
        {
            User32.INPUT[] tempInput = new User32.INPUT[1];
            ref User32.INPUT temp = ref tempInput[0];
            temp.Type = User32.INPUT_MOUSE;
            temp.Data.Mouse.ExtraInfo = IntPtr.Zero;
            temp.Data.Mouse.Flags = mouseButton;
            temp.Data.Mouse.MouseData = (uint)type;
            temp.Data.Mouse.Time = 0;
            temp.Data.Mouse.X = 0;
            temp.Data.Mouse.Y = 0;
            uint result = User32.SendInput(1, tempInput, Marshal.SizeOf(tempInput[0]));
        }

        public override void PerformMouseButtonPress(uint mouseButton)
        {
            User32.INPUT[] tempInput = new User32.INPUT[1];
            ref User32.INPUT temp = ref tempInput[0];
            temp.Type = User32.INPUT_MOUSE;
            temp.Data.Mouse.ExtraInfo = IntPtr.Zero;
            temp.Data.Mouse.Flags = mouseButton;
            temp.Data.Mouse.MouseData = 0;
            temp.Data.Mouse.Time = 0;
            temp.Data.Mouse.X = 0;
            temp.Data.Mouse.Y = 0;
            uint result = User32.SendInput(1, tempInput, Marshal.SizeOf(tempInput[0]));
        }

        public override void PerformMouseButtonRelease(uint mouseButton)
        {
            User32.INPUT[] tempInput = new User32.INPUT[1];
            ref User32.INPUT temp = ref tempInput[0];
            temp.Type = User32.INPUT_MOUSE;
            temp.Data.Mouse.ExtraInfo = IntPtr.Zero;
            temp.Data.Mouse.Flags = mouseButton;
            temp.Data.Mouse.MouseData = 0;
            temp.Data.Mouse.Time = 0;
            temp.Data.Mouse.X = 0;
            temp.Data.Mouse.Y = 0;
            uint result = User32.SendInput(1, tempInput, Marshal.SizeOf(tempInput[0]));
        }

        public override void PerformKeyPress(uint key)
        {
            User32.INPUT[] tempInput = new User32.INPUT[1];
            ref User32.INPUT temp = ref tempInput[0];
            ushort scancode = scancodeFromVK(key);
            bool extended = (scancode & 0x100) != 0;
            uint curflags = extended ? User32.KEYEVENTF_EXTENDEDKEY : 0;

            temp.Type = User32.INPUT_KEYBOARD;
            temp.Data.Keyboard.ExtraInfo = IntPtr.Zero;
            temp.Data.Keyboard.Flags = curflags;
            temp.Data.Keyboard.Scan = scancode;
            //sendInputs[0].Data.Keyboard.Flags = 1;
            //sendInputs[0].Data.Keyboard.Scan = 0;
            temp.Data.Keyboard.Time = 0;
            temp.Data.Keyboard.Vk = (ushort)key;
            uint result = User32.SendInput(1, tempInput, Marshal.SizeOf(tempInput[0]));
        }

        /// <summary>
        /// Used to send scancode key press events
        /// </summary>
        /// <param name="key">Windows VK value to convert</param>
        public override void PerformKeyPressAlt(uint key)
        {
            User32.INPUT[] tempInput = new User32.INPUT[1];
            ref User32.INPUT temp = ref tempInput[0];
            ushort scancode = scancodeFromVK(key);
            bool extended = (scancode & 0x100) != 0;
            uint curflags = extended ? User32.KEYEVENTF_EXTENDEDKEY : 0;

            temp.Type = User32.INPUT_KEYBOARD;
            temp.Data.Keyboard.ExtraInfo = IntPtr.Zero;
            temp.Data.Keyboard.Flags = User32.KEYEVENTF_SCANCODE | curflags;
            temp.Data.Keyboard.Scan = scancode;
            temp.Data.Keyboard.Time = 0;
            //temp.Data.Keyboard.Vk = (ushort)key;
            uint result = User32.SendInput(1, tempInput, Marshal.SizeOf(tempInput[0]));
        }

        public override void PerformKeyRelease(uint key)
        {
            User32.INPUT[] tempInput = new User32.INPUT[1];
            ref User32.INPUT temp = ref tempInput[0];
            ushort scancode = scancodeFromVK(key);
            bool extended = (scancode & 0x100) != 0;
            uint curflags = extended ? User32.KEYEVENTF_EXTENDEDKEY : 0;

            temp.Type = User32.INPUT_KEYBOARD;
            temp.Data.Keyboard.ExtraInfo = IntPtr.Zero;
            temp.Data.Keyboard.Flags = curflags | User32.KEYEVENTF_KEYUP;
            temp.Data.Keyboard.Scan = scancode;
            //sendInputs[0].Data.Keyboard.Flags = KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP;
            //sendInputs[0].Data.Keyboard.Scan = 0;
            temp.Data.Keyboard.Time = 0;
            temp.Data.Keyboard.Vk = (ushort)key;
            uint result = User32.SendInput(1, tempInput, Marshal.SizeOf(tempInput[0]));
        }

        /// <summary>
        /// Used to send scancode key release events
        /// </summary>
        /// <param name="key">Windows VK value to convert</param>
        public override void PerformKeyReleaseAlt(uint key)
        {
            User32.INPUT[] tempInput = new User32.INPUT[1];
            ref User32.INPUT temp = ref tempInput[0];
            ushort scancode = scancodeFromVK(key);
            bool extended = (scancode & 0x100) != 0;
            uint curflags = extended ? User32.KEYEVENTF_EXTENDEDKEY : 0;

            temp.Type = User32.INPUT_KEYBOARD;
            temp.Data.Keyboard.ExtraInfo = IntPtr.Zero;
            temp.Data.Keyboard.Flags = User32.KEYEVENTF_SCANCODE | User32.KEYEVENTF_KEYUP | curflags;
            temp.Data.Keyboard.Scan = scancode;
            temp.Data.Keyboard.Time = 0;
            //sendInputs[0].Data.Keyboard.Vk = MapVirtualKey(key, MAPVK_VK_TO_VSC);
            uint result = User32.SendInput(1, tempInput, Marshal.SizeOf(tempInput[0]));
        }

        public override string GetDisplayName()
        {
            return DISPLAY_NAME;
        }

        public override string GetFullDisplayName()
        {
            return DISPLAY_NAME;
        }

        public override string GetIdentifier()
        {
            return IDENTIFIER;
        }

        private static ushort scancodeFromVK(uint vkey)
        {
            ushort scancode = 0;
            if (vkey == User32.VK_PAUSE)
            {
                // MapVirtualKey does not work with VK_PAUSE
                scancode = 0x45;
            }
            else
            {
                scancode = User32.MapVirtualKey(vkey, User32.MAPVK_VK_TO_VSC);
            }

            switch (vkey)
            {
                case User32.VK_LEFT:
                case User32.VK_UP:
                case User32.VK_RIGHT:
                case User32.VK_DOWN:
                case User32.VK_PRIOR:
                case User32.VK_NEXT:
                case User32.VK_END:
                case User32.VK_HOME:
                case User32.VK_INSERT:
                case User32.VK_DELETE:
                case User32.VK_DIVIDE:
                case User32.VK_NUMLOCK:
                case User32.VK_RCONTROL:
                case User32.VK_RMENU:
                case User32.VK_VOLUME_MUTE:
                case User32.VK_VOLUME_DOWN:
                case User32.VK_VOLUME_UP:
                case User32.VK_MEDIA_NEXT_TRACK:
                case User32.VK_MEDIA_PREV_TRACK:
                case User32.VK_LAUNCH_MEDIA_SELECT:
                case User32.VK_BROWSER_HOME:
                case User32.VK_LAUNCH_MAIL:
                case User32.VK_LAUNCH_APP1:
                case User32.VK_LAUNCH_APP2:
                case User32.VK_APPS:
                    {
                        scancode |= (ushort)User32.EXTENDED_FLAG; // set extended bit
                        break;
                    }
            }

            return scancode;
        }
    }
}
