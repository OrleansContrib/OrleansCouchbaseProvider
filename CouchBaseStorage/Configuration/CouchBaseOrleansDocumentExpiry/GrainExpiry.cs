using System.Configuration;

namespace CouchbaseProviders.Configuration.CouchbaseOrleansDocumentExpiry
{
    public class GrainExpiry : ConfigurationElement
    {
        [ConfigurationProperty(CouchbaseOrleansGrainExpiryConstants.GrainTypePropertyName, IsRequired = true, IsKey = true)]
        public string GrainType
        {
            get { return (string) this[CouchbaseOrleansGrainExpiryConstants.GrainTypePropertyName]; }
            set { this[CouchbaseOrleansGrainExpiryConstants.GrainTypePropertyName] = value; }
        }

        [ConfigurationProperty(CouchbaseOrleansGrainExpiryConstants.ExpiryPropertyName, IsRequired = true)]
        public string Expiry
        {
            get { return (string) this[CouchbaseOrleansGrainExpiryConstants.ExpiryPropertyName]; }
            set { this[CouchbaseOrleansGrainExpiryConstants.ExpiryPropertyName] = value; }
        }
    }
}
