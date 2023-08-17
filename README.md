This is a fork of the Beat Saber map editor ChroMapper, featuring a very rough prototype implementation of built-in BPM and offset detection on the `bpm` branch.

The implementation is based on that of ArrowVortex by Bram van de Wetering, which is the current standard in Beat Saber mapping for BPM detection and syncing, and it performs comparably well.

A partial source of the AV implementation (or at least some version of it; I suspect the released version of AV is slightly different) and a paper by van de Wetering describing the algorithm can be found at https://github.com/nathanstep55/bpm-offset-detector, in the `legacy` and `original-paper` folders respectively.

## Try It Out
0. Setup ChroMapper and the Unity project (see the [ChroMapper Wiki](https://chromapper.atlassian.net/wiki/spaces/UG/overview) and [build guide](BUILD.md)).
1. Open up the project in Unity.
2. Press play then select the song to analyze.
3. In the Song Edit Menu, press Save then look at the Unity Console.
4. After several seconds, 1 to 5 results should be logged, each including a **BPM**, a **Fitness**, a **Beat Dur**, and an **Offset**. 

For example:
`[BPM:186] [Fitness:36.45967] [Beat Dur:0.3225806] [Offset:0.001816253]`

**BPM** is as it says, **Fitness** is the confidence of the algorithm in that BPM, **Beat Dur** is the duration of a beat in seconds at the BPM, and **Offset** is the offset in seconds of the detected beat grid from a beat grid at the same BPM aligned with the start of the audio file. 

Note that the results are in order of Fitness, but the correct BPM may not always be the first result or only a half, double, or in rare cases 2/3 or 4/3 ratio of the correct BPM may be present. Also, Fitness is relative to the other results but does not have a meaningful unit and Offset will always be in the range of -halfBeatDur to +halfBeatDur.

## Relevant Files and Changes
The main BPM detection code is in `Assets/__Scripts/BPM`.
- `SyncAnalysis.cs` contains the core algorithm.
- `SyncAnalyser.cs` is a minimal MonoBehaviour container for `SyncAnalysis`. 
- The `Aubio` folder contains my manual port of code from aubio, a dependency of the original code that handles the actual DSP (see the **Issues** section below for more on why I ported it as opposed to other options). *Notably I did not port the original FFT implementation and instead used the FFT that already exists in Chromapper.*

I added MathNet.Numerics.dll to the Plugins folder for its polynomial fit function which is needed to evaluate BPM candidates. It's a managed plugin which should work cross-platform although I haven't tested. It could likely be replaced with another solution if necessary.

Lastly, I've edited the `02_SongEditMenu` scene for quick and dirty testing. The only changes are an added `SyncAnalyser` object at the top level with the corresponding script and the addition of its Analyse function to the `OnClick()` list of the `Save and Exit Button` object (under `Canvas > SongInfoPanel > Save > Button Layout > Button layout`), so that when Save is clicked, the analysis runs on the current song.

## The Algorithm

For more detail, see the paper in the `original-paper` folder of https://github.com/nathanstep55/bpm-offset-detector.
My explanation for BPM estimation is as follows:
1. **Find the onsets.** There quite a bit to this and it's important it works well but both this implementation and the original mostly rely on aubio. See section 3.2 in the paper for the full onset extraction breakdown. 
2. **Assign a strength to each onset based on the energy of nearby samples.** This is not mentioned in the paper, but is in the original code and seems to make a big difference.
3. **Test how well the onsets align to a range of BPMs.** This is done with histograms of lengths corresponding the beat length of each BPM. The more clustered the onsets are within these histograms, the better aligned the BPM is. In other words, you modulo the time of each onset by the beat length of each BPM, and BPMs with more clustered results are better. Onset strength also factors in, and this is also where we need to poly fit to adjust for a bias towards higher BPMs (higher BPM = smaller intervals = the same number of onsets wrapped to a smaller range = naturally more clustering).
4. **Refine the best candidate BPMs.** Essentially we repeat the previous step, but for BPMs that are close to the best candidates but weren't previously tested.
5. **Take the best aligned BPMs.** 

Offset estimation is mostly the same process, except instead of comparing BPMs against each other, you look at the position of the strongest onset clusters in the histogram of a single BPM and see how offset it is from 0 and half the beat length, which correspond to aligned beats and offbeats.

## Issues
### Performance 
Probably the main problem with this implementation compared to AV is performance. While it's not unbearably slowed, it could definitely be improved. I believe the main issues are the use of ported Aubio code instead of a plugin and the lack of multithreading, particularly during the FFT and initial BPM testing (`FillCoarseIntervals()` in `SyncAnalysis.cs`)

Aubio does exist/can be built as plugins but because it's not C#, cross-platform support would require a plugin for each platform and a C# interface like https://github.com/aybe/aubio.net/tree/develop. I couldn't figure it all out easily so chose to just manually translate the required parts, but if you want to try the plugin approach, you can get aubio at https://aubio.org/download.

### Dealing with Offset
While the offset estimation seems to work and be accurate, it's a bit trickier than BPM to interpret and make useful to the user. I'm not sure how best to approach it and honestly haven't given it too much thought, especially as I'm not up-to-date on exactly what BPM and offset look like in map data. Part of me questions why it's even necessary to know the offset precisely. Would it not work to just add/remove a roughly good amount of silence from the file, then get the tempo, then add a BPM event at the first beat to align the grid? I guess eyeballing the first beat isn't always reliable. I'm sure there is a good approach to using the offset information, it would just be more involved than what I've done here.

### Minor Notes
In my testing, **this implementation is comparably accurate to AV, but results do sometimes differ slightly**. There are many possible reasons for this: some of the details of AV's algorithms are unclear (exact onset detection method, buffer sizes, etc.), the FFT might be different as mentioned, and I made some parameter adjustments to parts of the algorithm that I believe make it more accurate in niche cases (I've tried to make note of these in the large comment block at the top of `SyncAnalysis.cs`). **I don't really think this is a problem** - while results are sometimes different they don't seem worse - **but it's worth noting**.

The original implementation and the paper suggest testing a range of 89 to 205 BPM but provide no reason. I think it's a very reasonable range but wonder if there is a more appropriate range for this use case. The final BPM selection is user-driven, meaning the user can halve or double results as needed, so a smaller range, which could reduce analysis times, might not cause problems. It also might make sense to expose the test range to the user as a preference, given that they likely have a general sense of what the BPM could be.

`SyncAnalysis.cs` is big and kind of a mess because it's essentially a translation of `FindTempo.cpp` (the AV source file) which is likewise big and a mess. It should probably be broken up.
