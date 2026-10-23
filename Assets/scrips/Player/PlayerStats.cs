using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerStats : MonoBehaviour, IDamageable
{
    [Header("Zdravie")]
    public float maxHealth = 100f;
    public float invulnerabilityTime = 0.6f;

    [Header("Hlad")]
    public float maxHunger = 100f;
    [Tooltip("0.4 = prázdny hlad za cca 4 minúty")]
    public float hungerDrainPerSecond = 0.4f;
    [Tooltip("Damage za sekundu, keď je hlad na nule")]
    public float starvationDamagePerSecond = 2f;

    [Header("Smrť")]
    public bool restartOnDeath = true;
    public float restartDelay = 2f;

    public float Health { get; private set; }
    public float Hunger { get; private set; }
    public bool IsDead { get; private set; }

    public event Action<float, float> OnHealthChanged;   // aktuálne, max
    public event Action<float, float> OnHungerChanged;   // aktuálne, max
    public event Action OnDied;

    SpriteRenderer sr;
    Color baseColor = Color.white;
    float invulnerableUntil;

    void Awake()
    {
        Health = maxHealth;
        Hunger = maxHunger;

        sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) baseColor = sr.color;
    }

    void Update()
    {
        if (IsDead) return;

        if (Hunger > 0f)
        {
            Hunger = Mathf.Max(0f, Hunger - hungerDrainPerSecond * Time.deltaTime);
            OnHungerChanged?.Invoke(Hunger, maxHunger);
        }
        else
        {
            // hladuje, pomaly stráca zdravie (bez blikania a bez nezraniteľnosti)
            Damage(starvationDamagePerSecond * Time.deltaTime);
        }
    }

    // volajú nepriatelia a ich projektily
    public void TakeDamage(float damage, Vector2 hitDirection)
    {
        if (IsDead || Time.time < invulnerableUntil) return;

        invulnerableUntil = Time.time + invulnerabilityTime;
        Damage(damage);

        if (!IsDead)
            StartCoroutine(Blink());
    }

    public void Heal(float amount)
    {
        if (IsDead) return;
        Health = Mathf.Min(maxHealth, Health + amount);
        OnHealthChanged?.Invoke(Health, maxHealth);
    }

    public void Eat(float amount)
    {
        if (IsDead) return;
        Hunger = Mathf.Min(maxHunger, Hunger + amount);
        OnHungerChanged?.Invoke(Hunger, maxHunger);
    }

    void Damage(float amount)
    {
        if (IsDead) return;

        Health = Mathf.Max(0f, Health - amount);
        OnHealthChanged?.Invoke(Health, maxHealth);

        if (Health <= 0f)
            Die();
    }

    void Die()
    {
        IsDead = true;
        OnDied?.Invoke();

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;

        PlayerController pc = GetComponent<PlayerController>();
        if (pc != null) pc.enabled = false;

        if (restartOnDeath)
            Invoke(nameof(Restart), restartDelay);
    }

    void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    IEnumerator Blink()
    {
        if (sr == null) yield break;

        float end = Time.time + invulnerabilityTime;
        bool faded = false;

        while (Time.time < end)
        {
            faded = !faded;
            sr.color = faded ? new Color(baseColor.r, baseColor.g, baseColor.b, 0.35f) : baseColor;
            yield return new WaitForSeconds(0.08f);
        }

        sr.color = baseColor;
    }
}
