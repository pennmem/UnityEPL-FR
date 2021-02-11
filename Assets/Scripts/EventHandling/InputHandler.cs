using System;
using System.Collections.Generic;
using System.Collections.Concurrent;

public class InputHandler : MessageTreeNode<KeyMsg> {
    public InputHandler(EventQueue host, Func<MessageTreeNode<KeyMsg>, KeyMsg, bool> action) : base(host, action) {}

    public void Key(string key, bool pressed) {
        Do(new KeyMsg(key, pressed));
    }
    public virtual void SetAction(Func<InputHandler, KeyMsg, bool> action) {
        // should only be run from host's thread
        this.action = action as Func<MessageTreeNode<KeyMsg>, KeyMsg, bool>;
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