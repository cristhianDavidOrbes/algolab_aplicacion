using UnityEngine;

public class SimpleOvRGrabber : MonoBehaviour
{
    public enum HandSide
    {
        Left,
        Right
    }

    [Header("Control")]
    public HandSide handSide = HandSide.Right;

    [Header("Punto de agarre")]
    public Transform grabPoint;

    [Header("Agarre")]
    public float grabRadius = 0.25f;
    public LayerMask grabbableLayers = ~0;
    public float gripPressThreshold = 0.65f;
    public float gripReleaseThreshold = 0.35f;

    [Header("Movimiento mientras está agarrado")]
    public bool seguirConTransform = true;
    public bool mantenerOffsetAlAgarrar = true;

    [Header("Movimiento al soltar")]
    [Tooltip("Al soltar normalmente, usa estos multiplicadores del control. Si se desactiva, usa los multiplicadores configurados en SimpleMRGrabbable.")]
    public bool aplicarVelocidadAlSoltar = true;
    public float physicsThrowMultiplier = 1f;
    public float physicsAngularMultiplier = 1f;
    public float floatThrowMultiplier = 0.08f;
    public float floatAngularMultiplier = 0.05f;

    [Header("Debug")]
    public bool mostrarDebug = true;

    private SimpleMRGrabbable heldObject;
    private bool wasGripPressed;
    private float siguienteIntentoAgarre;
    private const float IntervaloReintentoAgarre = 0.06f;

    private Vector3 localPositionOffset;
    private Quaternion localRotationOffset;

    private Vector3 lastPosition;
    private Quaternion lastRotation;
    private Vector3 controllerVelocity;
    private Vector3 controllerAngularVelocity;

    private OVRInput.Axis1D GripAxis
    {
        get
        {
            return handSide == HandSide.Left
                ? OVRInput.Axis1D.PrimaryHandTrigger
                : OVRInput.Axis1D.SecondaryHandTrigger;
        }
    }

    private void Awake()
    {
        if (grabPoint == null)
        {
            grabPoint = transform;
        }
    }

    private void Start()
    {
        lastPosition = grabPoint.position;
        lastRotation = grabPoint.rotation;
        wasGripPressed = false;
    }

    private void Update()
    {
        UpdateControllerVelocity();

        if (heldObject != null && !heldObject.isActiveAndEnabled)
        {
            Release(false);
        }

        float gripValue = GetGripValue();

        bool isGripPressed;

        if (heldObject == null)
        {
            isGripPressed = gripValue > gripPressThreshold;
        }
        else
        {
            isGripPressed = gripValue > gripReleaseThreshold;
        }

        if (isGripPressed &&
            heldObject == null &&
            (!wasGripPressed || Time.unscaledTime >= siguienteIntentoAgarre))
        {
            siguienteIntentoAgarre =
                Time.unscaledTime + IntervaloReintentoAgarre;
            TryGrab();
        }

        if (!isGripPressed && heldObject != null)
        {
            Release();
        }

        wasGripPressed = isGripPressed;
    }

    private void LateUpdate()
    {
        FollowGrabPoint();
    }

    private void OnDisable()
    {
        ForceReleaseIfNeeded();
    }

    private void OnDestroy()
    {
        ForceReleaseIfNeeded();
    }

    private void UpdateControllerVelocity()
    {
        if (grabPoint == null)
        {
            return;
        }

        float deltaTime = Mathf.Max(Time.deltaTime, 0.0001f);

        controllerVelocity = (grabPoint.position - lastPosition) / deltaTime;

        Quaternion deltaRotation = grabPoint.rotation * Quaternion.Inverse(lastRotation);
        deltaRotation.ToAngleAxis(out float angle, out Vector3 axis);

        if (angle > 180f)
        {
            angle -= 360f;
        }

        if (axis != Vector3.zero)
        {
            controllerAngularVelocity = axis * angle * Mathf.Deg2Rad / deltaTime;
        }
        else
        {
            controllerAngularVelocity = Vector3.zero;
        }

        lastPosition = grabPoint.position;
        lastRotation = grabPoint.rotation;
    }

