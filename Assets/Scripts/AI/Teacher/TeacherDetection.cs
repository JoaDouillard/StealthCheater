using UnityEngine;

/// <summary>
/// Système de détection multi-zones pour le Teacher
///
/// 3 zones de détection avec temps différents:
/// - Zone 1 (8m) : Détection lente (5s)
/// - Zone 2 (6m) : Détection moyenne (3s)
/// - Zone 3 (2m) : Détection immédiate (0s)
///
/// Le crouch réduit les distances de toutes les zones (-25% par défaut)
/// </summary>
public class TeacherDetection : MonoBehaviour
{
    [Header("Configuration (Auto-loaded)")]
    [Tooltip("Configuration chargée depuis LevelManager")]
    [SerializeField] private LevelConfiguration currentConfig;

    [Header("Scene Settings")]
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private LayerMask playerLayer;

    [Header("Visualization")]
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private Color zone1Color = new Color(1f, 1f, 0f, 0.2f); // Jaune
    [SerializeField] private Color zone2Color = new Color(1f, 0.5f, 0f, 0.3f); // Orange
    [SerializeField] private Color zone3Color = new Color(1f, 0f, 0f, 0.4f); // Rouge
    [SerializeField] private Color detectionProgressColor = Color.red;

    // Detection settings (chargés depuis config)
    private float fieldOfViewAngle;
    private float zone1MaxDistance;
    private float zone1DetectionTime;
    private float zone2MaxDistance;
    private float zone2DetectionTime;
    private float zone3MaxDistance;
    private float zone3DetectionTime;
    private float crouchDistanceModifier;

    private Transform teacherTransform;
    private Transform player;
    private PlayerController playerController;
    private bool detectionActive = true;
    private bool hasDetectedPlayer = false;

    // État détection
    private bool playerInSight = false;
    private float detectionTimer = 0f;
    private float currentRequiredTime = 0f;
    private int currentZone = 0; // 0 = hors zone, 1/2/3 = zone active

