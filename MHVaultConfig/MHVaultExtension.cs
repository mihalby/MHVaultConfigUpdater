using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Text;
using System.Text.Json;
using VaultSharp;
using VaultSharp.V1.AuthMethods.AppRole;
using VaultSharp.V1.Commons;

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

    
}
