using UnityEngine;

// Rozsirenie existujuceho Room bez zasahu do Room.cs alebo Enemy.cs.
public enum DungeonRoomKind
{
    Start, Battle, Treasure, Rest, Elite, Boss,
    Supply, Ambush, Ruins
}

public class DungeonRoomExtras : MonoBehaviour
{
    public DungeonRoomKind kind = DungeonRoomKind.Battle;

    Room room;
    GameObject chestPrefab;
    float battleChestChance;
    bool battleChestRolled;
    bool chestSpawned;
    bool restUsed;

    // Povodna signatura ostava, aby sa nepokazili ine existujuce skripty.
    public void Setup(Room targetRoom, DungeonRoomKind roomKind,
        GameObject chest, bool useTiles, float chestChance)
    {
        Setup(targetRoom, roomKind, chest, useTiles, chestChance,
            new Vector2(20f, 12f), true, true);
    }

    public void Setup(Room targetRoom, DungeonRoomKind roomKind,
        GameObject chest, bool useTiles, float chestChance,
        Vector2 actualSize, bool obstacles, bool traps)
    {
        room = targetRoom;
        kind = roomKind;
        chestPrefab = chest;
        battleChestChance = Mathf.Clamp01(chestChance);
        battleChestRolled = false;
        chestSpawned = false;
        restUsed = false;
        if (useTiles) ApplyRoomTiles();
        if (obstacles) PlaceProps(actualSize, traps);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (kind != DungeonRoomKind.Rest || restUsed || !other.CompareTag("Player")) return;
        PlayerStats stats = other.GetComponentInParent<PlayerStats>();
        if (stats == null || stats.IsDead) return;
        restUsed = true;
        stats.Heal(25f);
        stats.Eat(30f);
        Debug.Log("REST ROOM: +25 HP, +30 hlad");
    }

    void Update()
    {
        if (room == null || !room.IsCleared || chestSpawned) return;
        switch (kind)
        {
            case DungeonRoomKind.Treasure: SpawnChest(LootChestTier.Treasure); break;
            case DungeonRoomKind.Rest: SpawnChest(LootChestTier.Rest); break;
            case DungeonRoomKind.Supply: SpawnChest(LootChestTier.Normal); break;
            case DungeonRoomKind.Elite:
            case DungeonRoomKind.Boss: SpawnChest(LootChestTier.Elite); break;
            case DungeonRoomKind.Ambush: SpawnChest(LootChestTier.Normal); break;
            case DungeonRoomKind.Ruins:
            case DungeonRoomKind.Battle:
                if (!battleChestRolled)
                {
                    battleChestRolled = true;
                    if (Random.value < (kind == DungeonRoomKind.Ruins ? 0.4f : battleChestChance))
                        SpawnChest(LootChestTier.Normal);
                }
                break;
        }
    }

