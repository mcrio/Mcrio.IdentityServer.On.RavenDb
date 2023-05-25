using System;

namespace Mcrio.IdentityServer.On.RavenDb.Storage.Stores.Exceptions
{
    /// <summary>
    /// Reservation document already added to unit of work.
    /// </summary>
    public sealed class ReservationDocumentAlreadyAddedToUnitOfWorkException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReservationDocumentAlreadyAddedToUnitOfWorkException"/> class.
        /// </summary>
        public ReservationDocumentAlreadyAddedToUnitOfWorkException()
            : base("Reservation document addition to the unit of work can be executed only once")
        {
        }
    }
}