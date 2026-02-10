using UnityEngine;

/// <summary>
/// Gère le cycle jour/nuit pendant l'examen
/// Heure aléatoire de départ (8h-16h), progression réaliste du soleil
/// </summary>
public class DayNightManager : MonoBehaviour
{
    public static DayNightManager Instance { get; private set; }

    [Header("Sun Settings")]
    [Tooltip("Directional Light représentant le soleil")]
    [SerializeField] private Light sunLight;

    [Header("Skybox Settings")]
    [Tooltip("Skybox pour le matin (5h-9h)")]
    [SerializeField] private Material skyboxMorning;

    [Tooltip("Skybox pour la fin de matinée (9h-12h)")]
    [SerializeField] private Material skyboxLateMorning;

    [Tooltip("Skybox pour le midi/après-midi (12h-16h)")]
    [SerializeField] private Material skyboxAfternoon;

    [Tooltip("Skybox pour le soir (16h-18h)")]
    [SerializeField] private Material skyboxEvening;

    [Header("Time Settings")]
    [Tooltip("Heure minimum de début (8h par défaut)")]
    [SerializeField] private float startHourMin = 8f;

    [Tooltip("Heure maximum de début (16h par défaut)")]
    [SerializeField] private float startHourMax = 16f;

    [Tooltip("Vitesse d'écoulement du temps (1 = temps réel, 2 = 2x plus rapide)")]
    [SerializeField] private float timeSpeed = 1f;

    [Header("Sun Rotation")]
    [Tooltip("Angle du soleil à 6h (lever) - Axe X")]
    [SerializeField] private float sunriseAngle = 0f;

    [Tooltip("Angle du soleil à 12h (midi) - Axe X")]
    [SerializeField] private float noonAngle = 90f;

    [Tooltip("Angle du soleil à 18h (coucher) - Axe X")]
    [SerializeField] private float sunsetAngle = 180f;

    [Header("Debug")]
    [Tooltip("Afficher l'heure actuelle dans la console (désactivé par défaut)")]
    [SerializeField] private bool showDebug = false;

    // État actuel
    private float currentHour = 12f; // Heure actuelle (format 24h)
    private bool isTimeRunning = false;
    private Material currentSkybox = null;

    private void Awake()
    {
        // Singleton
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // Vérifier que sunLight est assigné
        if (sunLight == null)
        {
            sunLight = FindFirstObjectByType<Light>();

            if (sunLight != null && sunLight.type != LightType.Directional)
            {
                Debug.LogWarning("[DayNightManager] La lumière trouvée n'est pas une Directional Light!");
                sunLight = null;
            }
        }

        if (sunLight == null)
        {
            Debug.LogError("[DayNightManager] ❌ Aucune Directional Light assignée ou trouvée!");
            return;
        }

        // Initialiser avec une heure aléatoire
        InitializeRandomTime();
    }

    private void Update()
    {
        if (!isTimeRunning) return;

        // Faire progresser le temps
        float deltaHours = (Time.deltaTime * timeSpeed) / 3600f; // Convertir secondes en heures
        currentHour += deltaHours;

        // Limiter à 24h (recommencer à 0)
        if (currentHour >= 24f)
        {
            currentHour -= 24f;
        }

        // Mettre à jour la position du soleil
        UpdateSunRotation();

        // Mettre à jour le skybox selon l'heure
        UpdateSkybox();

        // Debug
        if (showDebug && Time.frameCount % 60 == 0) // Afficher toutes les 60 frames
        {
            Debug.Log($"[DayNightManager] Heure actuelle: {GetFormattedTime()}");
        }
    }

    /// <summary>
    /// Initialise une heure aléatoire entre startHourMin et startHourMax
    /// </summary>
    private void InitializeRandomTime()
    {
        currentHour = Random.Range(startHourMin, startHourMax);

        if (showDebug)
        {
            Debug.Log($"[DayNightManager] ☀️ Heure de début: {GetFormattedTime()}");
        }

        // Mettre à jour immédiatement la rotation du soleil et le skybox
        UpdateSunRotation();
        UpdateSkybox();

        // Démarrer le temps
        isTimeRunning = true;
    }

