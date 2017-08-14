using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EditableExperiment : MonoBehaviour
{
	private static int wordsSeen;

	public TextDisplayer textDisplayer;


	IEnumerator Start()
	{
		UnityEPL.SetExperimentName ("FR1");

		//for example:
		string[] stimuli = new string[] { "Apple", "Banana", "Pear" };
		for (int i = 0; i < stimuli.Length; i++)
		{
			textDisplayer.DisplayText (stimuli [i]);
			yield return new WaitForSecondsRealtime (1);
			textDisplayer.ClearText ();
			yield return new WaitForSecondsRealtime (1);
		}
	}

	public static int WordsSeen()
	{
		return wordsSeen;
	}

	public static void ResetWordsSeen(int newWordsSeen)
	{
		wordsSeen = newWordsSeen;
	}

	public static void SaveState()
	{
		string filePath = System.IO.Path.Combine (Application.persistentDataPath, UnityEPL.GetParticipants()[0]);
		System.IO.File.WriteAllText (filePath, wordsSeen.ToString ());
	}
}