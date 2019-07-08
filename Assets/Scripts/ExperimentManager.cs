using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.IO;
using System;
using UnityEngine;

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
    // TODO: terrible name
    public EventQueue mainEvents = new EventQueue();

    //////////
    // Experiment Settings and Experiment object
    // that is instantiated once launch is called
    ////////// 
    // global random number source
    public static System.Random rnd = new System.Random();
    public ExperimentConfig expCfg;
    private ExperimentBase exp;

    //////////
    // Known experiment GameObjects to
    // check for and collect when changing
    // scenes. These are made available to 
    // other scripts instantiated by
    // Experiment Manager.
    //////////

    // event recorders
    RamulatorInterface ramInt;
    Syncbox syncBox;
    VoiceActivityDetection voiceActity;
    VideoControl videoControl;
    TextDisplayer textDisplayer; // doesn't currently support multiple  text displays
    SoundRecorder soundRecorder;
    ScriptedEventReporter scriptedInput;
    PeripheralInputReporter peripheralInput;
    WorldDataReporter worldInput;
    UIDataReporter uiInput;
    // experiment Launcher
    // audio input
    // text display
    // launcher panel

    // Start is called before the first frame update
    void Start()
    {
        TextAsset json = Resources.Load<TextAsset>("config");
        expCfg = JsonUtility.FromJson<ExperimentConfig>(json.text); 

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
    private void resetReferences() {
        textDisplayer = null;
        scriptedInput = null;
        peripheralInput = null;
        worldInput = null;
        uiInput = null;
        syncBox = null;
        ramInt = null;
        soundRecorder = null;
        videoControl = null;
    }

    void onSceneLoaded(Scene scene, LoadSceneMode mode) 
    {
        resetReferences();

        // text displayer
        GameObject canvas =  GameObject.Find("Canvas");
        if(canvas != null) {
            textDisplayer = canvas.GetComponent<TextDisplayer>();
        }

        // input reporters
        GameObject inputReporters = GameObject.Find("InputReporters");
        if(inputReporters != null) {
            scriptedInput = inputReporters.GetComponent<ScriptedEventReporter>();   
            peripheralInput = inputReporters.GetComponent<PeripheralInputReporter>();
            worldInput = inputReporters.GetComponent<WorldDataReporter>();
            uiInput = inputReporters.GetComponent<UIDataReporter>();
        }

        // syncbox
        GameObject sBox = GameObject.Find("Syncbox");
        if(sBox != null) {
            syncBox = sBox.GetComponent<Syncbox>();
        }

        // ramulator (stimbox)
        GameObject ramulator = GameObject.Find("RamulatorInterface");
        if(ramulator != null) {
            ramInt = ramulator.GetComponent<RamulatorInterface>();
        }

        // audio recorder
        GameObject voice = GameObject.Find("VAD");
        if(voice != null) {
           voiceActity = voice.GetComponent<VoiceActivityDetection>(); 
        }

        GameObject video = GameObject.Find("VideoPlayer");
        if(video != null) {
            videoControl = video.GetComponent<VideoControl>();
        }

        GameObject sound = GameObject.Find("SoundRecorder");
        if(sound != null) {
            soundRecorder = sound.GetComponent<SoundRecorder>();
        }
    }

    void launchExperiment() {
        // launch scene with exp, 
        // instantiate experiment,
        // call start function
        // TODO:
        SceneManager.LoadScene("ram_fr");
        return;
    }

    void launchLauncher() {
        SceneManager.LoadScene(expCfg.launcherScene);
    }
}
