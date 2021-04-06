using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using Zenject;

public abstract class BeatmapObjectContainerCollection : MonoBehaviour
{
    public static readonly int ChunkSize = 5;

    public static float Epsilon = 0.001f;
    public static float TranslucentCull = -0.001f;

    public static string TrackFilterID { get; private set; } = null;

    protected static Dictionary<BeatmapObject.Type, BeatmapObjectContainerCollection> loadedCollections = new Dictionary<BeatmapObject.Type, BeatmapObjectContainerCollection>();

    /// <summary>
    /// Grab a <see cref="BeatmapObjectContainerCollection"/> whose <see cref="ContainerType"/> matches the given type.
    /// To grab an inherited class, consider using <see cref="GetCollectionForType{T}(BeatmapObject.Type)"/>.
    /// </summary>
    /// <param name="type">The specific type of <see cref="BeatmapObject"/> that the collection must contain.</param>
    /// <returns>A generic <see cref="BeatmapObjectContainerCollection"/>.</returns>
    public static BeatmapObjectContainerCollection GetCollectionForType(BeatmapObject.Type type)
    {
        loadedCollections.TryGetValue(type, out BeatmapObjectContainerCollection collection);
        return collection;
    }

    /// <summary>
    /// Grab a <see cref="BeatmapObjectContainerCollection"/> whose <see cref="ContainerType"/> matches the given type.
    /// </summary>
    /// <typeparam name="T">A specific inheriting class to cast to.</typeparam>
    /// <param name="type">The specific type of <see cref="BeatmapObject"/> that the collection must contain.</param>
    /// <returns>A casted <see cref="BeatmapObjectContainerCollection"/>.</returns>
    public static T GetCollectionForType<T>(BeatmapObject.Type type) where T : BeatmapObjectContainerCollection
    {
        loadedCollections.TryGetValue(type, out BeatmapObjectContainerCollection collection);
        return collection as T;
    }

    /// <summary>
    /// Refreshes pools of all active <see cref="BeatmapObjectContainerCollection"/>
    /// </summary>
    /// <param name="forceRefresh">Whether or not to forcefully recycle all containers and spawn them again. This will cause quite a bit of lag.</param>
    public static void RefreshAllPools(bool forceRefresh = false)
    {
        foreach (BeatmapObjectContainerCollection collection in loadedCollections.Values)
        {
            collection.RefreshPool(forceRefresh);
        }
    }

    public abstract BeatmapObject.Type ContainerType { get; }

    /// <summary>
    /// A sorted set of loaded BeatmapObjects that is garaunteed to be sorted by time.
    /// </summary>
    public SortedSet<BeatmapObject> LoadedObjects = new SortedSet<BeatmapObject>(new BeatmapObjectComparer());
    /// <summary>
    /// A list of unsorted BeatmapObjects. Recommended only for fast iteration.
    /// </summary>
    public List<BeatmapObject> UnsortedObjects = new List<BeatmapObject>();
    /// <summary>
    /// A dictionary of all active BeatmapObjectContainers by the data they are attached to.
    /// </summary>
    public Dictionary<BeatmapObject, BeatmapObjectContainer> LoadedContainers = new Dictionary<BeatmapObject, BeatmapObjectContainer>();

    public bool IgnoreTrackFilter;

    protected AudioTimeSyncController AudioTimeSyncController;
    protected BeatmapObjectCallbackController SpawnCallbackController;
    protected BeatmapObjectCallbackController DespawnCallbackController;
    protected Settings Settings;
    protected PersistentUI PersistentUI;

    [Inject]
    private void Construct(AudioTimeSyncController atsc,
        [Inject(Id = "SPAWN")] BeatmapObjectCallbackController spawnCallback,
        [Inject(Id = "DESPAWN")] BeatmapObjectCallbackController despawnCallback,
        Settings settings,
        PersistentUI persistentUI)
    {
        AudioTimeSyncController = atsc;
        SpawnCallbackController = spawnCallback;
        DespawnCallbackController = despawnCallback;
        Settings = settings;
        PersistentUI = persistentUI;
    }

    private void Start()
    {
        UpdateEpsilon(Settings.TimeValueDecimalPrecision);
        Settings.NotifyBySettingName("TimeValueDecimalPrecision", UpdateEpsilon);
        Settings.NotifyBySettingName("EditorScale", UpdateEpsilon);
    }

