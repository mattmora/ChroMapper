using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class BeatmapObjectInstaller : MonoInstaller
{
    [SerializeField] private BeatmapEventContainer eventPrefab;
    [SerializeField] private Transform eventsGrid;

    public override void InstallBindings()
    {
        BindPool<BeatmapEventContainer, BeatmapEventContainer.Pool>(eventPrefab, eventsGrid);
    }

    private void BindPool<TContainer, TPool>(TContainer prefab, Transform parent)
        where TContainer : BeatmapObjectContainer
        where TPool : IMemoryPool
    {
        Container.BindMemoryPool<TContainer, TPool>()
            .FromComponentInNewPrefab(prefab)
            .UnderTransform(parent);
    }
}
