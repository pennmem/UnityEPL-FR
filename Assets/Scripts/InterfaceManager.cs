using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine.SceneManagement;
using UnityEngine;

// It is up to objects that are referenced in this class to 
// have adequate protection levels on all members, as classes
// with a reference to manager can call functions from or pass events
// to classes referenced here.

public class InterfaceManager : MonoBehaviour
{
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
        } else {
            _instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
    }

    //////////

    //////////
    // Non-unity event handling for scripts to 
    // activate InterfaceManager functions
    //////////
    public EventQueue mainEvents = new EventQueue();

    //////////
    // Experiment Settings and Experiment object
    // that is instantiated once launch is called
    ////////// 
    // global random number source
    public static System.Random rnd = new System.Random();

    // system configurations, generated on the fly by
    // FlexibleConfig
    public dynamic systemConfig = null;
    public dynamic experimentConfig = null;
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
    public Syncbox syncBox;
    public VideoControl videoControl;
    public TextDisplayer textDisplayer; // doesn't currently support multiple  text displays
    public SoundRecorder soundRecorder;

    //////////
    // Input reporters
    //////////
    public VoiceActivityDetection voiceActity;
    public ScriptedEventReporter scriptedInput;
    public PeripheralInputReporter peripheralInput;
    public WorldDataReporter worldInput;
    public UIDataReporter uiInput;

    // Start is called before the first frame update
    void Start()
    {
        TextAsset json = Resources.Load<TextAsset>("config");
        systemConfig = FlexibleConfig.loadFromText(json.text); 

        Debug.Log("Config loaded");

        // Unity interal event handling
        SceneManager.sceneLoaded += onSceneLoaded;

        if(getSetting("isTest")) {
            Debug.Log("Test file manager created");
            fileManager = new TestFileManager(this);
        } else {
            fileManager = new FileManager(this);
        }

        Debug.Log("Experiment Manager Up");

        // Start experiment Launcher scene
        mainEvents.Do(new EventBase(launchLauncher));
    }

    // Update is called once per frame
    void Update()
    {
        mainEvents.Process();

        // TODO: use for keyinput
    }

    //////////
    // collect references to managed objects
    // and release references to non-active objects
    //////////
    void onSceneLoaded(Scene scene, LoadSceneMode mode) 
    {

        // text displayer
        GameObject canvas =  GameObject.Find("MemoryWordCanvas");
        if(canvas != null) {
            textDisplayer = canvas.GetComponent<TextDisplayer>();
            Debug.Log("Found TextDisplay");
        }

        // input reporters
        GameObject inputReporters = GameObject.Find("InputReporters");
        if(inputReporters != null) {
            scriptedInput = inputReporters.GetComponent<ScriptedEventReporter>();   
            peripheralInput = inputReporters.GetComponent<PeripheralInputReporter>();
            worldInput = inputReporters.GetComponent<WorldDataReporter>();
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

        GameObject sound = GameObject.Find("SoundRecorder");
        if(sound != null) {
            soundRecorder = sound.GetComponent<SoundRecorder>();
            Debug.Log("Found SoundRecorder");
        }
    }

    //////////
    // Function that provides a clean interface for accessing
    // experiment and system settings. Settings in experiment
    // override those in system. Attempts to read non-existent
    // settings return null.
    //////////

    public dynamic getSetting(string setting) {
        dynamic value;
        if(experimentConfig != null) {
            if(((IDictionary<string, object>)experimentConfig).TryGetValue(setting, out value)) {
                return value;
            }
        }
        else if(systemConfig != null) {
            if(((IDictionary<string, object>)systemConfig).TryGetValue(setting, out value)) {
                return value;
            }
        }
        return null;
    }

    // returns true if value updated, false if new value added
    public bool changeSetting(string setting, dynamic value) {
        dynamic existing = getSetting(setting);
        if(existing == null) {
            ((IDictionary<string, object>)experimentConfig).Add(setting, value);
            return false;
        }
        else {
            Type t = existing.TypeOf();
            if( t != value.typeOf()) {
                throw new Exception("Cannot change the type of a setting");
            }
            ((IDictionary<string, object>)experimentConfig)[setting] = value;
            return true;
        }
    }

    //////////
    // Functions to be called from other
    // scripts through the messaging system
    //////////

    // TODO: deal with error states if conditions not met
    public void launchExperiment() {
        // launch scene with exp, 
        // instantiate experiment,
        // call start function

        // Check if settings are loaded
        if(experimentConfig != null) {
            Type t = Type.GetType((string)getSetting("experimentClass")); 
            exp = (ExperimentBase)Activator.CreateInstance(t, new object[] {this});

            SceneManager.LoadScene(getSetting("experimentScene"));
            exp.Do(new EventBase(exp.Run));
        }
        else {
            throw new Exception("No experiment configuration loaded");
        }
    }

    public void launchLauncher() {
        SceneManager.LoadScene(getSetting("launcherScene"));
    }

    public void loadExperimentConfig(string name) {
        TextAsset json = Resources.Load<TextAsset>(name);
        experimentConfig = FlexibleConfig.loadFromText(json.text); 
    }

    public void reportData(string type, DataPoint data) {

    }

    public void showText(string text) {
        if(textDisplayer == null) {
            throw new Exception("No text displayer in current scene");
        }
        else {
            string tag = "TEST";
            textDisplayer.DisplayText(tag, text);
        }
    }
    public void clearText() {
        if(textDisplayer == null) {
            throw new Exception("No text displayer in current scene");
        }
        else {
            textDisplayer.ClearText();
        }
    }

    public void showVideo(string video, Action callback) {
        if(videoControl == null) {
            throw new Exception("No video player in this scene");
        }

        // path to video asset relative to Resources folder
        string videoPath = getSetting(video);

        if(videoPath == null) {
            throw new Exception("Video resource not found");
        }

        videoControl.StartVideo(videoPath, callback);
    }

    // TODO: audio recording, hardware interface
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

    public virtual string experimentRoot() {
        // TODO: use . directory?
        return System.IO.Path.GetFullPath(".");
    }

    public string experimentPath() {
        string root = experimentRoot();
        Debug.Log(manager.getSetting("experimentName"));
        string dir = System.IO.Path.Combine(root, manager.getSetting("experimentName"));
        Debug.Log(dir);
        return dir;
    }
    public string participantPath(string participant) {
        string dir = experimentPath();
        dir = System.IO.Path.Combine(dir, manager.getSetting("participant"));
        return dir;
    }

    public string sessionPath(string participant, int session) {
        string dir = participantPath(participant);
        dir = System.IO.Path.Combine(dir, session.ToString() + ".session");
        return dir;
    }

    public bool isValidParticipant(string code) {
        if(manager.experimentConfig == null) {
            return false;
        }
        Regex rx = new Regex(@"^" + manager.getSetting("prefix") + @"\d{1,4}$");

        return rx.IsMatch(code);
    }

    public string getWordList() {
        string root = experimentRoot();
        return System.IO.Path.Combine(root, manager.getSetting("wordpool"));
    }

    // TODO: create session, participant
}

public class TestFileManager : FileManager {
    public TestFileManager(InterfaceManager _manager) : base(_manager) {
    }

    public override string experimentRoot() {
        return System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop);
    }
}