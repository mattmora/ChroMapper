using TMPro;
using UnityEngine;
using Zenject;

public class RotationDisplayController : MonoBehaviour
{
    [SerializeField] private RotationCallbackController rotationCallback;
    [SerializeField] private TextMeshProUGUI display;

    private Settings settings;

    [Inject]
    private void Construct(Settings settings)
    {
        this.settings = settings;
    }

    private void Start()
    {
        gameObject.SetActive(rotationCallback.IsActive);
        rotationCallback.RotationChangedEvent += RotationChanged;
    }

    private void RotationChanged(bool natural, int rotation)
    {
        display.text = (settings.Reset360DisplayOnCompleteTurn
            ? BetterModulo(rotation, 360)
            : rotation).ToString();
    }

    private int BetterModulo(int x, int m) => (x % m + m) % m; //thanks stackoverflow

    private void OnDestroy()
    {
        rotationCallback.RotationChangedEvent -= RotationChanged;
    }
}
