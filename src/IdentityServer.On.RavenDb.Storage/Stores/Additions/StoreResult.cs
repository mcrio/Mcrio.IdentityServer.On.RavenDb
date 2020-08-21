using System;

namespace Mcrio.IdentityServer.On.RavenDb.Storage.Stores.Additions
{
    public class StoreResult
    {
        private readonly string? _error;

        private StoreResult(string? error)
        {
            IsSuccess = error is null;
            _error = error;
        }

        public bool IsSuccess { get; }

        public bool IsFailure => !IsSuccess;

        public string Error => IsFailure
            ? _error!
            : throw new InvalidOperationException("Success result must not have an error.");

        public static StoreResult Success() => new StoreResult(null);

        public static StoreResult Failure(string error) => new StoreResult(error);
    }
}