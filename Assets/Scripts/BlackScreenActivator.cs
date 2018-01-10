using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlackScreenActivator : MonoBehaviour
{
    public GameObject blackScreen;

    void OnEnable()
    {
        EditableExperiment.OnStateChange += OnStateChange;
    }

    void OnDisable()
    {
        EditableExperiment.OnStateChange -= OnStateChange;
    }

    void OnStateChange(string stateName, bool on, Dictionary<string, object> extraData)
    {
        if (stateName.Equals("ENCODING"))
            blackScreen.SetActive(!on);
    }
}
