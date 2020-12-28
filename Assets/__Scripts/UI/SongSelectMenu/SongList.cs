using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;
using Zenject;

public class SongList : MonoBehaviour
{
    private static int lastVisitedPage = 0;
    private static bool lastVisited_WasWIP = true;

    // For localization
    public int CurrentPage => currentPage + 1;
    public int MaxPage => maxPage + 1;

    public List<BeatSaberSong> Songs = new List<BeatSaberSong>();
    public bool WIPLevels = true;
    public bool FilteredBySearch = false;

    [SerializeField] private InputField searchField;
    [SerializeField] private SongListItem[] items;
    [SerializeField] private LocalizeStringEvent pageTextString;
    [SerializeField] private int currentPage = 0;
    [SerializeField] private int maxPage = 0;
    [SerializeField] private Button firstButton;
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button lastButton;
    [SerializeField] private LocalizeStringEvent songLocationToggleText;

    private IEnumerable<BeatSaberSong> filteredSongs = new List<BeatSaberSong>();

    private Settings settings;

    [Inject]
    private void Construct(Settings settings)
    {
        this.settings = settings;
        WIPLevels = lastVisited_WasWIP;
    }

    private void Start()
    {
        RefreshSongList();
    }

    public void ToggleSongLocation()
    {
        WIPLevels = !WIPLevels;
        lastVisited_WasWIP = WIPLevels;
        RefreshSongList();
    }

    public void RefreshSongList()
    {
        songLocationToggleText.StringReference.TableEntryReference = WIPLevels ? "custom" : "wip";

        FilteredBySearch = !string.IsNullOrEmpty(searchField.text);
        string[] directories;
        directories = Directory.GetDirectories(WIPLevels ? settings.CustomWIPSongsFolder : settings.CustomSongsFolder);
        Songs.Clear();

        foreach (var dir in directories)
        {
            BeatSaberSong song = BeatSaberSong.GetSongFromFolder(dir);
            if (song == null)
            {
                Debug.LogWarning($"No song at location {dir} exists! Is it in a subfolder?");
            }
            else
            {
                Songs.Add(song);
            }
        }

        // Sort by song name, and filter by search text.
        Songs = Songs.OrderBy(x => x.songName).ToList();
        maxPage = Mathf.Max(0, Mathf.CeilToInt((Songs.Count - 1) / items.Length));

        if (FilteredBySearch)
        {
            FilterBySearch();
        }
        else
        {
            filteredSongs = Songs;
            SetPage(lastVisitedPage);
        }
    }

    public void FilterBySearch()
    {
        filteredSongs = Songs.Where(x => searchField.text != "" ? x.songName.AllIndexOf(searchField.text).Any() : true);
        maxPage = Mathf.Max(0, Mathf.CeilToInt((filteredSongs.Count() - 1) / items.Length));
        SetPage(lastVisitedPage);
    }

    public void SetPage(int page)
    {
        if (page < 0 || page > maxPage)
        {
            page = 0;
        }

        lastVisitedPage = page;
        currentPage = page;
        LoadPage();
        pageTextString.StringReference.RefreshString();

        firstButton.interactable = currentPage != 0;
        prevButton.interactable = currentPage - 1 >= 0;
        nextButton.interactable = currentPage + 1 <= maxPage;
        lastButton.interactable = currentPage != maxPage;
    }

    public void LoadPage()
    {
        int offset = currentPage * items.Length;
        for (int i = 0; i < items.Count(); i++)
        {
            if (i + offset < filteredSongs.Count())
            {
                BeatSaberSong song = filteredSongs.ElementAt(i + offset);
                items[i].gameObject.SetActive(true);
                items[i].AssignSong(song);
            }
            else
            {
                items[i].gameObject.SetActive(false);
            }
        }
    }

    public void FirstPage() => SetPage(0);

    public void PrevPage() => SetPage(currentPage - 1);

    public void NextPage() => SetPage(currentPage + 1);

    public void LastPage() => SetPage(maxPage);
}
