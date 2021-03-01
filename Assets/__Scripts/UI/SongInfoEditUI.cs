using SFB;
using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Networking;
using static UnityEngine.InputSystem.InputAction;
using Zenject;

public class SongInfoEditUI : MenuBase
{
    public class Environment
    {
        public readonly string humanName;
        public readonly string jsonName;

        public Environment(string humanName, string jsonName)
        {
            this.humanName = humanName;
            this.jsonName = jsonName;
        }
    }

    public static List<Environment> VanillaEnvironments = new List<Environment>()
    {
        new Environment("Default", "DefaultEnvironment"),
        new Environment("Big Mirror", "BigMirrorEnvironment"),
        new Environment("Triangle", "TriangleEnvironment"),
        new Environment("Nice", "NiceEnvironment"),
        new Environment("K/DA", "KDAEnvironment"),
        new Environment("Monstercat", "MonstercatEnvironment"),
        new Environment("Dragons", "DragonsEnvironment"),
        new Environment("Origins", "OriginsEnvironment"), // i swear to god if beat games reverts this back i am going to lose my shit
        new Environment("Crab Rave", "CrabRaveEnvironment"),
        new Environment("Panic! At The Disco", "PanicEnvironment"),
        new Environment("Rocket League", "RocketEnvironment"),
        new Environment("Green Day", "GreenDayEnvironment"),
        new Environment("Green Day Grenade", "GreenDayGrenadeEnvironment"),
        new Environment("Timbaland", "TimbalandEnvironment"),
        new Environment("FitBeat", "FitBeatEnvironment"),
        new Environment("Linkin Park", "LinkinParkEnvironment"),
        new Environment("BTS", "BTSEnvironment")
    };

    private static List<Environment> VanillaDirectionalEnvironments = new List<Environment>()
    {
        new Environment("Glass Desert", "GlassDesertEnvironment")
    };

    public static List<string> CharacteristicDropdownToBeatmapName = new List<string>()
    {
        "Standard",
        "NoArrows",
        "OneSaber",
        "360Degree",
        "90Degree",
        "Lightshow",
        "Lawless"
    };

    public static int GetDirectionalEnvironmentIDFromString(string environment)
    {
        int result = VanillaDirectionalEnvironments.TakeWhile(i => i.jsonName != environment).Count();
        return result == VanillaDirectionalEnvironments.Count ? 0 : result;
    }

    public static int GetEnvironmentIDFromString(string environment)
    {
        int result = VanillaEnvironments.TakeWhile(i => i.jsonName != environment).Count();
        return result == VanillaEnvironments.Count ? 0 : result;
    }

    public static string GetEnvironmentNameFromID(int id) => VanillaEnvironments[id].jsonName;

    public Action TempSongLoadedEvent;

    [SerializeField] private AudioSource previewAudio;
    [SerializeField] private DifficultySelect difficultySelect;
    [SerializeField] private TMP_InputField nameField;
    [SerializeField] private TMP_InputField subNameField;
    [SerializeField] private TMP_InputField songAuthorField;
    [SerializeField] private TMP_InputField authorField;
    [SerializeField] private TMP_InputField coverImageField;
    [SerializeField] private TMP_InputField bpmField;
    [SerializeField] private TMP_InputField prevStartField;
    [SerializeField] private TMP_InputField prevDurField;
    [SerializeField] private TMP_Dropdown environmentDropdown;
    [SerializeField] private TMP_Dropdown customPlatformsDropdown;
    [SerializeField] private TMP_InputField audioPath;
    [SerializeField] private TMP_InputField offset;
    [SerializeField] private Image revertInfoButtonImage;
    [SerializeField] private ContributorsController contributorController;

    private Coroutine reloadSongDataCoroutine;
    private string loadedSong = null;

    private BeatSaberSong song;
    private Settings settings;
    private SceneTransitionManager sceneTransitionManager;
    private PersistentUI persistentUI;
    private CustomPlatformsLoader customPlatformsLoader;

    [Inject]
    private void Construct(BeatSaberSong song, Settings settings, SceneTransitionManager sceneTransitionManager, PersistentUI persistentUI, CustomPlatformsLoader customPlatformsLoader)
    {
        this.settings = settings;
        this.song = song;
        this.sceneTransitionManager = sceneTransitionManager;
        this.persistentUI = persistentUI;
        this.customPlatformsLoader = customPlatformsLoader;
    }

