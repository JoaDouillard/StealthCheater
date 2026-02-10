using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using LitMotion;
using LitMotion.Extensions;

/// <summary>
/// Animation hover et click pour les boutons
/// Ajoute ce component à N'IMPORTE QUEL bouton pour animer automatiquement
/// </summary>
[RequireComponent(typeof(Button))]
public class ButtonHoverAnimation : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Hover Settings")]
    [SerializeField] private float hoverScale = 1.1f;
    [SerializeField] private float hoverDuration = 0.2f;
    [SerializeField] private Ease hoverEase = Ease.OutCubic;

    [Header("Click Settings")]
    [SerializeField] private float clickScale = 0.95f;
    [SerializeField] private float clickDuration = 0.1f;

    private RectTransform rectTransform;
    private Vector3 originalScale;
    private MotionHandle scaleMotion;
    private bool isHovering = false;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originalScale = rectTransform.localScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Cancel animation précédente
        if (scaleMotion.IsActive())
        {
            scaleMotion.Cancel();
        }

        isHovering = true;

        // Scale up
        scaleMotion = LMotion.Create(rectTransform.localScale, originalScale * hoverScale, hoverDuration)
            .WithEase(hoverEase)
            .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
            .BindToLocalScale(rectTransform);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Cancel animation précédente
        if (scaleMotion.IsActive())
        {
            scaleMotion.Cancel();
        }

        isHovering = false;

        // Retour scale normal
        scaleMotion = LMotion.Create(rectTransform.localScale, originalScale, hoverDuration)
            .WithEase(Ease.InCubic)
            .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
            .BindToLocalScale(rectTransform);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // Cancel animation précédente
        if (scaleMotion.IsActive())
        {
            scaleMotion.Cancel();
        }

        // Scale down (press effect)
        scaleMotion = LMotion.Create(rectTransform.localScale, originalScale * clickScale, clickDuration)
            .WithEase(Ease.OutQuad)
            .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
            .BindToLocalScale(rectTransform);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // Cancel animation précédente
        if (scaleMotion.IsActive())
        {
            scaleMotion.Cancel();
        }

        // Retour hover ou normal selon si on est encore sur le bouton
        Vector3 targetScale = isHovering ? originalScale * hoverScale : originalScale;

        scaleMotion = LMotion.Create(rectTransform.localScale, targetScale, clickDuration)
            .WithEase(Ease.OutBack)
            .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
            .BindToLocalScale(rectTransform);
    }

    private void OnDestroy()
    {
        if (scaleMotion.IsActive())
        {
            scaleMotion.Cancel();
        }
    }
}
