using Orleans;
using Orleans.Messaging;
using Orleans.Runtime.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Orleans.Runtime;
using Couchbase;
using Couchbase.Core;
using Couchbase.Linq;
using Couchbase.Linq.Extensions;
using Newtonsoft.Json;
using Couchbase.N1QL;
using Newtonsoft.Json.Linq;

namespace Orleans.Storage
{
    public class CouchBaseMembershipProvider : IMembershipTable
    {
        private MembershipDataManager manager;

        public Task DeleteMembershipTableEntries(string deploymentId)
        {
            return manager.DeleteMembershipTableEntries(deploymentId);
        }

        public Task InitializeMembershipTable(GlobalConfiguration globalConfiguration, bool tryInitTableVersion, TraceLogger traceLogger)
        {
            Couchbase.Configuration.Client.ClientConfiguration clientConfig = new Couchbase.Configuration.Client.ClientConfiguration();
            clientConfig.Servers.Clear();
            clientConfig.Servers.Add(new Uri(globalConfiguration.DataConnectionString));
            clientConfig.BucketConfigs.Clear();
            clientConfig.BucketConfigs.Add("membership", new Couchbase.Configuration.Client.BucketConfiguration
            {
                BucketName = "membership",
                Username = "",
                Password = ""
            });
            manager = new MembershipDataManager("membership", clientConfig);
            return TaskDone.Done;
        }

        public Task<bool> InsertRow(MembershipEntry entry, TableVersion tableVersion)
        {
            return manager.InsertRow(entry, tableVersion);
        }

        public Task<MembershipTableData> ReadAll()
        {
            return manager.ReadAll();
        }

        public Task<MembershipTableData> ReadRow(SiloAddress key)
        {
            return manager.ReadRow(key);
        }

        public Task UpdateIAmAlive(MembershipEntry entry)
        {
            return manager.UpdateIAmAlive(entry);
        }

        public Task<bool> UpdateRow(MembershipEntry entry, string etag, TableVersion tableVersion)
        {
            return manager.UpdateRow(entry, tableVersion, etag);
        }
    }

    public class CouchBaseGatewayListProvider : IGatewayListProvider
    {
        public bool IsUpdatable { get { return true; } }
        private TimeSpan refreshRate;
        private MembershipDataManager manager;
        public TimeSpan MaxStaleness
        {
            get
            {
                return refreshRate;
            }
        }

        public Task<IList<Uri>> GetGateways()
        {
            return manager.GetGateWays();
        }



        public Task InitializeGatewayListProvider(ClientConfiguration clientConfiguration, TraceLogger traceLogger)
        {
            Couchbase.Configuration.Client.ClientConfiguration clientConfig = new Couchbase.Configuration.Client.ClientConfiguration();
            clientConfig.Servers.Clear();
            clientConfig.Servers.Add(new Uri(clientConfiguration.DataConnectionString));
            clientConfig.BucketConfigs.Clear();
            clientConfig.BucketConfigs.Add("membership", new Couchbase.Configuration.Client.BucketConfiguration
            {
                BucketName = "membership",
                Username = "",
                Password = ""
            });
            manager = new MembershipDataManager("membership", null);

            refreshRate = clientConfiguration.GatewayListRefreshPeriod;
            return TaskDone.Done;
        }
    }

    public class MembershipDataManager : CouchBaseDataManager
    {
        private readonly TableVersion tableVersion = new TableVersion(0, "0");

        public MembershipDataManager(string bucketName, Couchbase.Configuration.Client.ClientConfiguration config) : base(bucketName, config)
        {
            bucket = ClusterHelper.GetBucket(bucketName);
        }

