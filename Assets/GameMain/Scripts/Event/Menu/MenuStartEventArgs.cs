using GameFramework;
using GameFramework.Event;

namespace Event
{
    public class MenuStartEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(MenuStartEventArgs).GetHashCode();

        public override int Id => EventId;

        public MenuStartEventArgs()
        {
        }

        public static MenuStartEventArgs Create()
        {
            return ReferencePool.Acquire<MenuStartEventArgs>();
        }

        public override void Clear()
        {
        }
    }
}