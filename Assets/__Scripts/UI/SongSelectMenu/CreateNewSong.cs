using System.Linq;
using UnityEngine;
using Zenject;

public class CreateNewSong : MonoBehaviour
{
    [SerializeField] private SongList list;

    private SceneTransitionManager sceneTransitionManager;
    private PersistentUI persistentUI;
    private Settings settings;

    [Inject]
    public void Construct(SceneTransitionManager sceneTransitionManager, PersistentUI persistentUI, Settings settings)
    {
        this.sceneTransitionManager = sceneTransitionManager;
        this.persistentUI = persistentUI;
        this.settings = settings;
    }

	public void CreateSong()
    {
        persistentUI.ShowInputBox("SongSelectMenu", "newmap.dialog", HandleNewSongName, "newmap.dialog.default");
    }

    private void HandleNewSongName(string res)
    {
        if (string.IsNullOrWhiteSpace(res) || string.IsNullOrEmpty(res)) return;

        if (list.Songs.Any(x => x.songName == res))
        {
            persistentUI.ShowInputBox("SongSelectMenu", "newmap.dialog.duplicate", HandleNewSongName, "newmap.dialog.default");
            return;
        }

        var dir = list.WIPLevels ? settings.CustomWIPSongsFolder : settings.CustomSongsFolder;

        BeatSaberSong song = new BeatSaberSong(dir, res);
        BeatSaberSong.DifficultyBeatmapSet standardSet = new BeatSaberSong.DifficultyBeatmapSet();
        song.difficultyBeatmapSets.Add(standardSet);
        
        sceneTransitionManager.LoadScene("02_SongEditMenu").WithDataInjectedEarly(song);
        persistentUI.DisplayMessage("SongSelectMenu", "newmap.message", PersistentUI.DisplayMessageType.BOTTOM);
    }
}
