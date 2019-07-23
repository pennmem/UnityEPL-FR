using System.Collections.Generic;
using System.Threading;
using System.Collections.Concurrent;
using System;
public class EventQueue {
    protected ConcurrentQueue<EventBase> eventQueue;
    protected ConcurrentBag<Timer> timers = new ConcurrentBag<Timer>();
    protected ConcurrentBag<RepeatingEvent> repeatingEvents = new ConcurrentBag<RepeatingEvent>();

    public virtual void Do(EventBase thisEvent) {
        eventQueue.Enqueue(thisEvent);
    }

    // run event after delay milliseconds, using closures to retain
    // event and destroy timer
    public virtual void DoIn(EventBase thisEvent, int delay) {
        Timer timer = null;
        Do(new EventBase( () => timer = new Timer(delegate(object obj){ Do(thisEvent); timer.Dispose();}, 
                                                        null, delay, Timeout.Infinite)));
    }

    // enqueues repeating event at set intervals. If timer isn't
    // stopped, stopping processing thread will still stop execution
    // of events
    public virtual void DoRepeating(RepeatingEvent thisEvent) {
        // timers should only be created if running
        repeatingEvents.Add(thisEvent);
        timers.Add(new Timer(delegate(Object obj){ RepeatingEvent evnt = (RepeatingEvent)obj;
                                                        if(!evnt.flag.IsSet){Do(evnt);} }, 
                                                        thisEvent, thisEvent.delay, thisEvent.interval));
    }

    // Process one event in the queue.
    // Returns true if an event was available to process.
    public bool Process() {
        EventBase thisEvent;
        if (eventQueue.TryDequeue(out thisEvent)) {
          thisEvent.Invoke();
          return true;
        }
        return false;
    }

    public EventQueue() {
        eventQueue = new ConcurrentQueue<EventBase>();
    }
}

public class EventBase {
    protected Action EventAction;
    public virtual void Invoke() {
        EventAction?.Invoke();
    }

    public EventBase(Action thisAction) {
        EventAction = thisAction;
    }

    public EventBase() {}
}

// Wrapper class to allow different delegate signatures
// in Event Manager
public class EventBase<T> : EventBase {
    protected new Action<T> EventAction;
    protected T t;

    public override void Invoke() {
        EventAction?.Invoke(t);
    }
    public EventBase(Action<T> thisAction, T _t) {
        EventAction += thisAction;
        t = _t;
    }
}

public class EventBase<T, U> : EventBase {
    protected new Action<T, U> EventAction;
    protected U u;
    protected T t;

    public override void Invoke() {
        EventAction?.Invoke(t, u);
    }
    public EventBase(Action<T, U> thisAction, T _t, U _u) {
        EventAction += thisAction;
        t = _t;
        u = _u;
    }
}

public class EventBase<T, U, V> : EventBase {
    protected new Action<T, U, V> EventAction;
    protected T t;
    protected U u;
    protected V v;

    public override void Invoke() {
        EventAction?.Invoke(t,u,v);
    }
    public EventBase(Action<T, U, V> thisAction, T _t, U _u, V _v) {
        EventAction += thisAction;
        t = _t;
        u = _u;
        v = _v;
    }
}
public class EventBase<T, U, V, W> : EventBase {
    protected new Action<T, U, V, W> EventAction;
    protected T t;
    protected U u;
    protected V v;
    protected W w;

    public override void Invoke() {
        EventAction?.Invoke(t, u, v, w);
    }
    public EventBase(Action<T, U, V, W> thisAction, T _t, U _u, V _v, W _w) {
        EventAction += thisAction;
        t = _t;
        u = _u;
        v = _v;
        w = _w;
    }
}
public class RepeatingEvent : EventBase {

    public volatile int iterations;

    public int maxIterations;
    public int delay;
    public int interval;
    public ManualResetEventSlim flag;

    public RepeatingEvent(Action _action, int _iterations, int _delay, int _interval, ManualResetEventSlim _flag = null) : base(_action) {
        maxIterations = _iterations;
        delay = _delay;
        interval = _interval;
        if(_flag == null) {
            flag = new ManualResetEventSlim();
        }
        else {
            flag = _flag;
        }
    }

    public override void Invoke() {
        if(!(maxIterations < 0) && (iterations >= maxIterations)) {
            flag.Set();
            return;
        }
        iterations += 1;
        base.Invoke();
    }
}
