using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaunchExperiment : MonoBehaviour
{
	public GameObject cantGoPrompt;
	public UnityEngine.UI.InputField participantNameInput;
	public UnityEngine.UI.InputField wordsSeenInput;
	public string experimentScene = "fr1";

	public void DoLaunchExperiment()
	{
		ushort wordsSeen;
		if (participantNameInput.text.Equals (""))
		{
			cantGoPrompt.GetComponent<UnityEngine.UI.Text> ().text = "Please enter a participant";
			cantGoPrompt.SetActive (true);
			return;
		}
		if (!ushort.TryParse (wordsSeenInput.text, out wordsSeen))
		{
			cantGoPrompt.GetComponent<UnityEngine.UI.Text> ().text = "Please enter words seen or 0 to start at the beginning";
			cantGoPrompt.SetActive (true);
			return;
		}
		UnityEPL.AddParticipant(participantNameInput.text);
		EditableExperiment.ResetWordsSeen(wordsSeen);

		UnityEngine.SceneManagement.SceneManager.LoadScene (experimentScene);
	}
}