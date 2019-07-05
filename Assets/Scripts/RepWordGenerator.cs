using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;


// TODO -- Dummy class.  Replace with real one.
class DummyExperimentManager {
  public static Random rnd = new Random();  // Use in main thread only.
}


// Stores a word and whether or not it should be stimulated during encoding.
public class WordStim {
  public string word;
  public bool stim;

  public WordStim(string new_word, bool new_stim = false) {
    word = new_word;
    stim = new_stim;
  }

  public override string ToString() {
    return String.Format("{0}:{1}", word, Convert.ToInt32(stim));
  }
}


// Stores the number of times to repeat a word, and the count of how many
// words should be repeated that many times.
public class RepCnt {
  public int rep;
  public int count;

  public RepCnt(int new_rep, int new_count) {
    rep = new_rep;
    count = new_count;
  }
}

// e.g. new RepCounts(3,6).RepCnt(2,3).RepCnt(1,3);
// Specifies 3 repeats of 6 words, 2 repeats of 3 words, 1 instance of 3 words.
public class RepCounts : List<RepCnt> {
  public RepCounts() { }

  public RepCounts(int rep, int count) {
    RepCnt(rep, count);
  }

  public RepCounts RepCnt(int rep, int count) {
    Add(new RepCnt(rep, count));
    return this;
  }

  public int TotalWords() {
    int total = 0;
    foreach (var r in this) {
      total += r.rep * r.count;
    }
    return total;
  }
}


// If "i" goes past "limit", an exception is thrown with the stored message.
public class BoundedInt {
  private int limit;
  private string message;
  private int i_;
  public int i {
    get { return i_; }
    set { Assert(value); i_ = value; }
  }

  public BoundedInt(int limit_, string message_) {
    limit = limit_;
    message = message_;
  }

  private void Assert(int i) {
    if (i >= limit) {
      // TODO Should this be "UnityException"?
      throw new IndexOutOfRangeException(message);
    }
  }
}


// This class keeps a list of words associated with their stim states.
public class StimWordList : IEnumerable<WordStim> {
  protected List<string> words_;
  public IList<string> words {
    get { return words_.AsReadOnly(); }
  }
  protected List<bool> stims_;
  public IList<bool> stims {
    get { return stims_.AsReadOnly(); }
  }
  public int Count {
    get { return words_.Count; }
  }

  public StimWordList() {
    words_ = new List<string>();
    stims_ = new List<bool>();
  }

  public StimWordList(List<string> word_list, List<bool> stim_list = null) {
    words_ = new List<string>(word_list);
    stims_ = new List<bool>(stim_list ?? new List<bool>());

    // Force the two lists to be the same size.
    if (stims_.Count > words_.Count) {
      stims_.RemoveRange(words_.Count, 0);
    }
    else {
      while (stims_.Count < words_.Count) {
        stims_.Add(false);
      }
    }
  }

  public StimWordList(List<WordStim> word_stim_list) {
    words_ = new List<string>();
    stims_ = new List<bool>();
    
    foreach (var ws in word_stim_list) {
      words_.Add(ws.word);
      stims_.Add(ws.stim);
    }
  }

  public void Add(string word, bool stim=false) {
    words_.Add(word);
    stims_.Add(stim);
  }

  public void Add(WordStim word_stim) {
    Add(word_stim.word, word_stim.stim);
  }

  public void Insert(int index, string word, bool stim=false) {
    words_.Insert(index, word);
    stims_.Insert(index, stim);
  }

  public void Insert(int index, WordStim word_stim) {
    Insert(index, word_stim.word, word_stim.stim);
  }

  public IEnumerator<WordStim> GetEnumerator() {
    for (int i=0; i<words_.Count; i++) {
      yield return new WordStim(words_[i], stims_[i]);
    }
  }
  IEnumerator System.Collections.IEnumerable.GetEnumerator() {
    return GetEnumerator();
  }

  // Read-only indexed access.
  public WordStim this[int i] {
    get { return new WordStim(words_[i], stims_[i]); }
  }

  public override string ToString() {
    string str = this[0].ToString();
    for (int i=1; i<this.Count; i++) {
      str += String.Format(", {0}", this[i]);
    }
    return str;
  }
}


// A list of words which will each be repeated the specified number of times.
class RepWordList : StimWordList {
  public int repeats;

  public RepWordList(int repeats_=1) {
    repeats = repeats_;
  }

  public RepWordList(List<string> word_list, int repeats_=1,
      List<bool> stim_list = null)
      : base(word_list, stim_list) {
    repeats = repeats_;
  }

  public void SetStim(int index, bool state=true) {
    stims_[index] = state;
  }
}


// Generates well-spaced RepFR wordlists with open-loop stimulation assigned.
class RepWordGenerator {
  // Fisher-Yates shuffle
  public static List<T> Shuffle<T>(IList<T> list) {
    var shuf = new List<T>(list);
    for (int i=shuf.Count-1; i>0; i--) {
      int j = DummyExperimentManager.rnd.Next(i+1);
      T tmp = shuf[i];
      shuf[i] = shuf[j];
      shuf[j] = tmp;
    }
    
    return shuf;
  }

