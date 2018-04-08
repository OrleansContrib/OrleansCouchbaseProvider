using System;
using CouchBaseProviders.Configuration;
using Xunit;

namespace CouchBaseStorageTests
{
    public class CouchbaseOrleansConfigurationExtensionsTests
    {
        [Fact]
        public void ReadOrleansConfigurationSectionTest()
        {
            // Act
            var documentExpiryConfiguration = CouchbaseOrleansConfigurationExtensions.GetGrainExpiries();

            // Assert
            Assert.Equal(3, documentExpiryConfiguration.Count);
            Assert.Contains(documentExpiryConfiguration, pair => pair.Key == "grainX" && pair.Value == TimeSpan.FromMinutes(1));
            Assert.Contains(documentExpiryConfiguration, pair => pair.Key == "grainY" && pair.Value == TimeSpan.FromHours(1));
            Assert.Contains(documentExpiryConfiguration, pair => pair.Key == "grainZ" && pair.Value == TimeSpan.FromDays(1));
        }

        [Fact]
        public void ReadOrleansConfigurationSectionTestWithIncorrectPath()
        {
            // Arrange
            var invalidConfigPath = "invalidPath";

            // Act
            var documentExpiryConfiguration = CouchbaseOrleansConfigurationExtensions.GetGrainExpiries(invalidConfigPath);

            // Assert
            Assert.Equal(0, documentExpiryConfiguration.Count);
        }
    }
}
