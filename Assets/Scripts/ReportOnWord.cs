using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReportOnWord : MonoBehaviour
{
    public WorldDataReporter reporter;

    private void OnEnable()
    {
        EditableExperiment.OnStateChange += OnWord;
    }

    private void OnDisable()
    {
        EditableExperiment.OnStateChange -= OnWord;
    }

    private void OnWord(string stateName, bool on, Dictionary<string, object> extraData)
    {
        if (stateName.Equals("WORD") || stateName.Equals("ORIENT"))
        {
            reporter.DoReport();
        }
    }
}
