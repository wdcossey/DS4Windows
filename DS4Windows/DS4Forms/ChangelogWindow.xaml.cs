namespace DS4WinWPF.DS4Forms;

/// <summary>
/// Interaction logic for ChangelogWindow.xaml
/// </summary>
public partial class ChangelogWindow : Window
{
    private ChangelogViewModel changelogVM;

    public ChangelogWindow(ChangelogViewModel viewModel)
    {
        InitializeComponent();

        changelogVM = viewModel;

        DataContext = changelogVM;

        SetupEvents();

        changelogVM.RetrieveChangelogInfoAsync();

    }

    private void SetupEvents()
    {
        changelogVM.ChangelogDocumentChanged += ChangelogVM_ChangelogDocumentChanged;
    }

    private void ChangelogVM_ChangelogDocumentChanged(object sender, EventArgs e)
    {
        richChangelogTxtBox.Document = changelogVM.ChangelogDocument;
    }

    private void CloseBtn_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}