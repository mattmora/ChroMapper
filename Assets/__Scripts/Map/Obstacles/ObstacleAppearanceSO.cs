using UnityEngine;
using Zenject;

[CreateAssetMenu(fileName = "ObstacleAppearanceSO", menuName = "Map/Appearance/Obstacle Appearance SO")]
public class ObstacleAppearanceSO : ScriptableObject
{
    public Color defaultObstacleColor = BeatSaberSong.DEFAULT_LEFTCOLOR;
    [SerializeField] private Color negativeWidthColor = Color.green;
    [SerializeField] private Color negativeDurationColor = Color.yellow;

    private Settings settings;

    [Inject]
    private void Construct(Settings settings)
    {
        this.settings = settings;
    }

    public void SetObstacleAppearance(BeatmapObstacleContainer obj, PlatformDescriptor platform = null)
    {
        if (platform != null) defaultObstacleColor = platform.colors.ObstacleColor;

        if (obj.obstacleData._duration < 0 && settings.ColorFakeWalls)
        {
            obj.SetColor(negativeDurationColor);
        }
        else
        {
            if (obj.obstacleData._customData != null)
            {
                Vector2 wallSize = obj.obstacleData._customData["_scale"]?.ReadVector2() ?? Vector2.one;
                if (wallSize.x < 0 || wallSize.y < 0 && settings.ColorFakeWalls)
                {
                    obj.SetColor(negativeWidthColor);
                }
                else
                {
                    obj.SetColor(defaultObstacleColor);
                }
                if (obj.obstacleData._customData.HasKey("_color"))
                {
                    obj.SetColor(obj.obstacleData._customData["_color"].ReadColor(defaultObstacleColor));
                }
            }
            else if (obj.obstacleData._width < 0 && settings.ColorFakeWalls)
            {
                obj.SetColor(negativeWidthColor);
            }
            else
            {
                obj.SetColor(defaultObstacleColor);
            }
        }
    }
}
