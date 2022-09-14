using DS4WinWPF.DS4Forms;

namespace DS4WinWPF;

public interface IWelcomeDialogFactory
{
    WelcomeDialog Create(bool loadConfig);
}

public class WelcomeDialogFactory : IWelcomeDialogFactory
{
    private readonly IServiceProvider _serviceProvider;

    public WelcomeDialogFactory(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

    public WelcomeDialog Create(bool loadConfig) =>
        ActivatorUtilities.CreateInstance<WelcomeDialog>(_serviceProvider, loadConfig);
}