using UnityEngine;
using System.Collections;

/// <summary>
/// Zone où le joueur peut copier (table cible)
/// </summary>
[RequireComponent(typeof(Collider))]
public class CopyZone : MonoBehaviour
{
    [Header("Copy Settings")]
    [SerializeField] private float copyDuration = 5f; // Temps nécessaire pour copier (en secondes)
    [SerializeField] private bool requireStillness = true; // Le joueur doit rester immobile

    [Header("Visualization")]
    [SerializeField] private GameObject indicator; // Indicateur visuel (optionnel)
    [SerializeField] private Color gizmoColor = Color.green;

    // État
    private bool playerInZone = false;
    private bool isCopying = false;
    private bool copyCompleted = false;
    private float copyProgress = 0f;

    // Références
    private PlayerController player;
    private Coroutine copyCoroutine;

    #region Unity Lifecycle

    private void Awake()
    {
        // Vérifier que le collider est en trigger
        Collider col = GetComponent<Collider>();
        if (!col.isTrigger)
        {
            Debug.LogWarning("[CopyZone] Le Collider doit être en IsTrigger !");
            col.isTrigger = true;
        }
    }

    private void Start()
    {
        // Afficher l'indicateur si présent
        if (indicator != null)
        {
            indicator.SetActive(true);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Le joueur entre dans la zone
        if (other.CompareTag("Player") && !copyCompleted)
        {
            playerInZone = true;
            player = other.GetComponent<PlayerController>();

            Debug.Log("[CopyZone] Joueur dans la zone de copie. Reste immobile pour copier...");

            // Démarrer la copie automatiquement
            StartCopying();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Le joueur sort de la zone
        if (other.CompareTag("Player"))
        {
            playerInZone = false;

            // Interrompre la copie si en cours
            if (isCopying && !copyCompleted)
            {
                StopCopying();
                Debug.Log("[CopyZone] Copie interrompue - Joueur a quitté la zone.");
            }
        }
    }

    private void Update()
    {
        // Vérifier si le joueur bouge pendant la copie
        if (isCopying && requireStillness && player != null)
        {
            // Si le joueur bouge trop, interrompre la copie
            if (player.GetComponent<CharacterController>().velocity.magnitude > 0.1f)
            {
                StopCopying();
                Debug.Log("[CopyZone] Copie interrompue - Le joueur a bougé.");
            }
        }
    }

    #endregion

    #region Copy Logic

    /// <summary>
    /// Démarre le processus de copie
    /// </summary>
    private void StartCopying()
    {
        if (isCopying || copyCompleted) return;

        isCopying = true;
        copyProgress = 0f;

        // Bloquer le mouvement du joueur
        if (player != null && requireStillness)
        {
            player.SetCanMove(false);
        }

        // Notifier le GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnCopyStarted();
        }

        // Démarrer la coroutine
        copyCoroutine = StartCoroutine(CopyCoroutine());

        Debug.Log("[CopyZone] Copie commencée...");
    }

    /// <summary>
    /// Arrête le processus de copie
    /// </summary>
    private void StopCopying()
    {
        if (!isCopying) return;

        isCopying = false;
        copyProgress = 0f;

        // Débloquer le joueur
        if (player != null)
        {
            player.SetCanMove(true);
        }

        // Arrêter la coroutine
        if (copyCoroutine != null)
        {
            StopCoroutine(copyCoroutine);
            copyCoroutine = null;
        }

        // Retour à l'état Playing
        if (GameManager.Instance != null && !copyCompleted)
        {
            GameManager.Instance.ChangeState(GameState.Playing);
        }
    }

    /// <summary>
    /// Coroutine qui gère le timer de copie
    /// </summary>
    private IEnumerator CopyCoroutine()
    {
        float elapsed = 0f;

        while (elapsed < copyDuration)
        {
            elapsed += Time.deltaTime;
            copyProgress = elapsed / copyDuration;

            // Ici, tu pourrais mettre à jour une barre de progression UI
            // UIManager.Instance.UpdateCopyProgress(copyProgress);

            yield return null;
        }

        // Copie terminée !
        OnCopyCompleted();
    }

    /// <summary>
    /// Appelé quand la copie est complétée avec succès
    /// </summary>
    private void OnCopyCompleted()
    {
        copyCompleted = true;
        isCopying = false;
        copyProgress = 1f;

        Debug.Log("[CopyZone] COPIE RÉUSSIE !");

        // Débloquer le joueur
        if (player != null)
        {
            player.SetCanMove(true);
        }

        // Désactiver l'indicateur
        if (indicator != null)
        {
            indicator.SetActive(false);
        }

        // Notifier le GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnCopyCompleted();
        }

        // Optionnel : Désactiver cette zone
        gameObject.SetActive(false);
    }

    #endregion

    #region Getters

    public bool IsCopying() => isCopying;
    public bool IsCopyCompleted() => copyCompleted;
    public float GetCopyProgress() => copyProgress;

    #endregion

    #region Debug

    private void OnDrawGizmos()
    {
        Gizmos.color = copyCompleted ? Color.gray : gizmoColor;
        
        // Visualiser la zone
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            
            if (col is BoxCollider box)
            {
                Gizmos.DrawWireCube(box.center, box.size);
            }
            else if (col is SphereCollider sphere)
            {
                Gizmos.DrawWireSphere(sphere.center, sphere.radius);
            }
        }
    }

    #endregion
}
