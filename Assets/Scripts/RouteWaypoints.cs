using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Ruta de la carrera. Los waypoints son los HIJOS de este objeto, en el orden
/// del Hierarchy (el primero es la salida, el último es la meta).
/// Sirve para: progreso de cada auto, posiciones, IA y puertas de eliminación.
/// </summary>
public class RouteWaypoints : MonoBehaviour
{
    [Header("Gizmos (solo editor)")]
    public Color lineColor = Color.yellow;
    public float gizmoRadius = 1.5f;

    private readonly List<Vector3> points = new List<Vector3>();
    private float[] cumulative = new float[0]; // distancia acumulada hasta cada punto

    public float TotalLength { get; private set; }
    public int PointCount => points.Count;

    void Awake()
    {
        Rebuild();
    }

    /// <summary>Vuelve a leer los hijos. Llamar si se mueven waypoints en runtime.</summary>
    public void Rebuild()
    {
        points.Clear();
        foreach (Transform child in transform)
            points.Add(child.position);

        cumulative = new float[points.Count];
        TotalLength = 0f;
        for (int i = 1; i < points.Count; i++)
        {
            TotalLength += Vector3.Distance(points[i - 1], points[i]);
            cumulative[i] = TotalLength;
        }
    }

    /// <summary>
    /// Devuelve cuántos metros de la ruta lleva una posición (proyectándola sobre la ruta).
    /// segmentHint recuerda el tramo anterior para buscar solo cerca y no saltar
    /// a otro tramo si la ruta se cruza. Empieza en -1.
    /// </summary>
    public float ProjectOnRoute(Vector3 pos, ref int segmentHint)
    {
        if (points.Count < 2) return 0f;

        int lastSeg = points.Count - 2;
        int from, to;
        if (segmentHint < 0)
        {
            from = 0;
            to = lastSeg;
        }
        else
        {
            from = Mathf.Max(0, segmentHint - 1);
            to = Mathf.Min(lastSeg, segmentHint + 3);
        }

        float bestSqr = float.MaxValue;
        int bestSeg = from;
        float bestT = 0f;

        for (int i = from; i <= to; i++)
        {
            Vector3 a = points[i];
            Vector3 ab = points[i + 1] - a;
            float lenSqr = ab.sqrMagnitude;
            float t = lenSqr > 0.0001f ? Mathf.Clamp01(Vector3.Dot(pos - a, ab) / lenSqr) : 0f;
            float dSqr = (a + ab * t - pos).sqrMagnitude;
            if (dSqr < bestSqr)
            {
                bestSqr = dSqr;
                bestSeg = i;
                bestT = t;
            }
        }

        segmentHint = bestSeg;
        float segLen = cumulative[bestSeg + 1] - cumulative[bestSeg];
        return cumulative[bestSeg] + bestT * segLen;
    }

    /// <summary>Punto de la ruta a cierta distancia desde la salida (útil para la IA).</summary>
    public Vector3 GetPointAtDistance(float distance)
    {
        if (points.Count == 0) return transform.position;
        if (points.Count == 1) return points[0];

        distance = Mathf.Clamp(distance, 0f, TotalLength);
        for (int i = 1; i < points.Count; i++)
        {
            if (distance <= cumulative[i])
            {
                float segLen = cumulative[i] - cumulative[i - 1];
                float t = segLen > 0.0001f ? (distance - cumulative[i - 1]) / segLen : 0f;
                return Vector3.Lerp(points[i - 1], points[i], t);
            }
        }
        return points[points.Count - 1];
    }

    void OnDrawGizmos()
    {
        int n = transform.childCount;
        Gizmos.color = lineColor;
        for (int i = 0; i < n; i++)
        {
            Vector3 p = transform.GetChild(i).position;
            Gizmos.DrawSphere(p, gizmoRadius);
            if (i > 0)
                Gizmos.DrawLine(transform.GetChild(i - 1).position, p);
#if UNITY_EDITOR
            Handles.Label(p + Vector3.up * 2.5f, i.ToString());
#endif
        }
    }

#if UNITY_EDITOR
    /// <summary>Crea un waypoint nuevo al final, siguiendo la dirección de los dos últimos.</summary>
    [ContextMenu("Agregar waypoint")]
    private void AddWaypoint()
    {
        int n = transform.childCount;
        Vector3 pos = transform.position;
        if (n >= 2)
        {
            Vector3 last = transform.GetChild(n - 1).position;
            Vector3 prev = transform.GetChild(n - 2).position;
            pos = last + (last - prev).normalized * 15f;
        }
        else if (n == 1)
        {
            pos = transform.GetChild(0).position + transform.forward * 15f;
        }

        GameObject go = new GameObject("WP_" + n);
        Undo.RegisterCreatedObjectUndo(go, "Agregar waypoint");
        go.transform.SetParent(transform);
        go.transform.position = pos;
        Selection.activeGameObject = go;
    }
#endif
}
