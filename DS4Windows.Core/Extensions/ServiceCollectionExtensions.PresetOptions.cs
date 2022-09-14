using DS4WinWPF.DS4Control;

namespace DS4Windows;

public static partial class ServiceCollectionExtensions
{
    public static IServiceCollection AddPresetOptions(this IServiceCollection services)
    {
        services.AddTransient<GamepadPreset>(provider =>
        {
            var controlService = provider.GetRequiredService<IControlService>();
            return ActivatorUtilities.CreateInstance<GamepadPreset>(provider, 
                controlService, 
                Translations.Strings.GamepadPresetName, 
                Translations.Strings.GamepadPresetDescription, 
                true,
                PresetOption.OutputContChoice.Xbox360);
        });
        
        services.AddTransient<GamepadGyroCamera>(provider =>
        {
            var controlService = provider.GetRequiredService<IControlService>();
            return ActivatorUtilities.CreateInstance<GamepadGyroCamera>(provider, 
                controlService, 
                Translations.Strings.GamepadGyroCameraName, 
                Translations.Strings.GamepadGyroCameraDescription, 
                true,
                PresetOption.OutputContChoice.Xbox360);
        });
        
        services.AddTransient<MixedPreset>(provider =>
        {
            var controlService = provider.GetRequiredService<IControlService>();
            return ActivatorUtilities.CreateInstance<MixedPreset>(provider, 
                controlService, 
                Translations.Strings.MixedPresetName, 
                Translations.Strings.MixedPresetDescription, 
                true,
                PresetOption.OutputContChoice.Xbox360);
        });
        
        services.AddTransient<MixedGyroMousePreset>(provider =>
        {
            var controlService = provider.GetRequiredService<IControlService>();
            return ActivatorUtilities.CreateInstance<MixedGyroMousePreset>(provider, 
                controlService, 
                Translations.Strings.MixedGyroMousePresetName, 
                Translations.Strings.MixedGyroMousePresetDescription,
                true,
                PresetOption.OutputContChoice.Xbox360);
        });
        
        services.AddTransient<KBMPreset>(provider =>
        {
            var controlService = provider.GetRequiredService<IControlService>();
            return ActivatorUtilities.CreateInstance<KBMPreset>(provider, 
                controlService, 
                Translations.Strings.KBMPresetName, 
                Translations.Strings.KBMPresetDescription,
                false,
                PresetOption.OutputContChoice.None);
        });
        
        services.AddTransient<KBMGyroMouse>(provider =>
        {
            var controlService = provider.GetRequiredService<IControlService>();
            return ActivatorUtilities.CreateInstance<KBMGyroMouse>(provider, 
                controlService, 
                Translations.Strings.KBMGyroMouseName, 
                Translations.Strings.KBMGyroMouseDescription,
                false,
                PresetOption.OutputContChoice.None);
        });

        return services
            .AddTransient<PresetOption, GamepadPreset>(provider => provider.GetRequiredService<GamepadPreset>())
            .AddTransient<PresetOption, GamepadGyroCamera>(provider => provider.GetRequiredService<GamepadGyroCamera>())
            .AddTransient<PresetOption, MixedPreset>(provider => provider.GetRequiredService<MixedPreset>())
            .AddTransient<PresetOption, MixedGyroMousePreset>(provider => provider.GetRequiredService<MixedGyroMousePreset>())
            .AddTransient<PresetOption, KBMPreset>(provider => provider.GetRequiredService<KBMPreset>())
            .AddTransient<PresetOption, KBMGyroMouse>(provider => provider.GetRequiredService<KBMGyroMouse>());
    }
}