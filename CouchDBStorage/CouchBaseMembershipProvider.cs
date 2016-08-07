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

namespace Orleans.Storage
{
    public class CouchBaseMembershipProvider : IMembershipTable
    {
        private IBucket bucket;
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
            manager = new MembershipDataManager("membership",null);
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
            return manager.UpdateRow(entry, tableVersion,etag);
        }
    }

    public class CouchBaseGatewayListProvider : IGatewayListProvider
    {
        public bool IsUpdatable { get { return true; } }
        private TimeSpan refreshRate;

        public TimeSpan MaxStaleness
        {
            get
            {
                return refreshRate;
            }
        }

        public async Task<IList<Uri>> GetGateways()
        {
            BucketContext b = new BucketContext(ClusterHelper.GetBucket("membership"));
            var result = await b.Query<MembershipEntry>()
                .Where(s => s.Status == SiloStatus.Active && s.ProxyPort != 0)
                .ExecuteAsync();
            var r = result.ToList().Select(x =>
            {
                x.SiloAddress.Endpoint.Port = x.ProxyPort;
                return x.SiloAddress.ToGatewayUri();
            })
                .ToList();
            return r;
        }

        public Task InitializeGatewayListProvider(ClientConfiguration clientConfiguration, TraceLogger traceLogger)
        {
            refreshRate = clientConfiguration.GatewayListRefreshPeriod;
            return TaskDone.Done;
        }
    }

    public class MembershipDataManager : CouchBaseDataManager
    {
        private readonly TableVersion tableVersion = new TableVersion(0, "0");

        public MembershipDataManager(string bucketName, Couchbase.Configuration.Client.ClientConfiguration config) : base(bucketName, config)
        {
            
        }

        
        public async Task<bool> InsertRow(MembershipEntry entry,TableVersion tableVersion)
        {
            try
            {
                string json = JsonConvert.SerializeObject(entry);
                var result = await bucket.InsertAsync<string>(entry.SiloAddress.ToParsableString(), json);
                return result.Success;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UpdateRow(MembershipEntry entry, TableVersion tableVersion,string eTag)
        {
            try
            {
                string json = JsonConvert.SerializeObject(entry);
                var result = await bucket.UpsertAsync<string>(entry.SiloAddress.ToParsableString(), json,ulong.Parse(eTag));
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
            var exp = b.Query<MembershipEntry>().Select(x => Tuple.Create<MembershipEntry, string>(x, N1QlFunctions.Meta(bucket.Name).Cas.ToString()));
            var result = await exp.ExecuteAsync();
            List<Tuple<MembershipEntry, string>> entries = new List<Tuple<MembershipEntry, string>>();
            foreach (var r in result)
            {
                entries.Add(Tuple.Create(r.Item1, r.Item2));
            }
            return new MembershipTableData(entries, new TableVersion(0, "0"));
        }

        public async Task DeleteMembershipTableEntries(string deploymentId)
        {
            BucketContext db = new BucketContext(bucket);
            var results = await db.Query<MembershipEntry>().ExecuteAsync();
            db.BeginChangeTracking();
            var r = results.ToList();
            r.Clear();
            results = r.AsEnumerable();
            db.EndChangeTracking();
        }

        public async Task<MembershipTableData> ReadRow(SiloAddress key)
        {
            BucketContext b = new BucketContext(bucket);
            var exp = b.Query<MembershipEntry>().Where(x => x.SiloAddress.Endpoint == key.Endpoint && x.SiloAddress.Generation == key.Generation)
                .Select(x => Tuple.Create<MembershipEntry, string>(x, N1QlFunctions.Meta(bucket.Name).Cas.ToString()));
            var result = await exp.ExecuteAsync();
            List<Tuple<MembershipEntry, string>> entries = new List<Tuple<MembershipEntry, string>>();
            foreach (var r in result)
            {
                entries.Add(Tuple.Create(r.Item1, r.Item2));
            }
            return new MembershipTableData(entries, new TableVersion(0, "0"));
        }

        public async Task UpdateIAmAlive(MembershipEntry entry)
        {
            var data = await bucket.GetAsync<string>(entry.SiloAddress.ToParsableString());
            MembershipEntry readEntry=new MembershipEntry();
            JsonConvert.PopulateObject(data.Value, readEntry);
            readEntry.IAmAliveTime = entry.IAmAliveTime;
            var json = JsonConvert.SerializeObject(readEntry);
            await bucket.UpsertAsync<string>(readEntry.SiloAddress.ToParsableString(), json);
        }
    }

    

}