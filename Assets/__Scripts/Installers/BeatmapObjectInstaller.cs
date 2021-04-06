using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class BeatmapObjectInstaller : MonoInstaller
{
    [Header("Notes")]
    [SerializeField] private BeatmapNoteContainer notePrefab;
    [SerializeField] private Transform notesGrid;
    [Header("Obstacles")]
    [SerializeField] private BeatmapObstacleContainer obstaclePrefab;
    [SerializeField] private Transform obstaclesGrid;
    [Header("Events")]
    [SerializeField] private BeatmapEventContainer eventPrefab;
    [SerializeField] private Transform eventsGrid;
    [Header("BPM Changes")]
    [SerializeField] private BeatmapBPMChangeContainer bpmChangePrefab;
    [SerializeField] private Transform bpmChangesGrid;
    [Header("Custom Events")]
    [SerializeField] private BeatmapCustomEventContainer customEventPrefab;
    [SerializeField] private Transform customEventsGrid;

    public override void InstallBindings()
    {
        BindPool<BeatmapNoteContainer, BeatmapNoteContainer.Pool>(notePrefab, notesGrid);
        BindPool<BeatmapObstacleContainer, BeatmapObstacleContainer.Pool>(obstaclePrefab, obstaclesGrid);
        BindPool<BeatmapEventContainer, BeatmapEventContainer.Pool>(eventPrefab, eventsGrid);
        BindPool<BeatmapBPMChangeContainer, BeatmapBPMChangeContainer.Pool>(bpmChangePrefab, bpmChangesGrid);
        BindPool<BeatmapCustomEventContainer, BeatmapCustomEventContainer.Pool>(customEventPrefab, customEventsGrid);
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
