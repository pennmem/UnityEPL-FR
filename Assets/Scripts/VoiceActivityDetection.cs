using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoiceActivityDetection : MonoBehaviour
{
    public RamulatorInterface ramulatorInterface;
    public SoundRecorder soundRecorder;
    public float speakingThreshold = 0.003f;
    //public AudioClip testClip;

    private bool talkingState = false;

	//private void Start()
	//{
 //       for (int i = 0; i < testClip.samples - 4410 * 10; i += 4410 * 5)
 //       {
 //           float[] samples = new float[4410*5];
 //           testClip.GetData(samples, i);
 //           double sum = 0;
 //           foreach (float sample in samples)
 //               sum += Mathf.Abs(sample);
 //           double average = sum / samples.Length;
 //           if (average > speakingThreshold && !talkingState)
 //           {
 //               talkingState = true;
 //               Debug.Log("word:" + (i / 44100).ToString());
 //           }
 //           else
 //           {
 //               talkingState = false;
 //           }
 //       }
 //       Debug.Log("test over");
	//}

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