using System.Collections.Generic;

namespace DS4WinWPF;

public class UpdateOptions
{
    public UpdateOption Self { get; set; }
    public UpdateOption Updater { get; set; }
}

public class UpdateOption : DownloadOption
{
    public string? Newest { get; set; }
    public string? Changelog { get; set; }
    public override string? AnyCPU => null;
}

public class InstallerOptions : Dictionary<string, DownloadOption>
{
}

public class DownloadOption
{
    // ReSharper disable InconsistentNaming
    // ReSharper disable MemberCanBePrivate.Global
    // ReSharper disable MemberCanBeProtected.Global
    
    public virtual string? AnyCPU { get; set; }
    public string? x86 { get; set; }
    public string? x64 { get; set; }

    public string? GetDownloadUrl() => 
        (Environment.Is64BitOperatingSystem ? x64 : x86) ?? AnyCPU;
    
    // ReSharper restore InconsistentNaming
    // ReSharper restore MemberCanBePrivate.Global
    // ReSharper restore MemberCanBeProtected.Global
}