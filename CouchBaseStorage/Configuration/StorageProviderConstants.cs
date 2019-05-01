﻿using Orleans.Providers;

namespace CouchbaseProviders.Configuration
{
    public static class StorageConstants
    {
        /// <summary>
        /// Expected property names specified into <see cref="IProviderConfiguration.Properties"/>.
        /// </summary>
        public static class PropertyNames
        {
            /// <summary>
            /// Path to target <see cref="CouchbaseClientSection"/> configuration XML element.
            /// </summary>
            public static readonly string ClientConfigurationSectionPath = "ClientConfigurationSectionPath";

            /// <summary>
            /// List of URI(s) to target Couchbase cluster node(s).
            /// </summary>
            public static readonly string Servers = "Servers";

            /// <summary>
            /// List of URI(s) to target Couchbase cluster node(s).
            /// </summary>
            /// <remarks>
            /// An alternative to the <see cref="Servers"/> property, maintained for backward compatibility.
            /// </remarks>
            public static readonly string Server = "Server";

            /// <summary>
            /// Target bucket name.
            /// </summary>
            public static readonly string BucketName = "BucketName";

            /// <summary>
            /// Bucket user name.
            /// </summary>
            public static readonly string UserName = "UserName";

            /// <summary>
            /// Bucket password.
            /// </summary>
            public static readonly string Password = "Password";
        }
    }
}
