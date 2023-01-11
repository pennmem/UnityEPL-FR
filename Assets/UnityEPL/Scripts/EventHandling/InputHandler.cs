using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using NetMQ;
using UnityEditorInternal;
using static UnityEditor.ShaderData;

using InputHandlerFunc = System.Func<InputHandler, KeyMsg, bool>;

public class InputHandler : MessageTreeNode<KeyMsg> {
    protected KeyMsg keyMsg;
    protected AutoResetEvent keyMsgWritten = null;
    protected EventLoop waitOnKeyEventLoop = null;

    public InputHandler(EventQueue host, InputHandlerFunc action) {
        this.host = host;
        SetAction(action);

        waitOnKeyEventLoop = new EventLoop();
        waitOnKeyEventLoop.Start();
    }

    // InputHandler for the WaitOnKey thread
    // TODO: JPB: Convert to static function to make it more clear
    private InputHandler(EventQueue host) {
        this.host = host;
        SetAction(ReportKey);

        keyMsgWritten = new AutoResetEvent(false);
    }

    public void Key(string key, bool pressed) {
        Do(new KeyMsg(key, pressed));
    }

    // TODO: JPB: Change this to a Do and make it work on other threads
    //            WaitOnKey needs to change like this at the same time
    public virtual void SetAction(InputHandlerFunc action) {
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

    // There can only be one call to this at a time
    // TODO: JPB: This can be improved by setting the action of the inputHandler to do nothing
    //            and then adding the new event as a child. This could then be called multiple times
    public KeyMsg WaitOnKey(InterfaceManager im, bool turnOffAllHandlers = true) {
        if (turnOffAllHandlers) {
            // Set up temporary InputHandler
            var tempInputHandler = new InputHandler(waitOnKeyEventLoop);

            // Replace im input handler
            var priorImInputHandler = im.DoGet(new Task<InputHandler>(() => {
                var priorInputHandler = im.inputHandler;
                im.inputHandler = tempInputHandler;
                return priorInputHandler;
            }));

            // Wait on key
            tempInputHandler.keyMsgWritten.WaitOne();

            // Put original input handler back
            im.DoBlocking(new EventBase(() => im.inputHandler = priorImInputHandler));

            return tempInputHandler.keyMsg;

        } else {
            // Turn off current handler
            var priorActiveState = this.active;
            this.active = false;

            // Set up temporary InputHandler
            var tempInputHandler = new InputHandler(waitOnKeyEventLoop);
            
            im.DoBlocking(new EventBase(() => im.inputHandler.RegisterChild(tempInputHandler)));

            // Wait on key
            tempInputHandler.keyMsgWritten.WaitOne();

            // Teardown
            im.DoBlocking(new EventBase(() => im.inputHandler.UnRegisterChild(tempInputHandler)));
            this.active = priorActiveState;

            return tempInputHandler.keyMsg;
        }        
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