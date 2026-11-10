using UnityEngine;

public class SpriteDirectionSwitcher : MonoBehaviour
{
    [Header("Sprite Settings (Sliced Texture Plate)")]
    [Tooltip("Index 0 = Right (_0), 1 = Left (_1), 2 = Up (_2), 3 = Down (_3)")]
    [SerializeField] private Sprite[] directionalSprites = new Sprite[4];

    private SpriteRenderer spriteRenderer;
    private Rigidbody rb3D;
    private Rigidbody2D rb2D;
    private Vector3 lastPosition;
    private int currentDirectionIndex = -1;

    void Start()
    {
        // Automatically cache the required components on the prefab
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb3D = GetComponent<Rigidbody>();
        rb2D = GetComponent<Rigidbody2D>();
        lastPosition = transform.position;

        // Initialize with the default direction (Right)
        UpdateSprite(0);
    }

    void Update()
    {
        Vector3 movementDirection = DetermineMovementDirection();

        // Only swap the sprite if the entity is actively moving
        if (movementDirection.sqrMagnitude > 0.001f)
        {
            int newDirection = CalculateDirectionIndex(movementDirection);
            if (newDirection != currentDirectionIndex)
            {
                UpdateSprite(newDirection);
            }
        }

        lastPosition = transform.position;
    }

    private Vector3 DetermineMovementDirection()
    {
        // 1. Check movement via 2D Physics Rigidbody
        if (rb2D != null && rb2D.linearVelocity.sqrMagnitude > 0.001f)
        {
            return new Vector3(rb2D.linearVelocity.x, rb2D.linearVelocity.y, 0f);
        }

        // 2. Check movement via 3D Physics Rigidbody
        if (rb3D != null && rb3D.linearVelocity.sqrMagnitude > 0.001f)
        {
            return rb3D.linearVelocity;
        }

        // 3. Fallback: Deduce direction based on positional change per frame
        return transform.position - lastPosition;
    }

    private int CalculateDirectionIndex(Vector3 direction)
    {
        // Compare horizontal velocity vs vertical velocity to pick the dominant axis
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
        {
            // Horizontal dominance: Right (0) vs Left (1)
            return (direction.x > 0) ? 0 : 1;
        }
        else
        {
            // Vertical dominance: Up (2) vs Down (3)
            return (direction.y > 0) ? 2 : 3;
        }
    }

    private void UpdateSprite(int index)
    {
        if (directionalSprites == null || index >= directionalSprites.Length || directionalSprites[index] == null)
            return;

        currentDirectionIndex = index;

        // Instantly swap the visible sub-sprite from your sliced sheet
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = directionalSprites[index];
        }
    }
}

