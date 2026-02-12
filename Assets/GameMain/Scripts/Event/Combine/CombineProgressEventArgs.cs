using GameFramework;
using GameFramework.Event;

namespace Event
{
    public class CombineProgressEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(CombineProgressEventArgs).GetHashCode();

        public override int Id => EventId;

        public int CurrentStep { get; private set; }

        public int TotalSteps { get; private set; }

        public CombineProgressEventArgs()
        {
            CurrentStep = 0;
            TotalSteps = 0;
        }

        public static CombineProgressEventArgs Create(int currentStep, int totalSteps)
        {
            var args = ReferencePool.Acquire<CombineProgressEventArgs>();

            args.CurrentStep = currentStep;
            args.TotalSteps = totalSteps;
            return args;
        }

        public override void Clear()
        {
            CurrentStep = 0;
            TotalSteps = 0;
        }
    }
}