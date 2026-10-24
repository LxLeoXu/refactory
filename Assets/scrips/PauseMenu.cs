using UnityEngine;
using UnityEngine.SceneManagement;
// 1. This line tells the script to use the New Input System
using UnityEngine.InputSystem; 

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    private bool isPaused = false;

    private void Start()
    {
        ResumeGame();
    }

    private void Update()
    {
        // 2. This is the correct way to detect the Escape key in the New Input System
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    public void PauseGame()
    {
        isPaused = true;
        SetMenuVisibility(true);
        Time.timeScale = 0f; 
    }

    public void ResumeGame()
    {
        isPaused = false;
        SetMenuVisibility(false);
        Time.timeScale = 1f; 
    }

    public void QuitToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu"); 
    }

    private void SetMenuVisibility(bool isVisible)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = isVisible ? 1f : 0f;
            canvasGroup.interactable = isVisible;
            canvasGroup.blocksRaycasts = isVisible;
        }
    }
}

