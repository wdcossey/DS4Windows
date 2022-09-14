namespace DS4WinWPF.DS4Forms;

/// <summary>
/// Interaction logic for UpdaterWindow.xaml
/// </summary>
public partial class UpdaterWindow : Window
{
    public MessageBoxResult Result { get; private set; } = MessageBoxResult.No;

    private readonly UpdaterWindowViewModel _updaterWindowViewModel;

    public UpdaterWindow(UpdaterWindowViewModel updaterWindowViewModel, string newVersion)
    {
        _updaterWindowViewModel = updaterWindowViewModel;
        
        InitializeComponent();

        Title = Localization.DS4Update;
            
        captionTextBlock.Text = Localization.DownloadVersion.Replace("*number*", newVersion);

        _updaterWindowViewModel.BlankSkippedVersion();

        DataContext = _updaterWindowViewModel;

        SetupEvents();

        _updaterWindowViewModel.RetrieveChangelogInfo();
    }

    private void SetupEvents()
    {
        _updaterWindowViewModel.ChangelogDocumentChanged += UpdaterWindowViewModel_ChangelogDocumentChanged;
    }

    private void UpdaterWindowViewModel_ChangelogDocumentChanged(object? sender, EventArgs e) => 
        richChangelogTxtBox.Document = _updaterWindowViewModel.ChangelogDocument;

    private void YesBtn_Click(object sender, RoutedEventArgs e)
    {
        Result = MessageBoxResult.Yes;
        Close();
    }

    private void NoBtn_Click(object sender, RoutedEventArgs e)
    {
        Result = MessageBoxResult.No;
        Close();
    }

    private void SkipVersionBtn_Click(object sender, RoutedEventArgs e)
    {
        Result = MessageBoxResult.No;
        _updaterWindowViewModel.SetSkippedVersion();
        Close();
    }
}