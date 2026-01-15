using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Script pour vérifier et activer Read/Write sur TOUS les prefabs du projet
/// Utile pour détecter les prefabs qui n'ont pas Read/Write activé
///
/// Utilisation:
/// 1. Tools → Verify Prefabs Read/Write
/// 2. Le script va scanner tous les prefabs
/// 3. Pour chaque prefab, il vérifie si les mesh enfants ont Read/Write
/// 4. Si non, il active Read/Write sur les mesh sources
/// </summary>
public class VerifyPrefabsReadWrite : EditorWindow
{
    private Vector2 scrollPosition;
    private List<string> problematicPrefabs = new List<string>();
    private bool isScanning = false;
    private bool hasScanned = false;
    private int totalPrefabs = 0;
    private int fixedCount = 0;

    [MenuItem("Tools/Verify Prefabs Read/Write")]
    public static void ShowWindow()
    {
        VerifyPrefabsReadWrite window = GetWindow<VerifyPrefabsReadWrite>();
        window.titleContent = new GUIContent("Verify Prefabs Read/Write");
        window.minSize = new Vector2(500, 400);
        window.Show();
    }

    private void OnGUI()
    {
        GUILayout.Label("Verify Prefabs Read/Write", EditorStyles.boldLabel);
        GUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "Ce script va vérifier que TOUS les prefabs du projet ont Read/Write activé sur leurs mesh.\n\n" +
            "Cela résout les erreurs NavMesh sur les objets spawnés dynamiquement.\n\n" +
            "Étape 1 : Cliquez sur 'Scan Prefabs'\n" +
            "Étape 2 : Cliquez sur 'Fix All' pour activer Read/Write",
            MessageType.Info
        );

        GUILayout.Space(10);

        // Bouton Scan
        if (!isScanning)
        {
            if (GUILayout.Button("1. Scan Prefabs", GUILayout.Height(30)))
            {
                ScanPrefabs();
            }
        }
        else
        {
            GUILayout.Label("Scanning...", EditorStyles.boldLabel);
        }

        GUILayout.Space(10);

        // Afficher les résultats
        if (hasScanned)
        {
            if (problematicPrefabs.Count > 0)
            {
                EditorGUILayout.HelpBox(
                    $"⚠️ {problematicPrefabs.Count} prefab(s) ont des mesh sans Read/Write!",
                    MessageType.Warning
                );

                GUILayout.Space(5);

                // Liste des prefabs problématiques
                GUILayout.Label("Prefabs problématiques:", EditorStyles.boldLabel);
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));
                foreach (string prefabPath in problematicPrefabs)
                {
                    EditorGUILayout.LabelField($"❌ {prefabPath}");
                }
                EditorGUILayout.EndScrollView();

                GUILayout.Space(10);

                // Bouton Fix
                if (GUILayout.Button("2. Fix All Prefabs", GUILayout.Height(30)))
                {
                    FixAllPrefabs();
                }

                if (fixedCount > 0)
                {
                    EditorGUILayout.HelpBox(
                        $"✅ {fixedCount} prefab(s) corrigé(s)!",
                        MessageType.Info
                    );
                }
            }
            else
            {
                EditorGUILayout.HelpBox(
                    $"✅ Tous les prefabs ({totalPrefabs}) ont Read/Write activé!",
                    MessageType.Info
                );
            }
        }
    }

    /// <summary>
    /// Scanne tous les prefabs du projet
    /// </summary>
    private void ScanPrefabs()
    {
        isScanning = true;
        problematicPrefabs.Clear();
        totalPrefabs = 0;
        fixedCount = 0;

        // Trouver tous les prefabs
        string[] allPrefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });

        foreach (string guid in allPrefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab != null)
            {
                totalPrefabs++;

                // Vérifier les mesh dans le prefab
                if (HasMeshWithoutReadWrite(prefab))
                {
                    problematicPrefabs.Add(path);
                }
            }
        }

        isScanning = false;
        hasScanned = true;

        Debug.Log($"[VerifyPrefabs] Scan terminé: {totalPrefabs} prefabs, {problematicPrefabs.Count} problématiques.");
    }

    /// <summary>
    /// Vérifie si un prefab contient des mesh sans Read/Write
    /// </summary>
    private bool HasMeshWithoutReadWrite(GameObject prefab)
    {
        MeshFilter[] meshFilters = prefab.GetComponentsInChildren<MeshFilter>(true);

        foreach (MeshFilter mf in meshFilters)
        {
            if (mf.sharedMesh != null && !mf.sharedMesh.isReadable)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Active Read/Write sur tous les prefabs problématiques
    /// </summary>
    private void FixAllPrefabs()
    {
        if (problematicPrefabs.Count == 0)
        {
            Debug.LogWarning("[VerifyPrefabs] Aucun prefab à corriger.");
            return;
        }

        bool confirm = EditorUtility.DisplayDialog(
            "Fix All Prefabs",
            $"Voulez-vous activer Read/Write sur {problematicPrefabs.Count} prefab(s) ?\n\n" +
            "Cela va modifier les modèles 3D sources (.fbx, .obj, etc.).",
            "Oui, corriger",
            "Annuler"
        );

        if (!confirm)
        {
            Debug.Log("[VerifyPrefabs] Opération annulée.");
            return;
        }

        fixedCount = 0;
        HashSet<string> processedModels = new HashSet<string>();

        int current = 0;
        foreach (string prefabPath in problematicPrefabs)
        {
            current++;
            EditorUtility.DisplayProgressBar(
                "Fixing Prefabs...",
                $"Processing: {prefabPath} ({current}/{problematicPrefabs.Count})",
                (float)current / problematicPrefabs.Count
            );

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab != null)
            {
                FixPrefabMeshes(prefab, processedModels);
            }
        }

        EditorUtility.ClearProgressBar();

        // Re-scanner pour vérifier
        ScanPrefabs();

        EditorUtility.DisplayDialog(
            "Terminé!",
            $"Read/Write activé avec succès!\n\n" +
            $"Modèles 3D modifiés: {fixedCount}\n\n" +
            "Vérifiez la console pour les détails.",
            "OK"
        );

        Debug.Log($"[VerifyPrefabs] ✅ Correction terminée: {fixedCount} modèle(s) 3D modifié(s).");
    }

    /// <summary>
    /// Active Read/Write sur les mesh d'un prefab
    /// </summary>
    private void FixPrefabMeshes(GameObject prefab, HashSet<string> processedModels)
    {
        MeshFilter[] meshFilters = prefab.GetComponentsInChildren<MeshFilter>(true);

        foreach (MeshFilter mf in meshFilters)
        {
            if (mf.sharedMesh != null && !mf.sharedMesh.isReadable)
            {
                // Trouver le modèle 3D source
                string meshAssetPath = AssetDatabase.GetAssetPath(mf.sharedMesh);

                if (!string.IsNullOrEmpty(meshAssetPath) && !processedModels.Contains(meshAssetPath))
                {
                    ModelImporter importer = AssetImporter.GetAtPath(meshAssetPath) as ModelImporter;

                    if (importer != null && !importer.isReadable)
                    {
                        importer.isReadable = true;
                        AssetDatabase.ImportAsset(meshAssetPath, ImportAssetOptions.ForceUpdate);
                        processedModels.Add(meshAssetPath);
                        fixedCount++;
                        Debug.Log($"[VerifyPrefabs] ✅ Read/Write activé: {meshAssetPath}");
                    }
                }
            }
        }
    }
}
