﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("UnityEPL/Handlers/Write to Disk Handler")]
public class WriteToDiskHandler : DataHandler 
{
	public enum FORMAT { JSON, CSV, SQL };
	public FORMAT outputFormat;

	private static int sessionNumber = -1;

	[HideInInspector]
	[SerializeField]
	private bool useDirectoryStructure = false;
	[HideInInspector]
	[SerializeField]
	private bool participantFirst = false;
	[HideInInspector]
	[SerializeField]
	private bool writeAutomatically = true;
	[HideInInspector]
	[SerializeField]
	private int framesPerWrite = 30;

	private System.Collections.Generic.List<DataPoint> waitingPoints = new System.Collections.Generic.List<DataPoint>();

	public static void SetSessionNumber (int newSessionNumber)
	{
		sessionNumber = newSessionNumber;
	}

	public void SetUseDirectoryStructure(bool newUse)
	{
		useDirectoryStructure = newUse;
	}
	public void SetParticipantFirst(bool isFirst)
	{
		participantFirst = isFirst;
	}
	public bool UseDirectoryStructure()
	{
		return useDirectoryStructure;
	}
	public bool ParticipantFirst()
	{
		return participantFirst;
	}

	public void SetWriteAutomatically(bool newAutomatically)
	{
		writeAutomatically = newAutomatically;
	}
	public bool WriteAutomatically()
	{
		return writeAutomatically;
	}
	public void SetFramesPerWrite(int newFrames)
	{
		if (newFrames > 0)
			framesPerWrite = newFrames;
	}
	public int GetFramesPerWrite()
	{
		return framesPerWrite;
	}



	protected override void Update()
	{
		base.Update ();

		if (Time.frameCount % framesPerWrite == 0)
			DoWrite ();
	}

	protected override void HandleDataPoints(DataPoint[] dataPoints)
	{
		waitingPoints.AddRange (dataPoints);
	}

	public void DoWrite()
	{
		string directory = UnityEPL.GetDataPath();
		string filePath = System.IO.Path.Combine (directory, "unnamed.file");
		if (ParticipantFirst () && UseDirectoryStructure()) 
		{
			directory = System.IO.Path.Combine (directory, string.Join ("", UnityEPL.GetParticipants ()));
			directory = System.IO.Path.Combine (directory, UnityEPL.GetExperimentName ());
		} 
		else if (UseDirectoryStructure())
		{
			directory = System.IO.Path.Combine (directory, UnityEPL.GetExperimentName ());
			directory = System.IO.Path.Combine (directory, string.Join ("", UnityEPL.GetParticipants ()));
		}
		if (sessionNumber != -1)
			directory = System.IO.Path.Combine (directory, "session_" + sessionNumber.ToString());
			
		System.IO.Directory.CreateDirectory (directory);

		foreach (DataPoint dataPoint in waitingPoints)
		{
			string writeMe = "unrecognized type";
			string extensionlessFileName = DataReporter.GetStartTime ().ToString("yyyy-MM-dd HH mm ss");
			switch (outputFormat)
			{
				case FORMAT.CSV:
					writeMe = dataPoint.ToCSV ();
					filePath = System.IO.Path.Combine(directory, extensionlessFileName + ".csv");
					break;
				case FORMAT.JSON:
					writeMe = dataPoint.ToJSON ();
					filePath = System.IO.Path.Combine(directory, extensionlessFileName + ".json");
					break;
				case FORMAT.SQL:
					writeMe = dataPoint.ToSQL ();
					filePath = System.IO.Path.Combine(directory, extensionlessFileName + ".sql");
					break;
			}
			System.IO.File.AppendAllText(filePath, writeMe + System.Environment.NewLine);
		}
	}
}