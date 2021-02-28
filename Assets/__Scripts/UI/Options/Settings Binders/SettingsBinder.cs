using UnityEngine;
using Zenject;

/// <summary>
/// Base class for Settings Binders, which abstract settings from one huge ass super class.
/// </summary>
public abstract class SettingsBinder : MonoBehaviour
{
    [HideInInspector] public SettingsType BindedSettingSearchType = SettingsType.ALL;
    [HideInInspector] public string BindedSetting = "None";
    [HideInInspector] public bool PopupEditorWarning = false;

    protected Settings settings;

    private PersistentUI persistentUI;

    [Inject]
    private void Construct(Settings settings, PersistentUI persistentUI)
    {
        this.settings = settings;
        this.persistentUI = persistentUI;
    }

    public virtual void SendValueToSettings(object value)
    {
        if (!string.IsNullOrEmpty(BindedSetting) && BindedSetting != "None")
        {
            if (PopupEditorWarning)
            {
                persistentUI?.ShowDialogBox("Options", "restartwarning", null, PersistentUI.DialogBoxPresetType.Ok);
            }
            settings.ApplyOptionByName(BindedSetting, UIValueToSettings(value));
        }
    }

    public virtual object RetrieveValueFromSettings()
    {
        if (string.IsNullOrEmpty(BindedSetting) || BindedSetting == "None") return null;
        return SettingsToUIValue(Settings.AllFieldInfos[BindedSetting].GetValue(settings));
    }

    /// <summary>
    /// Takes an input from an outside UI Element and transforms it to a value ready to be stored into settings.
    /// </summary>
    /// <param name="input">Value from a UI Element, such as a Slider.</param>
    /// <returns>A modified version designed to be intepreted internally, and be saved into the settings file.</returns>
    protected abstract object UIValueToSettings(object input);

    /// <summary>
    /// Takes an input from the settings file and transforms it to a value ready to be used with a UI Element.
    /// </summary>
    /// <param name="input">Value from Settings.</param>
    /// <returns>A modified version designed to be used witrh UI Elements, such as a Slider.</returns>
    protected abstract object SettingsToUIValue(object input);
    
    public enum SettingsType
    {
        ALL,
        STRING,
        INT,
        SINGLE,
        BOOL
    }
}
