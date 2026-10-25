using UnityEngine;

// Priradi ho DungeonBuffSpawner automaticky k farebnemu pickup objektu.
public class DungeonBuffPickup : MonoBehaviour
{
    public PlayerBuffType buffType = PlayerBuffType.Speed;
    [Min(1f)] public float duration = 35f;

    bool collected;
    Vector3 origin;
    float phase;

    void Start()
    {
        origin = transform.localPosition;
        phase = Random.Range(0f, 6.28f);
    }

    void Update()
    {
        if (collected) return;
        transform.localPosition = origin + Vector3.up *
            (Mathf.Sin(Time.time * 3.5f + phase) * 0.12f);
    }

    void OnTriggerEnter2D(Collider2D other) { TryCollect(other); }
    void OnTriggerStay2D(Collider2D other) { TryCollect(other); }

    void TryCollect(Collider2D other)
    {
        if (collected) return;
        PlayerBuffs buffs = other.GetComponentInParent<PlayerBuffs>();
        if (buffs == null) return;
        PlayerStats stats = buffs.GetComponent<PlayerStats>();
        if (stats != null && stats.IsDead) return;
        collected = true;
        buffs.AddBuff(buffType, duration);
        Destroy(gameObject);
    }
}
