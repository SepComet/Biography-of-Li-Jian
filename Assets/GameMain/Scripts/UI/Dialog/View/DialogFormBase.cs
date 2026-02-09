using Definition.Enum;
using Event;
using UnityEngine;

namespace UI
{
    public abstract class DialogFormBase : UGuiForm
    {
        [SerializeField] protected float _playSpeed;

        protected DialogFormContext _context;

        public abstract DialogFormMode UIMode { get; }

        public abstract void StartDialog(DialogFormContext context);

        protected override void OnOpen(object userData)
        {
            base.OnOpen(userData);

            if (!(userData is DialogFormContext context))
            {
                return;
            }

            _context = context;
            StartDialog(context);
        }

        protected override void OnClose(bool isShutdown, object userData)
        {
            _context = null;
            base.OnClose(isShutdown, userData);
        }

        public void OnClickNextLine()
        {
            GameEntry.Event.Fire(this, DialogNextLineRequestEventArgs.Create());
        }

        public void OnClickSkipDialog()
        {
            GameEntry.Event.Fire(this, DialogSkipRequestEventArgs.Create());
        }

        public void OnClickStopDialog()
        {
            GameEntry.Event.Fire(this, DialogStopRequestEventArgs.Create());
        }

        protected static string NormalizeValue(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            if (string.Equals(value, "Null", System.StringComparison.OrdinalIgnoreCase))
            {
                return string.Empty;
            }

            if (string.Equals(value, "None", System.StringComparison.OrdinalIgnoreCase))
            {
                return string.Empty;
            }

            return value;
        }
    }
}
