using System.Collections;
using System.Collections.Generic;
using System;
using Microsoft.CSharp;
using UnityEngine;

public abstract class WordListGenerator
{
    public virtual IronPython.Runtime.List GenerateListsAndWriteWordpool(int numberOfLists, int lengthOfEachList, bool isCategoryPool, bool isTwoParter, bool isEvenNumberSession, string participantCode)
    {
        WriteWordpoolToOutputFolder(isCategoryPool);
        return GenerateLists(numberOfLists, lengthOfEachList, new System.Random(), isCategoryPool, isTwoParter, isEvenNumberSession, participantCode);
    }

    public abstract IronPython.Runtime.List GenerateLists(int numberOfLists, int lengthOfEachList, System.Random rng, bool isCategoryPool, bool isTwoParter, bool isEvenNumberSession, string participantCode);

    private void WriteWordpoolToOutputFolder(bool isCategoryPool)
    {
        string directory = UnityEPL.GetParticipantFolder();
        string filename;
        if (isCategoryPool)
        {
            filename = "ram_categorized_en";
        }
        else
        {
            filename = "ram_wordpool_en";
        }

        string filePath = System.IO.Path.Combine(directory, filename);
        string[] ram_wordpool_lines = GetWordpoolLines(filename);

        if (isCategoryPool)
        {
            for (int i = 0; i < ram_wordpool_lines.Length; i++)
            {
                string line = ram_wordpool_lines[i];
                string[] split_line = line.Split('\t');
                ram_wordpool_lines[i] = split_line[1];
            }
        }

        System.IO.Directory.CreateDirectory(directory);
        System.IO.File.WriteAllLines(filePath + ".txt", ram_wordpool_lines);
    }

    private string[] GetWordpoolLines(string path)
    {
        string text = Resources.Load<TextAsset>(path).text;
        string[] lines = text.Split(new[] { '\r', '\n' });

        string[] lines_without_label = new string[lines.Length - 1];
        for (int i = 1; i < lines.Length; i++)
        {
            lines_without_label[i - 1] = lines[i];
        }

        return lines_without_label;
    }

    protected IronPython.Runtime.List ReadWordsFromPoolTxt(string path, bool isCategoryPool)
    {
        string[] lines = GetWordpoolLines(path);
        IronPython.Runtime.List words = new IronPython.Runtime.List();

        for (int i = 0; i < lines.Length; i++)
        {
            IronPython.Runtime.PythonDictionary word = new IronPython.Runtime.PythonDictionary();
            if (isCategoryPool)
            {
                string line = lines[i];
                string[] category_and_word = line.Split('\t');
                word["category"] = category_and_word[0];
                word["word"] = category_and_word[1];
            }
            else
            {
                word["word"] = lines[i];
            }
            words.Add(word);
        }

        return words;
    }

    protected IronPython.Runtime.List Shuffled(System.Random rng, IronPython.Runtime.List list)
    {
        IronPython.Runtime.List list_copy = new IronPython.Runtime.List();
        foreach (var item in list)
            list_copy.Add(item);

        IronPython.Runtime.List returnList = new IronPython.Runtime.List();

        while (list_copy.Count > 0)
        {
            returnList.Add(list_copy.pop(rng.Next(list_copy.Count)));
        }

        return returnList;
    }

    protected Dictionary<string, IronPython.Runtime.List> BuildCategoryToWordDict(System.Random rng, IronPython.Runtime.List list)
    {
        Dictionary<string, IronPython.Runtime.List> categoriesToWords = new Dictionary<string, IronPython.Runtime.List>();
        int totalWordCount = list.Count;
        foreach (IronPython.Runtime.PythonDictionary item in list)
        {
            string category = (string)item["category"];
            if (!categoriesToWords.ContainsKey(category))
            {
                categoriesToWords[category] = new IronPython.Runtime.List();
            }
            categoriesToWords[category].Add(item);
            categoriesToWords[category] = Shuffled(rng, categoriesToWords[category]);
        }
        return categoriesToWords;
    }

