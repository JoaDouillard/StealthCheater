using UnityEngine;


// Structure pour definir un prefab avec son pourcentage d'apparition
[System.Serializable]
public class PlantePrefabAvecPourcentage
{
    [Tooltip("Le prefab de la plante")]
    public GameObject prefab;

    [Tooltip("Pourcentage d'apparition (0-100)")]
    [Range(0f, 100f)]
    public float pourcentage = 25f;
}

public class PlantSpawner : MonoBehaviour
{
    [Header("Prefabs de plantes")]
    [Tooltip("Liste des prefabs de plantes avec leur pourcentage d'apparition")]
    public PlantePrefabAvecPourcentage[] plantePrefabs;

    [Header("Configuration de la zone")]
    [Tooltip("Nombre total de plantes a spawner")]
    public int nombrePlantes = 400;

    [Tooltip("Taille de la zone de spawn (X et Z)")]
    public Vector2 tailleZone = new Vector2(350, 350);

    [Tooltip("Hauteur du sol par defaut")]
    public float hauteurSol = 2.6f;

    [Header("Zone degagee au centre")]
    [Tooltip("Rayon du cercle degage au centre (pas de plantes)")]
    public float rayonZoneDegagee = 50f;

    [Tooltip("Rayon de transition (de clairseme a dense)")]
    public float rayonTransition = 100f;

    [Tooltip("Courbe de densite (0 = centre, 1 = bord)")]
    public AnimationCurve courbeDensite;

    [Header("Eviter les obstacles")]
    [Tooltip("Rayon de zone interdite autour des obstacles (10m par défaut)")]
    public float rayonZoneInterdite = 10f;

    [Tooltip("Layer(s) des obstacles à éviter (configurables)")]
    public LayerMask layerObstacle;

    [Tooltip("Distance minimale entre plantes")]
    public float distanceMinEntrePlantes = 3f;

    [Tooltip("Layer des objets crees")]
    public LayerMask layerCreate;

    [Header("Variation")]
    [Tooltip("Scale minimum des plantes")]
    public float scaleMin = 0.8f;

    [Tooltip("Scale maximum des plantes")]
    public float scaleMax = 1.5f;

    [Tooltip("Variation de scale selon la distance (plus petit au centre)")]
    public bool variationScaleParDistance = true;

    [Header("Clustering (groupes naturels)")]
    [Tooltip("Activer le clustering pour effet foret naturelle")]
    public bool activerClustering = true;

    [Tooltip("Nombre de centres de clusters")]
    public int nombreClusters = 10;

    [Tooltip("Rayon des clusters")]
    public float rayonCluster = 20f;

    [Tooltip("Probabilite de spawn dans un cluster (0-1)")]
    public float probabiliteCluster = 0.7f;

    [Header("Debug")]
    [Tooltip("Afficher les informations de debug")]
    public bool afficherDebug = true;

    [Tooltip("Afficher les gizmos de zones")]
    public bool afficherGizmos = true;

    private Vector3[] centresClusters;

    void Start()
    {
        // Configurer la courbe de densite par defaut si necessaire
        if (courbeDensite == null || courbeDensite.length == 0)
        {
            ConfigurerCourbeDensiteParDefaut();
        }

        // Generer les centres de clusters
        if (activerClustering)
        {
            GenererCentresClusters();
        }

        // Spawner les plantes
        SpawnerPlantes();
    }

    // Configure une courbe de densite par defaut
    void ConfigurerCourbeDensiteParDefaut()
    {
        courbeDensite = new AnimationCurve();
        courbeDensite.AddKey(0f, 0f);    // Centre = 0% de densite
        courbeDensite.AddKey(0.3f, 0.2f); // Transition debut
        courbeDensite.AddKey(0.6f, 0.6f); // Zone intermediaire
        courbeDensite.AddKey(0.8f, 0.9f); // Foret dense commence
        courbeDensite.AddKey(1f, 1f);     // Bord = 100% de densite
    }

