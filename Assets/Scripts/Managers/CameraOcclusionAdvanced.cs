using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// [EN STAND-BY] Système avancé d'occlusion avec transparence circulaire autour du player
/// Rend transparent UNIQUEMENT la partie des objets dans un cercle autour du nombril/torse du player
///
/// DÉSACTIVÉ TEMPORAIREMENT - Cause des crashes Unity
/// À réimplémenter plus tard quand le gameplay sera fonctionnel
/// </summary>

// DÉSACTIVÉ - Décommenter quand prêt à réimplémenter
/*
public class CameraOcclusionAdvanced : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform target; // Le player
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Transform maskCenter; // Nombril/Torse du player
    [SerializeField] private CameraFollow cameraFollow;

    [Header("Mask Settings")]
    [Tooltip("Rayon du cercle de transparence autour du player")]
    [SerializeField] private float maskRadius = 2f;

    [Tooltip("Transparence minimale dans le cercle (0 = invisible, 1 = opaque)")]
    [Range(0f, 1f)]
    [SerializeField] private float minAlpha = 0.3f;

    [Tooltip("Distance de fade pour transition douce")]
    [SerializeField] private float fadeDistance = 0.5f;

    [Tooltip("Layers des objets qui peuvent devenir transparents")]
    [SerializeField] private LayerMask occlusionLayers = -1;

    [Header("Shader Material")]
    [Tooltip("Material avec le shader Custom/TransparentWithMask")]
    [SerializeField] private Material transparentMaskMaterial;

    private Dictionary<Renderer, MaterialData> occludedObjects = new Dictionary<Renderer, MaterialData>();
    private List<Renderer> currentlyOccluded = new List<Renderer>();

    private class MaterialData
    {
        public Material[] originalMaterials;
        public Material[] maskMaterials;
    }

    private void Start()
    {
        // Auto-find references
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;

                // Chercher le nombril/torse
                maskCenter = FindChildRecursive(target, "Spine") ?? target;
                Debug.Log($"[CameraOcclusionAdvanced] Mask center: {maskCenter.name}");
            }
            else
            {
                Debug.LogError("[CameraOcclusionAdvanced] Player non trouvé!");
            }
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (cameraFollow == null)
        {
            cameraFollow = FindObjectOfType<CameraFollow>();
        }

        if (transparentMaskMaterial == null)
        {
            Debug.LogError("[CameraOcclusionAdvanced] Transparent Mask Material non assigné!");
            enabled = false;
        }
    }

    private void LateUpdate()
    {
        if (target == null || mainCamera == null || maskCenter == null) return;

        // Désactiver en First Person (pas besoin d'occlusion)
        if (cameraFollow != null && cameraFollow.GetCameraMode() == CameraMode.FirstPerson)
        {
            // Restaurer tous les objets occludés
            foreach (var kvp in occludedObjects)
            {
                RestoreOriginalMaterial(kvp.Key, kvp.Value);
            }
            occludedObjects.Clear();
            return;
        }

        currentlyOccluded.Clear();

        // Update shader parameters (position du centre du masque)
        if (transparentMaskMaterial != null)
        {
            transparentMaskMaterial.SetVector("_MaskCenter", maskCenter.position);
            transparentMaskMaterial.SetFloat("_MaskRadius", maskRadius);
            transparentMaskMaterial.SetFloat("_MinAlpha", minAlpha);
            transparentMaskMaterial.SetFloat("_FadeDistance", fadeDistance);
        }

        // Raycast vers le player
        Vector3 directionToTarget = target.position - mainCamera.transform.position;
        float distanceToTarget = directionToTarget.magnitude;

        RaycastHit[] hits = Physics.RaycastAll(
            mainCamera.transform.position,
            directionToTarget.normalized,
            distanceToTarget,
            occlusionLayers
        );

        // Appliquer le shader masque aux objets touchés
        foreach (RaycastHit hit in hits)
        {
            if (hit.transform == target || hit.transform.IsChildOf(target))
                continue;

            Renderer renderer = hit.collider.GetComponent<Renderer>();
            if (renderer != null && renderer.enabled)
            {
                currentlyOccluded.Add(renderer);
                ApplyMaskMaterial(renderer);
            }
        }

        // Restaurer les objets qui ne sont plus occlusifs
        List<Renderer> toRemove = new List<Renderer>();
        foreach (var kvp in occludedObjects)
        {
            Renderer renderer = kvp.Key;
            if (renderer == null || !currentlyOccluded.Contains(renderer))
            {
                RestoreOriginalMaterial(renderer, kvp.Value);
                toRemove.Add(renderer);
            }
        }

        foreach (Renderer renderer in toRemove)
        {
            occludedObjects.Remove(renderer);
        }
    }

    private void ApplyMaskMaterial(Renderer renderer)
    {
        if (!occludedObjects.ContainsKey(renderer))
        {
            MaterialData data = new MaterialData();
            data.originalMaterials = renderer.materials;
            data.maskMaterials = new Material[data.originalMaterials.Length];

            // Créer des instances du material masque
            for (int i = 0; i < data.originalMaterials.Length; i++)
            {
                data.maskMaterials[i] = new Material(transparentMaskMaterial);

                // Copier la texture de base
                if (data.originalMaterials[i].HasProperty("_MainTex"))
                {
                    data.maskMaterials[i].SetTexture("_MainTex", data.originalMaterials[i].GetTexture("_MainTex"));
                }

                // Copier la couleur de base
                if (data.originalMaterials[i].HasProperty("_Color"))
                {
                    data.maskMaterials[i].SetColor("_Color", data.originalMaterials[i].GetColor("_Color"));
                }
            }

            occludedObjects.Add(renderer, data);
            renderer.materials = data.maskMaterials;
        }
    }

    private void RestoreOriginalMaterial(Renderer renderer, MaterialData data)
    {
        if (renderer != null && data.originalMaterials != null)
        {
            renderer.materials = data.originalMaterials;
        }

        // Cleanup des materials temporaires
        if (data.maskMaterials != null)
        {
            foreach (Material mat in data.maskMaterials)
            {
                if (mat != null)
                {
                    Destroy(mat);
                }
            }
        }
    }

    private Transform FindChildRecursive(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name.Contains(childName))
                return child;

            Transform found = FindChildRecursive(child, childName);
            if (found != null)
                return found;
        }
        return null;
    }

    private void OnDrawGizmos()
    {
        if (maskCenter != null)
        {
            // Dessiner le cercle de masque
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Gizmos.DrawWireSphere(maskCenter.position, maskRadius);

            // Dessiner le rayon de fade
            Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
            Gizmos.DrawWireSphere(maskCenter.position, maskRadius - fadeDistance);
        }
    }

    private void OnDisable()
    {
        // Cleanup
        foreach (var kvp in occludedObjects)
        {
            RestoreOriginalMaterial(kvp.Key, kvp.Value);
        }
        occludedObjects.Clear();
    }

    private void OnDestroy()
    {
        OnDisable();
    }
}
*/
