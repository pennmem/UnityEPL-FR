using UnityEngine;
using System.Collections;

public static class FRExperimentSettings
{
	public static string[] Experiment

	public static ExperimentSettings GetFR1Settings()
	{
		ExperimentSettings FR1Settings = new ExperimentSettings();
		FR1Settings.experimentName = "FR1";
		FR1Settings.wordListGenerator = new FR1ListGenerator();
		FR1Settings.numberOfLists = 25;
		FR1Settings.wordsPerList = 12;
		FR1Settings.countdownLength = 10;
		FR1Settings.countdownTick = 1f;
		FR1Settings.wordPresentationLength = 1.6f;
		FR1Settings.minISI = 0.75f;
		FR1Settings.maxISI = 1f;
		FR1Settings.distractionLength = 20f;
		FR1Settings.answerConfirmationTime = 1f;
		FR1Settings.recallLength = 30f;
		return FR1Settings;
	}
}