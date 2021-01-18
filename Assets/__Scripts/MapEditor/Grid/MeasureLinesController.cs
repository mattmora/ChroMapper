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
    private Dictionary<float, TextMeshProUGUI> measureTextsByBeat = new Dictionary<float, TextMeshProUGUI>();
    private Dictionary<float, bool> previousEnabledByBeat = new Dictionary<float, bool>();

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
        if (!measureTextsByBeat.Any())
        {
            measureTextsByBeat.Add(0, measureLinePrefab);
            previousEnabledByBeat.Add(0, true);
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
        Queue<TextMeshProUGUI> existing = new Queue<TextMeshProUGUI>(measureTextsByBeat.Values);
        measureTextsByBeat.Clear();
        previousEnabledByBeat.Clear();

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
            measureTextsByBeat.Add(jsonBeat, text);
            previousEnabledByBeat.Add(jsonBeat, true);

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

        foreach (KeyValuePair<float, TextMeshProUGUI> kvp in measureTextsByBeat)
        {
            bool enabled = kvp.Key >= offsetBeat - beatsBehind && kvp.Key <= offsetBeat + beatsAhead;
            if (previousEnabledByBeat[kvp.Key] != enabled)
            {
                kvp.Value.gameObject.SetActive(enabled);
                previousEnabledByBeat[kvp.Key] = enabled;
            }
        }
    }

    private void RefreshPositions()
    {
        foreach (KeyValuePair<float, TextMeshProUGUI> kvp in measureTextsByBeat)
        {
            kvp.Value.transform.localPosition = new Vector3(0, kvp.Key * EditorScaleController.EditorScale, 0);
        }
    }
}
