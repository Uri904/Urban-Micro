using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Colócalo en un objeto vacío de la escena de carrera. Instancia autos rivales
/// (con IA) formados en filas detrás de la línea de salida, sobre la ruta.
/// </summary>
public class RaceGridSpawner : MonoBehaviour
{
    [Header("Ruta")]
    public RouteWaypoints route;

    [Header("Jugador")]
    [Tooltip("Arrastra tu auto (el de la escena, no el prefab) para formarlo en la línea junto con los rivales.")]
    public Transform playerTransform;

    [Header("Rivales")]
    [Tooltip("Prefabs de auto que pueden usar los rivales (Combi, Camión...). Se eligen al azar.")]
    public GameObject[] aiPrefabs;
    public int aiCount = 7;
    [Tooltip("Qué tanto se desvían del centro de la ruta al manejar, para que no vayan todos en la misma línea y se puedan rebasar.")]
    public float laneOffsetRange = 2f;
    [Tooltip("Variación de velocidad entre rivales (0.85 a 1.15 = entre 15% más lento y más rápido).")]
    public float minSpeedFactor = 0.85f;
    public float maxSpeedFactor = 1.15f;

    [Header("Formación de la parrilla")]
    public int lanesPerRow = 2;
    [Tooltip("Calcula la separación solo según el auto más grande de Ai Prefabs (recomendado). Si lo apagas, usa Row/Lane Spacing manuales.")]
    public bool autoSpacing = true;
    [Tooltip("Cuánto más grande que el auto es el 'rectángulo' que se le reserva. 1 = pegado, 1.5 = con margen cómodo.")]
    public float spacingMargin = 1.6f;
    public float rowSpacing = 9f;
    public float laneSpacing = 5f;
    [Tooltip("Qué tan atrás de la línea de salida arranca la primera fila.")]
    public float firstRowOffset = 9f;
    [Tooltip("Altura extra al aparecer, para no nacer enterrados en el piso (eso también los hace explotar).")]
    public float spawnHeight = 0.5f;
    [Tooltip("Segundos con la rotación congelada al nacer, por si se traslapan un poco entre sí.")]
    public float spawnGraceTime = 1f;

    private readonly List<RacerProgress> spawned = new List<RacerProgress>();

    void Start()
    {
        if (route == null) route = FindFirstObjectByType<RouteWaypoints>();
        if (route == null || aiPrefabs == null || aiPrefabs.Length == 0) return;

        if (autoSpacing) ComputeAutoSpacing();

        Vector3 start = route.StartPoint;
        Vector3 forward = route.StartDirection;
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        Quaternion rotation = forward.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(forward, Vector3.up) : Quaternion.identity;

        if (playerTransform != null)
        {
            playerTransform.position = start + Vector3.up * spawnHeight;
            playerTransform.rotation = rotation;
            Rigidbody playerRb = playerTransform.GetComponent<Rigidbody>();
            if (playerRb != null)
            {
                playerRb.linearVelocity = Vector3.zero;
                playerRb.angularVelocity = Vector3.zero;
            }
        }

        for (int i = 0; i < aiCount; i++)
        {
            int row = i / lanesPerRow;
            int lane = i % lanesPerRow;
            float gridLaneOffset = (lane - (lanesPerRow - 1) / 2f) * laneSpacing;
            float backOffset = firstRowOffset + row * rowSpacing;

            Vector3 pos = start - forward * backOffset + right * gridLaneOffset + Vector3.up * spawnHeight;

            GameObject prefab = aiPrefabs[Random.Range(0, aiPrefabs.Length)];
            GameObject car = Instantiate(prefab, pos, rotation);
            car.name = $"Rival_{i + 1}";

            RacerProgress rp = car.GetComponent<RacerProgress>();
            if (rp == null) rp = car.AddComponent<RacerProgress>();
            rp.isPlayer = false;
            rp.racerName = $"Rival {i + 1}";
            rp.route = route;

            AIDriver ai = car.GetComponent<AIDriver>();
            if (ai == null) ai = car.AddComponent<AIDriver>();
            // Se reparten las 4 personalidades en orden para garantizar variedad real
            // (con puro azar, en un grupo chico podrían salir todas iguales).
            ai.personalidad = (AIDriver.Personalidad)(i % 4);
            ai.laneOffset = Random.Range(-laneOffsetRange, laneOffsetRange);

            CarController cc = car.GetComponent<CarController>();
            if (cc != null)
            {
                float speedFactor = Random.Range(minSpeedFactor, maxSpeedFactor);
                cc.maxMotorTorque *= speedFactor;
            }

            Rigidbody carRb = car.GetComponent<Rigidbody>();
            if (carRb != null && spawnGraceTime > 0f)
            {
                RigidbodyConstraints original = carRb.constraints;
                carRb.constraints = original | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
                StartCoroutine(ReleaseConstraints(carRb, original, spawnGraceTime));
            }

            spawned.Add(rp);
        }
    }

    private System.Collections.IEnumerator ReleaseConstraints(Rigidbody carRb, RigidbodyConstraints original, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (carRb != null) carRb.constraints = original;
    }

    /// <summary>
    /// Mide el Box Collider de cada prefab rival y usa el más ancho/largo para
    /// separar la parrilla, así el auto más grande siempre cabe sin encimarse.
    /// </summary>
    private void ComputeAutoSpacing()
    {
        float maxWidth = 2f;
        float maxLength = 4f;

        foreach (GameObject prefab in aiPrefabs)
        {
            if (prefab == null) continue;
            BoxCollider box = prefab.GetComponent<BoxCollider>();
            if (box == null) continue;

            Vector3 size = Vector3.Scale(box.size, prefab.transform.localScale);
            maxWidth = Mathf.Max(maxWidth, size.x);
            maxLength = Mathf.Max(maxLength, size.z);
        }

        laneSpacing = maxWidth * spacingMargin;
        rowSpacing = maxLength * spacingMargin;
        firstRowOffset = Mathf.Max(firstRowOffset, maxLength * spacingMargin);
    }
}