using System.Threading.Tasks;
using Orleans;
using Orleans.Providers;

namespace TestGrains
{
    public interface ICouchBaseStorageGrain : IGrainWithGuidKey
    {
        Task Read();
        Task<int> GetValue();
        Task Write(int value);
        Task SetValue(int val);
        Task Delete();
    }

    public class StorageData
    {
        public int value;
    }

    public class CouchBaseStorageGrain : Grain<StorageData>, ICouchBaseStorageGrain
    {
        private StorageData state;

        public override Task OnActivateAsync()
        {
            if (state == null)
                state = new StorageData();
            return base.OnActivateAsync();
        }

        public Task<int> GetValue()
        {
            return Task.FromResult(state.value);
        }

        public Task Read()
        {
            return ReadStateAsync();
        }

        public Task Write(int value)
        {
            state.value = value;
            return WriteStateAsync();
        }

        public Task SetValue(int val)
        {
            state.value = val;
            return TaskDone.Done;
        }

        public async Task Delete()
        {
            await ClearStateAsync();
            state.value = 0;
        }
    }
}
