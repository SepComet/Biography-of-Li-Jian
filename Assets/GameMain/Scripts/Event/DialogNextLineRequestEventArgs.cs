using GameFramework;
using GameFramework.Event;

namespace Event
{
    public class DialogNextLineRequestEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(DialogNextLineRequestEventArgs).GetHashCode();

        public override int Id => EventId;

        public static DialogNextLineRequestEventArgs Create()
        {
            return ReferencePool.Acquire<DialogNextLineRequestEventArgs>();
        }

        public override void Clear()
        {
        }
    }
}
