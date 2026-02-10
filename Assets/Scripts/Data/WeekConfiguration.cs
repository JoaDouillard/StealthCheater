using UnityEngine;

/// <summary>
/// Configuration d'une semaine scolaire (5 jours : Lundi → Vendredi)
/// Chaque jour a 1 examen choisi aléatoirement parmi un pool
/// À créer en tant que ScriptableObject asset (Right Click > Create > StealthCheater > Week Configuration)
/// </summary>
[CreateAssetMenu(fileName = "WeekConfig", menuName = "StealthCheater/Week Configuration", order = 4)]
public class WeekConfiguration : ScriptableObject
{
    [Header("Week Info")]
    [Tooltip("Nom de la semaine (ex: Semaine 1, Semaine 2, etc.)")]
    public string weekName = "Semaine 1";

    [Tooltip("Description de la semaine (optionnel)")]
    [TextArea(2, 3)]
    public string description = "Semaine d'examens";

    [Header("Days Configuration")]
    [Tooltip("Configuration des 5 jours de la semaine (Lundi → Vendredi)")]
    public DayData[] weekDays = new DayData[5];

    [Header("Passing Grade")]
    [Tooltip("Moyenne minimum pour réussir la semaine (/20)")]
    [Range(0f, 20f)]
    public float passingGrade = 10f;

    /// <summary>
    /// Obtient la configuration d'un jour spécifique
    /// </summary>
    /// <param name="dayIndex">Index du jour (0 = Lundi, 4 = Vendredi)</param>
    public DayData GetDay(int dayIndex)
    {
        if (dayIndex < 0 || dayIndex >= weekDays.Length)
        {
            Debug.LogError($"[WeekConfiguration] Index de jour invalide: {dayIndex}");
            return null;
        }

        return weekDays[dayIndex];
    }

    /// <summary>
    /// Valide que la configuration est correcte
    /// </summary>
    public bool ValidateConfiguration()
    {
        bool isValid = true;

        if (weekDays == null || weekDays.Length != 5)
        {
            Debug.LogError($"[WeekConfiguration] {weekName}: Doit contenir exactement 5 jours (Lundi → Vendredi)");
            isValid = false;
            return isValid;
        }

        // Vérifier chaque jour
        for (int i = 0; i < weekDays.Length; i++)
        {
            if (weekDays[i] == null)
            {
                Debug.LogError($"[WeekConfiguration] {weekName}: Jour #{i + 1} est null!");
                isValid = false;
                continue;
            }

            if (!weekDays[i].ValidateDay(i))
            {
                isValid = false;
            }
        }

        return isValid;
    }

    private void OnValidate()
    {
        // Validation automatique dans l'Inspector
        ValidateConfiguration();
    }
}

/// <summary>
/// Configuration d'un jour de la semaine
/// </summary>
[System.Serializable]
public class DayData
{
    [Header("Day Info")]
    [Tooltip("Nom du jour (ex: Lundi, Mardi, etc.)")]
    public string dayName = "Lundi";

    [Header("Exam Configuration")]
    [Tooltip("Pool d'examens possibles pour ce jour (l'examen sera choisi aléatoirement)")]
    public SubjectData[] possibleExams;

    [Header("Questions Configuration")]
    [Tooltip("Nombre minimum de questions par examen")]
    [Range(2, 5)]
    public int minQuestions = 2;

    [Tooltip("Nombre maximum de questions par examen")]
    [Range(2, 5)]
    public int maxQuestions = 5;

    /// <summary>
    /// Choisit un examen aléatoire parmi le pool
    /// </summary>
    public SubjectData GetRandomExam()
    {
        if (possibleExams == null || possibleExams.Length == 0)
        {
            Debug.LogError($"[DayData] {dayName}: Aucun examen possible!");
            return null;
        }

        return possibleExams[Random.Range(0, possibleExams.Length)];
    }

    /// <summary>
    /// Choisit un nombre de questions aléatoire
    /// </summary>
    public int GetRandomQuestionCount()
    {
        return Random.Range(minQuestions, maxQuestions + 1);
    }

    /// <summary>
    /// Valide que la configuration du jour est correcte
    /// </summary>
    public bool ValidateDay(int dayIndex)
    {
        bool isValid = true;

        if (possibleExams == null || possibleExams.Length == 0)
        {
            Debug.LogError($"[DayData] {dayName} (jour {dayIndex + 1}): Aucun examen dans le pool!");
            isValid = false;
        }
        else
        {
            // Vérifier qu'aucun SubjectData n'est null
            for (int i = 0; i < possibleExams.Length; i++)
            {
                if (possibleExams[i] == null)
                {
                    Debug.LogError($"[DayData] {dayName} (jour {dayIndex + 1}): Examen #{i + 1} est null!");
                    isValid = false;
                }
            }
        }

        if (minQuestions > maxQuestions)
        {
            Debug.LogWarning($"[DayData] {dayName}: minQuestions ({minQuestions}) > maxQuestions ({maxQuestions}). Ajustement automatique.");
            minQuestions = maxQuestions;
        }

        return isValid;
    }
}
