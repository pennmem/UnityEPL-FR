using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VideoControl : MonoBehaviour
{
    public UnityEngine.KeyCode pauseToggleKey = UnityEngine.KeyCode.Space;
    public UnityEngine.KeyCode deactivateKey = UnityEngine.KeyCode.Escape;
    public UnityEngine.Video.VideoPlayer videoPlayer;
    public bool deactivateWhenFinished = true;

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

    public void SetVideo(string videoPath)
    {
        transform.GetComponent<VideoSelector>().videoPath = videoPath;
    }

    public void StartVideo(string customText = null)
    {
        if (customText != null)
            videoPlayer.transform.GetComponentInChildren<UnityEngine.UI.Text>().text = customText;

        gameObject.SetActive(true);
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
