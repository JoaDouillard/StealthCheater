using UnityEngine;

/// <summary>
/// Configuration d'un élève avec ses compétences par matière
/// À créer en tant que ScriptableObject asset (Right Click > Create > StealthCheater > Student Data)
/// </summary>
[CreateAssetMenu(fileName = "StudentData", menuName = "StealthCheater/Student Data", order = 1)]
public class StudentData : ScriptableObject
{
    [Header("Student Info")]
    [Tooltip("Nom de l'élève (ex: Thomas, Marie, Lucas, etc.)")]
    public string studentName = "Élève";

    [Header("Subject Competency Levels (0-100%)")]
    [Tooltip("Compétence en Mathématiques (0 = nul, 100 = expert)")]
    [Range(0, 100)]
    public int mathsSkill = 50;

    [Tooltip("Compétence en Français (0 = nul, 100 = expert)")]
    [Range(0, 100)]
    public int frenchSkill = 50;

    [Tooltip("Compétence en Histoire (0 = nul, 100 = expert)")]
    [Range(0, 100)]
    public int historySkill = 50;

    [Tooltip("Compétence en Sciences (0 = nul, 100 = expert)")]
    [Range(0, 100)]
    public int scienceSkill = 50;

    [Tooltip("Compétence en Philosophie (0 = nul, 100 = expert)")]
    [Range(0, 100)]
    public int philosophySkill = 50;

    [Tooltip("Compétence en Anglais (0 = nul, 100 = expert)")]
    [Range(0, 100)]
    public int englishSkill = 50;

    [Header("Copy Time Settings")]
    [Tooltip("Temps de copie si compétence 0% (secondes) - Rapide car réponse courte/simple")]
    [Range(1f, 5f)]
    public float minSkillCopyTime = 3f;

    [Tooltip("Temps de copie si compétence 100% (secondes) - Lent car réponse longue/complexe")]
    [Range(5f, 20f)]
    public float maxSkillCopyTime = 10f;

    [Header("Visual Hint Settings")]
    [Tooltip("Couleur du hint pour cet élève (utilisée dans l'UI/monde)")]
    public Color hintColor = Color.white;

    [Tooltip("Icône optionnelle pour identifier l'élève")]
    public Sprite studentIcon;

    /// <summary>
    /// Obtient le niveau de compétence pour une matière donnée
    /// </summary>
    public int GetSkillForSubject(SubjectType subjectType)
    {
        switch (subjectType)
        {
            case SubjectType.Maths:
                return mathsSkill;
            case SubjectType.French:
                return frenchSkill;
            case SubjectType.History:
                return historySkill;
            case SubjectType.Science:
                return scienceSkill;
            case SubjectType.Philosophy:
                return philosophySkill;
            case SubjectType.English:
                return englishSkill;
            default:
                Debug.LogWarning($"[StudentData] Subject type inconnu: {subjectType}");
                return 0;
        }
    }

    /// <summary>
    /// Calcule le temps de copie selon la compétence de l'élève
    /// Plus l'élève est compétent, plus ça prend du temps (réponse complexe)
    /// </summary>
    public float GetCopyTime(int skillLevel)
    {
        // Lerp entre minSkillCopyTime (0%) et maxSkillCopyTime (100%)
        return Mathf.Lerp(minSkillCopyTime, maxSkillCopyTime, skillLevel / 100f);
    }

    /// <summary>
    /// Calcule les points obtenus selon la compétence de l'élève
    /// Compétence 0-100% → Points 0-20 (note /20)
    /// </summary>
    public float GetPoints(int skillLevel)
    {
        // Convertir 0-100% en 0-20 points
        return skillLevel / 5f;
    }

    /// <summary>
    /// Retourne un hint textuel sur la compétence (pour UI)
    /// </summary>
    public string GetSkillHint(SubjectType subjectType)
    {
        int skill = GetSkillForSubject(subjectType);

        if (skill >= 80)
            return "Expert";
        else if (skill >= 60)
            return "Bon";
        else if (skill >= 40)
            return "Moyen";
        else if (skill >= 20)
            return "Faible";
        else
            return "Très faible";
    }
}

/// <summary>
/// Enum pour les différentes matières scolaires
/// </summary>
public enum SubjectType
{
    Maths,
    French,
    History,
    Science,
    Philosophy,
    English
}
