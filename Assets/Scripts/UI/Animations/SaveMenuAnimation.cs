using UnityEngine;
using LitMotion;
using LitMotion.Extensions;

/// <summary>
/// Animation du menu de sauvegarde - Slide + Fade + Scale pour le panel principal et le popup
/// </summary>
public class SaveMenuAnimation : MonoBehaviour
{
    [Header("Main Panel Settings")]
    [SerializeField] private float panelAnimDuration = 0.4f;
    [SerializeField] private Ease panelEaseType = Ease.OutBack;
    [SerializeField] private float panelInitialScale = 0.85f;

    [Header("Popup Settings")]
    [SerializeField] private float popupAnimDuration = 0.3f;
    [SerializeField] private Ease popupEaseType = Ease.OutBack;
    [SerializeField] private float popupInitialScale = 0.7f;

    [Header("References")]
    [Tooltip("Le popup NameInput (sera trouvé automatiquement si non assigné)")]
    [SerializeField] private GameObject nameInputPopup;

    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private CanvasGroup popupCanvasGroup;
    private RectTransform popupRectTransform;

    private MotionHandle fadeMotion;
    private MotionHandle scaleMotion;
    private MotionHandle popupFadeMotion;
    private MotionHandle popupScaleMotion;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;

        // Trouver le popup si non assigné
        if (nameInputPopup == null)
        {
            Transform popup = transform.Find("NameInputPopup");
            if (popup == null) popup = transform.Find("PopupNameInput");
            if (popup == null)
            {
                foreach (Transform child in GetComponentsInChildren<Transform>(true))
                {
                    if (child.name.Contains("NameInput") || child.name.Contains("Popup"))
                    {
                        popup = child;
                        break;
                    }
                }
            }
            if (popup != null) nameInputPopup = popup.gameObject;
        }

        // Setup popup
        if (nameInputPopup != null)
        {
            popupRectTransform = nameInputPopup.GetComponent<RectTransform>();
            popupCanvasGroup = nameInputPopup.GetComponent<CanvasGroup>();
            if (popupCanvasGroup == null)
            {
                popupCanvasGroup = nameInputPopup.AddComponent<CanvasGroup>();
            }
        }

        Debug.Log($"[SaveMenuAnimation] Initialisé. Popup: {(nameInputPopup != null ? nameInputPopup.name : "non trouvé")}");
    }

    private void OnEnable()
    {
        ShowPanel();
    }

    /// <summary>
    /// Animation d'entrée du panel principal
    /// </summary>
    public void ShowPanel()
    {
        if (canvasGroup == null || rectTransform == null) return;

        CancelPanelMotions();

        canvasGroup.alpha = 0f;
        rectTransform.localScale = Vector3.one * panelInitialScale;

        // Fade in
        fadeMotion = LMotion.Create(0f, 1f, panelAnimDuration)
            .WithEase(Ease.OutQuad)
            .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
            .Bind(alpha => canvasGroup.alpha = alpha);

        // Scale up avec bounce
        scaleMotion = LMotion.Create(Vector3.one * panelInitialScale, Vector3.one, panelAnimDuration)
            .WithEase(panelEaseType)
            .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
            .BindToLocalScale(rectTransform);

        Debug.Log("[SaveMenuAnimation] Panel Show animation");
    }

    /// <summary>
    /// Animation de sortie du panel principal
    /// </summary>
    public void HidePanel(System.Action onComplete = null)
    {
        if (canvasGroup == null || rectTransform == null)
        {
            onComplete?.Invoke();
            return;
        }

        CancelPanelMotions();

        float duration = panelAnimDuration * 0.6f;

        // Fade out
        fadeMotion = LMotion.Create(canvasGroup.alpha, 0f, duration)
            .WithEase(Ease.InQuad)
            .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
            .Bind(alpha => canvasGroup.alpha = alpha);

        // Scale down
        scaleMotion = LMotion.Create(rectTransform.localScale, Vector3.one * panelInitialScale, duration)
            .WithEase(Ease.InBack)
            .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
            .WithOnComplete(() =>
            {
                gameObject.SetActive(false);
                rectTransform.localScale = Vector3.one;
                canvasGroup.alpha = 1f;
                onComplete?.Invoke();
            })
            .BindToLocalScale(rectTransform);

        Debug.Log("[SaveMenuAnimation] Panel Hide animation");
    }

    /// <summary>
    /// Animation d'apparition du popup (zoom depuis le centre)
    /// </summary>
    public void ShowPopup()
    {
        if (nameInputPopup == null || popupCanvasGroup == null || popupRectTransform == null)
        {
            // Fallback: activer sans animation
            if (nameInputPopup != null) nameInputPopup.SetActive(true);
            return;
        }

        CancelPopupMotions();

        // État initial
        popupCanvasGroup.alpha = 0f;
        popupRectTransform.localScale = Vector3.one * popupInitialScale;
        nameInputPopup.SetActive(true);

        // Fade in
        popupFadeMotion = LMotion.Create(0f, 1f, popupAnimDuration)
            .WithEase(Ease.OutQuad)
            .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
            .Bind(alpha => popupCanvasGroup.alpha = alpha);

        // Scale avec effet "pop"
        popupScaleMotion = LMotion.Create(Vector3.one * popupInitialScale, Vector3.one, popupAnimDuration)
            .WithEase(popupEaseType)
            .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
            .BindToLocalScale(popupRectTransform);

        Debug.Log("[SaveMenuAnimation] Popup Show animation");
    }

    /// <summary>
    /// Animation de fermeture du popup
    /// </summary>
    public void HidePopup(System.Action onComplete = null)
    {
        if (nameInputPopup == null || popupCanvasGroup == null || popupRectTransform == null)
        {
            if (nameInputPopup != null) nameInputPopup.SetActive(false);
            onComplete?.Invoke();
            return;
        }

        CancelPopupMotions();

        float duration = popupAnimDuration * 0.6f;

        // Fade out
        popupFadeMotion = LMotion.Create(popupCanvasGroup.alpha, 0f, duration)
            .WithEase(Ease.InQuad)
            .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
            .Bind(alpha => popupCanvasGroup.alpha = alpha);

        // Scale down
        popupScaleMotion = LMotion.Create(popupRectTransform.localScale, Vector3.one * popupInitialScale, duration)
            .WithEase(Ease.InBack)
            .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
            .WithOnComplete(() =>
            {
                nameInputPopup.SetActive(false);
                popupRectTransform.localScale = Vector3.one;
                popupCanvasGroup.alpha = 1f;
                onComplete?.Invoke();
            })
            .BindToLocalScale(popupRectTransform);

        Debug.Log("[SaveMenuAnimation] Popup Hide animation");
    }

    private void CancelPanelMotions()
    {
        if (fadeMotion.IsActive()) fadeMotion.Cancel();
        if (scaleMotion.IsActive()) scaleMotion.Cancel();
    }

    private void CancelPopupMotions()
    {
        if (popupFadeMotion.IsActive()) popupFadeMotion.Cancel();
        if (popupScaleMotion.IsActive()) popupScaleMotion.Cancel();
    }

    private void OnDestroy()
    {
        CancelPanelMotions();
        CancelPopupMotions();
    }
}
