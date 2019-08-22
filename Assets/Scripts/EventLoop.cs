using System;
using System.Threading;
using System.Collections.Concurrent;

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
        StopTimers();
    }

    public void StopTimers() {
        foreach(RepeatingEvent e in repeatingEvents) {
            e.flag.Set();
        }

        foreach(Timer t in timers) {
            t.Dispose();
        }

        base.timers = new ConcurrentBag<Timer>();
        base.repeatingEvents = new ConcurrentBag<RepeatingEvent>();
    }

    public bool Running() {
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
        if(Running()) {
            base.Do(thisEvent);
            wait.Set();
        } else {
            throw new Exception("Can't enqueue an event to a non running Loop");
        }
    }


    // enqueues repeating event at set intervals. If timer isn't
    // stopped, stopping processing thread will still stop execution
    // of events
    public override void DoRepeating(RepeatingEvent thisEvent) {
        // timers should only be created if running
        if(Running()) {
            base.DoRepeating(thisEvent);
        } else {
            throw new Exception("Can't enqueue an event to a non running Loop");
        }
    }

    public EventLoop() {
    }
}
