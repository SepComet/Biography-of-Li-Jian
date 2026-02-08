using GameFramework;
using GameFramework.Event;

namespace Event
{
    public class CombineGuideMessageEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(CombineGuideMessageEventArgs).GetHashCode();

        public override int Id => EventId;

        public string Message { get; private set; }

        public CombineGuideMessageEventArgs()
        {
            Message = "";
        }

        public static CombineGuideMessageEventArgs Create(string message)
        {
            var args = ReferencePool.Acquire<CombineGuideMessageEventArgs>();
            
            args.Message = message;
            return args;
        }

        public override void Clear()
        {
            Message = "";
        }
    }
}