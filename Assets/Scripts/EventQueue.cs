using System.Collections.Generic;
using System.Threading;
using System.Collections.Concurrent;
using System;
using UnityEngine;

public class EventQueue {
    public ConcurrentQueue<IEventBase> eventQueue;

    protected volatile int repeatingEventID = 0;
    protected ConcurrentDictionary<int, RepeatingEvent> repeatingEvents = new ConcurrentDictionary<int, RepeatingEvent>();

    protected volatile bool running = true;

    public virtual void Do(IEventBase thisEvent) {
        eventQueue.Enqueue(thisEvent);
    }

    public virtual void DoIn(IEventBase thisEvent, int delay) {
        if(Running()) {
            RepeatingEvent repeatingEvent = new RepeatingEvent(thisEvent, 1, delay, Timeout.Infinite);

            DoRepeating(repeatingEvent);
        }
        else {
            throw new Exception("Can't add timed event to non running Queue");
        }
    }

    // enqueues repeating event at set intervals. If timer isn't
    // stopped, stopping processing thread will still stop execution
    // of events
    public virtual void DoRepeating(RepeatingEvent thisEvent) {
        // timers should only be created if running
        if(Running()) {
            thisEvent.timer = new HighPrecisionTimer(delegate(System.Object obj){ RepeatingEvent evnt = (RepeatingEvent)obj;
                                                            if(!evnt.flag.IsSet){Do(evnt);} }, 
                                                            thisEvent, thisEvent.delay, thisEvent.interval);
            repeatingEventID++;
            if(!repeatingEvents.TryAdd(repeatingEventID, thisEvent)) {
                throw new Exception("Could not add repeating event to queue");
            }
        }
        else {
            throw new Exception("Can't enqueue to non running Queue");
        }
    }

    // Process one event in the queue.
    // Returns true if an event was available to process.
    public bool Process() {
        IEventBase thisEvent;
        if (running && eventQueue.TryDequeue(out thisEvent)) {
            thisEvent.Invoke();
            return true;
        }
        return false;
    }

    public EventQueue() {
        eventQueue = new ConcurrentQueue<IEventBase>();

        RepeatingEvent cleanEvents = new RepeatingEvent(CleanRepeatingEvents, -1, 0, 30000);

        // Timers need a reference to Do, so need to have access to this scope
        cleanEvents.timer = new HighPrecisionTimer(delegate(System.Object obj){ RepeatingEvent evnt = (RepeatingEvent)obj;
                                                            if(!evnt.flag.IsSet){Do(evnt);} }, 
                                                            cleanEvents, cleanEvents.delay, cleanEvents.interval);
        
        if(!repeatingEvents.TryAdd(repeatingEventID, cleanEvents)) {
                throw new Exception("Could not add repeating event to queue");
            }
    }

    public bool Running() {
        return running;
    }

    public void Pause(bool pause) {
        DateTime time = HighResolutionDateTime.UtcNow;
        if(pause) {
            running = false;
            foreach(RepeatingEvent re in repeatingEvents.Values) {
                re.flag.Set();
                Debug.Log(re.delay);
                re.delay -= (int)((TimeSpan)(time - re.startTime)).TotalMilliseconds;
                if(re.delay <=0) {
                    re.delay = 0;
                }
                re.timer.Change(Timeout.Infinite, Timeout.Infinite);
                Debug.Log(re.delay);
            }
        } 
        else {
            running = true;
            foreach(RepeatingEvent re in repeatingEvents.Values) {
                re.flag.Reset();
                re.startTime = time;
                bool success = re.timer.Change(re.delay, re.interval);
                Debug.Log(re.delay);
            } 
        }
    }

    private void CleanRepeatingEvents() {
        RepeatingEvent re;
        foreach(int i in repeatingEvents.Keys) {
            if(repeatingEvents.TryGetValue(i, out re)) {
                if(re.iterations > 0 && re.iterations >= re.maxIterations) {
                    re.flag.Set();
                    re.timer.Dispose();
                    repeatingEvents.TryRemove(i, out re);
                }
            }
        }
    }
}

public interface IEventBase {
    void Invoke();
}

public class EventBase : IEventBase {
    protected Action EventAction;

    public virtual void Invoke() {
        try {
            EventAction?.Invoke();
        }
        catch(Exception e) {
            new ErrorNotification().Notify(e);
        }
    }

    public EventBase(Action thisAction) {
        EventAction = thisAction;
    }

    public EventBase() {}
}

// Wrapper class to allow different delegate signatures
// in Event Manager
public class EventBase<T> : EventBase {
    public EventBase(Action<T> thisAction, T t) : base(() => thisAction(t)) {}
}

public class EventBase<T, U> : EventBase {
    public EventBase(Action<T, U> thisAction, T t, U u) : base(() => thisAction(t, u)) {}
}

public class EventBase<T, U, V> : EventBase {
    public EventBase(Action<T, U, V> thisAction, T t, U u, V v) : base(() => thisAction(t, u, v)) {}
}
public class EventBase<T, U, V, W> : EventBase {
    public EventBase(Action<T, U, V, W> thisAction, T t, U u, V v, W w) : base(() =>thisAction(t, u, v, w)) {}
}
public class RepeatingEvent : IEventBase {

    public volatile int iterations;

    public int maxIterations;
    public int delay;
    public int interval;
    public ManualResetEventSlim flag;
    public HighPrecisionTimer timer;
    public DateTime startTime;

    private IEventBase thisEvent;

    public RepeatingEvent(Action _action, int _iterations, int _delay, int _interval, ManualResetEventSlim _flag = null) {
        maxIterations = _iterations;
        delay = _delay;
        interval = _interval;
        startTime = HighResolutionDateTime.UtcNow;

        if(_flag == null) {
            flag = new ManualResetEventSlim();
        }
        else {
            flag = _flag;
        }

        thisEvent = new EventBase(_action);
    }

    public RepeatingEvent(IEventBase originalEvent, int _iterations, int _delay, int _interval, ManualResetEventSlim _flag = null) {
        maxIterations = _iterations;
        delay = _delay;
        interval = _interval;
        startTime = HighResolutionDateTime.UtcNow;


        if(_flag == null) {
            flag = new ManualResetEventSlim();
        }
        else {
            flag = _flag;
        }

        thisEvent = originalEvent;
    }

    public void Invoke() {
        if(!(maxIterations < 0) && (iterations >= maxIterations)) {
            flag.Set();

            return;
        }
        iterations += 1;
        thisEvent.Invoke();
    }
}
