using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine.SceneManagement;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEngine;

// It is up to objects that are referenced in this class to 
// have adequate protection levels on all members, as classes
// with a reference to manager can call functions from or pass events
// to classes referenced here.

public class ExperimentManager : MonoBehaviour
{
    //////////
    // Singleton Boilerplate
    // makes sure that only one Experiment Manager
    // can exist in a scene and that this object
    // is not destroyed when changing scenes
    //////////

    private static ExperimentManager _instance;

    public static ExperimentManager Instance { get { return _instance; } }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            throw new System.InvalidOperationException("Cannot create multiple ExperimentManager Objects");
        } else {
            _instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
    }

    //////////

    //////////
    // Non-unity event handling for scripts to 
    // activate ExperimentManager functions
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
    // TODO: refactor into one game object
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

        if(systemConfig.isTest) {
            fileManager = new TestFileManager(this);
        } else {
            fileManager = new FileManager(this);
        }

        Debug.Log("Experiment Manager Up");

        // Start experiment Launcher scene
        launchLauncher();
    }

    // Update is called once per frame
    void Update()
    {
        mainEvents.Process();
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
            Debug.Log("Found VideoPlayer");
        }

        GameObject sound = GameObject.Find("SoundRecorder");
        if(sound != null) {
            soundRecorder = sound.GetComponent<SoundRecorder>();
            Debug.Log("Found SoundRecorder");
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
            // TODO: instantiate experiment by name
            exp = new RepFRExperiment();

            SceneManager.LoadScene(experimentConfig.experimentScene);
        }
        return;
    }

    public void launchLauncher() {
        SceneManager.LoadScene(systemConfig.launcherScene);
    }

    public void loadExperimentConfig(string name) {
        TextAsset json = Resources.Load<TextAsset>(name);
        experimentConfig = FlexibleConfig.loadFromText(json.text); 
    }
}


//////////
// Classes to manage the filesystem in
// which experiment data is stored
/////////

public class FileManager {

    ExperimentManager manager;

    public FileManager(ExperimentManager _manager) {
        manager = _manager;
    }

    public virtual string experimentRoot() {
        // TODO: use . directory?
        return manager.experimentConfig.experimentRoot; 
    }

    public string experimentPath() {
        string root = experimentRoot();
        string dir = System.IO.Path.Combine(root, manager.experimentConfig.experimentName);
        return dir;
    }
    public string participantPath(string participant) {
        string dir = experimentPath();
        dir = System.IO.Path.Combine(dir, manager.experimentConfig.participant);
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
        Regex rx = new Regex(@"^" + manager.experimentConfig.prefix +@"\d{1,4}$");

        return rx.IsMatch(code);
    }
}

public class TestFileManager : FileManager {
    public TestFileManager(ExperimentManager _manager) : base(_manager) {
    }

    public override string experimentRoot() {
        return System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop);
    }
}