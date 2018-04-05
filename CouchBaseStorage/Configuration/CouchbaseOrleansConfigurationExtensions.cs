using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
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

            var exceptions = new List<InvalidExpiryValueException>();

            foreach (GrainExpiry documentExpiry in grainExpiriesConfig.GrainExpiries)
            {
                if (string.IsNullOrWhiteSpace(documentExpiry.GrainType) || result.ContainsKey(documentExpiry.GrainType)) continue;

                if (!TimeSpan.TryParse(documentExpiry.Expiry, out TimeSpan expiry))
                {
                    exceptions.Add(new InvalidExpiryValueException($"Expiry value of {documentExpiry.Expiry} for grain type {documentExpiry.GrainType} is not in the correct format"));
                    continue;
                }

                result.Add(documentExpiry.GrainType, expiry);
            }

            if (exceptions.Any())
            {
                var message = new StringBuilder();
                message.AppendLine($"{exceptions.Count} document expiry values are invalid. Please see inner exceptions for details then check your config file.");
                message.AppendLine();
                message.AppendLine("Valid examples include:");
                message.AppendLine($"10 seconds: {TimeSpan.FromSeconds(10)}");
                message.AppendLine($"10 minutes: {TimeSpan.FromMinutes(10)}");
                message.AppendLine($"10 hours: {TimeSpan.FromHours(10)}");
                message.AppendLine($"10 days: {TimeSpan.FromDays(10)}");

                throw new AggregateException(message.ToString(), exceptions);
            }

            return result;
        }
    }
}
