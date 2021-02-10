using System;
using System.Collections.Generic;
using System.Collections.Concurrent;

public class InputHandler : MessageTreeNode<KeyMsg> {
    public InputHandler(EventQueue host, Func<MessageTreeNode<KeyMsg>, KeyMsg, bool> action) : base(host, action) {}

    public void Key(string key, bool pressed) {
        Do(new KeyMsg(key, pressed));
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