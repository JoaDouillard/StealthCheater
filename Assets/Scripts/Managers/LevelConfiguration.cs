using UnityEngine;

/// <summary>
/// Configuration pour un niveau (classroom) spécifique
/// Chaque niveau a sa propre configuration de teacher, patrol zone, points d'intérêt, etc.
/// À créer en tant que ScriptableObject asset (Right Click > Create > StealthCheater > Level Configuration)
/// </summary>
[CreateAssetMenu(fileName = "LevelConfig", menuName = "StealthCheater/Level Configuration", order = 0)]
public class LevelConfiguration : ScriptableObject
{
    [Header("Level Info")]
    [Tooltip("Nom du niveau (ex: Classroom 1, Classroom 2, etc.)")]
    public string levelName = "Level 1";

    [Tooltip("Index du niveau (0-3 pour les 4 niveaux)")]
    [Range(0, 3)]
    public int levelIndex = 0;

    [TextArea(2, 4)]
    [Tooltip("Description du niveau")]
    public string description = "Description du niveau";

    [Header("Patrol Zone Configuration")]
    [Tooltip("Taille de la zone de patrol (X, Y, Z)")]
    public Vector3 patrolZoneSize = new Vector3(20f, 5f, 20f);

    [Tooltip("Offset du centre de la zone de patrol par rapport au PatrolZoneCenter Transform dans la scène")]
    public Vector3 patrolZoneCenterOffset = Vector3.zero;

    [Header("Point Selection Settings")]
    [Tooltip("Nombre maximum de tentatives pour trouver un point NavMesh valide")]
    [Range(10, 100)]
    public int maxAttempts = 30;

    [Tooltip("Distance minimale entre le point actuel et le prochain point")]
    [Range(1f, 10f)]
    public float minDistanceFromCurrent = 2f;

    [Tooltip("Rayon de snap automatique vers les points d'intérêt")]
    [Range(0.5f, 5f)]
    public float interestPointSnapRadius = 1.5f;

    [Header("Interest Points Tags")]
    [Tooltip("Tag pour trouver le Board (Tableau) dans la scène")]
    public string boardTag = "Board";

    [Tooltip("Tag pour trouver les Windows (Fenêtres) dans la scène")]
    public string windowTag = "Window";

    [Header("Spawn Grid Configuration")]
    [Tooltip("Nombre de colonnes (largeur) de la grille de spawn points")]
    [Range(1, 10)]
    public int gridWidth = 5;

    [Tooltip("Nombre de rangées (profondeur) de la grille de spawn points")]
    [Range(1, 10)]
    public int gridHeight = 3;

    [Tooltip("Espacement en X (largeur) entre chaque spawn point (en mètres)")]
    [Range(1f, 5f)]
    public float cellSpacingX = 3f;

    [Tooltip("Espacement en Z (profondeur) entre chaque spawn point (en mètres)")]
    [Range(1f, 5f)]
    public float cellSpacingZ = 2f;

    [Header("Desk Spawn Probabilities (%)")]
    [Tooltip("Probabilité qu'un DeskSpawnPoint reste vide (rien ne spawn)")]
    [Range(0f, 100f)]
    public float emptyDeskProbability = 20f;

    [Tooltip("Probabilité de spawner un bureau vide (sans élève ni obstacle)")]
    [Range(0f, 100f)]
    public float deskOnlyProbability = 10f;

    [Tooltip("Probabilité de spawner un bureau avec obstacle (livres, pot de fleurs, etc.)")]
    [Range(0f, 100f)]
    public float deskObstacleProbability = 30f;

    [Tooltip("Probabilité de spawner un bureau avec étudiant")]
    [Range(0f, 100f)]
    public float deskStudentProbability = 40f;

    [Tooltip("Garantir qu'au moins ce nombre d'étudiants spawn (pour la zone de copie)")]
    [Range(1, 10)]
    public int minStudentsRequired = 1;

    [Header("Desk Prefabs")]
    [Tooltip("Prefabs de bureaux vides")]
    public GameObject[] deskOnlyPrefabs;

    [Tooltip("Prefabs de bureaux avec obstacles (pré-configurés avec obstacles)")]
    public GameObject[] deskObstaclePrefabs;

    [Tooltip("Prefabs de bureaux avec étudiants (pré-configurés avec Student)")]
    public GameObject[] deskStudentPrefabs;

    [Header("Destination Probabilities (%)")]
    [Tooltip("Probabilité de choisir un point NavMesh aléatoire")]
    [Range(0f, 100f)]
    public float randomNavMeshProbability = 70f;

    [Tooltip("Probabilité de choisir le tableau")]
    [Range(0f, 100f)]
    public float boardProbability = 15f;

    [Tooltip("Probabilité de choisir une fenêtre")]
    [Range(0f, 100f)]
    public float windowProbability = 15f;

    [Header("Teacher Movement")]
    [Tooltip("Vitesse de marche du teacher")]
    [Range(1f, 5f)]
    public float walkSpeed = 2f;

    [Tooltip("Temps d'attente minimum à un point")]
    [Range(1f, 60f)]
    public float minWaitTime = 4f;

    [Tooltip("Temps d'attente maximum à un point")]
    [Range(1f, 60f)]
    public float maxWaitTime = 30f;

    [Tooltip("Probabilité pondérée pour le temps d'attente. 0=temps courts fréquents, 1=distribution uniforme, 2+=temps longs fréquents")]
    [Range(0f, 3f)]
    public float waitTimeBias = 0.5f;

