using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Zone de copie autour d'un élève
/// Le joueur entre dans cette zone et appuie sur E pour copier la réponse d'un élève
/// Component à ajouter sur chaque GameObject Student
/// </summary>
[RequireComponent(typeof(SphereCollider))]
public class CopyZone : MonoBehaviour
{
    [Header("Student Configuration")]
    [Tooltip("Données de l'élève (compétences par matière)")]
    [SerializeField] private StudentData studentData;

    [Header("Zone Settings")]
    [Tooltip("Rayon de la zone de copie (mètres)")]
    [Range(0.5f, 3f)]
    [SerializeField] private float zoneRadius = 1.5f;

    [Tooltip("Layer du joueur pour détecter l'entrée")]
    [SerializeField] private LayerMask playerLayer;

    [Header("Visual Feedback")]
    [Tooltip("Afficher les gizmos de debug dans la Scene View")]
    [SerializeField] private bool showDebugGizmos = true;

    [Header("Debug")]
    [Tooltip("Afficher les logs de debug dans la console")]
    [SerializeField] private bool showDebugLogs = false;

    [Tooltip("Couleur de la zone active (matière correspond)")]
    [SerializeField] private Color activeColor = Color.green;

    [Tooltip("Couleur de la zone inactive (matière ne correspond pas)")]
    [SerializeField] private Color inactiveColor = Color.gray;

    // État de la zone
    private SphereCollider zoneCollider;
    private bool isActive = false;
    private SubjectType currentSubjectType;
    private bool playerInZone = false;
    private bool canStartCopying = false;
    private bool isCopying = false;
    private float copyTimer = 0f;
    private float requiredCopyTime = 0f;
    private int skillLevel = 0;
    private float pointsToEarn = 0f;

    // References
    private ExamManager currentExam;

    private void Awake()
    {
        // Configurer le collider
        zoneCollider = GetComponent<SphereCollider>();
        zoneCollider.isTrigger = true;
        zoneCollider.radius = zoneRadius;

        // Vérifier que StudentData est assigné
        if (studentData == null)
        {
            Debug.LogWarning($"[CopyZone] StudentData manquant sur {gameObject.name}! La zone ne fonctionnera pas correctement.");
        }
    }

    private void Update()
    {
        if (!isActive) return;

        // Vérifier input E pour démarrer la copie
        if (canStartCopying && !isCopying && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            StartCopying();
        }

        // Incrémenter le timer de copie seulement si on est en train de copier
        if (isCopying && playerInZone)
        {
            copyTimer += Time.deltaTime;

            // Afficher progression dans le HUD
            if (GameHUD.Instance != null && studentData != null)
            {
                GameHUD.Instance.ShowCopyProgress(copyTimer, requiredCopyTime, studentData.studentName);
            }

            // Vérifier si la copie est terminée
            if (copyTimer >= requiredCopyTime)
            {
                OnCopyCompleted();
            }
        }
    }

    /// <summary>
    /// Active la zone pour une matière spécifique
    /// </summary>
    public void ActivateForSubject(SubjectType subjectType)
    {
        if (studentData == null)
        {
            Debug.LogWarning($"[CopyZone] Impossible d'activer: StudentData manquant sur {gameObject.name}");
            return;
        }

        currentSubjectType = subjectType;
        isActive = true;

        // Obtenir compétence de l'élève pour cette matière
        skillLevel = studentData.GetSkillForSubject(subjectType);

        // Calculer temps de copie requis
        requiredCopyTime = studentData.GetCopyTime(skillLevel);

        // Calculer points à gagner
        pointsToEarn = studentData.GetPoints(skillLevel);

        // Trouver l'ExamManager actuel
        currentExam = FindFirstObjectByType<ExamManager>();

        LogDebug($"Zone activée pour {studentData.studentName} ({subjectType}) - Skill: {skillLevel}%, Temps: {requiredCopyTime:F1}s");
    }

    /// <summary>
    /// Désactive la zone
    /// </summary>
    public void Deactivate()
    {
        isActive = false;
        playerInZone = false;
        copyTimer = 0f;

        LogDebug($"Zone désactivée pour {studentData?.studentName ?? gameObject.name}");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isActive) return;

