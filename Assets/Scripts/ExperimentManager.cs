using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;

public class ExperimentManager : MonoBehaviour
{

    // TEMPORARY: global random number generator
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


    // pure C# event handling for experiment
    public EventManager expEvtMgr = new EventManager();
    public EventArgs eventArgs = new EventArgs();

    // maintain experiment settings
    public ExperimentSettings expSettings; // TODO
    public ExperimentBase exp = new Experiment();

    // available object references
    // TODO 

    // Start is called before the first frame update
    void Start()
    {
        // Unity interal event handling
        SceneManager.sceneLoaded += onSceneLoaded;

        Debug.Log("Experiment Manager Up");


        // Subscribe to events for delegated tasks
        expEvtMgr.startListener("launch", launchExperiment)


        // Start experiment Launcher
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // collect references to managed objects
    // and release references to non-active objects
    void onSceneLoaded(Scene scene, LoadSceneMode mode) 
    {

        // text displayer
        // event reporters

    }

    void launchExperiment() {
        // launch scene
        return;
    }
}
