using UnityEngine;

/// <summary>
/// Marque une position possible pour spawner un bureau/étudiant dans la salle de classe
/// Place ces GameObjects dans Unity pour créer la "matrice" de positions possibles
/// </summary>
public class DeskSpawnPoint : MonoBehaviour
{
    [Header("Visual Debug")]
    [Tooltip("Couleur du Gizmo dans la Scene View")]
    [SerializeField] private Color gizmoColor = Color.yellow;

    [Tooltip("Afficher le numéro du point dans la scène")]
    [SerializeField] private bool showIndex = true;

    [Header("Runtime Info (Read-Only)")]
    [Tooltip("GameObject spawné à cette position (assigné par DeskSpawner)")]
    public GameObject spawnedObject;

    [Tooltip("Type d'objet spawné à cette position")]
    public DeskSpawnType spawnedType = DeskSpawnType.Empty;

    private void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);

        // Dessiner une ligne vers le haut
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 1f);
    }

    private void OnDrawGizmosSelected()
    {
        // Dessiner plus visible quand sélectionné
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.8f);
        Gizmos.DrawSphere(transform.position + Vector3.up * 1f, 0.1f);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Nommer automatiquement selon position
        if (transform.parent != null)
        {
            int siblingIndex = transform.GetSiblingIndex();
            gameObject.name = $"DeskSpawnPoint_{siblingIndex:D2}";
        }
    }
#endif
}

/// <summary>
/// Type d'objet qui peut être spawné sur un DeskSpawnPoint
/// </summary>
public enum DeskSpawnType
{
    Empty,          // Rien (position vide)
    DeskOnly,       // Bureau vide
    DeskObstacle,   // Bureau + Obstacle (livres, pot de fleurs, etc.)
    DeskStudent     // Bureau + Étudiant (peut être zone de copie)
}
