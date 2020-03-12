using System;
using System.Threading;
using System.Collections.Concurrent;

public class EventLoop : EventQueue {
    private ManualResetEventSlim wait;


    public void Start(){
        // spawn thread
        // TODO: prevent multiple starts

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
        RepeatingEvent re;
        foreach(int i in repeatingEvents.Keys) {
            if(repeatingEvents.TryGetValue(i, out re)) {
                re.flag.Set();
                re.timer.Dispose();
                repeatingEvents.TryRemove(i, out re);
            }
        }

        base.repeatingEvents = new ConcurrentDictionary<int, RepeatingEvent>();
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
    public override void Do(IEventBase thisEvent) {
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
        running = false;
    }
}
