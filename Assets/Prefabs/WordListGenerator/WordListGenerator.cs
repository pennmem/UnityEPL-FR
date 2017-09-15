using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class WordListGenerator
{
	
	public abstract string[,] GenerateLists (int numberOfLists, int lengthOfEachList);

}

public class FR1ListGenerator : WordListGenerator
{

	//public override System.Collections.Generic.List<System.Collections.Generic.Dictionary<string, object>> GenerateLists (int numberOfLists, int lengthOfEachList)
	public override string[,] GenerateLists (int numberOfLists, int lengthOfEachList)
	{
		const int STIM_LIST_COUNT = 16;
		const int NONSTIM_LIST_COUNT = 6;
		const int BASELINE_LIST_COUNT = 3;

		const int A_STIM_COUNT = 5;
		const int B_STIM_COUNT = 5;
		const int AB_STIM_COUNT = 6;

		const int LEARNING_BLOCKS_COUNT = 4;

		string[,] lists = new string[numberOfLists, lengthOfEachList];

		//////////////////////Load the word pools
		string practice_pool_path = System.IO.Path.Combine (Application.dataPath, "Prefabs/WordListGenerator/wordpool/data/practice_en.txt");
		string main_pool_path = System.IO.Path.Combine (Application.dataPath, "Prefabs/WordListGenerator/wordpool/data/ram_wordpool_en.txt");

		IronPython.Runtime.List practice_words = ReadWordsFromPoolTxt (practice_pool_path);
		IronPython.Runtime.List main_words = ReadWordsFromPoolTxt (main_pool_path);

		//////////////////////Load the python wordpool code
		var engine = IronPython.Hosting.Python.CreateEngine ();
		var scope = engine.CreateScope ();

		string wordpool_path = System.IO.Path.Combine (Application.dataPath, "Prefabs/WordListGenerator/wordpool/nopandas.py");
		var source = engine.CreateScriptSourceFromFile (wordpool_path);

		source.Execute (scope);

		//////////////////////Call list creation functions from python
		var concatenate_session_lists = scope.GetVariable("concatenate_session_lists");
		var words_with_listnos = concatenate_session_lists (practice_words, main_words, lengthOfEachList, numberOfLists);

		IronPython.Runtime.List stim_nostim_list = new IronPython.Runtime.List ();
		for (int i = 0; i < STIM_LIST_COUNT; i++)
			stim_nostim_list.Add ("STIM");
		for (int i = 0; i < NONSTIM_LIST_COUNT; i++)
			stim_nostim_list.Add ("NON-STIM");
		stim_nostim_list = Shuffled (stim_nostim_list);

		var assign_list_types_from_type_list = scope.GetVariable("assign_list_types_from_type_list");
		var words_with_types = assign_list_types_from_type_list (words_with_listnos, BASELINE_LIST_COUNT, stim_nostim_list);

		IronPython.Runtime.List stim_channels_list = new IronPython.Runtime.List ();
		for (int i = 0; i < A_STIM_COUNT; i++)
			stim_channels_list.Add (new IronPython.Runtime.PythonTuple(new int[]{0}));
		for (int i = 0; i < B_STIM_COUNT; i++)
			stim_channels_list.Add (new IronPython.Runtime.PythonTuple(new int[]{1}));
		for (int i = 0; i < AB_STIM_COUNT; i++)
			stim_channels_list.Add (new IronPython.Runtime.PythonTuple(new int[]{0, 1}));
		stim_channels_list = Shuffled (stim_channels_list);

		var assign_multistim_from_stim_channels_list = scope.GetVariable("assign_multistim_from_stim_channels_list");
		var words_with_stim_channels = assign_multistim_from_stim_channels_list (words_with_types, stim_channels_list);

		HashSet<int> stim_listnos_set = new HashSet<int> ();
		HashSet<int> nonstim_listnos_set = new HashSet<int> ();
		foreach (var word in words_with_stim_channels)
		{
			if (word["type"].Equals("STIM"))
				stim_listnos_set.Add(word["listno"]);
			if (word["type"].Equals("NON-STIM"))
				nonstim_listnos_set.Add(word["listno"]);
		}
		System.Collections.Generic.List<int> stim_listnos = new System.Collections.Generic.List<int> (stim_listnos_set);
		System.Collections.Generic.List<int> nonstim_listnos = new System.Collections.Generic.List<int> (nonstim_listnos_set);

		System.Random rng = new System.Random();
		int first_random_stim_listno = stim_listnos[rng.Next(stim_listnos.Count)];
		stim_listnos.Remove(first_random_stim_listno);
		int second_random_stim_listno = stim_listnos[rng.Next(stim_listnos.Count)];

		int first_random_nonstim_listno = nonstim_listnos[rng.Next(nonstim_listnos.Count)];
		nonstim_listnos.Remove(first_random_nonstim_listno);
		int second_random_nonstim_listno = nonstim_listnos[rng.Next(nonstim_listnos.Count)];

		IronPython.Runtime.List learning_listnos = new IronPython.Runtime.List(){first_random_stim_listno, second_random_stim_listno, first_random_nonstim_listno, second_random_nonstim_listno};
		IronPython.Runtime.List learning_listnos_sequence = new IronPython.Runtime.List();

		for (int i = 0; i < LEARNING_BLOCKS_COUNT; i++)
			learning_listnos_sequence.extend(Shuffled(learning_listnos));

		var extract_blocks = scope.GetVariable("extract_blocks");
		var learning_blocks = extract_blocks (words_with_stim_channels, learning_listnos_sequence, LEARNING_BLOCKS_COUNT);

		var words_with_everythong = words_with_stim_channels.extend(learning_blocks);

		foreach (var word in words_with_everythong)
			foreach (var entry in word)
				Debug.Log(entry);

		return new string[1,1];//new System.Collections.Generic.List<System.Collections.Generic.Dictionary<string, object>> (words_with_everythong);
	}

	private IronPython.Runtime.List ReadWordsFromPoolTxt(string path)
	{
		string[] lines = System.IO.File.ReadAllLines (path);
		IronPython.Runtime.List words = new IronPython.Runtime.List();

		for (int i = 1; i < lines.Length; i++)
		{
			System.Collections.Generic.Dictionary<string, object> word = new System.Collections.Generic.Dictionary<string, object> ();
			word.Add ("word", lines [i]);
			words.Add (word);
		}

		return words;
	}

	private IronPython.Runtime.List Shuffled(IronPython.Runtime.List list)  
	{  
		System.Random rng = new System.Random ();
		IronPython.Runtime.List returnList = new IronPython.Runtime.List();

		while(list.Count > 0)
		{
			returnList.Add(list.pop(rng.Next(list.Count)));
		}

		return returnList;
	}
}