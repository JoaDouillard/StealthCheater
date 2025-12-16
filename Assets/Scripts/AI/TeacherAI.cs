using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Intelligence artificielle du professeur : patrouille et détection
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class TeacherAI : MonoBehaviour
{
    [Header("Patrol Settings")]
    [SerializeField] private Transform[] waypoints; // Points de patrouille
    [SerializeField] private float waypointWaitTime = 2f; // Temps d'attente à chaque waypoint
    [SerializeField] private bool randomPatrol = false; // Patrouille aléatoire ou séquentielle

    [Header("Detection Settings")]
    [SerializeField] private float detectionRange = 8f; // Distance de détection
    [SerializeField] private float detectionAngle = 90f; // Angle du champ de vision (90° = 45° de chaque côté)
    [SerializeField] private LayerMask obstacleLayer; // Layer des obstacles (tables, etc.)
    [SerializeField] private LayerMask playerLayer; // Layer du joueur

    [Header("Visualization")]
    [SerializeField] private bool showDetectionGizmos = true;
    [SerializeField] private Color detectionColor = Color.red;

    // Composants
    private NavMeshAgent agent;
    private Animator animator;

    // Variables de patrouille
    private int currentWaypointIndex = 0;
    private float waypointTimer = 0f;
    private bool isWaiting = false;

    // Détection
    private Transform player;
    private bool hasDetectedPlayer = false;

    #region Unity Lifecycle

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();

        // Trouver le joueur
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        else
        {
            Debug.LogWarning("[TeacherAI] Aucun joueur trouvé ! Tag le joueur avec 'Player'");
        }
    }

    private void Start()
    {
        // Démarrer la patrouille si on a des waypoints
        if (waypoints != null && waypoints.Length > 0)
        {
            GoToNextWaypoint();
        }
        else
        {
            Debug.LogWarning("[TeacherAI] Aucun waypoint assigné ! Le professeur ne patrouillera pas.");
        }
    }

    private void Update()
    {
        if (hasDetectedPlayer) return; // Si joueur détecté, on arrête tout

        // Patrouille
        HandlePatrol();

        // Détection du joueur
        DetectPlayer();

        // Animations
        UpdateAnimations();
    }

    #endregion

    #region Patrol

    /// <summary>
    /// Gère la logique de patrouille
    /// </summary>
    private void HandlePatrol()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        // Si on attend à un waypoint
        if (isWaiting)
        {
            waypointTimer += Time.deltaTime;

            if (waypointTimer >= waypointWaitTime)
            {
                isWaiting = false;
                waypointTimer = 0f;
                GoToNextWaypoint();
            }
        }
        // Si on est arrivé au waypoint
        else if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
            {
                // On est arrivé, on attend
                isWaiting = true;
            }
        }
    }

    /// <summary>
    /// Va au prochain waypoint
    /// </summary>
    private void GoToNextWaypoint()
    {
        if (waypoints.Length == 0) return;

        // Choisir le prochain waypoint
        if (randomPatrol)
        {
            currentWaypointIndex = Random.Range(0, waypoints.Length);
        }
        else
        {
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
        }

        // Aller vers le waypoint
        if (waypoints[currentWaypointIndex] != null)
        {
            agent.SetDestination(waypoints[currentWaypointIndex].position);
        }
    }

    #endregion

    #region Detection

    /// <summary>
    /// Détecte le joueur dans le champ de vision
    /// </summary>
    private void DetectPlayer()
    {
        if (player == null) return;

        // 1. Vérifier la distance
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer > detectionRange) return;

        // 2. Vérifier l'angle (champ de vision)
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);

        if (angleToPlayer > detectionAngle / 2f) return; // Pas dans le champ de vision

        // 3. Vérifier s'il n'y a pas d'obstacle (Raycast)
        RaycastHit hit;
        Vector3 rayOrigin = transform.position + Vector3.up * 1f; // Légèrement en hauteur
        Vector3 rayDirection = (player.position - rayOrigin).normalized;

        if (Physics.Raycast(rayOrigin, rayDirection, out hit, detectionRange, obstacleLayer | playerLayer))
        {
            // Si on touche le joueur
            if (hit.collider.CompareTag("Player"))
            {
                OnPlayerDetected();
            }
        }
    }

    /// <summary>
    /// Appelé quand le joueur est détecté
    /// </summary>
    private void OnPlayerDetected()
    {
        if (hasDetectedPlayer) return; // Éviter les appels multiples

        hasDetectedPlayer = true;
        agent.isStopped = true;

        Debug.Log("[TeacherAI] JOUEUR DÉTECTÉ !");

        // Regarder vers le joueur
        if (player != null)
        {
            transform.LookAt(new Vector3(player.position.x, transform.position.y, player.position.z));
        }

        // Notifier le GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPlayerDetected();
        }
    }

    #endregion

    #region Animation

    /// <summary>
    /// Met à jour les animations du professeur
    /// </summary>
    private void UpdateAnimations()
    {
        if (animator == null) return;

        // Vitesse pour Blend Tree (Idle/Walk)
        float speed = agent.velocity.magnitude / agent.speed; // Normalisé entre 0 et 1
        animator.SetFloat("Speed", speed);
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Définit les waypoints de patrouille
    /// </summary>
    public void SetWaypoints(Transform[] newWaypoints)
    {
        waypoints = newWaypoints;
        currentWaypointIndex = 0;
        
        if (waypoints.Length > 0)
        {
            GoToNextWaypoint();
        }
    }

    #endregion

    #region Debug Visualization

    private void OnDrawGizmos()
    {
        if (!showDetectionGizmos) return;

        // Visualiser le champ de vision
        Gizmos.color = detectionColor;

        Vector3 forward = transform.forward * detectionRange;
        Vector3 leftBoundary = Quaternion.Euler(0, -detectionAngle / 2f, 0) * forward;
        Vector3 rightBoundary = Quaternion.Euler(0, detectionAngle / 2f, 0) * forward;

        Gizmos.DrawLine(transform.position, transform.position + leftBoundary);
        Gizmos.DrawLine(transform.position, transform.position + rightBoundary);

        // Arc du champ de vision
        int segments = 20;
        float angleStep = detectionAngle / segments;
        Vector3 previousPoint = transform.position + leftBoundary;

        for (int i = 1; i <= segments; i++)
        {
            float currentAngle = -detectionAngle / 2f + angleStep * i;
            Vector3 direction = Quaternion.Euler(0, currentAngle, 0) * transform.forward * detectionRange;
            Vector3 point = transform.position + direction;

            Gizmos.DrawLine(previousPoint, point);
            previousPoint = point;
        }

        // Waypoints
        if (waypoints != null && waypoints.Length > 0)
        {
            Gizmos.color = Color.cyan;
            foreach (Transform waypoint in waypoints)
            {
                if (waypoint != null)
                {
                    Gizmos.DrawWireSphere(waypoint.position, 0.5f);
                }
            }

            // Lignes entre waypoints (si patrouille séquentielle)
            if (!randomPatrol)
            {
                for (int i = 0; i < waypoints.Length; i++)
                {
                    if (waypoints[i] != null && waypoints[(i + 1) % waypoints.Length] != null)
                    {
                        Gizmos.DrawLine(waypoints[i].position, waypoints[(i + 1) % waypoints.Length].position);
                    }
                }
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Sphere de détection
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }

    #endregion
}
