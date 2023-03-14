namespace MemoryStore
{
    public interface IMemoryStore
    {
        MemoryStoreOperationResult Add(string key, string value);

        // Keeping it simple with only read, write and delete
        //MemoryStoreOperationResult Update(string key, string newValue, string oldValue);

        string Get(string key);

        MemoryStoreOperationResult Delete(string key);
    }
}