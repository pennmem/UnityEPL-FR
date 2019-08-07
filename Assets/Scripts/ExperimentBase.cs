using System;
using System.Dynamic;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;


public abstract class ExperimentBase : EventLoop {
    public InterfaceManager manager;

    // dictionary containing lists of actions indexed
    // by the name of the function incrementing through
    // list of states
    protected Dictionary<string, List<Action>> stateMachine;

    protected dynamic state; // object to which fields can be added at runtime

    public ExperimentBase(InterfaceManager _manager) {
        manager = _manager;
        state = new ExpandoObject(); // all data must be serializable
        stateMachine = new Dictionary<string, List<Action>>();
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
        manager.Do(new EventBase<string, bool, Action>(manager.showVideo, 
                                                            "introductionVideo", true,
                                                            () => this.Do(new EventBase(Run))));
    }

    protected void CountdownVideo() {
        manager.Do(new EventBase<string, bool, Action>(manager.showVideo, 
                                                            "countdownVideo", false, 
                                                            () => this.Do(new EventBase(Run))));
    }

    protected void RecordTest(string wavPath) {
        manager.Do(new EventBase<string>(manager.recorder.StartRecording, wavPath));
        manager.Do(new EventBase<string, string, string>(manager.showText, "recording test", "Recording...", "red"));

        DoIn(new EventBase(() => {
                            manager.Do(new EventBase( () => {
                                manager.recorder.StopRecording();
                                manager.clearText();
                                }));
                            this.Do(new EventBase(Run));
                            }), 
                        manager.getSetting("micTestDuration"));
    }

    protected void PlaybackTest(string wavPath) {
        manager.Do(new EventBase<string, string, string>(manager.showText, "playing test", "Playing...", "green"));
// TODO:           textDisplayer.ChangeColor(Color.green);
        manager.Do(new EventBase( () => {
                manager.playback.clip = manager.recorder.AudioClipFromDatapath(wavPath);
                manager.playback.Play();
            }));

        DoIn(new EventBase(() => {
                                manager.Do(new EventBase(manager.clearText));
                                this.Do(new EventBase(Run));
                            }), manager.getSetting("micTestDuration"));
    }

    protected bool Encoding(IList<string> words, int index) {
        if(words.Count == index) {
            return true;
        }

        int interval;

        int[] limits = manager.getSetting("stimulusInterval");
        interval = InterfaceManager.rnd.Next(limits[0], limits[1]);

        manager.Do(new EventBase<string, string>(manager.showText, "word stimulus", words[index]));
        DoIn(new EventBase(() => { manager.Do(new EventBase(manager.clearText)); 
                                    this.DoIn(new EventBase(Run), interval);
                                }), 
                            (int)manager.getSetting("stimulusDuration"));
        
        return false;
    }

    protected void Distractor() {
        int[] nums = new int[] { InterfaceManager.rnd.Next(1, 9), InterfaceManager.rnd.Next(1, 9), InterfaceManager.rnd.Next(1, 9) };
        string problem = nums[0].ToString() + " + " + nums[1].ToString() + " + " + nums[2].ToString() + " = ";
        manager.Do(new EventBase<string, string>(manager.showText, "distractor", problem));
        Debug.Log("distractor");

        state.distractorProblem = nums;
        state.distractorAnswer = "";

        manager.RegisterKeyHandler(DistractorAnswer);
    }


    protected void Orientation() {

        int[] limits = manager.getSetting("stimulusInterval");
        int interval = InterfaceManager.rnd.Next(limits[0], limits[1]);

        limits = manager.getSetting("orientationDuration");
        int duration = InterfaceManager.rnd.Next(limits[0], limits[1]);
        manager.Do(new EventBase<string, string>(manager.showText, "orientation", "+"));
        DoIn(new EventBase(() => {
                                    manager.Do(new EventBase(manager.clearText)); 
                                    this.DoIn(new EventBase(Run), interval);
                                }), 
                                duration);
    }