    [Header("Detection Settings - Multi-Zone System")]
    [Tooltip("Angle du champ de vision (90° = 45° de chaque côté)")]
    [Range(30f, 180f)]
    public float fieldOfViewAngle = 90f;

    [Header("Zone 1 - Lointaine (Détection Lente)")]
    [Tooltip("Distance maximale de la zone 1 (détection lente)")]
    [Range(5f, 20f)]
    public float zone1MaxDistance = 8f;

    [Tooltip("Temps de détection en Zone 1 (secondes)")]
    [Range(1f, 10f)]
    public float zone1DetectionTime = 5f;

    [Header("Zone 2 - Moyenne (Détection Moyenne)")]
    [Tooltip("Distance maximale de la zone 2 (détection moyenne)")]
    [Range(3f, 15f)]
    public float zone2MaxDistance = 6f;

    [Tooltip("Temps de détection en Zone 2 (secondes)")]
    [Range(0.5f, 8f)]
    public float zone2DetectionTime = 3f;

    [Header("Zone 3 - Proche (Détection Immédiate)")]
    [Tooltip("Distance maximale de la zone 3 (détection immédiate)")]
    [Range(1f, 5f)]
    public float zone3MaxDistance = 2f;

    [Tooltip("Temps de détection en Zone 3 (secondes) - 0 = immédiat")]
    [Range(0f, 2f)]
    public float zone3DetectionTime = 0f;

    [Header("Crouch Modifier")]
    [Tooltip("Multiplicateur de distance quand le joueur est accroupi (0.75 = -25% de distance)")]
    [Range(0.1f, 1f)]
    public float crouchDistanceModifier = 0.75f;

    [Header("Special Behaviors (Optionnel)")]
    [Tooltip("Le teacher utilise-t-il un comportement spécial dans ce niveau ?")]
    public bool hasSpecialBehavior = false;

    [Tooltip("Description du comportement spécial (ex: patrouille plus rapide, plus de fenêtres, etc.)")]
    [TextArea(2, 3)]
    public string specialBehaviorDescription = "";

    /// <summary>
    /// Valide TOUTES les probabilités (patrol + desk) - Méthode générique
    /// </summary>
    public bool ValidateProbabilities()
    {
        return ValidatePatrolProbabilities() && ValidateDeskProbabilities();
    }

    /// <summary>
    /// Valide que les probabilités de patrol totalisent 100%
    /// </summary>
    public bool ValidatePatrolProbabilities()
    {
        float total = randomNavMeshProbability + boardProbability + windowProbability;
        return Mathf.Approximately(total, 100f);
    }

    /// <summary>
    /// Valide que les probabilités de desk totalisent 100%
    /// </summary>
    public bool ValidateDeskProbabilities()
    {
        float total = emptyDeskProbability + deskOnlyProbability + deskObstacleProbability + deskStudentProbability;
        return Mathf.Approximately(total, 100f);
    }

    /// <summary>
    /// Normalise TOUTES les probabilités (patrol + desk) - Méthode générique
    /// </summary>
    public void NormalizeProbabilities()
    {
        NormalizePatrolProbabilities();
        NormalizeDeskProbabilities();
    }

    /// <summary>
    /// Normalise les probabilités de patrol pour qu'elles totalisent 100%
    /// </summary>
    public void NormalizePatrolProbabilities()
    {
        float total = randomNavMeshProbability + boardProbability + windowProbability;

        if (total > 0f && !Mathf.Approximately(total, 100f))
        {
            randomNavMeshProbability = (randomNavMeshProbability / total) * 100f;
            boardProbability = (boardProbability / total) * 100f;
            windowProbability = (windowProbability / total) * 100f;

            Debug.Log($"[LevelConfiguration] {levelName}: Probabilités patrol normalisées à 100%");
        }
    }

    /// <summary>
    /// Normalise les probabilités de desk pour qu'elles totalisent 100%
    /// </summary>
    public void NormalizeDeskProbabilities()
    {
        float total = emptyDeskProbability + deskOnlyProbability + deskObstacleProbability + deskStudentProbability;

        if (total > 0f && !Mathf.Approximately(total, 100f))
        {
            emptyDeskProbability = (emptyDeskProbability / total) * 100f;
            deskOnlyProbability = (deskOnlyProbability / total) * 100f;
            deskObstacleProbability = (deskObstacleProbability / total) * 100f;
            deskStudentProbability = (deskStudentProbability / total) * 100f;

            Debug.Log($"[LevelConfiguration] {levelName}: Probabilités desk normalisées à 100%");
        }
    }

    private void OnValidate()
    {
        // Validation automatique dans l'Inspector
        if (!ValidatePatrolProbabilities())
        {
            float total = randomNavMeshProbability + boardProbability + windowProbability;
            Debug.LogWarning($"[LevelConfiguration] {levelName}: Les probabilités de patrol ne totalisent pas 100% ({total}%). " +
                           "Elles seront normalisées au runtime.");
        }

        if (!ValidateDeskProbabilities())
        {
            float total = emptyDeskProbability + deskOnlyProbability + deskObstacleProbability + deskStudentProbability;
            Debug.LogWarning($"[LevelConfiguration] {levelName}: Les probabilités de desk ne totalisent pas 100% ({total}%). " +
                           "Elles seront normalisées au runtime.");
        }

        if (maxWaitTime < minWaitTime)
        {
            maxWaitTime = minWaitTime;
            Debug.LogWarning($"[LevelConfiguration] {levelName}: maxWaitTime ne peut pas être inférieur à minWaitTime");
        }
    }
}
