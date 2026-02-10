using UnityEngine;
using System.IO;

/// <summary>
/// Gère le système de sauvegarde avec 3 slots
/// Singleton persistant entre les scènes
/// Sauvegarde en JSON dans Application.persistentDataPath
/// </summary>
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    [Header("Settings")]
    [Tooltip("Nombre de slots de sauvegarde disponibles")]
    [SerializeField] private int numberOfSlots = 3;

    [Header("Debug")]
    [Tooltip("Afficher les logs de progression des jours (pour debug)")]
    [SerializeField] private bool showDayProgressLogs = true;

    [Tooltip("Afficher les autres logs de debug")]
    [SerializeField] private bool showDebugLogs = false;

    // Slot actuellement chargé (0-2, ou -1 si aucun)
    private int currentLoadedSlot = -1;

    // Données actuelles en mémoire
    private SaveData currentSaveData = null;

    // Chemin du dossier de sauvegarde
    private string saveFolderPath;

    private void Awake()
    {
        // Singleton pattern avec DontDestroyOnLoad
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Définir le chemin de sauvegarde
        saveFolderPath = Path.Combine(Application.persistentDataPath, "Saves");

        // Créer le dossier s'il n'existe pas
        if (!Directory.Exists(saveFolderPath))
        {
            Directory.CreateDirectory(saveFolderPath);
            LogDebug($"Dossier de sauvegarde créé: {saveFolderPath}");
        }

        LogDebug($"SaveManager initialisé. Dossier: {saveFolderPath}");
    }

    #region Save/Load Operations

    /// <summary>
    /// Crée une nouvelle partie dans un slot
    /// </summary>
    /// <param name="slotIndex">Index du slot (0-2)</param>
    /// <param name="saveName">Nom donné à la sauvegarde</param>
    public void CreateNewSave(int slotIndex, string saveName)
    {
        if (!IsValidSlotIndex(slotIndex))
        {
            Debug.LogError($"[SaveManager] Index de slot invalide: {slotIndex}");
            return;
        }

        // Créer une nouvelle sauvegarde
        currentSaveData = new SaveData(saveName, slotIndex, 0, new float[5]);
        currentLoadedSlot = slotIndex;

        // Sauvegarder immédiatement
        SaveToSlot(slotIndex);

        LogDebug($"Nouvelle sauvegarde créée: '{saveName}' dans slot {slotIndex + 1}");
    }

    /// <summary>
    /// Charge une sauvegarde depuis un slot
    /// </summary>
    /// <param name="slotIndex">Index du slot (0-2)</param>
    /// <returns>True si chargement réussi</returns>
    public bool LoadFromSlot(int slotIndex)
    {
        if (!IsValidSlotIndex(slotIndex))
        {
            Debug.LogError($"[SaveManager] Index de slot invalide: {slotIndex}");
            return false;
        }

        string filePath = GetSaveFilePath(slotIndex);

        if (!File.Exists(filePath))
        {
            Debug.LogWarning($"[SaveManager] Aucune sauvegarde trouvée dans slot {slotIndex + 1}");
            return false;
        }

        try
        {
            // Lire le fichier JSON
            string json = File.ReadAllText(filePath);
            currentSaveData = JsonUtility.FromJson<SaveData>(json);
            currentLoadedSlot = slotIndex;

            LogDebug($"Sauvegarde chargée: '{currentSaveData.saveName}' depuis slot {slotIndex + 1}");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveManager] Erreur lors du chargement: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Sauvegarde les données actuelles dans un slot
    /// </summary>
    /// <param name="slotIndex">Index du slot (0-2)</param>
    public void SaveToSlot(int slotIndex)
    {
        if (!IsValidSlotIndex(slotIndex))
        {
            Debug.LogError($"[SaveManager] Index de slot invalide: {slotIndex}");
            return;
        }

        if (currentSaveData == null)
        {
            Debug.LogWarning("[SaveManager] Aucune donnée à sauvegarder!");
            return;
        }

        // Mettre à jour les métadonnées
        currentSaveData.saveSlotIndex = slotIndex;
        currentSaveData.lastSaveDate = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        try
        {
            // Convertir en JSON
            string json = JsonUtility.ToJson(currentSaveData, true);

            // Écrire dans le fichier
            string filePath = GetSaveFilePath(slotIndex);
            File.WriteAllText(filePath, json);

            LogDebug($"Sauvegarde écrite dans slot {slotIndex + 1}: {filePath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveManager] Erreur lors de la sauvegarde: {e.Message}");
        }
    }

    /// <summary>
    /// Auto-save dans le slot actuellement chargé
    /// </summary>
    public void AutoSave()
    {
        if (currentLoadedSlot == -1)
        {
            LogDebug("AutoSave ignoré: aucun slot chargé");
            return;
        }

        if (currentSaveData == null)
        {
            Debug.LogWarning("[SaveManager] AutoSave: aucune donnée en mémoire!");
            return;
        }

        SaveToSlot(currentLoadedSlot);
        LogDebug($"AutoSave effectué dans slot {currentLoadedSlot + 1}");
    }

    /// <summary>
    /// Supprime une sauvegarde
    /// </summary>
    /// <param name="slotIndex">Index du slot (0-2)</param>
    public void DeleteSave(int slotIndex)
    {
        if (!IsValidSlotIndex(slotIndex))
        {
            Debug.LogError($"[SaveManager] Index de slot invalide: {slotIndex}");
            return;
        }

        string filePath = GetSaveFilePath(slotIndex);

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            LogDebug($"Sauvegarde supprimée du slot {slotIndex + 1}");

            // Si c'était le slot chargé, réinitialiser
            if (currentLoadedSlot == slotIndex)
            {
                currentLoadedSlot = -1;
                currentSaveData = null;
            }
        }
        else
        {
            Debug.LogWarning($"[SaveManager] Aucune sauvegarde à supprimer dans slot {slotIndex + 1}");
        }
    }

    #endregion

    #region Slot Info

    /// <summary>
    /// Vérifie si un slot contient une sauvegarde
    /// </summary>
    public bool IsSlotUsed(int slotIndex)
    {
        if (!IsValidSlotIndex(slotIndex))
            return false;

        string filePath = GetSaveFilePath(slotIndex);
        return File.Exists(filePath);
    }

    /// <summary>
    /// Récupère les infos d'un slot sans le charger
    /// </summary>
    public SaveData GetSlotInfo(int slotIndex)
    {
        if (!IsValidSlotIndex(slotIndex))
            return null;

        string filePath = GetSaveFilePath(slotIndex);

        if (!File.Exists(filePath))
            return null;

        try
        {
            string json = File.ReadAllText(filePath);
            return JsonUtility.FromJson<SaveData>(json);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveManager] Erreur lecture slot {slotIndex + 1}: {e.Message}");
            return null;
        }
    }

    #endregion

    #region Current Save Data

    /// <summary>
    /// Met à jour les données de sauvegarde actuelles
    /// </summary>
    public void UpdateCurrentSaveData(int dayIndex, float[] grades)
    {
        LogDayProgress($"UpdateCurrentSaveData - dayIndex: {dayIndex}");

        if (currentSaveData == null)
        {
            Debug.LogError("[SaveManager] currentSaveData est NULL!");
            return;
        }

        LogDayProgress($"currentDayIndex: {currentSaveData.currentDayIndex} → {dayIndex}");
        currentSaveData.currentDayIndex = dayIndex;

        if (grades != null && grades.Length == 5)
        {
            System.Array.Copy(grades, currentSaveData.dayGrades, 5);
        }
    }

    /// <summary>
    /// Obtient les données de sauvegarde actuelles
    /// </summary>
    public SaveData GetCurrentSaveData()
    {
        return currentSaveData;
    }

    /// <summary>
    /// Obtient le slot actuellement chargé (-1 si aucun)
    /// </summary>
    public int GetCurrentLoadedSlot()
    {
        return currentLoadedSlot;
    }

    /// <summary>
    /// Vérifie si une sauvegarde est actuellement chargée
    /// </summary>
    public bool HasLoadedSave()
    {
        return currentLoadedSlot != -1 && currentSaveData != null;
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Retourne le chemin complet du fichier de sauvegarde pour un slot
    /// </summary>
    private string GetSaveFilePath(int slotIndex)
    {
        return Path.Combine(saveFolderPath, $"save_slot_{slotIndex}.json");
    }

    /// <summary>
    /// Vérifie si un index de slot est valide
    /// </summary>
    private bool IsValidSlotIndex(int slotIndex)
    {
        return slotIndex >= 0 && slotIndex < numberOfSlots;
    }

    /// <summary>
    /// Log de debug si activé
    /// </summary>
    private void LogDebug(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[SaveManager] {message}");
        }
    }

    /// <summary>
    /// Log de progression des jours (ON par défaut pour debug)
    /// </summary>
    private void LogDayProgress(string message)
    {
        if (showDayProgressLogs)
        {
            Debug.Log($"[SaveManager] {message}");
        }
    }

    #endregion

    #region Public Utilities

    /// <summary>
    /// Obtient le nombre de slots disponibles
    /// </summary>
    public int GetNumberOfSlots()
    {
        return numberOfSlots;
    }

    /// <summary>
    /// Vérifie si au moins un slot contient une sauvegarde
    /// </summary>
    public bool HasAnySave()
    {
        for (int i = 0; i < numberOfSlots; i++)
        {
            if (IsSlotUsed(i))
                return true;
        }
        return false;
    }

    #endregion
}
