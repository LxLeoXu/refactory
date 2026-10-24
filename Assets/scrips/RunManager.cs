using System.Collections.Generic;
using UnityEngine;

// Jedna scena: base -> nahodny dungeon -> base.
public class RunManager : MonoBehaviour
{
    [Header("Referencie")]
    public Transform player;
    public PlayerStats playerStats;
    public Inventory inventory;
    public DungeonGenerator dungeon;
    public CameraFollow cameraFollow;
    public CraftingUI craftingUI;

    [Header("Zakladna")]
    public Transform baseSpawn;
    public Vector2 baseRoomSize = new Vector2(20f, 12f);

    [Header("Nastavenia")]
    [Range(0f, 1f)] public float keepLootOnDeath = 0.5f;
    public float deathReturnDelay = 1.5f;

    bool inDungeon;
    float returnTime = -1f;
    Room bossRoom;
    Dictionary<string, int> startItems = new Dictionary<string, int>();

    void Awake()
    {
        if (dungeon != null) dungeon.generateOnStart = false;
    }

    void Start()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
        if (player != null)
        {
            if (playerStats == null) playerStats = player.GetComponent<PlayerStats>();
            if (inventory == null) inventory = player.GetComponent<Inventory>();
        }
        if (dungeon == null) dungeon = FindFirstObjectByType<DungeonGenerator>();
        if (cameraFollow == null) cameraFollow = FindFirstObjectByType<CameraFollow>();
        if (craftingUI == null) craftingUI = FindFirstObjectByType<CraftingUI>();

        if (player == null || playerStats == null || inventory == null || dungeon == null || baseSpawn == null)
        {
            Debug.LogError("RunManager: Chybaju referencie Player/PlayerStats/Inventory/Dungeon/BaseSpawn.");
            enabled = false;
            return;
        }
        dungeon.generateOnStart = false;
        playerStats.restartOnDeath = false;
        playerStats.OnDied += OnPlayerDied;
        EnterBase(false);
    }

    void OnDestroy()
    {
        if (playerStats != null) playerStats.OnDied -= OnPlayerDied;
    }

    void Update()
    {
        if (!inDungeon) return;
        if (returnTime >= 0f && Time.time >= returnTime)
        {
            returnTime = -1f;
            EnterBase(true);
        }
    }

    void SnapshotInventory()
    {
        startItems.Clear();
        foreach (Inventory.InventoryItem item in inventory.items)
        {
            if (item != null && !string.IsNullOrEmpty(item.itemId))
                startItems[item.itemId] = inventory.GetItemCount(item.itemId);
        }
    }

    public void StartRun()
    {
        if (inDungeon || playerStats.IsDead) return;
        SnapshotInventory();
        inDungeon = true;
        bossRoom = null;
        if (craftingUI != null)
        {
            craftingUI.showCrafting = false;
            craftingUI.enabled = false;
        }

        dungeon.Generate(); // Sam presunie hraca na start dungeonu.
        foreach (Room room in dungeon.GetComponentsInChildren<Room>())
            if (room.type == RoomType.Boss) bossRoom = room;
        if (bossRoom == null)
            Debug.LogWarning("RunManager: Boss room chyba; nastav aspon 2 miestnosti.");
    }

    void OnPlayerDied()
    {
        if (!inDungeon) return;
        returnTime = Time.time + deathReturnDelay;
    }

    void KeepHalfOfRunLoot()
    {
        // Zasoby vlastnene pred vypravou sa nestratia.
        var current = new List<Inventory.InventoryItem>(inventory.items);
        foreach (Inventory.InventoryItem item in current)
        {
            if (item == null || string.IsNullOrEmpty(item.itemId)) continue;
            int before = startItems.TryGetValue(item.itemId, out int stored) ? stored : 0;
            int gained = Mathf.Max(0, inventory.GetItemCount(item.itemId) - before);
            int kept = Mathf.FloorToInt(gained * keepLootOnDeath);
            inventory.RemoveItem(item.itemId, gained - kept);
        }
    }

    public void FinishRun()
    {
        if (!inDungeon || playerStats.IsDead || bossRoom == null || !bossRoom.IsCleared) return;
        EnterBase(false);
    }

    void EnterBase(bool died)
    {
        if (died) KeepHalfOfRunLoot();
        inDungeon = false;
        returnTime = -1f;
        bossRoom = null;
        playerStats.RespawnInBase();
        player.position = baseSpawn.position;
        if (cameraFollow != null)
        {
            Vector2 center = baseSpawn.position;
            cameraFollow.SetBounds(center - baseRoomSize / 2f, center + baseRoomSize / 2f);
        }
        if (craftingUI != null) craftingUI.enabled = true;
        foreach (Transform child in dungeon.transform)
            Destroy(child.gameObject);
    }

    void OnGUI()
    {
        if (!enabled) return;
        const float w = 235f;
        float x = (Screen.width - w) * 0.5f;
        if (!inDungeon)
        {
            if (GUI.Button(new Rect(x, Screen.height - 75f, w, 45f), "VYPRAVA DO DUNGEONU"))
                StartRun();
        }
        else if (bossRoom != null && bossRoom.IsCleared && !playerStats.IsDead)
        {
            if (GUI.Button(new Rect(x, Screen.height - 75f, w, 45f), "NAVRAT DO BASE (100% LOOT)"))
                FinishRun();
        }
        else if (playerStats.IsDead)
        {
            GUI.Box(new Rect(x, Screen.height - 75f, w, 45f), "Zomrel si! Navrat do BASE...");
        }
    }
}
