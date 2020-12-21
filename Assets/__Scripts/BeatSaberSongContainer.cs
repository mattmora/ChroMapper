using UnityEngine;

// TODO: Remove when all classes request the specific objects via zenject
public class BeatSaberSongContainer : MonoBehaviour
{
    public static BeatSaberSongContainer Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null) {
            Destroy(Instance.gameObject);
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public BeatSaberSong song;
    public BeatSaberSong.DifficultyBeatmap difficultyData;
    public AudioClip loadedSong;
    public BeatSaberMap map;

    public void SelectSongForEditing(BeatSaberSong song)
    {
        this.song = song;
    }
}
