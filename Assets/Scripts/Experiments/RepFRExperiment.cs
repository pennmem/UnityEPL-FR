using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using System.IO;
using UnityEngine; // to read resource files packaged with Unity

public class RepFRRun {
  public StimWordList encoding;
  public StimWordList recall;

  public RepFRRun(StimWordList encoding_list, StimWordList recall_list,
      bool set_encoding_stim=false, bool set_recall_stim=false) {
    encoding = encoding_list;
    recall = recall_list;
  }
}

public class RepFRSession : List<RepFRRun> {
}

public class RepFRExperiment : ExperimentBase {
  protected List<string> source_words;
  protected List<string> blank_words;
  protected RepCounts rep_counts = null;
  protected int words_per_list;
  protected int unique_words_per_list;

  protected RepFRSession currentSession;

  public RepFRExperiment(InterfaceManager _manager) : base(_manager) {
    // Repetition specification:
    int[] repeats = manager.GetSetting("wordRepeats");
    int[] counts = manager.GetSetting("wordCounts");

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

    // boilerplate needed by RepWordGenerator
    words_per_list = rep_counts.TotalWords();
    unique_words_per_list = rep_counts.UniqueWords();
    blank_words = new List<string>(Enumerable.Repeat(string.Empty, words_per_list));
    source_words = ReadWordpool();

    // load state if previously existing
    // state is constructed by base constructor
    dynamic loadedState = LoadState((string)manager.GetSetting("participantCode"),
                                    (int)manager.GetSetting("session"));

    if(loadedState != null) {
      currentSession = LoadSession((string)manager.GetSetting("participantCode"),
                                        (int)manager.GetSetting("session"));

      // log experiment resume
      manager.Do(new EventBase<string, Dictionary<string, object>>(manager.ReportEvent,
                                                                   "experiment resumed", null));
      ReportEvent("experiment resume", null);

      state.listIndex = ++loadedState.listIndex;
    }
    else {
      currentSession = GenerateSession();
    }

    Start();
    Do(new EventBase(Run));
  }

  //////////
  // State Machine Constructor Functions
  //////////

  public override dynamic GetState() {
    state = base.GetState(); // all data must be serializable
    state.runIndex = 0;
    state.micTestIndex = 0;
    state.mainLoopIndex = 0;
    state.listIndex = 0;
    state.wordIndex = 0;

    return state;
  }

  public override Dictionary<string, List<Action>> GetStateMachine() {
    // TODO: some of these functions could be re imagined with wrappers, where the
    // state machine has functions that take parameters and return functions, such
    // as using a single function for the 'repeatlast' state that takes a prompt
    // to show or having WaitForKey wrap an action. It's not clear whether 
    // this improves clarity or reusability at all,
    // so I've deferred this. If it makes sense to do this or make more use of
    // wrapper functions that add state machine information, please do.
    Dictionary<string, List<Action>> stateMachine = base.GetStateMachine();

    stateMachine["Run"] = new List<Action> {DoIntroductionPrompt,
                                            DoIntroductionVideo,
                                            DoRepeatVideo,
                                            DoMicrophoneTest, // runs MicrophoneTest states
                                            DoRepeatMicTest,
                                            DoQuitorContinue,
                                            MainLoop, // runs MainLoop states
                                            FinishExperiment};

    stateMachine["MainLoop"] = new List<Action> {DoStartTrial,
                                                 DoNextListPrompt,
                                                 DoRest,
                                                 DoCountdownVideo,
                                                 DoEncodingDelay,
                                                 DoEncoding,
                                                 DoRest,
                                                 DoRecallPrompt,
                                                 DoRecall,
                                                 DoEndTrial};

    stateMachine["MicrophoneTest"] = new List<Action> {DoMicTestPrompt,
                                                       DoRecordTest};

    return stateMachine;
  }

  //////////
  // Wait Functions
  //////////

  protected void DoPauseBeforeRecall() {
    int[] limits = manager.GetSetting("recallDelay");
    int interval = InterfaceManager.rnd.Value.Next(limits[0], limits[1]);
    state.mainLoopIndex++;
    WaitForTime(interval);
  }

