using GameFramework.Event;
using UnityGameFramework.Runtime;

namespace UI
{
    public class MenuFormController : IFormController<MenuFormContext>
    {
        private MenuFormContext _context;
        private MenuForm _menuForm;
        private int? _menuFormSerialId;
        private bool _pendingRefresh;

        public MenuFormController()
        {
            GameEntry.Event.Subscribe(OpenUIFormSuccessEventArgs.EventId, OpenUIFormSuccess);
            GameEntry.Event.Subscribe(CloseUIFormCompleteEventArgs.EventId, CloseUIFormComplete);
        }


        public int? OpenUI(MenuFormContext context)
        {
            if (context == null)
            {
                Log.Warning("MenuFormController open failed. context is null.");
                return null;
            }

            _context = context;

            if (_menuForm != null)
            {
                _menuForm.RefreshUI(_context);
                return _menuFormSerialId;
            }

            CloseUI();
            _pendingRefresh = true;
            _menuFormSerialId = GameEntry.UI.OpenUIForm(UIFormId.MenuForm, context);
            return _menuFormSerialId;
        }

        public void CloseUI()
        {
            _pendingRefresh = false;

            if (_menuFormSerialId.HasValue)
            {
                GameEntry.UI.CloseUIForm(_menuFormSerialId.Value);
                return;
            }

            if (_menuForm != null)
            {
                _menuForm.Close();
            }
        }

        private void TryRefreshUI()
        {
            if (_context == null)
            {
                return;
            }

            if (_menuForm == null)
            {
                _pendingRefresh = true;
                return;
            }

            _menuForm.RefreshUI(_context);
            _pendingRefresh = false;
        }

        ~MenuFormController()
        {
            GameEntry.Event.Unsubscribe(OpenUIFormSuccessEventArgs.EventId, OpenUIFormSuccess);
            GameEntry.Event.Unsubscribe(CloseUIFormCompleteEventArgs.EventId, CloseUIFormComplete);
        }

        #region EventHanlders

        private void OpenUIFormSuccess(object sender, GameEventArgs e)
        {
            if (!(e is OpenUIFormSuccessEventArgs args))
            {
                return;
            }

            if (!_menuFormSerialId.HasValue)
            {
                return;
            }

            if (args.UIForm == null || args.UIForm.SerialId != _menuFormSerialId.Value || args.UserData != _context)
            {
                return;
            }

            _menuForm = args.UIForm.Logic as MenuForm;
            
            if (_menuForm == null)
            {
                Log.Warning("DialogFormController open success but form logic is invalid.");
                return;
            }

            if (_pendingRefresh)
            {
                TryRefreshUI();
            }
        }
        
        private void CloseUIFormComplete(object sender, GameEventArgs e)
        {
            if (!(e is CloseUIFormCompleteEventArgs args))
            {
                return;
            }

            if (args.SerialId != _menuFormSerialId)
            {
                return;
            }

            _menuForm = null;
            _menuFormSerialId = null;
        }

        #endregion
    }
}