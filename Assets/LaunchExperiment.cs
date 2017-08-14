using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaunchExperiment : MonoBehaviour
{
	public UnityEngine.UI.InputField participantNameInput;
	public UnityEngine.UI.InputField wordsSeenInput;
	public string experimentScene = "fr1_experiment";

	public void DoLaunchExperiment()
	{
		int wordsSeen = int.Parse(wordsSeenInput.text);
		EditableExperiment.ResetWordsSeen(wordsSeen);
		UnityEPL.AddParticipant(participantNameInput.text);
		EditableExperiment.SaveState();
	}
}
