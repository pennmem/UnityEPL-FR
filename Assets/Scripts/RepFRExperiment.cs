using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

public class RepFRSession : List<RepFRRun> {
}


public class RepFRExperiment : ExperimentBase {
  protected List<string> source_words;
  protected List<string> blank_words;
  protected RepCounts rep_counts;
  protected int words_per_list;

  public RepFRExperiment(InterfaceManager _manager) : base(_manager) {
    //source_words = source_word_list;

    // TODO: - Get these parameters from the config system. -> most naturally expressed through nested array
    // Repetition specification:
    rep_counts = new RepCounts(3, 6).RepCnt(2, 3).RepCnt(1, 3);
    words_per_list = rep_counts.TotalWords();

    blank_words =
      new List<string>(Enumerable.Repeat(string.Empty, words_per_list));

    // Using Unity Asset loading, can be replaced
    string source_list = manager.fileManager.getWordList();
    var txt = File.ReadAllLines(source_list);
    source_words = new List<string>(txt);
    Debug.Log(source_words);

    // TODO: resuming session
    if(false) {

    }
    else {
      // add all state related data directly to
      // state class
      state.currentSession = GenerateSession();
      state.runIndex = 0;
      state.mainLoopIndex = 0;
      state.listIndex = 0;
      state.wordIndex = 0;
    }

    stateMachine["Run"] = new List<Action> {DoIntroductionVideo,
                                            MainLoop,
                                            Quit};
    stateMachine["MainLoop"] = new List<Action> {DoCountdownVideo,
                                                 DoEncoding,
                                                 DistractorTimeout,
                                                 DoDistractor};
                                                 //Recall};
    
    Start();
  }

  protected void Quit() {
    Debug.Log("Quitting");
    Stop();
    #if UNITY_EDITOR
    UnityEditor.EditorApplication.isPlaying = false;
    #else
    Application.Quit();
    #endif
    //no more calls to Run past this point
  }
  //////////
  // Run steps and wrapper functions
  // for advancing list state
  //////////
  
  protected void DoIntroductionVideo() {
    bool done = base.IntroductionVideo();
    if(done) {
      state.runIndex++;
      // video enqueues run, this function
      // must return before next Run call
      // can be processed
    }
  }


  //////////
  // Main Loop logic and
  // wrapper functions to ste
  //////////
  protected void MainLoop() {
    CheckLoop();
    stateMachine["MainLoop"][state.mainLoopIndex].Invoke();
  }

  protected void CheckLoop() {
    if(state.mainLoopIndex == stateMachine["MainLoop"].Count) {
      state.mainLoopIndex = 0;
      state.listIndex++;
    }

    if(state.listIndex  == state.currentSession.Count) {
      state.runIndex++;
      this.Do(new EventBase(Run));
      return;
    }
  }

  protected void DoCountdownVideo() {
    bool done = base.CountdownVideo();
    if(done) {
      state.mainLoopIndex++;
      // video enqueues run, this function
      // must return before next Run call
      // can be processed
    }
  }

  protected void DoEncoding() {
    // enforcing types with cast so that the base function can be called properly
    bool done = base.Encoding((IList<string>)state.currentSession[state.listIndex].encoding.words, (int)state.wordIndex);
    state.wordIndex++;
    if(done) {
      state.wordIndex = 0;
      state.mainLoopIndex++;
      this.Do(new EventBase(Run));
    }
  }

  protected void DistractorTimeout() {
    DoIn(new EventBase(() => state.mainLoopIndex++), (int)manager.getSetting("distractorDuration"));
    state.mainLoopIndex++;
    Do(new EventBase(Run));
  }

  protected void DoDistractor() {
    base.Distractor();
  }

  public RepFRRun MakeRun(bool enc_stim, bool rec_stim) {
    var enclist = RepWordGenerator.Generate(rep_counts, source_words, enc_stim);
    var reclist = RepWordGenerator.Generate(rep_counts, blank_words, rec_stim);
    return new RepFRRun(enclist, reclist, enc_stim, rec_stim);
  }


  public RepFRSession GenerateSession() {
    // Parameters retrieved from experiment config, given default
    // value if null. TODO:
    // Numbers of list types:
    int practice_lists = manager.getSetting("practiceLists") ?? 1;
    int pre_no_stim_lists = manager.getSetting("preNoStimLists") ?? 3;
    int encoding_only_lists = manager.getSetting("encodingOnlyLists") ?? 4;
    int retrieval_only_lists = manager.getSetting("retrievalOnlyLists") ?? 4;
    int encoding_and_retrieval_lists = manager.getSetting("encodingAndRetrievalLists") ?? 4;
    int no_stim_lists = manager.getSetting("noStimLists") ?? 10;
    

    var session = new RepFRSession();

    for (int i=0; i<practice_lists; i++) {
      session.Add(MakeRun(false, false));
    }
          
    for (int i=0; i<pre_no_stim_lists; i++) {
      session.Add(MakeRun(false, false));
    }

    var randomized_list = new RepFRSession();

    for (int i=0; i<encoding_only_lists; i++) {
      randomized_list.Add(MakeRun(true, false));
    }

    for (int i=0; i<retrieval_only_lists; i++) {
      randomized_list.Add(MakeRun(false, true));
    }

    for (int i=0; i<encoding_and_retrieval_lists; i++) {
      randomized_list.Add(MakeRun(true, true));
    }

    for (int i=0; i<no_stim_lists; i++) {
      randomized_list.Add(MakeRun(false, false));
    }

    session.AddRange(RepWordGenerator.Shuffle(randomized_list));

    return session;
  }
}
