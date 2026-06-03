using EasyTransition;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneTransitionLoader
{
    public static void LoadScene(string sceneName, TransitionManager transitionManager, TransitionSettings transitionSettings, float startDelay = 0f)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
            return;

        if (transitionManager == null)
            transitionManager = Object.FindAnyObjectByType<TransitionManager>();

        if (transitionManager != null && transitionSettings != null)
        {
            transitionManager.Transition(sceneName, transitionSettings, Mathf.Max(0f, startDelay));
            return;
        }

        SceneManager.LoadScene(sceneName);
    }

    public static void LoadCurrentScene()
    {
        var activeScene = SceneManager.GetActiveScene();
        if (activeScene.buildIndex >= 0)
            SceneManager.LoadScene(activeScene.buildIndex);
        else
            SceneManager.LoadScene(activeScene.name);
    }

    public static void LoadCurrentScene(TransitionManager transitionManager, TransitionSettings transitionSettings, float startDelay = 0f)
    {
        var activeScene = SceneManager.GetActiveScene();
        if (!string.IsNullOrWhiteSpace(activeScene.name))
        {
            LoadScene(activeScene.name, transitionManager, transitionSettings, startDelay);
            return;
        }

        LoadCurrentScene();
    }
}
