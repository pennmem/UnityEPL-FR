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

    public static InterfaceManager Instance { get { return _instance; } }

    private void Awake()
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
    }

    //////////

    //////////
    // Non-unity event handling for scripts to 
    // activate InterfaceManager functions
    //////////
    private EventQueue mainEvents = new EventQueue();

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
    public RamulatorInterface ramInt;
    public NonUnitySyncbox syncBox;
    public VideoControl videoControl;
    public TextDisplayer textDisplayer; // doesn't currently support multiple  text displays
    public SoundRecorder recorder;
    public GameObject warning;

    public AudioSource highBeep;
    public AudioSource lowBeep;
    public AudioSource lowerBeep;
    public AudioSource playback;

    //////////
    // Input reporters
    //////////
    public VoiceActivityDetection voiceActity;
    public ScriptedEventReporter scriptedInput;
    public PeripheralInputReporter peripheralInput;
    public UIDataReporter uiInput;
    private int eventsPerFrame;

    private bool process = true;

    // Start is called before the first frame update
    void Start()
    {
        // Unity interal event handling
        SceneManager.sceneLoaded += onSceneLoaded;


        // create objects not tied to unity
        fileManager = new FileManager(this);
        syncBox = new NonUnitySyncbox();
        onKey = new ConcurrentQueue<Action<string, bool>>();

        // load system configuration file
        string text = System.IO.File.ReadAllText(System.IO.Path.Combine(fileManager.ConfigPath(), SYSTEM_CONFIG));
        systemConfig = FlexibleConfig.LoadFromText(text);

        // Get all configuration files
        string configPath = fileManager.ConfigPath();
        string[] configs = Directory.GetFiles(configPath, "*.json");
        if(configs.Length < 2) {
            throw new Exception("Missing configuration file");
        }

        JArray exps = new JArray();

        for(int i=0, j=0; i<configs.Length; i++) {
            Debug.Log(configs[i]);
            if(!configs[i].Contains(SYSTEM_CONFIG))
                exps.Add(Path.GetFileNameWithoutExtension(configs[i]));
                j++;
        }
        ChangeSetting("availableExperiments", exps);

        // Initialize hardware
            // Stim hardward interface 

        // Syncbox interface
        if(!(bool)GetSetting("isTest")) {
            syncBox.Init();
        }

        // Start experiment Launcher scene
        mainEvents.Do(new EventBase(LaunchLauncher));
        eventsPerFrame = (int)(GetSetting("eventsPerFrame") ?? 1);
    }

    // Update is called once per frame
    void Update()
    {
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
        if((bool)GetSetting("isLegacyExperiment") == true) {
            Debug.Log("Legacy Experiment");
            process = true; // re enable processing of events
            return;
        }

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

        process = true; // re enable processing of events
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
    //////////

    public dynamic GetSetting(string setting) {
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

        return null;
    }

    // returns true if value updated, false if new value added
    public bool ChangeSetting(string setting, dynamic value) {
        JToken existing = GetSetting(setting);
        if(existing == null) {

            // even if setting belongs to systemConfig, experimentConfig setting overrides
            if(experimentConfig == null) {
                (systemConfig).Add(setting, value);
            }
            else {
                (experimentConfig).Add(setting, value);
            }
            return false;
        }
        else {
            // even if setting belongs to systemConfig, experimentConfig setting overrides
            if(experimentConfig == null) {
                (systemConfig)[setting] = value;
            }
            else {
                (experimentConfig)[setting] = value;
            }
            return true;
        }
    }

    //////////
    // Functions to be called from other
    // scripts through the messaging system
    //////////

    // TODO: deal with error states if conditions not met
    public void LaunchExperiment() {
        // launch scene with exp, 
        // instantiate experiment,
        // call start function

        // Check if settings are loaded
        if(experimentConfig != null) {
            process = false; // disable event processing during scene transition
            SceneManager.LoadScene((string)GetSetting("experimentScene"));

            Type t = Type.GetType((string)GetSetting("experimentClass")); 
            exp = (ExperimentBase)Activator.CreateInstance(t, new object[] {this});

            Cursor.visible = false;
            Application.runInBackground = true;
            // Make the game run as fast as possible
            QualitySettings.vSyncCount = (int)GetSetting("vSync");
            Application.targetFrameRate = (int)GetSetting("frameRate");

            // create path for current participant/session
            fileManager.CreateSession();

            // Start syncbox
            if(!(bool)GetSetting("isTest")) {
                syncBox.Do(new EventBase(syncBox.StartPulse));
            }

            exp.Do(new EventBase(exp.Run));
        }
        else {
            throw new Exception("No experiment configuration loaded");
        }
    }

    public void TestSyncbox() {
        syncBox.Do(new EventBase(syncBox.StartPulse));
        DoIn(new EventBase(syncBox.StopPulse), GetSetting("syncBoxTestLength"));
    }

    public void Quit() {
        Debug.Log("Quitting");
    #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
    #else
        Application.Quit();
    #endif
    //no more calls to Run past this point
    }

    public void LaunchLauncher() {
        process = false; // disable event processing during scene transition
        Debug.Log("Launching: " + (string)GetSetting("launcherScene"));
        SceneManager.LoadScene((string)GetSetting("launcherScene"));
    }

    public void LoadExperimentConfig(string name) {
        string text = System.IO.File.ReadAllText(System.IO.Path.Combine(fileManager.ConfigPath(), name + ".json"));
        experimentConfig = FlexibleConfig.LoadFromText(text); 
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

        // path to video asset relative to Resources folder
        string videoPath = (string)GetSetting(video);

        if(videoPath == null) {
            throw new Exception("Video resource not found");
        }

        videoControl.StartVideo(videoPath, skippable, callback);
    }

    public void ShowWarning(string warnMsg, int duration) {
        warning.SetActive(true);
        TextDisplayer warnText = warning.GetComponent<TextDisplayer>();
        warnText.DisplayText("warning", warnMsg);

        Do(new EventBase(() => { warnText.ClearText();
                                 warning.SetActive(false);}));

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
    // Wrappers to make event management API consistent
    //////////

    public void Do(EventBase thisEvent) {
        mainEvents.Do(thisEvent);
    }

    public void DoIn(EventBase thisEvent, int delay) {
        mainEvents.DoIn(thisEvent, delay);
    }

    public void DoRepeating(RepeatingEvent thisEvent) {
        mainEvents.DoRepeating(thisEvent);
    }
}


//////////
// Classes to manage the filesystem in
// which experiment data is stored
/////////

public class FileManager {

    InterfaceManager manager;

    public FileManager(InterfaceManager _manager) {
        manager = _manager;
    }

    public virtual string ExperimentRoot() {

        #if UNITY_EDITOR
            return System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop);
        #else
            return System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop);
            //return System.IO.Path.GetFullPath(".");
        #endif
    }

    public string ExperimentPath() {
        string root = ExperimentRoot();
        string dir = System.IO.Path.Combine(root, (string)manager.GetSetting("experimentName"));
        return dir;
    }
    public string ParticipantPath(string participant) {
        string dir = ExperimentPath();
        dir = System.IO.Path.Combine(dir, participant);
        return dir;
    }

    public string ParticipantPath() {
        string dir = ExperimentPath();
        string participant = (string)manager.GetSetting("participantCode");

        if(participant == null) {
            throw new Exception("No participant selected");
        }

        dir = System.IO.Path.Combine(dir, participant);
        return dir;
    }
    
    public string SessionPath(string participant, int session) {
        string dir = ParticipantPath(participant);
        dir = System.IO.Path.Combine(dir, session.ToString());
        return dir;
    }

    public string SessionPath() {
        string session = (string)manager.GetSetting("session").ToString();
        if(session == null) {
            throw new Exception("No session selected");
        }
        string dir = ParticipantPath();
        dir = System.IO.Path.Combine(dir, session.ToString());
        return dir;
    }

    public bool isValidParticipant(string code) {
        if((bool)manager.GetSetting("isTest")) {
            return true;
        }

        if((string)manager.GetSetting("experimentName") == null) {
            return false;
        }
        Regex rx = new Regex(@"^" + (string)manager.GetSetting("prefix") + @"\d{1,4}$");

        return rx.IsMatch(code);
    }

    public string GetWordList() {
        string root = ExperimentRoot();
        return System.IO.Path.Combine(root, (string)manager.GetSetting("wordpool"));
    }

    public void CreateSession() {
        Directory.CreateDirectory(SessionPath());
    }

    public void CreateParticipant() {
        Directory.CreateDirectory(ParticipantPath());
    }
    public void CreateExperiment() {
        Directory.CreateDirectory(ExperimentPath());
    }

    public string ConfigPath() {
        string root = ExperimentRoot();
        return System.IO.Path.Combine(root, "configs");
    }

    public int CurrentSession(string participant) {
        int nextSessionNumber = 0;
        while (System.IO.Directory.Exists(manager.fileManager.SessionPath(participant, nextSessionNumber)))
        {
            nextSessionNumber++;
        }
        return nextSessionNumber;
    }
}