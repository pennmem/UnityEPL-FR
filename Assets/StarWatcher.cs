using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StarWatcher : MonoBehaviour
{
	public UnityEngine.UI.Text starMaker;
	public GameObject enableMe;
	public ReturnHome[] sendUsHome;

	void Update ()
	{
		bool iSeeStars = (!starMaker.text.Equals("")) && (starMaker.text [0].Equals ('*') || starMaker.text[starMaker.text.Length-1].Equals('.'));
		enableMe.SetActive(iSeeStars);
		if (iSeeStars)
			foreach (ReturnHome sendMeHome in sendUsHome)
				sendMeHome.MayReturnHome ();
	}
}
