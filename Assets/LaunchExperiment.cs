using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaunchExperiment : MonoBehaviour
{
	public UnityEngine.UI.InputField participantNameInput;
	public UnityEngine.UI.InputField wordsSeenInput;
	public UnityEngine.UI.InputField randomSeedInput;
	public string experimentScene = "fr1";

	public void DoLaunchExperiment()
	{
		ushort wordsSeen = ushort.Parse(wordsSeenInput.text);
		ushort randomSeed = ushort.Parse (randomSeedInput.text);
		EditableExperiment.ResetWordsSeen(wordsSeen);
		EditableExperiment.ResetRandomSeed (randomSeed);
		UnityEPL.AddParticipant(participantNameInput.text);
		EditableExperiment.SaveState();
		UnityEngine.SceneManagement.SceneManager.LoadScene (experimentScene);
	}
}
