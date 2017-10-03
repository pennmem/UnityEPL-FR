using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlyText : MonoBehaviour
{
	public UnityEngine.UI.Text textElement;

	public float startZ = 3;
	public float staticZ = -4.5f;
	public float hoverTime = 1.6f;

	private bool startCaring = false;

	void OnEnable()
	{
		TextDisplayer.OnText += OnText;
	}

	void OnDisable()
	{
		TextDisplayer.OnText -= OnText;
	}
		
	private void ConstantState(bool active)
	{
		//Debug.Log ("ConstantState");
		textElement.color = new Color (0, 0, 0, 255);
		gameObject.transform.localPosition = new Vector3 (gameObject.transform.localPosition.x, gameObject.transform.localPosition.y, staticZ);
		gameObject.SetActive(active);
	}

	public void OnText(string text)
	{
		//Debug.Log ("OnText:" + text);
		for (int i = 0; i < text.Length; i++)
		{
			if (char.IsDigit (text, i))
			{
				startCaring = true;
				ConstantState (true);
				return;
			}
		}
		if (!text.Equals("") && startCaring)
			StartCoroutine (DoFly ());
	}

	private IEnumerator DoFly()
	{
		//Debug.Log ("DoFly");
		float startTime = Time.time;
		gameObject.transform.localPosition = new Vector3 (gameObject.transform.localPosition.x, gameObject.transform.localPosition.y, startZ);
		Vector3 hoverPosition = gameObject.transform.position;
		while (Time.time < startTime + hoverTime)
		{
			gameObject.transform.position = hoverPosition;
			yield return null;
		}
	}
}