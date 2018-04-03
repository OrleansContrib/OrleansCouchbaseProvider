using System.Configuration;

namespace CouchBaseProviders.Configuration.OrleansDocumentExpiry
{
    public class OrleansConfigurationSection : ConfigurationSection
    {
        [ConfigurationProperty(OrleansGrainExpiryConstants.GrainExpiryCollectionName, IsDefaultCollection = false)]
        public GrainExpiryCollection GrainExpiries => (GrainExpiryCollection) base[OrleansGrainExpiryConstants.GrainExpiryCollectionName];
    }
}
