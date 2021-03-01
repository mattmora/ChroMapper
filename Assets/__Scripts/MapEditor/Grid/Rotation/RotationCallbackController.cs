using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zenject;

public class RotationCallbackController : MonoBehaviour
{
    public bool IsActive { get; private set; }
    private readonly string[] enabledCharacteristics = { "360Degree", "90Degree", "Lawless" };

    [SerializeField] private BeatmapObjectCallbackController interfaceCallback;
    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private EventsContainer events;

    public Action<bool, int> RotationChangedEvent; //Natural, degrees
    public MapEvent LatestRotationEvent { get; private set; }

    public int Rotation { get; private set; }

    private Settings settings;
    private PersistentUI persistentUI;
    private BeatSaberSong.DifficultyBeatmap diff;

    [Inject]
    private void Construct(Settings settings, PersistentUI persistentUI, BeatSaberSong.DifficultyBeatmap diff)
    {
        this.settings = settings;
        this.persistentUI = persistentUI;
        this.diff = diff;
    }

    // Start is called before the first frame update
    internal void Start()
    {
        var set = diff.parentBeatmapSet;
        
        IsActive = enabledCharacteristics.Contains(set.beatmapCharacteristicName);
        
        if (IsActive && settings.Reminder_Loading360Levels)
        {
            persistentUI.ShowDialogBox(
                "PersistentUI", "360warning"
                , Handle360LevelReminder, PersistentUI.DialogBoxPresetType.OkIgnore);
        }

        interfaceCallback.EventPassedThreshold += EventPassedThreshold;
        atsc.OnPlayToggle += PlayToggle;
        atsc.OnTimeChanged += OnTimeChanged;
        Settings.NotifyBySettingName("RotateTrack", UpdateRotateTrack);
    }

    private void UpdateRotateTrack(object obj)
    {
        if (settings.RotateTrack) return;
        RotationChangedEvent?.Invoke(false, 0);
    }

    private void Handle360LevelReminder(int res)
    {
        settings.Reminder_Loading360Levels = res == 0;
    }

    private void OnTimeChanged()
    {
        if (atsc.IsPlaying) return;
        PlayToggle(false);
    }

    private void PlayToggle(bool isPlaying)
    {
        if (!IsActive) return;
        float time = atsc.CurrentBeat;
        IEnumerable<MapEvent> rotations = events.AllRotationEvents.Where(x => x._time < time || (x._time == time && x._type == MapEvent.EVENT_TYPE_EARLY_ROTATION));
        Rotation = 0;
        if (rotations.Count() > 0)
        {
            Rotation = rotations.Sum(x => x.GetRotationDegreeFromValue() ?? 0);
            LatestRotationEvent = rotations.LastOrDefault();
        }
        else LatestRotationEvent = null;
        RotationChangedEvent.Invoke(false, Rotation);
    }

    private void EventPassedThreshold(bool initial, int index, BeatmapObject obj)
    {
        MapEvent e = obj as MapEvent;
        if (e is null || !IsActive || (e == LatestRotationEvent && e._type == MapEvent.EVENT_TYPE_EARLY_ROTATION) || !e.IsRotationEvent) return;
        int rotationValue = e.GetRotationDegreeFromValue() ?? 0;
        Rotation += rotationValue;
        LatestRotationEvent = e;
        RotationChangedEvent.Invoke(true, Rotation);
    }

    private void OnDestroy()
    {
        interfaceCallback.EventPassedThreshold -= EventPassedThreshold;
        atsc.OnPlayToggle -= PlayToggle;
        atsc.OnTimeChanged -= OnTimeChanged;
        Settings.ClearSettingNotifications("RotateTrack");
    }
}
