using System;
using UnityEngine;
using UnityEngine.Video;

public class VideoControl : MonoBehaviour
{
    public UnityEngine.KeyCode pauseToggleKey = UnityEngine.KeyCode.Space;
    public UnityEngine.KeyCode deactivateKey = UnityEngine.KeyCode.Escape;
    public UnityEngine.Video.VideoPlayer videoPlayer;
    public bool deactivateWhenFinished = true;
    public Action callback = null;

    void Update()
    {
        if (Input.GetKeyDown(pauseToggleKey))
        {
            if (videoPlayer.isPlaying)
                videoPlayer.Pause();
            else
                videoPlayer.Play();
        }
        if (Input.GetKeyDown(deactivateKey))
        {
            videoPlayer.Stop();
            gameObject.SetActive(false);
        }
        if (videoPlayer.time >= videoPlayer.clip.length)
        {
            gameObject.SetActive(false);
        }
    }

    void OnDisable() {
        if(callback != null) {
            callback();
        }
        callback = null;
    }

    public void Start() {
        videoPlayer.loopPointReached += (vp) => gameObject.SetActive(false);
    }

    public void StartVideo(string video, Action onDone) {
        videoPlayer.clip = Resources.Load<VideoClip>(video);
        callback = onDone;
        gameObject.SetActive(true);
        videoPlayer.Play();
    }
    
    // legacy support
    public void StartVideo() {
        gameObject.SetActive(true);
        videoPlayer.Play();
    }

    public bool IsPlaying()
    {
        return gameObject.activeSelf;
    }

}
