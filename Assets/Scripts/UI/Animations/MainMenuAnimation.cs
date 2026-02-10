using UnityEngine;
using UnityEngine.UI;
using LitMotion;
using LitMotion.Extensions;
using System.Collections.Generic;

/// <summary>
/// Animation du main menu - Fade in progressif des boutons + slide
/// </summary>
public class MainMenuAnimation : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float delayBetweenButtons = 0.1f;
    [SerializeField] private float buttonAnimDuration = 0.5f;
    [SerializeField] private Ease easeType = Ease.OutCubic;

    [Header("Auto-detect Buttons")]
    [Tooltip("Si true, détecte automatiquement tous les boutons enfants")]
    [SerializeField] private bool autoDetectButtons = true;

    [Header("Manual Buttons (optional)")]
    [SerializeField] private List<GameObject> buttonsToAnimate = new List<GameObject>();

    private List<MotionHandle> activeMotions = new List<MotionHandle>();

    private void Start()
    {
        // Auto-détecter les boutons si activé
        if (autoDetectButtons && buttonsToAnimate.Count == 0)
        {
            Button[] buttons = GetComponentsInChildren<Button>(true);
            foreach (Button btn in buttons)
            {
                buttonsToAnimate.Add(btn.gameObject);
            }
            Debug.Log($"[MainMenuAnimation] {buttonsToAnimate.Count} boutons détectés automatiquement");
        }

        // Lancer l'animation d'entrée
        AnimateIn();
    }

    /// <summary>
    /// Anime l'apparition des boutons un par un
    /// UTILISE SCALE + ALPHA SEULEMENT (ne touche PAS aux positions pour préserver le Vertical Layout Group)
    /// </summary>
    public void AnimateIn()
    {
        // Annuler animations précédentes
        CancelAllMotions();

        for (int i = 0; i < buttonsToAnimate.Count; i++)
        {
            GameObject buttonObj = buttonsToAnimate[i];
            if (buttonObj == null) continue;

            // Ajouter CanvasGroup si manquant
            CanvasGroup canvasGroup = buttonObj.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = buttonObj.AddComponent<CanvasGroup>();
            }

            RectTransform rectTransform = buttonObj.GetComponent<RectTransform>();
            if (rectTransform == null) continue;

            // État initial
            canvasGroup.alpha = 0f;
            rectTransform.localScale = Vector3.one * 0.5f; // Scale réduit

            // Délai progressif pour chaque bouton
            float delay = i * delayBetweenButtons;

            // Animation fade in (alpha 0 → 1)
            var fadeMotion = LMotion.Create(0f, 1f, buttonAnimDuration)
                .WithDelay(delay)
                .WithEase(Ease.OutQuad)
                .Bind(alpha => canvasGroup.alpha = alpha);

            activeMotions.Add(fadeMotion);

            // Animation scale (0.5 → 1)
            var scaleMotion = LMotion.Create(Vector3.one * 0.5f, Vector3.one, buttonAnimDuration)
                .WithDelay(delay)
                .WithEase(easeType)
                .BindToLocalScale(rectTransform);

            activeMotions.Add(scaleMotion);
        }

        Debug.Log($"[MainMenuAnimation] Animation In démarrée pour {buttonsToAnimate.Count} boutons (SCALE + ALPHA)");
    }

    /// <summary>
    /// Anime la disparition des boutons
    /// </summary>
    public void AnimateOut()
    {
        // Annuler animations précédentes
        CancelAllMotions();

        for (int i = 0; i < buttonsToAnimate.Count; i++)
        {
            GameObject buttonObj = buttonsToAnimate[i];
            if (buttonObj == null) continue;

            CanvasGroup canvasGroup = buttonObj.GetComponent<CanvasGroup>();
            if (canvasGroup == null) continue;

            // Animation fade out rapide
            float delay = i * 0.05f; // Délai plus court pour la sortie
            var motion = LMotion.Create(canvasGroup.alpha, 0f, 0.3f)
                .WithDelay(delay)
                .WithEase(Ease.InQuad)
                .Bind(alpha => canvasGroup.alpha = alpha);

            activeMotions.Add(motion);
        }

        Debug.Log($"[MainMenuAnimation] Animation Out démarrée pour {buttonsToAnimate.Count} boutons");
    }

    /// <summary>
    /// Annule toutes les animations actives
    /// </summary>
    private void CancelAllMotions()
    {
        foreach (var motion in activeMotions)
        {
            if (motion.IsActive())
            {
                motion.Cancel();
            }
        }
        activeMotions.Clear();
    }

    private void OnDestroy()
    {
        CancelAllMotions();
    }
}
