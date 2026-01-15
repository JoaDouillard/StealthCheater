using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Génère automatiquement une matrice de DeskSpawnPoints dans la scène
/// Utilise les paramètres de LevelConfiguration (gridWidth, gridHeight, cellSpacing)
/// Peut être exécuté depuis l'Inspector via le bouton "Generate Spawn Matrix"
///
/// Setup:
/// 1. Ajouter ce script à un GameObject "SpawnPointGenerator" dans chaque Level_Props
/// 2. Assigner la LevelConfiguration correspondante
/// 3. Optionnellement, assigner un Transform parent (ex: "DeskSpawnPoints")
/// 4. Cliquer sur "Generate Spawn Matrix" dans l'Inspector
/// 5. Les spawn points seront créés en mode Editor (pas au runtime)
/// </summary>
public class SpawnPointMatrixGenerator : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("Configuration du niveau contenant les paramètres de grille")]
    [SerializeField] private LevelConfiguration levelConfig;

    [Tooltip("Parent où créer les spawn points (optionnel, créé automatiquement si null)")]
    [SerializeField] private Transform spawnPointsParent;

    [Header("Options de génération")]
    [Tooltip("Supprimer les spawn points existants avant de générer")]
    [SerializeField] private bool clearExistingSpawnPoints = true;

    [Tooltip("Nom du parent à créer/trouver")]
    [SerializeField] private string parentName = "DeskSpawnPoints";

    [Header("Preview (Read-Only)")]
    [Tooltip("Afficher la grille dans la Scene View")]
    [SerializeField] private bool showGridPreview = true;

    [SerializeField] private Color gridColor = new Color(0f, 1f, 1f, 0.5f);

    [Header("Info")]
    [SerializeField] private int lastGeneratedCount = 0;

    /// <summary>
    /// Génère la matrice de spawn points (appelé depuis l'Inspector)
    /// </summary>
    public void GenerateSpawnMatrix()
    {
        if (levelConfig == null)
        {
            Debug.LogError("[SpawnPointMatrixGenerator] LevelConfiguration non assignée!");
            return;
        }

        // Trouver ou créer le parent
        if (spawnPointsParent == null)
        {
            spawnPointsParent = FindOrCreateParent();
        }

        // Nettoyer si demandé
        if (clearExistingSpawnPoints)
        {
            ClearExistingSpawnPoints();
        }

        // Générer la grille
        int generated = GenerateGrid();

        lastGeneratedCount = generated;
        Debug.Log($"[SpawnPointMatrixGenerator] ✅ Matrice générée: {generated} spawn points " +
                 $"({levelConfig.gridWidth}x{levelConfig.gridHeight}) dans '{spawnPointsParent.name}'");
    }

    /// <summary>
    /// Génère la grille de spawn points
    /// Position relative au GameObject parent (coin haut-gauche = position du parent)
    /// Rotation: 180° sur Y pour que les props regardent vers -Z (tableau)
    /// </summary>
    private int GenerateGrid()
    {
        int count = 0;

        // Utiliser la position du parent comme point de départ (coin haut-gauche)
        Vector3 startPosition = transform.position;

        // Rotation pour que les props regardent vers le tableau (-Z)
        Quaternion spawnRotation = Quaternion.Euler(0f, 180f, 0f);

        for (int row = 0; row < levelConfig.gridHeight; row++)
        {
            for (int col = 0; col < levelConfig.gridWidth; col++)
            {
                // Calculer la position relative au parent avec espacement X et Z séparés
                Vector3 position = startPosition +
                                 new Vector3(
                                     col * levelConfig.cellSpacingX,
                                     0f,
                                     row * levelConfig.cellSpacingZ
                                 );

                // Créer le spawn point
                GameObject spawnPoint = new GameObject($"DeskSpawnPoint_R{row}_C{col}");
                spawnPoint.transform.position = position;
                spawnPoint.transform.rotation = spawnRotation; // Rotation vers -Z
                spawnPoint.transform.parent = spawnPointsParent;

                // Ajouter le composant DeskSpawnPoint
                DeskSpawnPoint deskSpawnPoint = spawnPoint.AddComponent<DeskSpawnPoint>();

#if UNITY_EDITOR
                // Marquer pour l'Undo system
                Undo.RegisterCreatedObjectUndo(spawnPoint, "Generate Spawn Point");
#endif

                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// Trouve ou crée le parent pour les spawn points
    /// </summary>
    private Transform FindOrCreateParent()
    {
        // Chercher dans les enfants
        Transform found = transform.Find(parentName);
        if (found != null)
        {
            Debug.Log($"[SpawnPointMatrixGenerator] Parent existant trouvé: {parentName}");
            return found;
        }

        // Créer un nouveau parent
        GameObject parent = new GameObject(parentName);
        parent.transform.parent = transform;
        parent.transform.localPosition = Vector3.zero;

#if UNITY_EDITOR
        Undo.RegisterCreatedObjectUndo(parent, "Create Spawn Points Parent");
#endif

        Debug.Log($"[SpawnPointMatrixGenerator] Nouveau parent créé: {parentName}");
        return parent.transform;
    }

    /// <summary>
    /// Supprime tous les DeskSpawnPoints existants dans le parent
    /// </summary>
    private void ClearExistingSpawnPoints()
    {
        if (spawnPointsParent == null)
            return;

        DeskSpawnPoint[] existingPoints = spawnPointsParent.GetComponentsInChildren<DeskSpawnPoint>();
        int count = existingPoints.Length;

        if (count > 0)
        {
            Debug.Log($"[SpawnPointMatrixGenerator] Suppression de {count} spawn points existants...");

            foreach (DeskSpawnPoint point in existingPoints)
            {
#if UNITY_EDITOR
                if (Application.isPlaying)
                {
                    Destroy(point.gameObject);
                }
                else
                {
                    DestroyImmediate(point.gameObject);
                }
#else
                Destroy(point.gameObject);
#endif
            }
        }
    }

    /// <summary>
    /// Dessine la grille dans la Scene View (preview)
    /// La grille est positionnée relativement au GameObject parent
    /// Affiche aussi une flèche pour indiquer la direction -Z (vers tableau)
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!showGridPreview || levelConfig == null)
            return;

        Gizmos.color = gridColor;

        // Utiliser la position du parent comme point de départ
        Vector3 startPosition = transform.position;

        for (int row = 0; row < levelConfig.gridHeight; row++)
        {
            for (int col = 0; col < levelConfig.gridWidth; col++)
            {
                Vector3 position = startPosition +
                                 new Vector3(
                                     col * levelConfig.cellSpacingX,
                                     0f,
                                     row * levelConfig.cellSpacingZ
                                 );

                // Dessiner une petite sphère
                Gizmos.DrawWireSphere(position, 0.2f);

                // Dessiner une croix au sol
                Gizmos.DrawLine(
                    position + Vector3.left * 0.3f,
                    position + Vector3.right * 0.3f
                );
                Gizmos.DrawLine(
                    position + Vector3.forward * 0.3f,
                    position + Vector3.back * 0.3f
                );

                // Dessiner une flèche vers -Z (direction du tableau)
                Vector3 arrowStart = position + Vector3.up * 0.1f;
                Vector3 arrowEnd = arrowStart + Vector3.back * 0.5f; // -Z
                Gizmos.DrawLine(arrowStart, arrowEnd);
                // Pointe de la flèche
                Gizmos.DrawLine(arrowEnd, arrowEnd + new Vector3(0.1f, 0f, 0.1f));
                Gizmos.DrawLine(arrowEnd, arrowEnd + new Vector3(-0.1f, 0f, 0.1f));
            }
        }

        // Dessiner le cadre de la grille
        Vector3 corner1 = startPosition;
        Vector3 corner2 = startPosition + new Vector3((levelConfig.gridWidth - 1) * levelConfig.cellSpacingX, 0f, 0f);
        Vector3 corner3 = startPosition + new Vector3((levelConfig.gridWidth - 1) * levelConfig.cellSpacingX, 0f, (levelConfig.gridHeight - 1) * levelConfig.cellSpacingZ);
        Vector3 corner4 = startPosition + new Vector3(0f, 0f, (levelConfig.gridHeight - 1) * levelConfig.cellSpacingZ);

        Gizmos.DrawLine(corner1, corner2);
        Gizmos.DrawLine(corner2, corner3);
        Gizmos.DrawLine(corner3, corner4);
        Gizmos.DrawLine(corner4, corner1);
    }

#if UNITY_EDITOR
    [ContextMenu("🔨 Generate Spawn Matrix")]
    private void ContextMenu_GenerateSpawnMatrix()
    {
        GenerateSpawnMatrix();
    }

    [ContextMenu("🗑️ Clear Spawn Points")]
    private void ContextMenu_ClearSpawnPoints()
    {
        if (spawnPointsParent == null)
        {
            spawnPointsParent = FindOrCreateParent();
        }

        ClearExistingSpawnPoints();
        Debug.Log("[SpawnPointMatrixGenerator] Spawn points supprimés");
    }

    [ContextMenu("📊 Count Spawn Points")]
    private void ContextMenu_CountSpawnPoints()
    {
        if (spawnPointsParent == null)
        {
            spawnPointsParent = FindOrCreateParent();
        }

        DeskSpawnPoint[] points = spawnPointsParent.GetComponentsInChildren<DeskSpawnPoint>();
        Debug.Log($"[SpawnPointMatrixGenerator] {points.Length} spawn points trouvés dans '{spawnPointsParent.name}'");
    }
#endif
}

