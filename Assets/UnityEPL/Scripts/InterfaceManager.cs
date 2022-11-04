using System;
using System.Threading;
using System.IO;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;
using System.Threading.Tasks;

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
    // let interface manager be seen as an event queue
    public static implicit operator EventQueue(InterfaceManager im) => im.mainEvents;

    // Deprecated Key handling
    private ConcurrentQueue<Action<string, bool>> onKey;

    //////////
    // Experiment Settings and Experiment object
    // that is instantiated once launch is called
    ////////// 
    // global random number source, wrapped so that out of thread 
    // access doesn't break generation
    public static ThreadLocal<System.Random> rnd = new ThreadLocal<System.Random>(() => new System.Random());

    // system configurations, generated on the fly by
    // FlexibleConfig
    private ExperimentBase exp;
    public InputHandler inputHandler;
    public FileManager fileManager;

    //////////
    // Known experiment GameObjects to
    // check for and collect when changing
    // scenes. These are made available to 
    // other scripts instantiated by
    // Experiment Manager.
    //////////

    // TODO: this could be better organized through an interface object
    // in each scene, so interface manager always looks for the same object
    // which exposes interface functions, rather than interface functions
    // living here. The could potentially be accomplished with a clever
    // namespace or similar to provide

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
    public InputReporter peripheralInput;
    public UIDataReporter uiInput;
    private int eventsPerFrame;

    // Start is called before the first frame update
    void Start()
    {
        // Unity internal event handling
        SceneManager.sceneLoaded += onSceneLoaded;

        // create objects not tied to unity
        fileManager = new FileManager(this);
        syncBox = new NonUnitySyncbox(this);
        onKey = new ConcurrentQueue<Action<string, bool>>();

        // configure default key handling to quit experiment
        inputHandler = new InputHandler(mainEvents, (handler, msg) => {
            UnityEngine.Debug.Log(msg.key + " " + quitKey + " " + msg.down);
            if(msg.down && msg.key == quitKey) {
                Quit();
                handler.active = false;
                return false;
            }
            return true;
        });

        #if !UNITY_WEBGL // System.IO
            Config.SetupSystemConfig(fileManager.ConfigPath());
        #else // !UNITY_WEBGL
            Config.SetupSystemConfig(Application.streamingAssetsPath);
        #endif // !UNITY_WEBGL

        // Get all configuration files
        string configPath = fileManager.ConfigPath();
        string[] configs = Directory.GetFiles(configPath, "*.json");
        if(configs.Length < 2) {
            Notify(new Exception("Configuration File Error"));
        }

        List<string> exps = new List<string>();

        for(int i=0, j=0; i<configs.Length; i++) {
            Debug.Log(configs[i]);
            if(!configs[i].Contains(SYSTEM_CONFIG))
                exps.Add(Path.GetFileNameWithoutExtension(configs[i]));
                j++;
        }
        Config.availableExperiments = exps.ToArray();

        // Syncbox interface
        if(!Config.isTest && !Config.noSyncbox) {
            syncBox.Init();
        }

        // Start experiment Launcher scene
        mainEvents.Do(new EventBase(LaunchLauncher));
        eventsPerFrame = Config.eventsPerFrame ?? 5;
    }

    void Update()
    {
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
            peripheralInput = inputReporters.GetComponent<InputReporter>();
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
    // Functions to be called from other
    // scripts through the messaging system
    //////////

    public void TestSyncbox(Action callback) {
        syncBox.Do(new EventBase(syncBox.StartPulse));
        // DoIn(new EventBase(syncBox.StopPulse), (int)GetSetting("syncBoxTestLength"));
        DoIn(new EventBase(syncBox.StopPulse), 5000); 
        DoIn(new EventBase(callback), 5000); 
    }

    public void LaunchExperiment() {
        // launch scene with exp, 
        // instantiate experiment,
        // call start function

        // Check if settings are loaded
        if (Config.IsExperimentConfigSetup()) {

            Cursor.visible = false;
            Application.runInBackground = true;

            // Make the game run as fast as possible
            QualitySettings.vSyncCount = Config.vSync;
            Application.targetFrameRate = Config.frameRate;
            
            // create path for current participant/session
            fileManager.CreateSession();

            mainEvents.Pause(true);
            SceneManager.LoadScene(Config.experimentScene);

            Do(new EventBase(() => {
                // Start syncbox
                syncBox.Do(new EventBase(syncBox.StartPulse));

                if(Config.elemem) {
                    hostPC = new ElememInterface(this);
                } else if(Config.ramulator) {
                    hostPC = new RamulatorWrapper(this);
                }

                LogExperimentInfo();

                Type t = Type.GetType(Config.experimentClass); 
                exp = (ExperimentBase)Activator.CreateInstance(t, new object[] {this});
            }));
        }
        else {
            throw new Exception("No experiment configuration loaded");
        }
    }

    public  void ReportEvent(string type, Dictionary<string, object> data, DateTime time) {
        scriptedInput.ReportScriptedEvent(type, data, time );
    }

    public  void ReportEvent(string type, Dictionary<string, object> data) {
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
            // reset external hardware state if exiting task
            syncBox.StopPulse();
            hostPC?.Disconnect();

            mainEvents.Pause(true);
            SceneManager.LoadScene(Config.launcherScene);
    }

    public void LoadExperimentConfig(string name) {
        Config.experimentConfigName = name;
        Config.SetupExperimentConfig();
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
        string videoPath = System.IO.Path.Combine(fileManager.ExperimentRoot(), Config.video);

        if(videoPath == null) {
            throw new Exception("Video resource not found");
        }

        videoControl.StartVideo(videoPath, skippable, callback);
    }

    public void Notify(Exception e) {
        warning.SetActive(true);
        TextDisplayer warnText = warning.GetComponent<TextDisplayer>();
        warnText.DisplayText("warning", e.Message);
        mainEvents.Pause(true);
    }

    public  void SetHostPCStatus(string status) {
        // TODO: at this point, nothing actually uses this
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
        versionsData.Add("build date", BuildInfo.ToString()); // compiler magic, gives compile date
        versionsData.Add("experiment version", Config.experimentName);
        versionsData.Add("logfile version", "0");
        versionsData.Add("participant", Config.participantCode);
        versionsData.Add("session", Config.session);

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
    // Wrappers to re export methods for EventQueue
    //////////

    public void Do(IEventBase thisEvent) {
        mainEvents.Do(thisEvent);
    }

    public void DoIn(IEventBase thisEvent, int delay) {
        mainEvents.DoIn(thisEvent, delay);
    }

    public void DoRepeating(IEventBase thisEvent, int iterations, int delay, int interval) {
        mainEvents.DoRepeating(thisEvent, iterations, delay, interval);
    }

    // Do not use this unless you HAVE to
    // This will block all things on the callers EventLoop
    public void DoBlocking(IEventBase thisEvent) {
        mainEvents.DoBlocking(thisEvent);
    }

    // Do not use this unless you HAVE to
    // This will block all things on the callers EventLoop
    public T DoGet<T>(Task<T> task) {
        return mainEvents.DoGet(task);
    }
}