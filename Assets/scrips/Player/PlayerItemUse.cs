using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Inventory))]
[RequireComponent(typeof(PlayerStats))]
[RequireComponent(typeof(PlayerController))]
public class PlayerItemUse : MonoBehaviour
{
    [Header("Health Potion")]
    public float potionHeal = 30f;

    [Header("Weapon Upgrade")]
    public float weaponDamageBonus = 3f;

    [Header("Armor Upgrade")]
    public float armorBonus = 0.10f;

    [Range(0f, 0.8f)]
    public float maxArmorReduction = 0.5f;

    private Inventory inventory;
    private PlayerStats stats;
    private PlayerController controller;

    void Awake()
    {
        inventory = GetComponent<Inventory>();
        stats = GetComponent<PlayerStats>();
        controller = GetComponent<PlayerController>();
    }

    void Update()
    {
        if (stats.IsDead || Keyboard.current == null)
            return;

        // 1 - Health Potion
        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            UseHealthPotion();
        }

        // 2 - Weapon Upgrade
        if (Keyboard.current.digit2Key.wasPressedThisFrame)
        {
            UseWeaponUpgrade();
        }

        // 3 - Armor Upgrade
        if (Keyboard.current.digit3Key.wasPressedThisFrame)
        {
            UseArmorUpgrade();
        }
    }

    void UseHealthPotion()
    {
        // Nepouzivaj potion pri plnom HP
        if (stats.Health >= stats.maxHealth)
            return;

        if (!inventory.RemoveItem("health_potion", 1))
        {
            Debug.Log("Nemas Health Potion!");
            return;
        }

        stats.Heal(potionHeal);

        Debug.Log("Pouzity Health Potion! +30 HP");
    }

    void UseWeaponUpgrade()
    {
        if (!inventory.RemoveItem("weapon_upgrade", 1))
        {
            Debug.Log("Nemas Weapon Upgrade!");
            return;
        }

        controller.projectileDamage += weaponDamageBonus;

        Debug.Log(
            "Weapon upgraded! Damage: " +
            controller.projectileDamage
        );
    }

    void UseArmorUpgrade()
    {
        if (stats.armorReduction >= maxArmorReduction)
        {
            Debug.Log("Armor je uz maximalne vylepseny!");
            return;
        }

        if (!inventory.RemoveItem("armor_upgrade", 1))
        {
            Debug.Log("Nemas Armor Upgrade!");
            return;
        }

        stats.armorReduction = Mathf.Min(
            maxArmorReduction,
            stats.armorReduction + armorBonus
        );

        Debug.Log(
            "Armor upgraded! Protection: " +
            (stats.armorReduction * 100f) + "%"
        );
    }

    void OnGUI()
    {
        if (inventory == null || stats == null || controller == null)
            return;

        float width = Mathf.Min(340f, Screen.width - 20f);

        Rect box = new Rect(
            10,
            Mathf.Max(10, Screen.height - 125),
            width,
            115
        );

        string info =
            "ITEMS / UPGRADES\n" +
            "1 - Potion: " +
            inventory.GetItemCount("health_potion") + "\n" +
            "2 - Weapon: " +
            inventory.GetItemCount("weapon_upgrade") +
            " | DMG: " + controller.projectileDamage + "\n" +
            "3 - Armor: " +
            inventory.GetItemCount("armor_upgrade") +
            " | Protection: " +
            Mathf.RoundToInt(stats.armorReduction * 100f) + "%";

        GUI.Box(box, info);
    }
}