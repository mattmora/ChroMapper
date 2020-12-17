using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class UIScaleController : MonoBehaviour
{
    private Dictionary<CanvasScaler, Vector2> scalers = new Dictionary<CanvasScaler, Vector2>();

    [Inject]
    private void Init(Settings settings)
    {
        foreach (var scaler in GetComponentsInChildren<CanvasScaler>())
        {
            scalers.Add(scaler, scaler.referenceResolution);
        }

        Settings.NotifyBySettingName(nameof(Settings.UIScale), RecalculateScale);
        
        RecalculateScale(settings.UIScale);
    }

    private void RecalculateScale(object obj)
    {
        float scale = Convert.ToSingle(obj);
        foreach (var kvp in scalers)
        {
            kvp.Key.referenceResolution = kvp.Value * scale;
        }
    }
}
