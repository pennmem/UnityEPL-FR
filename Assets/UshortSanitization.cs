using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UshortSanitization : MonoBehaviour 
{
	public GameObject sanitizationNotification;

	public void Sanitizite()
	{
		UnityEngine.UI.InputField inputField = GetComponent<UnityEngine.UI.InputField> ();
		ushort input;
		bool parsable = ushort.TryParse (inputField.text, out input);
		if (!parsable) 
		{
			inputField.text = "0";
			sanitizationNotification.SetActive (true);
		}
	}
}