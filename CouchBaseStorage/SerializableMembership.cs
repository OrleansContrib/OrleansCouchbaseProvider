#region

using System;
using System.Collections.Generic;
using System.Linq;
using Couchbase;
using Couchbase.Core.Buckets;
using Newtonsoft.Json;
using Orleans.Runtime;

#endregion

// ReSharper disable CheckNamespace
// ReSharper disable ClassNeverInstantiated.Global

namespace Orleans.Storage
{
    [JsonObject]
    public abstract class DocBaseOrleans : IDocBaseOrleans
    {
        //Allows at least one developer to run orleans in their IDE without running into connection issues with 'live' silos
        #if DEBUG
        public static string OrleansDocType = "orleans_" + Environment.GetEnvironmentVariable("HOSTNAME");
        #else
        public const string OrleansDocType = "orleans";
        #endif

        [JsonProperty("docType")]
        [JsonRequired]
        public string DocType { get; } = OrleansDocType;

        [JsonProperty("docSubType")]
        [JsonRequired]
        public string DocSubType { get; set; }

        [JsonProperty("id")]
        [JsonRequired]
        public string Id { get; set; }

        [JsonIgnore]
        public ulong Cas { get; set; }

        [JsonIgnore]
        public uint Expiry { get; set; }
        
        [JsonIgnore]
        public MutationToken Token { get; set; }
    }

    public interface IDocBaseOrleans
    {
        [JsonRequired]
        [JsonProperty("docSubType")]
        string DocSubType { get; }

        [JsonRequired]
        [JsonProperty("docType")]
        string DocType { get; }
    }

    public abstract class DocSubTypes
    {
        public const string Gateway = "gateway";
        public const string Membership = "membership";
        public const string Reminder = "reminder";
    }

    /// <summary>
    ///     This class is used to deserialized returned values from DB when Cas is returned
    ///     with the actual data as well.
    /// </summary>
    [JsonObject]
    internal class SiloRegistrationWithCas : DocBaseOrleans
    {
        [JsonProperty]
        public ulong Cas { get; set; }

        [JsonProperty]
        public CouchbaseSiloRegistration Membership { get; set; }
    }

    /// <summary>
    ///     JSON Serializable Object that when serialized and Base64 encoded, forms the Value part of a Silo's couchbase KVPair
    /// </summary>
    [JsonObject]
    public class CouchbaseSiloRegistration : DocBaseOrleans
    {
        /// <summary>
        ///     Persisted as part of the KV Key therefore not serialised.
        /// </summary>
        [JsonIgnore]
        internal SiloAddress Address { get; set; }

        /// <summary>
        ///     Persisted as part of the KV Key therefore not serialised.
        /// </summary>
        [JsonProperty]
        public string DeploymentId { get; set; }

        //Public properties are serialized to the KV.Value
        [JsonProperty]
        public string Hostname { get; set; }

        /// <summary>
        ///     Persisted in a separate KV Subkey, therefore not serialised but held here to enable cleaner assembly to
        ///     MembershipEntry.
        /// </summary>
        [JsonProperty]
        internal DateTime IAmAliveTime { get; set; }

        [JsonProperty]
        public int ProxyPort { get; set; }

        /// <summary>
        ///     The serialized version of the address
        /// </summary>
        [JsonProperty]
        public string SerializedAddress { get; set; }

        [JsonProperty]
        public string SiloName { get; set; }

        [JsonProperty]
        public DateTime StartTime { get; set; }

        [JsonProperty]
        public SiloStatus Status { get; set; }

        [JsonProperty]
        public List<SuspectingSilo> SuspectingSilos { get; set; }

        [JsonConstructor]
        internal CouchbaseSiloRegistration()
        {
            SuspectingSilos = new List<SuspectingSilo>();
            DocSubType = DocSubTypes.Membership;
        }

    }

    /// <summary>
    ///     JSON Serializable Object that when serialized and Base64 encoded, forms each entry in the SuspectingSilos list
    /// </summary>
    [JsonObject]
    public class SuspectingSilo : DocBaseOrleans
    {
        public SuspectingSilo()
        {
            DocSubType = DocSubTypes.Membership;
        }
        [JsonProperty]
        public DateTime Time { get; set; }
    }

    /// <summary>
    ///     Contains methods for converting  to and from a MembershipEntry.
    ///     This uses CouchbaseSiloRegistration objects as the serialisable KV.Value and minimises conversion operations.
    /// </summary>
    internal class CouchbaseSiloRegistrationUtility
    {
        internal static CouchbaseSiloRegistration FromMembershipEntry(string deploymentId, MembershipEntry entry, string etag)
        {
            if (entry.SuspectTimes == null)
                entry.SuspectTimes = new List<Tuple<SiloAddress, DateTime>>();
            var ret = new CouchbaseSiloRegistration
            {
                Id = entry.SiloAddress.ToParsableString(),
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
                SuspectingSilos = entry.SuspectTimes.Select(silo => new SuspectingSilo {Id = silo.Item1.ToParsableString(), Time = silo.Item2}).ToList()
            };

            return ret;
        }


        internal static Tuple<MembershipEntry, string> ToMembershipEntry(CouchbaseSiloRegistration siloRegistration, string cas = "")
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
                RoleName = string.Empty,
                UpdateZone = 0,
                FaultZone = 0
            };

            return new Tuple<MembershipEntry, string>(entry, cas);
        }
    }
}