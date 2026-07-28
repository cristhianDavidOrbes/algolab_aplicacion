using UnityEngine;

/// <summary>
/// Restringe el agarre directo de un objeto a una distancia real de su
/// superficie. Se usa en componentes internos que no deben poder tomarse
/// mediante un agarre lejano.
/// </summary>
[DisallowMultipleComponent]
public class AlgoLabGrabProximityGate : MonoBehaviour
{
    [Min(0.005f)]
    public float distanciaMaximaSuperficie = 0.055f;

    public bool exigirVidrioRoto = true;
    public AlgoLabRobotBreakableGlass vidrioRequerido;
    public Transform puntoRespaldo;
    [Tooltip("Si esta activo, el agarre solo se acepta cerca del punto de respaldo, aunque el objeto tenga otros colliders.")]
    public bool usarSoloPuntoRespaldo;

    private Collider[] collidersObjetivo;

    public void Configurar(
        float distanciaMaxima,
        AlgoLabRobotBreakableGlass vidrio,
        Transform respaldo = null)
    {
        distanciaMaximaSuperficie = Mathf.Max(0.005f, distanciaMaxima);
        vidrioRequerido = vidrio;
        exigirVidrioRoto = vidrio != null;
        puntoRespaldo = respaldo != null ? respaldo : transform;
        RefrescarColliders();
    }

    public bool PuedeAgarrarseDesde(Vector3 puntoMundo)
    {
        if (!isActiveAndEnabled)
            return true;

        if (exigirVidrioRoto &&
            (vidrioRequerido == null || !vidrioRequerido.Roto))
        {
            return false;
        }

        if (usarSoloPuntoRespaldo)
        {
            Transform respaldo =
                puntoRespaldo != null ? puntoRespaldo : transform;
            return Vector3.Distance(
                puntoMundo,
                respaldo.position
            ) <= Mathf.Max(0.005f, distanciaMaximaSuperficie);
        }

        if (collidersObjetivo == null || collidersObjetivo.Length == 0)
            RefrescarColliders();

        float distanciaMinima = float.PositiveInfinity;
        if (collidersObjetivo != null)
        {
            for (int i = 0; i < collidersObjetivo.Length; i++)
            {
                Collider collider = collidersObjetivo[i];
                if (collider == null || !collider.enabled || collider.isTrigger)
                    continue;

                Vector3 cercano = collider.ClosestPoint(puntoMundo);
                distanciaMinima = Mathf.Min(
                    distanciaMinima,
                    Vector3.Distance(puntoMundo, cercano)
                );
            }
        }

        if (float.IsPositiveInfinity(distanciaMinima))
        {
            Transform respaldo = puntoRespaldo != null ? puntoRespaldo : transform;
            distanciaMinima = Vector3.Distance(puntoMundo, respaldo.position);
        }

        return distanciaMinima <= Mathf.Max(0.005f, distanciaMaximaSuperficie);
    }

    private void Awake()
    {
        RefrescarColliders();
    }

    private void OnTransformChildrenChanged()
    {
        RefrescarColliders();
    }

    private void RefrescarColliders()
    {
        collidersObjetivo = GetComponentsInChildren<Collider>(true);
    }
}
