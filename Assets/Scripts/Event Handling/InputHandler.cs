using System;
using System.Collections.Generic;
using System.Collections.Concurrent;

public struct KeyMsg {
    string key;
    bool pressed;

    public KeyMsg(string key, bool pressed) {
        this.key = key;
        this.pressed = pressed;
    }
}

public class InputHandler : MessageTreeNode<KeyMsg> {

    public InputHandler(EventQueue host, Func<KeyMsg, bool> action)
        : base(host, action) {}

    public void Key(string key, bool pressed) {
        Do(new KeyMsg(key, pressed));
    }
}