using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zenject;

//Name and idea totally not stolen directly from Beat Saber
public class BeatmapObjectCallbackController : MonoBehaviour
{
    private static int eventsToLookAhead = 75;
    private static int notesToLookAhead = 25;

    public Action<bool, int, BeatmapObject> NotePassedThreshold;
    public Action<bool, int, BeatmapObject> EventPassedThreshold;
    public Action<bool, int> RecursiveNoteCheckFinished;
    public Action<bool, int> RecursiveEventCheckFinished;
    
    public float offset = 0;

    [SerializeField] private NotesContainer notesContainer;
    [SerializeField] private EventsContainer eventsContainer;

    [SerializeField] private int nextNoteIndex = 0;
    [SerializeField] private int nextEventIndex = 0;
    
    private List<BeatmapObject> nextEvents = new List<BeatmapObject>();
    private Queue<BeatmapObject> allEvents = new Queue<BeatmapObject>();
    private List<BeatmapObject> nextNotes = new List<BeatmapObject>();
    private Queue<BeatmapObject> allNotes = new Queue<BeatmapObject>();

    private float curTime;

    private AudioTimeSyncController timeSyncController;
    private string settingsName = null;
    private int multiplier = 1;
    private Settings settings;

    [Inject]
    private void Construct(AudioTimeSyncController atsc, Settings settings, [InjectOptional] string settingsName = null, [InjectOptional] int multiplier = 1)
    {
        timeSyncController = atsc;
        this.settingsName = settingsName;
        this.multiplier = multiplier;
        this.settings = settings;
    }

    private void OnEnable()
    {
        timeSyncController.OnPlayToggle += OnPlayToggle;

        if (settingsName != null)
        {
            Settings.NotifyBySettingName(settingsName, UpdateOffset);
            // Bit of a weird way to get the initial value but oh well?
            UpdateOffset(Settings.AllFieldInfos[settingsName].GetValue(settings));
        }
    }

    private void UpdateOffset(object value)
    {
        offset = (int)value * multiplier;
    }

    private void OnDisable()
    {
        timeSyncController.OnPlayToggle -= OnPlayToggle;

        if (settingsName != null)
        {
            Settings.ClearSettingNotifications(settingsName);
        }
    }

    private void OnPlayToggle(bool playing)
    {
        if (playing)
        {
            CheckAllNotes(false);
            CheckAllEvents(false);
        }
    }

    private void LateUpdate()
    {
        if (timeSyncController.IsPlaying)
        {
            curTime = timeSyncController.CurrentBeat;
            RecursiveCheckNotes(true, true);
            RecursiveCheckEvents(true, true);
        }
    }

    private void CheckAllNotes(bool natural)
    {
        //notesContainer.SortObjects();
        curTime = timeSyncController.CurrentBeat;
        allNotes.Clear();
        allNotes = new Queue<BeatmapObject>(notesContainer.LoadedObjects.Where(o => o._time >= curTime + offset));
        
        nextNoteIndex = notesContainer.LoadedObjects.Count - allNotes.Count;
        RecursiveNoteCheckFinished?.Invoke(natural, nextNoteIndex - 1);
        nextNotes.Clear();
        for (int i = 0; i < notesToLookAhead; i++)
            if (allNotes.Any()) nextNotes.Add(allNotes.Dequeue());
    }

    private void CheckAllEvents(bool natural)
    {
        allEvents.Clear();
        allEvents = new Queue<BeatmapObject>(eventsContainer.LoadedObjects.Where(o => o._time >= curTime + offset));
               
        nextEventIndex = eventsContainer.LoadedObjects.Count - allEvents.Count;
        RecursiveEventCheckFinished?.Invoke(natural, nextEventIndex - 1);
        nextEvents.Clear();
        for (int i = 0; i < eventsToLookAhead; i++)
            if (allEvents.Any()) nextEvents.Add(allEvents.Dequeue());
    }

    private void RecursiveCheckNotes(bool init, bool natural)
    {
        List<BeatmapObject> passed = new List<BeatmapObject>(nextNotes.Where(x => x._time <= curTime + offset));
        foreach (BeatmapObject newlyAdded in passed)
        {
            if (natural) NotePassedThreshold?.Invoke(init, nextNoteIndex, newlyAdded);
            nextNotes.Remove(newlyAdded);
            if (allNotes.Any() && natural) nextNotes.Add(allNotes.Dequeue());
            nextNoteIndex++;
        }
    }

    private void RecursiveCheckEvents(bool init, bool natural)
    {
        List<BeatmapObject> passed = new List<BeatmapObject>(nextEvents.Where(x => x._time <= curTime + offset));
        foreach (BeatmapObject newlyAdded in passed)
        {
            if (natural) EventPassedThreshold?.Invoke(init, nextEventIndex, newlyAdded);
            nextEvents.Remove(newlyAdded);
            if (allEvents.Any() && natural) nextEvents.Add(allEvents.Dequeue());
            nextEventIndex++;
        }
    }
}
