using MemoryStore.ZooKeeper;

namespace MemoryStore.Common
{
    public delegate void OnNodeChildrenUpdatedHandler(List<string> children);

    public interface INodeWatcher
    {
        Task RegisterWatchAsync(string path);
        event OnNodeChildrenUpdatedHandler OnNodeChildrenUpdated;
    }
}