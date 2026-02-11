using System;
using Definition.DataStruct;
using Definition.Enum;
using Event;
using GameFramework.Event;
using GameFramework.Fsm;
using GameFramework.Procedure;
using Setting;
using Sound;
using UI;
using UnityEngine;
using UnityEngine.Rendering;


namespace Procedure
{
    public class ProcedureMenu : ProcedureBase
    {
        public override bool UseNativeDialog => false;

        private MenuFormController _menuFormController;

        private SettingFormController _settingFormController;

        private const string SettingPrefix = "Setting.";

        private void StartGame()
        {
        }

        private void LoadGame()
        {
        }

        private void OpenSettingForm()
        {
            if (_settingFormController == null)
            {
                _settingFormController = new SettingFormController();
            }

            var settingContext = new SettingFormContext
            {
                Setting = GameEntry.Setting.GetGameSetting()
            };
            _settingFormController.OpenUI(settingContext);
        }

        private void ExitGame()
        {
            Application.Quit();
        }

        #region FSM

        protected override void OnEnter(IFsm<IProcedureManager> procedureOwner)
        {
            base.OnEnter(procedureOwner);

            var e = GameEntry.Event;
            e.Subscribe(MenuStartEventArgs.EventId, MenuStart);
            e.Subscribe(MenuContinueEventArgs.EventId, MenuContinue);
            e.Subscribe(MenuSettingEventArgs.EventId, MenuSetting);
            e.Subscribe(MenuExitEventArgs.EventId, MenuExit);
            e.Subscribe(SettingSaveEventArgs.EventId, SettingSave);


            _menuFormController = new MenuFormController();
            _menuFormController.OpenUI(new MenuFormContext());
        }

        protected override void OnLeave(IFsm<IProcedureManager> procedureOwner, bool isShutdown)
        {
            var e = GameEntry.Event;
            e.Unsubscribe(MenuStartEventArgs.EventId, MenuStart);
            e.Unsubscribe(MenuContinueEventArgs.EventId, MenuContinue);
            e.Unsubscribe(MenuSettingEventArgs.EventId, MenuSetting);
            e.Unsubscribe(MenuExitEventArgs.EventId, MenuExit);
            e.Unsubscribe(SettingSaveEventArgs.EventId, SettingSave);

            base.OnLeave(procedureOwner, isShutdown);
        }

        protected override void OnUpdate(IFsm<IProcedureManager> procedureOwner, float elapseSeconds,
            float realElapseSeconds)
        {
            base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);
        }

        #endregion

        #region Event Handlers

        private void MenuStart(object sender, GameEventArgs e)
        {
            if (!(e is MenuStartEventArgs)) return;
        }

        private void MenuContinue(object sender, GameEventArgs e)
        {
            if (!(e is MenuContinueEventArgs)) return;
        }

        private void MenuSetting(object sender, GameEventArgs e)
        {
            if (!(e is MenuSettingEventArgs)) return;
            OpenSettingForm();
        }

        private void MenuExit(object sender, GameEventArgs e)
        {
            if (!(e is MenuExitEventArgs)) return;
            ExitGame();
        }

        private void SettingSave(object sender, GameEventArgs e)
        {
            if (!(e is SettingSaveEventArgs args)) return;
            
            GameEntry.Sound.SetVolume("BGM", args.GameSettings.BGMVolume);
            GameEntry.Sound.SetVolume("SE", args.GameSettings.SEVolume);
            
            ScreenResolutionType resolution = args.GameSettings.ScreenResolution;
            int width = 0, height = 0;
            switch (resolution)
            {
                case ScreenResolutionType._1280x720:
                    width = 1280;
                    height = 720;
                    break;
                case ScreenResolutionType._1366x768:
                    width = 1366;
                    height = 768;
                    break;
                case ScreenResolutionType._1600x900:
                    width = 1600;
                    height = 900;
                    break;
                case ScreenResolutionType._1920x1080:
                    width = 1920;
                    height = 1080;
                    break;
                case ScreenResolutionType._2560x1440:
                    width = 2560;
                    height = 1440;
                    break;
                case ScreenResolutionType._2560x1600:
                    width = 2560;
                    height = 1600;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            ScreenWindowType resolutionWindow = args.GameSettings.ScreenWindow;
            switch (resolutionWindow)
            {
                case ScreenWindowType.FullScreen:
                    Screen.SetResolution(width, height, FullScreenMode.ExclusiveFullScreen);
                    break;
                case ScreenWindowType.Borderless:
                    Screen.SetResolution(width, height, FullScreenMode.FullScreenWindow);
                    break;
                case ScreenWindowType.Windowed:
                    Screen.SetResolution(width, height, FullScreenMode.Windowed);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            if (args.GameSettings.AntiAliasing)
            {
                foreach (var asset in GraphicsSettings.allConfiguredRenderPipelines)
                {
                    if (asset.name == "URP-AntiAliasing") GraphicsSettings.renderPipelineAsset = asset;
                }
            }
            else
            {
                foreach (var asset in GraphicsSettings.allConfiguredRenderPipelines)
                {
                    if (asset.name == "URP-Normal") GraphicsSettings.renderPipelineAsset = asset;
                }
            }

            Application.targetFrameRate = -1;
            QualitySettings.vSyncCount = args.GameSettings.VSync ? 1 : 0;
            
            GameEntry.Setting.SaveSetting(args.GameSettings);
        }
        
        #endregion
    }
}