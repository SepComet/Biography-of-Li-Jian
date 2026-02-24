using CustomComponent;
using GameFramework.Event;
using UnityGameFramework.Runtime;

namespace UI
{
    public class CombineFormController : IFormController<CombineFormContext>
    {
        private CombineComponent _combine;

        private CombineForm _combineForm;

        private int? _formSerialId;

        private CombineFormContext _context;

        public CombineFormController(CombineComponent controller)
        {
            _combine = controller;

            GameEntry.Event.Subscribe(OpenUIFormSuccessEventArgs.EventId, OpenUIFormSuccess);
            GameEntry.Event.Subscribe(CloseUIFormCompleteEventArgs.EventId, CloseUIFormComplete);
        }

        public int? OpenUI(CombineFormContext context)
        {
            if (_combine == null)
            {
                _combine = GameEntry.Combine;
            }

            if (_combine == null)
            {
                Log.Warning("CombineFormController open failed. Controller is null.");
                return null;
            }

            if (context != null)
            {
                _combine.SetFormContext(context);
            }

            _context = _combine.GetFormContext();
            if (_context == null)
            {
                Log.Warning("CombineFormController open failed. Form context is null.");
                return null;
            }

            _formSerialId = GameEntry.UI.OpenUIForm(UIFormId.CombineForm, _context);
            return _formSerialId;
        }

        public void CloseUI()
        {
            if (_formSerialId.HasValue)
            {
                GameEntry.UI.CloseUIForm(_formSerialId.Value);
                return;
            }

            if (_combineForm != null)
            {
                _combineForm.Close();
            }
        }

        ~CombineFormController()
        {
            GameEntry.Event.Unsubscribe(OpenUIFormSuccessEventArgs.EventId, OpenUIFormSuccess);
            GameEntry.Event.Unsubscribe(CloseUIFormCompleteEventArgs.EventId, CloseUIFormComplete);
        }

        private void OpenUIFormSuccess(object sender, GameEventArgs e)
        {
            if (!(e is OpenUIFormSuccessEventArgs args)) return;

            if (args.UserData == _context)
            {
                _combineForm = args.UIForm.Logic as CombineForm;
            }
        }

        private void CloseUIFormComplete(object sender, GameEventArgs e)
        {
            if (!(e is CloseUIFormCompleteEventArgs args)) return;

            if (args.SerialId == _formSerialId)
            {
                _combineForm = null;
                _formSerialId = null;
                _context = null;
            }
        }
    }
}
