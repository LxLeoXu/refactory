using UnityEngine;

// Originalne hudobne loopy: Base / Dungeon / Boss.
// Nemení existujuce komponenty hraca ani dungeonu.
[DisallowMultipleComponent]
public class GJGameMusic : MonoBehaviour
{
    public enum MusicArea { Base, Dungeon, Boss }

    [Header("Hudba")]
    public AudioClip baseMusic;
    public AudioClip dungeonMusic;
    public AudioClip bossMusic;

    [Header("Nastavenia")]
    [Range(0f, 1f)] public float volume = 0.32f;
    [Min(0.05f)] public float crossfadeSeconds = 1.0f;
    public bool autoDetectRoom = true;
    public bool persistBetweenScenes = true;

    private static GJGameMusic instance;
    private AudioSource first;
    private AudioSource second;
    private AudioSource current;
    private Transform player;
    private float nextCheck;
    private MusicArea activeArea;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        if (persistBetweenScenes) DontDestroyOnLoad(gameObject);

        first = GetComponent<AudioSource>();
        if (first == null) first = gameObject.AddComponent<AudioSource>();
        second = gameObject.AddComponent<AudioSource>();
        Configure(first);
        Configure(second);
    }

    static void Configure(AudioSource source)
    {
        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;  // 2D hudba, nezavisi od vzdialenosti
        source.volume = 0f;
    }

    void Start()
    {
        ChangeMusic(MusicArea.Base);
    }

    void Update()
    {
        if (autoDetectRoom && Time.unscaledTime >= nextCheck)
        {
            nextCheck = Time.unscaledTime + 0.35f;
            DetectRoom();
        }

        float step = Time.unscaledDeltaTime * Mathf.Max(0.01f, volume) /
                     Mathf.Max(0.05f, crossfadeSeconds);
        if (first != null) Fade(first, step);
        if (second != null) Fade(second, step);
    }

    void Fade(AudioSource source, float step)
    {
        float target = source == current ? volume : 0f;
        source.volume = Mathf.MoveTowards(source.volume, target, step);
        if (source != current && source.isPlaying && source.volume <= 0.0001f)
            source.Stop();
    }

    void DetectRoom()
    {
        if (player == null)
        {
            GameObject found = GameObject.FindWithTag("Player");
            if (found != null) player = found.transform;
        }

        if (player == null)
        {
            ChangeMusic(MusicArea.Base);
            return;
        }

        Room[] rooms = FindObjectsByType<Room>(FindObjectsSortMode.None);
        for (int i = 0; i < rooms.Length; i++)
        {
            Room room = rooms[i];
            if (room == null || !room.isActiveAndEnabled) continue;
            BoxCollider2D trigger = room.GetComponent<BoxCollider2D>();
            if (trigger == null || !trigger.bounds.Contains(player.position)) continue;

            bool bossFight = room.type == RoomType.Boss && !room.IsCleared;
            ChangeMusic(bossFight ? MusicArea.Boss : MusicArea.Dungeon);
            return;
        }

        ChangeMusic(MusicArea.Base);
    }

    void ChangeMusic(MusicArea area)
    {
        AudioClip clip = area == MusicArea.Boss ? bossMusic :
                         area == MusicArea.Dungeon ? dungeonMusic : baseMusic;
        if (clip == null) return;
        if (current != null && current.clip == clip && current.isPlaying)
        {
            activeArea = area;
            return;
        }
        AudioSource incoming = current == first ? second : first;
        incoming.Stop();
        incoming.clip = clip;
        incoming.volume = 0f;
        incoming.Play();
        current = incoming;
        activeArea = area;
    }

    [ContextMenu("Test - Base Music")]
    public void PlayBase()
    {
        autoDetectRoom = false;
        ChangeMusic(MusicArea.Base);
    }

    [ContextMenu("Test - Dungeon Music")]
    public void PlayDungeon()
    {
        autoDetectRoom = false;
        ChangeMusic(MusicArea.Dungeon);
    }

    [ContextMenu("Test - Boss Music")]
    public void PlayBoss()
    {
        autoDetectRoom = false;
        ChangeMusic(MusicArea.Boss);
    }

    [ContextMenu("Use Automatic Room Detection")]
    public void EnableAutomaticMusic()
    {
        autoDetectRoom = true;
        nextCheck = 0f;
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }
}