    protected void Recall(string wavPath) {
        manager.Do(new EventBase<string>(manager.recorder.StartRecording, wavPath));
        DoIn(new EventBase(() => {
                manager.Do(new EventBase(() => {
                    manager.recorder.StopRecording();
                    manager.clearText();
                    manager.lowBeep.Play();
                }));
                this.Do(new EventBase(Run));
        }), manager.getSetting("recallDuration") );
    }

    protected void RecallPrompt() {

        // TODO: VAD
        manager.Do(new EventBase(manager.highBeep.Play));
        manager.Do(new EventBase<string, string>(manager.showText, "recall stars", "*******"));
        DoIn(new EventBase(() => {
                                    manager.Do(new EventBase(manager.clearText));
                                    this.Do(new EventBase(Run));
                                }), 
                                (int)manager.getSetting("recallPromptDuration"));
    }
    
    
    public void QuitPrompt() {
        WaitForKey("Quit Prompt", "Running " + manager.getSetting("participantCode") + " in session " 
            + manager.getSetting("session") + " of " + manager.getSetting("experimentName") 
            + ".\n Press Y to continue, N to quit.", 
            (Action<string, bool>)QuitOrContinue);
    }

    public void WaitForKey(string tag, string prompt, Action<string, bool> keyHandler) {
        manager.Do(new EventBase<string, string>(manager.showText, tag, prompt));
        manager.Do(new EventBase<Action<string, bool>>(manager.RegisterKeyHandler, keyHandler));
    }
    
    public void WaitForSeconds(float seconds) {
        // convect to milliseconds
        DoIn(new EventBase(Run), (int)seconds*1000);
    }

    public void MicTestPrompt() {
        WaitForKey("start recording", "Press any key to record a sound after the beep.", AnyKey);
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
    
    protected void Quit() {
        Stop();
        manager.Do(new EventBase(manager.Quit));
    }


    //////////
    // Key Handler functions
    //////////

// TODO: Sum
    protected void DistractorAnswer(string key, bool down) {
        key = key.ToLower();
        if(down) {
            if(Regex.IsMatch(key, @"^\d+$")) {
                if(state.distractorAnswer.Length < 3) {
                    state.distractorAnswer = state.distractorAnswer + key;
                }
                manager.RegisterKeyHandler(DistractorAnswer);
            }
            // delete
            else if(key == "delete" || key == "backspace") {
                if(state.distractorAnswer != "") {
                    state.distractorAnswer = state.distractorAnswer.Substring(0, state.distractorAnswer.Length - 1);
                }
                manager.RegisterKeyHandler(DistractorAnswer);
            }
            // enter
            else if(key == "enter" || key == "return") {
                int result;
                if(int.TryParse(state.distractorAnswer, out result) && result == (state.distractorProblem[0] + state.distractorProblem[1] + state.distractorProblem[2])) {
                    manager.Do(new EventBase(manager.lowBeep.Play));
                } 
                else {
                    manager.Do(new EventBase(manager.lowerBeep.Play));
                }
                Do(new EventBase(Run));
                state.distractorProblem = "";
                state.distractorAnswer = "";
                return;
            }
        }
        else {
            manager.RegisterKeyHandler(DistractorAnswer);
        }
        string problem = state.distractorProblem[0].ToString() + " + " 
                        + state.distractorProblem[1].ToString() + " + " 
                        + state.distractorProblem[2].ToString() + " = ";
        string answer = state.distractorAnswer;
        manager.Do(new EventBase<string, string>(manager.showText, "distractor", problem + answer));
    }

    public void AnyKey(string key, bool down) {
        if(down) {
            manager.Do(new EventBase(manager.clearText));
            Do(new EventBase(Run));
        }
        else  {
            manager.RegisterKeyHandler(AnyKey);
        }
    }


    public void QuitOrContinue(string key, bool down) {

        if(down && key == "Y") {
            manager.Do(new EventBase(manager.clearText));
            this.Do(new EventBase(Run));
        }
        else if(down && key == "N") {
            this.Quit();
        }
        else {
            manager.RegisterKeyHandler(QuitOrContinue);
        }
    }

    public virtual void SaveState() {

    }
}
