using UnityEngine;
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

    [Header("Útok")]
    public GameObject projectilePrefab;
    public float projectileSpeed = 12f;
    public float projectileLifetime = 2f;
    public float fireRate = 0.25f;
    public float spriteAngleOffset = 0f;
    public float muzzleDistance = 0.6f;     // guľka vyletí kúsok pred hráča
    public float spreadAngle = 1.5f;        // malý rozptyl v stupňoch (0 = presná)
    public bool autoFire = false;    

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

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        cam = Camera.main;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

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
    }

    void Update()
    {
        moveInput = moveAction.ReadValue<Vector2>();
        UpdateAim();
        HandleDash();
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
            dashEndTime = Time.time + dashDuration;
            nextDashTime = Time.time + dashCooldown;
        }
    }

void HandleAttack()
{
    bool wantsToFire = autoFire ? attackAction.IsPressed() : attackAction.WasPressedThisFrame();
    if (!wantsToFire || Time.time < nextFireTime || projectilePrefab == null)
        return;

    nextFireTime = Time.time + fireRate;

    float spread = Random.Range(-spreadAngle, spreadAngle);
    Vector2 dir = Quaternion.Euler(0, 0, spread) * aimDir;
    float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

    Vector3 spawnPos = transform.position + (Vector3)(dir * muzzleDistance);
    GameObject proj = Instantiate(projectilePrefab, spawnPos,
        Quaternion.Euler(0, 0, angle + spriteAngleOffset));

    Projectile p = proj.GetComponent<Projectile>();
    if (p == null)
        p = proj.AddComponent<Projectile>();

    p.Init(dir, projectileSpeed, GetComponent<Collider2D>());

    Destroy(proj, projectileLifetime);
}
}