using UnityEngine;
using Zenject;

public class ReflectionProbeSettingUpdate : MonoBehaviour
{
    [SerializeField] private ReflectionProbe probe;

    private Settings settings;

    [Inject]
    private void Construct(Settings settings)
    {
        this.settings = settings;
    }

    void Start()
    {
        Settings.NotifyBySettingName("Reflections", UpdateReflectionSetting);
        UpdateReflectionSetting(settings.Reflections);
    }

    private void UpdateReflectionSetting(object obj)
    {
        probe.enabled = (bool)obj;
    }

    private void OnDestroy()
    {
        Settings.ClearSettingNotifications("Reflections");    
    }
}
