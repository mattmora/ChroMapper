using UnityEngine;
using Zenject;

public class MeasureLinesRenderingOrderController : MonoBehaviour
{
    [SerializeField] private Canvas effectingCanvas;

    [Inject]
    private void Construct(Settings settings)
    {
        UpdateCanvasOrder(settings.MeasureLinesShowOnTop);
    }

    private void Start()
    {
        Settings.NotifyBySettingName("MeasureLinesShowOnTop", UpdateCanvasOrder);
    }

    private void UpdateCanvasOrder(object obj)
    {
        effectingCanvas.sortingLayerName = (bool)obj ? "Background" : "Default";
    }

    private void OnDestroy()
    {
        Settings.ClearSettingNotifications("MeasureLinesShowOnTop");
    }
}
