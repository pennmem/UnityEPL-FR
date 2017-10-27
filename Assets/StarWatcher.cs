using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StarWatcher : MonoBehaviour
{
	public GameObject enableMe;
	public float enableTime = 30.1f;
	public ReturnHome[] sendUsHome;

	private bool rest = false;

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
		if (!(text.Equals("")) && (text [0].Equals ('+')))
			foreach (ReturnHome sendMeHome in sendUsHome)
				sendMeHome.MayReturnHome ();

		if (!rest && !(text.Equals("")))
		{
			bool iSeeDots = text [text.Length - 1].Equals ('.') || text.Contains ("=");
			enableMe.SetActive (iSeeDots);
		}

		bool iSeeStars = !(text.Equals("")) && (text [0].Equals ('*'));
		if (!rest && iSeeStars)
		{
			rest = true;
			enableMe.SetActive (iSeeStars);
			Invoke ("StopResting", enableTime);
		}
	}
}
