
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Converters;
using System;
using System.IO;
using System.Dynamic;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections.Concurrent;

// This class is thread safe
public static partial class Config {

    // Private Internal Variables
    private const string SYSTEM_CONFIG_NAME = "config.json";
    private static ConcurrentDictionary<string, object> systemConfig = null;
    private static ConcurrentDictionary<string, object> experimentConfig = null;
    private static string configPath = "CONFIG_PATH_NOT_SET";

    // Public Internal Variables
    public static string experimentConfigName = null;
    public static string elememStimMode = "none";

    // System Settings
    public static string niclServerIP { get { return Config.GetSetting<string>("niclServerIP"); } }
    public static int niclServerPort { get { return Config.GetSetting<int>("niclServerPort"); } }
    public static string elememServerIP { get { return Config.GetSetting<string>("elememServerIP"); } }
    public static int elememServerPort { get { return Config.GetSetting<int>("elememServerPort"); } }
    public static bool elememOn { get { return Config.GetSetting<bool>("elememOn"); } }

    // Hardware
    public static bool noSyncbox { get { return Config.GetSetting<bool>("noSyncbox"); } }
    public static bool ps4Controller { get { return Config.GetSetting<bool>("ps4Contoller"); } }

    // Programmer Conveniences
    public static bool lessTrials { get { return (bool)Config.GetSetting<bool>("lessTrials"); } }
    public static bool showFps { get { return Config.GetSetting<bool>("showFps"); } }

    // Game Section Skips
    //public static bool skipIntros { get { return (bool)Config.GetSetting("skipIntros"); } }
    //public static bool skipTownLearning { get { return (bool)Config.GetSetting("skipTownLearning"); } }
    //public static bool skipNewEfrKeypressCheck { get { return (bool)Config.GetSetting("skipNewEfrKeypressCheck"); } }
    //public static bool skipNewEfrKeypressPractice { get { return (bool)Config.GetSetting("skipNewEfrKeypressPractice"); } }

    // Local variables
    public static string participantCode {
        get { return Config.GetSetting<string>("participantCode"); }
        set { Config.SetSetting("participantCode", value); }
    }
    public static int session {
        get { return Config.GetSetting<int>("session"); }
        set { Config.SetSetting("session", value); }
    }
    public static string[] availableExperiments
    {
        get { return Config.GetSetting<string[]>("availableExperiments"); }
        set { Config.SetSetting("availableExperiments", value); }
    }

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

    // ElememInterface.cs
    public static string stimMode { get { return Config.GetSetting<string>("stimMode"); } }
    public static int heartbeatInterval { get { return Config.GetSetting<int>("heartbeatInterval"); } }

    // Functions
    public static void SaveConfigs(ScriptedEventReporter scriptedEventReporter, string path) {
        if (experimentConfig != null)
        {
            if (scriptedEventReporter != null)
                scriptedEventReporter.ReportScriptedEvent("experimentConfig", new Dictionary<string, object>(experimentConfig));
            #if !UNITY_WEBGL // System.IO
                FlexibleConfig.WriteToText(experimentConfig, Path.Combine(path, experimentConfigName + ".json"));
            #endif // !UNITY_WEBGL
        }

        if (systemConfig != null)
        {
            if (scriptedEventReporter != null)
                scriptedEventReporter.ReportScriptedEvent("systemConfig", new Dictionary<string, object>(systemConfig));
            #if !UNITY_WEBGL // System.IO
                FlexibleConfig.WriteToText(systemConfig, Path.Combine(path, SYSTEM_CONFIG_NAME));
            #endif // !UNITY_WEBGL
        }
    }

    public static bool IsExperimentConfigSetup() {
        return experimentConfigName != null;
    }

    // This has to be called before SetupExperimentConfig
    public static void SetupSystemConfig(string configPath) {
        systemConfig = null;
        Config.configPath = configPath;

        #if !UNITY_WEBGL // System.IO
            GetSystemConfig();
        #else // UNITY_WEBGL
            var ucr = new UnityCoroutineRunner();
            ucr.RunCoroutine(SetupOnlineSystemConfig());
        #endif // UNITY_WEBGL
    }

