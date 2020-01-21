using System;
using System.Dynamic;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using UnityEngine;


public abstract class ExperimentBase : EventLoop {
    public InterfaceManager manager;
    public GameObject microphoneTestMessage; // set in editor

    // dictionary containing lists of actions indexed
    // by the name of the function incrementing through
    // list of states
    protected Dictionary<string, List<Action>> stateMachine;

    protected dynamic state; // object to which fields can be added at runtime

    public ExperimentBase(InterfaceManager _manager) {
        manager = _manager;
        state = new ExpandoObject(); // all data must be serializable
        stateMachine = new Dictionary<string, List<Action>>();

        state.isComplete = false;
        state.runIndex = 0;
    }

    // executes state machine current function
    public void Run() {
        SaveState();
        stateMachine["Run"][state.runIndex].Invoke();
    }

    //////////
    // Worker Functions for common experiment tasks.
    // Ignorant of state machine structure, return
    // true if done with task, false if expecting
    // to be called again.
    //////////

    protected void IntroductionVideo() {
        manager.Do(new EventBase<string, bool, Action>(manager.ShowVideo, 
                                                            "introductionVideo", true,
                                                            () => this.Do(new EventBase(Run))));
    }

    protected void CountdownVideo() {
        manager.scriptedInput.ReportOutOfThreadScriptedEvent("countdown", new Dictionary<string, object>());
        manager.Do(new EventBase<string, bool, Action>(manager.ShowVideo, 
                                                            "countdownVideo", false, 
                                                            () => this.Do(new EventBase(Run))));
    }

    protected void RecordTest(string wavPath) {
        manager.Do(new EventBase(manager.lowBeep.Play));
        manager.Do(new EventBase<string>(manager.recorder.StartRecording, wavPath));
        manager.Do(new EventBase<string, string, string>(manager.ShowText, "microphone test recording", "Recording...", "red"));

        DoIn(new EventBase(() => {
                            manager.Do(new EventBase( () => {
                                manager.ClearText();
                                manager.recorder.StopRecording();
                                this.Do(new EventBase(Run));
                                }));
                            }), 
                        (int)manager.GetSetting("micTestDuration"));
    }

    // NOTE: rather than use flags for the audio test, this is entirely based off of timings.
    // Since there is processing latency (which seems to be unity version dependent), this
    // is really a bit of a hack that lets us get through the mic test unscathed. More time critical
    // applications need a different approach

    protected void PlaybackTest(string wavPath) {
        manager.Do(new EventBase<string, string, string>(manager.ShowText, "microphone test playing", "Playing...", "green"));
        manager.Do(new EventBase( () => {
                manager.playback.clip = manager.recorder.AudioClipFromDatapath(wavPath);
                manager.playback.Play();
            }));

        DoIn(new EventBase(() => {
                                manager.Do(new EventBase(manager.ClearText));
                                manager.Do(new EventBase(manager.ClearTitle));
                                this.Do(new EventBase(Run));
                            }), (int)manager.GetSetting("micTestDuration") + 1000); // pad for latency
    }

    protected bool Encoding(IList<string> words, int index) {
        if(words.Count == index) {
            return true;
        }

        int interval;

        int[] limits = manager.GetSetting("stimulusInterval").ToObject<int[]>();
        interval = InterfaceManager.rnd.Next(limits[0], limits[1]);

        manager.Do(new EventBase<string, string>(manager.ShowText, "word stimulus", words[index]));
        DoIn(new EventBase(() => { manager.Do(new EventBase(manager.ClearText)); 
                                    manager.scriptedInput.ReportOutOfThreadScriptedEvent("clear word stimulus", new Dictionary<string, object>());
                                    this.DoIn(new EventBase(Run), interval);
                                }), 
                            (int)manager.GetSetting("stimulusDuration"));
        
        return false;
    }

    protected void Distractor() {
        int[] nums = new int[] { InterfaceManager.rnd.Next(1, 9), InterfaceManager.rnd.Next(1, 9), InterfaceManager.rnd.Next(1, 9) };
        string problem = nums[0].ToString() + " + " + nums[1].ToString() + " + " + nums[2].ToString() + " = ";

        state.distractorProblem = nums;
        state.distractorAnswer = "";

        manager.RegisterKeyHandler(DistractorAnswer);
        manager.Do(new EventBase<string, string>(manager.ShowText, "display distractor problem", problem));
    }


