using UnityEngine;

public class CamionController : MonoBehaviour
{
    public Rigidbody rb;

    public WheelCollider ruedaDelanteraDerecha;
    public WheelCollider ruedaDelanteraIzquierda;
    public WheelCollider ruedaTraseraDerecha;
    public WheelCollider ruedaTraseraIzquierda;

    public float motor = 1500f;
    public float direccion = 30f;
    public float freno = 3000f;

    void FixedUpdate()
    {
        Debug.Log("CAMION CONTROLLER FUNCIONANDO");
float aceleracion = Input.GetAxis("Vertical");
Debug.Log("Aceleracion: " + aceleracion);
Debug.Log("Trasera derecha: " + ruedaTraseraDerecha.isGrounded);
Debug.Log("Trasera izquierda: " + ruedaTraseraIzquierda.isGrounded);
Debug.Log("Y WheelCollider trasero: " + ruedaTraseraDerecha.transform.position.y);
        float giro = Input.GetAxis("Horizontal");

        // Aceleración
        ruedaTraseraDerecha.motorTorque = aceleracion * motor;
        ruedaTraseraIzquierda.motorTorque = aceleracion * motor;

        // Dirección
        ruedaDelanteraDerecha.steerAngle = giro * direccion;
        ruedaDelanteraIzquierda.steerAngle = giro * direccion;

        // Frenado
        float fuerzaFreno = 0f;

        if (Input.GetKey(KeyCode.Space))
        {
            fuerzaFreno = freno;
        }

        ruedaDelanteraDerecha.brakeTorque = fuerzaFreno;
        ruedaDelanteraIzquierda.brakeTorque = fuerzaFreno;
        ruedaTraseraDerecha.brakeTorque = fuerzaFreno;
        ruedaTraseraIzquierda.brakeTorque = fuerzaFreno;
    }
}