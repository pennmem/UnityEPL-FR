using System;
using System.Dynamic;
using System.Collections.Generic;
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

    protected bool IntroductionVideo() {
        manager.Do(new EventBase<string, bool, Action>(manager.showVideo, 
                                                            "introductionVideo", true,
                                                            () => this.Do(new EventBase(Run))));
        return true;
    }

    protected bool CountdownVideo() {
        manager.Do(new EventBase<string, bool, Action>(manager.showVideo, 
                                                            "countdownVideo", false, 
                                                            () => this.Do(new EventBase(Run))));
        return true;
    }

    protected bool Encoding(IList<string> words, int index) {
        if(words.Count == index) {
            return true;
        }

        int interval;

        int[] limits = manager.getSetting("stimulusInterval");
        interval = InterfaceManager.rnd.Next(limits[0], limits[1]);

        manager.Do(new EventBase<string>(manager.showText, words[index]));
        DoIn(new EventBase(() => { manager.Do(new EventBase(manager.clearText)); 
                                    this.DoIn(new EventBase(Run), interval);
                                }), 
                                (int)manager.getSetting("stimulusDuration"));
        
        return false;
    }

    protected bool Distractor() {
        int[] nums = new int[] { InterfaceManager.rnd.Next(1, 9), InterfaceManager.rnd.Next(1, 9), InterfaceManager.rnd.Next(1, 9) };
        string problem = nums[0].ToString() + " + " + nums[1].ToString() + " + " + nums[2].ToString() + " = ";
        manager.Do(new EventBase<string>(manager.showText, problem));
        // TODO: wait for key

        DistractorAnswer(sum(nums));

        return false; // Done is controlled by keyhandler
    }

    protected void DistractorAnswer(int[] problem) {
        // TODO: show answer

        string answer = "";

        manager.RegisterKeyHandler((key, down) => {

            // input

            // delete

            // enter

        });
    }

    protected bool Orientation() {

        int[] limits = manager.getSetting("stimulusInterval");
        int interval = InterfaceManager.rnd.Next(limits[0], limits[1]);

        limits = manager.getSetting("orientationDuration");
        int duration = InterfaceManager.rnd.Next(limits[0], limits[1]);
        manager.Do(new EventBase<string>(manager.showText, "+"));
        DoIn(new EventBase(() => {
                                    manager.Do(new EventBase(manager.clearText)); 
                                    this.DoIn(new EventBase(Run), interval);
                                }), 
                                duration);
        return true;
    }

    protected bool Recall() {
        manager.Do(new EventBase<string>(manager.showText, "*******"));
        DoIn(new EventBase(() => {
                                    manager.Do(new EventBase(manager.clearText));
                                    this.Do(new EventBase(Run));
                                }), 
                                (int)manager.getSetting("recallPromptDuration"));
        
        return true;
    }
    
    
    public void QuitPrompt() {
        WaitForKey("Running " + manager.getSetting("participantCode") + " in session " 
            + manager.getSetting("session") + " of " + manager.getSetting("experimentName") 
            + ".\n Press Y to continue, N to quit.", 
            (Action<string, bool>)QuitOrContinue);
    }

    public void MicrophoneTest() {

    }

    public void WaitForKey(string prompt, Action<string, bool> keyHandler) {
        manager.Do(new EventBase<string>(manager.showText, prompt));
        manager.Do(new EventBase<Action<string, bool>>(manager.RegisterKeyHandler, keyHandler));
        // TODO: keys
    }
    
    public void WaitForSeconds(float seconds) {
        // convect to milliseconds
        DoIn(new EventBase(Run), (int)seconds*1000);
    }

    public void ConfirmStart() {
        WaitForKey("Please let the experimenter know \n" +
                "if you have any questions about \n" +
                "what you just did.\n\n" +
                "If you think you understand, \n" +
                "Please explain the task to the \n" +
                "experimenter in your own words.\n\n" +
                "Press any key to continue \n" +
                "to the first list.", (Action<string, bool>)AnyKey);
    }
    
    protected void Quit() {
        Debug.Log("Quitting");
        Stop();
    #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
    #else
        Application.Quit();
    #endif
    //no more calls to Run past this point
    }

    //////////
    // Key Handler functions
    //////////

    public void AnyKey(string key, bool down) {
        if(down) {
            manager.Do(new EventBase(manager.clearText));
            Run();
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
