using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Gère les paramètres du jeu (sensibilité souris, volume, etc.)
/// Singleton avec PlayerPrefs pour persister les settings
/// </summary>
public class GameSettings : MonoBehaviour
{
    public static GameSettings Instance { get; private set; }

    [Header("Audio")]
    [Tooltip("AudioMixer pour contrôler le volume")]
    [SerializeField] private AudioMixer audioMixer;

    [Header("Default Values")]
    [Tooltip("Sensibilité souris par défaut (1-10)")]
    [Range(1f, 10f)]
    [SerializeField] private float defaultMouseSensitivity = 5f;

    [Tooltip("Volume effets par défaut (0-1)")]
    [Range(0f, 1f)]
    [SerializeField] private float defaultSFXVolume = 0.8f;

    [Tooltip("Volume musique par défaut (0-1)")]
    [Range(0f, 1f)]
    [SerializeField] private float defaultMusicVolume = 0.6f;

    // PlayerPrefs keys
    private const string MOUSE_SENSITIVITY_KEY = "MouseSensitivity";
    private const string SFX_VOLUME_KEY = "SFXVolume";
    private const string MUSIC_VOLUME_KEY = "MusicVolume";

    // Current settings
    private float mouseSensitivity;
    private float sfxVolume;
    private float musicVolume;

    private void Awake()
    {
        // Singleton pattern avec DontDestroyOnLoad
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Charger les settings
        LoadSettings();
    }

    private void Start()
    {
        // Réappliquer les settings après que tous les Awake() soient terminés
        // Cela garantit que AudioManager est bien initialisé
        ApplySettings();
    }

    /// <summary>
    /// Charge les paramètres depuis PlayerPrefs
    /// </summary>
    private void LoadSettings()
    {
        // Sensibilité souris (1-10)
        mouseSensitivity = PlayerPrefs.GetFloat(MOUSE_SENSITIVITY_KEY, defaultMouseSensitivity);

        // Volumes (0-1)
        sfxVolume = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, defaultSFXVolume);
        musicVolume = PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, defaultMusicVolume);

        // Appliquer les settings
        ApplySettings();
    }

    /// <summary>
    /// Sauvegarde les paramètres dans PlayerPrefs
    /// </summary>
    public void SaveSettings()
    {
        PlayerPrefs.SetFloat(MOUSE_SENSITIVITY_KEY, mouseSensitivity);
        PlayerPrefs.SetFloat(SFX_VOLUME_KEY, sfxVolume);
        PlayerPrefs.SetFloat(MUSIC_VOLUME_KEY, musicVolume);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Applique les paramètres actuels
    /// </summary>
    private void ApplySettings()
    {
        // Appliquer via AudioManager (qui gère l'AudioMixer)
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetSFXVolume(sfxVolume);
            AudioManager.Instance.SetMusicVolume(musicVolume);
        }
    }

    #region Getters/Setters

    /// <summary>
    /// Définit la sensibilité de la souris (1-10)
    /// </summary>
    public void SetMouseSensitivity(float value)
    {
        mouseSensitivity = Mathf.Clamp(value, 1f, 10f);
        ApplySettings();
    }

    /// <summary>
    /// Obtient la sensibilité de la souris (1-10)
    /// </summary>
    public float GetMouseSensitivity() => mouseSensitivity;

    /// <summary>
    /// Obtient le multiplicateur de sensibilité (0.1 à 1.0)
    /// Pour utiliser dans CameraFollow: mouseSensitivity * GetMouseSensitivityMultiplier()
    /// </summary>
    public float GetMouseSensitivityMultiplier()
    {
        // Convertir 1-10 en 0.1-1.0
        return mouseSensitivity / 10f;
    }

    /// <summary>
    /// Définit le volume des effets sonores (0-1)
    /// </summary>
    public void SetSFXVolume(float value)
    {
        sfxVolume = Mathf.Clamp01(value);
        ApplySettings();
    }

    /// <summary>
    /// Obtient le volume des effets sonores (0-1)
    /// </summary>
    public float GetSFXVolume() => sfxVolume;

    /// <summary>
    /// Définit le volume de la musique (0-1)
    /// </summary>
    public void SetMusicVolume(float value)
    {
        musicVolume = Mathf.Clamp01(value);
        ApplySettings();
    }

    /// <summary>
    /// Obtient le volume de la musique (0-1)
    /// </summary>
    public float GetMusicVolume() => musicVolume;

    #endregion

    /// <summary>
    /// Reset tous les paramètres aux valeurs par défaut
    /// </summary>
    public void ResetToDefaults()
    {
        mouseSensitivity = defaultMouseSensitivity;
        sfxVolume = defaultSFXVolume;
        musicVolume = defaultMusicVolume;

        ApplySettings();
        SaveSettings();
    }
}
