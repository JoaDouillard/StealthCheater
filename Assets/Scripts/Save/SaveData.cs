using System;

/// <summary>
/// Structure contenant toutes les données d'une sauvegarde
/// Sérialisable pour être sauvegardée en JSON
/// </summary>
[Serializable]
public class SaveData
{
    // Métadonnées
    public string saveName;              // Nom donné par le joueur (ex: "Ma partie cool")
    public int saveSlotIndex;            // 0, 1, 2 (Slot 1, 2, 3)
    public string lastSaveDate;          // Date/heure dernière sauvegarde (format: "yyyy-MM-dd HH:mm:ss")
    public bool isUsed;                  // true si ce slot contient une sauvegarde

    // Progression
    public int currentDayIndex;          // 0-4 (0=Lundi, 1=Mardi, ..., 4=Vendredi)
    public float[] dayGrades;            // Notes de chaque jour (0-20), array de 5 éléments

    /// <summary>
    /// Constructeur par défaut (slot vide)
    /// </summary>
    public SaveData()
    {
        saveName = "";
        saveSlotIndex = 0;
        lastSaveDate = "";
        isUsed = false;
        currentDayIndex = 0;
        dayGrades = new float[5] { 0f, 0f, 0f, 0f, 0f };
    }

    /// <summary>
    /// Constructeur avec données
    /// </summary>
    public SaveData(string name, int slotIndex, int dayIndex, float[] grades)
    {
        saveName = name;
        saveSlotIndex = slotIndex;
        lastSaveDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        isUsed = true;
        currentDayIndex = dayIndex;

        // Copier les notes (sécurité)
        dayGrades = new float[5];
        if (grades != null && grades.Length == 5)
        {
            Array.Copy(grades, dayGrades, 5);
        }
        else
        {
            for (int i = 0; i < 5; i++)
                dayGrades[i] = 0f;
        }
    }

    /// <summary>
    /// Calcule la moyenne des jours joués (ignore les jours à 0)
    /// </summary>
    public float GetAverageScore()
    {
        // Moyenne des jours JOUÉS seulement (pas les jours à 0)
        float total = 0f;
        int daysPlayed = 0;

        for (int i = 0; i <= currentDayIndex; i++)
        {
            if (i < dayGrades.Length)
            {
                total += dayGrades[i];
                if (dayGrades[i] > 0f) // On compte seulement les jours avec une vraie note
                {
                    daysPlayed++;
                }
            }
        }

        // Si aucun jour joué, retourner 0
        if (daysPlayed == 0)
            return 0f;

        return total / daysPlayed;
    }

    /// <summary>
    /// Retourne le nom du jour actuel depuis la WeekConfiguration
    /// </summary>
    public string GetCurrentDayName()
    {
        // Essayer d'obtenir le nom depuis WeekManager (qui a la vraie config)
        if (WeekManager.Instance != null)
        {
            var weekConfig = WeekManager.Instance.GetWeekConfiguration();
            if (weekConfig != null)
            {
                var dayData = weekConfig.GetDay(currentDayIndex);
                if (dayData != null)
                {
                    return dayData.dayName;
                }
            }
        }

        // Fallback si WeekManager pas disponible
        switch (currentDayIndex)
        {
            case 0: return "Day 1";
            case 1: return "Day 2";
            case 2: return "Day 3";
            case 3: return "Day 4";
            case 4: return "Day 5";
            default: return "Unknown";
        }
    }

    /// <summary>
    /// Clone cette sauvegarde
    /// </summary>
    public SaveData Clone()
    {
        SaveData clone = new SaveData();
        clone.saveName = saveName;
        clone.saveSlotIndex = saveSlotIndex;
        clone.lastSaveDate = lastSaveDate;
        clone.isUsed = isUsed;
        clone.currentDayIndex = currentDayIndex;
        clone.dayGrades = new float[5];
        Array.Copy(dayGrades, clone.dayGrades, 5);
        return clone;
    }
}
