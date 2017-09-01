﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundRecorder : MonoBehaviour
{
	public string outputPath = System.IO.Path.GetFullPath(".");

	private AudioClip recording;

	//using the system's default device
	public void StartRecording(int secondsLength)
	{
		recording = Microphone.Start ("", true, secondsLength, 44100);
	}

	public void StopRecording()
	{
		Microphone.End ("");
		string filePath = System.IO.Path.Combine (outputPath, "Recording" + System.DateTime.Now.Ticks);
		SavWav.Save (filePath, recording);
	}
}