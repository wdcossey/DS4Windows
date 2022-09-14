using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using HttpProgress;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.Extensions.Options;

namespace DS4WinWPF.DS4Forms.ViewModels;

public class MainWindowsViewModel
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly UpdateOptions _updateOptions;
    private bool fullTabsEnabled = true;

    public MainWindowsViewModel(IHttpClientFactory httpClientFactory, IOptions<UpdateOptions> updateOptions)
    {
        _httpClientFactory = httpClientFactory;
        _updateOptions = updateOptions.Value;
    }
        
    public bool FullTabsEnabled
    {
        get => fullTabsEnabled;
        set
        {
            fullTabsEnabled = value;
            FullTabsEnabledChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    public event EventHandler FullTabsEnabledChanged;

    public string updaterExe = Environment.Is64BitProcess ? "DS4Updater.exe" : "DS4Updater_x86.exe";

    private async Task<string> DownloadUpstreamUpdaterVersionAsync()
    {
        var result = string.Empty;
        // Sorry other devs, gonna have to find your own server
        var client = _httpClientFactory.CreateClient("UpdateClient");
            
        var filename = Path.Combine(Path.GetTempPath(), "DS4Updater_version.txt");
        var readFile = false;
        await using (var downloadStream = new FileStream(filename, FileMode.Create))
        {
            var responseMessage = await client.GetAsync(_updateOptions.Updater.Newest, downloadStream);
            readFile = responseMessage.IsSuccessStatusCode;
        }

        if (readFile)
        {
            result = File.ReadAllText(filename).Trim();
            File.Delete(filename);
        }

        return result;
    }

    public async Task<(bool Launch, string UpstreamVersion)> RunUpdaterCheckAsync()
    {
        var launch = false;
            
        var destPath = Path.Combine(Global.exedirpath, "DS4Updater.exe");
        var updaterExists = File.Exists(destPath);
        var upstreamVersion = await DownloadUpstreamUpdaterVersionAsync();
        if (!updaterExists 
            || (!string.IsNullOrEmpty(upstreamVersion) && string.Compare(FileVersionInfo.GetVersionInfo(destPath).FileVersion, upstreamVersion, StringComparison.OrdinalIgnoreCase) != 0))
        {
            var client = _httpClientFactory.CreateClient("UpdateClient");
                
            var requestUri = string.Format(_updateOptions.Updater.GetDownloadUrl() ?? "{0}", upstreamVersion);
            var filename = Path.Combine(Path.GetTempPath(), "DS4Updater.exe");
            await using (var downloadStream = new FileStream(filename, FileMode.Create))
            {
                var responseMessage = await client.GetAsync(requestUri, downloadStream);
                launch = responseMessage.IsSuccessStatusCode;
            }

            if (!launch) 
                return (launch, upstreamVersion);
                
            if (Global.AdminNeeded())
            {
                var copyStatus = DS4Windows.Util.ElevatedCopyUpdater(filename);
                if (copyStatus != 0) launch = false;
            }
            else
            {
                if (updaterExists) File.Delete(destPath);
                File.Move(filename, destPath);
            }
        }

        return (launch, upstreamVersion);
    }

    public async Task DownloadUpstreamVersionInfoAsync()
    {
        // Sorry other devs, gonna have to find your own server
        var client = _httpClientFactory.CreateClient("UpdateClient");
        var filename = Global.appdatapath + "\\version.txt";
        var success = false;
        await using (var downloadStream = new FileStream(filename, FileMode.Create))
        {
            try
            {
                var responseMessage = await client.GetAsync(_updateOptions.Self.Newest, downloadStream);
                success = responseMessage.IsSuccessStatusCode;
            }
            catch (AggregateException) { }
        }

        if (!success && File.Exists(filename))
        {
            File.Delete(filename);
        }
    }

    public void CheckDrivers()
    {
        var deriverinstalled = Global.IsViGEmBusInstalled();
        if (!deriverinstalled || !Global.IsRunningSupportedViGEmBus())
        {
            var startInfo = new ProcessStartInfo();
            startInfo.FileName = $"{Global.exelocation}";
            startInfo.Arguments = "-driverinstall";
            startInfo.Verb = "runas";
            startInfo.UseShellExecute = true;
            try
            {
                using (var temp = Process.Start(startInfo))
                {
                }
            }
            catch { }
        }
    }

    public bool LauchDS4Updater()
    {
        var launch = false;
        using (var p = new Process())
        {
            p.StartInfo.FileName = Path.Combine(Global.exedirpath, "DS4Updater.exe");
            var isAdmin = Global.IsAdministrator();
            var argList = new List<string>();
            argList.Add("-autolaunch");
            if (!isAdmin)
            {
                argList.Add("-user");
            }

            // Specify current exe to have DS4Updater launch
            argList.Add("--launchExe");
            argList.Add(Global.exeFileName);

            p.StartInfo.Arguments = string.Join(" ", argList);
            if (Global.AdminNeeded())
                p.StartInfo.Verb = "runas";

            try { launch = p.Start(); }
            catch (InvalidOperationException) { }
        }

        return launch;
    }
}