  protected void DoEncodingDelay() {
    int[] limits = manager.GetSetting("stimulusInterval"); 
    int interval = InterfaceManager.rnd.Value.Next(limits[0], limits[1]);
    state.mainLoopIndex++;

    WaitForTime(interval);
  }  


  protected void DoRest() {
    int duration = (int)manager.GetSetting("restDuration");
    state.mainLoopIndex++;
    manager.Do(new EventBase<string, string>(manager.ShowText, "orientation stimulus", "+"));
    ReportEvent("rest", null);

    DoIn(new EventBase(() => {
                                manager.Do(new EventBase(manager.ClearText)); 
                                ReportEvent("rest end", null);
                                Run();
                            }), 
                            duration);
  }

  //////////
  // List Setup Functions
  //////////

  protected virtual void DoStartTrial() {
    Dictionary<string, object> data = new Dictionary<string, object>();
    data.Add("trial", state.listIndex);
    // data.Add("stim", currentSession[state.listIndex].encoding_stim);

    ReportEvent("start trial", data);

    state.mainLoopIndex++;

    if(state.listIndex == (int)manager.GetSetting("practiceLists")) {
      Do(new EventBase(DoConfirmStart));
    }
    else {
      Run();
    }
  }

  protected void DoEndTrial() {
    state.mainLoopIndex++;
    Run();
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
    WaitForKey("repeat mic test", "Did you hear the recording? \n(Y=Continue / N=Try Again).", 
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
    if(state.listIndex < (int)manager.GetSetting("practiceLists")) {
      WaitForKey("pause before list", "Press any key for practice trial.", AnyKey);
    }
    else {
      int trialNo = state.listIndex - (int)manager.GetSetting("practiceLists") + 1;
      WaitForKey("pause before list", "Press any key for trial " + trialNo.ToString() + ".", AnyKey);
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
      state.mainLoopIndex = 0;
      state.listIndex++;
    }

    if(state.listIndex  >= currentSession.Count) {
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
      Run();
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

    StimWordList currentList = currentSession[state.listIndex].encoding;

    if(state.wordIndex >= currentList.Count) {
      state.wordIndex = 0;
      state.mainLoopIndex++;
      Run();
      return;
    }

    Encoding(currentList, state.wordIndex);
    state.wordIndex++;
  }

  protected void DoDistractor() {
    base.Distractor();
  }

  protected void DistractorTimeout() {
    DoIn(new EventBase(() => state.mainLoopIndex++), (int)manager.GetSetting("distractorDuration"));
    state.mainLoopIndex++;
    Run();
  }

  protected void RecallStim() {
    Dictionary<string, object> data = new Dictionary<string, object>();
    ReportEvent("recall stimulus info", data);

    SendHostPCMessage("STIM", data);
  }

  protected void DoRecall() {
    state.mainLoopIndex++;

    bool stim = currentSession[state.listIndex].recall_stim;
    int stim_reps = 0;

    // Uniform stim.
    if (stim) {
      int recstim_interval = manager.GetSetting("recStimulusInterval").ToObject<int>();
      int stim_duration = manager.GetSetting("stimulusDuration").ToObject<int>();
      int rec_period = manager.GetSetting("recallDuration").ToObject<int>();
      stim_reps = rec_period / (stim_duration + recstim_interval);

      int total_interval = stim_duration + recstim_interval;
      int stim_time = total_interval;  // We'll do the first one directly at the end of this function.
      for (int i=1; i<stim_reps; i++) {
        DoIn(new EventBase(() => {
          RecallStim();
        }), stim_time);
        stim_time += total_interval;
      }
    }

    //// Match stim distribution to encoding period.
    //StimWordList dummyList = currentSession[state.listIndex].recall;
    //// Pre-queue timed events for recall stim during the base Recall state.
    //int stim_time = 0;
    //foreach (var rec_wordstim in dummyList) {
    //  bool stim = rec_wordstim.stim;
    //  int[] limits = manager.GetSetting("stimulusInterval").ToObject<int[]>(); 
    //  int interval = InterfaceManager.rnd.Next(limits[0], limits[1]);
    //  int duration = manager.GetSetting("stimulusDuration").ToObject<int>();
    //  stim_time += duration + interval;
    //
    //  if (stim) {
    //    // Calculated as past end of recall period.
    //    // Stop pre-arranging stim sequence here.
    //    if (stim_time + interval > manager.GetSetting("recallDuration").ToObject<int>()) {
    //      break;
    //    }
    //
    //    DoIn(new EventBase(() => {
    //      RecallStim();
    //    }), stim_time);
    //  }
    //}

    string path = System.IO.Path.Combine(manager.fileManager.SessionPath(), state.listIndex.ToString() + ".wav");
    Recall(path);

    // Make sure the first stim happens before other events delay this.
    if (stim && stim_reps > 0) {
      RecallStim();
    }
  }


  //////////
  // Microphone testing states
  //////////

  protected void DoRecordTest() {
    state.micTestIndex++;
    string file =  System.IO.Path.Combine(manager.fileManager.SessionPath(), "microphone_test_" 
                    + DataReporter.TimeStamp().ToString("yyyy-MM-dd_HH_mm_ss") + ".wav");

    state.recordTestPath = file;
    RecordTest(file);
  }

  // protected void DoPlaybackTest() {
  //   state.micTestIndex++;
  //   string file = state.recordTestPath;
  //   PlaybackTest(file);
  // }

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
    SaveSession();
  }

  // TODO: these should be moved to the Session class
  public void SaveSession() {
    string filename = System.IO.Path.Combine(manager.fileManager.SessionPath(), "session_words.json");
    JsonSerializer serializer = new JsonSerializer();

    // create .lst files for annotation scripts
    for(int i = 0; i < currentSession.Count; i++) {
      string lstfile = System.IO.Path.Combine(manager.fileManager.SessionPath(), i.ToString() + ".lst");
      IList<string> noRepeats = new HashSet<string>(currentSession[i].encoding.words).ToList();
      WriteAllLinesNoExtraNewline(lstfile, noRepeats); 
    }

    using (StreamWriter sw = new StreamWriter(filename))
      using (JsonWriter writer = new JsonTextWriter(sw))
      {
        serializer.Serialize(writer, currentSession);
      }
  }

  public RepFRSession LoadSession(string participant, int session) {
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

  public List<string> ReadWordpool() {
    // wordpool is a file with 'word' as a header and one word per line.
    // repeats are described in the config file with two matched arrays,
    // repeats and counts, which describe the number of presentations
    // words can have and the number of words that should be assigned to
    // each of those presentation categories.
    string source_list = manager.fileManager.GetWordList();
    source_words = new List<string>();

    //skip line for csv header
    foreach(var line in File.ReadLines(source_list).Skip(1))
    {
      source_words.Add(line);
    }

    // copy wordpool to session directory
    string path = System.IO.Path.Combine(manager.fileManager.SessionPath(), "wordpool.txt");
    System.IO.File.Copy(source_list, path, true);

    return source_words;

  }

  public RepFRRun MakeRun(RandomSubset subset_gen, bool enc_stim,
      bool rec_stim) {
    var enclist = RepWordGenerator.Generate(rep_counts,
        subset_gen.Get(unique_words_per_list), enc_stim);
    var reclist = RepWordGenerator.Generate(rep_counts, blank_words, rec_stim);
    return new RepFRRun(enclist, reclist, enc_stim, rec_stim);
  }


  public RepFRSession GenerateSession() {
    // Parameters retrieved from experiment config, given default
    // value if null.
    // Numbers of list types:
    int practice_lists = (int)manager.GetSetting("practiceLists");
    int pre_no_stim_lists = (int)manager.GetSetting("preNoStimLists");
    int encoding_only_lists = (int)manager.GetSetting("encodingOnlyLists");
    int retrieval_only_lists = (int)manager.GetSetting("retrievalOnlyLists");
    int encoding_and_retrieval_lists = (int)manager.GetSetting("encodingAndRetrievalLists");
    int no_stim_lists = (int)manager.GetSetting("noStimLists");
    
    RandomSubset subset_gen = new RandomSubset(source_words);


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