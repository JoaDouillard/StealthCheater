using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI pour le menu paramètres (sensibilité souris, volumes)
/// Peut être utilisé depuis le menu principal ou le menu pause
/// </summary>
public class SettingsMenuUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Panel du menu paramètres (SettingsPanel)")]
    [SerializeField] private GameObject settingsPanel;

    [Tooltip("Slider pour la sensibilité souris (1-10)")]
    [SerializeField] private Slider mouseSensitivitySlider;

    [Tooltip("Texte affichant la valeur de sensibilité")]
    [SerializeField] private TextMeshProUGUI mouseSensitivityValueText;

    [Tooltip("Slider pour le volume SFX (0-1)")]
    [SerializeField] private Slider sfxVolumeSlider;

    [Tooltip("Texte affichant le volume SFX")]
    [SerializeField] private TextMeshProUGUI sfxVolumeValueText;

    [Tooltip("Slider pour le volume musique (0-1)")]
    [SerializeField] private Slider musicVolumeSlider;

    [Tooltip("Texte affichant le volume musique")]
    [SerializeField] private TextMeshProUGUI musicVolumeValueText;

    [Header("Buttons")]
    [Tooltip("Bouton pour retourner au menu précédent")]
    [SerializeField] private Button backButton;

    [Tooltip("Bouton pour réinitialiser aux valeurs par défaut")]
    [SerializeField] private Button resetButton;

    // Bouton Apply supprimé - les paramètres s'appliquent automatiquement
    // et sont sauvegardés quand on quitte le menu

    [Tooltip("Bouton pour ouvrir le Tutorial")]
    [SerializeField] private Button tutorialButton;

    [Header("References")]
    [Tooltip("Script TutorialUI")]
    [SerializeField] private TutorialUI tutorialUI;

    // Callback pour revenir au menu précédent
    private System.Action onBackCallback;

    private void Awake()
    {
        // Setup boutons
        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackClicked);
        }

        if (resetButton != null)
        {
            resetButton.onClick.AddListener(OnResetClicked);
        }

        // Bouton Apply supprimé - les paramètres s'appliquent automatiquement

        if (tutorialButton != null)
        {
            tutorialButton.onClick.AddListener(OnTutorialClicked);
        }

        // Trouver TutorialUI si pas assigné
        if (tutorialUI == null)
        {
            tutorialUI = FindFirstObjectByType<TutorialUI>();
        }

        // Setup sliders listeners
        if (mouseSensitivitySlider != null)
        {
            mouseSensitivitySlider.onValueChanged.AddListener(OnMouseSensitivityChanged);
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        }

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        }

        // NE PAS désactiver le panel ici !
        // UIManager gère l'état actif/inactif des panels
    }

    /// <summary>
    /// Ouvre le menu paramètres
    /// </summary>
    /// <param name="onBack">Callback appelé quand on clique sur Retour (optionnel)</param>
    public void OpenSettings(System.Action onBack = null)
    {
        // UIManager gère l'activation du panel, on ne fait que charger les settings
        // Stocker le callback
        onBackCallback = onBack;

        // Charger les valeurs actuelles
        LoadCurrentSettings();
    }

    /// <summary>
    /// Ferme le menu paramètres
    /// </summary>
    public void CloseSettings()
    {
        // UIManager gère la désactivation du panel via le callback
    }

    /// <summary>
    /// Charge les paramètres actuels depuis GameSettings
    /// </summary>
    private void LoadCurrentSettings()
    {
        if (GameSettings.Instance == null)
        {
            Debug.LogWarning("[SettingsMenuUI] GameSettings instance manquante!");
            return;
        }

        // Sensibilité souris (1-10)
        if (mouseSensitivitySlider != null)
        {
            mouseSensitivitySlider.minValue = 1f;
            mouseSensitivitySlider.maxValue = 10f;
            mouseSensitivitySlider.wholeNumbers = true;
            mouseSensitivitySlider.value = GameSettings.Instance.GetMouseSensitivity();
        }

        // Volume SFX (0-1 mais affiché en %)
        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.minValue = 0f;
            sfxVolumeSlider.maxValue = 1f;
            sfxVolumeSlider.value = GameSettings.Instance.GetSFXVolume();
        }

        // Volume musique (0-1 mais affiché en %)
        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.minValue = 0f;
            musicVolumeSlider.maxValue = 1f;
            musicVolumeSlider.value = GameSettings.Instance.GetMusicVolume();
        }

        // Mettre à jour les textes
        UpdateValueTexts();
    }

    /// <summary>
    /// Met à jour les textes affichant les valeurs
    /// </summary>
    private void UpdateValueTexts()
    {
        if (mouseSensitivityValueText != null && mouseSensitivitySlider != null)
        {
            mouseSensitivityValueText.text = mouseSensitivitySlider.value.ToString("F0");
        }

        if (sfxVolumeValueText != null && sfxVolumeSlider != null)
        {
            sfxVolumeValueText.text = $"{(sfxVolumeSlider.value * 100f):F0}%";
        }

        if (musicVolumeValueText != null && musicVolumeSlider != null)
        {
            musicVolumeValueText.text = $"{(musicVolumeSlider.value * 100f):F0}%";
        }
    }

    #region Callbacks

    private void OnMouseSensitivityChanged(float value)
    {
        if (mouseSensitivityValueText != null)
        {
            mouseSensitivityValueText.text = value.ToString("F0");
        }

        // Appliquer immédiatement
        if (GameSettings.Instance != null)
        {
            GameSettings.Instance.SetMouseSensitivity(value);
        }
    }

    private void OnSFXVolumeChanged(float value)
    {
        if (sfxVolumeValueText != null)
        {
            sfxVolumeValueText.text = $"{(value * 100f):F0}%";
        }

        // Appliquer immédiatement
        if (GameSettings.Instance != null)
        {
            GameSettings.Instance.SetSFXVolume(value);
        }
    }

    private void OnMusicVolumeChanged(float value)
    {
        if (musicVolumeValueText != null)
        {
            musicVolumeValueText.text = $"{(value * 100f):F0}%";
        }

        // Appliquer immédiatement
        if (GameSettings.Instance != null)
        {
            GameSettings.Instance.SetMusicVolume(value);
        }
    }

    private void OnBackClicked()
    {
        // Sauvegarder avant de fermer
        if (GameSettings.Instance != null)
        {
            GameSettings.Instance.SaveSettings();
        }

        CloseSettings();

        // Appeler le callback pour revenir au menu précédent
        onBackCallback?.Invoke();
    }

    private void OnResetClicked()
    {
        if (GameSettings.Instance != null)
        {
            GameSettings.Instance.ResetToDefaults();
            LoadCurrentSettings(); // Recharger les valeurs par défaut dans l'UI
        }
    }

    // Bouton Apply supprimé - sauvegarde automatique quand on quitte

    private void OnTutorialClicked()
    {
        Debug.Log("[SettingsMenuUI] Ouverture du Tutorial...");

        if (tutorialUI == null)
        {
            Debug.LogError("[SettingsMenuUI] TutorialUI non assigné !");
            return;
        }

        // Ouvrir le tutorial avec callback pour revenir aux settings
        tutorialUI.OpenTutorial(() =>
        {
            // Retour aux settings après fermeture du tutorial
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowSettings();
            }
            else if (MainMenuUIManager.Instance != null)
            {
                MainMenuUIManager.Instance.ShowSettings();
            }
        });
    }

    #endregion
}
