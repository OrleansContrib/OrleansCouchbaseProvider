using CouchbaseProviders.CouchbaseComm;
using CouchbaseProviders.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Runtime;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace CouchbaseProviders.Reminders
{
    public sealed class CouchbaseReminderProvider : IReminderTable
    {
        private readonly ILogger _logger;
        private readonly IGrainReferenceConverter _grainReferenceConverter;
        private readonly CouchbaseProvidersSettings _settings;
        private ReminderDataManager _manager;
        private readonly IBucketFactory _bucketFactory;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IGrainFactory _grainFactory;

        public CouchbaseReminderProvider(
            ILogger<CouchbaseReminderProvider> logger,
            IOptions<CouchbaseProvidersSettings> options,
            IGrainReferenceConverter grainReferenceConverter,
            IBucketFactory bucketFactory,
            ILoggerFactory loggerFactory,
            IGrainFactory grainFactory)
        {
            _logger = logger;
            _loggerFactory = loggerFactory;
            _settings = options.Value;
            _grainReferenceConverter = grainReferenceConverter;
            _bucketFactory = bucketFactory;
            _grainFactory = grainFactory;
        }

        /// <inheritdoc />
        public Task Init()
        {
            _manager = new ReminderDataManager(_settings.ReminderBucketName, _bucketFactory, _loggerFactory, _grainFactory, _grainReferenceConverter);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task<ReminderTableData> ReadRows(GrainReference key)
        {
            return _manager.GetAllReminderRows();
        }

        /// <inheritdoc />
        public async Task<bool> RemoveRow(GrainReference grainRef, string reminderName, string eTag)
        {
           return await _manager.DeleteReminder(grainRef, reminderName, eTag);
        }

        /// <inheritdoc />
        public async Task<ReminderEntry> ReadRow(GrainReference grainRef, string reminderName)
        {
            return await _manager.ReadReminder(grainRef, reminderName);
        }

        /// <inheritdoc />
        public async Task TestOnlyClearTable()
        {
            await _manager.TestOnlyClearTable(_settings.ReminderBucketName);
        }

        /// <inheritdoc />
        public async Task<string> UpsertRow(ReminderEntry entry)
        {
                CouchbaseReminderDocument result = await _manager.UpdateReminder(entry);
                ReminderEntry reminder = result.ToReminderEntry(_grainReferenceConverter);
                return JsonConvert.SerializeObject(reminder);
        }

        /// <inheritdoc />
        public async Task<ReminderTableData> ReadRows(uint begin, uint end)
        {
                if (begin < end)
                {
                    return await _manager.ReadRowsInRange(begin, end);
                }
                else
                {
                    return await _manager.ReadRowsOutRange(begin, end);
                }
        }

    }
}