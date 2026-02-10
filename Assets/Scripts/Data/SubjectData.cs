using UnityEngine;

/// <summary>
/// Configuration d'une matière scolaire (Maths, Français, Histoire, etc.)
/// À créer en tant que ScriptableObject asset (Right Click > Create > StealthCheater > Subject Data)
/// </summary>
[CreateAssetMenu(fileName = "SubjectData", menuName = "StealthCheater/Subject Data", order = 2)]
public class SubjectData : ScriptableObject
{
    [Header("Subject Info")]
    [Tooltip("Type de matière (Maths, Français, etc.)")]
    public SubjectType subjectType = SubjectType.Maths;

    [Tooltip("Nom affiché de la matière")]
    public string displayName = "Mathématiques";

    [Tooltip("Couleur de la matière (pour UI/feedback)")]
    public Color subjectColor = Color.blue;

    [Tooltip("Icône de la matière (optionnel)")]
    public Sprite subjectIcon;

    [Header("Exam Configuration")]
    [Tooltip("Nombre de questions par contrôle")]
    [Range(2, 5)]
    public int questionsPerExam = 3;

    [Header("Writing Time Settings")]
    [Tooltip("Temps minimum pour écrire une réponse courte/simple (secondes)")]
    [Range(1f, 5f)]
    public float minWritingTime = 2f;

    [Tooltip("Temps maximum pour écrire une réponse longue/complexe (secondes)")]
    [Range(3f, 10f)]
    public float maxWritingTime = 5f;

    /// <summary>
    /// Calcule le temps d'écriture dans la ReturnZone selon la compétence de l'élève copié
    /// Réponse d'élève compétent = longue → temps écriture long
    /// Réponse d'élève faible = courte → temps écriture court
    /// </summary>
    public float GetWritingTime(int copiedStudentSkill)
    {
        // Plus l'élève copié est compétent, plus la réponse est longue/complexe
        return Mathf.Lerp(minWritingTime, maxWritingTime, copiedStudentSkill / 100f);
    }

    /// <summary>
    /// Génère le nom d'une question (simplifié : "Question 1", "Question 2", etc.)
    /// </summary>
    public string GetQuestionText(int questionNumber)
    {
        return $"Trouver la réponse à la question {questionNumber}";
    }

    /// <summary>
    /// Calcule le temps de copie moyen
    /// Estimation basée sur une compétence moyenne de 60% (élève "bon")
    /// </summary>
    public float GetAverageCopyTime()
    {
        // Estimation : élève avec compétence 60% (bon élève)
        // Temps copie moyen entre minSkillCopyTime (0%) et maxSkillCopyTime (100%)
        // Formule : Lerp(minSkillCopyTime, maxSkillCopyTime, 0.6)
        // Avec valeurs par défaut : Lerp(3, 10, 0.6) = 7.2 secondes

        // On retourne une estimation raisonnable : 8 secondes
        // (peut être ajusté selon le niveau de difficulté souhaité)
        return 8f;
    }

    /// <summary>
    /// Calcule le temps d'écriture moyen
    /// Moyenne entre le temps minimum et maximum
    /// </summary>
    public float GetAverageWriteTime()
    {
        // Moyenne entre temps min et max
        return (minWritingTime + maxWritingTime) / 2f;
    }
}
