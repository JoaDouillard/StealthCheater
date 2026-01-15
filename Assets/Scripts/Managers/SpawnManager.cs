using UnityEngine;
using System.Collections;

/// <summary>
/// Gère le spawn du Teacher et du Player aux bonnes positions DYNAMIQUEMENT
/// Teacher spawne au tableau (trouvé via tag "Board" du niveau actif)
/// Player spawne à la ReturnZone (placée par PropsSpawner)
///
/// NOUVEAU : Système 100% dynamique sans points fixes manuels
///
/// Setup:
/// 1. Créer un GameObject "SpawnManager" dans la scène
/// 2. Ajouter ce script
/// 3. Assigner Teacher et Player prefabs/références dans l'Inspector
/// 4. Le script trouve automatiquement:
///    - Le tableau via le tag "Board" (défini dans LevelConfiguration)
///    - La ReturnZone via PropsSpawner
/// </summary>
public class SpawnManager : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Le Teacher à spawner (ou référence si déjà dans la scène)")]
    [SerializeField] private GameObject teacherPrefabOrInstance;

    [Tooltip("Le Player à spawner (ou référence si déjà dans la scène)")]
    [SerializeField] private GameObject playerPrefabOrInstance;

    [Tooltip("Spawner le Teacher? (Décocher si déjà dans la scène)")]
    [SerializeField] private bool shouldSpawnTeacher = false;

    [Tooltip("Spawner le Player? (Décocher si déjà dans la scène)")]
    [SerializeField] private bool shouldSpawnPlayer = false;

    [Header("Spawn Delays")]
    [Tooltip("Nombre de frames à attendre pour que PropsSpawner finisse + NavMesh rebake")]
    [SerializeField] private int waitFrames = 2;

    [Header("Offset")]
    [Tooltip("Offset de position pour le Teacher (relatif au tableau)")]
    [SerializeField] private Vector3 teacherSpawnOffset = Vector3.zero;

    [Tooltip("Offset de position pour le Player (relatif à la ReturnZone)")]
    [SerializeField] private Vector3 playerSpawnOffset = Vector3.zero;

    [Header("Runtime Info (Read-Only)")]
    [SerializeField] private PropsSpawner propsSpawner;
    [SerializeField] private LevelConfiguration currentLevelConfig;
    [SerializeField] private GameObject boardObject;
    [SerializeField] private GameObject returnZoneObject;
    [SerializeField] private bool teacherSpawned = false;
    [SerializeField] private bool playerSpawned = false;

    private void Start()
    {
        // Attendre que PropsSpawner termine + NavMesh rebake
        StartCoroutine(SpawnEntitiesDelayed());
    }

    /// <summary>
    /// Spawne Teacher et Player après que PropsSpawner ait terminé
    /// </summary>
    private IEnumerator SpawnEntitiesDelayed()
    {
        // Attendre plusieurs frames pour que PropsSpawner termine complètement
        // Frame 1: Props spawn
        // Frame 2: NavMesh rebake
        // Frame 3: NPCs spawn (nous)
        for (int i = 0; i < waitFrames; i++)
        {
            yield return null;
        }

        Debug.Log("[SpawnManager] Début du spawn des entités...");

        // Trouver les composants nécessaires
        if (!FindRequiredComponents())
        {
            Debug.LogError("[SpawnManager] ❌ Impossible de trouver les composants requis!");
            yield break;
        }

        // Attendre que le NavMesh soit baké (CRITIQUE pour éviter NavMeshAgent null)
        Unity.AI.Navigation.NavMeshSurface navMesh = propsSpawner.GetNavMeshSurface();
        if (navMesh != null)
        {
            int waitFrames = 0;
            while (navMesh.navMeshData == null && waitFrames < 100)
            {
                waitFrames++;
                yield return null;
            }

            if (navMesh.navMeshData != null)
            {
                Debug.Log($"[SpawnManager] ✅ NavMesh prêt après {waitFrames} frame(s)");
            }
            else
            {
                Debug.LogWarning("[SpawnManager] ⚠️ NavMesh pas prêt après 100 frames, continue quand même...");
            }
        }

        // Trouver le tableau et la ReturnZone
        if (!FindSpawnPoints())
        {
            Debug.LogError("[SpawnManager] ❌ Impossible de trouver les points de spawn!");
            yield break;
        }

        // Spawner Teacher
        if (shouldSpawnTeacher)
        {
            SpawnTeacher();
        }
        else if (teacherPrefabOrInstance != null)
        {
            // Si Teacher existe déjà, le déplacer au tableau
            MoveTeacherToBoard();
        }

        // Spawner Player
        if (shouldSpawnPlayer)
        {
            SpawnPlayer();
        }
        else if (playerPrefabOrInstance != null)
        {
            // Si Player existe déjà, le déplacer à la ReturnZone
            MovePlayerToReturnZone();
        }

        Debug.Log($"[SpawnManager] ✅ Spawn terminé: Teacher={teacherSpawned}, Player={playerSpawned}");
    }

    /// <summary>
    /// Trouve les composants requis (PropsSpawner, LevelConfiguration)
    /// </summary>
    private bool FindRequiredComponents()
    {
        // Trouver PropsSpawner
        if (propsSpawner == null)
        {
            propsSpawner = FindFirstObjectByType<PropsSpawner>();
            if (propsSpawner == null)
            {
                Debug.LogError("[SpawnManager] ❌ PropsSpawner non trouvé!");
                return false;
            }
        }

        // Trouver LevelManager et sa configuration actuelle
        LevelManager levelManager = LevelManager.Instance;
        if (levelManager == null)
        {
            Debug.LogError("[SpawnManager] ❌ LevelManager.Instance est NULL!");
            return false;
        }

        currentLevelConfig = levelManager.GetCurrentLevelConfig();
        if (currentLevelConfig == null)
        {
            Debug.LogError("[SpawnManager] ❌ LevelConfiguration actuelle est NULL!");
            return false;
        }

        Debug.Log($"[SpawnManager] ✅ Composants trouvés: PropsSpawner={propsSpawner.name}, LevelConfig={currentLevelConfig.levelName}");
        return true;
    }

    /// <summary>
    /// Trouve dynamiquement le tableau et la ReturnZone
    /// </summary>
    private bool FindSpawnPoints()
    {
        // 1. Trouver le tableau via le tag défini dans LevelConfiguration
        string boardTag = currentLevelConfig.boardTag;
        if (string.IsNullOrEmpty(boardTag))
        {
            Debug.LogError("[SpawnManager] ❌ boardTag est vide dans LevelConfiguration!");
            return false;
        }

        boardObject = GameObject.FindGameObjectWithTag(boardTag);
        if (boardObject == null)
        {
            Debug.LogError($"[SpawnManager] ❌ Aucun GameObject avec le tag '{boardTag}' trouvé!");
            Debug.LogError($"[SpawnManager] Assurez-vous que le tableau du niveau actif a le tag '{boardTag}'.");
            return false;
        }

        Debug.Log($"[SpawnManager] ✅ Tableau trouvé: {boardObject.name} (tag: {boardTag})");

        // 2. Récupérer la ReturnZone depuis PropsSpawner (méthode publique)
        returnZoneObject = propsSpawner.GetReturnZone();

        if (returnZoneObject == null)
        {
            Debug.LogError("[SpawnManager] ❌ ReturnZone non trouvée!");
            Debug.LogError("[SpawnManager] Assurez-vous que PropsSpawner a bien placé la ReturnZone.");
            Debug.LogError("[SpawnManager] Vérifiez que:");
            Debug.LogError("  1. Le layer 'EmptyDesk' existe");
            Debug.LogError("  2. Les prefabs de bureaux vides ont le layer 'EmptyDesk'");
            Debug.LogError("  3. LevelConfiguration.deskOnlyProbability > 0%");
            return false;
        }

        Debug.Log($"[SpawnManager] ✅ ReturnZone trouvée: {returnZoneObject.name}");
        return true;
    }

    /// <summary>
    /// Spawne le Teacher au tableau
    /// </summary>
    private void SpawnTeacher()
    {
        if (boardObject == null)
        {
            Debug.LogError("[SpawnManager] ❌ Impossible de spawner Teacher: boardObject null!");
            return;
        }

        if (teacherPrefabOrInstance == null)
        {
            Debug.LogError("[SpawnManager] ❌ Impossible de spawner Teacher: teacherPrefabOrInstance null!");
            return;
        }

        // Position au tableau + offset
        Vector3 spawnPosition = boardObject.transform.position + teacherSpawnOffset;
        Quaternion spawnRotation = boardObject.transform.rotation;

        // Instantier si c'est un prefab
        if (!teacherPrefabOrInstance.scene.IsValid())
        {
            GameObject teacherInstance = Instantiate(teacherPrefabOrInstance, spawnPosition, spawnRotation);
            teacherInstance.name = "Teacher";
            teacherSpawned = true;
            Debug.Log($"[SpawnManager] ✅ Teacher spawné au tableau: {spawnPosition}");
        }
        else
        {
            // Si c'est une instance de scène, la déplacer
            teacherPrefabOrInstance.transform.position = spawnPosition;
            teacherPrefabOrInstance.transform.rotation = spawnRotation;
            teacherSpawned = true;
            Debug.Log($"[SpawnManager] ✅ Teacher déplacé au tableau: {spawnPosition}");
        }
    }

    /// <summary>
    /// Déplace le Teacher existant au tableau
    /// </summary>
    private void MoveTeacherToBoard()
    {
        if (boardObject == null)
        {
            Debug.LogWarning("[SpawnManager] ⚠️ Impossible de déplacer Teacher: boardObject non trouvé");
            return;
        }

        if (teacherPrefabOrInstance != null)
        {
            teacherPrefabOrInstance.transform.position = boardObject.transform.position + teacherSpawnOffset;
            teacherPrefabOrInstance.transform.rotation = boardObject.transform.rotation;
            Debug.Log($"[SpawnManager] ✅ Teacher déplacé au tableau");
        }
    }

    /// <summary>
    /// Spawne le Player à la ReturnZone
    /// </summary>
    private void SpawnPlayer()
    {
        if (returnZoneObject == null)
        {
            Debug.LogError("[SpawnManager] ❌ Impossible de spawner Player: returnZoneObject null!");
            return;
        }

        if (playerPrefabOrInstance == null)
        {
            Debug.LogError("[SpawnManager] ❌ Impossible de spawner Player: playerPrefabOrInstance null!");
            return;
        }

        // Position à la ReturnZone + offset
        Vector3 spawnPosition = returnZoneObject.transform.position + playerSpawnOffset;
        Quaternion spawnRotation = returnZoneObject.transform.rotation;

        // Instantier si c'est un prefab
        if (!playerPrefabOrInstance.scene.IsValid())
        {
            GameObject playerInstance = Instantiate(playerPrefabOrInstance, spawnPosition, spawnRotation);
            playerInstance.name = "Player";
            playerSpawned = true;
            Debug.Log($"[SpawnManager] ✅ Player spawné à la ReturnZone: {spawnPosition}");
        }
        else
        {
            // Si c'est une instance de scène, la déplacer
            playerPrefabOrInstance.transform.position = spawnPosition;
            playerPrefabOrInstance.transform.rotation = spawnRotation;
            playerSpawned = true;
            Debug.Log($"[SpawnManager] ✅ Player déplacé à la ReturnZone: {spawnPosition}");
        }
    }

    /// <summary>
    /// Déplace le Player existant à la ReturnZone
    /// </summary>
    private void MovePlayerToReturnZone()
    {
        if (returnZoneObject == null)
        {
            Debug.LogWarning("[SpawnManager] ⚠️ Impossible de déplacer Player: returnZoneObject non trouvé");
            return;
        }

        if (playerPrefabOrInstance != null)
        {
            playerPrefabOrInstance.transform.position = returnZoneObject.transform.position + playerSpawnOffset;
            playerPrefabOrInstance.transform.rotation = returnZoneObject.transform.rotation;
            Debug.Log($"[SpawnManager] ✅ Player déplacé à la ReturnZone");
        }
    }

