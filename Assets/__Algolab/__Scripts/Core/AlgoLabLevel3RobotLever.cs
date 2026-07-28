using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Palanca física del monitor. Se toma con el gatillo secundario y convierte
/// el desplazamiento del mando en rotación gradual del robot.
/// </summary>
[DisallowMultipleComponent]
public class AlgoLabLevel3RobotLever : MonoBehaviour
{
    public enum EjeRobot
    {
        InclinacionX,
        GiroY
    }

    public AlgoLabLevel3RobotPracticeRuntime runtime;
    public EjeRobot ejeRobot;
    public Collider zonaAgarre;
    public Vector3 ejeMovimientoEnPadre = Vector3.forward;
    public Vector3 ejeRotacionVisualLocal = Vector3.right;
    [Min(0.02f)] public float distanciaMovimientoCompleto = 0.10f;
    [Min(0.005f)] public float radioAgarre = 0.02f;
    [Range(0.1f, 1f)] public float umbralAgarrar = 0.62f;
    [Range(0f, 0.9f)] public float umbralSoltar = 0.30f;
    [Min(1f)] public float anguloVisualMaximo = 28f;
    [Min(0.005f)] public float distanciaVisualMaxima = 0.055f;
    [Min(1f)] public float velocidadCentro = 7f;

    private static readonly Dictionary<
        SimpleOvRGrabber,
        AlgoLabLevel3RobotLever
    > Propietarios = new Dictionary<
        SimpleOvRGrabber,
        AlgoLabLevel3RobotLever
    >();

    private Vector3 posicionInicial;
    private Quaternion rotacionInicial;
    private SimpleOvRGrabber controlActivo;
    private Vector3 puntoInicialEnPadre;
    private float valor;
    private SimpleOvRGrabber[] controles;
    private float siguienteBusqueda;

    private void Awake()
    {
        posicionInicial = transform.localPosition;
        rotacionInicial = transform.localRotation;
        if (zonaAgarre == null)
            zonaAgarre = GetComponent<Collider>();
    }

    private void OnEnable()
    {
        posicionInicial = transform.localPosition;
        rotacionInicial = transform.localRotation;
        LiberarControl();
        valor = 0f;
        BuscarControles();
    }

    private void OnDisable()
    {
        LiberarControl();
    }

    private void OnDestroy()
    {
        LiberarControl();
    }

    private void Update()
    {
        if (Time.unscaledTime >= siguienteBusqueda)
            BuscarControles();

        if (controlActivo == null)
            IntentarTomar();
        else
            ActualizarAgarre();

        if (controlActivo == null)
            valor = Mathf.MoveTowards(valor, 0f, velocidadCentro * Time.unscaledDeltaTime);

        // La palanca sigue fisicamente el movimiento adelante/atras del mando.
        // Se conserva su orientacion original para evitar el giro lateral que
        // producia el modelo importado.
        transform.localPosition =
            posicionInicial +
            ejeMovimientoEnPadre.normalized * valor * distanciaVisualMaxima;
        transform.localRotation = rotacionInicial;

        if (runtime != null && Mathf.Abs(valor) > 0.01f)
            runtime.AplicarEntradaRotacion(ejeRobot, valor, Time.deltaTime);
    }

    public void ReiniciarPalanca()
    {
        LiberarControl();
        valor = 0f;
        transform.localPosition = posicionInicial;
        transform.localRotation = rotacionInicial;
    }

    private void IntentarTomar()
    {
        if (controles == null)
            return;

        for (int i = 0; i < controles.Length; i++)
        {
            SimpleOvRGrabber control = controles[i];
            if (control == null || ObtenerGrip(control) < umbralAgarrar)
                continue;

            Transform punto = control.grabPoint != null
                ? control.grabPoint
                : control.transform;
            Vector3 cercano = zonaAgarre != null
                ? zonaAgarre.ClosestPoint(punto.position)
                : transform.position;
            if (Vector3.Distance(cercano, punto.position) > radioAgarre)
                continue;
            if (!EsLaPalancaMasCercana(control, punto.position))
                continue;
            if (Propietarios.TryGetValue(control, out AlgoLabLevel3RobotLever dueño) &&
                dueño != null && dueño != this)
            {
                continue;
            }

            controlActivo = control;
            Propietarios[control] = this;
            puntoInicialEnPadre = transform.parent != null
                ? transform.parent.InverseTransformPoint(punto.position)
                : punto.position;
            break;
        }
    }

    private void ActualizarAgarre()
    {
        if (controlActivo == null || ObtenerGrip(controlActivo) <= umbralSoltar)
        {
            LiberarControl();
            return;
        }

        Transform punto = controlActivo.grabPoint != null
            ? controlActivo.grabPoint
            : controlActivo.transform;
        Vector3 actual = transform.parent != null
            ? transform.parent.InverseTransformPoint(punto.position)
            : punto.position;
        Vector3 delta = actual - puntoInicialEnPadre;
        valor = Mathf.Clamp(
            Vector3.Dot(delta, ejeMovimientoEnPadre.normalized) /
            Mathf.Max(0.02f, distanciaMovimientoCompleto),
            -1f,
            1f
        );
    }

    private static float ObtenerGrip(SimpleOvRGrabber control)
    {
        if (control == null)
            return 0f;

        try
        {
            if (control.handSide == SimpleOvRGrabber.HandSide.Left)
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

            // Con un controlador concreto (RTouch), Meta reporta el grip
            // derecho como PrimaryHandTrigger. SecondaryHandTrigger se
            // conserva como respaldo cuando se consulta el conjunto Touch.
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
            return 0f;
        }
    }

    private void BuscarControles()
    {
        siguienteBusqueda = Time.unscaledTime + 1f;
        controles = FindObjectsByType<SimpleOvRGrabber>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );
    }

    private bool EsLaPalancaMasCercana(
        SimpleOvRGrabber control,
        Vector3 puntoMundo)
    {
        AlgoLabLevel3RobotLever[] palancas =
            FindObjectsByType<AlgoLabLevel3RobotLever>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );
        float propia = DistanciaSuperficie(puntoMundo);
        for (int i = 0; i < palancas.Length; i++)
        {
            AlgoLabLevel3RobotLever otra = palancas[i];
            if (otra == null || otra == this || otra.runtime != runtime)
                continue;
            if (otra.DistanciaSuperficie(puntoMundo) + 0.001f < propia)
                return false;
        }
        return true;
    }

    private float DistanciaSuperficie(Vector3 puntoMundo)
    {
        Vector3 cercano = zonaAgarre != null
            ? zonaAgarre.ClosestPoint(puntoMundo)
            : transform.position;
        return Vector3.Distance(cercano, puntoMundo);
    }

    private void LiberarControl()
    {
        if (controlActivo != null &&
            Propietarios.TryGetValue(
                controlActivo,
                out AlgoLabLevel3RobotLever propietario) &&
            propietario == this)
        {
            Propietarios.Remove(controlActivo);
        }
        controlActivo = null;
    }
}
