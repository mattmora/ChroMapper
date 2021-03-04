using System;
using System.IO;
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

    private void HandleNewSongName(string newSongName)
    {
        if (string.IsNullOrWhiteSpace(newSongName) || string.IsNullOrEmpty(newSongName)) return;

        var directory = list.WIPLevels ? settings.CustomWIPSongsFolder : settings.CustomSongsFolder;
        var newSong = new BeatSaberSong(directory, newSongName);

        if (list.Songs.Any(x => Path.GetFullPath(x.directory) == Path.GetFullPath(newSong.directory)))
        {
            persistentUI.ShowInputBox("SongSelectMenu", "newmap.dialog.duplicate", HandleNewSongName, "newmap.dialog.default");
            return;
        }

        var standardSet = new BeatSaberSong.DifficultyBeatmapSet();
        newSong.difficultyBeatmapSets.Add(standardSet);
        
        sceneTransitionManager.LoadScene("02_SongEditMenu").WithDataInjectedEarly(newSong);
        persistentUI.DisplayMessage("SongSelectMenu", "newmap.message", PersistentUI.DisplayMessageType.BOTTOM);
    }
}
