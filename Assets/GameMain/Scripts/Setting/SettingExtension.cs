using System.Data;
using Definition;
using Definition.DataStruct;
using Definition.Enum;
using UnityGameFramework.Runtime;

namespace Setting
{
    public static class SettingExtension
    {
        public static GameSetting GetGameSetting(this SettingComponent setting)
        {
            var data = new GameSetting
            {
                BGMVolume = setting.GetFloat(Constant.Setting.BGMVolume, 0.6f),
                SEVolume = setting.GetFloat(Constant.Setting.SEVolume, 0.6f),

                AllowShake = setting.GetBool(Constant.Setting.AllowShake, true),
                AllowBlink = setting.GetBool(Constant.Setting.AllowBlink, true),
                DialogWindowAlpha = (DialogWindowAlpha)setting.GetInt(Constant.Setting.DialogWindowAlpha, 2),
                DialogPlayingSpeed = (DialogPlayingSpeed)setting.GetInt(Constant.Setting.DialogPlayingSpeed, 1),

                ScreenResolution = (ScreenResolutionType)setting.GetInt(Constant.Setting.ScreenSolution, 1),
                ScreenWindow = (ScreenWindowType)setting.GetInt(Constant.Setting.ScreenWindow, 2),
                VSync = setting.GetBool(Constant.Setting.VSync, true),
                AntiAliasing = setting.GetBool(Constant.Setting.AntiAliasing, true)
            };

            return data;
        }

        public static void SaveSetting(this SettingComponent setting, GameSetting data)
        {
            setting.SetFloat(Constant.Setting.BGMVolume, data.BGMVolume);
            setting.SetFloat(Constant.Setting.SEVolume, data.SEVolume);

            setting.SetBool(Constant.Setting.AllowShake, data.AllowShake);
            setting.SetBool(Constant.Setting.AllowBlink, data.AllowBlink);
            setting.SetInt(Constant.Setting.DialogWindowAlpha, (int)data.DialogWindowAlpha);
            setting.SetInt(Constant.Setting.DialogPlayingSpeed, (int)data.DialogPlayingSpeed);

            setting.SetInt(Constant.Setting.ScreenSolution, (int)data.ScreenResolution);
            setting.SetInt(Constant.Setting.ScreenWindow, (int)data.ScreenWindow);
            setting.SetBool(Constant.Setting.VSync, data.VSync);
            setting.SetBool(Constant.Setting.AntiAliasing, data.AntiAliasing);
            
            setting.Save();
        }
    }
}