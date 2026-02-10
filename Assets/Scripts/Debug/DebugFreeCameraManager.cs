using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Système NoClip indépendant avec caméra séparée
/// Setup:
/// 1. Créer GameObject vide "DebugCameraManager"
/// 2. Ajouter ce script
/// 3. Le script créera automatiquement une Debug Camera
/// 4. Appuyer sur F pour toggle
/// Compatible avec Old et New Input System
/// </summary>
public class DebugFreeCameraManager : MonoBehaviour
{
    [Header("Toggle")]
    [SerializeField] private KeyCode toggleKey = KeyCode.F;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 15f;
    [SerializeField] private float sprintMultiplier = 2f;
    [SerializeField] private float mouseSensitivity = 3f;

    [Header("References (Auto-setup)")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Camera debugCamera;
    [SerializeField] private PlayerController playerController;

    [Header("Debug Info")]
    [SerializeField] private bool showDebugLogs = false; // Désactivé par défaut pour réduire les logs

    private bool isDebugCamActive = false;
    private float rotationX = 0f;
    private float rotationY = 0f;

    private void Start()
    {
        LogDebug("=== DebugFreeCameraManager Start ===");

        // Trouver caméra player automatiquement
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            LogDebug($"Player Camera auto-trouvée: {(playerCamera != null ? playerCamera.name : "NULL")}");
        }
        else
        {
            LogDebug($"Player Camera assignée: {playerCamera.name}");
        }

        // Trouver PlayerController
        if (playerController == null)
        {
            playerController = FindFirstObjectByType<PlayerController>();
            LogDebug($"PlayerController auto-trouvé: {(playerController != null ? "Oui" : "NULL")}");
        }
        else
        {
            LogDebug($"PlayerController assigné: {playerController.name}");
        }

        // Créer Debug Camera si elle n'existe pas
        if (debugCamera == null)
        {
            LogDebug("Debug Camera NULL, création automatique...");
            CreateDebugCamera();
        }
        else
        {
            LogDebug($"Debug Camera assignée: {debugCamera.name}");

            // Vérifier et gérer AudioListener
            AudioListener debugListener = debugCamera.GetComponent<AudioListener>();
            if (debugListener != null)
            {
                LogDebug("AudioListener trouvé sur Debug Camera, sera géré au toggle");
                debugListener.enabled = false;
            }
        }

        // Désactiver debug camera au démarrage
        if (debugCamera != null)
        {
            debugCamera.enabled = false;
            debugCamera.gameObject.SetActive(false);
            LogDebug("Debug Camera désactivée au démarrage");
        }

        LogDebug($"Setup terminé. Appuyez sur {toggleKey} pour toggle NoClip");
        LogDebug("=================================");
    }

    private void CreateDebugCamera()
    {
        // Créer GameObject pour debug camera
        GameObject debugCamObj = new GameObject("DebugCamera");
        debugCamObj.transform.SetParent(transform);

        // Ajouter Camera component
        debugCamera = debugCamObj.AddComponent<Camera>();

        // Copier settings de la player camera
        if (playerCamera != null)
        {
            debugCamera.fieldOfView = playerCamera.fieldOfView;
            debugCamera.nearClipPlane = playerCamera.nearClipPlane;
            debugCamera.farClipPlane = playerCamera.farClipPlane;
        }

        // Tag pour AudioListener
        AudioListener debugListener = debugCamObj.AddComponent<AudioListener>();
        debugListener.enabled = false; // Désactivé par défaut

        // Désactiver immédiatement la caméra (ne sera activée que lors du toggle)
        debugCamera.enabled = false;
        debugCamObj.SetActive(false);

        Debug.Log("[DebugCamera] Debug Camera créée automatiquement (désactivée)");
    }

    private void Update()
    {
        // Support pour Old et New Input System
        bool togglePressed = false;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
        {
            togglePressed = true;
            LogDebug("F pressé (New Input System)");
        }
#else
        if (Input.GetKeyDown(toggleKey))
        {
            togglePressed = true;
            LogDebug("F pressé (Old Input System)");
        }
#endif

        if (togglePressed)
        {
            ToggleDebugCamera();
        }

        if (isDebugCamActive && debugCamera != null)
        {
            HandleMovement();
            HandleRotation();
        }
    }

    private void ToggleDebugCamera()
    {
        isDebugCamActive = !isDebugCamActive;

        if (isDebugCamActive)
        {
            ActivateDebugCamera();
        }
        else
        {
            DeactivateDebugCamera();
        }
    }

