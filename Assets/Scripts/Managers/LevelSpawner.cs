using UnityEngine;

/// <summary>
/// Gère le spawn et despawn des objets dynamiques pour chaque niveau
/// Travaille en tandem avec LevelManager
///
/// Architecture:
/// - Environment_Static (toujours visible - architecture de base)
/// - Level_Props (activé/désactivé selon niveau)
///   - FixedPoints (Board, Windows, PatrolZone - ne changent pas)
///   - SpawnableProps (Desks, Students, Obstacles - à spawner dynamiquement si nécessaire)
///
/// Setup:
/// 1. Créer GameObject "LevelSpawner" dans la scène
/// 2. Ajouter ce script
/// 3. Assigner les Level Props GameObjects pour chaque niveau
/// </summary>
public class LevelSpawner : MonoBehaviour
{
    public static LevelSpawner Instance { get; private set; }

    [Header("Level Props (Objets dynamiques par niveau)")]
    [Tooltip("GameObjects contenant les props (objets fixes + spawnables) pour chaque niveau. Ajustez la taille selon vos besoins (1-4).")]
    [SerializeField] private GameObject[] levelPropsObjects;

    [Header("Spawning Settings")]
    [Tooltip("Les objets sont-ils déjà placés dans la scène ou doivent-ils être spawnés au runtime?")]
    [SerializeField] private bool usePreplacedObjects = true;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    private int currentActiveLevelIndex = -1;

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
        if (!ValidateSetup())
        {
            Debug.LogError("[LevelSpawner] Setup invalide! Vérifiez l'Inspector.");
            return;
        }

        // Désactiver tous les level props au démarrage
        DeactivateAllLevels();
    }

    /// <summary>
    /// Active les props d'un niveau spécifique
    /// Appelé par LevelManager après avoir choisi le niveau
    /// </summary>
    public void ActivateLevelProps(int levelIndex)
    {
        if (levelIndex < 0 || levelIndex >= levelPropsObjects.Length)
        {
            Debug.LogError($"[LevelSpawner] Index de niveau invalide: {levelIndex}");
            return;
        }

        LogDebug($"=== ACTIVATION LEVEL {levelIndex} PROPS ===");

        // Désactiver le niveau précédent si nécessaire
        if (currentActiveLevelIndex != -1 && currentActiveLevelIndex != levelIndex)
        {
            DeactivateLevel(currentActiveLevelIndex);
        }

        // Activer le nouveau niveau
        if (levelPropsObjects[levelIndex] != null)
        {
            levelPropsObjects[levelIndex].SetActive(true);
            currentActiveLevelIndex = levelIndex;

            LogDebug($"✅ Level {levelIndex} props activés: {levelPropsObjects[levelIndex].name}");

            // Si on utilise des objets pré-placés, c'est tout
            if (usePreplacedObjects)
            {
                LogDebug($"Mode: Objets pré-placés (déjà dans la scène)");
            }
            else
            {
                // TODO: Logique de spawn dynamique si nécessaire
                LogDebug($"Mode: Spawn dynamique (à implémenter si besoin)");
            }
        }
        else
        {
            Debug.LogError($"[LevelSpawner] Level Props GameObject {levelIndex} est NULL!");
        }

        LogDebug("====================================");
    }

    /// <summary>
    /// Désactive les props d'un niveau spécifique
    /// </summary>
    public void DeactivateLevel(int levelIndex)
    {
        if (levelIndex < 0 || levelIndex >= levelPropsObjects.Length)
        {
            return;
        }

        if (levelPropsObjects[levelIndex] != null)
        {
            levelPropsObjects[levelIndex].SetActive(false);
            LogDebug($"❌ Level {levelIndex} props désactivés");
        }
    }

    /// <summary>
    /// Désactive tous les level props
    /// </summary>
    public void DeactivateAllLevels()
    {
        LogDebug("Désactivation de tous les level props...");

        for (int i = 0; i < levelPropsObjects.Length; i++)
        {
            if (levelPropsObjects[i] != null)
            {
                levelPropsObjects[i].SetActive(false);
            }
        }

        currentActiveLevelIndex = -1;
    }

    /// <summary>
    /// Retourne le GameObject props du niveau actif
    /// </summary>
    public GameObject GetActiveLevel()
    {
        if (currentActiveLevelIndex >= 0 && currentActiveLevelIndex < levelPropsObjects.Length)
        {
            return levelPropsObjects[currentActiveLevelIndex];
        }
        return null;
    }

    /// <summary>
    /// Retourne l'index du niveau actif
    /// </summary>
    public int GetActiveLevelIndex()
    {
        return currentActiveLevelIndex;
    }

    /// <summary>
    /// Valide le setup
    /// </summary>
    private bool ValidateSetup()
    {
        if (levelPropsObjects == null || levelPropsObjects.Length == 0)
        {
            Debug.LogError("[LevelSpawner] Aucun Level Props GameObject assigné! Ajoutez au moins 1 level.");
            return false;
        }

        int validProps = 0;
        for (int i = 0; i < levelPropsObjects.Length; i++)
        {
            if (levelPropsObjects[i] != null)
            {
                validProps++;
            }
        }

        if (validProps == 0)
        {
            Debug.LogError("[LevelSpawner] Tous les Level Props sont NULL! Assignez au moins 1 level valide.");
            return false;
        }

        LogDebug($"✅ {validProps}/{levelPropsObjects.Length} Level Props valides détectés");
        return true;
    }

    private void LogDebug(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[LevelSpawner] {message}");
        }
    }

    private void OnValidate()
    {
        // Pas de validation stricte - accepte 1 à N levels
    }

#if UNITY_EDITOR
    [ContextMenu("🔄 Reload Active Level Props")]
    private void ReloadActiveLevelProps()
    {
        if (Application.isPlaying && currentActiveLevelIndex >= 0)
        {
            DeactivateLevel(currentActiveLevelIndex);
            ActivateLevelProps(currentActiveLevelIndex);
        }
    }

    [ContextMenu("❌ Deactivate All Levels")]
    private void DeactivateAllLevelsMenu()
    {
        if (Application.isPlaying)
        {
            DeactivateAllLevels();
        }
    }
#endif
}
