using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using KeyAction = System.Func<InputHandler, KeyMsg, bool>;

using UnityEngine;
using System.Diagnostics;
//using UnityEditor.VersionControl;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.Remoting.Messaging;


// TODO: it would be great to have a system that can handle the state more implicitly,
// such as changing it to an object that has Increment and Decrement state functions,
// is aware of the current timeline of the state, and takes a function that can inspect
// the current state and switch timelines
public abstract class ExperimentBase2 : EventLoop2 {
    public InterfaceManager manager;

    // dictionary containing lists of actions indexed
    // by the name of the function incrementing through
    // list of states
    protected StateMachine stateMachine;

    protected InputHandler inputHandler;

    public ExperimentBase2(InterfaceManager _manager) {
        manager = _manager;

        inputHandler = new InputHandler(this, null);
        manager.inputHandler.RegisterChild(inputHandler);

        CleanSlate(); // clear display, disable keyHandler
    }

    public abstract StateMachine GetStateMachine();

    // executes state machine current function
    protected void Run() {
        SaveState();
        CleanSlate();
        Do(stateMachine.GetState());
        Do(new EventBase<StateMachine>(stateMachine.GetState(), stateMachine));
    }

    protected void CleanSlate() {
        inputHandler.active = false;
        manager.Do(new EventBase(() => {
            manager.ClearText();
            manager.ClearTitle();
        }));
    }

    public void FinishExperiment(StateMachine state) {
        state.PopTimeline(); // empty timeline, so this session won't run on load
        SaveState();
        Quit();
    }

    //////////
    // Worker Functions for common experiment tasks.
    //////////

    protected void IntroductionVideo(StateMachine state) {
        state.IncrementState();
        manager.Do(new EventBase<string, bool, Action>(manager.ShowVideo,
                                                       Config.introductionVideo, true,
                                                       () => Do(new EventBase(Run))));
    }

    protected void CountdownVideo(StateMachine state) {
        ReportEvent("countdown", new Dictionary<string, object>());
        SendHostPCMessage("COUNTDOWN", null);

        state.IncrementState();
        manager.Do(new EventBase<string, bool, Action>(manager.ShowVideo,
                                                       Config.countdownVideo, false,
                                                       () => Do(new EventBase(Run))));
    }

    // NOTE: rather than use flags for the audio test, this is entirely based off of timings.
    // Since there is processing latency (which seems to be unity version dependent), this
    // is really a hack that lets us get through the mic test unscathed. More time critical
    // applications need a different approach
    protected void RecordTest(StateMachine state) {
        string wavPath = System.IO.Path.Combine(manager.fileManager.SessionPath(), "microphone_test_"
                    + DataReporter.TimeStamp().ToString("yyyy-MM-dd_HH_mm_ss") + ".wav");

        manager.Do(new EventBase(manager.lowBeep.Play));
        manager.Do(new EventBase<string>(manager.recorder.StartRecording, wavPath));
        manager.Do(new EventBase<string, string, string>(manager.ShowText, "microphone test recording", "Recording...", "red"));

        state.IncrementState();
        manager.DoIn(new EventBase(() => {
            manager.ShowText("microphone test playing", "Playing...", "green");
            manager.playback.clip = manager.recorder.StopRecording();
            manager.playback.Play(); // can't block manager thread, but could block
                                     // experiment to wait on play finishing;
                                     // could also subscribe to Unity event, if
                                     // there is one
        }),
            Config.micTestDuration);

        DoIn(new EventBase(() => {
            Run();
        }), Config.micTestDuration * 2);
    }

    protected void Encoding(WordStim word, int index) {
        // This needs to be wrapped, as it relies on data external to the state itself

        int[] limits = Config.stimulusInterval;
        int interval = InterfaceManager.rnd.Value.Next(limits[0], limits[1]);

        Dictionary<string, object> data = new Dictionary<string, object>();
        data.Add("word", word.word);
        data.Add("serialpos", index);
        data.Add("stim", word.stim);

        SendHostPCMessage("ISI", new Dictionary<string, object>() { { "duration", interval } });
        DoIn(new EventBase(() => {
            ReportEvent("word stimulus info", data);
            SendHostPCMessage("WORD", data);

            manager.Do(new EventBase<string, string>(manager.ShowText, "word stimulus", word.word));

            DoIn(new EventBase(() => {
                CleanSlate();
                ReportEvent("clear word stimulus", new Dictionary<string, object>());
                Run();
            }), Config.stimulusDuration);
        }), interval);
    }

