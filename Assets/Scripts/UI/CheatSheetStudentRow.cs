using UnityEngine;
using TMPro;

/// <summary>
/// Une ligne dans l'anti-sèche: nom de l'étudiant + ses notes par matière
/// </summary>
public class CheatSheetStudentRow : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI studentNameText;
    [SerializeField] private TextMeshProUGUI mathsText;
    [SerializeField] private TextMeshProUGUI frenchText;
    [SerializeField] private TextMeshProUGUI scienceText;
    [SerializeField] private TextMeshProUGUI historyText;
    [SerializeField] private TextMeshProUGUI philosophyText;
    [SerializeField] private TextMeshProUGUI englishText;

    // Couleurs fixes
    private static readonly Color goodColor = Color.green;   // >= 80%
    private static readonly Color badColor = Color.red;      // < 30%
    private static readonly Color normalColor = Color.black; // entre 30-80%

    /// <summary>
    /// Configure la ligne avec les données de l'étudiant
    /// </summary>
    public void Setup(StudentData student)
    {
        if (student == null) return;

        // Nom
        if (studentNameText != null)
            studentNameText.text = student.studentName;

        // Notes avec couleurs
        SetupGrade(mathsText, student.mathsSkill);
        SetupGrade(frenchText, student.frenchSkill);
        SetupGrade(scienceText, student.scienceSkill);
        SetupGrade(historyText, student.historySkill);
        SetupGrade(philosophyText, student.philosophySkill);
        SetupGrade(englishText, student.englishSkill);
    }

    private void SetupGrade(TextMeshProUGUI text, float skill)
    {
        if (text == null) return;

        // Afficher la note (skill est en %, on convertit en /20)
        float grade = skill / 5f; // 100% = 20/20
        text.text = $"{grade:F0}";

        // Couleur selon le niveau
        if (skill >= 80f)
            text.color = goodColor;
        else if (skill < 30f)
            text.color = badColor;
        else
            text.color = normalColor;
    }
}
