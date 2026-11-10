using UnityEngine;

public class FoodPickup : MonoBehaviour
{
    public float hungerRestore = 25f;
    public float healthRestore = 0f;

    // Stay (nie Enter), aby sa jedlo dalo zobrať aj neskôr, keď hlad klesne
    void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerStats stats = other.GetComponentInParent<PlayerStats>();
        if (stats == null || stats.IsDead) return;

        // plný hlad = jedlo ostane ležať
        if (stats.Hunger >= stats.maxHunger) return;

        stats.Eat(hungerRestore);
        if (healthRestore > 0f) stats.Heal(healthRestore);

        Destroy(gameObject);
    }
}
