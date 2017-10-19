using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TextResizer : MonoBehaviour
{
	public UnityEngine.UI.Text textElement;

	void OnEnable()
	{
		TextDisplayer.OnText += OnText;
	}

	void OnDisable()
	{
		TextDisplayer.OnText -= OnText;
	}

	void OnText(string text)
	{
		if (text.Length > 0 && text [text.Length - 1].Equals ('.'))
			textElement.resizeTextMaxSize = 80;
		else
			textElement.resizeTextMaxSize = 120;
	}

	void Start ()
	{
		
	}

	void Update ()
	{
		
	}
}
