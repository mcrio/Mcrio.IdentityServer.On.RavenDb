namespace Mcrio.IdentityServer.On.RavenDb.Storage.Entities
{
    /// <summary>
    /// Defines a generalized entity that must define an Id property.
    /// </summary>
    public interface IEntity
    {
        /// <summary>
        /// Gets the entity Id value.
        /// </summary>
        string Id { get; }
    }
}