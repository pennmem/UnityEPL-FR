public static partial class Config
{
    public static bool isTest { get { return Config.GetSetting<bool>("isTest"); } }
    public static int? eventsPerFrame { get { return Config.GetNullableSetting<int>("eventsPerFrame"); } }
    public static int vSync { get { return Config.GetSetting<int>("vSync"); } }
    public static int frameRate { get { return Config.GetSetting<int>("frameRate"); } }

    public static string experimentScene { get { return Config.GetSetting<string>("experimentScene"); } }
    public static bool elemem { get { return Config.GetSetting<bool>("elemem"); } }
    public static bool ramulator { get { return Config.GetSetting<bool>("ramulator"); } }

    public static string experimentClass { get { return Config.GetSetting<string>("experimentClass"); } }
    public static string launcherScene { get { return Config.GetSetting<string>("launcherScene"); } }
    public static string video { get { return Config.GetSetting<string>("video"); } }
    public static string experimentName { get { return Config.GetSetting<string>("experimentName"); } }
    public static string participantCode { get { return Config.GetSetting<string>("participantCode"); } }
    public static int session { get { return Config.GetSetting<int>("session"); } }
}