  // perm is the permutation to be assigned to the specified repword_lists,
  // interpreted in order.  If the first word in the first RepWordList is to
  // be repeated 3 times, the first three indices in perm are its locations
  // in the final list.  The score is a sum of the inverse
  // distances-minus-one between all neighboring repeats of each word.  Word
  // lists with repeats spaced farther receive the lowest scores, and word
  // lists with adjacent repeats receive a score of infinity.
  public static double SpacingScore(List<int> perm,
      List<RepWordList> repword_lists) {
    var split = new List<List<int>>();
    int offset = 0;
    foreach (var wl in repword_lists) {
      for (int w=0; w<wl.Count; w++) {
        var row = new List<int>();
        for (int r=0; r<wl.repeats; r++) {
          row.Add(perm[w*wl.repeats + r + offset]);
        }
        split.Add(row);
      }
      offset += wl.Count * wl.repeats;
    }

    double score = 0;

    foreach (var s in split) {
      s.Sort();

      for (int i=0; i<s.Count-1; i++) {
        double dist = s[i+1] - s[i];
        score += 1.0 / (dist-1);
      }
    }

    return score;
  }

  // Prepares a list of repeated words with better than random spacing,
  // while keeping the repeats associated with their stim state.
  public static StimWordList SpreadWords(
      List<RepWordList> repword_lists,
      double top_percent_spaced=0.2) {
    int word_len = 0;
    foreach (var wl in repword_lists) {
      word_len += wl.Count * wl.repeats;
    }

    var arrangements = new List<Tuple<double, List<int>>>();

    int iterations = Convert.ToInt32(100/top_percent_spaced);

    for (int i=0; i<iterations; i++) {
      double score = 1.0/0;
      int give_up = 20;
      var perm = new List<int>();
      while (give_up > 0 && double.IsInfinity(score)) {
        var range = Enumerable.Range(0, word_len).ToList();
        perm = Shuffle(range);

        score = SpacingScore(perm, repword_lists);
        give_up--;
      }
      arrangements.Add(new Tuple<double, List<int>>(score, perm));
    }

    arrangements.Sort((a,b) => a.Item1.CompareTo(b.Item1));

    var wordlst = new List<WordStim>();
    foreach (var wl in repword_lists) {
      foreach (var word_stim in wl) {
        for (int i=0; i<wl.repeats; i++) {
          wordlst.Add(word_stim);
        }
      }
    }

    var words_spread = new List<WordStim>(wordlst);

    for (int i=0; i<wordlst.Count; i++) {
      words_spread[arrangements[0].Item2[i]] = wordlst[i];
    }

    return new StimWordList(words_spread);
  }

  public static void AssignRandomStim(RepWordList rw) {
    for (int i=0; i<rw.Count; i++) {
      bool stim = Convert.ToBoolean(DummyExperimentManager.rnd.Next(2));
      rw.SetStim(i, stim);
    }
  }

  // Create a RepFR open-stim word list from specified lists of words to be
  // repeated and list of words to use once.
  public static StimWordList Generate(
      List<RepWordList> repeats,
      RepWordList singles,
      bool do_stim,
      double top_percent_spaced=0.2) {

    if (do_stim) {
      // Open-loop stim assigned here.
      foreach (var rw in repeats) {
        AssignRandomStim(rw);
      }
      AssignRandomStim(singles);
    }

    StimWordList prepared_words = SpreadWords(repeats, top_percent_spaced);

    foreach (var word_stim in singles) {
      int insert_at = DummyExperimentManager.rnd.Next(prepared_words.Count+1);
      prepared_words.Insert(insert_at, word_stim);
    }
    
    return prepared_words;
  }

  // Create a RepFR open-stim word list from a list of repetitions and counts,
  // and a list of candidate words.
  public static StimWordList Generate(
      RepCounts rep_cnts,
      List<string> input_words,
      bool do_stim,
      double top_percent_spaced=0.2) {

    var shuffled = Shuffle(input_words);

    var repeats = new List<RepWordList>();
    var singles = new RepWordList();

    var shuf = new BoundedInt(shuffled.Count,
        "Words required exceeded input word list size.");
    foreach (var rc in rep_cnts) {
      if (rc.rep == 1) {
        for (int i=0; i<rc.count; i++) {
          singles.Add(shuffled[shuf.i++]);
        }
      }
      else if (rc.rep > 1 && rc.count > 0) {
        var rep_words = new RepWordList(rc.rep);
        for (int i=0; i<rc.count; i++) {
          rep_words.Add(shuffled[shuf.i++]);
        }
        repeats.Add(rep_words);
      }
    }

    return Generate(repeats, singles, do_stim, top_percent_spaced);
  }
}


// Usage demonstration.
class TestingRepWord {
  //public static void Main() {
  public static void Test() {
    // Alternate calling modality.
//    var repeats1 = new RepWordList(new List<string>
//        {"cat", "dog", "fish", "bird", "shark", "tiger"}, 3);
//    var repeats2 = new RepWordList(new List<string>
//        {"corn", "wheat", "rice"}, 2);
//    var singles = new RepWordList(new List<string>
//        {"red", "blue", "green"});
//
//
//    StimWordList wordlst = RepWordGenerator.Generate(
//        new List<RepWordList>{repeats1, repeats2},
//        singles);


    var rep_counts = new RepCounts(3, 6).RepCnt(2, 3).RepCnt(1, 3);
    var input_word_list = new List<string> {"cat", "dog", "fish", "bird",
      "shark", "tiger", "corn", "wheat", "rice", "red", "blue", "green",
      "Mercury", "Venus", "Earth", "Mars"};

    StimWordList wordlst = RepWordGenerator.Generate(rep_counts,
        input_word_list, true);

    Console.WriteLine(wordlst);
    Console.WriteLine("Goal {0} words, generated {1} words.",
        rep_counts.TotalWords(), wordlst.Count);
  }
}
