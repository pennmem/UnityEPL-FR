using UnityEngine;
using System.Collections.Generic;
using System;
using System.Runtime.InteropServices;
using System.Threading;

#if !UNITY_WEBGL
public class NonUnitySyncbox : EventLoop, ISyncBox
{
    public InterfaceManager im;

    //Function from Corey's Syncbox plugin (called "ASimplePlugin")
	[DllImport ("ASimplePlugin")]
	private static extern IntPtr OpenUSB();
	[DllImport ("ASimplePlugin")]
	private static extern IntPtr CloseUSB();
	[DllImport ("ASimplePlugin")]
	private static extern IntPtr TurnLEDOn();
	[DllImport ("ASimplePlugin")]
	private static extern IntPtr TurnLEDOff();
	[DllImport ("ASimplePlugin")]
	private static extern float SyncPulse();

    private const int PULSE_START_DELAY = 1000; // ms
    private const int TIME_BETWEEN_PULSES_MIN = 800;
    private const int TIME_BETWEEN_PULSES_MAX = 1200;

    private volatile bool stopped = true;

    public NonUnitySyncbox(InterfaceManager _im) {
        im = _im;
    }


    public void Init() {
        if(Marshal.PtrToStringAuto(OpenUSB()) == "didn't open usb...") {
            im.ReportEvent("syncbox disconnected", null, DataReporter.TimeStamp());
            throw new OperationCanceledException("Failed to connect to syncbox");
        }
        StopPulse();
        Start();
    }

    // TODO: JPB: This is technically a race condition (should be a DoGet)
    public bool IsRunning() {
        return !stopped;
    }

    public void StartPulse() {
        Do(new EventBase(() => StartPulseHandler()));
    }
    public void StartPulseHandler() {
        if (!IsRunning())
        {
            stopped = false;
            DoIn(new EventBase(Pulse), PULSE_START_DELAY);
        }
    }

    public void StopPulse() {
        Do(new EventBase(() => StopPulseHandler()));
    }
    public void StopPulseHandler() {
        stopped = true;
    }

    protected void Pulse() {
        if (!stopped) {
            // Send a pulse
            im.Do(new EventBase<string, Dictionary<string, object>, DateTime>(im.ReportEvent,
                                                                              "syncPulse",
                                                                              null,
                                                                              DataReporter.TimeStamp()));
            SyncPulse();

            // Wait a random interval between min and max
            int timeBetweenPulses = (int)(TIME_BETWEEN_PULSES_MIN + (int)(InterfaceManager.rnd.Value.NextDouble() * (TIME_BETWEEN_PULSES_MAX - TIME_BETWEEN_PULSES_MIN)));
            DoIn(new EventBase(Pulse), timeBetweenPulses);
        }
    }
}
#else
public class NonUnitySyncbox : EventLoop {
    public NonUnitySyncbox(InterfaceManager _im) {}

    public void Init() {}
    public bool IsRunning() {return false;}
    public void StartPulse() {}
    public void StopPulse() {}

    private void Pulse() {}
}
#endif