        // Vérifier si c'est le joueur
        if (((1 << other.gameObject.layer) & playerLayer) != 0)
        {
            playerInZone = true;
            canStartCopying = true;

            LogDebug($"Joueur entre dans zone de {studentData.studentName}");

            // Show prompt "Press E to copy"
            if (GameHUD.Instance != null && studentData != null)
            {
                GameHUD.Instance.ShowInteractionPrompt($"Press E to copy from {studentData.studentName}");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!isActive) return;

        // Vérifier si c'est le joueur
        if (((1 << other.gameObject.layer) & playerLayer) != 0)
        {
            playerInZone = false;
            canStartCopying = false;

            if (isCopying)
            {
                LogDebug($"Joueur sort de zone - Copie annulée");
                isCopying = false;
                copyTimer = 0f;

                // Arrêter le son de copie
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.StopCopying();
                }

                // Cacher progression HUD
                if (GameHUD.Instance != null)
                {
                    GameHUD.Instance.HideCopyProgress();
                }
            }

            // Cacher prompt "Press E"
            if (GameHUD.Instance != null)
            {
                GameHUD.Instance.HideInteractionPrompt();
            }
        }
    }

    /// <summary>
    /// Démarre le processus de copie
    /// </summary>
    private void StartCopying()
    {
        if (!canStartCopying || isCopying) return;

        isCopying = true;
        copyTimer = 0f;

        LogDebug($"Début copie sur {studentData.studentName} ({requiredCopyTime:F1}s)");

        // Cacher le prompt "Press E"
        if (GameHUD.Instance != null)
        {
            GameHUD.Instance.HideInteractionPrompt();
        }

        // Son de copie (début + boucle)
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayCopyStart();
        }
    }

    /// <summary>
    /// Appelé quand la copie est terminée
    /// </summary>
    private void OnCopyCompleted()
    {
        LogDebug($"Copie terminée! {studentData.studentName} - {pointsToEarn:F2} pts");

        // Jouer le son de complétion (arrête aussi la boucle)
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayCopyComplete();
        }

        // Notifier l'ExamManager
        if (currentExam != null)
        {
            currentExam.OnCopyCompleted(pointsToEarn, studentData);
        }
        else
        {
            Debug.LogError("[CopyZone] ExamManager introuvable!");
        }

        // Cacher progression HUD
        if (GameHUD.Instance != null)
        {
            GameHUD.Instance.HideCopyProgress();
        }

        // Reset état
        isCopying = false;
        canStartCopying = false;
        playerInZone = false;
        copyTimer = 0f;

        // NOTE: La zone reste active pour les autres questions
        // Elle sera désactivée par ExamManager quand le contrôle est terminé
    }

    /// <summary>
    /// Obtient le pourcentage de progression de la copie (0-1)
    /// </summary>
    public float GetCopyProgress()
    {
        if (!playerInZone || requiredCopyTime <= 0f)
            return 0f;

        return Mathf.Clamp01(copyTimer / requiredCopyTime);
    }

    /// <summary>
    /// Vérifie si le joueur est dans la zone
    /// </summary>
    public bool IsPlayerInZone()
    {
        return playerInZone;
    }

    /// <summary>
    /// Obtient le StudentData associé
    /// </summary>
    public StudentData GetStudentData()
    {
        return studentData;
    }

    private void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;

        // Couleur selon l'état
        Color gizmoColor = isActive ? activeColor : inactiveColor;

        if (playerInZone)
        {
            gizmoColor = Color.yellow; // Jaune pendant la copie
        }

        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, zoneRadius);

        // Afficher info si actif
        if (isActive && studentData != null)
        {
#if UNITY_EDITOR
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 2f,
                $"{studentData.studentName}\n{currentSubjectType}: {skillLevel}%\nTemps: {requiredCopyTime:F1}s"
            );
#endif
        }
    }

    private void OnValidate()
    {
        // Synchroniser le rayon du collider avec le paramètre
        if (zoneCollider != null)
        {
            zoneCollider.radius = zoneRadius;
        }
    }

    /// <summary>
    /// Log conditionnel (seulement si showDebugLogs est activé)
    /// </summary>
    private void LogDebug(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[CopyZone] {message}");
        }
    }
}
