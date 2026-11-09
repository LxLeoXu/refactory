using UnityEngine;
using UnityEngine.InputSystem;

// Unity 6.3 / New Input System
// Pridaj na prazdny GameObject v MAIN MENU scene.
// Nepotrebuje Canvas, TextMeshPro ani ziadne prefaby.
public class MainMenuControlsPanel : MonoBehaviour
{
    [Header("Tlacidlo v hlavnom menu (0 az 1)")]
    [Range(0f, 1f)] public float buttonX = 0.44f;
    [Range(0f, 1f)] public float buttonY = 0.61f;
    public string buttonText = "Controls";
    public bool showMenuButton = true;

    [Header("Panel")]
    public bool openAtStart = false;

    bool isOpen;
    GUIStyle titleStyle;
    GUIStyle headingStyle;
    GUIStyle keyStyle;
    GUIStyle textStyle;
    GUIStyle hintStyle;
    GUIStyle buttonStyle;

    const float DesignWidth = 960f;
    const float DesignHeight = 540f;

    void Start()
    {
        isOpen = openAtStart;
    }

    void Update()
    {
        if (isOpen && Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            isOpen = false;
        }
    }

    // Ak mas vlastne tlacidlo v Canvase, pripoj mu tuto public metodu cez OnClick.
    public void OpenControls()  { isOpen = true; }
    public void CloseControls() { isOpen = false; }
    public void ToggleControls() { isOpen = !isOpen; }

    void OnGUI()
    {
        BuildStylesIfNeeded();
        GUI.depth = -1000; // panel pred ostatnymi OnGUI prvkami

        Matrix4x4 previousMatrix = GUI.matrix;
        float scale = Mathf.Min(Screen.width / DesignWidth, Screen.height / DesignHeight);
        if (scale <= 0f) return;

        float offsetX = (Screen.width - DesignWidth * scale) * 0.5f;
        float offsetY = (Screen.height - DesignHeight * scale) * 0.5f;
        GUI.matrix = Matrix4x4.TRS(
            new Vector3(offsetX, offsetY, 0f),
            Quaternion.identity,
            new Vector3(scale, scale, 1f));

        // Tlacidlo je pod Play / Settings; poziciu si vies nastavit v Inspectore.
        if (!isOpen && showMenuButton)
        {
            if (GUI.Button(new Rect(buttonX * DesignWidth,
                                    buttonY * DesignHeight,
                                    142f, 40f), buttonText, buttonStyle))
            {
                isOpen = true;
            }
        }

        if (isOpen)
        {
            DrawRect(new Rect(0f, 0f, DesignWidth, DesignHeight),
                new Color(0.015f, 0.025f, 0.05f, 0.88f));
            DrawRect(new Rect(74f, 32f, 812f, 476f),
                new Color(0.07f, 0.11f, 0.18f, 0.98f));
            DrawRect(new Rect(74f, 32f, 812f, 5f),
                new Color(0.97f, 0.64f, 0.18f));

            GUI.Label(new Rect(105f, 51f, 480f, 48f),
                "HOW TO PLAY", titleStyle);

            if (GUI.Button(new Rect(747f, 54f, 105f, 37f),
                "CLOSE  X", buttonStyle))
            {
                isOpen = false;
            }

            DrawRect(new Rect(104f, 105f, 752f, 2f),
                new Color(0.22f, 0.37f, 0.53f));

            GUI.Label(new Rect(108f, 119f, 347f, 34f),
                "MOVEMENT / ITEMS", headingStyle);
            GUI.Label(new Rect(501f, 119f, 347f, 34f),
                "WEAPONS / FACTORY", headingStyle);

            float y = 161f;
            const float line = 32f;
            DrawRow(108f, y + 0f * line, "WASD", "Move");
            DrawRow(108f, y + 1f * line, "MOUSE", "Aim");
            DrawRow(108f, y + 2f * line, "LMB", "Shoot");
            DrawRow(108f, y + 3f * line, "SPACE", "Dash / dodge");
            DrawRow(108f, y + 4f * line, "Q", "Place turret (5 Scrap)");
            DrawRow(108f, y + 5f * line, "TAB", "Inventory");
            DrawRow(108f, y + 6f * line, "1", "Health potion");
            DrawRow(108f, y + 7f * line, "2", "Weapon upgrade");
            DrawRow(108f, y + 8f * line, "3", "Armor upgrade");

            DrawRow(501f, y + 0f * line, "0", "Starter weapon");
            DrawRow(501f, y + 1f * line, "4", "Scrap Shotgun");
            DrawRow(501f, y + 2f * line, "5", "Crystal Rifle");
            DrawRow(501f, y + 3f * line, "6", "Furnace Cannon");
            DrawRow(501f, y + 4f * line, "E", "Factory interaction");
            DrawRow(501f, y + 5f * line, "F", "Forge / storage");
            DrawRow(501f, y + 6f * line, "W/S", "Select storage item");
            DrawRow(501f, y + 7f * line, "A/D", "Deposit / withdraw");
            DrawRow(501f, y + 8f * line, "SHIFT", "+ A/D = all items");

            DrawRect(new Rect(105f, 452f, 748f, 1f),
                new Color(0.22f, 0.37f, 0.53f));
            GUI.Label(new Rect(109f, 460f, 738f, 32f),
                "ESC closes this panel   |   Collect loot, craft upgrades, survive the dungeon!",
                hintStyle);
        }

        GUI.matrix = previousMatrix;
    }

    void DrawRow(float x, float y, string key, string description)
    {
        GUI.Label(new Rect(x, y, 84f, 28f), key, keyStyle);
        GUI.Label(new Rect(x + 91f, y, 290f, 28f), description, textStyle);
    }

    static void DrawRect(Rect rect, Color color)
    {
        Color previousColor = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previousColor;
    }

    void BuildStylesIfNeeded()
    {
        if (titleStyle != null) return;

        titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 34,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };
        titleStyle.normal.textColor = new Color(1f, 0.72f, 0.29f);

        headingStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 21,
            fontStyle = FontStyle.Bold
        };
        headingStyle.normal.textColor = new Color(0.46f, 0.84f, 1f);

        keyStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold
        };
        keyStyle.normal.textColor = new Color(1f, 0.72f, 0.29f);

        textStyle = new GUIStyle(GUI.skin.label) { fontSize = 18 };
        textStyle.normal.textColor = Color.white;

        hintStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 17,
            alignment = TextAnchor.MiddleCenter
        };
        hintStyle.normal.textColor = new Color(0.74f, 0.82f, 0.91f);

        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 19,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
    }
}
