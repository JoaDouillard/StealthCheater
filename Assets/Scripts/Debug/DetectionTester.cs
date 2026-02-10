using UnityEngine;

/// <summary>
/// Système de test isolé pour la détection Teacher → Player
///
/// UTILISATION:
/// 1. Créer une scène vide
/// 2. Créer 3 GameObjects: FakeTeacher, FakePlayer, FakeObstacle
/// 3. Ajouter ce script sur un GameObject vide (ex: "DetectionTester")
/// 4. Assigner les 3 transforms dans l'Inspector
/// 5. Configurer les layers (Player, Obstacle)
/// 6. Play la scène et déplacer les objets pour tester
///
/// GIZMOS:
/// - Cône jaune/orange/rouge = zones de détection (3 zones)
/// - Ligne VERTE = vision claire (pas d'obstacle)
/// - Ligne ROUGE + sphère = obstacle bloque la vision
/// - Sphère au-dessus du player = Player détecté
/// </summary>
public class DetectionTester : MonoBehaviour
{
    [Header("Test Objects")]
    [Tooltip("Transform représentant le Teacher (ex: un Cube rouge)")]
    public Transform fakeTeacher;

    [Tooltip("Transform représentant l'obstacle (ex: un Cube gris) - optionnel")]
    public Transform fakeObstacle;

    [Header("Player Body Parts (Assign Transform objects from your model)")]
    [Tooltip("Transform de la TÊTE du player (ex: Head bone/object)")]
    public Transform playerHead;

    [Tooltip("Transform de l'ÉPAULE GAUCHE du player (ex: LeftShoulder bone/object)")]
    public Transform playerShoulderLeft;

    [Tooltip("Transform de l'ÉPAULE DROITE du player (ex: RightShoulder bone/object)")]
    public Transform playerShoulderRight;

    [Tooltip("Transform de la MAIN GAUCHE du player (ex: LeftHand bone/object)")]
    public Transform playerHandLeft;

    [Tooltip("Transform de la MAIN DROITE du player (ex: RightHand bone/object)")]
    public Transform playerHandRight;

    [Tooltip("Transform du BASSIN/CENTRE du player (ex: Hips/Spine bone/object)")]
    public Transform playerCenter;

    [Tooltip("Transform du PIED GAUCHE du player (ex: LeftFoot bone/object)")]
    public Transform playerFootLeft;

    [Tooltip("Transform du PIED DROIT du player (ex: RightFoot bone/object)")]
    public Transform playerFootRight;

    [Header("Detection Settings")]
    [Tooltip("Angle du champ de vision (degrés)")]
    [Range(30f, 180f)]
    public float fieldOfViewAngle = 90f;

    [Tooltip("Zone 1 - Distance maximale (lente)")]
    public float zone1MaxDistance = 8f;

    [Tooltip("Zone 1 - Temps de détection (secondes)")]
    public float zone1DetectionTime = 5f;

    [Tooltip("Zone 2 - Distance maximale (moyenne)")]
    public float zone2MaxDistance = 6f;

    [Tooltip("Zone 2 - Temps de détection (secondes)")]
    public float zone2DetectionTime = 3f;

    [Tooltip("Zone 3 - Distance maximale (immédiate)")]
    public float zone3MaxDistance = 2f;

    [Tooltip("Zone 3 - Temps de détection (secondes)")]
    public float zone3DetectionTime = 0f;

    [Header("Raycast Settings")]
    [Tooltip("Hauteur des yeux du Teacher (relatif à sa position)")]
    [Range(0f, 3f)]
    public float teacherEyeHeight = 1.5f;

    [Tooltip("Pourcentage minimum de raycasts réussis pour détecter (0-100%)")]
    [Range(0f, 100f)]
    public float detectionThreshold = 20f; // 20% = au moins 1 raycast sur 7

    [Header("Layers")]
    [Tooltip("Layer du Player (pour le raycast)")]
    public LayerMask playerLayer;

    [Tooltip("Layer des Obstacles (pour le raycast)")]
    public LayerMask obstacleLayer;

