using System;
using System.Collections;
using System.Collections.Generic;

[Serializable]
public class ExperimentConfig {
    public bool isTest;
    public string launcherScene;
    public bool legacyExperiment;
    public Dictionary<string, string> experimentScenes;
};