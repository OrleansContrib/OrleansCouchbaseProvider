#region

using System;

#endregion

namespace CouchbaseProviders.Options
{
    /// <summary>
    ///     Since most implementations will have multiple buckets
    ///     defined in their Config it makes sense
    ///     to specify which bucket you want to use for membership storage
    ///     outside of the main reusable Config.
    /// </summary>
    public class CouchbaseProvidersSettings
    {
        public TimeSpan RefreshRate { get; set; }
        public string MembershipBucketName { get; set; }
        public string StorageBucketName { get; set; }
        public string ReminderBucketName { get; set; }
    }
}