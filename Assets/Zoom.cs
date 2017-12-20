using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Zoom : MonoBehaviour
{
    public float speed;

	void Update ()
    {
        gameObject.transform.position = gameObject.transform.position + new Vector3(Input.GetAxis("Left/Right") * Time.deltaTime * speed,
                                                                                    Input.GetAxis("Up/Down") * Time.deltaTime * speed,
                                                                                    Input.GetAxis("In/Out") * Time.deltaTime * speed);
	}
}