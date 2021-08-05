<img src="https://github.com/mcrio/Mcrio.IdentityServer.On.RavenDb/raw/master/ravendb-logo.png" height="100px" alt="RavenDB" />
<img src="https://github.com/mcrio/Mcrio.IdentityServer.On.RavenDb/raw/master/identity-server-logo.pngg" height="150px" alt="IdentityServer4" />

# IdentityServer4 on RavenDB

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

Using the NuGet package manager install the [Mcrio.IdentityServer.On.RavenDb](#) package, or add the following line to the .csproj file:

```xml
<ItemGroup>
    <PackageReference Include="Mcrio.IdentityServer.On.RavenDb"></PackageReference>
</ItemGroup>
```
 
This package contains extension methods which allow easy setup of RavenDB stores
with IdentityServer4.

If you want to reference the stores implementations package only, please check
NuGet package [Mcrio.IdentityServer.On.RavenDb.Storage](#).

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
            /* If true enabled token cleanup background server */
            /* Suggested way is to set to false and to go with the RavenDB auto cleanup of expired documents */
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