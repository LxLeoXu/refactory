using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// REFACTORY Patch 0.1.1
// Nezavisle Pause Menu: kresli sa cez OnGUI, ale kliknutia cita cez NEW Input System.
// Nepotrebuje Canvas, CanvasGroup, Button, EventSystem ani TextMeshPro.
// Dolezite: stare PauseMenu v Hierarchy vypni, aby naraz nebezali dva pause skripty.
public class RefactoryPauseMenuV2 : MonoBehaviour
{
    [Header("Scena hlavneho menu (musi byt v Build Profiles > Scene List)")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Diagnostika")]
    [SerializeField] private bool debugMessages = true;

    private bool isPaused;
    private bool previousCursorVisible;
    private CursorLockMode previousCursorLock;
    private string errorMessage = "";

    public bool IsPaused => isPaused;

    private void Start()
    {
        isPaused = false;
        Time.timeScale = 1f;
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            if (isPaused) ResumeGame();
            else PauseGame();
            return;
        }

        if (!isPaused) return;

        // Na rozdiel od GUI.Button to funguje aj s Active Input Handling = Input System Package.
        Mouse mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;

        Vector2 mousePosition = mouse.position.ReadValue();
        mousePosition.y = Screen.height - mousePosition.y; // Input System: dolny -> GUI: horny zaciatok

        if (GetContinueRect().Contains(mousePosition))
            ResumeGame();
        else if (GetMainMenuRect().Contains(mousePosition))
            QuitToMainMenu();
    }

    public void PauseGame()
    {
        if (isPaused) return;

        previousCursorVisible = Cursor.visible;
        previousCursorLock = Cursor.lockState;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        isPaused = true;
        errorMessage = "";
        Time.timeScale = 0f;
        if (debugMessages) Debug.Log("PauseMenuV2: PAUSED", this);
    }

    public void ResumeGame()
    {
        if (!isPaused) return;

        isPaused = false;
        errorMessage = "";
        Time.timeScale = 1f;
        Cursor.lockState = previousCursorLock;
        Cursor.visible = previousCursorVisible;
        if (debugMessages) Debug.Log("PauseMenuV2: RESUMED", this);
    }

    public void QuitToMainMenu()
    {
        if (!Application.CanStreamedLevelBeLoaded(mainMenuSceneName))
        {
            errorMessage = "Scene '" + mainMenuSceneName + "' is missing from Scene List!";
            Debug.LogError("PauseMenuV2: " + errorMessage, this);
            return;
        }

        if (debugMessages) Debug.Log("PauseMenuV2: Loading " + mainMenuSceneName, this);

        // Skutocne navrat do menu, nie ukoncenie aplikacie.
        isPaused = false;
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void OnDisable()
    {
        // Ak sa objekt vypne pocas pauzy, hra nesmie zostat zmrazena.
        if (!isPaused) return;
        isPaused = false;
        Time.timeScale = 1f;
        Cursor.lockState = previousCursorLock;
        Cursor.visible = previousCursorVisible;
    }

    private Rect GetPanelRect()
    {
        float width = Mathf.Min(480f, Mathf.Max(200f, Screen.width - 24f));
        float height = Mathf.Min(300f, Mathf.Max(170f, Screen.height - 24f));
        return new Rect((Screen.width - width) * 0.5f,
                        (Screen.height - height) * 0.5f, width, height);
    }

    private Rect GetContinueRect()
    {
        Rect panel = GetPanelRect();
        return new Rect(panel.x + 30f, panel.y + panel.height * 0.38f,
                        panel.width - 60f, 52f);
    }

    private Rect GetMainMenuRect()
    {
        Rect panel = GetPanelRect();
        return new Rect(panel.x + 30f, panel.y + panel.height * 0.38f + 64f,
                        panel.width - 60f, 52f);
    }

    private static void DrawRect(Rect rect, Color color)
    {
        Color saved = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = saved;
    }

    private void OnGUI()
    {
        if (!isPaused) return;

        // Iba vykreslovanie. Kliknutia sa riesia v Update cez Mouse.current.
        GUI.depth = -1000;
        DrawRect(new Rect(0, 0, Screen.width, Screen.height),
                 new Color(0.02f, 0.04f, 0.08f, 0.80f));

        Rect panel = GetPanelRect();
        DrawRect(panel, new Color(0.055f, 0.085f, 0.13f, 0.98f));
        DrawRect(new Rect(panel.x, panel.y, panel.width, 5f), new Color(1f, 0.55f, 0.10f));

        GUIStyle title = new GUIStyle(GUI.skin.label);
        title.alignment = TextAnchor.MiddleCenter;
        title.fontSize = 29;
        title.fontStyle = FontStyle.Bold;
        title.normal.textColor = Color.white;
        GUI.Label(new Rect(panel.x + 8f, panel.y + 24f, panel.width - 16f, 54f),
                  "REFACTORY  |  PAUSED", title);

        Mouse mouse = Mouse.current;
        Vector2 hoverPoint = new Vector2(-999f, -999f);
        if (mouse != null)
        {
            hoverPoint = mouse.position.ReadValue();
            hoverPoint.y = Screen.height - hoverPoint.y;
        }

        Rect continueRect = GetContinueRect();
        DrawRect(continueRect, continueRect.Contains(hoverPoint)
                 ? new Color(0.30f, 0.82f, 0.45f) : new Color(0.17f, 0.55f, 0.32f));
        Rect mainRect = GetMainMenuRect();
        DrawRect(mainRect, mainRect.Contains(hoverPoint)
                 ? new Color(0.93f, 0.40f, 0.25f) : new Color(0.62f, 0.24f, 0.19f));

        GUIStyle buttonText = new GUIStyle(GUI.skin.label);
        buttonText.alignment = TextAnchor.MiddleCenter;
        buttonText.fontSize = 21;
        buttonText.fontStyle = FontStyle.Bold;
        buttonText.normal.textColor = Color.white;
        GUI.Label(continueRect, "CONTINUE", buttonText);
        GUI.Label(mainRect, "MAIN MENU", buttonText);

        GUIStyle hint = new GUIStyle(GUI.skin.label);
        hint.alignment = TextAnchor.MiddleCenter;
        hint.fontSize = 13;
        hint.normal.textColor = new Color(0.75f, 0.83f, 0.90f);
        GUI.Label(new Rect(panel.x + 6f, panel.yMax - 37f, panel.width - 12f, 25f),
                  "ESC - Resume", hint);

        if (!string.IsNullOrEmpty(errorMessage))
        {
            hint.normal.textColor = Color.red;
            GUI.Label(new Rect(panel.x + 5f, panel.yMax - 60f, panel.width - 10f, 24f),
                      errorMessage, hint);
        }
    }
}
