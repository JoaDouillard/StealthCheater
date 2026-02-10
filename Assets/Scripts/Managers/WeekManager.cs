using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Gère une semaine scolaire complète (5 jours : Lundi → Vendredi)
/// Chaque jour = 1 examen = 1 level
/// Singleton accessible globalement, persiste entre les reloads de scène
/// </summary>
public class WeekManager : MonoBehaviour
{
    public static WeekManager Instance { get; private set; }

    [Header("Configuration")]
    [Tooltip("Configuration de la semaine (ScriptableObject)")]
    [SerializeField] private WeekConfiguration weekConfig;

    [Header("Scene References")]
    [Tooltip("Nom de la scène de jeu (GameScene) à recharger entre chaque jour")]
    [SerializeField] private string gameSceneName = "GameScene";

    [Header("Scene References")]
    [Tooltip("Nom de la scène Game Over (GameOver)")]
    [SerializeField] private string gameOverSceneName = "GameOver";

    [Header("Debug")]
    [Tooltip("Afficher les logs de progression des jours (pour debug)")]
    [SerializeField] private bool showDayProgressLogs = true;

    [Tooltip("Afficher les autres logs de debug")]
    [SerializeField] private bool showDebugLogs = false;

    [Header("UI References")]
    [Tooltip("Script DayResultsUI (doit être dans la scène)")]
    [SerializeField] private DayResultsUI dayResultsUI;

    [Tooltip("Script WeekResultsUI (doit être dans la scène)")]
    [SerializeField] private WeekResultsUI weekResultsUI;

    // État de la semaine
    private int currentDayIndex = 0; // 0 = Lundi, 4 = Vendredi
    private float[] dayGrades = new float[5]; // Notes de chaque jour (/20)
    private ExamManager currentExam;

    // Flags
    private bool isWeekStarted = false;
    private bool isWeekCompleted = false;
    private bool isDayInProgress = false;

    // Données du jour actuel
    private SubjectData currentDayExam;
    private int currentDayQuestions;

