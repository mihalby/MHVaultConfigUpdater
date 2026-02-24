using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Runtime.CompilerServices;


namespace MHVaultConfig.Extensions
{
    
    
    public static class WebApplicationBuilderExtensions
    {
        public static WebApplicationBuilder AddMHVaultConfig(
            this WebApplicationBuilder builder)
        {
            IServiceProvider tempServiceProvider = builder.Services.BuildServiceProvider();
                        
            var httpClientFactory = tempServiceProvider.GetRequiredService<IHttpClientFactory>();
                        
            var source = new MHVaultConfigurationSource(builder.Configuration, httpClientFactory);
            builder.Configuration.Sources.Add(source);

            builder.Services.AddSingleton(source.Provider);

            builder.Services.AddHostedService<MHVaultConfigUpdater>();

            return builder;
        }
    }

    public static class HostBuilderExtensions
    {
        public static IHostBuilder AddMHVaultConfigOnce(this IHostBuilder hostBuilder, bool skipSSL=false)
        {

            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback =
                            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };

            var httpClient = skipSSL ? new HttpClient(handler) : new HttpClient();

            hostBuilder.ConfigureHostConfiguration(configurationBuilder => {
                var configuration = configurationBuilder.Build();
                var source = new MHVaultConfigurationSource(configuration, httpClient);
                configurationBuilder.Sources.Add(source);
            });

            return hostBuilder;
        }
    }


}
