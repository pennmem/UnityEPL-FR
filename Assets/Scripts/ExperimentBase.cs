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
        // TODO: save state
        stateMachine["Run"][state.runIndex].Invoke();
    }

    //////////
    // Worker Functions for common experiment tasks.
    // Ignorant of state machine structure, return
    // true if done with task, false if expecting
    // to be called again.
    //////////

    protected bool IntroductionVideo() {
        manager.mainEvents.Do(new EventBase<string, Action>(manager.showVideo, 
                                                            "introductionVideo", 
                                                            () => this.Do(new EventBase(Run))));
        return true;
    }

    protected bool CountdownVideo() {
        manager.mainEvents.Do(new EventBase<string, Action>(manager.showVideo, 
                                                            "countdownVideo", 
                                                            () => this.Do(new EventBase(Run))));
        return true;
    }

    protected bool Encoding(IList<string> words, int index) {
        if(words.Count == index) {
            return true;
        }
        manager.mainEvents.Do(new EventBase<string>(manager.showText, words[index]));
        Debug.Log(manager.getSetting("stimulusDuration"));
        manager.mainEvents.DoIn(new EventBase(manager.clearText), (int)manager.getSetting("stimulusDuration"));
        Debug.Log(manager.getSetting("stimulusInterval"));
        this.DoIn(new EventBase(Run), (int)manager.getSetting("stimulusInterval"));
        return false;
    }

    protected bool Distractor() {
        return true;
    }

    protected bool Recall() {
        return true;
    }
    /*
    public void QuitPrompt() {
        WaitForKey("Running " + manager.getSetting("participantCode") + " in session " 
            + manager.getSetting("session") + " of " + manager.getSetting("experimentName") 
            + ".\n Press Y to continue, N to quit.", 
            (Action<KeyCode>)QuitOrContinue);
    }

    public void MicrophoneTest() {

    }

    public void WaitForKey(string prompt, Action<KeyCode> keyHandler) {
        // TODO:
        manager.mainEvents.Do(manager.showText(prompt));
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
                "to the first list.", (Action<KeyCode>)AnyKey);
    }
    */

    //////////
    // Key Handler functions
    //////////

    /*
    public void AnyKey(KeyCode key) {
        Run();
    }

    public void QuitOrContinue(KeyCode key) {
        if(key == KeyCode.N) {
            Application.Quit();
        }
        else if(key == KeyCode.Y) {
            Run();
        }
        // TODO: enqueue self as keyhandler
    }
    */


    public virtual void SaveState() {

    }
}