#if UNITY_EDITOR
/// <summary>
/// Custom Editor pour SpawnPointMatrixGenerator avec un gros bouton de génération
/// </summary>
[CustomEditor(typeof(SpawnPointMatrixGenerator))]
public class SpawnPointMatrixGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        SpawnPointMatrixGenerator generator = (SpawnPointMatrixGenerator)target;

        EditorGUILayout.Space(10);

        // Gros bouton de génération
        GUI.backgroundColor = new Color(0.3f, 0.8f, 1f);
        if (GUILayout.Button("🔨 GENERATE SPAWN MATRIX", GUILayout.Height(40)))
        {
            generator.GenerateSpawnMatrix();
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(5);

        // Bouton de nettoyage
        GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
        if (GUILayout.Button("🗑️ Clear Spawn Points", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog(
                "Confirmer la suppression",
                "Supprimer tous les spawn points existants?",
                "Oui", "Annuler"))
            {
                generator.GenerateSpawnMatrix(); // Clear is done inside
            }
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(5);

        // Info box
        EditorGUILayout.HelpBox(
            "1. Assigner une LevelConfiguration\n" +
            "2. Ajuster les paramètres de grille dans la config\n" +
            "3. Cliquer sur 'Generate Spawn Matrix'\n" +
            "4. Les spawn points apparaîtront dans la Scene View",
            MessageType.Info
        );
    }
}
#endif
