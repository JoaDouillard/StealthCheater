using UnityEngine;

/// <summary>
/// Démarre automatiquement la musique appropriée pour cette scène
/// Ajouter ce composant à un GameObject dans chaque scène
/// </summary>
public class SceneMusic : MonoBehaviour
{
    public enum MusicType
    {
        MainMenu,
        Game,
        GameOver
    }

    [Header("Configuration")]
    [SerializeField] private MusicType musicType = MusicType.Game;
    [SerializeField] private float startDelay = 0.1f;

    private void Start()
    {
        Invoke(nameof(StartMusic), startDelay);
    }

    private void StartMusic()
    {
        if (AudioManager.Instance == null)
        {
            Debug.LogWarning("[SceneMusic] AudioManager not found!");
            return;
        }

        switch (musicType)
        {
            case MusicType.MainMenu:
                AudioManager.Instance.PlayMainMenuMusic();
                break;
            case MusicType.Game:
                AudioManager.Instance.PlayGameMusic();
                break;
            case MusicType.GameOver:
                // Joue le SFX de game over puis la musique
                AudioManager.Instance.PlayGameOverSequence();
                break;
        }
    }
}
