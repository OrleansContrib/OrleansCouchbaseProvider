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

    public class MembershipDataManager : IDisposable
    {
        private readonly TableVersion tableVersion = new TableVersion(0, "0");
        private IBucket bucket;
        private string bucketName;

        public MembershipDataManager(string bucketName, Couchbase.Configuration.Client.ClientConfiguration clientConfig)
        {

            //Bucket name should not be empty
            //Keep in mind that you should create the buckets before being able to use them either
            //using the commandline tool or the web console
            if (string.IsNullOrWhiteSpace(bucketName))
                throw new ArgumentException("bucketName can not be null or empty");
            //config should not be null either
            if (clientConfig == null)
                throw new ArgumentException("You should suply a configuration to connect to CouchBase");

            this.bucketName = bucketName;
            if (!OrleansCouchBaseStorage.IsInitialized)
            {
                ClusterHelper.Initialize(clientConfig);
                OrleansCouchBaseStorage.IsInitialized = true;
            }
            else
            {
                foreach (var conf in clientConfig.BucketConfigs)
                {
                    ClusterHelper.Get().Configuration.BucketConfigs.Add(conf.Key, conf.Value);
                }
            }
            //cache the bucket.
            bucket = ClusterHelper.GetBucket(this.bucketName);
        }

        #region GatewayProvider
        public async Task<IList<Uri>> GetGateWays()
        {
            BucketContext b = new BucketContext(ClusterHelper.GetBucket("membership"));
            var getGateWaysQuery = new QueryRequest("select membership.* from membership");
            getGateWaysQuery.ScanConsistency(ScanConsistency.RequestPlus);
            getGateWaysQuery.Metrics(false);
            var result = await bucket.QueryAsync<CouchBaseSiloRegistration>(getGateWaysQuery);

            var r = result.Rows.Where(x => x.Status == SiloStatus.Active && x.ProxyPort != 0).Select(x => CouchbaseSiloRegistrationmUtility.ToMembershipEntry(x).Item1).Select(x =>
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
                var result = await bucket.InsertAsync<CouchBaseSiloRegistration>(entry.SiloAddress.ToParsableString(), serializable).ConfigureAwait(false);
                return result.Success;
            }
            catch (Exception x)
            {
                return false;
            }
        }

        public async Task<bool> UpdateRow(MembershipEntry entry, TableVersion tableVersion, string eTag)
        {
            try
            {
                var serializable = CouchbaseSiloRegistrationmUtility.FromMembershipEntry("", entry, eTag);
                var result = await bucket.UpsertAsync<CouchBaseSiloRegistration>(entry.SiloAddress.ToParsableString(), serializable, ulong.Parse(eTag)).ConfigureAwait(false);
                return result.Success;
            }
            catch (Exception x)
            {
                return false;
            }
        }

        public async Task<MembershipTableData> ReadAll()
        {
            BucketContext b = new BucketContext(bucket);
            var readAllQuery = new QueryRequest("select meta(membership).id from membership");
            readAllQuery.ScanConsistency(ScanConsistency.RequestPlus);
            readAllQuery.Metrics(false);
            var ids = await bucket.QueryAsync<JObject>(readAllQuery).ConfigureAwait(false);

            var idStrings = ids.Rows.Select(x => x["id"].ToString()).ToArray();
            var actuals = bucket.Get<CouchBaseSiloRegistration>(idStrings);//has no async version with batch reads
            List<Tuple<MembershipEntry, string>> entries = new List<Tuple<MembershipEntry, string>>();
            foreach (var actualRow in actuals.Values)
            {
                //var actualRow = await bucket.GetAsync<CouchBaseSiloRegistration>(r["id"].ToString());
                entries.Add(
                    CouchbaseSiloRegistrationmUtility.ToMembershipEntry(actualRow.Value, actualRow.Cas.ToString()));
            }
            return new MembershipTableData(entries, new TableVersion(0, "0"));
        }


        public async Task DeleteMembershipTableEntries(string deploymentId)
        {
            
            QueryRequest deleteQuery = new QueryRequest("delete from membership where deploymentId = \"" + deploymentId + "\"");
            deleteQuery.ScanConsistency(ScanConsistency.RequestPlus);
            deleteQuery.Metrics(false);
            var result = await bucket.QueryAsync<MembershipEntry>(deleteQuery).ConfigureAwait(false);

        }

        public async Task<MembershipTableData> ReadRow(SiloAddress key)
        {
            List<Tuple<MembershipEntry, string>> entries = new List<Tuple<MembershipEntry, string>>();
            bool exists = await bucket.ExistsAsync(key.ToParsableString());
            if (exists)
            {
                var row = await bucket.GetAsync<CouchBaseSiloRegistration>(key.ToParsableString()).ConfigureAwait(false);
                entries.Add(CouchbaseSiloRegistrationmUtility.ToMembershipEntry(row.Value, row.Cas.ToString()));
            }
            return new MembershipTableData(entries, new TableVersion(0, "0"));
        }

        public async Task UpdateIAmAlive(MembershipEntry entry)
        {
            var data = await bucket.GetAsync<CouchBaseSiloRegistration>(entry.SiloAddress.ToParsableString());
            data.Value.IAmAliveTime = entry.IAmAliveTime;
            var address = CouchbaseSiloRegistrationmUtility.ToMembershipEntry(data.Value).Item1.SiloAddress;
            await bucket.UpsertAsync<CouchBaseSiloRegistration>(address.ToParsableString(), data.Value).ConfigureAwait(false);
        }


        public void Dispose()
        {
            bucket.Dispose();
            bucket = null;
            //Closes the DB connection
            ClusterHelper.Close();
            OrleansCouchBaseStorage.IsInitialized = false;
        }

    }
}