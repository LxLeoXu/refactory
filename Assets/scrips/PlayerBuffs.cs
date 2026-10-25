using System.Collections.Generic;
using UnityEngine;

public enum PlayerBuffType
{
    Speed,
    Damage,
    Shield,
    RapidFire
}

// Docasne buffy. Nemeni Inventory, ItemUse, DungeonGenerator ani RunManager.
[RequireComponent(typeof(PlayerController), typeof(PlayerStats))]
public class PlayerBuffs : MonoBehaviour
{
    [Header("Sila buffov")]
    [Min(0f)] public float speedBonus = 2f;
    [Min(0f)] public float damageBonus = 4f;
    [Range(0f, 0.8f)] public float shieldBonus = 0.15f;
    [Min(0f)] public float rapidFireReduction = 0.07f;

    [Header("HUD")]
    public bool showBuffs = true;

    class ActiveBuff
    {
        public float endsAt;
        public float appliedAmount;
    }

    readonly Dictionary<PlayerBuffType, ActiveBuff> active =
        new Dictionary<PlayerBuffType, ActiveBuff>();
    readonly List<PlayerBuffType> expired = new List<PlayerBuffType>();

    PlayerController controller;
    PlayerStats stats;

    void Awake()
    {
        controller = GetComponent<PlayerController>();
        stats = GetComponent<PlayerStats>();
    }

    void OnEnable()
    {
        if (stats != null) stats.OnDied += ClearBuffs;
    }

    void OnDisable()
    {
        if (stats != null) stats.OnDied -= ClearBuffs;
        ClearBuffs();
    }

    public void AddBuff(PlayerBuffType type, float seconds = 35f)
    {
        if (stats == null || stats.IsDead || controller == null) return;
        seconds = Mathf.Max(1f, seconds);

        if (active.TryGetValue(type, out ActiveBuff previous))
        {
            // Opakovany pickup predlzi trvanie, ale nezvysi silu buffu.
            previous.endsAt = Mathf.Max(previous.endsAt, Time.time) + seconds;
            return;
        }

        float applied = 0f;
        switch (type)
        {
            case PlayerBuffType.Speed:
                applied = speedBonus;
                controller.moveSpeed += applied;
                break;

            case PlayerBuffType.Damage:
                applied = damageBonus;
                controller.projectileDamage += applied;
                break;

            case PlayerBuffType.Shield:
                float oldArmor = stats.armorReduction;
                stats.armorReduction = Mathf.Min(0.8f, oldArmor + shieldBonus);
                applied = stats.armorReduction - oldArmor;
                break;

            case PlayerBuffType.RapidFire:
                float oldRate = controller.fireRate;
                controller.fireRate = Mathf.Max(0.07f, oldRate - rapidFireReduction);
                applied = oldRate - controller.fireRate;
                break;
        }

        active.Add(type, new ActiveBuff
        {
            endsAt = Time.time + seconds,
            appliedAmount = applied
        });
        Debug.Log("Buff: " + type + " na " + seconds.ToString("0") + " s");
    }

    void Update()
    {
        if (active.Count == 0) return;
        expired.Clear();
        foreach (var entry in active)
            if (Time.time >= entry.Value.endsAt)
                expired.Add(entry.Key);

        foreach (PlayerBuffType type in expired)
            RemoveBuff(type);
    }

    void RemoveBuff(PlayerBuffType type)
    {
        if (!active.TryGetValue(type, out ActiveBuff buff)) return;

        if (controller != null && stats != null)
        {
            switch (type)
            {
                case PlayerBuffType.Speed:
                    controller.moveSpeed -= buff.appliedAmount;
                    break;
                case PlayerBuffType.Damage:
                    controller.projectileDamage -= buff.appliedAmount;
                    break;
                case PlayerBuffType.Shield:
                    stats.armorReduction = Mathf.Max(0f,
                        stats.armorReduction - buff.appliedAmount);
                    break;
                case PlayerBuffType.RapidFire:
                    controller.fireRate += buff.appliedAmount;
                    break;
            }
        }
        active.Remove(type);
    }

    public void ClearBuffs()
    {
        // Zavolaj aj z RunManager pri navrate do BASE, ak chces okamzity reset.
        expired.Clear();
        expired.AddRange(active.Keys);
        foreach (PlayerBuffType type in expired) RemoveBuff(type);
        expired.Clear();
    }

    void OnGUI()
    {
        if (!showBuffs || active.Count == 0) return;
        float width = 225f;
        float x = Mathf.Max(10f, (Screen.width - width) * 0.5f);
        float y = 10f;
        GUI.Box(new Rect(x, y, width, 27f + active.Count * 23f), "ACTIVE BUFFS");
        int line = 0;
        foreach (var entry in active)
        {
            float left = Mathf.Max(0f, entry.Value.endsAt - Time.time);
            GUI.Label(new Rect(x + 10f, y + 25f + line * 23f, width - 15f, 21f),
                entry.Key + " : " + left.ToString("0") + "s");
            line++;
        }
    }

#if UNITY_EDITOR
    [ContextMenu("TEST / Speed buff")]
    void TestSpeed() { AddBuff(PlayerBuffType.Speed); }
    [ContextMenu("TEST / Damage buff")]
    void TestDamage() { AddBuff(PlayerBuffType.Damage); }
    [ContextMenu("TEST / Shield buff")]
    void TestShield() { AddBuff(PlayerBuffType.Shield); }
    [ContextMenu("TEST / Rapid Fire buff")]
    void TestRapidFire() { AddBuff(PlayerBuffType.RapidFire); }
#endif
}
