using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TransparentOnEquals : MonoBehaviour 
{
	public UnityEngine.UI.Image imageElement;

	private Color originalColor;

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
		if (text.Contains("="))
		{
			imageElement.color = Color.clear;
		}
		else
		{
			imageElement.color = originalColor;
		}
	}

	void Start ()
	{
		originalColor = imageElement.color;
	}

	void Update ()
	{

	}
}