    public void Initialize(Transform transform, LevelConfiguration config)
    {
        teacherTransform = transform;
        currentConfig = config;

        // Charger les valeurs depuis la config
        if (currentConfig != null)
        {
            fieldOfViewAngle = currentConfig.fieldOfViewAngle;
            zone1MaxDistance = currentConfig.zone1MaxDistance;
            zone1DetectionTime = currentConfig.zone1DetectionTime;
            zone2MaxDistance = currentConfig.zone2MaxDistance;
            zone2DetectionTime = currentConfig.zone2DetectionTime;
            zone3MaxDistance = currentConfig.zone3MaxDistance;
            zone3DetectionTime = currentConfig.zone3DetectionTime;
            crouchDistanceModifier = currentConfig.crouchDistanceModifier;

            Debug.Log($"[TeacherDetection] Configuration chargée:");
            Debug.Log($"   - Zone 1: {zone1MaxDistance}m / {zone1DetectionTime}s");
            Debug.Log($"   - Zone 2: {zone2MaxDistance}m / {zone2DetectionTime}s");
            Debug.Log($"   - Zone 3: {zone3MaxDistance}m / {zone3DetectionTime}s");
            Debug.Log($"   - FOV: {fieldOfViewAngle}°");
            Debug.Log($"   - Crouch modifier: {crouchDistanceModifier}");
        }
        else
        {
            // Fallback values
            fieldOfViewAngle = 90f;
            zone1MaxDistance = 8f;
            zone1DetectionTime = 5f;
            zone2MaxDistance = 6f;
            zone2DetectionTime = 3f;
            zone3MaxDistance = 2f;
            zone3DetectionTime = 0f;
            crouchDistanceModifier = 0.75f;
            Debug.LogWarning("[TeacherDetection] Pas de config, utilisation des valeurs par défaut");
        }

        // Trouver joueur
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerController = playerObj.GetComponent<PlayerController>();
            Debug.Log($"[TeacherDetection] Player trouvé: {player.name}");
        }
        else
        {
            Debug.LogError("[TeacherDetection] PLAYER NON TROUVÉ! Vérifie le tag 'Player'");
        }
    }

    public void SetDetectionActive(bool active)
    {
        detectionActive = active;

        if (!active)
        {
            // Reset détection si désactivée
            playerInSight = false;
            detectionTimer = 0f;
            currentZone = 0;
        }
    }

    public void UpdateDetection()
    {
        if (!detectionActive)
        {
            playerInSight = false;
            detectionTimer = 0f;
            return;
        }

        if (player == null || hasDetectedPlayer)
        {
            return;
        }

        // Vérifier si le player est dans le champ de vision
        bool wasInSight = playerInSight;
        playerInSight = IsPlayerInFieldOfView();

        if (playerInSight)
        {
            // Player dans le champ de vision
            if (!wasInSight)
            {
                // Vient d'entrer dans le champ de vision
                Debug.Log($"[TeacherDetection] Player détecté dans Zone {currentZone} (temps requis: {currentRequiredTime}s)");
            }

            // Incrémenter le timer
            detectionTimer += Time.deltaTime;

            // Vérifier si détection complétée
            if (detectionTimer >= currentRequiredTime)
            {
                OnPlayerDetected();
            }
        }
        else
        {
            // Player hors du champ de vision
            if (wasInSight)
            {
                Debug.Log("[TeacherDetection] Player sorti du champ de vision");
            }

            // Reset le timer
            detectionTimer = 0f;
            currentZone = 0;
        }
    }

    private bool IsPlayerInFieldOfView()
    {
        Vector3 directionToPlayer = player.position - teacherTransform.position;
        float distanceToPlayer = directionToPlayer.magnitude;

        // Vérifier l'angle (FOV)
        float angle = Vector3.Angle(teacherTransform.forward, directionToPlayer);
        if (angle > fieldOfViewAngle / 2f)
        {
            return false; // Hors du champ de vision
        }

        // Déterminer si le player est accroupi
        bool isPlayerCrouching = false;
        if (playerController != null)
        {
            isPlayerCrouching = playerController.IsCrouching();
        }

        // Calculer les distances effectives (réduites si crouch)
        float distanceModifier = isPlayerCrouching ? crouchDistanceModifier : 1f;
        float effectiveZone1Dist = zone1MaxDistance * distanceModifier;
        float effectiveZone2Dist = zone2MaxDistance * distanceModifier;
        float effectiveZone3Dist = zone3MaxDistance * distanceModifier;

        // Déterminer dans quelle zone le player se trouve
        int detectedZone = 0;
        float requiredTime = 0f;

        if (distanceToPlayer <= effectiveZone3Dist)
        {
            // Zone 3 (proche) - Détection immédiate
            detectedZone = 3;
            requiredTime = zone3DetectionTime;
        }
        else if (distanceToPlayer <= effectiveZone2Dist)
        {
            // Zone 2 (moyenne) - Détection moyenne
            detectedZone = 2;
            requiredTime = zone2DetectionTime;
        }
        else if (distanceToPlayer <= effectiveZone1Dist)
        {
            // Zone 1 (lointaine) - Détection lente
            detectedZone = 1;
            requiredTime = zone1DetectionTime;
        }
        else
        {
            // Hors de toute zone
            return false;
        }

        // Update zone actuelle et temps requis
        if (currentZone != detectedZone)
        {
            // Changement de zone - reset timer
            detectionTimer = 0f;
            currentZone = detectedZone;
            currentRequiredTime = requiredTime;
        }

        // Vérifier les obstacles (raycast)
        if (Physics.Raycast(teacherTransform.position + Vector3.up, directionToPlayer.normalized,
                           out RaycastHit hit, distanceToPlayer, obstacleLayer))
        {
            return false; // Obstacle bloque la vision
        }

        return true;
    }

    private void OnPlayerDetected()
    {
        if (hasDetectedPlayer) return;

        hasDetectedPlayer = true;
        Debug.Log($"[TeacherDetection] ⚠️ PLAYER DÉTECTÉ! (Zone {currentZone}, temps: {detectionTimer:F1}s)");

        // Trigger Game Over via GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPlayerDetected();
        }

        // Animation Scold (aléatoire parmi 3 variantes)
        TeacherAI teacherAI = GetComponent<TeacherAI>();
        if (teacherAI != null)
        {
            int scoldVariant = Random.Range(0, 3);
            teacherAI.PlayScoldAnimation(scoldVariant);
        }
    }

    public bool HasDetectedPlayer() => hasDetectedPlayer;
    public float GetDetectionProgress() => currentRequiredTime > 0 ? detectionTimer / currentRequiredTime : 0f;
    public int GetCurrentZone() => currentZone;

    private void OnDrawGizmosSelected()
    {
        if (!showGizmos || teacherTransform == null) return;

        Vector3 position = teacherTransform.position;
        Vector3 forward = teacherTransform.forward;

        // Couleurs pour chaque zone
        DrawDetectionZone(position, forward, zone3MaxDistance, fieldOfViewAngle, zone3Color); // Rouge
        DrawDetectionZone(position, forward, zone2MaxDistance, fieldOfViewAngle, zone2Color); // Orange
        DrawDetectionZone(position, forward, zone1MaxDistance, fieldOfViewAngle, zone1Color); // Jaune

        // Si player détecté, dessiner ligne
        if (Application.isPlaying && playerInSight && player != null)
        {
            Gizmos.color = detectionProgressColor;
            Gizmos.DrawLine(position + Vector3.up, player.position + Vector3.up);

            // Afficher progression
            float progress = GetDetectionProgress();
            Gizmos.DrawWireSphere(player.position + Vector3.up * 2f, progress * 0.5f);
        }
    }

    private void DrawDetectionZone(Vector3 position, Vector3 forward, float range, float angle, Color color)
    {
        Gizmos.color = color;

        // Calculer les limites du cône
        Vector3 leftBoundary = Quaternion.Euler(0, -angle / 2f, 0) * forward * range;
        Vector3 rightBoundary = Quaternion.Euler(0, angle / 2f, 0) * forward * range;

        // Dessiner le cône
        Gizmos.DrawLine(position, position + leftBoundary);
        Gizmos.DrawLine(position, position + rightBoundary);

        // Arc de cercle
        int segments = 20;
        float angleStep = angle / segments;
        Vector3 previousPoint = position + leftBoundary;

        for (int i = 1; i <= segments; i++)
        {
            float currentAngle = -angle / 2f + angleStep * i;
            Vector3 direction = Quaternion.Euler(0, currentAngle, 0) * forward * range;
            Vector3 newPoint = position + direction;
            Gizmos.DrawLine(previousPoint, newPoint);
            previousPoint = newPoint;
        }
    }
}
