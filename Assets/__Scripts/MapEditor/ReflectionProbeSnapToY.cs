using UnityEngine;

public class ReflectionProbeSnapToY : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    
    private PlatformDescriptor descriptor;
    private Settings settings;

    private void Construct(Settings settings, PlatformDescriptor descriptor)
    {
        this.settings = settings;
        this.descriptor = descriptor;
    }

    // Thanks to Guidev on YouTube for the original code for planar reflections, which works just fine with Reflection Probes.
    private void Update()
    {
        if (descriptor is null || !settings.Reflections) return;
        Vector3 camDirWorld = mainCamera.transform.forward;
        Vector3 camUpWorld = mainCamera.transform.up;
        Vector3 camPosWorld = mainCamera.transform.position;

        Vector3 camDirPlane = descriptor.transform.InverseTransformDirection(camDirWorld);
        Vector3 camUpPlane = descriptor.transform.InverseTransformDirection(camUpWorld);
        Vector3 camPosPlane = descriptor.transform.InverseTransformPoint(camPosWorld);

        camDirPlane.y *= -1f;
        camUpPlane.y *= -1f;
        camPosPlane.y *= -1f;

        camDirWorld = descriptor.transform.TransformDirection(camDirPlane);
        camUpWorld = descriptor.transform.TransformDirection(camUpWorld);
        camPosWorld = descriptor.transform.TransformPoint(camPosPlane);

        transform.position = camPosWorld;
        transform.LookAt(camPosWorld + camDirWorld, camUpWorld);
    }
}
