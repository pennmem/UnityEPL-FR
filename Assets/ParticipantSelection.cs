using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticipantSelection : MonoBehaviour
{
	public UnityEngine.UI.InputField participantNameInput;
	public UnityEngine.UI.InputField wordsSeenInput;

	public void FindParticipants ()
	{
		UnityEngine.UI.Dropdown dropdown = GetComponent<UnityEngine.UI.Dropdown> ();

		string participantDirectory = System.IO.Path.Combine (UnityEPL.GetDataPath (), UnityEPL.GetExperimentName ());
		System.IO.Directory.CreateDirectory (participantDirectory);
		string[] filepaths = System.IO.Directory.GetFiles (participantDirectory);
		string[] filenames = new string[filepaths.Length];

		for (int i = 0; i < filepaths.Length; i++)
			filenames [i] = System.IO.Path.GetFileName (filepaths [i]);

		dropdown.AddOptions (new List<string>(filenames));
	}

	public void LoadParticipant()
	{
		UnityEngine.UI.Dropdown dropdown = GetComponent<UnityEngine.UI.Dropdown> ();
		if (dropdown.value <= 1)
		{
			wordsSeenInput.text = "0";
			participantNameInput.text = "New participant";
		}
		else
		{
			string participantName = dropdown.options [dropdown.value].text;
			string filepath = System.IO.Path.Combine (Application.persistentDataPath, participantName);
			string[] fileContents = System.IO.File.ReadAllLines (filepath);
			ushort wordsSeen = ushort.Parse (fileContents[0]);
			wordsSeenInput.text = wordsSeen.ToString ();
			participantNameInput.text = participantName;
		}
	}
}