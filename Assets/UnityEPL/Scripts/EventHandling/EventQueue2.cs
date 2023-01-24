using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

public class YieldedEvent : IEnumerator {
    protected Stack<IEnumerator> _enumerators;
    readonly object _lock = new object();
    protected bool _completed = false;

    // TODO: JPB: Pass in a wait signal to indicate when it is done
    //            This would be used in the EventLoop
    public YieldedEvent(IEnumerator enumerator) {
        _enumerators = new Stack<IEnumerator>();
        _enumerators.Push(enumerator);
    }

    public object Current {
        get {
            lock (_lock) {
                return _enumerators.Peek().Current;
            }
        }
    }

    // This function should only be called from one thread at a time
    // The lock is there to prevent this
    public bool MoveNext() {
        lock (_lock) {
            while (_enumerators.Count > 0) {
                IEnumerator currentEnumerator = _enumerators.Peek();
                if (currentEnumerator.MoveNext()) {
                    var current = currentEnumerator.Current;
                    if (current is IEnumerator) {
                        _enumerators.Push((IEnumerator)current);
                    } else {
                        return true;
                    }
                } else {
                    _enumerators.Pop();
                }
            }
            _completed = true;
            return false;
        }
    }

    public bool IsCompleted() {
        lock (_lock) {
            return _completed;
        }
    }

    public void Reset() {
        throw new NotSupportedException();
    }
}

public class YieldedEventQueue {
    protected ConcurrentQueue<Action> eventQueue;
    protected List<YieldedEvent> yieldedEvents = new List<YieldedEvent>();
    protected List<RepeatingEvent> repeatingEvents = new List<RepeatingEvent>();

    protected volatile int threadID = -1;
    protected volatile bool running = true;

    public YieldedEventQueue() {
        this.threadID = Thread.CurrentThread.ManagedThreadId;
    }

    // TODO: JPB: Add Do that takes an Action or IEvent?
    public virtual void Do(IEnumerator thisEvent) {
        var yieldedEvent = new YieldedEvent(thisEvent);
        eventQueue.Enqueue(() => {
            if (yieldedEvent.MoveNext()) {
                // Add event to yieldedEvents list if it didn't finish
                yieldedEvents.Add(yieldedEvent);
            }
        });
    }

    // TODO: JPB: Add DoIn
    public virtual void DoIn(IEnumerator thisEvent, int delay) {
        throw new NotImplementedException();
    }

    // TODO: JPB: Add DoRepeating
    public virtual void DoRepeating(IEnumerator thisEvent, int iterations, int delay, int interval) {
        throw new NotImplementedException();
    }
    public virtual void DoRepeating(RepeatingEvent thisEvent) {
        throw new NotImplementedException();
    }

    // Avoid using this if possible
    public virtual IEnumerator DoBlocking(IEnumerator thisEvent) {
        var yieldedEvent = new YieldedEvent(thisEvent);
        eventQueue.Enqueue(() => {
            if (yieldedEvent.MoveNext()) {
                // Add event to yieldedEvents list if it didn't finish
                yieldedEvents.Add(yieldedEvent);
            }
        });

        while (!yieldedEvent.IsCompleted()) {
            yield return null;
        }
        yield break;
    }

    // Avoid using this if possible
    public virtual IEnumerator<T> DoGet<T>(IEnumerator<T> thisEvent) {
        var yieldedEvent = new YieldedEvent(thisEvent);
        eventQueue.Enqueue(() => {
            if (yieldedEvent.MoveNext()) {
                // Add event to yieldedEvents list if it didn't finish
                yieldedEvents.Add(yieldedEvent);
            }
        });

        var waiting = default(T);
        while (!yieldedEvent.IsCompleted()) {
            yield return waiting;
        }
        yield return (T) yieldedEvent.Current;
    }

    public bool Process() {
        // Evaluate all yielded events
        foreach (var yieldedEvent in yieldedEvents) {
            yieldedEvent.MoveNext();
        }
        yieldedEvents.RemoveAll(x => x.IsCompleted());

        // Run the new event
        Action thisEvent;
        if (running && eventQueue.TryDequeue(out thisEvent)) {
            try {
                thisEvent.Invoke();
            } catch (Exception e) {
                ErrorNotification.Notify(e);
            }
            return true;
        }
        return false;
    }

    public bool IsRunning() {
        return running;
    }

