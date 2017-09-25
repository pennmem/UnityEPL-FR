using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class WordListGenerator
{
	
	public abstract IronPython.Runtime.List GenerateLists (int numberOfLists, int lengthOfEachList);

	protected IronPython.Runtime.List ReadWordsFromPoolTxt(string path)
	{
		string[] lines = System.IO.File.ReadAllLines (path);
		IronPython.Runtime.List words = new IronPython.Runtime.List();

		for (int i = 1; i < lines.Length; i++)
		{
			IronPython.Runtime.PythonDictionary word = new IronPython.Runtime.PythonDictionary();
			word["word"] = lines [i];
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

	protected Microsoft.Scripting.Hosting.ScriptScope BuildPythonScope()
	{
		var engine = IronPython.Hosting.Python.CreateEngine ();
		Microsoft.Scripting.Hosting.ScriptScope scope = engine.CreateScope ();

		string wordpool_path = System.IO.Path.Combine (Application.dataPath, "Prefabs/WordListGenerator/wordpool/nopandas.py");
		var source = engine.CreateScriptSourceFromFile (wordpool_path);

		source.Execute (scope);

		return scope;
	}
}

public class FR1ListGenerator : WordListGenerator
{
	public override IronPython.Runtime.List GenerateLists (int numberOfLists, int lengthOfEachList)
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
		string practice_pool_path = System.IO.Path.Combine (Application.dataPath, "Prefabs/WordListGenerator/wordpool/data/practice_en.txt");
		string main_pool_path = System.IO.Path.Combine (Application.dataPath, "Prefabs/WordListGenerator/wordpool/data/ram_wordpool_en.txt");

		IronPython.Runtime.List practice_words = ReadWordsFromPoolTxt (practice_pool_path);
		IronPython.Runtime.List main_words = ReadWordsFromPoolTxt (main_pool_path);
		System.Random rng = new System.Random ();
		practice_words = Shuffled (rng, practice_words);
		main_words = Shuffled (rng, main_words);


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


public class FR6ListGenerator : WordListGenerator
{

	public override IronPython.Runtime.List GenerateLists (int numberOfLists, int lengthOfEachList)
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
			learning_listnos_sequence.extend (Shuffled (rng, learning_listnos));

		var extract_blocks = scope.GetVariable("extract_blocks");
		var learning_blocks = extract_blocks (words_with_stim_channels, learning_listnos_sequence, LEARNING_BLOCKS_COUNT);


		//////////////////////combine learning blocks with regular lists and return
		words_with_stim_channels.extend(learning_blocks);

//		foreach (IronPython.Runtime.PythonDictionary word in words_with_stim_channels)
//			foreach (var entry in word.Values)
//				Debug.Log(entry);

		return words_with_stim_channels;
	}

}