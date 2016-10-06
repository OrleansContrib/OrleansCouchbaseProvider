using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Orleans.Runtime;
using Orleans.Providers;

namespace Orleans.Storage
{
    /// <summary>
    /// Base class for JSON-based grain storage providers.
    /// </summary>
    public abstract class BaseJSONStorageProvider : IStorageProvider
    {
        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings();

        /// <summary>
        /// Logger object
        /// </summary>
        public Logger Log
        {
            get; protected set;
        }

        /// <summary>
        /// Storage provider name
        /// </summary>
        public string Name { get; protected set; }

        /// <summary>
        /// Data manager instance
        /// </summary>
        /// <remarks>The data manager is responsible for reading and writing JSON strings.</remarks>
        protected IJSONStateDataManager DataManager { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        protected BaseJSONStorageProvider()
        {
        }

        /// <summary>
        /// Initializes the storage provider.
        /// </summary>
        /// <param name="name">The name of this provider instance.</param>
        /// <param name="providerRuntime">A Orleans runtime object managing all storage providers.</param>
        /// <param name="config">Configuration info for this provider instance.</param>
        /// <returns>Completion promise for this operation.</returns>
        public virtual Task Init(string name, IProviderRuntime providerRuntime, IProviderConfiguration config)
        {
            this.Name = name;
            Log = providerRuntime.GetLogger(this.GetType().FullName);
            return TaskDone.Done;
        }

        /// <summary>
        /// Closes the storage provider during silo shutdown.
        /// </summary>
        /// <returns>Completion promise for this operation.</returns>
        public Task Close()
        {
            if (DataManager != null)
                DataManager.Dispose();
            DataManager = null;
            return TaskDone.Done;
        }

        /// <summary>
        /// Reads persisted state from the backing store and deserializes it into the the target
        /// grain state object.
        /// </summary>
        /// <param name="grainType">A string holding the name of the grain class.</param>
        /// <param name="grainReference">Represents the long-lived identity of the grain.</param>
        /// <param name="grainState">A reference to an object to hold the persisted state of the grain.</param>
        /// <returns>Completion promise for this operation.</returns>
        public async Task ReadStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            if (DataManager == null) throw new ArgumentException("DataManager property not initialized");

            var grainTypeName = grainType.Split('.').Last();

            //Reads the state and returns a tuple with (data,ETag) structure.
            var entityDataAndEtag = await DataManager.Read(grainTypeName, grainReference.ToKeyString());
            //If no data exists Item1 will be null
            //If we can not find any data we don't touch the state object.
            if (entityDataAndEtag.Item1 != null)
            {
                ConvertFromStorageFormat(grainState, entityDataAndEtag.Item1);
                grainState.ETag = entityDataAndEtag.Item2;
            }
        }

        /// <summary>
        /// Writes the persisted state from a grain state object into its backing store.
        /// </summary>
        /// <param name="grainType">A string holding the name of the grain class.</param>
        /// <param name="grainReference">Represents the long-lived identity of the grain.</param>
        /// <param name="grainState">A reference to an object holding the persisted state of the grain.</param>
        /// <returns>Completion promise for this operation.</returns>
        public async Task WriteStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            if (DataManager == null) throw new ArgumentException("DataManager property not initialized");

            var grainTypeName = grainType.Split('.').Last();
            //Serialize the data
            var entityData = ConvertToStorageFormat(grainState);
            //Get the ETag to send to the DB
            var eTag = grainState.ETag;
            var returnedEtag = await DataManager.Write(grainTypeName, grainReference.ToKeyString(), entityData, eTag);
            //Set the new ETag on the state object.
            grainState.ETag = returnedEtag;
        }

        /// <summary>
        /// Removes grain state from its backing store, if found.
        /// </summary>
        /// <param name="grainType">A string holding the name of the grain class.</param>
        /// <param name="grainReference">Represents the long-lived identity of the grain.</param>
        /// <param name="grainState">An object holding the persisted state of the grain.</param>
        /// <returns></returns>
        public Task ClearStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            if (DataManager == null) throw new ArgumentException("DataManager property not initialized");

            var grainTypeName = grainType.Split('.').Last();
            //When deleting we at least read the grain state at least once so we should have the ETag
            DataManager.Delete(grainTypeName, grainReference.ToKeyString(), grainState.ETag);
            return TaskDone.Done;
        }

        /// <summary>
        /// Serializes from a grain instance to a JSON document.
        /// </summary>
        /// <param name="grainState">Grain state to be converted into JSON storage format.</param>
        /// <remarks>
        /// See:
        /// JSON.NET's website
        /// for more on the JSON serializer.
        /// </remarks>
        protected static string ConvertToStorageFormat(IGrainState grainState)
        {
            var jo = JObject.FromObject(grainState.State);
            jo.Add("type", grainState.State.GetType().ToString());
            return jo.ToString();
        }

        /// <summary>
        /// Constructs a grain state instance by deserializing a JSON document.
        /// </summary>
        /// <param name="grainState">Grain state to be populated for storage.</param>
        /// <param name="entityData">JSON storage format representaiton of the grain state.</param>
        protected static void ConvertFromStorageFormat(IGrainState grainState, string entityData)
        {
            JsonConvert.PopulateObject(entityData, grainState.State);
        }
    }
}
