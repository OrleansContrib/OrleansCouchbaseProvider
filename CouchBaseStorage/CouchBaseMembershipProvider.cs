#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Couchbase;
using Couchbase.Configuration.Client;
using Couchbase.Logging;
using Couchbase.N1QL;
using CouchbaseProviders.CouchbaseComm;
using CouchbaseProviders.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Orleans.Messaging;
using Orleans.Runtime;

#endregion

// ReSharper disable CheckNamespace
// ReSharper disable ClassNeverInstantiated.Global

namespace Orleans.Storage
{
    public class CouchbaseMembershipProvider : IMembershipTable
    {
        private readonly CouchbaseProvidersSettings _couchbaseProvidersSettings;
        public readonly string BucketName;


        private readonly IBucketFactory _bucketFactory;
        private readonly ILogger<CouchbaseMembershipProvider> _logger;
        private MembershipDataManager _manager;
        private readonly ILoggerFactory _loggerFactory;

        public CouchbaseMembershipProvider(ILogger<CouchbaseMembershipProvider> logger, IOptions<CouchbaseProvidersSettings> options, IBucketFactory bucketFactory, ILoggerFactory loggerFactory)
        {
            _logger = logger;
            _couchbaseProvidersSettings = options.Value;
            BucketName = _couchbaseProvidersSettings.MembershipBucketName;
            _bucketFactory = bucketFactory;
            _loggerFactory = loggerFactory;
        }

        public Task DeleteMembershipTableEntries(string deploymentId)
        {
            return _manager.DeleteMembershipTableEntries(deploymentId);
        }


        public Task InitializeMembershipTable(bool tryInitTableVersion)
        {
            _manager = new MembershipDataManager(_couchbaseProvidersSettings.MembershipBucketName, _bucketFactory, _loggerFactory);
            return Task.CompletedTask;
        }

        public Task<bool> InsertRow(MembershipEntry entry, TableVersion tableVersion)
        {
            return _manager.InsertRow(entry, tableVersion);
        }

        public Task<MembershipTableData> ReadAll()
        {
            return _manager.ReadAll();
        }

        public Task<MembershipTableData> ReadRow(SiloAddress key)
        {
            return _manager.ReadRow(key);
        }

        public Task UpdateIAmAlive(MembershipEntry entry)
        {
            return _manager.UpdateIAmAlive(entry);
        }

        public Task<bool> UpdateRow(MembershipEntry entry, string etag, TableVersion tableVersion)
        {
            return _manager.UpdateRow(entry, tableVersion, etag);
        }

        public Task CleanupDefunctSiloEntries(DateTimeOffset beforeDate)
        {
            return _manager.CleanupDefunctSiloEntries(beforeDate);
        }
    }

    public class CouchbaseGatewayListProvider : IGatewayListProvider
    {
        private readonly IBucketFactory _bucketFactory;
        private MembershipDataManager _manager;
        private CouchbaseProvidersSettings _settings;
        private ILoggerFactory _loggerFactory;

        public CouchbaseGatewayListProvider(IBucketFactory bucketFactory, IOptions<CouchbaseProvidersSettings> settings, ILoggerFactory loggerFactory)
        {
            _settings = settings.Value;
            _bucketFactory = bucketFactory;
            _loggerFactory = loggerFactory;
        }

        public bool IsUpdatable
        {
            get { return true; }
        }

        public TimeSpan MaxStaleness { get; private set; }

        public Task<IList<Uri>> GetGateways()
        {
            return _manager.GetGateWays();
        }


        public async Task InitializeGatewayListProvider()
        {
            _manager = new MembershipDataManager(_settings.MembershipBucketName, _bucketFactory, _loggerFactory);

            MaxStaleness = _settings.RefreshRate;

            //todo do we need this anymore?
            await _manager.CleanupDefunctSiloEntries();
        }
    }

