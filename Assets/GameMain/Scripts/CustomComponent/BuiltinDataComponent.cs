//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFramework;
using StarForce;
using UI;
using UnityEngine;
using UnityEngine.Serialization;
using UnityGameFramework.Runtime;

namespace CustomComponent
{
    public class BuiltinDataComponent : GameFrameworkComponent
    {
        [FormerlySerializedAs("m_BuildInfoTextAsset")] [SerializeField] private TextAsset _buildInfoTextAsset = null;

        [FormerlySerializedAs("m_DefaultDictionaryTextAsset")] [SerializeField] private TextAsset _defaultDictionaryTextAsset = null;

        [FormerlySerializedAs("m_UpdateResourceFormTemplate")] [SerializeField] private UpdateResourceForm _updateResourceFormTemplate = null;

        private BuildInfo _buildInfo = null;

        public BuildInfo BuildInfo => _buildInfo;

        public UpdateResourceForm UpdateResourceFormTemplate => _updateResourceFormTemplate;

        public void InitBuildInfo()
        {
            if (_buildInfoTextAsset == null || string.IsNullOrEmpty(_buildInfoTextAsset.text))
            {
                Log.Info("Build info can not be found or empty.");
                return;
            }

            _buildInfo = Utility.Json.ToObject<BuildInfo>(_buildInfoTextAsset.text);
            if (_buildInfo == null)
            {
                Log.Warning("Parse build info failure.");
                return;
            }
        }

        public void InitDefaultDictionary()
        {
            if (_defaultDictionaryTextAsset == null || string.IsNullOrEmpty(_defaultDictionaryTextAsset.text))
            {
                Log.Info("Default dictionary can not be found or empty.");
                return;
            }

            if (!GameEntry.Localization.ParseData(_defaultDictionaryTextAsset.text))
            {
                Log.Warning("Parse default dictionary failure.");
                return;
            }
        }
    }
}