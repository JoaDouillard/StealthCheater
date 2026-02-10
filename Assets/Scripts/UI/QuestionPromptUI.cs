//using UnityEngine;
//using TMPro;
//using System.Collections;

///// <summary>
///// UI affichée au début de chaque question pendant 10 secondes
///// Affiche le numéro de question et une instruction
///// </summary>
//public class QuestionPromptUI : MonoBehaviour
//{
//    [Header("UI References")]
//    [Tooltip("Panel à afficher/cacher")]
//    [SerializeField] private GameObject questionPromptPanel;

//    [Tooltip("Texte affichant le prompt de la question")]
//    [SerializeField] private TextMeshProUGUI questionPromptText;

//    [Header("Settings")]
//    [Tooltip("Durée d'affichage en secondes")]
//    [SerializeField] private float displayDuration = 2f; // 2 secondes par défaut

//    private Coroutine displayCoroutine;
//    private bool wasGameplayActive = true;

//    private void Awake()
//    {
//        // NE PAS désactiver le panel ici !
//        // UIManager gère l'état actif/inactif des panels
//    }

//    /// <summary>
//    /// Affiche le panel avec le numéro de la question
//    /// Met le jeu EN PAUSE pendant l'affichage
//    /// </summary>
//    /// <param name="questionNumber">Numéro de la question (1-5)</param>
//    public void Show(int questionNumber)
//    {
//        if (questionPromptPanel == null)
//        {
//            Debug.LogWarning("[QuestionPromptUI] QuestionPromptPanel not assigned!");
//            return;
//        }

//        // Mettre à jour le texte
//        if (questionPromptText != null)
//        {
//            questionPromptText.text = $"Question {questionNumber}";
//        }

//        // UIManager gère l'affichage du panel
//        if (UIManager.Instance != null)
//        {
//            UIManager.Instance.ShowQuestionPrompt();
//        }
//        else
//        {
//            Debug.LogError("[QuestionPromptUI] UIManager introuvable !");
//            return;
//        }

//        // PAUSE le jeu pendant l'affichage
//        wasGameplayActive = (Time.timeScale == 1f);
//        Time.timeScale = 0f;

//        Debug.Log($"[QuestionPromptUI] Question {questionNumber} affichée - Jeu EN PAUSE");

//        // Démarrer la coroutine pour cacher automatiquement
//        if (displayCoroutine != null)
//        {
//            StopCoroutine(displayCoroutine);
//        }
//        displayCoroutine = StartCoroutine(HideAfterDelay());
//    }

//    /// <summary>
//    /// Cache le panel immédiatement et reprend le jeu
//    /// </summary>
//    public void Hide()
//    {
//        // UIManager gère la désactivation du panel
//        if (UIManager.Instance != null)
//        {
//            UIManager.Instance.HideQuestionPrompt();
//        }

//        if (displayCoroutine != null)
//        {
//            StopCoroutine(displayCoroutine);
//            displayCoroutine = null;
//        }

//        // REPRENDRE le jeu si il était actif avant
//        if (wasGameplayActive)
//        {
//            Time.timeScale = 1f;
//        }

//        Debug.Log("[QuestionPromptUI] Question prompt caché - Jeu repris");
//    }

//    /// <summary>
//    /// Coroutine qui cache le panel après un délai (utilise WaitForSecondsRealtime car le jeu est en pause)
//    /// </summary>
//    private IEnumerator HideAfterDelay()
//    {
//        yield return new WaitForSecondsRealtime(displayDuration);
//        Hide();
//    }

//    /// <summary>
//    /// Change la durée d'affichage
//    /// </summary>
//    public void SetDisplayDuration(float duration)
//    {
//        displayDuration = Mathf.Max(1f, duration);
//    }
//}
