using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Add ONLY this component to the Player.
// It uses the existing Inventory and finds the dungeon room automatically.
[DisallowMultipleComponent]
[RequireComponent(typeof(Inventory))]
public class GJFieldTurretDeployer : MonoBehaviour
{
    [Header("Field Factory - Mini Turret")]
    [Min(1)] public int scrapCost = 5;
    [Tooltip("Actual item ID from items.json. Default: scrap")]
    public string scrapItemId = "scrap";
    [Min(1)] public int maximumActiveTurrets = 2;
    [Min(0.1f)] public float deployCooldown = 1f;
    [Min(0.2f)] public float placementDistance = 1.2f;

    [Header("Turret balance")]
    [Min(1f)] public float turretLifetime = 15f;
    [Min(1f)] public float turretRange = 7f;
    [Min(0.1f)] public float fireInterval = 0.7f;
    [Min(0.1f)] public float bulletDamage = 8f;
    [Min(0.1f)] public float bulletSpeed = 13f;

    [Header("HUD")]
    public bool showHint = true;

    Inventory inventory;
    PlayerStats stats;
    PlayerController controller;
    float nextDeployTime;
    string notice;
    float noticeUntil;
    readonly List<GJFieldTurret> turrets = new List<GJFieldTurret>();

    void Awake()
    {
        inventory = GetComponent<Inventory>();
        stats = GetComponent<PlayerStats>();
        controller = GetComponent<PlayerController>();
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame)
            TryDeploy();
    }

    public void TryDeploy()
    {
        if (inventory == null || (stats != null && stats.IsDead) ||
            (controller != null && !controller.enabled)) return;

        Room currentRoom = RoomAt(transform.position);
        if (currentRoom == null)
        {
            Notify("Turret mozes postavit iba v dungeone.");
            return;
        }

        turrets.RemoveAll(t => t == null);
        if (turrets.Count >= maximumActiveTurrets)
        {
            Notify("Maximalne " + maximumActiveTurrets + " aktivne turrety.");
            return;
        }
        if (Time.time < nextDeployTime) return;

        string id = ActualScrapId();
        if (inventory.GetItemCount(id) < scrapCost)
        {
            Notify("Potrebujes " + scrapCost + " Scrap.");
            return;
        }

        Vector2 place;
        if (!FindPlacement(currentRoom, out place))
        {
            Notify("Tu nie je miesto na turret.");
            return;
        }

        // Spend only AFTER finding a safe location; never charge on failure.
        if (!inventory.RemoveItem(id, scrapCost))
        {
            Notify("Nepodarilo sa spotrebovat Scrap.");
            return;
        }

        GameObject created = new GameObject("Field_MiniTurret");
        created.transform.SetParent(currentRoom.transform, true);
        created.transform.position = place;
        GJFieldTurret turret = created.AddComponent<GJFieldTurret>();
        turret.Initialize(currentRoom, transform, turretLifetime, turretRange,
            fireInterval, bulletDamage, bulletSpeed);
        turrets.Add(turret);
        nextDeployTime = Time.time + deployCooldown;
        Notify("Mini Turret postaveny! (-" + scrapCost + " Scrap)");
    }

    // The Inventory currently uses exact item ID comparisons. Handle Scrap/scrap safely.
    string ActualScrapId()
    {
        if (inventory == null) return scrapItemId;
        if (inventory.GetItemCount(scrapItemId) > 0) return scrapItemId;
        foreach (Inventory.InventoryItem item in inventory.items)
        {
            if (item != null && item.amount > 0 &&
                string.Equals(item.itemId, scrapItemId, StringComparison.OrdinalIgnoreCase))
                return item.itemId;
        }
        return scrapItemId;
    }

    Room RoomAt(Vector2 position)
    {
        // Room.cs puts a trigger BoxCollider2D on the Room game object.
        Collider2D[] hits = Physics2D.OverlapPointAll(position);
        foreach (Collider2D hit in hits)
        {
            if (hit == null || !hit.isTrigger) continue;
            Room room = hit.GetComponent<Room>();
            if (room != null) return room;
        }
        return null;
    }

    bool FindPlacement(Room room, out Vector2 result)
    {
        Vector2 origin = transform.position;
        Vector2 facing = Vector2.up;
        Camera cam = Camera.main;
        if (cam != null && Mouse.current != null)
        {
            Vector2 mouse = Mouse.current.position.ReadValue();
            Vector3 world = cam.ScreenToWorldPoint(new Vector3(mouse.x, mouse.y,
                Mathf.Abs(cam.transform.position.z - transform.position.z)));
            Vector2 candidate = (Vector2)world - origin;
            if (candidate.sqrMagnitude > 0.01f) facing = candidate.normalized;
        }

        Vector2 right = new Vector2(-facing.y, facing.x);
        Vector2[] points = {
            origin + facing * placementDistance,
            origin + right * placementDistance,
            origin - right * placementDistance,
            origin - facing * placementDistance,
            origin
        };

        foreach (Vector2 point in points)
        {
            if (RoomAt(point) != room || !IsSpotClear(point)) continue;
            result = point;
            return true;
        }
        result = origin;
        return false;
    }

    bool IsSpotClear(Vector2 position)
    {
        foreach (Collider2D c in Physics2D.OverlapCircleAll(position, 0.38f))
        {
            if (c == null || c.isTrigger || c.GetComponentInParent<PlayerStats>() != null)
                continue;
            // Walls, crates and enemies should not have a turret placed on top of them.
            return false;
        }
        return true;
    }

    void Notify(string text)
    {
        notice = text;
        noticeUntil = Time.unscaledTime + 2.5f;
        Debug.Log("FIELD FACTORY: " + text);
    }

    void OnGUI()
    {
        if (!showHint || inventory == null || RoomAt(transform.position) == null) return;
        turrets.RemoveAll(t => t == null);
        string id = ActualScrapId();
        string label = "[Q] Mini Turret (" + scrapCost + " Scrap)   " +
            inventory.GetItemCount(id) + " Scrap   " + turrets.Count + "/" + maximumActiveTurrets;
        Rect rect = new Rect(Mathf.Max(5f, (Screen.width - 290f) / 2f),
            Screen.height - 61f, 290f, 26f);
        GUI.Box(rect, label);
        if (Time.unscaledTime < noticeUntil)
            GUI.Label(new Rect(rect.x, rect.y - 25f, 350f, 24f), notice);
    }

#if UNITY_EDITOR
    [ContextMenu("TEST / Give 10 Scrap (Play Mode)")]
    void GiveTestScrap()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Run Play Mode before giving test items.", this);
            return;
        }
        if (inventory == null) inventory = GetComponent<Inventory>();
        inventory.AddItem(ActualScrapId(), 10);
        Notify("TEST: +10 Scrap");
    }
#endif
}
