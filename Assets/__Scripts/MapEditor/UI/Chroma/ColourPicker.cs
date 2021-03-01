using UnityEngine.UI;
using UnityEngine;
using Zenject;

public class ColourPicker : MonoBehaviour
{
    [SerializeField] private ColorPicker picker;
    [SerializeField] private ToggleColourDropdown dropdown;
    [SerializeField] private EventsContainer eventsContainer;
    [SerializeField] private Toggle toggle;
    [SerializeField] private Toggle placeChromaToggle;

    private Settings settings;

    [Inject]
    private void Construct(Settings settings)
    {
        this.settings = settings;
    }

    // Start is called before the first frame update
    private void Start()
    {
        SelectionController.ObjectWasSelectedEvent += SelectedObject;
        toggle.isOn = settings.PickColorFromChromaEvents;
        placeChromaToggle.isOn = settings.PlaceChromaColor;
    }

    private void OnDestroy()
    {
        SelectionController.ObjectWasSelectedEvent -= SelectedObject;
    }

    public void UpdateColourPicker(bool enabled)
    {
        settings.PickColorFromChromaEvents = enabled;
    }

    private void SelectedObject(BeatmapObject obj)
    {
        if (!settings.PickColorFromChromaEvents || !dropdown.Visible) return;
        if (obj._customData?.HasKey("_color") ?? false)
        {
            picker.CurrentColor = obj._customData["_color"];
        }
        if (!(obj is MapEvent e)) return;
        if (e._value >= ColourManager.RGB_INT_OFFSET)
        {
            picker.CurrentColor = ColourManager.ColourFromInt(e._value);
        }
        else if (e._lightGradient != null)
        {
            picker.CurrentColor = e._lightGradient.StartColor;
        }
    }
}
