using UnityEngine;

/// <summary>
/// Contient les références aux points fixes d'un niveau (spawn points, fenêtres, tableau)
/// Ce script doit être attaché à chaque Level_Props_X
///
/// Pourquoi un MonoBehaviour et pas dans LevelConfiguration?
/// - LevelConfiguration est un ScriptableObject (asset dans Project)
/// - Les ScriptableObjects ne peuvent pas référencer des Transform de la Hierarchy
/// - Ce script permet de drag-and-drop des objets de la scène directement
///
/// Setup:
/// 1. Attacher ce script à chaque Level_Props (Level_Props_1, Level_Props_2, etc.)
/// 2. Dans l'Inspector, drag-and-drop les Transform depuis la Hierarchy:
///    - Teacher Spawn Point: Position de spawn du professeur
///    - Player Spawn Point: Position de spawn du joueur
///    - Window Points: Toutes les fenêtres (points d'intérêt)
///    - Board Point: Le tableau (point d'intérêt)
/// 3. Le Teacher trouvera ce script avec FindObjectOfType<LevelFixedPoints>()
/// </summary>
public class LevelFixedPoints : MonoBehaviour
{
    [Header("Spawn Points")]
    [Tooltip("Position de spawn du Teacher (professeur)")]
    public Transform teacherSpawnPoint;

    [Tooltip("Position de spawn du Player (joueur)")]
    public Transform playerSpawnPoint;

    [Header("Points d'Intérêt")]
    [Tooltip("Fenêtres du niveau (le Teacher peut se diriger vers ces points)")]
    public Transform[] windowPoints;

    [Tooltip("Tableau du niveau (le Teacher peut se diriger vers ce point)")]
    public Transform boardPoint;

    [Header("Debug")]
    [Tooltip("Afficher les Gizmos dans la Scene View")]
    [SerializeField] private bool showGizmos = true;

    [Tooltip("Couleur des Gizmos")]
    [SerializeField] private Color gizmoColor = Color.yellow;

    /// <summary>
    /// Valide que tous les points requis sont assignés
    /// </summary>
    public bool ValidatePoints()
    {
        bool valid = true;

        if (teacherSpawnPoint == null)
        {
            Debug.LogError($"[LevelFixedPoints] {gameObject.name}: Teacher Spawn Point non assigné!");
            valid = false;
        }

        if (playerSpawnPoint == null)
        {
            Debug.LogError($"[LevelFixedPoints] {gameObject.name}: Player Spawn Point non assigné!");
            valid = false;
        }

        // Optionnel: windows et board peuvent être null
        if (windowPoints == null || windowPoints.Length == 0)
        {
            Debug.LogWarning($"[LevelFixedPoints] {gameObject.name}: Aucune fenêtre assignée");
        }

        if (boardPoint == null)
        {
            Debug.LogWarning($"[LevelFixedPoints] {gameObject.name}: Tableau non assigné");
        }

        return valid;
    }

    /// <summary>
    /// Retourne une fenêtre aléatoire
    /// </summary>
    public Transform GetRandomWindowPoint()
    {
        if (windowPoints == null || windowPoints.Length == 0)
        {
            Debug.LogWarning("[LevelFixedPoints] Aucune fenêtre disponible");
            return null;
        }

        return windowPoints[Random.Range(0, windowPoints.Length)];
    }

    /// <summary>
    /// Dessine les Gizmos dans la Scene View
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!showGizmos)
            return;

        Gizmos.color = gizmoColor;

        // Teacher Spawn Point
        if (teacherSpawnPoint != null)
        {
            Gizmos.DrawWireSphere(teacherSpawnPoint.position, 0.5f);
            DrawLabel(teacherSpawnPoint.position, "TEACHER SPAWN");
        }

        // Player Spawn Point
        if (playerSpawnPoint != null)
        {
            Gizmos.DrawWireSphere(playerSpawnPoint.position, 0.5f);
            DrawLabel(playerSpawnPoint.position + Vector3.up * 0.5f, "PLAYER SPAWN");
        }

        // Window Points
        if (windowPoints != null)
        {
            foreach (Transform window in windowPoints)
            {
                if (window != null)
                {
                    Gizmos.DrawWireCube(window.position, Vector3.one * 0.5f);
                    DrawLabel(window.position + Vector3.up * 0.5f, "WINDOW");
                }
            }
        }

        // Board Point
        if (boardPoint != null)
        {
            Gizmos.DrawWireCube(boardPoint.position, new Vector3(2f, 1f, 0.2f));
            DrawLabel(boardPoint.position + Vector3.up * 0.5f, "BOARD");
        }
    }

    /// <summary>
    /// Dessine un label dans la Scene View
    /// </summary>
    private void DrawLabel(Vector3 position, string text)
    {
#if UNITY_EDITOR
        UnityEditor.Handles.Label(position, text, new GUIStyle()
        {
            normal = new GUIStyleState() { textColor = gizmoColor },
            fontStyle = FontStyle.Bold,
            fontSize = 10
        });
#endif
    }

    /// <summary>
    /// Validation automatique dans l'Inspector
    /// </summary>
    private void OnValidate()
    {
        // Compter les points assignés
        int assignedPoints = 0;
        if (teacherSpawnPoint != null) assignedPoints++;
        if (playerSpawnPoint != null) assignedPoints++;
        if (boardPoint != null) assignedPoints++;
        if (windowPoints != null) assignedPoints += windowPoints.Length;

        if (assignedPoints == 0)
        {
            Debug.LogWarning($"[LevelFixedPoints] {gameObject.name}: Aucun point assigné!");
        }
    }

#if UNITY_EDITOR
    [ContextMenu("✅ Validate All Points")]
    private void ContextMenu_ValidatePoints()
    {
        if (ValidatePoints())
        {
            Debug.Log($"[LevelFixedPoints] {gameObject.name}: ✅ Tous les points requis sont assignés!");
        }
    }

    [ContextMenu("📊 Show Points Summary")]
    private void ContextMenu_ShowSummary()
    {
        Debug.Log($"=== LEVEL FIXED POINTS: {gameObject.name} ===\n" +
                 $"Teacher Spawn: {(teacherSpawnPoint != null ? teacherSpawnPoint.name : "NON ASSIGNÉ")}\n" +
                 $"Player Spawn: {(playerSpawnPoint != null ? playerSpawnPoint.name : "NON ASSIGNÉ")}\n" +
                 $"Board: {(boardPoint != null ? boardPoint.name : "NON ASSIGNÉ")}\n" +
                 $"Windows: {(windowPoints != null ? windowPoints.Length : 0)} fenêtre(s)\n" +
                 $"========================================");
    }
#endif
}
