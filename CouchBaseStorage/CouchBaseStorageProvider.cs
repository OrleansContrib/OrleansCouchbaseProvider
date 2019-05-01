using Couchbase;
using Couchbase.Core;
using Microsoft.Extensions.Logging;
using Orleans.Providers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CouchbaseProviders.CouchbaseComm;
using CouchbaseProviders.Options;
using Microsoft.Extensions.Options;
using Orleans.Runtime;

// ReSharper disable ClassNeverInstantiated.Global

namespace Orleans.Storage
{
    /// <inheritdoc />
    /// <summary>
    /// OrleansDocType storage provider implementation for Couchbase http://www.couchbase.com 
    /// </summary>
    /// <remarks>
    /// The storage provider should be registered via programmatic config or in a config file before you can use it.
    /// This providers uses optimistic concurrency and leverages the CAS of Couchbase when touching
    /// the database. If we don't use this feature always the last write wins and it might be desired
    /// in specific scenarios and can be added later on as a feature. CAS is a ulong value stored as string in the
    /// ETag of the state object.
    /// </remarks>
    public class OrleansCouchbaseStorage : BaseJSONStorageProvider
    {
        /// <summary>
        /// This is used internally only to avoid reinitializing the client connection
        /// when multiple providers of this type are defined to store values in multiple
        /// buckets.
        /// </summary>
        internal static bool IsInitialized;

        private readonly ILogger<OrleansCouchbaseStorage> _logger;
        private readonly CouchbaseProvidersSettings _settings;
        private readonly IBucketFactory _bucketFactory;
        private readonly string _storageBucketName;

        public OrleansCouchbaseStorage(ILoggerFactory loggerFactory, IOptions<CouchbaseProvidersSettings> options, IBucketFactory bucketFactory, ITypeResolver typeResolver, IGrainFactory grainFactory) : base(loggerFactory, typeResolver, grainFactory)
        {
            _settings = options.Value;
            _storageBucketName = _settings.StorageBucketName;
            _logger = loggerFactory.CreateLogger<OrleansCouchbaseStorage>();
            _bucketFactory = bucketFactory;
        }

        /// <summary>
        /// Initializes the provider during silo startup.
        /// </summary>
        /// <param name="name">The name of this provider instance.</param>
        /// <param name="providerRuntime">A OrleansDocType runtime object managing all storage providers.</param>
        /// <param name="config">Configuration info for this provider instance.</param>
        /// <returns>Completion promise for this operation.</returns>
        public override Task Init(string name, IProviderRuntime providerRuntime, IProviderConfiguration config)
        {
            Name = name;

//            var documentExpiries = CouchbaseOrleansConfigurationExtensions.GetGrainExpiries();

            DataManager = new CouchbaseDataManager(_storageBucketName, _bucketFactory);//, documentExpiries);
            return base.Init(name, providerRuntime, config);
        }
    }

    /// <summary>
    /// Interfaces with Couchbase on behalf of the provider.
    /// </summary>
    public class CouchbaseDataManager : IJSONStateDataManager
    {
        /// <summary>
        /// Name of the bucket that it works with.
        /// </summary>
        protected string BucketName;

        /// <summary>
        /// The cached bucket reference
        /// </summary>
        protected IBucket Bucket;

        private readonly IBucketFactory _bucketFactory;

