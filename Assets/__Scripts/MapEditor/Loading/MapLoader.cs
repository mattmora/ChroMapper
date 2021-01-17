using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Zenject;

public class MapLoader : MonoBehaviour, IAddLoadRoutine
{
    [SerializeField] TracksManager manager;
    [SerializeField] NoteLanesController noteLanesController;
    [Space]
    [SerializeField] Transform containerCollectionsContainer;

    private int noteLaneSize = 2;
    private int noteLayerSize = 3;

    private BeatSaberMap map;
    private Settings settings;
    private PersistentUI persistentUI;

    public IEnumerable<IEnumerator> AdditionalLoadRoutines => new[] { HardRefresh() };

    [Inject]
    private void Construct(BeatSaberMap map, Settings settings, PersistentUI persistentUI)
    {
        this.map = map;
        this.settings = settings;
        this.persistentUI = persistentUI;
    }

    public void UpdateMapData(BeatSaberMap map)
    {
        this.map = map;
    }

    public IEnumerator HardRefresh()
    {
        if (settings.Load_Notes) yield return StartCoroutine(LoadObjects(map._notes));
        if (settings.Load_Obstacles) yield return StartCoroutine(LoadObjects(map._obstacles));
        if (settings.Load_Events) yield return StartCoroutine(LoadObjects(map._events));
        if (settings.Load_Others)
        {
            yield return StartCoroutine(LoadObjects(map._BPMChanges));
            yield return StartCoroutine(LoadObjects(map._customEvents));
        }

        persistentUI.LevelLoadSliderLabel.text = "Finishing up...";
        
        manager.RefreshTracks();
        
        SelectionController.RefreshMap();
        
        persistentUI.LevelLoadSlider.gameObject.SetActive(false);
    }

    public IEnumerator LoadObjects<T>(IEnumerable<T> objects) where T : BeatmapObject
    {
        if (!objects.Any()) yield break;

        var collection = BeatmapObjectContainerCollection.GetCollectionForType(objects.First().beatmapType);

        if (collection == null) yield break;
        
        foreach (var obj in collection.LoadedObjects.ToArray()) collection.DeleteObject(obj, false, false);
        
        persistentUI.LevelLoadSlider.gameObject.SetActive(true);
        
        collection.LoadedObjects = new SortedSet<BeatmapObject>(objects, new BeatmapObjectComparer());
        collection.UnsortedObjects = collection.LoadedObjects.ToList();
        
        UpdateSlider<T>();
        
        if (typeof(T) == typeof(BeatmapNote) || typeof(T) == typeof(BeatmapObstacle))
        {
            for (int i = 0; i < objects.Count(); i++)
            {
                var data = objects.ElementAt(i);

                if (data is BeatmapNote noteData)
                {
                    if (noteData._lineIndex >= 1000 || noteData._lineIndex <= -1000 || noteData._lineLayer >= 1000 || noteData._lineLayer <= -1000) continue;
                    if (2 - noteData._lineIndex > noteLaneSize) noteLaneSize = 2 - noteData._lineIndex;
                    if (noteData._lineIndex - 1 > noteLaneSize) noteLaneSize = noteData._lineIndex - 1;
                    if (noteData._lineLayer + 1 > noteLayerSize) noteLayerSize = noteData._lineLayer + 1;
                }
                else if (data is BeatmapObstacle obstacleData)
                {
                    if (obstacleData._lineIndex >= 1000 || obstacleData._lineIndex <= -1000) continue;
                    if (2 - obstacleData._lineIndex > noteLaneSize) noteLaneSize = 2 - obstacleData._lineIndex;
                    if (obstacleData._lineIndex - 1 > noteLaneSize) noteLaneSize = obstacleData._lineIndex - 1;
                }
            }

            if (Settings.NonPersistentSettings.ContainsKey("NoteLanes"))
            {
                Settings.NonPersistentSettings["NoteLanes"] = (noteLaneSize * 2).ToString();
            }
            else
            {
                Settings.NonPersistentSettings.Add("NoteLanes", (noteLaneSize * 2).ToString());
            }

            noteLanesController.UpdateNoteLanes((noteLaneSize * 2).ToString());
        }
        if (typeof(T) == typeof(MapEvent))
        {
            manager.RefreshTracks();
            EventsContainer events = collection as EventsContainer;
            events.AllRotationEvents = objects.Cast<MapEvent>().Where(x => x.IsRotationEvent).ToList();
            events.AllBoostEvents = objects.Cast<MapEvent>().Where(x => x._type == MapEvent.EVENT_TYPE_BOOST_LIGHTS).ToList();
        }
        collection.RefreshPool(true);
    }

    private void UpdateSlider<T>() where T : BeatmapObject
    {
        persistentUI.LevelLoadSliderLabel.text = $"Loading {typeof(T).Name}s... ";
        persistentUI.LevelLoadSlider.value = 1;
    }
}
