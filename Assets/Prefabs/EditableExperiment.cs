using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct ExperimentSettings
{
	public WordListGenerator wordListGenerator;
	public string experimentName;
	public int numberOfLists;
	public int wordsPerList;
	public int countdownLength;
	public float countdownTick;
	public float wordPresentationLength;
	public float minISI;
	public float maxISI;
	public float distractionLength;
	public float answerConfirmationTime;
	public float recallLength;
}

public class EditableExperiment : MonoBehaviour
{
	private static ushort wordsSeen;

	public TextDisplayer textDisplayer;
	public SoundRecorder soundRecorder;
	public VideoControl videoPlayer;
	public WordListGenerator wordListGenerator;
	public KeyCode pauseKey = KeyCode.P;
	public GameObject pauseIndicator;

	private string[,] words;
	private bool paused = false;

	private ExperimentSettings currentSettings;

	//use update to collect user input every frame
	void Update()
	{
		//check for pause
		if (Input.GetKeyDown (pauseKey))
		{
			paused = !paused;
			pauseIndicator.SetActive (paused);
		}
	}

	private IEnumerator PausableWait(float waitTime)
	{
		float endTime = Time.time + waitTime;
		while (Time.time < endTime)
		{
			if (paused)
				endTime += Time.deltaTime;
			yield return null;
		}
	}

	IEnumerator Start()
	{
		UnityEPL.SetExperimentName ("FR1");

		words = wordListGenerator.GenerateLists (UnityEngine.Random.Range(int.MinValue, int.MaxValue), currentSettings.numberOfLists, currentSettings.wordsPerList);

		//start video player and wait for it to stop playing
		videoPlayer.StartVideo();
		while (videoPlayer.IsPlaying())
			yield return null;

		//starting from the beginning of the latest uncompleted list, do lists until the experiment is finished or stopped
		int startList = wordsSeen / currentSettings.wordsPerList;

		for (int i = startList; i < currentSettings.numberOfLists; i++)
		{
			yield return DoCountdown ();
			yield return DoEncoding ();
			yield return DoDistractor ();
			yield return DoRecall ();
		}

		textDisplayer.DisplayText ("display end message", "Woo!  The experiment is over.");
	}

	private IEnumerator DoCountdown()
	{
		for (int i = 0; i < currentSettings.countdownLength; i++)
		{
			textDisplayer.DisplayText ("countdown display", (currentSettings.countdownLength - i).ToString ());
			yield return PausableWait (currentSettings.countdownTick);
		}
	}

	private IEnumerator DoEncoding()
	{
		int currentList = wordsSeen / currentSettings.wordsPerList;
		wordsSeen = (ushort)(currentList * currentSettings.wordsPerList);
		Debug.Log ("Beginning list index " + currentList.ToString());
		for (int i = 0; i < currentSettings.wordsPerList; i++)
		{
			textDisplayer.DisplayText ("word stimulus", words [currentList, i]);
			IncrementWordsSeen();
			yield return PausableWait (currentSettings.wordPresentationLength);
			textDisplayer.ClearText ();
			yield return PausableWait (Random.Range (currentSettings.minISI, currentSettings.maxISI));
		}
	}

	private IEnumerator DoDistractor()
	{
		float endTime = Time.time + currentSettings.distractionLength;
		float answerTime = 0;

		string distractor = "";
		string answer = "";

		bool answered = true;

		int[] distractorProblem = DistractorProblem ();

		while (Time.time < endTime)
		{
			if (paused)
			{
				endTime += Time.deltaTime;
			}
			if (paused && answered)
			{
				answerTime += Time.deltaTime;
			}
			if (Time.time - answerTime > currentSettings.answerConfirmationTime && answered)
			{
				textDisplayer.textElement.color = Color.white;
				answered = false;
				distractorProblem = DistractorProblem ();
				distractor = distractorProblem [0].ToString () + " + " + distractorProblem [1].ToString () + " + " + distractorProblem [2].ToString () + " = ";
				answer = "";
				textDisplayer.DisplayText ("display distractor problem", distractor);
			}
			else
			{
				int numberInput = GetNumberInput ();
				if (numberInput != -1)
				{
					answer = answer + numberInput.ToString ();
					textDisplayer.DisplayText ("modify distractor answer", distractor + answer);
				}
				if (Input.GetKeyDown (KeyCode.Backspace) && !answer.Equals(""))
				{
					answer = answer.Substring (0, answer.Length - 1);
					textDisplayer.DisplayText ("modify distractor answer", distractor + answer);
				}
				if (Input.GetKeyDown (KeyCode.Return) && !answer.Equals(""))
				{
					answered = true;
					if (int.Parse (answer) == distractorProblem [0] + distractorProblem [1] + distractorProblem [2])
						textDisplayer.textElement.color = Color.green;
					else
						textDisplayer.textElement.color = Color.red;
					answerTime = Time.time;
				}
			}
			yield return null;
		}
		textDisplayer.textElement.color = Color.white;
		textDisplayer.ClearText ();
	}

	private IEnumerator DoRecall()
	{
		textDisplayer.DisplayText ("display recall text", "* * *");
		soundRecorder.StartRecording (30);
		yield return PausableWait(currentSettings.recallLength);
		soundRecorder.StopRecording();
		textDisplayer.ClearText ();
	}

	private int GetNumberInput()
	{
		if (Input.GetKeyDown (KeyCode.Keypad0) || Input.GetKeyDown (KeyCode.Alpha0))
			return 0;
		if (Input.GetKeyDown (KeyCode.Keypad1) || Input.GetKeyDown (KeyCode.Alpha1))
			return 1;
		if (Input.GetKeyDown (KeyCode.Keypad2) || Input.GetKeyDown (KeyCode.Alpha2))
			return 2;
		if (Input.GetKeyDown (KeyCode.Keypad3) || Input.GetKeyDown (KeyCode.Alpha3))
			return 3;
		if (Input.GetKeyDown (KeyCode.Keypad4) || Input.GetKeyDown (KeyCode.Alpha4))
			return 4;
		if (Input.GetKeyDown (KeyCode.Keypad5) || Input.GetKeyDown (KeyCode.Alpha5))
			return 5;
		if (Input.GetKeyDown (KeyCode.Keypad6) || Input.GetKeyDown (KeyCode.Alpha6))
			return 6;
		if (Input.GetKeyDown (KeyCode.Keypad7) || Input.GetKeyDown (KeyCode.Alpha7))
			return 7;
		if (Input.GetKeyDown (KeyCode.Keypad8) || Input.GetKeyDown (KeyCode.Alpha8))
			return 8;
		if (Input.GetKeyDown (KeyCode.Keypad9) || Input.GetKeyDown (KeyCode.Alpha9))
			return 9;
		return -1;
	}

	private int[] DistractorProblem()
	{
		return new int[] { Random.Range (1, 9), Random.Range (1, 9), Random.Range (1, 9) };
	}

	public static ushort WordsSeen()
	{
		return wordsSeen;
	}

	public static void ResetWordsSeen(ushort newWordsSeen)
	{
		wordsSeen = newWordsSeen;
	}

	private static void IncrementWordsSeen()
	{
		wordsSeen++;
		SaveState ();
	}

	public static void SaveState()
	{
		string filePath = System.IO.Path.Combine (Application.persistentDataPath, UnityEPL.GetExperimentName());
		filePath = System.IO.Path.Combine (filePath, UnityEPL.GetParticipants()[0]);
		string[] lines = new string[] { wordsSeen.ToString () };
		System.IO.File.WriteAllLines (filePath, lines);
	}
}