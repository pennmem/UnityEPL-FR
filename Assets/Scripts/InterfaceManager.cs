using System;
using System.IO;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine.SceneManagement;
using Newtonsoft.Json.Linq;
using UnityEngine;

// It is up to objects that are referenced in this class to 
// have adequate protection levels on all members, as classes
// with a reference to manager can call functions from or pass events
// to classes referenced here.

public class InterfaceManager : MonoBehaviour 
{
    private static string quitKey = "escape"; // escape to quit
    const string SYSTEM_CONFIG = "config.json";

    //////////
    // Singleton Boilerplate
    // makes sure that only one Experiment Manager
    // can exist in a scene and that this object
    // is not destroyed when changing scenes
    //////////

    private static InterfaceManager _instance;

    // pass references, rather than relying on Global
    //    public static InterfaceManager Instance { get { return _instance; } }

    protected  void Awake()
    {
        if (_instance != null && _instance != this)
        {
            throw new System.InvalidOperationException("Cannot create multiple InterfaceManager Objects");
        } 
        else {
            _instance = this;
            DontDestroyOnLoad(this.gameObject);
            DontDestroyOnLoad(warning);
        }

        ErrorNotification.mainThread = this;
    }

    //////////
    // Non-unity event handling for scripts to 
    // activate InterfaceManager functions
    //////////
    private EventQueue mainEvents = new EventQueue();
    public static implicit operator EventQueue(InterfaceManager im) => im.mainEvents;

    // queue to store key handlers before key event
    private ConcurrentQueue<Action<string, bool>> onKey;

    //////////
    // Experiment Settings and Experiment object
    // that is instantiated once launch is called
    ////////// 
    // global random number source
    public static System.Random rnd = new System.Random();

    // system configurations, generated on the fly by
    // FlexibleConfig
    private object configLock = new object(); // TODO: make access in-thread only
    public JObject systemConfig = null;
    public JObject experimentConfig = null;
    private ExperimentBase exp;

    public FileManager fileManager;

    //////////
    // Known experiment GameObjects to
    // check for and collect when changing
    // scenes. These are made available to 
    // other scripts instantiated by
    // Experiment Manager.
    //////////

    //////////
    // Devices that can be accessed by managed
    // scripts
    //////////
    public IHostPC hostPC;
    public NonUnitySyncbox syncBox;
    public VideoControl videoControl;
    public TextDisplayer textDisplayer;
    public SoundRecorder recorder;
    public GameObject warning;

    public AudioSource highBeep;
    public AudioSource lowBeep;
    public AudioSource lowerBeep;
    public AudioSource playback;

    public RamulatorInterface ramulator;

    //////////
    // Input reporters
    //////////

    public VoiceActivityDetection voiceActity;
    public ScriptedEventReporter scriptedInput;
    public PeripheralInputReporter peripheralInput;
    public UIDataReporter uiInput;
    private int eventsPerFrame;

    // Start is called before the first frame update
    void Start()
    {
        // Unity interal event handling
        SceneManager.sceneLoaded += onSceneLoaded;

        // create objects not tied to unity
        fileManager = new FileManager(this);
        syncBox = new NonUnitySyncbox(this);
        onKey = new ConcurrentQueue<Action<string, bool>>();

        // load system configuration file // TODO: function
        string text = System.IO.File.ReadAllText(System.IO.Path.Combine(fileManager.ConfigPath(), SYSTEM_CONFIG));

        lock(configLock) {
            systemConfig = FlexibleConfig.LoadFromText(text);
        }

        // Get all configuration files
        string configPath = fileManager.ConfigPath();
        string[] configs = Directory.GetFiles(configPath, "*.json");
        if(configs.Length < 2) {
            // TODO: notify
            ShowWarning("Configuration File Error", 5000);
            DoIn(new EventBase(Quit), 5000);            
        }

        JArray exps = new JArray();

        for(int i=0, j=0; i<configs.Length; i++) {
            Debug.Log(configs[i]);
            if(!configs[i].Contains(SYSTEM_CONFIG))
                exps.Add(Path.GetFileNameWithoutExtension(configs[i]));
                j++;
        }
        ChangeSetting("availableExperiments", exps);


        // Syncbox interface
        if(!(bool)GetSetting("isTest")) {
            syncBox.Init();
        }


        // Start experiment Launcher scene
        mainEvents.Do(new EventBase(LaunchLauncher));
        eventsPerFrame = (int)(GetSetting("eventsPerFrame") ?? 5);
    }

