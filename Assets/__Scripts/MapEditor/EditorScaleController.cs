using System;
using UnityEngine;
using Zenject;

public class EditorScaleController : MonoBehaviour
{

    public static float EditorScale = 4;
    public static Action<float> EditorScaleChangedEvent;

    private float PreviousEditorScale = -1;

    [SerializeField] private Transform moveableGridTransform;
    [SerializeField] private Transform[] scalingOffsets;
    
    private BeatmapObjectContainerCollection[] collections;
    
    private AudioTimeSyncController atsc;
    private Settings settings;
    private BeatSaberSong song;
    private BeatSaberSong.DifficultyBeatmap diff;

    [Inject]
    private void Construct(AudioTimeSyncController atsc, Settings settings, BeatSaberSong song, BeatSaberSong.DifficultyBeatmap diff)
    {
        this.atsc = atsc;
        this.settings = settings;
        this.song = song;
        this.diff = diff;
    }

    public void UpdateEditorScale(object value)
    {
        if (settings.NoteJumpSpeedForEditorScale) return;
        EditorScale = (float)value;
        if (PreviousEditorScale != EditorScale) Apply();
    }

    private void Apply()
    {
        foreach (BeatmapObjectContainerCollection collection in collections)
        {
            foreach (BeatmapObjectContainer b in collection.LoadedContainers.Values)
            {
                b.UpdateGridPosition();
            }
        }

        atsc.MoveToTimeInSeconds(atsc.CurrentSeconds);
        EditorScaleChangedEvent?.Invoke(EditorScale);
        PreviousEditorScale = EditorScale;
        foreach (Transform offset in scalingOffsets)
            offset.localScale = new Vector3(offset.localScale.x, offset.localScale.y, 8 * EditorScale);
    }

	// Use this for initialization
	private void Start ()
    {
        collections = moveableGridTransform.GetComponents<BeatmapObjectContainerCollection>();
        
        if (settings.NoteJumpSpeedForEditorScale)
        {
            CalculateNoteJumpSpeedScale();
        }
        else
        {
            PreviousEditorScale = EditorScale = settings.EditorScale;
        }

        Settings.NotifyBySettingName(nameof(Settings.EditorScale), UpdateEditorScale);
        Settings.NotifyBySettingName(nameof(Settings.NoteJumpSpeedForEditorScale), CalculateNoteJumpSpeedScale);
        Apply();
	}

    private void OnDestroy()
    {
        Settings.ClearSettingNotifications(nameof(Settings.EditorScale));
        Settings.ClearSettingNotifications(nameof(Settings.NoteJumpSpeedForEditorScale));
    }

    private void CalculateNoteJumpSpeedScale(object value = null)
    {
        var useNJSScale = (bool)value;

        if (!useNJSScale)
        {
            UpdateEditorScale(settings.EditorScale);
            return;
        }

        float bps = 60f / song.beatsPerMinute;
        float songNoteJumpSpeed = diff.noteJumpMovementSpeed;

        // When doing the math, it turns out that this all cancels out into what you see
        // We don't know where the hell 5/3 comes from, yay for magic numbers
        EditorScale = 5 / 3f * songNoteJumpSpeed * bps;
    }
}