    /// <summary>
    /// Given a list of objects, remove all existing ones that conflict.
    /// </summary>
    /// <param name="newObjects">Enumerable of new objects</param>
    public void RemoveConflictingObjects(IEnumerable<BeatmapObject> newObjects) => RemoveConflictingObjects(newObjects, out _);

    /// <summary>
    /// Given a list of objects, remove all existing ones that conflict.
    /// </summary>
    /// <param name="newObjects">Enumerable of new objects</param>
    /// <param name="conflicting">Enumerable of all existing objects that were deleted as a conflict.</param>
    public void RemoveConflictingObjects(IEnumerable<BeatmapObject> newObjects, out List<BeatmapObject> conflicting)
    {
        int conflictingObjects = 0;
        conflicting = new List<BeatmapObject>();
        foreach (BeatmapObject newObject in newObjects)
        {
            Debug.Log($"Performing conflicting check at {newObject._time}");

            var localWindow = GetBetween(newObject._time - 0.1f, newObject._time + 0.1f);
            BeatmapObject conflict = localWindow.Where(x => x.IsConflictingWith(newObject)).FirstOrDefault();
            if (conflict != null)
            {
                conflicting.Add(conflict);
                conflictingObjects++;
            }
        }
        foreach (BeatmapObject conflict in conflicting) //Haha InvalidOperationException go brrrrrrrrr
        {
            DeleteObject(conflict, false, false);
        }
        Debug.Log($"Removed {conflictingObjects} conflicting {ContainerType}s.");
    }

    public SortedSet<BeatmapObject> GetBetween(float time, float time2)
    {
        // Events etc. can still have a sort order between notes
        var now = new BeatmapNote(time - 0.0000001f, 0, 0, 0, 0);
        var window = new BeatmapNote(time2 + 0.0000001f, 0, 0, 0, 0);
        return LoadedObjects.GetViewBetween(now, window);
    }

    private void UpdateEpsilon(object precision)
    {
        Epsilon = 1 / Mathf.Pow(10, Settings.TimeValueDecimalPrecision);
        TranslucentCull = -Settings.EditorScale * Epsilon;
    }

    protected void SetTrackFilter()
    {
        PersistentUI.ShowInputBox("Filter notes and obstacles shown while editing to a certain track ID.\n\n" +
            "If you dont know what you're doing, turn back now.", HandleTrackFilter);
    }

    private void HandleTrackFilter(string res)
    {
        TrackFilterID = (string.IsNullOrEmpty(res) || string.IsNullOrWhiteSpace(res)) ? null : res;
    }

    public abstract void RefreshPool(bool forceRefresh = false);

    public void SpawnObject(BeatmapObject obj, bool removeConflicting = true, bool refreshesPool = true) => SpawnObject(obj, out _, removeConflicting, refreshesPool);

    public abstract void SpawnObject(BeatmapObject obj, out List<BeatmapObject> conflicting, bool removeConflicting = true, bool refreshesPool = true);

    /// <summary>
    /// Grabs <see cref="LoadedObjects"/> with other potential orderings added in. 
    /// This should not be used unless saving into a map file. Use <see cref="LoadedObjects"/> instead.
    /// </summary>
    /// <returns>A list of sorted objects</returns>
    public virtual IEnumerable<BeatmapObject> GrabSortedObjects() => LoadedObjects;

    /// <summary>
    /// Given a <see cref="BeatmapObjectContainer"/>, delete its attached object.
    /// </summary>
    /// <param name="obj">To delete.</param>
    /// <param name="triggersAction">Whether or not it triggers a <see cref="BeatmapObjectDeletionAction"/></param>
    /// <param name="comment">A comment that provides further description on why it was deleted.</param>
    public void DeleteObject(BeatmapObjectContainer obj, bool triggersAction = true, string comment = "No comment.") => DeleteObject(obj.objectData, triggersAction, true, comment);

    /// <summary>
    /// Deletes a <see cref="BeatmapObject"/>.
    /// </summary>
    /// <param name="obj">To delete.</param>
    /// <param name="triggersAction">Whether or not it triggers a <see cref="BeatmapObjectDeletionAction"/></param>
    /// <param name="refreshesPool">Whether or not the pool will be refreshed as a result of this deletion.</param>
    /// <param name="comment">A comment that provides further description on why it was deleted.</param>
    public abstract void DeleteObject(BeatmapObject obj, bool triggersAction = true, bool refreshesPool = true, string comment = "No comment.");
}

