// REFACTORY Patch 0.1.1 – one-click editor repair for PauseMenu UI.
// Put this file ONLY inside Assets/Editor/ (or another Editor folder).
// Unity menu: Tools > REFACTORY > Fix Pause Menu UI

using System;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class REFACTORYPauseUIFix
{
    [MenuItem("Tools/REFACTORY/Fix Pause Menu UI")]
    public static void FixPauseMenuUI()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("REFACTORY: Stop Play Mode before using the Pause Menu fix.");
            return;
        }

        // This searches inactive scene objects too, but ignores other open scenes.
        PauseMenu pauseMenu = null;
        Scene activeScene = SceneManager.GetActiveScene();
        foreach (var candidate in UnityEngine.Object.FindObjectsByType<PauseMenu>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (candidate.gameObject.scene == activeScene)
            {
                pauseMenu = candidate;
                break;
            }
        }

        if (pauseMenu == null)
        {
            Debug.LogError("REFACTORY: Could not find PauseMenu.cs in the active scene. Open LevelScene first.");
            return;
        }

        GameObject panel = pauseMenu.gameObject;
        if (!panel.activeSelf)
        {
            Undo.RecordObject(panel, "Activate PauseMenu");
            panel.SetActive(true);
        }

        Canvas parentCanvas = panel.transform.parent != null
            ? panel.transform.parent.GetComponentInParent<Canvas>()
            : null;
        if (parentCanvas == null)
        {
            Debug.LogError("REFACTORY: PauseMenu must be a child of a Canvas. Move it under your Canvas and run the fix again.");
            return;
        }

        // A separate, high-priority sub-canvas helps when other game HUDs cover the Pause UI.
        Canvas nestedCanvas = panel.GetComponent<Canvas>();
        if (nestedCanvas == null) nestedCanvas = Undo.AddComponent<Canvas>(panel);
        Undo.RecordObject(nestedCanvas, "Set Pause Menu canvas sorting");
        nestedCanvas.enabled = true;
        nestedCanvas.overrideSorting = true;
        nestedCanvas.sortingOrder = 5000;
        EditorUtility.SetDirty(nestedCanvas);

        GraphicRaycaster raycaster = panel.GetComponent<GraphicRaycaster>();
        if (raycaster == null) raycaster = Undo.AddComponent<GraphicRaycaster>(panel);
        Undo.RecordObject(raycaster, "Enable Pause Menu raycaster");
        raycaster.enabled = true;
        EditorUtility.SetDirty(raycaster);

        CanvasGroup group = panel.GetComponent<CanvasGroup>();
        if (group == null) group = Undo.AddComponent<CanvasGroup>(panel);
        Undo.RecordObject(group, "Configure Pause Menu canvas group");
        group.enabled = true;
        group.ignoreParentGroups = true;
        // The existing PauseMenu.cs controls alpha/interactable/blocksRaycasts at runtime.
        EditorUtility.SetDirty(group);

        // Ensure the original PauseMenu.cs references this group, preserving its methods.
        SerializedObject menuSerialized = new SerializedObject(pauseMenu);
        SerializedProperty canvasGroupProperty = menuSerialized.FindProperty("canvasGroup");
        if (canvasGroupProperty != null)
        {
            canvasGroupProperty.objectReferenceValue = group;
            menuSerialized.ApplyModifiedProperties();
        }
        else
        {
            Debug.LogWarning("REFACTORY: PauseMenu.cs does not expose a serialized 'canvasGroup' field. Assign it manually.");
        }

        // The big decorative background should not take mouse clicks meant for buttons.
        Image background = panel.GetComponent<Image>();
        if (background != null)
        {
            Undo.RecordObject(background, "Disable background UI raycast");
            background.raycastTarget = false;
            EditorUtility.SetDirty(background);
        }

        int continueCount = 0;
        int exitCount = 0;
        foreach (Button button in panel.GetComponentsInChildren<Button>(true))
        {
            Undo.RecordObject(button, "Configure Pause Menu button");
            button.enabled = true;
            button.interactable = true;
            button.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.95f, 0.45f, 1f);
            colors.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
            colors.colorMultiplier = 1f;
            button.colors = colors;

            Image buttonImage = button.GetComponent<Image>();
            if (buttonImage != null)
            {
                Undo.RecordObject(buttonImage, "Make Pause Menu button clickable");
                buttonImage.enabled = true;
                buttonImage.raycastTarget = true;
                button.targetGraphic = buttonImage;
                EditorUtility.SetDirty(buttonImage);
            }

            if (button.name.Equals("Continue", StringComparison.OrdinalIgnoreCase))
            {
                continueCount++;
                if (button.onClick.GetPersistentEventCount() == 0)
                    UnityEventTools.AddPersistentListener(button.onClick,
                        new UnityAction(pauseMenu.ResumeGame));
            }
            else if (button.name.Equals("Exit", StringComparison.OrdinalIgnoreCase))
            {
                exitCount++;
                if (button.onClick.GetPersistentEventCount() == 0)
                    UnityEventTools.AddPersistentListener(button.onClick,
                        new UnityAction(pauseMenu.QuitToMainMenu));
            }
            EditorUtility.SetDirty(button);
        }

        // Keep pause panel in front of other children of the same Canvas too.
        Undo.RecordObject(panel.transform, "Move PauseMenu to front");
        panel.transform.SetAsLastSibling();

        EventSystem eventSystem = UnityEngine.Object.FindFirstObjectByType<EventSystem>();
        if (eventSystem == null)
            Debug.LogWarning("REFACTORY: No active EventSystem. Create one via GameObject > UI > Event System and use Input System UI Input Module.");

        EditorSceneManager.MarkSceneDirty(activeScene);
        Debug.Log("REFACTORY: Pause UI updated. Nested Canvas sorting=5000, raycaster enabled, background click blocker disabled, buttons configured. " +
                  "Continue buttons: " + continueCount + ", Exit buttons: " + exitCount +
                  ". Save LevelScene (Ctrl+S), press Play, then ESC and test the buttons.");
    }
}
