using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.JSONSerializeModule;
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
    private ExperimentBase exp;

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
        string json = File.ReadAllText("config.json");
        expCfg = FromJson(json); 

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
        // event reporters
        // syncbox
        // ramulator (stimbox)
        // audio recorder

    }

    void launchExperiment() {
        // launch scene with exp, 
        // instantiate experiment,
        // call start function
        return;
    }
}
