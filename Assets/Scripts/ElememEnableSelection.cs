using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ElememEnableSelection : MonoBehaviour {
	public void SelectElememEnabled(bool newValue)
	{
		UnityEPL.SetUseElemem(newValue);
	}
}
