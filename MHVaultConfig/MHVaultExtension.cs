using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using VaultSharp;
using VaultSharp.V1.AuthMethods.AppRole;
using VaultSharp.V1.Commons;

namespace MHVaultConfig.Extensions
{
    public sealed class MHVaultConfigurationProvider : ConfigurationProvider
    {
        public void Update(Dictionary<string, string?> values)
        {
            Data = new Dictionary<string, string?>(values);
            OnReload();
        }
    }
    public sealed class MHVaultConfigurationSource : IConfigurationSource
    {
        public MHVaultConfigurationProvider Provider { get; } = new();

        public IConfigurationProvider Build(IConfigurationBuilder builder)
            => Provider;
    }
    public static class WebApplicationBuilderExtensions
    {
        public static WebApplicationBuilder AddMHVaultConfig(
            this WebApplicationBuilder builder)
        {
            var source = new MHVaultConfigurationSource();
            builder.Configuration.Sources.Add(source);

            builder.Services.AddSingleton(source);

            builder.Services.AddHostedService<MHVaultConfigUpdater>();

            return builder;
        }
    }

    public sealed class MHVaultConfigUpdater : BackgroundService
    {
        private readonly MHVaultConfigurationSource _source;
        private readonly IConfigurationRoot _configuration;
        private readonly ILogger _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        private IConfiguration cfg;

        private string jsonConfig = string.Empty;

        public MHVaultConfigUpdater(
            MHVaultConfigurationSource source,
            IConfiguration configuration,
            ILogger<MHVaultConfigUpdater> logger,
            IHttpClientFactory httpClientFactory
            )
        {
            _source= source;
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
                    _source.Provider.Update(newValues);
                    _configuration.Reload();
                    _logger.LogInformation("MHVaultConfigUpdater. Configuration updated from vault");
                }
                catch(Exception ex)
                {
                    _logger.LogError($"MHVaultConfigUpdater error get data from vault:{ex.Message}");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }

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


            //string jsonString = JsonSerializer.Serialize(secret.Data.Data);
            //Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonString));
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

        private async Task<Dictionary<string, string?>> UpdateConfig()
        {

            var secret = await GetSecret(_configuration.GetValue<string>("vault:path"), _configuration.GetValue<string>("vault:mountPoint"));

            BuildConfiguration(secret);

            return cfg.AsEnumerable().ToDictionary();
            
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
