using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Rend transparents les objets qui se trouvent entre la caméra et le player
/// </summary>
public class CameraOcclusionHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform target; // Le player
    [SerializeField] private Camera mainCamera;
    [SerializeField] private CameraFollow cameraFollow;

    [Header("Occlusion Settings")]
    [Tooltip("Distance autour du player où les objets deviennent transparents")]
    [SerializeField] private float occlusionRadius = 2f;

    [Tooltip("Transparence appliquée aux objets (0 = invisible, 1 = opaque)")]
    [Range(0f, 1f)]
    [SerializeField] private float transparencyAlpha = 0.3f;

    [Tooltip("Layer mask des objets qui peuvent devenir transparents")]
    [SerializeField] private LayerMask occlusionLayers = -1;

    [Tooltip("Vitesse de transition de transparence")]
    [SerializeField] private float fadeSpeed = 5f;

    // Tracking des objets occlusés
    private Dictionary<Renderer, MaterialData> occludedObjects = new Dictionary<Renderer, MaterialData>();
    private List<Renderer> currentlyOccluded = new List<Renderer>();

    private class MaterialData
    {
        public Material[] originalMaterials;
        public Material[] fadeMaterials;
        public float currentAlpha = 1f;
        public int renderQueue;
    }

    private void Start()
    {
        // Auto-find references si pas assignées
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
            }
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (cameraFollow == null)
        {
            cameraFollow = FindFirstObjectByType<CameraFollow>();
        }

        if (mainCamera == null)
        {
            Debug.LogError("[CameraOcclusionHandler] Main Camera not found!");
            enabled = false;
        }
    }

    private void LateUpdate()
    {
        if (target == null || mainCamera == null) return;

        // Désactiver en First Person (pas besoin d'occlusion)
        if (cameraFollow != null && cameraFollow.GetCameraMode() == CameraMode.FirstPerson)
        {
            // Restaurer tous les objets occludés
            foreach (var kvp in occludedObjects)
            {
                CleanupRenderer(kvp.Key);
            }
            occludedObjects.Clear();
            return;
        }

        currentlyOccluded.Clear();

        // Raycast de la caméra vers le player
        Vector3 directionToTarget = target.position - mainCamera.transform.position;
        float distanceToTarget = directionToTarget.magnitude;

        RaycastHit[] hits = Physics.RaycastAll(
            mainCamera.transform.position,
            directionToTarget.normalized,
            distanceToTarget,
            occlusionLayers
        );

        // Traiter tous les objets touchés par le raycast
        foreach (RaycastHit hit in hits)
        {
            // Ignorer le player lui-même
            if (hit.transform == target || hit.transform.IsChildOf(target))
                continue;

            Renderer renderer = hit.collider.GetComponent<Renderer>();
            if (renderer != null && renderer.enabled)
            {
                currentlyOccluded.Add(renderer);
                FadeOut(renderer);
            }
        }

        // Restaurer les objets qui ne sont plus occlusifs
        List<Renderer> toRemove = new List<Renderer>();
        foreach (var kvp in occludedObjects)
        {
            Renderer renderer = kvp.Key;
            if (renderer == null || !currentlyOccluded.Contains(renderer))
            {
                FadeIn(renderer, kvp.Value);

                // Si complètement restauré, marquer pour suppression
                if (kvp.Value.currentAlpha >= 0.99f)
                {
                    toRemove.Add(renderer);
                }
            }
        }

        // Nettoyer les objets restaurés
        foreach (Renderer renderer in toRemove)
        {
            CleanupRenderer(renderer);
        }
    }

    private void FadeOut(Renderer renderer)
    {
        if (!occludedObjects.ContainsKey(renderer))
        {
            // Première fois qu'on occlut cet objet
            MaterialData data = new MaterialData();
            data.originalMaterials = renderer.materials;
            data.fadeMaterials = new Material[data.originalMaterials.Length];

            // Créer des copies des matériaux pour le fade
            for (int i = 0; i < data.originalMaterials.Length; i++)
            {
                data.fadeMaterials[i] = new Material(data.originalMaterials[i]);

                // Passer en mode transparent si ce n'est pas déjà le cas
                SetupTransparentMaterial(data.fadeMaterials[i]);
            }

            data.currentAlpha = 1f;
            data.renderQueue = data.originalMaterials[0].renderQueue;

            occludedObjects.Add(renderer, data);
            renderer.materials = data.fadeMaterials;
        }

        MaterialData materialData = occludedObjects[renderer];

        // Fade vers la transparence
        materialData.currentAlpha = Mathf.MoveTowards(
            materialData.currentAlpha,
            transparencyAlpha,
            fadeSpeed * Time.deltaTime
        );

        // Appliquer l'alpha à tous les matériaux
        foreach (Material mat in materialData.fadeMaterials)
        {
            Color color = mat.color;
            color.a = materialData.currentAlpha;
            mat.color = color;
        }
    }

    private void FadeIn(Renderer renderer, MaterialData data)
    {
        if (renderer == null || data == null) return;

        // Fade vers l'opacité
        data.currentAlpha = Mathf.MoveTowards(
            data.currentAlpha,
            1f,
            fadeSpeed * Time.deltaTime
        );

        // Appliquer l'alpha à tous les matériaux
        if (data.fadeMaterials != null)
        {
            foreach (Material mat in data.fadeMaterials)
            {
                if (mat != null)
                {
                    Color color = mat.color;
                    color.a = data.currentAlpha;
                    mat.color = color;
                }
            }
        }
    }

    private void SetupTransparentMaterial(Material material)
    {
        // Configurer le matériau pour supporter la transparence
        material.SetFloat("_Mode", 3); // Transparent mode
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = 3000; // Transparent queue
    }

    private void CleanupRenderer(Renderer renderer)
    {
        if (renderer != null && occludedObjects.ContainsKey(renderer))
        {
            MaterialData data = occludedObjects[renderer];

            // Restaurer les matériaux originaux
            if (data.originalMaterials != null)
            {
                renderer.materials = data.originalMaterials;
            }

            // Détruire les matériaux temporaires
            if (data.fadeMaterials != null)
            {
                foreach (Material mat in data.fadeMaterials)
                {
                    if (mat != null)
                    {
                        Destroy(mat);
                    }
                }
            }

            occludedObjects.Remove(renderer);
        }
    }

    private void OnDisable()
    {
        // Restaurer tous les objets quand le script est désactivé
        foreach (var kvp in occludedObjects)
        {
            CleanupRenderer(kvp.Key);
        }
        occludedObjects.Clear();
    }

    private void OnDestroy()
    {
        // Cleanup au destroy
        OnDisable();
    }

    private void OnDrawGizmosSelected()
    {
        if (target == null || mainCamera == null) return;

        // Dessiner le raycast vers le player
        Gizmos.color = Color.red;
        Gizmos.DrawLine(mainCamera.transform.position, target.position);

        // Dessiner le rayon d'occlusion autour du player
        Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
        Gizmos.DrawWireSphere(target.position, occlusionRadius);
    }
}
