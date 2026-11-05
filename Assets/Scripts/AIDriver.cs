using UnityEngine;

/// <summary>
/// Piloto de IA. Va en el MISMO GameObject que CarController y RacerProgress.
/// Sigue un punto "por delante" en la ruta (lookAheadDistance) y dirige el
/// auto hacia él, bajando el acelerador en curvas cerradas.
///
/// Cada auto tiene una "Personalidad" que define cómo maneja: qué tan rápido
/// entra a las curvas, qué tanto se pega al centro de la ruta, y si cambia
/// de carril solo. Con "Aleatoria" el propio script elige una al iniciar.
/// </summary>
[RequireComponent(typeof(CarController))]
[RequireComponent(typeof(RacerProgress))]
public class AIDriver : MonoBehaviour, IVehicleDriver
{
    public enum Personalidad { Equilibrada, Agresiva, Cautelosa, Erratica, Aleatoria }

    [Header("Personalidad")]
    public Personalidad personalidad = Personalidad.Aleatoria;
    [Tooltip("Si está activo, la Personalidad sobreescribe los valores de abajo al iniciar. Desactívalo para ajustar todo a mano.")]
    public bool personalidadControlaValores = true;

    [Header("Seguimiento de ruta")]
    [Tooltip("Metros por delante del auto que apunta como objetivo. Más alto = curvas más suaves; más bajo = más preciso pero brusco.")]
    public float lookAheadDistance = 12f;
    [Tooltip("Qué tan a la izquierda (negativo) o derecha (positivo) del centro de la ruta maneja este auto. Lo asigna el spawner para que no vayan todos en fila.")]
    public float laneOffset = 0f;

    [Header("Manejo")]
    [Range(0f, 1f)] public float maxThrottle = 1f;
    [Tooltip("Acelerador mínimo incluso en la curva más cerrada, para que no se quede parado.")]
    [Range(0f, 1f)] public float minThrottle = 0.35f;
    [Tooltip("Qué tan brusco tiene que ser el volantazo (0-1) para frenar el acelerador al mínimo.")]
    public float sharpTurnThreshold = 0.6f;

    [Header("Variación (ruido en el acelerador)")]
    [Range(0f, 0.3f)] public float throttleJitter = 0.1f;

    [Header("Cambios de carril (solo se activa solo si es Errática)")]
    [Tooltip("Qué tan lejos del centro puede irse al cambiar de carril.")]
    public float laneWanderRange = 3f;
    [Tooltip("Cada cuántos segundos, en promedio, decide cambiar de carril.")]
    public Vector2 laneChangeIntervalRange = new Vector2(2.5f, 5f);
    [Tooltip("Qué tan rápido se mueve hacia el nuevo carril (más alto = cambio más brusco).")]
    public float laneChangeSpeed = 1.5f;

    private RacerProgress progress;
    private RouteWaypoints route;
    private float jitterSeed;

    private bool wandersLanes;
    private float laneTarget;
    private float laneChangeTimer;

    void Awake()
    {
        progress = GetComponent<RacerProgress>();
        jitterSeed = Random.Range(0f, 100f);
    }

    void Start()
    {
        route = progress.route != null ? progress.route : FindFirstObjectByType<RouteWaypoints>();

        // Se resuelve aquí (no en Awake) porque el spawner puede asignar la
        // Personalidad justo después de AddComponent(), y para entonces Awake
        // ya habría corrido con el valor por defecto.
        if (personalidad == Personalidad.Aleatoria)
            personalidad = (Personalidad)Random.Range(0, 4); // evita volver a elegir Aleatoria

        if (personalidadControlaValores)
            AplicarPersonalidad();

        wandersLanes = personalidad == Personalidad.Erratica;
        laneTarget = laneOffset;
        laneChangeTimer = Random.Range(laneChangeIntervalRange.x, laneChangeIntervalRange.y);
    }

    /// <summary>Configura los parámetros de manejo según el arquetipo elegido.</summary>
    private void AplicarPersonalidad()
    {
        switch (personalidad)
        {
            case Personalidad.Agresiva:
                // Frena poco, mira poco adelante, entra rápido a las curvas.
                lookAheadDistance = Random.Range(8f, 10f);
                maxThrottle = 1f;
                minThrottle = Random.Range(0.5f, 0.6f);
                sharpTurnThreshold = Random.Range(0.7f, 0.8f);
                throttleJitter = 0.06f;
                break;

            case Personalidad.Cautelosa:
                // Ve muy adelante y frena mucho antes de las curvas.
                lookAheadDistance = Random.Range(16f, 20f);
                maxThrottle = Random.Range(0.85f, 0.95f);
                minThrottle = Random.Range(0.2f, 0.3f);
                sharpTurnThreshold = Random.Range(0.4f, 0.5f);
                throttleJitter = 0.08f;
                break;

            case Personalidad.Erratica:
                // Manejo promedio, pero cambia de carril solo (ver GetInput).
                lookAheadDistance = Random.Range(10f, 14f);
                maxThrottle = Random.Range(0.9f, 1f);
                minThrottle = Random.Range(0.35f, 0.45f);
                sharpTurnThreshold = Random.Range(0.55f, 0.65f);
                throttleJitter = 0.15f;
                break;

            case Personalidad.Equilibrada:
            default:
                lookAheadDistance = 12f;
                maxThrottle = 1f;
                minThrottle = 0.35f;
                sharpTurnThreshold = 0.6f;
                throttleJitter = 0.1f;
                break;
        }
    }

    public void GetInput(out float vertical, out float horizontal, out bool brake)
    {
        brake = false;

        if (route == null || route.PointCount < 2)
        {
            vertical = 0f;
            horizontal = 0f;
            return;
        }

        if (wandersLanes) UpdateLaneWander();

        float targetDistance = progress.Distance + lookAheadDistance;
        Vector3 basePoint = route.GetPointAtDistance(targetDistance);
        Vector3 dir = route.GetDirectionAtDistance(targetDistance);
        Vector3 right = Vector3.Cross(Vector3.up, dir).normalized;
        Vector3 targetPoint = basePoint + right * laneOffset;

        Vector3 localTarget = transform.InverseTransformPoint(targetPoint);
        float angle = Mathf.Atan2(localTarget.x, localTarget.z) * Mathf.Rad2Deg;

        horizontal = Mathf.Clamp(angle / 30f, -1f, 1f);

        float turnSharpness = Mathf.Abs(horizontal);
        float throttle = turnSharpness >= sharpTurnThreshold
            ? minThrottle
            : Mathf.Lerp(maxThrottle, minThrottle, turnSharpness / sharpTurnThreshold);

        float jitter = 1f + (Mathf.PerlinNoise(jitterSeed, Time.time * 0.2f) - 0.5f) * 2f * throttleJitter;
        vertical = Mathf.Clamp01(throttle * jitter);
    }

    /// <summary>Cada cierto tiempo elige un nuevo carril y se desliza hacia él.</summary>
    private void UpdateLaneWander()
    {
        laneChangeTimer -= Time.deltaTime;
        if (laneChangeTimer <= 0f)
        {
            laneTarget = Random.Range(-laneWanderRange, laneWanderRange);
            laneChangeTimer = Random.Range(laneChangeIntervalRange.x, laneChangeIntervalRange.y);
        }
        laneOffset = Mathf.MoveTowards(laneOffset, laneTarget, laneChangeSpeed * Time.deltaTime);
    }
}