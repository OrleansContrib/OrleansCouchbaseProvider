// ReSharper disable InconsistentNaming
// ReSharper disable CheckNamespace
// ReSharper disable ClassNeverInstantiated.Global

using Couchbase.Configuration.Client;

namespace Orleans.Storage
{
    public class CouchbaseGatewayOptions
    {
        public string Group { get; set; }
        public string APIEndpoint { get; set; }
        public string APIToken { get; set; }
        public string CertificateData { get; set; }
    }
}
