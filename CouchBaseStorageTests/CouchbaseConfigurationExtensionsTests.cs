using System;
using CouchBaseProviders.Configuration;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Xunit;

namespace CouchBaseStorageTests
{
    public class CouchbaseConfigurationExtensionsTests
    {
        [Fact]
        public void ReadsPropertiesParsesClientConfigurationSectionTest()
        {
            // Arrange
            var properties = 
                new Dictionary<string, string>()
                {
                    {"ClientConfigurationSectionPath", "couchbaseClients/couchbase"},
                    {"BucketName", "orleans-storage"}
                };

            string storageBucketName = null;

            // Act
            var clientConfiguration = properties.ReadCouchbaseConfiguration(out storageBucketName);

            // Assert
            Assert.Equal(3, clientConfiguration.Servers.Count);
            Assert.Equal(new Uri("http://couchnode02:8091"), clientConfiguration.Servers.ElementAt(1));
            Assert.Equal(1, clientConfiguration.BucketConfigs.Count);
            Assert.Equal("orleans-storage", storageBucketName);
            Assert.Equal(storageBucketName, clientConfiguration.BucketConfigs.ElementAt(0).Key);
        }

        [Fact]
        public void ReadsPropertiesParsesSingleNodeConfigurationTest()
        {
            // Arrange
            const string singleNodeUri = "http://couchnode02:8091";
            var properties =
                new Dictionary<string, string>()
                {
                    {"Servers", singleNodeUri},
                    {"BucketName", "orleans-storage"}
                };

            string storageBucketName = null;
            // Act
            var clientConfiguration = properties.ReadCouchbaseConfiguration(out storageBucketName);

            // Assert
            Assert.Equal(1, clientConfiguration.Servers.Count);
            Assert.Equal(new Uri(singleNodeUri), clientConfiguration.Servers.ElementAt(0));
        }

        [Fact]
        public void ReadsPropertiesParsesWithMultipleNodeConfigurationTest()
        {
            // Arrange
            const string multipleNodeUris =
                "http://couchnode01:8091,http://couchnode02:8091,http://couchnode03:8091";

            var properties =
                new Dictionary<string, string>()
                {
                    {"Servers", multipleNodeUris},
                    {"BucketName", "orleans-storage"}
                };

            string storageBucketName = null;

            // Act
            var clientConfiguration = properties.ReadCouchbaseConfiguration(out storageBucketName);

            // Assert
            Assert.Equal(3, clientConfiguration.Servers.Count);
            Assert.Equal(new Uri("http://couchnode02:8091"), clientConfiguration.Servers.ElementAt(1));
        }

        [Fact]
        public void ReadsPropertiesParsesBucketCredentialsConfigurationTest()
        {
            // Arrange
            const string userName = "bucketUser";
            const string password = "bucketPassword";

            var properties =
                new Dictionary<string, string>()
                {
                    {"Servers", "http://couchnode01:8091"},
                    {"BucketName", "orleans-storage"},
                    {"UserName", userName},
                    {"Password", password}
                };

            string storageBucketName = null;

            // Act
            var clientConfiguration = properties.ReadCouchbaseConfiguration(out storageBucketName);

            // Assert
            Assert.Equal(1, clientConfiguration.BucketConfigs.Count);
            var bucketConfig = clientConfiguration.BucketConfigs.Single();
            Assert.Equal(userName, bucketConfig.Value.Username);
            Assert.Equal(password, bucketConfig.Value.Password);
        }

        [Fact]
        public void ReadsPropertiesFailsWithInexistentClientConfigurationSectionNameTest()
        {
            // Arrange
            const string badSectionName = "couchbaseClients/inexistent";
            var properties =
                new Dictionary<string, string>()
                {
                    {"ClientConfigurationSectionPath", badSectionName},
                    {"BucketName", "orleans-storage"}
                };

            string storageBucketName = null;

            // Act / Assert
            var configurationErrorsException =
                Assert.Throws<ConfigurationErrorsException>(
                    () => properties.ReadCouchbaseConfiguration(out storageBucketName));
            Assert.Equal(string.Format("Section '{0}' has not been configured.", badSectionName),
                configurationErrorsException.Message);
        }

        [Fact]
        public void ReadPropertiesFailsWhenMissingBucketNameConfigurationTest()
        {
            // Arrange
            var properties =
                new Dictionary<string, string>()
                {
                    {"ClientConfigurationSectionPath", "couchbaseClients/couchbase"}
                };

            string storageBucketName = null;

            // Act / Assert
            var configurationErrorsException =
                Assert.Throws<ConfigurationErrorsException>(
                    () => properties.ReadCouchbaseConfiguration(out storageBucketName));
            Assert.Equal("BucketName property not set.", configurationErrorsException.Message);
        }
        
        [Fact]
        public void ReadPropertiesFailsWhenMissingClientConfigurationSectionAndServersTest()
        {
            // Arrange
            var properties =
                new Dictionary<string, string>()
                {
                    {"BucketName", "orleans-storage"}
                };

            string storageBucketName = null;

            // Act / Assert
            var configurationErrorsException =
                Assert.Throws<ConfigurationErrorsException>(
                    () => properties.ReadCouchbaseConfiguration(out storageBucketName));
            Assert.Equal("Servers property not set.", configurationErrorsException.Message);
        }
    }
}
