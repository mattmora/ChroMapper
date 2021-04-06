using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zenject;

public class BeatmapObjectCollectionPool<TObject, TContainer> : IMemoryPool<TObject, TContainer>, IFactory, IFactory<TObject, TContainer>, IDisposable
    where TObject : BeatmapObject
    where TContainer : BeatmapObjectContainer
{
    private readonly LinkedList<TContainer> loadedContainers = new LinkedList<TContainer>();
    private readonly Queue<TContainer> pooledContainers = new Queue<TContainer>();

    private IFactory<TContainer> factory;

    [Inject]
    private void Construct(IFactory<TContainer> factory, MemoryPoolSettings settings)
    {
        this.factory = factory;
        Resize(settings.InitialSize);

#if UNITY_EDITOR
        StaticMemoryPoolRegistry.Add(this);
#endif
    }

    public IEnumerable<TContainer> ActiveItems => loadedContainers;

    public IEnumerable<TContainer> PooledItems => pooledContainers;

    public int NumTotal => NumActive + NumInactive;

    public int NumActive => loadedContainers.Count;

    public int NumInactive => pooledContainers.Count;

    public Type ItemType => typeof(TContainer);

    public void Clear() => Resize(0);

    public TContainer Spawn(BeatmapObject item) => Spawn(item as TObject); 

    public TContainer Spawn(TObject item)
    {
        if (!pooledContainers.Any()) ExpandBy(1);

        var container = pooledContainers.Dequeue();
        loadedContainers.AddLast(container);
        container.SafeSetActive(true);
        container.objectData = item;
        container.transform.localEulerAngles = Vector3.zero;
        container.UpdateGridPosition();
        item.HasAttachedContainer = true;
        return container;
    }

    public void Despawn(BeatmapObjectContainer item) => Despawn(item as TContainer);

    public void Despawn(TContainer item)
    {
        if (loadedContainers.Remove(item))
        {
            pooledContainers.Enqueue(item);
            item.objectData.HasAttachedContainer = false;
            item.objectData = null;
            item.SafeSetActive(false);
        }
    }

    public TContainer Despawn(TObject item)
    {
        var container = loadedContainers.Where(x => x.objectData == item).FirstOrDefault();
        if (container)
        {
            Despawn(container);
        }
        return container;
    }

    public void Despawn(object obj)
    {
        if (obj is TObject beatmapObject) Despawn(beatmapObject);
        else if (obj is TContainer container) Despawn(container);
        else throw new NotSupportedException();
    }

    public void ExpandBy(int numToAdd)
    {
        for (int i = 0; i < numToAdd; i++)
        {
            var newItem = factory.Create();
            newItem.Setup();
            pooledContainers.Enqueue(newItem);
        }
    }

    public void ShrinkBy(int numToRemove)
    {
        for (int i = 0; i < numToRemove; i++)
        {
            var toRemove = pooledContainers.Dequeue();
            UnityEngine.Object.Destroy(toRemove.gameObject);
        }
    }

    public void Resize(int desiredPoolSize)
    {
        if (desiredPoolSize < NumInactive) ShrinkBy(NumInactive - desiredPoolSize);
        else if (desiredPoolSize > NumInactive) ExpandBy(desiredPoolSize - NumInactive);
    }

    public TContainer Create(TObject item) => Spawn(item);

    public void Dispose()
    {
#if UNITY_EDITOR
        StaticMemoryPoolRegistry.Remove(this);
#endif
    }
}
