using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExperimentSelection : MonoBehaviour
{
    public GameObject activatable;

    void Awake()
    {
        UnityEngine.UI.Dropdown dropdown = GetComponent<UnityEngine.UI.Dropdown>();

        string[] experiments = FRExperimentSettings.GetExperimentNames();

        dropdown.AddOptions(new List<string>(experiments));
        SetExperiment();
    }

    public void SetExperiment()
    {
        UnityEngine.UI.Dropdown dropdown = GetComponent<UnityEngine.UI.Dropdown>();
        UnityEPL.SetExperimentName(dropdown.captionText.text);
        Debug.Log("Now using experiment: " + UnityEPL.GetExperimentName());
        activatable.SetActive(FRExperimentSettings.GetSettingsByName(UnityEPL.GetExperimentName()).useSessionListSelection);
    }
}