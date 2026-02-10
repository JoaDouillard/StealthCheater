using UnityEngine;

/// <summary>
/// Gère l'affichage des panels dans la scène MainMenu
/// Assure qu'un seul panel est visible à la fois
/// Similaire à UIManager mais pour le MainMenu
/// </summary>
public class MainMenuUIManager : MonoBehaviour
{
    public static MainMenuUIManager Instance { get; private set; }

    [Header("UI Panels")]
    [Tooltip("Panel du menu principal (boutons New Game, Continue, Settings, Quit)")]
    [SerializeField] private GameObject mainMenuPanel;

    [Tooltip("Panel de sélection des saves (3 slots + popups)")]
    [SerializeField] private GameObject saveMenuPanel;

    [Tooltip("Panel des paramètres")]
    [SerializeField] private GameObject settingsPanel;

    [Tooltip("Panel Tutorial")]
    [SerializeField] private GameObject tutorialPanel;

    private void Awake()
    {
        // Singleton pattern (pas DontDestroyOnLoad car spécifique à MainMenu)
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Au démarrage, afficher uniquement le MainMenuPanel
        ShowMainMenu();
    }

    /// <summary>
    /// Affiche le menu principal
    /// </summary>
    public void ShowMainMenu()
    {
        ShowPanel(mainMenuPanel);
        SetCursorState(true); // Curseur visible dans les menus
    }

    /// <summary>
    /// Affiche le menu de sélection des saves
    /// </summary>
    public void ShowSaveMenu()
    {
        ShowPanel(saveMenuPanel);
        SetCursorState(true);
    }

    /// <summary>
    /// Affiche les paramètres
    /// </summary>
    public void ShowSettings()
    {
        ShowPanel(settingsPanel);
        SetCursorState(true);
    }

    /// <summary>
    /// Affiche le tutorial
    /// </summary>
    public void ShowTutorial()
    {
        ShowPanel(tutorialPanel);
        SetCursorState(true);
    }

    /// <summary>
    /// Affiche un panel spécifique et masque tous les autres
    /// </summary>
    private void ShowPanel(GameObject panelToShow)
    {
        // Masquer tous les panels
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (saveMenuPanel != null) saveMenuPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (tutorialPanel != null) tutorialPanel.SetActive(false);

        // Afficher le panel demandé
        if (panelToShow != null)
        {
            panelToShow.SetActive(true);
            Debug.Log($"[MainMenuUIManager] Panel affiché: {panelToShow.name}");
        }
    }

    /// <summary>
    /// Gère l'état du curseur
    /// </summary>
    private void SetCursorState(bool visible)
    {
        Cursor.visible = visible;
        Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
    }
}
