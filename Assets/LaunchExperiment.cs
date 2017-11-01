using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaunchExperiment : MonoBehaviour
{
	public GameObject cantGoPrompt;
	public UnityEngine.UI.InputField participantNameInput;
	public UnityEngine.UI.InputField listNumberInput;
	public UnityEngine.UI.InputField sessionNumberInput;

	public void DoLaunchExperiment()
	{
		ushort listNumber;
		ushort sessionNumber;

		ExperimentSettings experimentSettings = FRExperimentSettings.GetSettingsByName (UnityEPL.GetExperimentName ());

		if (participantNameInput.text.Equals (""))
		{
			cantGoPrompt.GetComponent<UnityEngine.UI.Text> ().text = "Please enter a participant";
			cantGoPrompt.SetActive (true);
			return;
		}
		if (!IsValidParticipantName (participantNameInput.text))
		{
			cantGoPrompt.GetComponent<UnityEngine.UI.Text> ().text = "Please enter a valid participant name (ex. R1123E or LTP123)";
			cantGoPrompt.SetActive (true);
			return;
		}
		if (!ushort.TryParse (listNumberInput.text, out listNumber) || listNumber < 0 || listNumber >= experimentSettings.numberOfLists)
		{
			cantGoPrompt.GetComponent<UnityEngine.UI.Text> ().text = "Please enter a valid list index (0 to start at the beginning)";
			cantGoPrompt.SetActive (true);
			return;
		}
		if (!ushort.TryParse (sessionNumberInput.text, out sessionNumber) || sessionNumber < 0)
		{
			cantGoPrompt.GetComponent<UnityEngine.UI.Text> ().text = "Please enter a valid session index (0 for the first session)";
			cantGoPrompt.SetActive (true);
			return;
		}
		if (EditableExperiment.SessionComplete(sessionNumber, participantNameInput.text))
		{
			cantGoPrompt.GetComponent<UnityEngine.UI.Text> ().text = "That session has already been completed";
			cantGoPrompt.SetActive (true);
			return;
		}

		UnityEPL.AddParticipant(participantNameInput.text);
		UnityEPL.SetSessionNumber (sessionNumber);

		EditableExperiment.ConfigureExperiment((ushort)(listNumber*experimentSettings.wordsPerList), sessionNumber);
		UnityEngine.SceneManagement.SceneManager.LoadScene (FRExperimentSettings.ExperimentNameToExperimentScene(UnityEPL.GetExperimentName()));
	}

	private bool IsValidParticipantName(string name)
	{
		if (name.Length != 6 && name.Length != 4)
			return false;
		bool isValidRAMName = name [0].Equals ('R') && name [1].Equals ('1') && char.IsDigit (name [2]) && char.IsDigit (name [3]) && char.IsDigit (name [4]) && char.IsUpper (name [5]);
		bool isValidSCALPName = char.IsUpper (name [0]) && char.IsUpper (name [1]) && char.IsUpper (name [2]) && char.IsDigit (name [3]) && char.IsDigit (name [4]) && char.IsDigit (name [5]);
		bool isTest = name.Equals ("TEST");
		return isValidRAMName || isValidSCALPName || isTest;
	}
}