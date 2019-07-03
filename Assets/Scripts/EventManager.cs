using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

public class EventManager {

    // TODO: define special events (abort, paused)

    protected System.Collections.Concurrent.ConcurrentQueue<Action> eventQueue;
    protected bool abort;

    public void passEvent(Action thisEvent) {
        if(!abort) {
            eventQueue.Enqueue(thisEvent);
        }
    }

    public IEnumerator listen() {
        abort = false;
        while(!(abort || eventQueue.IsEmpty)) {
            Action thisAction;
            eventQueue.TryDequeue(out thisAction);
            thisAction?.Invoke();
            yield return null;
        }
        yield break;
    }

    public EventManager() {
        abort = false;
        eventQueue = new System.Collections.Concurrent.ConcurrentQueue<Action>();
    }
}
