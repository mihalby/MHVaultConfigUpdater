using Microsoft.Extensions.Configuration;


namespace MHVaultConfig
{
    public sealed class MHVaultConfigurationProvider : ConfigurationProvider
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        private readonly HttpClient _httpClient;
        

        public MHVaultConfigurationProvider(IHttpClientFactory httpClientFactory, IConfiguration configuration) : base()
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public MHVaultConfigurationProvider(HttpClient httpClient, IConfiguration configuration) : base()
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public override void Load()
        {
            // Block startup until initial data is fetched


            var secret = _httpClient == null ? Utils.GetSecret(_configuration.GetValue<string>("vault:path"), _configuration.GetValue<string>("vault:mountPoint"),
                _configuration.GetValue<string>("Vault:roleIdEnv"),
                _configuration.GetValue<string>("Vault:secretIdEnv"),
                _configuration.GetValue<string>("Vault:addrEnv"),
                _configuration.GetValue<string>("Vault:httpClientName"),
                _httpClientFactory).GetAwaiter().GetResult() :
                Utils.GetSecret(_configuration.GetValue<string>("vault:path"), _configuration.GetValue<string>("vault:mountPoint"),
                _configuration.GetValue<string>("Vault:roleIdEnv"),
                _configuration.GetValue<string>("Vault:secretIdEnv"),
                _configuration.GetValue<string>("Vault:addrEnv"),
                _httpClient).GetAwaiter().GetResult();

            var values = Utils.GetConfigDictionary(secret);

            Data = new Dictionary<string, string?>(values);
        }

       

        public void Update(Dictionary<string, string?> values)
        {
            Data = new Dictionary<string, string?>(values);
            OnReload();
        }
    }
}
