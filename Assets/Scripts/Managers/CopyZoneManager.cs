using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Gère l'activation des CopyZones sur les étudiants spawnés
/// Active 2-4 CopyZones aléatoirement selon le nombre d'étudiants
/// S'interface avec le LevelSpawner
/// </summary>
public class CopyZoneManager : MonoBehaviour
{
    public static CopyZoneManager Instance { get; private set; }

    [Header("CopyZone Settings")]
    [Tooltip("Nombre minimum de CopyZones à activer")]
    [Range(2, 6)]
    [SerializeField] private int minCopyZones = 2;

    [Tooltip("Nombre maximum de CopyZones à activer")]
    [Range(2, 6)]
    [SerializeField] private int maxCopyZones = 4;

    [Header("Hierarchy Settings")]
    [Tooltip("Nom du GameObject parent des levels dynamiques")]
    [SerializeField] private string levelsDynamicName = "Levels_Dynamic";

    [Tooltip("Nom du GameObject contenant les props spawnés")]
    [SerializeField] private string spawnPropsName = "SpawnProps";

    [Header("Debug")]
    [Tooltip("Afficher les logs de debug")]
    [SerializeField] private bool showDebugLogs = true;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Active les CopyZones sur les premiers étudiants de la liste
    /// Cette méthode est appelée par le LevelSpawner APRÈS le spawn des étudiants
    /// </summary>
    /// <param name="spawnedStudents">Liste des étudiants spawnés (dans l'ordre de spawn)</param>
    public void ActivateCopyZones(GameObject[] spawnedStudents)
    {
        if (spawnedStudents == null || spawnedStudents.Length == 0)
        {
            Debug.LogWarning("[CopyZoneManager] Aucun étudiant fourni!");
            return;
        }

        // Déterminer combien de zones activer (2-4, mais pas plus que le nombre d'étudiants)
        int zonesToActivate = Random.Range(minCopyZones, maxCopyZones + 1);
        zonesToActivate = Mathf.Min(zonesToActivate, spawnedStudents.Length);

        if (showDebugLogs)
        {
            Debug.Log($"[CopyZoneManager] 🎲 Activation de {zonesToActivate} CopyZones sur {spawnedStudents.Length} étudiants");
        }

        // Activer les CopyZones des X premiers étudiants, désactiver les autres
        for (int i = 0; i < spawnedStudents.Length; i++)
        {
            if (spawnedStudents[i] == null)
            {
                Debug.LogWarning($"[CopyZoneManager] Étudiant #{i + 1} est null!");
                continue;
            }

            CopyZone zone = spawnedStudents[i].GetComponentInChildren<CopyZone>();

            if (zone == null)
            {
                Debug.LogWarning($"[CopyZoneManager] Étudiant {spawnedStudents[i].name} n'a pas de CopyZone!");
                continue;
            }

            bool shouldActivate = (i < zonesToActivate);

            // Activer ou désactiver le GameObject de la CopyZone
            zone.gameObject.SetActive(shouldActivate);

            if (showDebugLogs)
            {
                string studentName = zone.GetStudentData()?.studentName ?? spawnedStudents[i].name;
                if (shouldActivate)
                {
                    Debug.Log($"[CopyZoneManager] ✅ CopyZone activée pour {studentName} (position {i + 1})");
                }
                else
                {
                    Debug.Log($"[CopyZoneManager] ❌ CopyZone désactivée pour {studentName} (position {i + 1})");
                }
            }
        }

        if (showDebugLogs)
        {
            Debug.Log($"[CopyZoneManager] 🎯 {zonesToActivate} CopyZones activées avec succès!");
        }
    }

    /// <summary>
    /// Active les CopyZones automatiquement en cherchant tous les Students avec tag
    /// ALTERNATIVE si le LevelSpawner ne peut pas passer la liste
    /// </summary>
    public void ActivateCopyZonesAuto()
    {
        // Trouver tous les GameObjects avec tag "Student"
        GameObject[] allStudents = GameObject.FindGameObjectsWithTag("Student");

        if (allStudents.Length == 0)
        {
            Debug.LogWarning("[CopyZoneManager] Aucun Student trouvé avec le tag 'Student'!");
            return;
        }

        if (showDebugLogs)
        {
            Debug.Log($"[CopyZoneManager] 🔍 {allStudents.Length} Students trouvés automatiquement");
        }

        // Appeler la méthode principale
        ActivateCopyZones(allStudents);
    }

