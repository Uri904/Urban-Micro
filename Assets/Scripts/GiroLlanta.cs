using UnityEngine;

public class GiroLlanta : MonoBehaviour
{
    public float velocidad = 360f; // grados por segundo

    void Update()
    {
        transform.Rotate(0f, 0f, -velocidad * Time.unscaledDeltaTime);
    }
}