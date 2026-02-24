# MFSys.MHVaultConfig

`MFSys.MHVaultConfig` is an ASP.NET Core configuration provider that loads .NET core application configuration values directly from HashiCorp Vault.
Now using only **AppRole authentication** for HashiCorp Vault.

It integrates with `WebApplicationBuilder` and injects keys stored from vault directly into the standard `IConfiguration` pipeline.
---

## Features

- Loads(adding or replace) keys(secrets) from HashiCorp Vault into application configuration
**for WebApplicationBuilder use extension AddMHVaultConfig, for IHostBuilder use extension AddMHVaultConfigOnce(read keys only one time)**
- HashiCorp Vault AppRole authentication
- Seamless integration with ASP.NET Core configuration
- Environment variable support
- Supports hierarchical configuration keys

## Installation

```bash
dotnet add package MFSys.MHVaultConfig
```

## Required Setup
**Application configuration must contains section "Vault"**
```
"Vault": {
  "path": "youapp/config",
  "mountPoint": "mfsys-secret",
  "httpClientName": "VaultClient",
  "roleIdEnv": "YOUSYS_VAULT_ROLE_ID",
  "secretIdEnv": "YOUSYS_VAULT_SECRET_ID",
  "addrEnv": "YOUSYS_VAULT_URL",
  "updateFromSec": 30
  
}
```
- path - Name of the KV secret engine mount.
- mountPoint - The secret path inside the mount.
- httpClientName - name of the IHttpClientFactory named client. HttpClientFactory must configured before use extension
- roleIdEnv - enveropment variable name where store roleId
- secretIdEnv - enveropment variable name where store secretId
- addrEnv - enveropment variable name where store HashiCorp vault instance address
- updateFromSec - reread from vaul to configuration each ...

**Enveropment variables in conformity with application configuration:**
```
export YOUSYS_VAULT_URL=https://localhost:8200
export YOUSYS_VAULT_ROLE_ID=your-role-id
export YOUSYS_VAULT_SECRET_ID=your-secret-id
```
HashiCorp vaults - json

## Example use

application settings.json
```
{
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://0.0.0.0:5000"
      }
    }
  },
  "Vault": {
    "path": "myapp/config",
    "mountPoint": "myapp-secret",
    "httpClientName": "VaultClient",
    "roleIdEnv": "MYSYS_VAULT_ROLE_ID",
    "secretIdEnv": "MYSYS_VAULT_SECRET_ID",
    "addrEnv": "MYSYS_VAULT_URL",
    "updateFromSec": 30
    
  }
}
```
Enveropment variables
```
export YOUSYS_VAULT_URL=https://localhost:8200
export YOUSYS_VAULT_ROLE_ID=your-role-id
export YOUSYS_VAULT_SECRET_ID=your-secret-id
```

c# code for WebApplication
```c#
var builder = WebApplication.CreateBuilder(args);


// config setup from other sources

builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("cfg/settings.json", optional: false, reloadOnChange: true)
    .AddJsonFile("cfg/serilog.json", optional: false, reloadOnChange: true);


//configure named IHttpClientFactory
builder.Services.AddHttpClient("VaultClient").ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    // Example: Disable cookie handling
    UseCookies = false,
    // Example: Customize server certificate validation (use with caution in production)
    ServerCertificateCustomValidationCallback = (request, cert, chain, errors) =>
    {
        var caCertificate = new X509Certificate2(Path.Combine(Directory.GetCurrentDirectory(), "cfg", "ca.crt"));
        if (cert == null)
            return false;

        var serverCert = new X509Certificate2(cert);

        var customChain = new X509Chain();
        customChain.ChainPolicy.ExtraStore.Add(caCertificate);
        customChain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
        customChain.ChainPolicy.VerificationFlags =
            X509VerificationFlags.AllowUnknownCertificateAuthority;

        var isValid = customChain.Build(serverCert);

        if (!isValid)
        {
            foreach (var status in customChain.ChainStatus)
                Console.WriteLine($"Chain error: {status.StatusInformation}");
        }

        return isValid;
    },
    
});
//use extension
builder.AddMHVaultConfig();
```
c# code for IHostBuilder
```
 public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
               .ConfigureLogging((ctx, cfg) => cfg.ClearProviders())
               .ConfigureHostConfiguration(cfg => {
                   cfg.SetBasePath(Directory.GetCurrentDirectory());
                   cfg.AddJsonFile(Path.Combine(AppContext.BaseDirectory, "cfg", "settings.json"), optional: false, reloadOnChange: true);
                })
               .AddMHVaultConfigOnce(skipSSL:true)
               ...
```

## HashiCorp vault secret example

```
{
  "ApiSettings": {
    "ApiKey": "SuperSecretToken123"
  },
  "Connections": {
    "main_pg": {
      "ConnectionString": "Host=,pg.local;Port=5432;Database=finshop;Username=apiuser;Password=pwd32344",
      "DbType": "postgres",
      "TimeOutSec": 30
    },
    "react": {
      "ConnectionString": "TrustServerCertificate=True;workstation id=SUTOR;packet size=4096;data source=ms.local;MultipleActiveResultSets=true;Persist Security Info=False;user id=sqluser;initial catalog=Db1;password=,drtbtrmw",
      "DbType": "mssql",
      "TimeOutSec": 60
    }
  }
}
```