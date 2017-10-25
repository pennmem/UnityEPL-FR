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
		if (!rest && (!starMaker.text.Equals("")))
		{
			bool iSeeDots = starMaker.text [starMaker.text.Length - 1].Equals ('.');
			enableMe.SetActive (iSeeDots);
		}

		bool iSeeStars = (!starMaker.text.Equals("")) && (starMaker.text [0].Equals ('*'));
		if (!rest && iSeeStars)
		{
			rest = true;
			enableMe.SetActive (iSeeStars);
			Invoke ("StopResting", enableTime);
		}
	}

	private void StopResting()
	{
		enableMe.SetActive (false);
		rest = false;
	}

	void OnEnable()
	{
		TextDisplayer.OnText += OnText;
	}

	void OnDisable()
	{
		TextDisplayer.OnText -= OnText;
	}

	void OnText(string text)
	{
		if ((!starMaker.text.Equals("")) && (starMaker.text [0].Equals ('+')))
			foreach (ReturnHome sendMeHome in sendUsHome)
				sendMeHome.MayReturnHome ();
	}
}
