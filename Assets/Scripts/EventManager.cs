using System;
using System.Collections;
using System.Collections.Generic;

public class EventManager {

    private Dictionary<string, Action<EventArgs>> eventDict;

    // Register a listener to an event
    public void startListening(string eventName, Action<EventArgs> listener) {
        Action<EventArgs> thisAction;
        if(eventDict.TryGetValue(eventName, out thisAction)) {
            thisAction += listener;
            eventDict[eventName] = thisAction;
        }
        else {
            thisAction += listener;
            eventDict.Add(eventName, listener);
        }
    }

    // Remove a listener from an event 
    public void stopListening(string eventName, Action<EventArgs> listener) {
        Action<EventArgs> thisAction;
        if(eventDict.TryGetValue(eventName, out thisAction)) {
            thisAction -= listener;
            eventDict[eventName] = thisAction;
        }
    }

    // call the actions associated with an event
    public void triggerEvent(string eventName, EventArgs args) {
        Action<EventArgs> thisAction;
        if(eventDict.TryGetValue(eventName, out thisAction)) {
            thisAction.Invoke(args);
        }
    }

    public EventManager() {
        eventDict = new Dictionary<string, Action<EventArgs>>();
    }
}

// Dummy class, would be ideal to make
// this polymorphic
public class EventArgs {
    public EventArgs() { x=10; y=40;}
    public int x;
    public int y;
}
