using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Système de caméra avec support First Person et Third Person
/// Toggle entre les deux modes avec la touche V
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target; // Le joueur à suivre

    [Header("Third Person Settings")]
    [Tooltip("Offset de la caméra en third person - Vue isométrique gaming (angle ~56°)")]
    [SerializeField] private Vector3 thirdPersonOffset = new Vector3(0, 12f, -8f);
    [SerializeField] private bool followX = true;
    [SerializeField] private bool followY = false;
    [SerializeField] private bool followZ = true;
    [SerializeField] private float smoothSpeed = 5f;
    [SerializeField] private bool lookAtTarget = true;

    [Header("First Person Settings")]
    [Tooltip("GameObjects à cacher en FP (tête, cheveux, etc.)")]
    [SerializeField] private GameObject[] meshesToHide;

    [Tooltip("Offset de la caméra quand DEBOUT (X=avant/arrière, Y=hauteur, Z=gauche/droite)")]
    [SerializeField] private Vector3 standingEyeOffset = new Vector3(0f, 1.5f, 0f);

    [Tooltip("Offset de la caméra quand ACCROUPI (X=avant/arrière, Y=hauteur, Z=gauche/droite)")]
    [SerializeField] private Vector3 crouchingEyeOffset = new Vector3(0.2f, 0.9f, 0f);
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float minVerticalAngle = -60f;
    [SerializeField] private float maxVerticalAngle = 60f;

    [Header("Camera Mode")]
    [SerializeField] private CameraMode currentMode = CameraMode.ThirdPerson;

    [Header("Projection Settings")]
    [Tooltip("Taille orthographique pour Third Person")]
    [SerializeField] private float orthographicSize = 10f;

    // Input
    private InputSystem_Actions inputActions;

    private Camera mainCamera;

    // First person rotation
    private float rotationX = 0f;
    private float rotationY = 0f;

    private void Awake()
    {
        inputActions = new InputSystem_Actions();
        mainCamera = GetComponent<Camera>();

        if (mainCamera == null)
        {
            Debug.LogError("[CameraFollow] Aucun component Camera trouvé!");
        }
    }

    private void OnEnable()
    {
        inputActions.Player.Enable();
        inputActions.Player.ToggleCamera.performed += _ => ToggleCameraMode();
    }

    private void OnDisable()
    {
        inputActions.Player.ToggleCamera.performed -= _ => ToggleCameraMode();
        inputActions.Player.Disable();
    }

    private void Start()
    {
        // Si pas de target assignée, chercher le joueur automatiquement
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
                Debug.Log("[CameraFollow] Target trouvée automatiquement : " + player.name);
            }
            else
            {
                Debug.LogError("[CameraFollow] Aucun joueur trouvé ! Assigne le target manuellement ou tag le joueur avec 'Player'");
            }
        }

        // Chercher automatiquement les meshes si pas assignés
        if ((meshesToHide == null || meshesToHide.Length == 0) && target != null)
        {
            List<GameObject> foundMeshes = new List<GameObject>();

            // Chercher la tête
            Transform headTransform = FindChildRecursive(target, "Head");
            if (headTransform != null)
            {
                MeshRenderer meshRenderer = headTransform.GetComponent<MeshRenderer>();
                if (meshRenderer != null)
                {
                    foundMeshes.Add(meshRenderer.gameObject);
                    Debug.Log($"[CameraFollow] ✅ Head Mesh trouvé : {meshRenderer.gameObject.name}");
                }
            }

            // Chercher les cheveux
            Transform hairTransform = FindChildRecursive(target, "Hair");
            if (hairTransform != null)
            {
                MeshRenderer hairRenderer = hairTransform.GetComponent<MeshRenderer>();
                if (hairRenderer != null)
                {
                    foundMeshes.Add(hairRenderer.gameObject);
                    Debug.Log($"[CameraFollow] ✅ Hair Mesh trouvé : {hairRenderer.gameObject.name}");
                }
            }

            if (foundMeshes.Count > 0)
            {
                meshesToHide = foundMeshes.ToArray();
                Debug.Log($"[CameraFollow] {meshesToHide.Length} mesh(es) à cacher trouvé(s)");
            }
            else
            {
                Debug.LogWarning("[CameraFollow] ⚠️ Aucun mesh à cacher trouvé automatiquement");
            }
        }

        // FORCER l'activation de CETTE caméra au démarrage
        if (mainCamera != null)
        {
            mainCamera.enabled = true;
            Debug.Log($"[CameraFollow] ✅ {gameObject.name} caméra forcée ACTIVE au démarrage");
        }

        // Désactiver TOUTES les autres caméras (sauf cette caméra Third Person)
        Camera[] allCameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
        int disabledCount = 0;
        foreach (Camera cam in allCameras)
        {
            if (cam != mainCamera && cam.enabled)
            {
                cam.enabled = false;
                disabledCount++;
                Debug.Log($"[CameraFollow] ❌ Caméra '{cam.name}' désactivée (pour forcer Third Person)");
            }
        }
        if (disabledCount > 0)
        {
            Debug.Log($"[CameraFollow] {disabledCount} autre(s) caméra(s) désactivée(s) pour garantir Third Person au démarrage");
        }

        // Initialiser la position/rotation selon le mode
        if (target != null)
        {
            if (currentMode == CameraMode.ThirdPerson)
            {
                InitializeThirdPerson();
            }
            else
            {
                InitializeFirstPerson();
            }
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;

        if (currentMode == CameraMode.ThirdPerson)
        {
            UpdateThirdPerson();
        }
        else
        {
            UpdateFirstPerson();
        }
    }

    /// <summary>
    /// Vérifie si le jeu est en pause (Time.timeScale == 0 ou menu ouvert)
    /// </summary>
    private bool IsGamePaused()
    {
        // Vérifier via UIManager si disponible
        if (UIManager.Instance != null)
        {
            return !UIManager.Instance.IsGameplayActive();
        }
        // Fallback: vérifier Time.timeScale
        return Time.timeScale == 0f;
    }

    private void ToggleCameraMode()
    {
        if (currentMode == CameraMode.ThirdPerson)
        {
            currentMode = CameraMode.FirstPerson;
            InitializeFirstPerson();
            Debug.Log("[CameraFollow] Switched to FIRST PERSON");
        }
        else
        {
            currentMode = CameraMode.ThirdPerson;
            InitializeThirdPerson();
            Debug.Log("[CameraFollow] Switched to THIRD PERSON");
        }
    }

    #region Third Person

    private void InitializeThirdPerson()
    {
        Vector3 initialPosition = CalculateThirdPersonPosition();
        transform.position = initialPosition;

        if (lookAtTarget)
        {
            transform.LookAt(target.position);
        }

        // Réactiver les meshes en Third Person
        if (meshesToHide != null)
        {
            foreach (GameObject mesh in meshesToHide)
            {
                if (mesh != null)
                {
                    mesh.SetActive(true);
                }
            }
            Debug.Log($"[CameraFollow] 👁️ {meshesToHide.Length} mesh(es) réactivé(s)");
        }

        // Passer en projection Orthographique
        if (mainCamera != null)
        {
            mainCamera.orthographic = true;
            mainCamera.orthographicSize = orthographicSize;
            Debug.Log("[CameraFollow] Third Person: Projection Orthographique activée");
        }
    }

    private void UpdateThirdPerson()
    {
        // Calculer la position cible
        Vector3 targetPosition = CalculateThirdPersonPosition();

        // Smooth ou instantané
        if (smoothSpeed > 0)
        {
            transform.position = Vector3.Lerp(
                transform.position,
                targetPosition,
                smoothSpeed * Time.deltaTime
            );
        }
        else
        {
            transform.position = targetPosition;
        }

        // Regarder le joueur
        if (lookAtTarget)
        {
            transform.LookAt(target.position);
        }
    }

    private Vector3 CalculateThirdPersonPosition()
    {
        Vector3 targetPos = target.position + thirdPersonOffset;

        // Appliquer les contraintes d'axes
        if (!followX)
        {
            targetPos.x = transform.position.x;
        }

        if (!followY)
        {
            targetPos.y = transform.position.y;
        }

        if (!followZ)
        {
            targetPos.z = transform.position.z;
        }

        return targetPos;
    }

    #endregion

    #region First Person

    private void InitializeFirstPerson()
    {
        // Position initiale (utilise headTransform si disponible)
        Vector3 initialPosition = GetFirstPersonPosition();
        transform.position = initialPosition;

        // Désactiver les meshes en First Person
        if (meshesToHide != null)
        {
            foreach (GameObject mesh in meshesToHide)
            {
                if (mesh != null)
                {
                    mesh.SetActive(false);
                }
            }
            Debug.Log($"[CameraFollow] 🙈 {meshesToHide.Length} mesh(es) désactivé(s)");
        }

        // Rotation initiale (regarder vers l'avant du player)
        rotationY = target.eulerAngles.y;
        rotationX = 0f;

        transform.rotation = Quaternion.Euler(rotationX, rotationY, 0f);

        // Passer en projection Perspective
        if (mainCamera != null)
        {
            mainCamera.orthographic = false;
            mainCamera.nearClipPlane = 0.1f; // Réduit le clipping des murs proches
            Debug.Log("[CameraFollow] First Person: Projection Perspective activée");
        }

        // Debug info
        Debug.Log($"[CameraFollow] First Person initialized at: {initialPosition}");
        Debug.Log($"[CameraFollow] Target position: {target.position}");
        Debug.Log($"[CameraFollow] Eye offset (standing): {standingEyeOffset}");
        Debug.Log($"[CameraFollow] Camera active: {GetComponent<Camera>().enabled}");

        // Vérifier s'il y a d'autres caméras actives
        Camera[] allCameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
        int activeCameras = 0;
        foreach (Camera cam in allCameras)
        {
            if (cam.enabled)
            {
                activeCameras++;
                Debug.Log($"[CameraFollow] Active camera found: {cam.name} (depth: {cam.depth})");
            }
        }
        if (activeCameras > 1)
        {
            Debug.LogWarning($"[CameraFollow] ⚠️ {activeCameras} caméras actives détectées! Il devrait y en avoir qu'une seule.");
        }
    }

    private void UpdateFirstPerson()
    {
        // Position suit le joueur
        Vector3 targetPosition = GetFirstPersonPosition();
        transform.position = targetPosition;

        // NE PAS traiter les inputs souris si le jeu est en pause
        if (IsGamePaused())
        {
            return;
        }

        // Lecture des inputs de la souris
        Vector2 lookInput = inputActions.Player.Look.ReadValue<Vector2>();

        // Appliquer le multiplicateur de sensibilité depuis GameSettings
        float sensitivity = mouseSensitivity;
        if (GameSettings.Instance != null)
        {
            sensitivity *= GameSettings.Instance.GetMouseSensitivityMultiplier();
        }

        // Rotation horizontale (Y axis)
        rotationY += lookInput.x * sensitivity;

        // Rotation verticale (X axis) - clamped
        rotationX -= lookInput.y * sensitivity;
        rotationX = Mathf.Clamp(rotationX, minVerticalAngle, maxVerticalAngle);

        // Appliquer rotation
        transform.rotation = Quaternion.Euler(rotationX, rotationY, 0f);
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Permet de changer le target en cours de jeu
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    /// <summary>
    /// Change le mode de caméra manuellement
    /// </summary>
    public void SetCameraMode(CameraMode mode)
    {
        if (currentMode == mode) return;

        currentMode = mode;

        if (currentMode == CameraMode.ThirdPerson)
        {
            InitializeThirdPerson();
        }
        else
        {
            InitializeFirstPerson();
        }
    }

    public CameraMode GetCameraMode() => currentMode;

    #endregion

    #region Helper Methods

    /// <summary>
    /// Calcule la position First Person avec offset complet (X, Y, Z)
    /// </summary>
    private Vector3 GetFirstPersonPosition()
    {
        // Vérifier si le player est accroupi
        PlayerController playerController = target.GetComponent<PlayerController>();
        bool isCrouching = false;

        if (playerController != null)
        {
            isCrouching = playerController.IsCrouching();
        }

        // Choisir l'offset selon l'état
        Vector3 eyeOffset = isCrouching ? crouchingEyeOffset : standingEyeOffset;

        // Position = Position du player + offset relatif à la rotation du player
        Vector3 cameraPosition = target.position + target.TransformDirection(eyeOffset);

        return cameraPosition;
    }

    /// <summary>
    /// Cherche récursivement un enfant par nom
    /// </summary>
    private Transform FindChildRecursive(Transform parent, string childName)
    {
        // Chercher dans les enfants directs
        foreach (Transform child in parent)
        {
            if (child.name.Equals(childName, System.StringComparison.OrdinalIgnoreCase))
            {
                return child;
            }

            // Chercher récursivement dans les enfants de l'enfant
            Transform found = FindChildRecursive(child, childName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    #endregion

    #region Gizmos

    /// <summary>
    /// Visualiser l'offset dans la Scene View
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (target == null) return;

        if (currentMode == CameraMode.ThirdPerson)
        {
            Gizmos.color = Color.yellow;
            Vector3 targetPosition = target.position + thirdPersonOffset;
            Gizmos.DrawLine(target.position, targetPosition);
            Gizmos.DrawWireSphere(targetPosition, 0.5f);

            // Dessiner l'angle
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(target.position, target.position + Vector3.up * 17.3f);
            Gizmos.DrawLine(target.position, target.position + Vector3.back * 10f);
        }
        else
        {
            // Position debout (vert)
            Gizmos.color = Color.green;
            Vector3 standingPosition = target.position + target.TransformDirection(standingEyeOffset);
            Gizmos.DrawWireSphere(standingPosition, 0.2f);
            Gizmos.DrawLine(target.position, standingPosition);

            // Position accroupie (jaune)
            Gizmos.color = Color.yellow;
            Vector3 crouchingPosition = target.position + target.TransformDirection(crouchingEyeOffset);
            Gizmos.DrawWireSphere(crouchingPosition, 0.2f);
            Gizmos.DrawLine(target.position, crouchingPosition);
        }
    }

    #endregion
}

public enum CameraMode
{
    ThirdPerson,
    FirstPerson
}
