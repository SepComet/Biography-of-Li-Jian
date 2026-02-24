using Definition.DataStruct;
using Definition.Enum;
using Event;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

namespace UI
{
    public class SettingForm : UGuiForm
    {
        [SerializeField] private Image[] _navigateButtonImages;

        [SerializeField] private TMP_Text[] _navigateButtonTexts;

        [SerializeField] private Color _darkColor;

        [SerializeField] private Color _brightColor;

        [SerializeField] private Slider _bgmVolumeSlider;

        [SerializeField] private Slider _seVolumeSlider;

        [SerializeField] private HorizonSelectGroup _allowShakeGroup;

        [SerializeField] private HorizonSelectGroup _allowBlinkGroup;

        [SerializeField] private HorizonSelectGroup _dialogWindowAlpha;

        [SerializeField] private HorizonSelectGroup _playingSpeed;

        [SerializeField] private HorizonSelectGroup _screenSolution;

        [SerializeField] private HorizonSelectGroup _screenWindow;

        [SerializeField] private HorizonSelectGroup _vSyncGroup;

        [SerializeField] private HorizonSelectGroup _antiAliasingGroup;

        private SettingFormController _controller;

        public void RefreshUI(SettingFormContext context)
        {
            _controller = context.Controller;

            bool isMobilePlatform = Application.isMobilePlatform;
            _screenSolution.gameObject.SetActive(!isMobilePlatform);
            _screenWindow.gameObject.SetActive(!isMobilePlatform);

            var setting = context.Setting;

            _bgmVolumeSlider.value = setting.BGMVolume * 5;
            _seVolumeSlider.value = setting.SEVolume * 5;

            _allowBlinkGroup.SetValue(setting.AllowBlink);
            _allowShakeGroup.SetValue(setting.AllowShake);
            _dialogWindowAlpha.SetValue((int)setting.DialogWindowAlpha);
            _playingSpeed.SetValue((int)setting.DialogPlayingSpeed);

            _screenSolution.SetValue((int)setting.ScreenResolution);
            _screenWindow.SetValue((int)setting.ScreenWindow);
            _vSyncGroup.SetValue(setting.VSync);
            _antiAliasingGroup.SetValue(setting.AntiAliasing);
        }

        protected override void OnOpen(object userData)
        {
            base.OnOpen(userData);

            if (!(userData is SettingFormContext context))
            {
                Log.Error("SettingFormContext is invalid.");
                return;
            }

            RefreshUI(context);
        }

        private GameSetting CollectSetting()
        {
            var setting = new GameSetting
            {
                BGMVolume = _bgmVolumeSlider.value / 5,
                SEVolume = _seVolumeSlider.value / 5,

                AllowShake = _allowShakeGroup.GetBoolValue(),
                AllowBlink = _allowBlinkGroup.GetBoolValue(),
                DialogWindowAlpha = (DialogWindowAlpha)_dialogWindowAlpha.GetIntValue(),
                DialogPlayingSpeed = (DialogPlayingSpeed)_playingSpeed.GetIntValue(),

                ScreenResolution = (ScreenResolutionType)_screenSolution.GetIntValue(),
                ScreenWindow = (ScreenWindowType)_screenWindow.GetIntValue(),
                VSync = _vSyncGroup.GetBoolValue(),
                AntiAliasing = _antiAliasingGroup.GetBoolValue()
            };

            return setting;
        }

        public void OnNavigateButtonClick(int index)
        {
            if (index < 0 || index >= _navigateButtonImages.Length)
            {
                Log.Error("NavigateButtonClick index is out of range.");
            }

            for (int i = 0; i < _navigateButtonImages.Length; i++)
            {
                _navigateButtonImages[i].color = i != index ? _brightColor : _darkColor;
                _navigateButtonTexts[i].color = i != index ? _darkColor : _brightColor;
            }
        }

        public void OnReturnButtonClick()
        {
            var dialogParams = new DialogParams
            {
                Title = "确认是否返回",
                Message = "是否保存更改？",
                ConfirmText = "保存并返回",
                CancelText = "直接返回",
                OtherText = "取消",
                Mode = 3,
            };
            dialogParams.OnClickConfirm += SaveSettingAndReturn;
            dialogParams.OnClickCancel += _ => _controller.CloseUI();

            GameEntry.UI.OpenUIForm(UIFormId.DialogForm, dialogParams);
        }

        private void SaveSettingAndReturn(object userData)
        {
            var setting = CollectSetting();
            GameEntry.Event.Fire(this, SettingSaveEventArgs.Create(setting));
            _controller.CloseUI();
        }
    }
}