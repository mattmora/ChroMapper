using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

/// <summary>
/// Thanks to SheetCode for being a huge help in making this work!
/// </summary>
public class SpectrogramChunk : MonoBehaviour
{
    private static readonly int Rotation = Shader.PropertyToID("_Rotation");
    private static readonly Vector2 spectrogramScale = new Vector2(4f, 0.1f);

    private WaveformGenerator waveform;
    private MeshRenderer meshRenderer;
    private Material meshMaterial;
    private Gradient colorHeightGradient;
    private float[][] localData;
    private int chunkID;
    private float min;
    private float max = 1;

    private Texture2D texture;
    // Reduces a seemingly expensive call to the "transform" property
    private Transform cachedTransform;

    private AudioTimeSyncController atsc;
    private BeatSaberSong song;
    private Settings settings;

    [Inject]
    private void Construct(AudioTimeSyncController atsc, BeatSaberSong song, Settings settings)
    {
        this.atsc = atsc;
        this.song = song;
        this.settings = settings;
    }

    private void Start()
    {
        cachedTransform = transform;
        gameObject.layer = 12;
        meshRenderer = GetComponent<MeshRenderer>();
        meshMaterial = meshRenderer.material;

        EditorScaleController.EditorScaleChangedEvent += EditorScaleChanged;
        atsc.OnTimeChanged += TimeUpdated;

        EditorScaleChanged(EditorScaleController.EditorScale);
        TimeUpdated();
    }

    private void EditorScaleChanged(float obj)
    {
        // Beats per centisecond, only needed for the 3d waveform. I don't know why
        float bpcs = song.beatsPerMinute / (60f * 100);
        
        transform.localPosition = new Vector3(0, -0.15f,
            (chunkID + (waveform.WaveformType == 2 ? bpcs : 0)) * (EditorScaleController.EditorScale * BeatmapObjectContainerCollection.ChunkSize));
        
        transform.localScale = new Vector3(spectrogramScale.x, spectrogramScale.y,
            BeatmapObjectContainerCollection.ChunkSize * EditorScaleController.EditorScale);
    }

    private void OnDestroy()
    {
        atsc.OnTimeChanged -= TimeUpdated;
        EditorScaleController.EditorScaleChangedEvent -= EditorScaleChanged;
    }

    public void UpdateMesh(float[][] data, Texture2D colors, int chunkID, WaveformGenerator gen, Gradient gradient)
    {
        localData = data;
        texture = colors;
        this.chunkID = chunkID;
        colorHeightGradient = gradient;
        waveform = gen;
        ReCalculateMesh();
    }

    private void TimeUpdated()
    {   
        int nearestChunk = (int)Math.Round(atsc.CurrentBeat / (double)BeatmapObjectContainerCollection.ChunkSize
            , MidpointRounding.AwayFromZero);
        
        bool enabled = chunkID > nearestChunk - settings.ChunkDistance && chunkID < nearestChunk + settings.ChunkDistance;
        
        if (meshRenderer.enabled != enabled) meshRenderer.enabled = enabled;

        if (enabled) meshMaterial.SetFloat(Rotation, cachedTransform.rotation.eulerAngles.y);
    }