    public static void SetupExperimentConfig() {
        experimentConfig = null;

        #if !UNITY_WEBGL // System.IO
            GetExperimentConfig();
        #else // UNITY_WEBGL
            var ucr = new UnityCoroutineRunner();
            ucr.RunCoroutine(SetupOnlineExperimentConfig());
        #endif // UNITY_WEBGL
    }

#if UNITY_WEBGL // System.IO
    private static IEnumerator SetupOnlineSystemConfig() {
        string systemConfigPath = System.IO.Path.Combine(configPath, SYSTEM_CONFIG_NAME);
        UnityWebRequest systemWWW = UnityWebRequest.Get(systemConfigPath);
        yield return systemWWW.SendWebRequest();

        if (systemWWW.result != UnityWebRequest.Result.Success) {
            Debug.Log("Network error " + systemWWW.error);
        } else {
            var onlineSystemConfigText = systemWWW.downloadHandler.text;
            Debug.Log("Online System Config fetched!!");
            Debug.Log(onlineSystemConfigText);
            systemConfig = new ConcurrentDictionary<string, dynamic>(FlexibleConfig.LoadFromText(onlineSystemConfigText));
        }
    }

    private static IEnumerator SetupOnlineExperimentConfig() {
        string experimentConfigPath = System.IO.Path.Combine(configPath, experimentConfigName);
        UnityWebRequest experimentWWW = UnityWebRequest.Get(experimentConfigPath);
        yield return experimentWWW.SendWebRequest();

        if (experimentWWW.result != UnityWebRequest.Result.Success){
            Debug.Log("Network error " + experimentWWW.error);
        } else {
            var onlineExperimentConfigText = experimentWWW.downloadHandler.text;
            Debug.Log("Online Experiment Config fetched!!");
            Debug.Log(onlineExperimentConfigText);
            experimentConfig = new ConcurrentDictionary<string, dynamic>(FlexibleConfig.LoadFromText(onlineExperimentConfigText));
        }
    }
#endif // UNITY_WEBGL

    private static Nullable<T> GetNullableSetting<T>(string setting) where T: struct{
        object value;

        if (IsExperimentConfigSetup()) {
            var experimentConfig = GetExperimentConfig();
            if (experimentConfig.TryGetValue(setting, out value))
                return (T)value;
        }

        var systemConfig = GetSystemConfig();
        if (systemConfig.TryGetValue(setting, out value))
            return (T)value;

        return null;
    }

    private static T GetSetting<T>(string setting)
    {
        object value;

        if (IsExperimentConfigSetup()) {
            var experimentConfig = GetExperimentConfig();
            if (experimentConfig.TryGetValue(setting, out value))
                return (T) value;
        }

        var systemConfig = GetSystemConfig();
        if (systemConfig.TryGetValue(setting, out value))
            return (T) value;

        string expConfigNotLoaded = IsExperimentConfigSetup() ? "" : "\nNote: Experiment config not loaded yet.";
        throw new MissingFieldException("Missing Config Setting " + setting + "." + expConfigNotLoaded);
    }

    private static void SetSetting<T>(string setting, T value) {
        object getValue;

        if (IsExperimentConfigSetup() && (GetExperimentConfig().TryGetValue(setting, out getValue)))
            // Setting is in Experiment Config
            GetExperimentConfig()[setting] = value;
        else if (GetSystemConfig().TryGetValue(setting, out getValue))
            // Setting is in System Config
            GetSystemConfig()[setting] = value;
        else if (IsExperimentConfigSetup())
            // Setting is not present, so put it in Experiment Config if it is setup
            GetExperimentConfig()[setting] = value;
        else
            // No other options, put it into System Config
            GetSystemConfig()[setting] = value;
    }

