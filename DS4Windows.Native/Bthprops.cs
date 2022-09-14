namespace DS4Windows.Native;

internal static class Bthprops
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct BLUETOOTH_FIND_RADIO_PARAMS
    {
        [MarshalAs(UnmanagedType.U4)]
        public int dwSize;
    }

    [DllImport("bthprops.cpl", CharSet = CharSet.Auto)]
    internal static extern IntPtr BluetoothFindFirstRadio(ref BLUETOOTH_FIND_RADIO_PARAMS pbtfrp, ref IntPtr phRadio);

    [DllImport("bthprops.cpl", CharSet = CharSet.Auto)]
    internal static extern bool BluetoothFindNextRadio(IntPtr hFind, ref IntPtr phRadio);

    [DllImport("bthprops.cpl", CharSet = CharSet.Auto)]
    internal static extern bool BluetoothFindRadioClose(IntPtr hFind);
}