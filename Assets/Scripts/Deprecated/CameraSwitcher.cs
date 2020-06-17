using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraSwitcher : MonoBehaviour 
{
    public Camera encodingCamera;
    public Camera otherCamera;

    private void OnEnable()
    {
        EditableExperiment.OnStateChange += OnState;
    }

    private void OnDisable()
    {
        EditableExperiment.OnStateChange -= OnState;
    }

    private void OnState(string stateName, bool on, Dictionary<string, object> extraData)
    {
        if (stateName.Equals("WORD") || stateName.Equals("ORIENT"))
            return;
        if (stateName.Equals("ENCODING") && on)
        {
            encodingCamera.enabled = true;
            otherCamera.enabled = false;
        }
        else
        {
            encodingCamera.enabled = false;
            otherCamera.enabled = true;
        }
    }
}
