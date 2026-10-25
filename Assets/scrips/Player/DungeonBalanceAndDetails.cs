using System.Collections.Generic;
using UnityEngine;

// Unity 6.3 - pridaj na ten isty GameObject ako DungeonGenerator.
// NEMENI Room.cs, DungeonRoomExtras.cs ani prefab nepriatelov.
[RequireComponent(typeof(DungeonGenerator))]
public class DungeonBalanceAndDetails : MonoBehaviour
{
    [Header("Balance V1")]
    [Tooltip("Pri Awake nastavi jemnejsiu obtiaznost na existujucom DungeonGenerator V3.")]
    public bool applyRecommendedBalance = true;

    [Header("Visual details - bez kolizii")]
    public bool decorateRooms = true;
    [Range(0, 20)] public int maxFloorDetailsPerRoom = 12;
    public bool placeWallLamps = true;
    public bool markSpecialRooms = true;

    private readonly HashSet<Room> decorated = new HashSet<Room>();
    private static Sprite crackSprite;
    private static Sprite gravelSprite;
    private static Sprite scrapSprite;
    private static Sprite runeSprite;
    private static Sprite lampSprite;
    private float nextScan;

    private void Awake()
    {
        if (!applyRecommendedBalance) return;

        DungeonGenerator gen = GetComponent<DungeonGenerator>();
        if (gen == null) return;

        // Zaciatok zostava napinavy, ale hrac ma viac miesta na dodge.
        gen.enemiesPerRoom = new Vector2Int(2, 4);
        gen.chestChanceInBattle = 0.35f;
        gen.eliteRoomChance = 0.18f;
        gen.ambushRoomChance = 0.12f;
        gen.ruinsRoomChance = 0.18f;

        // Dungeon V3: postupne zvysovanie bez nahleho preplnenia miestnosti.
        gen.extraRoomsPerRun = 1;
        gen.maxTotalRooms = 16;
        gen.extraEnemiesPerRun = 1;
        gen.depthStepsForExtraEnemy = 6;
        gen.maxEnemiesPerRoom = 7;
        gen.eliteChanceIncreasePerRun = 0.025f;
    }

    private void Update()
    {
        if (!decorateRooms || Time.time < nextScan) return;
        nextScan = Time.time + 0.35f;

        // Funguje aj po regenerovani dungeonu pri dalsiej vyprave.
        foreach (Transform child in transform)
        {
            if (!child.gameObject.activeInHierarchy) continue;
            Room room = child.GetComponent<Room>();
            if (room == null || decorated.Contains(room)) continue;
            DungeonRoomExtras extras = child.GetComponent<DungeonRoomExtras>();
            if (extras == null) continue;

            Decorate(room, extras.kind);
            decorated.Add(room);
        }

        // Odstrani referencie na uz znicene miestnosti po Generate().
        decorated.RemoveWhere(room => room == null);
    }

    private void Decorate(Room room, DungeonRoomKind kind)
    {
        Transform floor = room.transform.Find("Floor");
        if (floor == null) return;

        SpriteRenderer floorRenderer = floor.GetComponent<SpriteRenderer>();
        Vector2 size = floorRenderer != null && floorRenderer.drawMode == SpriteDrawMode.Tiled
            ? floorRenderer.size
            : new Vector2(Mathf.Abs(floor.localScale.x), Mathf.Abs(floor.localScale.y));
        if (size.x < 5f || size.y < 5f) return;

        EnsureSprites();
        // Vlastny RNG neovplyvni nahodne spawnovanie nepriatelov ani loot.
        int seed = unchecked(room.gridPos.x * 92837111 + room.gridPos.y * 689287499 +
            (int)kind * 2833 + Mathf.RoundToInt(size.x * 17f + size.y * 31f));
        System.Random rng = new System.Random(seed);

        int amount = Mathf.Min(maxFloorDetailsPerRoom,
            Mathf.RoundToInt(size.x * size.y / 30f));
        float maxX = size.x * 0.5f - 1.5f;
        float maxY = size.y * 0.5f - 1.5f;

        for (int i = 0; i < amount; i++)
        {
            Vector2 pos = Vector2.zero;
            bool valid = false;
            for (int tries = 0; tries < 18; tries++)
            {
                pos = new Vector2(Range(rng, -maxX, maxX), Range(rng, -maxY, maxY));
                if (!SafeForDecoration(pos, size, kind)) continue;
                valid = true;
                break;
            }
            if (!valid) continue;

            int style = rng.Next(0, 3);
            Sprite sprite = style == 0 ? crackSprite : style == 1 ? gravelSprite : scrapSprite;
            Color tint = style == 0
                ? new Color(0.16f, 0.20f, 0.25f, 0.60f)
                : style == 1
                    ? new Color(0.55f, 0.59f, 0.63f, 0.45f)
                    : new Color(0.54f, 0.48f, 0.34f, 0.50f);

            // Ziadne collidery: hrac sa nemoze zaseknut o dekoracie.
            CreateDecoration(room.transform, "Detail_" + style + "_" + i,
                sprite, pos, Range(rng, 0.7f, 1.4f), tint, -9);
        }

        if (markSpecialRooms && kind != DungeonRoomKind.Battle && kind != DungeonRoomKind.Start)
        {
            Color color = GetRoomAccent(kind);
            // Znacky su viditelne, ale nema ich zmysel prechadzat ako prekazku.
            CreateDecoration(room.transform, "RoomRune", runeSprite,
                new Vector2(0f, -size.y * 0.26f), 1.65f, color, -8);
        }

        if (placeWallLamps && kind != DungeonRoomKind.Start && kind != DungeonRoomKind.Rest)
        {
            if (lampSprite == null)
                lampSprite = Resources.Load<Sprite>("DungeonTiles/prop_lamp");

            if (lampSprite != null)
            {
                float lampX = Mathf.Max(3.1f, size.x * 0.5f - 3.1f);
                float lampY = size.y * 0.5f - 1.25f;
                CreateArtProp(room.transform, "Decor_Lamp_L", lampSprite,
                    new Vector2(-lampX, lampY), new Vector2(0.7f, 0.95f));
                CreateArtProp(room.transform, "Decor_Lamp_R", lampSprite,
                    new Vector2(lampX, lampY), new Vector2(0.7f, 0.95f));
            }
        }
    }

