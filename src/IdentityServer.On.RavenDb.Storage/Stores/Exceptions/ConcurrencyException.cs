using System;

namespace Mcrio.IdentityServer.On.RavenDb.Storage.Stores.Exceptions
{
    internal class ConcurrencyException : Exception
    {
        internal ConcurrencyException(string? message = null)
            : base(message ?? "Concurrency exception.")
        {
        }
    }
}