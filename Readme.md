<img src="https://github.com/mcrio/Mcrio.IdentityServer.On.RavenDb/raw/master/ravendb-logo.png" height="100px" alt="RavenDB" />
<img src="https://github.com/mcrio/Mcrio.IdentityServer.On.RavenDb/raw/master/identity-server-logo.png" height="150px" alt="IdentityServer4" />

# IdentityServer4 on RavenDB

[![Build status](https://dev.azure.com/midnight-creative/Mcrio.IdentityServer.On.RavenDb/_apis/build/status/Build)](https://dev.azure.com/midnight-creative/Mcrio.IdentityServer.On.RavenDb/_build/latest?definitionId=-1)
![Nuget](https://img.shields.io/nuget/v/Mcrio.IdentityServer.On.RavenDb)
![Nuget](https://img.shields.io/nuget/v/Mcrio.IdentityServer.On.RavenDb.Storage)

A RavenDB copy of the original EntityFramework IdentityServer4 store implementations.
Use RavenDB to store IdentityServer4 related entities.
Covers most of the tests implemented by the official EntityFramework stores.

## Getting Started

### Sample Projects

- **IdentityServer** / Identity server with ASP.net Core Identity on RavenDB using the `Mcrio.AspNetCore.Identity.On.RavenDb` package.

- **MyApi** / API with an endpoint that requires authentication.

- **ConsoleClient** / Console application using client credentials flow to access the secured MyApi endpoint.

- **MvcClient** / MVC client using the code flow to authenticate, get the tokens and call the secured MyApi endpoint.

*Note: If you change the current application urls and ports make sure to reflect the change in code, otherwise clients may not be able to connect.*

### Try the sample applications

```text
1. CD into the solution directory

2. Start the RavenDB docker container (use flag -d to start in background)
   $ docker-compose up

3. Start IdentityServer + protected API
   $ dotnet run -p sample/Mcrio.IdentityServer.On.RavenDb.Sample.IdentityServer/Mcrio.IdentityServer.On.RavenDb.Sample.IdentityServer.csproj
   $ dotnet run -p sample/Mcrio.IdentityServer.On.RavenDb.Sample.MyApi/Mcrio.IdentityServer.On.RavenDb.Sample.MyApi.csproj

3. MVC sample
   $ dotnet run -p sample/Mcrio.IdentityServer.On.RavenDb.Sample.MvcClient/Mcrio.IdentityServer.On.RavenDb.Sample.MvcClient.csproj

   Open in browser: https://localhost:5021
   That will open the MVC app that wants to try to connect to a protected API.

5. Console application Sample
   Obtain access token and retrieve data from a protected API.
   $ dotnet run -p sample/Mcrio.IdentityServer.On.RavenDb.Sample.ConsoleClient/Mcrio.IdentityServer.On.RavenDb.Sample.ConsoleClient.csproj
   
6. Device Flow Sample
   Authorize a device and retrieve data from a protected API.
   $ dotnet run -p sample/Mcrio.IdentityServer.On.RavenDb.Sample.DeviceFlowClient/Mcrio.IdentityServer.On.RavenDb.Sample.DeviceFlowClient.csproj

// RavenDB Studio is available at: http://localhost:32779
```

### NuGet Package

Using the NuGet package manager install the [Mcrio.IdentityServer.On.RavenDb](https://www.nuget.org/packages/Mcrio.IdentityServer.On.RavenDb/) package, or add the following line to the .csproj file:

```xml
<ItemGroup>
    <PackageReference Include="Mcrio.IdentityServer.On.RavenDb"></PackageReference>
</ItemGroup>
```
 
This package contains extension methods which allow easy setup of RavenDB stores
with IdentityServer4.

If you want to reference the stores implementations package only, please check
NuGet package [Mcrio.IdentityServer.On.RavenDb.Storage](https://www.nuget.org/packages/Mcrio.IdentityServer.On.RavenDb.Storage/).

## Usage

Please refer to sample projects for working examples.

### Simple usage

Add the following lines to Startup.cs.
```c# 
// ConfigureServices(...)
services
    // adds IdentityServer as per IDS4 documentation
    .AddIdentityServer()
    // adds RavenDbStores
    .AddRavenDbStores(
        // define how IAsyncDocumentSession is resolved from DI
        // as library does NOT directly inject IAsyncDocumentSession
        serviceProvider => serviceProvider.GetRequiredService<IAsyncDocumentSession>(),
        // define how IDocumentStore is resolved from DI
        // as library does NOT directly inject IAsyncDocumentSession
        serviceProvider => serviceProvider.GetRequiredService<IDocumentStore>(),
        // retrieve OperationalStoreOptions from configuration
        operationalStoreOptions => Configuration
            .GetSection("OperationalStoreOptions")
            .Bind(operationalStoreOptions),
        // IDS4 options as documented in official documentation
        addOperationalStore: true,
        addConfigurationStore: true,
        addConfigurationStoreCache: true
    )
    // ASP.Net identity on RavenDb. See NuGet Mcrio.AspNetCore.Identity.On.RavenDb
    .AddAspNetIdentity<RavenIdentityUser>()
    // as per IDS4 documentation
    .AddDeveloperSigningCredential();
```

Add the following configuration to `appsettings.json`:
```json5
{
    "OperationalStoreOptions": {
        /* If true sets expires metadata so that we can use RavenDB auto cleanup functionality for expired documents */
        "SetRavenDbDocumentExpiresMetadata": true,
        "TokenCleanup": {
            /* If true enables token cleanup background service */
            /* Suggested way is to false and to go with the RavenDB auto cleanup of expired documents */
            "EnableTokenCleanupBackgroundService": false,
            "CleanupIntervalSec": 60,
            "CleanupStartupDelaySec": 30,
            "DeleteByQueryMaxOperationsPerSecond": 1024
        }
    }
}
```

### Compare Exchange key prefixes

Extend DeviceFlowStore and override `protected virtual CompareExchangeUtility CreateCompareExchangeUtility()` to return
an extended `CompareExchangeUtility` that will override the functionality for generating
compare exchange key prefixes. See `CompareExchangeUtility.GetKeyPrefix` for predefined compare exchange key prefixes.

### Multi-tenant guidelines

- Extend `DeviceFlowCode` and `PersistedGrant` to include a `TenantId` property
- Extend `DeviceFlowstore` 
  - so it returns an extended `CompareExchangeUtility` which 
  includes the tenant identifier in the compare exchange prefixes
  - Override `StoreDeviceAuthorizationAsync` to assign Tenant ID to device entity
- Extend `PersistedGrantStore`:
  - Override `StoreAsync` to assign Tenant ID to Persisted Grant entity
  - Override `CheckRequiredFields` to make sure the persisted entity has Tenant ID set
  

## Release History

- **1.0.0**
  Stable version.

## Meta

Nikola Josipović

This project is licensed under the MIT License. See [License.md](License.md) for more information.

## Do you like this library?

<img src="https://img.shields.io/badge/%E2%82%B3%20%2F%20ADA-Buy%20me%20a%20coffee%20or%20two%20%3A)-green" alt="₳ ADA | Buy me a coffee or two :)" /> <br /><small> addr1q87dhpq4wkm5gucymxkwcatu2et5enl9z8dal4c0fj98fxznraxyxtx5lf597gunnxn3tewwr6x2y588ttdkdlgaz79spp3avz </small><br />

<img src="https://img.shields.io/badge/%CE%9E%20%2F%20ETH-...a%20nice%20cold%20beer%20%3A)-yellowgreen" alt="Ξ ETH | ...a nice cold beer :)" /> <br /> <small> 0xae0B28c1fCb707e1908706aAd65156b61aC6Ff0A </small><br />

<img src="https://img.shields.io/badge/%E0%B8%BF%20%2F%20BTC-...or%20maybe%20a%20good%20read%20%3A)-yellow" alt="฿ BTC | ...or maybe a good read :)" /> <br /> <small> bc1q3s8qjx59f4wu7tvz7qj9qx8w6ktcje5ktseq68 </small><br />

<img src="https://img.shields.io/badge/ADA%20POOL-Happy if you %20stake%20%E2%82%B3%20with%20Pale%20Blue%20Dot%20%5BPBD%5D%20%3A)-8a8a8a" alt="Happy if you stake ADA with Pale Blue Dot [PBD]" /> <br /> <small> <a href="https://palebluedotpool.org">https://palebluedotpool.org</a> </small>
<br />&nbsp;