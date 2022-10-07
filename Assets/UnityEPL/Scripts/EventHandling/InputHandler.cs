using System;
using System.Collections.ObjectModel;
using System.Threading;
using NetMQ;
using UnityEditorInternal;
using static UnityEditor.ShaderData;

public class InputHandler : MessageTreeNode<KeyMsg> {
    protected KeyMsg keyMsg;
    protected AutoResetEvent keyMsgWritten = null;
    protected EventLoop waitOnKeyEventLoop = null;

    public InputHandler(EventQueue host, Func<InputHandler, KeyMsg, bool> action) {
        this.host = host;
        SetAction(action);

        waitOnKeyEventLoop = new EventLoop();
        waitOnKeyEventLoop.Start();
    }

    // InputHandler for the WaitOnKey thread
    private InputHandler(EventQueue host) {
        this.host = host;
        SetAction(ReportKey);

        keyMsgWritten = new AutoResetEvent(false);
    }

    public void Key(string key, bool pressed) {
        Do(new KeyMsg(key, pressed));
    }

    // TODO: JPB: Change this to a Do and make it work on other threads
    //            WaitOnKey needs to change like this too at the same time
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

    // TODO: JPB: Add feature to turn off the other handlers
    //            I don't want "q" to quit if I'm recording keyboard input
    public KeyMsg WaitOnKey(ref InterfaceManager im) {
        // Turn off current handler
        var priorActiveState = this.active;
        this.active = false;

        // Set up temporary InputHandler
        var tempInputHandler = new InputHandler(waitOnKeyEventLoop);
        im.inputHandler.RegisterChild(tempInputHandler);

        // Wait on key
        tempInputHandler.keyMsgWritten.WaitOne();

        // Teardown
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