    private void ActivateDebugCamera()
    {
        LogDebug("=== ACTIVATION MODE NOCLIP ===");

        // Placer debug camera à position de player camera
        if (playerCamera != null && debugCamera != null)
        {
            debugCamera.transform.position = playerCamera.transform.position;
            debugCamera.transform.rotation = playerCamera.transform.rotation;

            // Init rotation pour mouse look
            rotationX = debugCamera.transform.eulerAngles.y;
            rotationY = -debugCamera.transform.eulerAngles.x;

            LogDebug($"Debug Camera positionnée à: {debugCamera.transform.position}");
        }

        // Activer debug camera
        if (debugCamera != null)
        {
            debugCamera.gameObject.SetActive(true);
            debugCamera.enabled = true;
            LogDebug("Debug Camera activée");
        }

        // Désactiver player camera
        if (playerCamera != null)
        {
            playerCamera.enabled = false;
            LogDebug("Player Camera désactivée");
        }

        // Bloquer player
        if (playerController != null)
        {
            playerController.SetCanMove(false);
            LogDebug("Player bloqué");
        }

        // Gérer AudioListeners
        if (playerCamera != null)
        {
            AudioListener playerListener = playerCamera.GetComponent<AudioListener>();
            if (playerListener != null)
            {
                playerListener.enabled = false;
                LogDebug("AudioListener Player Camera désactivé");
            }
        }

        // Activer AudioListener de debug camera si présent
        if (debugCamera != null)
        {
            AudioListener debugListener = debugCamera.GetComponent<AudioListener>();
            if (debugListener != null)
            {
                debugListener.enabled = true;
                LogDebug("AudioListener Debug Camera activé");
            }
        }

        // Lock cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Debug.Log("[DebugCamera] ✅ Mode NoClip ACTIVÉ (F pour désactiver) - ZQSD/Space/Ctrl pour bouger");
    }

    private void DeactivateDebugCamera()
    {
        LogDebug("=== DÉSACTIVATION MODE NOCLIP ===");

        // Désactiver debug camera
        if (debugCamera != null)
        {
            // Désactiver AudioListener de debug camera si présent
            AudioListener debugListener = debugCamera.GetComponent<AudioListener>();
            if (debugListener != null)
            {
                debugListener.enabled = false;
                LogDebug("AudioListener Debug Camera désactivé");
            }

            debugCamera.enabled = false;
            debugCamera.gameObject.SetActive(false);
            LogDebug("Debug Camera désactivée");
        }

        // Réactiver player camera
        if (playerCamera != null)
        {
            playerCamera.enabled = true;
            LogDebug("Player Camera réactivée");
        }

        // Débloquer player
        if (playerController != null)
        {
            playerController.SetCanMove(true);
            LogDebug("Player débloqué");
        }

        // Réactiver AudioListener de player camera
        if (playerCamera != null)
        {
            AudioListener playerListener = playerCamera.GetComponent<AudioListener>();
            if (playerListener != null)
            {
                playerListener.enabled = true;
                LogDebug("AudioListener Player Camera réactivé");
            }
        }

        // Lock cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Debug.Log("[DebugCamera] ❌ Mode NoClip DÉSACTIVÉ");
    }

    private void HandleMovement()
    {
        Vector3 moveDirection = Vector3.zero;

        // Support pour New et Old Input System
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            // ZQSD / WASD
            if (Keyboard.current.wKey.isPressed || Keyboard.current.zKey.isPressed)
                moveDirection += debugCamera.transform.forward;

            if (Keyboard.current.sKey.isPressed)
                moveDirection -= debugCamera.transform.forward;

            if (Keyboard.current.aKey.isPressed || Keyboard.current.qKey.isPressed)
                moveDirection -= debugCamera.transform.right;

            if (Keyboard.current.dKey.isPressed)
                moveDirection += debugCamera.transform.right;

            // Espace / Ctrl (monter/descendre)
            if (Keyboard.current.spaceKey.isPressed)
                moveDirection += Vector3.up;

            if (Keyboard.current.leftCtrlKey.isPressed)
                moveDirection -= Vector3.up;
        }
#else
        // ZQSD / WASD
        if (Input.GetKey(KeyCode.Z) || Input.GetKey(KeyCode.W))
            moveDirection += debugCamera.transform.forward;

        if (Input.GetKey(KeyCode.S))
            moveDirection -= debugCamera.transform.forward;

        if (Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.A))
            moveDirection -= debugCamera.transform.right;

        if (Input.GetKey(KeyCode.D))
            moveDirection += debugCamera.transform.right;

        // Espace / Ctrl (monter/descendre)
        if (Input.GetKey(KeyCode.Space))
            moveDirection += Vector3.up;

        if (Input.GetKey(KeyCode.LeftControl))
            moveDirection -= Vector3.up;
#endif

        // Sprint
        float speed = moveSpeed;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed)
            speed *= sprintMultiplier;
#else
        if (Input.GetKey(KeyCode.LeftShift))
            speed *= sprintMultiplier;
#endif

        // Appliquer mouvement
        debugCamera.transform.position += moveDirection.normalized * speed * Time.deltaTime;
    }

    private void HandleRotation()
    {
        float mouseX = 0f;
        float mouseY = 0f;

        // Support pour New et Old Input System
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();
            mouseX = mouseDelta.x * mouseSensitivity * 0.1f; // Ajusté pour correspondre au sens de GetAxis
            mouseY = mouseDelta.y * mouseSensitivity * 0.1f;
        }
#else
        mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
#endif

        rotationX += mouseX;
        rotationY += mouseY;
        rotationY = Mathf.Clamp(rotationY, -90f, 90f);

        debugCamera.transform.rotation = Quaternion.Euler(-rotationY, rotationX, 0f);
    }

    private void OnDestroy()
    {
        // Cleanup - réactiver player camera si le script est détruit
        if (debugCamera != null && debugCamera.gameObject != null)
        {
            Destroy(debugCamera.gameObject);
        }

        if (playerCamera != null)
        {
            playerCamera.enabled = true;
        }

        if (playerController != null)
        {
            playerController.SetCanMove(true);
        }
    }

    private void LogDebug(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[DebugFreeCameraManager] {message}");
        }
    }
}
