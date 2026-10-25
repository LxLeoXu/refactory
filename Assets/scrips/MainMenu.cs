using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string gameSceneName = "LevelScene";

    [Header("Main buttons")]
    [SerializeField] private GameObject continueButton;

    [Header("Confirm popup")]
    [SerializeField] private GameObject confirmPanel;
    [SerializeField] private TMP_Text confirmText;

    [Header("Fade / Controls")]
    [SerializeField] private SceneFader fader;
    [SerializeField] private GameObject controlsPanel;

    [Header("Settings")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private TMP_Dropdown resolutionDropdown;

    private Action pendingConfirmAction;
    private Resolution[] resolutions;

    // Must match BaseStorageChest save file
    private static string SavePath => Path.Combine(Application.persistentDataPath, "base_storage.json");

    private void Start()
    {
        Time.timeScale = 1f;

        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (confirmPanel != null) confirmPanel.SetActive(false);
        if (controlsPanel != null) controlsPanel.SetActive(false);
        if (continueButton != null) continueButton.SetActive(File.Exists(SavePath));

        SetupVolume();
        SetupFullscreen();
        SetupResolution();
    }

    private void Update()
    {
        if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;
        if (confirmPanel != null && confirmPanel.activeSelf) CancelConfirm();
        else if (controlsPanel != null && controlsPanel.activeSelf) CloseControls();
        else if (settingsPanel != null && settingsPanel.activeSelf) CloseSettingsMenu();
    }

    // ---------- Play / Continue / New game ----------

    // Kept for existing button wiring
    public void LoadGameScene(string sceneName)
    {
        Time.timeScale = 1f;
        if (fader != null) fader.FadeToScene(sceneName);
        else SceneManager.LoadScene(sceneName);
    }

    // ---------- Controls ----------

    public void OpenControls()
    {
        if (controlsPanel != null) controlsPanel.SetActive(true);
    }

    public void CloseControls()
    {
        if (controlsPanel != null) controlsPanel.SetActive(false);
    }

    public void ContinueGame()
    {
        LoadGameScene(gameSceneName);
    }

    public void NewGame()
    {
        if (File.Exists(SavePath))
            AskConfirm("Are you sure about starting a new game? Saved progress will be deleted.", StartNewGame);
        else
            StartNewGame();
    }

    private void StartNewGame()
    {
        if (File.Exists(SavePath)) File.Delete(SavePath);
        LoadGameScene(gameSceneName);
    }

    public void ResetProgress()
    {
        AskConfirm("Are you sure about deleting all saved progress?", () =>
        {
            if (File.Exists(SavePath)) File.Delete(SavePath);
            if (continueButton != null) continueButton.SetActive(false);
        });
    }

    // ---------- Quit ----------

    public void QuitGame()
    {
        AskConfirm("Are you sure about quitting the game?", DoQuit);
    }

    private void DoQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ---------- Confirm popup ----------

    private void AskConfirm(string message, Action onYes)
    {
        if (confirmPanel == null)
        {
            onYes?.Invoke(); // no popup assigned, just do it
            return;
        }
        pendingConfirmAction = onYes;
        if (confirmText != null) confirmText.text = message;
        confirmPanel.SetActive(true);
    }

    // Hook to Yes button
    public void ConfirmYes()
    {
        if (confirmPanel != null) confirmPanel.SetActive(false);
        Action action = pendingConfirmAction;
        pendingConfirmAction = null;
        action?.Invoke();
    }

    // Hook to No button
    public void CancelConfirm()
    {
        pendingConfirmAction = null;
        if (confirmPanel != null) confirmPanel.SetActive(false);
    }

    // ---------- Settings ----------

    public void OpenSettingsMenu()
    {
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    public void CloseSettingsMenu()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        PlayerPrefs.Save();
    }

    private void SetupVolume()
    {
        float saved = PlayerPrefs.GetFloat("MusicVolume", 1f);
        AudioListener.volume = saved;
        if (volumeSlider == null) return;
        volumeSlider.SetValueWithoutNotify(saved);
        volumeSlider.onValueChanged.AddListener(SetVolume);
    }

    public void SetVolume(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat("MusicVolume", value);
    }

    private void SetupFullscreen()
    {
        bool full = PlayerPrefs.GetInt("Fullscreen", Screen.fullScreen ? 1 : 0) == 1;
        Screen.fullScreen = full;
        if (fullscreenToggle == null) return;
        fullscreenToggle.SetIsOnWithoutNotify(full);
        fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
    }

    public void SetFullscreen(bool full)
    {
        Screen.fullScreen = full;
        PlayerPrefs.SetInt("Fullscreen", full ? 1 : 0);
    }

    private void SetupResolution()
    {
        if (resolutionDropdown == null) return;

        // Unique width x height list
        List<Resolution> unique = new List<Resolution>();
        List<string> options = new List<string>();
        foreach (Resolution r in Screen.resolutions)
        {
            string label = r.width + " x " + r.height;
            if (options.Contains(label)) continue;
            options.Add(label);
            unique.Add(r);
        }
        resolutions = unique.ToArray();

        int current = 0;
        for (int i = 0; i < resolutions.Length; i++)
        {
            if (resolutions[i].width == Screen.width && resolutions[i].height == Screen.height)
                current = i;
        }

        if (PlayerPrefs.HasKey("ResW"))
        {
            int w = PlayerPrefs.GetInt("ResW"), h = PlayerPrefs.GetInt("ResH");
            Screen.SetResolution(w, h, Screen.fullScreen);
            for (int i = 0; i < resolutions.Length; i++)
                if (resolutions[i].width == w && resolutions[i].height == h) current = i;
        }

        resolutionDropdown.ClearOptions();
        resolutionDropdown.AddOptions(options);
        resolutionDropdown.SetValueWithoutNotify(current);
        resolutionDropdown.onValueChanged.AddListener(SetResolution);
    }

    public void SetResolution(int index)
    {
        if (resolutions == null || index < 0 || index >= resolutions.Length) return;
        Resolution r = resolutions[index];
        Screen.SetResolution(r.width, r.height, Screen.fullScreen);
        PlayerPrefs.SetInt("ResW", r.width);
        PlayerPrefs.SetInt("ResH", r.height);
    }
}
