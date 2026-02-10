using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// <summary>
/// Gère l'anti-sèche permettant de voir les compétences des étudiants
/// Singleton accessible globalement
/// </summary>
public class CheatSheetManager : MonoBehaviour
{
    public static CheatSheetManager Instance { get; private set; }

    [Header("References")]
    [Tooltip("CheatSheetUI component du CheatSheetPanel (trouvé automatiquement si vide)")]
    [SerializeField] private CheatSheetUI cheatSheetUI;

    // Toggle key géré par InputSystem (Tab)

    [Header("Settings")]
    [Tooltip("L'anti-sèche est disponible dès le début")]
    [SerializeField] private bool availableAtStart = true;

    [Tooltip("Afficher les logs de debug")]
    [SerializeField] private bool showDebugLogs = false;

    private bool isAvailable = false;
    private List<StudentData> cachedStudentsList = new List<StudentData>();
    private InputSystem_Actions inputActions;

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
            return;
        }

        // Trouver CheatSheetUI si pas assigné
        if (cheatSheetUI == null)
        {
            cheatSheetUI = FindFirstObjectByType<CheatSheetUI>();

            if (cheatSheetUI == null)
            {
                Debug.LogError("[CheatSheetManager] CheatSheetUI introuvable! Assure-toi que CheatSheetPanel a le component CheatSheetUI.");
            }
        }

        // Input system
        inputActions = new InputSystem_Actions();
    }

    private void OnEnable()
    {
        if (inputActions != null)
        {
            inputActions.Player.Enable();
        }
    }

    private void OnDisable()
    {
        if (inputActions != null)
        {
            inputActions.Player.Disable();
        }
    }

    private void Start()
    {
        // Rendre disponible si configuré
        if (availableAtStart)
        {
            SetAvailable(true);
        }

        // Charger la liste des étudiants APRÈS un délai (pour attendre le spawn)
        StartCoroutine(LoadStudentsAfterSpawn());
    }

    /// <summary>
    /// Charge les Students après avoir attendu que le level soit chargé
    /// </summary>
    private System.Collections.IEnumerator LoadStudentsAfterSpawn()
    {
        Debug.Log("[CheatSheetManager] ⏳ Attente du chargement du level...");

        // Attendre quelques frames pour que LevelSpawner active le level
        yield return null;
        yield return null;
        yield return null;

        // Attendre 1 seconde pour être sûr que tout est chargé
        yield return new WaitForSeconds(1f);

        Debug.Log("[CheatSheetManager] ✅ Chargement Students maintenant");
        LoadStudentsFromScene();
    }

    private void Update()
    {
        // Vérifier input pour toggle (Tab avec InputSystem)
        // Seulement si on est en gameplay (pas dans un autre menu)
        if (isAvailable && Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
        {
            // Vérifier qu'on est en gameplay avant d'ouvrir
            if (UIManager.Instance != null && UIManager.Instance.IsGameplayActive())
            {
                ToggleCheatSheet();
            }
            // Ou si l'anti-sèche est déjà ouverte, la fermer
            else if (cheatSheetUI != null && cheatSheetUI.IsOpen())
            {
                ToggleCheatSheet();
            }
        }
    }

    /// <summary>
    /// Charge la liste des étudiants depuis la scène
    /// Utilise 3 méthodes de recherche (comme ExamManager)
    /// </summary>
    private void LoadStudentsFromScene()
    {
        cachedStudentsList.Clear();
        List<GameObject> studentObjects = new List<GameObject>();

        // MÉTHODE 1: Chercher par TAG "Student"
        try
        {
            GameObject[] taggedStudents = GameObject.FindGameObjectsWithTag("Student");
            if (taggedStudents != null && taggedStudents.Length > 0)
            {
                studentObjects.AddRange(taggedStudents);
                Debug.Log($"[CheatSheetManager] ✅ {taggedStudents.Length} Students trouvés par TAG");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[CheatSheetManager] ⚠️ Erreur lors de la recherche par tag: {e.Message}");
        }

        // MÉTHODE 2: Par component Student si rien trouvé
        if (studentObjects.Count == 0)
        {
            Debug.Log("[CheatSheetManager] 🔍 Essai par component Student...");
            Student[] studentComponents = FindObjectsByType<Student>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (Student s in studentComponents)
            {
                if (s != null && s.gameObject != null && !studentObjects.Contains(s.gameObject))
                {
                    studentObjects.Add(s.gameObject);
                }
            }

            if (studentObjects.Count > 0)
            {
                Debug.Log($"[CheatSheetManager] ✅ {studentObjects.Count} Students trouvés par COMPONENT");
            }
        }

        // MÉTHODE 3: Recherche manuelle dans Levels_Dynamic
        if (studentObjects.Count == 0)
        {
            Debug.Log("[CheatSheetManager] 🔍 Essai recherche manuelle dans Levels_Dynamic...");
            GameObject levelsDynamic = GameObject.Find("Levels_Dynamic");

            if (levelsDynamic != null)
            {
                Transform[] allChildren = levelsDynamic.GetComponentsInChildren<Transform>(true);
                foreach (Transform t in allChildren)
                {
                    if (t.name.StartsWith("Student") && !studentObjects.Contains(t.gameObject))
                    {
                        studentObjects.Add(t.gameObject);
                    }
                }

                if (studentObjects.Count > 0)
                {
                    Debug.Log($"[CheatSheetManager] ✅ {studentObjects.Count} Students trouvés dans Levels_Dynamic");
                }
            }
        }

        if (studentObjects.Count == 0)
        {
            Debug.LogError("[CheatSheetManager] ❌ AUCUN Student trouvé dans la scène !");
            Debug.LogError("[CheatSheetManager] 🔧 Vérifiez que:");
            Debug.LogError("[CheatSheetManager]    1. Les Students ont le TAG 'Student'");
            Debug.LogError("[CheatSheetManager]    2. Le level est chargé dans Levels_Dynamic");
            return;
        }

        // Extraire les StudentData de chaque Student
        foreach (GameObject studentObj in studentObjects)
        {
            CopyZone copyZone = studentObj.GetComponentInChildren<CopyZone>(true);

            if (copyZone != null)
            {
                StudentData studentData = copyZone.GetStudentData();

                if (studentData != null && !cachedStudentsList.Contains(studentData))
                {
                    cachedStudentsList.Add(studentData);
                    if (showDebugLogs)
                    {
                        Debug.Log($"[CheatSheetManager]   ✅ {studentData.studentName} ajouté");
                    }
                }
                else if (studentData == null)
                {
                    Debug.LogWarning($"[CheatSheetManager] Student '{studentObj.name}' a CopyZone mais pas de StudentData!");
                }
            }
            else
            {
                Debug.LogWarning($"[CheatSheetManager] Student '{studentObj.name}' n'a pas de CopyZone en enfant!");
            }
        }

        Debug.Log($"[CheatSheetManager] ✅ {cachedStudentsList.Count} étudiants chargés depuis la scène");
    }

    /// <summary>
    /// Ouvre/Ferme l'anti-sèche
    /// </summary>
    public void ToggleCheatSheet()
    {
        if (cheatSheetUI == null)
        {
            Debug.LogError("[CheatSheetManager] CheatSheetUI manquante!");
            return;
        }

        if (!isAvailable)
        {
            Debug.LogWarning("[CheatSheetManager] Anti-sèche pas encore disponible!");
            return;
        }

        // Recharger les étudiants si la liste est vide
        if (cachedStudentsList.Count == 0)
        {
            LoadStudentsFromScene();
        }

        // Trier par compétence dans la matière actuelle
        List<StudentData> sortedList = GetStudentsSortedByCurrentSubject();

        cheatSheetUI.ToggleCheatSheet(sortedList);
    }

    /// <summary>
    /// Retourne la liste des étudiants triés par compétence dans la matière actuelle
    /// </summary>
    private List<StudentData> GetStudentsSortedByCurrentSubject()
    {
        // Trouver l'ExamManager actif pour connaître la matière
        ExamManager examManager = FindFirstObjectByType<ExamManager>();
        SubjectType currentSubject = SubjectType.Maths; // Par défaut

        if (examManager != null)
        {
            currentSubject = examManager.GetSubjectType();
            if (showDebugLogs)
            {
                Debug.Log($"[CheatSheetManager] Tri par matière: {currentSubject}");
            }
        }

        // Copier et trier la liste par compétence décroissante
        List<StudentData> sorted = new List<StudentData>(cachedStudentsList);
        sorted.Sort((a, b) =>
        {
            float skillA = a.GetSkillForSubject(currentSubject);
            float skillB = b.GetSkillForSubject(currentSubject);
            return skillB.CompareTo(skillA); // Décroissant (meilleurs en premier)
        });

        return sorted;
    }

    /// <summary>
    /// Ouvre l'anti-sèche
    /// </summary>
    public void OpenCheatSheet()
    {
        if (cheatSheetUI == null || !isAvailable) return;

        if (cachedStudentsList.Count == 0)
        {
            LoadStudentsFromScene();
        }

        List<StudentData> sortedList = GetStudentsSortedByCurrentSubject();
        cheatSheetUI.OpenCheatSheet(sortedList);
    }

    /// <summary>
    /// Ferme l'anti-sèche
    /// </summary>
    public void CloseCheatSheet()
    {
        if (cheatSheetUI == null) return;
        cheatSheetUI.CloseCheatSheet();
    }

    /// <summary>
    /// Rend l'anti-sèche disponible ou non
    /// </summary>
    public void SetAvailable(bool available)
    {
        isAvailable = available;

        if (showDebugLogs)
        {
            Debug.Log($"[CheatSheetManager] Anti-sèche {(available ? "disponible" : "indisponible")}");
        }
    }

    /// <summary>
    /// Définit manuellement la liste des étudiants
    /// </summary>
    public void SetStudentsList(List<StudentData> students)
    {
        cachedStudentsList = new List<StudentData>(students);

        if (showDebugLogs)
        {
            Debug.Log($"[CheatSheetManager] Liste mise à jour: {students.Count} étudiants");
        }
    }

    /// <summary>
    /// Recharge la liste des étudiants depuis la scène
    /// </summary>
    public void RefreshStudentsList()
    {
        LoadStudentsFromScene();
    }

    /// <summary>
    /// Vérifie si l'anti-sèche est disponible
    /// </summary>
    public bool IsAvailable() => isAvailable;

    /// <summary>
    /// Vérifie si l'anti-sèche est ouverte
    /// </summary>
    public bool IsOpen() => cheatSheetUI != null && cheatSheetUI.IsOpen();
}
