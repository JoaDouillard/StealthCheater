using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(TeacherMovement))]
[RequireComponent(typeof(TeacherLookAt))]
[RequireComponent(typeof(TeacherDetection))]
[RequireComponent(typeof(TeacherPatrol))]
public class TeacherAI : MonoBehaviour
{
    [Header("Configuration (Auto-loaded from LevelManager)")]
    [Tooltip("Configuration actuelle chargée depuis LevelManager")]
    [SerializeField] private LevelConfiguration currentConfig;

    // Probabilités (chargées depuis config)
    private float randomNavMeshProbability;
    private float boardProbability;
    private float windowProbability;
    private float walkSpeed;

    private NavMeshAgent agent;
    private Animator animator;

    private TeacherMovement movement;
    private TeacherLookAt lookAt;
    private TeacherDetection detection;
    private TeacherPatrol patrol;

    private TeacherPatrol.PointType currentDestinationType;
    private Transform currentSpecialPoint;
    private bool hasChosenDestination = false;

    private float currentAnimSpeed = 0f;
    private bool idleVariantSet = false; // FIX: Évite de changer IdleVariant à chaque frame

    // Flag pour empêcher Update() de tourner avant l'initialisation complète
    private bool isInitialized = false;

    // CACHE: Students trouvés une seule fois (FIX MEMORY LEAK - évite FindGameObjectsWithTag répété)
    private GameObject[] cachedStudents;
    // CACHE: Position moyenne calculée UNE FOIS (élèves ne bougent jamais pendant le niveau)
    private Vector3 cachedStudentsCenter;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();

        // NOTE: Le GameObject Teacher est désactivé au départ et activé par SpawnManager après le NavMesh bake
        // Plus besoin de disable/enable le NavMeshAgent component

        // Trouver les scripts
        movement = GetComponent<TeacherMovement>();
        lookAt = GetComponent<TeacherLookAt>();
        detection = GetComponent<TeacherDetection>();
        patrol = GetComponent<TeacherPatrol>();