    // // Update is called once per frame
    // float deltaTime = 0.0f;
    // int updateRate = 10;
    // int frame = 0;
    void Update()
    {
		// deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        // frame++;

        // if(updateRate % frame == 0) {
        //     ReportEvent("FPS", new Dictionary<string, object>() {{"fps", 1.0f/deltaTime}}, DataReporter.TimeStamp());
        //     frame = 0;
        // }

        if(Input.GetKeyDown(quitKey)) {
            Quit();
        }

        int i = 0;
        while(mainEvents.Process() && (i < eventsPerFrame)) {
            i++;
        }
    }

    //////////
    // collect references to managed objects
    // and release references to non-active objects
    //////////
    void onSceneLoaded(Scene scene, LoadSceneMode mode) 
    {
        onKey = new ConcurrentQueue<Action<string, bool>>(); // clear keyhandler queue on scene change

        // text displayer
        GameObject canvas =  GameObject.Find("MemoryWordCanvas");
        if(canvas != null) {
            textDisplayer = canvas.GetComponent<TextDisplayer>();
            Debug.Log("Found TextDisplay");
        }

        // input reporters
        GameObject inputReporters = GameObject.Find("DataManager");
        if(inputReporters != null) {
            scriptedInput = inputReporters.GetComponent<ScriptedEventReporter>();   
            peripheralInput = inputReporters.GetComponent<PeripheralInputReporter>();
            uiInput = inputReporters.GetComponent<UIDataReporter>();
            Debug.Log("Found InputReporters");
        }

        GameObject voice = GameObject.Find("VAD");
        if(voice != null) {
           voiceActity = voice.GetComponent<VoiceActivityDetection>(); 
           Debug.Log("Found VoiceActivityDetector");
        }

        GameObject video = GameObject.Find("VideoPlayer");
        if(video != null) {
            videoControl = video.GetComponent<VideoControl>();
            video.SetActive(false);
            Debug.Log("Found VideoPlayer");
        }

        GameObject sound = GameObject.Find("Sounds");
        if(sound != null) {
            lowBeep = sound.transform.Find("LowBeep").gameObject.GetComponent<AudioSource>();
            lowerBeep =  sound.transform.Find("LowerBeep").gameObject.GetComponent<AudioSource>();
            highBeep =  sound.transform.Find("HighBeep").gameObject.GetComponent<AudioSource>();
            playback =  sound.transform.Find("Playback").gameObject.GetComponent<AudioSource>();
            Debug.Log("Found Sounds");
        }

        GameObject soundRecorder = GameObject.Find("SoundRecorder");
        if(soundRecorder != null) {
            recorder = soundRecorder.GetComponent<SoundRecorder>();
            Debug.Log("Found Sound Recorder");
        }

        GameObject ramulatorObject = GameObject.Find("RamulatorInterface");
        if(ramulatorObject != null) {
            ramulator = ramulatorObject.GetComponent<RamulatorInterface>();
            Debug.Log("Found Ramulator");
        }

        mainEvents.Pause(false);
    }

    void OnDisable() {
        if(syncBox.Running()) {
            syncBox.Do(new EventBase(syncBox.StopPulse));
        }
    }

    //////////
    // Function that provides a clean interface for accessing
    // experiment and system settings. Settings in experiment
    // override those in system. Attempts to read non-existent
    // settings return null.
    //
    // **** These are the only two InterfaceManager functions  *****
    // **** that may be called from outside the main Thread.   *****
    //////////

