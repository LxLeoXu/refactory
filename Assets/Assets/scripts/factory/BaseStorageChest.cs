using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

// Permanentny sklad v BASE. Nepouziva novy Inventory ani RunManager.
// F = otvorit/zatvorit, W/S = vyber, A/D = ulozit/vybrat 1, Shift+A/D = vsetko.
public class BaseStorageChest : MonoBehaviour
{
    [Serializable]
    public class StoredStack
    {
        public string itemId;
        public int amount;
    }

    [Header("Referencie (automaticky podla Player tagu)")]
    public Transform player;
    public Inventory inventory;

    [Header("Nastavenie skladu")]
    public float interactionRange = 2.5f;
    public List<StoredStack> storedItems = new List<StoredStack>();

    [Serializable] class SaveData { public List<StoredStack> items = new List<StoredStack>(); }

    string SavePath => Path.Combine(Application.persistentDataPath, "base_storage.json");

    void Save()
    {
        SaveData data = new SaveData { items = storedItems };
        File.WriteAllText(SavePath, JsonUtility.ToJson(data));
    }

    void Load()
    {
        if (!File.Exists(SavePath)) return;
        SaveData data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
        if (data != null && data.items != null) storedItems = data.items;
    }

    void OnDestroy() { Save(); }
    void OnApplicationQuit() { Save(); }
    void OnApplicationPause(bool paused) { if (paused) Save(); }

    public bool IsOpen { get; private set; }

    PlayerController playerController;
    bool playerControllerWasEnabled;
    int selected;
    Vector2 scroll;

    void Start()
    {
        Load();
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
        if (player != null)
        {
            if (inventory == null) inventory = player.GetComponent<Inventory>();
            playerController = player.GetComponent<PlayerController>();
        }
        if (inventory == null)
            Debug.LogWarning("BaseStorageChest: Nenajdeny Inventory na hracovi.", this);
    }

