using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Zenject;

public class SceneTransitionBuilder
{
    public IReadOnlyList<IEnumerator> EarlyLoadRoutines => earlyLoadRoutines.AsReadOnly();
    public IReadOnlyList<IEnumerator> LateLoadRoutines => earlyLoadRoutines.AsReadOnly();

    public string SceneName { get; private set; } = string.Empty;

    public LoadSceneMode LoadSceneMode { get; private set; } = LoadSceneMode.Single;
    public LoadSceneRelationship LoadSceneRelationship { get; private set; } = LoadSceneRelationship.None;
    
    public Action<DiContainer> EarlyBindingAction { get; private set; } = null;
    public Action<DiContainer> LateBindingAction { get; private set; } = null;

    public bool HasTransitioned { get; private set; } = false;

    private readonly List<IEnumerator> earlyLoadRoutines = new List<IEnumerator>();
    private readonly List<IEnumerator> lateLoadRoutines = new List<IEnumerator>();
    private readonly List<object> earlyLoadData = new List<object>();
    private readonly List<object> lateLoadData = new List<object>();

    public SceneTransitionBuilder(string sceneName)
    {
        SceneName = sceneName;
    }

    /// <summary>
    /// Unload all open scenes, and loads this one.
    /// </summary>
    public SceneTransitionBuilder AsSingle()
    {
        LoadSceneMode = LoadSceneMode.Single;
        return this;
    }

    /// <summary>
    /// Load this scene on top of all currently open scenes.
    /// </summary>
    public SceneTransitionBuilder AsAdditive()
    {
        LoadSceneMode = LoadSceneMode.Additive;
        return this;
    }

    /// <summary>
    /// This scene will inherit the <see cref="DiContainer"/> of the currently active Scene.
    /// </summary>
    public SceneTransitionBuilder AsChildScene()
    {
        LoadSceneRelationship = LoadSceneRelationship.Child;
        return this;
    }

    /// <summary>
    /// This scene will share the same parent <see cref="DiContainer"/>s as the currently active Scene.
    /// </summary>
    public SceneTransitionBuilder AsSiblingScene()
    {
        LoadSceneRelationship = LoadSceneRelationship.Sibling;
        return this;
    }

    /// <summary>
    /// Provides a list of coroutines to execute before the scene is loaded.
    /// </summary>
    public SceneTransitionBuilder WithEarlyLoadRoutines(params IEnumerator[] routines)
    {
        earlyLoadRoutines.AddRange(routines);
        return this;
    }

    /// <summary>
    /// Provides a list of coroutines to execute after the scene is loaded.
    /// </summary>
    public SceneTransitionBuilder WithLateLoadRoutines(params IEnumerator[] routines)
    {
        lateLoadRoutines.AddRange(routines);
        return this;
    }

    /// <summary>
    /// Provides data to be injected before any Contexts or Installers are executed.
    /// </summary>
    public SceneTransitionBuilder WithDataInjectedEarly(params object[] data)
    {
        earlyLoadData.AddRange(data);
        return this;
    }

    /// <summary>
    /// Provides data to be injected after any Contexts or Installers are executed.
    /// </summary>
    public SceneTransitionBuilder WithDataInjectedLate(params object[] data)
    {
        lateLoadData.AddRange(data);
        return this;
    }

    /// <summary>
    /// Executes the transition. Any configurable settings for this builder are locked, and can no longer be modified.
    /// </summary>
    /// <param name="sceneLoader">Scene Loader to operate on.</param>
    public IEnumerator Transition(ZenjectSceneLoader sceneLoader)
    {
        HasTransitioned = true;

        yield return sceneLoader.LoadSceneAsync(SceneName,
            LoadSceneMode,
            container => BindDataAndCallAction(container, EarlyBindingAction, earlyLoadData),
            LoadSceneRelationship,
            container => BindDataAndCallAction(container, LateBindingAction, lateLoadData));
    }

    private void BindDataAndCallAction(DiContainer container, Action<DiContainer> action, IEnumerable<object> data)
    {
        foreach (var obj in data)
        {
            container.Bind(obj.GetType()).FromInstance(obj);
        }
        action?.Invoke(container);
    }
}
