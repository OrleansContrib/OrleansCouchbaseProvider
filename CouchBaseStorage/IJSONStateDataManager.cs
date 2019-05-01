using System;
using System.Threading.Tasks;

namespace Orleans.Storage
{
    /// <summary>
    /// Defines the interface for the lower level of JSON storage providers, i.e.
    /// the part that writes JSON strings to the underlying storage. The higher level
    /// maps between grain state data and JSON.
    /// </summary>
    /// <remarks>
    /// Having this interface allows most of the serialization-level logic
    /// to be implemented in a base class of the storage providers.
    /// </remarks>
    public interface IJSONStateDataManager : IDisposable
    {
        /// <summary>
        /// Deletes the grain state associated with a given key from the collection
        /// </summary>
        /// <param name="collectionName">The name of a collection, such as a type name</param>
        /// <param name="key">The primary key of the object to delete</param>
        /// <param name="eTag"></param>
        System.Threading.Tasks.Task Delete(string collectionName, string key, string eTag);

        /// <summary>
        /// Reads grain state from storage.
        /// </summary>
        /// <param name="collectionName">The name of a collection, such as a type name.</param>
        /// <param name="key">The primary key of the object to read.</param>
        /// <returns>A string containing a JSON representation of the entity, if it exists; null otherwise.</returns>
        System.Threading.Tasks.Task<Tuple<string,string>> Read(string collectionName, string key);

        /// <summary>
        /// Writes grain state to storage.
        /// </summary>
        /// <param name="collectionName">The name of a collection, such as a type name.</param>
        /// <param name="key">The primary key of the object to write.</param>
        /// <param name="entityData">A string containing a JSON representation of the entity.</param>
        /// <param name="ETag"></param>
        System.Threading.Tasks.Task<string> Write(string collectionName, string key, string entityData, string ETag);

        /// <inheritdoc />
        /// <summary>
        /// Writes a document representing a grain state object.
        /// </summary>
        /// <param name="collectionName">The type of the grain state object.</param>
        /// <param name="key">The grain id string.</param>
        /// <param name="doc"></param>
        /// <param name="eTag"></param>
        /// <returns>Completion promise for this operation.</returns>
        Task<string> WriteAsync<T>(string collectionName, string key, T doc, string eTag)where T: DocBaseOrleans;
    }
}
