using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticipantSelection : MonoBehaviour
{
    public UnityEngine.UI.InputField participantNameInput;
    public UnityEngine.UI.Text sessionNumberText;
    public UnityEngine.UI.Text listNumberText;

    public static int nextSessionNumber = 0;
    public static int nextListNumber = 0;
    public static IronPython.Runtime.List nextWords = null;

    void Start()
    {
        FindParticipants();
    }

    public void FindParticipants()
    {
        UnityEngine.UI.Dropdown dropdown = GetComponent<UnityEngine.UI.Dropdown>();

        dropdown.ClearOptions();
        dropdown.AddOptions(new List<string>() { "Select participant...", "New participant" });

        string participantDirectory = EditableExperiment.CurrentExperimentFolderPath();
        System.IO.Directory.CreateDirectory(participantDirectory);
        string[] filepaths = System.IO.Directory.GetDirectories(participantDirectory);
        string[] filenames = new string[filepaths.Length];

        for (int i = 0; i < filepaths.Length; i++)
            filenames[i] = System.IO.Path.GetFileName(filepaths[i]);

        dropdown.AddOptions(new List<string>(filenames));
        dropdown.value = 0;
        dropdown.RefreshShownValue();

        nextSessionNumber = 0;
        nextListNumber = 0;
        nextWords = null;
        UpdateTexts();
    }

    public void ParticipantSelected()
    {
        UnityEngine.UI.Dropdown dropdown = GetComponent<UnityEngine.UI.Dropdown>();
        if (dropdown.value <= 1)
        {
            participantNameInput.text = "New participant";
        }
        else
        {
            LoadParticipant();
        }
    }

    public void LoadParticipant()
    {
        UnityEngine.UI.Dropdown dropdown = GetComponent<UnityEngine.UI.Dropdown>();
        string selectedParticipant = dropdown.captionText.text;

        if (!System.IO.Directory.Exists(EditableExperiment.ParticipantFolderPath(selectedParticipant)))
            throw new UnityException("You tried to load a participant that doesn't exist.");

        participantNameInput.text = selectedParticipant;

        nextSessionNumber = 0;
        while (System.IO.File.Exists(EditableExperiment.SessionFilePath(nextSessionNumber, selectedParticipant)))
        {
            nextSessionNumber++;
        }

        LoadSession(); 
    }

    public void LoadSession()
    {
        UnityEngine.UI.Dropdown dropdown = GetComponent<UnityEngine.UI.Dropdown>();
        string selectedParticipant = dropdown.captionText.text;
        string sessionFilePath = EditableExperiment.SessionFilePath(nextSessionNumber, selectedParticipant);
        if (System.IO.File.Exists(sessionFilePath))
        {
            string[] loadedState = System.IO.File.ReadAllLines(sessionFilePath);

            //loadListPosition
            nextListNumber = (int.Parse(loadedState[1]) / 12);

            //load words
            ExperimentSettings currentSettings = FRExperimentSettings.GetSettingsByName(UnityEPL.GetExperimentName());
            int wordCount = int.Parse(loadedState[2]);
            if (currentSettings.numberOfLists * currentSettings.wordsPerList != wordCount)
                throw new UnityException("Mismatch between saved word list and experiment settings.");
            int wordStartLine = 3;
            IronPython.Runtime.List words = new IronPython.Runtime.List();
            for (int i = wordStartLine; i < currentSettings.numberOfLists * currentSettings.wordsPerList + wordStartLine; i++)
            {
                string wordString = loadedState[i];

                IronPython.Runtime.PythonDictionary word = new IronPython.Runtime.PythonDictionary();
                string[] keyValues = wordString.Split(';');
                foreach (string keyValue in keyValues)
                {
                    if (keyValue.Equals(""))
                        continue;
                    string[] keyValuePair = keyValue.Split(':');
                    word[keyValuePair[0]] = keyValuePair[1];
                }

                words.Add(word);
            }
            nextWords = words;
        }
        else //start from the beginning if it doesn't exist yet
        {
            nextListNumber = 0;
            nextWords = null;
        }
        UpdateTexts();
    }

    public void DecreaseListNumber()
    {
        if (nextListNumber > 0)
            nextListNumber--;
        UpdateTexts();
    }

    public void IncreaseListNumber()
    {
        if (nextListNumber < FRExperimentSettings.GetSettingsByName(UnityEPL.GetExperimentName()).numberOfLists - 1)
            nextListNumber++;
        UpdateTexts();
    }

    public void DecreaseSessionNumber()
    {
        if (nextSessionNumber > 0)
            nextSessionNumber--;
        LoadSession();
    }

    public void IncreaseSessionNumber()
    {
        nextSessionNumber++;
        LoadSession();
    }

    public void UpdateTexts()
    {
        listNumberText.text = nextListNumber.ToString();
        sessionNumberText.text = nextSessionNumber.ToString();
    }
}