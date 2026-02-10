using UnityEngine;
using LitMotion;
using LitMotion.Extensions;

/// <summary>
/// Animation pour l'anti-sèche - Slide in depuis la droite + Fade (effet sneaky/stealth)
/// </summary>
public class CheatSheetAnimation : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float animationDuration = 0.5f;
    [SerializeField] private Ease easeType = Ease.OutCubic;

    [Header("Slide Direction")]
    [Tooltip("Direction du slide: Right = depuis la droite, Left = depuis la gauche")]
    [SerializeField] private SlideDirection slideDirection = SlideDirection.Right;

    [Header("Slide Distance")]
    [Tooltip("Distance de slide en pixels (0 = largeur de l'écran)")]
    [SerializeField] private float slideDistance = 0f; // 0 = auto (largeur écran)

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private MotionHandle fadeMotion;
    private MotionHandle slideMotion;
    private Vector2 originalPosition;
    private Vector2 hiddenPosition;

    private void Awake()
    {
        Debug.Log($"[CheatSheetAnimation] === Awake sur GameObject: {gameObject.name} ===");

        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        // Ajouter CanvasGroup si manquant
        if (canvasGroup == null)
        {
            Debug.Log("[CheatSheetAnimation] CanvasGroup manquant, ajout automatique...");
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // S'assurer que les boutons sont cliquables
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;

        if (rectTransform == null)
        {
            Debug.LogError("[CheatSheetAnimation] RectTransform manquant!");
            return;
        }

        // Sauvegarder position originale
        originalPosition = rectTransform.anchoredPosition;

        // Calculer position cachée
        float distance = slideDistance > 0 ? slideDistance : Screen.width;

        switch (slideDirection)
        {
            case SlideDirection.Right:
                hiddenPosition = originalPosition + Vector2.right * distance;
                break;
            case SlideDirection.Left:
                hiddenPosition = originalPosition + Vector2.left * distance;
                break;
            case SlideDirection.Up:
                hiddenPosition = originalPosition + Vector2.up * distance;
                break;
            case SlideDirection.Down:
                hiddenPosition = originalPosition + Vector2.down * distance;
                break;
        }

        Debug.Log($"[CheatSheetAnimation] Positions - Original: {originalPosition}, Hidden: {hiddenPosition}");
    }

    private void OnEnable()
    {
        // Lancer l'animation automatiquement quand le GameObject est activé
        Show();
    }

    /// <summary>
    /// Affiche l'anti-sèche avec slide in + fade
    /// </summary>
    public void Show()
    {
        if (canvasGroup == null || rectTransform == null) return;

        // Annuler animations précédentes
        if (fadeMotion.IsActive()) fadeMotion.Cancel();
        if (slideMotion.IsActive()) slideMotion.Cancel();

        // État initial
        canvasGroup.alpha = 0f;
        rectTransform.anchoredPosition = hiddenPosition;

        // Activer le GameObject
        gameObject.SetActive(true);

        // Animation fade in (alpha 0 → 1)
        fadeMotion = LMotion.Create(0f, 1f, animationDuration)
            .WithEase(Ease.OutQuad)
            .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale) // Continue même si pause
            .Bind(alpha => canvasGroup.alpha = alpha);

        // Animation slide in (hidden → original position)
        slideMotion = LMotion.Create(hiddenPosition, originalPosition, animationDuration)
            .WithEase(easeType)
            .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
            .BindToAnchoredPosition(rectTransform);

        Debug.Log("[CheatSheetAnimation] Animation Show démarrée (slide in)");
    }

    /// <summary>
    /// Cache l'anti-sèche avec slide out + fade
    /// </summary>
    public void Hide()
    {
        if (canvasGroup == null || rectTransform == null) return;

        // Annuler animations précédentes
        if (fadeMotion.IsActive()) fadeMotion.Cancel();
        if (slideMotion.IsActive()) slideMotion.Cancel();

        float duration = animationDuration * 0.8f;

        // Animation fade out (alpha 1 → 0)
        fadeMotion = LMotion.Create(canvasGroup.alpha, 0f, duration)
            .WithEase(Ease.InQuad)
            .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
            .Bind(alpha => canvasGroup.alpha = alpha);

        // Animation slide out (original → hidden position)
        slideMotion = LMotion.Create(rectTransform.anchoredPosition, hiddenPosition, duration)
            .WithEase(Ease.InCubic)
            .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
            .WithOnComplete(() =>
            {
                // Désactiver après l'animation
                gameObject.SetActive(false);
                // Reset position
                rectTransform.anchoredPosition = originalPosition;
            })
            .BindToAnchoredPosition(rectTransform);

        Debug.Log("[CheatSheetAnimation] Animation Hide démarrée (slide out)");
    }

    private void OnDestroy()
    {
        // Annuler animations si le GameObject est détruit
        if (fadeMotion.IsActive()) fadeMotion.Cancel();
        if (slideMotion.IsActive()) slideMotion.Cancel();
    }
}

/// <summary>
/// Direction du slide pour l'anti-sèche
/// </summary>
public enum SlideDirection
{
    Right,  // Depuis la droite (défaut, stealth style)
    Left,   // Depuis la gauche
    Up,     // Depuis le haut
    Down    // Depuis le bas
}