    private void Awake()
    {
        LogDebug($"⚡ Awake() - Instance existe déjà? {Instance != null}");

        // Singleton pattern avec DontDestroyOnLoad
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // S'abonner à l'événement de chargement de scène
            SceneManager.sceneLoaded += OnSceneLoaded;
            LogDebug("✅ Singleton créé, abonné à sceneLoaded");
        }
        else
        {
            LogDebug("❌ Instance existe déjà, destruction de ce doublon");
            Destroy(gameObject);
            return;
        }
    }

    private void OnDestroy()
    {
        // Se désabonner pour éviter les memory leaks
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// Appelé à chaque chargement de scène (y compris après reload)
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        LogDayProgress($"📦 OnSceneLoaded: '{scene.name}', currentDayIndex={currentDayIndex}");

        // Seulement si c'est la scène de jeu et que la semaine est en cours
        if (scene.name == gameSceneName && isWeekStarted && !isWeekCompleted && !isDayInProgress)
        {
            LogDayProgress($"✅ Démarrage du jour {currentDayIndex + 1}");
            StartCurrentDay();
        }
    }

    private void Start()
    {
        LogDayProgress($"🚀 Start() - currentDayIndex={currentDayIndex}");

        if (weekConfig == null)
        {
            Debug.LogError("[WeekManager] ❌ WeekConfiguration manquante!");
            return;
        }

        if (!weekConfig.ValidateConfiguration())
        {
            Debug.LogError("[WeekManager] ❌ Configuration invalide!");
            return;
        }

        // Vérifier si une sauvegarde est chargée
        if (SaveManager.Instance != null && SaveManager.Instance.HasLoadedSave())
        {
            LoadFromSave();
        }
        else if (!isWeekStarted)
        {
            LogDayProgress("▶️ Première fois, démarrage semaine");
            StartWeek();
        }
    }

    /// <summary>
    /// Charge les données depuis la sauvegarde
    /// </summary>
    private void LoadFromSave()
    {
        LogDayProgress("========== LoadFromSave() ==========");

        SaveData saveData = SaveManager.Instance.GetCurrentSaveData();

        if (saveData == null)
        {
            Debug.LogError("[WeekManager] SaveData est null!");
            StartWeek(); // Fallback
            return;
        }

        LogDayProgress($"💾 saveData.currentDayIndex = {saveData.currentDayIndex}");

        // Restaurer l'état
        isWeekStarted = true;
        currentDayIndex = saveData.currentDayIndex;

        LogDayProgress($"currentDayIndex mis à {currentDayIndex}");

        // Restaurer les notes
        for (int i = 0; i < dayGrades.Length && i < saveData.dayGrades.Length; i++)
        {
            dayGrades[i] = saveData.dayGrades[i];
        }

        // Démarrer le jour actuel
        StartCurrentDay();
    }

    /// <summary>
    /// Démarre la semaine scolaire
    /// </summary>
    public void StartWeek()
    {
        if (isWeekStarted)
        {
            LogDebug("La semaine est déjà démarrée!");
            return;
        }

        isWeekStarted = true;
        currentDayIndex = 0;

        // Réinitialiser toutes les notes
        for (int i = 0; i < dayGrades.Length; i++)
        {
            dayGrades[i] = 0f;
        }

        LogDayProgress($"🏫 Début de la semaine: {weekConfig.weekName}");

        // Démarrer le premier jour
        StartCurrentDay();
    }

    /// <summary>
    /// Démarre le jour actuel
    /// </summary>
    private void StartCurrentDay()
    {
        LogDayProgress($"========== StartCurrentDay() - Index: {currentDayIndex} ==========");

        if (currentDayIndex >= 5)
        {
            LogDayProgress("currentDayIndex >= 5, fin de semaine");
            EndWeek();
            return;
        }

        if (weekConfig == null)
        {
            Debug.LogError("[WeekManager] ❌ weekConfig est NULL!");
            return;
        }

        DayData dayData = weekConfig.GetDay(currentDayIndex);

        if (dayData == null)
        {
            Debug.LogError($"[WeekManager] ❌ DayData NULL pour index {currentDayIndex}!");
            return;
        }

        // Choisir examen et nombre de questions aléatoirement
        currentDayExam = dayData.GetRandomExam();
        currentDayQuestions = dayData.GetRandomQuestionCount();

        if (currentDayExam == null)
        {
            Debug.LogError($"[WeekManager] ❌ Impossible de choisir un examen pour {dayData.dayName}!");
            return;
        }

        isDayInProgress = true;

        LogDayProgress($"✅ JOUR: {dayData.dayName} (Index {currentDayIndex}) - Examen: {currentDayExam.displayName}");

        // Créer l'ExamManager pour cet examen
        GameObject examObj = new GameObject($"ExamManager_{currentDayExam.displayName}");
        currentExam = examObj.AddComponent<ExamManager>();

        // Passer le nom du jour
        string dayName = dayData.dayName;
        currentExam.Initialize(currentDayExam, currentDayQuestions, this, dayName);
    }

    /// <summary>
    /// Appelé par ExamManager quand l'examen est terminé
    /// </summary>
    public void OnExamCompleted(float grade)
    {
        LogDayProgress($"✅ Examen terminé! Note: {grade:F2}/20 (dayIndex: {currentDayIndex})");

        // Stocker la note
        dayGrades[currentDayIndex] = grade;

        isDayInProgress = false;

        // Détruire l'ExamManager
        if (currentExam != null)
        {
            Destroy(currentExam.gameObject);
            currentExam = null;
        }

        // Auto-save après chaque examen
        if (SaveManager.Instance != null && SaveManager.Instance.HasLoadedSave())
        {
            SaveManager.Instance.UpdateCurrentSaveData(currentDayIndex, dayGrades);
            SaveManager.Instance.AutoSave();
            LogDayProgress("💾 Auto-save effectué");
        }

        // Afficher les résultats du jour
        ShowDayResults();
    }

    /// <summary>
    /// Appelé par TeacherDetection (via GameManager) quand le joueur est attrapé
    /// </summary>
    public void OnPlayerCaught()
    {
        isDayInProgress = false;

        // Détruire l'ExamManager actuel
        if (currentExam != null)
        {
            Destroy(currentExam.gameObject);
            currentExam = null;
        }

        // GameManager charge la scène GameOver, pas besoin de le faire ici
    }

    /// <summary>
    /// Affiche les résultats du jour
    /// </summary>
    private void ShowDayResults()
    {
        // Récupérer les infos du jour
        DayData dayData = weekConfig.GetDay(currentDayIndex);
        string dayName = dayData != null ? dayData.dayName : $"Jour {currentDayIndex + 1}";
        string examName = currentDayExam != null ? currentDayExam.displayName : "Examen";
        float grade = dayGrades[currentDayIndex];

        LogDayProgress($"ShowDayResults - Index: {currentDayIndex}, Jour: {dayName}");

        // IMPORTANT: Toujours re-chercher DayResultsUI car la scène a pu être rechargée
        dayResultsUI = FindFirstObjectByType<DayResultsUI>(FindObjectsInactive.Include);

        if (dayResultsUI != null)
        {
            dayResultsUI.ShowResults(dayName, examName, grade);
        }
        else
        {
            Debug.LogError("[WeekManager] ❌ DayResultsUI introuvable!");
        }
    }

    /// <summary>
    /// Appelé par le bouton "Continuer" de l'UI DayResults
    /// </summary>
    public void OnDayResultsContinue()
    {
        LogDayProgress($"========== OnDayResultsContinue() ==========");
        LogDayProgress($"currentDayIndex AVANT: {currentDayIndex}");

        // Retour au HUD (UIManager gère le panel et Time.timeScale)
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowHUD();
        }

        // Passer au jour suivant
        currentDayIndex++;
        isDayInProgress = false;

        LogDayProgress($"currentDayIndex APRÈS: {currentDayIndex}");

        // Sauvegarder IMMÉDIATEMENT avec le nouveau dayIndex
        if (SaveManager.Instance != null && SaveManager.Instance.HasLoadedSave())
        {
            LogDayProgress($"💾 Sauvegarde avec dayIndex={currentDayIndex}");
            SaveManager.Instance.UpdateCurrentSaveData(currentDayIndex, dayGrades);
            SaveManager.Instance.AutoSave();
        }

        if (currentDayIndex >= 5)
        {
            EndWeek();
        }
        else
        {
            LogDayProgress($"Rechargement scène pour jour {currentDayIndex + 1}");
            SceneManager.LoadScene(gameSceneName);
        }
    }

    /// <summary>
    /// Fin de la semaine (tous les jours terminés)
    /// </summary>
    private void EndWeek()
    {
        if (isWeekCompleted)
        {
            return;
        }

        isWeekCompleted = true;

        // Calculer moyenne générale
        float totalGrade = 0f;
        for (int i = 0; i < dayGrades.Length; i++)
        {
            totalGrade += dayGrades[i];
        }

        float averageGrade = totalGrade / dayGrades.Length;

        LogDayProgress($"🏁 FIN DE LA SEMAINE! Moyenne: {averageGrade:F2}/20");

        // Afficher résultats de la semaine
        ShowWeekResults();
    }

    /// <summary>
    /// Affiche les résultats de la semaine
    /// </summary>
    private void ShowWeekResults()
    {
        // IMPORTANT: Toujours re-chercher WeekResultsUI car la scène a pu être rechargée
        weekResultsUI = FindFirstObjectByType<WeekResultsUI>(FindObjectsInactive.Include);

        if (weekResultsUI != null)
        {
            weekResultsUI.ShowResults();
        }
        else
        {
            Debug.LogError("[WeekManager] ❌ WeekResultsUI introuvable!");
        }
    }

    /// <summary>
    /// Appelé par le bouton "Recommencer" de l'UI WeekResults
    /// </summary>
    public void OnWeekResultsRestart()
    {
        // Reprendre le jeu (UIManager gérera le panel si nécessaire)
        Time.timeScale = 1f;

        // Redémarrer la semaine
        RestartWeek();
    }

    /// <summary>
    /// Charge la scène Game Over
    /// </summary>
    private void LoadGameOverScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameOverSceneName);
    }

    /// <summary>
    /// Relance le jour actuel (utilisé par GameOver)
    /// Conserve les notes des jours précédents
    /// </summary>
    public void RestartCurrentDay()
    {
        LogDayProgress($"🔄 Relance du jour {currentDayIndex}");

        // Remettre la note du jour actuel à 0
        dayGrades[currentDayIndex] = 0f;
        isDayInProgress = false;

        // Détruire l'ExamManager actuel si existe
        if (currentExam != null)
        {
            Destroy(currentExam.gameObject);
            currentExam = null;
        }

        // Auto-save avant de recharger
        if (SaveManager.Instance != null && SaveManager.Instance.HasLoadedSave())
        {
            SaveManager.Instance.UpdateCurrentSaveData(currentDayIndex, dayGrades);
            SaveManager.Instance.AutoSave();
        }

        SceneManager.LoadScene(gameSceneName);
    }

    /// <summary>
    /// Redémarre la semaine depuis le début
    /// </summary>
    public void RestartWeek()
    {
        // Reset variables
        currentDayIndex = 0;
        isWeekStarted = false;
        isWeekCompleted = false;
        isDayInProgress = false;

        // Reset notes
        for (int i = 0; i < dayGrades.Length; i++)
        {
            dayGrades[i] = 0f;
        }

        // Détruire l'ExamManager actuel si existe
        if (currentExam != null)
        {
            Destroy(currentExam.gameObject);
            currentExam = null;
        }

        LogDayProgress("🔄 Redémarrage de la semaine...");

        SceneManager.LoadScene(gameSceneName);
    }

    /// <summary>
    /// Obtient la configuration de la semaine
    /// </summary>
    public WeekConfiguration GetWeekConfiguration()
    {
        return weekConfig;
    }

    /// <summary>
    /// Obtient l'index du jour actuel (0 = Lundi, 4 = Vendredi)
    /// </summary>
    public int GetCurrentDayIndex()
    {
        return currentDayIndex;
    }

    /// <summary>
    /// Obtient la note d'un jour spécifique
    /// </summary>
    public float GetDayGrade(int dayIndex)
    {
        if (dayIndex < 0 || dayIndex >= dayGrades.Length)
        {
            return 0f;
        }

        return dayGrades[dayIndex];
    }

    /// <summary>
    /// Obtient toutes les notes de la semaine
    /// </summary>
    public float[] GetAllDayGrades()
    {
        return dayGrades;
    }

    /// <summary>
    /// Obtient la moyenne de la semaine
    /// </summary>
    public float GetWeekAverage()
    {
        float total = 0f;
        for (int i = 0; i < dayGrades.Length; i++)
        {
            total += dayGrades[i];
        }
        return total / dayGrades.Length;
    }

    /// <summary>
    /// Log conditionnel (seulement si showDebugLogs est activé)
    /// </summary>
    private void LogDebug(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[WeekManager] {message}");
        }
    }

    /// <summary>
    /// Log de progression des jours (ON par défaut pour debug)
    /// </summary>
    private void LogDayProgress(string message)
    {
        if (showDayProgressLogs)
        {
            Debug.Log($"[WeekManager] {message}");
        }
    }
}
