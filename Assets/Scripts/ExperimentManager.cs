using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.IO;
using UnityEngine;

public class ExperimentManager : MonoBehaviour
{

    // TEMPORARY: global random number generator

    // FIXME:
    public static Random rnd = new Random();

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
    public EventManager expEvtMgr = new EventManager();

    //////////
    // Experiment Settings and Experiment object
    // that is instantiated once launch is called
    ////////// 
    public ExperimentConfig expCfg; // TODO
    // TODO: private ExperimentBase exp;

    //////////
    // Known experiment GameObjects to
    // check for and collect when changing
    // scenes. These are made available to 
    // other scripts instantiated by
    // Experiment Manager.
    //////////

    // TODO 
    // event recorders
    // experiment Launcher
    // audio input
    // text display
    // launcher panel

    // Start is called before the first frame update
    void Start()
    {
        string json = File.ReadAllText("Assets/Resources/config.json");
        expCfg = JsonUtility.FromJson<ExperimentConfig>(json); 

        // Unity interal event handling
        SceneManager.sceneLoaded += onSceneLoaded;

        Debug.Log("Experiment Manager Up");


        // Subscribe to events for delegated tasks
        //expEvtMgr.startListening("launch", launchExperiment);


        // Start experiment Launcher scene
    }

    // Update is called once per frame
    void Update()
    {
        
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

    void launchExperiment() {
        // launch scene with exp, 
        // instantiate experiment,
        // call start function
        return;
    }
}
