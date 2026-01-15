using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class CopyZone : MonoBehaviour
{
    [Header("Copy Settings")]
    [SerializeField] private float copyDuration = 5f;

    [Header("Visualization")]
    [SerializeField] private GameObject indicator;
    [SerializeField] private Color gizmoColor = Color.green;

    private bool playerInZone = false;
    private bool isCopying = false;
    private bool copyCompleted = false;
    private float copyProgress = 0f;

    private PlayerController player;
    private Coroutine copyCoroutine;
    private InputSystem_Actions inputActions;

    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (!col.isTrigger)
        {
            col.isTrigger = true;
        }

        inputActions = new InputSystem_Actions();
    }

    private void OnEnable()
    {
        inputActions.Player.Enable();
    }

    private void OnDisable()
    {
        inputActions.Player.Disable();
    }

    private void Start()
    {
        if (indicator != null)
        {
            indicator.SetActive(true);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !copyCompleted)
        {
            playerInZone = true;
            player = other.GetComponent<PlayerController>();

            Debug.Log("[CopyZone] Appuyez sur E pour copier...");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInZone = false;

            if (isCopying && !copyCompleted)
            {
                StopCopying();
                Debug.Log("[CopyZone] Copie interrompue - Retournez dans la zone.");
            }
        }
    }

    private void Update()
    {
        if (!playerInZone || copyCompleted || isCopying) return;

        // Détecter la touche E
        if (inputActions.Player.Interact.triggered)
        {
            StartCopying();
        }
    }

    private void StartCopying()
    {
        if (isCopying || copyCompleted) return;

        isCopying = true;
        copyProgress = 0f;

        // Bloquer mouvement
        if (player != null)
        {
            player.SetCanMove(false);
        }

        // Notifier GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnCopyStarted();
        }

        copyCoroutine = StartCoroutine(CopyCoroutine());

        Debug.Log("[CopyZone] Copie en cours...");
    }

    private void StopCopying()
    {
        if (!isCopying) return;

        isCopying = false;
        copyProgress = 0f;

        if (player != null)
        {
            player.SetCanMove(true);
        }

        if (copyCoroutine != null)
        {
            StopCoroutine(copyCoroutine);
            copyCoroutine = null;
        }

        if (GameManager.Instance != null && !copyCompleted)
        {
            GameManager.Instance.ChangeState(GameState.Playing);
        }
    }

    private IEnumerator CopyCoroutine()
    {
        float elapsed = 0f;

        while (elapsed < copyDuration)
        {
            elapsed += Time.deltaTime;
            copyProgress = elapsed / copyDuration;

            yield return null;
        }

        OnCopyCompleted();
    }

    private void OnCopyCompleted()
    {
        copyCompleted = true;
        isCopying = false;
        copyProgress = 1f;

        Debug.Log("[CopyZone] COPIE RÉUSSIE !");

        if (player != null)
        {
            player.SetCanMove(true);
        }

        if (indicator != null)
        {
            indicator.SetActive(false);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnCopyCompleted();
        }

        // Activer ReturnZone
        ReturnZone returnZone = FindFirstObjectByType<ReturnZone>();
        if (returnZone != null)
        {
            returnZone.ActivateReturnZone();
        }

        gameObject.SetActive(false);
    }

    public bool IsCopying() => isCopying;
    public bool IsCopyCompleted() => copyCompleted;
    public float GetCopyProgress() => copyProgress;

    private void OnDrawGizmos()
    {
        Gizmos.color = copyCompleted ? Color.gray : gizmoColor;

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
}
