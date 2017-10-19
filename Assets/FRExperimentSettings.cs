using UnityEngine;
using System.Collections;

public static class FRExperimentSettings
{
	public static string[] GetExperimentNames()
	{
		return new string[] { GetFR1Settings ().experimentName,
							  GetFR6Settings ().experimentName,
							  GetTestFR1Settings().experimentName,
							  GetTestFR6Settings().experimentName,
							  "FR1_scalp",
							  "SFR"};
	}
		
	public static ExperimentSettings GetSettingsByName(string name)
	{
		switch (name)
		{
			case "FR1":
				return GetFR1Settings ();
			case "FR6":
				return GetFR6Settings ();
			case "FR1_test":
				return GetTestFR1Settings ();
			case "FR6_test":
				return GetTestFR6Settings ();
			case "FR1_scalp":
				return GetFR1Settings ();
			case "SFR":
				return GetFR1Settings ();

		}
		throw new UnityException ("No settings found with that name.");
	}

	public static string ExperimentNameToExperimentScene(string name)
	{
		switch (name)
		{
			case "FR1":
				return "ram_fr";
			case "FR6":
				return "ram_fr";
			case "FR1_test":
				return "ram_fr";
			case "FR6_test":
				return "ram_fr";
			case "FR1_scalp":
				return "scalp_fr";
			case "SFR":
				return "spatial_fr";
		}
		throw new UnityException ("That name was not recognized.");
	}

	public static ExperimentSettings GetFR1Settings()
	{
		ExperimentSettings FR1Settings = new ExperimentSettings();
		FR1Settings.experimentName = "FR1";
		FR1Settings.version = "1.0";
		FR1Settings.wordListGenerator = new FR1ListGenerator();
		FR1Settings.numberOfLists = 26;
		FR1Settings.wordsPerList = 12;
		FR1Settings.countdownLength = 10;
		FR1Settings.countdownTick = 1f;
		FR1Settings.wordPresentationLength = 1.6f;
		FR1Settings.minISI = 0.75f;
		FR1Settings.maxISI = 1f;
		FR1Settings.distractionLength = 20f;
		FR1Settings.answerConfirmationTime = 0f;
		FR1Settings.recallLength = 30f;
		FR1Settings.displayLearningMessageIndex = -1;
		FR1Settings.microphoneTestLength = 5;
		return FR1Settings;
	}

	public static ExperimentSettings GetFR6Settings()
	{
		ExperimentSettings FR6Settings = new ExperimentSettings();
		FR6Settings.experimentName = "FR6";
		FR6Settings.version = "6.0";
		FR6Settings.wordListGenerator = new FR6ListGenerator();
		FR6Settings.numberOfLists = 42;
		FR6Settings.wordsPerList = 12;
		FR6Settings.countdownLength = 10;
		FR6Settings.countdownTick = 1f;
		FR6Settings.wordPresentationLength = 1.6f;
		FR6Settings.minISI = 0.75f;
		FR6Settings.maxISI = 1f;
		FR6Settings.distractionLength = 20f;
		FR6Settings.answerConfirmationTime = 0f;
		FR6Settings.recallLength = 30f;
		FR6Settings.displayLearningMessageIndex = 26;
		FR6Settings.microphoneTestLength = 5;
		return FR6Settings;
	}

	public static ExperimentSettings GetTestFR1Settings()
	{
		ExperimentSettings testFR1Settings = GetFR1Settings();
		testFR1Settings.experimentName = "FR1_test";
		testFR1Settings.countdownTick = 0.01f;
		testFR1Settings.wordPresentationLength = 0.01f;
		testFR1Settings.minISI = 0.005f;
		testFR1Settings.maxISI = 0.01f;
		testFR1Settings.distractionLength = 0.1f;
		testFR1Settings.recallLength = 0.1f;
		return testFR1Settings;
	}

	public static ExperimentSettings GetTestFR6Settings()
	{
		ExperimentSettings testFR6Settings = GetFR6Settings();
		testFR6Settings.experimentName = "FR6_test";
		testFR6Settings.countdownTick = 0.01f;
		testFR6Settings.wordPresentationLength = 0.01f;
		testFR6Settings.minISI = 0.005f;
		testFR6Settings.maxISI = 0.01f;
		testFR6Settings.distractionLength = 0.1f;
		testFR6Settings.recallLength = 0.1f;
		return testFR6Settings;
	}
}