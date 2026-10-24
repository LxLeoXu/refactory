using System.Collections.Generic;
using UnityEngine;

// GAMEJAM DUNGEON V2 - kompatibilny s Room, DungeonRoomExtras, RunManager.
// Ziadne zmeny v Room.cs, Enemy.cs, LootChest.cs ani RunManager.cs.
public class DungeonGenerator : MonoBehaviour
{
    [Header("Dungeon")]
    public bool generateOnStart = true;
    [Min(2)] public int roomCount = 10;
    public Vector2 roomSize = new Vector2(20f, 12f); // stredna velkost / povodne nastavenie
    [Tooltip("0 = iny nahodny dungeon pri kazdom behu")]
    public int seed = 0;

    [Header("Rozne velkosti miestnosti")]
    public bool useVariableRoomSizes = true;
    public Vector2 smallRoomSize = new Vector2(16f, 10f);
    public Vector2 largeRoomSize = new Vector2(28f, 18f);

    [Header("Referencie")]
    public Transform player;
    public CameraFollow cameraFollow;

    [Header("Nepriatelia")]
    public GameObject[] enemyPrefabs;
    public GameObject bossPrefab;
    public Vector2Int enemiesPerRoom = new Vector2Int(2, 5);

    [Header("Truhlice a typy miestnosti")]
    public GameObject chestPrefab;
    [Range(0f, 1f)] public float chestChanceInBattle = 0.25f;
    [Range(0f, 1f)] public float eliteRoomChance = 0.25f;
    [Range(0f, 1f)] public float ruinsRoomChance = 0.2f;
    [Range(0f, 1f)] public float ambushRoomChance = 0.2f;
    public bool useDungeonTileArt = true;
    public bool generateObstacles = true;
    public bool generateTraps = true;

    [Header("Farby (ak nie su textury)")]
    public Color floorColor = new Color(0.18f, 0.18f, 0.22f);
    public Color wallColor = new Color(0.45f, 0.45f, 0.50f);
    public Color doorColor = new Color(0.65f, 0.40f, 0.20f);

    private static readonly Vector2Int[] Dirs = {
        Vector2Int.up, Vector2Int.down, Vector2Int.right, Vector2Int.left
    };

    void Start()
    {
        if (generateOnStart) Generate();
    }

    [ContextMenu("Regenerate")]
    public void Generate()
    {
        // Pri opakovanych vypravach odstranime stary dungeon bez docasnych dvojitych colliderov.
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            GameObject oldRoom = transform.GetChild(i).gameObject;
            oldRoom.SetActive(false);
            if (Application.isPlaying) Destroy(oldRoom);
            else DestroyImmediate(oldRoom);
        }

        if (seed != 0) Random.InitState(seed);

        // Stromove rozlozenie miestnosti: nikdy nespajame 2+ susedov (bez nechcenych dveri).
        Dictionary<Vector2Int, int> depth = new Dictionary<Vector2Int, int>();
        List<Vector2Int> cells = new List<Vector2Int>();
        Vector2Int origin = Vector2Int.zero;
        depth[origin] = 0;
        cells.Add(origin);

        int attempts = 0;
        while (cells.Count < Mathf.Max(2, roomCount) && attempts++ < 3000)
        {
            Vector2Int from = cells[Random.Range(0, cells.Count)];
            Vector2Int next = from + Dirs[Random.Range(0, Dirs.Length)];
            if (depth.ContainsKey(next) || CountNeighbours(next, depth) > 1) continue;
            depth[next] = depth[from] + 1;
            cells.Add(next);
        }

        Vector2Int bossCell = origin;
        foreach (Vector2Int c in cells)
            if (depth[c] > depth[bossCell]) bossCell = c;

        // Dalsie typy: pri >= 10 miestnostiach su zarucene Treasure, Rest,
        // Supply, Ambush, Ruins aj aspon jedna obycajna Battle room.
        List<Vector2Int> available = new List<Vector2Int>();
        foreach (Vector2Int c in cells)
            if (c != origin && c != bossCell) available.Add(c);

        for (int i = available.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            Vector2Int tmp = available[i]; available[i] = available[j]; available[j] = tmp;
        }

        Dictionary<Vector2Int, DungeonRoomKind> special = new Dictionary<Vector2Int, DungeonRoomKind>();
        DungeonRoomKind[] guaranteed = {
            DungeonRoomKind.Treasure, DungeonRoomKind.Rest, DungeonRoomKind.Supply,
            DungeonRoomKind.Ambush, DungeonRoomKind.Ruins, DungeonRoomKind.Battle
        };
        int countGuaranteed = Mathf.Min(available.Count, guaranteed.Length);
        for (int i = 0; i < countGuaranteed; i++) special[available[i]] = guaranteed[i];
        for (int i = countGuaranteed; i < available.Count; i++)
        {
            float roll = Random.value;
            DungeonRoomKind kind = roll < eliteRoomChance ? DungeonRoomKind.Elite
                : roll < eliteRoomChance + ruinsRoomChance ? DungeonRoomKind.Ruins
                : roll < eliteRoomChance + ruinsRoomChance + ambushRoomChance ? DungeonRoomKind.Ambush
                : DungeonRoomKind.Battle;
            special[available[i]] = kind;
        }

