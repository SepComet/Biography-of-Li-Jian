using Definition.DataStruct;
using GameFramework;
using GameFramework.Event;

namespace Event
{
    public class SettingSaveEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(SettingSaveEventArgs).GetHashCode();

        public override int Id => EventId;


        public GameSetting? GameSettings;

        public SettingSaveEventArgs()
        {
        }

        public static SettingSaveEventArgs Create(GameSetting gameSetting)
        {
            var args = ReferencePool.Acquire<SettingSaveEventArgs>();
            args.GameSettings = gameSetting;
            return args;
        }

        public override void Clear()
        {
            GameSettings = null;
        }
    }
}