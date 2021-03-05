using System;
using System.Collections;
using System.Collections.Generic;

public class Timeline<T> {
    // Could also do this functional style, with Func<StateMachine, StateMachine>>
    protected IList<T> states; 
    protected bool reset_on_load;
    protected int index;

    public Timeline(IList<T> states, 
                    bool reset_on_load = false) {
        this.states = states;
        this.reset_on_load = reset_on_load;
    }

    virtual public bool IncrementState() {
        if(index < states.Count() - 1 ) {
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

    public void GetState() {
        return states[index];
    }

    public void LoadInternalState(dynamic state) {
        if(!reset_on_load) {
            index = state;
        }
        else {
            index = 0;
        }
    }
    
    public dynamic GetInternalState() {
        return index;
    }
}

public class DataTimeline<T> : Timeline<T> {
    public DateTimeline(IList<T> states, bool reset_on_load = false) : base(states, reset_on_load) {}
}

public class ExperimentTimeline : Timeline<Action<StateMachine>> {
    public DateTimeline(IList<Action<StateMachine>> states, bool reset_on_load = false) : base(states, reset_on_load) {}
}

public class LoopTimeline : ExperimentTimeline {
    public DateTimeline(IList<Action<StateMachine>> states, bool reset_on_load = false) : base(states, reset_on_load) {}

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


public class StateMachine<T> : Dictionary<string, ExperimentTimeline> {
    public T sessionData;

    public StateMachine(T sessionData) : base() {
        this.sessionData = sessionData;
    }

    public void Run() {
        GetTimeline(timelines.Peek()).GetState().Invoke(this);
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
        if(self.Contains(timeline)) {
            timelines.Push(timeline);
        }
        else {
            throw new Exception("State machine has no timeline " + timeline);
        }
    }

    public void PopTimeline() {
        timelines.Pop();
    }

    //     public void SaveState(string path) {
    //          iterate over all timelines and save their GetInternalState
    //     }
    //     public void LoadState(string path) {
    //     }

    private ExperimentTimeline GetTimeline(string timeline) {
        // this throws a keyerror if not existing, which
        // is enough of an exception for now
        return this[timeline];
    }
}
