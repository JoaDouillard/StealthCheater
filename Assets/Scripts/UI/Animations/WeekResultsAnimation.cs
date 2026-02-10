using UnityEngine;
using LitMotion;
using LitMotion.Extensions;

/// <summary>
/// Animation pour l'écran de résultats de la semaine - Fade + Scale + Bounce (effet celebratoire)
/// </summary>
public class WeekResultsAnimation : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float animationDuration = 1f;
    [SerializeField] private Ease easeType = Ease.OutElastic;

    [Header("Scale Settings")]
    [SerializeField] private float initialScale = 0.3f;
    [SerializeField] private float overshootScale = 1.15f;

    [Header("Rotation Effect")]
    [Tooltip("Active une légère rotation pour effet celebratoire")]
    [SerializeField] private bool enableRotation = false;
    [SerializeField] private float rotationAmount = 5f;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private MotionHandle fadeMotion;
    private MotionHandle scaleMotion;
    private MotionHandle rotationMotion;

    private void Awake()
    {
        Debug.Log($"[WeekResultsAnimation] === Awake sur GameObject: {gameObject.name} ===");

        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        // Ajouter CanvasGroup si manquant
        if (canvasGroup == null)
        {
            Debug.Log("[WeekResultsAnimation] CanvasGroup manquant, ajout automatique...");
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // S'assurer que les boutons sont cliquables
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;

        if (rectTransform == null)
        {
            Debug.LogError("[WeekResultsAnimation] RectTransform manquant!");
        }
    }

    private void OnEnable()
    {
        // Lancer l'animation automatiquement quand le GameObject est activé
        Show();
    }

    /// <summary>
    /// Affiche les résultats de la semaine avec animation celebratoire
    /// </summary>
    public void Show()
    {
        if (canvasGroup == null || rectTransform == null) return;

        // Annuler animations précédentes
        if (fadeMotion.IsActive()) fadeMotion.Cancel();
        if (scaleMotion.IsActive()) scaleMotion.Cancel();
        if (rotationMotion.IsActive()) rotationMotion.Cancel();

        // État initial
        canvasGroup.alpha = 0f;
        rectTransform.localScale = Vector3.one * initialScale;

        if (enableRotation)
        {
            rectTransform.localRotation = Quaternion.Euler(0f, 0f, rotationAmount);
        }

        // Activer le GameObject
        gameObject.SetActive(true);

        // Animation fade in (alpha 0 → 1)
        fadeMotion = LMotion.Create(0f, 1f, animationDuration)
            .WithEase(Ease.OutQuad)
            .Bind(alpha => canvasGroup.alpha = alpha);

        // Animation scale avec bounce (small → overshoot → 1)
        // Utilise un sequence pour faire: initialScale → overshootScale → 1.0
        scaleMotion = LMotion.Create(Vector3.one * initialScale, Vector3.one, animationDuration)
            .WithEase(easeType)
            .BindToLocalScale(rectTransform);

        // Animation rotation retour à 0 (optionnel)
        if (enableRotation)
        {
            rotationMotion = LMotion.Create(rotationAmount, 0f, animationDuration)
                .WithEase(Ease.OutBack)
                .Bind(angle =>
                {
                    rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
                });
        }

        Debug.Log("[WeekResultsAnimation] Animation Show démarrée (celebratoire)");
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
        if (rotationMotion.IsActive()) rotationMotion.Cancel();

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
                rectTransform.localRotation = Quaternion.identity;
            })
            .BindToLocalScale(rectTransform);

        Debug.Log("[WeekResultsAnimation] Animation Hide démarrée");
    }

    private void OnDestroy()
    {
        // Annuler animations si le GameObject est détruit
        if (fadeMotion.IsActive()) fadeMotion.Cancel();
        if (scaleMotion.IsActive()) scaleMotion.Cancel();
        if (rotationMotion.IsActive()) rotationMotion.Cancel();
    }
}
