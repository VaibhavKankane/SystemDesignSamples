using System.Collections.Concurrent;

namespace MemoryStore
{
    public enum MemoryStoreOperationResult
    {
        Success = 0,
        Failed_AlreadyExist,
        Failed_NotExist
    }

    public class MemoryStoreDictImpl : IMemoryStore
    {
        readonly ConcurrentDictionary<string, string> _store;

        public MemoryStoreDictImpl()
        {
            _store = new ConcurrentDictionary<string, string>();
        }

        public MemoryStoreOperationResult Add(string key, string value)
        {
            var result = _store.TryAdd(key, value);
            if (!result)
                return MemoryStoreOperationResult.Failed_AlreadyExist;

            return MemoryStoreOperationResult.Success;
        }

        public MemoryStoreOperationResult Update(string key, string newValue, string oldValue)
        {
            var result = _store.TryUpdate(key, newValue, oldValue);
            if (!result)
                return MemoryStoreOperationResult.Failed_NotExist;

            return MemoryStoreOperationResult.Success;
        }

        public string Get(string key)
        {
            var found = _store.TryGetValue(key, out string value);
            if(found)
                return value;
            else
                return string.Empty;
        }

        public MemoryStoreOperationResult Delete(string key)
        {
            var result = _store.TryRemove(key, out _);
            if (!result)
                return MemoryStoreOperationResult.Failed_NotExist;

            return MemoryStoreOperationResult.Success;
        }
    }
}
