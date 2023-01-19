using System;
using System.Collections;
using System.Collections.Generic;

using StateEvent = System.Func<System.Collections.IEnumerator>;

public class States : IEnumerable<StateEvent> {
    public List<StateEvent> stateEvents {
        get; protected set;
    }

    public States() {
        stateEvents = new List<StateEvent>();
    }

    public void Add(StateEvent stateEvent) {
        stateEvents.Add(stateEvent);
    }

    public void Add(States states) {
        stateEvents.AddRange(states);
    }

    public IEnumerator<StateEvent> GetEnumerator() {
        return stateEvents.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return GetEnumerator();
    }


    // HOW TO USE

    public static IEnumerator A() {
        yield return 1;
        yield return 2;
    }

    public static void Do(IEnumerator e) {
        while (e.MoveNext()) {
            Console.WriteLine(e.Current);
        }
    }

    public void HowToUse() {
        var s1 = new States() { A, A };
        var states = new States { s1, A };

        foreach (var stateEvent in states) {
            Console.WriteLine(stateEvent());
            Do(stateEvent());
        }
    }

    public static void EvalEnumerator(IEnumerator enumerator) {
        while (enumerator.MoveNext());
    }

    public static T EvalEnumerator<T>(IEnumerator<T> enumerator) {
        while (enumerator.MoveNext());
        return enumerator.Current;
    }
}

//[Serializable]
//public class Timeline<T> : IList<T> { //: IDeserializationCallback {
//    protected List<T> items = new List<T>();
//    protected bool reset_on_load;
//    public virtual bool IsReadOnly { get { return false; } }
//    public int index;
//    public virtual int Count {get { return items.Count; } }

//    public Timeline(IEnumerable<T> states, 
//                    bool reset_on_load = false) {
//        this.AddRange(states);
//        this.reset_on_load = reset_on_load;
//    }

//    public Timeline(bool reset_on_load = false) {
//        this.reset_on_load = reset_on_load;
//    }

//    virtual public bool IncrementState() {
//        if(index < this.Count - 1 ) {
//            index++;
//            return true;
//        }
//        else {
//            return false;
//        }
//    }

//    virtual public bool DecrementState() {
//        if(index > 0) {
//            index--;
//            return true;
//        }
//        else {
//            return false;
//        }
//    }

//    // void IDeserializationCallback.OnDeserialization(Object sender)
//    // {
//    //     // if reset is set, reset when the object is deserialized
//    //     if(reset_on_load) {
//    //         index = 0;
//    //     }
//    // }

//    public virtual T this[int i] {
//        get { return items[i]; }
//        set { throw new NotSupportedException("Indexing is read only"); }
//    }

//    public T GetState() {
//        return this[index];
//    }

//    virtual public int IndexOf(T item) {
//        throw new NotSupportedException("Provided only for compatibility");
//    }

//    virtual public void Insert(int index, T item) {
//        items.Insert(index, item);
//    }

//    virtual public void RemoveAt(int index) {
//        items.RemoveAt(index);
//    }

//    virtual public void Add(T item) {
//        items.Add(item);
//    }

//    virtual public void AddRange(IEnumerable<T> new_items) {
//        items.AddRange(new_items);
//    }

//    virtual public void Clear() {
//        items.Clear();
//    }

//    virtual public bool Contains(T item) {
//        throw new NotSupportedException("Provided only for compatibility");
//    }

//    virtual public void CopyTo(T[] array, int index) {
//        items.CopyTo(array, index);
//    }

//    virtual public bool Remove(T item) {
//        throw new NotSupportedException("Provided only for compatibility");
//    }

//    virtual public IEnumerator<T> GetEnumerator() {
//        return items.GetEnumerator();
//    }

//    IEnumerator System.Collections.IEnumerable.GetEnumerator() {
//       return this.GetEnumerator();
//    }
//}

[Serializable]
public class ExperimentTimeline2 : Timeline<Action<StateMachine2>> {
    public ExperimentTimeline2(List<Action<StateMachine2>> states, bool reset_on_load = false) : base(states, reset_on_load) {}
    // TODO: don't serialize functions
}

//[Serializable]
//public class LoopTimeline : ExperimentTimeline {
//    public LoopTimeline(List<Action<StateMachine>> states, bool reset_on_load = false) : base(states, reset_on_load) {}

//    // TODO: don't serialize functions

//    override public bool IncrementState() {
//        if(index < this.Count - 1 ) {
//            index++;
//        }
//        else {
//            index = 0;
//        }
//        return true;
//    }

//    override public bool DecrementState() {
//        if(index > 0) {
//            index--;
//        }
//        else {
//            index = this.Count - 1;
//        }
//        return true;
//    }
//}

public class StateMachine2 : Dictionary<string, ExperimentTimeline2> {
    // Must be a serializable type
    public bool isComplete { get; set; } = false;
    protected object currentSession;
    protected Type currentSessionType;

    public static StateMachine2 GenStateMachine<T>(T currentSession) {
        return new StateMachine2(currentSession, typeof(T));
    }

    private StateMachine2(object currentSession, Type currentSessionType) : base() {
        this.currentSession = currentSession;
        this.currentSessionType = currentSessionType;
    }

    public T CurrentSession<T>() {
        if (typeof(T) != currentSessionType)
        {
            // It would be optimal to make this into a static check without using dynamic.
            // I don't know how to do that and not have templates all over the code base.
            // Can't use dynamic because WebGL doesn't support it.
            ErrorNotification.Notify(new Exception("CurrentSession() template argument did not match the currentSessionType"));
        }
        return (T) currentSession;
    }

    public Action<StateMachine2> GetState() {
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

    private ExperimentTimeline2 GetTimeline(string timeline) {
        // this throws a keyerror if not existing, which
        // is enough of an exception for now
        return this[timeline];
    }
}
