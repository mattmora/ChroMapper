using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GridRotationController : MonoBehaviour
{
    public RotationCallbackController RotationCallback;
    [SerializeField] private float rotationChangingTime = 1;
    [SerializeField] private Vector3 rotationPoint = LoadInitialMap.PlatformOffset;
    [SerializeField] private bool rotateTransform = true;

    public Action ObjectRotationChangedEvent;

    private float currentRotation;
    private int targetRotation;
    private int cachedRotation;
    private List<Renderer> allRotationalRenderers = new List<Renderer>();

    private static readonly int Rotation = Shader.PropertyToID("_Rotation");

    private void Start()
    {
        if (RotationCallback != null) Init();
    }

    public void Init()
    {
        RotationCallback.RotationChangedEvent += RotationChanged;
        Settings.NotifyBySettingName("RotateTrack", UpdateRotateTrack);
        if (!GetComponentsInChildren<Renderer>().Any()) return;
        allRotationalRenderers.AddRange(GetComponentsInChildren<Renderer>().Where(x => x.material.HasProperty(Rotation)));
    }

    private void UpdateRotateTrack(object obj)
    {
        bool rotating = (bool)obj;
        if (rotating)
        {
            targetRotation = RotationCallback.Rotation;
            ChangeRotation(RotationCallback.Rotation);
        }
        else
        {
            targetRotation = 0;
            ChangeRotation(0);
        }
    }

    private void RotationChanged(bool natural, int rotation)
    {
        if (!RotationCallback.IsActive || !Settings.Instance.RotateTrack) return;
        cachedRotation = rotation;
        if (!natural)
        {
            targetRotation = rotation;
            ChangeRotation(rotation);
            return;
        }
    }

    private void Update()
    {
        if (RotationCallback is null || !RotationCallback.IsActive || !Settings.Instance.RotateTrack) return;

        if (Mathf.Abs(targetRotation - cachedRotation) > 0.01f)
        {
            targetRotation = cachedRotation;
        }

        if (Mathf.Abs(currentRotation - targetRotation) > 0.01f)
        {
            ChangeRotation(Mathf.Lerp(currentRotation, targetRotation, 0.075f));
        }
    }

    private void ChangeRotation(float rotation)
    {
        if (rotateTransform) transform.RotateAround(rotationPoint, Vector3.up, rotation - currentRotation);
        currentRotation = rotation;
        ObjectRotationChangedEvent?.Invoke();

        var transformRotation = transform.eulerAngles.y;
        allRotationalRenderers.ForEach(r => r.material.SetFloat(Rotation, transformRotation));
    }

    private void OnDestroy()
    {
        RotationCallback.RotationChangedEvent -= RotationChanged;
        Settings.ClearSettingNotifications("RotateTrack");
    }
}
