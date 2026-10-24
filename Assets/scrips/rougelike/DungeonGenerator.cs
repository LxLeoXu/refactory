using System.Collections.Generic;
using UnityEngine;

// Vychadza z DungeonGenerator z GameJam_BaseRun_System.
// Zachovava verejne polia a Generate(), ktore pouziva RunManager.
public class DungeonGenerator : MonoBehaviour
{
    [Header("Dungeon")]
    public bool generateOnStart = true;
    [Min(2)] public int roomCount = 10;
    public Vector2 roomSize = new Vector2(20f, 12f);
    [Tooltip("0 = nahodny seed")]
    public int seed = 0;

    [Header("Referencie")]
    public Transform player;
    public CameraFollow cameraFollow;

    [Header("Nepriatelia")]
    public GameObject[] enemyPrefabs;
    public GameObject bossPrefab;
    public Vector2Int enemiesPerRoom = new Vector2Int(2, 5);

    [Header("Truhlice a miestnosti")]
    [Tooltip("Nepovinne: prefab s LootChest. Ak nie je priradeny, truhlica sa vytvori automaticky.")]
    public GameObject chestPrefab;
    [Range(0f, 1f)] public float chestChanceInBattle = 0.25f;
    [Range(0f, 1f)] public float eliteRoomChance = 0.25f;
    public bool useDungeonTileArt = true;

    [Header("Farby (fallback bez sprite assetov)")]
    public Color floorColor = new Color(0.18f, 0.18f, 0.22f);
    public Color wallColor = new Color(0.45f, 0.45f, 0.50f);
    public Color doorColor = new Color(0.65f, 0.40f, 0.20f);

    // N,S,E,W: rovnake poradie ako v Room.cs.
    static readonly Vector2Int[] Dirs =
    {
        Vector2Int.up, Vector2Int.down, Vector2Int.right, Vector2Int.left
    };

    void Start()
    {
        if (generateOnStart) Generate();
    }

    [ContextMenu("Regenerate")]
    public void Generate()
    {
        // Pre existujuci RunManager - starsie miestnosti zaniknu na konci frame.
        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }

        if (seed != 0) Random.InitState(seed);

        var depth = new Dictionary<Vector2Int, int>();
        var cells = new List<Vector2Int>();
        Vector2Int start = Vector2Int.zero;
        depth[start] = 0;
        cells.Add(start);

        int targetCount = Mathf.Max(2, roomCount);
        int safety = 0;
        while (cells.Count < targetCount && safety++ < 2000)
        {
            Vector2Int from = cells[Random.Range(0, cells.Count)];
            Vector2Int next = from + Dirs[Random.Range(0, Dirs.Length)];
            if (depth.ContainsKey(next)) continue;
            if (CountNeighbours(next, depth) > 1) continue;
            depth[next] = depth[from] + 1;
            cells.Add(next);
        }

        Vector2Int bossCell = start;
        foreach (Vector2Int cell in cells)
            if (depth[cell] > depth[bossCell]) bossCell = cell;

        // Garantujeme Treasure a Rest, ked je miesto (najlepsie roomCount >= 6).
        var specialCandidates = new List<Vector2Int>();
        foreach (Vector2Int cell in cells)
            if (cell != start && cell != bossCell) specialCandidates.Add(cell);
        for (int i = specialCandidates.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            Vector2Int temp = specialCandidates[i];
            specialCandidates[i] = specialCandidates[j];
            specialCandidates[j] = temp;
        }

        var special = new Dictionary<Vector2Int, DungeonRoomKind>();
        if (specialCandidates.Count > 0)
            special[specialCandidates[0]] = DungeonRoomKind.Treasure;
        if (specialCandidates.Count > 1)
            special[specialCandidates[1]] = DungeonRoomKind.Rest;
        for (int i = 2; i < specialCandidates.Count; i++)
            if (Random.value < eliteRoomChance)
                special[specialCandidates[i]] = DungeonRoomKind.Elite;

        if (useDungeonTileArt) CreateBackground(cells);

        Room startRoom = null;
        foreach (Vector2Int cell in cells)
        {
            DungeonRoomKind kind = cell == start ? DungeonRoomKind.Start
                : cell == bossCell ? DungeonRoomKind.Boss
                : special.TryGetValue(cell, out DungeonRoomKind value) ? value
                : DungeonRoomKind.Battle;

            RoomType roomType = kind == DungeonRoomKind.Start ? RoomType.Start
                : kind == DungeonRoomKind.Boss ? RoomType.Boss : RoomType.Normal;

            bool[] connected = new bool[4];
            for (int i = 0; i < 4; i++)
                connected[i] = depth.ContainsKey(cell + Dirs[i]);

            GameObject go = new GameObject($"Room {cell.x},{cell.y} ({kind})");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(cell.x * roomSize.x, cell.y * roomSize.y, 0f);

            // Treasure/Rest bez nepriatelov; Elite ma viac nepriatelov.
            bool peaceful = kind == DungeonRoomKind.Treasure || kind == DungeonRoomKind.Rest;
            GameObject[] enemies = peaceful ? new GameObject[0] : enemyPrefabs;
            Vector2Int count = peaceful ? Vector2Int.zero
                : kind == DungeonRoomKind.Elite
                    ? enemiesPerRoom + new Vector2Int(2, 2) : enemiesPerRoom;

            Room room = go.AddComponent<Room>();
            room.Init(cell, roomType, roomSize, connected, enemies, bossPrefab, count,
                cameraFollow, floorColor, wallColor, doorColor);

            DungeonRoomExtras extras = go.AddComponent<DungeonRoomExtras>();
            extras.Setup(room, kind, chestPrefab, useDungeonTileArt, chestChanceInBattle);

            if (kind == DungeonRoomKind.Start) startRoom = room;
        }

        if (startRoom != null)
        {
            if (player != null) player.position = startRoom.transform.position;
            startRoom.ApplyCameraBounds();
        }
    }


    void CreateBackground(List<Vector2Int> cells)
    {
        Sprite sprite = Resources.Load<Sprite>("DungeonTiles/background_void");
        if (sprite == null || cells.Count == 0) return;
        int minX = cells[0].x, maxX = cells[0].x;
        int minY = cells[0].y, maxY = cells[0].y;
        foreach (Vector2Int cell in cells)
        {
            minX = Mathf.Min(minX, cell.x);
            maxX = Mathf.Max(maxX, cell.x);
            minY = Mathf.Min(minY, cell.y);
            maxY = Mathf.Max(maxY, cell.y);
        }

        GameObject bg = new GameObject("DungeonBackground");
        bg.transform.SetParent(transform, false);
        bg.transform.localPosition = new Vector3(
            (minX + maxX) * roomSize.x * 0.5f,
            (minY + maxY) * roomSize.y * 0.5f, 0f);
        SpriteRenderer renderer = bg.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.drawMode = SpriteDrawMode.Tiled;
        renderer.size = new Vector2(
            (maxX - minX + 3) * roomSize.x,
            (maxY - minY + 3) * roomSize.y);
        renderer.sortingOrder = -50;
    }

    int CountNeighbours(Vector2Int cell, Dictionary<Vector2Int, int> occupied)
    {
        int count = 0;
        foreach (Vector2Int direction in Dirs)
            if (occupied.ContainsKey(cell + direction)) count++;
        return count;
    }
}
