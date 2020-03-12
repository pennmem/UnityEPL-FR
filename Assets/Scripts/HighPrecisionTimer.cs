using System;
using System.Diagnostics;
using System.Threading;

class HighPrecisionTimer : IDisposable {
   private DateTime start = HighResolutionDateTime.UtcNow; 
   private Stopwatch sw = new Stopwatch();
   private ManualResetEventSlim flag = new ManualResetEventSlim();
   private Int32 dueTime;
   private Int32 period;
   private System.Threading.WaitCallback callback;
   private volatile bool running = false;
   private object stateInfo;

    public HighPrecisionTimer(System.Threading.WaitCallback _callback, object _stateInfo, Int32 _dueTime, Int32 _period) {
        dueTime = _dueTime;
        period = _period;
        callback = _callback;
        stateInfo = _stateInfo;

        flag.Reset();
        running = true;
        ThreadPool.QueueUserWorkItem(Spinner, stateInfo);
    }


    private void Spinner(object stateInfo) {
        sw.Start();
        Int32 remainingWait = dueTime;

        if(dueTime < 0) {
            flag.Wait(-1);
        }

        while(Running()) {
            sw.Restart();

            if(remainingWait > 0) {
                flag.Wait(remainingWait);
                remainingWait -= (Int32)sw.ElapsedMilliseconds;
            }
            else {
                remainingWait = period + remainingWait;
                ThreadPool.QueueUserWorkItem(callback, stateInfo);
            }

            if(period < 0) {
                flag.Wait(-1);
            }
        }
    }

// TODO: change and finalize thread safe?
    public void Change(Int32 _dueTime, Int32 _period) {
        flag.Set();
        running = false;

        Thread.MemoryBarrier();

        Interlocked.Exchange(ref dueTime, _dueTime);
        Interlocked.Exchange(ref period, _period);
        Interlocked.Exchange(ref flag, new ManualResetEventSlim());

        Thread.MemoryBarrier();

        running = true;
        flag.Reset();

        ThreadPool.QueueUserWorkItem(Spinner, stateInfo);
    }

    public void Dispose() {
        running = false;
        Thread.MemoryBarrier();
        flag.Set();
    }

    private bool Running() {
        return running;
    }

}