using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This is attacked to the participant selection dropdown.  It is responsible for loading the information about the selected participant.
/// 
/// It also allows users to edit the loaded information with Increase/Decrease Session/List number buttons.
/// </summary>
public class UpdatedParticipantSelection : MonoBehaviour
{
    ExperimentManager manager;
    public UnityEngine.UI.InputField participantNameInput;
    public UnityEngine.UI.Text sessionNumberText;
    public UnityEngine.UI.Text listNumberText;

    public static int nextSessionNumber = 0;
    public static int nextListNumber = 0;
    public static IronPython.Runtime.List nextWords = null;

    // assumes that experiment names are unique
    private string currentExperiment;

    void Awake() {
        GameObject mgr = GameObject.Find("ExperimentManager");
        manager = (ExperimentManager)mgr.GetComponent("ExperimentManager");

        FindParticipants();
    }

    void Update() {

        // update participants when new experiments are loaded
        if((manager.experimentConfig != null) && (currentExperiment != manager.experimentConfig.experimentName)) {
            currentExperiment = manager.experimentConfig.experimentName;
            FindParticipants();
        }
    }

    public void FindParticipants()
    {
        UnityEngine.UI.Dropdown dropdown = GetComponent<UnityEngine.UI.Dropdown>();

        dropdown.ClearOptions();
        dropdown.AddOptions(new List<string>() { "Select participant...", "New participant" });

        if(!(manager.experimentConfig == null)) {
            string participantDirectory = manager.fileManager.experimentPath();
            System.IO.Directory.CreateDirectory(participantDirectory);
            string[] filepaths = System.IO.Directory.GetDirectories(participantDirectory);
            string[] filenames = new string[filepaths.Length];

            for (int i = 0; i < filepaths.Length; i++)
                filenames[i] = System.IO.Path.GetFileName(filepaths[i]);

            dropdown.AddOptions(new List<string>(filenames));
        }
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

        if (!System.IO.Directory.Exists(manager.fileManager.participantPath(selectedParticipant)))
            throw new UnityException("You tried to load a participant that doesn't exist.");

        participantNameInput.text = selectedParticipant;

        nextSessionNumber = 0;
        while (System.IO.File.Exists(manager.fileManager.sessionPath(selectedParticipant, nextSessionNumber)))
        {
            nextSessionNumber++;
        }

        LoadSession(); 
    }

    // TODO: this logic belongs to experiment
    public void LoadSession()
    {
        UnityEngine.UI.Dropdown dropdown = GetComponent<UnityEngine.UI.Dropdown>();
        string selectedParticipant = dropdown.captionText.text;
        string sessionFilePath = manager.fileManager.sessionPath(selectedParticipant, nextSessionNumber);
        if (System.IO.File.Exists(sessionFilePath))
        {
            // TODO: resuming experiment
            Debug.Log("Session already started"); 
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