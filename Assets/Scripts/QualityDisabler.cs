using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QualityDisabler : MonoBehaviour
{
    public UnityEngine.PostProcessing.PostProcessingBehaviour disableMe;

	void Start ()
    {
        Debug.Log("Quality level: " + QualitySettings.GetQualityLevel().ToString());
        if (QualitySettings.GetQualityLevel() == 0)
            disableMe.enabled = false;
	}
}
