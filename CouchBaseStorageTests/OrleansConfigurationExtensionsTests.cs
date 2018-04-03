using System;
using CouchBaseProviders.Configuration;
using Xunit;

namespace CouchBaseStorageTests
{
    public class OrleansConfigurationExtensionsTests
    {
        [Fact]
        public void ReadOrleansConfigurationSectionTest()
        {
            // Act
            var documentExpiryConfiguration = OrleansConfigurationExtensions.ReadDocumentExpiryConfiguration();

            // Assert
            Assert.Equal(3, documentExpiryConfiguration.Count);
            Assert.Contains(documentExpiryConfiguration, pair => pair.Key == "grainX" && pair.Value == TimeSpan.FromMinutes(1));
            Assert.Contains(documentExpiryConfiguration, pair => pair.Key == "grainY" && pair.Value == TimeSpan.FromHours(1));
            Assert.Contains(documentExpiryConfiguration, pair => pair.Key == "grainZ" && pair.Value == TimeSpan.FromDays(1));
        }
    }
}
