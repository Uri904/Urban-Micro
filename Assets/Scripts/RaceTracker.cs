using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Un solo objeto por escena de carrera. Ordena a los corredores vivos por distancia
/// recorrida y les asigna su posición (1 = va ganando).
/// </summary>
public class RaceTracker : MonoBehaviour
{
    public static RaceTracker Instance { get; private set; }

    private readonly List<RacerProgress> racers = new List<RacerProgress>();
    private readonly List<RacerProgress> ranking = new List<RacerProgress>();

    public IReadOnlyList<RacerProgress> Racers => racers;
    public int TotalRacers => racers.Count;
    public int AliveCount { get; private set; }
    public RacerProgress Player { get; private set; }

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Register(RacerProgress racer)
    {
        if (!racers.Contains(racer)) racers.Add(racer);
        if (racer.isPlayer) Player = racer;
    }

    public void Unregister(RacerProgress racer)
    {
        racers.Remove(racer);
        if (Player == racer) Player = null;
    }

    void Update()
    {
        ranking.Clear();
        foreach (RacerProgress r in racers)
            if (!r.IsEliminated) ranking.Add(r);

        ranking.Sort((a, b) => b.Distance.CompareTo(a.Distance));
        AliveCount = ranking.Count;

        for (int i = 0; i < ranking.Count; i++)
            ranking[i].Position = i + 1;
    }
}
