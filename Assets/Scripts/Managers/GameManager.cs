using UnityEngine;

/// <summary>
/// Gère l'état du jeu et les événements globaux (Singleton)
/// </summary>
public class GameManager : MonoBehaviour
{
    // Singleton
    public static GameManager Instance { get; private set; }

    [Header("Game State")]
    [SerializeField] private GameState currentState = GameState.Playing;

    [Header("References")]
    [SerializeField] private PlayerController player;

    // Events (pour communiquer avec d'autres scripts)
    public delegate void GameStateChanged(GameState newState);
    public event GameStateChanged OnGameStateChanged;

    #region Unity Lifecycle

    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        // DontDestroyOnLoad(gameObject); // Décommenter si tu veux garder entre les scènes
    }

    private void Start()
    {
        // Initialisation
        ChangeState(GameState.Playing);
    }

    #endregion

    #region Game State Management

    /// <summary>
    /// Change l'état du jeu
    /// </summary>
    public void ChangeState(GameState newState)
    {
        if (currentState == newState) return;

        currentState = newState;
        Debug.Log($"[GameManager] State changed to: {newState}");

        // Notifier les listeners
        OnGameStateChanged?.Invoke(newState);

        // Gérer les actions selon l'état
        HandleStateChange(newState);
    }

    /// <summary>
    /// Gère les actions à faire selon le nouvel état
    /// </summary>
    private void HandleStateChange(GameState newState)
    {
        switch (newState)
        {
            case GameState.Playing:
                Time.timeScale = 1f;
                if (player != null) player.SetCanMove(true);
                break;

            case GameState.Copying:
                // Le joueur peut être bloqué pendant la copie
                if (player != null) player.SetCanMove(false);
                break;

            case GameState.GameOver:
                Time.timeScale = 0f; // Pause le jeu
                Debug.Log("GAME OVER - Vous avez été repéré!");
                // TODO: Afficher écran Game Over
                break;

            case GameState.Victory:
                Time.timeScale = 0f;
                Debug.Log("VICTOIRE - Mission réussie!");
                // TODO: Afficher écran Victory
                break;

            case GameState.Paused:
                Time.timeScale = 0f;
                // TODO: Afficher menu pause
                break;
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Appelé quand le joueur est détecté par le professeur
    /// </summary>
    public void OnPlayerDetected()
    {
        ChangeState(GameState.GameOver);
    }

    /// <summary>
    /// Appelé quand le joueur commence à copier
    /// </summary>
    public void OnCopyStarted()
    {
        ChangeState(GameState.Copying);
    }

    /// <summary>
    /// Appelé quand la copie est terminée
    /// </summary>
    public void OnCopyCompleted()
    {
        ChangeState(GameState.Playing);
        Debug.Log("Copie terminée ! Retournez à votre place.");
        // TODO: Marquer la table du joueur comme destination
    }

    /// <summary>
    /// Appelé quand le joueur atteint sa table après avoir copié
    /// </summary>
    public void OnMissionCompleted()
    {
        ChangeState(GameState.Victory);
    }

    /// <summary>
    /// Rejouer la partie
    /// </summary>
    public void RestartGame()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        );
    }

    /// <summary>
    /// Retour au menu principal
    /// </summary>
    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }

    #endregion

    #region Getters

    public GameState GetCurrentState() => currentState;

    public PlayerController GetPlayer() => player;

    public void SetPlayer(PlayerController playerRef)
    {
        player = playerRef;
    }

    #endregion
}

/// <summary>
/// États possibles du jeu
/// </summary>
public enum GameState
{
    Playing,    // Jeu en cours normal
    Copying,    // Le joueur est en train de copier
    GameOver,   // Détecté par le prof
    Victory,    // Mission réussie
    Paused      // Menu pause
}
