using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using Zenject;

public class RefreshMapController : MonoBehaviour, CMInput.IRefreshMapActions
{
    [SerializeField] private MapLoader loader;
    [SerializeField] private TracksManager tracksManager;
    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private TMP_FontAsset cancelFontAsset;
    [SerializeField] private TMP_FontAsset moreOptionsFontAsset;
    [SerializeField] private TMP_FontAsset thingYouCanRefreshFontAsset;

    private BeatSaberSong song;
    private BeatSaberSong.DifficultyBeatmap diff;
    private BeatSaberMap map;
    private SelectionController selection;
    private PersistentUI persistentUI;

    [Inject]
    private void Construct(BeatSaberSong song, BeatSaberSong.DifficultyBeatmap diff, BeatSaberMap map, SelectionController selection, PersistentUI persistentUI)
    {
        this.song = song;
        this.diff = diff;
        this.map = map;
        this.selection = selection;
        this.persistentUI = persistentUI;
    }

    public void InitiateRefreshConversation()
    {
        PersistentUI.Instance.ShowDialogBox("Mapper", "refreshmap",
            HandleFirstLayerConversation, new string[] { "refreshmap.notes", "refreshmap.walls", "refreshmap.events", "refreshmap.other", "refreshmap.full", "refreshmap.cancel" },
            new TMP_FontAsset[] { thingYouCanRefreshFontAsset, thingYouCanRefreshFontAsset, thingYouCanRefreshFontAsset, thingYouCanRefreshFontAsset, thingYouCanRefreshFontAsset, cancelFontAsset });
    }

    private void HandleFirstLayerConversation(int res)
    {
        switch (res)
        {
            case 0:
                StartCoroutine(RefreshMap(true, false, false, false, false));
                break;
            case 1:
                StartCoroutine(RefreshMap(false, true, false, false, false));
                break;
            case 2:
                StartCoroutine(RefreshMap(false, false, true, false, false));
                break;
            case 3:
                StartCoroutine(RefreshMap(false, false, false, true, false));
                break;
            case 4:
                StartCoroutine(RefreshMap(false, false, false, false, true));
                break;
        }
    }

    private IEnumerator RefreshMap(bool notes, bool obstacles, bool events, bool others, bool full)
    {
        yield return persistentUI.FadeInLoadingScreen();

        map = song.GetMapFromDifficultyBeatmap(diff);
        loader.UpdateMapData(map);

        var currentBeat = atsc.CurrentBeat;
        atsc.MoveToTimeInBeats(0);

        if (full)
        {
            yield return StartCoroutine(loader.HardRefresh());
        }
        else
        {
            if (notes) yield return StartCoroutine(loader.LoadObjects(map._notes));
            if (obstacles) yield return StartCoroutine(loader.LoadObjects(map._obstacles));
            if (events) yield return StartCoroutine(loader.LoadObjects(map._events));
            if (others)
            {
                yield return StartCoroutine(loader.LoadObjects(map._BPMChanges));
                yield return StartCoroutine(loader.LoadObjects(map._customEvents));
            }
        }

        tracksManager.RefreshTracks();
        selection.RefreshMap();
        atsc.MoveToTimeInBeats(currentBeat);

        yield return persistentUI.FadeOutLoadingScreen();
    }

    public void OnRefreshMap(InputAction.CallbackContext context)
    {
        if (context.performed)
            InitiateRefreshConversation();
    }
}
