using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct ExperimentSettings
{
	public WordListGenerator wordListGenerator;
	public string experimentName;
	public string version;
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
	public int displayLearningMessageIndex;
}

public class EditableExperiment : MonoBehaviour
{
	private static ushort wordsSeen;
	private static ushort session;
	private static List<IronPython.Runtime.PythonDictionary> words;
	private static ExperimentSettings currentSettings;

	public RamulatorInterface ramulatorInterface;
	public TextDisplayer textDisplayer;
	public SoundRecorder soundRecorder;
	public VideoControl videoPlayer;
	public VideoControl countdownVideoPlayer;
	public KeyCode pauseKey = KeyCode.P;
	public GameObject pauseIndicator;
	public ScriptedEventReporter scriptedEventReporter;
	public AudioSource highBeep;
	public AudioSource lowBeep;
	public AudioSource lowerBeep;

	private bool paused = false;
	private string current_phase_type;

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
		Cursor.visible = false;

		if (currentSettings.Equals(default(ExperimentSettings)))
			throw new UnityException ("Please call ConfigureExperiment before loading the experiment scene.");

		//write versions to logfile
		Dictionary<string, string> versionsData = new Dictionary<string, string>();
		versionsData.Add ("UnityEPL version", Application.version);
		versionsData.Add ("Experiment version", currentSettings.version);
		versionsData.Add ("Logfile version", "1");
		scriptedEventReporter.ReportScriptedEvent("versions", versionsData, 0);

		if (ramulatorInterface != null)
			yield return ramulatorInterface.BeginNewSession (session);

		//starting from the beginning of the latest uncompleted list, do lists until the experiment is finished or stopped
		int startList = wordsSeen / currentSettings.wordsPerList;

		for (int i = startList; i < currentSettings.numberOfLists; i++)
		{
			if (ramulatorInterface != null)
			{
				ramulatorInterface.BeginNewTrial (i);
				current_phase_type = (string)words[wordsSeen] ["type"];
			}

			if (startList == 0 && i == 0)
				yield return DoIntroductionVideo ();

			if (i == 1 && i != startList)
				yield return PressAnyKey ("Please let the experimenter know \n" +
					"if you have any questions about \n" +
					"what you just did.\n\n" +
					"If you think you understand, \n" +
					"Please explain the task to the \n" +
					"experimenter in your own words.\n\n" +
					"Press any key to continue \n" +
					"to the first list.");
			else if (i == currentSettings.displayLearningMessageIndex)
				yield return PressAnyKey ("Great work!  You have successfully completed "+ i.ToString() +
					" trials of the experiment. In the last phase of the experiment, we will " +
					"have you study four more lists of words, but each of these lists will " +
					"include the identical set of 12 words presented in a random order.  As " +
					"before, you will study the words one at a time, then you will solve some " +
					"math problems, and then you will try to recall as many words as you can " +
					"remember in any order.  \n\n" +
					"Please explain these instructions back to the tester in your own words.\n\n" +
					"Press any key to continue to the next list.");
			else if (i != 0)
				yield return PressAnyKey ("Press any key to continue to trial " + i.ToString ());

			yield return DoCountdown ();
			yield return DoEncoding ();
			yield return DoDistractor ();
			yield return DoRecall ();
		}

