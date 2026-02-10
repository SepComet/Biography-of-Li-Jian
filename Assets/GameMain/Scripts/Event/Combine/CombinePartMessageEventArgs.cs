using GameFramework;
using GameFramework.Event;

namespace Event
{
    public class CombinePartMessageEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(CombinePartMessageEventArgs).GetHashCode();

        public override int Id => EventId;

        public string Message { get; private set; }

        public CombinePartMessageEventArgs()
        {
            Message = "";
        }

        public static CombinePartMessageEventArgs Create(string message)
        {
            var args = ReferencePool.Acquire<CombinePartMessageEventArgs>();
            
            args.Message = message;
            return args;
        }

        public override void Clear()
        {
            Message = "";
        }
    }
}