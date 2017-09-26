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
	private static ushort session;
	private static IronPython.Runtime.List words;
	private static ExperimentSettings currentSettings;

	public RamulatorInterface ramulatorInterface;
	public TextDisplayer textDisplayer;
	public SoundRecorder soundRecorder;
	public VideoControl videoPlayer;
	public KeyCode pauseKey = KeyCode.P;
	public GameObject pauseIndicator;

	private bool paused = false;

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
		if (currentSettings.Equals(default(ExperimentSettings)))
			throw new UnityException ("Please call ConfigureExperiment before loading the experiment scene.");

		if (ramulatorInterface != null)
			yield return ramulatorInterface.BeginNewSession (session);

		//starting from the beginning of the latest uncompleted list, do lists until the experiment is finished or stopped
		int startList = wordsSeen / currentSettings.wordsPerList;

		if (startList == 0)
			yield return DoIntroductionVideo ();

		for (int i = startList; i < currentSettings.numberOfLists; i++)
		{
			if (i == startList && i != 0)
				yield return PressAnyKey ("Once you're ready, press any key to begin.");

			if (ramulatorInterface != null)
				ramulatorInterface.BeginNewTrial (i);
			yield return DoCountdown ();
			yield return DoEncoding ();
			yield return DoDistractor ();
			yield return DoRecall ();

			if (i == 0)
				yield return PressAnyKey ("Please let the experimenter know \n" +
					"if you have any questions about \n" +
					"what you just did.\n\n" +
					"If you think you understand, \n" +
					"Please explain the task to the \n" +
					"experimenter in your own words.\n\n" +
					"Press any key to continue \n" +
					"to the first list");

		}

		textDisplayer.DisplayText ("display end message", "Woo!  The experiment is over.");
	}

	private IEnumerator DoIntroductionVideo()
	{
		yield return PressAnyKey ("Press any key to play movie");

		bool replay = false;
		do 
		{
			//start video player and wait for it to stop playing
			videoPlayer.StartVideo ();
			while (videoPlayer.IsPlaying ())
				yield return null;

			textDisplayer.DisplayText("repeat video prompt", "Press Y to continue to practice list, \n Press N to replay instructional video");
			while (!Input.GetKeyDown(KeyCode.Y) && !Input.GetKeyDown(KeyCode.N))
			{
				yield return null;
			}
			replay = Input.GetKey(KeyCode.N);

		}
		while (replay);

	}

	private IEnumerator PressAnyKey(string displayText)
	{
		yield return null;
		textDisplayer.DisplayText ("press any key prompt", displayText);
		while (!Input.anyKeyDown)
			yield return null;
		textDisplayer.ClearText ();
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
		List<IronPython.Runtime.PythonDictionary> dotNetWords = new List<IronPython.Runtime.PythonDictionary> ();
		foreach (IronPython.Runtime.PythonDictionary word in words)
			dotNetWords.Add (word);

		int currentList = wordsSeen / currentSettings.wordsPerList;
		wordsSeen = (ushort)(currentList * currentSettings.wordsPerList);
		Debug.Log ("Beginning list index " + currentList.ToString());
		for (int i = 0; i < currentSettings.wordsPerList; i++)
		{
			string word = (string)dotNetWords[wordsSeen]["word"];
			textDisplayer.DisplayText ("word stimulus", word);
			if (ramulatorInterface != null)
				ramulatorInterface.SetState ("WORD", true, dotNetWords[wordsSeen]);
			yield return PausableWait (currentSettings.wordPresentationLength);
			textDisplayer.ClearText ();
			if (ramulatorInterface != null)
				ramulatorInterface.SetState ("WORD", false, dotNetWords[wordsSeen]);
			IncrementWordsSeen();
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
		soundRecorder.StartRecording (Mathf.CeilToInt(currentSettings.recallLength));
		yield return PausableWait(currentSettings.recallLength);

		//path
		int listno = (wordsSeen/12) - 1;
		string output_directory = UnityEPL.GetDataPath ();
		string wavFilePath = System.IO.Path.Combine (output_directory, listno.ToString());
		string lstFilePath = System.IO.Path.Combine (output_directory, listno.ToString () + ".lst");
		WriteLstFile (lstFilePath);

		soundRecorder.StopRecording(wavFilePath);
		textDisplayer.ClearText ();
	}

	private void WriteLstFile(string lstFilePath)
	{
		string[] lines = new string[currentSettings.wordsPerList];
		IronPython.Runtime.List lastTwelveWords = (IronPython.Runtime.List)words.__getslice__ (wordsSeen - currentSettings.wordsPerList, wordsSeen);
		int i = 0;
		foreach (IronPython.Runtime.PythonDictionary word in lastTwelveWords)
		{
			lines [i] = (string)word["word"];
			i++;
		}
		System.IO.FileInfo lstFile = new System.IO.FileInfo(lstFilePath);
		lstFile.Directory.Create();
		System.IO.File.WriteAllLines (lstFile.FullName, lines);
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

	private static void IncrementWordsSeen()
	{
		wordsSeen++;
		SaveState ();
	}

	public static void SaveState()
	{
		string filePath = SessionFilePath (session, UnityEPL.GetParticipants()[0]);
		string[] lines = new string[currentSettings.numberOfLists*currentSettings.wordsPerList+3];
		lines [0] = session.ToString ();
		lines [1] = wordsSeen.ToString ();
		lines [2] = (currentSettings.numberOfLists * currentSettings.wordsPerList).ToString ();
		if (words == null)
			throw new UnityException ("I can't save the state because a word list has not yet been generated");
		int i = 3;
		foreach (IronPython.Runtime.PythonDictionary word in words)
		{
			foreach (string key in word.Keys)
			{
				string value_string = word[key] == null ? "" : word [key].ToString ();
				lines [i] = lines [i] + key + ":" + value_string + ";";
			}
			i++;
		}
		System.IO.Directory.CreateDirectory (System.IO.Path.GetDirectoryName(filePath));
		System.IO.File.WriteAllLines (filePath, lines);
	}

	public static string SessionFilePath(int sessionNumber, string participantName)
	{
		string filePath = ParticipantFolderPath(participantName);
		filePath = System.IO.Path.Combine (filePath, sessionNumber.ToString() + ".session");
		return filePath;
	}

	public static string ParticipantFolderPath (string participantName)
	{
		return System.IO.Path.Combine (CurrentExperimentFolderPath(), participantName);
	}

	public static string CurrentExperimentFolderPath()
	{
		return System.IO.Path.Combine (Application.persistentDataPath, UnityEPL.GetExperimentName());
	}

	public static void SetWords(IronPython.Runtime.List newWords)
	{
		words = newWords;
	}

	public static bool SessionComplete(int sessionNumber, string participantName)
	{
		string sessionFilePath = EditableExperiment.SessionFilePath (sessionNumber, participantName);
		if (!System.IO.File.Exists(sessionFilePath))
			return false;
		string[] loadedState = System.IO.File.ReadAllLines(sessionFilePath);
		int wordsSeenInFile = int.Parse(loadedState [1]);
		int wordCount = int.Parse(loadedState [2]);
		return wordsSeenInFile >= wordCount;
	}

	public static void ConfigureExperiment(ushort newWordsSeen, ushort newSessionNumber)
	{
		wordsSeen = newWordsSeen;
		session = newSessionNumber;
		currentSettings = FRExperimentSettings.GetSettingsByName (UnityEPL.GetExperimentName ());
		if (words == null)
			words = currentSettings.wordListGenerator.GenerateLists (currentSettings.numberOfLists, currentSettings.wordsPerList);
		SaveState ();
	}
}