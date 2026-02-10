using UnityEngine;
using LitMotion;
using LitMotion.Extensions;

/// <summary>
/// Animation du menu pause - Slide down depuis le haut
/// </summary>
public class PauseMenuAnimation : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float animationDuration = 0.5f;
    [SerializeField] private Ease easeType = Ease.OutCubic;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector2 hiddenPosition;
    private Vector2 visiblePosition;
    private MotionHandle currentMotion;

    private void Awake()
    {
        Debug.Log($"[PauseMenuAnimation] === Awake sur GameObject: {gameObject.name} ===");

        rectTransform = GetComponent<RectTransform>();

        if (rectTransform == null)
        {
            Debug.LogError("[PauseMenuAnimation] RectTransform manquant!");
            return;
        }

        // Ajouter CanvasGroup si manquant (pour gérer les interactions)
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            Debug.Log("[PauseMenuAnimation] CanvasGroup manquant, ajout automatique...");
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        else
        {
            Debug.Log("[PauseMenuAnimation] ✅ CanvasGroup déjà présent");
        }

        // S'assurer que les boutons sont cliquables
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;
        Debug.Log($"[PauseMenuAnimation] ✅ CanvasGroup configuré: blocksRaycasts={canvasGroup.blocksRaycasts}, interactable={canvasGroup.interactable}");

        // Vérifier si un parent a un CanvasGroup qui pourrait bloquer
        CanvasGroup parentCanvasGroup = transform.parent?.GetComponent<CanvasGroup>();
        if (parentCanvasGroup != null)
        {
            Debug.Log($"[PauseMenuAnimation] ⚠️ Parent CanvasGroup détecté: blocksRaycasts={parentCanvasGroup.blocksRaycasts}, interactable={parentCanvasGroup.interactable}");
            if (!parentCanvasGroup.blocksRaycasts || !parentCanvasGroup.interactable)
            {
                Debug.LogWarning("[PauseMenuAnimation] ⚠️ Le parent CanvasGroup pourrait bloquer les interactions!");
            }
        }

        // Position visible = centre (0, 0)
        visiblePosition = Vector2.zero;

        // Position cachée = en haut hors écran
        hiddenPosition = new Vector2(0, Screen.height);

        Debug.Log($"[PauseMenuAnimation] Positions - Hidden: {hiddenPosition}, Visible: {visiblePosition}");
    }

    private void OnEnable()
    {
        // Lancer l'animation automatiquement quand le panel est activé
        Show();
    }

    /// <summary>
    /// Affiche le menu avec animation slide down
    /// </summary>
    public void Show()
    {
        if (rectTransform == null) return;

        // Annuler animation précédente si existe
        if (currentMotion.IsActive())
        {
            currentMotion.Cancel();
        }

        // Positionner en haut
        rectTransform.anchoredPosition = hiddenPosition;

        // Activer le GameObject
        gameObject.SetActive(true);

        // Animation: slide down vers le centre
        currentMotion = LMotion.Create(hiddenPosition, visiblePosition, animationDuration)
            .WithEase(easeType)
            .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale) // Continue même si Time.timeScale = 0 (pause)
            .BindToAnchoredPosition(rectTransform);

        Debug.Log("[PauseMenuAnimation] Animation Show démarrée");
    }

    /// <summary>
    /// Cache le menu avec animation slide up
    /// </summary>
    public void Hide()
    {
        if (rectTransform == null) return;

        // Annuler animation précédente
        if (currentMotion.IsActive())
        {
            currentMotion.Cancel();
        }

        // Animation: slide up vers le haut
        currentMotion = LMotion.Create(rectTransform.anchoredPosition, hiddenPosition, animationDuration * 0.7f)
            .WithEase(Ease.InCubic)
            .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
            .WithOnComplete(() =>
            {
                // Désactiver après l'animation
                gameObject.SetActive(false);
            })
            .BindToAnchoredPosition(rectTransform);

        Debug.Log("[PauseMenuAnimation] Animation Hide démarrée");
    }

    private void OnDestroy()
    {
        // Annuler l'animation si le GameObject est détruit
        if (currentMotion.IsActive())
        {
            currentMotion.Cancel();
        }
    }
}