        // Vérifications
        if (movement == null)
        {
            Debug.LogError("[TeacherAI] TeacherMovement manquant! Ajoute-le manuellement.");
            return;
        }
        if (lookAt == null)
        {
            Debug.LogError("[TeacherAI] TeacherLookAt manquant! Ajoute-le manuellement.");
            return;
        }
        if (detection == null)
        {
            Debug.LogError("[TeacherAI] TeacherDetection manquant! Ajoute-le manuellement.");
            return;
        }
        if (patrol == null)
        {
            Debug.LogError("[TeacherAI] TeacherPatrol manquant! Ajoute-le manuellement.");
            return;
        }
    }

    private void Start()
    {
        // Attendre que le NavMesh soit prêt avant de commencer la patrouille
        StartCoroutine(InitializeWhenNavMeshReady());
    }

    /// <summary>
    /// Attend que le NavMesh soit prêt avant d'initialiser le Teacher
    /// </summary>
    private System.Collections.IEnumerator InitializeWhenNavMeshReady()
    {
        // NOTE: Le GameObject est activé PAR SpawnManager APRÈS que le NavMesh soit baké
        // Donc normalement le NavMesh devrait être prêt dès qu'on arrive ici

        // Attendre 1 frame pour que le NavMeshAgent s'initialise correctement
        yield return null;

        // Vérifier que le NavMeshAgent est placé sur un NavMesh
        int waitFrames = 0;
        while (agent != null && !agent.isOnNavMesh && waitFrames < 30) // Max 30 frames (~1 seconde à 30fps)
        {
            waitFrames++;
            yield return null;
        }

        if (agent == null || !agent.isOnNavMesh)
        {
            Debug.LogError("[TeacherAI] ❌ NavMesh non disponible! Vérifiez que:");
            Debug.LogError("  1. NavMeshSurface est configuré et rebake correctement");
            Debug.LogError("  2. Le Teacher GameObject est désactivé au départ");
            Debug.LogError("  3. SpawnManager active le Teacher APRÈS le bake");
            Debug.LogError("  4. Le layer 'LevelFloor' est bien assigné au sol");
            yield break;
        }

        // Maintenant on peut initialiser
        LoadConfiguration();

        // Configurer vitesse depuis la config
        agent.speed = walkSpeed;

        // Initialiser les sub-systèmes avec la config
        movement.Initialize(agent, currentConfig);
        lookAt.Initialize(transform);
        detection.Initialize(transform, currentConfig);

        // CACHE les Students UNE SEULE FOIS (FIX MEMORY LEAK)
        System.Collections.Generic.List<GameObject> studentsList = new System.Collections.Generic.List<GameObject>();

        // Méthode 1: Par TAG
        try
        {
            GameObject[] taggedStudents = GameObject.FindGameObjectsWithTag("Student");
            if (taggedStudents != null && taggedStudents.Length > 0)
            {
                studentsList.AddRange(taggedStudents);
                Debug.Log($"[TeacherAI] ✅ {taggedStudents.Length} Students trouvés par TAG");
            }
        }
        catch { }

        // Méthode 2: Par component si rien trouvé
        if (studentsList.Count == 0)
        {
            Student[] studentComponents = FindObjectsByType<Student>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Student s in studentComponents)
            {
                if (s != null) studentsList.Add(s.gameObject);
            }
        }

        // Méthode 3: Recherche manuelle dans Levels_Dynamic
        if (studentsList.Count == 0)
        {
            GameObject levelsDynamic = GameObject.Find("Levels_Dynamic");
            if (levelsDynamic != null)
            {
                Transform[] allChildren = levelsDynamic.GetComponentsInChildren<Transform>(true);
                foreach (Transform t in allChildren)
                {
                    if (t.name.StartsWith("Student"))
                    {
                        studentsList.Add(t.gameObject);
                    }
                }
            }
        }

        cachedStudents = studentsList.ToArray();
        Debug.Log($"[TeacherAI] ✅ {cachedStudents.Length} Students trouvés pour le cache");

        // CALCULER position moyenne des élèves UNE SEULE FOIS (élèves ne bougent jamais)
        CalculateStudentsCenterOnce();

        // Marquer comme initialisé pour permettre à Update() de tourner
        isInitialized = true;

        // Commencer la patrouille
        ChooseAndGoToDestination();
    }

    /// <summary>
    /// Charge la configuration depuis le LevelManager
    /// </summary>
    private void LoadConfiguration()
    {
        if (LevelManager.Instance == null)
        {
            Debug.LogError("[TeacherAI] LevelManager.Instance est NULL! Assurez-vous qu'il y a un LevelManager dans la scène.");
            return;
        }

        currentConfig = LevelManager.Instance.GetCurrentConfiguration();

        if (currentConfig == null)
        {
            Debug.LogError("[TeacherAI] Impossible de charger la configuration du niveau!");
            return;
        }

        // Charger les valeurs depuis la config
        randomNavMeshProbability = currentConfig.randomNavMeshProbability;
        boardProbability = currentConfig.boardProbability;
        windowProbability = currentConfig.windowProbability;
        walkSpeed = currentConfig.walkSpeed;
    }

    private void Update()
    {
        // Ne rien faire tant que le Teacher n'est pas complètement initialisé
        if (!isInitialized)
        {
            return;
        }

        movement.UpdateMovement();

        // Pendant le mouvement : regarder direction
        if (movement.IsMoving())
        {
            lookAt.LookInMovementDirection(movement.GetVelocity());
            detection.SetDetectionActive(true);
            hasChosenDestination = true;
        }
        // Arrivé à destination (une seule fois)
        else if (movement.IsWaiting() && hasChosenDestination)
        {
            HandleArrival();
            hasChosenDestination = false;
        }
        // Attente terminée - choisir nouvelle destination
        else if (!movement.IsWaiting() && !movement.IsMoving() && !hasChosenDestination)
        {
            ChooseAndGoToDestination();
        }

        detection.UpdateDetection();
        UpdateAnimations();
    }

    private void ChooseAndGoToDestination()
    {
        TeacherPatrol.PatrolPoint patrolPoint;

        float random = Random.Range(0f, 100f);

        // Déterminer le type de destination selon les probabilités
        if (random < randomNavMeshProbability)
        {
            // NavMesh aléatoire (avec snap automatique)
            patrolPoint = patrol.GetNextRandomPoint(transform.position);
        }
        else if (random < randomNavMeshProbability + boardProbability && patrol.CanVisitBoard())
        {
            // Tableau (si pas déjà visité)
            patrolPoint = patrol.GetBoardPoint();
        }
        else if (random < randomNavMeshProbability + boardProbability + windowProbability)
        {
            // Fenêtre
            patrolPoint = patrol.GetRandomWindowPoint();
        }
        else
        {
            // Fallback vers NavMesh
            patrolPoint = patrol.GetNextRandomPoint(transform.position);
        }

        // Sauvegarder état actuel
        currentDestinationType = patrolPoint.type;
        currentSpecialPoint = patrolPoint.interestTransform;

        movement.GoToPoint(patrolPoint.position);
    }

    private void HandleArrival()
    {
        switch (currentDestinationType)
        {
            case TeacherPatrol.PointType.Board:
                // Tableau : regarder vers les élèves (position moyenne cachée)
                lookAt.LookAtPosition(cachedStudentsCenter);
                detection.SetDetectionActive(true);
                break;

            case TeacherPatrol.PointType.Window:
                // Fenêtre : regarder PAR la fenêtre (pas vers les élèves)
                lookAt.LookAtPointDirection(currentSpecialPoint);
                detection.SetDetectionActive(true); // Gardé actif pour détecter le player même à la fenêtre
                break;

            case TeacherPatrol.PointType.RandomNavMesh:
                // NavMesh aléatoire : regarder vers les élèves (position moyenne cachée)
                lookAt.LookAtPosition(cachedStudentsCenter);
                detection.SetDetectionActive(true);
                break;
        }
    }

    /// <summary>
    /// Calcule la position moyenne des élèves UNE SEULE FOIS à l'initialisation
    /// Les élèves ne bougent JAMAIS pendant le niveau, donc pas besoin de recalculer
    /// </summary>
    private void CalculateStudentsCenterOnce()
    {
        if (cachedStudents == null || cachedStudents.Length == 0)
        {
            // Pas de students, regarder vers l'avant
            cachedStudentsCenter = transform.position + transform.forward * 5f;
            Debug.LogWarning("[TeacherAI] Aucun Student trouvé, direction par défaut utilisée");
            return;
        }

        Vector3 sum = Vector3.zero;
        int validStudents = 0;

        foreach (GameObject student in cachedStudents)
        {
            if (student != null)
            {
                sum += student.transform.position;
                validStudents++;
            }
        }

        if (validStudents > 0)
        {
            cachedStudentsCenter = sum / validStudents;
            Debug.Log($"[TeacherAI] Position moyenne des {validStudents} élèves calculée: {cachedStudentsCenter}");
        }
        else
        {
            cachedStudentsCenter = transform.position + transform.forward * 5f;
            Debug.LogWarning("[TeacherAI] Aucun Student valide, direction par défaut utilisée");
        }
    }


    private void UpdateAnimations()
    {
        if (animator == null) return;

        // FIX: Déterminer l'état de mouvement de manière plus précise
        bool actuallyMoving = agent != null && agent.velocity.sqrMagnitude > 0.01f && !movement.IsWaiting();

        // Transition rapide si vraiment en mouvement, sinon graduelle
        float lerpSpeed = actuallyMoving ? 10f : 5f;
        float targetSpeed = actuallyMoving ? 1f : 0f;

        currentAnimSpeed = Mathf.Lerp(currentAnimSpeed, targetSpeed, Time.deltaTime * lerpSpeed);

        // Snap quand très proche (évite valeurs intermédiaires)
        if (Mathf.Abs(currentAnimSpeed - targetSpeed) < 0.05f)
        {
            currentAnimSpeed = targetSpeed;
        }

        animator.SetFloat("Speed", currentAnimSpeed);

        // Animation spéciale à la fenêtre
        bool isAtWindow = (currentDestinationType == TeacherPatrol.PointType.Window && movement.IsWaiting());
        animator.SetBool("IsAtWindow", isAtWindow);

        // FIX: Variante d'idle aléatoire (changée UNE SEULE FOIS quand on arrive)
        if (movement.HasReachedDestination() && !hasChosenDestination && !idleVariantSet)
        {
            // Utiliser float pour BlendTree: 0.0, 0.5, 1.0 pour les 3 variantes
            int idleVariant = Random.Range(0, 3); // 0, 1 ou 2
            float idleVariantFloat = idleVariant * 0.5f; // 0→0.0, 1→0.5, 2→1.0
            animator.SetFloat("IdleVariant", idleVariantFloat);
            idleVariantSet = true; // Marquer comme défini
        }

        // Reset le flag quand on commence à bouger
        if (actuallyMoving && idleVariantSet)
        {
            idleVariantSet = false;
        }
    }

    public void PlayScoldAnimation(int variant)
    {
        if (animator == null) return;

        // Définir la variante avant de trigger
        animator.SetInteger("ScoldVariant", variant);
        animator.SetTrigger("Scold");
    }
}
