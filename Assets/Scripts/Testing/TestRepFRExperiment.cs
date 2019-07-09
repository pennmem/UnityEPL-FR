using System;
using System.Collections.Generic;

class TestingRepFR {
  public static void StimCheck(bool stim_state, StimWordList wordlst) {
    int stim_count = 0;
    foreach (var w in wordlst) {
      if (w.stim) {
        stim_count++;
      }
    }

    if (stim_state) {
      if (stim_count == 0 || stim_count == wordlst.Count) {
        Console.WriteLine("ERROR: Stim distribution does not look randomized.");
      }
      else {
        Console.WriteLine("Stim true test passed with {0} of {1}.", stim_count,
            wordlst.Count);
      }
    }
    else {
      if (stim_count == 0) {
        Console.WriteLine("Stim false successful.");
      }
      else {
        Console.WriteLine("ERROR: Stim false failed.");
      }
    }
  }

  public static void Main() {
    // TODO - Update test to use configuration system.
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

      StimCheck(session[i].encoding_stim, session[i].encoding);
      StimCheck(session[i].recall_stim, session[i].recall);
    }
  }
}

