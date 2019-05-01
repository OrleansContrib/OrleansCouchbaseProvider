using System.Threading.Tasks;
using Orleans;
using Orleans.Providers;

namespace TestGrains
{
    public interface ICouchbaseStorageGrain : IGrainWithGuidKey
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

    public class CouchbaseStorageGrain : Grain<StorageData>, ICouchbaseStorageGrain
    {
        public override Task OnActivateAsync()
        {
            if (State == null)
                State = new StorageData();
            return base.OnActivateAsync();
        }

        public Task<int> GetValue()
        {
            return Task.FromResult(State.value);
        }

        public Task Read()
        {
            return ReadStateAsync();
        }

        public Task Write(int value)
        {
            State.value = value;
            return WriteStateAsync();
        }

        public Task SetValue(int val)
        {
            State.value = val;
            return TaskDone.Done;
        }

        public async Task Delete()
        {
            await ClearStateAsync();
            State.value = 0;
        }
    }
}
