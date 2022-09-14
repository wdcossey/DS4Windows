using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Windows.Controls;
using HttpProgress;
using Microsoft.Extensions.Options;
using NonFormTimer = System.Timers.Timer;

namespace DS4WinWPF.DS4Forms;

/// <summary>
/// Interaction logic for WelcomeDialog.xaml
/// </summary>
public partial class WelcomeDialog : Window
{
    private readonly InstallerOptions _installerOptions;
    private readonly IHttpClientFactory _httpClientFactory;

    public WelcomeDialog(IOptions<InstallerOptions> installerSettingsOptions, IHttpClientFactory httpClientFactory, bool loadConfig = false)
    {
        _installerOptions = installerSettingsOptions.Value;
        _httpClientFactory = httpClientFactory;
            
        if (loadConfig)
        {
            Global.FindConfigLocation();
            Global.Load();
            //DS4Windows.Global.SetCulture(DS4Windows.Global.UseLang);
        }

        InitializeComponent();

        // FakerInput only works on Windows 8 or newer
        step5FakerInputPanel.IsEnabled = Global.IsWin8OrGreater();

        // HidHide only works on Windows 10 x64
        step4HidHidePanel.IsEnabled = IsHidHideControlCompatible();
    }

    private bool IsHidHideControlCompatible()
    {
        // HidHide only works on Windows 10 x64
        return DS4Windows.Global.IsWin10OrGreater() &&
               Environment.Is64BitOperatingSystem;
    }

    private bool IsFakerInputControlCompatible()
    {
        // FakerInput works on Windows 8.1 and later. Going to attempt
        // to support x64 and x86 arch
        return DS4Windows.Global.IsWin8OrGreater();
    }

    private void FinishedBtn_Click(object sender, RoutedEventArgs e) =>
        Close();

    private void Step2Btn_Click(object sender, RoutedEventArgs e)
    {
        Process.Start("http://www.microsoft.com/accessories/en-gb/d/xbox-360-controller-for-windows");
    }

    private void BluetoothSetLink_Click(object sender, RoutedEventArgs e)
    {
        Process.Start("control", "bthprops.cpl");
    }

    #region ViGEm Install

    private void VigemInstallBtn_Click(object sender, RoutedEventArgs e)=>
        InitDownload("ViGEmBus", sender as ContentControl);

    #endregion
        
    #region HidHide Install
        
    private void HidHideInstall_Click(object sender, RoutedEventArgs e) =>
        InitDownload("HidHide", sender as ContentControl);

    #endregion

    #region FakerInput Install
        
    private void FakerInputInstallBtn_Click(object sender, RoutedEventArgs e) =>
        InitDownload("FakerInput", sender as ContentControl);

    #endregion

    #region Installer

    private void InitDownload(string installerKey, ContentControl? control)
    {
        var downloadUrl = _installerOptions[installerKey].GetDownloadUrl();
        if (string.IsNullOrWhiteSpace(downloadUrl))
            return;

        var fileName = new FileInfo(downloadUrl).Name;
            
        var actualFilePath = Path.Combine(Path.GetTempPath(), fileName);
            
        if (File.Exists(actualFilePath)) 
            File.Delete(actualFilePath);
            
        var tempFilePath = Path.Combine(Path.GetTempPath(),  $"{fileName}.tmp");
            
        if (File.Exists(tempFilePath)) 
            File.Delete(tempFilePath);

        EnableControls(false);
        DownloadLaunch(downloadUrl, actualFilePath, tempFilePath, control);
    }
        
    private async void DownloadLaunch(string downloadUrl, string actualFilePath, string tempFilePath, ContentControl? control)
    {
        var progress = new Progress<ICopyProgress>(x => // Please see "Notes on IProgress<T>"
        {
            if (control is null)
                return;
                
            // This is your progress event!
            // It will fire on every buffer fill so don't do anything expensive.
            // Writing to the console IS expensive, so don't do the following in practice...
            control.Content = Localization.Downloading.Replace("*number*%", x.PercentComplete.ToString("P"));
            //Console.WriteLine(x.PercentComplete.ToString("P"));
        });
            
        bool downloadSuccess;
            
        await using (var downloadStream = new FileStream(tempFilePath, FileMode.CreateNew))
        {
            var client = _httpClientFactory.CreateClient("InstallerClient");
            var response = await client.GetAsync(downloadUrl, downloadStream, progress);
            downloadSuccess = response.IsSuccessStatusCode;
        }

        try
        {
            if (downloadSuccess)
                File.Move(tempFilePath, actualFilePath);

            if (!File.Exists(actualFilePath))
            {
                InstallFailed(control);
                return;
            }
                
            var startInfo = new ProcessStartInfo(actualFilePath)
            {
                UseShellExecute = true // Needed to run program as admin
            };
            var process = Process.Start(startInfo);

            if (process != null && process.HasExited != true)
            {
                InstallInstalling(control);

                await process.WaitForExitAsync();

                //https://docs.microsoft.com/en-us/windows/win32/msi/error-codes
                var exitCode = process.ExitCode;

                if (Global.IsFakerInputInstalled())
                    InstallCompleted(control);
                else
                    InstallFailed(control);
            }
            else
            {
                InstallFailed(control);
            }
        }
        catch
        {
            InstallFailed(control);
        }
        finally
        {
            File.Delete(actualFilePath);
            File.Delete(tempFilePath);
        }
    }

    private void InstallInstalling(ContentControl? control)
    {
        if (control is null)
            return;
            
        Dispatcher.BeginInvoke(() =>
        {
            control.Content = Localization.Installing;
        });
    }

    private void InstallCompleted(ContentControl? control)
    {
        Dispatcher.BeginInvoke(() =>
        {
            if (control is not null)
                control.Content = Localization.InstallComplete;
            EnableControls(true);
        });
    }

    private void InstallFailed(ContentControl? control)
    {
        Dispatcher.BeginInvoke(() =>
        {
            if (control is not null)
                control.Content = Localization.InstallFailed;
            EnableControls(true);
        }, null);
    }
        
    #endregion
        
    private void EnableControls(bool on)
    {
        vigemInstallBtn.IsEnabled = on;
        step4HidHidePanel.IsEnabled = on;
        step5FakerInputPanel.IsEnabled = on;

        // Perform compatibility checks for controls that might need
        // to be disabled when on is set to true
        if (on)
        {
            LateControlsCheck();
        }
    }

    /// <summary>
    /// Possibly disable some controls for components that are not compatible
    /// with the installed version of Windows or system configuration
    /// </summary>
    private void LateControlsCheck()
    {
        step4HidHidePanel.IsEnabled = IsHidHideControlCompatible();
        step5FakerInputPanel.IsEnabled = IsFakerInputControlCompatible();
    }
}