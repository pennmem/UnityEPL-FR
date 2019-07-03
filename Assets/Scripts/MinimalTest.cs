using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MinimalTest : MonoBehaviour {

	// Use this for initialization
	IEnumerator Start () {
		while(true) {
			Debug.Log("logging from Start");
			yield return null;
		}
	}
	
	// Update is called once per frame
	void Update () {
		Debug.Log("logging from Update");
	}
}
