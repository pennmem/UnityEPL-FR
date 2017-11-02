using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticipantSelection : MonoBehaviour
{
	public UnityEngine.UI.InputField participantNameInput;
	public static int nextSessionNumber = 0;

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

		while (System.IO.File.Exists (EditableExperiment.SessionFilePath (nextSessionNumber, selectedParticipant)))
		{
			nextSessionNumber++;
		}
	}
}