    // Genere les centres des clusters pour distribution naturelle
    void GenererCentresClusters()
    {
        centresClusters = new Vector3[nombreClusters];

        for (int i = 0; i < nombreClusters; i++)
        {
            // Position aleatoire mais pas trop proche du centre
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float distance = Random.Range(rayonZoneDegagee + 20f, tailleZone.x / 2f - 20f);

            float x = Mathf.Cos(angle) * distance;
            float z = Mathf.Sin(angle) * distance;

            centresClusters[i] = new Vector3(x, 0f, z);
        }

        if (afficherDebug)
        {
            Debug.Log($"[PlantSpawner] {nombreClusters} centres de clusters generes");
        }
    }


    // Choisit un prefab de plante selon les pourcentages definis
    GameObject ChoisirPrefabSelonPourcentage()
    {
        // Calculer le total des pourcentages
        float totalPourcentage = 0f;
        foreach (var plantePrefab in plantePrefabs)
        {
            if (plantePrefab.prefab != null)
            {
                totalPourcentage += plantePrefab.pourcentage;
            }
        }

        // Si aucun pourcentage defini, choisir aleatoirement
        if (totalPourcentage <= 0f)
        {
            int randomIndex = Random.Range(0, plantePrefabs.Length);
            return plantePrefabs[randomIndex].prefab;
        }

        // Generer un nombre aleatoire entre 0 et le total
        float randomValue = Random.Range(0f, totalPourcentage);

        // Trouver le prefab correspondant
        float cumulatif = 0f;
        foreach (var plantePrefab in plantePrefabs)
        {
            if (plantePrefab.prefab != null)
            {
                cumulatif += plantePrefab.pourcentage;
                if (randomValue <= cumulatif)
                {
                    return plantePrefab.prefab;
                }
            }
        }

        // Fallback: retourner le premier prefab valide
        foreach (var plantePrefab in plantePrefabs)
        {
            if (plantePrefab.prefab != null)
            {
                return plantePrefab.prefab;
            }
        }

        return null;
    }

    // Spawner toutes les plantes avec distribution naturelle
    void SpawnerPlantes()
    {
        if (plantePrefabs == null || plantePrefabs.Length == 0)
        {
            Debug.LogWarning("[PlantSpawner] Aucun prefab de plante assigne !");
            return;
        }

        Debug.Log($"[PlantSpawner] DEMARRAGE - {plantePrefabs.Length} prefabs disponibles");
        Debug.Log($"[PlantSpawner] Zone: {tailleZone}, Rayon degage: {rayonZoneDegagee}");
        Debug.Log($"[PlantSpawner] LayerObstacle: {layerObstacle.value}, LayerCreate: {layerCreate.value}");

        // Afficher les pourcentages configures
        if (afficherDebug)
        {
            float totalPourcentage = 0f;
            for (int i = 0; i < plantePrefabs.Length; i++)
            {
                if (plantePrefabs[i].prefab != null)
                {
                    totalPourcentage += plantePrefabs[i].pourcentage;
                    Debug.Log($"[PlantSpawner] Prefab {i} ({plantePrefabs[i].prefab.name}): {plantePrefabs[i].pourcentage}%");
                }
            }
            Debug.Log($"[PlantSpawner] Total des pourcentages: {totalPourcentage}%");
        }

        int spawned = 0;
        int tentatives = 0;
        int maxTentatives = nombrePlantes * 50;
        int rejetZoneDegagee = 0;
        int rejetObstacle = 0;
        int rejetDistancePlantes = 0;

        while (spawned < nombrePlantes && tentatives < maxTentatives)
        {
            tentatives++;

            // Generer une position
            Vector3 position = GenererPositionNaturelle();

            // Verifier si la position est valide (avec compteurs)
            float distanceCentre = new Vector2(position.x, position.z).magnitude;
            if (distanceCentre < rayonZoneDegagee)
            {
                rejetZoneDegagee++;
                continue;
            }

            // Essayer de trouver une position valide proche si obstacle détecté
            if (!TrouverPositionValide(ref position, ref rejetObstacle, ref rejetDistancePlantes))
            {
                continue; // Aucune position valide trouvée après tentatives
            }

            // Ajuster la hauteur avec raycast
            position = AjusterHauteurAvecRaycast(position);

            // Choisir un prefab selon les pourcentages
            GameObject prefab = ChoisirPrefabSelonPourcentage();

            if (prefab == null)
            {
                Debug.LogWarning("[PlantSpawner] Aucun prefab valide trouve !");
                continue;
            }

            // Spawner la plante
            GameObject plante = Instantiate(prefab, position, Quaternion.identity, transform);

            // Assigner le layer configuré (trouver le premier layer actif dans layerCreate)
            int layerToAssign = GetFirstLayerFromMask(layerCreate);
            if (layerToAssign >= 0)
            {
                plante.layer = layerToAssign;
            }

            // Rotation aleatoire sur Y
            plante.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);

            // Scale variable
            float scale = CalculerScale(position);
            plante.transform.localScale = Vector3.one * scale;

            spawned++;
        }

