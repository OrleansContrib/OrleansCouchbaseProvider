using System.Configuration;

namespace CouchBaseProviders.Configuration.CouchbaseOrleansDocumentExpiry
{
    public class CouchbaseOrleansConfigurationSection : ConfigurationSection
    {
        [ConfigurationProperty(CouchbaseOrleansGrainExpiryConstants.GrainExpiryCollectionName, IsDefaultCollection = false)]
        public GrainExpiryCollection GrainExpiries => (GrainExpiryCollection) base[CouchbaseOrleansGrainExpiryConstants.GrainExpiryCollectionName];
    }
}
