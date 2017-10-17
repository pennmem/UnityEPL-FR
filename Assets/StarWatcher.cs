using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StarWatcher : MonoBehaviour
{
	public UnityEngine.UI.Text starMaker;
	public GameObject enableMe;
	public float enableTime = 30.1f;
	public ReturnHome[] sendUsHome;

	private bool rest = false;

	void Update ()
	{
		bool iSeeStars = (!starMaker.text.Equals("")) && (starMaker.text [0].Equals ('*'));

		if (!rest && (!starMaker.text.Equals("")))
		{
			bool iSeeDots = starMaker.text [starMaker.text.Length - 1].Equals ('.');
			enableMe.SetActive (iSeeDots);
		}

		if (!rest && iSeeStars)
		{
			rest = true;
			enableMe.SetActive (iSeeStars);
			Invoke ("StopResting", enableTime);
			foreach (ReturnHome sendMeHome in sendUsHome)
				sendMeHome.MayReturnHome ();
		}
	}

	private void StopResting()
	{
		enableMe.SetActive (false);
		rest = false;
	}
}
