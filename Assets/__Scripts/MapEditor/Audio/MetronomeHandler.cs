using System;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class MetronomeHandler : MonoBehaviour
{
    public bool CowBell;

    [SerializeField] private AudioSource songAudioSource;
    [SerializeField] private AudioClip metronomeSound;
    [SerializeField] private AudioClip moreCowbellSound;
    [SerializeField] private AudioClip cowbellSound;
    [SerializeField] private AudioUtil audioUtil;
    [SerializeField] private GameObject metronomeUI;

    private float lastBPM = 100;
    private float beatProgress = 0;
    private BeatmapBPMChange lastBPMChange = null;
    private Animator metronomeUIAnimator;
    private static readonly int Bpm = Animator.StringToHash("BPM");
    private bool metronomeUIDirection = true;
    private bool CowBellPlayed;

    private float songSpeed = 1;

    private BeatSaberSong song;
    private AudioTimeSyncController atsc;
    private Settings settings;
    private BPMChangesContainer bpmChangesContainer;

    [Inject]
    private void Construct(BeatSaberSong song, AudioTimeSyncController atsc, Settings settings, BPMChangesContainer bpmChangesContainer)
    {
        this.song = song;
        this.atsc = atsc;
        this.settings = settings;
        this.bpmChangesContainer = bpmChangesContainer;
    }

    private void Start()
    {
        metronomeUIAnimator = metronomeUI.GetComponent<Animator>();
        Settings.NotifyBySettingName("SongSpeed", UpdateSongSpeed);

        lastBPM = song.beatsPerMinute;
        atsc.OnPlayToggle += OnPlayToggle;
    }

    private void UpdateSongSpeed(object value)
    {
        var speedValue = (float)Convert.ChangeType(value, typeof(float));
        songSpeed = speedValue / 10f;
    }

    private void OnDestroy()
    {
        atsc.OnPlayToggle -= OnPlayToggle;
    }

    private float metronomeVolume;
    
    private void LateUpdate()
    {
        if (CowBell && !CowBellPlayed)
        {
            audioUtil.PlayOneShotSound(moreCowbellSound);
            CowBellPlayed = true;
        }
        else if (!CowBell)
        {
            CowBellPlayed = false;
        }

        metronomeVolume = settings.MetronomeVolume;

        if (metronomeVolume != 0f && atsc.IsPlaying)
        {
            var toCheck = bpmChangesContainer.FindLastBPM(atsc.CurrentBeat);

            if (lastBPMChange != toCheck)
            {
                lastBPMChange = toCheck;
                lastBPM = lastBPMChange?._BPM ?? song.beatsPerMinute;
                audioUtil.PlayOneShotSound(CowBell ? cowbellSound : metronomeSound, settings.MetronomeVolume);
                RunAnimation();
                beatProgress = 0;
            }

            beatProgress += lastBPM / 60f * Time.deltaTime * songSpeed;
            if (!metronomeUI.activeInHierarchy) metronomeUI.SetActive(true);
            if (beatProgress >= 1)
            {
                beatProgress %= 1;
                audioUtil.PlayOneShotSound(CowBell ? cowbellSound : metronomeSound, settings.MetronomeVolume);
                RunAnimation();
            }
        }
        else metronomeUI.SetActive(false);
    }

    private void RunAnimation()
    {
        if (!metronomeUIAnimator.gameObject.activeInHierarchy)
            return;

        metronomeUIAnimator.StopPlayback();
        metronomeUIAnimator.SetFloat(Bpm, Mathf.Abs(lastBPM * songAudioSource.pitch));
        metronomeUIAnimator.Play(metronomeUIDirection ? "Metronome_R2L" : "Metronome_L2R");
        metronomeUIDirection = !metronomeUIDirection;
    }

    void OnPlayToggle(bool playing)
    {
        if (metronomeVolume == 0) return;
        if (playing)
        {
            RunAnimation();

            lastBPMChange = bpmChangesContainer.FindLastBPM(atsc.CurrentBeat);
            lastBPM = lastBPMChange?._BPM ?? song.beatsPerMinute;

            if (lastBPMChange != null)
            {
                float differenceInSongBPM = atsc.CurrentBeat - lastBPMChange._time;
                float differenceInLastBPM = differenceInSongBPM * lastBPMChange._BPM / song.beatsPerMinute;
                beatProgress = differenceInLastBPM % 1;
            }
            else
            {
                beatProgress = atsc.CurrentBeat % 1;
            }
        }
    }
}
