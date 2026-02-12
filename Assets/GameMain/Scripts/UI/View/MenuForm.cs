using Event;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace UI
{
    public class MenuForm : UGuiForm
    {
        [SerializeField] private GameObject _continueButton;

        public void RefreshUI(MenuFormContext context)
        {
            _continueButton.SetActive(context.HasGameData);
        }

        protected override void OnOpen(object userData)
        {
            base.OnOpen(userData);

            if (!(userData is MenuFormContext context))
            {
                Log.Error("MenuFormContext is invalid.");
                return;
            }

            RefreshUI(context);
        }

        public void OnContinueButtonClick()
        {
            GameEntry.Event.Fire(this, MenuContinueEventArgs.Create());
        }

        public void OnStartButtonClick()
        {
            GameEntry.Event.Fire(this, MenuStartEventArgs.Create());
        }

        public void OnSettingButtonClick()
        {
            GameEntry.Event.Fire(this, MenuSettingEventArgs.Create());
        }

        public void OnExitButtonClick()
        {
            GameEntry.Event.Fire(this, MenuExitEventArgs.Create());
        }
    }
}