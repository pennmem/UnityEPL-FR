using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WebRtc;

public class VoiceActivityDetection : MonoBehaviour
{
    public RamulatorInterface ramulatorInterface;

    private bool talkingState = false;

	private void Update()
	{
        bool someoneIsTalking = SomeoneIsTalking();
        if (someoneIsTalking != talkingState)
        {
            talkingState = someoneIsTalking;
            ramulatorInterface.SetState("VOCALIZATION", talkingState, new Dictionary<string, object>());
        }
	}

    private bool SomeoneIsTalking()
    {
        return true;
    }
}