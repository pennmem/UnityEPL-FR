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
		string python_source = System.IO.Path.Combine (Application.dataPath, "Prefabs/WordListGenerator/wordpool_generation/wordpool/listgen/fr.py");
		Debug.Log (python_source);
		var source = engine.CreateScriptSourceFromFile(python_source); // Load the script
		source.Execute(scope);

		// get function and dynamically invoke
		var generate_session_pool = scope.GetVariable("generate_session_pool");
		var result = generate_session_pool(); // returns 42 (Int32)
		Debug.Log(result);

		return lists;
	}
}