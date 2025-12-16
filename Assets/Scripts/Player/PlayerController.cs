using UnityEngine;

/// <summary>
/// Contrôle le déplacement du joueur avec le Character Controller
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float gravity = -9.81f;

    [Header("References")]
    private CharacterController characterController;
    private InputSystem_Actions inputActions;
    private Animator animator;

    // Variables internes
    private Vector2 moveInput;
    private bool isMoving;
    private bool canMove = true; // Pour bloquer le mouvement pendant la copie
    private Vector3 velocity; // Vélocité pour la gravité

    #region Unity Lifecycle

    private void Awake()
    {
        // Récupérer les composants
        characterController = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();

        // Initialiser l'Input System
        inputActions = new InputSystem_Actions();
    }

    private void OnEnable()
    {
        // Activer les inputs
        inputActions.Player.Enable();
    }

    private void OnDisable()
    {
        // Désactiver les inputs
        inputActions.Player.Disable();
    }

    private void Update()
    {
        // Appliquer la gravité
        ApplyGravity();

        if (!canMove) return;

        // Lire les inputs
        ReadInput();

        // Déplacer le personnage
        MoveCharacter();

        // Faire tourner le personnage
        RotateCharacter();

        // Mettre à jour les animations
        UpdateAnimations();
    }

    #endregion

    #region Movement

    /// <summary>
    /// Lit les inputs du joueur
    /// </summary>
    private void ReadInput()
    {
        moveInput = inputActions.Player.Move.ReadValue<Vector2>();
        isMoving = moveInput.magnitude > 0.1f;
    }

    /// <summary>
    /// Déplace le personnage selon les inputs
    /// </summary>
    private void MoveCharacter()
    {
        if (!isMoving) return;

        // Convertir l'input 2D en mouvement 3D (plan XZ)
        Vector3 moveDirection = new Vector3(moveInput.x, 0f, moveInput.y);

        // Normaliser pour éviter mouvement plus rapide en diagonal
        moveDirection.Normalize();

        // Appliquer le mouvement via Character Controller
        characterController.Move(moveDirection * moveSpeed * Time.deltaTime);
    }

    /// <summary>
    /// Applique la gravité au personnage
    /// </summary>
    private void ApplyGravity()
    {
        // Vérifier si on est au sol
        if (characterController.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Petite force pour rester collé au sol
        }

        // Appliquer la gravité
        velocity.y += gravity * Time.deltaTime;

        // Appliquer la vélocité verticale
        characterController.Move(velocity * Time.deltaTime);
    }

    /// <summary>
    /// Fait tourner le personnage dans la direction du mouvement
    /// </summary>
    private void RotateCharacter()
    {
        if (!isMoving) return;

        // Direction de rotation
        Vector3 lookDirection = new Vector3(moveInput.x, 0f, moveInput.y);

        // Rotation progressive (smooth)
        Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }

    #endregion

    #region Animation

    /// <summary>
    /// Met à jour les paramètres de l'Animator
    /// </summary>
    private void UpdateAnimations()
    {
        if (animator == null) return;

        // Paramètre Speed pour Blend Tree Walk/Idle
        float speed = isMoving ? 1f : 0f;
        animator.SetFloat("Speed", speed);
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Bloque ou débloque le mouvement du joueur
    /// </summary>
    public void SetCanMove(bool value)
    {
        canMove = value;

        // Si on bloque le mouvement, remettre l'animation à Idle
        if (!canMove && animator != null)
        {
            animator.SetFloat("Speed", 0f);
        }
    }

    /// <summary>
    /// Téléporte le joueur à une position
    /// </summary>
    public void TeleportTo(Vector3 position)
    {
        characterController.enabled = false;
        transform.position = position;
        characterController.enabled = true;
    }

    #endregion

    #region Debug (à supprimer plus tard)

    private void OnDrawGizmos()
    {
        // Visualiser la direction du mouvement en mode debug
        if (isMoving && Application.isPlaying)
        {
            Gizmos.color = Color.green;
            Vector3 moveDir = new Vector3(moveInput.x, 0f, moveInput.y).normalized;
            Gizmos.DrawLine(transform.position, transform.position + moveDir * 2f);
        }
    }

    #endregion
}
