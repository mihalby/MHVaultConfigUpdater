# MFSys.MHVaultConfig

`MFSys.MHVaultConfig` is an ASP.NET Core configuration provider that loads application configuration values directly from HashiCorp Vault.
Now using only **AppRole authentication**.

It integrates with `WebApplicationBuilder` and injects keys stored from vault directly into the standard `IConfiguration` pipeline.
---

## Features

- Loads(adding or replace) keys(secrets) from HashiCorp Vault into application configuration
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

c# code
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
