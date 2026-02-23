using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using VaultSharp;
using VaultSharp.V1.AuthMethods.AppRole;
using VaultSharp.V1.Commons;

namespace MHVaultConfig
{
    public static class Utils
    {
        public static async Task<Secret<SecretData>> GetSecret(IConfiguration _configuration,IHttpClientFactory _httpClientFactory)
        {

            var pt = _configuration.GetValue<string>("vault:path");
            var mp = _configuration.GetValue<string>("vault:mountPoint");
            var roleIdEnvName = _configuration.GetValue<string>("Vault:roleIdEnv");
            var secretIdEnvName = _configuration.GetValue<string>("Vault:secretIdEnv");
            var vaultAddrEnvName = _configuration.GetValue<string>("Vault:addrEnv");
            var vaultHttpClientName = _configuration.GetValue<string>("Vault:httpClientName");

            return await GetSecret(pt, mp, roleIdEnvName, secretIdEnvName, vaultAddrEnvName, vaultHttpClientName, _httpClientFactory);
        }
        public static async Task<Secret<SecretData>> GetSecret(string pt, string mp, string roleIdEnvName, string secretIdEnvName, string vaultAddrEnvName, string vaultHttpClientName,
            IHttpClientFactory _httpClientFactory)
        {
           

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

        private static IConfiguration BuildConfiguration(Secret<SecretData> secret)
        {
            var jsonConfig = JsonSerializer.Serialize(secret.Data.Data);
            Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonConfig));
            var cfg = new ConfigurationBuilder()
                .AddJsonStream(stream)
                .Build();


            return cfg;
        }

        public static Dictionary<string, string?> GetConfigDictionary(Secret<SecretData> secret)
        {

            
            return BuildConfiguration(secret).AsEnumerable().ToDictionary();

        }

       

    }
}
