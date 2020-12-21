using System.Linq;
using UnityEngine;
using Zenject;

public class CreateNewSong : MonoBehaviour
{
    [SerializeField] private SongList list;

    private SceneTransitionManager sceneTransitionManager;
    private PersistentUI persistentUI;

    [Inject]
    public void Construct(SceneTransitionManager sceneTransitionManager, PersistentUI persistentUI)
    {
        this.sceneTransitionManager = sceneTransitionManager;
        this.persistentUI = persistentUI;
    }

	public void CreateSong()
    {
        persistentUI.ShowInputBox("SongSelectMenu", "newmap.dialog", HandleNewSongName, "newmap.dialog.default");
    }

    private void HandleNewSongName(string res)
    {
        if (res is null) return;
        if (list.Songs.Any(x => x.songName == res))
        {
            persistentUI.ShowInputBox("SongSelectMenu", "newmap.dialog.duplicate", HandleNewSongName, "newmap.dialog.default");
            return;
        }

        BeatSaberSong song = new BeatSaberSong(list.WIPLevels, res);
        BeatSaberSong.DifficultyBeatmapSet standardSet = new BeatSaberSong.DifficultyBeatmapSet();
        song.difficultyBeatmapSets.Add(standardSet);
        BeatSaberSongContainer.Instance.SelectSongForEditing(song);

        sceneTransitionManager.LoadScene("02_SongEditMenu").WithDataInjectedEarly(song);
        persistentUI.DisplayMessage("SongSelectMenu", "newmap.message", PersistentUI.DisplayMessageType.BOTTOM);
    }
}
