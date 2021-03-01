using UnityEngine;

[CreateAssetMenu(fileName = "ImageList", menuName = "ImageList")]
public class ImageList : ScriptableObject {

    public Sprite[] sprites;
    public Sprite DarkSprite;
    [Space]
    [Header("Kiwi dont kill me please")]
    public Sprite DefaultPlatform;
    public Sprite TrianglePlatform;
    public Sprite BigMirrorPlatform;
    public Sprite NicePlatform;
    public Sprite KDAPlatform;
    public Sprite VaporFramePlatform;
    public Sprite BigMirrorV2Platform;
    public Sprite FailsafeBackground;
    
    public Sprite GetRandomSprite() => DarkSprite;

    /* I've decided to remove random BG functionality, because I don't think it matches well with CM's new UI "language"
     * 
     * The UI has matured from being fun with colors all around, to being more serious and professional,
     * designed by people who are actually competent with UI work, unlike myself.
     * 
     * As such, I felt that the random rainbow Chroma backgrounds that served as backgrounds, as well as the
     * environment-specific loading transitions, to conflict with the new direction of CM's UI.
     * 
     * Sorry if this dissapoints anyone, especially Aalto.
     */
}
