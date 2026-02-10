using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Diagnostic complet des panels UI
/// Teste l'affichage de chaque panel et affiche des logs détaillés
/// </summary>
public class UIPanelDiagnostic : MonoBehaviour
{
    [Header("Test Manual")]
    [Tooltip("Appuie sur les touches pour tester chaque panel")]
    public bool enableManualTests = true;

    private void Start()
    {
        Debug.Log("=== UI PANEL DIAGNOSTIC ===");
        DiagnosticUIManager();
        Debug.Log("=== APPUIE SUR LES TOUCHES POUR TESTER ===");
        Debug.Log("1 = CheatSheet | 2 = Pause | 3 = Settings");
        Debug.Log("4 = DayResults | 5 = WeekResults | 6 = ExamStart");
        Debug.Log("7 = QuestionPrompt | 8 = Tutorial | 0 = HUD");
    }

    /// <summary>
    /// Diagnostic complet de UIManager
    /// </summary>
    void DiagnosticUIManager()
    {
        if (UIManager.Instance == null)
        {
            Debug.LogError("✗ UIManager INTROUVABLE !");
            return;
        }

        Debug.Log("✓ UIManager trouvé");

        // Utiliser réflexion pour accéder aux champs privés
        var type = typeof(UIManager);
        var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

        // Vérifier chaque panel
        CheckPanel("hudPanel");
        CheckPanel("pauseMenuPanel");
        CheckPanel("settingsPanel");
        CheckPanel("cheatSheetPanel");
        CheckPanel("dayResultsPanel");
        CheckPanel("weekResultsPanel");
        CheckPanel("examStartPanel");
        CheckPanel("questionPromptPanel");
        CheckPanel("tutorialPanel");
    }

    /// <summary>
    /// Vérifie si un panel est assigné dans UIManager
    /// </summary>
    void CheckPanel(string fieldName)
    {
        var type = typeof(UIManager);
        var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        var field = type.GetField(fieldName, flags);

        if (field != null)
        {
            GameObject panel = field.GetValue(UIManager.Instance) as GameObject;
            if (panel != null)
            {
                Debug.Log($"  ✓ {fieldName} assigné : {panel.name}");
            }
            else
            {
                Debug.LogError($"  ✗ {fieldName} NON ASSIGNÉ dans UIManager !");
            }
        }
    }

    private void Update()
    {
        if (!enableManualTests) return;
        if (Keyboard.current == null) return;

        // Tester CheatSheet
        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            Debug.Log("[TEST] Affichage CheatSheet...");
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowCheatSheet();
                Debug.Log($"  → Appel UIManager.ShowCheatSheet() effectué");
                Debug.Log($"  → IsGameplayActive: {UIManager.Instance.IsGameplayActive()}");
            }
        }

        // Tester Pause
        if (Keyboard.current.digit2Key.wasPressedThisFrame)
        {
            Debug.Log("[TEST] Affichage Pause Menu...");
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowPauseMenu();
                Debug.Log($"  → Appel UIManager.ShowPauseMenu() effectué");
            }
        }

        // Tester Settings
        if (Keyboard.current.digit3Key.wasPressedThisFrame)
        {
            Debug.Log("[TEST] Affichage Settings...");
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowSettings();
                Debug.Log($"  → Appel UIManager.ShowSettings() effectué");
            }
        }

        // Tester DayResults
        if (Keyboard.current.digit4Key.wasPressedThisFrame)
        {
            Debug.Log("[TEST] Affichage DayResults...");
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowDayResults();
                Debug.Log($"  → Appel UIManager.ShowDayResults() effectué");
            }
        }

        // Tester WeekResults
        if (Keyboard.current.digit5Key.wasPressedThisFrame)
        {
            Debug.Log("[TEST] Affichage WeekResults...");
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowWeekResults();
                Debug.Log($"  → Appel UIManager.ShowWeekResults() effectué");
            }
        }

        // Tester ExamStart
        if (Keyboard.current.digit6Key.wasPressedThisFrame)
        {
            Debug.Log("[TEST] Affichage ExamStart...");
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowExamStart();
                Debug.Log($"  → Appel UIManager.ShowExamStart() effectué");
            }
        }

        // Tester Tutorial
        if (Keyboard.current.digit8Key.wasPressedThisFrame)
        {
            Debug.Log("[TEST] Affichage Tutorial...");
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowTutorial();
                Debug.Log($"  → Appel UIManager.ShowTutorial() effectué");
            }
        }

        // Retour HUD
        if (Keyboard.current.digit0Key.wasPressedThisFrame)
        {
            Debug.Log("[TEST] Retour HUD...");
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowHUD();
                Debug.Log($"  → Appel UIManager.ShowHUD() effectué");
            }
        }
    }
}
