using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticipantSelection : MonoBehaviour
{
	public UnityEngine.UI.InputField participantNameInput;
	public UnityEngine.UI.InputField listNumberInupt;
	public UnityEngine.UI.InputField currentSessionInput;

	void Start ()
	{
		FindParticipants ();
	}

	public void FindParticipants ()
	{
		UnityEngine.UI.Dropdown dropdown = GetComponent<UnityEngine.UI.Dropdown> ();

		dropdown.ClearOptions ();
		dropdown.AddOptions (new List<string>() {"Select participant...", "New participant"});

		string participantDirectory = EditableExperiment.CurrentExperimentFolderPath();
		System.IO.Directory.CreateDirectory (participantDirectory);
		string[] filepaths = System.IO.Directory.GetDirectories (participantDirectory);
		string[] filenames = new string[filepaths.Length];

		for (int i = 0; i < filepaths.Length; i++)
			filenames [i] = System.IO.Path.GetFileName (filepaths [i]);

		dropdown.AddOptions (new List<string>(filenames));
	}

	public void ParticipantSelected()
	{
		UnityEngine.UI.Dropdown dropdown = GetComponent<UnityEngine.UI.Dropdown> ();
		if (dropdown.value <= 1)
		{
			listNumberInupt.text = "0";
			currentSessionInput.text = "0";
			participantNameInput.text = "New participant";
		}
		else
		{
			LoadParticipant ();
		}
	}

	public void LoadParticipant()
	{
		UnityEngine.UI.Dropdown dropdown = GetComponent<UnityEngine.UI.Dropdown> ();
		string selectedParticipant = dropdown.captionText.text;

		if (!System.IO.Directory.Exists (EditableExperiment.ParticipantFolderPath(selectedParticipant)))
			throw new UnityException ("You tried to load a participant that doesn't exist.");
		
		participantNameInput.text = selectedParticipant;

		//identify earliest incomplete session
		string[] sessionFiles = System.IO.Directory.GetFiles(EditableExperiment.ParticipantFolderPath(selectedParticipant));
		int sessionNumber = 0;
		for (sessionNumber = 0; sessionNumber < sessionFiles.Length; sessionNumber++)
		{
			if (!EditableExperiment.SessionComplete(sessionNumber, selectedParticipant))
			{
				break;
			}
		}


		//load earliest incomplete session
		string sessionFilePath = EditableExperiment.SessionFilePath(sessionNumber, selectedParticipant);
		if (System.IO.File.Exists(sessionFilePath))
		{
			string[] loadedState = System.IO.File.ReadAllLines(sessionFilePath);
			currentSessionInput.text = loadedState [0];
			listNumberInupt.text = (int.Parse(loadedState [1])/12).ToString();
			//load words
			ExperimentSettings currentSettings = FRExperimentSettings.GetSettingsByName(UnityEPL.GetExperimentName());
			int wordCount = int.Parse(loadedState [2]);
			if (currentSettings.numberOfLists * currentSettings.wordsPerList != wordCount)
				throw new UnityException ("Mismatch between saved word list and experiment settings.");
			int wordStartLine = 3;
			IronPython.Runtime.List words = new IronPython.Runtime.List();
			for (int i = wordStartLine; i < currentSettings.numberOfLists * currentSettings.wordsPerList+wordStartLine; i++)
			{
				string wordString = loadedState [i];

				IronPython.Runtime.PythonDictionary word = new IronPython.Runtime.PythonDictionary ();
				string[] keyValues = wordString.Split (';');
				foreach (string keyValue in keyValues)
				{
					if (keyValue.Equals (""))
						continue;
					string[] keyValuePair = keyValue.Split (':');
					word [keyValuePair [0]] = keyValuePair [1];
				}

				words.Add (word);
			}
			EditableExperiment.SetWords (words);
		}
		else //start from the beginning if it doesn't exist yet
		{
			currentSessionInput.text = sessionNumber.ToString();
			listNumberInupt.text = "0";
		}
	}
}