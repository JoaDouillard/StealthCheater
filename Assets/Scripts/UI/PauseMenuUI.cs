using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// UI pour le menu pause (accessible pendant le jeu avec Escape)
/// Boutons: Continuer, Recommencer, Paramètres, Menu Principal, Quitter
/// </summary>
public class PauseMenuUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Panel du menu pause (PauseMenuPanel)")]
    [SerializeField] private GameObject pauseMenuPanel;

    [Header("Buttons")]
    [Tooltip("Bouton Continuer (reprendre le jeu)")]
    [SerializeField] private Button resumeButton;

    [Tooltip("Bouton Recommencer (relancer le niveau)")]
    [SerializeField] private Button restartButton;

    [Tooltip("Bouton Paramètres")]
    [SerializeField] private Button settingsButton;

    [Tooltip("Bouton Menu Principal")]
    [SerializeField] private Button mainMenuButton;

    [Tooltip("Bouton Quitter")]
    [SerializeField] private Button quitButton;

    [Header("References")]
    [Tooltip("SettingsMenuUI pour ouvrir les paramètres")]
    [SerializeField] private SettingsMenuUI settingsMenu;

    [Header("Scene Settings")]
    [Tooltip("Nom de la scène du menu principal")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private bool isPaused = false;
    private InputSystem_Actions inputActions;

    private void Awake()
    {
        // Setup boutons
        if (resumeButton != null)
        {
            resumeButton.onClick.AddListener(OnResumeClicked);
        }

        if (restartButton != null)
        {
            restartButton.onClick.AddListener(OnRestartClicked);
        }

        if (settingsButton != null)
        {
            settingsButton.onClick.AddListener(OnSettingsClicked);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);
        }

        if (quitButton != null)
        {
            quitButton.onClick.AddListener(OnQuitClicked);
        }

        // Trouver SettingsMenuUI si pas assigné
        if (settingsMenu == null)
        {
            settingsMenu = FindFirstObjectByType<SettingsMenuUI>();
        }

        // Input system
        inputActions = new InputSystem_Actions();

        // NE PAS désactiver le panel ici !
        // UIManager gère l'état actif/inactif des panels
    }

    private void OnEnable()
    {
        if (inputActions != null)
        {
            inputActions.Player.Enable();
        }
    }

    private void OnDisable()
    {
        if (inputActions != null)
        {
            inputActions.Player.Disable();
        }
    }

    private void Update()
    {
        // Toggle pause avec Escape (InputSystem Keyboard)
        // Seulement si pas d'autre menu ouvert
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            // Si on est dans le menu pause, fermer
            if (isPaused)
            {
                TogglePause();
            }
            // Si on est en gameplay, ouvrir le pause
            else if (UIManager.Instance != null && UIManager.Instance.IsGameplayActive())
            {
                TogglePause();
            }
        }
    }

    /// <summary>
    /// Toggle le menu pause
    /// </summary>
    public void TogglePause()
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

    /// <summary>
    /// Met le jeu en pause
    /// </summary>
    public void PauseGame()
    {
        isPaused = true;

        // UIManager gère TOUT l'affichage des panels
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowPauseMenu();
        }
        else
        {
            Debug.LogError("[PauseMenuUI] UIManager introuvable ! Le menu pause ne peut pas s'afficher.");
        }
    }

    /// <summary>
    /// Reprend le jeu
    /// </summary>
    public void ResumeGame()
    {
        isPaused = false;

        // UIManager gère TOUT l'affichage des panels
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowHUD();
        }
        else
        {
            Debug.LogError("[PauseMenuUI] UIManager introuvable ! Impossible de reprendre correctement.");
        }

        Debug.Log("[PauseMenuUI] Jeu repris");
    }

    #region Button Callbacks

    private void OnResumeClicked()
    {
        ResumeGame();
    }

    private void OnRestartClicked()
    {
        Debug.Log("[PauseMenuUI] Recommencer le niveau...");

        // Reprendre le temps
        Time.timeScale = 1f;

        // Recommencer la semaine via WeekManager
        if (WeekManager.Instance != null)
        {
            WeekManager.Instance.RestartWeek();
        }
        else
        {
            // Fallback: recharger la scène actuelle
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    private void OnSettingsClicked()
    {
        Debug.Log("[PauseMenuUI] Ouverture paramètres...");

        if (settingsMenu != null)
        {
            // Utiliser UIManager pour afficher settings
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowSettings();
            }

            // Ouvrir les paramètres avec callback pour revenir au menu pause
            settingsMenu.OpenSettings(() => {
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ShowPauseMenu();
                }
                else
                {
                    ShowPauseMenu();
                }
            });
        }
        else
        {
            Debug.LogError("[PauseMenuUI] SettingsMenuUI manquante!");
        }
    }

    private void OnMainMenuClicked()
    {
        Debug.Log("[PauseMenuUI] Retour au menu principal...");

        // Reprendre le temps
        Time.timeScale = 1f;

        // Charger le menu principal
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void OnQuitClicked()
    {
        Debug.Log("[PauseMenuUI] Quitter le jeu...");

        // Reprendre le temps
        Time.timeScale = 1f;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    #endregion

    /// <summary>
    /// Retour au menu pause (appelé depuis SettingsMenu)
    /// </summary>
    public void ShowPauseMenu()
    {
        // UIManager gère TOUT l'affichage des panels
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowPauseMenu();
        }
        else
        {
            Debug.LogError("[PauseMenuUI] UIManager introuvable !");
        }
    }

    /// <summary>
    /// Vérifie si le jeu est en pause
    /// </summary>
    public bool IsPaused() => isPaused;
}
