using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class WordListGenerator
{
	public virtual IronPython.Runtime.List GenerateListsAndWriteWordpool (int numberOfLists, int lengthOfEachList)
	{
		WriteRAMWordpool ();
		return GenerateLists (numberOfLists, lengthOfEachList);
	}

	public abstract IronPython.Runtime.List GenerateLists (int numberOfLists, int lengthOfEachList);

	private void WriteRAMWordpool()
	{
		string directory = UnityEPL.GetParticipantFolder ();
		string filePath = System.IO.Path.Combine (directory, "RAM_wordpool.txt");
		string[] ram_wordpool_lines = GetWordpoolLines ("ram_wordpool_en");
		System.IO.Directory.CreateDirectory (directory);
		System.IO.File.WriteAllLines (filePath, ram_wordpool_lines);
	}

	private string[] GetWordpoolLines(string path)
	{
		string text = Resources.Load<TextAsset> (path).text;
		string[] lines = text.Split(new [] { '\r', '\n' });

		string[] lines_without_label = new string[lines.Length - 1];
		for (int i = 1; i < lines.Length; i++)
		{
			lines_without_label [i - 1] = lines [i];
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
				string line = lines [i];
				string[] category_and_word = line.Split ('\t');
				word ["category"] = category_and_word [0];
				word ["word"] = category_and_word [1];
			}
			else
			{
				word["word"] = lines [i];
			}
			words.Add (word);
		}
			
		return words;
	}

	protected IronPython.Runtime.List Shuffled(System.Random rng, IronPython.Runtime.List list)
	{
		IronPython.Runtime.List list_copy = new IronPython.Runtime.List ();
		foreach (var item in list)
			list_copy.Add (item);

		IronPython.Runtime.List returnList = new IronPython.Runtime.List();

		while(list_copy.Count > 0)
		{
			returnList.Add(list_copy.pop(rng.Next(list_copy.Count)));
		}

		return returnList;
	}

	protected Dictionary <string, IronPython.Runtime.List> BuildCategoryToWordDict (System.Random rng, IronPython.Runtime.List list)
	{
		Dictionary <string, IronPython.Runtime.List> categoriesToWords = new Dictionary<string, IronPython.Runtime.List> ();
		int totalWordCount = list.Count;
		foreach (IronPython.Runtime.PythonDictionary item in list)
		{
			string category = (string)item["category"];
			if (!categoriesToWords.ContainsKey (category))
			{
				categoriesToWords [category] = new IronPython.Runtime.List ();
			}
			categoriesToWords[category].Add (item);
			categoriesToWords [category] = Shuffled (rng, categoriesToWords [category]);
		}
		return categoriesToWords;
	}

	protected IronPython.Runtime.List CategoryShuffle(System.Random rng, IronPython.Runtime.List list, int lengthOfEachList)
	{
		/////////////in order to select words from appropriate categories, build a dict with categories as keys and a list of words as values
		Dictionary <string, IronPython.Runtime.List> categoriesToWords = BuildCategoryToWordDict (rng, list);

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
				categoriesToWords = BuildCategoryToWordDict (rng, list);
				returnList = new IronPython.Runtime.List();
				continue;
			}

			List<string> keyList = new List<string>(categoriesToWords.Keys);

			IronPython.Runtime.List singleList = new IronPython.Runtime.List();

			//////////find three random unique categories
			string randomCategoryA = keyList [rng.Next (keyList.Count)];
			string randomCategoryB;
			do
			{
				randomCategoryB = keyList [rng.Next (keyList.Count)];
			}
			while (randomCategoryB.Equals(randomCategoryA));
			string randomCategoryC;
			do
			{
				randomCategoryC = keyList [rng.Next (keyList.Count)];
			}
			while (randomCategoryC.Equals(randomCategoryA) | randomCategoryC.Equals(randomCategoryB));

			//////////get four words from each of these categories
			IronPython.Runtime.List groupA = new IronPython.Runtime.List ();
			IronPython.Runtime.List groupB = new IronPython.Runtime.List ();
			IronPython.Runtime.List groupC = new IronPython.Runtime.List ();

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
				secondHalf.Insert(secondHalf.Count-1, first_half_last_item);
			foreach (int item in secondHalf)
				groups.append(item);

			//////////append words to the final list according to the integers gotten above
			foreach(int groupNo in groups)
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
		var engine = IronPython.Hosting.Python.CreateEngine ();
		Microsoft.Scripting.Hosting.ScriptScope scope = engine.CreateScope ();

		string wordpool_text = Resources.Load<TextAsset>("nopandas").text;
		var source = engine.CreateScriptSourceFromString (wordpool_text);

		source.Execute (scope);

		return scope;
	}
}

