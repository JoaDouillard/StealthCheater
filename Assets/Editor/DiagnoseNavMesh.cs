using UnityEngine;
using UnityEditor;
using Unity.AI.Navigation;

/// <summary>
/// Script de diagnostic pour comprendre pourquoi le NavMesh ne se construit pas
///
/// Utilisation:
/// 1. Tools → Diagnose NavMesh
/// 2. Le script va analyser la configuration et identifier les problèmes
/// </summary>
public class DiagnoseNavMesh : EditorWindow
{
    private Vector2 scrollPosition;

    [MenuItem("Tools/Diagnose NavMesh")]
    public static void ShowWindow()
    {
        DiagnoseNavMesh window = GetWindow<DiagnoseNavMesh>();
        window.titleContent = new GUIContent("Diagnose NavMesh");
        window.minSize = new Vector2(600, 500);
        window.Show();
    }

    private void OnGUI()
    {
        GUILayout.Label("NavMesh Diagnostic Tool", EditorStyles.boldLabel);
        GUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "Ce script analyse votre configuration NavMesh et identifie les problèmes potentiels.\n\n" +
            "Cliquez sur 'Run Diagnostic' pour lancer l'analyse.",
            MessageType.Info
        );

        GUILayout.Space(10);

        if (GUILayout.Button("Run Diagnostic", GUILayout.Height(40)))
        {
            RunDiagnostic();
        }

