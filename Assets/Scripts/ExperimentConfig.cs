using System.Collections;
using UnityEngine.JSONSerializeModule;

[Serializable]
public class ExperimentConfig {
    public bool isTest;
    public string launcherScene;
    public bool legacyExperiment;
};