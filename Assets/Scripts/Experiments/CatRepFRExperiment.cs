using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class CatRepFRExperiment : RepFRExperiment {
    public CatRepFRExperiment(InterfaceManager _manager) : base(_manager) {}

    //////////
    // State Machine Constructor Functions
    //////////

    public override StateMachine GetStateMachine() {
        StateMachine stateMachine = new StateMachine(currentSession);

        // TODO: reformat
        stateMachine["Run"] = new ExperimentTimeline(
            new List<Action<StateMachine>> {
                IntroductionPrompt,
                IntroductionVideo,
                RepeatVideo,
                MicrophoneTest, // runs MicrophoneTest states
                QuitPrompt,
                Practice, // runs Practice states
                ConfirmStart,
                MainLoop, // runs MainLoop states
                FinishExperiment});

        // though it is largely the same as the main loop,
        // practice is a conceptually distinct state machine
        // that just happens to overlap with MainLoop
        stateMachine["Practice"] = new LoopTimeline(
            new List<Action<StateMachine>> {
                StartTrial,
                NextPracticeListPrompt,
                PreCountdownRest,
                CountdownVideo,
                EncodingDelay,
                Encoding,
                Rest,
                RecallPrompt,
                Recall,
                EndPracticeTrial});

        stateMachine["MainLoop"] = new LoopTimeline(
            new List<Action<StateMachine>> {
                StartTrial,
                NextListPrompt,
                PreCountdownRest,
                CountdownVideo,
                EncodingDelay,
                Encoding,
                Rest,
                RecallPrompt,
                Recall,
                EndTrial});

        stateMachine["MicrophoneTest"] = new LoopTimeline(
            new List<Action<StateMachine>> {
                MicTestPrompt,
                RecordTest,
                RepeatMicTest});

        stateMachine.PushTimeline("Run");
        return stateMachine;
    }

    protected void PreCountdownRest(StateMachine state) {
        int duration = (int)manager.GetSetting("preCountdownRestDuration");
        state.IncrementState();
        manager.Do(new EventBase<string, string>(manager.ShowText, "orientation stimulus", "+"));
        ReportEvent("pre-countdown rest", null);
        SendHostPCMessage("REST", null);

        DoIn(new EventBase(() => {
            ReportEvent("pre-countdown rest end", null);
            Run();
        }), duration);
    }

    protected override void InitSession() {
        // boilerplate needed by RepWordGenerator
        words_per_list = rep_counts.TotalWords();
        unique_words_per_list = rep_counts.UniqueWords();
        blank_words = new List<string>(Enumerable.Repeat(string.Empty, words_per_list));
        var source_words = ReadCategorizedWordpool();

        var words = new CategorizedRandomSubset(source_words);

        // TODO: Load Session
        currentSession = GenerateSession(words);
    }

    public List<CategorizedWord> ReadCategorizedWordpool() {
        // wordpool is a file with 'category\tword' as a header
        // with one category and one word per line.
        // repeats are described in the config file with two matched arrays,
        // repeats and counts, which describe the number of presentations
        // words can have and the number of words that should be assigned to
        // each of those presentation categories.
        string source_list = manager.fileManager.GetWordList();
        Debug.Log(source_list);
        var source_words = new List<CategorizedWord>();

        //skip line for csv header
        foreach (var line in File.ReadLines(source_list).Skip(1)) {
            string[] category_and_word = line.Split('\t');
            source_words.Add(new CategorizedWord(category_and_word[1], category_and_word[0]));
        }

        // copy wordpool to session directory
        string path = System.IO.Path.Combine(manager.fileManager.SessionPath(), "wordpool.txt");
        System.IO.File.Copy(source_list, path, true);

        return source_words;
    }
}

// Provides random subsets of a word pool without replacement.
public class CategorizedRandomSubset : RandomSubset {
    protected Dictionary<string, List<Word>> shuffled = new();

    public CategorizedRandomSubset(List<CategorizedWord> source_words) {
        var catWords = new Dictionary<string, List<Word>>();

        foreach (var word in source_words) {
            List<Word> temp;
            catWords.TryGetValue(word.category, out temp);
            temp = temp != null ? temp : new();
            temp.Add(word);
            catWords[word.category] = temp; // TODO: JPB: Is this line needed?
        }

        foreach (var words in catWords) {
            shuffled[words.Key] = words.Value.Shuffle();
        }
    }

    // Get one word from each category
    public override List<Word> Get(int amount) {
        var remainingCategories = shuffled
            .Where(x => x.Value.Count() > 0)
            .ToList().Shuffle();

        if (amount > remainingCategories.Count()) {
            throw new IndexOutOfRangeException("Word list too small for session");
        }

        // Make sure to use the categories with more items first to balance item usage
        remainingCategories.Sort((x,y) => y.Value.Count().CompareTo(x.Value.Count()));

        var words = new List<Word>();
        for (int i = 0; i < amount; ++i) {
            var catWords = remainingCategories[i];
            words.Add(catWords.Value.Last());
            shuffled[catWords.Key].RemoveAt(catWords.Value.Count - 1);
        }

        return words;
    }
}

public class CategorizedWord : Word {
    public string category { get; protected set; }

    public CategorizedWord(string word, string category) : base(word) {
        this.category = category;
    }
}
