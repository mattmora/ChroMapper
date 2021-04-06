using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zenject;

public class DingOnNotePassingGrid : MonoBehaviour
{
    public static Dictionary<int, bool> NoteTypeToDing = new Dictionary<int, bool>()
    {
        { BeatmapNote.NOTE_TYPE_A, true },
        { BeatmapNote.NOTE_TYPE_B, true },
        { BeatmapNote.NOTE_TYPE_BOMB, false },
    };

    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private AudioSource songAudioSource;
    [SerializeField] private AudioSource source;
    [SerializeField] private SoundList[] soundLists;
    [SerializeField] private int DensityCheckOffset = 2;
    [SerializeField] private float ThresholdInNoteTime = 0.25f;
    [SerializeField] private AudioUtil audioUtil;
    [SerializeField] private NotesContainer container;
    [SerializeField] private BeatmapObjectCallbackController defaultCallbackController;
    [SerializeField] private BeatmapObjectCallbackController beatSaberCutCallbackController;
    [SerializeField] private BongoCat bongocat;
    [SerializeField] private GameObject discordPingPrefab;

    //debug
    [SerializeField] float difference;

    private float offset;

    private float lastCheckedTime;
    private float songSpeed = 1;

    private Settings settings;

    [Inject]
    private void Construct(Settings settings)
    {
        this.settings = settings;
    }

    private void Start()
    {
        Settings.NotifyBySettingName("Ding_Red_Notes", UpdateRedNoteDing);
        Settings.NotifyBySettingName("Ding_Blue_Notes", UpdateBlueNoteDing);
        Settings.NotifyBySettingName("Ding_Bombs", UpdateBombDing);
        Settings.NotifyBySettingName("NoteHitSound", UpdateHitSoundType);
        Settings.NotifyBySettingName("SongSpeed", UpdateSongSpeed);

        NoteTypeToDing[BeatmapNote.NOTE_TYPE_A] = settings.Ding_Red_Notes;
        NoteTypeToDing[BeatmapNote.NOTE_TYPE_B] = settings.Ding_Blue_Notes;
        NoteTypeToDing[BeatmapNote.NOTE_TYPE_BOMB] = settings.Ding_Bombs;

        beatSaberCutCallbackController.offset = atsc.GetBeatFromSeconds(0.5f);

        UpdateHitSoundType(settings.NoteHitSound);

        atsc.OnPlayToggle += OnPlayToggle;
    }

    private void UpdateSongSpeed(object value)
    {
        var speedValue = (float)Convert.ChangeType(value, typeof(float));
        songSpeed = speedValue / 10f;
    }

    private void OnPlayToggle(bool playing)
    {
        lastCheckedTime = -1;
        audioUtil.StopOneShot();
        if (playing)
        {
            var notes = container.GetBetween(atsc.CurrentBeat, atsc.CurrentBeat + beatSaberCutCallbackController.offset);

            // Schedule notes between now and threshold
            foreach (var n in notes)
            {
                PlaySound(false, 0, n);
            }
        }
    }

    private void OnDestroy()
    {
        atsc.OnPlayToggle -= OnPlayToggle;
    }

    private void UpdateRedNoteDing(object obj)
    {
        NoteTypeToDing[BeatmapNote.NOTE_TYPE_A] = (bool)obj;
    }

    private void UpdateBlueNoteDing(object obj)
    {
        NoteTypeToDing[BeatmapNote.NOTE_TYPE_B] = (bool)obj;
    }

    private void UpdateBombDing(object obj)
    {
        NoteTypeToDing[BeatmapNote.NOTE_TYPE_BOMB] = (bool)obj;
    }

    private void UpdateHitSoundType(object obj)
    {
        int soundID = (int)obj;
        var isBeatSaberCutSound = soundID == (int)HitSounds.SLICE;

        if (isBeatSaberCutSound)
        {
            offset = 0.18f;
        }
        else
        {
            offset = 0;
        }
    }

    private void OnDisable()
    {
        beatSaberCutCallbackController.NotePassedThreshold -= PlaySound;
        defaultCallbackController.NotePassedThreshold -= TriggerBongoCat;

        Settings.ClearSettingNotifications("Ding_Red_Notes");
        Settings.ClearSettingNotifications("Ding_Blue_Notes");
        Settings.ClearSettingNotifications("Ding_Bombs");
        Settings.ClearSettingNotifications("NoteHitSound");
        Settings.ClearSettingNotifications("SongSpeed");
    }

    private void OnEnable()
    {
        Settings.NotifyBySettingName("Ding_Red_Notes", UpdateRedNoteDing);
        Settings.NotifyBySettingName("Ding_Blue_Notes", UpdateBlueNoteDing);
        Settings.NotifyBySettingName("Ding_Bombs", UpdateBombDing);
        Settings.NotifyBySettingName("NoteHitSound", UpdateHitSoundType);
        Settings.NotifyBySettingName("SongSpeed", UpdateSongSpeed);

        beatSaberCutCallbackController.NotePassedThreshold += PlaySound;
        defaultCallbackController.NotePassedThreshold += TriggerBongoCat;
    }

    void TriggerBongoCat(bool initial, int index, BeatmapObject objectData)
    {
        // Filter notes that are too far behind the current beat
        if (objectData._time - atsc.CurrentBeat <= -0.5f) return;

        var soundListId = settings.NoteHitSound;
        if (soundListId == (int)HitSounds.DISCORD)
        {
            Instantiate(discordPingPrefab, gameObject.transform, true);
        }

        // bongo cat
        bongocat.TriggerArm(objectData as BeatmapNote, container);
    }

    void PlaySound(bool initial, int index, BeatmapObject objectData)
    {
        // Filter notes that are too far behind the current beat
        if (objectData._time - atsc.CurrentBeat <= -0.5f) return;

        //actual ding stuff
        if (objectData._time == lastCheckedTime || !NoteTypeToDing[((BeatmapNote) objectData)._type]) return;
        /*
         * As for why we are not using "initial", it is so notes that are not supposed to ding do not prevent notes at
         * the same time that are supposed to ding from triggering the sound effects.
         */
        lastCheckedTime = objectData._time;
        var soundListId = settings.NoteHitSound;
        var list = soundLists[soundListId];

        var shortCut = false;
        if (index - DensityCheckOffset > 0 && index + DensityCheckOffset < container.LoadedObjects.Count)
        {
            BeatmapObject first = container.LoadedObjects.ElementAt(index + DensityCheckOffset);
            BeatmapObject second = container.LoadedObjects.ElementAt(index - DensityCheckOffset);
            if (first != null && second != null)
            {
                if (first._time - objectData._time <= ThresholdInNoteTime && objectData._time - second._time <= ThresholdInNoteTime)
                    shortCut = true;
            }
        }

        var timeUntilDing = objectData._time - atsc.GetBeatFromSeconds(songAudioSource.time);
        var hitTime = (atsc.GetSecondsFromBeat(timeUntilDing) / songSpeed) - offset;
        audioUtil.PlayOneShotSound(list.GetRandomClip(shortCut), settings.NoteHitVolume, 1, hitTime);
    }

}
