using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class EditorInstaller : MonoInstaller
{
    [SerializeField] private GameObject[] platformPrefabs;
    [SerializeField] private GameObject[] directionalPlatformPrefabs;

    [SerializeField] private BeatmapObjectCallbackController spawnCallback;
    [SerializeField] private BeatmapObjectCallbackController despawnCallback;

    [SerializeField] private GameObject spectrogramChunkPrefab;
    [SerializeField] private Transform spectrogramParentTransform;

    // These are all injected into the scene via the transition, so no need to re-bind them here.
    private BeatSaberSong loadedSong;
    private BeatSaberSong.DifficultyBeatmap loadedDifficultyBeatmap;
    private AudioClip loadedAudio;
    private BeatSaberMap loadedMap;
    private CustomPlatformsLoader customPlatformsLoader;

    [Inject]
    public void Construct(BeatSaberSong song, BeatSaberSong.DifficultyBeatmap diff, AudioClip audio, BeatSaberMap map, CustomPlatformsLoader customPlatformsLoader)
    {
        loadedSong = song;
        loadedDifficultyBeatmap = diff;
        loadedAudio = audio;
        loadedMap = map;
        this.customPlatformsLoader = customPlatformsLoader;
    }

    public override void InstallBindings()
    {
        // TODO Remove (this is temporary so I can remove all instances of BeatSaberSongContainer from earlier scenes and still have CM work)
        BeatSaberSongContainer.Instance.song = loadedSong;
        BeatSaberSongContainer.Instance.difficultyData = loadedDifficultyBeatmap;
        BeatSaberSongContainer.Instance.loadedSong = loadedAudio;
        BeatSaberSongContainer.Instance.map = loadedMap;
        
        // Loading platform
        var environmentID = SongInfoEditUI.GetEnvironmentIDFromString(loadedSong.environmentName);
        var customPlat = false;
        var directional = false;

        if (loadedSong.customData != null && !string.IsNullOrEmpty(loadedSong.customData["_customEnvironment"]))
        {
            if (customPlatformsLoader.GetAllEnvironmentIds().IndexOf(loadedSong.customData["_customEnvironment"]) >= 0)
            {
                customPlat = true;
            }
        }

        var characteristic = loadedDifficultyBeatmap.parentBeatmapSet.beatmapCharacteristicName;
        if (characteristic == "360Degree" || characteristic == "90Degree")
        {
            environmentID = SongInfoEditUI.GetDirectionalEnvironmentIDFromString(loadedSong.allDirectionsEnvironmentName);
            directional = true;
        }

        GameObject platform = customPlat ?
            customPlatformsLoader.LoadPlatform(loadedSong.customData["_customEnvironment"], platformPrefabs[environmentID]) :
            directional ?
                directionalPlatformPrefabs[environmentID] :
                platformPrefabs[environmentID];

        if (!customPlat)
        {
            platform = Instantiate(platform, LoadInitialMap.PlatformOffset, Quaternion.identity);
        }

        Container.QueueForInject(platform.GetComponent<PlatformDescriptor>());
        Container.Bind<PlatformDescriptor>().FromComponentOn(platform).AsSingle();

        // Callback controllers
        Container.BindInstance(nameof(Settings.Offset_Spawning)).WhenInjectedIntoInstance(spawnCallback);

        Container.BindInstance(nameof(Settings.Offset_Despawning)).WhenInjectedIntoInstance(despawnCallback);
        Container.BindInstance(-1).WhenInjectedIntoInstance(despawnCallback);

        // Factories
        Container.BindFactory<float[][], Texture2D, int, WaveformGenerator, Gradient, SpectrogramChunk, SpectrogramChunk.Factory>()
            .WithFactoryArguments(spectrogramChunkPrefab, spectrogramParentTransform);
    }
}
