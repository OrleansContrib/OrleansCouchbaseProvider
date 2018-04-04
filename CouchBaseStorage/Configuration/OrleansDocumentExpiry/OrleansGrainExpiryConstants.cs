namespace CouchBaseProviders.Configuration.OrleansDocumentExpiry
{
    public static class OrleansGrainExpiryConstants
    {
        /// <summary>
        /// Path to target <see cref="OrleansConfigurationSection"/> configuration XML element.
        /// </summary>
        public static readonly string OrleansConfigurationSectionPath = "orleans";

        public const string GrainExpiryCollectionName = "grainExpiry";

        public const string GrainTypePropertyName = "grainType";

        public const string ExpiryPropertyName = "timespan";
    }
}
