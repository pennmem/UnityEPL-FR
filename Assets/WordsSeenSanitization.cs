using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WordsSeenSanitization : MonoBehaviour 
{
	public GameObject sanitizationNotification;

	public void Sanitizite()
	{
		UnityEngine.UI.InputField inputField = GetComponent<UnityEngine.UI.InputField> ();
		int input;
		bool parsable = int.TryParse (inputField.text, out input);
		if (!parsable || input < 0) 
		{
			inputField.text = "0";
			sanitizationNotification.SetActive (true);
		}
	}
}