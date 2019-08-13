using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Converters;
using System.IO;
using UnityEngine; // to read resource files packaged with Unity

public class RepFRRun {
  public StimWordList encoding;
  public StimWordList recall;
  public bool encoding_stim;
  public bool recall_stim;

  public RepFRRun(StimWordList encoding_list, StimWordList recall_list,
      bool set_encoding_stim=false, bool set_recall_stim=false) {
    encoding = encoding_list;
    recall = recall_list;
    encoding_stim = set_encoding_stim;
    recall_stim = set_recall_stim;
  }
}

// Provides random subsets of a word pool without replacement.
// TODO - This should be moved to a more general location for reuse.
public class RandomSubset {
  protected List<String> shuffled;
  protected int index;

  public RandomSubset(List<String> source_words) {
    shuffled = RepWordGenerator.Shuffle(source_words);
    index = 0;
  }

  public List<String> Get(int amount) {
    if ((shuffled.Count - index) < amount) {
      throw new IndexOutOfRangeException("Word list too small for session");
    }
    int index_now = index;
    index += amount;

    return shuffled.GetRange(index_now, amount);
  }
}

public class RepFRSession : List<RepFRRun> {
}


public class RepFRExperiment : ExperimentBase {
  protected List<string> source_words;
  protected List<string> blank_words;
  protected RepCounts rep_counts = null;
  protected int words_per_list;

  protected RepFRSession currentSession;

  public RepFRExperiment(InterfaceManager _manager) : base(_manager) {
    // Repetition specification:
    int[] repeats = manager.GetSetting("wordRepeats").ToObject<int[]>();
    int[] counts = manager.GetSetting("wordCounts").ToObject<int[]>();

    if(repeats.Length != counts.Length) {
      throw new Exception("Word Repeats and Counts not aligned");
    }

    for(int i=0; i < repeats.Length; i++) {
      if(rep_counts == null) {
        rep_counts = new RepCounts(repeats[i], counts[i]);
      }
      else {
        rep_counts = rep_counts.RepCnt(repeats[i], counts[i]);
      }
    }
    words_per_list = rep_counts.TotalWords();

    blank_words =
      new List<string>(Enumerable.Repeat(string.Empty, words_per_list));


    string source_list = manager.fileManager.GetWordList();
    source_words = new List<string>();
    //skip line for csv header
    foreach(var line in File.ReadLines(source_list).Skip(1))
    {
      source_words.Add(line);
    }

    // load state if previously existing
    dynamic loadedState = LoadState((string)manager.GetSetting("participantCode"), (int)manager.GetSetting("session"));
    if(loadedState != null) {
      state = loadedState;
      currentSession = LoadRepFRSession((string)manager.GetSetting("participantCode"), (int)manager.GetSetting("session"));
      if(state.isComplete) {
        // FIXME: WARN and return to LAUNCHER 
      }

      // TODO: is this right?
      if(state.wordIndex > 0) {
        state.listIndex++;
      }
    }
    // generate new session if untouched
    else {
      currentSession = GenerateSession();
      state.listIndex = 0;
    }

    // always resume at the beginning
    state.wordIndex = 0;
    state.runIndex = 0;
    state.mainLoopIndex = 0;
    state.micTestIndex = 0;

    stateMachine["Run"] = new List<Action> {DoMicrophoneTest,
                                            DoRepeatMicTest,
                                            DoIntroductionPrompt,
                                            DoIntroductionVideo,
                                            DoRepeatVideo,
                                            DoQuitorContinue,
                                            MainLoop,
                                            Quit};

    stateMachine["MainLoop"] = new List<Action> {DoNextListPrompt,
                                                 DoCountdownVideo,
                                                 DoOrientation,
                                                 DoEncoding,
                                                 DistractorTimeout,
                                                 DoDistractor,
                                                 DoPauseBeforeRecall,
                                                 DoRecallPrompt,
                                                 DoRecall};

    stateMachine["MicrophoneTest"] = new List<Action> {DoMicTestPrompt,
                                                       DoRecordTest,
                                                       DoPlaybackTest};
    
    Start();
  }


  //////////
  // Wait Functions
  //////////