    // TODO: JPB: Remove EncodingPostDelay
    protected void EncodingPostDelay(WordStim word, int index) {
        // This needs to be wrapped, as it relies on data external to the state itself

        int[] limits = Config.stimulusInterval;
        int interval = InterfaceManager.rnd.Value.Next(limits[0], limits[1]);

        Dictionary<string, object> data = new Dictionary<string, object>();
        data.Add("word", word.word);
        data.Add("serialpos", index);
        data.Add("stim", word.stim);

        ReportEvent("word stimulus info", data);
        SendHostPCMessage("WORD", data);

        manager.Do(new EventBase<string, string>(manager.ShowText, "word stimulus", word.word));

        DoIn(new EventBase(() => {
            CleanSlate();
            ReportEvent("clear word stimulus", new Dictionary<string, object>());
            SendHostPCMessage("ISI", new Dictionary<string, object>() { { "duration", interval } });

            DoIn(new EventBase(Run), interval);
        }), Config.stimulusDuration);
    }

    protected class DistractorState {
        public Stopwatch stopwatch = new Stopwatch();
        public int[] nums = new int[3] { -1, -1, -1 };
        public string message = "distractor update";
        public string problem = "";
        public string answer = "";
    }
    protected DistractorState ds = new DistractorState();

    protected void Distractor(StateMachine state) {
        ds.nums = new int[] { InterfaceManager.rnd.Value.Next(1, 10),
                           InterfaceManager.rnd.Value.Next(1, 10),
                           InterfaceManager.rnd.Value.Next(1, 10) };
        ds.message = "distractor update";
        ds.problem = ds.nums[0].ToString() + " + " +
                     ds.nums[1].ToString() + " + " +
                     ds.nums[2].ToString() + " = ";
        ds.answer = "";

        inputHandler.SetAction(DistractorHandler);
        manager.Do(new EventBase<string, string>(manager.ShowText, ds.message, ds.problem + ds.answer));
        ds.stopwatch.Start();
        inputHandler.active = true;
    }

    protected bool DistractorHandler(InputHandler handler, KeyMsg msg) {
        if (msg.down != true) { return true; }

        var key = msg.key;
        UnityEngine.Debug.Log("Distractor keypress: " + key);

        // Enter only numbers
        if (Regex.IsMatch(key, @"\d$")) {
            key = key[key.Length - 1].ToString(); // Unity gives numbers as Alpha# or Keypad#
            if (ds.answer.Length < 3) {
                ds.answer += key;
            }
            ds.message = "modify distractor answer";
        }
        // Delete key removes last character from answer
        else if (key == "delete" || key == "backspace") {
            if (ds.answer != "") {
                ds.answer = ds.answer.Substring(0, ds.answer.Length - 1);
            }
            ds.message = "modify distractor answer";
        }
        // Submit answer
        else if (key == "enter" || key == "return") {
            bool correct = int.Parse(ds.answer) == ds.nums.Sum();

            // Play tone depending on right or wrong answer
            if (correct) {
                manager.Do(new EventBase(manager.lowBeep.Play));
            } else {
                manager.Do(new EventBase(manager.lowerBeep.Play));
            }

            // Report results
            ds.message = "distractor answered";
            manager.Do(new EventBase<string, string>(manager.ShowText, ds.message, ds.problem + ds.answer));
            ReportDistractor(ds.message, correct, ds.problem, ds.answer);

            // End distractor or setup next math problem
            if (ds.stopwatch.ElapsedMilliseconds > Config.distractorDuration) {
                ds.stopwatch.Reset();
                stateMachine.IncrementState();
                Run();
                handler.active = false;
                return false;
            } else {
                ds.nums = new int[] { InterfaceManager.rnd.Value.Next(1, 10),
                                   InterfaceManager.rnd.Value.Next(1, 10),
                                   InterfaceManager.rnd.Value.Next(1, 10) };
                ds.message = "display distractor problem";
                ds.problem = ds.nums[0].ToString() + " + " +
                             ds.nums[1].ToString() + " + " +
                             ds.nums[2].ToString() + " = ";
                ds.answer = "";
                manager.Do(new EventBase<string, string>(manager.ShowText, ds.message, ds.problem + ds.answer));
            }
        }

        // Update screen
        manager.Do(new EventBase<string, string>(manager.ShowText, ds.message, ds.problem + ds.answer));
        return true;
    }