    private static bool SafeForDecoration(Vector2 p, Vector2 size, DungeonRoomKind kind)
    {
        // Ponechat priestor v strede aj okolo Chest na (0, +1.8).
        float centerClear = kind == DungeonRoomKind.Boss ? 4.2f : 2.5f;
        if (p.sqrMagnitude < centerClear * centerClear) return false;
        if ((p - new Vector2(0f, 1.8f)).sqrMagnitude < 2.25f) return false;

        // Volne koridory od kazdych dveri do stredu (sirka ~3 jednotky).
        if (Mathf.Abs(p.x) < 1.75f || Mathf.Abs(p.y) < 1.75f) return false;

        // Obmedzit zahltenie oblasti, kde extras umiestnuju rohovy cover.
        float cornerX = size.x * 0.5f - 3f;
        float cornerY = size.y * 0.5f - 2.8f;
        if (Mathf.Abs(Mathf.Abs(p.x) - cornerX) < 1.25f &&
            Mathf.Abs(Mathf.Abs(p.y) - cornerY) < 1.25f) return false;
        return true;
    }

    private static Color GetRoomAccent(DungeonRoomKind kind)
    {
        switch (kind)
        {
            case DungeonRoomKind.Treasure: return new Color(1f, 0.80f, 0.25f, 0.65f);
            case DungeonRoomKind.Rest: return new Color(0.35f, 1f, 0.55f, 0.65f);
            case DungeonRoomKind.Supply: return new Color(0.35f, 0.8f, 1f, 0.65f);
            case DungeonRoomKind.Boss: return new Color(1f, 0.20f, 0.2f, 0.8f);
            case DungeonRoomKind.Elite: return new Color(1f, 0.45f, 0.2f, 0.65f);
            case DungeonRoomKind.Ambush: return new Color(1f, 0.25f, 0.4f, 0.65f);
            default: return new Color(0.72f, 0.5f, 1f, 0.6f);
        }
    }

    private static GameObject CreateDecoration(Transform parent, string label, Sprite sprite,
        Vector2 position, float scale, Color tint, int order)
    {
        GameObject go = new GameObject(label);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(position.x, position.y, 0f);
        go.transform.localScale = new Vector3(scale, scale, 1f);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = tint;
        sr.sortingOrder = order;
        return go;
    }

    private static void CreateArtProp(Transform parent, string name, Sprite sprite,
        Vector2 pos, Vector2 desiredSize)
    {
        GameObject go = CreateDecoration(parent, name, sprite, pos, 1f, Color.white, -4);
        Vector2 spriteSize = sprite.bounds.size;
        go.transform.localScale = new Vector3(
            desiredSize.x / Mathf.Max(0.0001f, spriteSize.x),
            desiredSize.y / Mathf.Max(0.0001f, spriteSize.y), 1f);
    }

    private static float Range(System.Random rng, float a, float b)
    {
        return a + (float)rng.NextDouble() * (b - a);
    }

    private static void EnsureSprites()
    {
        if (crackSprite == null)
        {
            crackSprite = MakePixelSprite("Crack", 0);
            gravelSprite = MakePixelSprite("Gravel", 1);
            scrapSprite = MakePixelSprite("Scrap", 2);
            runeSprite = MakePixelSprite("Rune", 3);
        }
    }

    private static Sprite MakePixelSprite(string label, int type)
    {
        const int n = 16;
        Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.name = "GeneratedDungeon_" + label;
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
        Color32[] pixels = new Color32[n * n];
        Color32 white = new Color32(255, 255, 255, 255);

        if (type == 0) // praskliny v kamennej podlahe
        {
            int[] ys = { 8, 8, 7, 7, 6, 7, 8, 8, 9, 9, 10 };
            for (int i = 0; i < ys.Length; i++) pixels[ys[i] * n + i + 2] = white;
            pixels[5 * n + 7] = white;
            pixels[4 * n + 7] = white;
        }
        else if (type == 1) // kamienky
        {
            FillPixels(pixels, 3, 5, 3, 2);
            FillPixels(pixels, 10, 9, 3, 2);
            FillPixels(pixels, 7, 3, 2, 2);
        }
        else if (type == 2) // drobny srot
        {
            FillPixels(pixels, 3, 10, 4, 1);
            FillPixels(pixels, 6, 7, 1, 3);
            FillPixels(pixels, 10, 4, 3, 2);
            FillPixels(pixels, 11, 7, 1, 2);
        }
        else // runova znacka
        {
            for (int x = 3; x <= 12; x++)
            {
                pixels[3 * n + x] = white;
                pixels[12 * n + x] = white;
            }
            for (int y = 3; y <= 12; y++)
            {
                pixels[y * n + 3] = white;
                pixels[y * n + 12] = white;
            }
            for (int i = 5; i <= 10; i++)
            {
                pixels[i * n + i] = white;
                pixels[(15 - i) * n + i] = white;
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply(false, false);
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 16f);
    }

    private static void FillPixels(Color32[] pixels, int x, int y, int w, int h)
    {
        Color32 white = new Color32(255, 255, 255, 255);
        for (int py = y; py < y + h; py++)
            for (int px = x; px < x + w; px++)
                pixels[py * 16 + px] = white;
    }
}
