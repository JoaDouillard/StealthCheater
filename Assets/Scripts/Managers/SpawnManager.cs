using UnityEngine;
using System.Collections;


public class SpawnManager : MonoBehaviour
{
    [Header("References (GameObjects désactivés dans la scène)")]
    [Tooltip("Le Teacher GameObject (doit être DÉSACTIVÉ dans la scène au départ)")]
    [SerializeField] private GameObject teacherGameObject;

    [Tooltip("Le Player GameObject (doit être DÉSACTIVÉ dans la scène au départ)")]
    [SerializeField] private GameObject playerGameObject;

    [Header("Spawn Delays")]
    [Tooltip("Nombre de frames à attendre pour que PropsSpawner finisse + NavMesh rebake")]
    [SerializeField] private int waitFrames = 2;

    [Header("Offset")]
    [Tooltip("Offset de position pour le Teacher (relatif au tableau)")]
    [SerializeField] private Vector3 teacherSpawnOffset = Vector3.zero;

    [Tooltip("Offset de position pour le Player (relatif à la ReturnZone)")]
    [SerializeField] private Vector3 playerSpawnOffset = Vector3.zero;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

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

        if (showDebugLogs) Debug.Log("[SpawnManager] Spawn...");

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

            if (navMesh.navMeshData == null && showDebugLogs)
            {
                Debug.Log("[SpawnManager] NavMesh pas prêt, continue...");
            }
        }

        // Trouver le tableau et la ReturnZone
        if (!FindSpawnPoints())
        {
            Debug.LogError("[SpawnManager] ❌ Impossible de trouver les points de spawn!");
            yield break;
        }

        // ACTIVER ET POSITIONNER LE TEACHER
        if (teacherGameObject != null)
        {
            // Activer le Teacher (il était désactivé au départ)
            teacherGameObject.SetActive(true);

            // ATTENDRE 1 FRAME pour que Unity finalise l'activation
            yield return null;

            // Le déplacer au tableau
            if (boardObject != null)
            {
                // DÉSACTIVER NavMeshAgent AVANT téléportation (sinon il corrige la position)
                UnityEngine.AI.NavMeshAgent teacherAgent = teacherGameObject.GetComponent<UnityEngine.AI.NavMeshAgent>();
                if (teacherAgent != null)
                {
                    teacherAgent.enabled = false;
                }

                // TÉLÉPORTER
                teacherGameObject.transform.position = boardObject.transform.position + teacherSpawnOffset;
                teacherGameObject.transform.rotation = boardObject.transform.rotation;

                // ATTENDRE 1 FRAME puis RÉACTIVER NavMeshAgent
                yield return null;

                if (teacherAgent != null)
                {
                    teacherAgent.enabled = true;
                }
            }
            else
            {
                Debug.LogError("[SpawnManager] ❌ boardObject est NULL! Teacher non positionné.");
            }

            teacherSpawned = true;
        }
        else
        {
            Debug.LogWarning("[SpawnManager] ⚠️ Teacher GameObject non assigné dans l'Inspector!");
        }

        // ACTIVER ET POSITIONNER LE PLAYER
        if (playerGameObject != null)
        {
            // Activer le Player (il était désactivé au départ)
            playerGameObject.SetActive(true);

            // ATTENDRE 1 FRAME pour que Unity finalise l'activation
            yield return null;

            // Le déplacer à la ReturnZone
            if (returnZoneObject != null)
            {
                // DÉSACTIVER CharacterController AVANT téléportation (sinon il corrige la position)
                CharacterController playerController = playerGameObject.GetComponent<CharacterController>();
                if (playerController != null)
                {
                    playerController.enabled = false;
                }

                // TÉLÉPORTER
                playerGameObject.transform.position = returnZoneObject.transform.position + playerSpawnOffset;
                playerGameObject.transform.rotation = returnZoneObject.transform.rotation;

                // ATTENDRE 1 FRAME puis RÉACTIVER CharacterController
                yield return null;

                if (playerController != null)
                {
                    playerController.enabled = true;
                }
            }
            else
            {
                Debug.LogError("[SpawnManager] ❌ returnZoneObject est NULL! Player non positionné.");
            }

            playerSpawned = true;
        }
        else
        {
            Debug.LogWarning("[SpawnManager] ⚠️ Player GameObject non assigné dans l'Inspector!");
        }

        if (showDebugLogs) Debug.Log($"[SpawnManager] Spawn OK: T={teacherSpawned}, P={playerSpawned}");
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

        // IMPORTANT: Chercher le tableau UNIQUEMENT dans le niveau actif (pas dans toute la scène)
        // Sinon on risque de trouver le tableau d'un autre niveau désactivé
        GameObject activeLevel = LevelSpawner.Instance?.GetActiveLevel();
        if (activeLevel == null)
        {
            Debug.LogError("[SpawnManager] ❌ Impossible de récupérer le niveau actif depuis LevelSpawner!");
            Debug.LogError("[SpawnManager] Assurez-vous qu'un LevelSpawner existe dans la scène.");
            return false;
        }

        // Chercher le board dans les enfants du niveau actif
        Transform[] allTransforms = activeLevel.GetComponentsInChildren<Transform>(true);

        boardObject = null;
        foreach (Transform t in allTransforms)
        {
            if (t.CompareTag(boardTag))
            {
                if (boardObject == null)
                {
                    boardObject = t.gameObject;
                }
            }
        }

        if (boardObject == null)
        {
            Debug.LogError($"[SpawnManager] ❌ Aucun GameObject avec le tag '{boardTag}' trouvé dans le niveau actif ({activeLevel.name})!");
            Debug.LogError($"[SpawnManager] Assurez-vous que le tableau du niveau actif a le tag '{boardTag}'.");
            return false;
        }

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

        return true;
    }

    // *** Anciennes méthodes de spawn supprimées ***
    // Le système utilise maintenant SetActive(true) sur des GameObjects désactivés
    // Voir SpawnEntitiesDelayed() pour la nouvelle implémentation propre

