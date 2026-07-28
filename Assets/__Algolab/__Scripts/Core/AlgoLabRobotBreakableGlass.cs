using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Vidrio fisico de los compartimientos del robot.
/// Se rompe por una colision suficientemente fuerte o por el movimiento rapido
/// de un mando/mano dentro de su volumen, incluso si el rig no usa colliders.
/// </summary>
public class AlgoLabRobotBreakableGlass : MonoBehaviour
{
    public enum Compartimiento
    {
        Temperatura,
        Bateria
    }

    [Header("Rotura")]
    public Compartimiento compartimiento;
    [Min(0.2f)] public float velocidadMinima = 0.85f;
    [Min(0.01f)] public float margenDeteccion = 0.055f;
    [Min(0f)] public float impulsoFragmentos = 1.1f;
    [Min(0f)] public float dispersionFragmentos = 0.55f;
    public Renderer vidrioRenderer;
    public Collider vidrioCollider;
    public GameObject fragmentos;

    public bool Roto { get; private set; }

    private AlgoLabEncapsulationRobotPractice practica;
    private readonly List<Transform> puntosSeguimiento = new List<Transform>();
    private readonly Dictionary<int, Vector3> posicionesAnteriores =
        new Dictionary<int, Vector3>();
    private float siguienteBusqueda;
    private Rigidbody[] cuerposFragmentos;
    private EstadoFragmento[] estadosFragmentos;

    private struct EstadoFragmento
    {
        public Vector3 posicionLocal;
        public Quaternion rotacionLocal;
    }

    public void Configurar(
        AlgoLabEncapsulationRobotPractice controlador,
        Compartimiento tipo,
        Renderer renderer,
        Collider collider,
        GameObject shards)
    {
        practica = controlador;
        compartimiento = tipo;
        vidrioRenderer = renderer;
        vidrioCollider = collider;
        fragmentos = shards;
        PrepararFragmentos();
        ReiniciarVidrio();
        BuscarPuntosSeguimiento();
    }

    private void Update()
    {
        if (Roto || vidrioCollider == null)
            return;

        if (Time.unscaledTime >= siguienteBusqueda)
        {
            siguienteBusqueda = Time.unscaledTime + 2f;
            BuscarPuntosSeguimiento();
        }

        Bounds bounds = vidrioCollider.bounds;
        bounds.Expand(margenDeteccion * 2f);
        float dt = Mathf.Max(Time.unscaledDeltaTime, 0.001f);

        for (int i = 0; i < puntosSeguimiento.Count; i++)
        {
            Transform punto = puntosSeguimiento[i];
            if (punto == null || !punto.gameObject.activeInHierarchy)
                continue;

            int id = punto.GetInstanceID();
            Vector3 actual = punto.position;
            if (!posicionesAnteriores.TryGetValue(id, out Vector3 anterior))
            {
                posicionesAnteriores[id] = actual;
                continue;
            }

            posicionesAnteriores[id] = actual;
            Vector3 velocidad = (actual - anterior) / dt;
            bool impacto =
                bounds.Contains(actual) ||
                bounds.Contains(anterior) ||
                SegmentoIntersecaBounds(anterior, actual, bounds);
            if (velocidad.magnitude >= velocidadMinima && impacto)
            {
                Romper(bounds.ClosestPoint(actual), velocidad);
                return;
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!Roto && collision.relativeVelocity.magnitude >= velocidadMinima)
        {
            Vector3 punto = collision.contactCount > 0
                ? collision.GetContact(0).point
                : transform.position;
            Romper(punto, collision.relativeVelocity);
        }
    }

    [ContextMenu("Romper vidrio")]
    public void Romper()
    {
        Romper(
            transform.position,
            transform.forward * Mathf.Max(velocidadMinima, 1f)
        );
    }

    public void Romper(Vector3 puntoImpacto, Vector3 velocidadImpacto)
    {
        if (Roto)
            return;

        Roto = true;
        if (vidrioRenderer != null)
            vidrioRenderer.enabled = false;
        if (vidrioCollider != null)
            vidrioCollider.enabled = false;
        if (fragmentos != null)
        {
            fragmentos.SetActive(true);
            ActivarFisicaFragmentos(puntoImpacto, velocidadImpacto);
        }

        if (practica != null)
            practica.NotificarVidrioRoto(compartimiento);
    }

    public void ReiniciarVidrio()
    {
        Roto = false;
        if (vidrioRenderer != null)
            vidrioRenderer.enabled = true;
        if (vidrioCollider != null)
            vidrioCollider.enabled = true;
        RestaurarFragmentos();
        if (fragmentos != null)
            fragmentos.SetActive(false);
        posicionesAnteriores.Clear();
    }

    private void BuscarPuntosSeguimiento()
    {
        puntosSeguimiento.Clear();

        SimpleOvRGrabber[] mandos = FindObjectsByType<SimpleOvRGrabber>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );
        for (int i = 0; i < mandos.Length; i++)
            AgregarPunto(mandos[i].grabPoint != null ? mandos[i].grabPoint : mandos[i].transform);

        SimpleOVRHandGrabber[] manos = FindObjectsByType<SimpleOVRHandGrabber>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );
        for (int i = 0; i < manos.Length; i++)
            AgregarPunto(manos[i].grabPoint != null ? manos[i].grabPoint : manos[i].transform);

