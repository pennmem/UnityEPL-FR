using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnableOnStars : MonoBehaviour
{
	public UnityEngine.UI.Text starMaker;
	public GameObject enableMe;
	public ReturnHome sendMeHome;

	void Update ()
	{
		bool iSeeStars = (!starMaker.text.Equals("")) && starMaker.text [0].Equals ('*');
		enableMe.SetActive(iSeeStars);
		if (iSeeStars)
			sendMeHome.MayReturnHome ();
	}
}