    void Update()
    {
        if (player == null || inventory == null || Keyboard.current == null) return;
        bool nearby = Vector2.Distance(transform.position, player.position) <= interactionRange;
        if (!nearby && IsOpen) SetOpen(false);
        if (!nearby) return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard.fKey.wasPressedThisFrame)
        {
            SetOpen(!IsOpen);
            return;
        }
        if (!IsOpen) return;
        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            SetOpen(false);
            return;
        }

        List<string> ids = GetVisibleItemIds();
        if (ids.Count == 0) return;
        selected = Mathf.Clamp(selected, 0, ids.Count - 1);
        if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame)
            selected = (selected - 1 + ids.Count) % ids.Count;
        if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame)
            selected = (selected + 1) % ids.Count;

        string id = ids[selected];
        bool all = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
        if (keyboard.aKey.wasPressedThisFrame) Deposit(id, all ? int.MaxValue : 1);
        if (keyboard.dKey.wasPressedThisFrame) Withdraw(id, all ? int.MaxValue : 1);
    }

    void SetOpen(bool open)
    {
        if (IsOpen == open) return;
        IsOpen = open;
        if (playerController == null) return;
        if (open)
        {
            playerControllerWasEnabled = playerController.enabled;
            playerController.enabled = false; // New Input System prestane hrat pohyb
            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;
        }
        else if (playerControllerWasEnabled)
        {
            PlayerStats stats = player.GetComponent<PlayerStats>();
            if (stats == null || !stats.IsDead) playerController.enabled = true;
        }
    }

    void OnDisable()
    {
        if (IsOpen) SetOpen(false);
    }

    public int GetStoredCount(string itemId)
    {
        StoredStack stack = storedItems.Find(x => x != null && x.itemId == itemId);
        return stack == null ? 0 : stack.amount;
    }

    public int Deposit(string itemId, int amount)
    {
        if (inventory == null || string.IsNullOrEmpty(itemId) || amount <= 0) return 0;
        int moved = Mathf.Min(amount, inventory.GetItemCount(itemId));
        if (moved <= 0 || !inventory.RemoveItem(itemId, moved)) return 0;
        StoredStack stack = storedItems.Find(x => x != null && x.itemId == itemId);
        if (stack == null) storedItems.Add(new StoredStack { itemId = itemId, amount = moved });
        else stack.amount += moved;
        Save();
        return moved;
    }

    public int Withdraw(string itemId, int amount)
    {
        if (inventory == null || string.IsNullOrEmpty(itemId) || amount <= 0) return 0;
        StoredStack stack = storedItems.Find(x => x != null && x.itemId == itemId);
        if (stack == null || stack.amount <= 0) return 0;
        int moved = Mathf.Min(amount, stack.amount);
        stack.amount -= moved;
        if (stack.amount == 0) storedItems.Remove(stack);
        inventory.AddItem(itemId, moved);
        Save();
        return moved;
    }

    List<string> GetVisibleItemIds()
    {
        HashSet<string> unique = new HashSet<string>();
        if (inventory != null)
            foreach (Inventory.InventoryItem item in inventory.items)
                if (item != null && item.amount > 0 && !string.IsNullOrEmpty(item.itemId))
                    unique.Add(item.itemId);
        foreach (StoredStack item in storedItems)
            if (item != null && item.amount > 0 && !string.IsNullOrEmpty(item.itemId))
                unique.Add(item.itemId);
        List<string> ids = new List<string>(unique);
        ids.Sort(StringComparer.OrdinalIgnoreCase);
        return ids;
    }

    void OnGUI()
    {
        if (player == null || inventory == null) return;
        bool nearby = Vector2.Distance(transform.position, player.position) <= interactionRange;
        if (!nearby) return;
        if (!IsOpen)
        {
            GUI.Box(new Rect(12f, Screen.height - 80f, 260f, 34f), "[F] SKLAD - otvorit");
            return;
        }

        float width = Mathf.Min(680f, Screen.width - 20f);
        float height = Mathf.Min(490f, Screen.height - 30f);
        Rect panel = new Rect((Screen.width - width) / 2f, (Screen.height - height) / 2f, width, height);
        GUILayout.BeginArea(panel, GUI.skin.box);
        GUILayout.BeginHorizontal();
        GUILayout.Label("BASE STORAGE  |  Inventar <-> Sklad");
        if (GUILayout.Button("Zavriet [F / ESC]", GUILayout.Width(145f))) SetOpen(false);
        GUILayout.EndHorizontal();
        GUILayout.Label("W/S: vyber  |  A: uloz 1  |  D: vyber 1  |  Shift + A/D: vsetko");

        List<string> ids = GetVisibleItemIds();
        if (ids.Count == 0) GUILayout.Label("Zatial nemas ziadne predmety. Najprv prines loot z dungeonu.");
        selected = Mathf.Clamp(selected, 0, Mathf.Max(0, ids.Count - 1));
        scroll = GUILayout.BeginScrollView(scroll, GUILayout.ExpandHeight(true));
        for (int i = 0; i < ids.Count; i++)
        {
            string id = ids[i];
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button((i == selected ? "> " : "  ") + id, GUILayout.MinWidth(95f), GUILayout.ExpandWidth(true))) selected = i;
            GUILayout.Label("Batoh " + inventory.GetItemCount(id) + "  |  Sklad " + GetStoredCount(id), GUILayout.MinWidth(125f));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Uloz 1")) Deposit(id, 1);
            if (GUILayout.Button("Uloz vsetko")) Deposit(id, int.MaxValue);
            if (GUILayout.Button("Vyber 1")) Withdraw(id, 1);
            if (GUILayout.Button("Vyber vsetko")) Withdraw(id, int.MaxValue);
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }
        GUILayout.EndScrollView();
        GUILayout.Label("Sklad v BASE zostava nedotknuty pri smrti. Neulozene veci z vypravy sa riadia RunManagerom.");
        GUILayout.EndArea();
    }
}
