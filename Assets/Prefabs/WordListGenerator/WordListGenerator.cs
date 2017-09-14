using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class WordListGenerator
{
	
	public abstract string[,] GenerateLists (int randomSeed, int numberOfLists, int lengthOfEachList);

}

public class FR1ListGenerator : WordListGenerator
{

	public override string[,] GenerateLists (int randomSeed, int numberOfLists, int lengthOfEachList)
	{
		string[,] lists = new string[numberOfLists, lengthOfEachList];

		var engine = IronPython.Hosting.Python.CreateEngine();
		var scope = engine.CreateScope();

		string wordpool_path = System.IO.Path.Combine (Application.dataPath, "Prefabs/WordListGenerator/wordpool/nopandas.py");
		var source = engine.CreateScriptSourceFromFile (wordpool_path);
		source.Execute(scope);


		// get function and dynamically invoke
		var generate_session_pool = scope.GetVariable("concatenate_session_lists");
		var result = generate_session_pool();
		Debug.Log(result);

		return lists;
	}
}