using System.Collections;
using UnityEngine;
using Zenject;

public class WaveformGenerator : MonoBehaviour
{
    [SerializeField] private Transform spectroParent;
    [SerializeField] private AudioManager audioManager;
    [SerializeField] [GradientUsage(true)] private Gradient spectrogramHeightGradient;
    [SerializeField] [GradientUsage(true)] private Gradient spectrogramGradient2d;

    public int WaveformType;

    private WaveformData waveformData;

    private AudioTimeSyncController atsc;
    private SpectrogramChunk.Factory spectrogramFactory;
    private Settings settings;
    private AudioClip loadedClip;

    [Inject]
    private void Construct(AudioTimeSyncController atsc, SpectrogramChunk.Factory spectrogramFactory, Settings settings, AudioClip loadedClip)
    {
        this.atsc = atsc;
        this.spectrogramFactory = spectrogramFactory;
        this.settings = settings;
        this.loadedClip = loadedClip;
    }

    private IEnumerator Start()
    {
        WaveformType = settings.Waveform;
        
        if (WaveformType == 0) yield break;

        waveformData = new WaveformData();
        audioManager.SetSecondPerChunk(atsc.GetSecondsFromBeat(BeatmapObjectContainerCollection.ChunkSize));
        spectroParent.localPosition = new Vector3(0, 0, -atsc.offsetBeat * EditorScaleController.EditorScale * 2);

        // Start the background worker
        audioManager.Begin(WaveformType == 2, WaveformType == 2 ? spectrogramHeightGradient : spectrogramGradient2d, loadedClip, waveformData, atsc, BeatmapObjectContainerCollection.ChunkSize);

        // Loop while we have completed calulations waiting to be rendered
        //  or there are threads still running generating new chunks
        //  or we are still writing a previous save to disk (we save again at the end)
        while (audioManager.chunksComplete.Count > 0 || audioManager.IsAlive())
        {
            if (audioManager.chunksComplete.TryDequeue(out var chunkId))
            {
                var toRender = new float[audioManager.ColumnsPerChunk][];
                waveformData.GetChunk(chunkId, audioManager.ColumnsPerChunk, ref toRender);

                var gradient = WaveformType == 2 ? spectrogramHeightGradient : spectrogramGradient2d;
                spectrogramFactory.Create(toRender, waveformData.BandColors[chunkId], chunkId, this, gradient);

                // Wait 2 frames for smoooth
                yield return new WaitForEndOfFrame();
            }
            yield return new WaitForEndOfFrame();
        }

        Debug.Log("WaveformGenerator: Main thread done");
    }
}
