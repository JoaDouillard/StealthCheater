using UnityEngine;

public class TeacherLookAt : MonoBehaviour
{
    [Header("Look Settings")]
    [SerializeField] private float rotationSpeed = 3f;

    private Transform teacherTransform;
    private Quaternion targetRotation;
    private bool useMovementDirection = false;
    private Vector3 movementVelocity;

    public void Initialize(Transform transform)
    {
        teacherTransform = transform;
        targetRotation = transform.rotation;
    }

    private void Update()
    {
        if (teacherTransform == null) return;

        // Si on doit regarder la direction du mouvement
        if (useMovementDirection && movementVelocity.sqrMagnitude > 0.1f)
        {
            Vector3 lookDir = movementVelocity.normalized;
            lookDir.y = 0f;
            if (lookDir != Vector3.zero)
            {
                targetRotation = Quaternion.LookRotation(lookDir);
            }
        }

        // Rotation smooth
        teacherTransform.rotation = Quaternion.Slerp(
            teacherTransform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }

    public void LookInMovementDirection(Vector3 velocity)
    {
        useMovementDirection = true;
        movementVelocity = velocity;
    }

    public void LookAtPointDirection(Transform point)
    {
        useMovementDirection = false;

        if (point == null) return;

        // Utiliser la rotation du point (sa direction forward)
        targetRotation = point.rotation;
    }

    public void LookAtClassCenter()
    {
        useMovementDirection = false;

        Vector3 classCenter = GetClassCenter();
        LookAtTarget(classCenter);
    }

    public void LookAtPosition(Vector3 targetPosition)
    {
        useMovementDirection = false;
        LookAtTarget(targetPosition);
    }

    private void LookAtTarget(Vector3 targetPosition)
    {
        if (teacherTransform == null) return;

        Vector3 direction = (targetPosition - teacherTransform.position).normalized;
        direction.y = 0f;

        if (direction != Vector3.zero)
        {
            targetRotation = Quaternion.LookRotation(direction);
        }
    }

    private Vector3 GetClassCenter()
    {
        GameObject[] students = GameObject.FindGameObjectsWithTag("Student");
        if (students.Length == 0)
        {
            return teacherTransform.position + teacherTransform.forward * 5f;
        }

        Vector3 sum = Vector3.zero;
        foreach (GameObject student in students)
        {
            sum += student.transform.position;
        }

        return sum / students.Length;
    }
}
