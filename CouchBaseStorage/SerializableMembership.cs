using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Orleans;
using Orleans.Runtime;

namespace Orleans.Storage
{
    /// <summary>
    /// This class is used to deserialized returned values from DB when Cas is returned
    /// with the actual data as well.
    /// </summary>
    [JsonObject]
    class SiloRegistrationWithCas
    {
        [JsonProperty]
        public ulong Cas { get; set; }

        [JsonProperty]
        public CouchBaseSiloRegistration Membership { get; set; }
    }

    /// <summary>
    /// JSON Serializable Object that when serialized and Base64 encoded, forms the Value part of a Silo's couchbase KVPair
    /// </summary>
    [JsonObject]
    public class CouchBaseSiloRegistration
    {
        /// <summary>
        /// Persisted as part of the KV Key therefore not serialised.
        /// </summary>
        [JsonProperty]
        public String DeploymentId { get; set; }

        /// <summary>
        /// Persisted as part of the KV Key therefore not serialised.
        /// </summary>
        [JsonIgnore]
        internal SiloAddress Address { get; set; }

        /// <summary>
        /// The serialized version of the address
        /// </summary>
        [JsonProperty]
        public string SerializedAddress { get; set; }

        /// <summary>
        /// Persisted in a separate KV Subkey, therefore not serialised but held here to enable cleaner assembly to MembershipEntry.
        [JsonProperty]
        internal DateTime IAmAliveTime { get; set; }

        //Public properties are serialized to the KV.Value
        [JsonProperty]
        public String Hostname { get; set; }

        [JsonProperty]
        public Int32 ProxyPort { get; set; }

        [JsonProperty]
        public DateTime StartTime { get; set; }

        [JsonProperty]
        public SiloStatus Status { get; set; }

        [JsonProperty]
        public String SiloName { get; set; }

        [JsonProperty]
        public List<SuspectingSilo> SuspectingSilos { get; set; }

        [JsonConstructor]
        internal CouchBaseSiloRegistration()
        {
            SuspectingSilos = new List<SuspectingSilo>();
        }
    }

    /// <summary>
    /// JSON Serializable Object that when serialized and Base64 encoded, forms each entry in the SuspectingSilos list
    /// </summary>
    [JsonObject]
    public class SuspectingSilo
    {
        [JsonProperty]
        public String Id { get; set; }

        [JsonProperty]
        public DateTime Time { get; set; }
    }

    /// <summary>
    /// Contains methods for converting  to and from a MembershipEntry.  
    /// This uses CoouchBaseSiloRegistration objects as the serialisable KV.Value and minimises conversion operations.
    /// </summary>
    internal class CouchbaseSiloRegistrationmUtility
    {

        internal static CouchBaseSiloRegistration FromMembershipEntry(String deploymentId, MembershipEntry entry, String etag)
        {
            if (entry.SuspectTimes == null)
                entry.SuspectTimes = new List<Tuple<SiloAddress, DateTime>>();
            var ret = new CouchBaseSiloRegistration
            {
                DeploymentId = deploymentId,
                Address = entry.SiloAddress,
                SerializedAddress = entry.SiloAddress.ToParsableString(),
                IAmAliveTime = entry.IAmAliveTime,
                //Cas = Convert.ToUInt64(etag),
                Hostname = entry.HostName,
                ProxyPort = entry.ProxyPort,
                StartTime = entry.StartTime,
                Status = entry.Status,
                //SiloName = entry.SiloName,
                SuspectingSilos = entry.SuspectTimes.Select(silo => new SuspectingSilo { Id = silo.Item1.ToParsableString(), Time = silo.Item2 }).ToList()
            };

            return ret;
        }



        internal static Tuple<MembershipEntry, String> ToMembershipEntry(CouchBaseSiloRegistration siloRegistration,string cas="")
        {
            siloRegistration.Address = SiloAddress.FromParsableString(siloRegistration.SerializedAddress);
            var entry = new MembershipEntry
            {
                SiloAddress = siloRegistration.Address,
                HostName = siloRegistration.Hostname,
                Status = siloRegistration.Status,
                ProxyPort = siloRegistration.ProxyPort,
                StartTime = siloRegistration.StartTime,
                SuspectTimes = siloRegistration.SuspectingSilos.Select(silo => new Tuple<SiloAddress, DateTime>(SiloAddress.FromParsableString(silo.Id), silo.Time)).ToList(),
                IAmAliveTime = siloRegistration.IAmAliveTime,
                //SiloName = siloRegistration.SiloName,

                // Optional - only for Azure role so initialised here
                RoleName = String.Empty,
                UpdateZone = 0,
                FaultZone = 0
            };

            return new Tuple<MembershipEntry, String>(entry,cas);
        }
    }
}
