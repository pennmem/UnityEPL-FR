using UnityEngine;
using System.Collections;

public static class FRExperimentSettings
{
	public static string[] GetExperimentNames()
	{
		return new string[] { GetFR1Settings ().experimentName,
							  GetFR6Settings ().experimentName};
	}
		
	public static ExperimentSettings GetSettingsByName(string name)
	{
		switch (name) 
		{
			case "FR1":
				return GetFR1Settings ();
			case "FR6":
				return GetFR6Settings ();
		}
		throw new UnityException ("No settings found with that name");
		return GetFR1Settings();
	}

	public static ExperimentSettings GetFR1Settings()
	{
		ExperimentSettings FR1Settings = new ExperimentSettings();
		FR1Settings.experimentName = "FR1";
		FR1Settings.wordListGenerator = new FR1ListGenerator();
		FR1Settings.numberOfLists = 2;
		FR1Settings.wordsPerList = 2;
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

	public static ExperimentSettings GetFR6Settings()
	{
		ExperimentSettings FR1Settings = new ExperimentSettings();
		FR1Settings.experimentName = "FR6";
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