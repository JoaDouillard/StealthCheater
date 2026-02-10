using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Zone du bureau du joueur où il retourne et appuie sur E pour écrire la réponse copiée
/// Component à ajouter sur le bureau/desk du Player
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class ReturnZone : MonoBehaviour
{
    [Header("Zone Settings")]
    [Tooltip("Taille de la zone du bureau (mètres)")]
    [SerializeField] private Vector3 zoneSize = new Vector3(2f, 1f, 2f);

    [Tooltip("Layer du joueur pour détecter l'entrée")]
    [SerializeField] private LayerMask playerLayer;

    [Header("Visual Feedback")]
    [Tooltip("Afficher les gizmos de debug dans la Scene View")]
    [SerializeField] private bool showDebugGizmos = true;

    [Header("Debug")]
    [Tooltip("Afficher les logs de debug dans la console")]
    [SerializeField] private bool showDebugLogs = false;

    [Tooltip("Couleur de la zone quand active")]
    [SerializeField] private Color activeColor = Color.cyan;

    [Tooltip("Couleur de la zone pendant l'écriture")]
    [SerializeField] private Color writingColor = Color.yellow;

    // État de la zone
    private BoxCollider zoneCollider;
    private bool playerInZone = false;
    private bool canStartWriting = false;
    private bool isWriting = false;
    private float writeTimer = 0f;
    private float requiredWriteTime = 0f;
    private float pointsToWrite = 0f;
    private StudentData copiedStudent;

    // Flag pour savoir si le joueur a copié une réponse (set par StartWriting)
    private bool hasCopiedAnswer = false;

    // References
    private ExamManager currentExam;

    private void Awake()
    {
        // Configurer le collider
        zoneCollider = GetComponent<BoxCollider>();
        zoneCollider.isTrigger = true;
        zoneCollider.size = zoneSize;

        LogDebug("Zone configurée");

        if (playerLayer.value == 0)
        {
            Debug.LogError("[ReturnZone] PlayerLayer non configuré!");
        }
    }

    private void Update()
    {
        // Vérifier input E pour démarrer l'écriture
        // Seulement si le joueur a copié une réponse ET est dans la zone
        if (hasCopiedAnswer && canStartWriting && !isWriting && playerInZone && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            BeginWriting();
        }

        // Incrémenter le timer d'écriture seulement si on écrit
        if (isWriting && playerInZone)
        {
            writeTimer += Time.deltaTime;

            // Afficher progression dans le HUD
            if (GameHUD.Instance != null)
            {
                GameHUD.Instance.ShowWriteProgress(writeTimer, requiredWriteTime);
            }

            // Vérifier si l'écriture est terminée
            if (writeTimer >= requiredWriteTime)
            {
                OnWritingCompleted();
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Vérifier si c'est le joueur
        if (((1 << other.gameObject.layer) & playerLayer) != 0)
        {
            playerInZone = true;

            LogDebug($"Joueur entre dans zone (hasCopiedAnswer: {hasCopiedAnswer})");

            // Afficher prompt SEULEMENT si le joueur a copié une réponse
            if (hasCopiedAnswer && !isWriting)
            {
                canStartWriting = true;
                if (GameHUD.Instance != null)
                {
                    GameHUD.Instance.ShowInteractionPrompt("Press E to write the answer");
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Vérifier si c'est le joueur
        if (((1 << other.gameObject.layer) & playerLayer) != 0)
        {
            playerInZone = false;
            canStartWriting = false;

            if (isWriting)
            {
                LogDebug("Joueur sort de zone - Écriture annulée");
                isWriting = false;
                writeTimer = 0f;

                // Arrêter le son d'écriture
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.StopWriting();
                }

                // Cacher progression HUD
                if (GameHUD.Instance != null)
                {
                    GameHUD.Instance.HideWriteProgress();
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
    /// Vérifie si le joueur peut commencer à écrire
    /// Appelé quand le joueur entre dans la zone
    /// </summary>
    private void CheckIfCanStartWriting()
    {
        if (isWriting) return;

        if (currentExam == null)
            currentExam = FindFirstObjectByType<ExamManager>();

        if (currentExam == null || !currentExam.IsWaitingForAnswer())
            return;
    }

    /// <summary>
    /// Prépare l'écriture (appelé après une copie réussie)
    /// L'écriture démarrera quand le joueur appuiera sur E dans la zone
    /// </summary>
    public void StartWriting(float points, StudentData student, SubjectData subject)
    {
        pointsToWrite = points;
        copiedStudent = student;

        // MARQUER que le joueur a copié une réponse
        hasCopiedAnswer = true;

        // Calculer temps d'écriture selon la compétence de l'élève copié
        int copiedSkill = student.GetSkillForSubject(subject.subjectType);
        requiredWriteTime = subject.GetWritingTime(copiedSkill);

        // Trouver ExamManager
        if (currentExam == null)
        {
            currentExam = FindFirstObjectByType<ExamManager>();
        }

        LogDebug($"Prêt à écrire - Points: {pointsToWrite:F2}, Temps: {requiredWriteTime:F1}s");

        // Si déjà dans la zone, afficher le prompt
        if (playerInZone && GameHUD.Instance != null)
        {
            GameHUD.Instance.ShowInteractionPrompt("Press E to write the answer");
            canStartWriting = true;
        }
    }

    /// <summary>
    /// Démarre réellement l'écriture (appelé quand joueur appuie sur E)
    /// </summary>
    private void BeginWriting()
    {
        if (!playerInZone || isWriting) return;

        isWriting = true;
        writeTimer = 0f;

        LogDebug("Début de l'écriture");

        // Cacher le prompt "Press E"
        if (GameHUD.Instance != null)
        {
            GameHUD.Instance.HideInteractionPrompt();
        }

        // Son d'écriture (début + boucle)
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayWriteStart();
        }
    }

    /// <summary>
    /// Appelé quand l'écriture est terminée
    /// </summary>
    private void OnWritingCompleted()
    {
        LogDebug($"Écriture terminée! {pointsToWrite:F2} pts");

        // Jouer le son de complétion d'écriture (arrête aussi la boucle)
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayWriteComplete();
        }

        // Notifier l'ExamManager
        if (currentExam != null)
        {
            currentExam.OnAnswerCompleted(pointsToWrite);
        }
        else
        {
            Debug.LogError("[ReturnZone] ExamManager introuvable!");
        }

        // Cacher progression HUD
        if (GameHUD.Instance != null)
        {
            GameHUD.Instance.HideWriteProgress();
        }

        // Reset état
        isWriting = false;
        canStartWriting = false;
        hasCopiedAnswer = false; // Reset pour la prochaine question
        writeTimer = 0f;
        pointsToWrite = 0f;
        copiedStudent = null;
    }

    /// <summary>
    /// Obtient le pourcentage de progression de l'écriture (0-1)
    /// </summary>
    public float GetWriteProgress()
    {
        if (!isWriting || requiredWriteTime <= 0f)
            return 0f;

        return Mathf.Clamp01(writeTimer / requiredWriteTime);
    }

    /// <summary>
    /// Vérifie si le joueur est dans la zone
    /// </summary>
    public bool IsPlayerInZone()
    {
        return playerInZone;
    }

    /// <summary>
    /// Vérifie si on est en train d'écrire
    /// </summary>
    public bool IsWriting()
    {
        return isWriting;
    }

    private void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;

        // Couleur selon l'état
        Color gizmoColor = isWriting ? writingColor : activeColor;

        Gizmos.color = gizmoColor;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(Vector3.zero, zoneSize);

        // Reset matrix
        Gizmos.matrix = Matrix4x4.identity;

#if UNITY_EDITOR
        if (isWriting)
        {
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 2f,
                $"Écriture en cours...\n{writeTimer:F1}s / {requiredWriteTime:F1}s"
            );
        }
#endif
    }

    private void OnValidate()
    {
        // Synchroniser la taille du collider avec le paramètre
        if (zoneCollider != null)
        {
            zoneCollider.size = zoneSize;
        }
    }

    /// <summary>
    /// Log conditionnel (seulement si showDebugLogs est activé)
    /// </summary>
    private void LogDebug(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[ReturnZone] {message}");
        }
    }
}