    protected void DistractorOriginal(StateMachine state) {
        int[] nums = new int[] { InterfaceManager.rnd.Value.Next(1, 10),
                                 InterfaceManager.rnd.Value.Next(1, 10),
                                 InterfaceManager.rnd.Value.Next(1, 10) };
        string message = "display distractor problem";
        string problem = nums[0].ToString() + " + " +
                         nums[1].ToString() + " + " +
                         nums[2].ToString() + " = ";
        string answer = "";
        Stopwatch stopwatch = new Stopwatch();

        stopwatch.Start();

        // TODO: JPB: Add support for game pauses
        while (true) {
            manager.Do(new EventBase<string, string>(manager.ShowText, message, problem + answer));

            string key = inputHandler.WaitOnKey(manager, false).key;

            // Enter only numbers
            if (Regex.IsMatch(key, @"\d$")) {
                key = key[key.Length - 1].ToString(); // Unity gives numbers as Alpha# or Keypad#
                if (answer.Length < 3) {
                    answer += key;
                }
                message = "modify distractor answer";
            }
            // Delete key removes last character from answer
            else if (key == "delete" || key == "backspace") {
                if (answer != "") {
                    answer = answer.Substring(0, answer.Length - 1);
                }
                message = "modify distractor answer";
            }
            // Submit answer
            else if (key == "enter" || key == "return") {
                bool correct = int.Parse(answer) == nums.Sum();

                // Play tone depending on right or wrong answer
                if (correct) {
                    manager.Do(new EventBase(manager.lowBeep.Play));
                } else {
                    manager.Do(new EventBase(manager.lowerBeep.Play));
                }

                // Report results
                message = "distractor answered";
                manager.Do(new EventBase<string, string>(manager.ShowText, message, problem + answer));
                ReportDistractor(message, correct, problem, answer);

                // End distractor or setup next math problem
                if (stopwatch.ElapsedMilliseconds > Config.distractorDuration) {
                    break;
                } else {
                    nums = new int[] { InterfaceManager.rnd.Value.Next(1, 10),
                                       InterfaceManager.rnd.Value.Next(1, 10),
                                       InterfaceManager.rnd.Value.Next(1, 10) };
                    message = "display distractor problem";
                    problem = nums[0].ToString() + " + " +
                              nums[1].ToString() + " + " +
                              nums[2].ToString() + " = ";
                    answer = "";
                    manager.Do(new EventBase<string, string>(manager.ShowText, message, problem + answer));
                }
            }
        }

        state.IncrementState();
        Do(new EventBase(Run));
    }

    protected void Orientation(StateMachine state) {
        int[] limits = Config.orientationDuration;
        int duration = InterfaceManager.rnd.Value.Next(limits[0], limits[1]);
        manager.Do(new EventBase<string, string>(manager.ShowText, "orientation stimulus", "+"));

        SendHostPCMessage("ORIENT", null);

        state.IncrementState();
        DoIn(new EventBase(() => {
                CleanSlate();
                SendHostPCMessage("ISI", new Dictionary<string, object>() {{"duration", duration} });
                Run();
        }), duration);
    }

    private void RecallStim() {
        var data = new Dictionary<string, object>();
        ReportEvent("recall stimulus info", data);
        SendHostPCMessage("STIM", data);
    }

    private bool SetupRecallStim() {
        // Uniform stim.
        int recstim_interval = Config.recStimulusInterval;
        int stim_duration = Config.stimulusDuration;
        int rec_period = Config.recallDuration;
        int stim_reps = rec_period / (stim_duration + recstim_interval);

        int total_interval = stim_duration + recstim_interval;
        int stim_time = total_interval;  // We'll do the first one directly at the end of this function.
        for (int i=1; i<stim_reps; i++) {
            DoIn(new EventBase(() => {
                RecallStim();
            }), stim_time);
            stim_time += total_interval;
        }

        //// Match stim distribution to encoding period.
        //StimWordList dummyList = currentSession[state.listIndex].recall;
        //// Pre-queue timed events for recall stim during the base Recall state.
        //int stim_time = 0;
        //foreach (var rec_wordstim in dummyList) {
        //    bool stim = rec_wordstim.stim;
        //    int[] limits = manager.GetSetting("stimulusInterval").ToObject<int[]>();
        //    int interval = InterfaceManager.rnd.Next(limits[0], limits[1]);
        //    int duration = manager.GetSetting("stimulusDuration").ToObject<int>();
        //    stim_time += duration + interval;
        //
        //    if (stim) {
        //        // Calculated as past end of recall period.
        //        // Stop pre-arranging stim sequence here.
        //        if (stim_time + interval > manager.GetSetting("recallDuration").ToObject<int>()) {
        //            break;
        //        }
        //
        //        DoIn(new EventBase(() => {
        //            RecallStim();
        //        }), stim_time);
        //    }
        //}

        return stim_reps > 0;
    }

