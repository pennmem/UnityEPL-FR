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

    private const float PULSE_START_DELAY = 1f;
    private const float TIME_BETWEEN_PULSES_MIN = 0.8f;
    private const float TIME_BETWEEN_PULSES_MAX = 1.2f;
    private const int SECONDS_TO_MILLISECONDS = 1000;

    private Thread syncpulseThread;
    private ManualResetEventSlim flag;

    public int testField;

    void Init() {
        Debug.Log(Marshal.PtrToStringAuto (OpenUSB()));
        Debug.Log(testField);

        flag = new ManualResetEventSlim();

        syncpulseThread = new Thread(Pulse);
        syncpulseThread.Start();
        StopPulse();
    }

    void StartPulse() {
        flag.Set();
    }

	void Pulse ()
    {
        System.Random random = new System.Random();

        //delay before starting pulses
        flag.Reset();
        flag.Wait((int)(PULSE_START_DELAY*SECONDS_TO_MILLISECONDS));
		while (true)
        {
            //pulse
            manager.scriptedInput.ReportOutOfThreadScriptedEvent("Sync pulse begin", new System.Collections.Generic.Dictionary<string, object>());
            SyncPulse();
            manager.scriptedInput.ReportOutOfThreadScriptedEvent("Sync pulse end", new System.Collections.Generic.Dictionary<string, object>());

            //wait a random time between min and max
            float timeBetweenPulses = (float)(TIME_BETWEEN_PULSES_MIN + (random.NextDouble() * (TIME_BETWEEN_PULSES_MAX - TIME_BETWEEN_PULSES_MIN)));
            flag.Reset();
            flag.Wait((int)(timeBetweenPulses * SECONDS_TO_MILLISECONDS));
		}
	}

    void StopPulse() {
        flag.Wait(-1);
    }
}
