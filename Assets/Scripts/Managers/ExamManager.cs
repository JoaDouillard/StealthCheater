using UnityEngine;

/// <summary>
/// Gère UN contrôle (exam) avec ses questions, timer, et score
/// Créé dynamiquement par WeekManager pour chaque jour
/// </summary>
public class ExamManager : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("UI affichée au début du level ET pour chaque question")]
    [SerializeField] private ExamStartUI examStartUI;

    [Header("Debug")]
    [Tooltip("Afficher les logs de debug dans la console")]
    [SerializeField] private bool showDebugLogs = false;

    [Header("Time Configuration")]
    [Tooltip("Secondes de bonus le lundi (diminue de bonusReductionPerDay chaque jour)")]
    [SerializeField] private float mondayTimeBonus = 60f;

    [Tooltip("Réduction du bonus de temps par jour (ex: 5 = -5s par jour après lundi)")]
    [SerializeField] private float bonusReductionPerDay = 5f;

    // Configuration
    private SubjectData subjectData;
    private WeekManager weekManager;
    private string dayName; // "Monday", "Tuesday", etc.

    // État du contrôle
    private int totalQuestions;
    private int currentQuestionIndex = 0;  // Question en cours (1-based)
    private int questionsAnswered = 0;     // Questions répondues (pour le compteur HUD)
    private float currentExamScore = 0f;   // Score accumulé (0-20)

    // Timer
    private float examTimer = 0f;
    private float maxExamTime = 0f; // Temps maximum calculé automatiquement
    private bool isExamActive = false;

    // Flags
    private bool isWaitingForAnswer = false;
    private bool isInitialized = false;

    // Copie actuelle (pour passer les données à ReturnZone)
    private float currentCopyPoints = 0f;
    private StudentData currentCopiedStudent = null;

    // Liste des CopyZones actives pour cette question
    private System.Collections.Generic.List<CopyZone> activeCopyZones = new System.Collections.Generic.List<CopyZone>();

    private void Awake()
    {
        // Trouver ExamStartUI si pas assigné (même si désactivé)
        if (examStartUI == null)
        {
            examStartUI = FindFirstObjectByType<ExamStartUI>(FindObjectsInactive.Include);
            if (examStartUI == null)
            {
                Debug.LogError("[ExamManager] ExamStartUI introuvable ! Assigne-le dans l'Inspector.");
            }
        }
    }

    /// <summary>
    /// Initialise le contrôle avec sa configuration
    /// </summary>
    /// <param name="subject">Matière de l'examen</param>
    /// <param name="questionCount">Nombre de questions (2-5)</param>
    /// <param name="manager">WeekManager parent</param>
    /// <param name="day">Nom du jour (Monday, Tuesday, etc.)</param>
    public void Initialize(SubjectData subject, int questionCount, WeekManager manager, string day = "Monday")
    {
        subjectData = subject;
        totalQuestions = questionCount;
        weekManager = manager;
        dayName = day;

        if (subjectData == null)
        {
            Debug.LogError("[ExamManager] SubjectData est null!");
            return;
        }

        // Calculer le temps maximum:
        // Pour chaque question: temps MAX de copie + temps MAX de recopie
        // + bonus jour (lundi = 60s, -5s par jour)

        // Temps max de copie = maxSkillCopyTime (défaut 10s pour compétence 100%)
        float maxCopyTimePerQuestion = 10f;

        // Temps max de recopie = maxWritingTime (défaut 5s)
        float maxWriteTimePerQuestion = subjectData.maxWritingTime;

        // Temps par question
        float timePerQuestion = maxCopyTimePerQuestion + maxWriteTimePerQuestion;

        // Temps total des questions
        float questionsTime = timePerQuestion * totalQuestions;

        // Bonus selon le jour (lundi = 60s, mardi = 55s, etc.)
        int dayIndex = GetDayIndex(dayName);
        float dayBonus = mondayTimeBonus - (dayIndex * bonusReductionPerDay);
        dayBonus = Mathf.Max(0f, dayBonus);

        // Temps total = (temps par question × nombre de questions) + bonus jour
        maxExamTime = questionsTime + dayBonus;

        LogDebug($"Init: {subjectData.displayName}, {totalQuestions}Q, {maxExamTime:F0}s");

        isInitialized = true;

        // IMPORTANT: Attendre que le level soit complètement chargé avant de démarrer
        // Les Students sont dans les levels dynamiques qui peuvent ne pas être encore activés
        StartCoroutine(WaitForLevelAndStartExam());
    }

    /// <summary>
    /// Attend que le level soit chargé puis démarre l'examen
    /// </summary>
    private System.Collections.IEnumerator WaitForLevelAndStartExam()
    {
        // Attendre quelques frames pour que LevelSpawner active le level
        yield return null;
        yield return null;
        yield return null;

        // Vérifier que le level est bien chargé
        if (LevelSpawner.Instance != null)
        {
            GameObject activeLevel = LevelSpawner.Instance.GetActiveLevel();
            if (activeLevel == null)
            {
                yield return new WaitForSeconds(0.5f);
            }
        }

        StartExam();
    }

    /// <summary>
    /// Démarre le contrôle
    /// </summary>
    private void StartExam()
    {
        if (!isInitialized)
        {
            Debug.LogError("[ExamManager] ExamManager pas initialisé!");
            return;
        }

        isExamActive = true;
        currentQuestionIndex = 0;
        questionsAnswered = 0;
        currentExamScore = 0f;
        examTimer = 0f;

        LogDebug($"Début du contrôle: {subjectData.displayName}");

        // Afficher le panel de début d'examen
        if (examStartUI != null)
        {
            examStartUI.Show(dayName, subjectData.displayName, totalQuestions);
        }

        // Activer les CopyZones pour les élèves compétents dans cette matière
        ActivateCopyZonesForSubject();

        // Afficher les infos dans le HUD
        if (GameHUD.Instance != null)
        {
            GameHUD.Instance.SetMaxExamTime(maxExamTime); // Pour les couleurs du timer
            GameHUD.Instance.ShowExamInfo(subjectData.displayName, 0, totalQuestions);
        }

        // Attendre la fin de la séquence d'intro PUIS démarrer la première question
        StartCoroutine(WaitForIntroThenStartFirstQuestion());
    }

    /// <summary>
    /// Attend la fin de la séquence d'intro puis démarre la première question
    /// </summary>
    private System.Collections.IEnumerator WaitForIntroThenStartFirstQuestion()
    {
        // Attendre que la séquence d'intro soit terminée
        if (examStartUI != null)
        {
            while (examStartUI.IsSequenceRunning)
            {
                yield return null;
            }
        }

        // Maintenant démarrer la première question
        StartNextQuestion();
    }

    private void Update()
    {
        if (!isExamActive) return;

        // Incrémenter le timer
        examTimer += Time.deltaTime;
        float remainingTime = maxExamTime - examTimer;

        // Afficher le timer dans le HUD
        if (GameHUD.Instance != null)
        {
            GameHUD.Instance.ShowExamTimer(remainingTime);
        }

        // Tic-tac en boucle à partir de 15 secondes avant la fin
        if (remainingTime <= 15f && remainingTime > 0f && AudioManager.Instance != null)
        {
            AudioManager.Instance.StartTimerTicking();
        }

        // Sonnerie d'école 2 secondes avant la fin
        if (remainingTime <= 2f && remainingTime > 0f && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySchoolBell();
        }

        // Vérifier si le temps est écoulé
        if (examTimer >= maxExamTime)
        {
            LogDebug("Temps écoulé!");
            EndExam();
        }
    }

    /// <summary>
    /// Démarre la prochaine question
    /// </summary>
    private void StartNextQuestion()
    {
        // Vérifier si on a déjà répondu à toutes les questions
        if (questionsAnswered >= totalQuestions)
        {
            // Toutes les questions sont terminées
            EndExam();
            return;
        }

        currentQuestionIndex++;
        isWaitingForAnswer = true;

        LogDebug($"Question {currentQuestionIndex}/{totalQuestions}");

        // Réactiver les CopyZones pour cette question (si pas la première)
        if (currentQuestionIndex > 1)
        {
            ReactivateCopyZonesForNextQuestion();
        }

        // Afficher le numéro de question avec ExamStartUI
        if (examStartUI != null)
        {
            examStartUI.ShowQuestionNumber(currentQuestionIndex);
        }

        // Le compteur HUD affiche les questions RÉPONDUES (pas la question en cours)
        // Il sera mis à jour dans OnAnswerCompleted() après avoir répondu
    }

    /// <summary>
    /// Active 2-4 CopyZones ALÉATOIRES parmi les Students compétents
    /// Chaque Student a UNE CopyZone en enfant (désactivée par défaut)
    /// </summary>
    private void ActivateCopyZonesForSubject()
    {
        var studentsList = new System.Collections.Generic.List<GameObject>();

        // Chercher par TAG "Student"
        try
        {
            GameObject[] taggedStudents = GameObject.FindGameObjectsWithTag("Student");
            if (taggedStudents != null && taggedStudents.Length > 0)
                studentsList.AddRange(taggedStudents);
        }
        catch { }

        // Fallback: chercher par component Student
        if (studentsList.Count == 0)
        {
            Student[] studentComponents = FindObjectsByType<Student>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Student s in studentComponents)
            {
                if (s != null && s.gameObject != null)
                    studentsList.Add(s.gameObject);
            }
        }

        if (studentsList.Count == 0)
        {
            Debug.LogError("[ExamManager] Aucun Student trouvé!");
            return;
        }

        // Trouver les CopyZones compétentes
        var competentZones = new System.Collections.Generic.List<CopyZone>();

        foreach (GameObject studentObj in studentsList)
        {
            CopyZone zone = studentObj.GetComponentInChildren<CopyZone>(true);
            if (zone != null)
            {
                StudentData studentData = zone.GetStudentData();
                if (studentData != null)
                {
                    float skill = studentData.GetSkillForSubject(subjectData.subjectType);
                    if (skill >= 10f)
                        competentZones.Add(zone);
                }
            }
        }

        if (competentZones.Count == 0)
        {
            Debug.LogError("[ExamManager] Aucune CopyZone compétente!");
            return;
        }

        // Activer toutes les CopyZones compétentes
        activeCopyZones.Clear();
        foreach (CopyZone zone in competentZones)
        {
            zone.gameObject.SetActive(true);
            zone.ActivateForSubject(subjectData.subjectType);
            activeCopyZones.Add(zone);
        }

        LogDebug($"{activeCopyZones.Count} CopyZones activées");
    }

    /// <summary>
    /// Désactive toutes les CopyZones
    /// </summary>
    private void DeactivateAllCopyZones()
    {
        // Trouver toutes les CopyZones directement (plus fiable)
        CopyZone[] allZones = FindObjectsByType<CopyZone>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        int deactivated = 0;
        foreach (CopyZone zone in allZones)
        {
            if (zone != null)
            {
                zone.Deactivate();
                deactivated++;
            }
        }

        LogDebug($"{deactivated} CopyZones désactivées");
    }

    /// <summary>
    /// Appelé par CopyZone quand le joueur a fini de copier
    /// </summary>
    public void OnCopyCompleted(float pointsEarned, StudentData copiedStudent)
    {
        if (!isWaitingForAnswer) return;

        LogDebug($"Copie terminée: {pointsEarned:F2} pts");

        currentCopyPoints = pointsEarned;
        currentCopiedStudent = copiedStudent;

        DisableCopyZonesForCurrentQuestion();

        ReturnZone returnZone = FindFirstObjectByType<ReturnZone>();
        if (returnZone != null)
        {
            returnZone.StartWriting(pointsEarned, copiedStudent, subjectData);
        }
        else
        {
            Debug.LogError("[ExamManager] ReturnZone introuvable!");
        }

        if (GameHUD.Instance != null)
        {
            GameHUD.Instance.ShowInteractionPrompt("Return to your desk to write the answer!");
        }
    }

    /// <summary>
    /// Désactive toutes les CopyZones pour cette question (une seule copie autorisée)
    /// </summary>
    private void DisableCopyZonesForCurrentQuestion()
    {
        int disabled = 0;
        foreach (CopyZone zone in activeCopyZones)
        {
            if (zone != null)
            {
                zone.Deactivate();
                disabled++;
            }
        }
        LogDebug($"{disabled} CopyZones désactivées");
    }

    /// <summary>
    /// Réactive les CopyZones pour la prochaine question
    /// </summary>
    private void ReactivateCopyZonesForNextQuestion()
    {
        int reactivated = 0;
        foreach (CopyZone zone in activeCopyZones)
        {
            if (zone != null)
            {
                zone.gameObject.SetActive(true);
                zone.ActivateForSubject(subjectData.subjectType);
                reactivated++;
            }
        }
        LogDebug($"{reactivated} CopyZones réactivées");
    }

    /// <summary>
    /// Appelé par ReturnZone quand le joueur a fini d'écrire la réponse
    /// </summary>
    public void OnAnswerCompleted(float pointsEarned)
    {
        if (!isWaitingForAnswer) return;

        questionsAnswered++;

        LogDebug($"Réponse: {pointsEarned:F2} pts ({questionsAnswered}/{totalQuestions})");

        // Ajouter les points au score du contrôle
        currentExamScore += pointsEarned;

        isWaitingForAnswer = false;

        // Mettre à jour le compteur HUD avec les questions RÉPONDUES
        if (GameHUD.Instance != null)
        {
            GameHUD.Instance.UpdateQuestionCounter(questionsAnswered, totalQuestions);
        }

        // Passer à la question suivante
        StartNextQuestion();
    }

    /// <summary>
    /// Termine le contrôle et calcule la note finale
    /// </summary>
    private void EndExam()
    {
        isExamActive = false;

        // Reset l'état du timer (tic-tac + sonnerie) pour le prochain examen
        if (AudioManager.Instance != null)
            AudioManager.Instance.ResetTimerState();

        // Désactiver toutes les CopyZones
        DeactivateAllCopyZones();

        // Cacher les infos du HUD
        if (GameHUD.Instance != null)
        {
            GameHUD.Instance.HideExamInfo();
            GameHUD.Instance.HideExamTimer();
            GameHUD.Instance.HideInteractionPrompt();
            GameHUD.Instance.HideCopyProgress();
            GameHUD.Instance.HideWriteProgress();
        }

        // Calculer note finale
        float finalGrade = CalculateFinalGrade();

        LogDebug($"Exam terminé: {finalGrade:F2}/20");

        // Notifier le WeekManager
        if (weekManager != null)
        {
            weekManager.OnExamCompleted(finalGrade);
        }
        else
        {
            WeekManager wm = FindFirstObjectByType<WeekManager>();
            if (wm != null)
                wm.OnExamCompleted(finalGrade);
        }
    }

    /// <summary>
    /// Calcule la note finale avec pénalité de timer
    /// </summary>
    private float CalculateFinalGrade()
    {
        // La note est la MOYENNE des points gagnés par question
        // Si le joueur n'a répondu à aucune question, note = 0
        if (questionsAnswered == 0)
            return 0f;

        float averageScore = currentExamScore / questionsAnswered;

        // TODO: Implémenter système de pénalité de timer (à faire après selon user)
        // Formule future:
        // - Meilleur temps = Σ(temps copie max de toutes questions) + bonusTime
        // - Si temps > meilleur temps → pénalité = (temps - meilleur temps) * penaltyPerSecond
        // - Note finale = max(0, score brut - pénalité)

        return Mathf.Clamp(averageScore, 0f, 20f);
    }

    /// <summary>
    /// Obtient le type de matière du contrôle
    /// </summary>
    public SubjectType GetSubjectType()
    {
        return subjectData != null ? subjectData.subjectType : SubjectType.Maths;
    }

    /// <summary>
    /// Obtient l'index de la question actuelle (1-based)
    /// </summary>
    public int GetCurrentQuestionNumber()
    {
        return currentQuestionIndex;
    }

    /// <summary>
    /// Obtient le nombre total de questions
    /// </summary>
    public int GetTotalQuestions()
    {
        return totalQuestions;
    }

    /// <summary>
    /// Obtient le temps écoulé
    /// </summary>
    public float GetElapsedTime()
    {
        return examTimer;
    }

    /// <summary>
    /// Obtient le temps maximum autorisé
    /// </summary>
    public float GetMaxTime()
    {
        return maxExamTime;
    }

    /// <summary>
    /// Obtient le temps restant
    /// </summary>
    public float GetRemainingTime()
    {
        return Mathf.Max(0f, maxExamTime - examTimer);
    }

    /// <summary>
    /// Obtient le score actuel
    /// </summary>
    public float GetCurrentScore()
    {
        return currentExamScore;
    }

    /// <summary>
    /// Obtient le nom de la matière
    /// </summary>
    public string GetSubjectName()
    {
        return subjectData != null ? subjectData.displayName : "Unknown";
    }

    /// <summary>
    /// Vérifie si le joueur est en train d'attendre de répondre
    /// </summary>
    public bool IsWaitingForAnswer()
    {
        return isWaitingForAnswer;
    }

    /// <summary>
    /// Convertit le nom du jour en index (0 = Lundi, 1 = Mardi, etc.)
    /// </summary>
    private int GetDayIndex(string day)
    {
        switch (day.ToLower())
        {
            case "monday":
            case "lundi":
                return 0;
            case "tuesday":
            case "mardi":
                return 1;
            case "wednesday":
            case "mercredi":
                return 2;
            case "thursday":
            case "jeudi":
                return 3;
            case "friday":
            case "vendredi":
                return 4;
            default:
                Debug.LogWarning($"[ExamManager] Jour inconnu: {day}, utilisation de l'index 0 (Lundi)");
                return 0;
        }
    }

    /// <summary>
    /// Retourne le chemin complet d'un Transform dans la hiérarchie (pour debug)
    /// </summary>
    private string GetFullPath(Transform t)
    {
        string path = t.name;
        Transform parent = t.parent;
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }
        return path;
    }

    /// <summary>
    /// Log conditionnel (seulement si showDebugLogs est activé)
    /// </summary>
    private void LogDebug(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[ExamManager] {message}");
        }
    }
}
