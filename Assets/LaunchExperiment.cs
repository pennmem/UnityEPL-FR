using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaunchExperiment : MonoBehaviour
{
	public GameObject cantGoPrompt;
	public UnityEngine.UI.InputField participantNameInput;
	public UnityEngine.UI.InputField wordsSeenInput;
	public UnityEngine.UI.InputField sessionNumberInput;
	public string experimentScene = "fr1";

	public void DoLaunchExperiment()
	{
		ushort wordsSeen;
		ushort sessionNumber;

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
		if (!ushort.TryParse (sessionNumberInput.text, out sessionNumber))
		{
			cantGoPrompt.GetComponent<UnityEngine.UI.Text> ().text = "Please enter a valid session number.";
			cantGoPrompt.SetActive (true);
			return;
		}
		if (EditableExperiment.SessionComplete(sessionNumber, participantNameInput.text))
		{
			cantGoPrompt.GetComponent<UnityEngine.UI.Text> ().text = "That session has already been completed.";
			cantGoPrompt.SetActive (true);
			return;
		}

		UnityEPL.AddParticipant(participantNameInput.text);
		EditableExperiment.ConfigureExperiment(wordsSeen, sessionNumber);

		UnityEngine.SceneManagement.SceneManager.LoadScene (experimentScene);
	}
}