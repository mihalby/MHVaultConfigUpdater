using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using VaultSharp;
using VaultSharp.V1.AuthMethods.AppRole;
using VaultSharp.V1.Commons;

namespace MHVaultConfig
{
    public sealed class MHVaultConfigUpdater : BackgroundService
    {
        private readonly MHVaultConfigurationProvider _sourceProvider;
        private readonly IConfigurationRoot _configuration;
        private readonly ILogger _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        
        public MHVaultConfigUpdater(
            MHVaultConfigurationProvider sourceProvider,
            IConfiguration configuration,
            ILogger<MHVaultConfigUpdater> logger,
            IHttpClientFactory httpClientFactory
            )
        {
            _sourceProvider = sourceProvider;
            _configuration = (IConfigurationRoot)configuration;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {

            var timer = new PeriodicTimer(TimeSpan.FromSeconds(_configuration.GetValue<int>("Vault:updateFromSec")));
            _logger.LogInformation("MHVaultConfigUpdaterStart");

            do
            {
                _logger.LogInformation("Get config from vault  at-: {time}", DateTimeOffset.Now);

                try
                {
                    var newValues = await UpdateConfig();
                    _sourceProvider.Update(newValues);
                    _configuration.Reload();
                    _logger.LogInformation("MHVaultConfigUpdater. Configuration updated from vault");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"MHVaultConfigUpdater error get data from vault:{ex.Message}");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }

        /*
        private async Task<Secret<SecretData>> GetSecret(string pt, string mp)
        {
            var roleIdEnvName = _configuration.GetValue<string>("Vault:roleIdEnv");
            var secretIdEnvName = _configuration.GetValue<string>("Vault:secretIdEnv");
            var vaultAddrEnvName = _configuration.GetValue<string>("Vault:addrEnv");
            var vaultHttpClientName = _configuration.GetValue<string>("Vault:httpClientName");

            var roleId = Environment.GetEnvironmentVariable(roleIdEnvName);
            var secretId = Environment.GetEnvironmentVariable(secretIdEnvName);
            var vaultAddress = Environment.GetEnvironmentVariable(vaultAddrEnvName);

            var authMethod = new AppRoleAuthMethodInfo(roleId, secretId);

            var vaultClientSettings = new VaultClientSettings(vaultAddress, authMethod)
            {
                MyHttpClientProviderFunc = _ => vaultHttpClientName != null ? _httpClientFactory.CreateClient(vaultHttpClientName) : _httpClientFactory.CreateClient()
            };

            var vaultClient = new VaultClient(vaultClientSettings);

            try
            {

                var secret = await vaultClient.V1.Secrets.KeyValue.V2
                        .ReadSecretAsync(
                            path: pt,
                            mountPoint: mp
                            );

                return secret;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }



        }
        

        private IConfiguration BuildConfiguration(Secret<SecretData> secret)
        {
            jsonConfig = JsonSerializer.Serialize(secret.Data.Data);
            Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonConfig));
            cfg = new ConfigurationBuilder()
                .AddJsonStream(stream)
                .Build();


            return cfg;
        }
        */

        private async Task<Dictionary<string, string?>> UpdateConfig()
        {

            var secret = await Utils.GetSecret(_configuration,_httpClientFactory);

            return Utils.GetConfigDictionary(secret);

        }
        

        private Task<Dictionary<string, string?>> LoadFromVault()
        {
            // load secrets here
            return Task.FromResult(new Dictionary<string, string?>
            {
                ["MyOptions:ApiKey"] = "new-key"
            });
        }
    }
}
