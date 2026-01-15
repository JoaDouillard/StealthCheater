using UnityEngine;
using UnityEngine.AI;

public class TeacherPatrol : MonoBehaviour
{
    [Header("Scene References")]
    [Tooltip("Centre de la zone de patrol (GameObject vide au centre de la classroom)")]
    [SerializeField] private Transform patrolZoneCenter;

    [Header("Configuration (Auto-loaded from LevelManager)")]
    [Tooltip("Configuration actuelle chargée depuis LevelManager")]
    [SerializeField] private LevelConfiguration currentConfig;

    [Header("Interest Points (Auto-detected by Tags)")]
    [Tooltip("Board trouvé automatiquement par tag")]
    [SerializeField] private Transform boardPoint;

    [Tooltip("Windows trouvées automatiquement par tag")]
    [SerializeField] private Transform[] windowPoints;

    // Valeurs locales (chargées depuis config)
    private Vector3 patrolZoneSize;
    private int maxAttempts;
    private float minDistanceFromCurrent;
    private float interestPointSnapRadius;

    // État actuel
    private Vector3 currentPosition;
    private Vector3 lastRandomPoint = Vector3.zero;
    private Transform lastInterestPoint = null;
    private bool hasVisitedBoard = false;
    private bool[] hasVisitedWindow;

    public enum PointType { RandomNavMesh, Board, Window }

    public struct PatrolPoint
    {
        public Vector3 position;
        public PointType type;
        public Transform interestTransform; // null si RandomNavMesh
        public int windowIndex; // -1 si pas Window
    }

    private void Start()
    {
        // Charger dans Start() pour être sûr que LevelManager est prêt
        LoadConfiguration();
        FindInterestPoints();

        if (windowPoints != null)
        {
            hasVisitedWindow = new bool[windowPoints.Length];
        }
    }

    /// <summary>
    /// Charge la configuration depuis le LevelManager
    /// </summary>
    private void LoadConfiguration()
    {
        if (LevelManager.Instance == null)
        {
            Debug.LogError("[TeacherPatrol] LevelManager.Instance est NULL! Assurez-vous qu'il y a un LevelManager dans la scène.");
            return;
        }

        currentConfig = LevelManager.Instance.GetCurrentConfiguration();

        if (currentConfig == null)
        {
            Debug.LogError("[TeacherPatrol] Impossible de charger la configuration du niveau!");
            return;
        }

        // Charger les valeurs depuis la config
        patrolZoneSize = currentConfig.patrolZoneSize;
        maxAttempts = currentConfig.maxAttempts;
        minDistanceFromCurrent = currentConfig.minDistanceFromCurrent;
        interestPointSnapRadius = currentConfig.interestPointSnapRadius;

        Debug.Log($"[TeacherPatrol] ✅ Configuration chargée: {currentConfig.levelName}");
    }

    /// <summary>
    /// Trouve automatiquement les points d'intérêt par tags
    /// </summary>
    private void FindInterestPoints()
    {
        if (currentConfig == null) return;

        // Trouver le Board
        GameObject boardObject = GameObject.FindGameObjectWithTag(currentConfig.boardTag);
        if (boardObject != null)
        {
            boardPoint = boardObject.transform;
        }
        else
        {
            Debug.LogWarning($"[TeacherPatrol] ⚠️ Aucun Board trouvé avec le tag '{currentConfig.boardTag}'");
        }

        // Trouver les Windows
        GameObject[] windowObjects = GameObject.FindGameObjectsWithTag(currentConfig.windowTag);
        if (windowObjects != null && windowObjects.Length > 0)
        {
            windowPoints = new Transform[windowObjects.Length];
            for (int i = 0; i < windowObjects.Length; i++)
            {
                windowPoints[i] = windowObjects[i].transform;
            }
        }
        else
        {
            Debug.LogWarning($"[TeacherPatrol] ⚠️ Aucune Window trouvée avec le tag '{currentConfig.windowTag}'");
            windowPoints = new Transform[0];
        }
    }

    public PatrolPoint GetNextRandomPoint(Vector3 fromPosition)
    {
        currentPosition = fromPosition;

        // Chercher un point NavMesh random dans la zone
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            Vector3 randomPoint = GetRandomPointInZone();

            // Vérifier si sur NavMesh
            if (!IsPointOnNavMesh(randomPoint, out Vector3 navMeshPoint))
            {
                // Point pas sur NavMesh, réessaye
                continue;
            }

            // Vérifier distance minimale de la position actuelle
            float distFromCurrent = Vector3.Distance(navMeshPoint, currentPosition);
            if (distFromCurrent < minDistanceFromCurrent)
            {
                // Trop proche, réessaye
                continue;
            }

            // Point valide trouvé
            // Debug: Point NavMesh valide trouvé à X m (tentative Y)

            // Vérifier snap vers point d'intérêt
            PatrolPoint snapResult = CheckSnapToInterestPoint(navMeshPoint);
            if (snapResult.type != PointType.RandomNavMesh)
            {
                // Snap détecté
                return snapResult;
            }

            // Retourner point NavMesh normal
            lastRandomPoint = navMeshPoint;
            return new PatrolPoint
            {
                position = navMeshPoint,
                type = PointType.RandomNavMesh,
                interestTransform = null,
                windowIndex = -1
            };
        }