    [Header("Gizmos Colors")]
    public Color zone1Color = new Color(1f, 1f, 0f, 0.2f); // Jaune
    public Color zone2Color = new Color(1f, 0.5f, 0f, 0.3f); // Orange
    public Color zone3Color = new Color(1f, 0f, 0f, 0.4f); // Rouge
    public Color raycastClearColor = Color.green;
    public Color raycastBlockedColor = Color.red;

    [Header("Runtime Info (Read-Only)")]
    [SerializeField] private bool playerInSight = false;
    [SerializeField] private int currentZone = 0; // 0 = hors zone, 1/2/3 = zone active
    [SerializeField] private float detectionTimer = 0f;
    [SerializeField] private float currentRequiredTime = 0f;
    [SerializeField] private bool isDetected = false;

    // Multiples raycasts debug (7 points sur le corps du player)
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

    private void Update()
    {
        if (fakeTeacher == null)
        {
            Debug.LogWarning("[DetectionTester] Fake Teacher non assigné!");
            return;
        }

        // Vérifier qu'au moins un Transform du player est assigné
        if (!HasAnyBodyPart())
        {
            Debug.LogWarning("[DetectionTester] Aucune partie du corps du player assignée! Assigne au moins un Transform (Head, Hands, etc.)");
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
                Debug.Log($"[DetectionTester] ✅ Player entré dans Zone {currentZone} (temps requis: {currentRequiredTime}s)");
            }

            // Incrémenter le timer
            detectionTimer += Time.deltaTime;

            // Vérifier si détection complétée
            if (detectionTimer >= currentRequiredTime && !isDetected)
            {
                isDetected = true;
                Debug.Log($"[DetectionTester] ⚠️ PLAYER DÉTECTÉ! (Zone {currentZone}, temps: {detectionTimer:F1}s)");
            }
        }
        else
        {
            // Player hors du champ de vision
            if (wasInSight)
            {
                Debug.Log("[DetectionTester] ❌ Player sorti du champ de vision");
            }

            // Reset le timer
            detectionTimer = 0f;
            currentZone = 0;
            isDetected = false;
        }
    }

    /// <summary>
    /// Vérifie si au moins une partie du corps est assignée
    /// </summary>
    private bool HasAnyBodyPart()
    {
        return playerHead != null ||
               playerShoulderLeft != null ||
               playerShoulderRight != null ||
               playerHandLeft != null ||
               playerHandRight != null ||
               playerCenter != null ||
               playerFootLeft != null ||
               playerFootRight != null;
    }

    /// <summary>
    /// Récupère la position du centre du player (pour FOV check)
    /// Utilise le premier Transform assigné comme référence
    /// </summary>
    private Vector3 GetPlayerCenterPosition()
    {
        if (playerCenter != null) return playerCenter.position;
        if (playerHead != null) return playerHead.position;
        if (playerShoulderLeft != null) return playerShoulderLeft.position;
        if (playerShoulderRight != null) return playerShoulderRight.position;
        if (playerHandLeft != null) return playerHandLeft.position;
        if (playerHandRight != null) return playerHandRight.position;
        if (playerFootLeft != null) return playerFootLeft.position;
        if (playerFootRight != null) return playerFootRight.position;

        return Vector3.zero; // Fallback (ne devrait jamais arriver)
    }

    private bool IsPlayerInFieldOfView()
    {
        Vector3 playerCenterPos = GetPlayerCenterPosition();
        Vector3 directionToPlayer = playerCenterPos - fakeTeacher.position;
        float distanceToPlayer = directionToPlayer.magnitude;

        // Vérifier l'angle (FOV) - basé sur le centre du player
        float angle = Vector3.Angle(fakeTeacher.forward, directionToPlayer);
        if (angle > fieldOfViewAngle / 2f)
        {
            return false; // Hors du champ de vision
        }

        // Déterminer dans quelle zone le player se trouve
        int detectedZone = 0;
        float requiredTime = 0f;

        if (distanceToPlayer <= zone3MaxDistance)
        {
            detectedZone = 3;
            requiredTime = zone3DetectionTime;
        }
        else if (distanceToPlayer <= zone2MaxDistance)
        {
            detectedZone = 2;
            requiredTime = zone2DetectionTime;
        }
        else if (distanceToPlayer <= zone1MaxDistance)
        {
            detectedZone = 1;
            requiredTime = zone1DetectionTime;
        }
        else
        {
            return false; // Hors de toute zone
        }

        // Update zone actuelle et temps requis
        if (currentZone != detectedZone)
        {
            detectionTimer = 0f;
            currentZone = detectedZone;
            currentRequiredTime = requiredTime;
        }

        // MULTIPLES RAYCASTS vers les Transform assignés du corps du player
        Vector3 teacherEyePos = fakeTeacher.position + Vector3.up * teacherEyeHeight;

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

        Debug.Log($"[DetectionTester] 📊 Raycasts: {successfulRaycasts}/{totalRaycasts} réussis ({successPercentage:F0}%) - Seuil: {detectionThreshold}%");

        // Player visible si le pourcentage dépasse le seuil
        bool isVisible = successPercentage >= detectionThreshold;

        if (isVisible)
        {
            Debug.Log($"[DetectionTester] ✅ Player VISIBLE (au moins {successfulRaycasts} partie(s) du corps visible(s))");
        }
        else
        {
            Debug.Log($"[DetectionTester] 🚫 Player CACHÉ (pas assez de parties visibles)");
        }

        return isVisible;
    }

    private void OnDrawGizmos()
    {
        if (fakeTeacher == null) return;

        Vector3 position = fakeTeacher.position;
        Vector3 forward = fakeTeacher.forward;

        // Dessiner les 3 zones de détection
        DrawDetectionZone(position, forward, zone3MaxDistance, fieldOfViewAngle, zone3Color); // Rouge (proche)
        DrawDetectionZone(position, forward, zone2MaxDistance, fieldOfViewAngle, zone2Color); // Orange (moyen)
        DrawDetectionZone(position, forward, zone1MaxDistance, fieldOfViewAngle, zone1Color); // Jaune (loin)

        // TOUJOURS dessiner la position des yeux du Teacher (sphère cyan)
        Vector3 teacherEyePos = fakeTeacher.position + Vector3.up * teacherEyeHeight;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(teacherEyePos, 0.15f);

#if UNITY_EDITOR
        UnityEditor.Handles.Label(teacherEyePos + Vector3.up * 0.3f, "Yeux Teacher");
#endif

        // Si en Play mode, dessiner les multiples raycasts
        if (Application.isPlaying && HasAnyBodyPart())
        {
            Vector3 playerCenterPos = GetPlayerCenterPosition();
            // Dessiner les 7 raycasts
            for (int i = 0; i < totalRaycasts; i++)
            {
                RaycastResult result = raycastResults[i];

                if (result.hitObstacle)
                {
                    // Raycast bloqué - ROUGE
                    Gizmos.color = raycastBlockedColor;
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
                    Gizmos.color = raycastClearColor;
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
            UnityEditor.Handles.Label(playerCenterPos + Vector3.up * 2.5f, stats);
#endif

            // Si player dans le champ de vision
            if (playerInSight)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(playerCenterPos + Vector3.up * 2f, 0.3f);

                // Progression de détection
                float progress = currentRequiredTime > 0 ? detectionTimer / currentRequiredTime : 0f;
                Gizmos.color = Color.Lerp(Color.yellow, Color.red, progress);
                Gizmos.DrawWireSphere(playerCenterPos + Vector3.up * 2.2f, progress * 0.4f);
            }

            // Si détection complétée
            if (isDetected)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(playerCenterPos + Vector3.up * 3f, 0.5f);
            }
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

    [ContextMenu("Reset Detection")]
    private void ResetDetection()
    {
        detectionTimer = 0f;
        currentZone = 0;
        isDetected = false;
        playerInSight = false;
        Debug.Log("[DetectionTester] Detection réinitialisée");
    }
}
