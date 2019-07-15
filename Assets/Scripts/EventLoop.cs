using System;
using System.Threading;
using System.Collections.Concurrent;


// TODO: dispose of all created handles, make resuming either
// possible or explicitly reset the state of the queue

public class EventLoop : EventQueue {
    protected volatile bool running = false;
    private ManualResetEventSlim wait;
    private ConcurrentBag<Timer> timers = new ConcurrentBag<Timer>();
    private ConcurrentBag<RepeatingEvent> repeatingEvents = new ConcurrentBag<RepeatingEvent>();

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

        timers = new ConcurrentBag<Timer>();
        repeatingEvents = new ConcurrentBag<RepeatingEvent>();
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
    public void DoRepeating(RepeatingEvent thisEvent) {
        // timers should only be created if running
        if(Running()) {
            repeatingEvents.Add(thisEvent);
            timers.Add(new Timer(delegate(Object obj){ RepeatingEvent evnt = (RepeatingEvent)obj;
                                                        if(!evnt.flag.IsSet){Do(evnt);} }, 
                                                        thisEvent, thisEvent.delay, thisEvent.interval));
        } else {
            throw new Exception("Can't enqueue an event to a non running Loop");
        }
    }

    public EventLoop() {
    }
}
