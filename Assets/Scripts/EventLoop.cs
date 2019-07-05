using System;
using System.Collections;
using System.Threading;
using System.Collections.Concurrent;
using System.Collections.Generic;

public class EventLoop : EventQueue {
    private ManualResetEventSlim wait;
    CancellationTokenSource tokenSource;
    CancellationToken token;

    public void Start(){
        // spawn thread
        Thread loop = new Thread(Loop);
        wait = new ManualResetEventSlim();
        tokenSource = new CancellationTokenSource();
        token = tokenSource.Token;

        loop.Start();
    }

    public void Stop(){
        tokenSource.Cancel();
        wait.Set();
    }
    public void Loop() {
        while(!token.IsCancellationRequested) {
            Process();

            wait.Reset();
            try {
                wait.Wait(token);
            } 
            catch (OperationCanceledException e) {
                Process();
            }
        }

        wait.Dispose();
        tokenSource.Dispose();
    }
    public override void Do(EventBase thisEvent) {
        base.Do(thisEvent);
        wait.Set();
    }

    public EventLoop() {
    }
}
