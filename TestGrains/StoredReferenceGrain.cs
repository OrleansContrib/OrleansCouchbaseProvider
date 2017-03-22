namespace TestGrains
{
    using System;
    using System.Threading.Tasks;

    using Orleans;

    /// <summary>
    /// Contract for a referenced grain.
    /// </summary>
    public interface IStoredReferenceGrain : IGrainWithGuidKey
    {
        /// <summary>
        /// Adds reference tag and timestamp, and saves grain state.
        /// </summary>
        /// <param name="referenceTag"></param>
        /// <returns></returns>
        Task SetReferenceState(string referenceTag);

        Task<string> GetReferenceTag();

        Task<DateTime> GetReferencedAt();
    }

    public class ReferenceStorageData
    {
        public string ReferenceTag { get; set; }

        public DateTime ReferencedAt { get; set; }
    }

    /// <inheritdoc />
    public class StoredReferenceGrain : Grain<ReferenceStorageData>, IStoredReferenceGrain
    {
        public override Task OnActivateAsync()
        {
            if (State == null)
            {
                State = new ReferenceStorageData();

            }

            return base.OnActivateAsync();
        }

        /// <inheritdoc />
        public async Task SetReferenceState(string referenceTag)
        {
            State.ReferenceTag = referenceTag;
            State.ReferencedAt = DateTime.UtcNow;
            await WriteStateAsync();
        }

        public Task<string> GetReferenceTag()
        {
            return Task.FromResult(State.ReferenceTag);
        }

        public Task<DateTime> GetReferencedAt()
        {
            return Task.FromResult(State.ReferencedAt);
        }
    }
}
