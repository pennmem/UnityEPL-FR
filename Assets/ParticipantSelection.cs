using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticipantSelection : MonoBehaviour
{
	public UnityEngine.UI.InputField participantNameInput;
	public UnityEngine.UI.InputField wordsSeenInput;
	public UnityEngine.UI.InputField randomSeedInput;

	void Start ()
	{
		UnityEngine.UI.Dropdown dropdown = GetComponent<UnityEngine.UI.Dropdown> ();

		string[] filepaths = System.IO.Directory.GetFiles (Application.persistentDataPath);
		string[] filenames = new string[filepaths.Length];

		for (int i = 0; i < filepaths.Length; i++)
			filenames [i] = System.IO.Path.GetFileName (filepaths [i]);
			
		dropdown.AddOptions (new List<string>(filenames));
	}

	void Update ()
	{
		
	}

	public void LoadParticipant()
	{
		UnityEngine.UI.Dropdown dropdown = GetComponent<UnityEngine.UI.Dropdown> ();
		if (dropdown.value <= 1) 
		{
			wordsSeenInput.text = "0";
			participantNameInput.text = "New participant";
			randomSeedInput.text = Random.Range (0, 65535).ToString();
		}
		else
		{
			string participantName = dropdown.options [dropdown.value].text;
			string filepath = System.IO.Path.Combine (Application.persistentDataPath, participantName);
			string[] fileContents = System.IO.File.ReadAllLines (filepath);
			ushort wordsSeen = ushort.Parse (fileContents[0]);
			ushort randomSeed = ushort.Parse (fileContents [1]);
			wordsSeenInput.text = wordsSeen.ToString ();
			participantNameInput.text = participantName;
			randomSeedInput.text = randomSeed.ToString ();
		}
	}
}