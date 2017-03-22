using System.Threading.Tasks;
using Orleans;

namespace TestGrains
{
    using System;

    /// <summary>
    /// Contract for a grain that references another grain, also persisting other grain's reference into this grain's state.
    /// </summary>
    public interface ICouchBaseWithGrainReferenceStorageGrain : IGrainWithGuidKey
    {
        Task Read();

        /// <summary>
        /// References another grain for inclusion into this grain's state.
        /// </summary>
        /// <param name="referenceTag">Tag to be applied into referenced grain's state.</param>
        Task ReferenceOtherGrain(string referenceTag);

        Task Write();

        Task<string> GetReferencedTag();

        Task<DateTime> GetReferencedAt();

        Task Delete();
    }

    public class ReferencedGrainState
    {
        public IStoredReferenceGrain StoredReferenceGrain { get; set; }
    }

    /// <inheritdoc />
    public class CouchBaseWithGrainReferenceStorageGrain : Grain<ReferencedGrainState>, ICouchBaseWithGrainReferenceStorageGrain
    {
        public override Task OnActivateAsync()
        {
            if (State == null)
            {
                State = new ReferencedGrainState();
            }

            return base.OnActivateAsync();
        }

        public async Task Read()
        {
            await ReadStateAsync();
        }

        public Task ReferenceOtherGrain(string referenceTag)
        {
            //  Reference another grain to include into state persistence
            var referencedGrainId = this.GetPrimaryKey();
            State.StoredReferenceGrain = this.GrainFactory.GetGrain<IStoredReferenceGrain>(referencedGrainId);
            State.StoredReferenceGrain.SetReferenceState(referenceTag);

            return TaskDone.Done;
        }

        public Task Write()
        {
            return WriteStateAsync();
        }

        public async Task Delete()
        {
            await ClearStateAsync();
            State.StoredReferenceGrain = null;
        }

        public async Task<string> GetReferencedTag()
        {
            return await State.StoredReferenceGrain.GetReferenceTag();
        }

        public async Task<DateTime> GetReferencedAt()
        {
            return await State.StoredReferenceGrain.GetReferencedAt();
        }
    }
}
