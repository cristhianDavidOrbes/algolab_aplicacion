using UnityEngine;

public class AlgoLabDiagramBillboard : MonoBehaviour
{
    [Header("Objetivo")]
    public Transform objetivoCamara;

    [Header("Punto de mirada opcional")]
    [Tooltip("Se actualiza automáticamente según el estado contraído/expandido. Si está vacío, usa la posición del propio objeto como respaldo.")]
    public Transform puntoMiradaActual;

    [Header("Puntos para panel expandible")]
    public Transform puntoMiradaContraido;
    public Transform puntoMiradaExpandido;

    [Header("Estado")]
    public bool usarPuntoExpandido = false;

    [Header("Configuración")]
    public bool soloEjeY = true;
    public bool invertirFrente = false;
    public float suavizado = 10f;

    [Header("Sincronización de punto de mirada")]
    [Tooltip("Recomendado activado. Cada frame vuelve a escoger el punto correcto: contraído usa PuntoMiradaContraido y expandido usa PuntoMiradaExpandido. Evita que se quede pegado al punto expandido.")]
    public bool refrescarPuntoMiradaCadaFrame = true;

    [Tooltip("Recomendado activado. Si el punto del estado actual no existe, usa este transform como respaldo y NO conserva el punto anterior.")]
    public bool usarTransformComoRespaldoSiFaltaPuntoEstado = true;

    [Tooltip("Debug opcional para revisar qué punto está usando el billboard.")]
    public bool mostrarDebugPuntoMirada = false;

    [Header("Protección cuando el jugador está MUY cerca")]
    [Tooltip("Evita que el panel dé vueltas cuando la cabeza/cámara queda demasiado cerca del punto de mirada.")]
    public bool protegerRotacionCuandoCamaraEstaMuyCerca = true;

    [Tooltip("Recomendado activado para el panel de progreso: la protección solo se aplica cuando el panel está contraído. Expandido se comporta normal.")]
    public bool protegerSoloCuandoEstaContraido = true;

    [Tooltip("Distancia mínima en metros. Debe ser pequeña para que solo proteja cuando estés MUY cerca. Recomendado: 0.10 a 0.15.")]
    public float distanciaMinimaCamaraPuntoMirada = 0.12f;

    [Tooltip("Si está activo, cuando la cámara está muy cerca conserva la última rotación estable en vez de recalcular LookRotation con una dirección inestable.")]
    public bool mantenerUltimaRotacionSiEstaMuyCerca = true;

    [Tooltip("Suavizado usado solo para volver a la última rotación estable cuando se activa la protección.")]
    public float suavizadoProteccionCerca = 25f;

    [Tooltip("Debug opcional para ver cuándo se está bloqueando la rotación por cercanía.")]
    public bool mostrarDebugProteccionCerca = false;

    private Quaternion ultimaRotacionEstable;
    private bool tieneUltimaRotacionEstable;
    private bool ultimoEstadoExpandidoAplicado;
    private Transform ultimoPuntoAplicado;

    public bool EstaUsandoPuntoExpandido => usarPuntoExpandido;
    public Transform PuntoMiradaUsado => puntoMiradaActual;

    private void Start()
    {
        PrepararCamaraSiHaceFalta();
        GuardarRotacionEstableActual();
        ActualizarPuntoMirada();
    }

    private void OnEnable()
    {
        PrepararCamaraSiHaceFalta();
        GuardarRotacionEstableActual();
        ActualizarPuntoMirada();
    }

    private void LateUpdate()
    {
        PrepararCamaraSiHaceFalta();

        if (objetivoCamara == null)
        {
            return;
        }

        if (refrescarPuntoMiradaCadaFrame)
        {
            ActualizarPuntoMirada();
        }

        Transform referenciaMirada = puntoMiradaActual != null
            ? puntoMiradaActual
            : transform;

        Vector3 direccion = referenciaMirada.position - objetivoCamara.position;

        if (soloEjeY)
        {
            direccion.y = 0f;
        }

        if (DebeCongelarRotacionPorCercania(direccion))
        {
            AplicarRotacionEstableSiHaceFalta();
            return;
        }

        if (direccion.sqrMagnitude < 0.0001f)
        {
            AplicarRotacionEstableSiHaceFalta();
            return;
        }

        if (!AlgoLabPanelFacing.TryGetStableRotation(
                direccion,
                soloEjeY,
                Quaternion.identity,
                invertirFrente,
                out Quaternion rotacionObjetivo))
        {
            AplicarRotacionEstableSiHaceFalta();
            return;
        }

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            rotacionObjetivo,
            Mathf.Clamp01(Time.unscaledDeltaTime * Mathf.Max(0f, suavizado))
        );

