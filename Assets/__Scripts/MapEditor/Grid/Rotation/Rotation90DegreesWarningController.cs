using UnityEngine;
using TMPro;
using Zenject;

public class Rotation90DegreesWarningController : MonoBehaviour
{
    [SerializeField] private TracksManager tracksManager;
    [SerializeField] private RotationCallbackController rotationCallback;
    [SerializeField] private TextMeshProUGUI rotationDisplay;

    private BeatSaberSong.DifficultyBeatmap diff;

    [Inject]
    private void Construct(BeatSaberSong.DifficultyBeatmap diff)
    {
        this.diff = diff;
    }

    private void Start()
    {
        if (diff.parentBeatmapSet.beatmapCharacteristicName == "90Degree")
        {
            rotationCallback.RotationChangedEvent += RotationChangedEvent;
        }
    }

    private void RotationChangedEvent(bool natural, int rotation)
    {
        rotationDisplay.color = (rotation < -45 || rotation > 45) ? Color.red : Color.white;
    }

    private void OnDestroy()
    {
        if (diff.parentBeatmapSet.beatmapCharacteristicName == "90Degree")
        {
            rotationCallback.RotationChangedEvent -= RotationChangedEvent;
        }
    }
}
