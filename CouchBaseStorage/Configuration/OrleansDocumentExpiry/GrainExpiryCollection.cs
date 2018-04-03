using System;
using System.Configuration;

namespace CouchBaseProviders.Configuration.OrleansDocumentExpiry
{
    [ConfigurationCollection(typeof(GrainExpiry))]
    public class GrainExpiryCollection : ConfigurationElementCollection
    {
        private const string PropertyName = "add";

        public override ConfigurationElementCollectionType CollectionType => ConfigurationElementCollectionType.BasicMapAlternate;

        protected override string ElementName => PropertyName;

        protected override bool IsElementName(string elementName)
        {
            return elementName.Equals(PropertyName, StringComparison.InvariantCultureIgnoreCase);
        }

        public override bool IsReadOnly()
        {
            return false;
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new GrainExpiry();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((GrainExpiry)element).GrainType;
        }

        public GrainExpiry this[int idx] => (GrainExpiry)BaseGet(idx);
    }
}