    /// <summary>
    /// Requires that the words in the list have a category entry and that categories have some multiple of four words in each of them.
    /// 
    /// Shuffles into lists of 12 words appended without delineation to the return list.
    /// </summary>
    /// <returns>The shuffled list of words.</returns>
    /// <param name="rng">Rng.</param>
    /// <param name="list">List.</param>
    /// <param name="lengthOfEachList">Length of each list.</param>
    protected IronPython.Runtime.List CategoryShuffle(System.Random rng, IronPython.Runtime.List list, int lengthOfEachList)
    {
        if (lengthOfEachList != 12)
            throw new UnityException("Currently only lists of 12 words are supported by CatFR.");

        /////////////in order to select words from appropriate categories, build a dict with categories as keys and a list of words as values
        Dictionary<string, IronPython.Runtime.List> categoriesToWords = BuildCategoryToWordDict(rng, list);

        /////////////we will append words to this in the proper order and then return it
        IronPython.Runtime.List returnList = new IronPython.Runtime.List();

        bool finished = false;
        int iterations = 0;
        do
        {
            iterations++;
            if (iterations > 1000)
            {
                finished = true;
                throw new UnityException("Error while shuffle catFR list");
            }

            ////////////if there are less than three categories remaining, we are on the last list and can't complete it validly
            ////////////this is currently handled by simply trying the whole process again
            if (categoriesToWords.Count < 3)
            {
                //start over
                categoriesToWords = BuildCategoryToWordDict(rng, list);
                returnList = new IronPython.Runtime.List();
                continue;
            }

            List<string> keyList = new List<string>(categoriesToWords.Keys);

            //////////find three random unique categories
            string randomCategoryA = keyList[rng.Next(keyList.Count)];
            string randomCategoryB;
            do
            {
                randomCategoryB = keyList[rng.Next(keyList.Count)];
            }
            while (randomCategoryB.Equals(randomCategoryA));
            string randomCategoryC;
            do
            {
                randomCategoryC = keyList[rng.Next(keyList.Count)];
            }
            while (randomCategoryC.Equals(randomCategoryA) | randomCategoryC.Equals(randomCategoryB));

            //////////get four words from each of these categories
            IronPython.Runtime.List groupA = new IronPython.Runtime.List();
            IronPython.Runtime.List groupB = new IronPython.Runtime.List();
            IronPython.Runtime.List groupC = new IronPython.Runtime.List();

            for (int i = 0; i < 4; i++)
            {
                groupA.Add(categoriesToWords[randomCategoryA].pop());
            }
            for (int i = 0; i < 4; i++)
            {
                groupB.Add(categoriesToWords[randomCategoryB].pop());
            }
            for (int i = 0; i < 4; i++)
            {
                groupC.Add(categoriesToWords[randomCategoryC].pop());
            }

            //////////remove categories from dict if all 12 words have been used
            if (categoriesToWords[randomCategoryA].Count == 0)
                categoriesToWords.Remove(randomCategoryA);
            if (categoriesToWords[randomCategoryB].Count == 0)
                categoriesToWords.Remove(randomCategoryB);
            if (categoriesToWords[randomCategoryC].Count == 0)
                categoriesToWords.Remove(randomCategoryC);

            //////////integers 0, 1, 2, 0, 1, 2 representing the order in which to present pairs of words from categories (A == 1, B == 2, etc.)
            //////////make sure to fulfill the requirement that both halves have ABC and the end of the first half is not the beginning of the second
            IronPython.Runtime.List groups = new IronPython.Runtime.List();
            for (int i = 0; i < 3; i++)
            {
                groups.Add(i);
            }
            groups = Shuffled(rng, groups);
            int index = 0;
            int first_half_last_item = 0;
            foreach (int item in groups)
            {
                if (index == 2)
                    first_half_last_item = item;
                index++;
            }

            IronPython.Runtime.List secondHalf = new IronPython.Runtime.List();
            for (int i = 0; i < 3; i++)
            {
                secondHalf.Add(i);
            }
            secondHalf.Remove(first_half_last_item);
            secondHalf = Shuffled(rng, secondHalf);
            bool insertAtEnd = rng.Next(2) == 0;
            if (insertAtEnd)
                secondHalf.Insert(secondHalf.Count, first_half_last_item);
            else
                secondHalf.Insert(secondHalf.Count - 1, first_half_last_item);
            foreach (int item in secondHalf)
                groups.append(item);

            //////////append words to the final list according to the integers gotten above
            foreach (int groupNo in groups)
            {
                if (groupNo == 0)
                {
                    returnList.append(groupA.pop());
                    returnList.append(groupA.pop());
                }
                if (groupNo == 1)
                {
                    returnList.append(groupB.pop());
                    returnList.append(groupB.pop());
                }
                if (groupNo == 2)
                {
                    returnList.append(groupC.pop());
                    returnList.append(groupC.pop());
                }
            }

            //////////if there are no more categories left, we're done
            if (categoriesToWords.Count == 0)
                finished = true;
        }
        while (!finished);

        return returnList;
    }



