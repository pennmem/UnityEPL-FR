using System.Collections.Generic;
using System.Collections.Concurrent;
using System;
public class EventQueue {
    protected ConcurrentQueue<EventBase> eventQueue;

    public virtual void Do(EventBase thisEvent) {
        eventQueue.Enqueue(thisEvent);
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

// Wrapper class to allow different delegate signatures
// in Event Manager
public class EventBase<T> : EventBase {
    protected new Action<T> EventAction;
    protected T args;

    public override void Invoke() {
        EventAction?.Invoke(args);
    }
    public EventBase(Action<T> thisAction, T _args) {
        EventAction += thisAction;
        args = _args;
    }
}

public class EventBase {
    protected Action EventAction;
    public virtual void Invoke() {
        EventAction?.Invoke();
    }

    public EventBase(Action thisAction) {
        EventAction += thisAction;
    }

    public EventBase() {}
}
