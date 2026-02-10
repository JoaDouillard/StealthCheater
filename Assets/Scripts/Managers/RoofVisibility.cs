using UnityEngine;

/// <summary>
/// Gère la visibilité du toit en fonction du mode de caméra
/// - Third Person: Toit VISIBLE (SetActive true)
/// - First Person: Toit CACHÉ (SetActive false)
/// </summary>
public class RoofVisibility : MonoBehaviour
{
    [Header("Roof Reference")]
    [Tooltip("GameObject du toit à afficher/cacher")]
    [SerializeField] private GameObject roofObject;

    [Header("Camera Reference")]
    [Tooltip("CameraFollow pour détecter le mode (trouvée automatiquement si vide)")]
    [SerializeField] private CameraFollow cameraFollow;

    [Header("Debug")]
    [Tooltip("Afficher les logs de debug")]
    [SerializeField] private bool showDebugLogs = false;

    private CameraMode lastCameraMode;

    private void Start()
    {
        // Trouver CameraFollow automatiquement si pas assignée
        if (cameraFollow == null)
        {
            cameraFollow = FindFirstObjectByType<CameraFollow>();

            if (cameraFollow == null)
            {
                Debug.LogError("[RoofVisibility] ❌ CameraFollow introuvable! Assigne-la manuellement dans l'Inspector.");
                enabled = false;
                return;
            }

            if (showDebugLogs)
            {
                Debug.Log($"[RoofVisibility] ✅ CameraFollow trouvée automatiquement: {cameraFollow.name}");
            }
        }

        // Vérifier que le toit est assigné
        if (roofObject == null)
        {
            Debug.LogError("[RoofVisibility] ❌ roofObject non assigné! Assigne le GameObject du toit dans l'Inspector.");
            enabled = false;
            return;
        }

        // Initialiser avec le mode actuel
        lastCameraMode = cameraFollow.GetCameraMode();
        UpdateRoofVisibility(lastCameraMode);
    }

    private void Update()
    {
        if (cameraFollow == null || roofObject == null) return;

        // Vérifier si le mode a changé
        CameraMode currentMode = cameraFollow.GetCameraMode();

        if (currentMode != lastCameraMode)
        {
            lastCameraMode = currentMode;
            UpdateRoofVisibility(currentMode);
        }
    }

    /// <summary>
    /// Met à jour la visibilité du toit selon le mode de caméra
    /// </summary>
    private void UpdateRoofVisibility(CameraMode mode)
    {
        if (roofObject == null) return;

        if (mode == CameraMode.FirstPerson)
        {
            // First Person: CACHER le toit
            roofObject.SetActive(true);

            if (showDebugLogs)
            {
                Debug.Log($"[RoofVisibility] 🙈 Toit CACHÉ (First Person)");
            }
        }
        else
        {
            // Third Person: AFFICHER le toit
            roofObject.SetActive(false);

            if (showDebugLogs)
            {
                Debug.Log($"[RoofVisibility] 👁️ Toit VISIBLE (Third Person)");
            }
        }
    }

    /// <summary>
    /// Permet de changer le toit en cours de jeu
    /// </summary>
    public void SetRoofObject(GameObject newRoof)
    {
        roofObject = newRoof;

        if (cameraFollow != null)
        {
            UpdateRoofVisibility(cameraFollow.GetCameraMode());
        }
    }

    /// <summary>
    /// Force la mise à jour de la visibilité
    /// </summary>
    public void ForceUpdateVisibility()
    {
        if (cameraFollow != null)
        {
            UpdateRoofVisibility(cameraFollow.GetCameraMode());
        }
    }
}
