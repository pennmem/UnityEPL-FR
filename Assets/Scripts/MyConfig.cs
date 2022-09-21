public static partial class Config
{
    // InterfaceManager.cs
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


    // RepFRExperiment.cs
    public static int[] wordRepeats { get { return Config.GetSetting<int[]>("wordRepeats"); } }
    public static int[] wordCounts { get { return Config.GetSetting<int[]>("wordCounts"); } }
    public static int[] recallDelay { get { return Config.GetSetting<int[]>("recallDelay"); } }
    public static int[] stimulusInterval { get { return Config.GetSetting<int[]>("stimulusInterval"); } }
    public static int restDuration { get { return Config.GetSetting<int>("restDuration"); } }
    public static int practiceLists { get { return Config.GetSetting<int>("practiceLists"); } }
    public static int preNoStimLists { get { return Config.GetSetting<int>("preNoStimLists"); } }
    public static int encodingOnlyLists { get { return Config.GetSetting<int>("encodingOnlyLists"); } }
    public static int retrievalOnlyLists { get { return Config.GetSetting<int>("retrievalOnlyLists"); } }
    public static int encodingAndRetrievalLists { get { return Config.GetSetting<int>("encodingAndRetrievalLists"); } }
    public static int noStimLists { get { return Config.GetSetting<int>("noStimLists"); } }

    // ltpRepFRExperiment.cs
    public static int[] restLists { get { return Config.GetSetting<int[]>("restLists"); } }

    // FileManager.cs
    public static string dataPath { get { return Config.GetSetting<string>("dataPath"); } }
    public static string wordpool { get { return Config.GetSetting<string>("wordpool"); } }
    public static string prefix { get { return Config.GetSetting<string>("prefix"); } }

    // ExperimentBase.cs
    public static int micTestDuration { get { return Config.GetSetting<int>("micTestDuration"); } }
    public static int distractorDuration { get { return Config.GetSetting<int>("distractorDuration"); } }
    public static int[] orientationDuration { get { return Config.GetSetting<int[]>("orientationDuration"); } }
    public static int recStimulusInterval { get { return Config.GetSetting<int>("recStimulusInterval"); } }
    public static int stimulusDuration { get { return Config.GetSetting<int>("stimulusDuration"); } }
    public static int recallDuration { get { return Config.GetSetting<int>("recallDuration"); } }
    public static int finalRecallDuration { get { return Config.GetSetting<int>("finalRecallDuration"); } }

    // ExperimentSelection.cs
    public static string[] availableExperiments { get { return Config.GetSetting<string[]>("availableExperiments"); } }

    // ElememInterface.cs
    public static string stimMode { get { return Config.GetSetting<string>("stimMode"); } }
    public static int heartbeatInterval { get { return Config.GetSetting<int>("heartbeatInterval"); } }
    
}