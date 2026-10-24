using UnityEngine;
using UnityEngine.InputSystem;

// Game jam HUD: no Canvas, TextMeshPro or scene setup required.
// Attach to any active GameObject in the scene (e.g. GameManager).
public class HUD : MonoBehaviour
{
    [Header("References (optional - found automatically)")]
    public PlayerStats playerStats;
    public Inventory inventory;

    [Header("Appearance")]
    public bool inventoryVisible = true;
    public float uiScale = 1f;

    Texture2D pixel;
    GUIStyle headingStyle;
    GUIStyle textStyle;
    GUIStyle hintStyle;

    void Awake()
    {
        if (playerStats == null || inventory == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                if (playerStats == null) playerStats = player.GetComponent<PlayerStats>();
                if (inventory == null) inventory = player.GetComponent<Inventory>();
            }
        }

        pixel = new Texture2D(1, 1);
        pixel.SetPixel(0, 0, Color.white);
        pixel.Apply();
    }

    void Update()
    {
        // Compatible with New Input System. Tab toggles the inventory panel.
        if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
            inventoryVisible = !inventoryVisible;
    }

    void OnGUI()
    {
        if (pixel == null) return;
        if (headingStyle == null)
        {
            headingStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold };
            headingStyle.normal.textColor = Color.white;
            textStyle = new GUIStyle(GUI.skin.label) { fontSize = 14 };
            textStyle.normal.textColor = Color.white;
            hintStyle = new GUIStyle(GUI.skin.label) { fontSize = 12 };
            hintStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f);
        }

        float scale = Mathf.Clamp(uiScale, 0.75f, 2f);
        Matrix4x4 previousMatrix = GUI.matrix;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
        float screenWidth = Screen.width / scale;

        DrawPanel(new Rect(12, 12, 276, 116));
        GUI.Label(new Rect(24, 18, 250, 25), "PLAYER", headingStyle);
        if (playerStats != null)
        {
            DrawBar(new Rect(24, 50, 250, 23), "HP", playerStats.Health,
                playerStats.maxHealth, new Color(0.86f, 0.22f, 0.23f));
            DrawBar(new Rect(24, 82, 250, 23), "HLAD", playerStats.Hunger,
                playerStats.maxHunger, new Color(0.88f, 0.64f, 0.18f));
        }
        else
        {
            GUI.Label(new Rect(24, 53, 250, 45), "PlayerStats sa nenasiel!", textStyle);
        }

        float x = Mathf.Max(12f, screenWidth - 240f);
        if (inventoryVisible)
        {
            int count = inventory != null ? inventory.items.Count : 0;
            float panelHeight = Mathf.Min(380f, 75f + count * 26f);
            DrawPanel(new Rect(x, 12, 228, panelHeight));
            GUI.Label(new Rect(x + 12, 18, 206, 25), "INVENTAR", headingStyle);
            GUI.Label(new Rect(x + 12, 43, 206, 20), "TAB - skryt", hintStyle);

            if (inventory == null)
                GUI.Label(new Rect(x + 12, 68, 206, 42), "Inventory sa nenasiel!", textStyle);
            else if (count == 0)
                GUI.Label(new Rect(x + 12, 70, 206, 28), "Inventar je prazdny.", textStyle);
            else
            {
                float rowY = 70;
                int visibleRows = Mathf.Min(count, 11);
                for (int i = 0; i < visibleRows; i++)
                {
                    var item = inventory.items[i];
                    if (item == null || item.amount <= 0) continue;
                    GUI.Label(new Rect(x + 12, rowY, 206, 24),
                        item.itemId + "  x" + item.amount, textStyle);
                    rowY += 26;
                }
                if (count > visibleRows)
                    GUI.Label(new Rect(x + 12, rowY, 206, 20), "+ dalsie predmety...", hintStyle);
            }
        }
        else
        {
            DrawPanel(new Rect(x, 12, 228, 32));
            GUI.Label(new Rect(x + 12, 18, 206, 22), "TAB - otvorit inventar", hintStyle);
        }

        GUI.matrix = previousMatrix;
    }

    void DrawPanel(Rect rect)
    {
        Color saved = GUI.color;
        GUI.color = new Color(0.06f, 0.08f, 0.12f, 0.88f);
        GUI.DrawTexture(rect, pixel);
        GUI.color = saved;
    }

    void DrawBar(Rect rect, string label, float value, float maximum, Color fillColor)
    {
        Color saved = GUI.color;
        GUI.color = new Color(0.23f, 0.23f, 0.25f, 1f);
        GUI.DrawTexture(rect, pixel);
        GUI.color = fillColor;
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width *
            Mathf.Clamp01(maximum > 0 ? value / maximum : 0f), rect.height), pixel);
        GUI.color = saved;
        GUI.Label(new Rect(rect.x + 6, rect.y + 1, rect.width - 12, rect.height),
            label + ": " + Mathf.CeilToInt(value) + " / " + Mathf.CeilToInt(maximum), textStyle);
    }

    void OnDestroy()
    {
        if (pixel != null) Destroy(pixel);
    }
}
