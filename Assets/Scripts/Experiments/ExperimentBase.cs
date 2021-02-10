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

    protected InputHandler keyHandler;

    protected dynamic state; // object to which fields can be added at runtime

    public ExperimentBase(InterfaceManager _manager) {
        manager = _manager;
        stateMachine = new Dictionary<string, List<Action>>();

        // keyHandler = new InputHandler();
        // keyHandler.active = false;
        // manager.keyHandler.RegisterChild(keyHandler);

        // generate state machine in function so
        // these experiment functions can be inherited
        // for variants
        state = GetState();
        stateMachine = GetStateMachine();
    }

    public virtual dynamic GetState() {
        state = new ExpandoObject(); // all data must be serializable
        state.isComplete = false;
        state.runIndex = 0;
        return state;
    }
    
    public virtual Dictionary<string, List<Action>> GetStateMachine() {
       var stateMachine = new Dictionary<string, List<Action>>();
       stateMachine["Run"] = new List<Action>();
       return stateMachine;
    }

    // executes state machine current function
    public void Run() {
        SaveState();
        Do(new EventBase(stateMachine["Run"][state.runIndex]));
    }

    public void FinishExperiment() {
        state.isComplete = true;
        SaveState();
        Quit();
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
        ReportEvent("countdown", new Dictionary<string, object>());
        SendHostPCMessage("COUNTDOWN", null);

        manager.Do(new EventBase<string, bool, Action>(manager.ShowVideo, 
                                                            "countdownVideo", false, 
                                                            () => this.Do(new EventBase(Run))));
    }

    // NOTE: rather than use flags for the audio test, this is entirely based off of timings.
    // Since there is processing latency (which seems to be unity version dependent), this
    // is really a hack that lets us get through the mic test unscathed. More time critical
    // applications need a different approach
    protected void RecordTest(string wavPath) {
        manager.Do(new EventBase(manager.lowBeep.Play));
        manager.Do(new EventBase<string>(manager.recorder.StartRecording, wavPath));
        manager.Do(new EventBase<string, string, string>(manager.ShowText, "microphone test recording", "Recording...", "red"));

        DoIn(new EventBase(() => {
                            manager.Do(new EventBase( () => {
                                manager.ShowText("microphone test playing", "Playing...", "green");
                                manager.playback.clip = manager.recorder.StopRecording();
                                manager.playback.Play(); // can't block manager thread, but could block
                                                         // experiment to wait on play finishing;
                                                         // could also subscribe to Unity event, if
                                                         // there is one
                                manager.DoIn(new EventBase(() => {
                                    manager.ClearText();
                                    manager.ClearTitle();
                                    Run();
                                }), (int)manager.GetSetting("micTestDuration") + 1000); // pad for latency
                                // Run();
                                }));
                            }), 
                        (int)manager.GetSetting("micTestDuration"));
    }

    protected void Encoding(StimWordList encodingList, int index) {
        int interval;
        WordStim word = encodingList[index];

        int[] limits = manager.GetSetting("stimulusInterval");
        interval = InterfaceManager.rnd.Value.Next(limits[0], limits[1]);

        Dictionary<string, object> data = new Dictionary<string, object>();
        data.Add("word", word.word);
        data.Add("serialpos", index);
        data.Add("stim", word.stim);

        ReportEvent("word stimulus info", data);
        SendHostPCMessage("WORD", data);

        manager.Do(new EventBase<string, string>(manager.ShowText, "word stimulus", word.word));


        DoIn(new EventBase(() => { manager.Do(new EventBase(manager.ClearText)); 
                                    ReportEvent("clear word stimulus", new Dictionary<string, object>());
                                    SendHostPCMessage("ISI", new Dictionary<string, object>() {{"duration", interval}});

                                    this.DoIn(new EventBase(Run), interval);
                                }), 
                            (int)manager.GetSetting("stimulusDuration"));
    }

    protected void Distractor() {
        int[] nums = new int[] { InterfaceManager.rnd.Value.Next(1, 9),
                                 InterfaceManager.rnd.Value.Next(1, 9),
                                 InterfaceManager.rnd.Value.Next(1, 9) };

        string problem = nums[0].ToString() + " + " + nums[1].ToString() + " + " + nums[2].ToString() + " = ";

        state.distractorProblem = nums;
        state.distractorAnswer = "";

        manager.RegisterKeyHandler(DistractorAnswer);
        manager.Do(new EventBase<string, string>(manager.ShowText, "display distractor problem", problem));
    }


    protected void Orientation() {

        int[] limits = manager.GetSetting("stimulusInterval");
        int interval = InterfaceManager.rnd.Value.Next(limits[0], limits[1]);

        limits = manager.GetSetting("orientationDuration");
        int duration = InterfaceManager.rnd.Value.Next(limits[0], limits[1]);
        manager.Do(new EventBase<string, string>(manager.ShowText, "orientation stimulus", "+"));

        SendHostPCMessage("ORIENT", null);

        DoIn(new EventBase(() => {
                                    manager.Do(new EventBase(manager.ClearText)); 

                                    SendHostPCMessage("ISI", new Dictionary<string, object>() {{"duration", interval}});

                                    this.DoIn(new EventBase(Run), interval);
                                }), 
                                duration);
    }

    protected void Recall(string wavPath) {
        manager.Do(new EventBase(() => {
                            // NOTE: unlike other events, that should be aligned to when they are called,
                            //       this event needs to be precisely aligned with the beginning of a
                            //       recording.
                            manager.recorder.StartRecording(wavPath);
                            ReportEvent("start recall period", new Dictionary<string, object>());
                        }));

        int duration = (int)manager.GetSetting("recallDuration");

        SendHostPCMessage("RECALL", new Dictionary<string, object>() {{"duration", duration}});

        DoIn(new EventBase(() => {
                manager.Do(new EventBase(() => {
                    manager.recorder.StopRecording();
                    manager.ClearText();
                    manager.lowBeep.Play();
                }));

                ReportEvent("end recall period", new Dictionary<string, object>());
                Run();
        }), duration );
    }

    protected void FinalRecall(string wavPath) {
        manager.Do(new EventBase(() => {
                            // NOTE: unlike other events, that should be aligned to when they are called,
                            //       this event needs to be precisely aligned with the beginning of a
                            //       recording.
                            manager.recorder.StartRecording(wavPath);
                            ReportEvent("start final recall period", new Dictionary<string, object>());
                        }));

        int duration = (int)manager.GetSetting("finalRecallDuration");

        SendHostPCMessage("FINAL RECALL", new Dictionary<string, object>() {{"duration", duration}});

        DoIn(new EventBase(() => {
                manager.Do(new EventBase(() => {
                    manager.recorder.StopRecording();
                    manager.ClearText();
                    manager.lowBeep.Play();
                }));

                ReportEvent("end final recall period", new Dictionary<string, object>());
                Run();
        }), duration );
    }

    protected void RecallPrompt() {
        manager.Do(new EventBase(manager.highBeep.Play));
        manager.Do(new EventBase<string, string>(manager.ShowText, "display recall text", "*******"));

        DoIn(new EventBase(() => {
                                    manager.Do(new EventBase(manager.ClearText));
                                    Run();
                                }), 500); // magic number is the duration of beep
    }
    
    
    protected void QuitPrompt() {
        WaitForKey("subject/session confirmation", "Running " + (string)manager.GetSetting("participantCode") + " in session " 
            + (int)manager.GetSetting("session") + " of " + (string)manager.GetSetting("experimentName") 
            + ".\n Press Y to continue, N to quit.", 
            (Action<string, bool>)QuitOrContinue);
    }

    protected void WaitForKey(string tag, string prompt, Action<string, bool> keyHandler) {
        manager.Do(new EventBase<string, string>(manager.ShowText, tag, prompt));
        manager.Do(new EventBase<Action<string, bool>>(manager.RegisterKeyHandler, keyHandler));
        // manager.Do(new EventBase<InputHandler>(manager.input.RegisterChild, keyHandler))
    }

    // protected void WaitForKey(string tag, string prompt, Func<InputHandler, KeyMsg, bool> keyHandler) {
    //     manager.Do(new EventBase<string, string>(manager.ShowText, tag, prompt));
    //     // TODO: take string or keyhandler, auto generate for string
    //     manager.Do(new EventBase<InputHandler>(manager.input.RegisterChild, keyHandler));
    // }
    
    protected void WaitForTime(int milliseconds) {
        // convert to milliseconds
        DoIn(new EventBase(Run), milliseconds); 
    }

    protected void MicTestPrompt() {
        manager.Do(new EventBase<string, string>(manager.ShowTitle, "microphone test title", "Microphone Test"));
        WaitForKey("microphone test prompt", "Press any key to record a sound after the beep.", AnyKey);
    }

    protected void ConfirmStart() {
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
        ReportEvent("experiment quit", null);
        SendHostPCMessage("EXIT", null);

        if(state.isComplete){
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
                    ReportDistractor("distractor answered", true, state.distractorProblem[0].ToString() + " + " 
                        + state.distractorProblem[1].ToString() + " + " 
                        + state.distractorProblem[2].ToString() + " = ", state.distractorAnswer);
                } 
                else {
                    manager.Do(new EventBase(manager.lowerBeep.Play));
                    ReportDistractor("distractor answered", false, state.distractorProblem[0].ToString() + " + " 
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
        ReportDistractor(message, false, problem, answer);

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

    // protected bool AnyKey(InputHandler handler, KeyMsg msg) {
    //     if(msg.down) {
    //         handler.active = false;
    //         manager.Do(new EventBase(manager.ClearText));
    //         Do(new EventBase(Run));
    //         return false;
    //     }
    //     return true;
    // }

    // protected bool AnyKey(InputHandler handler, KeyMsg msg) {
    //     if(msg.down && msg.key == 'Space') {
    //         handler.active = false;
    //         manager.Do(new EventBase(manager.ClearText));
    //         Do(new EventBase(Run));
    //         return false;
    //     }
    //     return true;
    // }

    // protected bool QuitOrContinue(InputHandler handler, KeyMsg msg) {
    //     if(msg.down && msg.key == "Y") {
    //         manager.Do(new EventBase(manager.ClearText));
    //         this.Do(new EventBase(Run));
    //         handler.active = false;
    //         return false;
    //     }
    //     else if(msg.down && msg.key == "N") {
    //         handler.active = false;
    //         Quit();
    //         return false;
    //     }
    //     return true;
    // }


    public void QuitOrContinue(string key, bool down) {
        if(down && key == "Y") {
            manager.Do(new EventBase(manager.ClearText));
            this.Do(new EventBase(Run));
        }
        else if(down && key == "N") {
            Quit();
        }
        else {
            manager.RegisterKeyHandler(QuitOrContinue);
        }
    }

    public void PressSpace(string key, bool down) {
        if(down && key == "Space") {
            manager.Do(new EventBase(manager.ClearText));
            this.Do(new EventBase(Run));
        }
        else {
            manager.RegisterKeyHandler(PressSpace);
        }
    }

    //////////
    // Saving and loading state logic
    //////////

    protected void ReportBeepPlayed(string beep, string duration) {
        Dictionary<string, object> dataDict = new Dictionary<string, object>() { { "sound name", beep }, { "sound duration", duration } };
        ReportEvent("Sound Played", dataDict);
    }

    protected void SendHostPCMessage(string type, Dictionary<string, object> data) {
        manager.Do(new EventBase<string, Dictionary<string, object>>(manager.SendHostPCMessage, type, data));
    }

    protected void ReportEvent(string type, Dictionary<string, object> data) {
        manager.Do(new EventBase<string, Dictionary<string, object>>(manager.ReportEvent, type, data));
    }

    private void ReportDistractor(string type, bool correct, string problem, string answer)
    {
        Dictionary<string, object> dataDict = new Dictionary<string, object>();
        dataDict.Add("correct", correct);
        dataDict.Add("problem", problem);
        dataDict.Add("answer", answer);
        ReportEvent(type, dataDict);
        SendHostPCMessage("MATH", dataDict);
    }
    
    public virtual void SaveState() {
        string path = System.IO.Path.Combine(manager.fileManager.SessionPath(), "experiment_state.json");
        FlexibleConfig.WriteToText(state, path);
    }

    public virtual dynamic LoadState(string participant, int session) {
        if(System.IO.File.Exists(System.IO.Path.Combine(manager.fileManager.SessionPath(participant, session), "experiment_state.json"))) {
            string json = System.IO.File.ReadAllText(System.IO.Path.Combine(manager.fileManager.SessionPath(participant, session), "experiment_state.json"));
            dynamic jObjCfg = FlexibleConfig.LoadFromText(json);
            dynamic state =  FlexibleConfig.CastToStatic(jObjCfg);

            if(state.isComplete) {
                ErrorNotification.Notify(new InvalidOperationException("Session Already Complete"));
            }

            return state;
        }
        else {
            return null;
        }
    }

    protected static void WriteAllLinesNoExtraNewline(string path, IList<string> lines)
    {
        if (path == null)
            throw new UnityException("path argument should not be null");
        if (lines == null)
            throw new UnityException("lines argument should not be null");

        using (var stream = System.IO.File.OpenWrite(path))
        {
            using (System.IO.StreamWriter writer = new System.IO.StreamWriter(stream))
            {
                if (lines.Count > 0)
                {
                    for (int i = 0; i < lines.Count - 1; i++)
                    {
                        writer.WriteLine(lines[i]);
                    }
                    writer.Write(lines[lines.Count - 1]);
                }
            }
        }
    }
}