using System;
using GameFramework;
using GameFramework.Event;
using GameFramework.Resource;
using System.Collections.Generic;
using System.Linq;
using CustomUtility;
using DataTable;
using Definition;
using Definition.Enum;
using Setting;
using Sound;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.Rendering;
using UnityGameFramework.Runtime;
using ProcedureOwner = GameFramework.Fsm.IFsm<GameFramework.Procedure.IProcedureManager>;

namespace Procedure
{
    public class ProcedurePreload : ProcedureBase
    {
        public static readonly string[] DataTableNames = new string[]
        {
            "Entity",
            "BGM",
            "Scene",
            "UIForm",
            "SE",
            "Dialog",
            "DialogLine"
        };

        private Dictionary<string, bool> _loadedFlag = new Dictionary<string, bool>();

        public override bool UseNativeDialog => true;

        protected override void OnEnter(ProcedureOwner procedureOwner)
        {
            base.OnEnter(procedureOwner);

            GameEntry.Event.Subscribe(LoadConfigSuccessEventArgs.EventId, OnLoadConfigSuccess);
            GameEntry.Event.Subscribe(LoadConfigFailureEventArgs.EventId, OnLoadConfigFailure);
            GameEntry.Event.Subscribe(LoadDataTableSuccessEventArgs.EventId, OnLoadDataTableSuccess);
            GameEntry.Event.Subscribe(LoadDataTableFailureEventArgs.EventId, OnLoadDataTableFailure);
            GameEntry.Event.Subscribe(LoadDictionarySuccessEventArgs.EventId, OnLoadDictionarySuccess);
            GameEntry.Event.Subscribe(LoadDictionaryFailureEventArgs.EventId, OnLoadDictionaryFailure);

            _loadedFlag.Clear();

            PreloadResources();
        }

        protected override void OnLeave(ProcedureOwner procedureOwner, bool isShutdown)
        {
            GameEntry.Event.Unsubscribe(LoadConfigSuccessEventArgs.EventId, OnLoadConfigSuccess);
            GameEntry.Event.Unsubscribe(LoadConfigFailureEventArgs.EventId, OnLoadConfigFailure);
            GameEntry.Event.Unsubscribe(LoadDataTableSuccessEventArgs.EventId, OnLoadDataTableSuccess);
            GameEntry.Event.Unsubscribe(LoadDataTableFailureEventArgs.EventId, OnLoadDataTableFailure);
            GameEntry.Event.Unsubscribe(LoadDictionarySuccessEventArgs.EventId, OnLoadDictionarySuccess);
            GameEntry.Event.Unsubscribe(LoadDictionaryFailureEventArgs.EventId, OnLoadDictionaryFailure);

            base.OnLeave(procedureOwner, isShutdown);
        }

        protected override void OnUpdate(ProcedureOwner procedureOwner, float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);

            foreach (KeyValuePair<string, bool> loadedFlag in _loadedFlag)
            {
                if (!loadedFlag.Value)
                {
                    return;
                }
            }

            procedureOwner.SetData<VarInt32>("NextSceneId", (int)SceneId.Menu);
            ChangeState<ProcedureChangeScene>(procedureOwner);
        }

        private void PreloadResources()
        {
            // Preload data tables
            foreach (string dataTableName in DataTableNames)
            {
                LoadDataTable(dataTableName);
            }

            // Preload dictionaries
            // LoadDictionary("Default");

            // Preload fonts
            LoadFont("MainFont");
            LoadTMPFont("MainTMPFont");

            LoadSetting();
        }

        private void LoadDataTable(string dataTableName)
        {
            string dataTableAssetName = AssetUtility.GetDataTableAsset(dataTableName, false);
            _loadedFlag.Add(dataTableAssetName, false);
            GameEntry.DataTable.LoadDataTable(dataTableName, dataTableAssetName, this);
        }

        private void LoadDictionary(string dictionaryName)
        {
            string dictionaryAssetName = AssetUtility.GetDictionaryAsset(dictionaryName, false);
            _loadedFlag.Add(dictionaryAssetName, false);
            GameEntry.Localization.ReadData(dictionaryAssetName, this);
        }

