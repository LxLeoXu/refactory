using UnityEngine;
using UnityEngine.UI;

public class PlayerHUD : MonoBehaviour
{
    public PlayerStats stats;
    public Slider healthBar;
    public Slider hungerBar;

    void Start()
    {
        if (stats == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) stats = p.GetComponent<PlayerStats>();
        }

        if (stats == null)
        {
            Debug.LogWarning("PlayerHUD: nenašiel som PlayerStats na hráčovi.");
            return;
        }

        stats.OnHealthChanged += UpdateHealth;
        stats.OnHungerChanged += UpdateHunger;

        UpdateHealth(stats.Health, stats.maxHealth);
        UpdateHunger(stats.Hunger, stats.maxHunger);
    }

    void OnDestroy()
    {
        if (stats == null) return;
        stats.OnHealthChanged -= UpdateHealth;
        stats.OnHungerChanged -= UpdateHunger;
    }

    void UpdateHealth(float current, float max)
    {
        if (healthBar == null) return;
        healthBar.maxValue = max;
        healthBar.value = current;
    }

    void UpdateHunger(float current, float max)
    {
        if (hungerBar == null) return;
        hungerBar.maxValue = max;
        hungerBar.value = current;
    }
}
