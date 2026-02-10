using GameFramework;
using GameFramework.Event;

namespace Event
{
    public class DialogSkipRequestEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(DialogSkipRequestEventArgs).GetHashCode();

        public override int Id => EventId;

        public static DialogSkipRequestEventArgs Create()
        {
            return ReferencePool.Acquire<DialogSkipRequestEventArgs>();
        }

        public override void Clear()
        {
        }
    }
}
