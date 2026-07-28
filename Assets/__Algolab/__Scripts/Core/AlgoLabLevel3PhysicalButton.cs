using UnityEngine;

/// <summary>
/// Botón mecánico para VR. Detecta la punta del mando cerca de su volumen,
/// baja de forma proporcional y dispara una sola pulsación al llegar al fondo.
/// </summary>
[DisallowMultipleComponent]
public class AlgoLabLevel3PhysicalButton : MonoBehaviour
{
    public AlgoLabLevel3RobotPracticeRuntime runtime;
    public Collider superficie;
    public Vector3 ejePresionLocal = Vector3.down;
    [Min(0.005f)] public float recorrido = 0.035f;
    [Min(0.01f)] public float radioContacto = 0.075f;
    [Range(0.1f, 1f)] public float umbralActivacion = 0.78f;
    [Range(0f, 0.9f)] public float umbralRearme = 0.25f;
    [Min(1f)] public float velocidadRetorno = 16f;

    private Vector3 posicionInicial;
    private Quaternion rotacionInicial;
    private float presionVisual;
    private bool armado = true;
    private SimpleOvRGrabber[] controles;
    private SimpleOVRHandGrabber[] manos;
    private float siguienteBusqueda;

    private void Awake()
    {
        posicionInicial = transform.localPosition;
        rotacionInicial = transform.localRotation;
        if (superficie == null)
            superficie = GetComponent<Collider>();
    }

    private void OnEnable()
    {
        posicionInicial = transform.localPosition;
        rotacionInicial = transform.localRotation;
        presionVisual = 0f;
        armado = true;
        BuscarPuntosDeContacto();
    }

    private void Update()
    {
        if (Time.unscaledTime >= siguienteBusqueda)
            BuscarPuntosDeContacto();

        float objetivo = CalcularPresion();
        float rapidez = objetivo > presionVisual
            ? velocidadRetorno * 2.2f
            : velocidadRetorno;
        presionVisual = Mathf.MoveTowards(
            presionVisual,
            objetivo,
            rapidez * Time.unscaledDeltaTime
        );

        // El eje se expresa en el espacio del padre. El modelo del boton ya
        // viene inclinado desde Blender; volver a rotar el eje con esa pose
        // hacia que el boton se deslizara hacia un lado en vez de hundirse.
        Vector3 direccionEnPadre =
            ejePresionLocal.normalized * recorrido;
        transform.localPosition = posicionInicial + direccionEnPadre * presionVisual;

        if (armado && presionVisual >= umbralActivacion)
        {
            armado = false;
            if (runtime != null)
                runtime.PulsarBotonEnergia();
        }
        else if (!armado && presionVisual <= umbralRearme)
        {
            armado = true;
        }
    }

    public void ReiniciarBoton()
    {
        presionVisual = 0f;
        armado = true;
        transform.localPosition = posicionInicial;
        transform.localRotation = rotacionInicial;
    }

    [ContextMenu("Simular pulsación")]
    public void SimularPulsacion()
    {
        if (runtime != null)
            runtime.PulsarBotonEnergia();
    }

    private float CalcularPresion()
    {
        float mayor = 0f;

        if (controles != null)
        {
            for (int i = 0; i < controles.Length; i++)
            {
                if (controles[i] == null)
                    continue;
                Transform punto = controles[i].grabPoint != null
                    ? controles[i].grabPoint
                    : controles[i].transform;
                mayor = Mathf.Max(mayor, PresionDesdePunto(punto.position));
            }
        }

        if (manos != null)
        {
            for (int i = 0; i < manos.Length; i++)
            {
                if (manos[i] == null)
                    continue;
                Transform punto = manos[i].grabPoint != null
                    ? manos[i].grabPoint
                    : manos[i].transform;
                mayor = Mathf.Max(mayor, PresionDesdePunto(punto.position));
            }
        }

        return mayor;
    }

    private float PresionDesdePunto(Vector3 punto)
    {
        Vector3 cercano = superficie != null
            ? superficie.ClosestPoint(punto)
            : transform.position;
        float distancia = Vector3.Distance(punto, cercano);
        return 1f - Mathf.Clamp01(distancia / Mathf.Max(0.01f, radioContacto));
    }

    private void BuscarPuntosDeContacto()
    {
        siguienteBusqueda = Time.unscaledTime + 1f;
        controles = FindObjectsByType<SimpleOvRGrabber>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );
        manos = FindObjectsByType<SimpleOVRHandGrabber>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );
    }
}