public abstract class FRListGenerator : WordListGenerator
{
	private bool isCategory;

	public FRListGenerator(bool catifyMe = false)
	{
		isCategory = catifyMe;
	}

	public override IronPython.Runtime.List GenerateLists (int numberOfLists, int lengthOfEachList)
	{
		return GenerateListsOptionalCategory (numberOfLists, lengthOfEachList, isCategory);
	}

	public abstract IronPython.Runtime.List GenerateListsOptionalCategory (int numberOfLists, int lengthOfEachList, bool isCategoryPool);
}

public class FR1ListGenerator : FRListGenerator
{
	public FR1ListGenerator(bool catifyMe = false) : base(catifyMe: catifyMe)
	{

	}

	public override IronPython.Runtime.List GenerateListsOptionalCategory (int numberOfLists, int lengthOfEachList, bool isCategoryPool)
	{
		const int STIM_LIST_COUNT = 16;
		const int NONSTIM_LIST_COUNT = 6;
		const int BASELINE_LIST_COUNT = 3;

		const int A_STIM_COUNT = 5;
		const int B_STIM_COUNT = 5;
		const int AB_STIM_COUNT = 6;


		//////////////////////Load the python wordpool code
		Microsoft.Scripting.Hosting.ScriptScope scope = BuildPythonScope();


		//////////////////////Load the word pools

		IronPython.Runtime.List practice_words;
		IronPython.Runtime.List main_words;
		if (isCategoryPool)
		{
			practice_words = ReadWordsFromPoolTxt ("practice_cat_en", isCategoryPool);
			main_words = ReadWordsFromPoolTxt ("ram_categorized_en", isCategoryPool);
		}
		else 
		{
			practice_words = ReadWordsFromPoolTxt ("practice_en", isCategoryPool);
			main_words = ReadWordsFromPoolTxt ("ram_wordpool_en", isCategoryPool);
		}

		System.Random rng = new System.Random ();

		if (isCategoryPool)
		{
			practice_words = CategoryShuffle (rng, practice_words, lengthOfEachList);
			main_words = CategoryShuffle (rng, main_words, lengthOfEachList);
		}
		else
		{
			practice_words = Shuffled (rng, practice_words);
			main_words = Shuffled (rng, main_words);
		}


		////////////////////////////////////////////Call list creation functions from python
		//////////////////////Concatenate into lists with numbers
		var concatenate_session_lists = scope.GetVariable("concatenate_session_lists");
		var words_with_listnos = concatenate_session_lists (practice_words, main_words, lengthOfEachList, numberOfLists-1); // -1 because the practice list doesn't count


		//////////////////////Build type lists and assign tpyes
		IronPython.Runtime.List stim_nostim_list = new IronPython.Runtime.List ();
		for (int i = 0; i < STIM_LIST_COUNT; i++)
			stim_nostim_list.Add ("STIM");
		for (int i = 0; i < NONSTIM_LIST_COUNT; i++)
			stim_nostim_list.Add ("NON-STIM");
		stim_nostim_list = Shuffled (rng, stim_nostim_list);

		var assign_list_types_from_type_list = scope.GetVariable("assign_list_types_from_type_list");
		var words_with_types = assign_list_types_from_type_list (words_with_listnos, BASELINE_LIST_COUNT, stim_nostim_list);


		//////////////////////Build stim channel lists and assign stim channels
		IronPython.Runtime.List stim_channels_list = new IronPython.Runtime.List ();
		for (int i = 0; i < A_STIM_COUNT; i++)
			stim_channels_list.Add (new IronPython.Runtime.PythonTuple(new int[]{0}));
		for (int i = 0; i < B_STIM_COUNT; i++)
			stim_channels_list.Add (new IronPython.Runtime.PythonTuple(new int[]{1}));
		for (int i = 0; i < AB_STIM_COUNT; i++)
			stim_channels_list.Add (new IronPython.Runtime.PythonTuple(new int[]{0, 1}));
		stim_channels_list = Shuffled (rng, stim_channels_list);

		var assign_multistim_from_stim_channels_list = scope.GetVariable("assign_multistim_from_stim_channels_list");
		var words_with_stim_channels = assign_multistim_from_stim_channels_list (words_with_types, stim_channels_list);

		return words_with_stim_channels;
	}
}


