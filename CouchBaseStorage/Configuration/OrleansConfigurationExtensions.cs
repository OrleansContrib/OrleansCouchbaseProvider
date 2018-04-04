using System;
using System.Collections.Generic;
using System.Configuration;
using CouchBaseProviders.Configuration.CouchbaseOrleansDocumentExpiry;

namespace CouchBaseProviders.Configuration
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

            foreach (GrainExpiry documentExpiry in grainExpiriesConfig.GrainExpiries)
            {
                if (string.IsNullOrWhiteSpace(documentExpiry.GrainType) || result.ContainsKey(documentExpiry.GrainType)) continue;

                var expiry = TimeSpan.Parse(documentExpiry.Expiry);
                result.Add(documentExpiry.GrainType, expiry);
            }

            return result;
        }
    }
}
