using System.Collections;
using System.Collections.Generic;
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
        }

        DontDestroyOnLoad(this.gameObject);
    }

    //////////

    //////////
    // Non-unity event handling for scripts to 
    // activate ExperimentManager functions
    //////////
    // FIXME: terrible name
    public EventQueue mainEvents = new EventQueue();

    //////////
    // Experiment Settings and Experiment object
    // that is instantiated once launch is called
    ////////// 
    // global random number source
    public static System.Random rnd = new System.Random();
    public dynamic systemConfig = null;
    public dynamic experimentConfig = null;
    public ExperimentBase exp;

    //////////
    // Known experiment GameObjects to
    // check for and collect when changing
    // scenes. These are made available to 
    // other scripts instantiated by
    // Experiment Manager.
    //////////

    public RamulatorInterface ramInt;
    public Syncbox syncBox;
    public VoiceActivityDetection voiceActity;
    public VideoControl videoControl;
    public TextDisplayer textDisplayer; // doesn't currently support multiple  text displays
    public SoundRecorder soundRecorder;
    public ScriptedEventReporter scriptedInput;
    public PeripheralInputReporter peripheralInput;
    public WorldDataReporter worldInput;
    public UIDataReporter uiInput;
    // experiment Launcher

    // Start is called before the first frame update
    void Start()
    {
        TextAsset json = Resources.Load<TextAsset>("config");
        systemConfig = FlexibleConfig.loadFromText(json.text); 

        Debug.Log("Config loaded");

        // Unity interal event handling
        SceneManager.sceneLoaded += onSceneLoaded;

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

        // syncbox
        GameObject sBox = GameObject.Find("Syncbox");
        if(sBox != null) {
            syncBox = sBox.GetComponent<Syncbox>();
            Debug.Log("Found SyncboxInterface");
        }

        // ramulator (stimbox)
        GameObject ramulator = GameObject.Find("RamulatorInterface");
        if(ramulator != null) {
            ramInt = ramulator.GetComponent<RamulatorInterface>();
            Debug.Log("Found RamulatorInterface");
        }

        // audio recorder
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