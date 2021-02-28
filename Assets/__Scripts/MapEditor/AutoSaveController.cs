using SimpleJSON;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Zenject;

public class AutoSaveController : MonoBehaviour, CMInput.ISavingActions
{
    private const int MAXIMUM_AUTOSAVE_COUNT = 15;

    [SerializeField] private Toggle autoSaveToggle;

    private Thread savingThread = null;
    private float t = 0;
    private List<DirectoryInfo> currentAutoSaves = new List<DirectoryInfo>();

    private Settings settings;
    private BeatSaberSong song;
    private BeatSaberSong.DifficultyBeatmap diff;
    private BeatSaberMap map;
    private PersistentUI persistentUI;
    private SelectionController selection;

    [Inject]
    private void Construct(Settings settings, BeatSaberSong song, BeatSaberSong.DifficultyBeatmap diff, BeatSaberMap map, PersistentUI persistentUI,
        SelectionController selection)
    {
        this.settings = settings;
        this.song = song;
        this.diff = diff;
        this.map = map;
        this.persistentUI = persistentUI;
        this.selection = selection;
    }

    public void ToggleAutoSave(bool enabled)
    {
        settings.AutoSave = enabled;
    }

	private void Start ()
    {
        autoSaveToggle.isOn = settings.AutoSave;
        t = 0;

        var autoSavesDir = Path.Combine(song.directory, "autosaves");
        if (Directory.Exists(autoSavesDir))
        {
            foreach (var dir in Directory.EnumerateDirectories(autoSavesDir))
            {
                currentAutoSaves.Add(new DirectoryInfo(dir));
            }
        }

        CleanAutosaves();
    }
	
	private void Update ()
    {
        if (!settings.AutoSave || !Application.isFocused) return;
        
        t += Time.deltaTime;

        if (t > (settings.AutoSaveInterval * 60))
        {
            t = 0;
            Save(true);
        }
	}

    public void Save(bool auto = false)
    {
        if (savingThread != null && savingThread.IsAlive)
        {
            Debug.LogError(":hyperPepega: :mega: STOP TRYING TO SAVE THE SONG WHILE ITS ALREADY SAVING TO DISK");
            return;
        }

        var notification = persistentUI.DisplayMessage("Mapper", $"{(auto ? "auto" : "")}save.message", PersistentUI.DisplayMessageType.BOTTOM);
        notification.skipFade = true;
        notification.waitTime = 5.0f;

        // Make sure our map is up to date
        selection.RefreshMap();

        // Run this baby on another thread
        Task.Run(() => SavingTask(auto));
    }

    private void SavingTask(bool auto)
    {
        // Making sure this does not interfere with game thread
        Thread.CurrentThread.IsBackground = true;
        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
        Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

        // Saving Map Data
        var originalMap = map.directoryAndFile;
        var originalSong = song.directory;

        if (auto)
        {
            var autoSaveDir = Path.Combine(originalSong, "autosaves", DateTime.Now.ToString("dd-MM-yyyy_HH-mm-ss"));

            Debug.Log($"Auto saved to: {autoSaveDir}");

            Directory.CreateDirectory(autoSaveDir);

            map.directoryAndFile = Path.Combine(autoSaveDir, diff.beatmapFilename);
            song.directory = autoSaveDir;

            var newDirectoryInfo = new DirectoryInfo(autoSaveDir);
            currentAutoSaves.Add(newDirectoryInfo);
            CleanAutosaves();
        }

        map.Save(settings.AdvancedShit);
        map.directoryAndFile = originalMap;

        if (diff.customData == null)
        {
            diff.customData = new JSONObject();
        }

        song.SaveSong();
        song.directory = originalSong;
    }

    public void OnSave(InputAction.CallbackContext context)
    {
        if (context.performed) Save();
    }

    private void CleanAutosaves()
    {
        if (currentAutoSaves.Count <= MAXIMUM_AUTOSAVE_COUNT) return;

        Debug.Log($"Too many autosaves; removing excess... ({currentAutoSaves.Count} > {MAXIMUM_AUTOSAVE_COUNT})");

        var ordered = currentAutoSaves.OrderByDescending(d => d.LastWriteTime).ToArray();
        currentAutoSaves = ordered.Take(MAXIMUM_AUTOSAVE_COUNT).ToList();

        foreach (var directoryInfo in ordered.Skip(MAXIMUM_AUTOSAVE_COUNT))
        {
            Directory.Delete(directoryInfo.FullName, true);
        }
    }
}
