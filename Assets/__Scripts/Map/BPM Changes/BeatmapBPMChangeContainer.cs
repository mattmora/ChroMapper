using System;
using System.Globalization;
using TMPro;
using UnityEngine;

public class BeatmapBPMChangeContainer : BeatmapObjectContainer {

    public override BeatmapObject objectData { get => bpmData; set => bpmData = (BeatmapBPMChange)value; }

    public BeatmapBPMChange bpmData;

    [SerializeField] private TextMeshProUGUI bpmText;

    public void UpdateBPMText()
    {
        bpmText.text = bpmData._BPM.ToString(CultureInfo.InvariantCulture);
    }

    public override void UpdateGridPosition()
    {
        transform.localPosition = new Vector3(0.5f, 0.5f, bpmData._time * EditorScaleController.EditorScale);
        bpmText.text = bpmData._BPM.ToString(CultureInfo.InvariantCulture);
    }

    public class Pool : BeatmapObjectCollectionPool<BeatmapBPMChange, BeatmapBPMChangeContainer> { }
}