    protected Microsoft.Scripting.Hosting.ScriptScope BuildPythonScope()
    {
        var engine = IronPython.Hosting.Python.CreateEngine();
        Microsoft.Scripting.Hosting.ScriptScope scope = engine.CreateScope();

        string wordpool_text = Resources.Load<TextAsset>("nopandas").text;
        var source = engine.CreateScriptSourceFromString(wordpool_text);

        source.Execute(scope);

        return scope;
    }
}

public class FRListGenerator : WordListGenerator
{
    private int STIM_LIST_COUNT;
    private int NONSTIM_LIST_COUNT;
    private int BASELINE_LIST_COUNT;
    private int PS_LIST_COUNT;

    private int A_STIM_COUNT;
    private int B_STIM_COUNT;
    private int AB_STIM_COUNT;

    private int AMPLITUDE_COUNT;

    /// <summary>
    /// One of these should be attached to the ExperimentSettings scruct.
    /// </summary>
    /// <param name="NEW_STIM_LIST_COUNT">How many lists for which to instruct ramulator to stim on.  These will be interleaved with nonstim lists.</param>
    /// <param name="NEW_NONSTIM_LIST_COUNT">How many lists for which to instruct ramulator not to stim on.  These will be interleaved with stim lists.</param>
    /// <param name="NEW_BASELINE_LIST_COUNT">How many baseline lists to run before stim and nonstim lists start.</param>
    /// <param name="NEW_A_STIM_COUNT">Of stim lists, how many should be at stim site A.  (For many experiments, this is all the stim lists).</param>
    /// <param name="NEW_B_STIM_COUNT">Of stim lists, how many should be at stim site A. </param>
    /// <param name="NEW_AB_STIM_COUNT">Of stim lists, how many should be at stim site A and B simultaneously. </param>
    /// <param name="NEW_AMPLITUDE_COUNT">How many different amplitudes to use.  (For example, 1 for all the same amplitude, two if we want stim lists to be evenly divided between two different amplitudes.  Note the actual amplitude values are not set here.  We don't want to be responsible for that!</param>
    /// <param name="NEW_PS_LIST_COUNT">How many lists to label as "PS" when communicating with ramulator.  These will come after baseline and before stim/nonstim, but no experiments include both stim/nonstim and PS lists.</param>
    public FRListGenerator(int NEW_STIM_LIST_COUNT, int NEW_NONSTIM_LIST_COUNT, int NEW_BASELINE_LIST_COUNT, int NEW_A_STIM_COUNT, int NEW_B_STIM_COUNT, int NEW_AB_STIM_COUNT, int NEW_AMPLITUDE_COUNT, int NEW_PS_LIST_COUNT = 0)
    {
        STIM_LIST_COUNT = NEW_STIM_LIST_COUNT;
        NONSTIM_LIST_COUNT = NEW_NONSTIM_LIST_COUNT;
        BASELINE_LIST_COUNT = NEW_BASELINE_LIST_COUNT;
        PS_LIST_COUNT = NEW_PS_LIST_COUNT;
        A_STIM_COUNT = NEW_A_STIM_COUNT;
        B_STIM_COUNT = NEW_B_STIM_COUNT;
        AB_STIM_COUNT = NEW_AB_STIM_COUNT;
        AMPLITUDE_COUNT = NEW_AMPLITUDE_COUNT;
    }

    public override IronPython.Runtime.List GenerateLists(int numberOfLists, int lengthOfEachList, System.Random rng, bool isCategoryPool, bool isTwoParter, bool isEvenNumberSession, string participantCode)
    {
        return GenerateListsOptionalCategory(numberOfLists, lengthOfEachList, isCategoryPool, rng, isTwoParter, isEvenNumberSession, participantCode);
    }