    public void Pause(bool pause) {
        running = !pause;

        foreach (RepeatingEvent re in repeatingEvents) {
            re.Pause(pause);
        }
    }

    public void ClearRepeatingEvent(RepeatingEvent thisEvent) {
        if (repeatingEvents.Contains(thisEvent)) {
            repeatingEvents.Remove(thisEvent);
        }
    }
}

// –––––––––––––––––––––––––––––––––––––––––––––––––––––

public interface IEvent {
    void Invoke();
}

public class BaseEvent : IEvent {
    protected Action EventAction;

    public virtual void Invoke() {
        EventAction?.Invoke();
    }

    public BaseEvent(Action thisAction) {
        EventAction = thisAction;
    }
}

// Wrapper class to allow different delegate signatures
// in Event Manager
public class BaseEvent<T> : BaseEvent {
    public BaseEvent(Action<T> thisAction, T t) : base(() => thisAction(t)) {}
}

public class BaseEvent<T, U> : BaseEvent {
    public BaseEvent(Action<T, U> thisAction, T t, U u) : base(() => thisAction(t, u)) {}
}

public class BaseEvent<T, U, V> : BaseEvent {
    public BaseEvent(Action<T, U, V> thisAction, T t, U u, V v) : base(() => thisAction(t, u, v)) {}
}
public class BaseEvent<T, U, V, W> : BaseEvent {
    public BaseEvent(Action<T, U, V, W> thisAction, T t, U u, V v, W w) : base(() =>thisAction(t, u, v, w)) {}
}

//public class RepeatingEvent : IEventBase {

//    private int iterations;
//    private readonly int maxIterations;
//    private int delay;
//    private int interval;
//    public readonly ManualResetEventSlim flag;
//    private Timer timer;
//    private DateTime startTime;
//    private IEventBase thisEvent;
//    private EventQueue queue;

//    public RepeatingEvent(IEventBase originalEvent, int _iterations, int _delay, int _interval,
//                          EventQueue _queue, ManualResetEventSlim _flag = null) {
//        maxIterations = _iterations;
//        delay = _delay;
//        interval = _interval;
//        startTime = HighResolutionDateTime.UtcNow;
//        queue = _queue;


//        if (_flag == null) {
//            flag = new ManualResetEventSlim();
//        } else {
//            flag = _flag;
//        }

//        thisEvent = originalEvent;
//        SetTimer();
//    }

//    public RepeatingEvent(Action _action, int _iterations, int _delay,
//                          int _interval, EventQueue _queue,
//                          ManualResetEventSlim _flag = null)
//                          : this(new EventBase(_action),
//                                 _iterations, _delay,
//                                 _interval, _queue, _flag) {
//    }

//    private void SetTimer() {
//        TimerCallback callback = (Object obj) => {
//            // event is a keyword
//            var evnt = (RepeatingEvent)obj;
//            if (!evnt.flag.IsSet) {
//                queue.Do(evnt);
//            }
//        };

//        this.timer = new Timer(callback, this, delay, interval);
//    }

//    public void Invoke() {
//        if (!(maxIterations < 0) && (iterations >= maxIterations)) {
//            Stop();
//            return;
//        }

//        Interlocked.Increment(ref this.iterations);
//        thisEvent.Invoke();
//    }

//    public void Pause(bool pause) {
//        DateTime time = HighResolutionDateTime.UtcNow;
//        // examples don't check success of Change
//        if (pause) {
//            flag.Set();
//            delay -= (int)((TimeSpan)(time - startTime)).TotalMilliseconds;
//            if (delay <= 0) {
//                // Set delay to be the remaining portion of interval
//                // |----||----------||-----------|
//                // delay    interval    interval
//                //         elapsed          |
//                // remaining = interval - (elapsed - delay) % interval
//                // C# mod is remainder, rather than mod,
//                // so it's happy with negative values here
//                delay = interval + (delay % interval);
//            }
//            timer?.Change(Timeout.Infinite, Timeout.Infinite);
//        } else {
//            flag.Reset();
//            startTime = time;
//            timer?.Change(delay, interval);
//        }
//    }

//    public void Stop() {
//        flag.Set();
//        timer?.Dispose();
//        // set timer to null in case Pause is queued before event is removed
//        timer = null;
//        queue.Do(new EventBase<RepeatingEvent>(queue.ClearRepeatingEvent, this));
//    }
//}