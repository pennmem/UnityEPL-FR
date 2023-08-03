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
    public UnityEngine.GameObject loadingButton;

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
        StartCoroutine(LaunchExperimentCoroutine());
    }

    private IEnumerator LaunchExperimentCoroutine()
    {
	Debug.Log("In LaunchExperiment.");
        if (participantNameInput.text.Equals(""))
        {
            cantGoPrompt.GetComponent<UnityEngine.UI.Text>().text = "Please enter a participant";
            cantGoPrompt.SetActive(true);
            yield break;
        }
        if (!IsValidParticipantName(participantNameInput.text))
        {
            cantGoPrompt.GetComponent<UnityEngine.UI.Text>().text = "Please enter a valid participant name (ex. R1123E or LTP123)";
            cantGoPrompt.SetActive(true);
            yield break;
        }

        int sessionNumber = ParticipantSelection.nextSessionNumber;
        if (EditableExperiment.SessionComplete(sessionNumber, participantNameInput.text))
        {
            cantGoPrompt.GetComponent<UnityEngine.UI.Text>().text = "That session has already been completed.";
            cantGoPrompt.SetActive(true);
            yield break;
        }

	Debug.Log("Attempting to start.");
        UnityEPL.AddParticipant(participantNameInput.text);
        UnityEPL.SetSessionNumber(sessionNumber);

	Debug.Log("Selecting Participant.");
        int listNumber = ParticipantSelection.nextListNumber;
	Debug.Log("Mark 1.");
        IronPython.Runtime.List words = ParticipantSelection.nextWords;
        Debug.Log(words);
	Debug.Log("Mark 2.");
        EditableExperiment.ConfigureExperiment((ushort)(listNumber * 12), (ushort)sessionNumber, newWords: words);
	Debug.Log("Mark 3.");
        launchButton.SetActive(false);
	Debug.Log("Mark 4.");
        loadingButton.SetActive(true);
	Debug.Log("At End.");
        yield return null;
        UnityEngine.SceneManagement.SceneManager.LoadScene(FRExperimentSettings.ExperimentNameToExperimentScene(UnityEPL.GetExperimentName()));
    }

    private bool IsValidParticipantName(string name)
    {
        bool isTest = name.Equals("TEST");
        if (isTest)
            return true;
        if (name.Length == 6)
        {
            bool isValidRAMName = name[0].Equals('R') && name[1].Equals('1') && char.IsDigit(name[2]) && char.IsDigit(name[3]) && char.IsDigit(name[4]) && char.IsUpper(name[5]);
            bool isValidSCALPName = char.IsUpper(name[0]) && char.IsUpper(name[1]) && char.IsUpper(name[2]) && char.IsDigit(name[3]) && char.IsDigit(name[4]) && char.IsDigit(name[5]);
            return isValidRAMName || isValidSCALPName;
        }
        if (name.Length == 8)
        {
            bool isValidRAMName = name[0].Equals('R') && name[1].Equals('1') && char.IsDigit(name[2]) && char.IsDigit(name[3]) && char.IsDigit(name[4]) && char.IsUpper(name[5]) && name[6].Equals('_') && char.IsDigit(name[7]);
            return isValidRAMName;
        }

        else
            return false;
    }
}
