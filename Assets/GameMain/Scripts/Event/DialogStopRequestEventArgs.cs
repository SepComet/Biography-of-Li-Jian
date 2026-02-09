using GameFramework;
using GameFramework.Event;

namespace Event
{
    public class DialogStopRequestEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(DialogStopRequestEventArgs).GetHashCode();

        public override int Id => EventId;

        public static DialogStopRequestEventArgs Create()
        {
            return ReferencePool.Acquire<DialogStopRequestEventArgs>();
        }

        public override void Clear()
        {
        }
    }
}
