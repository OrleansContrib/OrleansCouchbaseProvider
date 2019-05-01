#region

using System;
using System.Collections.Generic;
using System.Linq;
using Couchbase.Core.Buckets;
using Newtonsoft.Json;
using Orleans;
using Orleans.Runtime;
using Orleans.Storage;

#endregion

namespace CouchbaseProviders.Reminders
{
    public class CouchbaseReminderDocument : DocBaseOrleans, IGrainReminder
    {
        [JsonRequired]
        public long GrainHash { get; set; }

        [JsonRequired]
        public string GrainId { get; set; }

        [JsonRequired]
        public TimeSpan Period { get; set; }

        [JsonRequired]
        public string ReminderName { get; set; }

        [JsonRequired]
        public DateTime StartAt { get; set; }

        public GrainReference GrainRef { get; set; }

        public string ETag { get; set; }

        public new string DocSubType => DocSubTypes.Reminder;

        public CouchbaseReminderDocument(){}

        public CouchbaseReminderDocument(ReminderEntry entry, string etag)
        {
            GrainHash = entry.GrainRef.GetUniformHashCode();
            GrainId = entry.GrainRef.ToKeyString();
            GrainRef = entry.GrainRef;
            Period = entry.Period;
            ReminderName = entry.ReminderName;
            StartAt = entry.StartAt;
            ETag = etag ?? "";
        }

        public ReminderEntry ToReminderEntry(IGrainReferenceConverter grainReferenceConverter)
        {
            return new ReminderEntry
            {
                GrainRef = grainReferenceConverter.GetGrainFromKeyString(GrainId),
                ETag = ETag,
                ReminderName = ReminderName,
                StartAt = StartAt,
                Period = Period
            };
        }

        public static IEnumerable<ReminderEntry> ToReminderEntries(IEnumerable<CouchbaseReminderDocument> docs, IGrainReferenceConverter grainReferenceConverter)
        {
            return docs.Select(couchbaseReminderDocument => couchbaseReminderDocument.ToReminderEntry(grainReferenceConverter)).ToList();
        }

    }
}