using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using System;
using Zenject;

public class PostProcessingController : MonoBehaviour
{

    public Volume PostProcess;
    [SerializeField] private Slider intensitySlider;
    [SerializeField] private TextMeshProUGUI intensityLabel;
    [SerializeField] private Toggle chromaticAberration;
    
    [Inject]
    private void Construct(Settings settings)
    {
        UpdatePostProcessIntensity(settings.PostProcessingIntensity);
        UpdateChromaticAberration(settings.ChromaticAberration);
    }

    private void Start()
    {
        Settings.NotifyBySettingName(nameof(Settings.PostProcessingIntensity), UpdatePostProcessIntensity);
        Settings.NotifyBySettingName(nameof(Settings.ChromaticAberration), UpdateChromaticAberration);
    }

    public void UpdatePostProcessIntensity(object o)
    {
        PostProcess.profile.TryGet(out Bloom bloom);
        bloom.intensity.value = (float)o;
    }

    public void UpdateChromaticAberration(object o)
    {
        PostProcess.profile.TryGet(out ChromaticAberration ca);
        ca.active = (bool)o;
    }
}
