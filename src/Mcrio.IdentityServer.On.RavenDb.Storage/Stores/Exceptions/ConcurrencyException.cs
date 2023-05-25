using System;

namespace Mcrio.IdentityServer.On.RavenDb.Storage.Stores.Exceptions
{
    /// <summary>
    /// Concurrency exception.
    /// </summary>
    public class ConcurrencyException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConcurrencyException"/> class.
        /// </summary>
        /// <param name="message">Optional exception message.</param>
        public ConcurrencyException(string? message = null)
            : base(message ?? "Concurrency exception.")
        {
        }
    }
}