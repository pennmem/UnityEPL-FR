using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
//using UnityEngine; // to read resource files packaged with Unity

public class FRExperiment : ExperimentBase {
  protected List<string> source_words;
  protected List<string> blank_words;
  protected RepCounts rep_counts = null;
  protected int words_per_list;
  protected int unique_words_per_list;

  protected FRSession currentSession;

  public FRExperiment(InterfaceManager _manager) : base(_manager) {
    // Repetition specification:
    int[] repeats = Config.wordRepeats;
    int[] counts = Config.wordCounts;

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

    // TODO: Load Session
    currentSession = GenerateSession();
    stateMachine = GetStateMachine();

    Start();
    Do(new EventBase(Run));
  }

  //////////
  // State Machine Constructor Functions
  //////////

  public override StateMachine GetStateMachine() {
    StateMachine stateMachine = StateMachine.GenStateMachine(currentSession);

    // TODO: reformat
    stateMachine["Run"] = new ExperimentTimeline(new List<Action<StateMachine>> {
        Introduction, // runs Introduction states
        MicrophoneTest, // runs MicrophoneTest states
        QuitPrompt,
        Practice, // runs Practice states
        ConfirmStart,
        MainLoop, // runs MainLoop states
        FinishExperiment});

    // though it is largely the same as the main loop,
    // practice is a conceptually distinct state machine
    // that just happens to overlap with MainLoop
    stateMachine["Practice"] = new LoopTimeline(new List<Action<StateMachine>> {
        StartTrial,
        NextPracticeListPrompt,
        Rest,
        CountdownVideo,
        EncodingDelay,
        Encoding,
        Distractor,
        RecallPrompt,
        Recall,
        EndPracticeTrial});

    stateMachine["MainLoop"] =  new LoopTimeline(new List<Action<StateMachine>> {
        StartTrial,
        NextListPrompt,
        Rest,
        CountdownVideo,
        EncodingDelay,
        Encoding,
        Distractor,
        RecallPrompt,
        Recall,
        EndTrial});

    stateMachine["Introduction"] = new LoopTimeline(new List<Action<StateMachine>> {
        IntroductionPrompt,
        IntroductionVideo,
        RepeatVideo});

    stateMachine["MicrophoneTest"] = new LoopTimeline(new List<Action<StateMachine>> {
        MicTestPrompt,
        RecordTest,
        RepeatMicTest});
    
    stateMachine.PushTimeline("Run");
    return stateMachine;
  }

  //////////
  // Wait Functions
  //////////

  protected void PauseBeforeRecall(StateMachine state) {
    int[] limits = Config.recallDelay;
    int interval = InterfaceManager.rnd.Value.Next(limits[0], limits[1]);
    state.IncrementState();
    WaitForTime(interval);
  }

  protected void EncodingDelay(StateMachine state) {
    int[] limits = Config.stimulusInterval; 
    int interval = InterfaceManager.rnd.Value.Next(limits[0], limits[1]);
    state.IncrementState();
    WaitForTime(interval);
  }

  protected void Rest(StateMachine state) {
    int duration = (int)Config.restDuration;
    state.IncrementState();
    manager.Do(new EventBase<string, string>(manager.ShowText, "orientation stimulus", "+"));
    ReportEvent("rest", null);
    SendHostPCMessage("REST", null);

    DoIn(new EventBase(() => {
                                ReportEvent("rest end", null);
                                Run();
                            }), 
                            duration);
  }

  //////////
  // List Setup Functions
  //////////

  protected virtual void StartTrial(StateMachine state) {
    Dictionary<string, object> data = new Dictionary<string, object>();
    data.Add("trial", state.CurrentSession<FRSession>().GetListIndex());
    data.Add("stim", currentSession.GetState().encoding_stim);

    ReportEvent("start trial", data);
    SendHostPCMessage("TRIAL", data);

    state.IncrementState();

    Run();
  }

  protected void EndTrial(StateMachine state) {
    if(state.CurrentSession<FRSession>().NextList()) {
      state.IncrementState();
    }
    else {
      state.PopTimeline();
    }
    
    Run();
  }

  // FIXME awkward handling of practice
  protected void EndPracticeTrial(StateMachine state) {
    if(state.CurrentSession<FRSession>().NextList()) {
      state.IncrementState();

      if(state.CurrentSession<FRSession>().GetListIndex() >= Config.practiceLists) {
        state.PopTimeline();
      }
    }
    else {
      state.PopTimeline();
    }

    
    Run();
  }

  //////////
  // Text prompts and associated key handlers
  //////////

  protected void RepeatVideo(StateMachine state) {
    WaitForKey("repeat introduction video", "Press Y to continue to practice list, \n Press N to replay instructional video.", 
                RepeatOrContinue);
  }

  protected void RepeatMicTest(StateMachine state) {
    WaitForKey("repeat mic test", "Did you hear the recording? \n(Y=Continue / N=Try Again).", 
                LoopOrContinue);
  }

