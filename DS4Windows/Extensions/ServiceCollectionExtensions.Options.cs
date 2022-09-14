namespace DS4WinWPF;

public static partial class ServiceCollectionExtensions
{
    public static IServiceCollection AddUpdateOptions(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<UpdateOptions>(options => config.GetSection("Updates").Bind(options));
        
        services.AddHttpClient();
        services.AddHttpClient("UpdateClient");
        
        return services;
    }
    
    public static IServiceCollection AddInstallerOptions(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<InstallerOptions>(options => config.GetSection("Installers").Bind(options));

        services.AddHttpClient();
        services.AddHttpClient("InstallerClient");

        return services;
    }
}