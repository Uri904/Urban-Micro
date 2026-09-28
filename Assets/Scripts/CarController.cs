using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controlador arcade para un vehículo de 4 ruedas usando WheelCollider.
/// Colócalo en el objeto raíz del auto (donde está el Rigidbody).
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class CarController : MonoBehaviour
{
    [Header("Ruedas - Colliders (físicas)")]
    public WheelCollider frontLeftCollider;
    public WheelCollider frontRightCollider;
    public WheelCollider rearLeftCollider;
    public WheelCollider rearRightCollider;

    [Header("Ruedas - Meshes (visuales)")]
    public Transform frontLeftMesh;
    public Transform frontRightMesh;
    public Transform rearLeftMesh;
    public Transform rearRightMesh;

    [Header("Configuración de manejo")]
    public float maxMotorTorque = 1500f;
    public float maxSteerAngle = 30f;
    public float maxBrakeTorque = 3000f;
    [Tooltip("True = tracción trasera, False = tracción a las 4 ruedas")]
    public bool rearWheelDrive = true;

    [Header("Estabilidad")]
    public Vector3 centerOfMassOffset = new Vector3(0f, -0.5f, 0f);

    private Rigidbody rb;
    // Offsets entre cada malla visual y su WheelCollider (se calculan al iniciar)
    private Quaternion[] meshRotOffset = new Quaternion[4];
    private Vector3[] meshPosOffset = new Vector3[4];
    private float inputVertical;
    private float inputHorizontal;
    private bool inputBrake;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass += centerOfMassOffset;
    }

    void Start()
    {
        CacheWheelOffsets();
    }

    void Update()
    {
        ReadInput();
    }

    void FixedUpdate()
    {
        ApplySteering();
        ApplyMotor();
        ApplyBrake();
        UpdateWheelMeshes();
    }

    private void ReadInput()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        inputVertical = 0f;
        if (kb.wKey.isPressed || kb.upArrowKey.isPressed) inputVertical = 1f;
        else if (kb.sKey.isPressed || kb.downArrowKey.isPressed) inputVertical = -1f;

        inputHorizontal = 0f;
        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) inputHorizontal = 1f;
        else if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) inputHorizontal = -1f;

        inputBrake = kb.spaceKey.isPressed;
    }

    private void ApplySteering()
    {
        float steer = inputHorizontal * maxSteerAngle;
        frontLeftCollider.steerAngle = steer;
        frontRightCollider.steerAngle = steer;
    }

    private void ApplyMotor()
    {
        float torque = inputVertical * maxMotorTorque;

        if (rearWheelDrive)
        {
            rearLeftCollider.motorTorque = torque;
            rearRightCollider.motorTorque = torque;
        }
        else
        {
            frontLeftCollider.motorTorque = torque;
            frontRightCollider.motorTorque = torque;
            rearLeftCollider.motorTorque = torque;
            rearRightCollider.motorTorque = torque;
        }
    }

    private void ApplyBrake()
    {
        float brake = inputBrake ? maxBrakeTorque : 0f;
        frontLeftCollider.brakeTorque = brake;
        frontRightCollider.brakeTorque = brake;
        rearLeftCollider.brakeTorque = brake;
        rearRightCollider.brakeTorque = brake;
    }

    private WheelCollider[] Colliders => new[] { frontLeftCollider, frontRightCollider, rearLeftCollider, rearRightCollider };
    private Transform[] Meshes => new[] { frontLeftMesh, frontRightMesh, rearLeftMesh, rearRightMesh };

    // Guarda cómo está orientada/posicionada cada malla respecto al Transform de su collider
    // en reposo (sin giro ni dirección), para respetar la orientación original del modelo.
    private void CacheWheelOffsets()
    {
        var cols = Colliders;
        var meshes = Meshes;
        for (int i = 0; i < 4; i++)
        {
            if (cols[i] == null || meshes[i] == null) continue;
            Transform ct = cols[i].transform;
            meshRotOffset[i] = Quaternion.Inverse(ct.rotation) * meshes[i].rotation;
            meshPosOffset[i] = Quaternion.Inverse(ct.rotation) * (meshes[i].position - ct.position);
        }
    }

    private void UpdateWheelMeshes()
    {
        var cols = Colliders;
        var meshes = Meshes;
        for (int i = 0; i < 4; i++)
        {
            if (cols[i] == null || meshes[i] == null) continue;
            cols[i].GetWorldPose(out Vector3 pos, out Quaternion rot);
            meshes[i].position = pos + rot * meshPosOffset[i];
            meshes[i].rotation = rot * meshRotOffset[i];
        }
    }
}