		textDisplayer.DisplayText ("display end message", "Woo!  The experiment is over.");
	}

	private IEnumerator DoIntroductionVideo()
	{
		yield return PressAnyKey ("Press any key to play movie.");

		bool replay = false;
		do 
		{
			//start video player and wait for it to stop playing
			SetRamulatorState ("INSTRUCT", true, new Dictionary<string, string> ());
			videoPlayer.StartVideo ();
			while (videoPlayer.IsPlaying ())
				yield return null;
			SetRamulatorState ("INSTRUCT", false, new Dictionary<string, string> ());

			SetRamulatorState ("WAITING", true, new Dictionary<string, string> ());
			textDisplayer.DisplayText("repeat video prompt", "Press Y to continue to practice list, \n Press N to replay instructional video.");
			while (!Input.GetKeyDown(KeyCode.Y) && !Input.GetKeyDown(KeyCode.N))
			{
				yield return null;
			}
			textDisplayer.ClearText();
			SetRamulatorState ("WAITING", false, new Dictionary<string, string> ());
			replay = Input.GetKey(KeyCode.N);

		}
		while (replay);
	}

	private IEnumerator PressAnyKey(string displayText)
	{
		SetRamulatorState ("WAITING", true, new Dictionary<string, string> ());
		yield return null;
		textDisplayer.DisplayText ("press any key prompt", displayText);
		while (!Input.anyKeyDown)
			yield return null;
		textDisplayer.ClearText ();
		SetRamulatorState ("WAITING", false, new Dictionary<string, string> ());
	}

	private IEnumerator DoCountdown()
	{
		SetRamulatorState ("COUNTDOWN", true, new Dictionary<string, string> ());
		countdownVideoPlayer.StartVideo ();
		while (countdownVideoPlayer.IsPlaying ())
			yield return null;
//		for (int i = 0; i < currentSettings.countdownLength; i++)
//		{
//			textDisplayer.DisplayText ("countdown display", (currentSettings.countdownLength - i).ToString ());
//			yield return PausableWait (currentSettings.countdownTick);
//		}
		SetRamulatorState ("COUNTDOWN", false, new Dictionary<string, string> ());
	}

	private IEnumerator DoEncoding()
	{
		SetRamulatorState ("ENCODING", true, new Dictionary<string, string> ());

		int currentList = wordsSeen / currentSettings.wordsPerList;
		wordsSeen = (ushort)(currentList * currentSettings.wordsPerList);
		Debug.Log ("Beginning list index " + currentList.ToString());

		textDisplayer.DisplayText ("orientation stimulus", "+");
		SetRamulatorState ("ORIENT", true, new Dictionary<string, string> ());
		yield return PausableWait (currentSettings.wordPresentationLength);
		textDisplayer.ClearText ();
		SetRamulatorState ("ORIENT", false, new Dictionary<string, string> ());
		yield return PausableWait (Random.Range (currentSettings.minISI, currentSettings.maxISI));

		for (int i = 0; i < currentSettings.wordsPerList; i++)
		{
			string word = (string)words[wordsSeen]["word"];
			textDisplayer.DisplayText ("word stimulus", word);
			SetRamulatorWordState (true, words [wordsSeen]);
			yield return PausableWait (currentSettings.wordPresentationLength);
			textDisplayer.ClearText ();
			SetRamulatorWordState(false, words [wordsSeen]);
			IncrementWordsSeen();
			yield return PausableWait (Random.Range (currentSettings.minISI, currentSettings.maxISI));
		}
		SetRamulatorState ("ENCODING", false, new Dictionary<string, string> ());
	}

	private void SetRamulatorWordState(bool state, IronPython.Runtime.PythonDictionary wordData)
	{
		Dictionary<string, string> dotNetWordData = new Dictionary<string, string> ();
		foreach (string key in wordData.Keys)
			dotNetWordData.Add (key, wordData [key] == null ? "" : wordData [key].ToString());
		SetRamulatorState ("WORD", state, dotNetWordData);
	}

	private void SetRamulatorState(string stateName, bool state, Dictionary<string, string> extraData)
	{
		if (!stateName.Equals("WORD"))
			extraData.Add ("phase_type", current_phase_type);
		if (ramulatorInterface != null)
			ramulatorInterface.SetState (stateName, state, extraData);
	}

	private IEnumerator DoDistractor()
	{
		SetRamulatorState ("DISTRACT", true, new Dictionary<string, string> ());
		float endTime = Time.time + currentSettings.distractionLength;

		string distractor = "";
		string answer = "";

		float displayTime = 0;
		float answerTime = 0;

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
				textDisplayer.ChangeColor(Color.white);
				answered = false;
				distractorProblem = DistractorProblem ();
				distractor = distractorProblem [0].ToString () + " + " + distractorProblem [1].ToString () + " + " + distractorProblem [2].ToString () + " = ";
				answer = "";
				textDisplayer.DisplayText ("display distractor problem", distractor);
				displayTime = Time.time;
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
				if ((Input.GetKeyDown (KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) && !answer.Equals(""))
				{
					answered = true;
					int result;
					bool correct;
					if (int.TryParse(answer, out result) && result == distractorProblem [0] + distractorProblem [1] + distractorProblem [2])
					{
						textDisplayer.ChangeColor (Color.green);
						correct = true;
						lowBeep.Play ();
					}
					else
					{
						textDisplayer.ChangeColor (Color.red);
						correct = false;
						lowerBeep.Play ();
					}
					ReportDistractorAnswered (correct, distractor, answer);
					answerTime = Time.time;
					if (ramulatorInterface != null)
						ramulatorInterface.SendMathMessage (distractor, answer, (int)((answerTime - displayTime) * 1000), correct);
				}
			}
			yield return null;
		}
		textDisplayer.OriginalColor();
		textDisplayer.ClearText ();
		SetRamulatorState ("DISTRACT", false, new Dictionary<string, string> ());
	}

	private void ReportDistractorAnswered(bool correct, string problem, string answer)
	{
		Dictionary<string, string> dataDict = new Dictionary<string, string> ();
		dataDict.Add ("correctness", correct.ToString());
		dataDict.Add ("problem", problem);
		dataDict.Add ("answer", answer);
		scriptedEventReporter.ReportScriptedEvent ("distractor answered", dataDict);
	}

	private IEnumerator DoRecall()
	{
		SetRamulatorState ("RETRIEVAL", true, new Dictionary<string, string> ());
		highBeep.Play ();
		scriptedEventReporter.ReportScriptedEvent("Sound played", new Dictionary<string, string>(){{"sound name", "high beep"}, {"sound duration", highBeep.clip.length.ToString()}});
		textDisplayer.DisplayText ("display recall text", "* * * * * *");
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
		lowBeep.Play ();
		scriptedEventReporter.ReportScriptedEvent("Sound played", new Dictionary<string, string>(){{"sound name", "low beep"}, {"sound duration", lowBeep.clip.length.ToString()}});
		SetRamulatorState ("RETRIEVAL", false, new Dictionary<string, string> ());
	}

	private void WriteLstFile(string lstFilePath)
	{
		string[] lines = new string[currentSettings.wordsPerList];
		int startIndex = wordsSeen - currentSettings.wordsPerList;
		for (int i = startIndex; i < wordsSeen; i++)
		{
			IronPython.Runtime.PythonDictionary word = words [i];
			lines [i - (startIndex)] = (string)word["word"];
		}
		System.IO.FileInfo lstFile = new System.IO.FileInfo(lstFilePath);
		lstFile.Directory.Create();
		WriteAllLinesNoExtraNewline (lstFile.FullName, lines);
	}

	//thanks Virtlink from stackoverflow
	private static void WriteAllLinesNoExtraNewline(string path, params string[] lines)
	{
		if (path == null)
			throw new UnityException("path argument should not be null");
		if (lines == null)
			throw new UnityException("lines argument should not be null");

		using (var stream = System.IO.File.OpenWrite(path))
			using (System.IO.StreamWriter writer = new System.IO.StreamWriter(stream))
			{
				if (lines.Length > 0)
				{
					for (int i = 0; i < lines.Length - 1; i++)
					{
						writer.WriteLine(lines[i]);
					}
					writer.Write(lines[lines.Length - 1]);
				}
			}
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
		List<IronPython.Runtime.PythonDictionary> dotNetWords = new List<IronPython.Runtime.PythonDictionary> ();
		foreach (IronPython.Runtime.PythonDictionary word in newWords)
			dotNetWords.Add (word);
		SetWords (dotNetWords);
	}

	public static void SetWords(List<IronPython.Runtime.PythonDictionary> newWords)
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
			SetWords(currentSettings.wordListGenerator.GenerateLists (currentSettings.numberOfLists, currentSettings.wordsPerList));
		SaveState ();
	}
}