    private void Start()
    {
        environmentDropdown.ClearOptions();
        environmentDropdown.AddOptions(VanillaEnvironments.Select(it => it.humanName).ToList());

        customPlatformsDropdown.ClearOptions();
        customPlatformsDropdown.AddOptions(new List<string> { "None" });
        customPlatformsDropdown.AddOptions(customPlatformsLoader.GetAllEnvironmentIds());

        LoadFromSong();
    }

    /// <summary>
    /// Default object to select when pressing Tab and nothing is selected
    /// </summary>
    /// <returns>A GUI object</returns>
    protected override GameObject GetDefault()
    {
        return nameField.gameObject;
    }

    /// <summary>
    /// Callback for when escape is pressed, user wants out of here
    /// </summary>
    /// <param name="context">Information about the event</param>
    public override void OnLeaveMenu(CallbackContext context)
    {
        if (context.performed) ReturnToSongList();
    }

    /// <summary>
    /// Save the changes the user has made in the song info panel
    /// </summary>
    public void SaveToSong()
    {
        song.songName = nameField.text;
        song.songSubName = subNameField.text;
        song.songAuthorName = songAuthorField.text;
        song.levelAuthorName = authorField.text;
        song.coverImageFilename = coverImageField.text;
        song.songFilename = audioPath.text;

        song.beatsPerMinute = GetTextValue(bpmField);
        song.previewStartTime = GetTextValue(prevStartField);
        song.previewDuration = GetTextValue(prevDurField);
        song.songTimeOffset = GetTextValue(offset);

        if (song.songTimeOffset > 0)
        {
            persistentUI.ShowDialogBox("SongEditMenu", "songtimeoffset.warning", null, PersistentUI.DialogBoxPresetType.Ok);
        }

        song.environmentName = GetEnvironmentNameFromID(environmentDropdown.value);

        if (song.customData == null) song.customData = new JSONObject();

        if (customPlatformsDropdown.value > 0)
        {
            song.customData["_customEnvironment"] = customPlatformsDropdown.captionText.text;

            if (customPlatformsLoader.GetAllEnvironments().TryGetValue(customPlatformsDropdown.captionText.text, out PlatformInfo info))
            {
                song.customData["_customEnvironmentHash"] = info.Md5Hash;
            }
        }
        else
        {
            song.customData.Remove("_customEnvironment");
            song.customData.Remove("_customEnvironmentHash");
        }

        contributorController.Commit();
        song.contributors = contributorController.contributors;

        song.SaveSong();

        // Trigger validation checks, if this is the first save they will not have been done yet
        coverImageField.GetComponent<InputBoxFileValidator>().OnUpdate();
        audioPath.GetComponent<InputBoxFileValidator>().OnUpdate();
        ReloadAudio();

        persistentUI.DisplayMessage("SongEditMenu", "saved", PersistentUI.DisplayMessageType.BOTTOM);
    }

    /// <summary>
    /// Populate UI from song data
    /// </summary>
    public void LoadFromSong()
    {
        nameField.text = song.songName;
        subNameField.text = song.songSubName;
        songAuthorField.text = song.songAuthorName;
        authorField.text = song.levelAuthorName;

        BroadcastMessage("OnValidate"); // god unity why are you so dumb

        coverImageField.text = song.coverImageFilename;
        audioPath.text = song.songFilename;

        offset.text = song.songTimeOffset.ToString();
        if (song.songTimeOffset > 0)
        {
            persistentUI.ShowDialogBox("SongEditMenu", "songtimeoffset.warning", null, PersistentUI.DialogBoxPresetType.Ok);
        }

        bpmField.text = song.beatsPerMinute.ToString();
        prevStartField.text = song.previewStartTime.ToString();
        prevDurField.text = song.previewDuration.ToString();

        environmentDropdown.value = GetEnvironmentIDFromString(song.environmentName);

        customPlatformsDropdown.value = CustomPlatformFromSong();
        if (customPlatformsDropdown.value == 0)
        {
            customPlatformsDropdown.captionText.text = "None";
        }

        contributorController.UndoChanges();

        ReloadAudio();
    }

    /// <summary>
    /// Get the id for the custom platform specified in the song data
    /// </summary>
    /// <returns>Custom platform index</returns>
    private int CustomPlatformFromSong()
    {
        if (song.customData != null)
        {
            if (!string.IsNullOrEmpty(song.customData["_customEnvironment"]))
            {
                return customPlatformsLoader.GetAllEnvironmentIds().IndexOf(song.customData["_customEnvironment"]) + 1;
            }
        }
        return 0;
    }

