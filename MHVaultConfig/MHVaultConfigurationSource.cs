using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace MHVaultConfig
{
    public sealed class MHVaultConfigurationSource : IConfigurationSource
    {
        public MHVaultConfigurationProvider Provider { get; }

        public MHVaultConfigurationSource(IConfiguration configuration,IHttpClientFactory httpClientFactory) 
        {

            Provider = new(httpClientFactory,configuration);

        }


        public IConfigurationProvider Build(IConfigurationBuilder builder)
            => Provider;
    }
}
