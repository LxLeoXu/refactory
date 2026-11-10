using System.Collections.Generic;
using UnityEngine;

// Mine Spider kladie pasce, jeho standardny pohyb/HP riadi Enemy.cs.
public class GJMineSpiderAttack : MonoBehaviour
{
    [Header("Mine Spider - pasce")]
    [Min(0.5f)] public float mineCooldown = 4.8f;
    [Min(1f)] public float detectionRange = 8f;
    [Range(1, 4)] public int maxActiveMines = 2;
    [Min(0f)] public float mineDamage = 11f;
    [Min(0.1f)] public float mineTriggerRadius = 1.35f;
    [Min(0.1f)] public float mineBlastRadius = 1.9f;
    [Min(0.1f)] public float mineWarningTime = 0.75f;
    [Min(1f)] public float mineLifetime = 7.5f;

    readonly List<GameObject> mines = new List<GameObject>();
    Transform player;
    PlayerStats stats;
    float nextMine;

    void Start()
    {
        GameObject target = GameObject.FindGameObjectWithTag("Player");
        if (target != null)
        {
            player = target.transform;
            stats = target.GetComponent<PlayerStats>();
        }
        nextMine = Time.time + Random.Range(1.8f, 3.2f);
    }

    void Update()
    {
        if (player == null || (stats != null && stats.IsDead)) return;
        if (Time.time < nextMine) return;
        if (Vector2.Distance(transform.position, player.position) > detectionRange) return;

        mines.RemoveAll(mine => mine == null);
        if (mines.Count >= maxActiveMines) return;

        GameObject mineObject = GJSpiderMine.Spawn(transform.position,
            transform.parent, mineDamage, mineTriggerRadius, mineBlastRadius,
            mineWarningTime, mineLifetime);
        mines.Add(mineObject);
        nextMine = Time.time + Mathf.Max(0.5f, mineCooldown);
    }
}
