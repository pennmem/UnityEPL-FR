using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JumpForward : MonoBehaviour
{
	public float unitsDistance = 30f;
	public float secondsInterval = 20f;

	private float lastJump;

	void Start()
	{
		lastJump = Time.time;
	}

	void Update ()
	{
		if (Time.time > lastJump + secondsInterval)
		{
			gameObject.transform.position = gameObject.transform.position + new Vector3 (0, 0, unitsDistance);
			lastJump = Time.time;
		}
	}
}