        if (afficherDebug)
        {
            float taux = (float)spawned / tentatives * 100f;
            Debug.Log($"[PlantSpawner] RESULTAT: {spawned}/{nombrePlantes} plantes spawnees ({tentatives} tentatives, taux: {taux:F1}%)");
            Debug.Log($"[PlantSpawner] Rejets: Zone degagee={rejetZoneDegagee}, Obstacle={rejetObstacle}, Distance={rejetDistancePlantes}");
        }
    }

    // Genere une position naturelle basee sur la densite et les clusters
    Vector3 GenererPositionNaturelle()
    {
        Vector3 position;

        // Decide si on utilise un cluster ou une position aleatoire
        if (activerClustering && centresClusters != null && Random.value < probabiliteCluster)
        {
            // Position dans un cluster
            Vector3 centreCluster = centresClusters[Random.Range(0, centresClusters.Length)];
            Vector2 offset = Random.insideUnitCircle * rayonCluster;
            position = centreCluster + new Vector3(offset.x, 0f, offset.y);
        }
        else
        {
            // Position aleatoire ponderee par la densite
            position = GenererPositionPonderee();
        }

        return position;
    }


    // Genere une position ponderee par la courbe de densite
    Vector3 GenererPositionPonderee()
    {
        int maxTentatives = 20;
        for (int i = 0; i < maxTentatives; i++)
        {
            // Position aleatoire dans toute la zone
            float x = Random.Range(-tailleZone.x / 2, tailleZone.x / 2);
            float z = Random.Range(-tailleZone.y / 2, tailleZone.y / 2);
            Vector3 pos = new Vector3(x, 0f, z);

            // Calculer la distance au centre (normalise 0-1)
            float distanceCentre = new Vector2(x, z).magnitude;
            float distanceMax = tailleZone.x / 2f;
            float pourcentageDistance = Mathf.Clamp01(distanceCentre / distanceMax);

            // Obtenir la densite cible depuis la courbe
            float densiteCible = courbeDensite.Evaluate(pourcentageDistance);

            // Accepter ou rejeter selon la densite
            if (Random.value < densiteCible)
            {
                return pos;
            }
        }

        // Fallback: position aleatoire simple
        return new Vector3(
            Random.Range(-tailleZone.x / 2, tailleZone.x / 2),
            0f,
            Random.Range(-tailleZone.y / 2, tailleZone.y / 2)
        );
    }


    // Ajuste la hauteur avec un raycast vers le sol
    Vector3 AjusterHauteurAvecRaycast(Vector3 position)
    {
        Vector3 positionHaute = position + Vector3.up * 100f;

        RaycastHit hit;
        if (Physics.Raycast(positionHaute, Vector3.down, out hit, 200f))
        {
            position.y = hit.point.y;
        }
        else
        {
            position.y = hauteurSol;
        }

        return position;
    }


    // Calcule le scale selon la distance au centre
    float CalculerScale(Vector3 position)
    {
        if (!variationScaleParDistance)
        {
            return Random.Range(scaleMin, scaleMax);
        }

        // Plus petit au centre, plus grand aux bords
        float distanceCentre = new Vector2(position.x, position.z).magnitude;
        float distanceMax = tailleZone.x / 2f;
        float pourcentage = Mathf.Clamp01(distanceCentre / distanceMax);

        // Scale augmente avec la distance
        float scaleBase = Mathf.Lerp(scaleMin, scaleMax, pourcentage);

        // Ajouter une variation aleatoire
        float variation = Random.Range(-0.2f, 0.2f);

        return Mathf.Clamp(scaleBase + variation, scaleMin, scaleMax);
    }


    // Regenerer toutes les plantes (pour tests)
    public void RegenerPlantes()
    {
        // Detruire toutes les plantes existantes
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        // Regenerer les clusters
        if (activerClustering)
        {
            GenererCentresClusters();
        }

        // Respawner
        SpawnerPlantes();
    }

    /// <summary>
    /// Trouve une position valide en générant de nouvelles positions aléatoires
    /// Essaie 4 fois de trouver une position libre, sinon abandonne
    /// VERIFIE: zone dégagée, obstacles (10m), distance autres plantes
    /// IMPORTANT : Zone interdite de 10m autour de TOUS les obstacles
    /// </summary>
    bool TrouverPositionValide(ref Vector3 position, ref int rejetObstacle, ref int rejetDistancePlantes)
    {
        int maxTentativesRepositionnement = 4;

        for (int i = 0; i < maxTentativesRepositionnement; i++)
        {
            // Position actuelle (tentative 0) ou nouvelle position aléatoire (tentatives 1+)
            Vector3 positionTest = position;

            if (i > 0)
            {
                // Générer une NOUVELLE position complètement aléatoire dans toute la zone
                positionTest = GenererPositionNaturelle();
            }

            // 1. Vérifier zone dégagée (centre)
            float distanceCentre = new Vector2(positionTest.x, positionTest.z).magnitude;
            if (distanceCentre < rayonZoneDegagee)
            {
                continue; // Trop proche du centre
            }

            // 2. Vérifier OBSTACLES dans un rayon de 10m (ZONE INTERDITE ABSOLUE)
            // Si un obstacle est trouvé dans ce rayon → position invalide
            if (Physics.CheckSphere(positionTest, rayonZoneInterdite, layerObstacle))
            {
                if (i == 0) rejetObstacle++; // Compter seulement pour position initiale
                continue; // Position dans zone interdite, passer à la suivante
            }

            // 3. Vérifier distance avec autres plantes
            if (Physics.CheckSphere(positionTest, distanceMinEntrePlantes, layerCreate))
            {
                if (i == 0) rejetDistancePlantes++; // Compter seulement pour position initiale
                continue;
            }

            // Position valide trouvée !
            position = positionTest;
            return true;
        }

        // Aucune position valide trouvée après 4 tentatives → ne pas spawner
        return false;
    }

    /// <summary>
    /// Extrait le premier layer actif d'un LayerMask
    /// </summary>
    int GetFirstLayerFromMask(LayerMask mask)
    {
        // Parcourir tous les layers possibles (0-31)
        for (int i = 0; i < 32; i++)
        {
            // Vérifier si ce layer est inclus dans le mask
            if ((mask.value & (1 << i)) != 0)
            {
                return i;
            }
        }

        // Aucun layer trouvé, retourner -1
        return -1;
    }

    // Visualisation dans l'editeur
    void OnDrawGizmos()
    {
        if (!afficherGizmos) return;

        // Zone totale
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, new Vector3(tailleZone.x, 0.5f, tailleZone.y));

        // Zone degagee (cercle central)
        Gizmos.color = Color.red;
        DrawCircle(transform.position, rayonZoneDegagee, 32);

        // Zone de transition
        Gizmos.color = Color.yellow;
        DrawCircle(transform.position, rayonTransition, 32);

        // Centres de clusters (si actives)
        if (activerClustering && centresClusters != null)
        {
            Gizmos.color = Color.cyan;
            foreach (Vector3 centre in centresClusters)
            {
                Gizmos.DrawWireSphere(transform.position + centre, rayonCluster);
            }
        }
    }

    // Dessine un cercle pour les gizmos
    void DrawCircle(Vector3 center, float radius, int segments)
    {
        float angle = 0f;
        float angleStep = 360f / segments;
        Vector3 lastPoint = center + new Vector3(Mathf.Cos(0) * radius, 0f, Mathf.Sin(0) * radius);

        for (int i = 1; i <= segments; i++)
        {
            angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 newPoint = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            Gizmos.DrawLine(lastPoint, newPoint);
            lastPoint = newPoint;
        }
    }

    void OnApplicationQuit()
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
    }
}
