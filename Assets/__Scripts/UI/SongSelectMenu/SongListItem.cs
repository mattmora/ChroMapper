using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Zenject;

public class SongListItem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI text;
    [SerializeField] private Button button;

    private BeatSaberSong song;
    private SceneTransitionManager sceneTransitionManager;

    [Inject]
    public void Construct(SceneTransitionManager sceneTransitionManager)
    {
        this.sceneTransitionManager = sceneTransitionManager;
    }

    public void AssignSong(BeatSaberSong song)
    {
        this.song = song;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(ButtonClicked);
        text.text = $"{song.songName.StripTMPTags()} <size=50%><i>{song.songSubName.StripTMPTags()}</i></size>";
    }

    void ButtonClicked()
    {
        Debug.Log("Edit button for song " + song.songName);

        if (song != null)
        {
            sceneTransitionManager.LoadScene("02_SongEditMenu").WithDataInjectedEarly(song);
        }
    }
}
