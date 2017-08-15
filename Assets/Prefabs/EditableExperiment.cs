using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EditableExperiment : MonoBehaviour
{
	private static ushort wordsSeen;
	private static ushort randomSeed;

	public TextDisplayer textDisplayer;
	public SoundRecorder soundRecorder;
	public VideoControl videoPlayer;
	public WordListGenerator wordListGenerator;
	public KeyCode pauseKey = KeyCode.P;
	public GameObject pauseIndicator;

	private string[,] words;
	private bool paused = false;

	private const int numberOfLists = 25;
	private const int wordsPerList = 12;

	private const int countdownLength = 10;
	private const float countdownTick = 1f;
	private const float wordPresentationLength = 1.6f;
	private const float minISI = 0.75f;
	private const float maxISI = 1f;
	private const float distractionLength = 20f;
	private const float answerConfirmationTime = 1f;
	private const float recallLength = 30f;

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

		words = wordListGenerator.GenerateLists (randomSeed, numberOfLists, wordsPerList);

		//start video player and wait for it to stop playing
		videoPlayer.StartVideo();
		while (videoPlayer.IsPlaying())
			yield return null;

		//starting from the beginning of the latest uncompleted list, do lists until the experiment is finished or stopped
		int startList = wordsSeen / wordsPerList;

		for (int i = startList; i < numberOfLists; i++)
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
		for (int i = 0; i < countdownLength; i++)
		{
			textDisplayer.DisplayText ("countdown display", (countdownLength - i).ToString ());
			yield return PausableWait (countdownTick);
		}
	}

	private IEnumerator DoEncoding()
	{
		int currentList = wordsSeen / wordsPerList;
		wordsSeen = (ushort)(currentList * wordsPerList);
		Debug.Log ("Beginning list index " + currentList.ToString());
		for (int i = 0; i < wordsPerList; i++)
		{
			textDisplayer.DisplayText ("word stimulus", words [currentList, i]);
			IncrementWordsSeen();
			yield return PausableWait (wordPresentationLength);
			textDisplayer.ClearText ();
			yield return PausableWait (Random.Range (minISI, maxISI));
		}
	}

	private IEnumerator DoDistractor()
	{
		float endTime = Time.time + distractionLength;
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
			if (Time.time - answerTime > answerConfirmationTime && answered)
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
					textDisplayer.DisplayText ("modify distractor answer", distractor);
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
		soundRecorder.StartRecording ();
		yield return PausableWait(recallLength);
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

	public static void ResetRandomSeed(ushort newRandomSeed)
	{
		randomSeed = newRandomSeed;
	}

	public static void SaveState()
	{
		string filePath = System.IO.Path.Combine (Application.persistentDataPath, UnityEPL.GetParticipants()[0]);
		string[] lines = new string[] { wordsSeen.ToString (), randomSeed.ToString () };
		System.IO.File.WriteAllLines (filePath, lines);
	}
}