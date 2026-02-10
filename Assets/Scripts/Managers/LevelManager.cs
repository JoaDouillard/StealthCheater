using UnityEngine;

/// <summary>
/// Gère la sélection et la configuration du niveau actuel
/// Singleton accessible globalement pour que le Teacher puisse charger sa config
///
/// Setup:
/// 1. Créer GameObject "LevelManager" dans la scène
/// 2. Ajouter ce script
/// 3. Assigner les 4 LevelConfiguration assets dans l'Inspector
/// 4. Configurer le LevelSelectionMode (Random ou Manual)
/// </summary>
public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("Level Configurations")]
    [Tooltip("Configurations des niveaux (1 à 4 classrooms). Ajustez la taille selon vos besoins.")]
    [SerializeField] private LevelConfiguration[] levelConfigurations;

    [Header("Level Selection")]
    [Tooltip("Mode de sélection du niveau")]
    [SerializeField] private LevelSelectionMode selectionMode = LevelSelectionMode.Random;

    [Tooltip("Index du niveau à charger (si Manual mode)")]
    [SerializeField] private int currentLevelIndex = 0;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    // [Header("Info")] // UNUSED - Champ informatif commenté pour éviter warning CS0414
    // [Tooltip("Le LevelSpawner gère maintenant l'activation/désactivation des classrooms")]
    // [SerializeField] private string info = "Utilisez LevelSpawner pour assigner les Level Props GameObjects";

    // Configuration actuelle
    private LevelConfiguration currentConfiguration;
    private int selectedLevelIndex = 0;

    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Validation
        if (!ValidateConfigurations())
        {
            Debug.LogError("[LevelManager] Configurations invalides! Vérifiez l'Inspector.");
            return;
        }

        // Sélectionner et charger le niveau
        SelectLevel();
        LoadLevel();
    }

    /// <summary>
    /// Sélectionne le niveau selon le mode configuré
    /// </summary>
    private void SelectLevel()
    {
        switch (selectionMode)
        {
            case LevelSelectionMode.Random:
                selectedLevelIndex = Random.Range(0, levelConfigurations.Length);
                LogDebug($"Niveau sélectionné ALÉATOIREMENT: {selectedLevelIndex}");
                break;

            case LevelSelectionMode.Manual:
                selectedLevelIndex = Mathf.Clamp(currentLevelIndex, 0, levelConfigurations.Length - 1);
                LogDebug($"Niveau sélectionné MANUELLEMENT: {selectedLevelIndex}");
                break;

            case LevelSelectionMode.Sequential:
                // Pour séquentiel, on peut utiliser PlayerPrefs pour garder le dernier niveau
                selectedLevelIndex = PlayerPrefs.GetInt("LastLevelIndex", 0);
                selectedLevelIndex = (selectedLevelIndex + 1) % levelConfigurations.Length;
                PlayerPrefs.SetInt("LastLevelIndex", selectedLevelIndex);
                LogDebug($"Niveau sélectionné SÉQUENTIELLEMENT: {selectedLevelIndex}");
                break;
        }

        currentConfiguration = levelConfigurations[selectedLevelIndex];
    }

    /// <summary>
    /// Charge le niveau sélectionné (active la classroom correspondante via LevelSpawner)
    /// </summary>
    private void LoadLevel()
    {
        if (currentConfiguration == null)
        {
            Debug.LogError("[LevelManager] Aucune configuration sélectionnée!");
            return;
        }

        LogDebug("=== CHARGEMENT DU NIVEAU ===");
        LogDebug($"Niveau: {currentConfiguration.levelName}");
        LogDebug($"Index: {selectedLevelIndex}");
        LogDebug($"Description: {currentConfiguration.description}");

        // Normaliser les probabilités si nécessaire
        if (!currentConfiguration.ValidateProbabilities())
        {
            currentConfiguration.NormalizeProbabilities();
        }

        // Demander au LevelSpawner d'activer les props du niveau
        if (LevelSpawner.Instance != null)
        {
            LevelSpawner.Instance.ActivateLevelProps(selectedLevelIndex);
            LogDebug("✅ Level Props activés via LevelSpawner");
        }
        else
        {
            Debug.LogWarning("[LevelManager] LevelSpawner.Instance est NULL! Les classrooms ne seront pas activées/désactivées.");
            Debug.LogWarning("[LevelManager] Créez un GameObject 'LevelSpawner' avec le script LevelSpawner.cs");
        }

        LogDebug("============================");
        Debug.Log($"[LevelManager] ✅ Niveau '{currentConfiguration.levelName}' chargé avec succès!");
    }

    /// <summary>
    /// Retourne la configuration du niveau actuel
    /// </summary>
    public LevelConfiguration GetCurrentConfiguration()
    {
        return currentConfiguration;
    }

    /// <summary>
    /// Alias de GetCurrentConfiguration (pour compatibilité PropsSpawner)
    /// </summary>
    public LevelConfiguration GetCurrentLevelConfig()
    {
        return currentConfiguration;
    }

    /// <summary>
    /// Retourne l'index du niveau actuel
    /// </summary>
    public int GetCurrentLevelIndex()
    {
        return selectedLevelIndex;
    }

    /// <summary>
    /// Change manuellement de niveau (utile pour testing)
    /// </summary>
    public void ChangeLevel(int levelIndex)
    {
        if (levelIndex < 0 || levelIndex >= levelConfigurations.Length)
        {
            Debug.LogError($"[LevelManager] Index de niveau invalide: {levelIndex}");
            return;
        }

        selectedLevelIndex = levelIndex;
        currentConfiguration = levelConfigurations[selectedLevelIndex];
        LoadLevel();

        Debug.Log($"[LevelManager] Changement vers niveau {levelIndex}: {currentConfiguration.levelName}");
    }

    /// <summary>
    /// Valide que les configurations sont correctement assignées
    /// </summary>
    private bool ValidateConfigurations()
    {
        if (levelConfigurations == null || levelConfigurations.Length == 0)
        {
            Debug.LogError("[LevelManager] Aucune LevelConfiguration assignée! Ajoutez au moins 1 config.");
            return false;
        }

        // Compter les configs valides (non-NULL)
        int validConfigs = 0;
        for (int i = 0; i < levelConfigurations.Length; i++)
        {
            if (levelConfigurations[i] != null)
            {
                validConfigs++;
            }
        }

        if (validConfigs == 0)
        {
            Debug.LogError("[LevelManager] Toutes les configurations sont NULL! Assignez au moins 1 config valide.");
            return false;
        }

        LogDebug($"✅ {validConfigs}/{levelConfigurations.Length} configurations valides détectées");
        return true;
    }

    private void LogDebug(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[LevelManager] {message}");
        }
    }

    // Méthodes utilitaires pour l'Inspector/Debug
    private void OnValidate()
    {
        // Ajuster currentLevelIndex si hors limites
        if (levelConfigurations != null && levelConfigurations.Length > 0)
        {
            if (currentLevelIndex >= levelConfigurations.Length)
            {
                currentLevelIndex = levelConfigurations.Length - 1;
            }
        }
    }

