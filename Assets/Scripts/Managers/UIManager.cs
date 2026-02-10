using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Gère l'affichage des panels UI. Un seul panel actif à la fois.
/// Les scripts UI appellent UIManager pour afficher leur panel.
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Panels")]
    [SerializeField] private GameObject hudPanel;
    [SerializeField] private GameObject examStartPanel;
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private GameObject cheatSheetPanel;
    [SerializeField] private GameObject dayResultsPanel;
    [SerializeField] private GameObject weekResultsPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject tutorialPanel;

    private GameObject currentPanel;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    private void Start()
    {
        // Initialiser en affichant le HUD au démarrage
        // Cela définit correctement currentPanel et le curseur
        ShowHUD();
    }

    private void Update()
    {
        // Escape = toggle pause (sauf si ExamStart actif)
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (currentPanel == examStartPanel) return; // Pas d'interruption pendant ExamStart

            if (currentPanel == pauseMenuPanel || currentPanel == settingsPanel || currentPanel == cheatSheetPanel)
                ShowHUD();
            else if (currentPanel == hudPanel || currentPanel == null)
                ShowPauseMenu();
        }
    }

    private void Show(GameObject panel, bool showCursor, bool pause)
    {
        bool isOpeningPanel = (panel != hudPanel && panel != null);
        bool isClosingPanel = (currentPanel != hudPanel && currentPanel != null && panel == hudPanel);

        // Cacher l'ancien panel
        if (currentPanel != null) currentPanel.SetActive(false);

        // Afficher le nouveau
        if (panel != null) panel.SetActive(true);
        currentPanel = panel;

        // Curseur et pause
        Cursor.visible = showCursor;
        Cursor.lockState = showCursor ? CursorLockMode.None : CursorLockMode.Locked;
        Time.timeScale = pause ? 0f : 1f;

        // Sons de panel
        if (AudioManager.Instance != null)
        {
            if (isOpeningPanel)
                AudioManager.Instance.PlayPanelOpen();
            else if (isClosingPanel)
                AudioManager.Instance.PlayPanelClose();
        }
    }

    public void ShowHUD() => Show(hudPanel, false, false);
    public void ShowExamStart() => Show(examStartPanel, false, true);
    public void HideExamStart() => Show(hudPanel, false, false);
    public void ShowPauseMenu() => Show(pauseMenuPanel, true, true);
    public void ShowCheatSheet() => Show(cheatSheetPanel, true, true);
    public void ShowSettings() => Show(settingsPanel, true, true);
    public void ShowDayResults() => Show(dayResultsPanel, true, true);
    public void ShowWeekResults() => Show(weekResultsPanel, true, true);
    public void ShowTutorial() => Show(tutorialPanel, true, true);

    public bool IsGameplayActive() => currentPanel == hudPanel || currentPanel == null;
    public bool IsMenuOpen() => currentPanel == pauseMenuPanel || currentPanel == settingsPanel || currentPanel == cheatSheetPanel;
}
