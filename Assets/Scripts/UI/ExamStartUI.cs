using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// Affiche le jour, l'examen et les questions au début, puis "Question X" à chaque nouvelle question.
/// </summary>
public class ExamStartUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI dayText;
    [SerializeField] private TextMeshProUGUI examNameText;
    [SerializeField] private TextMeshProUGUI questionsText;

    [Header("Settings")]
    [SerializeField] private float displayDuration = 2f;

    private Coroutine currentSequence;
    public bool IsSequenceRunning { get; private set; }

    private void Awake()
    {
        HideAllTexts();
    }

    public void Show(string dayName, string subjectName, int questionCount)
    {
        // Activer le panel AVANT de démarrer la coroutine
        if (UIManager.Instance != null)
            UIManager.Instance.ShowExamStart();

        if (currentSequence != null)
            StopCoroutine(currentSequence);

        currentSequence = StartCoroutine(PlayExamSequence(dayName, subjectName, questionCount));
    }

    public void ShowQuestionNumber(int questionNumber)
    {
        // IMPORTANT: Activer le panel AVANT de démarrer la coroutine
        if (UIManager.Instance != null)
            UIManager.Instance.ShowExamStart();

        if (currentSequence != null)
            StopCoroutine(currentSequence);

        currentSequence = StartCoroutine(PlayQuestionSequence(questionNumber));
    }

    private IEnumerator PlayExamSequence(string dayName, string subjectName, int questionCount)
    {
        IsSequenceRunning = true;

        // Étape 1 : JOUR
        HideAllTexts();
        if (dayText != null)
        {
            dayText.text = dayName.ToUpper();
            dayText.gameObject.SetActive(true);
        }
        yield return new WaitForSecondsRealtime(displayDuration);

        // Étape 2 : EXAM + QUESTIONS
        HideAllTexts();
        if (examNameText != null)
        {
            examNameText.text = $"{subjectName} Exam";
            examNameText.gameObject.SetActive(true);
        }
        if (questionsText != null)
        {
            questionsText.text = $"Answer {questionCount} questions";
            questionsText.gameObject.SetActive(true);
        }
        yield return new WaitForSecondsRealtime(displayDuration);

        EndSequence();
    }

    private IEnumerator PlayQuestionSequence(int questionNumber)
    {
        IsSequenceRunning = true;

        HideAllTexts();
        if (questionsText != null)
        {
            questionsText.text = $"Question {questionNumber}";
            questionsText.gameObject.SetActive(true);
        }
        yield return new WaitForSecondsRealtime(displayDuration);

        EndSequence();
    }

    private void EndSequence()
    {
        HideAllTexts();
        IsSequenceRunning = false;
        currentSequence = null;

        if (UIManager.Instance != null)
            UIManager.Instance.ShowHUD();
    }

    private void HideAllTexts()
    {
        if (dayText != null) dayText.gameObject.SetActive(false);
        if (examNameText != null) examNameText.gameObject.SetActive(false);
        if (questionsText != null) questionsText.gameObject.SetActive(false);
    }
}
