using UnityEngine;
using UnityEngine.InputSystem;
using System.Reflection;

// Add ONCE to Player. Reuses the existing projectile prefab and PlayerController movement.
// Controls shooting while enabled. Does not modify or replace PlayerController.cs.
[RequireComponent(typeof(PlayerController), typeof(Inventory))]
public class FactoryWeapons : MonoBehaviour
{
    public enum WeaponType { Starter, ScrapShotgun, CrystalRifle, FurnaceCannon }

    [Header("Equipped weapon")]
    public WeaponType equipped = WeaponType.Starter;

    [Header("Compatibility")]
    [Tooltip("Damage used when PlayerController does not have projectileDamage field.")]
    [Min(1f)] public float fallbackBaseDamage = 10f;
    public bool showWeaponHud = true;

    PlayerController controller;
    PlayerStats stats;
    Inventory inventory;
    GameObject projectilePrefab;
    Collider2D ownerCollider;
    Camera cam;
    FieldInfo playerDamageField;
    float nextShotTime;
    bool initialized;
    string notification;
    float notificationUntil;

    const string ShotgunId = "factory_scrap_shotgun";
    const string RifleId = "factory_crystal_rifle";
    const string CannonId = "factory_furnace_cannon";

    void Awake()
    {
        controller = GetComponent<PlayerController>();
        stats = GetComponent<PlayerStats>();
        inventory = GetComponent<Inventory>();
        ownerCollider = GetComponent<Collider2D>();
        cam = Camera.main;
        // Older versions of PlayerController lack projectileDamage; this supports both.
        playerDamageField = typeof(PlayerController).GetField("projectileDamage", BindingFlags.Instance | BindingFlags.Public);
    }

    void Start()
    {
        if (controller == null) { enabled = false; return; }
        projectilePrefab = controller.projectilePrefab;
        if (projectilePrefab == null)
        {
            Debug.LogError("FactoryWeapons: Assign a projectile prefab to PlayerController first.", this);
            enabled = false;
            return;
        }
        if (projectilePrefab.GetComponentInChildren<Collider2D>() == null)
            Debug.LogWarning("FactoryWeapons: Projectile prefab needs Collider2D (preferably Is Trigger).", this);
        // Stop the default single-shot system while keeping movement + dash enabled.
        controller.projectilePrefab = null;
        initialized = true;
        if (equipped != WeaponType.Starter && !Owns(equipped)) equipped = WeaponType.Starter;
    }

    void OnEnable()
    {
        if (initialized && controller != null && controller.projectilePrefab == projectilePrefab)
            controller.projectilePrefab = null;
    }

    void OnDisable()
    {
        // Restore normal weapon shooting if this component is disabled.
        if (controller != null && controller.projectilePrefab == null && projectilePrefab != null)
            controller.projectilePrefab = projectilePrefab;
    }