        #region GatewayProvider
        public async Task<IList<Uri>> GetGateWays()
        {
            BucketContext b = new BucketContext(ClusterHelper.GetBucket("membership"));
            var request = new QueryRequest("select membership.* from membership");
            request.ScanConsistency(ScanConsistency.RequestPlus);
            request.Metrics(false);
            var result = await bucket.QueryAsync<CouchBaseSiloRegistration>(request);

            var r = result.Rows.Select(x => CouchbaseSiloRegistrationmUtility.ToMembershipEntry(x).Item1).Select(x =>
            {
                //EXISTED IN CONSOLE MEMBERSHIP, am not sure why
                //x.SiloAddress.Endpoint.Port = x.ProxyPort; 
                return x.SiloAddress.ToGatewayUri();
            })
                .ToList();
            return r;
        }
        #endregion



        public async Task<bool> InsertRow(MembershipEntry entry, TableVersion tableVersion)
        {
            try
            {
                var serializable = CouchbaseSiloRegistrationmUtility.FromMembershipEntry("", entry, "0");


                var result = await bucket.InsertAsync<CouchBaseSiloRegistration>(entry.SiloAddress.ToParsableString(), serializable);

                return result.Success;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UpdateRow(MembershipEntry entry, TableVersion tableVersion, string eTag)
        {
            try
            {
                var serializable = CouchbaseSiloRegistrationmUtility.FromMembershipEntry("", entry, eTag);
                var result = await  bucket.UpsertAsync<CouchBaseSiloRegistration>(entry.SiloAddress.ToParsableString(), serializable, ulong.Parse(eTag));
                return result.Success;
            }
            catch
            {
                return false;
            }
        }

        public async Task<MembershipTableData> ReadAll()
        {
            BucketContext b = new BucketContext(bucket);
            var request = new QueryRequest("select meta(membership).id from membership");
            request.ScanConsistency(ScanConsistency.RequestPlus);
            request.Metrics(false);
            var exp = await bucket.QueryAsync<JObject>(request);
            
            List<Tuple<MembershipEntry, string>> entries = new List<Tuple<MembershipEntry, string>>();
            foreach (var r in exp.Rows)
            {
                var res = await bucket.GetAsync<CouchBaseSiloRegistration>(r["id"].ToString());
                entries.Add(
                    CouchbaseSiloRegistrationmUtility.ToMembershipEntry(res.Value,res.Cas.ToString()));
            }
            return new MembershipTableData(entries, new TableVersion(0, "0"));
        }


        public async Task DeleteMembershipTableEntries(string deploymentId)
        {
            QueryRequest deleteQuery = new QueryRequest("delete from membership where deploymentId = \"" + deploymentId+"\"");
            deleteQuery.ScanConsistency(ScanConsistency.RequestPlus);
            deleteQuery.Metrics(false);
            var result = await bucket.QueryAsync<MembershipEntry>(deleteQuery);

        }

        public async Task<MembershipTableData> ReadRow(SiloAddress key)
        {
            BucketContext b = new BucketContext(bucket);
            var request = new QueryRequest("select meta(membership).id from membership where serializedAddress = \"" + key.Endpoint.ToString() + "@" + key.Generation+"\"");
            request.Metrics(false);
            request.ScanConsistency(ScanConsistency.RequestPlus);
            var exp = await bucket.QueryAsync<JObject>(request);


            List<Tuple<MembershipEntry, string>> entries = new List<Tuple<MembershipEntry, string>>();
            foreach (var r in exp.Rows)
            {
                var res = await bucket.GetAsync<CouchBaseSiloRegistration>(r["id"].ToString());
                entries.Add(CouchbaseSiloRegistrationmUtility.ToMembershipEntry(res.Value,res.Cas.ToString()));
            }
            return new MembershipTableData(entries, new TableVersion(0, "0"));
        }

        public async Task UpdateIAmAlive(MembershipEntry entry)
        {
            var data = await bucket.GetAsync<CouchBaseSiloRegistration>(entry.SiloAddress.ToParsableString());
            data.Value.IAmAliveTime = entry.IAmAliveTime;
            var address = CouchbaseSiloRegistrationmUtility.ToMembershipEntry(data.Value).Item1.SiloAddress;
            await bucket.UpsertAsync<CouchBaseSiloRegistration>(address.ToParsableString(), data.Value);
        }
    }
}