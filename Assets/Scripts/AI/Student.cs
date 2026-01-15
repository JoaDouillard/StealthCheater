using UnityEngine;

/// <summary>
/// Élève statique qui joue une animation Idle
/// Sert d'obstacle visuel et de décor
/// </summary>
public class Student : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private string idleAnimationName = "Sitting"; // Nom de l'animation Idle (ex: "Sitting", "Idle")

    [Header("Obstacle Settings")]
    [SerializeField] private bool isObstacle = true; // Bloque la ligne de vue du professeur
    [SerializeField] private LayerMask obstacleLayer; // Layer "Obstacle"

    // Composants
    private Animator animator;

    #region Unity Lifecycle

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();

        // Configurer le layer comme obstacle si nécessaire
        if (isObstacle)
        {
            SetupObstacleLayer();
        }
    }

    private void Start()
    {
        // Jouer l'animation Idle
        PlayIdleAnimation();
    }

    #endregion

    #region Animation

    /// <summary>
    /// Joue l'animation Idle (assis)
    /// </summary>
    private void PlayIdleAnimation()
    {
        if (animator == null)
        {
            Debug.LogWarning($"[Student] Pas d'Animator sur {gameObject.name}");
            return;
        }

        // Si tu as un Animator Controller avec un paramètre "Speed"
        if (animator.parameters.Length > 0)
        {
            animator.SetFloat("Speed", 0f); // Idle
        }

        // OU si tu veux jouer directement une animation spécifique
        // animator.Play(idleAnimationName);
    }

    #endregion

    #region Obstacle Setup

    /// <summary>
    /// Configure l'élève comme obstacle pour la détection
    /// </summary>
    private void SetupObstacleLayer()
    {
        // Mettre le GameObject sur le layer "Obstacle"
        int obstacleLayerIndex = LayerMask.NameToLayer("Obstacle");

        if (obstacleLayerIndex == -1)
        {
            Debug.LogWarning("[Student] Layer 'Obstacle' n'existe pas ! Crée-le dans les Tags & Layers.");
            return;
        }

        gameObject.layer = obstacleLayerIndex;

        Debug.Log($"[Student] {gameObject.name} configuré comme obstacle (Layer: Obstacle)");
    }

    #endregion

    #region Gizmos

    private void OnDrawGizmos()
    {
        // Dessiner une sphère pour visualiser l'élève en mode Scene
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 1f, 0.3f);
    }

    #endregion
}