#if UNITY_EDITOR
    [ContextMenu("🔄 Reposition Teacher")]
    private void ContextMenu_RepositionTeacher()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[SpawnManager] Cette fonction ne marche qu'en Play Mode");
            return;
        }

        FindRequiredComponents();
        FindSpawnPoints();

        // Repositionner le Teacher au tableau
        if (teacherGameObject != null && boardObject != null)
        {
            teacherGameObject.transform.position = boardObject.transform.position + teacherSpawnOffset;
            teacherGameObject.transform.rotation = boardObject.transform.rotation;
            Debug.Log("[SpawnManager] ✅ Teacher repositionné au tableau");
        }
        else
        {
            Debug.LogWarning("[SpawnManager] ⚠️ Teacher ou Board non trouvé");
        }
    }

    [ContextMenu("🔄 Reposition Player")]
    private void ContextMenu_RepositionPlayer()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[SpawnManager] Cette fonction ne marche qu'en Play Mode");
            return;
        }

        FindRequiredComponents();
        FindSpawnPoints();

        // Repositionner le Player à la ReturnZone
        if (playerGameObject != null && returnZoneObject != null)
        {
            playerGameObject.transform.position = returnZoneObject.transform.position + playerSpawnOffset;
            playerGameObject.transform.rotation = returnZoneObject.transform.rotation;
            Debug.Log("[SpawnManager] ✅ Player repositionné à la ReturnZone");
        }
        else
        {
            Debug.LogWarning("[SpawnManager] ⚠️ Player ou ReturnZone non trouvé");
        }
    }

    [ContextMenu("📊 Show Spawn Info")]
    private void ContextMenu_ShowSpawnInfo()
    {
        Debug.Log($"=== SPAWN MANAGER INFO ===\n" +
                 $"Teacher GameObject: {(teacherGameObject != null ? teacherGameObject.name : "NULL")}\n" +
                 $"Teacher Active: {(teacherGameObject != null ? teacherGameObject.activeSelf : false)}\n" +
                 $"Player GameObject: {(playerGameObject != null ? playerGameObject.name : "NULL")}\n" +
                 $"Player Active: {(playerGameObject != null ? playerGameObject.activeSelf : false)}\n" +
                 $"PropsSpawner: {(propsSpawner != null ? "✅" : "❌")}\n" +
                 $"LevelConfig: {(currentLevelConfig != null ? currentLevelConfig.levelName : "NULL")}\n" +
                 $"Board: {(boardObject != null ? boardObject.name : "NULL")}\n" +
                 $"ReturnZone: {(returnZoneObject != null ? returnZoneObject.name : "NULL")}\n" +
                 $"Teacher Spawned: {teacherSpawned}\n" +
                 $"Player Spawned: {playerSpawned}\n" +
                 $"==========================");
    }
#endif
}
