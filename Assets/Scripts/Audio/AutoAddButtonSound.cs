using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Ajoute automatiquement un son de clic à TOUS les boutons enfants
/// Mettre ce script sur le Canvas principal
/// </summary>
public class AutoAddButtonSound : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("Chercher aussi dans les objets inactifs")]
    [SerializeField] private bool includeInactive = true;

    private void Awake()
    {
        AddSoundToAllButtons();
    }

    /// <summary>
    /// Trouve tous les boutons et leur ajoute un listener pour le son
    /// </summary>
    private void AddSoundToAllButtons()
    {
        Button[] allButtons = GetComponentsInChildren<Button>(includeInactive);

        int count = 0;
        foreach (Button button in allButtons)
        {
            // Ajouter le listener pour le son de clic
            button.onClick.AddListener(PlayButtonSound);
            count++;
        }

        Debug.Log($"[AutoAddButtonSound] {count} boutons configurés avec son de clic");
    }

    private void PlayButtonSound()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClick();
        }
    }
}
