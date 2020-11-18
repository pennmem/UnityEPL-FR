using System.Collections.Generic;
using System.Threading;
using System.Collections.Concurrent;
using System;
using UnityEngine;

public class EventQueue {
    public ConcurrentQueue<IEventBase> eventQueue;

    protected int repeatingEventID = 0;
    protected ConcurrentDictionary<int, RepeatingEvent> repeatingEvents = new ConcurrentDictionary<int, RepeatingEvent>();

    protected volatile bool running = true;

    public virtual void Do(IEventBase thisEvent) {
        eventQueue.Enqueue(thisEvent);
    }

    public virtual void DoIn(IEventBase thisEvent, int delay) {
        if(Running()) {
            RepeatingEvent repeatingEvent = new RepeatingEvent(thisEvent, 1, delay, Timeout.Infinite, this);

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

        if(Running()) {
            Interlocked.Increment(ref this.repeatingEventID);
            if(!repeatingEvents.TryAdd(repeatingEventID, thisEvent)) {
                throw new Exception("Could not add repeating event to queue");
            }
        }
        else {
            throw new Exception("Can't enqueue to non running Queue");
        }
    }

    public virtual void DoRepeating(IEventBase thisEvent, int iterations, int delay, int interval) {
        // timers should only be created if running
        DoRepeating(new RepeatingEvent(thisEvent, iterations, delay, interval, this));
    }

    // Process one event in the queue.
    // Returns true if an event was available to process.
    public bool Process() {
        IEventBase thisEvent;
        if (running && eventQueue.TryDequeue(out thisEvent)) {
            try {
                thisEvent.Invoke();
            } catch(Exception e) {
                ErrorNotification.Notify(e);
            }
            return true;
        }
        return false;
    }

    public EventQueue() {
        eventQueue = new ConcurrentQueue<IEventBase>();
        RepeatingEvent cleanEvents = new RepeatingEvent(CleanRepeatingEvents, -1, 0, 30000, this);
        DoRepeating(cleanEvents);
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
                re.delay -= (int)((TimeSpan)(time - re.startTime)).TotalMilliseconds;
                if(re.delay <=0) {
                    re.delay = 0;
                }
                re.timer.Change(Timeout.Infinite, Timeout.Infinite);
            }
        } 
        else {
            running = true;
            foreach(RepeatingEvent re in repeatingEvents.Values) {
                re.flag.Reset();
                re.startTime = time;
                bool success = re.timer.Change(re.delay, re.interval);
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

    public int iterations;

    public readonly int maxIterations;
    public int delay;
    public int interval;
    public readonly ManualResetEventSlim flag;
    public Timer timer;
    public DateTime startTime;

    private IEventBase thisEvent;
    private EventQueue queue;

public RepeatingEvent(IEventBase originalEvent, int _iterations, int _delay, int _interval, EventQueue _queue, ManualResetEventSlim _flag = null) {
        maxIterations = _iterations;
        delay = _delay;
        interval = _interval;
        startTime = HighResolutionDateTime.UtcNow;
        queue = _queue;


        if(_flag == null) {
            flag = new ManualResetEventSlim();
        }
        else {
            flag = _flag;
        }

        thisEvent = originalEvent;
        SetTimer();
    }

    public RepeatingEvent(Action _action, int _iterations, int _delay,
                          int _interval, EventQueue _queue,
                          ManualResetEventSlim _flag = null) 
                          : this(new EventBase(_action), 
                                 _iterations, _delay, 
                                 _interval, _queue, _flag) {}

    private void SetTimer() {
        this.timer = new Timer(delegate(System.Object obj){ RepeatingEvent evnt = (RepeatingEvent)obj;
                                                                              if(!evnt.flag.IsSet){this.queue.Do(evnt);} 
                                                                            }, 
                                                this, this.delay, this.interval);
    }

    public void Invoke() {
        if(!(maxIterations < 0) && (iterations >= maxIterations)) {
            flag.Set();
            return;
        }
        Interlocked.Increment(ref this.iterations);
        thisEvent.Invoke();
    }
}
