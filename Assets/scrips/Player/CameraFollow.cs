using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    [Header("Cieľ")]
    public Transform target;
    public float smoothTime = 0.15f;

    [Header("Look ahead (kamera sa posúva smerom k myši)")]
    public bool useMouseLookAhead = true;
    [Range(0f, 1f)] public float lookAheadFactor = 0.25f;
    public float maxLookAhead = 3f;

    [Header("Hranice miestnosti / levelu")]
    public bool useBounds = false;
    public Vector2 minBounds = new Vector2(-20, -20);
    public Vector2 maxBounds = new Vector2(20, 20);

    Camera cam;
    Vector3 smoothedPos;
    Vector3 velocity;

    float shakeTimer;
    float shakeDuration;
    float shakeMagnitude;

    void Awake()
    {
        cam = GetComponent<Camera>();
        smoothedPos = transform.position;
    }

    void Start()
    {
        if (target != null)
        {
            smoothedPos = new Vector3(target.position.x, target.position.y, transform.position.z);
            transform.position = smoothedPos;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector2 desired = target.position;

        if (useMouseLookAhead && Mouse.current != null)
        {
            Vector2 mouseScreen = Mouse.current.position.ReadValue();
            Vector2 mouseWorld = cam.ScreenToWorldPoint(mouseScreen);
            Vector2 offset = (mouseWorld - (Vector2)target.position) * lookAheadFactor;
            desired += Vector2.ClampMagnitude(offset, maxLookAhead);
        }

        Vector3 targetPos = new Vector3(desired.x, desired.y, smoothedPos.z);

        if (useBounds)
            targetPos = ClampToBounds(targetPos);

        smoothedPos = Vector3.SmoothDamp(smoothedPos, targetPos, ref velocity, smoothTime);

        // shake sa pripočíta až na konci, aby neovplyvnil plynulé sledovanie
        Vector3 shakeOffset = Vector3.zero;
        if (shakeTimer > 0f)
        {
            shakeTimer -= Time.deltaTime;
            float fade = Mathf.Clamp01(shakeTimer / shakeDuration);
            shakeOffset = (Vector3)(Random.insideUnitCircle * shakeMagnitude * fade);
        }

        transform.position = smoothedPos + shakeOffset;
    }

    Vector3 ClampToBounds(Vector3 pos)
    {
        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;

        float minX = minBounds.x + halfW;
        float maxX = maxBounds.x - halfW;
        float minY = minBounds.y + halfH;
        float maxY = maxBounds.y - halfH;

        // ak je miestnosť menšia ako kamera, vycentruj
        pos.x = minX > maxX ? (minBounds.x + maxBounds.x) * 0.5f : Mathf.Clamp(pos.x, minX, maxX);
        pos.y = minY > maxY ? (minBounds.y + maxBounds.y) * 0.5f : Mathf.Clamp(pos.y, minY, maxY);
        return pos;
    }

    // Zavolaj odkiaľkoľvek: FindFirstObjectByType<CameraFollow>().Shake(0.2f, 0.3f);
    public void Shake(float duration, float magnitude)
    {
        shakeDuration = duration;
        shakeTimer = duration;
        shakeMagnitude = magnitude;
    }

    // Pri prechode do ďalšej miestnosti zmeň hranice
    public void SetBounds(Vector2 min, Vector2 max)
    {
        minBounds = min;
        maxBounds = max;
        useBounds = true;
    }

    void OnDrawGizmosSelected()
    {
        if (!useBounds) return;
        Gizmos.color = Color.yellow;
        Vector2 center = (minBounds + maxBounds) * 0.5f;
        Vector2 size = maxBounds - minBounds;
        Gizmos.DrawWireCube(center, size);
    }
}