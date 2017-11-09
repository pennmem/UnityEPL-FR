using UnityEngine;
using System.Collections;

/// <summary>
/// This struct contains all the settings required to run an FR experiment.
/// 
/// The behavior of the experiment will automatically adjust according to these settings.
/// </summary>
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
    public float minOrientationStimulusLength;
    public float maxOrientationStimulusLength;
    public float minPauseBeforeRecall;
    public float maxPauseBeforeRecall;
    public float recallTextDisplayLength;
    public bool useRamulator;
    public bool isTwoParter;
    public bool isCategoryPool;
}

public static class FRExperimentSettings
{



    /// <summary>
    /// Gets the FR 1 settings.
    /// 
    /// //Short FR1 & CatFR1 Task Design//
    /// Each session includes one practice list and 12 experimental lists
    /// Word lists are constructed from the word pool according to an algorithm that generates unique lists with mean pairwise LSA similarity within each list of approximately 0.2.
    /// Half of the words in the pool will be seen once in Session 1, and the other half will beseen in Session 2.
    /// 
    /// 
    /// The following SESSION DESIGN specifications apply to all FR experiments:
    ///     A session begins by with an introductory video containing the task instructions
    ///     A 10 second countdown video precedes the encoding phase of each list
    ///     Estimated time to complete each session is approximately 25 min
    /// //LIST ENCODING PHASE//
    ///     Each list starts with a orienting stimulus displayed for 1000–1400 ms (uniformly distributed)
    ///     12 words are then presented for 1600 ms each, each word preceded by an inter-stimulus interval of 750-1000ms.  There is no break after the last word has been displayed for 1600ms- distractors immediate appear on the screen.
    /// //DISTRACTOR PHASE//
    ///     Arithmetic problems are presented for at least 20 s
    ///     Problems are presented in the form A+B+C = ?, where A/B/C are random integers 1–9
    ///     All keystrokes are recorded until the subject presses enter
    ///     After each arithmetic problem, if the 20 s time limit has been exceeded, a tone is presented indicating the end of the distractor phase and the beginning of the retrieval phase.
    ///     There is then a 1000-1400ms pause before recall.
    /// //RECALL PHASE//
    ///     Audio is recorded via the microphone for 30s
    ///     After 30 seconds a tone is presented, indicating the end of the retrieval phase and the end of that list.
    /// 
    /// //Free Recall Word Pool and List Creation//
    ///     The word pool is adapted from the word pool used in the pyFR task, which has been run in a set of over 150 intracranial patients (Burke et al., 2013; Long et al., 2014).
    ///     A set of 288 words was chosen from the pyFR word pool, based on the recall performance of a separate set of participants who completed a large-scale study of free recall.
    ///     Recall performance in this large-scale task was modeled to estimate the effect of each individual word on recall, removing influences of serial position and frequency, concreteness, imageability and length.
    ///     Estimates for the words were used to identify the words at the top and bottom of the distribution for removal.
    /// </summary>
    /// <returns>The FR 1 settings.</returns>
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
        FR1Settings.minOrientationStimulusLength = 1f;
        FR1Settings.maxOrientationStimulusLength = 1.4f;
        FR1Settings.minPauseBeforeRecall = 1f;
        FR1Settings.maxPauseBeforeRecall = 1.4f;
        FR1Settings.recallTextDisplayLength = 0.5f;
        FR1Settings.useRamulator = true;
        FR1Settings.isTwoParter = true;
        return FR1Settings;
    }

    /// <summary>
    /// Gets the cat FR 1 settings.
    /// 
    /// The following information applies to all CatFR experiments:
    /// //Categorized Free Recall Word Pool and List Creation//
    ///     The word pool was generated via a series of pilot studies conducted on Amazon Mechanical Turk. 
    ///     First, a set of 40 participants generated lists of exemplars from 28 categories.
    ///     The resulting exemplars were sorted according to the number of participants that produced that specific exemplar.
    ///     The top 25 exemplars from each category were then selected for the second pilot study (# of participants that produced each exemplar in the top 25 set, M = 19; range = 7-40).
    ///     In the second pilot study, a set of 45 participants rated (on a 1-7 scale) each of the 25 items from the 28 categories in terms of how ‘typical’ the item is of all members of the category.
    ///     From these ratings, the 12 most prototypical items from each category were selected. The three categories with the lowest mean prototypicality across the 12 exemplars were discarded, leaving 25 categories for inclusion in the study.
    ///     This process ensured that the exemplars that were chosen for the word pool were drawn only from highly prototypical members of each category (mean prototypicality rating across the final set = 6.3).
    ///     The 25 lists for the first session were constructed by randomly selecting three categories from the set and then randomly selecting four exemplars from each of those categories.
    ///     Category members were presented in pairs, with the order determined such that two pairs of items from the same category were never presented consecutively.
    ///     Lists will also have the constraint that half of the words from each category will be presented in the first half of the list, and the other half of the words will be presented in the second half of the list.
    /// </summary>
    /// <returns>The cat FR 1 settings.</returns>
    public static ExperimentSettings GetCatFR1Settings()
	{
        ExperimentSettings CatFR1Settings = GetFR1Settings();
        CatFR1Settings.experimentName = "CatFR1";
        CatFR1Settings.isCategoryPool = true;
        return CatFR1Settings;
    }

    /// <summary>
    /// Gets the FR 6 settings.
    /// 
    /// FR6 is a stim experiment.  Lists are handled as in FR1.  No stimulation is applied in list 0 (practice list) or lists 1-3 (baseline lists)/
    /// 
    /// The remaining 22 lists are randomly interleaved: Five stimming on one channel, five stimming on another, six stimming on both channels, and six with no stim.
    /// </summary>
    /// <returns>The FR 6 settings.</returns>
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
        FR6Settings.minOrientationStimulusLength = 1f;
        FR6Settings.maxOrientationStimulusLength = 1.4f;
        FR6Settings.minPauseBeforeRecall = 1f;
        FR6Settings.maxPauseBeforeRecall = 1.4f;
        FR6Settings.recallTextDisplayLength = 0.5f;
        FR6Settings.useRamulator = true;
        FR6Settings.isTwoParter = false;
        return FR6Settings;
    }

    /// <summary>
    /// Gets the CatFR 6 settings.
    /// 
    /// CatFR6 is identical to FR6, but uses the category word pool described above.
    /// </summary>
    /// <returns>The CatFR 6 settings.</returns>
    public static ExperimentSettings GetCatFR6Settings()
    {
        ExperimentSettings CatFR6Settings = GetFR6Settings();
        CatFR6Settings.experimentName = "CatFR6";
        CatFR6Settings.isCategoryPool = true;
        return CatFR6Settings;
    }

    /// <summary>
    /// Gets the test FR 1 settings.
    /// 
    /// This is used for rapid visual confirmation of experiment behavior.
    /// </summary>
    /// <returns>The test FR 1 settings.</returns>
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

    /// <summary>
    /// Gets the test cat FR 1 settings.
    /// 
    /// This is used for rapid visual confirmation of experiment behavior.
    /// </summary>
    /// <returns>The test cat FR 1 settings.</returns>
    public static ExperimentSettings GetTestCatFR1Settings()
    {
        ExperimentSettings testCatFR1Settings = GetTestFR1Settings();
        testCatFR1Settings.experimentName = "CatFR1_test";
        testCatFR1Settings.isCategoryPool = true;
        return testCatFR1Settings;
    }

    /// <summary>
    /// Gets the test FR 6 settings.
    /// 
    /// This is used for rapid visual confirmation of experiment behavior.
    /// </summary>
    /// <returns>The test FR 6 settings.</returns>
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

    /// <summary>
    /// Gets the test cat FR 6 settings.
    /// 
    /// This is used for rapid visual confirmation of experiment behavior.
    /// </summary>
    /// <returns>The test cat FR 6 settings.</returns>
    public static ExperimentSettings GetTestCatFR6Settings()
    {
        ExperimentSettings testCatFR6Settings = GetTestFR6Settings();
        testCatFR6Settings.experimentName = "CatFR6_test";
        testCatFR6Settings.isCategoryPool = true;
        return testCatFR6Settings;
    }


    /// <summary>
    /// Gets the experiment names.
    /// </summary>
    /// <returns>The experiment names as an array of strings.  These array elements will be presented to the user as the available selection of experiments.</returns>
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

    /// <summary>
    /// Takes a string name of an experiment.
    /// </summary>
    /// <returns>The ExperimentSettings associated with that name.</returns>
    /// <param name="name">Name.</param>
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

    /// <summary>
    /// Takes the name of an experiment.
    /// </summary>
    /// <returns>The name of the unity scene used to run that experiment.</returns>
    /// <param name="name">Name.</param>
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
}