    public class MembershipDataManager : CouchbaseDataManager
    {
        private readonly TableVersion tableVersion = new TableVersion(0, "0");
        private readonly ILogger<MembershipDataManager> _logger;

        public MembershipDataManager(string bucketName, ClientConfiguration clientConfig) : base(bucketName, clientConfig)
        {
        }

        public MembershipDataManager(string bucketName, IBucketFactory bucketFactory, ILoggerFactory loggerFactory) : base(bucketName, bucketFactory)
        {
            _logger = loggerFactory.CreateLogger<MembershipDataManager>();
        }


        public async Task DeleteMembershipTableEntries(string deploymentId)
        {
            var deleteQuery = new QueryRequest($"delete from {BucketName} where deploymentId = \"{deploymentId}\" and docType = \"{DocBaseOrleans.OrleansDocType}\" and docSubType = \"{DocSubTypes.Membership}\"");
            deleteQuery.ScanConsistency(ScanConsistency.RequestPlus);
            deleteQuery.Metrics(false);
            IQueryResult<MembershipEntry> result = await Bucket.QueryAsync<MembershipEntry>(deleteQuery).ConfigureAwait(false);
            //todo log failures
        }

        public async Task CleanupDefunctSiloEntries()
        {
            await CleanupDefunctSiloEntries(MaxAgeExpiredSilosMin);
        }

        internal async Task CleanupDefunctSiloEntries(DateTimeOffset beforeDate)
        {
            double totalMinutes = (DateTimeOffset.Now - beforeDate).TotalMinutes;
            await CleanupDefunctSiloEntries(totalMinutes);
        }

        private async Task CleanupDefunctSiloEntries(double totalMinutes)
        {
            var deleteQuery = new QueryRequest($"delete from {BucketName} where status = {(int)SiloStatus.Dead} and docType = \"{DocBaseOrleans.OrleansDocType}\" and docSubType = \"{DocSubTypes.Membership}\" and STR_TO_MILLIS(Date_add_str(Now_utc(), -{totalMinutes}, 'minute'))  > STR_TO_MILLIS(iAmAliveTime)");
            deleteQuery.ScanConsistency(ScanConsistency.RequestPlus);
            deleteQuery.Metrics(false);
            IQueryResult<MembershipEntry> result = await Bucket.QueryAsync<MembershipEntry>(deleteQuery).ConfigureAwait(false);
            if (!result.Success)
            {
                _logger.LogError(new CouchbaseQueryResponseException($"{GetType().Name}: Error removing expired silo records from Couchbase. ", result.Status, result.Errors),"");
            }
        }

        public static int MaxAgeExpiredSilosMin => 20;

        #region GatewayProvider

        public async Task<IList<Uri>> GetGateWays()
        {
//            var b = new BucketContext(ClusterHelper.GetBucketAsync(CouchbaseMembershipProvider.BucketName));
            //todo Is this the right query?
//            var getGateWaysQuery = new QueryRequest($"select id from {BucketName} where docType = \"{DocBaseOrleans.OrleansDocType}\" and docSubType = \"{DocSubTypes.Membership}\" and status = {(int)SiloStatus.Active}");
//            getGateWaysQuery.ScanConsistency(ScanConsistency.RequestPlus);
//            getGateWaysQuery.Metrics(false);
//            IQueryResult<CouchbaseSiloRegistration> result = await Bucket.QueryAsync<CouchbaseSiloRegistration>(getGateWaysQuery);
            MembershipTableData tableData = await ReadAll();
            List<MembershipEntry> result = tableData.Members.Select(tableDataMember => tableDataMember.Item1).ToList();
            List<Uri> r = result.Where(x => x.Status == SiloStatus.Active && x.ProxyPort != 0).Select(x =>
                {
                    //EXISTED IN CONSOLE MEMBERSHIP, am not sure why
                    x.SiloAddress.Endpoint.Port = x.ProxyPort; 
                    Uri address =  x.SiloAddress.ToGatewayUri();
                    return address;
                })
                .ToList();
            return r;
        }

