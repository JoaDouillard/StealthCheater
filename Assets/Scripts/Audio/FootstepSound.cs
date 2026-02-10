using UnityEngine;

/// <summary>
/// Joue des sons de pas basés sur la VITESSE de déplacement
/// L'intervalle entre les pas s'adapte automatiquement à la vitesse
/// </summary>
public class FootstepSound : MonoBehaviour
{
    public enum EntityType
    {
        Player,
        Teacher
    }

    [Header("Configuration")]
    [SerializeField] private EntityType entityType = EntityType.Player;

    [Header("Speed Thresholds")]
    [Tooltip("Vitesse minimum pour jouer des pas (en dessous = pas de son)")]
    [SerializeField] private float minSpeed = 0.5f;

    [Tooltip("Vitesse de marche normale")]
    [SerializeField] private float walkSpeed = 2f;

    [Tooltip("Vitesse de course")]
    [SerializeField] private float runSpeed = 5f;

    [Header("Step Intervals")]
    [Tooltip("Intervalle entre les pas en marchant (secondes)")]
    [SerializeField] private float walkStepInterval = 0.6f;

    [Tooltip("Intervalle entre les pas en courant (secondes)")]
    [SerializeField] private float runStepInterval = 0.4f;

    [Tooltip("Intervalle entre les pas accroupi (secondes)")]
    [SerializeField] private float crouchStepInterval = 0.85f;

    // État interne
    private float stepTimer = 0f;
    private Vector3 lastPosition;
    private float currentSpeed = 0f;

    // Références optionnelles (pour détecter crouch)
    private PlayerController playerController;

    private void Start()
    {
        lastPosition = transform.position;

        // Trouver PlayerController si c'est le player
        if (entityType == EntityType.Player)
        {
            playerController = GetComponent<PlayerController>();
        }
    }

    private void Update()
    {
        // Calculer la vitesse depuis le changement de position
        Vector3 currentPos = transform.position;
        float distance = Vector3.Distance(currentPos, lastPosition);
        currentSpeed = distance / Time.deltaTime;
        lastPosition = currentPos;

        // Pas de son si on ne bouge pas assez
        if (currentSpeed < minSpeed)
        {
            stepTimer = 0f;
            return;
        }

        // Calculer l'intervalle basé sur la vitesse
        float interval = CalculateStepInterval();

        // Timer pour les pas
        stepTimer -= Time.deltaTime;
        if (stepTimer <= 0f)
        {
            PlayFootstep();
            stepTimer = interval;
        }
    }

    /// <summary>
    /// Calcule l'intervalle entre les pas basé sur la vitesse actuelle
    /// </summary>
    private float CalculateStepInterval()
    {
        // Si accroupi (player uniquement)
        if (entityType == EntityType.Player && playerController != null && playerController.IsCrouching())
        {
            return crouchStepInterval;
        }

        // Interpoler entre marche et course selon la vitesse
        // Plus on va vite, plus l'intervalle est court
        float speedRatio = Mathf.InverseLerp(walkSpeed, runSpeed, currentSpeed);
        float interval = Mathf.Lerp(walkStepInterval, runStepInterval, speedRatio);

        return interval;
    }

    private void PlayFootstep()
    {
        if (AudioManager.Instance == null) return;

        switch (entityType)
        {
            case EntityType.Player:
                AudioManager.Instance.PlayPlayerFootstep();
                break;
            case EntityType.Teacher:
                AudioManager.Instance.PlayTeacherFootstep();
                break;
        }
    }
}
