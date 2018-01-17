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
        if (on && stateName.Equals("WORD"))
        {
            reporter.DoReport(new Dictionary<string, object>() {{"word", extraData["word"]}});
        }
    }
}