#if UNITY_EDITOR
    [ContextMenu("🔄 Reload Current Level")]
    private void ReloadCurrentLevel()
    {
        if (Application.isPlaying && currentConfiguration != null)
        {
            LoadLevel();
        }
    }

    [ContextMenu("🎲 Change to Random Level")]
    private void ChangeToRandomLevel()
    {
        if (Application.isPlaying)
        {
            ChangeLevel(Random.Range(0, 4));
        }
    }

    [ContextMenu("📊 Show Current Configuration")]
    private void ShowCurrentConfiguration()
    {
        if (currentConfiguration != null)
        {
            Debug.Log($"=== CURRENT LEVEL CONFIGURATION ===\n" +
                     $"Name: {currentConfiguration.levelName}\n" +
                     $"Index: {selectedLevelIndex}\n" +
                     $"Patrol Zone Size: {currentConfiguration.patrolZoneSize}\n" +
                     $"Probabilities: NavMesh={currentConfiguration.randomNavMeshProbability}%, " +
                     $"Board={currentConfiguration.boardProbability}%, Window={currentConfiguration.windowProbability}%\n" +
                     $"Walk Speed: {currentConfiguration.walkSpeed}\n" +
                     $"Detection Zones: Zone1={currentConfiguration.zone1MaxDistance}m/{currentConfiguration.zone1DetectionTime}s, " +
                     $"Zone2={currentConfiguration.zone2MaxDistance}m/{currentConfiguration.zone2DetectionTime}s, " +
                     $"Zone3={currentConfiguration.zone3MaxDistance}m/{currentConfiguration.zone3DetectionTime}s");
        }
    }
#endif
}

/// <summary>
/// Mode de sélection du niveau au démarrage
/// </summary>
public enum LevelSelectionMode
{
    Random,      // Choix aléatoire parmi les 4 niveaux
    Manual,      // Choix manuel via currentLevelIndex
    Sequential   // 0 → 1 → 2 → 3 → 0...
}
