using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TextDisplayer : MonoBehaviour 
{
	public delegate void TextDisplayed(string text);
	public static TextDisplayed OnText;

	public ScriptedEventReporter wordEventReporter;
	public UnityEngine.UI.Text[] textElements;

	public void DisplayText(string description, string text)
	{
		if (OnText != null)
			OnText (text);
		foreach (UnityEngine.UI.Text textElement in textElements)
		{
			textElement.text = text;
		}
		Dictionary<string, string> dataDict = new Dictionary<string, string> ();
		dataDict.Add ("displayed text", text);
		wordEventReporter.ReportScriptedEvent (description, dataDict, 1);
	}

	public void ClearText()
	{
		foreach (UnityEngine.UI.Text textElement in textElements)
		{
			textElement.text = "";
		}
		wordEventReporter.ReportScriptedEvent ("text display cleared", new Dictionary<string, string> (), 1);
	}

	public void ChangeColor(Color newColor)
	{
		foreach (UnityEngine.UI.Text textElement in textElements)
		{
			textElement.color = newColor;
		}
	}

	public string CurrentText()
	{
		if (textElements.Length == 0)
			throw new UnityException ("There aren't any text elements assigned to this TextDisplayer.");
		return textElements[0].text;
	}
}