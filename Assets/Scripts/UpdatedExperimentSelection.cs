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
    public GameObject activatable;

    void Awake()
    {
        
        GameObject mgr = GameObject.Find("ExperimentManager");
        manager = (ExperimentManager)mgr.GetComponent("ExperimentManager");

        UnityEngine.UI.Dropdown dropdown = GetComponent<UnityEngine.UI.Dropdown>();

        string[] experiments = manager.systemConfig.availableExperiments;

        dropdown.AddOptions(new List<string>(experiments));
        SetExperiment();
    }

    public void SetExperiment()
    {
        UnityEngine.UI.Dropdown dropdown = GetComponent<UnityEngine.UI.Dropdown>();

        manager.mainEvents.Do(new EventBase<string>(manager.loadExperimentConfig, dropdown.captionText.text));
        
        activatable.SetActive(true);
    }
}