#if UNITY_EDITOR
    [ContextMenu("🔄 Respawn Teacher")]
    private void ContextMenu_RespawnTeacher()
    {
        if (Application.isPlaying)
        {
            StartCoroutine(RespawnTeacherCoroutine());
        }
    }

    private IEnumerator RespawnTeacherCoroutine()
    {
        FindRequiredComponents();
        FindSpawnPoints();
        yield return null;

        if (shouldSpawnTeacher)
        {
            SpawnTeacher();
        }
        else
        {
            MoveTeacherToBoard();
        }
    }

    [ContextMenu("🔄 Respawn Player")]
    private void ContextMenu_RespawnPlayer()
    {
        if (Application.isPlaying)
        {
            StartCoroutine(RespawnPlayerCoroutine());
        }
    }

    private IEnumerator RespawnPlayerCoroutine()
    {
        FindRequiredComponents();
        FindSpawnPoints();
        yield return null;

        if (shouldSpawnPlayer)
        {
            SpawnPlayer();
        }
        else
        {
            MovePlayerToReturnZone();
        }
    }

    [ContextMenu("📊 Show Spawn Info")]
    private void ContextMenu_ShowSpawnInfo()
    {
        Debug.Log($"=== SPAWN MANAGER INFO ===\n" +
                 $"Teacher Spawned: {teacherSpawned}\n" +
                 $"Player Spawned: {playerSpawned}\n" +
                 $"PropsSpawner: {(propsSpawner != null ? "✅" : "❌")}\n" +
                 $"LevelConfig: {(currentLevelConfig != null ? currentLevelConfig.levelName : "NULL")}\n" +
                 $"Board: {(boardObject != null ? boardObject.name : "NULL")}\n" +
                 $"ReturnZone: {(returnZoneObject != null ? returnZoneObject.name : "NULL")}\n" +
                 $"==========================");
    }
#endif
}
