using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class EditorInstaller : MonoInstaller
{
    // These are all injected into the scene via the transition, so no need to re-bind them here.
    private BeatSaberSong loadedSong;
    private BeatSaberSong.DifficultyBeatmap loadedDifficultyBeatmap;
    private AudioClip loadedAudio;
    private BeatSaberMap loadedMap;

    [Inject]
    public void Construct(BeatSaberSong song, BeatSaberSong.DifficultyBeatmap diff, AudioClip audio, BeatSaberMap map)
    {
        loadedSong = song;
        loadedDifficultyBeatmap = diff;
        loadedAudio = audio;
        loadedMap = map;
    }

    public override void InstallBindings()
    {
        // TODO Remove (this is temporary so I can remove all instances of BeatSaberSongContainer from earlier scenes and still have CM work)
        BeatSaberSongContainer.Instance.song = loadedSong;
        BeatSaberSongContainer.Instance.difficultyData = loadedDifficultyBeatmap;
        BeatSaberSongContainer.Instance.loadedSong = loadedAudio;
        BeatSaberSongContainer.Instance.map = loadedMap;
    }
}
