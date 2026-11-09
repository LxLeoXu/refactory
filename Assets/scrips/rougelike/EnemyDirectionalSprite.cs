using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyDirectionalSprite : MonoBehaviour
{
    [Header("Sprite Renderer")]
    public SpriteRenderer targetRenderer;

    [Header("Smery")]
    public Sprite spriteDown;
    public Sprite spriteUp;
    public Sprite spriteLeft;
    public Sprite spriteRight;

    [Header("Nastavenia")]
    public float movementThreshold = 0.1f;

    private Rigidbody2D rb;
    private Vector2 lastDirection = Vector2.down;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (targetRenderer == null)
            targetRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    void LateUpdate()
    {
        if (targetRenderer == null)
            return;

        Vector2 velocity = rb.linearVelocity;

        if (velocity.sqrMagnitude > movementThreshold * movementThreshold)
            lastDirection = velocity.normalized;

        Sprite nextSprite;

        if (Mathf.Abs(lastDirection.x) > Mathf.Abs(lastDirection.y))
        {
            nextSprite = lastDirection.x > 0
                ? spriteRight
                : spriteLeft;
        }
        else
        {
            nextSprite = lastDirection.y > 0
                ? spriteUp
                : spriteDown;
        }

        if (nextSprite != null && targetRenderer.sprite != nextSprite)
            targetRenderer.sprite = nextSprite;
    }
}