public abstract class BeatmapObjectContainerCollection<TObject, TContainer, TPool> : BeatmapObjectContainerCollection
    where TObject : BeatmapObject
    where TContainer : BeatmapObjectContainer
    where TPool : BeatmapObjectCollectionPool<TObject, TContainer>
{
    public Transform GridTransform;
    public Transform PoolTransform;
    public bool UseChunkLoadingWhenPlaying = false;

    private float previousATSCBeat = -1;
    private int previousChunk = -1;

    private TPool pool;

    [Inject]
    public void Construct(TPool pool)
    {
        this.pool = pool;
    }

    private void Awake()
    {
        BeatmapObjectContainer.FlaggedForDeletionEvent += DeleteObject;
        if (loadedCollections.ContainsKey(ContainerType))
        {
            loadedCollections[ContainerType] = this;
        }
        else
        {
            loadedCollections.Add(ContainerType, this);
        }
        SubscribeToCallbacks();
    }

    /// <summary>
    /// Refreshes the pool, with lower and upper bounds being automatically defined by chunks or spawn/despawn offsets.
    /// </summary>
    /// <param name="forceRefresh">All currently active containers will be recycled, even if they shouldn't be.</param>
    public override void RefreshPool(bool forceRefresh = false)
    {
        float epsilon = Mathf.Pow(10, -9);
        if (AudioTimeSyncController.IsPlaying)
        {
            float spawnOffset = UseChunkLoadingWhenPlaying ? (2 * ChunkSize) : SpawnCallbackController.offset;
            float despawnOffset = UseChunkLoadingWhenPlaying ? (-2 * ChunkSize) : DespawnCallbackController.offset;
            RefreshPool(AudioTimeSyncController.CurrentBeat + despawnOffset - epsilon,
                AudioTimeSyncController.CurrentBeat + spawnOffset + epsilon, forceRefresh);
        }
        else
        {
            int nearestChunk = (int)Math.Round(previousATSCBeat / (double)ChunkSize, MidpointRounding.AwayFromZero);
            // Since ChunkDistance is the amount of total chunks, we divide by two so that the total amount of loaded chunks
            // both before and after the current one equal to the ChunkDistance setting
            int chunks = Mathf.RoundToInt(Settings.ChunkDistance / 2);
            RefreshPool((nearestChunk - chunks) * ChunkSize - epsilon,
                (nearestChunk + chunks) * ChunkSize + epsilon, forceRefresh);
        }
    }

    /// <summary>
    /// Refreshes the pool with a defined lower and upper bound.
    /// </summary>
    /// <param name="lowerBound">Objects below this point in time will not be given a container.</param>
    /// <param name="upperBound">Objects above this point in time will not be given a container.</param>
    /// <param name="forceRefresh">All currently active containers will be recycled, even if they shouldn't be.</param>
    public void RefreshPool(float lowerBound, float upperBound, bool forceRefresh = false)
    {
        foreach (var obj in UnsortedObjects)
        //for (int i = 0; i < LoadedObjects.Count; i++)
        {
            if (forceRefresh)
            {
                RecycleContainer(obj);
            }
            if (obj._time >= lowerBound && obj._time <= upperBound)
            {
                if (!obj.HasAttachedContainer) CreateContainerFromPool(obj);
            }
            else if (obj.HasAttachedContainer)
            {
                if (obj is BeatmapObstacle obs && obs._time < lowerBound && obs._time + obs._duration >= lowerBound) continue;
                RecycleContainer(obj);
            }
            if (obj is BeatmapObstacle obst && obst._time < lowerBound && obst._time + obst._duration >= lowerBound)
            {
                CreateContainerFromPool(obj);
            }
        }
    }

    /// <summary>
    /// Dequeues a container from the pool and attaches it to a provided <see cref="BeatmapObject"/>
    /// </summary>
    /// <param name="obj">Object to store within the container.</param>
    protected void CreateContainerFromPool(BeatmapObject obj)
    {
        if (obj.HasAttachedContainer) return;
        var dequeued = pool.Spawn(obj as TObject);
        UpdateContainerData(dequeued, obj as TObject);
        LoadedContainers.Add(obj, dequeued);
        dequeued.OutlineVisible = SelectionController.IsObjectSelected(obj);
        PluginLoader.BroadcastEvent<ObjectLoadedAttribute, BeatmapObjectContainer>(dequeued);
        OnContainerSpawn(dequeued, obj);
    }

    /// <summary>
    /// Recycles the container belonging to a provided <see cref="BeatmapObject"/>, putting it back into the container pool for future use.
    /// </summary>
    /// <param name="obj">Object whose container will be recycled.</param>
    protected void RecycleContainer(BeatmapObject obj)
    {
        if (!obj.HasAttachedContainer) return;
        var container = pool.Despawn(obj as TObject);
        OnContainerDespawn(container, obj);
        LoadedContainers.Remove(obj);
    }

    /// <summary>
    /// Deletes a <see cref="BeatmapObject"/>.
    /// </summary>
    /// <param name="obj">To delete.</param>
    /// <param name="triggersAction">Whether or not it triggers a <see cref="BeatmapObjectDeletionAction"/></param>
    /// <param name="refreshesPool">Whether or not the pool will be refreshed as a result of this deletion.</param>
    /// <param name="comment">A comment that provides further description on why it was deleted.</param>
    public override void DeleteObject(BeatmapObject obj, bool triggersAction = true, bool refreshesPool = true, string comment = "No comment.")
    {
        var removed = UnsortedObjects.Remove(obj);
        var removed2 = LoadedObjects.Remove(obj);

        if (removed && removed2)
        {
            //Debug.Log($"Deleting container with hash code {toDelete.GetHashCode()}");
            SelectionController.Deselect(obj, triggersAction);
            if (triggersAction) BeatmapActionContainer.AddAction(new BeatmapObjectDeletionAction(obj, comment));
            RecycleContainer(obj);
            if (refreshesPool) RefreshPool();
            OnObjectDelete(obj);
        }
        else
        {
            // The objects are not in the collection, but are still being removed.
            // This could be because of ghost blocks, so let's try forcefully recycling that container.
            Debug.LogError($"Object could not be deleted, please report this ({removed}, {removed2})");
        }
    }

    internal virtual void LateUpdate()
    {
        if ((AudioTimeSyncController.IsPlaying && !UseChunkLoadingWhenPlaying)
            || AudioTimeSyncController.CurrentBeat == previousATSCBeat) return;
        previousATSCBeat = AudioTimeSyncController.CurrentBeat;
        int nearestChunk = (int)Math.Round(previousATSCBeat / (double)ChunkSize, MidpointRounding.AwayFromZero);
        if (nearestChunk != previousChunk)
        {
            RefreshPool();
            previousChunk = nearestChunk;
        }
    }

    private void OnDestroy()
    {
        BeatmapObjectContainer.FlaggedForDeletionEvent -= DeleteObject;
        loadedCollections.Remove(ContainerType);
        UnsubscribeToCallbacks();
    }

    /// <summary>
    /// SSpawns an object into the collection.
    /// </summary>
    /// <param name="obj">To spawn.</param>
    /// <param name="conflicting">An enumerable of all objects that were deleted as a conflict.</param>
    /// <param name="removeConflicting">Whether or not <see cref="RemoveConflictingObjects(IEnumerable{BeatmapObject}, out IEnumerable{BeatmapObject})"/> will be called.</param>
    /// <param name="refreshesPool">Whether or not the pool will be refreshed.</param>
    public override void SpawnObject(BeatmapObject obj, out List<BeatmapObject> conflicting, bool removeConflicting = true, bool refreshesPool = true)
    {
        //Debug.Log($"Spawning object with hash code {obj.GetHashCode()}");
        if (removeConflicting)
        {
            RemoveConflictingObjects(new[] { obj }, out conflicting);
        }
        else
        {
            conflicting = new List<BeatmapObject>() { };
        }
        LoadedObjects.Add(obj);
        UnsortedObjects.Add(obj);
        OnObjectSpawned(obj);
        //Debug.Log($"Total object count: {LoadedObjects.Count}");
        if (refreshesPool)
        {
            RefreshPool();
        }
    }

    public virtual void RefreshContainerColors() { }

    protected virtual void UpdateContainerData(TContainer con, TObject obj) { }

    protected virtual void OnObjectDelete(BeatmapObject obj) { }

    protected virtual void OnObjectSpawned(BeatmapObject obj) { }

    protected virtual void OnContainerSpawn(BeatmapObjectContainer container, BeatmapObject obj) { }

    protected virtual void OnContainerDespawn(BeatmapObjectContainer container, BeatmapObject obj) { }

    internal abstract void SubscribeToCallbacks();

    internal abstract void UnsubscribeToCallbacks();
}
