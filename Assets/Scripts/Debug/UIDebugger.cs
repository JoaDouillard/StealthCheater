using UnityEngine;

/// <summary>
/// Script de diagnostic pour vérifier que tous les managers UI sont présents
/// TEMPORAIRE - À supprimer une fois tout fonctionnel
/// </summary>
public class UIDebugger : MonoBehaviour
{
    private void Start()
    {
        Debug.Log("=== UI DEBUGGER - DIAGNOSTIC COMPLET ===");

        // UIManager
        if (UIManager.Instance != null)
        {
            Debug.Log("✓ UIManager trouvé");
        }
        else
        {
            Debug.LogError("✗ UIManager MANQUANT !");
        }

        // CheatSheetManager
        if (CheatSheetManager.Instance != null)
        {
            Debug.Log("✓ CheatSheetManager trouvé");
            Debug.Log($"  - Disponible: {CheatSheetManager.Instance.IsAvailable()}");
        }
        else
        {
            Debug.LogError("✗ CheatSheetManager MANQUANT !");
        }

        // PauseMenuUI
        PauseMenuUI pauseUI = FindFirstObjectByType<PauseMenuUI>();
        if (pauseUI != null)
        {
            Debug.Log("✓ PauseMenuUI trouvé");
        }
        else
        {
            Debug.LogError("✗ PauseMenuUI MANQUANT !");
        }

        // GameHUD
        if (GameHUD.Instance != null)
        {
            Debug.Log("✓ GameHUD trouvé");
        }
        else
        {
            Debug.LogError("✗ GameHUD MANQUANT !");
        }

        // WeekManager
        if (WeekManager.Instance != null)
        {
            Debug.Log("✓ WeekManager trouvé");
        }
        else
        {
            Debug.LogError("✗ WeekManager MANQUANT !");
        }

        // ExamManager
        ExamManager examMgr = FindFirstObjectByType<ExamManager>();
        if (examMgr != null)
        {
            Debug.Log("✓ ExamManager trouvé (sera créé dynamiquement)");
        }

        Debug.Log("=== FIN DIAGNOSTIC ===");
    }

    private void Update()
    {
        // DÉSACTIVÉ - Utilise Input System maintenant
        // Les tests d'inputs ne sont plus nécessaires car l'Input System gère ça

        /* ANCIEN CODE AVEC KEYCODE (NE FONCTIONNE PAS AVEC INPUT SYSTEM)
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            Debug.Log("[UIDebugger] TAB pressé");

            if (UIManager.Instance != null)
            {
                Debug.Log($"  - UIManager.IsGameplayActive: {UIManager.Instance.IsGameplayActive()}");
            }

            if (CheatSheetManager.Instance != null)
            {
                Debug.Log($"  - CheatSheetManager.IsAvailable: {CheatSheetManager.Instance.IsAvailable()}");
            }
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Log("[UIDebugger] ESCAPE pressé");
        }
        */
    }
}