    void Update()
    {
        if (controller == null || !controller.enabled || projectilePrefab == null ||
            (stats != null && stats.IsDead)) return;

        Keyboard kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.digit0Key.wasPressedThisFrame || kb.numpad0Key.wasPressedThisFrame)
                Equip(WeaponType.Starter);
            if (kb.digit4Key.wasPressedThisFrame || kb.numpad4Key.wasPressedThisFrame)
                Equip(WeaponType.ScrapShotgun);
            if (kb.digit5Key.wasPressedThisFrame || kb.numpad5Key.wasPressedThisFrame)
                Equip(WeaponType.CrystalRifle);
            if (kb.digit6Key.wasPressedThisFrame || kb.numpad6Key.wasPressedThisFrame)
                Equip(WeaponType.FurnaceCannon);
        }

        // If crafting items were lost on death, fall back to starter automatically.
        if (!Owns(equipped)) equipped = WeaponType.Starter;

        bool wantsFire = false;
        Mouse mouse = Mouse.current;
        if (mouse != null)
            wantsFire = controller.autoFire || equipped != WeaponType.Starter
                ? mouse.leftButton.isPressed : mouse.leftButton.wasPressedThisFrame;
        Gamepad pad = Gamepad.current;
        if (pad != null)
            wantsFire |= controller.autoFire || equipped != WeaponType.Starter
                ? pad.rightTrigger.isPressed : pad.rightTrigger.wasPressedThisFrame;

        if (wantsFire && Time.time >= nextShotTime) Fire();
    }

    public bool Equip(WeaponType type)
    {
        if (!Owns(type))
        {
            Notify("Najprv vyrob zbran vo factory!");
            return false;
        }
        equipped = type;
        Notify("Zbran: " + WeaponName(type));
        return true;
    }

    bool Owns(WeaponType type)
    {
        if (type == WeaponType.Starter) return true;
        if (inventory == null) return false;
        string id = type == WeaponType.ScrapShotgun ? ShotgunId :
                    type == WeaponType.CrystalRifle ? RifleId : CannonId;
        return inventory.GetItemCount(id) > 0;
    }

    void Fire()
    {
        Vector2 aim = GetAimDirection();
        int pelletCount = 1;
        float damageMultiplier = 1f;
        float speedMultiplier = 1f;
        float rateMultiplier = 1f;
        float cone = controller.spreadAngle;
        float visualScale = 1f;
        Color tint = Color.white;

        switch (equipped)
        {
            case WeaponType.ScrapShotgun:
                pelletCount = 3; damageMultiplier = 0.65f; rateMultiplier = 1.8f;
                cone = 18f; tint = new Color(1f, 0.67f, 0.29f); break;
            case WeaponType.CrystalRifle:
                damageMultiplier = 1.7f; rateMultiplier = 0.85f;
                speedMultiplier = 1.35f; cone = 0.3f;
                tint = new Color(0.38f, 0.97f, 1f); break;
            case WeaponType.FurnaceCannon:
                damageMultiplier = 3f; rateMultiplier = 2.2f;
                speedMultiplier = 0.8f; cone = 1f; visualScale = 1.5f;
                tint = new Color(1f, 0.38f, 0.15f); break;
        }

        float cooldown = Mathf.Max(0.07f, controller.fireRate * rateMultiplier);
        nextShotTime = Time.time + cooldown;
        float baseDamage = GetBaseDamage();

        for (int i = 0; i < pelletCount; i++)
        {
            float degrees = pelletCount == 1 ? Random.Range(-cone, cone) :
                Mathf.Lerp(-cone * 0.5f, cone * 0.5f, i / (float)(pelletCount - 1));
            Vector2 direction = Quaternion.Euler(0, 0, degrees) * aim;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            Vector3 pos = transform.position + (Vector3)(direction * controller.muzzleDistance);

            GameObject bullet = Instantiate(projectilePrefab, pos,
                Quaternion.Euler(0, 0, angle + controller.spriteAngleOffset));
            // Important: older Enemy.cs deals fixed damage to projectiles tagged "Projectile".
            // Untagging prevents double hits; FactoryWeaponHit handles actual damage instead.
            bullet.tag = "Untagged";
            if (visualScale != 1f) bullet.transform.localScale *= visualScale;
            SpriteRenderer sr = bullet.GetComponentInChildren<SpriteRenderer>();
            if (sr != null && equipped != WeaponType.Starter) sr.color = tint;

            FactoryWeaponHit hit = bullet.GetComponent<FactoryWeaponHit>();
            if (hit == null) hit = bullet.AddComponent<FactoryWeaponHit>();
            hit.Init(Mathf.Max(1f, baseDamage * damageMultiplier), transform);

            Projectile projectile = bullet.GetComponent<Projectile>();
            if (projectile == null) projectile = bullet.AddComponent<Projectile>();
            projectile.Init(direction, controller.projectileSpeed * speedMultiplier, ownerCollider);
            Destroy(bullet, Mathf.Max(0.1f, controller.projectileLifetime));
        }
    }

    float GetBaseDamage()
    {
        if (playerDamageField != null && playerDamageField.FieldType == typeof(float))
            return Mathf.Max(1f, (float)playerDamageField.GetValue(controller));
        return Mathf.Max(1f, fallbackBaseDamage);
    }

    Vector2 GetAimDirection()
    {
        Gamepad pad = Gamepad.current;
        if (pad != null)
        {
            Vector2 stick = pad.rightStick.ReadValue();
            if (stick.sqrMagnitude > 0.04f) return stick.normalized;
        }
        if (cam == null) cam = Camera.main;
        if (cam != null && Mouse.current != null)
        {
            Vector2 screen = Mouse.current.position.ReadValue();
            Vector3 world = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y,
                Mathf.Abs(cam.transform.position.z - transform.position.z)));
            Vector2 dir = (Vector2)(world - transform.position);
            if (dir.sqrMagnitude > 0.001f) return dir.normalized;
        }
        return transform.right;
    }

    static string WeaponName(WeaponType w)
    {
        switch (w)
        {
            case WeaponType.ScrapShotgun: return "Scrap Shotgun";
            case WeaponType.CrystalRifle: return "Crystal Rifle";
            case WeaponType.FurnaceCannon: return "Furnace Cannon";
            default: return "Starter";
        }
    }

    void Notify(string text)
    {
        notification = text;
        notificationUntil = Time.unscaledTime + 2.5f;
        Debug.Log("FactoryWeapons: " + text);
    }

    void OnGUI()
    {
        if (!showWeaponHud || inventory == null) return;
        float w = 260f;
        float x = Mathf.Max(10f, Screen.width - w - 10f);
        float y = Mathf.Max(10f, Screen.height - 175f);
        GUI.Box(new Rect(x, y, w, 160f), "FACTORY WEAPONS");
        GUI.Label(new Rect(x + 10f, y + 25f, w - 20f, 23f), "Equipped: " + WeaponName(equipped));
        GUI.Label(new Rect(x + 10f, y + 48f, w - 20f, 20f), "[0] Starter");
        GUI.Label(new Rect(x + 10f, y + 68f, w - 20f, 20f), "[4] Scrap Shotgun " + (Owns(WeaponType.ScrapShotgun) ? "OK" : "LOCK"));
        GUI.Label(new Rect(x + 10f, y + 88f, w - 20f, 20f), "[5] Crystal Rifle " + (Owns(WeaponType.CrystalRifle) ? "OK" : "LOCK"));
        GUI.Label(new Rect(x + 10f, y + 108f, w - 20f, 20f), "[6] Furnace Cannon " + (Owns(WeaponType.FurnaceCannon) ? "OK" : "LOCK"));
        if (Time.unscaledTime < notificationUntil)
            GUI.Label(new Rect(x + 10f, y + 133f, w - 20f, 20f), notification);
    }

#if UNITY_EDITOR
    [ContextMenu("TEST / Give All Factory Weapons")]
    void GiveTestWeapons()
    {
        Inventory inv = GetComponent<Inventory>();
        if (inv == null) return;
        inv.AddItem(ShotgunId, 1);
        inv.AddItem(RifleId, 1);
        inv.AddItem(CannonId, 1);
    }
#endif
}
