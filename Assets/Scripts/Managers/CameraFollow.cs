using UnityEngine;

/// <summary>
/// Fait suivre la caméra au joueur avec un offset fixe
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target; // Le joueur à suivre

    [Header("Settings")]
    [SerializeField] private Vector3 offset = new Vector3(0, 15, -10); // Offset par défaut
    [SerializeField] private bool followX = true;
    [SerializeField] private bool followY = false; // Généralement false pour vue isométrique
    [SerializeField] private bool followZ = true;
    [SerializeField] private float smoothSpeed = 5f; // Vitesse de suivi (0 = instantané)

    [Header("Rotation")]
    [SerializeField] private bool lookAtTarget = true;

    private void Start()
    {
        // Si pas de target assignée, chercher le joueur automatiquement
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
                Debug.Log("[CameraFollow] Target trouvée automatiquement : " + player.name);
            }
            else
            {
                Debug.LogError("[CameraFollow] Aucun joueur trouvé ! Assigne le target manuellement ou tag le joueur avec 'Player'");
            }
        }

        // Positionner la caméra immédiatement au démarrage
        if (target != null)
        {
            Vector3 initialPosition = CalculateTargetPosition();
            transform.position = initialPosition;

            if (lookAtTarget)
            {
                transform.LookAt(target.position);
            }
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // Calculer la position cible
        Vector3 targetPosition = CalculateTargetPosition();

        // Smooth ou instantané
        if (smoothSpeed > 0)
        {
            transform.position = Vector3.Lerp(
                transform.position,
                targetPosition,
                smoothSpeed * Time.deltaTime
            );
        }
        else
        {
            transform.position = targetPosition;
        }

        // Regarder le joueur
        if (lookAtTarget)
        {
            transform.LookAt(target.position);
        }
    }

    /// <summary>
    /// Calcule la position cible de la caméra en fonction de l'offset et des axes suivis
    /// </summary>
    private Vector3 CalculateTargetPosition()
    {
        Vector3 targetPos = target.position + offset;

        // Appliquer les contraintes d'axes
        if (!followX)
        {
            targetPos.x = transform.position.x;
        }

        if (!followY)
        {
            targetPos.y = transform.position.y;
        }

        if (!followZ)
        {
            targetPos.z = transform.position.z;
        }

        return targetPos;
    }

    /// <summary>
    /// Permet de changer le target en cours de jeu
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    /// <summary>
    /// Visualiser l'offset dans la Scene View
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (target == null) return;

        Gizmos.color = Color.yellow;
        Vector3 targetPosition = target.position + offset;
        Gizmos.DrawLine(target.position, targetPosition);
        Gizmos.DrawWireSphere(targetPosition, 0.5f);
    }
}