        Transform[] transforms = FindObjectsByType<Transform>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );
        for (int i = 0; i < transforms.Length; i++)
        {
            string nombre = transforms[i].name;
            if (nombre.Contains("ControllerAnchor") ||
                nombre.Contains("HandAnchor") ||
                nombre.Contains("ControllerModel"))
            {
                AgregarPunto(transforms[i]);
            }
        }
    }

    private void AgregarPunto(Transform punto)
    {
        if (punto != null && !puntosSeguimiento.Contains(punto))
            puntosSeguimiento.Add(punto);
    }

    private static bool SegmentoIntersecaBounds(
        Vector3 inicio,
        Vector3 fin,
        Bounds bounds)
    {
        Vector3 desplazamiento = fin - inicio;
        float distancia = desplazamiento.magnitude;
        if (distancia <= 0.0001f)
            return false;

        Ray ray = new Ray(inicio, desplazamiento / distancia);
        return bounds.IntersectRay(ray, out float distanciaImpacto) &&
               distanciaImpacto <= distancia;
    }

    private void PrepararFragmentos()
    {
        if (fragmentos == null)
        {
            cuerposFragmentos = System.Array.Empty<Rigidbody>();
            estadosFragmentos = System.Array.Empty<EstadoFragmento>();
            return;
        }

        cuerposFragmentos = fragmentos.GetComponentsInChildren<Rigidbody>(true);
        estadosFragmentos = new EstadoFragmento[cuerposFragmentos.Length];
        for (int i = 0; i < cuerposFragmentos.Length; i++)
        {
            Rigidbody cuerpo = cuerposFragmentos[i];
            estadosFragmentos[i] = new EstadoFragmento
            {
                posicionLocal = cuerpo.transform.localPosition,
                rotacionLocal = cuerpo.transform.localRotation
            };
            if (!cuerpo.isKinematic)
            {
                cuerpo.linearVelocity = Vector3.zero;
                cuerpo.angularVelocity = Vector3.zero;
            }
            cuerpo.isKinematic = true;
            cuerpo.useGravity = false;
            cuerpo.constraints = RigidbodyConstraints.FreezeAll;
        }
    }

    private void ActivarFisicaFragmentos(
        Vector3 puntoImpacto,
        Vector3 velocidadImpacto)
    {
        if (cuerposFragmentos == null)
            PrepararFragmentos();

        Vector3 velocidadBase = Vector3.ClampMagnitude(
            velocidadImpacto * 0.32f,
            2.6f
        );
        for (int i = 0; i < cuerposFragmentos.Length; i++)
        {
            Rigidbody cuerpo = cuerposFragmentos[i];
            if (cuerpo == null)
                continue;

            cuerpo.constraints = RigidbodyConstraints.None;
            cuerpo.isKinematic = false;
            cuerpo.useGravity = true;
            cuerpo.linearDamping = 0.08f;
            cuerpo.angularDamping = 0.05f;
            cuerpo.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            Vector3 direccion = cuerpo.worldCenterOfMass - puntoImpacto;
            if (direccion.sqrMagnitude < 0.0001f)
                direccion = transform.forward;
            direccion.Normalize();

            Vector3 lateral = new Vector3(
                Mathf.Sin((i + 1) * 1.71f),
                Mathf.Cos((i + 1) * 2.13f) * 0.45f,
                Mathf.Sin((i + 1) * 2.77f)
            ) * dispersionFragmentos;
            cuerpo.linearVelocity =
                velocidadBase + direccion * impulsoFragmentos + lateral;
            cuerpo.angularVelocity = new Vector3(
                3.5f + i * 0.37f,
                -2.4f + i * 0.51f,
                4.1f - i * 0.29f
            );
        }
    }

    private void RestaurarFragmentos()
    {
        if (cuerposFragmentos == null ||
            estadosFragmentos == null ||
            cuerposFragmentos.Length != estadosFragmentos.Length)
        {
            PrepararFragmentos();
        }

        for (int i = 0; i < cuerposFragmentos.Length; i++)
        {
            Rigidbody cuerpo = cuerposFragmentos[i];
            if (cuerpo == null)
                continue;

            if (!cuerpo.isKinematic)
            {
                cuerpo.linearVelocity = Vector3.zero;
                cuerpo.angularVelocity = Vector3.zero;
            }
            cuerpo.isKinematic = true;
            cuerpo.useGravity = false;
            cuerpo.constraints = RigidbodyConstraints.FreezeAll;
            cuerpo.transform.localPosition = estadosFragmentos[i].posicionLocal;
            cuerpo.transform.localRotation = estadosFragmentos[i].rotacionLocal;
        }
    }
}
