using System;
using UnityEngine;
using Zenject;

public class BeatmapCustomEventContainer : BeatmapObjectContainer
{
    public override BeatmapObject objectData { get => customEventData; set => customEventData = (BeatmapCustomEvent)value; }
    public BeatmapCustomEvent customEventData;

    private CustomEventsContainer collection;

    [Inject]
    public void Construct(CustomEventsContainer collection)
    {
        this.collection = collection;
    }

    public override void UpdateGridPosition()
    {
        transform.localPosition = new Vector3(
            collection.CustomEventTypes.IndexOf(customEventData._type), 0.5f, customEventData._time * EditorScaleController.EditorScale);
    }

    public class Pool : BeatmapObjectCollectionPool<BeatmapCustomEvent, BeatmapCustomEventContainer> { }
}
