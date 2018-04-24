using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoiceActivityDetection : MonoBehaviour
{
    public RamulatorInterface ramulatorInterface;
    public SoundRecorder soundRecorder;
    public float speakingThreshold = 0.01f;

    private bool talkingState = false;

	private void Update()
	{
        if (Time.timeSinceLevelLoad < 1)
            return;

        bool someoneIsTalking = SomeoneIsTalking();
        if (someoneIsTalking != talkingState)
        {
            talkingState = someoneIsTalking;
            ramulatorInterface.SetState("VOCALIZATION", talkingState, new Dictionary<string, object>());
        }
	}

    private bool SomeoneIsTalking()
    {
        float[] samples = soundRecorder.LastSamples(4410*5);
        double sum = 0;
        foreach (float sample in samples)
            sum += Mathf.Abs(sample);
        double average = sum / samples.Length;

        return average > speakingThreshold;
    }
}