    public  dynamic GetSetting(string setting) {
        lock(configLock) {
            JToken value = null;

            if(experimentConfig != null) {
                if(experimentConfig.TryGetValue(setting, out value)) {
                    if(value != null) {
                        return value;
                    }
                }
            }

            if(systemConfig != null) {
                if(systemConfig.TryGetValue(setting, out value)) {
                    return value;
                }
            }
        }

        throw new MissingFieldException("Missing Setting " + setting + ".");
    }

    public  void ChangeSetting(string setting, dynamic value) {
        JToken existing;

        try {
            existing = GetSetting(setting);
        }
        catch(MissingFieldException) {
            existing = null;
        }

        lock(configLock) {
            if(existing == null) {
                // even if setting belongs to systemConfig, experimentConfig setting s
                if(experimentConfig == null) {
                    systemConfig.Add(setting, value);
                }
                else {
                    experimentConfig.Add(setting, value);
                }

                return;
            }
            else {
                // even if setting belongs to systemConfig, experimentConfig setting s
                if(experimentConfig == null) {
                    systemConfig[setting] = value;
                }
                else {
                    experimentConfig[setting] = value;
                }

                return;
            }
        }
    }

    //////////
    // Functions to be called from other
    // scripts through the messaging system
    //////////

    public void TestSyncbox(Action callback) {
        syncBox.Do(new EventBase(syncBox.StartPulse));
        // DoIn(new EventBase(syncBox.StopPulse), (int)GetSetting("syncBoxTestLength"));
        DoIn(new EventBase(syncBox.StopPulse), 5000); 
        DoIn(new EventBase(callback), 5000); 
    }

    // TODO: deal with error states if conditions not met
    public void LaunchExperiment() {
        // launch scene with exp, 
        // instantiate experiment,
        // call start function

        // Check if settings are loaded
        if(experimentConfig != null) {

            Cursor.visible = false;
            Application.runInBackground = true;

            // Make the game run as fast as possible
            QualitySettings.vSyncCount = (int)GetSetting("vSync");
            Application.targetFrameRate = (int)GetSetting("frameRate");
            
            // create path for current participant/session
            fileManager.CreateSession();

            Do(new EventBase(() => {mainEvents.Pause(true);
                                    SceneManager.LoadScene((string)GetSetting("experimentScene"));
                                }));

            Do(new EventBase(() => {
                // Start syncbox
                syncBox.Do(new EventBase(syncBox.StartPulse));

                if((bool)GetSetting("elemem")) {
                    hostPC = new ElememInterface(this);
                } else if((bool)GetSetting("ramulator")) {
                    hostPC = new RamulatorWrapper(this);
                }

                LogExperimentInfo();

                Type t = Type.GetType((string)GetSetting("experimentClass")); 
                exp = (ExperimentBase)Activator.CreateInstance(t, new object[] {this});
            }));
        }
        else {
            throw new Exception("No experiment configuration loaded");
        }
    }

    public  void ReportEvent(string type, Dictionary<string, object> data, DateTime time) {
        // TODO: time stamps
        scriptedInput.ReportScriptedEvent(type, data, time );
    }

    public  void ReportEvent(string type, Dictionary<string, object> data) {
        // TODO: time stamps
        scriptedInput.ReportScriptedEvent(type, data);
    }

    public  void Quit() {
        Debug.Log("Quitting");
    #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
    #else
        Application.Quit();
    #endif
    //no more calls to Run past this point
    }

    public void LaunchLauncher() {

        Debug.Log("Launching: " + (string)GetSetting("launcherScene"));
        Do(new EventBase(() => {
                                mainEvents.Pause(true);
                                SceneManager.LoadScene((string)GetSetting("launcherScene"));
                            }));
    }

    public void LoadExperimentConfig(string name) {
        lock(configLock) {
            string text = System.IO.File.ReadAllText(System.IO.Path.Combine(fileManager.ConfigPath(), name + ".json"));
            experimentConfig = FlexibleConfig.LoadFromText(text); 
        }
    }

