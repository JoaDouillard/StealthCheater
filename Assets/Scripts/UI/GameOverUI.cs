using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// UI pour l'écran Game Over (joueur attrapé par le prof)
/// Boutons: Restart (relance le jour actuel), Main Menu, Quit
/// </summary>
public class GameOverUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Panel Game Over (GameOverPanel)")]
    [SerializeField] private GameObject gameOverPanel;

    [Tooltip("Texte titre (ex: 'GAME OVER')")]
    [SerializeField] private TextMeshProUGUI titleText;

    [Tooltip("Texte message (ex: 'You got caught!')")]
    [SerializeField] private TextMeshProUGUI messageText;

    [Header("Buttons")]
    [Tooltip("Bouton Restart (relance le jour actuel)")]
    [SerializeField] private Button restartButton;

    [Tooltip("Bouton Main Menu")]
    [SerializeField] private Button mainMenuButton;

    [Tooltip("Bouton Quit")]
    [SerializeField] private Button quitButton;

    [Header("Scene Settings")]
    [Tooltip("Nom de la scène du menu principal")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private void Awake()
    {
        // Setup boutons
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(OnRestartClicked);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);
        }

        if (quitButton != null)
        {
            quitButton.onClick.AddListener(OnQuitClicked);
        }

        // Panel caché par défaut (sera affiché dans Start)
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
    }

    private void Start()
    {
        // Afficher automatiquement le Game Over quand la scène charge
        ShowGameOver("You got caught by the teacher!");
    }

    /// <summary>
    /// Affiche l'écran Game Over
    /// Appelé automatiquement quand la scène GameOver est chargée
    /// </summary>
    /// <param name="message">Message personnalisé (optionnel)</param>
    public void ShowGameOver(string message = "You got caught by the teacher!")
    {
        // Mettre à jour le titre
        if (titleText != null)
        {
            titleText.text = "GAME OVER";
        }

        // Mettre à jour le message
        if (messageText != null)
        {
            messageText.text = message;
        }

        // Afficher le panel (scène dédiée, pas besoin d'UIManager)
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        // Afficher curseur
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // Pas besoin de Time.timeScale = 0 car on est dans une scène séparée
    }

    /// <summary>
    /// Cache l'écran Game Over
    /// </summary>
    public void HideGameOver()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        // Reprendre le jeu
        Time.timeScale = 1f;
    }

    #region Button Callbacks

    /// <summary>
    /// Relance le jour actuel (conserve les notes des jours précédents)
    /// </summary>
    private void OnRestartClicked()
    {
        Debug.Log("[GameOverUI] Restart du jour actuel...");

        // Reprendre le temps
        Time.timeScale = 1f;

        // WeekManager est DontDestroyOnLoad et sera toujours là
        // Simplement recharger GameScene, WeekManager relancera le jour actuel
        SceneManager.LoadScene("GameScene");
    }

    /// <summary>
    /// Retour au menu principal (sauvegarde l'état actuel avant)
    /// </summary>
    private void OnMainMenuClicked()
    {
        Debug.Log("[GameOverUI] Retour au menu principal...");

        // Sauvegarder avant de quitter
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.AutoSave();
        }

        // Reprendre le temps
        Time.timeScale = 1f;

        // Charger le menu principal
        SceneManager.LoadScene(mainMenuSceneName);
    }

    /// <summary>
    /// Quitter le jeu (sauvegarde l'état actuel avant)
    /// </summary>
    private void OnQuitClicked()
    {
        Debug.Log("[GameOverUI] Quitter le jeu...");

        // Sauvegarder avant de quitter
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.AutoSave();
        }

        // Reprendre le temps
        Time.timeScale = 1f;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    #endregion
}
