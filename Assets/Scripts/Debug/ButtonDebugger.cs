using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Script de diagnostic pour les problèmes de boutons non cliquables
/// Placer sur le Canvas principal pour diagnostiquer les problèmes
/// </summary>
public class ButtonDebugger : MonoBehaviour
{
    [Header("Debug Settings")]
    [SerializeField] private bool runDiagnosticOnStart = true;
    [SerializeField] private bool fixProblemsAutomatically = true;

    private void Start()
    {
        if (runDiagnosticOnStart)
        {
            Invoke(nameof(RunFullDiagnostic), 0.5f); // Délai pour que tout soit initialisé
        }
    }

    [ContextMenu("Run Full Diagnostic")]
    public void RunFullDiagnostic()
    {
        Debug.Log("=== [ButtonDebugger] DIAGNOSTIC COMPLET ===");

        CheckEventSystem();
        CheckGraphicRaycaster();
        CheckCanvasGroups();
        CheckButtons();

        Debug.Log("=== [ButtonDebugger] FIN DIAGNOSTIC ===");
    }

    private void CheckEventSystem()
    {
        EventSystem eventSystem = FindFirstObjectByType<EventSystem>();

        if (eventSystem == null)
        {
            Debug.LogError("[ButtonDebugger] ❌ AUCUN EventSystem trouvé! Les boutons ne peuvent pas recevoir d'input.");

            if (fixProblemsAutomatically)
            {
                GameObject esObj = new GameObject("EventSystem");
                esObj.AddComponent<EventSystem>();
                esObj.AddComponent<StandaloneInputModule>();
                Debug.Log("[ButtonDebugger] ✅ EventSystem créé automatiquement");
            }
        }
        else if (!eventSystem.enabled || !eventSystem.gameObject.activeInHierarchy)
        {
            Debug.LogError($"[ButtonDebugger] ❌ EventSystem '{eventSystem.name}' est DÉSACTIVÉ!");

            if (fixProblemsAutomatically)
            {
                eventSystem.enabled = true;
                eventSystem.gameObject.SetActive(true);
                Debug.Log("[ButtonDebugger] ✅ EventSystem réactivé");
            }
        }
        else
        {
            Debug.Log($"[ButtonDebugger] ✅ EventSystem OK: {eventSystem.name}");
        }
    }

    private void CheckGraphicRaycaster()
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (Canvas canvas in canvases)
        {
            GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();

            if (raycaster == null)
            {
                Debug.LogWarning($"[ButtonDebugger] ⚠️ Canvas '{canvas.name}' n'a pas de GraphicRaycaster!");

                if (fixProblemsAutomatically)
                {
                    canvas.gameObject.AddComponent<GraphicRaycaster>();
                    Debug.Log($"[ButtonDebugger] ✅ GraphicRaycaster ajouté à '{canvas.name}'");
                }
            }
            else if (!raycaster.enabled)
            {
                Debug.LogWarning($"[ButtonDebugger] ⚠️ GraphicRaycaster sur '{canvas.name}' est désactivé!");

                if (fixProblemsAutomatically)
                {
                    raycaster.enabled = true;
                    Debug.Log($"[ButtonDebugger] ✅ GraphicRaycaster réactivé sur '{canvas.name}'");
                }
            }
            else
            {
                Debug.Log($"[ButtonDebugger] ✅ Canvas '{canvas.name}' a GraphicRaycaster OK");
            }
        }
    }

    private void CheckCanvasGroups()
    {
        CanvasGroup[] groups = FindObjectsByType<CanvasGroup>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (CanvasGroup group in groups)
        {
            if (!group.blocksRaycasts)
            {
                Debug.LogWarning($"[ButtonDebugger] ⚠️ CanvasGroup sur '{group.name}' a blocksRaycasts=FALSE! Les boutons enfants ne seront pas cliquables.");
            }

            if (!group.interactable)
            {
                Debug.LogWarning($"[ButtonDebugger] ⚠️ CanvasGroup sur '{group.name}' a interactable=FALSE! Les boutons enfants seront désactivés visuellement.");
            }

            if (group.alpha < 0.01f)
            {
                Debug.LogWarning($"[ButtonDebugger] ⚠️ CanvasGroup sur '{group.name}' a alpha={group.alpha}! Invisible.");
            }
        }
    }

    private void CheckButtons()
    {
        Button[] allButtons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        int totalButtons = allButtons.Length;
        int buttonsWithAnimation = 0;
        int buttonsWithRaycastIssue = 0;
        int buttonsWithInteractableIssue = 0;

        foreach (Button button in allButtons)
        {
            // Vérifier l'animation
            if (button.GetComponent<ButtonHoverAnimation>() != null)
            {
                buttonsWithAnimation++;
            }

            // Vérifier le raycast target sur l'image
            Image image = button.GetComponent<Image>();
            if (image != null && !image.raycastTarget)
            {
                Debug.LogWarning($"[ButtonDebugger] ⚠️ Bouton '{button.name}' a Image.raycastTarget=FALSE!");
                buttonsWithRaycastIssue++;

                if (fixProblemsAutomatically)
                {
                    image.raycastTarget = true;
                    Debug.Log($"[ButtonDebugger] ✅ raycastTarget activé sur '{button.name}'");
                }
            }

            // Vérifier interactable
            if (!button.interactable)
            {
                Debug.LogWarning($"[ButtonDebugger] ⚠️ Bouton '{button.name}' a interactable=FALSE!");
                buttonsWithInteractableIssue++;
            }

            // Vérifier si le bouton est dans un panel de pause
            if (button.transform.parent != null && button.transform.parent.name.Contains("Pause"))
            {
                Debug.Log($"[ButtonDebugger] 📌 Bouton Pause: '{button.name}' - Interactable={button.interactable}, HasAnimation={button.GetComponent<ButtonHoverAnimation>() != null}");
            }
        }

        Debug.Log($"[ButtonDebugger] 📊 RÉSUMÉ BOUTONS:");
        Debug.Log($"  - Total: {totalButtons}");
        Debug.Log($"  - Avec animation hover: {buttonsWithAnimation}");
        Debug.Log($"  - Problème raycast: {buttonsWithRaycastIssue}");
        Debug.Log($"  - Non interactables: {buttonsWithInteractableIssue}");
    }

    /// <summary>
    /// Force l'ajout de ButtonHoverAnimation à tous les boutons qui n'en ont pas
    /// </summary>
    [ContextMenu("Force Add Animations To All Buttons")]
    public void ForceAddAnimationsToAllButtons()
    {
        Button[] allButtons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        int added = 0;
        foreach (Button button in allButtons)
        {
            if (button.GetComponent<ButtonHoverAnimation>() == null)
            {
                button.gameObject.AddComponent<ButtonHoverAnimation>();
                added++;
                Debug.Log($"[ButtonDebugger] ✅ Animation ajoutée à '{button.name}'");
            }
        }

        Debug.Log($"[ButtonDebugger] {added} animations ajoutées");
    }
}
