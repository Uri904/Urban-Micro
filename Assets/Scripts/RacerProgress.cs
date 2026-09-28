using UnityEngine;

/// <summary>
/// Va en el prefab de cada auto (jugador o IA). Mide cuántos metros de la ruta lleva.
/// </summary>
public class RacerProgress : MonoBehaviour
{
    [Header("Identidad")]
    public string racerName = "Corredor";
    [Tooltip("Márcalo en la instancia del jugador (no en el prefab).")]
    public bool isPlayer = false;

    [Header("Ruta (si se deja vacía, la busca sola en la escena)")]
    public RouteWaypoints route;

    /// <summary>Metros recorridos sobre la ruta.</summary>
    public float Distance { get; private set; }
    public bool IsEliminated { get; private set; }
    /// <summary>Lugar actual entre los corredores vivos (lo calcula RaceTracker).</summary>
    public int Position { get; set; }

    private int segmentHint = -1;

    void Start()
    {
        if (route == null)
            route = FindFirstObjectByType<RouteWaypoints>();

        if (RaceTracker.Instance != null)
            RaceTracker.Instance.Register(this);
    }

    void OnDestroy()
    {
        if (RaceTracker.Instance != null)
            RaceTracker.Instance.Unregister(this);
    }

    void Update()
    {
        if (IsEliminated || route == null) return;
        Distance = route.ProjectOnRoute(transform.position, ref segmentHint);
    }

    /// <summary>Congela el progreso (se usará en el modo eliminación).</summary>
    public void Eliminate()
    {
        IsEliminated = true;
    }

    /// <summary>Llamar después de teletransportar el auto (reinicio, respawn).</summary>
    public void ResetProgressTracking()
    {
        segmentHint = -1;
    }
}
