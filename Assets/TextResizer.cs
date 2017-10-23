using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TextResizer : MonoBehaviour
{
	public UnityEngine.UI.Text textElement;

	private Vector2 originalAnchorMin;
	private Vector2 originalAnchorMax;

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
		{
			textElement.resizeTextMaxSize = 80;
			textElement.rectTransform.anchorMin = new Vector2 (0, 0);
			textElement.rectTransform.anchorMax = new Vector2 (1, 1);
		}
		else
		{
			textElement.resizeTextMaxSize = 300;
			textElement.rectTransform.anchorMin = originalAnchorMin;
			textElement.rectTransform.anchorMax = originalAnchorMax;
		}
	}

	void Start ()
	{
		originalAnchorMin = textElement.rectTransform.anchorMin;
		originalAnchorMax = textElement.rectTransform.anchorMax;
	}

	void Update ()
	{
		
	}
}
