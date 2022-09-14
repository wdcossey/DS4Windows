namespace DS4Windows.Native;

internal static class PsApi
{
    [DllImport("psapi.dll")]
    internal static extern uint GetModuleFileNameEx(IntPtr hWnd, IntPtr hModule, StringBuilder lpFileName, int nSize);

}