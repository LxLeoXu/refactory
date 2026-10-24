using System.Collections.Generic;
using UnityEngine;

public class DungeonGenerator : MonoBehaviour
{
    [Header("Dungeon")]
    public bool generateOnStart = true;
    public int roomCount = 10;
    public Vector2 roomSize = new Vector2(20f, 12f);
    [Tooltip("0 = náhodný seed")]
    public int seed = 0;

    [Header("Referencie")]
    public Transform player;
    public CameraFollow cameraFollow;

    [Header("Nepriatelia")]
    public GameObject[] enemyPrefabs;
    public GameObject bossPrefab;
    public Vector2Int enemiesPerRoom = new Vector2Int(2, 5);

    [Header("Farby (kým nemáš grafiku)")]
    public Color floorColor = new Color(0.18f, 0.18f, 0.22f);
    public Color wallColor = new Color(0.45f, 0.45f, 0.50f);
    public Color doorColor = new Color(0.65f, 0.40f, 0.20f);

    // poradie: N, S, E, W (rovnaké ako v Room.cs)
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
        foreach (Transform child in transform)
            Destroy(child.gameObject);

        if (seed != 0) Random.InitState(seed);

        // 1) vygeneruj rozloženie na mriežke (strom bez slučiek)
        var depth = new Dictionary<Vector2Int, int>();
        var cells = new List<Vector2Int>();
        Vector2Int start = Vector2Int.zero;
        depth[start] = 0;
        cells.Add(start);

        int safety = 0;
        while (cells.Count < roomCount && safety++ < 2000)
        {
            Vector2Int from = cells[Random.Range(0, cells.Count)];
            Vector2Int next = from + Dirs[Random.Range(0, Dirs.Length)];

            if (depth.ContainsKey(next)) continue;
            if (CountNeighbours(next, depth) > 1) continue;   // dotýkal by sa viac ako jednej miestnosti

            depth[next] = depth[from] + 1;
            cells.Add(next);
        }

        // 2) boss = najvzdialenejšia miestnosť od štartu
        Vector2Int bossCell = start;
        foreach (Vector2Int c in cells)
            if (depth[c] > depth[bossCell]) bossCell = c;

        // 3) vytvor miestnosti
        Room startRoom = null;
        foreach (Vector2Int cell in cells)
        {
            RoomType type = cell == start ? RoomType.Start
                          : cell == bossCell ? RoomType.Boss
                          : RoomType.Normal;

            bool[] connected = new bool[4];
            for (int i = 0; i < 4; i++)
                connected[i] = depth.ContainsKey(cell + Dirs[i]);

            GameObject go = new GameObject($"Room {cell.x},{cell.y} ({type})");
            go.transform.SetParent(transform);
            go.transform.position = transform.position + new Vector3(cell.x * roomSize.x, cell.y * roomSize.y, 0f);

            Room room = go.AddComponent<Room>();
            room.Init(cell, type, roomSize, connected, enemyPrefabs, bossPrefab, enemiesPerRoom,
                cameraFollow, floorColor, wallColor, doorColor);

            if (type == RoomType.Start) startRoom = room;
        }

        // 4) hráč a kamera do štartovnej miestnosti
        if (player != null)
            player.position = startRoom.transform.position;

        startRoom.ApplyCameraBounds();
    }

    int CountNeighbours(Vector2Int cell, Dictionary<Vector2Int, int> occupied)
    {
        int n = 0;
        foreach (Vector2Int d in Dirs)
            if (occupied.ContainsKey(cell + d)) n++;
        return n;
    }
}
