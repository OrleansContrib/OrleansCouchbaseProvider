using System.Configuration;

namespace CouchBaseProviders.Configuration.OrleansDocumentExpiry
{
    public class GrainExpiry : ConfigurationElement
    {
        [ConfigurationProperty(OrleansGrainExpiryConstants.GrainTypePropertyName, IsRequired = true, IsKey = true)]
        public string GrainType
        {
            get { return (string) this[OrleansGrainExpiryConstants.GrainTypePropertyName]; }
            set { this[OrleansGrainExpiryConstants.GrainTypePropertyName] = value; }
        }

        [ConfigurationProperty(OrleansGrainExpiryConstants.ExpiryPropertyName, IsRequired = true)]
        public string Expiry
        {
            get { return (string) this[OrleansGrainExpiryConstants.ExpiryPropertyName]; }
            set { this[OrleansGrainExpiryConstants.ExpiryPropertyName] = value; }
        }
    }
}
