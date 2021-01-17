using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class LoadInitialMap : MonoBehaviour
{

    [SerializeField] AudioTimeSyncController atsc;

    public static Action<PlatformDescriptor> PlatformLoadedEvent;
    public static Action LevelLoadedEvent;
    public static readonly Vector3 PlatformOffset = new Vector3(0, -0.5f, -1.5f);

    private PlatformDescriptor loadedPlatform;

    [Inject]
    private void Construct(PlatformDescriptor loadedPlatform)
    {
        this.loadedPlatform = loadedPlatform;
    }

    private void Start()
    {
        PlatformLoadedEvent.Invoke(loadedPlatform); //Trigger event for classes that use the platform

        LevelLoadedEvent?.Invoke();
    }
}
