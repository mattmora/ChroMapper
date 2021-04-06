﻿using UnityEngine;
using TMPro;
using System.Linq;
using System.Collections.Generic;
using Zenject;
using SimpleJSON;

public class CreateEventTypeLabels : MonoBehaviour
{
    [SerializeField] private TMP_FontAsset availableAsset;
    [SerializeField] private TMP_FontAsset utilityAsset;
    [SerializeField] private TMP_FontAsset redAsset;
    [SerializeField] private GameObject layerInstantiate;
    [SerializeField] private DarkThemeSO darkTheme;
    [SerializeField] private RotationCallbackController rotationCallback;

    private bool loadedWithRotationEvents = false;

    public int NoRotationLaneOffset => loadedWithRotationEvents || rotationCallback.IsActive ? 0 : -2;

    private LightsManager[] lightingManagers;

    private readonly List<LaneInfo> laneObjs = new List<LaneInfo>();

    private Settings settings;

    [Inject]
	private void Construct(Settings settings, PlatformDescriptor descriptor, BeatSaberMap map)
    {
        this.settings = settings;

        loadedWithRotationEvents = map._events.Any(i => i.IsRotationEvent);

        lightingManagers = descriptor.LightingManagers;

        UpdateLabels(EventsContainer.PropMode.Off, MapEvent.EVENT_TYPE_RING_LIGHTS);
    }

    public void UpdateLabels(EventsContainer.PropMode propMode, int eventType, int lanes = 16)
    {
        foreach (Transform children in layerInstantiate.transform.parent.transform)
        {
            if (children.gameObject.activeSelf)
                Destroy(children.gameObject);
        }
        laneObjs.Clear();

        for (int i = 0; i < lanes; i++)
        {
            int modified = (propMode == EventsContainer.PropMode.Off ? EventTypeToModifiedType(i) : i) + NoRotationLaneOffset;
            if (modified < 0 && propMode == EventsContainer.PropMode.Off) continue;

            var laneInfo = new LaneInfo(i, propMode != EventsContainer.PropMode.Off ? i : modified);

            GameObject instantiate = Instantiate(layerInstantiate, layerInstantiate.transform.parent);
            instantiate.SetActive(true);
            instantiate.transform.localPosition = new Vector3(propMode != EventsContainer.PropMode.Off ? i : modified, 0, 0);
            laneObjs.Add(laneInfo);

            try
            {
                TextMeshProUGUI textMesh = instantiate.GetComponentInChildren<TextMeshProUGUI>();
                if (propMode != EventsContainer.PropMode.Off)
                {
                    textMesh.font = utilityAsset;
                    if (i == 0)
                    {
                        textMesh.text = "All Lights";
                        textMesh.font = redAsset;
                    }
                    else
                    {
                        textMesh.text = $"{lightingManagers[eventType].name} ID {i}";
                        if (i % 2 == 0)
                            textMesh.font = utilityAsset;
                        else
                            textMesh.font = availableAsset;
                    }
                }
                else
                {
                    switch (i)
                    {
                        case MapEvent.EVENT_TYPE_RINGS_ROTATE:
                            textMesh.font = utilityAsset;
                            textMesh.text = "Ring Rotation";
                            break;
                        case MapEvent.EVENT_TYPE_RINGS_ZOOM:
                            textMesh.font = utilityAsset;
                            textMesh.text = "Ring Zoom";
                            break;
                        case MapEvent.EVENT_TYPE_LEFT_LASERS_SPEED:
                            textMesh.text = "Left Laser Speed";
                            textMesh.font = utilityAsset;
                            break;
                        case MapEvent.EVENT_TYPE_RIGHT_LASERS_SPEED:
                            textMesh.text = "Right Laser Speed";
                            textMesh.font = utilityAsset;
                            break;
                        case MapEvent.EVENT_TYPE_EARLY_ROTATION:
                            textMesh.text = "Rotation (Include)";
                            textMesh.font = utilityAsset;
                            break;
                        case MapEvent.EVENT_TYPE_LATE_ROTATION:
                            textMesh.text = "Rotation (Exclude)";
                            textMesh.font = utilityAsset;
                            break;
                        case MapEvent.EVENT_TYPE_BOOST_LIGHTS:
                            textMesh.text = "Boost Lights";
                            textMesh.font = utilityAsset;
                            break;
                        default:
                            if (lightingManagers.Length > i)
                            {
                                LightsManager customLight = lightingManagers[i];
                                textMesh.text = customLight?.name;
                                textMesh.font = availableAsset;
                            }
                            else
                            {
                                Destroy(textMesh);
                                laneObjs.Remove(laneInfo);
                            }
                            break;
                    }
                }

                if (settings.DarkTheme)
                {
                    textMesh.font = darkTheme.TekoReplacement;
                }

                laneInfo.Name = textMesh.text;
            }
            catch { }
        }

        laneObjs.Sort();
    }

