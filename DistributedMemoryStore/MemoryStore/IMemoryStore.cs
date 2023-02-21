namespace MemoryStore
{
    public interface IMemoryStore
    {
        MemoryStoreOperationResult Add(string key, string value);

        MemoryStoreOperationResult Update(string key, string newValue, string oldValue);

        string Get(string key);

        MemoryStoreOperationResult Delete(string key);
    }
}