namespace DS4Windows;

public static partial class ServiceCollectionExtensions
{
    public static IServiceCollection AddCore(this IServiceCollection services)
    {
        return 
            services
                .AddSingleton<IControlServiceFactory, ControlServiceFactory>()
                .AddSingleton<IControlService>(provider => provider.GetRequiredService<IControlServiceFactory>().Create())
                .AddSingleton<IDS4Devices, DS4Devices>();;
    }
}