    protected void Recall(string wavPath, bool stim) {
        bool stimSetup = stim ? SetupRecallStim() : false;

        manager.Do(new EventBase(() => {
                            // NOTE: unlike other events, that should be aligned to when they are called,
                            //       this event needs to be precisely aligned with the beginning of
                            //       recording.
                            manager.recorder.StartRecording(wavPath);
                            ReportEvent("start recall period", new Dictionary<string, object>());
                        }));

        int duration = Config.recallDuration;

        SendHostPCMessage("RECALL", new Dictionary<string, object>() {{"duration", duration}});

        manager.DoIn(new EventBase(() => {
                manager.recorder.StopRecording(); // FIXME: this call is SLOW
                manager.lowBeep.Play(); // TODO: we should wait for this beep to finish
        }), duration );

        DoIn(new EventBase(() => {
                ReportEvent("end recall period", new Dictionary<string, object>());
                Run();
        }), duration );

        // Make sure the first stim happens before other events delay this.
        if (stimSetup) {
            RecallStim();
        }
    }

    protected void FinalRecall(string wavPath) {
        // FIXME: this very much violates DRY

        manager.Do(new EventBase(() => {
                            // NOTE: unlike other events, that should be aligned to when they are called,
                            //       this event needs to be precisely aligned with the beginning of a
                            //       recording.
                            manager.recorder.StartRecording(wavPath);
                            ReportEvent("start final recall period", new Dictionary<string, object>());
                        }));

        int duration = Config.finalRecallDuration;

        SendHostPCMessage("FINAL RECALL", new Dictionary<string, object>() {{"duration", duration}});

        DoIn(new EventBase(() => {
                ReportEvent("end final recall period", new Dictionary<string, object>());
                Run();
        }), duration );

        manager.DoIn(new EventBase(() => {
            manager.recorder.StopRecording();
            manager.lowBeep.Play(); // TODO: we should wait for this beep to finish
        }), duration );
    }

    protected void RecallPrompt(StateMachine state) {
        manager.Do(new EventBase(() => {
                manager.highBeep.Play();
                manager.ShowText("display recall text", "*******");
            }));

        state.IncrementState();
        DoIn(new EventBase(Run), Config.recallPromptDuration);
    }
    
    
    protected void QuitPrompt(StateMachine state) {
        WaitForKey("subject/session confirmation", 
            "Running " + Config.participantCode + " in session " 
            + Config.session + " of " + Config.experimentName 
            + ".\nPress Y to continue, N to quit.", 
            (KeyAction)QuitOrContinue);
    }


    protected void MicTestPrompt(StateMachine state) {
        manager.Do(new EventBase<string, string>(manager.ShowTitle, "microphone test title", "Microphone Test"));
        WaitForKey("microphone test prompt", "Press any key to record a sound after the beep.", (KeyAction)AnyKey);
    }

    protected void ConfirmStart(StateMachine state) {
        WaitForKey("confirm start", "Please let the experimenter know \n" +
                "if you have any questions about \n" +
                "what you just did.\n\n" +
                "If you think you understand, \n" +
                "Please explain the task to the \n" +
                "experimenter in your own words.\n\n" +
                "Press any key to continue \n" +
                "to the first list.", (KeyAction)AnyKey);
    }
    
    protected virtual void Quit() {
        CleanSlate();
        ReportEvent("experiment quit", null);
        SendHostPCMessage("EXIT", null);

        if(stateMachine.isComplete){
            manager.Do(new EventBase<string, string>(manager.ShowText, "session end", "Yay! Session Complete."));
        }
        Stop();
        manager.DoIn(new EventBase(manager.LaunchLauncher), 10000);
    }


    //////////
    // Utility Functions
    //////////

    protected void WaitForKey(string tag, string prompt, Func<InputHandler, KeyMsg, bool> handler) {
        manager.Do(new EventBase<string, string>(manager.ShowText, tag, prompt));
        inputHandler.SetAction(handler);
        inputHandler.active = true;
    }

