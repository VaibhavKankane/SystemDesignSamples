using System.Numerics;

namespace MemoryStore
{
    /// <summary>
    /// Always return increasing sequence numbers
    /// </summary>
    internal class LamportClock
    {
        private long _latestTick;

        public LamportClock(long tick)
        {
           _latestTick = tick;
        }

        internal long GetNext()
        {
            _latestTick = Math.Max(DateTime.UtcNow.Ticks, _latestTick);
            _latestTick++;
            return _latestTick;
        }
    }
}