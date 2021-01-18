using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;
using Zenject;

public class MeasureLinesController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI measureLinePrefab;
    [SerializeField] private RectTransform parent;
    [SerializeField] private Transform noteGrid;
    [SerializeField] private Transform frontNoteGridScaling;
    [SerializeField] private Transform measureLineGrid;
    [SerializeField] private GridChild measureLinesGridChild;

    private float previousATSCBeat = -1;
    private List<(float beat, TextMeshProUGUI text, bool previousState)> measureTexts = new List<(float, TextMeshProUGUI, bool)>();

    private bool init = false;

    private AudioTimeSyncController atsc;
    private BPMChangesContainer bpmChangesContainer;
    private BeatSaberSong song;
    private AudioClip loadedClip;

    [Inject]
    private void Construct(AudioTimeSyncController atsc, BPMChangesContainer bpmChangesContainer, BeatSaberSong song, AudioClip loadedClip)
    {
        this.atsc = atsc;
        this.bpmChangesContainer = bpmChangesContainer;
        this.song = song;
        this.loadedClip = loadedClip;
    }

    private void Start()
    {
        if (!measureTexts.Any())
        {
            measureTexts.Add((0, measureLinePrefab, true));
        }
        EditorScaleController.EditorScaleChangedEvent += EditorScaleUpdated;
    }

    private void OnDestroy()
    {
        EditorScaleController.EditorScaleChangedEvent -= EditorScaleUpdated;
    }

    private void EditorScaleUpdated(float obj)
    {
        RefreshPositions();
    }

    public void RefreshMeasureLines()
    {
        init = false;
        Queue<TextMeshProUGUI> existing = new Queue<TextMeshProUGUI>(measureTexts.Select(m => m.text));
        measureTexts.Clear();

        int rawBeatsInSong = Mathf.FloorToInt(atsc.GetBeatFromSeconds(loadedClip.length));
        float jsonBeat = 0;
        int modifiedBeats = 0;
        float songBPM = song.beatsPerMinute;

        List<BeatmapBPMChange> allBPMChanges = new List<BeatmapBPMChange>()
        {
            new BeatmapBPMChange(songBPM, 0)
        };
        allBPMChanges.AddRange(bpmChangesContainer.LoadedObjects.Cast<BeatmapBPMChange>());

        while (jsonBeat <= rawBeatsInSong)
        {
            var text = existing.Count > 0 ? existing.Dequeue() : Instantiate(measureLinePrefab, parent);
            text.text = $"{modifiedBeats}";
            text.transform.localPosition = new Vector3(0, jsonBeat * EditorScaleController.EditorScale, 0);
            measureTexts.Add((jsonBeat, text, true));

            modifiedBeats++;
            var last = allBPMChanges.Last(x => x._Beat <= modifiedBeats);
            jsonBeat = ((modifiedBeats - last._Beat) / last._BPM * songBPM) + last._time;
        }

        // Set proper spacing between Notes grid, Measure lines, and Events grid
        measureLinesGridChild.Size = modifiedBeats > 1000 ? 1 : 0;

        foreach (TextMeshProUGUI leftovers in existing)
        {
            Destroy(leftovers.gameObject);
        }

        init = true;
        RefreshVisibility();
        RefreshPositions();
    }

    void LateUpdate()
    {
        if (atsc.CurrentBeat == previousATSCBeat || !init) return;
        previousATSCBeat = atsc.CurrentBeat;
        RefreshVisibility();
    }

    private void RefreshVisibility()
    {
        float offsetBeat = atsc.CurrentBeat - atsc.offsetBeat;
        float beatsAhead = frontNoteGridScaling.localScale.z / EditorScaleController.EditorScale;
        float beatsBehind = beatsAhead / 4f;

        for (int i = 0; i < measureTexts.Count; i++)
        {
            var (beat, text, previousState) = measureTexts[i];

            bool enabled = beat >= offsetBeat - beatsBehind && beat <= offsetBeat + beatsAhead;
            
            if (previousState != enabled)
            {
                text.enabled = enabled;
                measureTexts[i] = (beat, text, enabled);
            }
        }
    }

    private void RefreshPositions()
    {
        measureTexts.ForEach(measure => measure.text.transform.localPosition = Vector3.up * measure.beat * EditorScaleController.EditorScale);
    }
}
