using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneChange : MonoBehaviour
{
    [Header("Scene Transition")]
    [Tooltip("Drag a completely black UI Panel with a CanvasGroup component here")]
    public CanvasGroup fadePanel;
    public float fadeDuration = 0.5f;

    // Call this from your Button's OnClick event
    public void MoveToScene(string sceneName)
    {
        if (fadePanel != null)
        {
            StartCoroutine(FadeAndLoad(sceneName));
        }
        else
        {
            // Fallback: If you forgot to assign a fade panel, just load instantly
            SceneManager.LoadScene(sceneName);
        }
    }

    private IEnumerator FadeAndLoad(string sceneName)
    {
        // 1. Instantly block mouse clicks so the player can't spam the button
        fadePanel.blocksRaycasts = true;

        // 2. Smoothly fade the alpha from 0 (clear) to 1 (solid black)
        float time = 0;
        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            fadePanel.alpha = Mathf.Lerp(0f, 1f, time / fadeDuration);
            yield return null;
        }

        fadePanel.alpha = 1f;

        // 3. Teleport to the new scene!
        SceneManager.LoadScene(sceneName);
    }

    // Call this from your Quit Button's OnClick event
    public void QuitGame()
    {
        Debug.Log("Quitting Game...");

        // This makes the Quit button work even when testing inside Unity!
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}