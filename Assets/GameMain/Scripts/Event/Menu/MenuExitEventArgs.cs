using GameFramework;
using GameFramework.Event;

namespace Event
{
    public class MenuExitEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(MenuExitEventArgs).GetHashCode();

        public override int Id => EventId;

        public MenuExitEventArgs()
        {
        }

        public static MenuExitEventArgs Create()
        {
            return ReferencePool.Acquire<MenuExitEventArgs>();
        }

        public override void Clear()
        {
        }
    }
}