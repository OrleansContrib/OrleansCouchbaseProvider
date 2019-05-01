using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using CouchbaseProviders.Configuration.CouchbaseOrleansDocumentExpiry;

namespace CouchbaseProviders.Configuration
{
    /// <summary>
    /// Orleans configuration utility extension methods.
    /// </summary>
    public static class CouchbaseOrleansConfigurationExtensions
    {
        public static Dictionary<string, TimeSpan> GetGrainExpiries(string couchbaseOrleansConfigurationSectionPath = CouchbaseOrleansGrainExpiryConstants.CouchbaseOrleansConfigurationSectionPath)
        {
            var result = new Dictionary<string, TimeSpan>();

            if (string.IsNullOrWhiteSpace(couchbaseOrleansConfigurationSectionPath)) return result;

            var grainExpiriesConfig = (CouchbaseOrleansConfigurationSection)ConfigurationManager.GetSection(couchbaseOrleansConfigurationSectionPath);
            if (grainExpiriesConfig == null || grainExpiriesConfig.GrainExpiries.Count == 0) return result;

            var grainTypesWithInvalidTimespan = new List<string>();

            foreach (GrainExpiry documentExpiry in grainExpiriesConfig.GrainExpiries)
            {
                if (string.IsNullOrWhiteSpace(documentExpiry.GrainType) || result.ContainsKey(documentExpiry.GrainType)) continue;

                TimeSpan expiry;
                if (!TimeSpan.TryParse(documentExpiry.Expiry, out expiry))
                {
                    grainTypesWithInvalidTimespan.Add(documentExpiry.GrainType);
                    continue;
                }

                result.Add(documentExpiry.GrainType, expiry);
            }

            if (grainTypesWithInvalidTimespan.Any())
            {
                throw InvalidExpiryValueException.Generate(grainTypesWithInvalidTimespan);
            }

            return result;
        }
    }
}
