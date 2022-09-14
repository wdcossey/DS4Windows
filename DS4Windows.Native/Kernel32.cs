namespace DS4Windows.Native;

[SuppressUnmanagedCodeSecurity]
internal static class Kernel32
{
    [DllImport("kernel32.dll")]
    internal static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern Boolean DeviceIoControl(IntPtr DeviceHandle, UInt32 IoControlCode, ref long InBuffer, Int32 InBufferSize, IntPtr OutBuffer, Int32 OutBufferSize, ref Int32 BytesReturned, IntPtr Overlapped);

    [DllImport("kernel32.dll", SetLastError = true, EntryPoint = "DeviceIoControl")]
    internal static extern Boolean DeviceIoControl(IntPtr DeviceHandle, UInt32 IoControlCode, IntPtr InBuffer, Int32 InBufferSize, IntPtr OutBuffer, Int32 OutBufferSize, ref Int32 BytesReturned, IntPtr Overlapped);

    [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true, CharSet = CharSet.Auto)]
    internal static extern bool CloseHandle(IntPtr hObject);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true, EntryPoint = "QueryDosDeviceW")]
    internal static extern uint QueryDosDevice(string lpDeviceName, StringBuilder lpTargetPath, int ucchMax);

    [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true, CharSet = CharSet.Auto)]
    internal static extern bool CancelIo(IntPtr hFile);

    [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true, CharSet = CharSet.Auto)]
    internal static extern bool CancelIoEx(IntPtr hFile, IntPtr lpOverlapped);

    [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true, CharSet = CharSet.Auto)]
    internal static extern bool CancelSynchronousIo(IntPtr hObject);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
    internal static extern IntPtr CreateEvent(ref SECURITY_ATTRIBUTES securityAttributes, int bManualReset, int bInitialState, string lpName);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    internal static extern IntPtr CreateFile(string lpFileName, uint dwDesiredAccess, int dwShareMode, ref SECURITY_ATTRIBUTES lpSecurityAttributes, int dwCreationDisposition, uint dwFlagsAndAttributes, int hTemplateFile);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    internal static extern SafeFileHandle CreateFile(String lpFileName, UInt32 dwDesiredAccess, Int32 dwShareMode, IntPtr lpSecurityAttributes, Int32 dwCreationDisposition, UInt32 dwFlagsAndAttributes, Int32 hTemplateFile);
    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern bool ReadFile(IntPtr hFile, [Out] byte[] lpBuffer, uint nNumberOfBytesToRead, out uint lpNumberOfBytesRead, IntPtr lpOverlapped);

    [DllImport("kernel32.dll")]
    internal static extern uint WaitForSingleObject(IntPtr hHandle, int dwMilliseconds);

    [DllImport("kernel32.dll")]
    internal static extern bool WriteFile(IntPtr hFile, byte[] lpBuffer, uint nNumberOfBytesToWrite, out uint lpNumberOfBytesWritten, [In] ref System.Threading.NativeOverlapped lpOverlapped);

    [StructLayout(LayoutKind.Sequential)]
    internal struct SECURITY_ATTRIBUTES
    {
        public int nLength;
        public IntPtr lpSecurityDescriptor;
        public bool bInheritHandle;
    }
}