using System.Globalization;
using System.Threading;
using WPFLocalizeExtension.Engine;

namespace DS4WinWPF;

public class CultureHelper
{
    public static void SetUICulture(string culture)
    {
        try
        {
            //CultureInfo ci = new CultureInfo("ja");
            var cultureInfo = CultureInfo.GetCultureInfo(culture);
            LocalizeDictionary.Instance.SetCurrentThreadCulture = true;
            LocalizeDictionary.Instance.Culture = cultureInfo;
            // fixes the culture in threads
            CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;
            //DS4WinWPF.Properties.Resources.Culture = ci;
            Thread.CurrentThread.CurrentCulture = cultureInfo;
            Thread.CurrentThread.CurrentUICulture = cultureInfo;
        }
        catch (CultureNotFoundException) { /* Skip setting culture that we cannot set */ }
    }
}