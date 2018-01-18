using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundRecorder : MonoBehaviour
{
    private AudioClip recording;
    private int offset;

    //using the system's default device
    public void StartRecording(int secondsMaxLength)
    {
        recording = Microphone.Start("", false, secondsMaxLength*2, 44100);
        float[] mostRecentSample = new float[] {0};
        while (mostRecentSample[0].Equals(0f))
        {
            int recordedUpToSample = 0;
            int microphonePosition = Microphone.GetPosition("") - 1;
            if (microphonePosition > 0)
                recordedUpToSample = microphonePosition;
            recording.GetData(mostRecentSample, recordedUpToSample);
        }
        offset = Microphone.GetPosition("");
        Debug.Log("Recording offset due to microphone latency: " + offset.ToString());
    }

    public void StopRecording(int waitForDuration, string outputFilePath)
    {
        while (Microphone.GetPosition("") < waitForDuration * 44100 + offset)
        {
            
        }
        AudioClip croppedClip = AudioClip.Create("cropped recording", 44100 * waitForDuration, 1, 44100, false);
        float[] saveData = new float[44100 * waitForDuration];
        recording.GetData(saveData, offset);
        croppedClip.SetData(saveData, 0);
        SavWav.Save(outputFilePath, croppedClip);
    }

    public AudioClip AudioClipFromDatapath(string datapath)
    {
        string url = "file:///" + datapath;
        WWW audioFile = new WWW(url);
        return audioFile.GetAudioClip();
    }
}