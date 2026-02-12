using GameFramework;
using GameFramework.Event;

namespace Event
{
    public class CombineCompletedEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(CombineCompletedEventArgs).GetHashCode();

        public override int Id => EventId;

        public CombineCompletedEventArgs()
        {
        }

        public static CombineCompletedEventArgs Create()
        {
            return ReferencePool.Acquire<CombineCompletedEventArgs>();
        }

        public override void Clear()
        {
        }
    }
}