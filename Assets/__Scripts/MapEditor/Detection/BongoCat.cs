using UnityEngine;
using Zenject;

public class BongoCat : MonoBehaviour
{
    [SerializeField] private Sprite dLdR;
    [SerializeField] private Sprite dLuR;
    [SerializeField] private Sprite uLdR;
    [SerializeField] private Sprite uLuR;
    [SerializeField] private AudioClip bongoCatAudioClip;
    [SerializeField] private AudioUtil audioUtil;
    [SerializeField] private bool Larm;
    [SerializeField] private bool Rarm;

    private float LarmTimeout;
    private float RarmTimeout;

    private SpriteRenderer comp;

    private AudioTimeSyncController atsc;
    private Settings settings;
    private PersistentUI persistentUI;

    [Inject]
    private void Construct(AudioTimeSyncController atsc, Settings settings, PersistentUI persistentUI)
    {
        this.atsc = atsc;
        this.settings = settings;
        this.persistentUI = persistentUI;
    }

    private void Start()
    {
        comp = GetComponent<SpriteRenderer>();
        comp.enabled = settings.BongoBoye;
    }

    public void ToggleBongo()
    {
        if (settings.BongoBoye)
        {
            persistentUI.DisplayMessage("Bongo cat disabled :(", PersistentUI.DisplayMessageType.BOTTOM);
        }
        else
        {
            audioUtil.PlayOneShotSound(bongoCatAudioClip);
            persistentUI.DisplayMessage("Bongo cat joins the fight!", PersistentUI.DisplayMessageType.BOTTOM);
        }
        settings.BongoBoye = !settings.BongoBoye;
        comp.enabled = settings.BongoBoye;
    }

    public void TriggerArm(BeatmapNote note, NotesContainer container)
    {
        //Ignore bombs here to improve performance.
        if (!settings.BongoBoye || note._type == BeatmapNote.NOTE_TYPE_BOMB) return;
        
        var next = container.UnsortedObjects.Find(x => x._time > note._time && ((BeatmapNote)x)._type == note._type);
        
        var timer = 0.125f;
        if (!(next is null))
        {
            float half = atsc.GetSecondsFromBeat((next._time - note._time) / 2f);
            timer = next != null ? Mathf.Clamp(half, 0.05f, 0.2f) : 0.125f; // clamp to accommodate sliders and long gaps between notes
        }
        
        switch (note._type)
        {
            case BeatmapNote.NOTE_TYPE_A:
                Larm = true;
                LarmTimeout = timer;
                break;
            case BeatmapNote.NOTE_TYPE_B:
                Rarm = true;
                RarmTimeout = timer;
                break;
        }
    }

    private void Update()
    {
        LarmTimeout -= Time.deltaTime;
        RarmTimeout -= Time.deltaTime;
        if (LarmTimeout < 0) Larm = false;
        if (RarmTimeout < 0) Rarm = false;

        if (Larm) comp.sprite = Rarm ? dLdR : dLuR;
        else comp.sprite = Rarm ? uLdR : uLuR;
    }
}