    public void ShowText(string tag, string text, string color) {
        if(textDisplayer == null) {
            throw new Exception("No text displayer in current scene");
        }
        else {
            Color myColor = Color.clear;
            ColorUtility.TryParseHtmlString(color, out myColor); 

            textDisplayer.ChangeColor(myColor);
            textDisplayer.DisplayText(tag, text);
        }
    }
    public void ShowText(string tag, string text) {
        if(textDisplayer == null) {
            throw new Exception("No text displayer in current scene");
        }
        else {
            textDisplayer.DisplayText(tag, text);
        }
    }
    public void ClearText() {
        if(textDisplayer == null) {
            throw new Exception("No text displayer in current scene");
        }
        else {
            textDisplayer.OriginalColor();
            textDisplayer.ClearText();
        }
    }
    
    public void ShowTitle(string tag, string text) {
        if(textDisplayer == null) {
            throw new Exception("No text displayer in current scene");
        }
        else {
            textDisplayer.DisplayTitle(tag, text);
        }
    }
    public void ClearTitle() {
        if(textDisplayer == null) {
            throw new Exception("No text displayer in current scene");
        }
        else {
            textDisplayer.OriginalColor();
            textDisplayer.ClearTitle();
        }
    }

    public void ShowVideo(string video, bool skippable, Action callback) {
        if(videoControl == null) {
            throw new Exception("No video player in this scene");
        }

        // absolute video path
        string videoPath = System.IO.Path.Combine(fileManager.ExperimentRoot(), (string)GetSetting(video));

        if(videoPath == null) {
            throw new Exception("Video resource not found");
        }

        videoControl.StartVideo(videoPath, skippable, callback);
    }

    public void ShowWarning(string warnMsg, int duration) {
        warning.SetActive(true);
        TextDisplayer warnText = warning.GetComponent<TextDisplayer>();
        warnText.DisplayText("warning", warnMsg);

        DoIn(new EventBase(() => { warnText.ClearText();
                                   warning.SetActive(false);}), duration);

    }

    public  void Notify(Exception e) {
        Debug.Log("Popup now displayed... invisibly");
        Debug.Log(e);

        // FIXME
        warning.SetActive(true);
        TextDisplayer warnText = warning.GetComponent<TextDisplayer>();
        warnText.DisplayText("warning", e.Message);

        DoIn(new EventBase(() => { warnText.ClearText();
                                   warning.SetActive(false);}), 5000);

    }

    public  void SetHostPCStatus(string status) {
        // TODO
        Debug.Log("Host PC Status");
        Debug.Log(status);
    }

    public  void SendHostPCMessage(string message, Dictionary<string, object> data) {
        hostPC?.Do(new EventBase<string, Dictionary<string, object>>(hostPC.SendMessage, message, data));
    }

    protected void LogExperimentInfo() {
        //write versions to logfile
        Dictionary<string, object> versionsData = new Dictionary<string, object>();
        versionsData.Add("application version", Application.version);
        versionsData.Add("build date", BuildInfo.Date());
        versionsData.Add("experiment version", (string)GetSetting("experimentName"));
        versionsData.Add("logfile version", "0");
        versionsData.Add("participant", (string)GetSetting("participantCode"));
        versionsData.Add("session", (int)GetSetting("session"));

        // occurring during loading, so reference may not yet be obtained
        ReportEvent("session start", versionsData);
    }

    //////////
    // Key handling code that receives key inputs from
    // an external script and modifies program behavior
    // accordingly
    //////////
    
    public void Key(string key, bool pressed) {
        Action<string, bool> action;
        while(onKey.Count != 0) {
            if(onKey.TryDequeue(out action)) {
                Do(new EventBase<string, bool>(action, key, pressed));
            }
        }
    }

    public void RegisterKeyHandler(Action<string, bool> handler) {
        onKey.Enqueue(handler);
    }

    //////////
    // Wrappers to make event management interface consistent
    //////////

    public  void Do(IEventBase thisEvent) {
        mainEvents.Do(thisEvent);
    }

    public  void DoIn(IEventBase thisEvent, int delay) {
        mainEvents.DoIn(thisEvent, delay);
    }

    public  void DoRepeating(IEventBase thisEvent, int iterations, int delay, int interval) {
        mainEvents.DoRepeating(thisEvent, iterations, delay, interval);
    }
}