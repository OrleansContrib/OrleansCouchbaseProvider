#region

using System.Linq;
using System.Threading.Tasks;
using Couchbase;
using Couchbase.Configuration.Client;
using Couchbase.Core;
using Microsoft.Extensions.Options;
// ReSharper disable ClassWithVirtualMembersNeverInherited.Global

#endregion

namespace CouchbaseProviders.CouchbaseComm
{
    public class BucketFactory : IBucketFactory
    {
        public BucketFactory(IOptions<CouchbaseClientDefinition> couchbaseOptions)
        {
            Config = new ClientConfiguration(couchbaseOptions.Value);
            if (Config == null || Config.Servers.Count == 0 || Config.BucketConfigs.Count == 0)
            {
                throw new InitializationException("Settings either not valid or not provided.  ");
            }

            if (!ClusterHelper.Initialized)
            {
                ClusterHelper.Initialize(Config);
            }
        }

        public ClientConfiguration Config { get; }

        public virtual async Task<IBucket> GetBucketAsync(string bucketName, string bucketPassword = null)
        {
            return await ClusterHelper.GetBucketAsync(bucketName, bucketPassword);
        }

        public virtual IBucket GetBucket(string bucketName, string bucketPassword = null)
        {
            if (!string.IsNullOrWhiteSpace(bucketPassword))
            {
                return ClusterHelper.GetBucket(bucketName, bucketPassword);
            }

            bucketPassword = GetBucketPasswordFromConfig(bucketName);
            return ClusterHelper.GetBucket(bucketName, bucketPassword);
        }

        private string GetBucketPasswordFromConfig(string bucketName)
        {
            BucketConfiguration bucketDefinition = Config.BucketConfigs[bucketName];
            string bucketPassword = bucketDefinition.Password;
            return bucketPassword;
        }

        public virtual bool IsClusterOpen()
        {
            return ClusterHelper.Initialized;
        }

        public virtual void CloseBucket(string bucketName)
        {
            ClusterHelper.RemoveBucket(bucketName);
        }

        public virtual void CloseCluster()
        {
            ClusterHelper.Close();
        }
    }
}