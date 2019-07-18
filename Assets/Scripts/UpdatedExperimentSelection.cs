using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This is attached to the dropdown menu which selects experiments.
/// 
/// It only needs to call UnityEPL.SetExperimentName().
/// </summary>
public class UpdatedExperimentSelection : MonoBehaviour
{
    public ExperimentManager manager;

    void Awake()
    {
        
        GameObject mgr = GameObject.Find("ExperimentManager");
        manager = (ExperimentManager)mgr.GetComponent("ExperimentManager");

        UnityEngine.UI.Dropdown dropdown = GetComponent<UnityEngine.UI.Dropdown>();

        string[] experiments = manager.getSetting("availableExperiments");

        dropdown.AddOptions(new List<string>(new string[] {"Select Task..."}));
        dropdown.AddOptions(new List<string>(experiments));
        SetExperiment();
    }

    public void SetExperiment()
    {
        UnityEngine.UI.Dropdown dropdown = GetComponent<UnityEngine.UI.Dropdown>();

        if(dropdown.captionText.text != "Select Task...") {
            manager.mainEvents.Do(new EventBase<string>(manager.loadExperimentConfig, 
                dropdown.captionText.text));
        }
    }
}