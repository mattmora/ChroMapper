using System;
using UnityEngine;
using Zenject;

public class BeatmapObstacleContainer : BeatmapObjectContainer
{
    private static readonly int ColorTint = Shader.PropertyToID("_ColorTint");

    public override BeatmapObject objectData { get => obstacleData; set => obstacleData = (BeatmapObstacle)value; }

    public bool IsRotatedByNoodleExtensions => obstacleData._customData != null && (obstacleData._customData?.HasKey("_rotation") ?? false);

    public BeatmapObstacle obstacleData;

    private TracksManager manager;
    private Settings settings;
    private BeatSaberSong song;
    private BeatSaberSong.DifficultyBeatmap difficultyData;

    [Inject]
    public void Construct(TracksManager manager, Settings settings, BeatSaberSong song, BeatSaberSong.DifficultyBeatmap difficultyData)
    {
        this.manager = manager;
        this.settings = settings;
        this.song = song;
        this.difficultyData = difficultyData;
    }

    public override void UpdateGridPosition()
    {
        float duration = obstacleData._duration;
        Vector3 localRotation = Vector3.zero;

        //Take half jump duration into account if the setting is enabled.
        if (obstacleData._duration < 0 && settings.ShowMoreAccurateFastWalls)
        {
            float num = 60f / song.beatsPerMinute;
            float halfJumpDuration = 4;
            float songNoteJumpSpeed = difficultyData.noteJumpMovementSpeed;
            float songStartBeatOffset = difficultyData.noteJumpStartBeatOffset;

            while (songNoteJumpSpeed * num * halfJumpDuration > 18)
                halfJumpDuration /= 2;

            halfJumpDuration += songStartBeatOffset;

            if (halfJumpDuration < 1) halfJumpDuration = 1;

            duration -= duration * Mathf.Abs(duration / halfJumpDuration);
        }

        duration *= EditorScaleController.EditorScale; // Apply Editor Scale here since it can be overwritten by NE _scale Z

        if (obstacleData._customData != null)
        {
            if (obstacleData._customData.HasKey("_scale"))
            {
                if (obstacleData._customData["_scale"].Count > 2) //Apparently scale supports Z now, ok
                {
                    duration = obstacleData._customData["_scale"]?.ReadVector3().z ?? duration;
                }
            }
            if (obstacleData._customData.HasKey("_localRotation"))
            {
                localRotation = obstacleData._customData["_localRotation"]?.ReadVector3() ?? Vector3.zero;
            }
            if (obstacleData._customData.HasKey("_rotation"))
            {
                Track track = null;
                if (obstacleData._customData["_rotation"].IsNumber)
                {
                    float rotation = obstacleData._customData["_rotation"];
                    track = manager.CreateTrack(rotation);
                }
                else if (obstacleData._customData["_rotation"].IsArray)
                {
                    track = manager.CreateTrack(obstacleData._customData["_rotation"].ReadVector3());
                }
                track?.AttachContainer(this);
            }
        }

        var bounds = obstacleData.GetShape();

        transform.localPosition = new Vector3(
            bounds.Position,
            bounds.StartHeight,
            obstacleData._time * EditorScaleController.EditorScale
            );
        transform.localScale = new Vector3(
            bounds.Width,
            bounds.Height,
            duration
            );
        if (localRotation != Vector3.zero)
        {
            transform.localEulerAngles = Vector3.zero;
            Vector3 side = transform.right.normalized * (bounds.Width / 2);
            Vector3 rectWorldPos = transform.position + side;

            transform.RotateAround(rectWorldPos, transform.right, localRotation.x);
            transform.RotateAround(rectWorldPos, transform.up, localRotation.y);
            transform.RotateAround(rectWorldPos, transform.forward, localRotation.z);
        }
    }

    public override void SetColor(Color? color)
    {
        if (color is null)
        {
            Debug.LogError("Attempted to set a null color for a wall.");
        }

        ModelMaterials.ForEach(m => m.SetColor(ColorTint, color ?? Color.red));
    }

    public class Pool : BeatmapObjectCollectionPool<BeatmapObstacle, BeatmapObstacleContainer> { }
}
