using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReturnHome : MonoBehaviour
{
	public float zThreshold = 400f;

	private Vector3 home;

	void Start()
	{
		home = transform.position;
	}
		
	public void MayReturnHome()
	{
		if (transform.position.z > zThreshold)
			transform.position = home;
	}
}
