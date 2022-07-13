using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VideoControl : MonoBehaviour
{
    public UnityEngine.KeyCode pauseToggleKey = UnityEngine.KeyCode.Space;
    public UnityEngine.KeyCode deactivateKey = UnityEngine.KeyCode.Escape;
    public UnityEngine.Video.VideoPlayer videoPlayer;
    public bool deactivateWhenFinished = true;

    private double duration = -1;

    void Start()
    {
        videoPlayer.loopPointReached += EndReached;
    }

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
    }

    public IEnumerator SetVideo(string videoPath)
    {
        transform.GetComponent<VideoSelector>().videoPath = videoPath;

        var isActive = gameObject.activeSelf;
        gameObject.SetActive(true);
        videoPlayer.Prepare();
        while (!videoPlayer.isPrepared) { yield return null; }
        SetVideoDuration();
        gameObject.SetActive(isActive);
    }

    private void SetVideoDuration()
    {
        //videoPlayer.Prepare();
        double time = videoPlayer.frameCount / videoPlayer.frameRate;
        TimeSpan VideoUrlLength = TimeSpan.FromSeconds(time);
        duration = VideoUrlLength.TotalSeconds;
    }

    public int VideoDurationSeconds()
    {
        if (duration > 0)
            return (int)Math.Ceiling(duration);
        else
            return (int)Math.Ceiling(videoPlayer.clip.length);
    }

    public void StartVideo(string customText = null)
    {
        if (customText != null)
            videoPlayer.transform.GetComponentInChildren<UnityEngine.UI.Text>().text = customText;

        gameObject.SetActive(true);
        videoPlayer.Play();
    }

    public bool IsPlaying()
    {
        return gameObject.activeSelf;
    }

    private void EndReached(UnityEngine.Video.VideoPlayer vp)
    {
        gameObject.SetActive(false);
    }
}