    private void ReCalculateMesh()
    {
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        mesh.Clear();

        List<Vector3> verts = new List<Vector3>();
        List<int> triangles = new List<int>();

        float xRange = 1;
        int xSamples = Math.Min(50, localData[0].Length) - 1;
        int zSamples = 299;

        for (int l = 0; l < zSamples; l++)
        {
            int m = (localData.Length - 1) * l / zSamples;
            int m2 = (localData.Length - 1) * (l + 1) / zSamples;

            float[] currentVolumes = localData[m];
            float[] previousVolumes = localData[m2];

            float zBandValue = (float)m / (localData.Length - 1);
            float zBandNextValue = (float)(m2) / (localData.Length - 1);

            for (int k = 0; k < xSamples; k++)
            {
                int i = (currentVolumes.Length - 1) * k / xSamples;
                int i2 = (currentVolumes.Length - 1) * (k + 1) / xSamples;

                // calculating x position 
                float x = (float)i / (currentVolumes.Length - 2) * xRange;
                float xNext = (float)i2 / (currentVolumes.Length - 2) * xRange;
                float volume = currentVolumes[i];
                float voulumeNext = currentVolumes[i2];

                // two volumes that was previous
                float volumePrevious = previousVolumes[i];
                float volumeNextPrevious = previousVolumes[i2];
                if (volume > max) max = volume;
                if (volume < min) min = volume;

                if (m == 0)
                    GenerateFrontFace(x, xNext, volume, voulumeNext, verts, triangles, zBandValue);

                // connection with previous band

                // adding verst connecting this band with the next one
                verts.Add(new Vector3(x, volume, zBandValue));
                verts.Add(new Vector3(xNext, voulumeNext, zBandValue));
                verts.Add(new Vector3(x, volumePrevious, zBandNextValue));
                verts.Add(new Vector3(xNext, volumeNextPrevious, zBandNextValue));

                int start_point = verts.Count - 4;
                // adding 2 triangles using this vertex
                triangles.Add(start_point + 0);
                triangles.Add(start_point + 2);
                triangles.Add(start_point + 1);
                triangles.Add(start_point + 2);
                triangles.Add(start_point + 3);
                triangles.Add(start_point + 1);

                // left side
                if (i == 0)
                {
                    verts.Add(new Vector3(x, 0, zBandValue));
                    verts.Add(new Vector3(x, 0, zBandNextValue));
                    verts.Add(new Vector3(x, volume, zBandValue));
                    verts.Add(new Vector3(x, volumePrevious, zBandNextValue));
                    start_point = verts.Count - 4;
                    // adding 2 triangles using this vertex
                    triangles.Add(start_point + 0);
                    triangles.Add(start_point + 1);
                    triangles.Add(start_point + 2);
                    triangles.Add(start_point + 1);
                    triangles.Add(start_point + 3);
                    triangles.Add(start_point + 2);
                }

                // right side
                if (i == currentVolumes.Length - 2)
                {
                    verts.Add(new Vector3(xNext, 0, zBandValue));
                    verts.Add(new Vector3(xNext, 0, zBandNextValue));
                    verts.Add(new Vector3(xNext, volume, zBandValue));
                    verts.Add(new Vector3(xNext, volumePrevious, zBandNextValue));
                    start_point = verts.Count - 4;
                    // adding 2 triangles using this vertex
                    triangles.Add(start_point + 0);
                    triangles.Add(start_point + 2);
                    triangles.Add(start_point + 1);
                    triangles.Add(start_point + 1);
                    triangles.Add(start_point + 2);
                    triangles.Add(start_point + 3);
                }

            }

        }
        mesh.vertices = verts.ToArray();
        mesh.triangles = triangles.ToArray();
        if (waveform.WaveformType == 2)
        {
            // 3D color the mesh
            List<Color> meshColors = new List<Color>(verts.Count);
            foreach (Vector3 vertex in verts)
            {
                float lerp = Mathf.InverseLerp(min, max, vertex.y);
                if (float.IsNaN(lerp)) lerp = 0;
                meshColors.Add(colorHeightGradient.Evaluate(lerp));
            }
            mesh.colors = meshColors.ToArray();
        }
        else
        {
            // In 2d we have too much data to draw in the mesh so we have to render a texture to color space between vertexes
            // Simplest UV ever
            Vector2[] uv = new Vector2[verts.Count];
            for (int i = 0; i < verts.Count; i++)
            {
                Vector3 it = verts[i];
                uv[i] = new Vector2(
                    Mathf.Clamp(it.z, 0.001f, 0.999f),
                    Mathf.Clamp(it.x, 0.001f, 0.999f)
                );
            }
            mesh.uv = uv;

            // apply texture to mesh
            Material customMaterial = new Material(Shader.Find("Shader Graphs/Spectrogram 2D"));
            // wait why am i not setting this to the same render queue as lights, pepega
            customMaterial.renderQueue = 2925;
            customMaterial.SetFloat("_Rotation", transform.rotation.eulerAngles.y);
            customMaterial.SetTexture("_MainTex", texture);
            GetComponent<MeshRenderer>().material = customMaterial;
        }

        mesh.RecalculateNormals();
    }

    private void GenerateFrontFace(float x, float x_next, float volume, float volume_next, List<Vector3> verts, List<int> triangles, float zBandValue)
    {
        verts.Add(new Vector3(x, 0, zBandValue));
        verts.Add(new Vector3(x, volume, zBandValue));
        verts.Add(new Vector3(x_next, 0, zBandValue));
        verts.Add(new Vector3(x_next, volume_next, zBandValue));
        int start_point = verts.Count - 4;
        triangles.Add(start_point + 0);
        triangles.Add(start_point + 1);
        triangles.Add(start_point + 2);
        triangles.Add(start_point + 1);
        triangles.Add(start_point + 3);
        triangles.Add(start_point + 2);
    }

    public class Factory : PlaceholderFactory<float[][], Texture2D, int, WaveformGenerator, Gradient, SpectrogramChunk>
    {
        private DiContainer container;
        private GameObject chunkPrefab;
        private Transform parentTransform;

        private Factory(DiContainer container, GameObject chunkPrefab, Transform parentTransform)
        {
            this.container = container;
            this.chunkPrefab = chunkPrefab;
            this.parentTransform = parentTransform;
        }

        public override SpectrogramChunk Create(float[][] toRender, Texture2D bandColors, int id, WaveformGenerator waveformGen, Gradient colorGradient)
        {
            var instantiate = container.InstantiatePrefabForComponent<SpectrogramChunk>(chunkPrefab, parentTransform);
            instantiate.UpdateMesh(toRender, bandColors, id, waveformGen, colorGradient);
            return instantiate;
        }
    }
}