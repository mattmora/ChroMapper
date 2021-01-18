using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;
using UnityEngine.Localization.Components;
using System;

public class CountersPlusController : MonoBehaviour {

    [SerializeField] private NotesContainer notes;
    [SerializeField] private ObstaclesContainer obstacles;
    [SerializeField] private EventsContainer events;
    [SerializeField] private BPMChangesContainer bpm;
    [SerializeField] private AudioSource cameraAudioSource;
    [SerializeField] private AudioTimeSyncController atsc;

    [SerializeField] private LocalizeStringEvent notesMesh;
    [SerializeField] private LocalizeStringEvent notesPSMesh;
    [SerializeField] private LocalizeStringEvent[] allStrings;

    [SerializeField] private LocalizeStringEvent selectionString;

    private List<Func<object>> localizeStringArguments;

    private SwingsPerSecond swingsPerSecond;

    private void Start()
    {
        swingsPerSecond = new SwingsPerSecond(notes, obstacles);

        // I'm doing weird shit with stuff like this because StringReference.RefreshString() is pretty expensive.
        // Because Counters+ updates regularly, I want to limit string updates to only when the numbers actually change
        localizeStringArguments = new List<Func<object>>()
        {
            () => NotesCount,
            () => NPSCount,
            () => OverallSPS,
            () => ObstacleCount,
            () => EventCount,
            () => BPMCount,
            () => null, // Pretty much Time Spent Mapping will be the only one that continously updates
        };

        Settings.NotifyBySettingName("CountersPlus", ToggleCounters);
        ToggleCounters(Settings.Instance.CountersPlus);
        
        StartCoroutine(DelayedUpdate());
        StartCoroutine(CalculateSPS());
    }

    private IEnumerator CalculateSPS()
    {
        while (true)
        {
            yield return new WaitForSeconds(5);
            // Takes ~1ms, calculates red, blue and total stats (we only show total for now)
            swingsPerSecond.Update();
        }
    }

    private IEnumerator DelayedUpdate ()
    {
        while (true)
        {
            yield return new WaitForSeconds(1); //I wouldn't want to update this every single frame.

            if (!Settings.Instance.CountersPlus)
                continue;

            if (NotesSelected > 0)
            {
                notesMesh.StringReference.TableEntryReference = "countersplus.notes.selected";
                notesPSMesh.StringReference.TableEntryReference = "countersplus.nps.selected";
                localizeStringArguments[0] = () => NotesSelected;
                localizeStringArguments[1] = () => NPSselected;
            }
            else
            {
                notesMesh.StringReference.TableEntryReference = "countersplus.notes";
                notesPSMesh.StringReference.TableEntryReference = "countersplus.nps";
                localizeStringArguments[0] = () => NotesCount;
                localizeStringArguments[1] = () => NPSCount;
            }

            float timeMapping = BeatSaberSongContainer.Instance.map._time;
            seconds = Mathf.Abs(Mathf.FloorToInt(timeMapping * 60 % 60));
            minutes = Mathf.FloorToInt(timeMapping % 60);
            hours = Mathf.FloorToInt(timeMapping / 60);

            for (int i = 0; i < allStrings.Length; i++)
            {
                var str = allStrings[i];
                if (!str.StringReference.Arguments[0].Equals(localizeStringArguments[i]()))
                {
                    str.StringReference.RefreshString();
                }
            }
        }
	}

    private void Update() // i do want to update this every single frame
    {
        if (Application.isFocused) BeatSaberSongContainer.Instance.map._time += Time.deltaTime / 60; // only tick while application is focused

        selectionString.gameObject.SetActive(SelectionController.HasSelectedObjects());
        if (SelectionController.HasSelectedObjects()) // selected counter; does not rely on counters+ option
        {
            if (!selectionString.StringReference.Arguments[0].Equals(SelectedCount))
            {
                selectionString.StringReference.RefreshString();
            }
        }
    }

    public void ToggleCounters(object value)
    {
        bool enabled = (bool)value;
        foreach (Transform child in transform) child.gameObject.SetActive(enabled);
    }

    private void OnDestroy()
    {
        Settings.ClearSettingNotifications("CountersPlus");
    }

    ///// Localization /////

    public int NotesCount => notes.UnsortedObjects.Count;

    public float NPSCount => NotesCount / cameraAudioSource.clip.length;

    public int NotesSelected => SelectionController.SelectedObjects.Where(x => x is BeatmapNote).Count();

    public float NPSselected
    {
        get
        {
            var sel = SelectionController.SelectedObjects.OrderBy(it => it._time);
            float beatTimeDiff = sel.Last()._time - sel.First()._time;
            float secDiff = atsc.GetSecondsFromBeat(beatTimeDiff);

            return NotesSelected / secDiff;
        }
    }

    public int ObstacleCount => obstacles.UnsortedObjects.Count;

    public int EventCount => events.UnsortedObjects.Count;

    public int BPMCount => bpm.UnsortedObjects.Count;

    public int SelectedCount => SelectionController.SelectedObjects.Count;

    public float OverallSPS => swingsPerSecond?.Total?.Overall ?? 0;

    [HideInInspector]
    public int hours, minutes, seconds = 0;
}
