using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;


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

  public RepFRExperiment(List<string> source_word_list) {
    source_words = source_word_list;

    // TODO - Get these parameters from the config system.
    // Repetition specification:
    rep_counts = new RepCounts(3, 6).RepCnt(2, 3).RepCnt(1, 3);
    words_per_list = rep_counts.TotalWords();

    blank_words =
      new List<string>(Enumerable.Repeat(string.Empty, words_per_list));
  }

  public override void Run() {
//    if ( !Running() ) {
//      break;
//    }
  }


  public RepFRRun MakeRun(bool enc_stim, bool rec_stim) {
    var enclist = RepWordGenerator.Generate(rep_counts, source_words, enc_stim);
    var reclist = RepWordGenerator.Generate(rep_counts, blank_words, rec_stim);
    return new RepFRRun(enclist, reclist, enc_stim, rec_stim);
  }


  public RepFRSession GenerateSession() {
    // TODO - Get these parameters from the config system.
    // Numbers of list types:
    int practice_lists = 1;
    int pre_no_stim_lists = 3;
    int encoding_only_lists = 4;
    int retrieval_only_lists = 4;
    int encoding_and_retrieval_lists = 4;
    int no_stim_lists = 10;
    

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


class TestingRepFR {
  //public static void Main() {
  public static void Test() {
    
    var input_word_list = new List<string> {"cat", "dog", "fish", "bird",
      "shark", "tiger", "corn", "wheat", "rice", "red", "blue", "green",
      "Mercury", "Venus", "Earth", "Mars"};
    var experiment = new RepFRExperiment(input_word_list);
    var session = experiment.GenerateSession();

    for (int i=0; i<session.Count; i++) {
      Console.WriteLine("-----------------------------------");
      Console.WriteLine("List {0} ({1}) - {2}", i, session[i].encoding_stim,
          session[i].encoding);
      Console.WriteLine("List {0} ({1}) - {2}", i, session[i].recall_stim,
          session[i].recall);
    }
  }
}