        #endregion


        public async Task<bool> InsertRow(MembershipEntry entry, TableVersion tableVersion)
        {
            try
            {
                CouchbaseSiloRegistration serializable = CouchbaseSiloRegistrationUtility.FromMembershipEntry("", entry, "0");
//                IOperationResult<CouchbaseSiloRegistration> result = await Bucket.InsertAsync(entry.SiloAddress.ToParsableString(), serializable).ConfigureAwait(false);
                IOperationResult<CouchbaseSiloRegistration> result = await Bucket.UpsertAsync(entry.SiloAddress.ToParsableString(), serializable).ConfigureAwait(false);
                return result.Success;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<MembershipTableData> ReadAll()
        {
            //todo is this the right query?
            var readAllQuery = new QueryRequest($"select meta().id from {BucketName} where docType = \"{DocBaseOrleans.OrleansDocType}\" and docSubType = \"{DocSubTypes.Membership}\"");
            readAllQuery.ScanConsistency(ScanConsistency.RequestPlus);
            readAllQuery.Metrics(false);
            IQueryResult<JObject> ids = await Bucket.QueryAsync<JObject>(readAllQuery).ConfigureAwait(false);

            List<string> idStrings = ids.Rows.Select(x => x["id"].ToString()).ToList();
            IDocumentResult<CouchbaseSiloRegistration>[] actuals = await Bucket.GetDocumentsAsync<CouchbaseSiloRegistration>(idStrings); //has no async version with batch reads
            var entries = new List<Tuple<MembershipEntry, string>>();
            foreach (IDocumentResult<CouchbaseSiloRegistration> actualRow in actuals)
            {
                //var actualRow = await bucket.GetAsync<CouchbaseSiloRegistration>(r["id"].ToString());
                entries.Add(CouchbaseSiloRegistrationUtility.ToMembershipEntry(actualRow.Content, actualRow.Document.Cas.ToString()));
            }

            return new MembershipTableData(entries, new TableVersion(0, "0"));
        }

        public async Task<MembershipTableData> ReadRow(SiloAddress key)
        {
            var entries = new List<Tuple<MembershipEntry, string>>();
            IOperationResult<CouchbaseSiloRegistration> row = await Bucket.GetAsync<CouchbaseSiloRegistration>(key.ToParsableString()).ConfigureAwait(false);
            if (row.Success)
            {
                entries.Add(CouchbaseSiloRegistrationUtility.ToMembershipEntry(row.Value, row.Cas.ToString()));
            }

            return new MembershipTableData(entries, new TableVersion(0, "0"));
        }

        public async Task UpdateIAmAlive(MembershipEntry entry)
        {
            IOperationResult<CouchbaseSiloRegistration> data = await Bucket.GetAsync<CouchbaseSiloRegistration>(entry.SiloAddress.ToParsableString());
            data.Value.IAmAliveTime = entry.IAmAliveTime;
            SiloAddress address = CouchbaseSiloRegistrationUtility.ToMembershipEntry(data.Value).Item1.SiloAddress;
            await Bucket.UpsertAsync(address.ToParsableString(), data.Value).ConfigureAwait(false);
        }

        public async Task<bool> UpdateRow(MembershipEntry entry, TableVersion tableVersion, string eTag)
        {
            try
            {
                CouchbaseSiloRegistration serializableData = CouchbaseSiloRegistrationUtility.FromMembershipEntry("", entry, eTag);
                var temp = JsonConvert.SerializeObject(serializableData);
                IOperationResult<CouchbaseSiloRegistration> result = await Bucket.UpsertAsync(entry.SiloAddress.ToParsableString(), serializableData, ulong.Parse(eTag)).ConfigureAwait(false);
                return result.Success;
            }
            catch (Exception)
            {
                return false;
            }
        }


    }
}