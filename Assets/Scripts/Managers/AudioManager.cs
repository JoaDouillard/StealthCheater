using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Gestionnaire audio global - UTILISE L'AUDIOMIXER
/// Mettre dans MainMenu (persiste avec DontDestroyOnLoad)
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Mixer (OBLIGATOIRE)")]
    [Tooltip("Glisse ton MainAudioMixer ici")]
    [SerializeField] private AudioMixer audioMixer;

    [Header("Mixer Groups (OBLIGATOIRE)")]
    [Tooltip("Le groupe SFX du mixer")]
    [SerializeField] private AudioMixerGroup sfxMixerGroup;
    [Tooltip("Le groupe Music du mixer")]
    [SerializeField] private AudioMixerGroup musicMixerGroup;

    [Header("Noms des paramètres exposés")]
    [SerializeField] private string masterVolumeParam = "MasterVolume";
    [SerializeField] private string sfxVolumeParam = "SFXVolume";
    [SerializeField] private string musicVolumeParam = "MusicVolume";

    [Header("Music Clips")]
    [SerializeField] private AudioClip mainMenuMusic;
    [SerializeField] private AudioClip gameMusic;
    [SerializeField] private AudioClip gameOverMusic;

    [Header("Game Over")]
    [SerializeField] private AudioClip gameOverSFX;

    [Header("UI Sounds")]
    [SerializeField] private AudioClip buttonClickSound;
    [SerializeField] private AudioClip panelOpenSound;
    [SerializeField] private AudioClip panelCloseSound;

    [Header("Copy Sounds")]
    [SerializeField] private AudioClip copyStartSound;
    [SerializeField] private AudioClip copyingLoopSound;
    [SerializeField] private AudioClip copyCompleteSound;

    [Header("Write Sounds")]
    [SerializeField] private AudioClip writeStartSound;
    [SerializeField] private AudioClip writingLoopSound;
    [SerializeField] private AudioClip writeCompleteSound;

    [Header("Player Footsteps")]
    [SerializeField] private AudioClip playerFootstepLeft;
    [SerializeField] private AudioClip playerFootstepRight;
    [SerializeField] private AudioClip playerCrouchSound;

    [Header("Teacher Footsteps")]
    [SerializeField] private AudioClip teacherFootstepLeft;
    [SerializeField] private AudioClip teacherFootstepRight;

    [Header("Detection")]
    [Tooltip("Son joué UNE SEULE FOIS quand le joueur entre dans le champ de vision")]
    [SerializeField] private AudioClip detectionAlertSound;

    [Header("Timer")]
    [Tooltip("Son de tic-tac (15 secondes avant la fin)")]
    [SerializeField] private AudioClip timerTickSound;
    [Tooltip("Son de la sonnerie d'école (5 secondes avant la fin)")]
    [SerializeField] private AudioClip schoolBellSound;

    // Audio Sources (créées automatiquement et connectées au mixer)
    private AudioSource musicSource;
    private AudioSource sfxSource;
    private AudioSource loopSfxSource;
    private AudioSource timerTickSource; // Source dédiée pour le tick-tac en loop

    // État footsteps
    private bool playerLeftFoot = true;
    private bool teacherLeftFoot = true;

    // État timer
    private bool tickingStarted = false;
    private bool bellPlayed = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            CreateAudioSources();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void CreateAudioSources()
    {
        // Créer et configurer les AudioSources CONNECTÉES AU MIXER

        // Music Source → connectée au groupe Music
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;
        if (musicMixerGroup != null)
            musicSource.outputAudioMixerGroup = musicMixerGroup;

        // SFX Source → connectée au groupe SFX
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.loop = false;
        sfxSource.playOnAwake = false;
        if (sfxMixerGroup != null)
            sfxSource.outputAudioMixerGroup = sfxMixerGroup;

        // Loop SFX Source → connectée au groupe SFX
        loopSfxSource = gameObject.AddComponent<AudioSource>();
        loopSfxSource.loop = true;
        loopSfxSource.playOnAwake = false;
        if (sfxMixerGroup != null)
            loopSfxSource.outputAudioMixerGroup = sfxMixerGroup;

        // Timer Tick Source → connectée au groupe SFX (loop pour tic-tac)
        timerTickSource = gameObject.AddComponent<AudioSource>();
        timerTickSource.loop = true;
        timerTickSource.playOnAwake = false;
        if (sfxMixerGroup != null)
            timerTickSource.outputAudioMixerGroup = sfxMixerGroup;

        if (audioMixer == null)
            Debug.LogError("[AudioManager] AudioMixer non assigné ! Les volumes ne fonctionneront pas.");
        if (sfxMixerGroup == null)
            Debug.LogError("[AudioManager] SFX Mixer Group non assigné !");
        if (musicMixerGroup == null)
            Debug.LogError("[AudioManager] Music Mixer Group non assigné !");
    }

    #region Volume Control (via AudioMixer)

    /// <summary>
    /// Volume Master (0-1)
    /// </summary>
    public void SetMasterVolume(float volume)
    {
        if (audioMixer == null) return;
        float db = volume > 0.001f ? Mathf.Log10(volume) * 20f : -80f;
        audioMixer.SetFloat(masterVolumeParam, db);
    }

    /// <summary>
    /// Volume SFX (0-1)
    /// </summary>
    public void SetSFXVolume(float volume)
    {
        if (audioMixer == null)
        {
            Debug.LogError("[AudioManager] SetSFXVolume: audioMixer est NULL!");
            return;
        }
        float db = volume > 0.001f ? Mathf.Log10(volume) * 20f : -80f;
        bool success = audioMixer.SetFloat(sfxVolumeParam, db);
        if (!success)
        {
            Debug.LogError($"[AudioManager] ÉCHEC SetFloat('{sfxVolumeParam}'). Vérifie que le paramètre est EXPOSÉ dans l'AudioMixer!");
        }
    }

    /// <summary>
    /// Volume Music (0-1)
    /// </summary>
    public void SetMusicVolume(float volume)
    {
        if (audioMixer == null) return;
        float db = volume > 0.001f ? Mathf.Log10(volume) * 20f : -80f;
        audioMixer.SetFloat(musicVolumeParam, db);
    }

    /// <summary>
    /// Obtient le volume actuel (0-1)
    /// </summary>
    public float GetMasterVolume()
    {
        if (audioMixer == null) return 1f;
        audioMixer.GetFloat(masterVolumeParam, out float db);
        return Mathf.Pow(10f, db / 20f);
    }

    public float GetSFXVolume()
    {
        if (audioMixer == null) return 1f;
        audioMixer.GetFloat(sfxVolumeParam, out float db);
        return Mathf.Pow(10f, db / 20f);
    }

    public float GetMusicVolume()
    {
        if (audioMixer == null) return 1f;
        audioMixer.GetFloat(musicVolumeParam, out float db);
        return Mathf.Pow(10f, db / 20f);
    }

    #endregion

    #region Music

    public void PlayMainMenuMusic() => PlayMusic(mainMenuMusic);
    public void PlayGameMusic() => PlayMusic(gameMusic);
    public void PlayGameOverMusic() => PlayMusic(gameOverMusic);

    public void PlayGameOverSequence()
    {
        StopMusic();
        if (gameOverSFX != null)
        {
            PlaySFX(gameOverSFX);
            Invoke(nameof(PlayGameOverMusic), gameOverSFX.length + 0.5f);
        }
        else
        {
            PlayGameOverMusic();
        }
    }

    private void PlayMusic(AudioClip clip)
    {
        if (clip == null || musicSource == null) return;
        if (musicSource.clip == clip && musicSource.isPlaying) return;
        musicSource.clip = clip;
        musicSource.Play();
    }

    public void StopMusic()
    {
        if (musicSource != null) musicSource.Stop();
    }

    public void PauseMusic()
    {
        if (musicSource != null) musicSource.Pause();
    }

    public void ResumeMusic()
    {
        if (musicSource != null) musicSource.UnPause();
    }

    #endregion

    #region UI Sounds

    public void PlayButtonClick() => PlaySFX(buttonClickSound);
    public void PlayPanelOpen() => PlaySFX(panelOpenSound);
    public void PlayPanelClose() => PlaySFX(panelCloseSound);

    #endregion

    #region Copy Sounds

    public void PlayCopyStart()
    {
        PlaySFX(copyStartSound);
        if (copyingLoopSound != null)
        {
            float delay = copyStartSound != null ? copyStartSound.length : 0f;
            Invoke(nameof(StartCopyingLoop), delay);
        }
    }

    private void StartCopyingLoop() => PlayLoopSFX(copyingLoopSound);

    public void PlayCopyComplete()
    {
        CancelInvoke(nameof(StartCopyingLoop));
        StopLoopSFX();
        PlaySFX(copyCompleteSound);
    }

    public void StopCopying()
    {
        CancelInvoke(nameof(StartCopyingLoop));
        StopLoopSFX();
    }

    #endregion

    #region Write Sounds

    public void PlayWriteStart()
    {
        PlaySFX(writeStartSound);
        if (writingLoopSound != null)
        {
            float delay = writeStartSound != null ? writeStartSound.length : 0f;
            Invoke(nameof(StartWritingLoop), delay);
        }
    }

    private void StartWritingLoop() => PlayLoopSFX(writingLoopSound);

    public void PlayWriteComplete()
    {
        CancelInvoke(nameof(StartWritingLoop));
        StopLoopSFX();
        PlaySFX(writeCompleteSound);
    }

    public void StopWriting()
    {
        CancelInvoke(nameof(StartWritingLoop));
        StopLoopSFX();
    }

    #endregion

    #region Footsteps

    /// <summary>
    /// Joue un pas du joueur (alterne gauche/droite)
    /// </summary>
    public void PlayPlayerFootstep()
    {
        AudioClip clip = playerLeftFoot ? playerFootstepLeft : playerFootstepRight;
        if (clip == null) clip = playerFootstepLeft ?? playerFootstepRight;
        if (clip != null) PlaySFX(clip);
        playerLeftFoot = !playerLeftFoot;
    }

    /// <summary>
    /// Joue un pas du prof (alterne gauche/droite)
    /// </summary>
    public void PlayTeacherFootstep()
    {
        AudioClip clip = teacherLeftFoot ? teacherFootstepLeft : teacherFootstepRight;
        if (clip == null) clip = teacherFootstepLeft ?? teacherFootstepRight;
        if (clip != null) PlaySFX(clip);
        teacherLeftFoot = !teacherLeftFoot;
    }

    public void PlayPlayerCrouch() => PlaySFX(playerCrouchSound);

    #endregion

    #region Detection

    /// <summary>
    /// Son d'alerte quand le joueur entre dans le champ de vision
    /// </summary>
    public void PlayDetectionAlert() => PlaySFX(detectionAlertSound);

    #endregion

    #region Timer

    /// <summary>
    /// Démarre le tic-tac du timer (à appeler 15 secondes avant la fin)
    /// </summary>
    public void StartTimerTicking()
    {
        if (tickingStarted) return;
        if (timerTickSound == null || timerTickSource == null) return;

        tickingStarted = true;
        timerTickSource.clip = timerTickSound;
        timerTickSource.Play();
    }

    /// <summary>
    /// Arrête le tic-tac du timer
    /// </summary>
    public void StopTimerTicking()
    {
        if (timerTickSource != null)
            timerTickSource.Stop();
    }

    /// <summary>
    /// Joue la sonnerie d'école (à appeler 5 secondes avant la fin)
    /// </summary>
    public void PlaySchoolBell()
    {
        if (bellPlayed) return;
        bellPlayed = true;
        PlaySFX(schoolBellSound);
    }

    /// <summary>
    /// Reset l'état du timer (pour un nouvel examen)
    /// </summary>
    public void ResetTimerState()
    {
        tickingStarted = false;
        bellPlayed = false;
        StopTimerTicking();
    }

    #endregion

    #region Core Methods

    private void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip, volume);
    }

    private void PlayLoopSFX(AudioClip clip)
    {
        if (clip == null || loopSfxSource == null) return;
        if (loopSfxSource.clip == clip && loopSfxSource.isPlaying) return;
        loopSfxSource.clip = clip;
        loopSfxSource.Play();
    }

    private void StopLoopSFX()
    {
        if (loopSfxSource != null) loopSfxSource.Stop();
    }

    #endregion
}
