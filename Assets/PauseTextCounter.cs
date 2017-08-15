using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseTextCounter : MonoBehaviour
{
	private float startTime;

	void OnEnable()
	{
		startTime = Time.time;
	}

	void Update () 
	{
		float aliveTime = Time.time - startTime;

		GetComponent<UnityEngine.UI.Text> ().text = "Paused (" + Mathf.FloorToInt (aliveTime) + ")";
	}
}
