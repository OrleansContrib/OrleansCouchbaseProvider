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
    }


}
