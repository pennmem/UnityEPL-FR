using System;
using System.Collections;
using System.Threading;
using System.Collections.Concurrent;
using System.Collections.Generic;

public class EventLoop : EventQueue {
    protected volatile bool abort;
    private ManualResetEventSlim wait;

    public void Start(){
        // spawn thread
        abort = false;
        Thread loop = new Thread(Loop);
        loop.Start();
    }

    public void Stop(){
        abort = true;
        wait.Set();
    }

    protected bool Running() {
        if(!abort) {
            return true;
        }
        else {
            Process();
            return false;
        }
    }

    public void Loop() {
        while(Running()) {
            wait.Reset();
            Process();
            // Don't block indefinitely
            wait.Wait(10);
        }
    }
    public override void Do(EventBase thisEvent) {
        base.Do(thisEvent);
        wait.Set();
    }

    public EventLoop() {
        wait = new ManualResetEventSlim();
    }
}
