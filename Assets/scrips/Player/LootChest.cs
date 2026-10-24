using UnityEngine;
using UnityEngine.InputSystem;

public enum LootChestTier { Normal, Treasure, Rest, Elite }

// E otvorenie -> 1-3 itemy vypadnu na zem -> zbieranie cez existujuci ItemPickup.
public class LootChest : MonoBehaviour
{
    [Header("Interakcia")]
    public float interactionDistance = 2.5f;
    public LootChestTier tier = LootChestTier.Normal;

    [Header("Odmien")]
    public int minRewards = 1;
    public int maxRewards = 3;

    [Header("Nepovinne: mozes pouzit svoj pickup prefab")]
    public GameObject pickupPrefab;

    bool opened;
    Transform player;
    SpriteRenderer spriteRenderer;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sortingOrder = 4;
        UpdateSprite();
    }

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }

    public void SetTier(LootChestTier newTier)
    {
        tier = newTier;
        if (tier == LootChestTier.Treasure || tier == LootChestTier.Elite)
        {
            minRewards = 2;
            maxRewards = 3;
        }
        else if (tier == LootChestTier.Rest)
        {
            minRewards = 1;
            maxRewards = 2;
        }
        UpdateSprite();
    }

    void UpdateSprite()
    {
        if (spriteRenderer == null) return;
        Sprite sprite = Resources.Load<Sprite>(
            opened ? "DungeonTiles/chest_open" : "DungeonTiles/chest_closed");
        if (sprite != null) spriteRenderer.sprite = sprite;
    }

    void Update()
    {
        if (opened || Keyboard.current == null) return;
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
        if (player == null || Vector2.Distance(player.position, transform.position) > interactionDistance)
            return;
        PlayerStats stats = player.GetComponent<PlayerStats>();
        if (stats != null && stats.IsDead) return;
        if (Keyboard.current.eKey.wasPressedThisFrame) Open();
    }

    public void Open()
    {
        if (opened) return;
        opened = true;
        UpdateSprite();
        int count = Random.Range(Mathf.Max(1, minRewards), Mathf.Max(minRewards, maxRewards) + 1);
        for (int i = 0; i < count; i++)
        {
            string id = RollItemId();
            int amount = tier == LootChestTier.Treasure ? Random.Range(2, 5) : Random.Range(1, 4);
            if (id == "health_potion" || id.EndsWith("_upgrade")) amount = 1;
            Vector2 direction = new Vector2(
                Mathf.Cos(i * Mathf.PI * 2f / count),
                Mathf.Sin(i * Mathf.PI * 2f / count));
            SpawnPickup(id, amount, (Vector2)transform.position + direction * 1.2f);
        }
        Debug.Log("Chest opened! Tier: " + tier + ", rolls: " + count);
    }

    string RollItemId()
    {
        int value = Random.Range(0, 100);
        switch (tier)
        {
            case LootChestTier.Treasure:
                if (value < 40) return "crystal";
                if (value < 60) return "weapon_part";
                if (value < 75) return "armor_plate";
                if (value < 90) return "health_potion";
                return "weapon_upgrade";
            case LootChestTier.Rest:
                return value < 80 ? "health_potion" : "crystal";
            case LootChestTier.Elite:
                if (value < 25) return "crystal";
                if (value < 50) return "weapon_part";
                if (value < 75) return "armor_plate";
                if (value < 90) return "health_potion";
                return "armor_upgrade";
            default:
                if (value < 50) return "iron";
                if (value < 85) return "scrap";
                if (value < 95) return "crystal";
                return "health_potion";
        }
    }

    void SpawnPickup(string id, int amount, Vector2 position)
    {
        GameObject go;
        if (pickupPrefab != null)
        {
            go = Instantiate(pickupPrefab, position, Quaternion.identity);
        }
        else
        {
            go = new GameObject("Pickup_" + id);
            go.transform.position = position;
            SpriteRenderer sprite = go.AddComponent<SpriteRenderer>();
            sprite.sprite = Resources.Load<Sprite>("ItemIcons/" + id);
            sprite.sortingOrder = 6;
            if (sprite.sprite == null)
            {
                Debug.LogWarning("Chest: chyba sprite ItemIcons/" + id);
            }
            go.transform.localScale = Vector3.one * 0.75f;
            CircleCollider2D collider = go.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.4f;
            Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
        }

        // Drz loot ako dieta miestnosti, aby ho RunManager pri navrate
        // do base vycistil spolu so zvyskom dungeonu.
        if (transform.parent != null) go.transform.SetParent(transform.parent, true);

        ItemPickup pickup = go.GetComponent<ItemPickup>();
        if (pickup == null) pickup = go.AddComponent<ItemPickup>();
        pickup.Setup(id, amount);

        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            Sprite matchingIcon = Resources.Load<Sprite>("ItemIcons/" + id);
            if (matchingIcon != null) sr.sprite = matchingIcon;
        }
    }

    void OnGUI()
    {
        if (opened || player == null) return;
        if (Vector2.Distance(player.position, transform.position) > interactionDistance) return;
        Rect box = new Rect((Screen.width - 230f) / 2f, Screen.height - 140f, 230f, 35f);
        GUI.Box(box, "[E] Otvorit truhlicu");
    }
}
