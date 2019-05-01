// ReSharper disable InconsistentNaming
// ReSharper disable CheckNamespace
// ReSharper disable ClassNeverInstantiated.Global
namespace Orleans.Storage
{
    public class CouchbaseClusteringOptions
    {
        public string Group { get; set; }
        public string APIEndpoint { get; set; }
        public string APIToken { get; set; }
        public string CertificateData { get; set; }
        public bool CanCreateResources { get; set; }
        public bool DropResourcesOnInit { get; set; }
    }
}
