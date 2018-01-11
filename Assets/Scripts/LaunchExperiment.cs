using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This handles the button which launches the experiment.
/// 
/// DoLaunchExperiment is responsible for calling EditableExperiment.ConfigureExperiment with the proper parameters.
/// </summary>
public class LaunchExperiment : MonoBehaviour
{
    public GameObject cantGoPrompt;
    public UnityEngine.UI.InputField participantNameInput;
    public UnityEngine.GameObject launchButton;
    public UnityEngine.GameObject greyedLaunchButton;

    void Update()
    {
        launchButton.SetActive(IsValidParticipantName(participantNameInput.text));
        greyedLaunchButton.SetActive(!launchButton.activeSelf);

        if (IsValidParticipantName(participantNameInput.text))
        {
            int sessionNumber = ParticipantSelection.nextSessionNumber;
            launchButton.GetComponentInChildren<UnityEngine.UI.Text>().text = "Start session " + sessionNumber.ToString();
        }
    }

    public void DoLaunchExperiment()
    {
        if (participantNameInput.text.Equals(""))
        {
            cantGoPrompt.GetComponent<UnityEngine.UI.Text>().text = "Please enter a participant";
            cantGoPrompt.SetActive(true);
            return;
        }
        if (!IsValidParticipantName(participantNameInput.text))
        {
            cantGoPrompt.GetComponent<UnityEngine.UI.Text>().text = "Please enter a valid participant name (ex. R1123E or LTP123)";
            cantGoPrompt.SetActive(true);
            return;
        }

        int sessionNumber = ParticipantSelection.nextSessionNumber;
        if (EditableExperiment.SessionComplete(sessionNumber, participantNameInput.text))
        {
            cantGoPrompt.GetComponent<UnityEngine.UI.Text>().text = "That session has already been completed.";
            cantGoPrompt.SetActive(true);
            return;
        }

        UnityEPL.AddParticipant(participantNameInput.text);
        UnityEPL.SetSessionNumber(sessionNumber);

        int listNumber = ParticipantSelection.nextListNumber;
        IronPython.Runtime.List words = ParticipantSelection.nextWords;
        EditableExperiment.ConfigureExperiment((ushort)(listNumber*12), (ushort)sessionNumber, newWords: words);
        UnityEngine.SceneManagement.SceneManager.LoadScene(FRExperimentSettings.ExperimentNameToExperimentScene(UnityEPL.GetExperimentName()));
    }

    private bool IsValidParticipantName(string name)
    {
        bool isTest = name.Equals("TEST");
        if (isTest)
            return true;
        if (name.Length != 6)
            return false;
        bool isValidRAMName = name[0].Equals('R') && name[1].Equals('1') && char.IsDigit(name[2]) && char.IsDigit(name[3]) && char.IsDigit(name[4]) && char.IsUpper(name[5]);
        bool isValidSCALPName = char.IsUpper(name[0]) && char.IsUpper(name[1]) && char.IsUpper(name[2]) && char.IsDigit(name[3]) && char.IsDigit(name[4]) && char.IsDigit(name[5]);
        return isValidRAMName || isValidSCALPName;
    }
}