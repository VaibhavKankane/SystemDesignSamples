using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemoryStore.Common
{
    public static class Constants
    {
        public const string LeaderHeader = "FromLeader";
        public const string ServiceRootNodeInZooKeeper = "/Services";
        public const string MemoryServiceNodeInZooKeeper = "MemoryStoreService";
        public const string BaseAddressOfReplicaWithoutPort = "http://host.docker.internal:";
    }
}
