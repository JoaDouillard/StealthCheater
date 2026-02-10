using UnityEngine;
using LitMotion;
using LitMotion.Extensions;

/// <summary>
/// Animation dramatique pour l'écran Game Over - Fade + Scale + Shake
/// </summary>
public class GameOverAnimation : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float animationDuration = 0.8f;
    [SerializeField] private Ease easeType = Ease.OutElastic;

    [Header("Shake Effect")]
    [SerializeField] private bool enableShake = true;
    [SerializeField] private float shakeIntensity = 10f;
    [SerializeField] private float shakeDuration = 0.5f;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private MotionHandle fadeMotion;
    private MotionHandle scaleMotion;
    private MotionHandle shakeMotion;
    private Vector2 originalPosition;

    private void Awake()
    {
        Debug.Log($"[GameOverAnimation] === Awake sur GameObject: {gameObject.name} ===");

        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        // Ajouter CanvasGroup si manquant
        if (canvasGroup == null)
        {
            Debug.Log("[GameOverAnimation] CanvasGroup manquant, ajout automatique...");
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // S'assurer que les boutons sont cliquables
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;

        if (rectTransform == null)
        {
            Debug.LogError("[GameOverAnimation] RectTransform manquant!");
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
    /// Affiche le Game Over avec animation dramatique
    /// </summary>
    public void Show()
    {
        if (canvasGroup == null || rectTransform == null) return;

        // Annuler animations précédentes
        if (fadeMotion.IsActive()) fadeMotion.Cancel();
        if (scaleMotion.IsActive()) scaleMotion.Cancel();
        if (shakeMotion.IsActive()) shakeMotion.Cancel();

        // État initial
        canvasGroup.alpha = 0f;
        rectTransform.localScale = Vector3.one * 0.5f;

        // Activer le GameObject
        gameObject.SetActive(true);

        // Animation fade in (alpha 0 → 1)
        fadeMotion = LMotion.Create(0f, 1f, animationDuration)
            .WithEase(Ease.OutQuad)
            .Bind(alpha => canvasGroup.alpha = alpha);

        // Animation scale (0.5 → 1.2 → 1) avec bounce
        scaleMotion = LMotion.Create(Vector3.one * 0.5f, Vector3.one, animationDuration)
            .WithEase(easeType)
            .BindToLocalScale(rectTransform);

        // Animation shake (optionnel) pour effet dramatique
        if (enableShake)
        {
            shakeMotion = LMotion.Create(0f, 1f, shakeDuration)
                .WithDelay(animationDuration * 0.5f) // Start shake halfway through
                .WithEase(Ease.OutQuad)
                .WithOnComplete(() => rectTransform.anchoredPosition = originalPosition)
                .Bind(t =>
                {
                    // Shake effect diminuant avec le temps
                    float shakeAmount = shakeIntensity * (1f - t);
                    Vector2 shake = new Vector2(
                        Random.Range(-shakeAmount, shakeAmount),
                        Random.Range(-shakeAmount, shakeAmount)
                    );
                    rectTransform.anchoredPosition = originalPosition + shake;
                });
        }

        Debug.Log("[GameOverAnimation] Animation Show démarrée");
    }

    /// <summary>
    /// Cache le Game Over (si nécessaire)
    /// </summary>
    public void Hide()
    {
        if (canvasGroup == null || rectTransform == null) return;

        // Annuler animations précédentes
        if (fadeMotion.IsActive()) fadeMotion.Cancel();
        if (scaleMotion.IsActive()) scaleMotion.Cancel();
        if (shakeMotion.IsActive()) shakeMotion.Cancel();

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

        Debug.Log("[GameOverAnimation] Animation Hide démarrée");
    }

    private void OnDestroy()
    {
        // Annuler animations si le GameObject est détruit
        if (fadeMotion.IsActive()) fadeMotion.Cancel();
        if (scaleMotion.IsActive()) scaleMotion.Cancel();
        if (shakeMotion.IsActive()) shakeMotion.Cancel();
    }
}
