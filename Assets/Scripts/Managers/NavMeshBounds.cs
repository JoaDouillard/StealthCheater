using UnityEngine;

/// <summary>
/// Définit une zone rectangulaire pour limiter le NavMesh
/// Utilisé avec NavMeshSurface pour ne construire le NavMesh que dans cette zone
///
/// Setup:
/// 1. Créer un GameObject "NavMeshBounds" dans chaque Level_Props
/// 2. Ajouter ce script
/// 3. Positionner et redimensionner la zone dans la Scene View
/// 4. Assigner ce GameObject dans NavMeshSurface.collectObjects = "Volume"
/// 5. Drag-and-drop ce GameObject dans NavMeshSurface.collectSources
///
/// Pourquoi ?
/// - Limite le NavMesh à la salle de classe uniquement
/// - Évite de bake le NavMesh sur toute la map
/// - Améliore les performances et évite les erreurs
/// </summary>
public class NavMeshBounds : MonoBehaviour
{
    [Header("Bounds Configuration")]
    [Tooltip("Taille de la zone NavMesh (X, Y, Z en mètres)")]
    [SerializeField] private Vector3 boundsSize = new Vector3(20f, 5f, 20f);

    [Header("Visualization")]
    [Tooltip("Afficher la zone dans la Scene View")]
    [SerializeField] private bool showGizmos = true;

    [Tooltip("Couleur de la zone")]
    [SerializeField] private Color gizmoColor = new Color(0f, 1f, 1f, 0.3f);

    [Tooltip("Couleur des bords de la zone")]
    [SerializeField] private Color gizmoWireColor = new Color(0f, 1f, 1f, 1f);

    /// <summary>
    /// Retourne les bounds de la zone
    /// </summary>
    public Bounds GetBounds()
    {
        return new Bounds(transform.position, boundsSize);
    }

    /// <summary>
    /// Dessine la zone dans la Scene View
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!showGizmos)
            return;

        // Dessiner la zone remplie
        Gizmos.color = gizmoColor;
        Gizmos.DrawCube(transform.position, boundsSize);

        // Dessiner les bords de la zone
        Gizmos.color = gizmoWireColor;
        Gizmos.DrawWireCube(transform.position, boundsSize);

        // Dessiner un label
        DrawLabel();
    }

    /// <summary>
    /// Dessine un label dans la Scene View
    /// </summary>
    private void DrawLabel()
    {
#if UNITY_EDITOR
        Vector3 labelPosition = transform.position + Vector3.up * (boundsSize.y / 2f + 1f);
        UnityEditor.Handles.Label(labelPosition, $"NavMesh Bounds\n{boundsSize.x}x{boundsSize.y}x{boundsSize.z}m",
            new GUIStyle()
            {
                normal = new GUIStyleState() { textColor = gizmoWireColor },
                fontStyle = FontStyle.Bold,
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter
            });
#endif
    }

    /// <summary>
    /// Valide la configuration
    /// </summary>
    private void OnValidate()
    {
        // Empêcher des tailles négatives
        boundsSize.x = Mathf.Max(boundsSize.x, 1f);
        boundsSize.y = Mathf.Max(boundsSize.y, 1f);
        boundsSize.z = Mathf.Max(boundsSize.z, 1f);
    }

#if UNITY_EDITOR
    [ContextMenu("📏 Fit to Children")]
    private void ContextMenu_FitToChildren()
    {
        // Calculer les bounds qui englobent tous les enfants
        Bounds combinedBounds = new Bounds(transform.position, Vector3.zero);
        bool hasChildren = false;

        foreach (Transform child in transform)
        {
            Renderer renderer = child.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                if (!hasChildren)
                {
                    combinedBounds = renderer.bounds;
                    hasChildren = true;
                }
                else
                {
                    combinedBounds.Encapsulate(renderer.bounds);
                }
            }
        }

        if (hasChildren)
        {
            boundsSize = combinedBounds.size;
            transform.position = combinedBounds.center;
            Debug.Log($"[NavMeshBounds] Bounds ajusté: {boundsSize}");
        }
        else
        {
            Debug.LogWarning("[NavMeshBounds] Aucun Renderer trouvé dans les enfants!");
        }
    }

    [ContextMenu("📊 Show Bounds Info")]
    private void ContextMenu_ShowInfo()
    {
        Debug.Log($"=== NAVMESH BOUNDS INFO ===\n" +
                 $"Position: {transform.position}\n" +
                 $"Size: {boundsSize}\n" +
                 $"Volume: {boundsSize.x * boundsSize.y * boundsSize.z}m³\n" +
                 $"===========================");
    }
#endif
}