    /// <summary>
    /// Met à jour la rotation du soleil en fonction de l'heure actuelle
    /// </summary>
    private void UpdateSunRotation()
    {
        if (sunLight == null) return;

        // Calculer l'angle du soleil en fonction de l'heure (6h-18h = jour)
        // 6h = 0°, 12h = 90°, 18h = 180°
        float normalizedTime = Mathf.Clamp01((currentHour - 6f) / 12f); // 0 = 6h, 1 = 18h

        // Interpoler l'angle entre lever et coucher
        float sunAngle = Mathf.Lerp(sunriseAngle, sunsetAngle, normalizedTime);

        // Appliquer la rotation (X = angle vertical du soleil)
        sunLight.transform.rotation = Quaternion.Euler(sunAngle, 170f, 0f);
        // Y = 170° pour orienter le soleil vers le sud-ouest (ajustable selon la scène)
    }

    /// <summary>
    /// Met à jour le skybox en fonction de l'heure actuelle
    /// Matin (5h-9h), Fin matinée (9h-12h), Après-midi (12h-16h), Soir (16h-18h)
    /// </summary>
    private void UpdateSkybox()
    {
        Material targetSkybox = null;

        // Déterminer quel skybox utiliser selon l'heure
        if (currentHour >= 5f && currentHour < 9f)
        {
            targetSkybox = skyboxMorning; // Matin
        }
        else if (currentHour >= 9f && currentHour < 12f)
        {
            targetSkybox = skyboxLateMorning; // Fin de matinée
        }
        else if (currentHour >= 12f && currentHour < 16f)
        {
            targetSkybox = skyboxAfternoon; // Après-midi
        }
        else if (currentHour >= 16f && currentHour < 18f)
        {
            targetSkybox = skyboxEvening; // Soir
        }
        else
        {
            // Heure hors plage (nuit) - utiliser après-midi par défaut
            targetSkybox = skyboxAfternoon;
        }

        // Changer le skybox seulement si différent (évite changements inutiles)
        if (targetSkybox != null && targetSkybox != currentSkybox)
        {
            RenderSettings.skybox = targetSkybox;
            currentSkybox = targetSkybox;
            DynamicGI.UpdateEnvironment(); // Mettre à jour l'éclairage ambiant
        }
    }

    /// <summary>
    /// Démarre le cycle jour/nuit
    /// </summary>
    public void StartTime()
    {
        isTimeRunning = true;
    }

    /// <summary>
    /// Met en pause le cycle jour/nuit
    /// </summary>
    public void StopTime()
    {
        isTimeRunning = false;
    }

    /// <summary>
    /// Définit une heure spécifique
    /// </summary>
    public void SetTime(float hour)
    {
        currentHour = Mathf.Clamp(hour, 0f, 24f);
        UpdateSunRotation();
    }

    /// <summary>
    /// Obtient l'heure actuelle (format 24h)
    /// </summary>
    public float GetCurrentHour()
    {
        return currentHour;
    }

    /// <summary>
    /// Obtient l'heure formatée (HH:MM)
    /// </summary>
    public string GetFormattedTime()
    {
        int hours = Mathf.FloorToInt(currentHour);
        int minutes = Mathf.FloorToInt((currentHour - hours) * 60f);

        return $"{hours:D2}:{minutes:D2}";
    }

    /// <summary>
    /// Vérifie si c'est la nuit (avant 6h ou après 18h)
    /// </summary>
    public bool IsNight()
    {
        return currentHour < 6f || currentHour >= 18f;
    }

    /// <summary>
    /// Vérifie si c'est le jour (entre 6h et 18h)
    /// </summary>
    public bool IsDay()
    {
        return !IsNight();
    }
}
