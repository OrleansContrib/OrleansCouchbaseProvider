using System;
using System.Collections.Generic;
using System.Configuration;
using CouchBaseProviders.Configuration.OrleansDocumentExpiry;

namespace CouchBaseProviders.Configuration
{
    /// <summary>
    /// Orleans configuration utility extension methods.
    /// </summary>
    public static class OrleansConfigurationExtensions
    {
        public static Dictionary<string, TimeSpan> ReadDocumentExpiryConfiguration()
        {
            var result = new Dictionary<string, TimeSpan>();

            if (string.IsNullOrWhiteSpace(OrleansGrainExpiryConstants.OrleansConfigurationSectionPath)) return result;

            var grainExpiriesConfig = (OrleansConfigurationSection)ConfigurationManager.GetSection(OrleansGrainExpiryConstants.OrleansConfigurationSectionPath);
            if (grainExpiriesConfig == null || grainExpiriesConfig.GrainExpiries.Count == 0) return result;

            foreach (GrainExpiry documentExpiry in grainExpiriesConfig.GrainExpiries)
            {
                var expiry = TimeSpan.Parse(documentExpiry.Expiry);
                result.Add(documentExpiry.GrainType, expiry);
            }

            return result;
        }
    }
}
