using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game State")]
    [SerializeField] private GameState currentState = GameState.Playing;

    [Header("References")]
    [SerializeField] private PlayerController player;

    public delegate void GameStateChanged(GameState newState);
    public event GameStateChanged OnGameStateChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        ChangeState(GameState.Playing);
    }

    public void ChangeState(GameState newState)
    {
        if (currentState == newState) return;

        currentState = newState;
        Debug.Log($"[GameManager] State changed to: {newState}");

        OnGameStateChanged?.Invoke(newState);
        HandleStateChange(newState);
    }

    private void HandleStateChange(GameState newState)
    {
        switch (newState)
        {
            case GameState.Playing:
                Time.timeScale = 1f;
                if (player != null) player.SetCanMove(true);
                break;

            case GameState.Copying:
                if (player != null) player.SetCanMove(false);
                break;

            case GameState.GameOver:
                Time.timeScale = 0f;
                Debug.Log("GAME OVER - Vous avez été repéré!");
                break;

            case GameState.Victory:
                Time.timeScale = 0f;
                Debug.Log("VICTOIRE - Mission réussie!");
                break;

            case GameState.Paused:
                Time.timeScale = 0f;
                break;
        }
    }

    public void OnPlayerDetected()
    {
        // Jouer animation de défaite
        if (player != null)
        {
            player.PlayDefeatAnimation();
        }

        ChangeState(GameState.GameOver);
    }

    public void OnCopyStarted()
    {
        ChangeState(GameState.Copying);
    }

    public void OnCopyCompleted()
    {
        ChangeState(GameState.Playing);
        Debug.Log("Copie terminée ! Retournez à votre place.");
    }

    public void OnMissionCompleted()
    {
        ChangeState(GameState.Victory);
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        );
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }

    public GameState GetCurrentState() => currentState;
    public PlayerController GetPlayer() => player;
    public void SetPlayer(PlayerController playerRef) => player = playerRef;
}

public enum GameState
{
    Playing,
    Copying,
    GameOver,
    Victory,
    Paused
}
