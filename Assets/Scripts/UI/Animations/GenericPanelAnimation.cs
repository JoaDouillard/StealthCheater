using UnityEngine;
using LitMotion;
using LitMotion.Extensions;

/// <summary>
/// Animation générique réutilisable pour n'importe quel panel UI
/// Combine Fade + Scale + Slide (tous optionnels)
/// </summary>
public class GenericPanelAnimation : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private float animationDuration = 0.5f;
    [SerializeField] private Ease easeType = Ease.OutCubic;

    [Header("Effects")]
    [Tooltip("Active l'effet fade (alpha)")]
    [SerializeField] private bool enableFade = true;

    [Tooltip("Active l'effet scale")]
    [SerializeField] private bool enableScale = true;

    [Tooltip("Active l'effet slide")]
    [SerializeField] private bool enableSlide = false;

    [Header("Scale Settings")]
    [SerializeField] private float initialScale = 0.8f;
    [SerializeField] private float targetScale = 1f;

    [Header("Slide Settings")]
    [SerializeField] private SlideDirection slideDirection = SlideDirection.Down;
    [SerializeField] private float slideDistance = 50f;

    [Header("Auto Animation")]
    [Tooltip("Lance automatiquement Show() quand le GameObject est activé")]
    [SerializeField] private bool autoAnimateOnEnable = true;

    [Header("Time Scale")]
    [Tooltip("Continue l'animation même si le jeu est en pause (Time.timeScale = 0)")]
    [SerializeField] private bool ignoreTimeScale = false;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private MotionHandle fadeMotion;
    private MotionHandle scaleMotion;
    private MotionHandle slideMotion;
    private Vector2 originalPosition;
    private Vector2 hiddenPosition;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        // Ajouter CanvasGroup si nécessaire (pour fade)
        if (enableFade && canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // S'assurer que les interactions fonctionnent
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
        }

        if (rectTransform == null)
        {
            Debug.LogError($"[GenericPanelAnimation] RectTransform manquant sur {gameObject.name}!");
            return;
        }

        // Calculer positions pour le slide
        if (enableSlide)
        {
            originalPosition = rectTransform.anchoredPosition;

            switch (slideDirection)
            {
                case SlideDirection.Right:
                    hiddenPosition = originalPosition + Vector2.right * slideDistance;
                    break;
                case SlideDirection.Left:
                    hiddenPosition = originalPosition + Vector2.left * slideDistance;
                    break;
                case SlideDirection.Up:
                    hiddenPosition = originalPosition + Vector2.up * slideDistance;
                    break;
                case SlideDirection.Down:
                    hiddenPosition = originalPosition + Vector2.down * slideDistance;
                    break;
            }
        }
    }

    private void OnEnable()
    {
        if (autoAnimateOnEnable)
        {
            Show();
        }
    }

    /// <summary>
    /// Affiche le panel avec les animations configurées
    /// </summary>
    public void Show()
    {
        if (rectTransform == null) return;

        // Annuler animations précédentes
        CancelAllMotions();

        // Activer le GameObject
        gameObject.SetActive(true);

        // Animation fade in
        if (enableFade && canvasGroup != null)
        {
            canvasGroup.alpha = 0f;

            var fadeBuilder = LMotion.Create(0f, 1f, animationDuration)
                .WithEase(Ease.OutQuad);

            if (ignoreTimeScale)
            {
                fadeBuilder = fadeBuilder.WithScheduler(MotionScheduler.UpdateIgnoreTimeScale);
            }

            fadeMotion = fadeBuilder.Bind(alpha => canvasGroup.alpha = alpha);
        }

        // Animation scale
        if (enableScale)
        {
            rectTransform.localScale = Vector3.one * initialScale;

            var scaleBuilder = LMotion.Create(Vector3.one * initialScale, Vector3.one * targetScale, animationDuration)
                .WithEase(easeType);

            if (ignoreTimeScale)
            {
                scaleBuilder = scaleBuilder.WithScheduler(MotionScheduler.UpdateIgnoreTimeScale);
            }

            scaleMotion = scaleBuilder.BindToLocalScale(rectTransform);
        }

        // Animation slide
        if (enableSlide)
        {
            rectTransform.anchoredPosition = hiddenPosition;

            var slideBuilder = LMotion.Create(hiddenPosition, originalPosition, animationDuration)
                .WithEase(easeType);

            if (ignoreTimeScale)
            {
                slideBuilder = slideBuilder.WithScheduler(MotionScheduler.UpdateIgnoreTimeScale);
            }

            slideMotion = slideBuilder.BindToAnchoredPosition(rectTransform);
        }

        Debug.Log($"[GenericPanelAnimation] Show animation started on {gameObject.name}");
    }

    /// <summary>
    /// Cache le panel avec les animations configurées
    /// </summary>
    public void Hide()
    {
        if (rectTransform == null) return;

        // Annuler animations précédentes
        CancelAllMotions();

        float duration = animationDuration * 0.7f;

        // Animation fade out
        if (enableFade && canvasGroup != null)
        {
            var fadeBuilder = LMotion.Create(canvasGroup.alpha, 0f, duration)
                .WithEase(Ease.InQuad);

            if (ignoreTimeScale)
            {
                fadeBuilder = fadeBuilder.WithScheduler(MotionScheduler.UpdateIgnoreTimeScale);
            }

            fadeMotion = fadeBuilder.Bind(alpha => canvasGroup.alpha = alpha);
        }

        // Animation scale down
        if (enableScale)
        {
            var scaleBuilder = LMotion.Create(rectTransform.localScale, Vector3.one * initialScale, duration)
                .WithEase(Ease.InBack)
                .WithOnComplete(() =>
                {
                    // Désactiver et reset après l'animation
                    gameObject.SetActive(false);
                    ResetState();
                });

            if (ignoreTimeScale)
            {
                scaleBuilder = scaleBuilder.WithScheduler(MotionScheduler.UpdateIgnoreTimeScale);
            }

            scaleMotion = scaleBuilder.BindToLocalScale(rectTransform);
        }
        else
        {
            // Si pas de scale animation, désactiver après fade
            if (enableFade)
            {
                // Le fade va gérer la désactivation
                var fadeBuilder = LMotion.Create(canvasGroup.alpha, 0f, duration)
                    .WithEase(Ease.InQuad)
                    .WithOnComplete(() =>
                    {
                        gameObject.SetActive(false);
                        ResetState();
                    });

                if (ignoreTimeScale)
                {
                    fadeBuilder = fadeBuilder.WithScheduler(MotionScheduler.UpdateIgnoreTimeScale);
                }

                fadeMotion = fadeBuilder.Bind(alpha => canvasGroup.alpha = alpha);
            }
        }

        // Animation slide out
        if (enableSlide)
        {
            var slideBuilder = LMotion.Create(rectTransform.anchoredPosition, hiddenPosition, duration)
                .WithEase(Ease.InCubic);

            if (ignoreTimeScale)
            {
                slideBuilder = slideBuilder.WithScheduler(MotionScheduler.UpdateIgnoreTimeScale);
            }

            slideMotion = slideBuilder.BindToAnchoredPosition(rectTransform);
        }

        Debug.Log($"[GenericPanelAnimation] Hide animation started on {gameObject.name}");
    }

    /// <summary>
    /// Reset l'état du panel
    /// </summary>
    private void ResetState()
    {
        if (rectTransform == null) return;

        rectTransform.localScale = Vector3.one * targetScale;

        if (enableSlide)
        {
            rectTransform.anchoredPosition = originalPosition;
        }

        if (canvasGroup != null && enableFade)
        {
            canvasGroup.alpha = 1f;
        }
    }

    /// <summary>
    /// Annule toutes les animations en cours
    /// </summary>
    private void CancelAllMotions()
    {
        if (fadeMotion.IsActive()) fadeMotion.Cancel();
        if (scaleMotion.IsActive()) scaleMotion.Cancel();
        if (slideMotion.IsActive()) slideMotion.Cancel();
    }

    private void OnDestroy()
    {
        CancelAllMotions();
    }
}
