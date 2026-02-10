using System;
using GameFramework.Event;
using UnityGameFramework.Runtime;

namespace UI
{
    public class SettingFormController : IFormController<SettingFormContext>
    {
        private SettingFormContext _context;
        private SettingForm _settingForm;
        private int? _formSerialId;
        private bool _pendingRefresh;

        public SettingFormController()
        {
            GameEntry.Event.Subscribe(OpenUIFormSuccessEventArgs.EventId, OnOpenUIFormSuccess);
            GameEntry.Event.Subscribe(CloseUIFormCompleteEventArgs.EventId, OnCloseUIFormComplete);
        }

        public int? OpenUI(SettingFormContext context)
        {
            if (context == null)
            {
                Log.Warning("SettingFormController open failed. context is null.");
                return null;
            }

            _context = context;

            if (_settingForm != null)
            {
                _settingForm.RefreshUI(context);
                return _formSerialId;
            }

            CloseUI();
            _pendingRefresh = true;
            _formSerialId = GameEntry.UI.OpenUIForm(UIFormId.SettingForm, context);
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

            if (_settingForm != null)
            {
                _settingForm.Close();
            }
        }

        ~SettingFormController()
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

            if (_settingForm == null)
            {
                _pendingRefresh = true;
                return;
            }

            _settingForm.RefreshUI(_context);
            _pendingRefresh = false;
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

            _settingForm = args.UIForm.Logic as SettingForm;
            
            if (_settingForm == null)
            {
                Log.Warning("SettingFormController open success but form logic is invalid.");
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

            _settingForm = null;
            _formSerialId = null;
            _pendingRefresh = false;
        }
    }
}