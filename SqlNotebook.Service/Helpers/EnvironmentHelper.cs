using System;

namespace DAP.SqlNotebook.Service.Helpers;

public static class EnvironmentHelper
{
    public static string GetAspnetCoreEnvironment()
    {
        return Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
    }
}
//
// public static class ServiceHostBuilderExtensions
// {
//     public static ServiceHostBuilder<TSettings> ConfigureCdpHost<TSettings>(
//         this ServiceHostBuilder<TSettings> serviceHostBuilder)
//         where TSettings : ServiceSettings, new()
//     {
//         return serviceHostBuilder
//             .SetCdpConfigurationBuilder()
//             .SetCdpLogFactoryBuilder();
//     }
//
//     public static ServiceHostBuilder<TSettings> ConfigureAsKestrelWindowsService<TSettings>(
//         this ServiceHostBuilder<TSettings> serviceHostBuilder,
//         Action<KestrelServerOptions, TSettings>? configureKestrel = null)
//         where TSettings : ServiceSettings, new()
//     {
//         serviceHostBuilder.ConfigureWebApplication((builder, settings) =>
//         {
//             builder.Host.UseWindowsService();
//             builder.WebHost.UseKestrel(opts =>
//             {
//                 opts.AddServerHeader = false;
//                 configureKestrel?.Invoke(opts, settings);
//             });
//         });
//         return serviceHostBuilder;
//     }
//
//     private static ServiceHostBuilder<TSettings> SetCdpConfigurationBuilder<TSettings>(
//         this ServiceHostBuilder<TSettings> serviceHostBuilder)
//         where TSettings : ServiceSettings, new()
//     {
//         serviceHostBuilder
//             .SetConfigurationBuilder((configurationBuilder, env) =>
//             {
//                 configurationBuilder
//                     .AddJsonFile("commonsettings.json", optional: false)
//                     .AddJsonFile($"commonsettings.{env}.json", optional: true)
//                     .AddJsonFile("appsettings.json", optional: false)
//                     .AddJsonFile($"appsettings.{env}.json", true)
//                     .AddEnvironmentVariables("SS_");
//             });
//         return serviceHostBuilder;
//     }
//
//     private static ServiceHostBuilder<TSettings> SetCdpLogFactoryBuilder<TSettings>(
//         this ServiceHostBuilder<TSettings> serviceHostBuilder)
//         where TSettings : ServiceSettings, new()
//     {
//         serviceHostBuilder
//             .SetLogFactoryBuilder((configuration, provider) =>
//             {
//                 return new CoreNLogLogFactoryBuilder(
//                     configuration,
//                     LoggerSettingsType.Json,
//                     setupBuilder =>
//                     {
//                         setupBuilder.SetupExtensions(builder =>
//                             {
//                                 builder.RegisterCdpLayout(configuration, provider);
//                                 //the next line is required for the ConfigSettingLayoutRenderer, i.e. ${configsetting:Item=blah} to function
//                                 builder.RegisterConfigSettings(configuration);
//                             });
//
//                         setupBuilder.ReloadConfiguration();
//                     }
//                     );
//             });
//         return serviceHostBuilder;
//     }
// }