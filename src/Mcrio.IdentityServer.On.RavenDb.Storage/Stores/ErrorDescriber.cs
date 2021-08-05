namespace Mcrio.IdentityServer.On.RavenDb.Storage.Stores
{
    internal static class ErrorDescriber
    {
        internal const string GeneralError = "An error occured. Check your logs.";
        internal const string IdMustNotBeEmpty = "Entity ID must not be empty";
        internal const string EntityNotFound = "Entity not found. ID: {0}";
        internal const string ClientIdMissing = "Client ID is missing.";
        internal const string ProtocolTypeMissing = "Protocol Type is missing.";
        internal const string ConcurrencyException = "Concurrency exception.";
        internal const string IdentityResourceNameMissing = "Identity resource name is missing.";
        internal const string ApiResourceNameMissing = "Api resource name is missing.";
        internal const string ApiScopeNameMissing = "Api scope name is missing.";
        internal const string PersistedGrantKeyMissing = "Persisted grant Key is missing.";
        internal const string PersistedGrantTypeMissing = "Persisted grant Type is missing.";
        internal const string PersistedGrantClientIdMissing = "Persisted grant Type is missing.";
        internal const string PersistedGrantCreationTimeMissing = "Persisted grant Creation Time is missing.";
        internal const string PersistedGrantDataMissing = "Persisted grant Data is missing.";
        internal const string DeviceFlowCodeUserCodeMissing = "Device flow code User Code missing.";
        internal const string DeviceFlowCodeDeviceCodeMissing = "Device flow code Device Code missing.";
        internal const string DeviceFlowCodeClientIdMissing = "Device flow code Client Id missing.";
        internal const string DeviceFlowCodeCreationTimeMissing = "Device flow code Creation Time missing.";
        internal const string DeviceFlowCodeExpirationMissing = "Device flow code Expiration missing.";
        internal const string DeviceFlowCodeDataMissing = "Device flow code Data missing.";
    }
}