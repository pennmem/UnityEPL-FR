using System.Collections.Generic;
using System.Collections.Concurrent;
using System;
public class EventQueue {
    protected ConcurrentQueue<EventBase> eventQueue;

    public virtual void Do(EventBase thisEvent) {
        // TODO: check if event belongs to current thread
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
