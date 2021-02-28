using UnityEngine;
using UnityEngine.Localization.Components;

public class ValidateDirectorySettingsBinder : SettingsBinder
{
    [SerializeField] private LocalizeStringEvent errorText;

    protected override object SettingsToUIValue(object input) => input;

    protected override object UIValueToSettings(object input)
    {
        string old = Settings.AllFieldInfos[BindedSetting].GetValue(settings).ToString();
        
        Settings.AllFieldInfos[BindedSetting].SetValue(settings, input);

        errorText.StringReference.TableEntryReference = "validate.good";
        
        if (!settings.ValidateInstallation(ErrorFeedback))
        {
            return old;
        }
        else
        {
            return input;
        }
    }

    private void ErrorFeedback(string feedback)
    {
        errorText.StringReference.TableEntryReference = feedback;
    }
}