        // Fallback: retourne un point sans contraintes
        Debug.LogWarning($"[TeacherPatrol] Aucun point valide après {maxAttempts} tentatives, fallback sans contraintes");
        Vector3 fallbackPoint = GetRandomPointInZone();
        if (IsPointOnNavMesh(fallbackPoint, out Vector3 fallbackNavMesh))
        {
            return new PatrolPoint
            {
                position = fallbackNavMesh,
                type = PointType.RandomNavMesh,
                interestTransform = null,
                windowIndex = -1
            };
        }

        // Dernier recours
        return new PatrolPoint
        {
            position = currentPosition + Random.insideUnitSphere * 5f,
            type = PointType.RandomNavMesh,
            interestTransform = null,
            windowIndex = -1
        };
    }

    public PatrolPoint GetBoardPoint()
    {
        if (boardPoint == null)
        {
            Debug.LogWarning("[TeacherPatrol] boardPoint est null!");
            return GetNextRandomPoint(currentPosition);
        }

        hasVisitedBoard = true;
        lastInterestPoint = boardPoint;

        return new PatrolPoint
        {
            position = boardPoint.position,
            type = PointType.Board,
            interestTransform = boardPoint,
            windowIndex = -1
        };
    }

    public PatrolPoint GetRandomWindowPoint()
    {
        if (windowPoints == null || windowPoints.Length == 0)
        {
            Debug.LogWarning("[TeacherPatrol] Pas de windowPoints configurés!");
            return GetNextRandomPoint(currentPosition);
        }

        // Choisir une fenêtre random
        int windowIndex = Random.Range(0, windowPoints.Length);
        Transform chosenWindow = windowPoints[windowIndex];

        if (chosenWindow == null)
        {
            Debug.LogWarning($"[TeacherPatrol] Window {windowIndex} est null!");
            return GetNextRandomPoint(currentPosition);
        }

        hasVisitedWindow[windowIndex] = true;
        lastInterestPoint = chosenWindow;

        return new PatrolPoint
        {
            position = chosenWindow.position,
            type = PointType.Window,
            interestTransform = chosenWindow,
            windowIndex = windowIndex
        };
    }

    public void ResetVisitedPoints()
    {
        hasVisitedBoard = false;
        lastInterestPoint = null;

        if (hasVisitedWindow != null)
        {
            for (int i = 0; i < hasVisitedWindow.Length; i++)
            {
                hasVisitedWindow[i] = false;
            }
        }

        // Points visités réinitialisés
    }

    public bool CanVisitBoard()
    {
        // Ne peut pas revisiter le tableau s'il vient d'y être
        return !hasVisitedBoard;
    }

    public bool CanVisitWindow(int windowIndex)
    {
        if (windowIndex < 0 || windowIndex >= hasVisitedWindow.Length)
            return false;

        // Ne peut pas revisiter la même fenêtre
        return !hasVisitedWindow[windowIndex];
    }

    private Vector3 GetRandomPointInZone()
    {
        // Si patrolZoneCenter n'est pas assigné, chercher automatiquement dans le Level_Props actif
        if (patrolZoneCenter == null)
        {
            patrolZoneCenter = FindPatrolZoneCenterInLevel();
        }

        Vector3 center = patrolZoneCenter != null ? patrolZoneCenter.position : transform.position;

        float randomX = Random.Range(-patrolZoneSize.x / 2f, patrolZoneSize.x / 2f);
        float randomY = Random.Range(-patrolZoneSize.y / 2f, patrolZoneSize.y / 2f);
        float randomZ = Random.Range(-patrolZoneSize.z / 2f, patrolZoneSize.z / 2f);

        return center + new Vector3(randomX, randomY, randomZ);
    }

    /// <summary>
    /// Cherche automatiquement le PatrolZoneCenter dans le Level_Props actif (recherche récursive profonde)
    /// </summary>
    private Transform FindPatrolZoneCenterInLevel()
    {
        // Trouver le LevelSpawner
        LevelSpawner levelSpawner = FindFirstObjectByType<LevelSpawner>();
        if (levelSpawner == null)
        {
            Debug.LogWarning("[TeacherPatrol] ⚠️ LevelSpawner non trouvé! Utilisation de transform.position comme centre.");
            return null;
        }

        // Récupérer le niveau actif
        GameObject activeLevel = levelSpawner.GetActiveLevel();
        if (activeLevel == null)
        {
            Debug.LogWarning("[TeacherPatrol] ⚠️ Aucun niveau actif trouvé! Utilisation de transform.position comme centre.");
            return null;
        }

        // Recherche RÉCURSIVE PROFONDE dans toute l'arborescence (Level_Props/FixedPoints/PatrolZoneCenter)
        Transform patrolCenter = FindTransformRecursive(activeLevel.transform, "PatrolZoneCenter");
        if (patrolCenter != null)
        {
            Debug.Log($"[TeacherPatrol] ✅ PatrolZoneCenter trouvé automatiquement: {GetFullPath(patrolCenter)}");
            return patrolCenter;
        }

        // Fallback : chercher avec des noms alternatifs
        string[] alternativeNames = { "PatrolCenter", "Patrol_Zone_Center", "patrol_center" };
        foreach (string altName in alternativeNames)
        {
            patrolCenter = FindTransformRecursive(activeLevel.transform, altName);
            if (patrolCenter != null)
            {
                Debug.Log($"[TeacherPatrol] ✅ PatrolZoneCenter trouvé (nom alternatif '{altName}'): {GetFullPath(patrolCenter)}");
                return patrolCenter;
            }
        }

        Debug.LogWarning($"[TeacherPatrol] ⚠️ PatrolZoneCenter non trouvé dans {activeLevel.name} (recherche profonde effectuée).");
        Debug.LogWarning($"[TeacherPatrol] Utilisation de transform.position comme centre.");
        Debug.LogWarning($"[TeacherPatrol] Pour corriger: Créez un GameObject 'PatrolZoneCenter' dans {activeLevel.name} (peut être dans un sous-dossier comme FixedPoints)");
        return null;
    }

    /// <summary>
    /// Recherche récursive profonde d'un Transform par son nom dans toute l'arborescence
    /// </summary>
    private Transform FindTransformRecursive(Transform parent, string name)
    {
        // Vérifier le parent lui-même
        if (parent.name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
        {
            return parent;
        }

        // Chercher dans tous les enfants récursivement
        foreach (Transform child in parent)
        {
            Transform found = FindTransformRecursive(child, name);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    /// <summary>
    /// Retourne le chemin complet d'un Transform (pour debug)
    /// </summary>
    private string GetFullPath(Transform t)
    {
        string path = t.name;
        Transform current = t.parent;
        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }
        return path;
    }

    private bool IsPointOnNavMesh(Vector3 point, out Vector3 navMeshPoint)
    {
        NavMeshHit hit;
        if (NavMesh.SamplePosition(point, out hit, 2f, NavMesh.AllAreas))
        {
            navMeshPoint = hit.position;
            return true;
        }

        navMeshPoint = point;
        return false;
    }

    private PatrolPoint CheckSnapToInterestPoint(Vector3 randomPoint)
    {
        // Vérifier distance au tableau
        if (boardPoint != null && CanVisitBoard())
        {
            float distToBoard = Vector3.Distance(randomPoint, boardPoint.position);

            if (distToBoard <= interestPointSnapRadius)
            {
                Debug.Log($"[TeacherPatrol] ✅ Board trouvé");
                hasVisitedBoard = true;
                lastInterestPoint = boardPoint;

                return new PatrolPoint
                {
                    position = boardPoint.position,
                    type = PointType.Board,
                    interestTransform = boardPoint,
                    windowIndex = -1
                };
            }
        }

        // Vérifier distance aux fenêtres
        if (windowPoints != null)
        {
            for (int i = 0; i < windowPoints.Length; i++)
            {
                Transform window = windowPoints[i];
                if (window == null || !CanVisitWindow(i)) continue;

                float distToWindow = Vector3.Distance(randomPoint, window.position);

                if (distToWindow <= interestPointSnapRadius)
                {
                    Debug.Log($"[TeacherPatrol] ✅ {i} Window(s) trouvée(s)");
                    hasVisitedWindow[i] = true;
                    lastInterestPoint = window;

                    return new PatrolPoint
                    {
                        position = window.position,
                        type = PointType.Window,
                        interestTransform = window,
                        windowIndex = i
                    };
                }
            }
        }

        // Pas de snap
        return new PatrolPoint
        {
            position = randomPoint,
            type = PointType.RandomNavMesh,
            interestTransform = null,
            windowIndex = -1
        };
    }

    private void OnDrawGizmosSelected()
    {
        // Dessiner la zone de patrol
        Vector3 center = patrolZoneCenter != null ? patrolZoneCenter.position : transform.position;

        Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
        Gizmos.DrawCube(center, patrolZoneSize);

        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(center, patrolZoneSize);

        // Dessiner les points d'intérêt avec cercles de snap
        if (boardPoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(boardPoint.position, 0.5f);

            Gizmos.color = Color.yellow;
            DrawCircle(boardPoint.position, interestPointSnapRadius, 32);
        }

        if (windowPoints != null)
        {
            foreach (Transform window in windowPoints)
            {
                if (window != null)
                {
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawWireSphere(window.position, 0.5f);

                    Gizmos.color = Color.magenta;
                    DrawCircle(window.position, interestPointSnapRadius, 32);
                }
            }
        }
    }

    private void DrawCircle(Vector3 center, float radius, int segments)
    {
        float angleStep = 360f / segments;
        Vector3 previousPoint = center + new Vector3(radius, 0, 0);

        for (int i = 1; i <= segments; i++)
        {
            float angle = angleStep * i * Mathf.Deg2Rad;
            Vector3 newPoint = center + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
            Gizmos.DrawLine(previousPoint, newPoint);
            previousPoint = newPoint;
        }
    }
}
