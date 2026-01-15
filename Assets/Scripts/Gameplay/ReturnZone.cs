using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class ReturnZone : MonoBehaviour
{
    [Header("Writing Settings")]
    [SerializeField] private float writingDuration = 5f;

    [Header("Visualization")]
    [SerializeField] private GameObject indicator;
    [SerializeField] private Color gizmoColor = Color.blue;

    private bool isActive = false;
    private bool playerInZone = false;
    private bool isWriting = false;
    private bool writingCompleted = false;
    private float writingProgress = 0f;

    private PlayerController player;
    private Coroutine writingCoroutine;
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
            indicator.SetActive(false);
        }
    }

    public void ActivateReturnZone()
    {
        isActive = true;

        if (indicator != null)
        {
            indicator.SetActive(true);
        }

        Debug.Log("[ReturnZone] Zone active ! Retournez à votre place.");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && isActive && !writingCompleted)
        {
            playerInZone = true;
            player = other.GetComponent<PlayerController>();

            Debug.Log("[ReturnZone] Appuyez sur E pour écrire...");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInZone = false;

            if (isWriting && !writingCompleted)
            {
                StopWriting();
                Debug.Log("[ReturnZone] Écriture interrompue - Retournez dans la zone.");
            }
        }
    }

    private void Update()
    {
        if (!playerInZone || !isActive || writingCompleted || isWriting) return;

        // Détecter la touche E
        if (inputActions.Player.Interact.triggered)
        {
            StartWriting();
        }
    }

    private void StartWriting()
    {
        if (isWriting || writingCompleted) return;

        isWriting = true;
        writingProgress = 0f;

        // Bloquer mouvement
        if (player != null)
        {
            player.SetCanMove(false);
        }

        writingCoroutine = StartCoroutine(WritingCoroutine());

        Debug.Log("[ReturnZone] Écriture en cours...");
    }

    private void StopWriting()
    {
        if (!isWriting) return;

        isWriting = false;
        writingProgress = 0f;

        if (player != null)
        {
            player.SetCanMove(true);
        }

        if (writingCoroutine != null)
        {
            StopCoroutine(writingCoroutine);
            writingCoroutine = null;
        }
    }

    private IEnumerator WritingCoroutine()
    {
        float elapsed = 0f;

        while (elapsed < writingDuration)
        {
            elapsed += Time.deltaTime;
            writingProgress = elapsed / writingDuration;

            yield return null;
        }

        OnWritingCompleted();
    }

    private void OnWritingCompleted()
    {
        writingCompleted = true;
        isWriting = false;
        writingProgress = 1f;

        Debug.Log("[ReturnZone] VICTOIRE !");

        if (player != null)
        {
            player.SetCanMove(true);
        }

        if (indicator != null)
        {
            indicator.SetActive(false);
        }

        // Notifier GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnMissionCompleted();
        }

        isActive = false;
    }

    public bool IsWriting() => isWriting;
    public float GetWritingProgress() => writingProgress;

    private void OnDrawGizmos()
    {
        Gizmos.color = isActive ? gizmoColor : Color.gray;

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