        private void LoadFont(string fontName)
        {
            _loadedFlag.Add(Utility.Text.Format("Font.{0}", fontName), false);
            GameEntry.Resource.LoadAsset(AssetUtility.GetFontAsset(fontName), Constant.AssetPriority.FontAsset,
                new LoadAssetCallbacks(
                    (assetName, asset, duration, userData) =>
                    {
                        _loadedFlag[Utility.Text.Format("Font.{0}", fontName)] = true;
                        UGuiForm.SetMainFont((Font)asset);
                        Log.Info("Load font '{0}' OK.", fontName);
                    },
                    (assetName, status, errorMessage, userData) =>
                    {
                        Log.Error("Can not load font '{0}' from '{1}' with error message '{2}'.", fontName, assetName,
                            errorMessage);
                    }));
        }

        private void LoadTMPFont(string fontName)
        {
            _loadedFlag.Add(Utility.Text.Format("Font.{0}", fontName), false);
            GameEntry.Resource.LoadAsset(AssetUtility.GetTMPFontAsset(fontName), Constant.AssetPriority.FontAsset,
                new LoadAssetCallbacks(
                    (assetName, asset, duration, userData) =>
                    {
                        _loadedFlag[Utility.Text.Format("Font.{0}", fontName)] = true;
                        UGuiForm.SetMainTMPFont((TMP_FontAsset)asset);
                        Log.Info("Load font '{0}' OK.", fontName);
                    },
                    (assetName, status, errorMessage, userData) =>
                    {
                        Log.Error("Can not load font '{0}' from '{1}' with error message '{2}'.", fontName, assetName,
                            errorMessage);
                    }));
        }

        private void LoadSetting()
        {
            var setting = GameEntry.Setting.GetGameSetting();

            GameEntry.Sound.SetVolume("BGM", setting.BGMVolume);
            GameEntry.Sound.SetVolume("SE", setting.SEVolume);

            ScreenResolutionType resolution = setting.ScreenResolution;
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

            ScreenWindowType resolutionWindow = setting.ScreenWindow;
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

            Application.targetFrameRate = -1;
            QualitySettings.vSyncCount = setting.VSync ? 1 : 0;
            if (setting.AntiAliasing)
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
        }

        #region Event Hanlders

        private void OnLoadConfigSuccess(object sender, GameEventArgs e)
        {
            LoadConfigSuccessEventArgs ne = (LoadConfigSuccessEventArgs)e;
            if (ne.UserData != this)
            {
                return;
            }

            _loadedFlag[ne.ConfigAssetName] = true;
            Log.Info("Load config '{0}' OK.", ne.ConfigAssetName);
        }

        private void OnLoadConfigFailure(object sender, GameEventArgs e)
        {
            LoadConfigFailureEventArgs ne = (LoadConfigFailureEventArgs)e;
            if (ne.UserData != this)
            {
                return;
            }

            Log.Error("Can not load config '{0}' from '{1}' with error message '{2}'.", ne.ConfigAssetName,
                ne.ConfigAssetName, ne.ErrorMessage);
        }

        private void OnLoadDataTableSuccess(object sender, GameEventArgs e)
        {
            LoadDataTableSuccessEventArgs ne = (LoadDataTableSuccessEventArgs)e;
            if (ne.UserData != this)
            {
                return;
            }

            _loadedFlag[ne.DataTableAssetName] = true;
            Log.Info("Load data table '{0}' OK.", ne.DataTableAssetName);
        }

        private void OnLoadDataTableFailure(object sender, GameEventArgs e)
        {
            LoadDataTableFailureEventArgs ne = (LoadDataTableFailureEventArgs)e;
            if (ne.UserData != this)
            {
                return;
            }

            Log.Error("Can not load data table '{0}' from '{1}' with error message '{2}'.", ne.DataTableAssetName,
                ne.DataTableAssetName, ne.ErrorMessage);
        }

        private void OnLoadDictionarySuccess(object sender, GameEventArgs e)
        {
            LoadDictionarySuccessEventArgs ne = (LoadDictionarySuccessEventArgs)e;
            if (ne.UserData != this)
            {
                return;
            }

            _loadedFlag[ne.DictionaryAssetName] = true;
            Log.Info("Load dictionary '{0}' OK.", ne.DictionaryAssetName);
        }

        private void OnLoadDictionaryFailure(object sender, GameEventArgs e)
        {
            LoadDictionaryFailureEventArgs ne = (LoadDictionaryFailureEventArgs)e;
            if (ne.UserData != this)
            {
                return;
            }

            Log.Error("Can not load dictionary '{0}' from '{1}' with error message '{2}'.", ne.DictionaryAssetName,
                ne.DictionaryAssetName, ne.ErrorMessage);
        }

        #endregion
    }
}