    protected void Orientation() {

        int[] limits = manager.GetSetting("stimulusInterval").ToObject<int[]>();
        int interval = InterfaceManager.rnd.Next(limits[0], limits[1]);

        limits = manager.GetSetting("orientationDuration").ToObject<int[]>();
        int duration = InterfaceManager.rnd.Next(limits[0], limits[1]);
        manager.Do(new EventBase<string, string>(manager.ShowText, "orientation stimulus", "+"));
        DoIn(new EventBase(() => {
                                    manager.Do(new EventBase(manager.ClearText)); 
                                    this.DoIn(new EventBase(Run), interval);
                                }), 
                                duration);
    }

    protected void Recall(string wavPath) {
        manager.Do(new EventBase<string>(manager.recorder.StartRecording, wavPath));
        DoIn(new EventBase(() => {
                manager.Do(new EventBase(() => {
                    manager.scriptedInput.ReportOutOfThreadScriptedEvent("end recall period", new Dictionary<string, object>());
                    manager.recorder.StopRecording();
                    manager.ClearText();
                    manager.lowBeep.Play();
                }));
                this.Do(new EventBase(Run));
        }), (int)manager.GetSetting("recallDuration") );
    }

    protected void RecallPrompt() {
        manager.Do(new EventBase(manager.highBeep.Play));
        manager.Do(new EventBase<string, string>(manager.ShowText, "display recall text", "*******"));
        DoIn(new EventBase(() => {
                                    manager.Do(new EventBase(manager.ClearText));
                                    this.Do(new EventBase(Run));
                                }), 
                                (int)manager.GetSetting("recallPromptDuration"));
    }
    
    
    public void QuitPrompt() {
        WaitForKey("subject/session confirmation", "Running " + (string)manager.GetSetting("participantCode") + " in session " 
            + (int)manager.GetSetting("session") + " of " + (string)manager.GetSetting("experimentName") 
            + ".\n Press Y to continue, N to quit.", 
            (Action<string, bool>)QuitOrContinue);
    }

    public void WaitForKey(string tag, string prompt, Action<string, bool> keyHandler) {
        manager.Do(new EventBase<string, string>(manager.ShowText, tag, prompt));
        manager.Do(new EventBase<Action<string, bool>>(manager.RegisterKeyHandler, keyHandler));
    }
    
    public void WaitForTime(int milliseconds) {
        // convect to milliseconds
        DoIn(new EventBase(Run), milliseconds); 
    }

    public void MicTestPrompt() {
        manager.Do(new EventBase<string, string>(manager.ShowTitle, "microphone test title", "Microphone Test"));
        WaitForKey("microphone test prompt", "Press any key to record a sound after the beep.", AnyKey);
    }

    public void ConfirmStart() {
        WaitForKey("confirm start", "Please let the experimenter know \n" +
                "if you have any questions about \n" +
                "what you just did.\n\n" +
                "If you think you understand, \n" +
                "Please explain the task to the \n" +
                "experimenter in your own words.\n\n" +
                "Press any key to continue \n" +
                "to the first list.", AnyKey);
    }
    
    protected virtual void Quit() {
        if(state.listIndex == (int)manager.GetSetting("numLists")){
            state.isComplete = true;
            manager.Do(new EventBase<string, string>(manager.ShowText, "session end", "Yay! Session Complete."));
        }
        Stop();
        manager.DoIn(new EventBase(manager.LaunchLauncher), 10000);
    }


    //////////
    // Key Handler functions
    //////////

