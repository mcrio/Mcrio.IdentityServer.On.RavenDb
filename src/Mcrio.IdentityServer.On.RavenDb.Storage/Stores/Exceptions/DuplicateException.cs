using System;

namespace Mcrio.IdentityServer.On.RavenDb.Storage.Stores.Exceptions
{
    /// <summary>
    /// Duplicate item exception thrown when item already exists.
    /// </summary>
    public class DuplicateException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DuplicateException"/> class.
        /// </summary>
        /// <param name="message">Optional exception message.</param>
        public DuplicateException(string? message = null)
            : base(message ?? "Item already exists.")
        {
        }
    }
}