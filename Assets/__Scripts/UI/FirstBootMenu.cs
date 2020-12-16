using System;
using System.Collections;
using System.Globalization;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using Microsoft.Win32;
using System.IO;
using UnityEngine.Localization.Settings;
using SFB;
using TMPro;
using Zenject;

public class FirstBootMenu : MonoBehaviour {

    [SerializeField] private GameObject directoryCanvas;
    [SerializeField] private TMP_InputField directoryField;
    [SerializeField] private TMP_Dropdown graphicsDropdown;
    [SerializeField] private GameObject helpPanel;

    [SerializeField] private InputBoxFileValidator validation;

    private static readonly string oculusStoreBeatSaberFolderName = "hyperbolic-magnetism-beat-saber";

    private Regex appManifestRegex = new Regex(@"\s""installdir""\s+""(.+)""", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private Regex libraryRegex = new Regex(@"\s""\d""\s+""(.+)""", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private Settings settings;
    private SceneTransitionManager sceneTransitionManager;

    [Inject]
    private void Init(Settings settings, SceneTransitionManager sceneTransitionManager)
    {
        this.settings = settings;
        this.sceneTransitionManager = sceneTransitionManager;

        if (settings.ValidateInstallation(null))
        {
            Debug.Log("Auto loaded directory");
            FirstBootRequirementsMet();
            return;
        }

        if (SystemInfo.graphicsMemorySize <= 1024)
        {
            graphicsDropdown.value = 2;
        }
        else if (SystemInfo.graphicsMemorySize <= 2048)
        {
            graphicsDropdown.value = 1;
        }

        string posInstallationDirectory = GuessBeatSaberInstallationDirectory();
        if (!string.IsNullOrEmpty(posInstallationDirectory))
        {
            directoryField.text = posInstallationDirectory;
            ValidateQuiet();
        }

        directoryCanvas.SetActive(true);
    }

    private void SetFromTextbox()
    {
        string installation = directoryField.text;
        if (installation == null)
        {
            return;
        }
        settings.BeatSaberInstallation = Settings.ConvertToDirectory(installation);
    }

    public void SetDirectoryButtonPressed()
    {
        SetFromTextbox();
        if (settings.ValidateInstallation(ErrorFeedbackWithContinue))
        {
            SetDefaults();
            FirstBootRequirementsMet();
            return;
        }
        validation.SetValidationState(true, false);
    }

    public void SetDefaults()
    {
        switch (graphicsDropdown.value)
        {
            case 2:
                settings.Waveform = 0;
                settings.PostProcessingIntensity = 0;
                settings.ChromaticAberration = false;
                settings.SimpleBlocks = true;
                settings.Reflections = false;
                settings.HighQualityBloom = false;
                break;
            case 1:
                settings.HighQualityBloom = false;
                settings.Reflections = false;
                break;
        }
    }

    public void ErrorFeedback(string s)
    {
        StartCoroutine(DoErrorFeedback(s, false));
    }

    public void ErrorFeedbackWithContinue(string s)
    {
        StartCoroutine(DoErrorFeedback(s, true));
    }

    private IEnumerator DoErrorFeedback(string s, bool continueAfter)
    {
        var arg = LocalizationSettings.StringDatabase.GetLocalizedStringAsync("FirstBoot", s);
        yield return arg;
        PersistentUI.Instance.ShowDialogBox("FirstBoot", "validate.dialog",
            continueAfter ? (Action<int>)HandleGenerateMissingFoldersWithContinue : HandleGenerateMissingFolders, PersistentUI.DialogBoxPresetType.YesNo, new object[] { arg.Result });
    }

    internal void HandleGenerateMissingFolders(int res)
    {
        HandleGenerateMissingFolders(res, false);
    }

    internal void HandleGenerateMissingFoldersWithContinue(int res)
    {
        HandleGenerateMissingFolders(res, true);
    }

    internal void HandleGenerateMissingFolders(int res, bool continueAfter)
    {
        if (res == 0)
        {
            Debug.Log("Creating directories that do not exist...");
            if (!Directory.Exists(settings.BeatSaberInstallation))
            {
                Directory.CreateDirectory(settings.BeatSaberInstallation);
            }
            if (!Directory.Exists(settings.CustomSongsFolder))
            {
                Directory.CreateDirectory(settings.CustomSongsFolder);
            }
            if (!Directory.Exists(settings.CustomWIPSongsFolder))
            {
                Directory.CreateDirectory(settings.CustomWIPSongsFolder);
            }
            SetDefaults();
            FirstBootRequirementsMet();
        }
    }

    public void FirstBootRequirementsMet() {
        ColourHistory.Load(); //Load color history from file.
        CustomPlatformsLoader.Instance.Init();
        sceneTransitionManager.LoadScene("01_SongSelectMenu");
    }

    public void ToggleHelp() {
        helpPanel.SetActive(!helpPanel.activeSelf);
    }

    private string GuessBeatSaberInstallationDirectory()
    {
        string posInstallationDirectory = GuessSteamInstallationDirectory();
        if (!string.IsNullOrEmpty(posInstallationDirectory))
        {
            return posInstallationDirectory;
        }
        posInstallationDirectory = GuessOculusInstallationDirectory();
        return posInstallationDirectory;
    }

    private string GuessSteamInstallationDirectory()
    {
        // The Steam App ID seems to be static e.g. https://store.steampowered.com/app/620980/Beat_Saber/
        string steamRegistryKey = "HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\Steam App 620980";
        string registryValue = "InstallLocation";
        try
        {
            string installDirectory = (string) Registry.GetValue(steamRegistryKey, registryValue, "");
            if (!string.IsNullOrEmpty(installDirectory))
            {
                return installDirectory;
            }
            return GuessSteamInstallationDirectoryComplex();
        } catch(System.Exception e)
        {
            Debug.Log("Error reading Steam registry key" + e);
            return "";
        }
    }

    private string GuessSteamInstallationDirectoryComplex()
    {
        // The above registry key only exists if you've installed Beat Saber since last installing windows
        // if you copy the game files or reinstall windows then the registry key will be missing even though
        // the game launches just fine
        string steamRegistryKey = "HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node\\Valve\\Steam";
        string registryValue = "InstallPath";

        List<string> libraryFolders = new List<string>();

        string mainSteamFolder = (string) Registry.GetValue(steamRegistryKey, registryValue, "");
        libraryFolders.Add(mainSteamFolder);

        string libraryFolderFilename = mainSteamFolder + "\\steamapps\\libraryfolders.vdf";
        if (File.Exists(libraryFolderFilename))
        {
            using (StreamReader reader = new StreamReader(libraryFolderFilename))
            {
                string text = reader.ReadToEnd();
                MatchCollection matches = libraryRegex.Matches(text);

                foreach (Match match in matches)
                {
                    if (Directory.Exists(match.Groups[1].Value))
                    {
                        libraryFolders.Add(match.Groups[1].Value);
                    }
                }
            }
        }

        foreach (string libraryFolder in libraryFolders)
        {
            string fileName = libraryFolder + "\\steamapps\\appmanifest_620980.acf";
            if (File.Exists(fileName))
            {
                using (StreamReader reader = new StreamReader(fileName))
                {
                    string text = reader.ReadToEnd();

                    GroupCollection installDirMatch = appManifestRegex.Matches(text)[0].Groups;
                    string installDir = libraryFolder + "\\steamapps\\common\\" + installDirMatch[1].Value;

                    if (Directory.Exists(installDir))
                    {
                        return installDir;
                    }
                }
            }
        }
        return "";
    }

    private string GuessOculusInstallationDirectory()
    {
        string oculusRegistryKey = "HKEY_LOCAL_MACHINE\\SOFTWARE\\WOW6432Node\\Oculus VR, LLC\\Oculus";
        try
        {
            string registryValue = "InitialAppLibrary";
            string software = "Software";
            
            // older Oculus installations seem to have created the InitialAppLibrary value
            string installPath = GryRegistryWithPath(oculusRegistryKey + "\\Config",  registryValue, software, oculusStoreBeatSaberFolderName, "");

            if (!string.IsNullOrEmpty(installPath))
            {
                return installPath;
            }

            // the default library for newer installations seem to be below the base directory in "Software\\Software" folder.
            registryValue = "Base";
            installPath = GryRegistryWithPath(oculusRegistryKey, registryValue, software, software, oculusStoreBeatSaberFolderName);

            if (Directory.Exists(installPath))
            {
                return installPath;
            } else
            {
                return TryOculusStoreLibraryLocations();
            }
        } catch (System.Exception e)
        {
            Debug.Log("Error guessing Oculus Beat Saber Directory" + e);
            return "";
        }
    }

    private string GryRegistryWithPath(string registryKey, string registryValue, string path1, string path2, string path3)
    {
        string oculusBaseDirectory = (string) Registry.GetValue(registryKey, registryValue, "");
        if (string.IsNullOrEmpty(oculusBaseDirectory))
        {
            return "";
        }
        string installPath;
        if (string.IsNullOrEmpty(path3))
        {
            installPath = Path.Combine(oculusBaseDirectory, path1, path2);
        }
        else
        {
            installPath = Path.Combine(oculusBaseDirectory, path1, path2, path3);
        }
        if (Directory.Exists(installPath))
        {
            return installPath;
        }
        else
        {
            return "";
        }
    }

    private string TryOculusStoreLibraryLocations()
    {
        RegistryKey libraryKey = Registry.CurrentUser.OpenSubKey("Software\\Oculus VR, LLC\\Oculus\\Libraries");
        if (libraryKey == null)
        {
            return "";
        }
        string[] subKeys = libraryKey.GetSubKeyNames();
        foreach(string subKeyName in subKeys)
        {
            object originalPath = libraryKey.OpenSubKey(subKeyName).GetValue("OriginalPath");
            if (originalPath != null && string.IsNullOrEmpty((string)originalPath))
            {
                continue;
            }
            string installPath = Path.Combine((string)originalPath, "Software", oculusStoreBeatSaberFolderName);
            if (Directory.Exists(installPath))
            {
                return installPath;
            }
        }
        return "";
    }

    public void OpenFolderBrowser()
    {
        var paths = StandaloneFileBrowser.OpenFolderPanel("Installation Directory", "", false);
        var installation = paths[0];
        directoryField.text = installation;
        settings.BeatSaberInstallation = Settings.ConvertToDirectory(installation);
        validation.SetValidationState(true, settings.ValidateInstallation(ErrorFeedback));
    }

    public void ValidateQuiet()
    {
        SetFromTextbox();
        validation.SetValidationState(true, settings.ValidateInstallation(null));
    }

}
