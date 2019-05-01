#region

using System.Threading.Tasks;
using Couchbase.Configuration.Client;
using Couchbase.Core;

#endregion

namespace CouchbaseProviders.CouchbaseComm
{
    public partial interface IBucketFactory
    {
        ClientConfiguration Config { get; }
        void CloseBucket(string bucketName);
        void CloseCluster();
        IBucket GetBucket(string bucketName, string bucketPassword = null);
        Task<IBucket> GetBucketAsync(string bucketName, string bucketPassword = null);
        bool IsClusterOpen();
    }
}