  protected void DoPauseBeforeRecall() {
    int[] limits = manager.GetSetting("recallDelay").ToObject<int[]>();
    int interval = InterfaceManager.rnd.Next(limits[0], limits[1]);
    state.mainLoopIndex++;
    WaitForTime(interval);
  }

  //////////
  // Text prompts and associated key handlers
  //////////
  protected void DoConfirmStart() {
    ConfirmStart();
  }

  protected void DoQuitorContinue(){
    state.runIndex++;
    QuitPrompt();
  }

  protected void DoMicTestPrompt() {
    state.micTestIndex++;
    MicTestPrompt();
  }

  protected void DoRepeatVideo() {
    WaitForKey("repeat introduction video", "Press Y to continue to practice list, \n Press N to replay instructional video.", 
                RepeatOrContinue);
  }

  protected void DoRepeatMicTest() {
    WaitForKey("repeat mic test", "Did you hear the recording? \n(Y=Continue / N=Try Again / C=Cancel).", 
                RepeatOrContinue);
  }

  protected void DoIntroductionPrompt() {
    state.runIndex++;
    WaitForKey("show instruction video", "Press any key to show instruction video", AnyKey);
  }

  protected void DoRecallPrompt() {
    state.mainLoopIndex++;
    base.RecallPrompt();
  }

  protected void DoNextListPrompt() {
    state.mainLoopIndex++; 
    if(state.listIndex == 0) {
      WaitForKey("pause before list", "Press any key for practice trial.", AnyKey);
    }
    else {
      WaitForKey("pause before list", "Press any key for trial " + (string)state.listIndex.ToString() + ".", AnyKey);
    }
  }

  //////////
  // Video Presentation functions
  //////////

  protected void DoIntroductionVideo() {
    state.runIndex++;
    base.IntroductionVideo();
  }

  protected void DoCountdownVideo() {
    state.mainLoopIndex++;
    base.CountdownVideo();
  }

  //////////
  // Top level functions for state machine loops
  //////////

  protected void MainLoop() {
    bool loop = CheckLoop();
    if(loop) {
      stateMachine["MainLoop"][state.mainLoopIndex].Invoke();
    }
  }

  protected bool CheckLoop() {
    if(state.mainLoopIndex == stateMachine["MainLoop"].Count) {
      if(state.listIndex == 0) {
        Do(new EventBase(DoConfirmStart));
      }
      state.mainLoopIndex = 0;
      state.listIndex++;

      return false;
    }

    if(state.listIndex  == currentSession.Count) {
      state.runIndex++;
      this.Do(new EventBase(Run));
      return false;
    }

    return true;
  }

  protected void DoMicrophoneTest() {
    if(state.micTestIndex == stateMachine["MicrophoneTest"].Count()) {
      state.runIndex++;
      state.micTestIndex = 0;
      Do(new EventBase(Run));
      return;
    } 
    else {
      stateMachine["MicrophoneTest"][state.micTestIndex].Invoke();
    }
  }

  //////////
  // Experiment presentation stages
  //////////

  protected void DoOrientation() {
    state.mainLoopIndex++;
    base.Orientation();
  }
  protected void DoEncoding() {
    // enforcing types with cast so that the base function can be called properly
    bool done = base.Encoding((IList<string>)currentSession[state.listIndex].encoding.words, (int)state.wordIndex);
    state.wordIndex++;
    if(done) {
      state.wordIndex = 0;
      state.mainLoopIndex++;
      this.Do(new EventBase(Run));
    }
  }

  protected void DoDistractor() {
    base.Distractor();
  }

  protected void DistractorTimeout() {
    DoIn(new EventBase(() => state.mainLoopIndex++), (int)manager.GetSetting("distractorDuration"));
    state.mainLoopIndex++;
    Do(new EventBase(Run));
  }

  protected void DoRecall() {
    state.mainLoopIndex++;
    string path = System.IO.Path.Combine(manager.fileManager.SessionPath(), state.listIndex.ToString());
    Recall(path);
  }

  //////////
  // Microphone testing states
  //////////

