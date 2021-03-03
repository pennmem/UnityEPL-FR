using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using UnityEngine;

public class InputHandler : MessageTreeNode<KeyMsg> {
    new protected Func<InputHandler, KeyMsg, bool> action;

    public InputHandler(EventQueue host, Func<InputHandler, KeyMsg, bool> action) {
        this.host = host;
        this.action = action;
    }

    public void Key(string key, bool pressed) {
        Do(new KeyMsg(key, pressed));
    }
    public virtual void SetAction(Func<InputHandler, KeyMsg, bool> action) {
        // should only be run from host's thread
        this.action = action;// as 
    }
}

public struct KeyMsg {
    public string key;
    public bool down;

    public KeyMsg(string key, bool down) {
        this.key = key;
        this.down = down;
    }
}