    protected void DistractorAnswer(string key, bool down) {
        int Sum(int[] arg){
            int sum = 0;
            for(int i=0; i < arg.Length; i++) {
                sum += arg[i];
            }
            return sum;
        }

        string message = "distractor update";

        // at every stage other than confirming answer, keyhandler is
        // re-enqueued to interface manager

        key = key.ToLower();
        if(down) {
            // enter only numbers
            if(Regex.IsMatch(key, @"\d$")) {
                key = key[key.Length-1].ToString(); // Unity gives numbers as Alpha# or Keypad#
                if(state.distractorAnswer.Length < 3) {
                    state.distractorAnswer = state.distractorAnswer + key;
                }
                message = "modify distractor answer";
            }
            // delete key removes last character from answer
            else if(key == "delete" || key == "backspace") {
                if(state.distractorAnswer != "") {
                    state.distractorAnswer = state.distractorAnswer.Substring(0, state.distractorAnswer.Length - 1);
                }
                message = "modify distractor answer";
            }
            // submit answer and play tone depending on right or wrong answer 
            else if(key == "enter" || key == "return") {
                int result;
                int.TryParse(state.distractorAnswer, out result) ;
                if(result == Sum(state.distractorProblem)) {
                    manager.Do(new EventBase(manager.lowBeep.Play));
                    ReportDistractorAnswered(true, state.distractorProblem[0].ToString() + " + " 
                        + state.distractorProblem[1].ToString() + " + " 
                        + state.distractorProblem[2].ToString() + " = ", state.distractorAnswer);
                } 
                else {
                    manager.Do(new EventBase(manager.lowerBeep.Play));
                    ReportDistractorAnswered(false, state.distractorProblem[0].ToString() + " + " 
                        + state.distractorProblem[1].ToString() + " + " 
                        + state.distractorProblem[2].ToString() + " = ", state.distractorAnswer);
                }
                Do(new EventBase(Run));
                manager.Do(new EventBase(manager.ClearText));
                state.distractorProblem = "";
                state.distractorAnswer = "";
                return;
            }
        }

        manager.RegisterKeyHandler(DistractorAnswer);

        string problem = state.distractorProblem[0].ToString() + " + " 
                        + state.distractorProblem[1].ToString() + " + " 
                        + state.distractorProblem[2].ToString() + " = ";
        string answer = state.distractorAnswer;
        manager.Do(new EventBase<string, string>(manager.ShowText, message, problem + answer));
    }

    public void AnyKey(string key, bool down) {
        if(down) {
            manager.Do(new EventBase(manager.ClearText));
            Do(new EventBase(Run));
        }
        else  {
            manager.RegisterKeyHandler(AnyKey);
        }
    }


    public void QuitOrContinue(string key, bool down) {
        if(down && key == "Y") {
            manager.Do(new EventBase(manager.ClearText));
            this.Do(new EventBase(Run));
        }
        else if(down && key == "N") {
            this.Quit();
        }
        else {
            manager.RegisterKeyHandler(QuitOrContinue);
        }
    }


    //////////
    // Saving and loading state logic
    //////////

    private void ReportBeepPlayed(string beep, string duration) {
        Dictionary<string, object> dataDict = new Dictionary<string, object>() { { "sound name", beep }, { "sound duration", duration } };
        manager.scriptedInput.ReportScriptedEvent("Sound Played", dataDict);
    }

    private void ReportDistractorAnswered(bool correct, string problem, string answer)
    {
        Dictionary<string, object> dataDict = new Dictionary<string, object>();
        dataDict.Add("correctness", correct.ToString());
        dataDict.Add("problem", problem);
        dataDict.Add("answer", answer);
        manager.scriptedInput.ReportScriptedEvent("distractor answered", dataDict);
    }
    
    public virtual void SaveState() {
        string path = System.IO.Path.Combine(manager.fileManager.SessionPath(), "experiment_state.json");
        FlexibleConfig.WriteToText(state, path);
    }

    public virtual dynamic LoadState(string participant, int session) {
        if(System.IO.File.Exists(System.IO.Path.Combine(manager.fileManager.SessionPath(participant, session), "experiment_state.json"))) {
            string json = System.IO.File.ReadAllText(System.IO.Path.Combine(manager.fileManager.SessionPath(participant, session), "experiment_state.json"));
            dynamic jObjCfg = FlexibleConfig.LoadFromText(json);
            return FlexibleConfig.CastToStatic(jObjCfg);
        }
        else {
            return null;
        }
    }
}
