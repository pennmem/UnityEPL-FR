using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RouletteTextDisplayer : TextDisplayer
{
	public UnityEngine.Collider[] viewZones;
	public GameObject viewer;
	public UnityEngine.UI.Text alwaysDisplay;

	public virtual void DisplayText(string description, string text)
	{
		if (OnText != null)
			OnText (text);
		alwaysDisplay.text = text;
		for (int i = 0; i < viewZones.Length; i++)
		{
			UnityEngine.Collider viewZone = viewZones [i];
			UnityEngine.UI.Text textElement = textElements [i];
			if (viewZone.bounds.Contains (viewer.transform.position))
			{
				Debug.Log (viewZone.gameObject.name);
				textElement.text = text;
			}
		}
		Dictionary<string, string> dataDict = new Dictionary<string, string> ();
		dataDict.Add ("displayed text", text);
		wordEventReporter.ReportScriptedEvent (description, dataDict, 1);
	}
}