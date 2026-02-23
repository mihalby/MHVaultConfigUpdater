using Microsoft.Extensions.Configuration;


namespace MHVaultConfig
{
    public sealed class MHVaultConfigurationProvider : ConfigurationProvider
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        /*
        public string pt { get; set; }
        public string mp { get; set; }
        public string roleIdEnvName { get; set; }
        public string secretIdEnvName { get; set; }
        public string vaultAddrEnvName { get; set; }
        public string vaultHttpClientName { get; set; }
        */

        public MHVaultConfigurationProvider(IHttpClientFactory httpClientFactory, IConfiguration configuration) : base()
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }
        public override void Load()
        {
            // Block startup until initial data is fetched
            //var values = FetchFromVaultAsync().GetAwaiter().GetResult();

            //Data = new Dictionary<string, string?>(values);
            
            var secret = Utils.GetSecret(_configuration.GetValue<string>("vault:path"), _configuration.GetValue<string>("vault:mountPoint"),
                _configuration.GetValue<string>("Vault:roleIdEnv"),
                _configuration.GetValue<string>("Vault:secretIdEnv"),
                _configuration.GetValue<string>("Vault:addrEnv"),
                _configuration.GetValue<string>("Vault:httpClientName"), 
                _httpClientFactory).GetAwaiter().GetResult();
            var values = Utils.GetConfigDictionary(secret);

            Data = new Dictionary<string, string?>(values);
        }

        /*
        private async Task<Dictionary<string, string?>> FetchFromVaultAsync()
        {
            // call vault here
        }
        */

        public void Update(Dictionary<string, string?> values)
        {
            Data = new Dictionary<string, string?>(values);
            OnReload();
        }
    }
}
