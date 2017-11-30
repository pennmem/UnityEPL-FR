using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveForward : MonoBehaviour
{
    public float speed = 1f;

    void Update()
    {
        gameObject.transform.position = gameObject.transform.position + new Vector3(0, 0, speed * Time.deltaTime);
    }
}
