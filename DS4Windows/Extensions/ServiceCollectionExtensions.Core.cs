using DS4WinWPF.DS4Forms;

namespace DS4WinWPF;

public static partial class ServiceCollectionExtensions
{
    // ReSharper disable once InconsistentNaming
    public static IServiceCollection AddUICore(this IServiceCollection services)
    {
        return services
            .AddSingleton<MainWindow>()
            .AddSingleton<WelcomeDialog>()
            .AddTransient<PresetOptionWindow>()
            .AddTransient<ChangelogWindow>()
            .AddViewModels()
            .AddFactories();
    }
    
    private static IServiceCollection AddViewModels(this IServiceCollection services)
    {
        services
            .AddSingleton<MainWindowsViewModel>()
            .AddSingleton<UpdaterWindow>()
            .AddTransient<PresetOptionViewModel>()
            .AddTransient<SettingsViewModel>()
            .AddTransient<LogViewModel>()
            .AddTransient<ChangelogViewModel>()
            .AddTransient<UpdaterWindowViewModel>()
            .AddTransient<BindingWindowViewModel>()
            .AddTransient<ControllerListViewModel>()
            .AddTransient<AxialStickControlViewModel>()
            .AddTransient<TrayIconViewModel>();
        
        services
            .AddTransient<IUpdaterWindowFactory, UpdaterWindowFactory>()
            .AddTransient<IUpdaterWindowViewModelFactory, UpdaterWindowViewModelFactory>()
            .AddTransient<IProfileSettingsViewModelFactory, ProfileSettingsViewModelFactory>()
            .AddTransient<IMappingListViewModelFactory, MappingListViewModelFactory>()
            .AddTransient<ISpecialActionsListViewModelFactory, SpecialActionsListViewModelFactory>()
            .AddTransient<IBindingWindowViewModelFactory, BindingWindowViewModelFactory>();
        
        return services;
    }
    
    private static IServiceCollection AddFactories(this IServiceCollection services)
    {
        return services
            .AddSingleton<IAutoProfileCheckerFactory, AutoProfileCheckerFactory>()
            .AddTransient<ISpecialActionEditorFactory, SpecialActionEditorFactory>()
            .AddTransient<IBindingWindowFactory, BindingWindowFactory>()
            .AddTransient<IProfileEditorFactory, ProfileEditorFactory>()
            .AddTransient<IWelcomeDialogFactory, WelcomeDialogFactory>();
    }
}