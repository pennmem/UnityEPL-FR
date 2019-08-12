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
    public InterfaceManager manager;

    void Awake()
    {
        
        GameObject mgr = GameObject.Find("InterfaceManager");
        manager = (InterfaceManager)mgr.GetComponent("InterfaceManager");

        UnityEngine.UI.Dropdown dropdown = GetComponent<UnityEngine.UI.Dropdown>();

        string[] experiments = manager.GetSetting("availableExperiments").ToObject<string[]>();

        dropdown.AddOptions(new List<string>(new string[] {"Select Task..."}));
        dropdown.AddOptions(new List<string>(experiments));
        SetExperiment();
    }

    public void SetExperiment()
    {
        UnityEngine.UI.Dropdown dropdown = GetComponent<UnityEngine.UI.Dropdown>();

        if(dropdown.captionText.text != "Select Task...") {
            manager.Do(new EventBase<string>(manager.LoadExperimentConfig, 
                dropdown.captionText.text));
        }
    }
}