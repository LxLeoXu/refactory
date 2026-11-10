using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private bool debugPauseInput = true;

    private bool isPaused;

    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
    }

    private void Start()
    {
        ResumeGame();
        if (canvasGroup == null)
            Debug.LogError("PauseMenu: CanvasGroup nie je priradeny!", this);
    }

 private void Update()
{
    // TEST: Stlačením M počas pauzy sa vráť do menu.
    if (Keyboard.current != null &&
        isPaused &&
        Keyboard.current.mKey.wasPressedThisFrame)
    {
        QuitToMainMenu();
        return;
    }

    if (Keyboard.current == null ||
        !Keyboard.current.escapeKey.wasPressedThisFrame)
        return;

    if (debugPauseInput)
        Debug.Log("PauseMenu: ESC zachytene, isPaused=" + isPaused, this);

    if (isPaused)
        ResumeGame();
    else
        PauseGame();
}

    public void PauseGame()
    {
        if (canvasGroup == null)
        {
            Debug.LogError("PauseMenu: Pauza nezapnuta - chyba CanvasGroup.", this);
            return;
        }

        isPaused = true;
        SetMenuVisibility(true);
        Time.timeScale = 0f;
        if (debugPauseInput) Debug.Log("PauseMenu: hra pozastavena", this);
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
        SetMenuVisibility(false);
        if (debugPauseInput) Debug.Log("PauseMenu: hra pokracuje", this);
    }

public void QuitToMainMenu()
{
    Debug.Log("EXIT BUTTON CLICKED!");

    Time.timeScale = 1f;
    SceneManager.LoadScene("MainMenu");
}  

    private void SetMenuVisibility(bool visible)
    {
        if (canvasGroup == null) return;
        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
    }

    private void OnDisable()
    {
        if (!isPaused) return;
        isPaused = false;
        Time.timeScale = 1f;
    }
}