    void SpawnChest(LootChestTier tier)
    {
        chestSpawned = true;
        GameObject chest;
        if (chestPrefab != null) chest = Instantiate(chestPrefab, transform);
        else
        {
            chest = new GameObject("LootChest");
            chest.transform.SetParent(transform, false);
            chest.AddComponent<SpriteRenderer>();
            BoxCollider2D col = chest.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.8f, 0.6f);
        }
        chest.name = "Chest_" + tier;
        chest.transform.localPosition = new Vector3(0f, 1.8f, 0f);
        LootChest loot = chest.GetComponent<LootChest>();
        if (loot == null) loot = chest.AddComponent<LootChest>();
        loot.SetTier(tier);
    }

    void ApplyRoomTiles()
    {
        string floorFile = "floor_stone";
        if (kind == DungeonRoomKind.Treasure) floorFile = "floor_treasure";
        else if (kind == DungeonRoomKind.Rest) floorFile = "floor_moss";
        else if (kind == DungeonRoomKind.Elite || kind == DungeonRoomKind.Boss) floorFile = "floor_danger";
        else if (kind == DungeonRoomKind.Ruins) floorFile = "floor_ruins";
        else if (kind == DungeonRoomKind.Supply) floorFile = "floor_supply";
        else if (kind == DungeonRoomKind.Ambush) floorFile = "floor_ambush";

        foreach (Transform child in transform)
        {
            SpriteRenderer sr = child.GetComponent<SpriteRenderer>();
            if (sr == null) continue;
            string file = child.name == "Floor" ? floorFile
                : child.name.StartsWith("Door_")
                    ? (child.gameObject.activeSelf ? "wall_stone" : "door_wood")
                    : "wall_stone";
            Sprite sprite = Resources.Load<Sprite>("DungeonTiles/" + file);
            if (sprite == null) continue;

            // Room.Box pouziva scale, sprite tiles drawMode vyzaduje scale=1.
            // Fyzicke rozmery collideru preto prepocitame len raz v Setup().
            Vector3 oldScale = child.localScale;
            BoxCollider2D col = child.GetComponent<BoxCollider2D>();
            if (col != null)
            {
                col.size = Vector2.Scale(col.size, new Vector2(oldScale.x, oldScale.y));
                col.offset = Vector2.Scale(col.offset, new Vector2(oldScale.x, oldScale.y));
            }
            child.localScale = Vector3.one;
            sr.sprite = sprite;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = new Vector2(oldScale.x, oldScale.y);
            sr.color = Color.white;
        }
    }

    void PlaceProps(Vector2 size, bool traps)
    {
        float x = Mathf.Max(3.2f, size.x * 0.5f - 3f);
        float y = Mathf.Max(2.4f, size.y * 0.5f - 2.8f);
        // Vsetko iba v rohoch, aby stred, spawn bossa a priechody dverami zostali volne.
        if (kind == DungeonRoomKind.Start)
        {
            Prop("LampLeft", "prop_lamp", new Vector2(-x, y), new Vector2(0.8f, 1.2f), false, new Color(1f, 0.7f, 0.25f));
            Prop("LampRight", "prop_lamp", new Vector2(x, y), new Vector2(0.8f, 1.2f), false, new Color(1f, 0.7f, 0.25f));
            return;
        }
        if (kind == DungeonRoomKind.Treasure || kind == DungeonRoomKind.Supply)
        {
            Prop("CrateLeft", "prop_crate", new Vector2(-x, -y), Vector2.one, true, new Color(0.45f, 0.3f, 0.2f));
            Prop("CrateRight", "prop_crate", new Vector2(x, -y), Vector2.one, true, new Color(0.45f, 0.3f, 0.2f));
        }
        else if (kind == DungeonRoomKind.Rest)
        {
            Prop("LampLeft", "prop_lamp", new Vector2(-x, y), Vector2.one, false, Color.yellow);
            Prop("LampRight", "prop_lamp", new Vector2(x, y), Vector2.one, false, Color.yellow);
        }
        else
        {
            bool ruins = kind == DungeonRoomKind.Ruins || kind == DungeonRoomKind.Boss;
            Prop("Cover_NE", ruins ? "prop_pillar" : "prop_crate", new Vector2(x, y),
                ruins ? new Vector2(1.1f, 1.1f) : Vector2.one, true, Color.gray);
            Prop("Cover_SW", ruins ? "prop_pillar" : "prop_crate", new Vector2(-x, -y),
                ruins ? new Vector2(1.1f, 1.1f) : Vector2.one, true, Color.gray);
            if (kind == DungeonRoomKind.Ruins || kind == DungeonRoomKind.Elite || kind == DungeonRoomKind.Boss)
            {
                Prop("Cover_NW", "prop_pillar", new Vector2(-x, y), new Vector2(1.1f, 1.1f), true, Color.gray);
            }
            if (traps && (kind == DungeonRoomKind.Ruins || kind == DungeonRoomKind.Ambush
                || kind == DungeonRoomKind.Elite))
            {
                GameObject trap = Prop("Trap_SE", "trap_spikes", new Vector2(x, -y),
                    new Vector2(1.3f, 1.3f), false, new Color(0.75f, 0.18f, 0.15f));
                BoxCollider2D col = trap.AddComponent<BoxCollider2D>();
                col.isTrigger = true;
                col.size = trap.GetComponent<SpriteRenderer>().sprite.bounds.size;
                trap.AddComponent<DungeonSpikeTrap>();
            }
        }
    }

    GameObject Prop(string label, string spriteId, Vector2 pos,
        Vector2 displaySize, bool solid, Color fallbackColor)
    {
        GameObject go = new GameObject(label);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = pos;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        Sprite sprite = Resources.Load<Sprite>("DungeonTiles/" + spriteId);
        if (sprite == null)
        {
            Texture2D white = Texture2D.whiteTexture;
            sprite = Sprite.Create(white, new Rect(0, 0, white.width, white.height),
                new Vector2(0.5f, 0.5f), white.width);
            sr.color = fallbackColor;
        }
        sr.sprite = sprite;
        sr.sortingOrder = 1;
        Vector2 actual = sprite.bounds.size;
        go.transform.localScale = new Vector3(
            displaySize.x / Mathf.Max(0.0001f, actual.x),
            displaySize.y / Mathf.Max(0.0001f, actual.y), 1f);
        if (solid)
        {
            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            // collider nema byt taky velky ako scaled PNG; sprite bounds je prirodzena velkost.
            col.size = actual;
        }
        return go;
    }
}
