using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This is attached to the text which displays when the experiment is paused.  It increments a counter to
/// indicate for how long the experiment has been paused.
/// 
/// (Pausing is not currently desired in any of the experiments.)
/// </summary>
public class PauseTextCounter : MonoBehaviour
{
    private float startTime;

    void OnEnable()
    {
        startTime = Time.time;
    }

    void Update()
    {
        float aliveTime = Time.time - startTime;

        GetComponent<UnityEngine.UI.Text>().text = "Paused (" + Mathf.FloorToInt(aliveTime) + ")";
    }
}