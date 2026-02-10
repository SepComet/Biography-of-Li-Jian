using GameFramework;
using GameFramework.Event;

namespace Event
{
    public class MenuContinueEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(MenuContinueEventArgs).GetHashCode();

        public override int Id => EventId;

        public MenuContinueEventArgs()
        {
        }

        public static MenuContinueEventArgs Create()
        {
            return ReferencePool.Acquire<MenuContinueEventArgs>();
        }

        public override void Clear()
        {
        }
    }
}