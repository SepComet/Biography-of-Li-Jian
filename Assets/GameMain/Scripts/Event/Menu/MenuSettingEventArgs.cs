using GameFramework;
using GameFramework.Event;

namespace Event
{
    public class MenuSettingEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(MenuSettingEventArgs).GetHashCode();

        public override int Id => EventId;

        public MenuSettingEventArgs()
        {
        }

        public static MenuSettingEventArgs Create()
        {
            return ReferencePool.Acquire<MenuSettingEventArgs>();
        }

        public override void Clear()
        {
        }
    }
}