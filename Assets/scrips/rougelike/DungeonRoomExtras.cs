using UnityEngine;

// Samostatne rozsiruje Room bez potreby menit tvoje Room.cs alebo Enemy.cs.
public enum DungeonRoomKind { Start, Battle, Treasure, Rest, Elite, Boss }

public class DungeonRoomExtras : MonoBehaviour
{
    public DungeonRoomKind kind = DungeonRoomKind.Battle;

    Room room;
    GameObject chestPrefab;
    bool chestSpawned;
    bool restUsed;
    float battleChestChance;
    bool battleChestRolled;

    public void Setup(Room targetRoom, DungeonRoomKind roomKind,
        GameObject chest, bool useTiles, float chestChance)
    {
        room = targetRoom;
        kind = roomKind;
        chestPrefab = chest;
        battleChestChance = chestChance;
        battleChestRolled = false;
        if (useTiles) ApplyRoomTiles();
    }

    // Volane na tom istom objekte ako trigger vytvoreny v Room.Build().
    void OnTriggerEnter2D(Collider2D other)
    {
        if (kind != DungeonRoomKind.Rest || restUsed || !other.CompareTag("Player"))
            return;

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

        if (kind == DungeonRoomKind.Treasure)
            SpawnChest(LootChestTier.Treasure);
        else if (kind == DungeonRoomKind.Rest)
            SpawnChest(LootChestTier.Rest);
        else if (kind == DungeonRoomKind.Elite || kind == DungeonRoomKind.Boss)
            SpawnChest(LootChestTier.Elite);
        else if (kind == DungeonRoomKind.Battle && !battleChestRolled)
        {
            battleChestRolled = true;
            if (Random.value <= battleChestChance)
                SpawnChest(LootChestTier.Normal);
        }
    }

    void SpawnChest(LootChestTier tier)
    {
        chestSpawned = true;
        // Pokial neexistuje prefab, vytvorime chest s obrazkom z Resources.
        GameObject chest;
        if (chestPrefab != null)
            chest = Instantiate(chestPrefab, transform);
        else
        {
            chest = new GameObject("LootChest");
            chest.transform.SetParent(transform, false);
            chest.AddComponent<SpriteRenderer>();
            BoxCollider2D collider = chest.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.8f, 0.6f);
        }

        chest.name = "Chest_" + tier;
        // Mimo stredu, kde moze stat hrac po vstupe do miestnosti.
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
        else if (kind == DungeonRoomKind.Elite || kind == DungeonRoomKind.Boss)
            floorFile = "floor_danger";

        foreach (Transform child in transform)
        {
            SpriteRenderer sr = child.GetComponent<SpriteRenderer>();
            if (sr == null) continue;

            string file = child.name == "Floor" ? floorFile
                : child.name.StartsWith("Door_") ? (child.gameObject.activeSelf ? "wall_stone" : "door_wood") : "wall_stone";
            Sprite sprite = Resources.Load<Sprite>("DungeonTiles/" + file);
            if (sprite == null) continue;

            // Povodny Room.Box pouziva scale. Pre pixel tiles prejdeme
            // na SpriteDrawMode.Tiled a collider ponechame v rovnakej velkosti.
            Vector3 oldScale = child.localScale;
            BoxCollider2D box = child.GetComponent<BoxCollider2D>();
            if (box != null)
            {
                box.size = new Vector2(oldScale.x * box.size.x, oldScale.y * box.size.y);
                box.offset = new Vector2(oldScale.x * box.offset.x, oldScale.y * box.offset.y);
            }
            child.localScale = Vector3.one;
            sr.sprite = sprite;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = new Vector2(oldScale.x, oldScale.y);
            sr.color = Color.white;
        }
    }
}