  protected void IntroductionPrompt(StateMachine state) {
    WaitForKey("show instruction video", "Press any key to show instruction video", AnyKey);
  }

  protected void NextListPrompt(StateMachine state) {
    // FIXME: awkward handling of practice
    int list = (int)state.CurrentSession<FRSession>().GetListIndex() + 1 - Config.practiceLists;
    WaitForKey("pause before list", 
               String.Format("Press any key for trial {0}.", list),
               AnyKey);
  }

  protected void NextPracticeListPrompt(StateMachine state) {
    WaitForKey("pause before list", "Press any key for practice trial.", AnyKey);
  }

  //////////
  // Top level functions for state machine loops
  //////////

  // NOTE: could also use this as a 'check loop' function
  protected void Practice(StateMachine state) {
    state.IncrementState();
    state.PushTimeline("Practice");
    Run();
  }

  protected void MainLoop(StateMachine state) {
    state.IncrementState();
    state.PushTimeline("MainLoop");
    Run();
  }

  protected void Introduction(StateMachine state) {
    state.IncrementState();
    state.PushTimeline("IntroductionVideo");
    Run();
  }

  protected void MicrophoneTest(StateMachine state) {
    state.IncrementState();
    state.PushTimeline("MicrophoneTest");
    Run();
  }

  //////////
  // Experiment presentation stages
  //////////

  // TODO: need to separately manage practice and encoding lists
  protected void Encoding(StateMachine state) {
    Encoding((WordStim)state.CurrentSession<FRSession>().GetWord(), state.CurrentSession<FRSession>().GetSerialPos());
    if(!state.CurrentSession<FRSession>().NextWord()) {
        state.IncrementState();
    }
  }

  protected void Recall(StateMachine state) {
    string wavPath = System.IO.Path.Combine(manager.fileManager.SessionPath(), 
                                            state.CurrentSession<FRSession>().GetListIndex().ToString() + ".wav");
    state.IncrementState(); // TODO: JPB: Move this into ExperimentBase.cs
    bool stim = currentSession.GetState().recall_stim;
    Recall(wavPath, stim);
  }

  //////////
  // Experiment specific saving and loading logic
  //////////

  // TODO: saving and loading is entirely changed with StateMachine
  public override void SaveState() {
    // base.SaveState();
    // SaveSession();
  }

  public void WriteLstFile(StimWordList list, int index) {
    // create .lst files for annotation scripts
    string lstfile = System.IO.Path.Combine(manager.fileManager.SessionPath(), index.ToString() + ".lst");
    IList<string> noRepeats = new HashSet<string>(list.words).ToList();
    File.WriteAllLines(lstfile, noRepeats, System.Text.Encoding.UTF8);
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

  public FRRun MakeRun(RandomSubset subset_gen, bool enc_stim,
      bool rec_stim) {
    var enclist = RepWordGenerator.Generate(rep_counts,
        subset_gen.Get(unique_words_per_list), enc_stim);
    var reclist = RepWordGenerator.Generate(rep_counts, blank_words, rec_stim);
    return new FRRun(enclist, reclist, enc_stim, rec_stim);
  }


  public FRSession GenerateSession() {
    // Parameters retrieved from experiment config, given default
    // value if null.
    // Numbers of list types:
    int practice_lists = Config.practiceLists;
    int pre_no_stim_lists = Config.preNoStimLists;
    int encoding_only_lists = Config.encodingOnlyLists;
    int retrieval_only_lists = Config.retrievalOnlyLists;
    int encoding_and_retrieval_lists = Config.encodingAndRetrievalLists;
    int no_stim_lists = Config.noStimLists;
    
    RandomSubset subset_gen = new RandomSubset(source_words);

    var session = new FRSession();

    for (int i=0; i<practice_lists; i++) {
      session.Add(MakeRun(subset_gen, false, false));
    }

    for (int i=0; i<pre_no_stim_lists; i++) {
      session.Add(MakeRun(subset_gen, false, false));
    }

    var randomized_list = new FRSession();

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

    for (int i=0; i<session.Count; i++) {
      WriteLstFile(session[i].encoding, i);
    }

    return session;
  }
}

public class FRRun
{
    public StimWordList encoding;
    public StimWordList recall;
    public bool encoding_stim;
    public bool recall_stim;

    public FRRun(StimWordList encoding_list, StimWordList recall_list,
        bool set_encoding_stim = false, bool set_recall_stim = false)
    {
        encoding = encoding_list;
        recall = recall_list;
        encoding_stim = set_encoding_stim;
        recall_stim = set_recall_stim;
    }
}

[Serializable]
public class FRSession : Timeline<FRRun> {

  public bool NextWord() {
      return GetState().encoding.IncrementState();
  }

  public WordStim GetWord() {
    return GetState().encoding.GetState();
  }

  public bool NextList() {
    return IncrementState();
  }

  public int GetSerialPos() {
    return GetState().encoding.index;
  }

  public int GetListIndex() {
    return index;
  }

  // override void IDeserializationCallback.OnDeserialization(Object sender)
  // {
  //   NextList();
  // }
}