    public int LaneIdToEventType(int laneId)
    {
        return laneObjs[laneId].Type;
    }

    public int EventTypeToLaneId(int eventType)
    {
        return laneObjs.FindIndex(it => it.Type == eventType);
    }

    public int? LightIdsToPropId(int type, int[] lightID)
    {
        if (type >= lightingManagers.Length)
            return null;

        return lightingManagers[type].ControllingLights.FirstOrDefault(x => lightID.Contains(x.lightID))?.propGroup;
    }

    public int[] PropIdToLightIds(int type, int propID)
    {
        if (type >= lightingManagers.Length)
            return new int[0];

        return lightingManagers[type].ControllingLights.Where(x => x.propGroup == propID).Select(x => x.lightID).OrderBy(x => x).Distinct().ToArray();
    }
    
    public JSONArray PropIdToLightIdsJ(int type, int propID)
    {
        var result = new JSONArray();
        foreach (var lightingEvent in PropIdToLightIds(type, propID))
        {
            result.Add(lightingEvent);
        }
        return result;
    }

    public int EditorToLightID(int type, int lightID)
    {
        return lightingManagers[type].LightIDPlacementMap[lightID];
    }

    public int LightIDToEditor(int type, int lightID)
    {
        if (lightingManagers[type].LightIDPlacementMapReverse.ContainsKey(lightID))
        {
            return lightingManagers[type].LightIDPlacementMapReverse[lightID];
        }
        return -1;
    }

    private static int[] ModifiedToEventArray = { 14, 15, 0, 1, 2, 3, 4, 8, 9, 12, 13, 5, 6, 7, 10, 11 };
    private static int[] EventToModifiedArray = { 2, 3, 4, 5, 6, 11, 12, 13, 7, 8, 14, 15, 9, 10, 0, 1 };

    /// <summary>
    /// Turns an eventType to a modified type for organizational purposes in the Events Grid.
    /// </summary>
    /// <param name="eventType">Type usually found in a MapEvent object.</param>
    /// <returns></returns>
    public static int EventTypeToModifiedType(int eventType)
    {
        if (!EventToModifiedArray.Contains(eventType))
        {
            Debug.LogWarning($"Event Type {eventType} does not have a modified type");
            return eventType;
        }
        return EventToModifiedArray[eventType];
    }

    /// <summary>
    /// Turns a modified type to an event type to be stored in a MapEvent object.
    /// </summary>
    /// <param name="modifiedType">Modified type (Usually from EventPreview)</param>
    /// <returns></returns>
    public static int ModifiedTypeToEventType(int modifiedType)
    {
        if (!ModifiedToEventArray.Contains(modifiedType))
        {
            Debug.LogWarning($"Event Type {modifiedType} does not have a valid event type! WTF!?!?");
            return modifiedType;
        }
        return ModifiedToEventArray[modifiedType];
    }

    public int MaxLaneId()
    {
        return laneObjs.Count - 1;
    }
}
