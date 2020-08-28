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
   private object locker = new Object();

    public HighPrecisionTimer(System.Threading.WaitCallback _callback, object _stateInfo, Int32 _dueTime, Int32 _period) {
        dueTime = _dueTime;
        period = _period;
        callback = _callback;
        stateInfo = _stateInfo;

        flag.Reset();

        lock(locker) {running = true;}

        ThreadPool.QueueUserWorkItem(Spinner, stateInfo);
    }


    private void Spinner(object stateInfo) {
        sw.Start();
        Int32 remainingWait = dueTime;

        flag.Reset();
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
        flag.Dispose();
    }

    // FIXME: fail condition
    public bool Change(Int32 _dueTime, Int32 _period) {
        lock(locker) {running = false;}
        flag.Set();

        Interlocked.Exchange(ref this.dueTime, _dueTime);
        Interlocked.Exchange(ref this.period, _period);

        lock(locker) {running = true;}
        flag = new ManualResetEventSlim(true);

        ThreadPool.QueueUserWorkItem(Spinner, stateInfo);

        return true;
    }

    public void Dispose() {
        lock(locker) {running = true;}
        flag.Set();
    }

    private bool Running() {
        lock(locker) {return running;}
    }

}