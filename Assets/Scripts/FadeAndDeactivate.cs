using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This is attached to the error message that appears when someone tries to start the experiment with invalid parameters.
/// 
/// It simply makes the message fade away.
/// </summary>
public class FadeAndDeactivate : MonoBehaviour
{
    public float fadeTime;

    private Color originalColor;
    private float startTime;

    void OnEnable()
    {
        originalColor = GetComponent<UnityEngine.UI.Text>().color;
        startTime = Time.time;
    }

    void Update()
    {
        UnityEngine.UI.Text text = GetComponent<UnityEngine.UI.Text>();
        float aliveTime = Time.time - startTime;
        text.color = new Color(originalColor.r, originalColor.g, originalColor.b, originalColor.a * ((fadeTime - aliveTime) / fadeTime));
        if (aliveTime > fadeTime)
        {
            text.color = originalColor;
            gameObject.SetActive(false);
        }
    }
}
