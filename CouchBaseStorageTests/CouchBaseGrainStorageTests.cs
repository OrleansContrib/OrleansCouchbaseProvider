using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Couchbase;
using Orleans.Storage;
using Orleans.TestingHost;
using Orleans;
using TestGrains;

namespace CouchBaseStorageTests
{
    public class CouchBaseGrainStorageTests : IClassFixture<CouchBaseGrainStorageTests.CouchBaseGrainStorageFixture>
    {
        public class CouchBaseGrainStorageFixture : IDisposable
        {
            public TestCluster hostedCluster;

            private void AdjustConfig(Orleans.Runtime.Configuration.ClusterConfiguration c)
            {
                c.Globals.RegisterStorageProvider<Orleans.Storage.OrleansCouchBaseStorage>("Default",
                    new Dictionary<string, string>()
                            {
                                { "Server","http://localhost:8091" },
                                { "UserName","" },
                                { "Password","" },
                                { "BucketName","default" }
                            });
                
                
            }

            public CouchBaseGrainStorageFixture()
            {
                GrainClient.Uninitialize();
                TestClusterOptions o = new TestClusterOptions(2);
                AdjustConfig(o.ClusterConfiguration);
                
                hostedCluster = new TestCluster(o);

                if (hostedCluster.Primary == null)
                    hostedCluster.Deploy();

            }

            public void Dispose()
            {
                hostedCluster.StopAllSilos();
            }
        }

        private TestCluster host;

        public CouchBaseGrainStorageTests(CouchBaseGrainStorageTests.CouchBaseGrainStorageFixture fixture)
        {
            host = fixture.hostedCluster;
        }

        [Fact]
        public async Task StartSiloWithCouchBaseStorage()
        {
            var id = Guid.NewGuid();
            var grain = host.GrainFactory.GetGrain<ICouchBaseStorageGrain>(id);
            var first = await grain.GetValue();
            Assert.Equal(0, first);
            await grain.Write(3);
            Assert.Equal(3, await grain.GetValue());
            await grain.Delete();
            Assert.Equal(0, await grain.GetValue());
        }

        [Fact]
        public async Task StoresGrainStateWithReferencedGrainTest()
        {
            var grainId = Guid.NewGuid();
            var grain = this.host.GrainFactory.GetGrain<ICouchBaseWithGrainReferenceStorageGrain>(grainId);

            // Request grain to reference another grain
            var referenceTag = $"Referenced by grain {grainId}";
            await grain.ReferenceOtherGrain(referenceTag);

            // Verify referenced grain values
            var retrievedReferenceTag = await grain.GetReferencedTag();
            Assert.Equal(referenceTag, retrievedReferenceTag);
            var retrievedReferencedAt = await grain.GetReferencedAt();
            Assert.NotEqual(default(DateTime), retrievedReferencedAt);

            // Write state
            await grain.Write();

            // Restart all test silos
            var silos = new[] { this.host.Primary }.Union(this.host.SecondarySilos).ToList();
            foreach (var siloHandle in silos)
            {
                this.host.RestartSilo(siloHandle);
            }

            // Re-initialize client
            host.KillClient();
            host.InitializeClient();

            // Revive persisted grain
            var grainPostRestart = this.host.GrainFactory.GetGrain<ICouchBaseWithGrainReferenceStorageGrain>(grainId);

            // Force read persisted state
            await grainPostRestart.Read();

            // Verify persisted state post restart
            var retrievedReferenceTagPostWrite = await grainPostRestart.GetReferencedTag();
            Assert.Equal(referenceTag, retrievedReferenceTagPostWrite);
            var retrievedReferencedAtPostWrite = await grainPostRestart.GetReferencedAt();
            Assert.Equal(retrievedReferencedAt, retrievedReferencedAtPostWrite);
        }
    }


}