        /// <summary>
        /// Document expiries by grain type
        /// </summary>
        private Dictionary<string, TimeSpan> DocumentExpiries { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="bucketName">Name of the bucket that this manager should operate on.</param>
        /// <param name="clientConfig">Configuration object for the database client</param>
        public CouchbaseDataManager(string bucketName, Couchbase.Configuration.Client.ClientConfiguration clientConfig) : this(bucketName, clientConfig, new Dictionary<string, TimeSpan>())
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="bucketName">Name of the bucket that this manager should operate on.</param>
        /// <param name="clientConfig">Configuration object for the database client</param>
        /// /// <param name="documentExpiries">Expiry times by grain type</param>
        public CouchbaseDataManager(string bucketName, Couchbase.Configuration.Client.ClientConfiguration clientConfig, Dictionary<string, TimeSpan> documentExpiries)
        {
            //Bucket name should not be empty
            //Keep in mind that you should create the buckets before being able to use them either
            //using the commandline tool or the web console
            if (string.IsNullOrWhiteSpace(bucketName))
                throw new ArgumentException("bucketName can not be null or empty");
            //config should not be null either
            if (clientConfig == null)
                throw new ArgumentException("You should supply a configuration to connect to Couchbase");

            this.BucketName = bucketName;
            if (!OrleansCouchbaseStorage.IsInitialized)
            {
                ClusterHelper.Initialize(clientConfig);
                OrleansCouchbaseStorage.IsInitialized = true;
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
            Bucket = ClusterHelper.GetBucket(this.BucketName);

            DocumentExpiries = documentExpiries;
        }

        public CouchbaseDataManager(string bucketName, IBucketFactory bucketFactory)
        {
            //Bucket name should not be empty
            //Keep in mind that you should create the buckets before being able to use them either
            //using the commandline tool or the web console
            if (string.IsNullOrWhiteSpace(bucketName))
                throw new ArgumentException("bucketName can not be null or empty");
            //config should not be null either
            if (bucketFactory.Config == null)
                throw new ArgumentException("You should supply a configuration to connect to Couchbase");

            if(!bucketFactory.IsClusterOpen())
                throw new InitializationException("Couchbase Cluster not initialized.  Did you forget to add IBucketFactory to IOC?");
            BucketName = bucketName;
            Bucket = bucketFactory.GetBucket(bucketName);
            OrleansCouchbaseStorage.IsInitialized = true;
            _bucketFactory = bucketFactory;
        }

        /// <summary>
        /// Deletes a document representing a grain state object.
        /// </summary>
        /// <param name="collectionName">The type of the grain state object.</param>
        /// <param name="key">The grain id string.</param>
        /// <param name="eTag"></param>
        /// <returns>Completion promise for this operation.</returns>
        public async Task Delete(string collectionName, string key, string eTag)
        {
            var docID = GetDocumentID(collectionName, key);
            var result = await Bucket.RemoveAsync(docID, ulong.Parse(eTag));
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
            var docId = GetDocumentID(collectionName, key);

            //If there is a value we read it and consider the CAS as ETag as well and return
            //both as a tuple
            var result = await Bucket.GetAsync<string>(docId);
            if (result.Success)
                return Tuple.Create(result.Value, result.Cas.ToString());
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
        /// <param name="eTag"></param>
        /// <returns>Completion promise for this operation.</returns>
        public async Task<string> Write(string collectionName, string key, string entityData, string eTag)
        {
            string docId = GetDocumentID(collectionName, key);

            TimeSpan expiry = TimeSpan.Zero;
            if (DocumentExpiries != null && DocumentExpiries.ContainsKey(collectionName))
            {
                expiry = DocumentExpiries[collectionName];
            }

            if (ulong.TryParse(eTag, out ulong realETag))
            {
                var r = await Bucket.UpsertAsync<string>(docId, entityData, realETag, expiry);
                if (!r.Success)
                {
                    throw new Orleans.Storage.InconsistentStateException(r.Status.ToString(), eTag, r.Cas.ToString());
                }

                return r.Cas.ToString();
            }
            else
            {
                var r = await Bucket.InsertAsync<string>(docId, entityData, expiry);

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

        /// <inheritdoc />
        /// <summary>
        /// Writes a document representing a grain state object.
        /// </summary>
        /// <param name="collectionName">The type of the grain state object.</param>
        /// <param name="key">The grain id string.</param>
        /// <param name="doc"></param>
        /// <param name="eTag"></param>
        /// <returns>Completion promise for this operation.</returns>
        public async Task<string> WriteAsync<T>(string collectionName, string key, T doc, string eTag)where T: DocBaseOrleans
        {
            string docId = GetDocumentID(collectionName, key);
            doc.Id = docId;
            TimeSpan expiry = TimeSpan.Zero;
            if (DocumentExpiries != null && DocumentExpiries.ContainsKey(collectionName))
            {
                expiry = DocumentExpiries[collectionName];
            }

            if (ulong.TryParse(eTag, out ulong realETag))
            {
                IOperationResult<T> r = await Bucket.UpsertAsync(docId, doc, realETag, expiry);
                if (!r.Success)
                {
                    throw new Orleans.Storage.InconsistentStateException(r.Status.ToString(), eTag, r.Cas.ToString());
                }

                return r.Cas.ToString();
            }
            else
            {
                IOperationResult<T> r = await Bucket.InsertAsync(docId, doc, expiry);

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
			_bucketFactory.CloseBucket(BucketName);
            Bucket = null;
            OrleansCouchbaseStorage.IsInitialized = false;
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
        // ReSharper disable once VirtualMemberNeverOverridden.Global
        protected virtual string GetDocumentID(string collectionName, string key)
        {
            return collectionName + "_" + key;
        }
    }
}