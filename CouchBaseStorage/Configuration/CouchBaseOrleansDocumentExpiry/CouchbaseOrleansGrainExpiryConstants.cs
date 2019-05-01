namespace CouchbaseProviders.Configuration.CouchbaseOrleansDocumentExpiry
{
    public static class CouchbaseOrleansGrainExpiryConstants
    {
        /// <summary>
        /// Path to target <see cref="CouchbaseOrleansConfigurationSection"/> configuration XML element.
        /// </summary>
        public const string CouchbaseOrleansConfigurationSectionPath = "orleans";

        /// <summary>
        /// The configuration element which contains the collection of expiry timespans by grain type.
        /// </summary>
        public const string GrainExpiryCollectionName = "grainExpiry";

        /// <summary>
        /// The name of the configuration attribute where the grain type is provided.
        /// </summary>
        public const string GrainTypePropertyName = "grainType";

        /// <summary>
        /// The name of the expiry attribute where the timespan value is provided.
        /// </summary>
        public const string ExpiryPropertyName = "expiresIn";
    }
}
