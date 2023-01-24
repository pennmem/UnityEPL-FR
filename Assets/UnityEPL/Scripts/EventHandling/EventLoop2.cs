using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

public class EventLoop2 : YieldedEventQueue {
    protected ManualResetEventSlim wait;
    private CancellationTokenSource tokenSource;
    private CancellationToken cancellationToken;

    public EventLoop2()
    {
        wait = new ManualResetEventSlim();
        running = false;
    }

    ~EventLoop2()
    {
        StopTimers();
        wait.Dispose();
    }

    public void Start(){
        if(IsRunning()) {
            return;
        }

        running = true;
        Thread loop = new Thread(Loop);

        tokenSource = new CancellationTokenSource();
        loop.Start(tokenSource.Token);
    }

    public void Stop(){
        if(!IsRunning()) {
            return;
        }

        running = false;
        tokenSource.Cancel();
        tokenSource.Dispose();
        wait.Set();
        StopTimers();
    }

    public void StopTimers() {
        foreach(var re in repeatingEvents) {
            re.Stop();
        }
        repeatingEvents.Clear();
    }

    protected void Loop(object token) {
        cancellationToken = (CancellationToken) token;
        wait.Reset();
        while(!cancellationToken.IsCancellationRequested) {
            bool event_ran = Process();
            if(!event_ran) {
                wait.Wait(20);
                wait.Reset();
            }
        }
    }

    public override void Do(IEnumerator thisEvent) {
        base.Do(thisEvent);
        wait.Set();
    }

    public override void DoIn(IEnumerator thisEvent, int delay) {
        base.DoIn(thisEvent, delay);
        wait.Set();
    }

    public override void DoRepeating(IEnumerator thisEvent, int iterations, int delay, int interval) {
        base.DoRepeating(thisEvent, iterations, delay, interval);
        wait.Set();
    }
    public override void DoRepeating(RepeatingEvent thisEvent) {
        base.DoRepeating(thisEvent);
        wait.Set();
    }

    public override IEnumerator DoBlocking(IEnumerator thisEvent) {
        return base.DoBlocking(thisEvent);
        // TODO: JPB: (bug) Set needs to be right before task.Wait() in EventQueue
        // wait.Set();
    }

    public override IEnumerator<T> DoGet<T>(IEnumerator<T> thisEvent) {
        return base.DoGet(thisEvent);
        // TODO: JPB: (bug) Set needs to be right before task.Wait() in EventLoop
        // wait.Set();
    }

    //public override void DoRepeating(RepeatingEvent thisEvent) {
    //    // enqueues repeating event at set intervals. If timer isn't
    //    // stopped, stopping processing thread will still stop execution
    //    // of events. Timers should only be created if running,
    //    // otherwise their behavior isn't well defined

    //    if(Running()) {
    //        base.DoRepeating(thisEvent);
    //    } else {
    //        throw new Exception("Can't enqueue an event to a non running Loop");
    //    }
    //}

    //// Maybe make all Events in ExperimentBase YieldedFuncs?
    ////     - Implicit yields
    //// throw new InvalidOperationException("Cannot call DoYielded if there is already another yielded function in the same EventQueue");
    //protected class YieldedFunc {
    //    public bool IsStarted { get; protected set; }
    //    public bool IsDone { get; protected set; }
    //    public IEnumerator Enumerator { get; protected set; }

    //    public YieldedFunc(IEnumerator enumerator) {
    //        this.Enumerator = enumerator;
    //        this.IsStarted = false;
    //        this.IsDone = false;
    //    }

    //    public void Start() {
    //        this.IsStarted = true;
    //    }

    //    public bool MoveNext() {
    //        var result = this.Enumerator.MoveNext();
    //        this.IsDone = !result;
    //        return result;
    //    }

    //    // Current can't be implemented because it would require dynamic keyword
    //}

    //protected List<YieldedFunc> yieldedFuncs;
    //public virtual T DoYielded<T>(IEnumerator<T> enumerator) {
    //    var yieldedFunc = new YieldedFunc(enumerator);
    //    yieldedFuncs.Add(yieldedFunc);
    //    DoBlocking(new EventBase(() => {
    //        // It loops since it is in charge now.
    //        yieldedFunc.Start();
    //        LoopYielded(yieldedFunc);
    //    }));

    //    return enumerator.Current;
    //}

    //// TODO: JPB: I may be able to remove the parameter and combine with Loop above
    ////            Do this by checking if finished is true and it is the lastStarted
    //// This has the HUGE problem!!!
    ////     Imagine a situation where you did WaitOnKey and then did WaitOnClassifier
    ////     If the WaitOnClassifier took 5 seconds, you wouldn't get any key input for those 5 seconds
    //protected void LoopYielded(YieldedFunc originalYieldedFunc) {
    //    wait.Reset();
    //    while (!cancellationToken.IsCancellationRequested) {
    //        var lastStarted = yieldedFuncs.FindLast(x => x.IsStarted);
    //        foreach (var yieldedFunc in yieldedFuncs) {
    //            if (yieldedFunc.IsStarted) {
    //                var current = yieldedFunc.Enumerator.Current;
    //                yieldedFunc.MoveNext();
    //                //if (finished && yieldedFunc == lastStarted) {
    //                if (yieldedFunc.IsDone && yieldedFunc == originalYieldedFunc) {
    //                    return;
    //                }
    //            }
    //        }

    //        bool event_ran = Process();
    //        if (!event_ran) {
    //            wait.Wait(200);
    //            wait.Reset();
    //        }
    //    }
    //}
}