        // TRIK: vsetky miestnosti v tom istom STLpCI maju rovnaku sirku
        // a v tom istom RIADKU rovnaku vysku. Dvere preto presne licuju,
        // hoci jednotlive miestnosti maju rozne rozmery. Ziadne medzery!
        Dictionary<int, float> widths = new Dictionary<int, float>();
        Dictionary<int, float> heights = new Dictionary<int, float>();
        foreach (Vector2Int c in cells)
        {
            if (!widths.ContainsKey(c.x))
                widths.Add(c.x, PickDimension(smallRoomSize.x, roomSize.x, largeRoomSize.x));
            if (!heights.ContainsKey(c.y))
                heights.Add(c.y, PickDimension(smallRoomSize.y, roomSize.y, largeRoomSize.y));
        }

        Dictionary<Vector2Int, Vector2> positions = new Dictionary<Vector2Int, Vector2>();
        Dictionary<Vector2Int, Vector2> sizes = new Dictionary<Vector2Int, Vector2>();
        foreach (Vector2Int c in cells)
        {
            Vector2 actualSize = new Vector2(widths[c.x], heights[c.y]);
            sizes[c] = actualSize;
            positions[c] = new Vector2(AxisCenter(c.x, widths), AxisCenter(c.y, heights));
        }

        if (useDungeonTileArt) CreateBackground(cells, positions, sizes);

        Room startRoom = null;
        foreach (Vector2Int c in cells)
        {
            DungeonRoomKind kind = c == origin ? DungeonRoomKind.Start
                : c == bossCell ? DungeonRoomKind.Boss : special[c];
            RoomType roomType = kind == DungeonRoomKind.Start ? RoomType.Start
                : kind == DungeonRoomKind.Boss ? RoomType.Boss : RoomType.Normal;

            bool[] connected = new bool[4];
            for (int i = 0; i < 4; i++) connected[i] = depth.ContainsKey(c + Dirs[i]);

            GameObject go = new GameObject("Room " + c.x + "," + c.y + " (" + kind + ")");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = positions[c];

            bool peaceful = kind == DungeonRoomKind.Treasure || kind == DungeonRoomKind.Rest
                || kind == DungeonRoomKind.Supply;
            GameObject[] enemies = peaceful ? new GameObject[0] : enemyPrefabs;
            Vector2Int amount = peaceful ? Vector2Int.zero : enemiesPerRoom;
            if (kind == DungeonRoomKind.Elite) amount += new Vector2Int(2, 2);
            if (kind == DungeonRoomKind.Ambush) amount += new Vector2Int(3, 3);
            if (kind == DungeonRoomKind.Ruins) amount += new Vector2Int(1, 1);

            Room room = go.AddComponent<Room>();
            room.Init(c, roomType, sizes[c], connected, enemies, bossPrefab, amount,
                cameraFollow, floorColor, wallColor, doorColor);

            DungeonRoomExtras extras = go.AddComponent<DungeonRoomExtras>();
            extras.Setup(room, kind, chestPrefab, useDungeonTileArt, chestChanceInBattle,
                sizes[c], generateObstacles, generateTraps);
            if (kind == DungeonRoomKind.Start) startRoom = room;
        }

        if (startRoom != null)
        {
            if (player != null) player.position = startRoom.transform.position;
            startRoom.ApplyCameraBounds();
        }
        Debug.Log("Dungeon V2: " + cells.Count + " miestnosti, rozne velkosti, boss " + bossCell);
    }

    float PickDimension(float small, float medium, float large)
    {
        if (!useVariableRoomSizes) return Mathf.Max(10f, medium);
        int roll = Random.Range(0, 3);
        return Mathf.Max(10f, roll == 0 ? small : roll == 1 ? medium : large);
    }

    static float AxisCenter(int index, Dictionary<int, float> dimension)
    {
        if (index == 0) return 0f;
        float center = 0f;
        if (index > 0)
        {
            for (int i = 1; i <= index; i++)
                center += (dimension[i - 1] + dimension[i]) * 0.5f;
        }
        else
        {
            for (int i = -1; i >= index; i--)
                center -= (dimension[i + 1] + dimension[i]) * 0.5f;
        }
        return center;
    }

    void CreateBackground(List<Vector2Int> cells,
        Dictionary<Vector2Int, Vector2> positions, Dictionary<Vector2Int, Vector2> sizes)
    {
        Sprite sprite = Resources.Load<Sprite>("DungeonTiles/background_void");
        if (sprite == null || cells.Count == 0) return;

        Vector2 low = positions[cells[0]] - sizes[cells[0]] * 0.5f;
        Vector2 high = positions[cells[0]] + sizes[cells[0]] * 0.5f;
        foreach (Vector2Int c in cells)
        {
            low = Vector2.Min(low, positions[c] - sizes[c] * 0.5f);
            high = Vector2.Max(high, positions[c] + sizes[c] * 0.5f);
        }
        GameObject bg = new GameObject("DungeonBackground");
        bg.transform.SetParent(transform, false);
        bg.transform.localPosition = (low + high) * 0.5f;
        SpriteRenderer sr = bg.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = (high - low) + Vector2.one * 20f;
        sr.sortingOrder = -50;
    }

    int CountNeighbours(Vector2Int cell, Dictionary<Vector2Int, int> occupied)
    {
        int n = 0;
        foreach (Vector2Int direction in Dirs)
            if (occupied.ContainsKey(cell + direction)) n++;
        return n;
    }
}
