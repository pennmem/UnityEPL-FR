using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WordAudioPresenter : MonoBehaviour
{
    public UnityEngine.AudioClip[] words;
    public UnityEngine.AudioSource wordSource;

    private Dictionary<string, AudioClip> wordToClipDict = new Dictionary<string, AudioClip>();

    private void OnEnable()
    {
        EditableExperiment.OnStateChange += WordSpeaker;
    }

    private void OnDisable()
    {
        EditableExperiment.OnStateChange -= WordSpeaker;
    }

    private void WordSpeaker(string stateName, bool on, Dictionary<string, object> extraData)
    {
        if (stateName.Equals("WORD") && on)
        {
            string word = (string)extraData["word"];
            if (wordToClipDict.ContainsKey(word))
            {
                wordSource.clip = wordToClipDict[word];
                wordSource.Play();
            }
            else
            {
                Debug.LogWarning(word + " does not have an associated audio recording.");
            }
        }
    }

    void Start () 
    {
        foreach (AudioClip word in words)
        {
            wordToClipDict[word.name] = word;
        }
	}
}