    protected void WaitForKey(string tag, string prompt, string key) {
        manager.Do(new EventBase<string, string>(manager.ShowText, tag, prompt));

        inputHandler.SetAction(
            (handler, msg) => {
                if(msg.down && msg.key == key) {
                    handler.active = false;
                    Do(new EventBase(() => {
                        stateMachine.IncrementState();
                        Run();
                    }));
                    return false;
                }
                return true;
            }
        );
        inputHandler.active = true;
    }

    protected void WaitForTime(int milliseconds) {
        // convert to milliseconds
        DoIn(new EventBase(Run), milliseconds);
    }

    //////////
    // Key Handling functions--register action to Input Handler, which then receives messages from
    // manager. Input may also be handled by registering another handler as a child of manager.
    //////////

    protected bool AnyKey(InputHandler handler, KeyMsg msg) {
        if(msg.down) {
            handler.active = false; // also done by CleanSlate
            Do(new EventBase(() => {
                stateMachine.IncrementState();
                Run();
            }));
            return false;
        }
        return true;
    }

    protected bool QuitOrContinue(InputHandler handler, KeyMsg msg) {
        if(msg.down && msg.key == "y") {
            Do(new EventBase(() => {
                stateMachine.IncrementState();
                Run();
            }));
            return false;
        }
        else if(msg.down && msg.key == "n") {
            Quit();
            return false;
        }
        return true;
    }

    // Repeats state or continues to next state
    protected bool RepeatStateOrContinue(InputHandler handler, KeyMsg msg) {
        if(msg.down && msg.key == "n") {
            // Repeat the previous state
            handler.active = false;
            Do(new EventBase(() => {
                stateMachine.DecrementState();
                Run();
            }));
            return false;
        }
        else if(msg.down && msg.key == "y") {
            // Proceed to the next state
            handler.active = false;
            Do(new EventBase(() => {
                stateMachine.IncrementState();
                Run();
            }));
            return false;
        }
        return true;
    }

    // Repeats state or exits loop timeline
    protected bool RepeatStateOrExitLoop(InputHandler handler, KeyMsg msg) {
        if (msg.down && msg.key == "n") {
            // Repeat the previous state
            handler.active = false;
            Do(new EventBase(() => {
                stateMachine.DecrementState();
                Run();
            }));
            return false;
        } else if (msg.down && msg.key == "y") {
            // This ends the loop timeline and resumes the outer scope
            handler.active = false;
            Do(new EventBase(() => {
                stateMachine.PopTimeline();
                Run();
            }));
            return false;
        }
        return true;
    }

    // Repeats loop for entire LoopTimeline or exits loop timeline
    // If this is not used at the END of a LoopTimeline, then it acts as ContinueOrExitLoop
    protected bool RepeatLoopOrExitLoop(InputHandler handler, KeyMsg msg) {
        if(msg.down && msg.key == "n") {
            // This brings us back to the top of a loop timeline
            handler.active = false;
            Do(new EventBase(() => {
                stateMachine.IncrementState();
                Run();
            }));
            return false;
        }
        else if(msg.down && msg.key == "y") {
            // This ends the loop timeline and resumes the outer scope
            handler.active = false;
            Do(new EventBase(() => {
                stateMachine.PopTimeline();
                Run();
            }));
            return false;
        }
        return true;
    }

    //////////
    // Saving, Reporting, and Loading state logic
    //////////

    protected void ReportBeepPlayed(string beep, string duration) {
        var dataDict = new Dictionary<string, object>() { { "sound name", beep }, 
                                                          { "sound duration", duration } };
        ReportEvent("Sound Played", dataDict);
    }

    protected void SendHostPCMessage(string type, Dictionary<string, object> data) {
        manager.Do(new EventBase<string, Dictionary<string, object>>(manager.SendHostPCMessage, 
                                                                     type, data));
    }

    protected void ReportEvent(string type, Dictionary<string, object> data) {
        manager.Do(new EventBase<string, Dictionary<string, object>>(manager.ReportEvent, 
                                                                     type, data));
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
        // TODO: StreamWriter to save
        JsonConvert.SerializeObject(stateMachine);
    }

    public virtual StateMachine LoadState(string participant, int session) {
        var logPath = System.IO.Path.Combine(manager.fileManager.SessionPath(participant, session), 
                                             "experiment_state.json");
        if(System.IO.File.Exists(logPath)) {
            string json = System.IO.File.ReadAllText(logPath);
            StateMachine state = JsonConvert.DeserializeObject<StateMachine>(json);

            if(state.isComplete) {
                ErrorNotification.Notify(new InvalidOperationException("Session Already Complete"));
            }
            return state;
        }
        else {
            return null;
        }
    }
}