using System.Windows.Controls;

namespace DS4WinWPF.DS4Forms;

/// <summary>
/// Interaction logic for LanguagePackControl.xaml
/// </summary>
public partial class LanguagePackControl : UserControl
{
    private readonly LanguagePackViewModel _langPackViewModel;

    public LanguagePackControl()
    {
        InitializeComponent();

        _langPackViewModel = new LanguagePackViewModel();
        DataContext = null;
        _langPackViewModel.ScanFinished += LangPackViewModel_ScanFinished;
        _langPackViewModel.SelectedIndexChanged += CheckForCultureChange;

        _langPackViewModel.ScanForLangPacks();
    }

    private void CheckForCultureChange(object sender, EventArgs e)
    {
        if (_langPackViewModel.ChangeLanguagePack())
        {
            MessageBox.Show(Localization.LanguagePackApplyRestartRequired,
                "DS4Windows", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void LangPackViewModel_ScanFinished(object sender, EventArgs e)
    {
        Dispatcher.BeginInvoke(() =>
        {
            cbCulture.IsEnabled = true;
            DataContext = _langPackViewModel;
        });
    }
}