    /// <summary>
    /// Start the LoadAudio Coroutine
    /// </summary>
    public void ReloadAudio()
    {
        StartCoroutine(LoadAudio());
    }

    /// <summary>
    /// Try and load the song, this is used for the song preview as well as later
    /// passed to the mapping scene
    /// </summary>
    /// <param name="useTemp">Should we load the song the user has updated in the UI or from the saved song data</param>
    /// <returns>Coroutine IEnumerator</returns>
    private IEnumerator LoadAudio(bool useTemp = true)
    {
        if (song.directory == null)
        {
            yield break;
        }

        string fullPath = Path.Combine(song.directory, useTemp ? audioPath.text : song.songFilename);

        if (fullPath == loadedSong)
        {
            yield break;
        }

        Debug.Log("Loading audio");
        if (File.Exists(fullPath))
        {
            if (audioPath.text.ToLower().EndsWith("ogg") || audioPath.text.ToLower().EndsWith("egg"))
            {
                UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip($"file:///{Uri.EscapeDataString(fullPath)}", AudioType.OGGVORBIS);
                //Escaping should fix the issue where half the people can't open ChroMapper's editor (I believe this is caused by spaces in the directory, hence escaping)
                yield return www.SendWebRequest();
                Debug.Log("Song loaded!");
                AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                if (clip == null)
                {
                    Debug.Log("Error getting Audio data!");
                    sceneTransitionManager.CancelLoading("load.error.audio");
                }
                loadedSong = fullPath;
                clip.name = "Song";
                previewAudio.clip = clip;

                if (useTemp)
                {
                    TempSongLoadedEvent?.Invoke();
                }
            }
            else
            {
                Debug.Log("Incompatible file type! WTF!?");
                sceneTransitionManager.CancelLoading("load.error.audio2");
            }
        }
        else
        {
            sceneTransitionManager.CancelLoading("load.error.audio3");
            Debug.Log("Song does not exist! WTF!?");
            Debug.Log(fullPath);
        }
    }

    /// <summary>
    /// Check the user wants to delete the map
    /// </summary>
    public void DeleteMap()
    {
        persistentUI.ShowDialogBox("SongEditMenu", "delete.dialog", HandleDeleteMap, PersistentUI.DialogBoxPresetType.YesNo, new[] { song.songName });
    }

    /// <summary>
    /// Delete the map, it's still recoverable externally
    /// </summary>
    /// <param name="result">Confirmation from the user</param>
    private void HandleDeleteMap(int result)
    {
        // Left button (ID 0) pressed; the user wants to delete the map.
        if (result == 0) 
        {
            FileOperationAPIWrapper.MoveToRecycleBin(song.directory);
            ReturnToSongList();
        }
    }

    private void AddToZip(ZipArchive archive, string fileLocation)
    {
        string fullPath = Path.Combine(song.directory, fileLocation);
        if (File.Exists(fullPath))
        {
            archive.CreateEntryFromFile(fullPath, fileLocation);
        }
    }

