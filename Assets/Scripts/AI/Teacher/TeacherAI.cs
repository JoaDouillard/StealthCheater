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

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();

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
        Debug.Log("[TeacherAI] Attente du NavMesh...");

        // Attendre que le NavMeshAgent soit placé sur un NavMesh
        int waitFrames = 0;
        while (!agent.isOnNavMesh && waitFrames < 100) // Max 100 frames (~3 secondes à 30fps)
        {
            waitFrames++;
            yield return null;
        }

        if (!agent.isOnNavMesh)
        {
            Debug.LogError("[TeacherAI] ❌ NavMesh non disponible après 100 frames! Vérifiez que:");
            Debug.LogError("  1. NavMeshSurface est configuré et rebake correctement");
            Debug.LogError("  2. Le Teacher est spawné sur une zone avec NavMesh");
            Debug.LogError("  3. Les mesh ont Read/Write activé (Tools → Verify Prefabs Read/Write)");
            yield break;
        }

        Debug.Log($"[TeacherAI] ✅ NavMesh prêt après {waitFrames} frame(s)");

        // Maintenant on peut initialiser
        LoadConfiguration();

        // Configurer vitesse depuis la config
        agent.speed = walkSpeed;

        // Initialiser les sub-systèmes avec la config
        movement.Initialize(agent, currentConfig);
        lookAt.Initialize(transform);
        detection.Initialize(transform, currentConfig);

        Debug.Log($"[TeacherAI] ✅ Teacher AI initialisé avec config: {currentConfig?.levelName ?? "NULL"}");

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

        Debug.Log($"[TeacherAI] ✅ Configuration chargée: {currentConfig.levelName}");
        Debug.Log($"[TeacherAI]    - Probabilités: NavMesh={randomNavMeshProbability}%, Board={boardProbability}%, Window={windowProbability}%");
        Debug.Log($"[TeacherAI]    - Vitesse de marche: {walkSpeed} m/s");
    }

    private void Update()
    {
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
            Debug.Log("[TeacherAI] Choix: Point NavMesh aléatoire");
            patrolPoint = patrol.GetNextRandomPoint(transform.position);
        }
        else if (random < randomNavMeshProbability + boardProbability && patrol.CanVisitBoard())
        {
            // Tableau (si pas déjà visité)
            Debug.Log("[TeacherAI] Choix: Tableau");
            patrolPoint = patrol.GetBoardPoint();
        }
        else if (random < randomNavMeshProbability + boardProbability + windowProbability)
        {
            // Fenêtre
            Debug.Log("[TeacherAI] Choix: Fenêtre");
            patrolPoint = patrol.GetRandomWindowPoint();
        }
        else
        {
            // Fallback vers NavMesh
            Debug.Log("[TeacherAI] Choix: Fallback vers NavMesh");
            patrolPoint = patrol.GetNextRandomPoint(transform.position);
        }

        // Sauvegarder état actuel
        currentDestinationType = patrolPoint.type;
        currentSpecialPoint = patrolPoint.interestTransform;

        // Afficher le choix final
        string destinationName = patrolPoint.type == TeacherPatrol.PointType.Board ? "TABLEAU" :
                                 patrolPoint.type == TeacherPatrol.PointType.Window ? $"FENÊTRE {patrolPoint.windowIndex}" :
                                 "NAVMESH RANDOM";
        Debug.Log($"[TeacherAI] → Destination finale: {destinationName} ({patrolPoint.position})");

        movement.GoToPoint(patrolPoint.position);
    }

    private void HandleArrival()
    {
        switch (currentDestinationType)
        {
            case TeacherPatrol.PointType.Board:
                lookAt.LookAtClassCenter();
                detection.SetDetectionActive(true);
                break;

            case TeacherPatrol.PointType.Window:
                lookAt.LookAtPointDirection(currentSpecialPoint);
                detection.SetDetectionActive(true); // Gardé actif pour détecter le player même à la fenêtre
                break;

            case TeacherPatrol.PointType.RandomNavMesh:
                // Regarder vers la moyenne des positions des Students
                Vector3 studentsCenter = GetStudentsAveragePosition();
                lookAt.LookAtPosition(studentsCenter);
                detection.SetDetectionActive(true);
                break;
        }
    }

    private Vector3 GetStudentsAveragePosition()
    {
        GameObject[] students = GameObject.FindGameObjectsWithTag("Student");

        if (students == null || students.Length == 0)
        {
            // Pas de students, regarder vers l'avant
            return transform.position + transform.forward * 5f;
        }

        Vector3 sum = Vector3.zero;
        foreach (GameObject student in students)
        {
            if (student != null)
                sum += student.transform.position;
        }

        return sum / students.Length;
    }


    private void UpdateAnimations()
    {
        if (animator == null) return;

        // Déterminer l'état réel du NavMeshAgent
        bool actuallyMoving = agent.velocity.magnitude > 0.3f && !movement.IsWaiting();
        bool shouldBeMoving = agent.hasPath && !agent.isStopped && !movement.IsWaiting();

        // Si on devrait bouger MAIS qu'on ne bouge pas encore, attendre un peu
        if (shouldBeMoving && !actuallyMoving)
        {
            // On vient juste de commencer à bouger, garder Speed à 0 encore un instant
            return;
        }

        float targetSpeed = actuallyMoving ? 1f : 0f;

        currentAnimSpeed = Mathf.Lerp(currentAnimSpeed, targetSpeed, Time.deltaTime * 8f);

        // Snap quand très proche
        if (Mathf.Abs(currentAnimSpeed - targetSpeed) < 0.05f)
            currentAnimSpeed = targetSpeed;

        animator.SetFloat("Speed", currentAnimSpeed);

        // Animation spéciale à la fenêtre
        bool isAtWindow = (currentDestinationType == TeacherPatrol.PointType.Window && movement.IsWaiting());
        animator.SetBool("IsAtWindow", isAtWindow);

        // Variante d'idle aléatoire (changée quand on arrive à destination)
        if (movement.HasReachedDestination() && !hasChosenDestination)
        {
            // Utiliser float pour BlendTree: 0.0, 0.5, 1.0 pour les 3 variantes
            int idleVariant = Random.Range(0, 3); // 0, 1 ou 2
            float idleVariantFloat = idleVariant * 0.5f; // 0→0.0, 1→0.5, 2→1.0
            animator.SetFloat("IdleVariant", idleVariantFloat);
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
