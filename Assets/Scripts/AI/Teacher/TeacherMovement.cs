using UnityEngine;
using UnityEngine.AI;

public class TeacherMovement : MonoBehaviour
{
    [Header("Configuration (Auto-loaded)")]
    [Tooltip("Configuration chargée depuis LevelManager")]
    [SerializeField] private LevelConfiguration currentConfig;

    // Wait settings (chargés depuis config)
    private float minWaitDuration;
    private float maxWaitDuration;
    private float waitTimeBias;

    private NavMeshAgent agent;
    private bool isWaiting = false;
    private float waitTimer = 0f;
    private float currentWaitDuration = 0f;
    private bool hasReachedDestination = false;

    public void Initialize(NavMeshAgent navAgent, LevelConfiguration config)
    {
        agent = navAgent;
        currentConfig = config;

        if (currentConfig != null)
        {
            minWaitDuration = currentConfig.minWaitTime;
            maxWaitDuration = currentConfig.maxWaitTime;
            waitTimeBias = currentConfig.waitTimeBias;
            Debug.Log($"[TeacherMovement] Wait times configurés: {minWaitDuration}s - {maxWaitDuration}s (bias: {waitTimeBias})");
        }
        else
        {
            // Fallback values si pas de config
            minWaitDuration = 2f;
            maxWaitDuration = 5f;
            waitTimeBias = 1f;
            Debug.LogWarning("[TeacherMovement] Pas de config, utilisation des valeurs par défaut");
        }
    }

    public void UpdateMovement()
    {
        // Vérification de sécurité : agent doit être initialisé
        if (agent == null)
        {
            Debug.LogWarning("[TeacherMovement] ⚠️ NavMeshAgent est null! UpdateMovement() ignoré.");
            return;
        }

        // Si en attente
        if (isWaiting)
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= currentWaitDuration)
            {
                StopWaiting();
            }
            return;
        }

        // Vérifier si arrivé à destination
        if (!agent.pathPending && agent.hasPath)
        {
            if (agent.remainingDistance <= agent.stoppingDistance)
            {
                if (!hasReachedDestination)
                {
                    OnReachedDestination();
                }
            }
        }
    }

    public void GoToPoint(Vector3 destination)
    {
        // Vérifier que l'agent existe
        if (agent == null)
        {
            Debug.LogError("[TeacherMovement] ❌ NavMeshAgent est null! GoToPoint() annulé.");
            return;
        }

        // Vérifier que l'agent est sur le NavMesh avant de définir une destination
        if (!agent.isOnNavMesh)
        {
            Debug.LogWarning("[TeacherMovement] ⚠️ Impossible d'aller à destination: Agent pas sur le NavMesh!");
            return;
        }

        hasReachedDestination = false;
        isWaiting = false;
        agent.isStopped = false;

        // Vérifier que la destination est valide avant de la définir
        if (!agent.SetDestination(destination))
        {
            Debug.LogWarning($"[TeacherMovement] ⚠️ Destination invalide: {destination}");
        }
    }

    private void OnReachedDestination()
    {
        hasReachedDestination = true;
        agent.isStopped = true;

        // Commencer à attendre
        StartWaiting();
    }

    private void StartWaiting()
    {
        isWaiting = true;
        waitTimer = 0f;
        currentWaitDuration = GetWeightedWaitTime();
    }

    /// <summary>
    /// Calcule un temps d'attente avec probabilité pondérée
    /// bias < 1 = favorise temps courts
    /// bias = 1 = distribution uniforme
    /// bias > 1 = favorise temps longs (moins probable)
    /// </summary>
    private float GetWeightedWaitTime()
    {
        // Générer un nombre aléatoire entre 0 et 1
        float random = Random.value;

        // Appliquer une courbe de puissance pour biaiser la distribution
        // Plus le bias est bas, plus les valeurs basses sont probables
        float biased = Mathf.Pow(random, waitTimeBias);

        // Mapper vers la plage [min, max]
        float waitTime = Mathf.Lerp(minWaitDuration, maxWaitDuration, biased);

        return waitTime;
    }

    private void StopWaiting()
    {
        isWaiting = false;
        hasReachedDestination = false;
        agent.isStopped = false;
    }

    public bool IsWaiting() => isWaiting;
    public bool HasReachedDestination() => hasReachedDestination;
    public bool IsMoving() => agent != null && agent.velocity.sqrMagnitude > 0.1f;
    public Vector3 GetVelocity() => agent != null ? agent.velocity : Vector3.zero;

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || agent == null) return;

        if (agent.hasPath)
        {
            Gizmos.color = isWaiting ? Color.red : Color.green;
            Gizmos.DrawLine(transform.position, agent.destination);
            Gizmos.DrawSphere(agent.destination, 0.5f);
        }
    }
}
