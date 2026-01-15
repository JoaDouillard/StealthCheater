using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Script Editor pour activer "Read/Write Enabled" sur tous les mesh du projet
/// Cela résout les erreurs "RuntimeNavMeshBuilder: Source mesh does not allow read access"
///
/// Utilisation:
/// 1. Place ce script dans Assets/Editor/
/// 2. Menu Unity: Tools → Enable Read/Write on All Meshes
/// 3. Le script va scanner tous les modèles 3D et activer Read/Write
/// </summary>
public class EnableReadWriteOnAllMeshes : EditorWindow
{
    private Vector2 scrollPosition;
    private List<ModelImporter> foundImporters = new List<ModelImporter>();
    private int processedCount = 0;
    private int alreadyEnabledCount = 0;
    private bool isScanning = false;
    private bool hasScanned = false;

    [MenuItem("Tools/Enable Read/Write on All Meshes")]
    public static void ShowWindow()
    {
        EnableReadWriteOnAllMeshes window = GetWindow<EnableReadWriteOnAllMeshes>();
        window.titleContent = new GUIContent("Enable Read/Write");
        window.minSize = new Vector2(400, 300);
        window.Show();
    }

    private void OnGUI()
    {
        GUILayout.Label("Enable Read/Write on All Meshes", EditorStyles.boldLabel);
        GUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "Ce script va activer 'Read/Write Enabled' sur tous les modèles 3D du projet.\n\n" +
            "Cela résout les erreurs NavMesh : 'Source mesh does not allow read access'.\n\n" +
            "Étape 1 : Cliquez sur 'Scan Project' pour trouver tous les modèles.\n" +
            "Étape 2 : Cliquez sur 'Enable Read/Write' pour les modifier.",
            MessageType.Info
        );

        GUILayout.Space(10);

        // Bouton Scan
        if (!isScanning)
        {
            if (GUILayout.Button("1. Scan Project", GUILayout.Height(30)))
            {
                ScanProject();
            }
        }
        else
        {
            GUILayout.Label("Scanning...", EditorStyles.boldLabel);
        }

        GUILayout.Space(10);

        // Afficher les résultats du scan
        if (hasScanned && foundImporters.Count > 0)
        {
            EditorGUILayout.HelpBox(
                $"Trouvé {foundImporters.Count} modèle(s) 3D dans le projet.",
                MessageType.Info
            );

            GUILayout.Space(5);

            // Liste des modèles trouvés
            GUILayout.Label("Modèles trouvés:", EditorStyles.boldLabel);
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(150));
            foreach (ModelImporter importer in foundImporters)
            {
                string status = importer.isReadable ? "✅ Read/Write déjà activé" : "❌ Read/Write désactivé";
                EditorGUILayout.LabelField($"• {importer.assetPath}", status);
            }
            EditorGUILayout.EndScrollView();

            GUILayout.Space(10);

            // Bouton Enable Read/Write
            if (GUILayout.Button("2. Enable Read/Write on All", GUILayout.Height(30)))
            {
                EnableReadWriteOnAll();
            }

            GUILayout.Space(5);

            // Afficher le résumé
            if (processedCount > 0)
            {
                EditorGUILayout.HelpBox(
                    $"✅ Traitement terminé!\n\n" +
                    $"Modifiés: {processedCount - alreadyEnabledCount}\n" +
                    $"Déjà activés: {alreadyEnabledCount}\n" +
                    $"Total: {processedCount}",
                    MessageType.Info
                );
            }
        }
        else if (hasScanned && foundImporters.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "Aucun modèle 3D trouvé dans le projet.",
                MessageType.Warning
            );
        }
    }

    /// <summary>
    /// Scanne le projet pour trouver tous les ModelImporter
    /// </summary>
    private void ScanProject()
    {
        isScanning = true;
        foundImporters.Clear();
        processedCount = 0;
        alreadyEnabledCount = 0;

        // Trouver tous les assets dans le projet
        string[] allAssetPaths = AssetDatabase.GetAllAssetPaths();

        foreach (string assetPath in allAssetPaths)
        {
            // Vérifier si c'est un modèle 3D
            if (assetPath.StartsWith("Assets/") &&
                (assetPath.EndsWith(".fbx") || assetPath.EndsWith(".obj") ||
                 assetPath.EndsWith(".dae") || assetPath.EndsWith(".3ds") ||
                 assetPath.EndsWith(".blend") || assetPath.EndsWith(".max")))
            {
                ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
                if (importer != null)
                {
                    foundImporters.Add(importer);
                }
            }
        }

        isScanning = false;
        hasScanned = true;

        Debug.Log($"[EnableReadWrite] Scan terminé: {foundImporters.Count} modèle(s) trouvé(s).");
    }

    /// <summary>
    /// Active Read/Write sur tous les ModelImporter trouvés
    /// </summary>
    private void EnableReadWriteOnAll()
    {
        if (foundImporters.Count == 0)
        {
            Debug.LogWarning("[EnableReadWrite] Aucun modèle trouvé. Lancez d'abord un scan.");
            return;
        }

        processedCount = 0;
        alreadyEnabledCount = 0;

        // Demander confirmation
        bool confirm = EditorUtility.DisplayDialog(
            "Enable Read/Write on All Meshes",
            $"Voulez-vous activer 'Read/Write Enabled' sur {foundImporters.Count} modèle(s) ?\n\n" +
            "Cette opération va modifier les import settings de tous les modèles 3D.",
            "Oui, activer",
            "Annuler"
        );

        if (!confirm)
        {
            Debug.Log("[EnableReadWrite] Opération annulée par l'utilisateur.");
            return;
        }

        // Progress bar
        int total = foundImporters.Count;
        int current = 0;

        foreach (ModelImporter importer in foundImporters)
        {
            current++;
            EditorUtility.DisplayProgressBar(
                "Enabling Read/Write...",
                $"Processing: {importer.assetPath} ({current}/{total})",
                (float)current / total
            );

            if (!importer.isReadable)
            {
                importer.isReadable = true;
                AssetDatabase.ImportAsset(importer.assetPath, ImportAssetOptions.ForceUpdate);
                Debug.Log($"[EnableReadWrite] ✅ Read/Write activé: {importer.assetPath}");
            }
            else
            {
                alreadyEnabledCount++;
                Debug.Log($"[EnableReadWrite] ⏭️ Déjà activé (skip): {importer.assetPath}");
            }

            processedCount++;
        }

        EditorUtility.ClearProgressBar();

        // Message de confirmation
        EditorUtility.DisplayDialog(
            "Terminé!",
            $"Read/Write activé avec succès!\n\n" +
            $"Modifiés: {processedCount - alreadyEnabledCount}\n" +
            $"Déjà activés: {alreadyEnabledCount}\n" +
            $"Total: {processedCount}\n\n" +
            "Vous pouvez maintenant re-bake le NavMesh.",
            "OK"
        );

        Debug.Log($"[EnableReadWrite] ✅ Traitement terminé: {processedCount - alreadyEnabledCount} modifié(s), {alreadyEnabledCount} déjà activé(s).");
    }
}
