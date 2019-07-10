using System;
using System.Collections;
using System.Threading;
using System.Collections.Concurrent;
using System.Collections.Generic;

public class EventLoop : EventQueue {
    protected volatile bool running = false;
    private ManualResetEventSlim wait;

    public void Start(){
        // spawn thread
        running = true;
        Thread loop = new Thread(Loop);
        wait = new ManualResetEventSlim();

        loop.Start();
    }

    public void Stop(){
        running = false;
        wait.Set();
    }

    protected bool Running() {
        return running;
    }

    public void Loop() {
        wait.Reset();
        while(Running()) {
            bool event_ran = Process();
            if ( ! event_ran ) {
                // Don't block indefinitely
                wait.Wait(200);
                wait.Reset();
            }
        }
        wait.Dispose();
    }
    public override void Do(EventBase thisEvent) {
        base.Do(thisEvent);
        wait.Set();
    }

    public EventLoop() {
    }
}
