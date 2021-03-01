using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Zenject;

// TODO: Is this obsolete (or rather, could this be made obsolete)
[CreateAssetMenu(fileName = "DarkThemeSO", menuName = "Map/Dark Theme SO")]
public class DarkThemeSO : ScriptableObject
{
    [SerializeField] private TMP_FontAsset BeonReplacement;
    public TMP_FontAsset TekoReplacement;
    [SerializeField] private Font BeonUnityReplacement;
    [SerializeField] private Font TekoUnityReplacement;

    private Settings settings;

    [Inject]
    private void Construct(Settings settings)
    {
        this.settings = settings;
    }

    public void DarkThemeifyUI()
    {
        if (!settings.DarkTheme) return;
        foreach (TextMeshProUGUI jankCodeMate in Resources.FindObjectsOfTypeAll<TextMeshProUGUI>()) {
            if (jankCodeMate.font.name.Contains("Beon")) jankCodeMate.font = BeonReplacement;
            if (jankCodeMate.font.name.Contains("Teko")) jankCodeMate.font = TekoReplacement;
        }
        foreach (Text jankCodeMate in Resources.FindObjectsOfTypeAll<Text>())
        {
            if (jankCodeMate.font.name.Contains("Beon")) jankCodeMate.font = BeonUnityReplacement;
            if (jankCodeMate.font.name.Contains("Teko")) jankCodeMate.font = TekoUnityReplacement;
        }
    }
}