  protected void DoRecordTest() {
    state.micTestIndex++;
    // TODO: timestamp
    string file =  System.IO.Path.Combine(manager.fileManager.SessionPath(), "microphone_test_" 
                    /*+ DataReporter.RealWorldTime().ToString("yyyy-MM-dd_HH_mm_ss")*/ + ".wav");

    state.recordTestPath = file;
    RecordTest(file);
  }

  protected void DoPlaybackTest() {
    state.micTestIndex++;
    string file = state.recordTestPath;
    PlaybackTest(file);
  }

  //////////
  // state-specific key handlers
  //////////

  protected void RepeatOrContinue(string key, bool down) {
    if(down && key=="N") {
      state.runIndex--;
      Do(new EventBase(Run));
    }
    else if(down && key=="Y") {
      state.runIndex++;
      manager.Do(new EventBase(manager.ClearText));
      Do(new EventBase(Run));
    }
    else {
      manager.RegisterKeyHandler(RepeatOrContinue);
    }
  }

  //////////
  // Experiment specific saving and loading logic
  //////////

  public override void SaveState() {
    base.SaveState();
    SaveRepFRSession();
  }

  public void SaveRepFRSession() {
    string filename = System.IO.Path.Combine(manager.fileManager.SessionPath(), "session_words.json");
    JsonSerializer serializer = new JsonSerializer();

    using (StreamWriter sw = new StreamWriter(filename))
      using (JsonWriter writer = new JsonTextWriter(sw))
      {
        serializer.Serialize(writer, currentSession);
      }
  }
  public RepFRSession LoadRepFRSession(string participant, int session) {
    if(System.IO.File.Exists(System.IO.Path.Combine(manager.fileManager.SessionPath(participant, session), "session_words.json"))) {
      string json = System.IO.File.ReadAllText(System.IO.Path.Combine(manager.fileManager.SessionPath(participant, session), "session_words.json"));
      return JsonConvert.DeserializeObject<RepFRSession>(json);
    }
    else{
      return null;
    }
  }

  //////////
  // Word/Stim list generation
  //////////

  public RepFRRun MakeRun(RandomSubset subset_gen, bool enc_stim,
      bool rec_stim) {
    var enclist = RepWordGenerator.Generate(rep_counts,
        subset_gen.Get(words_per_list), enc_stim);
    var reclist = RepWordGenerator.Generate(rep_counts, blank_words, rec_stim);
    return new RepFRRun(enclist, reclist, enc_stim, rec_stim);
  }


  public RepFRSession GenerateSession() {
    // Parameters retrieved from experiment config, given default
    // value if null.
    // Numbers of list types:
    int practice_lists = (int)(manager.GetSetting("practiceLists") ?? 1);
    int pre_no_stim_lists = (int)(manager.GetSetting("preNoStimLists") ?? 3);
    int encoding_only_lists = (int)(manager.GetSetting("encodingOnlyLists") ?? 4);
    int retrieval_only_lists = (int)(manager.GetSetting("retrievalOnlyLists") ?? 4);
    int encoding_and_retrieval_lists = (int)(manager.GetSetting("encodingAndRetrievalLists") ?? 4);
    int no_stim_lists = (int)(manager.GetSetting("noStimLists") ?? 10);
    
    subset_gen = RandomSubset(source_words);


    var session = new RepFRSession();

    for (int i=0; i<practice_lists; i++) {
      session.Add(MakeRun(subset_gen, false, false));
    }
          
    for (int i=0; i<pre_no_stim_lists; i++) {
      session.Add(MakeRun(subset_gen, false, false));
    }

    var randomized_list = new RepFRSession();

    for (int i=0; i<encoding_only_lists; i++) {
      randomized_list.Add(MakeRun(subset_gen, true, false));
    }

    for (int i=0; i<retrieval_only_lists; i++) {
      randomized_list.Add(MakeRun(subset_gen, false, true));
    }

    for (int i=0; i<encoding_and_retrieval_lists; i++) {
      randomized_list.Add(MakeRun(subset_gen, true, true));
    }

    for (int i=0; i<no_stim_lists; i++) {
      randomized_list.Add(MakeRun(subset_gen, false, false));
    }

    session.AddRange(RepWordGenerator.Shuffle(randomized_list));

    return session;
  }
}
