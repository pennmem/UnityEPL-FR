using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// This is attached to the dropdown menu which selects experiments.
/// 
/// It only needs to call UnityEPL.SetExperimentName().
/// </summary>
public class ExperimentSelection : MonoBehaviour
{
    public InterfaceManager manager;

    void Awake()
    {
        GameObject mgr = GameObject.Find("InterfaceManager");
        manager = (InterfaceManager)mgr.GetComponent("InterfaceManager");

        UnityEngine.UI.Dropdown dropdown = GetComponent<UnityEngine.UI.Dropdown>();

        string[] experiments = Config.availableExperiments;

        dropdown.AddOptions(new List<string>(new string[] {"Select Task..."}));
        dropdown.AddOptions(Config.availableExperiments.ToList());
        SetExperiment();
    }

    public void SetExperiment()
    {
        UnityEngine.UI.Dropdown dropdown = GetComponent<UnityEngine.UI.Dropdown>();

        if(dropdown.captionText.text != "Select Task...") {
            Debug.Log("Task chosen");
            manager.Do(new EventBase<string>(manager.LoadExperimentConfig, 
                dropdown.captionText.text));
        }
    }
}