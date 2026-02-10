using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI pour le panel Tutorial (accessible depuis Settings et MainMenu)
/// Affiche les conseils de jeu, contrôles, mécaniques, etc.
/// </summary>
public class TutorialUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Panel Tutorial (TutorialPanel)")]
    [SerializeField] private GameObject tutorialPanel;

    [Tooltip("Titre du tutorial")]
    [SerializeField] private TextMeshProUGUI titleText;

    [Tooltip("Contenu scrollable du tutorial")]
    [SerializeField] private TextMeshProUGUI contentText;

    [Header("Buttons")]
    [Tooltip("Bouton Close (fermer le tutorial)")]
    [SerializeField] private Button closeButton;

    [Header("Pages")]
    [Tooltip("Page actuelle (0 = première page)")]
    [SerializeField] private int currentPage = 0;

    [Tooltip("Bouton Page Précédente")]
    [SerializeField] private Button previousPageButton;

    [Tooltip("Bouton Page Suivante")]
    [SerializeField] private Button nextPageButton;

    [Tooltip("Texte indicateur de page (ex: '1/5')")]
    [SerializeField] private TextMeshProUGUI pageIndicatorText;

    // Callback pour retour au menu précédent
    private System.Action onCloseCallback;

    // Contenu des pages du tutorial
    private readonly string[] tutorialPages = new string[]
    {
        // Page 1 - Basic Controls
        @"<size=28><b>BASIC CONTROLS</b></size>

<b>Movement:</b>
<b>Z / W</b> - Move Forward
<b>S</b> - Move Backward
<b>Q / A</b> - Move Left
<b>D</b> - Move Right
<b>SHIFT</b> - Sprint (move faster)
<b>CTRL</b> - Crouch (hide better)

<b>Camera:</b>
<b>Mouse</b> - Look around (First Person)
<b>V</b> - Switch Camera (First/Third Person)",

        // Page 2 - Interaction Controls
        @"<size=28><b>INTERACTION CONTROLS</b></size>

<b>Copying:</b>
<b>E</b> - Copy from a student (near their desk)
<b>E</b> - Write answer on your paper (at your desk)

<b>Menus:</b>
<b>ESCAPE</b> - Pause Menu
<b>TAB</b> - Open/Close Cheat Sheet

<b>Cheat Sheet:</b>
Shows all students and their grades.
Use it to find the best student to copy from!
<i>Warning: The game does NOT pause while it's open!</i>",

        // Page 3 - Game Context
        @"<size=28><b>THE STORY</b></size>

You are a student who didn't study for the exams...
Your only chance? <b>Cheat!</b>

<b>Your Week:</b>
- 5 days of exams (Monday → Friday)
- Each day has a different subject
- Each exam has 2-5 questions

<b>Your Goal:</b>
- Copy answers from smarter students
- Return to your desk to write them down
- Don't get caught by the teacher!
- Get the best average score possible",

        // Page 4 - Tips & Strategies
        @"<size=28><b>TIPS & STRATEGIES</b></size>

<b>Stealth:</b>
- <b>Crouch</b> to be less visible
- <b>Hide behind desks</b> and objects
- Watch the teacher's patrol pattern
- Move when she's not looking!

<b>Copying:</b>
- Check the <b>Cheat Sheet</b> (TAB) first
- Copy from students with <b>high grades</b> (green)
- Avoid students with <b>low grades</b> (red)
- Better student = More points!

<b>Time Management:</b>
- Watch the timer! It changes color when low
- Faster completion = Better score",

        // Page 5 - Detection & Game Over
        @"<size=28><b>DETECTION SYSTEM</b></size>

<b>Teacher Vision:</b>
The teacher patrols the classroom.
If she sees you cheating → <b>GAME OVER!</b>

<b>Detection Levels:</b>
- <color=green>SAFE</color> - Teacher can't see you
- <color=yellow>VISIBLE</color> - In her field of view
- <color=orange>SUSPECT</color> - She's getting suspicious
- <color=red>DANGER</color> - About to catch you!

<b>If Caught:</b>
- You can restart the current day
- Previous days' scores are kept
- Try a different strategy!

<b>Good luck, cheater!</b>"
    };

    private void Awake()
    {
        // Setup boutons
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(OnCloseClicked);
        }

        if (previousPageButton != null)
        {
            previousPageButton.onClick.AddListener(OnPreviousPageClicked);
        }

        if (nextPageButton != null)
        {
            nextPageButton.onClick.AddListener(OnNextPageClicked);
        }

        // NE PAS désactiver le panel ici !
        // UIManager gère l'état actif/inactif des panels
    }

    /// <summary>
    /// Ouvre le tutorial
    /// </summary>
    public void OpenTutorial(System.Action onClose = null)
    {
        onCloseCallback = onClose;

        // Détecter quel manager est présent (GameScene ou MainMenu)
        if (UIManager.Instance != null)
        {
            // GameScene
            UIManager.Instance.ShowTutorial();
        }
        else if (MainMenuUIManager.Instance != null)
        {
            // MainMenu
            MainMenuUIManager.Instance.ShowTutorial();
        }
        else
        {
            Debug.LogError("[TutorialUI] Aucun manager UI trouvé (UIManager ou MainMenuUIManager) !");
            return;
        }

        // Afficher la première page
        currentPage = 0;
        ShowPage(currentPage);
    }

    /// <summary>
    /// Ferme le tutorial
    /// </summary>
    public void CloseTutorial()
    {
        // Détecter quel manager est présent
        if (UIManager.Instance != null)
        {
            // GameScene - retour HUD
            UIManager.Instance.ShowHUD();
        }
        else if (MainMenuUIManager.Instance != null)
        {
            // MainMenu - le callback gère le retour
            // Ne rien faire ici, le callback s'en occupe
        }
        else
        {
            Debug.LogError("[TutorialUI] Aucun manager UI trouvé !");
        }

        // Callback pour retour au menu précédent
        onCloseCallback?.Invoke();
    }

    /// <summary>
    /// Affiche une page spécifique du tutorial
    /// </summary>
    private void ShowPage(int pageIndex)
    {
        // Vérifier index valide
        if (pageIndex < 0 || pageIndex >= tutorialPages.Length)
        {
            return;
        }

        currentPage = pageIndex;

        // Mettre à jour le titre
        if (titleText != null)
        {
            titleText.text = $"Tutorial - Page {currentPage + 1}";
        }

        // Mettre à jour le contenu
        if (contentText != null)
        {
            contentText.text = tutorialPages[currentPage];
        }

        // Mettre à jour l'indicateur de page
        if (pageIndicatorText != null)
        {
            pageIndicatorText.text = $"{currentPage + 1} / {tutorialPages.Length}";
        }

        // Activer/désactiver boutons navigation
        if (previousPageButton != null)
        {
            previousPageButton.interactable = (currentPage > 0);
        }

        if (nextPageButton != null)
        {
            nextPageButton.interactable = (currentPage < tutorialPages.Length - 1);
        }
    }

    #region Button Callbacks

    private void OnCloseClicked()
    {
        CloseTutorial();
    }

    private void OnPreviousPageClicked()
    {
        if (currentPage > 0)
        {
            ShowPage(currentPage - 1);
        }
    }

    private void OnNextPageClicked()
    {
        if (currentPage < tutorialPages.Length - 1)
        {
            ShowPage(currentPage + 1);
        }
    }

    #endregion
}
