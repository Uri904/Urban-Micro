using UnityEngine;

/// <summary>
/// Cámara en tercera persona que sigue al auto con suavizado.
/// Colócalo en la Main Camera.
/// </summary>
public class SmoothCameraFollow : MonoBehaviour
{
    [Header("Objetivo")]
    public Transform target; // Arrastra aquí el Transform del auto

    [Header("Posicionamiento")]
    public Vector3 offset = new Vector3(0f, 3.5f, -7f);
    public float followSpeed = 8f;
    public float rotationSpeed = 6f;

    [Header("Mirada")]
    public Vector3 lookOffset = new Vector3(0f, 1f, 0f);

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = target.TransformPoint(offset);
        transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);

        Quaternion desiredRotation = Quaternion.LookRotation((target.position + lookOffset) - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationSpeed * Time.deltaTime);
    }
}
