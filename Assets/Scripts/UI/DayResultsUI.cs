using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI pour afficher les résultats d'un jour (1 examen)
/// Component à ajouter sur le GameObject DayResults dans le Canvas
/// </summary>
public class DayResultsUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Texte affichant le nom du jour (ex: 'Lundi')")]
    [SerializeField] private TextMeshProUGUI dayNameText;

    [Tooltip("Texte affichant le nom de l'examen (ex: 'Mathématiques')")]
    [SerializeField] private TextMeshProUGUI examNameText;

    [Tooltip("Texte affichant la note (ex: '16/20')")]
    [SerializeField] private TextMeshProUGUI gradeText;

    [Tooltip("Bouton 'Continuer' pour passer au jour suivant")]
    [SerializeField] private Button continueButton;

    private void Awake()
    {
        // Configurer le bouton
        if (continueButton != null)
        {
            continueButton.onClick.AddListener(OnContinueClicked);
        }
        else
        {
            Debug.LogWarning("[DayResultsUI] Bouton Continue non assigné!");
        }
    }

    /// <summary>
    /// Affiche les résultats du jour
    /// </summary>
    /// <param name="dayName">Nom du jour (Lundi, Mardi, etc.)</param>
    /// <param name="examName">Nom de l'examen (Mathématiques, etc.)</param>
    /// <param name="grade">Note obtenue (/20)</param>
    public void ShowResults(string dayName, string examName, float grade)
    {
        // Mettre à jour les textes
        if (dayNameText != null)
        {
            dayNameText.text = dayName;
        }

        if (examNameText != null)
        {
            examNameText.text = examName;
        }

        if (gradeText != null)
        {
            gradeText.text = $"{grade:F2}/20";
        }

        // Utiliser UIManager pour afficher le panel
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowDayResults();
        }
        else
        {
            Debug.LogWarning("[DayResultsUI] UIManager introuvable! Le panel ne sera pas affiché correctement.");
        }
    }

    /// <summary>
    /// Appelé quand le joueur clique sur "Continuer"
    /// </summary>
    private void OnContinueClicked()
    {
        // Notifier le WeekManager
        if (WeekManager.Instance != null)
        {
            WeekManager.Instance.OnDayResultsContinue();
        }
        else
        {
            Debug.LogError("[DayResultsUI] WeekManager introuvable!");
        }
    }
}
