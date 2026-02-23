using Microsoft.Extensions.Configuration;


namespace MHVaultConfig
{
    public sealed class MHVaultConfigurationProvider : ConfigurationProvider
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        

        public MHVaultConfigurationProvider(IHttpClientFactory httpClientFactory, IConfiguration configuration) : base()
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }
        public override void Load()
        {
            // Block startup until initial data is fetched
            
            
            var secret = Utils.GetSecret(_configuration.GetValue<string>("vault:path"), _configuration.GetValue<string>("vault:mountPoint"),
                _configuration.GetValue<string>("Vault:roleIdEnv"),
                _configuration.GetValue<string>("Vault:secretIdEnv"),
                _configuration.GetValue<string>("Vault:addrEnv"),
                _configuration.GetValue<string>("Vault:httpClientName"), 
                _httpClientFactory).GetAwaiter().GetResult();
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
