using Definition.Enum;
using GameFramework.Event;
using UnityGameFramework.Runtime;

namespace UI
{
    public class DialogFormController : IFormController<DialogFormContext>
    {
        private DialogFormContext _context;
        private DialogFormBase _dialogForm;
        private int? _formSerialId;
        private bool _pendingRefresh;

        public DialogFormController()
        {
            GameEntry.Event.Subscribe(OpenUIFormSuccessEventArgs.EventId, OnOpenUIFormSuccess);
            GameEntry.Event.Subscribe(CloseUIFormCompleteEventArgs.EventId, OnCloseUIFormComplete);
        }

        public int? OpenUI(DialogFormContext context)
        {
            if (context == null)
            {
                Log.Warning("DialogFormController open failed. context is null.");
                return null;
            }

            _context = context;

            UIFormId targetFormId = MapDialogFormId(context.DialogUIMode);
            if (targetFormId == UIFormId.Undefined)
            {
                Log.Warning("DialogFormController open failed. Unsupported mode '{0}'.", context.DialogUIMode.ToString());
                return null;
            }

            if (_dialogForm != null && _dialogForm.UIMode == context.DialogUIMode)
            {
                _dialogForm.StartDialog(context);
                return _formSerialId;
            }

            CloseUI();
            _pendingRefresh = true;
            _formSerialId = GameEntry.UI.OpenUIForm(targetFormId, context);
            return _formSerialId;
        }

        public void CloseUI()
        {
            _pendingRefresh = false;

            if (_formSerialId.HasValue)
            {
                GameEntry.UI.CloseUIForm(_formSerialId.Value);
                return;
            }

            if (_dialogForm != null)
            {
                _dialogForm.Close();
            }
        }

        public void OnDialogStarted(DialogFormContext context)
        {
            _context = context;
            TryRefreshUI();
        }

        public void OnDialogLineChanged(DialogFormContext context)
        {
            _context = context;
            TryRefreshUI();
        }

        public void OnDialogEnded(DialogFormContext context)
        {
            _context = context;
        }

        ~DialogFormController()
        {
            GameEntry.Event.Unsubscribe(OpenUIFormSuccessEventArgs.EventId, OnOpenUIFormSuccess);
            GameEntry.Event.Unsubscribe(CloseUIFormCompleteEventArgs.EventId, OnCloseUIFormComplete);
        }

        private void TryRefreshUI()
        {
            if (_context == null)
            {
                return;
            }

            if (_dialogForm == null)
            {
                _pendingRefresh = true;
                return;
            }

            _dialogForm.StartDialog(_context);
            _pendingRefresh = false;
        }

        private static UIFormId MapDialogFormId(DialogFormMode mode)
        {
            switch (mode)
            {
                case DialogFormMode.Mask:
                    return UIFormId.MaskDialogForm;
                case DialogFormMode.BottomBox:
                    return UIFormId.BottomDialogForm;
                case DialogFormMode.BubbleBox:
                    return UIFormId.BottomDialogForm;
                default:
                    return UIFormId.Undefined;
            }
        }

        private void OnOpenUIFormSuccess(object sender, GameEventArgs e)
        {
            if (!(e is OpenUIFormSuccessEventArgs args))
            {
                return;
            }

            if (!_formSerialId.HasValue)
            {
                return;
            }

            if (args.UIForm == null || args.UIForm.SerialId != _formSerialId.Value || args.UserData != _context)
            {
                return;
            }

            _dialogForm = args.UIForm.Logic as DialogFormBase;
            if (_dialogForm == null)
            {
                Log.Warning("DialogFormController open success but form logic is invalid.");
                return;
            }

            if (_pendingRefresh)
            {
                TryRefreshUI();
            }
        }

        private void OnCloseUIFormComplete(object sender, GameEventArgs e)
        {
            if (!(e is CloseUIFormCompleteEventArgs args))
            {
                return;
            }

            if (args.SerialId != _formSerialId)
            {
                return;
            }

            _dialogForm = null;
            _formSerialId = null;
            _pendingRefresh = false;
        }
    }
}
