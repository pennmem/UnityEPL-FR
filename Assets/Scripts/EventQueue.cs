using System.Collections.Generic;
using System.Collections.Concurrent;
using System;
public class EventQueue {
    protected ConcurrentQueue<EventBase> eventQueue;

    public virtual void Do(EventBase thisEvent) {
        // TODO: check if event belongs to current thread
        eventQueue.Enqueue(thisEvent);
    }

    public void Process() {
        EventBase thisEvent;
        while(!eventQueue.IsEmpty) {
            thisEvent = null;
            eventQueue.TryDequeue(out thisEvent);
            thisEvent?.Invoke();
        }
    }

    public EventQueue() {
        eventQueue = new ConcurrentQueue<EventBase>();
    }
}

// Wrapper class to allow different delegate signatures
// in Event Manager
public class EventBase {
    protected Action<EventBase> EventAction;
    public virtual void Invoke() {
        EventAction?.Invoke(this);
    }
    public EventBase(Action<EventBase> thisAction) {
        EventAction += thisAction;
    }

    public EventBase() {}
}