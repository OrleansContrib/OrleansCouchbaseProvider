using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Orleans.Providers;
using Couchbase;
using Couchbase.Core;
using CouchBaseProviders.Configuration;

namespace Orleans.Storage
{
    /// <summary>
    /// Orleans storage provider implementation for CouchBase http://www.couchbase.com 
    /// </summary>
    /// <remarks>
    /// The storage provider should be registered via programatic config or in a config file before you can use it.
    /// 
    /// This providers uses optimistic concurrency and leverages the CAS of CouchBase when touching
    /// the database. If we don't use this feature always the last write wins and it might be desired
    /// in specific scenarios and can be added later on as a feature. CAS is a ulong value stored as string in the
    /// ETag of the state object.
    /// </remarks>
    public class OrleansCouchBaseStorage : BaseJSONStorageProvider
    {
        /// <summary>
        /// This is used internally only to avoid reinitializing the client connection
        /// when multiple providers of this type are defined to store values in multiple
        /// buckets.
        /// </summary>
        internal static bool IsInitialized;
        
        /// <summary>
        /// Initializes the provider during silo startup.
        /// </summary>
        /// <param name="name">The name of this provider instance.</param>
        /// <param name="providerRuntime">A Orleans runtime object managing all storage providers.</param>
        /// <param name="config">Configuration info for this provider instance.</param>
        /// <returns>Completion promise for this operation.</returns>
        public override Task Init(string name, IProviderRuntime providerRuntime, IProviderConfiguration config)
        {
            this.Name = name;

            string storageBucketName = null;
            var clientConfiguration = config.Properties.ReadCouchbaseConfiguration(out storageBucketName);

            var documentExpiries = CouchbaseOrleansConfigurationExtensions.GetGrainExpiries();

            DataManager = new CouchBaseDataManager(storageBucketName, clientConfiguration, documentExpiries);
            return base.Init(name, providerRuntime, config);
        }
    }

    /// <summary>
    /// Interfaces with CouchBase on behalf of the provider.
    /// </summary>
    public class CouchBaseDataManager : IJSONStateDataManager
    {
        /// <summary>
        /// Name of the bucket that it works with.
        /// </summary>
        protected readonly string bucketName;

        /// <summary>
        /// The cached bucket reference
        /// </summary>
        protected IBucket bucket;

        /// <summary>
        /// Document expiries by grain type
        /// </summary>
        private Dictionary<string, TimeSpan> DocumentExpiries { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="bucketName">Name of the bucket that this manager should operate on.</param>
        /// <param name="clientConfig">Configuration object for the database client</param>
        public CouchBaseDataManager(string bucketName, Couchbase.Configuration.Client.ClientConfiguration clientConfig) : this(bucketName, clientConfig, new Dictionary<string, TimeSpan>())
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="bucketName">Name of the bucket that this manager should operate on.</param>
        /// <param name="clientConfig">Configuration object for the database client</param>
        /// /// <param name="documentExpiries">Expiry times by grain type</param>
        public CouchBaseDataManager(string bucketName, Couchbase.Configuration.Client.ClientConfiguration clientConfig, Dictionary<string, TimeSpan> documentExpiries)
        {
            //Bucket name should not be empty
            //Keep in mind that you should create the buckets before being able to use them either
            //using the commandline tool or the web console
            if (string.IsNullOrWhiteSpace(bucketName))
                throw new ArgumentException("bucketName can not be null or empty");
            //config should not be null either
            if (clientConfig == null)
                throw new ArgumentException("You should supply a configuration to connect to CouchBase");

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
                    if (ClusterHelper.Get().Configuration.BucketConfigs.ContainsKey(conf.Key))
                    {
                        ClusterHelper.Get().Configuration.BucketConfigs.Remove(conf.Key);
                    }
                    ClusterHelper.Get().Configuration.BucketConfigs.Add(conf.Key, conf.Value);
                }
            }
            //cache the bucket.
            bucket = ClusterHelper.GetBucket(this.bucketName);

            DocumentExpiries = documentExpiries;
        }

        /// <summary>
        /// Deletes a document representing a grain state object.
        /// </summary>
        /// <param name="collectionName">The type of the grain state object.</param>
        /// <param name="key">The grain id string.</param>
        /// <returns>Completion promise for this operation.</returns>
        public async Task Delete(string collectionName, string key, string eTag)
        {
            var docID = GetDocumentID(collectionName, key);
            var result = await bucket.RemoveAsync(docID, ulong.Parse(eTag));
            if (!result.Success)
                throw new Orleans.Storage.InconsistentStateException(result.Message, eTag, result.Cas.ToString());
        }

        /// <summary>
        /// Reads a document representing a grain state object.
        /// </summary>
        /// <param name="collectionName">The type of the grain state object.</param>
        /// <param name="key">The grain id string.</param>
        /// <returns>Completion promise for this operation.</returns>
        public async Task<Tuple<string, string>> Read(string collectionName, string key)
        {
            var docID = GetDocumentID(collectionName, key);

            //If there is a value we read it and consider the CAS as ETag as well and return
            //both as a tuple
            var result = await bucket.GetAsync<string>(docID);
            if (result.Success)
                return Tuple.Create<string, string>(result.Value, result.Cas.ToString());
            if (!result.Success && result.Status == Couchbase.IO.ResponseStatus.KeyNotFound) //not found
                return Tuple.Create<string, string>(null, "");

            throw result.Exception;
        }
        
        /// <summary>
        /// Writes a document representing a grain state object.
        /// </summary>
        /// <param name="collectionName">The type of the grain state object.</param>
        /// <param name="key">The grain id string.</param>
        /// <param name="entityData">The grain state data to be stored./</param>
        /// <returns>Completion promise for this operation.</returns>
        public async Task<string> Write(string collectionName, string key, string entityData, string eTag)
        {
            var docID = GetDocumentID(collectionName, key);

            var expiry = DocumentExpiries.ContainsKey(collectionName) ? DocumentExpiries[collectionName] : TimeSpan.Zero;

            ulong realETag;
            if (ulong.TryParse(eTag, out realETag))
            {
                var r = await bucket.UpsertAsync<string>(docID, entityData, realETag, expiry);
                if (!r.Success)
                {
                    throw new Orleans.Storage.InconsistentStateException(r.Status.ToString(), eTag, r.Cas.ToString());
                }

                return r.Cas.ToString();
            }
            else
            {
                var r = await bucket.InsertAsync<string>(docID, entityData, expiry);

                //check if key exist and we don't have the CAS
                if (!r.Success && r.Status == Couchbase.IO.ResponseStatus.KeyExists)
                {
                    throw new Orleans.Storage.InconsistentStateException(r.Status.ToString(), eTag, r.Cas.ToString());
                }
                else if (!r.Success)
                    throw new System.Exception(r.Status.ToString());

                return r.Cas.ToString();
            }
        }

        public void Dispose()
        {
			bucket.Dispose();
            bucket = null;
            //Closes the DB connection
            ClusterHelper.Close();
            OrleansCouchBaseStorage.IsInitialized = false;
			GC.SuppressFinalize(this);
        }


        /// <summary>
        /// Creates a document ID based on the type name and key of the grain
        /// </summary>
        /// <param name="collectionName"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        /// <remarks>Each ID should be at most 250 bytes and it should not cause an issue unless you have
        /// an appetite for very long class names.
        /// The id will be of form TypeName_Key where TypeName doesn't include any namespace
        /// or version info.
        /// </remarks>
        private string GetDocumentID(string collectionName, string key)
        {
            return collectionName + "_" + key;
        }
    }
}