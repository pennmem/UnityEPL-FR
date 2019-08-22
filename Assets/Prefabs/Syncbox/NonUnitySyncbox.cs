using UnityEngine;
using System.Collections;
using System;
using System.Runtime.InteropServices;
using System.Threading;

public class NonUnitySyncbox : EventLoop 
{

    public InterfaceManager manager;

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

    private const int PULSE_START_DELAY = 1000;
    private const int TIME_BETWEEN_PULSES_MIN = 800;
    private const int TIME_BETWEEN_PULSES_MAX = 1200;

    private volatile bool stopped = true;

    public int testField;

    public void Init() {
        Debug.Log(Marshal.PtrToStringAuto (OpenUSB()));
        Debug.Log(testField);

        StopPulse();
        Start();
    }

    public bool IsRunning() {
        return !stopped;
    }

    public void StartPulse() {
        stopped = false;
        DoIn(new EventBase(Pulse), PULSE_START_DELAY);
    }

	private void Pulse ()
    {
		if(!stopped)
        {
            //pulse
            manager.scriptedInput.ReportOutOfThreadScriptedEvent("Sync pulse begin", new System.Collections.Generic.Dictionary<string, object>());
            SyncPulse();
            manager.scriptedInput.ReportOutOfThreadScriptedEvent("Sync pulse end", new System.Collections.Generic.Dictionary<string, object>());

            //wait a random time between min and max
            int timeBetweenPulses = (int)(TIME_BETWEEN_PULSES_MIN + (InterfaceManager.rnd.NextDouble() * (TIME_BETWEEN_PULSES_MAX - TIME_BETWEEN_PULSES_MIN)));
            DoIn(new EventBase(Pulse), timeBetweenPulses);
		}
	}

    public void StopPulse() {
        stopped = true;
    }
}
