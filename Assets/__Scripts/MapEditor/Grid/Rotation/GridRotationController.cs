using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zenject;

public class GridRotationController : MonoBehaviour
{

    private static readonly int Rotation = Shader.PropertyToID("_Rotation");

    [SerializeField] private float rotationChangingTime = 1;
    [SerializeField] private Vector3 rotationPoint = LoadInitialMap.PlatformOffset;
    [SerializeField] private bool rotateTransform = true;

    public Action ObjectRotationChangedEvent;

    private float currentRotation;
    private int targetRotation;
    private int cachedRotation;
    private List<Renderer> allRotationalRenderers = new List<Renderer>();

    private Settings settings;
    private RotationCallbackController rotationCallback;

    [Inject]
    private void Construct(
        Settings settings,
        [InjectOptional] RotationCallbackController rotationCallback = null)
    {
        this.rotationCallback = rotationCallback;
        this.settings = settings;

        if (rotationCallback != null) Init();
    }

    private void Start()
    {
        if (rotationCallback != null) Init();
    }

    public void Init()
    {
        rotationCallback.RotationChangedEvent += RotationChanged;
        Settings.NotifyBySettingName("RotateTrack", UpdateRotateTrack);
        if (!GetComponentsInChildren<Renderer>().Any()) return;
        allRotationalRenderers.Clear();
        allRotationalRenderers.AddRange(GetComponentsInChildren<Renderer>().Where(x => x.material.HasProperty(Rotation)));
    }

    private void UpdateRotateTrack(object obj)
    {
        bool rotating = (bool)obj;
        if (rotating)
        {
            targetRotation = rotationCallback.Rotation;
            ChangeRotation(rotationCallback.Rotation);
        }
        else
        {
            targetRotation = 0;
            ChangeRotation(0);
        }
    }

    private void RotationChanged(bool natural, int rotation)
    {
        if (!rotationCallback.IsActive || !settings.RotateTrack) return;
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
        if (rotationCallback is null || !rotationCallback.IsActive || !settings.RotateTrack) return;

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
        if (rotationCallback != null) rotationCallback.RotationChangedEvent -= RotationChanged;
        Settings.ClearSettingNotifications("RotateTrack");
    }
}
