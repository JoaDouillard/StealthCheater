using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.AI.Navigation;

/// <summary>
/// Gère le spawn aléatoire des props (chaises, bureaux, étudiants, obstacles) dans le niveau actif
/// Travaille avec LevelSpawner pour spawner dans le bon niveau
/// Place automatiquement CopyZone (sur Student) et ReturnZone (sur EmptyDesk)
/// </summary>
public class PropsSpawner : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Configuration du niveau actuel (auto-trouvée si vide)")]
    [SerializeField] private LevelConfiguration levelConfig;

    [Tooltip("Parent où spawner les objets (auto-trouvé dans Level_Props actif)")]
    [SerializeField] private Transform spawnPropsParent;

    [Header("Zones Gameplay")]
    [Tooltip("Prefab de la zone de copie (trigger où copier)")]
    [SerializeField] private GameObject copyZonePrefab;

    [Tooltip("Prefab de la zone de retour (place du joueur)")]
    [SerializeField] private GameObject returnZonePrefab;

    [Header("Layers pour filtrage")]
    [Tooltip("Layer des objets avec étudiant")]
    [SerializeField] private string studentLayerName = "Student";

    [Tooltip("Layer des bureaux vides")]
    [SerializeField] private string emptyDeskLayerName = "EmptyDesk";

    // [Tooltip("Layer des obstacles")] // UNUSED - Commenté pour éviter warning CS0414
    // [SerializeField] private string obstacleLayerName = "Obstacle";

    [Header("NavMesh")]
    [Tooltip("NavMeshSurface à rebaker après spawn (optionnel, auto-trouvé si vide)")]
    [SerializeField] private NavMeshSurface navMeshSurface;

    // [Tooltip("Attendre la fin du rebake avant de continuer?")] // UNUSED - Le rebake est synchrone
    // [SerializeField] private bool waitForNavMeshRebake = true;

    [Header("Runtime Info (Read-Only)")]
    [SerializeField] private int totalSpawnPoints = 0;
    [SerializeField] private int spawnedProps = 0;
    [SerializeField] private int spawnedStudents = 0;
    [SerializeField] private int spawnedEmptyDesks = 0;
    [SerializeField] private GameObject copyZoneInstance;
    [SerializeField] private GameObject returnZoneInstance;

    private List<DeskSpawnPoint> allSpawnPoints = new List<DeskSpawnPoint>();
    private List<GameObject> studentObjects = new List<GameObject>();
    private List<GameObject> emptyDeskObjects = new List<GameObject>();

    // Système anti-doublons: decks de prefabs (shuffle + draw)
    private List<GameObject> deskOnlyDeck = new List<GameObject>();
    private List<GameObject> deskObstacleDeck = new List<GameObject>();
    private List<GameObject> deskStudentDeck = new List<GameObject>();

    private void Start()
    {
        // Auto-find level config
        if (levelConfig == null)
        {
            LevelManager levelManager = FindFirstObjectByType<LevelManager>();
            if (levelManager != null)
            {
                levelConfig = levelManager.GetCurrentLevelConfig();
            }
        }

        if (levelConfig == null)
        {
            Debug.LogError("[PropsSpawner] Aucune LevelConfiguration trouvée!");
            enabled = false;
            return;
        }

        // Trouver le parent SpawnProps dans le Level_Props actif
        if (spawnPropsParent == null)
        {
            spawnPropsParent = FindSpawnPropsParent();
        }

        if (spawnPropsParent == null)
        {
            Debug.LogError("[PropsSpawner] SpawnProps parent non trouvé! Vérifier la hiérarchie du niveau.");
            enabled = false;
            return;
        }

        // SpawnProps trouvé

        // Normaliser les probabilités
        levelConfig.NormalizeDeskProbabilities();

        // Initialiser les decks anti-doublons
        InitializePrefabDecks();

        // Scanner les spawn points
        ScanSpawnPoints();

        // Spawner tous les props
        SpawnAllProps();

        // Placer les zones gameplay
        PlaceCopyZone();
        PlaceReturnZone();

        // Rebaker le NavMesh après spawn
        RebakeNavMesh();

        // Log résumé
        Debug.Log($"[PropsSpawner] ✅ Spawn terminé: {spawnedProps} props, {spawnedStudents} étudiants, {spawnedEmptyDesks} bureaux vides sur {totalSpawnPoints} positions");
    }

    /// <summary>
    /// Trouve le parent SpawnProps dans le Level_Props actif
    /// </summary>
    private Transform FindSpawnPropsParent()
    {
        LevelSpawner levelSpawner = FindFirstObjectByType<LevelSpawner>();
        if (levelSpawner == null)
        {
            Debug.LogError("[PropsSpawner] LevelSpawner non trouvé!");
            return null;
        }

        GameObject activeLevel = levelSpawner.GetActiveLevel();
        if (activeLevel == null)
        {
            Debug.LogError("[PropsSpawner] Aucun niveau actif trouvé!");
            return null;
        }

        // Chercher SpawnProps dans le niveau actif
        Transform SpawnProps = activeLevel.transform.Find("SpawnProps");
        if (SpawnProps == null)
        {
            Debug.LogError($"[PropsSpawner] 'SpawnProps' non trouvé dans {activeLevel.name}! Créer ce GameObject dans la hiérarchie.");
            return null;
        }

        return SpawnProps;
    }

    /// <summary>
    /// Initialise les decks de prefabs (anti-doublons)
    /// Chaque deck contient tous les prefabs disponibles, shufflés
    /// Quand le deck est vide, on le reremplit et on reshuffle
    /// </summary>
    private void InitializePrefabDecks()
    {
        // Deck DeskOnly
        if (levelConfig.deskOnlyPrefabs != null && levelConfig.deskOnlyPrefabs.Length > 0)
        {
            deskOnlyDeck.AddRange(levelConfig.deskOnlyPrefabs);
            ShuffleList(deskOnlyDeck);
        }

        // Deck DeskObstacle
        if (levelConfig.deskObstaclePrefabs != null && levelConfig.deskObstaclePrefabs.Length > 0)
        {
            deskObstacleDeck.AddRange(levelConfig.deskObstaclePrefabs);
            ShuffleList(deskObstacleDeck);
        }

        // Deck DeskStudent
        if (levelConfig.deskStudentPrefabs != null && levelConfig.deskStudentPrefabs.Length > 0)
        {
            deskStudentDeck.AddRange(levelConfig.deskStudentPrefabs);
            ShuffleList(deskStudentDeck);
        }
    }

    /// <summary>
    /// Tire un prefab du deck (système anti-doublons)
    /// Quand le deck est vide, le reremplit avec tous les prefabs et reshuffle
    /// </summary>
    private GameObject DrawFromDeck(List<GameObject> deck, GameObject[] sourcePrefabs)
    {
        // Si le deck est vide, le reremplit
        if (deck.Count == 0)
        {
            if (sourcePrefabs == null || sourcePrefabs.Length == 0)
            {
                Debug.LogWarning("[PropsSpawner] Impossible de reremplit le deck: pas de prefabs source!");
                return null;
            }

            deck.AddRange(sourcePrefabs);
            ShuffleList(deck);
        }

        // Tirer le dernier prefab du deck
        GameObject prefab = deck[deck.Count - 1];
        deck.RemoveAt(deck.Count - 1);
        return prefab;
    }

    /// <summary>
    /// Scanner tous les DeskSpawnPoint dans le niveau
    /// </summary>
    private void ScanSpawnPoints()
    {
        // Chercher tous les DeskSpawnPoint dans la scène
        allSpawnPoints.AddRange(FindObjectsByType<DeskSpawnPoint>(FindObjectsSortMode.None));

        totalSpawnPoints = allSpawnPoints.Count;

        if (totalSpawnPoints == 0)
        {
            Debug.LogWarning("[PropsSpawner] Aucun DeskSpawnPoint trouvé! Créer des spawn points dans la scène.");
        }
    }

    /// <summary>
    /// Spawner tous les props selon les probabilités
    /// </summary>
    private void SpawnAllProps()
    {
        // Shuffle pour randomiser
        ShuffleList(allSpawnPoints);

        foreach (DeskSpawnPoint spawnPoint in allSpawnPoints)
        {
            DeskSpawnType type = ChooseSpawnType();
            SpawnAtPoint(spawnPoint, type);
        }

        // Garantir le nombre minimum d'étudiants
        EnsureMinimumStudents();

        // Garantir au moins 1 bureau vide (CRITIQUE pour ReturnZone)
        EnsureMinimumEmptyDesks();
    }

    /// <summary>
    /// Choisir aléatoirement le type selon probabilités
    /// </summary>
    private DeskSpawnType ChooseSpawnType()
    {
        float roll = Random.Range(0f, 100f);
        float cumulative = 0f;

        cumulative += levelConfig.emptyDeskProbability;
        if (roll < cumulative) return DeskSpawnType.Empty;

        cumulative += levelConfig.deskOnlyProbability;
        if (roll < cumulative) return DeskSpawnType.DeskOnly;

        cumulative += levelConfig.deskObstacleProbability;
        if (roll < cumulative) return DeskSpawnType.DeskObstacle;

        return DeskSpawnType.DeskStudent;
    }

    /// <summary>
    /// Spawner un prop à un spawn point
    /// </summary>
    private void SpawnAtPoint(DeskSpawnPoint spawnPoint, DeskSpawnType type)
    {
        GameObject prefabToSpawn = null;

        switch (type)
        {
            case DeskSpawnType.Empty:
                spawnPoint.spawnedType = DeskSpawnType.Empty;
                return;

            case DeskSpawnType.DeskOnly:
                prefabToSpawn = DrawFromDeck(deskOnlyDeck, levelConfig.deskOnlyPrefabs);
                break;

            case DeskSpawnType.DeskObstacle:
                prefabToSpawn = DrawFromDeck(deskObstacleDeck, levelConfig.deskObstaclePrefabs);
                break;

            case DeskSpawnType.DeskStudent:
                prefabToSpawn = DrawFromDeck(deskStudentDeck, levelConfig.deskStudentPrefabs);
                break;
        }

        if (prefabToSpawn != null)
        {
            // Apply rotation offset from level configuration (useful for levels with different orientations)
            Quaternion rotationOffset = Quaternion.Euler(levelConfig.spawnRotationOffset);
            Quaternion finalRotation = spawnPoint.transform.rotation * rotationOffset;

            GameObject spawned = Instantiate(
                prefabToSpawn,
                spawnPoint.transform.position,
                finalRotation,
                spawnPropsParent
            );

            spawned.name = $"{prefabToSpawn.name}_{spawnPoint.name}";
            spawnPoint.spawnedObject = spawned;
            spawnPoint.spawnedType = type;
            spawnedProps++;

            // Filtrer par Layer pour les zones gameplay
            // IMPORTANT : Chercher dans les enfants aussi (le layer peut être sur un child)
            bool hasStudentLayer = CheckLayerRecursive(spawned, studentLayerName);
            bool hasEmptyDeskLayer = CheckLayerRecursive(spawned, emptyDeskLayerName);

            if (hasStudentLayer)
            {
                studentObjects.Add(spawned);
                spawnedStudents++;
                // Étudiant trouvé
            }
            else if (hasEmptyDeskLayer || type == DeskSpawnType.DeskOnly)
            {
                // Considérer les DeskOnly comme bureaux vides même sans layer
                emptyDeskObjects.Add(spawned);
                spawnedEmptyDesks++;
            }
        }
    }

    /// <summary>
    /// Vérifie récursivement si un GameObject ou ses enfants ont un layer spécifique
    /// </summary>
    private bool CheckLayerRecursive(GameObject obj, string layerName)
    {
        // Vérifier l'objet lui-même
        if (LayerMask.LayerToName(obj.layer) == layerName)
        {
            return true;
        }

        // Vérifier tous les enfants récursivement
        foreach (Transform child in obj.transform)
        {
            if (CheckLayerRecursive(child.gameObject, layerName))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Garantir le minimum d'étudiants
    /// </summary>
    private void EnsureMinimumStudents()
    {
        int studentsNeeded = levelConfig.minStudentsRequired - spawnedStudents;

        if (studentsNeeded > 0)
        {
            Debug.LogWarning($"[PropsSpawner] Seulement {spawnedStudents} étudiants, {studentsNeeded} de plus nécessaires");

            List<DeskSpawnPoint> availablePoints = allSpawnPoints
                .Where(sp => sp.spawnedType != DeskSpawnType.DeskStudent)
                .ToList();

            ShuffleList(availablePoints);

            for (int i = 0; i < studentsNeeded && i < availablePoints.Count; i++)
            {
                DeskSpawnPoint point = availablePoints[i];

                if (point.spawnedObject != null)
                {
                    Destroy(point.spawnedObject);
                    spawnedProps--;
                }

                SpawnAtPoint(point, DeskSpawnType.DeskStudent);
            }
        }
    }

    /// <summary>
    /// Garantir au moins 1 bureau vide (CRITIQUE pour ReturnZone)
    /// Si aucun bureau vide n'existe, force le spawn d'un DeskOnly
    /// </summary>
    private void EnsureMinimumEmptyDesks()
    {
        // Vérifier si au moins 1 bureau vide existe
        if (emptyDeskObjects.Count == 0)
        {
            Debug.LogWarning("[PropsSpawner] ⚠️ Aucun bureau vide détecté! Forçage d'un bureau vide pour ReturnZone...");

            // Trouver les spawn points disponibles (éviter de remplacer les étudiants)
            List<DeskSpawnPoint> availablePoints = allSpawnPoints
                .Where(sp => sp.spawnedType != DeskSpawnType.DeskStudent)
                .ToList();

            if (availablePoints.Count > 0)
            {
                ShuffleList(availablePoints);
                DeskSpawnPoint point = availablePoints[0];

                // Détruire l'objet existant si présent
                if (point.spawnedObject != null)
                {
                    Destroy(point.spawnedObject);
                    spawnedProps--;
                }

                // Forcer le spawn d'un DeskOnly (bureau vide)
                SpawnAtPoint(point, DeskSpawnType.DeskOnly);

                Debug.Log($"[PropsSpawner] ✅ Bureau vide forcé sur {point.name} pour garantir ReturnZone");
            }
            else if (allSpawnPoints.Count > 0)
            {
                // Dernier recours: remplacer même un étudiant si nécessaire
                Debug.LogWarning("[PropsSpawner] ⚠️ Aucun point disponible, remplacement d'un étudiant par un bureau vide...");

                DeskSpawnPoint point = allSpawnPoints[0];

                // Retirer l'étudiant de la liste si c'en était un
                if (point.spawnedObject != null)
                {
                    studentObjects.Remove(point.spawnedObject);
                    Destroy(point.spawnedObject);
                    spawnedProps--;
                    spawnedStudents--;
                }

                // Forcer le spawn d'un DeskOnly
                SpawnAtPoint(point, DeskSpawnType.DeskOnly);

                Debug.Log($"[PropsSpawner] ✅ Bureau vide forcé (remplacement étudiant) sur {point.name}");
            }
            else
            {
                Debug.LogError("[PropsSpawner] ❌ CRITIQUE: Aucun spawn point disponible pour forcer un bureau vide!");
            }
        }
    }

    /// <summary>
    /// Placer la CopyZone sur un étudiant aléatoire
    /// </summary>
    private void PlaceCopyZone()
    {
        if (studentObjects.Count == 0)
        {
            Debug.LogError("[PropsSpawner] ❌ Aucun étudiant (Layer 'Student') spawné! Impossible de placer CopyZone.");
            Debug.LogError("[PropsSpawner] Vérifiez que:");
            Debug.LogError("  1. Le layer 'Student' existe (Edit → Project Settings → Tags and Layers)");
            Debug.LogError("  2. Le personnage étudiant (enfant du prefab) a le layer 'Student'");
            Debug.LogError("  3. LevelConfiguration.deskStudentPrefabs contient des prefabs valides");
            return;
        }

        GameObject chosenStudentPrefab = studentObjects[Random.Range(0, studentObjects.Count)];

        // Trouver le GameObject avec le layer "Student" (le personnage étudiant)
        GameObject studentCharacter = FindChildWithLayer(chosenStudentPrefab, studentLayerName);

        if (studentCharacter == null)
        {
            Debug.LogWarning($"[PropsSpawner] ⚠️ Aucun enfant avec layer 'Student' trouvé dans {chosenStudentPrefab.name}. Utilisation du prefab root.");
            studentCharacter = chosenStudentPrefab;
        }

        if (copyZonePrefab != null)
        {
            copyZoneInstance = Instantiate(
                copyZonePrefab,
                studentCharacter.transform.position,
                Quaternion.identity,
                studentCharacter.transform
            );

            copyZoneInstance.name = "CopyZone";
            Debug.Log($"[PropsSpawner] ✅ CopyZone placée sur {studentCharacter.name} (parent: {chosenStudentPrefab.name})");
        }
        else
        {
            Debug.LogWarning("[PropsSpawner] ⚠️ CopyZone prefab non assigné!");
        }
    }

    /// <summary>
    /// Trouve le premier enfant (récursivement) qui a un layer spécifique
    /// </summary>
    private GameObject FindChildWithLayer(GameObject parent, string layerName)
    {
        // Vérifier l'objet lui-même
        if (LayerMask.LayerToName(parent.layer) == layerName)
        {
            return parent;
        }

        // Chercher dans les enfants
        foreach (Transform child in parent.transform)
        {
            GameObject found = FindChildWithLayer(child.gameObject, layerName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    /// <summary>
    /// Placer la ReturnZone sur un bureau vide aléatoire
    /// </summary>
    private void PlaceReturnZone()
    {
        if (emptyDeskObjects.Count == 0)
        {
            Debug.LogError("[PropsSpawner] ❌ Aucun bureau vide (Layer 'EmptyDesk') spawné! Impossible de placer ReturnZone.");
            return;
        }

        GameObject chosenEmptyDesk = emptyDeskObjects[Random.Range(0, emptyDeskObjects.Count)];

        if (returnZonePrefab != null)
        {
            returnZoneInstance = Instantiate(
                returnZonePrefab,
                chosenEmptyDesk.transform.position,
                Quaternion.identity,
                chosenEmptyDesk.transform
            );

            returnZoneInstance.name = "ReturnZone";
            Debug.Log($"[PropsSpawner] ✅ ReturnZone placée sur {chosenEmptyDesk.name}");
        }
        else
        {
            Debug.LogWarning("[PropsSpawner] ⚠️ ReturnZone prefab non assigné!");
        }
    }

    /// <summary>
    /// Shuffle une liste
    /// </summary>
    private void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    /// <summary>
    /// Rebake le NavMesh après le spawn des props
    /// Utilise le NavMeshSurface global (NavMeshManager) qui bake uniquement sur le layer "LevelFloor"
    /// </summary>
    private void RebakeNavMesh()
    {
        // Auto-trouver NavMeshSurface global si non assigné
        if (navMeshSurface == null)
        {
            navMeshSurface = FindFirstObjectByType<NavMeshSurface>();
        }

        if (navMeshSurface != null)
        {
            Debug.Log("[PropsSpawner] 🔄 Début du rebake NavMesh...");

            // BuildNavMesh est synchrone, donc il attend la fin automatiquement
            navMeshSurface.BuildNavMesh();

            Debug.Log("[PropsSpawner] ✅ NavMesh rebaked avec succès!");
        }
        else
        {
            Debug.LogError("[PropsSpawner] ❌ Aucun NavMeshSurface trouvé dans la scène!");
            Debug.LogError("[PropsSpawner] Le NavMesh ne sera pas rebaked. Le Teacher ne pourra pas se déplacer.");
            Debug.LogError("[PropsSpawner] SOLUTION:");
            Debug.LogError("[PropsSpawner] 1. Créez un GameObject 'NavMeshManager' dans la scène");
            Debug.LogError("[PropsSpawner] 2. Ajoutez le composant 'NavMesh Surface'");
            Debug.LogError("[PropsSpawner] 3. Configurez Include Layers = UNIQUEMENT 'LevelFloor'");
            Debug.LogError("[PropsSpawner] 4. Configurez Use Geometry = 'Physics Colliders'");
        }
    }

    /// <summary>
    /// Retourne le NavMeshSurface utilisé (pour accès externe)
    /// </summary>
    public NavMeshSurface GetNavMeshSurface()
    {
        return navMeshSurface;
    }

    /// <summary>
    /// Retourne la ReturnZone spawnée (pour accès externe par SpawnManager)
    /// </summary>
    public GameObject GetReturnZone()
    {
        return returnZoneInstance;
    }

    /// <summary>
    /// Retourne la CopyZone spawnée (pour accès externe)
    /// </summary>
    public GameObject GetCopyZone()
    {
        return copyZoneInstance;
    }

    /// <summary>
    /// Cleanup
    /// </summary>
    private void OnDestroy()
    {
        foreach (DeskSpawnPoint spawnPoint in allSpawnPoints)
        {
            if (spawnPoint != null && spawnPoint.spawnedObject != null)
            {
                Destroy(spawnPoint.spawnedObject);
            }
        }

        if (copyZoneInstance != null) Destroy(copyZoneInstance);
        if (returnZoneInstance != null) Destroy(returnZoneInstance);
    }
}
