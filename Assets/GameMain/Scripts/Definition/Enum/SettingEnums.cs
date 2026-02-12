namespace Definition.Enum
{
    public enum DialogPlayingSpeed: byte
    {
        Slow,
        Medium,
        Fast,
    }

    public enum DialogWindowAlpha : byte
    {
        None,
        Low,
        Medium,
        High
    }

    public enum ScreenResolutionType : byte
    {
        _1280x720,
        _1366x768,
        _1600x900,
        _1920x1080,
        _2560x1440,
        _2560x1600
    }

    public enum ScreenWindowType : byte
    {
        Borderless,
        FullScreen,
        Windowed,
    }
}