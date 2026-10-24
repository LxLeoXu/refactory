using System.Collections.Generic;
using UnityEngine;

public enum RoomType { Start, Normal, Boss }

public class Room : MonoBehaviour
{
    public Vector2Int gridPos;
    public RoomType type;
    public bool IsCleared { get; private set; }

    const float WallThickness = 1f;
    const float DoorWidth = 3f;

    Vector2 size;
    bool[] connected;                                   // poradie: N, S, E, W
    readonly GameObject[] doors = new GameObject[4];

    GameObject[] enemyPrefabs;
    GameObject bossPrefab;
    Vector2Int enemyRange;
    CameraFollow cameraFollow;

    readonly List<GameObject> alive = new List<GameObject>();
    bool entered;
    bool locked;

    static Sprite whiteSprite;

    static Sprite White
    {
        get
        {
            if (whiteSprite == null)
            {
                Texture2D tex = Texture2D.whiteTexture;
                whiteSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f), tex.width);   // 1 sprite = 1 unit
            }
            return whiteSprite;
        }
    }

    public void Init(Vector2Int gridPos, RoomType type, Vector2 size, bool[] connected,
        GameObject[] enemyPrefabs, GameObject bossPrefab, Vector2Int enemyRange,
        CameraFollow cameraFollow, Color floorColor, Color wallColor, Color doorColor)
    {
        this.gridPos = gridPos;
        this.type = type;
        this.size = size;
        this.connected = connected;
        this.enemyPrefabs = enemyPrefabs;
        this.bossPrefab = bossPrefab;
        this.enemyRange = enemyRange;
        this.cameraFollow = cameraFollow;

        Build(floorColor, wallColor, doorColor);
    }

    void Build(Color floorColor, Color wallColor, Color doorColor)
    {
        float hw = size.x / 2f;
        float hh = size.y / 2f;
        float t = WallThickness;
        float gap = DoorWidth;

        Box("Floor", Vector2.zero, size, floorColor, -10, false);

        // steny s dierou uprostred každej strany
        float segW = (size.x - gap) / 2f;
        float segH = (size.y - gap) / 2f;
        float sx = gap / 2f + segW / 2f;
        float sy = gap / 2f + segH / 2f;

        foreach (int s in new[] { 1, -1 })
        {
            Box("Wall", new Vector2(sx, s * (hh - t / 2f)), new Vector2(segW, t), wallColor, -5, true);
            Box("Wall", new Vector2(-sx, s * (hh - t / 2f)), new Vector2(segW, t), wallColor, -5, true);
            Box("Wall", new Vector2(s * (hw - t / 2f), sy), new Vector2(t, segH), wallColor, -5, true);
            Box("Wall", new Vector2(s * (hw - t / 2f), -sy), new Vector2(t, segH), wallColor, -5, true);
        }

        // dvere vypĺňajú diery. Bez susedy = trvalá stena, so susedom = otvorené, kým sa nezamkne
        doors[0] = Box("Door_N", new Vector2(0, hh - t / 2f), new Vector2(gap, t), connected[0] ? doorColor : wallColor, -5, true);
        doors[1] = Box("Door_S", new Vector2(0, -(hh - t / 2f)), new Vector2(gap, t), connected[1] ? doorColor : wallColor, -5, true);
        doors[2] = Box("Door_E", new Vector2(hw - t / 2f, 0), new Vector2(t, gap), connected[2] ? doorColor : wallColor, -5, true);
        doors[3] = Box("Door_W", new Vector2(-(hw - t / 2f), 0), new Vector2(t, gap), connected[3] ? doorColor : wallColor, -5, true);

        for (int i = 0; i < 4; i++)
            doors[i].SetActive(!connected[i]);

        // trigger vnútri miestnosti (menší ako miestnosť, aby sa spustil až keď je hráč poriadne vnútri)
        BoxCollider2D trigger = gameObject.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = size - Vector2.one * (2f * t + 2f);
    }

    GameObject Box(string name, Vector2 localPos, Vector2 boxSize, Color color, int order, bool solid)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = new Vector3(boxSize.x, boxSize.y, 1f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = White;
        sr.color = color;
        sr.sortingOrder = order;

        if (solid)
        {
            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.size = Vector2.one;
        }
        return go;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        ApplyCameraBounds();

        if (entered) return;
        entered = true;

        if (type == RoomType.Start)
        {
            IsCleared = true;
            return;
        }

        SpawnEnemies(other.transform);

        if (alive.Count > 0) Lock(true);
        else IsCleared = true;
    }

    void Update()
    {
        if (!locked) return;

        alive.RemoveAll(e => e == null);
        if (alive.Count == 0)
        {
            IsCleared = true;
            Lock(false);
        }
    }

    public void ApplyCameraBounds()
    {
        if (cameraFollow == null) return;
        Vector2 c = transform.position;
        cameraFollow.SetBounds(c - size / 2f, c + size / 2f);
    }

    void Lock(bool state)
    {
        locked = state;
        for (int i = 0; i < 4; i++)
            if (connected[i]) doors[i].SetActive(state);
    }

    void SpawnEnemies(Transform player)
    {
        if (type == RoomType.Boss && bossPrefab != null)
        {
            Spawn(bossPrefab, Vector2.zero);
            return;
        }

        if (enemyPrefabs == null || enemyPrefabs.Length == 0) return;

        int count = Random.Range(enemyRange.x, enemyRange.y + 1);
        if (type == RoomType.Boss) count *= 2;

        float mx = size.x / 2f - WallThickness - 2f;
        float my = size.y / 2f - WallThickness - 2f;

        for (int i = 0; i < count; i++)
        {
            Vector2 pos = Vector2.zero;
            for (int tries = 0; tries < 10; tries++)
            {
                pos = new Vector2(Random.Range(-mx, mx), Random.Range(-my, my));
                float dist = Vector2.Distance((Vector2)transform.position + pos, player.position);
                if (dist > 4f) break;   // nespawnuj nepriateľa hráčovi na hlavu
            }
            Spawn(enemyPrefabs[Random.Range(0, enemyPrefabs.Length)], pos);
        }
    }

    void Spawn(GameObject prefab, Vector2 localPos)
    {
        GameObject e = Instantiate(prefab, transform);
        e.transform.localPosition = localPos;
        alive.Add(e);
    }
}
