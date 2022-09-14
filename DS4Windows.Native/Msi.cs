namespace DS4Windows.Native;

[SuppressUnmanagedCodeSecurity]
internal static class Msi
{
    [DllImport("msi.dll", CharSet = CharSet.Auto)]
    internal static extern uint MsiGetShortcutTarget(string targetFile, StringBuilder productCode, StringBuilder featureID, StringBuilder componentCode);

    [DllImport("msi.dll", CharSet = CharSet.Auto)]
    internal static extern InstallState MsiGetComponentPath(string productCode, string componentCode, StringBuilder componentPath, ref int componentPathBufferSize);

    internal const int MaxFeatureLength = 38;
    internal const int MaxGuidLength = 38;
    internal const int MaxPathLength = 1024;

    internal enum InstallState
    {
        NotUsed = -7,
        BadConfig = -6,
        Incomplete = -5,
        SourceAbsent = -4,
        MoreData = -3,
        InvalidArg = -2,
        Unknown = -1,
        Broken = 0,
        Advertised = 1,
        Removed = 1,
        Absent = 2,
        Local = 3,
        Source = 4,
        Default = 5
    }
}