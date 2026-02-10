using UnityEngine;
using LitMotion;
using LitMotion.Extensions;

/// <summary>
/// Animation du menu settings - Fade in + Scale
/// </summary>
public class SettingsMenuAnimation : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float animationDuration = 0.4f;
    [SerializeField] private Ease easeType = Ease.OutBack;

    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private MotionHandle fadeMotion;
    private MotionHandle scaleMotion;

    private void Awake()
    {
        Debug.Log($"[SettingsMenuAnimation] === Awake sur GameObject: {gameObject.name} ===");

        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        // Ajouter CanvasGroup si manquant
        if (canvasGroup == null)
        {
            Debug.Log("[SettingsMenuAnimation] CanvasGroup manquant, ajout automatique...");
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        else
        {
            Debug.Log("[SettingsMenuAnimation] ✅ CanvasGroup déjà présent");
        }

        // S'assurer que les boutons/sliders sont cliquables
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;
        Debug.Log($"[SettingsMenuAnimation] ✅ CanvasGroup configuré: blocksRaycasts={canvasGroup.blocksRaycasts}, interactable={canvasGroup.interactable}");

        // Vérifier si un parent a un CanvasGroup qui pourrait bloquer
        CanvasGroup parentCanvasGroup = transform.parent?.GetComponent<CanvasGroup>();
        if (parentCanvasGroup != null)
        {
            Debug.Log($"[SettingsMenuAnimation] ⚠️ Parent CanvasGroup détecté: blocksRaycasts={parentCanvasGroup.blocksRaycasts}, interactable={parentCanvasGroup.interactable}");
            if (!parentCanvasGroup.blocksRaycasts || !parentCanvasGroup.interactable)
            {
                Debug.LogWarning("[SettingsMenuAnimation] ⚠️ Le parent CanvasGroup pourrait bloquer les interactions!");
            }
        }

        if (rectTransform == null)
        {
            Debug.LogError("[SettingsMenuAnimation] RectTransform manquant!");
        }
    }

    /// <summary>
    /// Affiche le menu avec fade in + scale
    /// </summary>
    public void Show()
    {
        if (canvasGroup == null || rectTransform == null) return;

        // Annuler animations précédentes
        if (fadeMotion.IsActive()) fadeMotion.Cancel();
        if (scaleMotion.IsActive()) scaleMotion.Cancel();

        // État initial
        canvasGroup.alpha = 0f;
        rectTransform.localScale = Vector3.one * 0.8f;

        // Activer le GameObject
        gameObject.SetActive(true);

        // Animation fade in (alpha 0 → 1)
        fadeMotion = LMotion.Create(0f, 1f, animationDuration)
            .WithEase(Ease.OutQuad)
            .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
            .Bind(alpha => canvasGroup.alpha = alpha);

        // Animation scale (0.8 → 1)
        scaleMotion = LMotion.Create(Vector3.one * 0.8f, Vector3.one, animationDuration)
            .WithEase(easeType)
            .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
            .BindToLocalScale(rectTransform);

        Debug.Log("[SettingsMenuAnimation] Animation Show démarrée");
    }

    /// <summary>
    /// Cache le menu avec fade out + scale down
    /// </summary>
    public void Hide()
    {
        if (canvasGroup == null || rectTransform == null) return;

        // Annuler animations précédentes
        if (fadeMotion.IsActive()) fadeMotion.Cancel();
        if (scaleMotion.IsActive()) scaleMotion.Cancel();

        float duration = animationDuration * 0.6f;

        // Animation fade out (alpha 1 → 0)
        fadeMotion = LMotion.Create(canvasGroup.alpha, 0f, duration)
            .WithEase(Ease.InQuad)
            .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
            .Bind(alpha => canvasGroup.alpha = alpha);

        // Animation scale down (1 → 0.8)
        scaleMotion = LMotion.Create(rectTransform.localScale, Vector3.one * 0.8f, duration)
            .WithEase(Ease.InBack)
            .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
            .WithOnComplete(() =>
            {
                // Désactiver après l'animation
                gameObject.SetActive(false);
                // Reset scale
                rectTransform.localScale = Vector3.one;
            })
            .BindToLocalScale(rectTransform);

        Debug.Log("[SettingsMenuAnimation] Animation Hide démarrée");
    }

    private void OnDestroy()
    {
        // Annuler animations si le GameObject est détruit
        if (fadeMotion.IsActive()) fadeMotion.Cancel();
        if (scaleMotion.IsActive()) scaleMotion.Cancel();
    }
}
