using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExperimentSelection : MonoBehaviour
{
	public UnityEngine.UI.InputField participantNameInput;
	public UnityEngine.UI.InputField wordsSeenInput;

	void Start ()
	{
		UnityEngine.UI.Dropdown dropdown = GetComponent<UnityEngine.UI.Dropdown> ();

		string[] experiments;

		dropdown.AddOptions (new List<string>(experiments));
	}

	public void SetExperiment()
	{
		UnityEngine.UI.Dropdown dropdown = GetComponent<UnityEngine.UI.Dropdown> ();
		UnityEPL.SetExperimentName (dropdown.captionText.text);
		Debug.Log ("Now using experiment: " + UnityEPL.GetExperimentName ());
	}
}