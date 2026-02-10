using UnityEngine;

public class TeacherLookAt : MonoBehaviour
{
    [Header("Look Settings")]
    [SerializeField] private float rotationSpeed = 3f;

    private Transform teacherTransform;
    private Quaternion targetRotation;
    private bool useMovementDirection = false;
    private Vector3 movementVelocity;

    // CACHE: Students trouvés une seule fois au lieu de chercher à chaque frame (FIX MEMORY LEAK)
    private GameObject[] cachedStudents;

    public void Initialize(Transform transform)
    {
        teacherTransform = transform;
        targetRotation = transform.rotation;

        // CACHE les Students UNE SEULE FOIS à l'initialisation
        System.Collections.Generic.List<GameObject> studentsList = new System.Collections.Generic.List<GameObject>();

        // Méthode 1: Par TAG
        try
        {
            GameObject[] taggedStudents = GameObject.FindGameObjectsWithTag("Student");
            if (taggedStudents != null && taggedStudents.Length > 0)
            {
                studentsList.AddRange(taggedStudents);
                Debug.Log($"[TeacherLookAt] ✅ {taggedStudents.Length} Students trouvés par TAG");
            }
        }
        catch { }

        // Méthode 2: Par component si rien trouvé
        if (studentsList.Count == 0)
        {
            Student[] studentComponents = Object.FindObjectsByType<Student>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Student s in studentComponents)
            {
                if (s != null) studentsList.Add(s.gameObject);
            }
        }

        // Méthode 3: Recherche manuelle dans Levels_Dynamic
        if (studentsList.Count == 0)
        {
            GameObject levelsDynamic = GameObject.Find("Levels_Dynamic");
            if (levelsDynamic != null)
            {
                Transform[] allChildren = levelsDynamic.GetComponentsInChildren<Transform>(true);
                foreach (Transform t in allChildren)
                {
                    if (t.name.StartsWith("Student"))
                    {
                        studentsList.Add(t.gameObject);
                    }
                }
            }
        }

        cachedStudents = studentsList.ToArray();

        if (cachedStudents.Length == 0)
        {
            Debug.LogWarning("[TeacherLookAt] ⚠️ Aucun Student trouvé, le professeur regardera vers l'avant par défaut");
        }
        else
        {
            Debug.Log($"[TeacherLookAt] ✅ {cachedStudents.Length} Students trouvés pour LookAtClassCenter");
        }
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
        // IMPORTANT: Utiliser le CACHE pour les Students (trouvés UNE FOIS)
        // MAIS recalculer leur position moyenne à chaque appel (pour suivre les mouvements)

        if (cachedStudents == null || cachedStudents.Length == 0)
        {
            // Pas de students, regarder vers l'avant
            return teacherTransform.position + teacherTransform.forward * 5f;
        }

        Vector3 sum = Vector3.zero;
        int validStudents = 0;

        foreach (GameObject student in cachedStudents)
        {
            if (student != null) // Check null au cas où un student serait détruit
            {
                sum += student.transform.position;
                validStudents++;
            }
        }

        if (validStudents > 0)
        {
            return sum / validStudents;
        }
        else
        {
            return teacherTransform.position + teacherTransform.forward * 5f;
        }
    }
}
