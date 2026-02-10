using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI pour afficher les résultats de la semaine complète (5 jours)
/// Component à ajouter sur le GameObject WeekResults dans le Canvas
/// </summary>
public class WeekResultsUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Texte affichant le nom de la semaine (ex: 'Semaine 1')")]
    [SerializeField] private TextMeshProUGUI weekNameText;

    [Tooltip("Texte affichant la moyenne de la semaine (ex: '14.5/20')")]
    [SerializeField] private TextMeshProUGUI averageText;

    [Tooltip("Texte affichant les détails de chaque jour")]
    [SerializeField] private TextMeshProUGUI detailsText;

    [Tooltip("Bouton 'Recommencer' pour refaire la semaine")]
    [SerializeField] private Button restartButton;

    [Tooltip("Bouton 'Menu Principal' (optionnel)")]
    [SerializeField] private Button menuButton;

    private void Awake()
    {
        // Configurer les boutons
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(OnRestartClicked);
        }
        else
        {
            Debug.LogWarning("[WeekResultsUI] Bouton Restart non assigné!");
        }

        if (menuButton != null)
        {
            menuButton.onClick.AddListener(OnMenuClicked);
        }
    }

    /// <summary>
    /// Affiche les résultats de la semaine
    /// </summary>
    public void ShowResults()
    {
        if (WeekManager.Instance == null)
        {
            Debug.LogError("[WeekResultsUI] WeekManager introuvable!");
            return;
        }

        WeekConfiguration weekConfig = WeekManager.Instance.GetWeekConfiguration();
        float[] grades = WeekManager.Instance.GetAllDayGrades();
        float average = WeekManager.Instance.GetWeekAverage();

        // Mettre à jour le nom de la semaine
        if (weekNameText != null && weekConfig != null)
        {
            weekNameText.text = weekConfig.weekName;
        }

        // Mettre à jour la moyenne
        if (averageText != null)
        {
            averageText.text = $"Average: {average:F2}/20";
        }

        // Mettre à jour les détails
        if (detailsText != null && weekConfig != null)
        {
            string details = "";

            for (int i = 0; i < grades.Length; i++)
            {
                DayData day = weekConfig.GetDay(i);
                if (day != null)
                {
                    details += $"{day.dayName}: {grades[i]:F2}/20\n";
                }
            }

            detailsText.text = details;
        }

        // Utiliser UIManager pour afficher le panel
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowWeekResults();
        }
        else
        {
            Debug.LogWarning("[WeekResultsUI] UIManager introuvable! Le panel ne sera pas affiché correctement.");
        }
    }

    /// <summary>
    /// Appelé quand le joueur clique sur "Recommencer"
    /// </summary>
    private void OnRestartClicked()
    {
        // Notifier le WeekManager
        if (WeekManager.Instance != null)
        {
            WeekManager.Instance.OnWeekResultsRestart();
        }
        else
        {
            Debug.LogError("[WeekResultsUI] WeekManager introuvable!");
        }
    }

    /// <summary>
    /// Appelé quand le joueur clique sur "Menu Principal"
    /// </summary>
    private void OnMenuClicked()
    {
        // Charger le menu principal
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
}
