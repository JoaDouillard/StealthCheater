using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float crouchSpeedMultiplier = 0.5f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float gravity = -9.81f;

    [Header("Crouch Settings")]
    [SerializeField] private float crouchHeightMultiplier = 0.6f;

    [Header("Camera Reference (Auto-found)")]
    [SerializeField] private CameraFollow cameraFollow;

    private CharacterController characterController;
    private InputSystem_Actions inputActions;
    private Animator animator;

    private Vector2 moveInput;
    private bool isMoving;
    private bool canMove = true;
    private bool isCrouching = false;
    private Vector3 velocity;

    private float normalHeight;
    private float crouchHeight;
    private Vector3 normalCenter;
    private Vector3 crouchCenter;

    private float currentSpeed = 0f;
    private float speedVelocity = 0f;
    private float speedSmoothTime = 0.1f;
    private float currentCrouch = 0f;
    private float crouchVelocity = 0f;
    private float crouchSmoothTime = 0.15f;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();

        inputActions = new InputSystem_Actions();

        // Trouver CameraFollow automatiquement
        if (cameraFollow == null)
        {
            cameraFollow = FindFirstObjectByType<CameraFollow>();
        }

        // Stocker hauteurs normales et accroupies
        normalHeight = characterController.height;
        crouchHeight = normalHeight * crouchHeightMultiplier;
        normalCenter = characterController.center;
        crouchCenter = new Vector3(normalCenter.x, normalCenter.y * crouchHeightMultiplier, normalCenter.z);
    }

    private void OnEnable()
    {
        inputActions.Player.Enable();
        inputActions.Player.Crouch.performed += _ => ToggleCrouch();
    }

    private void OnDisable()
    {
        inputActions.Player.Crouch.performed -= _ => ToggleCrouch();
        inputActions.Player.Disable();
    }

    private void Update()
    {
        ApplyGravity();

        if (!canMove) return;

        ReadInput();
        MoveCharacter();
        RotateCharacter();
        UpdateAnimations();
    }

    private void ReadInput()
    {
        moveInput = inputActions.Player.Move.ReadValue<Vector2>();
        isMoving = moveInput.magnitude > 0.1f;
    }

    private void MoveCharacter()
    {
        if (!isMoving) return;

        Vector3 moveDirection;

        // En First Person, mouvement relatif à la caméra
        if (cameraFollow != null && cameraFollow.GetCameraMode() == CameraMode.FirstPerson)
        {
            // Récupérer la direction de la caméra (projection sur le plan horizontal)
            Transform cameraTransform = cameraFollow.transform;
            Vector3 forward = cameraTransform.forward;
            Vector3 right = cameraTransform.right;

            // Projeter sur le plan XZ (horizontal)
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            // WASD relatif à la caméra
            // moveInput.y : W(1)/S(-1) → forward/backward
            // moveInput.x : D(1)/A(-1) → right/left
            moveDirection = forward * moveInput.y + right * moveInput.x;
        }
        else
        {
            // Third Person : mouvement absolu (comme avant)
            moveDirection = new Vector3(-moveInput.x, 0f, -moveInput.y);
        }

        moveDirection.Normalize();

        // Vitesse réduite si accroupi
        float currentSpeed = isCrouching ? moveSpeed * crouchSpeedMultiplier : moveSpeed;

        characterController.Move(moveDirection * currentSpeed * Time.deltaTime);
    }

    private void ApplyGravity()
    {
        if (characterController.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        velocity.y += gravity * Time.deltaTime;
        characterController.Move(velocity * Time.deltaTime);
    }

    private void RotateCharacter()
    {
        // En First Person, le personnage suit la rotation Y de la caméra
        if (cameraFollow != null && cameraFollow.GetCameraMode() == CameraMode.FirstPerson)
        {
            // Suivre uniquement la rotation horizontale (Y) de la caméra
            Vector3 cameraEuler = cameraFollow.transform.eulerAngles;
            Quaternion targetRotation = Quaternion.Euler(0f, cameraEuler.y, 0f);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime * 2f // Plus rapide en FP
            );
        }
        else
        {
            // Third Person : rotation vers la direction du mouvement (comme avant)
            if (!isMoving) return;

            Vector3 lookDirection = new Vector3(-moveInput.x, 0f, -moveInput.y);

            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }
    }

    private void UpdateAnimations()
    {
        if (animator == null) return;

        // Smooth transitions légères
        float targetSpeed = isMoving ? 1f : 0f;
        float targetCrouch = isCrouching ? 1f : 0f;

        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * 10f);
        currentCrouch = Mathf.Lerp(currentCrouch, targetCrouch, Time.deltaTime * 8f);

        // Snap quand très proche
        if (Mathf.Abs(currentSpeed - targetSpeed) < 0.05f)
            currentSpeed = targetSpeed;
        if (Mathf.Abs(currentCrouch - targetCrouch) < 0.05f)
            currentCrouch = targetCrouch;

        animator.SetFloat("Speed", currentSpeed);
        animator.SetFloat("IsCrouching", currentCrouch);
    }

    private void ToggleCrouch()
    {
        isCrouching = !isCrouching;

        if (isCrouching)
        {
            // Réduire hauteur et centre
            characterController.height = crouchHeight;
            characterController.center = crouchCenter;
        }
        else
        {
            // Vérifier qu'il y a de la place pour se relever
            if (CanStandUp())
            {
                characterController.height = normalHeight;
                characterController.center = normalCenter;
            }
            else
            {
                // Pas assez de place, rester accroupi
                isCrouching = true;
            }
        }
    }

    private bool CanStandUp()
    {
        // Raycast vers le haut pour vérifier s'il y a un obstacle
        float checkDistance = normalHeight - crouchHeight + 0.1f;
        Vector3 rayOrigin = transform.position + Vector3.up * crouchHeight;

        return !Physics.Raycast(rayOrigin, Vector3.up, checkDistance);
    }

    public void SetCanMove(bool value)
    {
        canMove = value;

        if (!canMove && animator != null)
        {
            animator.SetFloat("Speed", 0f);
        }
    }

    public void TeleportTo(Vector3 position)
    {
        characterController.enabled = false;
        transform.position = position;
        characterController.enabled = true;
    }

    public bool IsCrouching() => isCrouching;

    public void PlayDefeatAnimation()
    {
        if (animator == null) return;
        animator.SetTrigger("Defeat");
    }

    private void OnDrawGizmos()
    {
        if (isMoving && Application.isPlaying)
        {
            Gizmos.color = Color.green;
            Vector3 moveDir = new Vector3(-moveInput.x, 0f, -moveInput.y).normalized;
            Gizmos.DrawLine(transform.position, transform.position + moveDir * 2f);
        }
    }
}
