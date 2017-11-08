using UnityEngine;
using System.Collections;

public struct ExperimentSettings
{
    public WordListGenerator wordListGenerator;
    public string experimentName;
    public string version;
    public int numberOfLists;
    public int wordsPerList;
    public int countdownLength;
    public int microphoneTestLength;
    public float countdownTick;
    public float wordPresentationLength;
    public float minISI;
    public float maxISI;
    public float distractionLength;
    public float answerConfirmationTime;
    public float recallLength;
    public float minPauseBeforeWords;
    public float maxPauseBeforeWords;
    public float minPauseBeforeRecall;
    public float maxPauseBeforeRecall;
    public float recallTextDisplayLength;
    public bool useRamulator;
    public bool isTwoParter;
    public bool isCategoryPool;
}

public static class FRExperimentSettings
{
    public static string[] GetExperimentNames()
    {
        return new string[] {
                                GetFR1Settings ().experimentName,
                                GetCatFR1Settings ().experimentName,
                                GetFR6Settings ().experimentName,
                                GetCatFR6Settings ().experimentName,
                                //GetTestFR1Settings().experimentName,
                                GetTestFR6Settings().experimentName,
                                GetTestCatFR6Settings().experimentName,
                                //"FR1_scalp",
                                "SFR",
                            };
    }

    public static ExperimentSettings GetSettingsByName(string name)
    {
        switch (name)
        {
            case "FR1":
                return GetFR1Settings();
            case "CatFR1":
                return GetCatFR1Settings();
            case "FR6":
                return GetFR6Settings();
            case "CatFR6":
                return GetCatFR6Settings();
            case "FR1_test":
                return GetTestFR1Settings();
            case "FR6_test":
                return GetTestFR6Settings();
            case "CatFR6_test":
                return GetTestCatFR6Settings();
            case "FR1_scalp":
                return GetFR1Settings();
            case "SFR":
                return GetFR1Settings();
        }
        throw new UnityException("No settings found with that name.");
    }

    public static string ExperimentNameToExperimentScene(string name)
    {
        switch (name)
        {
            case "FR1":
                return "ram_fr";
            case "CatFR1":
                return "ram_fr";
            case "FR6":
                return "ram_fr";
            case "CatFR6":
                return "ram_fr";
            case "FR1_test":
                return "ram_fr";
            case "FR6_test":
                return "ram_fr";
            case "CatFR6_test":
                return "ram_fr";
            case "SFR":
                return "sfr";
        }
        throw new UnityException("That name was not recognized.");
    }

    public static ExperimentSettings GetFR1Settings()
    {
        ExperimentSettings FR1Settings = new ExperimentSettings();
        FR1Settings.experimentName = "FR1";
        FR1Settings.version = "1.0";
        FR1Settings.wordListGenerator = new FRListGenerator(0, 12, 0, 0, 0, 0);
        FR1Settings.isCategoryPool = false;
        FR1Settings.numberOfLists = 13;
        FR1Settings.wordsPerList = 12;
        FR1Settings.countdownLength = 10;
        FR1Settings.countdownTick = 1f;
        FR1Settings.wordPresentationLength = 1.6f;
        FR1Settings.minISI = 0.75f;
        FR1Settings.maxISI = 1f;
        FR1Settings.distractionLength = 20f;
        FR1Settings.answerConfirmationTime = 0f;
        FR1Settings.recallLength = 30f;
        FR1Settings.microphoneTestLength = 5;
        FR1Settings.minPauseBeforeWords = 1f;
        FR1Settings.maxPauseBeforeWords = 1.4f;
        FR1Settings.minPauseBeforeRecall = 0.5f;
        FR1Settings.maxPauseBeforeRecall = 0.7f;
        FR1Settings.recallTextDisplayLength = 0.5f;
        FR1Settings.useRamulator = true;
        FR1Settings.isTwoParter = true;
        return FR1Settings;
    }

    public static ExperimentSettings GetCatFR1Settings() ///note that currently only 12 wordsPerList is supported by CatFR
	{
        ExperimentSettings CatFR1Settings = GetFR1Settings();
        CatFR1Settings.experimentName = "CatFR1";
        CatFR1Settings.isCategoryPool = true;
        return CatFR1Settings;
    }

    public static ExperimentSettings GetFR6Settings()
    {
        ExperimentSettings FR6Settings = new ExperimentSettings();
        FR6Settings.experimentName = "FR6";
        FR6Settings.version = "6.0";
        FR6Settings.wordListGenerator = new FRListGenerator(16, 6, 3, 5, 5, 6);
        FR6Settings.isCategoryPool = false;
        FR6Settings.numberOfLists = 26;
        FR6Settings.wordsPerList = 12;
        FR6Settings.countdownLength = 10;
        FR6Settings.countdownTick = 1f;
        FR6Settings.wordPresentationLength = 1.6f;
        FR6Settings.minISI = 0.75f;
        FR6Settings.maxISI = 1f;
        FR6Settings.distractionLength = 20f;
        FR6Settings.answerConfirmationTime = 0f;
        FR6Settings.recallLength = 30f;
        FR6Settings.microphoneTestLength = 5;
        FR6Settings.minPauseBeforeWords = 1f;
        FR6Settings.maxPauseBeforeWords = 1.4f;
        FR6Settings.minPauseBeforeRecall = 0.5f;
        FR6Settings.maxPauseBeforeRecall = 0.7f;
        FR6Settings.recallTextDisplayLength = 0.5f;
        FR6Settings.useRamulator = true;
        FR6Settings.isTwoParter = false;
        return FR6Settings;
    }

    public static ExperimentSettings GetCatFR6Settings()
    {
        ExperimentSettings CatFR6Settings = GetFR6Settings();
        CatFR6Settings.experimentName = "CatFR6";
        CatFR6Settings.isCategoryPool = true;
        return CatFR6Settings;
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
        testFR1Settings.useRamulator = false;
        return testFR1Settings;
    }

    public static ExperimentSettings GetTestCatFR1Settings()
    {
        ExperimentSettings testCatFR1Settings = GetTestFR1Settings();
        testCatFR1Settings.experimentName = "CatFR1_test";
        testCatFR1Settings.isCategoryPool = true;
        return testCatFR1Settings;
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
        testFR6Settings.useRamulator = false;
        return testFR6Settings;
    }

    public static ExperimentSettings GetTestCatFR6Settings()
    {
        ExperimentSettings testCatFR6Settings = GetTestFR6Settings();
        testCatFR6Settings.experimentName = "CatFR6_test";
        testCatFR6Settings.isCategoryPool = true;
        return testCatFR6Settings;
    }
}