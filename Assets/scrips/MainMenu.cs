using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject settingsPanel;

    [Header("Settings Elements")]
    [SerializeField] private Slider volumeSlider;

    private void Start()
    {
        // Uistíme sa, že je menu na začiatku skryté
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }

        // Načítame uloženú hlasitosť (ak neexistuje, predvolená je 1.0)
        if (volumeSlider != null)
        {
            volumeSlider.value = PlayerPrefs.GetFloat("MusicVolume", 1.0f);
            volumeSlider.onValueChanged.AddListener(SetVolume);
        }
    }

    public void LoadGameScene(string sceneName)
    {
        Time.timeScale = 1f; 
        SceneManager.LoadScene(sceneName); 
    }

    // Opravená funkcia pre tlačidlo Settings
    public void OpenSettingsMenu()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
        }
    }

    // Nová funkcia pre zatvorenie nastavení
    public void CloseSettingsMenu()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }

    // Volá sa automaticky pri pohybe sliderom
    public void SetVolume(float value)
    {
        PlayerPrefs.SetFloat("MusicVolume", value);
        PlayerPrefs.Save();

        // Tu neskôr prepojíte váš reálny zvukový systém (AudioSource / Mixer)
        Debug.Log($"Hlasitosť hudby zmenená na: {value}"); 
    }
}

