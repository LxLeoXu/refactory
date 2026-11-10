using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Pridaj na GameObject Dungeon s DungeonGenerator.
// Reaguje aj na dalsie Generate() - netreba upravovat generator.
public class DungeonBuffSpawner : MonoBehaviour
{
    [Header("Sance po vycisteni miestnosti")]
    [Range(0f, 1f)] public float treasureChance = 1f;
    [Range(0f, 1f)] public float restChance = 0.75f;
    [Range(0f, 1f)] public float eliteChance = 0.65f;
    [Range(0f, 1f)] public float battleChance = 0.18f;

    [Header("Buff")]
    [Min(1f)] public float buffDuration = 35f;
    [Tooltip("Odporucana pozicia mimo stredu truhlice")]
    public Vector2 localPickupPosition = new Vector2(-2.5f, -1.5f);

    [Header("Volitelne sprity (Speed, Damage, Shield, RapidFire)")]
    public Sprite[] buffSprites = new Sprite[4];

    readonly HashSet<Room> handled = new HashSet<Room>();
    static Sprite fallbackSprite;

    void OnEnable() { StartCoroutine(WatchRooms()); }

    IEnumerator WatchRooms()
    {
        while (true)
        {
            handled.RemoveWhere(room => room == null);
            foreach (Room room in GetComponentsInChildren<Room>())
            {
                if (room == null || handled.Contains(room) || !room.IsCleared)
                    continue;

                // Generator vytvara nazvy ako Room 1,0 (Treasure), ... (Elite).
                float chance = GetChance(room.name);
                handled.Add(room); // sanca sa vyhodnoti iba raz
                if (chance > 0f && Random.value <= chance)
                    SpawnPickup(room.transform);
            }
            yield return new WaitForSeconds(0.45f);
        }
    }

    float GetChance(string name)
    {
        if (name.Contains("(Treasure)")) return treasureChance;
        if (name.Contains("(Rest)")) return restChance;
        if (name.Contains("(Elite)")) return eliteChance;
        if (name.Contains("(Battle)") || name.Contains("(Ambush)") ||
            name.Contains("(Ruins)")) return battleChance;
        return 0f; // Start, Supply a Boss bez buffu
    }

    void SpawnPickup(Transform room)
    {
        PlayerBuffType type = (PlayerBuffType)Random.Range(0, 4);
        GameObject go = new GameObject("BuffPickup_" + type);
        go.transform.SetParent(room, false);
        go.transform.localPosition = localPickupPosition;
        go.transform.localScale = Vector3.one * 0.65f;

        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        int index = (int)type;
        renderer.sprite = buffSprites != null && index < buffSprites.Length &&
                          buffSprites[index] != null ? buffSprites[index] : GetFallback();
        renderer.color = BuffColor(type);
        renderer.sortingOrder = 5;

        CircleCollider2D trigger = go.AddComponent<CircleCollider2D>();
        trigger.isTrigger = true;
        trigger.radius = 0.65f;

        DungeonBuffPickup pickup = go.AddComponent<DungeonBuffPickup>();
        pickup.buffType = type;
        pickup.duration = buffDuration;
    }

    static Sprite GetFallback()
    {
        if (fallbackSprite == null)
        {
            Texture2D texture = Texture2D.whiteTexture;
            fallbackSprite = Sprite.Create(texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f), texture.width);
        }
        return fallbackSprite;
    }

    static Color BuffColor(PlayerBuffType type)
    {
        switch (type)
        {
            case PlayerBuffType.Speed: return new Color(0.2f, 0.8f, 1f);
            case PlayerBuffType.Damage: return new Color(1f, 0.35f, 0.3f);
            case PlayerBuffType.Shield: return new Color(0.55f, 1f, 0.45f);
            default: return new Color(1f, 0.8f, 0.25f);
        }
    }
}
