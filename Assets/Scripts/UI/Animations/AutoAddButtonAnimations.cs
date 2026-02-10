using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Script à placer sur le Canvas principal (dans chaque scène)
/// Ajoute automatiquement ButtonHoverAnimation à TOUS les boutons au démarrage
/// </summary>
public class AutoAddButtonAnimations : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Ajouter automatiquement ButtonHoverAnimation à tous les boutons?")]
    [SerializeField] private bool autoAddOnStart = true;

    [Tooltip("Inclure les boutons désactivés?")]
    [SerializeField] private bool includeInactive = true;

    private void Start()
    {
        if (autoAddOnStart)
        {
            AddAnimationsToAllButtons();
        }
    }

    /// <summary>
    /// Ajoute ButtonHoverAnimation à TOUS les boutons trouvés dans la scène
    /// </summary>
    public void AddAnimationsToAllButtons()
    {
        // Trouver TOUS les boutons dans la scène (y compris inactifs)
        Button[] allButtons = FindObjectsByType<Button>(includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        int added = 0;
        int skipped = 0;

        foreach (Button button in allButtons)
        {
            // Vérifier si le bouton a déjà ButtonHoverAnimation
            if (button.GetComponent<ButtonHoverAnimation>() == null)
            {
                button.gameObject.AddComponent<ButtonHoverAnimation>();
                added++;
            }
            else
            {
                skipped++;
            }
        }

        Debug.Log($"[AutoAddButtonAnimations] ✅ {added} boutons animés, {skipped} déjà animés");
    }

    /// <summary>
    /// Supprime ButtonHoverAnimation de TOUS les boutons (utile pour debug)
    /// </summary>
    public void RemoveAnimationsFromAllButtons()
    {
        ButtonHoverAnimation[] anims = FindObjectsByType<ButtonHoverAnimation>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (var anim in anims)
        {
            DestroyImmediate(anim);
        }

        Debug.Log($"[AutoAddButtonAnimations] ❌ {anims.Length} animations supprimées");
    }
}
