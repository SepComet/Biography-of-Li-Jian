using Definition.Enum;

namespace Definition.DataStruct
{
    public struct GameSetting
    {
        #region SoundConfig

        /// <summary>
        /// 音乐音量
        /// </summary>
        public float BGMVolume { get; set; }

        /// <summary>
        /// 音效音量
        /// </summary>
        public float SEVolume { get; set; }

        #endregion

        #region GameConfig

        /// <summary>
        /// 允许画面震动
        /// </summary>
        public bool AllowShake { get; set; }

        /// <summary>
        /// 允许画面闪光
        /// </summary>
        public bool AllowBlink { get; set; }

        /// <summary>
        /// 对话窗口透明度
        /// </summary>
        public DialogWindowAlpha DialogWindowAlpha { get; set; }

        /// <summary>
        /// 对话播放速度
        /// </summary>
        public DialogPlayingSpeed DialogPlayingSpeed { get; set; }

        #endregion

        #region ScreenConfig

        /// <summary>
        /// 屏幕分辨率
        /// </summary>
        public ScreenResolutionType ScreenResolution { get; set; }

        /// <summary>
        /// 屏幕窗口模式
        /// </summary>
        public ScreenWindowType ScreenWindow { get; set; }

        /// <summary>
        /// 垂直同步
        /// </summary>
        public bool VSync { get; set; }

        /// <summary>
        /// 抗锯齿
        /// </summary>
        public bool AntiAliasing { get; set; }

        #endregion
    }
}