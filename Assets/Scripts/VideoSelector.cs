using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VideoSelector : MonoBehaviour
{
    public UnityEngine.Video.VideoPlayer videoPlayer;
    public UnityEngine.RectTransform videoTransform;
    public UnityEngine.Video.VideoClip FR1clip;
    public UnityEngine.Video.VideoClip CatFR1clip;
    public UnityEngine.Video.VideoClip FR6clip;
    public UnityEngine.Video.VideoClip CatFR6clip;
    public UnityEngine.Video.VideoClip PS5frClip;
    public UnityEngine.Video.VideoClip CatPS5frClip;

	void OnEnable ()
    {
        Vector2 originalAnchorMin = videoTransform.anchorMin;
        Vector2 originalAnchorMax = videoTransform.anchorMax;

        if (UnityEPL.GetExperimentName().Equals("FR1"))
            videoPlayer.clip = FR1clip;
        if (UnityEPL.GetExperimentName().Equals("CatFR1"))
            videoPlayer.clip = CatFR1clip;
        if (UnityEPL.GetExperimentName().Equals("FR6"))
            videoPlayer.clip = FR6clip;
        if (UnityEPL.GetExperimentName().Equals("CatFR6"))
            videoPlayer.clip = CatFR6clip;
        if (UnityEPL.GetExperimentName().Equals("PS5_FR"))
            videoPlayer.clip = PS5frClip;
        if (UnityEPL.GetExperimentName().Equals("PS5_CatFR"))
            videoPlayer.clip = CatPS5frClip;
        if (UnityEPL.GetExperimentName().Equals("FR1") ||
            UnityEPL.GetExperimentName().Equals("CatFR1") ||
            UnityEPL.GetExperimentName().Equals("PS5_FR") ||
            UnityEPL.GetExperimentName().Equals("PS5_CatFR") )
        {
            videoTransform.anchorMin = new Vector2(0, originalAnchorMin.y);
            videoTransform.anchorMax = new Vector2(1, originalAnchorMax.y);
        }

        videoPlayer.Play();
	}
}