    /// <summary>
    /// Create a zip for sharing the map
    /// </summary>
    public void PackageZip()
    {
        string infoFileLocation = "";
        string zipPath = "";
        if (song.directory != null)
        {
            zipPath = Path.Combine(song.directory, song.cleanSongName + ".zip");
            // Mac doesn't seem to like overwriting existing zips, so delete the old one first
            File.Delete(zipPath);

            infoFileLocation = Path.Combine(song.directory, "info.dat");
        }

        if (!File.Exists(infoFileLocation))
        {
            Debug.LogError(":hyperPepega: :mega: WHY TF ARE YOU TRYING TO PACKAGE A MAP WITH NO INFO.DAT FILE");
            persistentUI.ShowDialogBox("SongEditMenu", "zip.warning", null, PersistentUI.DialogBoxPresetType.Ok);
            return;
        }

        using (ZipArchive archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
        { 
            // oh yeah lolpants is gonna kill me if it isnt packaged as "Info.dat"
            archive.CreateEntryFromFile(infoFileLocation, "Info.dat");

            AddToZip(archive, song.coverImageFilename);
            AddToZip(archive, song.songFilename);

            foreach (var contributor in song.contributors.DistinctBy(it => it.LocalImageLocation))
            {
                string imageLocation = Path.Combine(song.directory, contributor.LocalImageLocation);
                if (contributor.LocalImageLocation != song.coverImageFilename &&
                    File.Exists(imageLocation) && !File.GetAttributes(imageLocation).HasFlag(FileAttributes.Directory))
                {
                    archive.CreateEntryFromFile(imageLocation, contributor.LocalImageLocation);
                }
            }

            foreach (var set in song.difficultyBeatmapSets)
            {
                foreach (var map in set.difficultyBeatmaps)
                {
                    AddToZip(archive, map.beatmapFilename);
                }
            }
        }
        OpenSelectedMapInFileBrowser();
    }

    /// <summary>
    /// Open the folder containing the map's files in a native file browser
    /// </summary>
    public void OpenSelectedMapInFileBrowser()
    {
        try
        {
            string winPath = song.directory.Replace("/", "\\").Replace("\\\\", "\\");
            Debug.Log($"Opening song directory ({winPath}) with Windows...");
            System.Diagnostics.Process.Start("explorer.exe", $"\"{winPath}\"");
        }catch
        {
            if (song.directory == null)
            {
                persistentUI.ShowDialogBox("SongEditMenu", "explorer.warning", null, PersistentUI.DialogBoxPresetType.Ok);
                return;
            }
            Debug.Log("Windows opening failed, attempting Mac...");
            try
            {
                string macPath = song.directory.Replace("\\", "/").Replace("//", "/");
                if (!macPath.StartsWith("\"")) macPath = "\"" + macPath;
                if (!macPath.EndsWith("\"")) macPath = macPath + "\"";
                System.Diagnostics.Process.Start("open", macPath);
            }
            catch
            {
                Debug.Log("What is this, some UNIX bullshit?");
                persistentUI.ShowDialogBox("Unrecognized OS!\n\nIf you happen to know Linux and would like to contribute," +
                    " please contact me on Discord: Caeden117#0117", null, PersistentUI.DialogBoxPresetType.Ok);
            }
        }
    }

    /// <summary>
    /// Return the the song list scene, if the user has unsaved changes ask first
    /// </summary>
    public void ReturnToSongList()
    {
        // Do nothing if a dialog is open
        if (persistentUI.DialogBox_IsEnabled) return;

        CheckForChanges(HandleReturnToSongList);
    }

    /// <summary>
    /// Return the the song list scene
    /// </summary>
    /// <param name="r">Confirmation from the user</param>
    public void HandleReturnToSongList(int r)
    {
        if (r == 0)
        {
            sceneTransitionManager.LoadScene("01_SongSelectMenu");
        }
    }

    /// <summary>
    /// The user wants to edit the map
    /// Check first that some objects are enabled and that there are no unsaved changes
    /// </summary>
    public void EditMapButtonPressed()
    {
        // If no difficulty is selected or there is a dialog open do nothing
        if (difficultySelect.CurrentlySelectedDifficulty == null || persistentUI.DialogBox_IsEnabled)
        {
            return;
        }

        bool a = settings.Load_Notes;
        bool b = settings.Load_Obstacles;
        bool c = settings.Load_Events;
        bool d = settings.Load_Others;

        if (!(a || b || c || d))
        {
            persistentUI.ShowDialogBox("SongEditMenu", "load.warning", null, PersistentUI.DialogBoxPresetType.Ok);
            return;
        }
        else if (!(a && b && c && d))
        {
            persistentUI.ShowDialogBox("SongEditMenu", "load.warning2", null, PersistentUI.DialogBoxPresetType.Ok);
        }

        CheckForChanges(HandleEditMapButtonPressed);
    }

    /// <summary>
    /// Load the editor scene
    /// </summary>
    /// <param name="r">Confirmation from the user</param>
    private void HandleEditMapButtonPressed(int r)
    {
        if (r == 0)
        {
            var diffData = difficultySelect.CurrentlySelectedDifficulty;
            var map = song.GetMapFromDifficultyBeatmap(diffData);

            Debug.Log("Transitioning...");
            if (map != null)
            {
                settings.LastLoadedMap = song.directory;
                settings.LastLoadedChar = diffData.parentBeatmapSet.beatmapCharacteristicName;
                settings.LastLoadedDiff = diffData.difficulty;

                sceneTransitionManager.LoadScene("03_Mapper")
                    .WithEarlyLoadRoutines(LoadAudio(false))
                    .WithDataInjectedEarly(song, map, previewAudio.clip, diffData);
            }
        }
    }

    /// <summary>
    /// Helper methods to prompt the user if there are unsaved changes
    /// Will call the callback immediately if there are none
    /// </summary>
    /// <param name="callback">Method to call when the user has made a decision</param>
    /// <returns>True if a dialog has been opened, false otherwise</returns>
    private bool CheckForChanges(Action<int> callback)
    {
        if (IsDirty())
        {
            persistentUI.ShowDialogBox("SongEditMenu", "unsaved.warning", callback, PersistentUI.DialogBoxPresetType.YesNo);
            return true;
        }
        else if (difficultySelect.IsDirty())
        {
            persistentUI.ShowDialogBox("SongEditMenu", "unsaveddiff.warning", callback, PersistentUI.DialogBoxPresetType.YesNo);
            return true;
        }
        else if (contributorController.IsDirty())
        {
            persistentUI.ShowDialogBox("SongEditMenu", "unsavedcontributor.warning", callback, PersistentUI.DialogBoxPresetType.YesNo);
            return true;
        }
        callback(0);
        return false;
    }

    /// <summary>
    /// Edit contributors button has been pressed
    /// Check there are no unsaved changes
    /// </summary>
    public void EditContributors()
    {
        // Do nothing if a dialog is open
        if (persistentUI.DialogBox_IsEnabled) return;

        var wrapper = contributorController.transform.parent.gameObject;
        wrapper.SetActive(!wrapper.activeSelf);
    }

    /// <summary>
    /// Undo button has been pressed, trigger animation and reload the song data
    /// </summary>
    public void UndoChanges()
    {
        reloadSongDataCoroutine = StartCoroutine(SpinReloadSongDataButton());
        LoadFromSong();
    }

    /// <summary>
    /// Spins the undo button for extra flare
    /// </summary>
    /// <returns>Coroutine IEnumerator</returns>
    private IEnumerator SpinReloadSongDataButton()
    {
        if (reloadSongDataCoroutine != null) StopCoroutine(reloadSongDataCoroutine);

        float startTime = Time.time;
        var transform1 = revertInfoButtonImage.transform;
        Quaternion rotationQ = transform1.rotation;
        Vector3 rotation = rotationQ.eulerAngles;
        rotation.z = -330;
        transform1.rotation = Quaternion.Euler(rotation);

        while (true)
        {
            float rot = rotation.z;
            float timing = (Time.time / startTime) * 0.075f;
            rot = Mathf.Lerp(rot, 30f, timing);
            rotation.z = rot;
            transform1.rotation = Quaternion.Euler(rotation);

            if (rot >= 25f)
            {
                rotation.z = 30;
                transform1.rotation = Quaternion.Euler(rotation);
                yield break;
            }
            yield return new WaitForFixedUpdate();
        }
    }

    /// <summary>
    /// Helper method to get the float value from a UI element
    /// Returns the placeholder value if the field is empty
    /// </summary>
    /// <param name="inputfield">Text field to get the value from</param>
    /// <returns>The value parsed to a float</returns>
    private static float GetTextValue(TMP_InputField inputfield)
    {
        if (!float.TryParse(inputfield.text, out float result))
        {
            if (!float.TryParse(inputfield.placeholder.GetComponent<TMP_Text>().text, out result))
            {
                // How have you changed the placeholder so that it isn't valid?
                result = 0;
            }
        }
        return result;
    }

    /// <summary>
    /// Check if any changes have been made from the original song data
    /// </summary>
    /// <returns>True if user has made changes, false otherwise</returns>
    private bool IsDirty()
    {
        return song.songName != nameField.text ||
            song.songSubName != subNameField.text ||
            song.songAuthorName != songAuthorField.text ||
            song.levelAuthorName != authorField.text ||
            song.coverImageFilename != coverImageField.text ||
            song.songFilename != audioPath.text ||
            !NearlyEqual(song.beatsPerMinute, GetTextValue(bpmField)) ||
            !NearlyEqual(song.previewStartTime, GetTextValue(prevStartField)) ||
            !NearlyEqual(song.previewDuration, GetTextValue(prevDurField)) ||
            !NearlyEqual(song.songTimeOffset, GetTextValue(offset)) ||
            environmentDropdown.value != GetEnvironmentIDFromString(song.environmentName) ||
            customPlatformsDropdown.value != CustomPlatformFromSong();
    }

    private static bool NearlyEqual(float a, float b, float epsilon = 0.01f)
    {
        return a.Equals(b) || Math.Abs(a - b) < epsilon;
    }

}
