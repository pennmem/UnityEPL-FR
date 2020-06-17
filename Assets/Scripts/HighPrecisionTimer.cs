using System;
using System.Diagnostics;
using System.Threading;

public class HighPrecisionTimer : IDisposable {
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

            if(remainingWait <= 0) {
                ThreadPool.QueueUserWorkItem(callback, stateInfo);

                if(period >= 0) {
                    remainingWait = period + remainingWait;
                } else {
                    flag.Wait(-1);
                }
            }
        }
    }

// TODO: change and finalize thread safe?
// FIXME: fail condition
    public bool Change(Int32 _dueTime, Int32 _period) {
        running = false;
        Thread.MemoryBarrier();
        flag.Set();

        dueTime = _dueTime;
        period = _period;

        running = true;
        flag.Reset();

        ThreadPool.QueueUserWorkItem(Spinner, stateInfo);

        return true;
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