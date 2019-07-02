using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;

public class ExperimentManager : MonoBehaviour
{

    // TO REMOVE
    public int rnd() {
        return 4;
        // certified random by dice roll
    }

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
    public EventManager ExpEvtMgr = new EventManager();
    public EventArgs EventArts = new EventArgs();

    // maintain experiment settings

    // Start is called before the first frame update
    void Start()
    {
        // Unity interal event handling
        SceneManager.sceneLoaded += onSceneLoaded;

        Debug.Log("Experiment Manager Up");


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

    }
}
