using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// HUD displaying exam info, timer, copy/write progress, detection status.
/// </summary>
public class GameHUD : MonoBehaviour
{
    public static GameHUD Instance { get; private set; }

    [Header("Progress Bar (Copy & Write)")]
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private Image progressBar;

    [Header("Detection")]
    [SerializeField] private TextMeshProUGUI detectionStatusText;
    [SerializeField] private Image detectionIndicator;

    [Header("Timer")]
    [SerializeField] private TextMeshProUGUI examTimerText;

    [Header("Exam Info")]
    [SerializeField] private TextMeshProUGUI examNameText;
    [SerializeField] private TextMeshProUGUI questionCounterText;

    [Header("Interaction")]
    [SerializeField] private TextMeshProUGUI interactionPromptText;

    [Header("Colors")]
    [SerializeField] private Color safeColor = Color.green;
    [SerializeField] private Color visibleColor = Color.yellow;
    [SerializeField] private Color suspectColor = new Color(1f, 0.5f, 0f); // Orange
    [SerializeField] private Color dangerColor = Color.red;

    [Header("Timer Colors (by percentage)")]
    [SerializeField] private Color timerNormalColor = Color.white;
    [SerializeField] private Color timerWarningColor = new Color(1f, 0.5f, 0f); // Orange
    [SerializeField] private Color timerDangerColor = Color.red;

    // Pour calculer le pourcentage du timer
    private float maxExamTime = 0f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        HideProgress();
        SetDetectionStatus(DetectionStatus.Safe);
        HideInteractionPrompt();
        HideExamInfo();
    }

    #region Progress (Copy & Write)

    public void ShowCopyProgress(float currentTime, float maxTime, string studentName)
    {
        ShowProgress($"Copying from {studentName}: {currentTime:F1}s / {maxTime:F1}s", currentTime, maxTime);
    }

    public void HideCopyProgress() => HideProgress();

    public void ShowWriteProgress(float currentTime, float maxTime)
    {
        ShowProgress($"Writing: {currentTime:F1}s / {maxTime:F1}s", currentTime, maxTime);
    }

    public void HideWriteProgress() => HideProgress();

    private void ShowProgress(string text, float currentTime, float maxTime)
    {
        if (progressText != null)
        {
            progressText.gameObject.SetActive(true);
            progressText.text = text;
        }

        if (progressBar != null)
        {
            progressBar.gameObject.SetActive(true);
            progressBar.fillAmount = Mathf.Clamp01(currentTime / maxTime);
        }
    }

    private void HideProgress()
    {
        if (progressText != null) progressText.gameObject.SetActive(false);
        if (progressBar != null)
        {
            progressBar.gameObject.SetActive(false);
            progressBar.fillAmount = 0f;
        }
    }

    #endregion

    #region Detection Status

    public void SetDetectionStatus(DetectionStatus status)
    {
        if (detectionStatusText != null)
        {
            switch (status)
            {
                case DetectionStatus.Safe:
                    detectionStatusText.text = "SAFE";
                    detectionStatusText.color = safeColor;
                    break;
                case DetectionStatus.Visible:
                    detectionStatusText.text = "VISIBLE";
                    detectionStatusText.color = visibleColor;
                    break;
                case DetectionStatus.Suspect:
                    detectionStatusText.text = "SUSPECT";
                    detectionStatusText.color = suspectColor;
                    break;
                case DetectionStatus.Danger:
                    detectionStatusText.text = "DANGER!";
                    detectionStatusText.color = dangerColor;
                    break;
            }
        }

        if (detectionIndicator != null)
        {
            switch (status)
            {
                case DetectionStatus.Safe: detectionIndicator.color = safeColor; break;
                case DetectionStatus.Visible: detectionIndicator.color = visibleColor; break;
                case DetectionStatus.Suspect: detectionIndicator.color = suspectColor; break;
                case DetectionStatus.Danger: detectionIndicator.color = dangerColor; break;
            }
        }
    }

    #endregion

    #region Exam Timer

    /// <summary>
    /// Sets the max time for percentage calculation
    /// </summary>
    public void SetMaxExamTime(float maxTime)
    {
        maxExamTime = maxTime;
    }

    /// <summary>
    /// Shows exam timer with color based on percentage remaining
    /// </summary>
    public void ShowExamTimer(float remainingTime)
    {
        if (examTimerText == null) return;

        examTimerText.gameObject.SetActive(true);

        int minutes = Mathf.FloorToInt(remainingTime / 60f);
        int seconds = Mathf.FloorToInt(remainingTime % 60f);
        examTimerText.text = $"Time: {minutes:00}:{seconds:00}";

        // Color by percentage: >50% white, 20-50% orange, <20% red
        if (maxExamTime > 0f)
        {
            float percentRemaining = remainingTime / maxExamTime;

            if (percentRemaining > 0.5f) // More than 50% remaining = white
            {
                examTimerText.color = timerNormalColor;
            }
            else if (percentRemaining > 0.2f) // 20-50% remaining = orange
            {
                examTimerText.color = timerWarningColor;
            }
            else // Less than 20% = red
            {
                examTimerText.color = timerDangerColor;
            }
        }
        else
        {
            examTimerText.color = timerNormalColor;
        }
    }

    public void HideExamTimer()
    {
        if (examTimerText != null) examTimerText.gameObject.SetActive(false);
    }

    #endregion

    #region Exam Info

    public void ShowExamInfo(string subjectName, int currentQuestion, int totalQuestions)
    {
        if (examNameText != null)
        {
            examNameText.gameObject.SetActive(true);
            examNameText.text = $"{subjectName} Exam";
        }

        if (questionCounterText != null)
        {
            questionCounterText.gameObject.SetActive(true);
            questionCounterText.text = $"Question {currentQuestion} / {totalQuestions}";
        }
    }

    public void UpdateQuestionCounter(int currentQuestion, int totalQuestions)
    {
        if (questionCounterText != null)
        {
            questionCounterText.text = $"Question {currentQuestion} / {totalQuestions}";
        }
    }

    public void HideExamInfo()
    {
        if (examNameText != null) examNameText.gameObject.SetActive(false);
        if (questionCounterText != null) questionCounterText.gameObject.SetActive(false);
    }

    #endregion

    #region Interaction Prompt

    public void ShowInteractionPrompt(string message)
    {
        if (interactionPromptText != null)
        {
            interactionPromptText.gameObject.SetActive(true);
            interactionPromptText.text = message;
        }
    }

    public void HideInteractionPrompt()
    {
        if (interactionPromptText != null) interactionPromptText.gameObject.SetActive(false);
    }

    #endregion
}

public enum DetectionStatus
{
    Safe,
    Visible,
    Suspect,
    Danger
}