    public IronPython.Runtime.List GenerateListsOptionalCategory(int numberOfLists, int lengthOfEachList, bool isCategoryPool, System.Random rng, bool isTwoParter, bool isEvenNumberSession, string participantCode)
    {
        //////////////////////Load the python wordpool code
        Microsoft.Scripting.Hosting.ScriptScope scope = BuildPythonScope();


        //////////////////////Load the word pool
        IronPython.Runtime.List all_words;
        if (isCategoryPool)
        {
            all_words = ReadWordsFromPoolTxt("ram_categorized_en", isCategoryPool);
        }
        else
        {
            all_words = ReadWordsFromPoolTxt("ram_wordpool_en", isCategoryPool);
        }

        //////////////////////For two part experiments, reliably shuffle according to participant name and construct halves, then shuffle again
        /// Otherwise, just shuffle
        if (isTwoParter)
        {
            System.Random reliable_random = new System.Random(participantCode.GetHashCode());
            if (!isCategoryPool)
            {
                all_words = Shuffled(reliable_random, all_words);
            }
            else
            {
                all_words = CategoryShuffle(reliable_random, all_words, 12);
            }

            if (isEvenNumberSession)
            {
                all_words = (IronPython.Runtime.List)all_words.__getslice__(0, all_words.Count / 2);
            }
            else
            {
                all_words = (IronPython.Runtime.List)all_words.__getslice__(all_words.Count / 2, all_words.Count);
            }
        }
        if (isCategoryPool)
        {
            all_words = CategoryShuffle(rng, all_words, lengthOfEachList);
        }
        else
        {
            all_words = Shuffled(rng, all_words);
        }

        ////////////////////////////////////////////Call list creation functions from python
        //////////////////////Concatenate into lists with numbers
        var assign_list_numbers_from_word_list = scope.GetVariable("assign_list_numbers_from_word_list");
        var words_with_listnos = assign_list_numbers_from_word_list(all_words, numberOfLists);


        //////////////////////Build type lists and assign tpyes
        IronPython.Runtime.List stim_nostim_list = new IronPython.Runtime.List();
        for (int i = 0; i < STIM_LIST_COUNT; i++)
            stim_nostim_list.Add("STIM");
        for (int i = 0; i < NONSTIM_LIST_COUNT; i++)
            stim_nostim_list.Add("NON-STIM");
        stim_nostim_list = Shuffled(rng, stim_nostim_list);

        var assign_list_types_from_type_list = scope.GetVariable("assign_list_types_from_type_list");
        var words_with_types = assign_list_types_from_type_list(words_with_listnos, BASELINE_LIST_COUNT, stim_nostim_list, num_ps: PS_LIST_COUNT);


        //////////////////////Build stim channel lists and assign stim channels
        IronPython.Runtime.List stim_channels_list = new IronPython.Runtime.List();
        for (int i = 0; i < A_STIM_COUNT; i++)
            stim_channels_list.Add(new IronPython.Runtime.PythonTuple(new int[] { 0 }));
        for (int i = 0; i < B_STIM_COUNT; i++)
            stim_channels_list.Add(new IronPython.Runtime.PythonTuple(new int[] { 1 }));
        for (int i = 0; i < AB_STIM_COUNT; i++)
            stim_channels_list.Add(new IronPython.Runtime.PythonTuple(new int[] { 0, 1 }));
        stim_channels_list = Shuffled(rng, stim_channels_list);

        var assign_multistim_from_stim_channels_list = scope.GetVariable("assign_multistim_from_stim_channels_list");
        var words_with_stim_channels = assign_multistim_from_stim_channels_list(words_with_types, stim_channels_list);


        ////////////////////Build amplitude index list and assign amplitude indeces
        IronPython.Runtime.List amplitude_index_list = new IronPython.Runtime.List();
        int lists_per_amplitude_index = 0;
        if (AMPLITUDE_COUNT != 0)
            lists_per_amplitude_index = STIM_LIST_COUNT / AMPLITUDE_COUNT;
        for (int amplitude_index = 0; amplitude_index < AMPLITUDE_COUNT; amplitude_index++)
        {
            for (int i = 0; i < lists_per_amplitude_index; i++)
                amplitude_index_list.Add(amplitude_index);
        }
        amplitude_index_list = Shuffled(rng, amplitude_index_list);

        var assign_amplitudes_from_amplitude_index_list = scope.GetVariable("assign_amplitudes_from_amplitude_index_list");
        var words_with_amplitude_indices = assign_amplitudes_from_amplitude_index_list(words_with_stim_channels, amplitude_index_list);


        return words_with_amplitude_indices;
    }
}