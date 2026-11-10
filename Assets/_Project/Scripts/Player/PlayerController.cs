using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem; 

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{

    [Header("Pohyb")]
    public float moveSpeed = 6f;

    [Header("Dash")]
    public float dashSpeed = 18f;
    public float dashDuration = 0.15f;
    public float dashCooldown = 0.8f;

    [Header("Dash - ochrana a efekt")]
    [Tooltip("Ochrana od zaciatku dashu (0.2 s = 0.15 s dash + 0.05 s tolerancia).")]
    [Min(0f)] public float dashInvulnerabilityDuration = 0.2f;
    public bool enableDashTrail = true;
    [Range(0.02f, 0.2f)] public float dashTrailInterval = 0.045f;
    [Range(0.05f, 1f)] public float dashTrailLifetime = 0.18f;
    [Range(0f, 1f)] public float dashTrailOpacity = 0.35f;
    public bool showDashCooldownHUD = true;

    // PlayerStats cez toto kontroluje, ci ma zasah ignorovat.
    public bool IsDashInvulnerable => isActiveAndEnabled && Time.time < dashProtectionEndTime;
    public bool IsDashing => isDashing;
    public float DashCooldownRemaining => Mathf.Max(0f, nextDashTime - Time.time);

    [Header("Útok")]
    public GameObject projectilePrefab;
    public float projectileSpeed = 12f;
    public float projectileLifetime = 2f;
    public float fireRate = 0.25f;
    public float spriteAngleOffset = 0f;
    public float muzzleDistance = 0.6f;     // guľka vyletí kúsok pred hráča
    public float spreadAngle = 1.5f;        // malý rozptyl v stupňoch (0 = presná)
    public bool autoFire = false;
    [Header("Weapon Damage")]
    public float projectileDamage = 10f;    

    Rigidbody2D rb;
    Camera cam;

    // Akcie vytvorené v kóde, nepotrebuješ .inputactions asset
    InputAction moveAction;
    InputAction dashAction;
    InputAction attackAction;
    InputAction aimStickAction;
    InputAction mousePosAction;

    Vector2 moveInput;
    Vector2 aimDir = Vector2.right;

    bool isDashing;
    float dashEndTime;
    float nextDashTime;
    float nextFireTime;
    Vector2 dashDir;
    float dashProtectionEndTime;
    float nextTrailTime;
    float lastUsedDashCooldown = 0.8f;
    SpriteRenderer playerSprite;
    PlayerStats stats;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        cam = Camera.main;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        // U rychleho dashu pomaha zabranit prechodu cez tenke kolidery.
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        playerSprite = GetComponentInChildren<SpriteRenderer>();
        stats = GetComponent<PlayerStats>();

        moveAction = new InputAction("Move", InputActionType.Value);
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
        moveAction.AddBinding("<Gamepad>/leftStick");

        dashAction = new InputAction("Dash", InputActionType.Button);
        dashAction.AddBinding("<Keyboard>/space");
        dashAction.AddBinding("<Gamepad>/buttonSouth");

        attackAction = new InputAction("Attack", InputActionType.Button);
        attackAction.AddBinding("<Mouse>/leftButton");
        attackAction.AddBinding("<Gamepad>/rightTrigger");

        aimStickAction = new InputAction("AimStick", InputActionType.Value, "<Gamepad>/rightStick");
        mousePosAction = new InputAction("MousePos", InputActionType.Value, "<Mouse>/position");
    }

    void OnEnable()
    {
        moveAction.Enable();
        dashAction.Enable();
        attackAction.Enable();
        aimStickAction.Enable();
        mousePosAction.Enable();
    }

    void OnDisable()
    {
        moveAction.Disable();
        dashAction.Disable();
        attackAction.Disable();
        aimStickAction.Disable();
        mousePosAction.Disable();
        ResetDashState();
    }

    // Pri smrti alebo navrate do zakladne nezostane aktivna ochrana ani cooldown.
    public void ResetDashState()
    {
        isDashing = false;
        dashEndTime = 0f;
        dashProtectionEndTime = 0f;
        nextDashTime = 0f;
        nextTrailTime = 0f;
    }

    void OnDestroy()
    {
        moveAction?.Dispose();
        dashAction?.Dispose();
        attackAction?.Dispose();
        aimStickAction?.Dispose();
        mousePosAction?.Dispose();
    }

    void Update()
    {
        moveInput = moveAction.ReadValue<Vector2>();
        UpdateAim();
        HandleDash();
        UpdateDashTrail();
        HandleAttack();
    }

    void FixedUpdate()
    {
        if (isDashing)
            rb.linearVelocity = dashDir * dashSpeed;   // Unity 2022 a staršie: rb.velocity
        else
            rb.linearVelocity = moveInput.normalized * moveSpeed;
    }

    void UpdateAim()
    {
        Vector2 stick = aimStickAction.ReadValue<Vector2>();

        if (stick.sqrMagnitude > 0.2f * 0.2f)
        {
            aimDir = stick.normalized;
        }
        else if (Mouse.current != null)
        {
            Vector2 screenPos = mousePosAction.ReadValue<Vector2>();
            Vector3 world = cam.ScreenToWorldPoint(screenPos);
            Vector2 dir = (Vector2)world - (Vector2)transform.position;
            if (dir.sqrMagnitude > 0.0001f)
                aimDir = dir.normalized;
        }
    }

    void HandleDash()
    {
        if (isDashing && Time.time >= dashEndTime)
            isDashing = false;

        if (dashAction.WasPressedThisFrame() && !isDashing && Time.time >= nextDashTime)
        {
            // dash v smere pohybu, ak stojíš, tak smerom k mieridlu
            dashDir = moveInput.sqrMagnitude > 0.01f ? moveInput.normalized : aimDir;
            isDashing = true;
            dashEndTime = Time.time + Mathf.Max(0.01f, dashDuration);
            // Ochrana moze trvat o kratku chvilu dlhsie ako samotny dash.
            dashProtectionEndTime = Time.time + Mathf.Max(0f, dashInvulnerabilityDuration);
            lastUsedDashCooldown = Mathf.Max(0.01f, dashCooldown);
            nextDashTime = Time.time + Mathf.Max(dashDuration, dashCooldown);
            nextTrailTime = Time.time;
        }
    }

    void UpdateDashTrail()
    {
        if (!isDashing || !enableDashTrail || Time.time < nextTrailTime)
            return;

        nextTrailTime = Time.time + Mathf.Max(0.02f, dashTrailInterval);
        SpawnDashAfterimage();
    }

    void SpawnDashAfterimage()
    {
        if (playerSprite == null || playerSprite.sprite == null) return;

        // Kopia obrazku bez collidera: nenaraza do stien ani do nepriatelov.
        GameObject ghost = new GameObject("DashAfterimage");
        ghost.transform.position = playerSprite.transform.position;
        ghost.transform.rotation = playerSprite.transform.rotation;
        ghost.transform.localScale = playerSprite.transform.lossyScale;

        SpriteRenderer ghostSprite = ghost.AddComponent<SpriteRenderer>();
        ghostSprite.sprite = playerSprite.sprite;
        ghostSprite.sharedMaterial = playerSprite.sharedMaterial;
        ghostSprite.sortingLayerID = playerSprite.sortingLayerID;
        ghostSprite.sortingOrder = playerSprite.sortingOrder - 1;
        ghostSprite.flipX = playerSprite.flipX;
        ghostSprite.flipY = playerSprite.flipY;
        ghostSprite.drawMode = playerSprite.drawMode;
        if (playerSprite.drawMode != SpriteDrawMode.Simple)
            ghostSprite.size = playerSprite.size;

        Color c = playerSprite.color;
        ghostSprite.color = new Color(c.r, c.g, c.b, Mathf.Min(c.a, dashTrailOpacity));

        float lifetime = Mathf.Max(0.05f, dashTrailLifetime);
        Destroy(ghost, lifetime + 0.05f); // zaloha, aj keby sa prerusila coroutine
        StartCoroutine(FadeAfterimage(ghostSprite, lifetime));
    }

    IEnumerator FadeAfterimage(SpriteRenderer ghostSprite, float lifetime)
    {
        Color startColor = ghostSprite.color;
        float passed = 0f;
        while (ghostSprite != null && passed < lifetime)
        {
            passed += Time.deltaTime;
            Color c = startColor;
            c.a = startColor.a * (1f - Mathf.Clamp01(passed / lifetime));
            ghostSprite.color = c;
            yield return null;
        }
        if (ghostSprite != null) Destroy(ghostSprite.gameObject);
    }

    void OnGUI()
    {
        if (!showDashCooldownHUD || !isActiveAndEnabled || (stats != null && stats.IsDead))
            return;

        const float width = 180f;
        float x = Mathf.Max(8f, Screen.width - width - 12f);
        float y = Mathf.Max(8f, Screen.height - 76f);
        GUI.Box(new Rect(x, y, width, 64f), "DASH [SPACE]");

        float remaining = DashCooldownRemaining;
        GUI.Label(new Rect(x + 12f, y + 23f, width - 24f, 20f),
            remaining <= 0f ? "READY" : "Cooldown: " + remaining.ToString("0.0") + " s");

        GUI.Box(new Rect(x + 12f, y + 46f, width - 24f, 9f), GUIContent.none);
        float progress = remaining <= 0f
            ? 1f
            : 1f - Mathf.Clamp01(remaining / Mathf.Max(0.01f, lastUsedDashCooldown));
        Color oldColor = GUI.color;
        GUI.color = remaining <= 0f
            ? new Color(0.3f, 0.9f, 0.7f, 0.95f)
            : new Color(0.65f, 0.75f, 1f, 0.95f);
        GUI.DrawTexture(new Rect(x + 13f, y + 47f, (width - 26f) * progress, 7f),
            Texture2D.whiteTexture);
        GUI.color = oldColor;
    }

    void HandleAttack()
{
    bool wantsToFire = autoFire ? attackAction.IsPressed() : attackAction.WasPressedThisFrame();
    if (!wantsToFire || Time.time < nextFireTime || projectilePrefab == null)
        return;

    nextFireTime = Time.time + fireRate;

    float spread = UnityEngine.Random.Range(-spreadAngle, spreadAngle);
    Vector2 dir = Quaternion.Euler(0, 0, spread) * aimDir;
    float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

    Vector3 spawnPos = transform.position + (Vector3)(dir * muzzleDistance);
    GameObject proj = Instantiate(projectilePrefab, spawnPos,
        Quaternion.Euler(0, 0, angle + spriteAngleOffset));

    Projectile p = proj.GetComponent<Projectile>();
    if (p == null)
        p = proj.AddComponent<Projectile>();

    p.Init(
    dir,
    projectileSpeed,
    GetComponent<Collider2D>(),
    projectileDamage
);

    Destroy(proj, projectileLifetime);
}
}