    private static IDictionary<string, object> GetSystemConfig()
    {
        if (systemConfig == null)
        {
            // Setup config file
            #if !UNITY_WEBGL // System.IO
                string text = File.ReadAllText(Path.Combine(configPath, SYSTEM_CONFIG_NAME));
                systemConfig = new ConcurrentDictionary<string, dynamic>(FlexibleConfig.LoadFromText(text));
            #else
                if (onlineSystemConfigText == null)
                    Debug.Log("Missing config from web");
                else
                    systemConfig = new ConcurrentDictionary<string, dynamic>(FlexibleConfig.LoadFromText(onlineSystemConfigText));
            #endif
        }
        return (IDictionary<string, object>)systemConfig;
    }

    private static IDictionary<string, object> GetExperimentConfig()
    {
        if (experimentConfig == null)
        {
            // Setup config file
            #if !UNITY_WEBGL // System.IO
                string text = File.ReadAllText(Path.Combine(configPath, experimentConfigName + ".json"));
                experimentConfig = new ConcurrentDictionary<string, dynamic>(FlexibleConfig.LoadFromText(text));
            #else
                if (onlineExperimentConfigText == null)
                    Debug.Log("Missing config from web");
                else
                    experimentConfig = new ConcurrentDictionary<string, dynamic>(FlexibleConfig.LoadFromText(onlineExperimentConfigText));
            #endif
        }
        return (IDictionary<string, object>)experimentConfig;
    }
}


public static class FlexibleConfig {
    public static IDictionary<string, object> LoadFromText(string json) {
        JObject cfg = JObject.Parse(json);
        return CastToStatic(cfg);
    }

    public static void WriteToText(object data, string filename) {
    JsonSerializer serializer = new JsonSerializer();

    using (StreamWriter sw = new StreamWriter(filename))
      using (JsonWriter writer = new JsonTextWriter(sw))
      {
        serializer.Serialize(writer, data);
      }
    }

    public static IDictionary<string, object> CastToStatic(JObject cfg) {
        // casts a JObject consisting of simple types (int, bool, string,
        // float, and single dimensional arrays) to a C# expando object, obviating
        // the need for casts to work in C# native types

        var settings = new ExpandoObject();  // dynamic

        foreach(JProperty prop in cfg.Properties()) {
            // convert from JObject types to .NET internal types
            // and add to dynamic settings object
            // if JSON contains arrays, we need to peek at the
            // type of the contents to get the right cast, as
            // C# doesn't implicitly cast the contents of an
            // array when casting the array

            if(prop.Value is Newtonsoft.Json.Linq.JArray) {
                JTokenType jType = JTokenType.None;

                foreach(JToken child in prop.Value.Children()) {
                    if(jType == JTokenType.None) {
                        jType = child.Type;
                    }
                    else if (jType != child.Type) {
                        throw new Exception("Mixed type arrays not supported");     
                    }
                }

                Type cType = JTypeConversion((int)jType);
                if(cType  == typeof(string)) {
                    ((IDictionary<string, object>)settings).Add(prop.Name, prop.Value.ToObject<string[]>());
                } 
                else if(cType == typeof(int)) {
                    ((IDictionary<string, object>)settings).Add(prop.Name, prop.Value.ToObject<int[]>());
                }
                else if(cType == typeof(float)) {
                    ((IDictionary<string, object>)settings).Add(prop.Name, prop.Value.ToObject<float[]>());
                }
                else if(cType == typeof(bool)) {
                    ((IDictionary<string, object>)settings).Add(prop.Name, prop.Value.ToObject<bool[]>());
                }
            }
            else {
                Type cType = JTypeConversion((int)prop.Value.Type);
                if(cType == typeof(string)) {
                    ((IDictionary<string, object>)settings).Add(prop.Name, prop.Value.ToObject<string>());
                }
                else if(cType == typeof(int)) {
                    ((IDictionary<string, object>)settings).Add(prop.Name, prop.Value.ToObject<int>());
                }
                else if(cType == typeof(float)) {
                    ((IDictionary<string, object>)settings).Add(prop.Name, prop.Value.ToObject<float>());
                }
                else if(cType == typeof(bool)) {
                    ((IDictionary<string, object>)settings).Add(prop.Name, prop.Value.ToObject<bool>());
                }
            }
        }
        return (IDictionary<string, object>)settings;
    }