    /// <summary>
    /// Active les CopyZones en cherchant dans la hiérarchie Levels_Dynamic/LevelX_Props/SpawnProps
    /// NOUVELLE MÉTHODE - Parcourt TOUS les objets de SpawnProps et cherche les Students
    /// Évite les doublons basés sur le NOM du Student (Student 0, Student 1, etc.)
    /// Sélectionne aléatoirement X Students uniques et active leurs CopyZones
    /// </summary>
    public void ActivateCopyZonesFromDynamicLevel()
    {
        // 1. Trouver Levels_Dynamic
        GameObject levelsDynamic = GameObject.Find(levelsDynamicName);

        if (levelsDynamic == null)
        {
            Debug.LogError($"[CopyZoneManager] ❌ GameObject '{levelsDynamicName}' introuvable dans la scène!");
            return;
        }

        // 2. Trouver le level actif parmi les enfants
        GameObject activeLevel = null;

        foreach (Transform child in levelsDynamic.transform)
        {
            if (child.gameObject.activeInHierarchy && child.name.Contains("Level") && child.name.Contains("Props"))
            {
                activeLevel = child.gameObject;
                break;
            }
        }

        if (activeLevel == null)
        {
            Debug.LogError($"[CopyZoneManager] ❌ Aucun level actif trouvé dans '{levelsDynamicName}'!");
            return;
        }

        if (showDebugLogs)
        {
            Debug.Log($"[CopyZoneManager] 🔍 Level actif trouvé: {activeLevel.name}");
        }

        // 3. Trouver SpawnProps dans le level actif
        Transform spawnProps = activeLevel.transform.Find(spawnPropsName);

        if (spawnProps == null)
        {
            Debug.LogError($"[CopyZoneManager] ❌ '{spawnPropsName}' introuvable dans {activeLevel.name}!");
            return;
        }

        if (showDebugLogs)
        {
            Debug.Log($"[CopyZoneManager] ✅ SpawnProps trouvé avec {spawnProps.childCount} enfants");
        }

        // 4. Parcourir TOUS les enfants de SpawnProps et chercher les Students récursivement
        // Utiliser un dictionnaire pour éviter les doublons basés sur le NOM du Student
        Dictionary<string, CopyZone> uniqueStudentCopyZones = new Dictionary<string, CopyZone>();

        foreach (Transform child in spawnProps)
        {
            // Chercher TOUS les GameObjects "Student" dans cet enfant (récursivement)
            Transform[] allStudents = child.GetComponentsInChildren<Transform>();

            foreach (Transform potentialStudent in allStudents)
            {
                // Vérifier si c'est un GameObject Student (nom contient "Student")
                if (potentialStudent.name.Contains("Student"))
                {
                    // Chercher la CopyZone dans ce Student
                    CopyZone copyZone = potentialStudent.GetComponentInChildren<CopyZone>();

                    if (copyZone != null)
                    {
                        // Utiliser le NOM du Student comme clé unique
                        string studentName = potentialStudent.name;

                        // Si ce Student n'a pas déjà été ajouté (éviter doublons)
                        if (!uniqueStudentCopyZones.ContainsKey(studentName))
                        {
                            uniqueStudentCopyZones.Add(studentName, copyZone);

                            if (showDebugLogs)
                            {
                                string dataName = copyZone.GetStudentData()?.studentName ?? studentName;
                                Debug.Log($"[CopyZoneManager] 📌 Student trouvé: {studentName} (Data: {dataName}) dans {child.name}");
                            }
                        }
                        else if (showDebugLogs)
                        {
                            Debug.LogWarning($"[CopyZoneManager] ⚠️ Doublon ignoré: {studentName} déjà dans la liste");
                        }
                    }
                }
            }
        }

        if (uniqueStudentCopyZones.Count == 0)
        {
            Debug.LogWarning("[CopyZoneManager] ❌ Aucun Student avec CopyZone trouvé dans SpawnProps!");
            return;
        }

        if (showDebugLogs)
        {
            Debug.Log($"[CopyZoneManager] ✅ {uniqueStudentCopyZones.Count} Students uniques trouvés");
        }

        // 5. Convertir le dictionnaire en liste pour pouvoir mélanger
        List<CopyZone> allUniqueCopyZones = new List<CopyZone>(uniqueStudentCopyZones.Values);

        // 6. Mélanger aléatoirement la liste (Shuffle)
        for (int i = allUniqueCopyZones.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            CopyZone temp = allUniqueCopyZones[i];
            allUniqueCopyZones[i] = allUniqueCopyZones[randomIndex];
            allUniqueCopyZones[randomIndex] = temp;
        }

        // 7. Déterminer combien de zones activer (2-4, mais pas plus que le nombre disponible)
        int zonesToActivate = Random.Range(minCopyZones, maxCopyZones + 1);
        zonesToActivate = Mathf.Min(zonesToActivate, allUniqueCopyZones.Count);

        if (showDebugLogs)
        {
            Debug.Log($"[CopyZoneManager] 🎲 Activation de {zonesToActivate} CopyZones sur {allUniqueCopyZones.Count} Students uniques");
        }

        // 8. D'abord, DÉSACTIVER TOUTES les CopyZones
        foreach (CopyZone zone in allUniqueCopyZones)
        {
            zone.gameObject.SetActive(false);
        }

        // 9. Activer UNIQUEMENT les X premières CopyZones (après mélange aléatoire)
        for (int i = 0; i < zonesToActivate; i++)
        {
            allUniqueCopyZones[i].gameObject.SetActive(true);

            if (showDebugLogs)
            {
                string studentName = allUniqueCopyZones[i].GetStudentData()?.studentName ?? $"Student {i}";
                Debug.Log($"[CopyZoneManager] ✅ CopyZone activée pour {studentName}");
            }
        }

        if (showDebugLogs)
        {
            Debug.Log($"[CopyZoneManager] 🎯 {zonesToActivate} CopyZones activées avec succès (sur {allUniqueCopyZones.Count} Students)!");
        }
    }

    /// <summary>
    /// Désactive toutes les CopyZones (utile pour reset)
    /// </summary>
    public void DeactivateAllCopyZones()
    {
        CopyZone[] allZones = FindObjectsOfType<CopyZone>();

        foreach (CopyZone zone in allZones)
        {
            zone.gameObject.SetActive(false);
        }

        if (showDebugLogs)
        {
            Debug.Log($"[CopyZoneManager] ❌ Toutes les CopyZones désactivées ({allZones.Length})");
        }
    }

    /// <summary>
    /// Obtient le nombre de CopyZones actuellement actives
    /// </summary>
    public int GetActiveCopyZoneCount()
    {
        CopyZone[] allZones = FindObjectsOfType<CopyZone>();
        int activeCount = 0;

        foreach (CopyZone zone in allZones)
        {
            if (zone.gameObject.activeInHierarchy)
            {
                activeCount++;
            }
        }

        return activeCount;
    }
}
