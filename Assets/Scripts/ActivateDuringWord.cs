using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActivateDuringWord : MonoBehaviour
{
    public GameObject activateMe;

    private void OnEnable()
    {
        EditableExperiment.OnStateChange += OnWord;
    }

    private void OnDisable()
    {
        EditableExperiment.OnStateChange -= OnWord;
    }

    private void OnWord (string stateName, bool on, Dictionary<string, object> extraData)
    {
        if (stateName.Equals("WORD") || stateName.Equals("ORIENT"))
        {
            activateMe.SetActive(on);
        }
    }
}
