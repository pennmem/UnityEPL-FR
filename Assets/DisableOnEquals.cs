using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisableOnEquals : MonoBehaviour
{
	public GameObject disableMe;

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
		disableMe.SetActive (!text.Contains ("="));
	}
}