    private void TryGrab()
    {
        if (heldObject != null)
        {
            return;
        }

        Collider[] hits = Physics.OverlapSphere(
            grabPoint.position,
            grabRadius,
            grabbableLayers,
            QueryTriggerInteraction.Ignore
        );

        SimpleMRGrabbable closest = null;
        float closestDistance = float.MaxValue;
        System.Collections.Generic.Dictionary<SimpleMRGrabbable, float>
            distancias =
                new System.Collections.Generic.Dictionary<
                    SimpleMRGrabbable,
                    float
                >();

        foreach (Collider hit in hits)
        {
            SimpleMRGrabbable grabbable = hit.GetComponentInParent<SimpleMRGrabbable>();

            if (grabbable == null)
            {
                continue;
            }

            if (grabbable.IsGrabbed)
            {
                continue;
            }

            AlgoLabGrabProximityGate proximityGate =
                grabbable.GetComponent<AlgoLabGrabProximityGate>();
            if (proximityGate != null &&
                !proximityGate.PuedeAgarrarseDesde(grabPoint.position))
            {
                continue;
            }

            Vector3 puntoCercano = hit.ClosestPoint(grabPoint.position);
            float distance = Vector3.Distance(
                grabPoint.position,
                puntoCercano
            );
            if (distancias.TryGetValue(grabbable, out float anterior) &&
                anterior <= distance)
            {
                continue;
            }
            distancias[grabbable] = distance;

            if (distance < closestDistance)
            {
                closest = grabbable;
                closestDistance = distance;
            }
        }

        if (closest == null)
        {
            return;
        }

        heldObject = closest;

        if (mantenerOffsetAlAgarrar)
        {
            localPositionOffset = grabPoint.InverseTransformPoint(heldObject.transform.position);
            localRotationOffset = Quaternion.Inverse(grabPoint.rotation) * heldObject.transform.rotation;
        }
        else
        {
            localPositionOffset = Vector3.zero;
            localRotationOffset = Quaternion.identity;
        }

        heldObject.BeginGrab();

        Rigidbody rb = heldObject.Rigidbody;

        if (rb != null)
        {
            rb.useGravity = false;
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        FollowGrabPoint();

        DebugLog("SimpleOvRGrabber: objeto agarrado.");
    }

    private float GetGripValue()
    {
        try
        {
            if (handSide == HandSide.Left)
            {
                return Mathf.Max(
                    OVRInput.Get(
                        OVRInput.Axis1D.PrimaryHandTrigger,
                        OVRInput.Controller.LTouch
                    ),
                    OVRInput.Get(OVRInput.RawAxis1D.LHandTrigger),
                    OVRInput.Get(
                        OVRInput.Axis1D.PrimaryHandTrigger,
                        OVRInput.Controller.Touch
                    )
                );
            }

            return Mathf.Max(
                OVRInput.Get(
                    OVRInput.Axis1D.PrimaryHandTrigger,
                    OVRInput.Controller.RTouch
                ),
                OVRInput.Get(OVRInput.RawAxis1D.RHandTrigger),
                OVRInput.Get(
                    OVRInput.Axis1D.SecondaryHandTrigger,
                    OVRInput.Controller.Touch
                )
            );
        }
        catch
        {
            return OVRInput.Get(GripAxis);
        }
    }

    private void FollowGrabPoint()
    {
        if (heldObject == null || grabPoint == null)
        {
            return;
        }

        Transform target = heldObject.transform;

        Vector3 targetPosition = grabPoint.TransformPoint(localPositionOffset);
        Quaternion targetRotation = grabPoint.rotation * localRotationOffset;

        Rigidbody rb = heldObject.Rigidbody;

        if (seguirConTransform || rb == null)
        {
            target.position = targetPosition;
            target.rotation = targetRotation;
        }
        else
        {
            rb.MovePosition(targetPosition);
            rb.MoveRotation(targetRotation);
        }

        if (rb != null)
        {
            rb.useGravity = false;
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    private void Release()
    {
        Release(true);
    }

    private void Release(bool aplicarLanzamiento)
    {
        if (heldObject == null)
        {
            return;
        }

        SimpleMRGrabbable releasedObject = heldObject;
        heldObject = null;

        Rigidbody rb = releasedObject.Rigidbody;

        releasedObject.EndGrab();

        if (rb != null && !rb.isKinematic)
        {
            if (releasedObject.releaseMode == SimpleMRGrabbable.ReleaseMode.Physics)
            {
                if (aplicarLanzamiento)
                {
                    float multiplicadorLineal = aplicarVelocidadAlSoltar
                        ? physicsThrowMultiplier
                        : releasedObject.multiplicadorImpulsoLineal;
                    float multiplicadorAngular = aplicarVelocidadAlSoltar
                        ? physicsAngularMultiplier
                        : releasedObject.multiplicadorImpulsoAngular;

                    rb.linearVelocity = Vector3.ClampMagnitude(
                        controllerVelocity * multiplicadorLineal,
                        releasedObject.velocidadLinealMaximaAlSoltar
                    );
                    rb.angularVelocity = Vector3.ClampMagnitude(
                        controllerAngularVelocity * multiplicadorAngular,
                        releasedObject.velocidadAngularMaximaAlSoltar
                    );
                }
                else
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
            }
            else
            {
                if (aplicarLanzamiento && aplicarVelocidadAlSoltar)
                {
                    rb.linearVelocity = controllerVelocity * floatThrowMultiplier;
                    rb.angularVelocity = controllerAngularVelocity * floatAngularMultiplier;
                }
                else if (!aplicarLanzamiento)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
            }
        }

        wasGripPressed = false;

        DebugLog("SimpleOvRGrabber: objeto soltado.");
    }

    private void ForceReleaseIfNeeded()
    {
        if (heldObject == null)
        {
            return;
        }

        Release(false);
    }

    public bool EstaAgarrando(SimpleMRGrabbable objetivo)
    {
        return objetivo != null && heldObject == objetivo;
    }

    public void SoltarSiEstaAgarrando(SimpleMRGrabbable objetivo)
    {
        if (objetivo != null && heldObject == objetivo)
            Release(false);
    }

    public void SoltarObjetoActualSinImpulso()
    {
        Release(false);
    }

    private void OnDrawGizmosSelected()
    {
        Transform point = grabPoint != null ? grabPoint : transform;
        Gizmos.DrawWireSphere(point.position, grabRadius);
    }

    private void DebugLog(string mensaje)
    {
        if (mostrarDebug)
        {
            Debug.Log(mensaje);
        }
    }
}
