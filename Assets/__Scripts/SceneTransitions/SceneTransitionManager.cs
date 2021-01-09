using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;
using Zenject;

public class SceneTransitionManager : MonoBehaviour
{

    // TODO make instance variable
    public static bool IsLoading { get; private set; }

    // TODO Remove when all is moved to zenject
    public static SceneTransitionManager Instance { get; private set; }

    private static Queue<SceneTransitionBuilder> transitions = new Queue<SceneTransitionBuilder>();

    [SerializeField] private DarkThemeSO darkThemeSO;

    private PersistentUI persistentUI;
    private ZenjectSceneLoader sceneLoader;

    [Inject]
    private void Construct(PersistentUI persistentUI, ZenjectSceneLoader sceneLoader)
    {
        Instance = this;
        this.persistentUI = persistentUI;
        this.sceneLoader = sceneLoader;
    }

    // TODO: Rename to something like "ImmediateTransition"
    /// <summary>
    /// Create a basic <see cref="SceneTransitionBuilder"/> with the specified scene name, executes all enqueued builders.
    /// </summary>
    /// <param name="scene">Name of the scene to transition to.</param>
    /// <returns>A builder class to further configure the transition.</returns>
    public SceneTransitionBuilder LoadScene(string scene)
    {
        if (IsLoading) return null;
        darkThemeSO.DarkThemeifyUI();
        IsLoading = true;

        var builder = new SceneTransitionBuilder(scene, this);
        transitions.Enqueue(builder);

        ExecuteTransition();

        return builder;
    }

    /// <summary>
    /// Create a basic <see cref="SceneTransitionBuilder"/> with the specified name, and adds it to the queue. No transition will occur.
    /// </summary>
    /// <param name="scene">Name of the scene.</param>
    /// <returns>A builder class to further configure the transition.</returns>
    public SceneTransitionBuilder EnqueueTransition(string scene)
    {
        var builder = new SceneTransitionBuilder(scene, this);
        transitions.Enqueue(builder);
        return builder;
    }

    /// <summary>
    /// Executes a provided list of transition builders, overridding any previously queued transitions.
    /// </summary>
    /// <param name="builders">List of builders.</param>
    public void ExecuteTransitionStack(params SceneTransitionBuilder[] builders)
    {
        transitions = new Queue<SceneTransitionBuilder>(builders);
        ExecuteTransition();
    }

    /// <summary>
    /// Executes a transition with all currently enqueued <see cref="SceneTransitionBuilder"/>s.
    /// </summary>
    public void ExecuteTransition()
    {
        StartCoroutine(SceneTransition());
    }

    public void CancelLoading(string message)
    {
        if (!IsLoading) return;
        StopAllCoroutines();
        IsLoading = false;
        StartCoroutine(CancelLoadingTransitionAndDisplay(message));
    }

    private IEnumerator SceneTransition()
    {
        yield return persistentUI.FadeInLoadingScreen();

        while (transitions.Count > 0) yield return StartCoroutine(transitions.Dequeue().Transition(sceneLoader));

        darkThemeSO.DarkThemeifyUI();
        persistentUI.LevelLoadSlider.gameObject.SetActive(false);
        persistentUI.LevelLoadSliderLabel.text = "";

        yield return persistentUI.FadeOutLoadingScreen();

        IsLoading = false;
    }


    private IEnumerator CancelLoadingTransitionAndDisplay(string key)
    {
        if (!string.IsNullOrEmpty(key))
        {
            var message = LocalizationSettings.StringDatabase.GetLocalizedStringAsync("SongEditMenu", key);
            yield return persistentUI.DisplayMessage(message, PersistentUI.DisplayMessageType.BOTTOM);
        }
        yield return persistentUI.FadeOutLoadingScreen();
    }
}
