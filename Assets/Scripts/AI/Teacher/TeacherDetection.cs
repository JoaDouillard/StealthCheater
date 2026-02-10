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

    [Header("Player Body Parts (Assign Transform from Player model)")]
    [Tooltip("Transform de la TÊTE du player")]
    [SerializeField] private Transform playerHead;

    [Tooltip("Transform de l'ÉPAULE GAUCHE du player")]
    [SerializeField] private Transform playerShoulderLeft;

    [Tooltip("Transform de l'ÉPAULE DROITE du player")]
    [SerializeField] private Transform playerShoulderRight;

    [Tooltip("Transform de la MAIN GAUCHE du player")]
    [SerializeField] private Transform playerHandLeft;

    [Tooltip("Transform de la MAIN DROITE du player")]
    [SerializeField] private Transform playerHandRight;

    [Tooltip("Transform du BASSIN/CENTRE du player")]
    [SerializeField] private Transform playerCenter;

    [Tooltip("Transform du PIED GAUCHE du player")]
    [SerializeField] private Transform playerFootLeft;

    [Tooltip("Transform du PIED DROIT du player")]
    [SerializeField] private Transform playerFootRight;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

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
    private float detectionThreshold;

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

    // Multi-raycast system
    private struct RaycastResult
    {
        public Vector3 start;
        public Vector3 end;
        public bool hitObstacle;
        public RaycastHit hit;
        public string pointName;
    }

    private RaycastResult[] raycastResults = new RaycastResult[8];
    private int successfulRaycasts = 0;
    private int totalRaycasts = 8;

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
            detectionThreshold = currentConfig.detectionThreshold;
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
            detectionThreshold = 20f;
            if (showDebugLogs) Debug.Log("[TeacherDetection] Pas de config, valeurs par défaut");
        }

        // Trouver joueur
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerController = playerObj.GetComponent<PlayerController>();

            // Auto-assign body parts si non assignés manuellement
            if (playerHead == null || playerCenter == null)
            {
                AutoAssignPlayerBodyParts(playerObj);
            }
        }
        else
        {
            Debug.LogError("[TeacherDetection] PLAYER NON TROUVÉ! Vérifie le tag 'Player'");
        }
    }

    /// <summary>
    /// Trouve automatiquement les bones/Transform du player si non assignés manuellement
    /// </summary>
    private void AutoAssignPlayerBodyParts(GameObject playerObj)
    {
        // Chercher dans la hiérarchie du player
        Transform[] allTransforms = playerObj.GetComponentsInChildren<Transform>();

        foreach (Transform t in allTransforms)
        {
            string name = t.name.ToLower();

            // Tête
            if (playerHead == null && (name.Contains("head") || name.Contains("tete") || name.Contains("tête")))
            {
                playerHead = t;
            }
            // Épaule gauche
            else if (playerShoulderLeft == null && (name.Contains("shoulder") && (name.Contains("left") || name.Contains(".l"))))
            {
                playerShoulderLeft = t;
            }
            // Épaule droite
            else if (playerShoulderRight == null && (name.Contains("shoulder") && (name.Contains("right") || name.Contains(".r"))))
            {
                playerShoulderRight = t;
            }
            // Main gauche
            else if (playerHandLeft == null && (name.Contains("hand") && (name.Contains("left") || name.Contains(".l"))))
            {
                playerHandLeft = t;
            }
            // Main droite
            else if (playerHandRight == null && (name.Contains("hand") && (name.Contains("right") || name.Contains(".r"))))
            {
                playerHandRight = t;
            }
            // Bassin/Hips
            else if (playerCenter == null && (name.Contains("hips") || name.Contains("pelvis") || name.Contains("bassin")))
            {
                playerCenter = t;
            }
            // Pied gauche
            else if (playerFootLeft == null && (name.Contains("foot") && (name.Contains("left") || name.Contains(".l"))))
            {
                playerFootLeft = t;
            }
            // Pied droit
            else if (playerFootRight == null && (name.Contains("foot") && (name.Contains("right") || name.Contains(".r"))))
            {
                playerFootRight = t;
            }
        }

        // Log ce qui a été trouvé
        int foundCount = 0;
        if (playerHead != null) foundCount++;
        if (playerShoulderLeft != null) foundCount++;
        if (playerShoulderRight != null) foundCount++;
        if (playerHandLeft != null) foundCount++;
        if (playerHandRight != null) foundCount++;
        if (playerCenter != null) foundCount++;
        if (playerFootLeft != null) foundCount++;
        if (playerFootRight != null) foundCount++;

        if (showDebugLogs) Debug.Log($"[TeacherDetection] Auto-assign: {foundCount}/8 body parts");
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

            // Mettre à jour le HUD - Safe
            if (GameHUD.Instance != null)
            {
                GameHUD.Instance.SetDetectionStatus(DetectionStatus.Safe);
            }
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
            // Son d'alerte quand le player ENTRE dans la zone (une seule fois)
            if (!wasInSight && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayDetectionAlert();
            }

            // Player dans le champ de vision
            // Incrémenter le timer
            detectionTimer += Time.deltaTime;

            // Calculer progression détection
            float progress = GetDetectionProgress();

            // Déterminer le statut de détection pour le HUD
            DetectionStatus status;

            if (progress >= 0.7f) // 70% ou plus → DANGER
            {
                status = DetectionStatus.Danger;
            }
            else if (progress >= 0.3f) // 30-70% → SUSPECT
            {
                status = DetectionStatus.Suspect;
            }
            else if (progress > 0f) // 0-30% → VISIBLE (dans la zone, détection commence)
            {
                status = DetectionStatus.Visible;
            }
            else // progress = 0 (juste entré dans la zone)
            {
                status = DetectionStatus.Visible;
            }

            // Mettre à jour le HUD
            if (GameHUD.Instance != null)
            {
                GameHUD.Instance.SetDetectionStatus(status);
            }

            // Vérifier si détection complétée
            if (detectionTimer >= currentRequiredTime)
            {
                OnPlayerDetected();
            }
        }
        else
        {
            // Player hors du champ de vision
            // Reset le timer
            detectionTimer = 0f;
            currentZone = 0;

            // Mettre à jour le HUD - Safe
            if (GameHUD.Instance != null)
            {
                GameHUD.Instance.SetDetectionStatus(DetectionStatus.Safe);
            }
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

        // MULTIPLES RAYCASTS vers les Transform assignés du corps du player
        Vector3 teacherEyePos = teacherTransform.position + Vector3.up * 1.5f;

        // Construire la liste dynamique des parties du corps assignées
        System.Collections.Generic.List<(Transform transform, string name)> bodyParts = new System.Collections.Generic.List<(Transform, string)>();

        if (playerHead != null) bodyParts.Add((playerHead, "Tête"));
        if (playerShoulderLeft != null) bodyParts.Add((playerShoulderLeft, "Épaule G"));
        if (playerShoulderRight != null) bodyParts.Add((playerShoulderRight, "Épaule D"));
        if (playerHandLeft != null) bodyParts.Add((playerHandLeft, "Main G"));
        if (playerHandRight != null) bodyParts.Add((playerHandRight, "Main D"));
        if (playerCenter != null) bodyParts.Add((playerCenter, "Bassin"));
        if (playerFootLeft != null) bodyParts.Add((playerFootLeft, "Pied G"));
        if (playerFootRight != null) bodyParts.Add((playerFootRight, "Pied D"));

        // Si aucune partie du corps n'est assignée, fallback vers le centre du player
        if (bodyParts.Count == 0)
        {
            bodyParts.Add((player, "Centre"));
        }

        // Ajuster le nombre total de raycasts selon le nombre de Transform assignés
        totalRaycasts = bodyParts.Count;
        if (raycastResults.Length != totalRaycasts)
        {
            raycastResults = new RaycastResult[totalRaycasts];
        }

        // Effectuer les raycasts
        successfulRaycasts = 0;

        for (int i = 0; i < totalRaycasts; i++)
        {
            Transform bodyTransform = bodyParts[i].transform;
            string bodyName = bodyParts[i].name;

            Vector3 targetPoint = bodyTransform.position;
            Vector3 direction = (targetPoint - teacherEyePos).normalized;
            float distance = Vector3.Distance(teacherEyePos, targetPoint);

            raycastResults[i].start = teacherEyePos;
            raycastResults[i].end = targetPoint;
            raycastResults[i].pointName = bodyName;

            // Raycast avec le layer mask des obstacles
            if (Physics.Raycast(teacherEyePos, direction, out RaycastHit hit, distance, obstacleLayer))
            {
                // Obstacle bloque ce raycast
                raycastResults[i].hitObstacle = true;
                raycastResults[i].hit = hit;
            }
            else
            {
                // Ce raycast passe (pas d'obstacle)
                raycastResults[i].hitObstacle = false;
                successfulRaycasts++;
            }
        }

        // Calculer le pourcentage de raycasts réussis
        float successPercentage = (successfulRaycasts / (float)totalRaycasts) * 100f;

        // Player visible si le pourcentage dépasse le seuil
        return successPercentage >= detectionThreshold;
    }

    private void OnPlayerDetected()
    {
        if (hasDetectedPlayer) return;

        hasDetectedPlayer = true;
        if (showDebugLogs) Debug.Log($"[TeacherDetection] Player détecté (Zone {currentZone})");

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

        // Calculer distances effectives (avec modulation crouch si le player est accroupi)
        float distMod = 1f;
        if (Application.isPlaying && playerController != null && playerController.IsCrouching())
        {
            distMod = crouchDistanceModifier;
        }

        float effectiveZone1 = zone1MaxDistance * distMod;
        float effectiveZone2 = zone2MaxDistance * distMod;
        float effectiveZone3 = zone3MaxDistance * distMod;

        // Couleurs pour chaque zone (avec distances modulées)
        DrawDetectionZone(position, forward, effectiveZone3, fieldOfViewAngle, zone3Color); // Rouge
        DrawDetectionZone(position, forward, effectiveZone2, fieldOfViewAngle, zone2Color); // Orange
        DrawDetectionZone(position, forward, effectiveZone1, fieldOfViewAngle, zone1Color); // Jaune

        // Afficher info crouch
        if (Application.isPlaying && playerController != null && playerController.IsCrouching())
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(position + Vector3.up * 3f, 0.3f);
        }

        // Si player détecté, dessiner ligne
        if (Application.isPlaying && playerInSight && player != null)
        {
            Gizmos.color = detectionProgressColor;
            Gizmos.DrawLine(position + Vector3.up, player.position + Vector3.up);

            // Afficher progression
            float progress = GetDetectionProgress();
            Gizmos.DrawWireSphere(player.position + Vector3.up * 2f, progress * 0.5f);
        }

        // Afficher les multiples raycasts
        if (Application.isPlaying && player != null)
        {
            Vector3 teacherEyePos = teacherTransform.position + Vector3.up * 1.5f;

            // Dessiner sphère cyan aux yeux du Teacher
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(teacherEyePos, 0.15f);

            // Dessiner tous les raycasts
            for (int i = 0; i < totalRaycasts; i++)
            {
                RaycastResult result = raycastResults[i];

                if (result.hitObstacle)
                {
                    // Raycast bloqué - ROUGE
                    Gizmos.color = Color.red;
                    Gizmos.DrawLine(result.start, result.hit.point);
                    Gizmos.DrawWireSphere(result.hit.point, 0.1f);

#if UNITY_EDITOR
                    // Label avec nom du point et obstacle
                    UnityEditor.Handles.Label(result.hit.point, $"{result.pointName}\n❌ {result.hit.collider.name}");
#endif
                }
                else
                {
                    // Raycast passe - VERT
                    Gizmos.color = Color.green;
                    Gizmos.DrawLine(result.start, result.end);
                    Gizmos.DrawWireSphere(result.end, 0.1f);

#if UNITY_EDITOR
                    // Label avec nom du point
                    UnityEditor.Handles.Label(result.end, $"{result.pointName}\n✅");
#endif
                }
            }

            // Afficher statistiques au-dessus du player
#if UNITY_EDITOR
            float successPercentage = (successfulRaycasts / (float)totalRaycasts) * 100f;
            string stats = $"Visibilité: {successfulRaycasts}/{totalRaycasts} ({successPercentage:F0}%)\nSeuil: {detectionThreshold}%";
            UnityEditor.Handles.Label(player.position + Vector3.up * 2.5f, stats);
#endif
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
