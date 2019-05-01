using System;
using System.Collections.Generic;
using System.Configuration;
using Couchbase.Configuration.Client;
using CouchbaseProviders.Configuration;
using Orleans.Providers;

namespace CouchBaseProviders.Configuration
{
    /// <summary>
    /// Couchbase configuration utility extension methods.
    /// </summary>
    public static class CouchbaseConfigurationExtensions
    {
        /// <summary>
        /// Parses Orleans provider configuration into a <see cref="ClientConfiguration"/> instance.
        /// </summary>
        /// <param name="properties">Orleans <see cref="IProviderConfiguration.Properties"/>.</param>
        /// <param name="storageBucketName">Name of bucket configured for grain state document storage.</param>
        /// <returns>Parsed Couchbase client configuration.</returns>
        public static ClientConfiguration ReadCouchbaseConfiguration(this IDictionary<string, string> properties, 
            out string storageBucketName)
        {
            // Determine target data bucket
            storageBucketName = GetPropertyValue(properties, StorageConstants.PropertyNames.BucketName, true);

            // Attempt with Couchbase client configuration section
            var clientSectionName = GetPropertyValue(properties, StorageConstants.PropertyNames.ClientConfigurationSectionPath, false);
            if (!string.IsNullOrWhiteSpace(clientSectionName))
            {
                var clientSection = (CouchbaseClientSection)ConfigurationManager.GetSection(clientSectionName);
                if (clientSection == null)
                {
                    throw new ConfigurationErrorsException(string.Format("Section '{0}' has not been configured.", clientSectionName));
                }
                return new ClientConfiguration(clientSection);
            }

            // Assume provider properties client configuration
            var user = GetPropertyValue(properties, StorageConstants.PropertyNames.UserName, false);
            var password = GetPropertyValue(properties, StorageConstants.PropertyNames.Password, false);
            var servers = GetPropertyValue(properties, StorageConstants.PropertyNames.Server, false) ??
                          GetPropertyValue(properties, StorageConstants.PropertyNames.Servers, true);

            var clientConfiguration = new ClientConfiguration();

            clientConfiguration.Servers.Clear();
            foreach (var s in servers.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                clientConfiguration.Servers.Add(new Uri(s));
            }

            clientConfiguration.BucketConfigs.Clear();
            clientConfiguration.BucketConfigs.Add(storageBucketName, new Couchbase.Configuration.Client.BucketConfiguration
            {
                BucketName = storageBucketName,
                Username = user,
                Password = password
            });

            return clientConfiguration;
        }

        private static string GetPropertyValue(IDictionary<string, string> properties, string key, bool isRequired)
        {
            string value = null;
            if (!properties.TryGetValue(key, out value) && isRequired && string.IsNullOrWhiteSpace(value))
            {
                throw new ConfigurationErrorsException(string.Format("{0} property not set.", key));
            }

            return value;
        }
    }
}