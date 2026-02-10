using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// UI pour le menu principal
/// Boutons: Play, Paramètres, Tutorial, Quitter
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Panel du menu principal (MainMenuPanel)")]
    [SerializeField] private GameObject mainMenuPanel;

    [Header("Buttons")]
    [Tooltip("Bouton Play - ouvre le menu de sauvegarde")]
    [SerializeField] private Button playButton;

    [Tooltip("Bouton Paramètres")]
    [SerializeField] private Button settingsButton;

    [Tooltip("Bouton Quitter")]
    [SerializeField] private Button quitButton;

    [Tooltip("Bouton Tutorial")]
    [SerializeField] private Button tutorialButton;

    [Header("References")]
    [Tooltip("SettingsMenuUI pour ouvrir les paramètres")]
    [SerializeField] private SettingsMenuUI settingsMenu;

    [Tooltip("SaveMenuUI pour gérer les sauvegardes")]
    [SerializeField] private SaveMenuUI saveMenu;

    [Tooltip("TutorialUI pour ouvrir le tutorial")]
    [SerializeField] private TutorialUI tutorialUI;

    [Header("Scene Settings")]
    [Tooltip("Nom de la scène de jeu à charger")]
    [SerializeField] private string gameSceneName = "GameScene";

    private void Awake()
    {
        // Setup bouton Play
        if (playButton != null)
        {
            playButton.onClick.AddListener(OnPlayClicked);
        }

        if (settingsButton != null)
        {
            settingsButton.onClick.AddListener(OnSettingsClicked);
        }

        if (quitButton != null)
        {
            quitButton.onClick.AddListener(OnQuitClicked);
        }

        if (tutorialButton != null)
        {
            tutorialButton.onClick.AddListener(OnTutorialClicked);
        }

        // Trouver les références si pas assignées
        if (settingsMenu == null)
        {
            settingsMenu = FindFirstObjectByType<SettingsMenuUI>();
        }

        if (saveMenu == null)
        {
            saveMenu = FindFirstObjectByType<SaveMenuUI>();
        }

        if (tutorialUI == null)
        {
            tutorialUI = FindFirstObjectByType<TutorialUI>();
        }
    }

    private void Start()
    {
        // Afficher le menu principal
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(true);
        }

        // S'assurer que le temps n'est pas en pause
        Time.timeScale = 1f;

        Debug.Log("[MainMenuUI] Menu principal initialisé");
    }

    /// <summary>
    /// Ouvre le menu de sauvegarde
    /// </summary>
    private void OnPlayClicked()
    {
        Debug.Log("[MainMenuUI] Play cliqué...");

        if (saveMenu != null)
        {
            if (MainMenuUIManager.Instance != null)
            {
                MainMenuUIManager.Instance.ShowSaveMenu();
            }

            saveMenu.OpenPlayMenu(() =>
            {
                if (MainMenuUIManager.Instance != null)
                {
                    MainMenuUIManager.Instance.ShowMainMenu();
                }
            });
        }
        else
        {
            Debug.LogError("[MainMenuUI] SaveMenuUI manquante!");
            SceneManager.LoadScene(gameSceneName);
        }
    }

    /// <summary>
    /// Ouvre le menu paramètres
    /// </summary>
    private void OnSettingsClicked()
    {
        Debug.Log("[MainMenuUI] Ouverture paramètres...");

        if (settingsMenu != null)
        {
            if (MainMenuUIManager.Instance != null)
            {
                MainMenuUIManager.Instance.ShowSettings();
            }

            settingsMenu.OpenSettings(() =>
            {
                if (MainMenuUIManager.Instance != null)
                {
                    MainMenuUIManager.Instance.ShowMainMenu();
                }
            });
        }
        else
        {
            Debug.LogError("[MainMenuUI] SettingsMenuUI manquante!");
        }
    }

    /// <summary>
    /// Quitte le jeu
    /// </summary>
    private void OnQuitClicked()
    {
        Debug.Log("[MainMenuUI] Quitter le jeu...");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    /// <summary>
    /// Ouvre le tutorial
    /// </summary>
    private void OnTutorialClicked()
    {
        Debug.Log("[MainMenuUI] Ouverture du Tutorial...");

        if (tutorialUI == null)
        {
            Debug.LogError("[MainMenuUI] TutorialUI non assigné !");
            return;
        }

        if (MainMenuUIManager.Instance != null)
        {
            MainMenuUIManager.Instance.ShowTutorial();
        }

        tutorialUI.OpenTutorial(() =>
        {
            if (MainMenuUIManager.Instance != null)
            {
                MainMenuUIManager.Instance.ShowMainMenu();
            }
        });
    }

    /// <summary>
    /// Retour au menu principal
    /// </summary>
    public void ShowMainMenu()
    {
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(true);
        }
    }
}
