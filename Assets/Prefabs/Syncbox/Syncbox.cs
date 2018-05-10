﻿using UnityEngine;
using System.Collections;
using System;
using System.Runtime.InteropServices;
using System.Threading;

public class Syncbox : MonoBehaviour
{

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

    public ScriptedEventReporter scriptedEventReporter;

	// Use this for initialization
	void Start ()
    {
        //open usb, log the result string returned
		Debug.Log(Marshal.PtrToStringAuto (OpenUSB()));

        //start a thread which will send the pulses
        syncpulseThread = new Thread(Pulse);
        syncpulseThread.Start();
	}

	void Pulse ()
    {
        System.Random random = new System.Random();

        //delay before starting pulses
        Thread.Sleep((int)(PULSE_START_DELAY*SECONDS_TO_MILLISECONDS));
		while (true)
        {
            //pulse
            SyncPulse();
            //log the pulse
            scriptedEventReporter.ReportOutOfThreadScriptedEvent("Sync pulse begin", new System.Collections.Generic.Dictionary<string, object>());
            //wait a random time between min and max
            float timeBetweenPulses = (float)(TIME_BETWEEN_PULSES_MIN + (random.NextDouble() * (TIME_BETWEEN_PULSES_MAX - TIME_BETWEEN_PULSES_MIN)));
            Thread.Sleep((int)(timeBetweenPulses * SECONDS_TO_MILLISECONDS));
		}
	}

	void OnApplicationQuit()
    {
        //close usb, log the result string returned
		Debug.Log(Marshal.PtrToStringAuto (CloseUSB()));
        //stop the pulsing thread
        syncpulseThread.Abort();
	}

}