        GUILayout.Space(10);
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        EditorGUILayout.EndScrollView();
    }

    private void RunDiagnostic()
    {
        Debug.Log("=== NAVMESH DIAGNOSTIC ===");

        // 1. Trouver tous les NavMeshSurface
        NavMeshSurface[] surfaces = FindObjectsOfType<NavMeshSurface>();

        if (surfaces.Length == 0)
        {
            Debug.LogError("❌ PROBLÈME 1 : Aucun NavMeshSurface trouvé dans la scène!");
            Debug.LogError("   SOLUTION : Ajoutez un composant NavMeshSurface à un GameObject (ex: NavMeshBounds ou Ground)");
            Debug.LogError("   1. Sélectionnez le GameObject");
            Debug.LogError("   2. Add Component → Navigation → NavMesh Surface");
            return;
        }

        Debug.Log($"✅ {surfaces.Length} NavMeshSurface(s) trouvé(s)");

        // 2. Analyser chaque NavMeshSurface
        for (int i = 0; i < surfaces.Length; i++)
        {
            NavMeshSurface surface = surfaces[i];
            Debug.Log($"\n--- NavMeshSurface #{i + 1} : {surface.gameObject.name} ---");

            // Vérifier Collect Objects
            Debug.Log($"   Collect Objects: {surface.collectObjects}");
            if (surface.collectObjects == CollectObjects.Children)
            {
                Debug.LogWarning("   ⚠️ Collect Objects = Children → Le NavMesh ne sera construit QUE sur les enfants directs.");
                Debug.LogWarning("      Si vos props sont spawnés ailleurs, le NavMesh ne les verra pas.");
                Debug.LogWarning("      SOLUTION : Utilisez 'All' ou 'Volume'");
            }

            // Vérifier Layer Mask
            Debug.Log($"   Include Layers: {LayerMaskToString(surface.layerMask)}");
            if (surface.layerMask.value == 0)
            {
                Debug.LogError("   ❌ PROBLÈME 2 : Include Layers est vide!");
                Debug.LogError("      SOLUTION : Cochez au moins 'Default' dans Include Layers");
            }

            // Vérifier Use Geometry
            Debug.Log($"   Use Geometry: {surface.useGeometry}");
            // Note : Recommandé d'utiliser Physics Colliders (valeur 0)
            if ((int)surface.useGeometry != 0)
            {
                Debug.LogWarning($"   ⚠️ Use Geometry = {surface.useGeometry}");
                Debug.LogWarning("      Recommandé : Physics Colliders (plus performant et fiable)");
            }

            // Vérifier si le NavMesh a été baké
            if (surface.navMeshData == null)
            {
                Debug.LogError("   ❌ PROBLÈME 3 : NavMesh pas encore baké!");
                Debug.LogError("      SOLUTION : Cliquez sur 'Bake' dans l'Inspector du NavMeshSurface");
                Debug.LogError("      OU lancez le jeu (le NavMesh se bake automatiquement au runtime)");
            }
            else
            {
                Debug.Log($"   ✅ NavMesh Data présent: {surface.navMeshData.name}");
                Debug.Log($"      Bounds: {surface.navMeshData.sourceBounds.size}");
            }

            // Vérifier les enfants si Collect Objects = Volume
            if (surface.collectObjects == CollectObjects.Volume)
            {
                BoxCollider boxCollider = surface.GetComponent<BoxCollider>();
                if (boxCollider == null)
                {
                    Debug.LogWarning("   ⚠️ Collect Objects = Volume MAIS aucun BoxCollider trouvé!");
                    Debug.LogWarning("      SOLUTION : Ajoutez un BoxCollider au GameObject pour définir la zone");
                }
                else
                {
                    Debug.Log($"   ✅ BoxCollider trouvé: Size = {boxCollider.size}");
                }
            }
        }

        // 3. Vérifier les colliders dans la scène
        Debug.Log("\n--- Vérification des Colliders ---");
        Collider[] allColliders = FindObjectsOfType<Collider>();
        Debug.Log($"   {allColliders.Length} Collider(s) trouvé(s) dans la scène");

        if (allColliders.Length == 0)
        {
            Debug.LogError("   ❌ PROBLÈME 4 : Aucun Collider trouvé!");
            Debug.LogError("      Le NavMesh a besoin de Colliders pour se construire.");
            Debug.LogError("      SOLUTION : Ajoutez des BoxCollider/MeshCollider aux objets (sol, bureaux, murs)");
        }

        // 4. Vérifier si les objets ont Read/Write
        Debug.Log("\n--- Vérification Read/Write ---");
        MeshFilter[] meshFilters = FindObjectsOfType<MeshFilter>();
        int meshesWithoutReadWrite = 0;
        foreach (MeshFilter mf in meshFilters)
        {
            if (mf.sharedMesh != null && !mf.sharedMesh.isReadable)
            {
                meshesWithoutReadWrite++;
            }
        }

        if (meshesWithoutReadWrite > 0)
        {
            Debug.LogWarning($"   ⚠️ {meshesWithoutReadWrite} mesh(es) sans Read/Write détecté(s)");
            Debug.LogWarning("      Cela peut causer des erreurs 'Combined Mesh does not allow read access'");
            Debug.LogWarning("      SOLUTION : Tools → Verify Prefabs Read/Write");
        }
        else
        {
            Debug.Log($"   ✅ Tous les mesh ont Read/Write activé");
        }

        // 5. Résumé et recommandations
        Debug.Log("\n=== RÉSUMÉ ===");
        Debug.Log("Problèmes à corriger en priorité:");
        if (surfaces.Length == 0)
        {
            Debug.LogError("   1. Ajouter un NavMeshSurface");
        }
        else
        {
            bool hasProblem = false;
            foreach (NavMeshSurface surface in surfaces)
            {
                if (surface.layerMask.value == 0)
                {
                    Debug.LogError($"   2. {surface.gameObject.name} : Include Layers est vide");
                    hasProblem = true;
                }
                if (surface.navMeshData == null)
                {
                    Debug.LogWarning($"   3. {surface.gameObject.name} : NavMesh pas baké (normal si pas encore lancé le jeu)");
                    hasProblem = true;
                }
            }

            if (!hasProblem)
            {
                Debug.Log("   ✅ Configuration semble correcte!");
                Debug.Log("   Si le NavMesh ne se construit toujours pas au runtime:");
                Debug.Log("      - Vérifiez que PropsSpawner appelle bien RebakeNavMesh()");
                Debug.Log("      - Vérifiez les logs de PropsSpawner dans la Console pendant le jeu");
            }
        }

        Debug.Log("=========================");
    }

    private string LayerMaskToString(LayerMask mask)
    {
        string result = "";
        for (int i = 0; i < 32; i++)
        {
            if ((mask.value & (1 << i)) != 0)
            {
                string layerName = LayerMask.LayerToName(i);
                if (!string.IsNullOrEmpty(layerName))
                {
                    if (result.Length > 0) result += ", ";
                    result += layerName;
                }
            }
        }
        return string.IsNullOrEmpty(result) ? "(vide)" : result;
    }
}