public class FR6ListGenerator : FRListGenerator
{
	public FR6ListGenerator(bool catifyMe = false) : base(catifyMe: catifyMe)
	{
		
	}

	public override IronPython.Runtime.List GenerateListsOptionalCategory (int numberOfLists, int lengthOfEachList, bool isCategoryPool)
	{
		const int LEARNING_BLOCKS_COUNT = 4;


		//////////////////////Load the python wordpool code
		Microsoft.Scripting.Hosting.ScriptScope scope = BuildPythonScope();


		//////////////////////Start with FR1 lists
		IronPython.Runtime.List words_with_stim_channels = new FR1ListGenerator ().GenerateLists (numberOfLists-(LEARNING_BLOCKS_COUNT*4), lengthOfEachList);


		//////////////////////Get four listnos, shuffle into learning blocks, assign blocknos
		HashSet<int> stim_listnos_set = new HashSet<int> ();
		HashSet<int> nonstim_listnos_set = new HashSet<int> ();
		foreach (IronPython.Runtime.PythonDictionary word in words_with_stim_channels)
		{
			if (word["type"].Equals("STIM"))
				stim_listnos_set.Add((int)word["listno"]);
			if (word["type"].Equals("NON-STIM"))
				nonstim_listnos_set.Add((int)word["listno"]);
		}
		System.Collections.Generic.List<int> abstim_listnos = new System.Collections.Generic.List<int> ();
		foreach (int listno in stim_listnos_set)
		{
			int firstWordIndex = listno * lengthOfEachList;
			IronPython.Runtime.List singleWordSlice = (IronPython.Runtime.List)words_with_stim_channels.__getslice__(firstWordIndex, firstWordIndex+1);
			IronPython.Runtime.PythonDictionary word = (IronPython.Runtime.PythonDictionary)singleWordSlice.pop ();
			if (((IronPython.Runtime.PythonTuple)word ["stim_channels"]).ToString ().Equals ("(0, 1)"))
				abstim_listnos.Add (listno);
		}
		System.Collections.Generic.List<int> nonstim_listnos = new System.Collections.Generic.List<int> (nonstim_listnos_set);

		System.Random rng = new System.Random();
		int first_random_stim_listno = abstim_listnos[rng.Next(abstim_listnos.Count)];
		abstim_listnos.Remove(first_random_stim_listno);
		int second_random_stim_listno = abstim_listnos[rng.Next(abstim_listnos.Count)];

		int first_random_nonstim_listno = nonstim_listnos[rng.Next(nonstim_listnos.Count)];
		nonstim_listnos.Remove(first_random_nonstim_listno);
		int second_random_nonstim_listno = nonstim_listnos[rng.Next(nonstim_listnos.Count)];

		IronPython.Runtime.List learning_listnos = new IronPython.Runtime.List(){first_random_stim_listno, second_random_stim_listno, first_random_nonstim_listno, second_random_nonstim_listno};
		IronPython.Runtime.List learning_listnos_sequence = new IronPython.Runtime.List();

		for (int i = 0; i < LEARNING_BLOCKS_COUNT; i++)
			learning_listnos_sequence.extend (Shuffled (rng, learning_listnos));

		var extract_blocks = scope.GetVariable("extract_blocks");
		IronPython.Runtime.List learning_blocks = extract_blocks (words_with_stim_channels, learning_listnos_sequence, LEARNING_BLOCKS_COUNT);

		//////////////////////shuffle each list within the learning blocks
		for (int i = 0; i < learning_blocks.__len__() / lengthOfEachList; i++)
		{
			IronPython.Runtime.List single_list = (IronPython.Runtime.List)learning_blocks.__getslice__ (i * lengthOfEachList, (i + 1) * lengthOfEachList);
			if (isCategoryPool)
			{
				single_list = CategoryShuffle (rng, single_list, lengthOfEachList);
			}
			else
			{
				single_list = Shuffled (rng, single_list);
			}
			learning_blocks.__setslice__ (i * lengthOfEachList, (i + 1) * lengthOfEachList, single_list);
		}

		//////////////////////combine learning blocks with regular lists and return
		words_with_stim_channels.extend(learning_blocks);

//		foreach (IronPython.Runtime.PythonDictionary word in words_with_stim_channels)
//			foreach (var entry in word.Values)
//				Debug.Log(entry);

		return words_with_stim_channels;
	}
}