    public static Type JTypeConversion(int t) {
        switch(t) {
            case 6:
                return typeof(int);
            case 7:
                return typeof(float);
            case 8:
                return typeof(string);
            case 9: 
                return typeof(bool);
            default:
                throw new Exception("Unsupported Type");
        }
    }
}

// This is a dynamic version of the FlexibleConfig class
// This cannot be used right now because WebGL does not support the dynamic keyword
// The moment it does, this version of the class should be used instead of the one above
// Other code will need to be restructured when this is used
// Make sure it stays thread safe though

//public static class FlexibleConfig {
//    public static dynamic LoadFromText(string json) {
//        JObject cfg = JObject.Parse(json);
//        return CastToStatic(cfg);
//    }

//    public static void WriteToText(dynamic data, string filename) {
//    JsonSerializer serializer = new JsonSerializer();

//    using (StreamWriter sw = new StreamWriter(filename))
//      using (JsonWriter writer = new JsonTextWriter(sw))
//      {
//        serializer.Serialize(writer, data);
//      }
//    }

//    private static dynamic CastToStatic(JObject cfg) {
//        // casts a JObject consisting of simple types (int, bool, string,
//        // float, and single dimensional arrays) to a C# expando object, obviating
//        // the need for casts to work in C# native types

//        dynamic settings = new ExpandoObject();

//        foreach(JProperty prop in cfg.Properties()) {
//            // convert from JObject types to .NET internal types
//            // and add to dynamic settings object
//            // if JSON contains arrays, we need to peek at the
//            // type of the contents to get the right cast, as
//            // C# doesn't implicitly cast the contents of an
//            // array when casting the array

//            if(prop.Value is Newtonsoft.Json.Linq.JArray) {
//                JTokenType jType = JTokenType.None;

//                foreach(JToken child in prop.Value.Children()) {
//                    if(jType == JTokenType.None) {
//                        jType = child.Type;
//                    }
//                    else if (jType != child.Type) {
//                        throw new Exception("Mixed type arrays not supported");
//                    }
//                }

//                Type cType = JTypeConversion((int)jType);
//                if(cType  == typeof(string)) {
//                    ((IDictionary<string, dynamic>)settings).Add(prop.Name, prop.Value.ToObject<string[]>());
//                }
//                else if(cType == typeof(int)) {
//                    ((IDictionary<string, dynamic>)settings).Add(prop.Name, prop.Value.ToObject<int[]>());
//                }
//                else if(cType == typeof(float)) {
//                    ((IDictionary<string, dynamic>)settings).Add(prop.Name, prop.Value.ToObject<float[]>());
//                }
//                else if(cType == typeof(bool)) {
//                    ((IDictionary<string, dynamic>)settings).Add(prop.Name, prop.Value.ToObject<bool[]>());
//                }
//            }
//            else {
//                Type cType = JTypeConversion((int)prop.Value.Type);
//                if(cType == typeof(string)) {
//                    ((IDictionary<string, dynamic>)settings).Add(prop.Name, prop.Value.ToObject<string>());
//                }
//                else if(cType == typeof(int)) {
//                    ((IDictionary<string, dynamic>)settings).Add(prop.Name, prop.Value.ToObject<int>());
//                }
//                else if(cType == typeof(float)) {
//                    ((IDictionary<string, dynamic>)settings).Add(prop.Name, prop.Value.ToObject<float>());
//                }
//                else if(cType == typeof(bool)) {
//                    ((IDictionary<string, dynamic>)settings).Add(prop.Name, prop.Value.ToObject<bool>());
//                }
//            }
//        }

//        return settings;
//    }

//    private static Type JTypeConversion(int t) {
//        switch(t) {
//            case 6:
//                return typeof(int);
//            case 7:
//                return typeof(float);
//            case 8:
//                return typeof(string);
//            case 9:
//                return typeof(bool);
//            default:
//                throw new Exception("Unsupported Type");
//        }
//    }
//}
