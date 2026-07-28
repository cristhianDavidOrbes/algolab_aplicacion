using UnityEngine;

public class SimpleOVRHandGrabber : MonoBehaviour
{
    public enum HandSide
    {
        Left,
        Right
    }

    [Header("Mano")]
    public HandSide handSide = HandSide.Right;
    public OVRHand ovrHand;
    public Transform grabPoint;

    [Header("Agarre con dedos")]
    public OVRHand.HandFinger pinchFinger = OVRHand.HandFinger.Index;
    public float pinchThreshold = 0.7f;
    public float grabRadius = 0.18f;
    public LayerMask grabbableLayers = ~0;

    [Header("Movimiento al soltar")]
    public float physicsThrowMultiplier = 1f;
    public float physicsAngularMultiplier = 1f;
    public float floatThrowMultiplier = 0.35f;
    public float floatAngularMultiplier = 0.25f;

    [Header("Debug")]
    public bool mostrarDebug = true;

    private SimpleMRGrabbable heldObject;
    private Transform originalParent;
    private bool wasPinching;

    private Vector3 lastPosition;
    private Quaternion lastRotation;
    private Vector3 handVelocity;
    private Vector3 handAngularVelocity;

    private void Awake()
    {
        if (grabPoint == null)
        {
            grabPoint = transform;
        }

        if (ovrHand == null)
        {
            ovrHand = GetComponentInChildren<OVRHand>();
        }

        if (ovrHand == null)
        {
            ovrHand = GetComponentInParent<OVRHand>();
        }
    }

    private void Start()
    {
        lastPosition = grabPoint.position;
        lastRotation = grabPoint.rotation;
    }

    private void Update()
    {
        UpdateHandVelocity();

        if (heldObject != null && !heldObject.isActiveAndEnabled)
        {
            Release(false);
        }

        if (ovrHand == null)
        {
            return;
        }

        if (!ovrHand.IsTracked || !ovrHand.IsDataValid)
        {
            if (heldObject != null)
            {
                Release();
            }

            wasPinching = false;
            return;
        }

        float pinchStrength = ovrHand.GetFingerPinchStrength(pinchFinger);
        bool isPinching = pinchStrength >= pinchThreshold;

        if (isPinching && !wasPinching)
        {
            TryGrab();
        }

        if (!isPinching && wasPinching)
        {
            Release();
        }

        wasPinching = isPinching;
    }

    private void OnDisable()
    {
        Release(false);
        wasPinching = false;
    }

    private void OnDestroy()
    {
        Release(false);
    }

    private void UpdateHandVelocity()
    {
        if (grabPoint == null)
        {
            return;
        }

        float deltaTime = Mathf.Max(Time.deltaTime, 0.0001f);

        handVelocity = (grabPoint.position - lastPosition) / deltaTime;

        Quaternion deltaRotation = grabPoint.rotation * Quaternion.Inverse(lastRotation);
        deltaRotation.ToAngleAxis(out float angle, out Vector3 axis);

        if (angle > 180f)
        {
            angle -= 360f;
        }

        if (axis != Vector3.zero)
        {
            handAngularVelocity = axis * angle * Mathf.Deg2Rad / deltaTime;
        }
        else
        {
            handAngularVelocity = Vector3.zero;
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

            float distance = Vector3.Distance(
                grabPoint.position,
                grabbable.transform.position
            );

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
        originalParent = heldObject.transform.parent;

        heldObject.BeginGrab();

        Rigidbody rb = heldObject.Rigidbody;

        if (rb != null)
        {
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        heldObject.transform.SetParent(grabPoint, true);

        DebugLog("HAND GRABBER: objeto agarrado.");
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

        releasedObject.transform.SetParent(originalParent, true);

        releasedObject.EndGrab();

        Rigidbody rb = releasedObject.Rigidbody;

        if (rb != null && !rb.isKinematic)
        {
            if (releasedObject.releaseMode == SimpleMRGrabbable.ReleaseMode.Physics)
            {
                rb.linearVelocity = aplicarLanzamiento
                    ? handVelocity * physicsThrowMultiplier
                    : Vector3.zero;
                rb.angularVelocity = aplicarLanzamiento
                    ? handAngularVelocity * physicsAngularMultiplier
                    : Vector3.zero;
            }
            else
            {
                rb.linearVelocity = aplicarLanzamiento
                    ? handVelocity * floatThrowMultiplier
                    : Vector3.zero;
                rb.angularVelocity = aplicarLanzamiento
                    ? handAngularVelocity * floatAngularMultiplier
                    : Vector3.zero;
            }
        }

        heldObject = null;
        originalParent = null;

        DebugLog("HAND GRABBER: objeto soltado.");
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
