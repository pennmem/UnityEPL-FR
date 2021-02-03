using System;
using System.Collections.Generic;
using System.Collections.Concurrent;

// TODO: template on event type,
// have a way to interchange actions for children
public class IEventComponent<T> {
    private EventQueue host;

    public IEventComponent(EventQueue host) {
        this.host = host;
    }

    public void Do(T ev) {
        host.Do(ev);
    }
}

public class MessageTreeNode<U, T> : IEventComponent<IEventBase> {
    private ConcurrentQueue<IEventComponent> children();
    private U action;

    private void Propagate(T msg) {
        foreach(var child in this.children) {
            child.Handle(msg);
        }
    }

    public void Handle(T msg) {
        Do(new EventBase(action, msg));
    }

    public void RegisterChild(EventTreeNode<T> child) {
        if(!children.Contains(child)){
            children.Add(child);
        }
    }

    public void UnRegisterChild(EventTreeNode<T> child) {
        if(children.Contains(child)){
            children.Remove(child);
        }
    }
}

public class KeyMsg : IEventBase {
    string key;
    bool pressed;
}

public class InputHandler : EventTreeNode<Func<KeyMsg, bool>, KeyEvent> {

    private bool active = false;

    public void Key(string key, bool pressed) {
        Handle(new KeyMsg(key, pressed));
    }

    public void Handle(KeyMsg msg) {
        Do(new EventBase( () => {
            // operator short circuits if not active
            if( active && action.Invoke(msg) ) {
                Propagate(KeyMsg);
            }
        }));
    }
}