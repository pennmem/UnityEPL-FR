using System;
using System.Collections.ObjectModel;
using System.Threading;
using NetMQ;
using UnityEditorInternal;
using static UnityEditor.ShaderData;

public class InputHandler : MessageTreeNode<KeyMsg> {
    protected KeyMsg keyMsg;
    protected AutoResetEvent keyMsgWritten = new AutoResetEvent(false);
    protected EventLoop waitOnKeyEventLoop = new EventLoop();

    public InputHandler(EventQueue host, Func<InputHandler, KeyMsg, bool> action) {
        this.host = host;
        SetAction(action);
        waitOnKeyEventLoop.Start();
    }

    public void Key(string key, bool pressed) {
        Do(new KeyMsg(key, pressed));
    }

    public virtual void SetAction(Func<InputHandler, KeyMsg, bool> action) {
        // should only be run from host's thread
        this.action = (node, msg) => action((InputHandler)node, msg);
    }

    public static bool ReportKey(InputHandler handler, KeyMsg msg) {
        if (!msg.down) {
            return false;
        }

        handler.keyMsg = msg;
        handler.keyMsgWritten.Set();
        return true;
    }

    // TODO: JPB: This creates it's own waitOnKeyEventLoop... stop that
    public KeyMsg WaitOnKey(ref InterfaceManager im) {
        var priorActiveState = this.active;
        this.active = false;
        var tempInputHandler = new InputHandler(waitOnKeyEventLoop, ReportKey);
        im.inputHandler.RegisterChild(tempInputHandler);

        tempInputHandler.keyMsgWritten.WaitOne();

        im.inputHandler.UnRegisterChild(tempInputHandler);
        this.active = priorActiveState;

        return tempInputHandler.keyMsg;
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
