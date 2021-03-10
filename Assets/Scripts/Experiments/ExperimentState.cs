using System;
using System.Collections;
using System.Collections.Generic;

[Serializable]
public class Timeline<T> { //: IDeserializationCallback {
    // TODO: would be nice to give this IList interface

    public List<T> states; 
    protected bool reset_on_load;
    public int index;

    public Timeline(List<T> states, 
                    bool reset_on_load = false) {
        this.states = states;
        this.reset_on_load = reset_on_load;
    }

    public Timeline(bool reset_on_load = false) {
        this.states = new List<T>();
        this.reset_on_load = reset_on_load;
    }

    virtual public bool IncrementState() {
        if(index < states.Count - 1 ) {
            index++;
            return true;
        }
        else {
            return false;
        }
    }

    virtual public bool DecrementState() {
        if(index > 0) {
            index--;
            return true;
        }
        else {
            return false;
        }
    }

    // void IDeserializationCallback.OnDeserialization(Object sender)
    // {
    //     // if reset is set, reset when the object is deserialized
    //     if(reset_on_load) {
    //         index = 0;
    //     }
    // }

    public T GetState() {
        return states[index];
    }
}

[Serializable]
public class ExperimentTimeline : Timeline<Action<StateMachine>> {
    public ExperimentTimeline(List<Action<StateMachine>> states, bool reset_on_load = false) : base(states, reset_on_load) {}
    // TODO: don't serialize functions
}

[Serializable]
public class LoopTimeline : ExperimentTimeline {
    public LoopTimeline(List<Action<StateMachine>> states, bool reset_on_load = false) : base(states, reset_on_load) {}

    // TODO: don't serialize functions

    override public bool IncrementState() {
        if(index < states.Count - 1 ) {
            index++;
        }
        else {
            index = 0;
        }
        return true;
    }

    override public bool DecrementState() {
        if(index > 0) {
            index--;
        }
        else {
            index = states.Count - 1;
        }
            return true;
    }
}

public class StateMachine : Dictionary<string, ExperimentTimeline> {
    // Must be a serializable type
    public dynamic currentSession;
    public bool isComplete {get; set; } = false;

    public StateMachine(dynamic currentSession) : base() {
        this.currentSession = currentSession;
    }

    public Action<StateMachine> GetState() {
        return GetTimeline(timelines.Peek()).GetState();
    }

    // LIFO queue describing state machine timelines,
    // the timeline visible with Peek is the current timeline
    protected Stack<string> timelines = new Stack<string>();

    public void IncrementState() {
        if(!GetTimeline(timelines.Peek()).IncrementState()) {
            PopTimeline();
        }
    }

    public void DecrementState() {
        if(!GetTimeline(timelines.Peek()).DecrementState()){
            PopTimeline();
        }
    }

    public void PushTimeline(string timeline) {
        if(this.ContainsKey(timeline)) {
            timelines.Push(timeline);
        }
        else {
            throw new Exception("State machine has no timeline " + timeline);
        }
    }

    public void PopTimeline() {
        timelines.Pop();
    }

    private ExperimentTimeline GetTimeline(string timeline) {
        // this throws a keyerror if not existing, which
        // is enough of an exception for now
        return this[timeline];
    }
}
