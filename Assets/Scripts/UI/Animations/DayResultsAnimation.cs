using UnityEngine;
using LitMotion;
using LitMotion.Extensions;

/// <summary>
/// Animation pour l'écran de résultats du jour - Fade + Scale + Slide up
/// </summary>
public class DayResultsAnimation : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float animationDuration = 0.6f;
    [SerializeField] private Ease easeType = Ease.OutBack;

    [Header("Slide Settings")]
    [SerializeField] private bool enableSlide = true;
    [SerializeField] private float slideDistance = 50f;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private MotionHandle fadeMotion;
    private MotionHandle scaleMotion;
    private MotionHandle slideMotion;
    private Vector2 originalPosition;

    private void Awake()
    {
        Debug.Log($"[DayResultsAnimation] === Awake sur GameObject: {gameObject.name} ===");

        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        // Ajouter CanvasGroup si manquant
        if (canvasGroup == null)
        {
            Debug.Log("[DayResultsAnimation] CanvasGroup manquant, ajout automatique...");
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // S'assurer que les boutons sont cliquables
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;

        if (rectTransform == null)
        {
            Debug.LogError("[DayResultsAnimation] RectTransform manquant!");
        }

        // Sauvegarder position originale
        originalPosition = rectTransform.anchoredPosition;
    }

    private void OnEnable()
    {
        // Lancer l'animation automatiquement quand le GameObject est activé
        Show();
    }

    /// <summary>
    /// Affiche les résultats avec animation
    /// </summary>
    public void Show()
    {
        if (canvasGroup == null || rectTransform == null) return;

        // Annuler animations précédentes
        if (fadeMotion.IsActive()) fadeMotion.Cancel();
        if (scaleMotion.IsActive()) scaleMotion.Cancel();
        if (slideMotion.IsActive()) slideMotion.Cancel();

        // État initial
        canvasGroup.alpha = 0f;
        rectTransform.localScale = Vector3.one * 0.8f;

        if (enableSlide)
        {
            // Position initiale: en bas
            rectTransform.anchoredPosition = originalPosition + Vector2.down * slideDistance;
        }

        // Activer le GameObject
        gameObject.SetActive(true);

        // Animation fade in (alpha 0 → 1)
        fadeMotion = LMotion.Create(0f, 1f, animationDuration)
            .WithEase(Ease.OutQuad)
            .Bind(alpha => canvasGroup.alpha = alpha);

        // Animation scale (0.8 → 1)
        scaleMotion = LMotion.Create(Vector3.one * 0.8f, Vector3.one, animationDuration)
            .WithEase(easeType)
            .BindToLocalScale(rectTransform);

        // Animation slide up (optionnel)
        if (enableSlide)
        {
            slideMotion = LMotion.Create(originalPosition + Vector2.down * slideDistance, originalPosition, animationDuration)
                .WithEase(easeType)
                .BindToAnchoredPosition(rectTransform);
        }

        Debug.Log("[DayResultsAnimation] Animation Show démarrée");
    }

    /// <summary>
    /// Cache les résultats avec animation
    /// </summary>
    public void Hide()
    {
        if (canvasGroup == null || rectTransform == null) return;

        // Annuler animations précédentes
        if (fadeMotion.IsActive()) fadeMotion.Cancel();
        if (scaleMotion.IsActive()) scaleMotion.Cancel();
        if (slideMotion.IsActive()) slideMotion.Cancel();

        float duration = animationDuration * 0.6f;

        // Animation fade out (alpha 1 → 0)
        fadeMotion = LMotion.Create(canvasGroup.alpha, 0f, duration)
            .WithEase(Ease.InQuad)
            .Bind(alpha => canvasGroup.alpha = alpha);

        // Animation scale down (1 → 0.8)
        scaleMotion = LMotion.Create(rectTransform.localScale, Vector3.one * 0.8f, duration)
            .WithEase(Ease.InBack)
            .WithOnComplete(() =>
            {
                // Désactiver après l'animation
                gameObject.SetActive(false);
                // Reset
                rectTransform.localScale = Vector3.one;
                rectTransform.anchoredPosition = originalPosition;
            })
            .BindToLocalScale(rectTransform);

        // Animation slide down (optionnel)
        if (enableSlide)
        {
            slideMotion = LMotion.Create(rectTransform.anchoredPosition, originalPosition + Vector2.down * slideDistance, duration)
                .WithEase(Ease.InCubic)
                .BindToAnchoredPosition(rectTransform);
        }

        Debug.Log("[DayResultsAnimation] Animation Hide démarrée");
    }

    private void OnDestroy()
    {
        // Annuler animations si le GameObject est détruit
        if (fadeMotion.IsActive()) fadeMotion.Cancel();
        if (scaleMotion.IsActive()) scaleMotion.Cancel();
        if (slideMotion.IsActive()) slideMotion.Cancel();
    }
}