        GuardarRotacionEstableActual();
    }

    private void PrepararCamaraSiHaceFalta()
    {
        if (objetivoCamara == null && Camera.main != null)
        {
            objetivoCamara = Camera.main.transform;
        }
    }

    private void GuardarRotacionEstableActual()
    {
        ultimaRotacionEstable = transform.rotation;
        tieneUltimaRotacionEstable = true;
    }

    private bool DebeCongelarRotacionPorCercania(Vector3 direccionPlano)
    {
        if (!protegerRotacionCuandoCamaraEstaMuyCerca)
        {
            return false;
        }

        if (protegerSoloCuandoEstaContraido && usarPuntoExpandido)
        {
            return false;
        }

        float distanciaMinima = Mathf.Max(0.01f, distanciaMinimaCamaraPuntoMirada);
        bool muyCerca = direccionPlano.sqrMagnitude < distanciaMinima * distanciaMinima;

        if (muyCerca && mostrarDebugProteccionCerca)
        {
            Debug.Log(
                "Billboard protegido por cercanía MUY cercana: " + name +
                " | estado=" + (usarPuntoExpandido ? "Expandido" : "Contraído") +
                " | punto=" + (puntoMiradaActual != null ? puntoMiradaActual.name : "null") +
                " | distancia=" + Mathf.Sqrt(direccionPlano.sqrMagnitude).ToString("F3")
            );
        }

        return muyCerca;
    }

    private void AplicarRotacionEstableSiHaceFalta()
    {
        if (!mantenerUltimaRotacionSiEstaMuyCerca || !tieneUltimaRotacionEstable)
        {
            return;
        }

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            ultimaRotacionEstable,
            Mathf.Clamp01(Time.unscaledDeltaTime * Mathf.Max(0f, suavizadoProteccionCerca))
        );
    }

    public void UsarPuntoContraido()
    {
        SetExpandido(false);
    }

    public void UsarPuntoExpandido()
    {
        SetExpandido(true);
    }

    public void SetExpandido(bool expandido)
    {
        usarPuntoExpandido = expandido;
        ActualizarPuntoMirada();
    }

    public void ForzarRefrescoPuntoMirada()
    {
        ActualizarPuntoMirada();
    }

    private void ActualizarPuntoMirada()
    {
        Transform nuevoPunto = null;

        if (usarPuntoExpandido)
        {
            nuevoPunto = puntoMiradaExpandido;
        }
        else
        {
            nuevoPunto = puntoMiradaContraido;
        }

        // Importante: si falta el punto del estado actual, NO se conserva el punto anterior,
        // porque eso hace que el panel contraído siga mirando con el punto expandido.
        if (nuevoPunto == null && usarTransformComoRespaldoSiFaltaPuntoEstado)
        {
            nuevoPunto = transform;
        }

        bool cambioEstado = ultimoEstadoExpandidoAplicado != usarPuntoExpandido;
        bool cambioPunto = ultimoPuntoAplicado != nuevoPunto;

        puntoMiradaActual = nuevoPunto;
        ultimoEstadoExpandidoAplicado = usarPuntoExpandido;
        ultimoPuntoAplicado = nuevoPunto;

        if ((cambioEstado || cambioPunto) && mostrarDebugPuntoMirada)
        {
            Debug.Log(
                "Billboard punto mirada actualizado: " + name +
                " | estado=" + (usarPuntoExpandido ? "Expandido" : "Contraído") +
                " | punto=" + (puntoMiradaActual != null ? puntoMiradaActual.name : "null")
            );
        }

        GuardarRotacionEstableActual();
    }
}
