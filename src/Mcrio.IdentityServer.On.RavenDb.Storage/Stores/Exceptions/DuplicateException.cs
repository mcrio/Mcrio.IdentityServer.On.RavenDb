using System;

namespace Mcrio.IdentityServer.On.RavenDb.Storage.Stores.Exceptions
{
    internal class DuplicateException : Exception
    {
        internal DuplicateException(string? message = null)
            : base(message ?? "Item already exists.")
        {
        }
    }
}