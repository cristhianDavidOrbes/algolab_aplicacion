using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Conecta el robot y el monitor diseñados en Unity con la práctica de
/// encapsulamiento. No genera sustitutos visuales: trabaja sobre la jerarquía
/// editable guardada por el diseñador.
/// </summary>
[DefaultExecutionOrder(500)]
[DisallowMultipleComponent]
public class AlgoLabLevel3RobotPracticeRuntime : MonoBehaviour
{
    public bool Explotado => explotado;
    public float TiempoRestante => tiempoRestante;
    public float TiempoPracticaRestante => tiempoPracticaRestante;
    public bool CargadorConectado => cargadorConectado;
    public bool BateriaOriginalInstalada => bateriaOriginalInstalada;
    public bool TemperaturaOriginalInstalada => temperaturaOriginalInstalada;

    [Header("Reglas")]
    [Min(3f)] public float segundosAntesDeExplosion = 60f;
    [Min(30f)] public float duracionMaximaPractica = 300f;
    [Min(1f)] public float cargaPorSegundo = 24f;
    [Min(1f)] public float enfriamientoPorSegundo = 18f;
    [Min(0.03f)] public float distanciaConexionCargador = 0.05f;
    [Min(0.08f)] public float distanciaDesconexionCargador = 0.18f;
    [Min(0.08f)] public float distanciaVentilador = 0.24f;
    [Min(0f)] public float profundidadInsercionCargador = 0.105f;
    [Min(1f)] public float velocidadRotacionRobot = 82f;
    [Range(5f, 40f)] public float inclinacionMaxima = 25f;

    [Header("Referencias principales")]
    public AlgoLabEncapsulationRobotPractice practica;
    public Transform visualRoot;
    public Transform robot;
    public Transform modeloRobot;
    public Transform panelHerramientas;
    public Transform modeloMonitor;

    [Header("Herramientas")]
    public Transform cargador;
    public Transform puntaCargador;
    public Transform anclaCableMonitor;
    public Transform anclaCableCargador;
    public Transform ventilador;
    public Transform aspasVentilador;
    public Transform puertoCarga;
    public Transform objetivoTemperatura;
    public Transform bateriaRepuestoPrivada;
    public Transform temperaturaRepuestoPrivada;
    public SimpleMRGrabbable cargadorGrab;
    public SimpleMRGrabbable ventiladorGrab;
    public AlgoLabLevel3FlexibleCable cable;

    [Header("Controles")]
    public AlgoLabLevel3PhysicalButton botonEnergia;
    public AlgoLabLevel3RobotLever palancaX;
    public AlgoLabLevel3RobotLever palancaY;

    [Header("Pantallas")]
    public TMP_Text textoAdvertencia;
    public TMP_Text textoMensaje;
    public TMP_Text textoAccion;
    public TMP_Text textoBateriaMonitor;
    public TMP_Text textoTemperaturaMonitor;
    public TMP_Text textoTemperaturaRobot;
    public GameObject pantallaApagado;
    public GameObject pantallaEncendido;
    public TMP_Text textoEstadoApagado;
    public TMP_Text textoEstadoEncendido;
    public Button botonReintentar;

    [Header("Indicadores del robot")]
    public Renderer luzError;
    public Renderer luzPreparando;
    public Renderer luzArreglado;
    public Renderer luzMonitorApagado;
    public Renderer luzMonitorEncendido;
    public Renderer barraBateria;

    [Header("Explosión")]
    [Min(0.5f)] public float fuerzaExplosion = 3.8f;
    [Min(0.5f)] public float radioExplosion = 1.8f;

    private bool inicializado;
    private bool explotado;
    private bool cargadorConectado;
    private bool cargadorDebeAlejarseAntesReconectar;
    private bool cargadorFueSoltadoTrasConexion;
    private bool ventiladorEnZona;
    private bool bateriaRepuestoManipulada;
    private bool temperaturaRepuestoManipulada;
    private bool bateriaRepuestoAcoplada;
    private bool temperaturaRepuestoAcoplada;
    private float tiempoRestante;
    private float tiempoPracticaRestante;
    private float giroY;
    private float inclinacionX;
    private Quaternion rotacionRobotInicial;
    private Vector3 posicionModeloInicial;
    private Vector3 escalaBarraInicial;
    private Vector3 posicionBarraInicial;
    private float baseInferiorBarra;
    private float mensajeHasta;
    private string mensajeCorto = string.Empty;
    private ParticleSystem particulasExplosion;
    private ParticleSystem particulasCalor;
    private Material materialParticulasExplosion;
    private GameObject arenaExplosion;
    private readonly List<ParteExplosion> partesExplosion = new List<ParteExplosion>();
    private readonly List<GameObject> objetosOcultosExplosion = new List<GameObject>();
    private readonly List<HuesoAnimado> huesos = new List<HuesoAnimado>();
    private SimpleMRGrabbable bateriaRepuestoGrab;
    private SimpleMRGrabbable temperaturaRepuestoGrab;
    private Vector3 bateriaRepuestoPosicionInicial;
    private Quaternion bateriaRepuestoRotacionInicial;
    private Vector3 bateriaRepuestoEscalaInicial;
    private Vector3 temperaturaRepuestoPosicionInicial;
    private Quaternion temperaturaRepuestoRotacionInicial;
    private Vector3 temperaturaRepuestoEscalaInicial;
    private Transform bateriaOriginal;
    private Transform temperaturaOriginal;
    private SimpleMRGrabbable bateriaOriginalGrab;
    private SimpleMRGrabbable temperaturaOriginalGrab;
    private Transform bateriaOriginalPadre;
    private Transform temperaturaOriginalPadre;
    private Vector3 bateriaOriginalPosicionLocal;
    private Quaternion bateriaOriginalRotacionLocal;
    private Vector3 bateriaOriginalEscalaLocal;
    private Vector3 temperaturaOriginalPosicionLocal;
    private Quaternion temperaturaOriginalRotacionLocal;
    private Vector3 temperaturaOriginalEscalaLocal;
    private bool bateriaOriginalInstalada = true;
    private bool temperaturaOriginalInstalada = true;
    private bool bateriaOriginalManipulada;
    private bool temperaturaOriginalManipulada;
    private AlgoLabRobotBreakableGlass vidrioBateria;
    private AlgoLabRobotBreakableGlass vidrioTemperatura;
    private AlgoLabClassDiagramController diagrama;
    private AlgoLabProgressPanel panelProgreso;
    private AlgoLabRobotMouthWaveform bocaRobot;
    private AudioSource fuenteVozRobot;
    private Coroutine rutinaApagadoVoz;
    private Coroutine rutinaSeguimientoVoz;
    private bool vozPausadaPorApagado;
    private float volumenVozOriginal = 1f;
    private float pitchVozOriginal = 1f;
    private bool iaAgradecioReparacion;
    private string atributoEnError = string.Empty;
    private static readonly Color ColorSeleccion =
        new Color(1f, 0.78f, 0.06f);
    private static readonly Color ColorCorrecto =
        new Color(0.08f, 0.92f, 0.36f);
    private static readonly Color ColorError =
        new Color(1f, 0.12f, 0.10f);

    public bool IAActivaEnEstaPractica =>
        isActiveAndEnabled &&
        gameObject.activeInHierarchy &&
        practica != null &&
        practica.PracticaIniciada &&
        !explotado;

    public bool PuedeReproducirVozRobot =>
        IAActivaEnEstaPractica && practica.Encendido;

    public bool TieneVozPausada =>
        vozPausadaPorApagado &&
        fuenteVozRobot != null &&
        fuenteVozRobot.clip != null;

    private sealed class ParteExplosion
    {
        public Transform transform;
        public Transform padre;
        public Vector3 posicionLocal;
        public Quaternion rotacionLocal;
        public Vector3 escalaLocal;
        public Rigidbody rigidbodyAgregado;
        public Collider colliderAgregado;
        public SimpleMRGrabbable agarrableAgregado;
    }

    private struct HuesoAnimado
    {
        public Transform transform;
        public Quaternion rotacionInicial;
        public EjeHueso eje;
        public float amplitud;
        public float fase;
    }

    private enum EjeHueso
    {
        X,
        Y
    }

    private void Awake()
    {
        AsegurarInicializacion();
    }

    private void Start()
    {
        AsegurarInicializacion();
        if (practica != null && !practica.PracticaIniciada)
            practica.IniciarPractica();
    }

    private void OnDestroy()
    {
        if (practica != null)
            practica.FeedbackCambiado -= AlCambiarFeedback;
        if (materialParticulasExplosion != null)
            Destroy(materialParticulasExplosion);
        if (rutinaApagadoVoz != null)
            StopCoroutine(rutinaApagadoVoz);
        if (rutinaSeguimientoVoz != null)
            StopCoroutine(rutinaSeguimientoVoz);
    }

    private void Update()
    {
        AsegurarInicializacion();
        if (!inicializado || practica == null)
            return;

        float dt = Mathf.Max(0f, Time.deltaTime);

        if (!explotado && !practica.PracticaCompletada)
        {
            tiempoPracticaRestante =
                Mathf.Max(0f, tiempoPracticaRestante - dt);
            practica.ActualizarPuntajePorTiempo(
                tiempoPracticaRestante,
                duracionMaximaPractica
            );
            if (tiempoPracticaRestante <= 0f)
            {
                ExplotarRobot();
                ActualizarInterfaz();
                return;
            }

            ActualizarHerramientas(dt);

            if (practica.Encendido && practica.Averiado)
            {
                tiempoRestante = Mathf.Max(0f, tiempoRestante - dt);
                AnimarAveria(dt);
                if (tiempoRestante <= 0f)
                    ExplotarRobot();
            }
            else
            {
                RestaurarPoseSuave(dt);
            }
        }
        else if (practica.PracticaCompletada && !explotado)
        {
            AnimarBaile(dt);
        }

        ActualizarEfectoCalor();
        ActualizarTonoVozAveriada();
        ActualizarInterfaz();
    }

    private void LateUpdate()
    {
        // Una vez que la punta entra en la zona corta del puerto, el conector
        // se alinea y encaja incluso si el usuario todavia mantiene el agarre.
        // Al volver a agarrarlo despues de soltarlo se desconecta normalmente.
        if (cargadorConectado && !explotado)
            EncajarCargador();
    }

    public void ReiniciarDesdeControlador()
    {
        AsegurarInicializacion();
        if (!inicializado)
            return;

        RestaurarExplosion();
        explotado = false;
        cargadorConectado = false;
        cargadorDebeAlejarseAntesReconectar = false;
        cargadorFueSoltadoTrasConexion = false;
        ventiladorEnZona = false;
        ReiniciarRepuestosPrivados();
        tiempoRestante = segundosAntesDeExplosion;
        tiempoPracticaRestante = duracionMaximaPractica;
        iaAgradecioReparacion = false;
        giroY = 0f;
        inclinacionX = 0f;
        mensajeCorto = string.Empty;
        mensajeHasta = 0f;

        if (robot != null)
            robot.localRotation = rotacionRobotInicial;
        if (modeloRobot != null)
            modeloRobot.localPosition = posicionModeloInicial;
        RestaurarPoseInmediata();

        if (cargadorGrab != null && cargadorGrab.Rigidbody != null)
            cargadorGrab.Rigidbody.isKinematic = false;
        ReiniciarComponentesOriginales();
        cable?.ReiniciarCable();
        botonEnergia?.ReiniciarBoton();
        palancaX?.ReiniciarPalanca();
        palancaY?.ReiniciarPalanca();
        if (botonReintentar != null)
            botonReintentar.gameObject.SetActive(false);
        if (arenaExplosion != null)
            arenaExplosion.SetActive(false);
        if (particulasExplosion != null)
            particulasExplosion.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ActualizarInterfaz();
    }

    public void PulsarBotonEnergia()
    {
        if (practica == null || explotado || practica.PracticaCompletada)
            return;

        if (practica.Encendido)
        {
            SeleccionarMetodo("apagar");
            practica.MetodoApagar();
            ApagarVozGradualmente();
        }
        else
        {
            SeleccionarAtributo("estado");
            practica.MetodoEncender();
            if (practica.Encendido)
                ReanudarVozGradualmente();
        }

        ActualizarInterfaz();
    }

    /// <summary>
    /// Agrega a la consulta de IA el estado real del robot sin afectar al
    /// asistente en los demas niveles.
    /// </summary>
    public string ConstruirPreguntaParaRobot(string pregunta)
    {
        if (!IAActivaEnEstaPractica || string.IsNullOrWhiteSpace(pregunta))
            return pregunta;

        string estado;
        if (practica.PracticaCompletada)
        {
            estado =
                "Estas reparado, encendido y estable. Responde como el robot " +
                "del taller, en espanol natural, amable y conciso.";
        }
        else if (practica.Encendido && practica.Averiado)
        {
            estado =
                "Estas encendido y averiado. Responde en primera persona como " +
                "un robot con fallas. Explica claramente esta solucion: primero " +
                "deben apagarte; luego cargarte al 100 por ciento usando el " +
                "puerto externo y enfriar el modulo hasta 10 grados con el " +
                "ventilador. No deben reemplazar la bateria ni el modulo porque " +
                "son atributos privados. Finalmente deben encenderte. Mantente " +
                "breve; la aplicacion agregara fallas audibles a tu texto.";
        }
        else
        {
            estado =
                "Estas apagado durante una reparacion. Puedes responder por " +
                "subtitulos de forma breve, pero no afirmes que estas encendido.";
        }

        return
            "[CONTEXTO INTERNO DE LA PRACTICA, NO LO REPITAS]\n" +
            estado + "\n" +
            "Bateria actual: " + practica.Energia + "%. " +
            "Temperatura actual: " + practica.Temperatura + " C.\n" +
            "[PREGUNTA DEL ESTUDIANTE]\n" +
            pregunta.Trim();
    }

    /// <summary>
    /// Convierte la respuesta general de la IA en la voz coherente del robot.
    /// </summary>
    public string PrepararRespuestaDelRobot(string respuesta)
    {
        if (!IAActivaEnEstaPractica || string.IsNullOrWhiteSpace(respuesta))
            return respuesta;

        string resultado = LimpiarFormatoParaVoz(respuesta);
        if (practica.PracticaCompletada)
        {
            if (!iaAgradecioReparacion)
            {
                iaAgradecioReparacion = true;
                resultado = "Gracias por repararme. " + resultado;
            }
            return string.IsNullOrWhiteSpace(resultado)
                ? "Gracias por repararme. Ya estoy funcionando correctamente."
                : resultado;
        }

        if (practica.Encendido && practica.Averiado)
        {
            return
                "Aayuda, nenecesito que me aaaaareglesssss. " +
                "Paara arreeeglarme, ttttienes que apagarme. " +
                "Luego conecta el caargador al puerto de atraaas hasta llegar " +
                "a cien por ciento. Rrrrompe el vidrio delantero y ennfria el " +
                "moodulo de tempeeratura con el veeentilador amarillo hasta " +
                "diez grados. No reeemplaces la bateria ni el moodulo porque " +
                "son atributos prrivados. Al final, ennciendeme.";
        }

        return string.IsNullOrWhiteSpace(resultado)
            ? "Estoy apagado. Enciendeme para poder hablar."
            : resultado;
    }

    public void NotificarInicioVozRobot(AudioSource fuente)
    {
        if (!IAActivaEnEstaPractica || fuente == null)
            return;

        if (rutinaApagadoVoz != null)
        {
            StopCoroutine(rutinaApagadoVoz);
            rutinaApagadoVoz = null;
        }
        if (rutinaSeguimientoVoz != null)
        {
            StopCoroutine(rutinaSeguimientoVoz);
            rutinaSeguimientoVoz = null;
        }

        fuenteVozRobot = fuente;
        vozPausadaPorApagado = false;
        volumenVozOriginal = Mathf.Max(0.01f, fuente.volume);
        pitchVozOriginal = Mathf.Approximately(fuente.pitch, 0f)
            ? 1f
            : fuente.pitch;
        bocaRobot?.ComenzarHablar(fuente);

        if (!PuedeReproducirVozRobot)
            rutinaApagadoVoz = StartCoroutine(
                ApagarVozAlComenzarRutina()
            );
    }

    public void NotificarFinVozRobot()
    {
        if (rutinaApagadoVoz != null || vozPausadaPorApagado)
            return;
        RestaurarFuenteVoz();
        bocaRobot?.Reposo();
    }

    public void ApagarVozGradualmente()
    {
        if (rutinaApagadoVoz != null)
            StopCoroutine(rutinaApagadoVoz);

        if (rutinaSeguimientoVoz != null)
        {
            StopCoroutine(rutinaSeguimientoVoz);
            rutinaSeguimientoVoz = null;
        }

        if (fuenteVozRobot == null ||
            (!fuenteVozRobot.isPlaying && !vozPausadaPorApagado))
        {
            rutinaApagadoVoz = null;
            bocaRobot?.Reposo();
            return;
        }

        rutinaApagadoVoz = StartCoroutine(ApagarVozRutina());
    }

    private IEnumerator ApagarVozAlComenzarRutina()
    {
        // TextToSpeechAgent dispara onSpeakStarting justo antes de Play().
        // Esperar un frame evita que una respuesta termine sonando si el
        // usuario apagó el robot mientras el audio todavía se sintetizaba.
        yield return null;
        rutinaApagadoVoz = null;
        if (!PuedeReproducirVozRobot)
            ApagarVozGradualmente();
    }

    private IEnumerator ApagarVozRutina()
    {
        AudioSource fuente = fuenteVozRobot;
        if (fuente == null)
        {
            rutinaApagadoVoz = null;
            yield break;
        }

        if (vozPausadaPorApagado)
        {
            rutinaApagadoVoz = null;
            yield break;
        }

        float volumenInicial = Mathf.Max(0f, fuente.volume);
        float pitchInicial = fuente.pitch;
        const float duracion = 1.35f;
        float transcurrido = 0f;
        while (transcurrido < duracion &&
               fuente != null &&
               fuente.isPlaying)
        {
            transcurrido += Mathf.Max(0.001f, Time.unscaledDeltaTime);
            float t = Mathf.Clamp01(transcurrido / duracion);
            float curva = 1f - t * t;
            fuente.volume = volumenInicial * curva;
            fuente.pitch = Mathf.Lerp(
                pitchInicial,
                Mathf.Max(0.32f, pitchInicial * 0.42f),
                t
            );
            yield return null;
        }

        if (fuente != null && fuente.isPlaying)
        {
            fuente.volume = 0f;
            fuente.pitch = Mathf.Max(0.32f, pitchVozOriginal * 0.42f);
            fuente.Pause();
            vozPausadaPorApagado = true;
        }
        else
        {
            vozPausadaPorApagado = false;
            RestaurarFuenteVoz();
        }
        bocaRobot?.Reposo();
        rutinaApagadoVoz = null;
    }

    private void ReanudarVozGradualmente()
    {
        if (!vozPausadaPorApagado ||
            fuenteVozRobot == null ||
            fuenteVozRobot.clip == null)
        {
            return;
        }

        if (rutinaApagadoVoz != null)
            StopCoroutine(rutinaApagadoVoz);
        if (rutinaSeguimientoVoz != null)
            StopCoroutine(rutinaSeguimientoVoz);

        rutinaApagadoVoz = StartCoroutine(ReanudarVozRutina());
    }

    private IEnumerator ReanudarVozRutina()
    {
        AudioSource fuente = fuenteVozRobot;
        if (fuente == null || fuente.clip == null)
        {
            vozPausadaPorApagado = false;
            rutinaApagadoVoz = null;
            yield break;
        }

        float pitchLento = Mathf.Max(0.32f, pitchVozOriginal * 0.42f);
        fuente.volume = 0f;
        fuente.pitch = pitchLento;
        fuente.UnPause();
        vozPausadaPorApagado = false;
        bocaRobot?.ComenzarHablar(fuente);

        const float duracion = 1.05f;
        float transcurrido = 0f;
        while (transcurrido < duracion &&
               fuente != null &&
               fuente.isPlaying &&
               practica != null &&
               practica.Encendido)
        {
            transcurrido += Mathf.Max(0.001f, Time.unscaledDeltaTime);
            float t = Mathf.SmoothStep(
                0f,
                1f,
                Mathf.Clamp01(transcurrido / duracion)
            );
            fuente.volume = Mathf.Lerp(0f, volumenVozOriginal, t);
            fuente.pitch = Mathf.Lerp(pitchLento, pitchVozOriginal, t);
            yield return null;
        }

        if (fuente != null &&
            fuente.isPlaying &&
            practica != null &&
            practica.Encendido)
        {
            RestaurarFuenteVoz();
            rutinaApagadoVoz = null;
            rutinaSeguimientoVoz = StartCoroutine(
                VigilarFinVozReanudada(fuente)
            );
            yield break;
        }

        rutinaApagadoVoz = null;
        if (practica != null && !practica.Encendido && fuente != null)
            ApagarVozGradualmente();
        else
            bocaRobot?.Reposo();
    }

    private IEnumerator VigilarFinVozReanudada(AudioSource fuente)
    {
        while (fuente != null && fuente.isPlaying)
            yield return null;

        rutinaSeguimientoVoz = null;
        if (!vozPausadaPorApagado)
        {
            RestaurarFuenteVoz();
            bocaRobot?.Reposo();
        }
    }

    /// <summary>
    /// Desvanece la respuesta anterior justo antes de reproducir una nueva.
    /// La sintesis nueva se inicia despues de este fundido para que dos voces
    /// nunca se superpongan ni la anterior se corte bruscamente.
    /// </summary>
    public IEnumerator DesvanecerRespuestaActual(AudioSource fuente)
    {
        if (rutinaApagadoVoz != null)
        {
            StopCoroutine(rutinaApagadoVoz);
            rutinaApagadoVoz = null;
        }
        if (rutinaSeguimientoVoz != null)
        {
            StopCoroutine(rutinaSeguimientoVoz);
            rutinaSeguimientoVoz = null;
        }

        if (fuente == null)
            fuente = fuenteVozRobot;
        if (fuente == null)
            yield break;

        if (vozPausadaPorApagado)
        {
            fuente.Stop();
            vozPausadaPorApagado = false;
            RestaurarFuenteVoz();
            bocaRobot?.Reposo();
            yield break;
        }

        if (fuente.isPlaying)
        {
            float volumenInicial = Mathf.Max(0f, fuente.volume);
            const float duracion = 0.42f;
            float transcurrido = 0f;
            while (transcurrido < duracion &&
                   fuente != null &&
                   fuente.isPlaying)
            {
                transcurrido += Mathf.Max(
                    0.001f,
                    Time.unscaledDeltaTime
                );
                float t = Mathf.SmoothStep(
                    0f,
                    1f,
                    Mathf.Clamp01(transcurrido / duracion)
                );
                fuente.volume = Mathf.Lerp(volumenInicial, 0f, t);
                yield return null;
            }
        }

        if (fuente != null)
            fuente.Stop();
        vozPausadaPorApagado = false;
        RestaurarFuenteVoz();
        bocaRobot?.Reposo();
    }

    private void ActualizarTonoVozAveriada()
    {
        if (fuenteVozRobot == null ||
            !fuenteVozRobot.isPlaying ||
            vozPausadaPorApagado ||
            rutinaApagadoVoz != null ||
            practica == null)
        {
            return;
        }

        if (!practica.Encendido || !practica.Averiado)
        {
            fuenteVozRobot.pitch = Mathf.MoveTowards(
                fuenteVozRobot.pitch,
                pitchVozOriginal,
                Time.unscaledDeltaTime * 0.9f
            );
            return;
        }

        // El robot averiado solo eleva el tono en pulsos. Nunca baja de su
        // tono normal; el descenso grave queda reservado al apagado.
        float pulso = Mathf.Max(
            0f,
            Mathf.Sin(Time.unscaledTime * 4.35f)
        );
        float objetivo = pitchVozOriginal * (1f + pulso * 0.24f);
        fuenteVozRobot.pitch = Mathf.MoveTowards(
            fuenteVozRobot.pitch,
            objetivo,
            Time.unscaledDeltaTime * 1.8f
        );
        fuenteVozRobot.pitch = Mathf.Max(
            pitchVozOriginal,
            fuenteVozRobot.pitch
        );
    }

    private void RestaurarFuenteVoz()
    {
        if (fuenteVozRobot == null)
            return;
        fuenteVozRobot.volume = volumenVozOriginal;
        fuenteVozRobot.pitch = pitchVozOriginal;
    }

    private void ConfigurarBocaRobot()
    {
        Transform boca = BuscarRecursivo(robot, "bocaRobot");
        if (boca == null)
            return;

        Transform existente = BuscarHijoDirecto(boca, "LineaVozRobot");
        GameObject linea;
        if (existente != null)
        {
            linea = existente.gameObject;
        }
        else
        {
            linea = new GameObject(
                "LineaVozRobot",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(AlgoLabRobotMouthWaveform)
            );
            linea.transform.SetParent(boca, false);
        }

        RectTransform rect = linea.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition3D = Vector3.zero;
        rect.sizeDelta = new Vector2(4.85f, 2.25f);
        rect.localScale = Vector3.one;
        rect.SetAsLastSibling();

        bocaRobot = linea.GetComponent<AlgoLabRobotMouthWaveform>();
        bocaRobot.color = Color.black;
        bocaRobot.puntos = 13;
        bocaRobot.grosor = 0.36f;
        bocaRobot.amplitudMaxima = 0.52f;
        bocaRobot.velocidad = 14f;
        bocaRobot.raycastTarget = false;
    }

    private static string LimpiarFormatoParaVoz(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return string.Empty;

        string limpio = texto
            .Replace("**", "")
            .Replace("__", "")
            .Replace("```", "")
            .Replace("`", "")
            .Replace("*", "")
            .Replace("#", "")
            .Replace("[", "")
            .Replace("]", "")
            .Replace(">", "");

        string[] lineas = limpio.Split(
            new[] { '\r', '\n' },
            StringSplitOptions.RemoveEmptyEntries
        );
        for (int i = 0; i < lineas.Length; i++)
        {
            string linea = lineas[i].Trim();
            while (linea.StartsWith("- ") || linea.StartsWith("• "))
                linea = linea.Substring(2).TrimStart();
            lineas[i] = linea;
        }
        return string.Join(" ", lineas).Trim();
    }

    public void AplicarEntradaRotacion(
        AlgoLabLevel3RobotLever.EjeRobot eje,
        float valor,
        float dt)
    {
        if (robot == null || explotado || Mathf.Abs(valor) < 0.01f)
            return;

        float magnitud = Mathf.Clamp01(Mathf.Abs(valor));
        // La zona central sigue siendo precisa, pero al llevar la palanca al
        // extremo el robot acelera con claridad y deja de sentirse pesado.
        float multiplicadorMaximo = Mathf.Lerp(
            0.55f,
            1.65f,
            magnitud * magnitud
        );
        float delta =
            valor * velocidadRotacionRobot * multiplicadorMaximo * dt;

        if (eje == AlgoLabLevel3RobotLever.EjeRobot.GiroY)
        {
            giroY = Mathf.Repeat(
                giroY + delta + 180f,
                360f
            ) - 180f;
        }
        else
        {
            // El eje X es deliberadamente continuo: la palanca puede hacer
            // girar el robot una vuelta completa y continuar sin topes.
            inclinacionX = Mathf.Repeat(
                inclinacionX + delta + 180f,
                360f
            ) - 180f;
        }

        Vector3 baseEuler = rotacionRobotInicial.eulerAngles;
        robot.localRotation = Quaternion.Euler(
            baseEuler.x + inclinacionX,
            baseEuler.y + giroY,
            baseEuler.z
        );
    }

    public void Reintentar()
    {
        if (practica != null)
            practica.IniciarPractica();
    }

    public void ReiniciarRepuestosPrivados()
    {
        bateriaRepuestoManipulada = false;
        temperaturaRepuestoManipulada = false;
        bateriaRepuestoAcoplada = false;
        temperaturaRepuestoAcoplada = false;
        DevolverRepuestoAlPanel(
            bateriaRepuestoGrab,
            bateriaRepuestoPrivada,
            bateriaRepuestoPosicionInicial,
            bateriaRepuestoRotacionInicial,
            bateriaRepuestoEscalaInicial
        );
        DevolverRepuestoAlPanel(
            temperaturaRepuestoGrab,
            temperaturaRepuestoPrivada,
            temperaturaRepuestoPosicionInicial,
            temperaturaRepuestoRotacionInicial,
            temperaturaRepuestoEscalaInicial
        );
        practica?.NotificarRetiroReemplazoPrivadoBateria();
        practica?.NotificarRetiroReemplazoPrivadoTemperatura();
    }

    public void SeleccionarAtributo(string atributo)
    {
        ObtenerDiagrama()?.MantenerAtributoConColor(
            "Robot",
            atributo,
            ColorSeleccion
        );
    }

    public void MarcarAtributoCorrecto(string atributo)
    {
        atributoEnError = string.Empty;
        ObtenerDiagrama()?.MantenerAtributoConColor(
            "Robot",
            atributo,
            ColorCorrecto
        );
    }

    public void MarcarAtributoError(string atributo)
    {
        atributoEnError = atributo ?? string.Empty;
        ObtenerDiagrama()?.MantenerAtributoConColor(
            "Robot",
            atributo,
            ColorError
        );
    }

    public void SeleccionarMetodo(string metodo)
    {
        ObtenerDiagrama()?.MantenerMetodoConColor(
            "Robot",
            metodo,
            ColorSeleccion
        );
    }

    public void MarcarMetodoCorrecto(string metodo)
    {
        ObtenerDiagrama()?.MantenerMetodoConColor(
            "Robot",
            metodo,
            ColorCorrecto
        );
    }

    public void RestaurarErrorDiagramaComoSeleccion()
    {
        if (string.IsNullOrWhiteSpace(atributoEnError))
            return;
        string atributo = atributoEnError;
        atributoEnError = string.Empty;
        SeleccionarAtributo(atributo);
    }

    private AlgoLabClassDiagramController ObtenerDiagrama()
    {
        if (diagrama == null)
        {
            diagrama = FindFirstObjectByType<AlgoLabClassDiagramController>(
                FindObjectsInactive.Include
            );
        }
        return diagrama;
    }

    [ContextMenu("Simular explosión")]
    public void SimularExplosion()
    {
        ExplotarRobot();
    }

    private void AsegurarInicializacion()
    {
        if (inicializado)
            return;

        if (practica == null)
            practica = GetComponent<AlgoLabEncapsulationRobotPractice>();
        if (practica == null)
            return;

        VincularAutomaticamente();
        if (robot == null || panelHerramientas == null)
            return;

        ConfigurarHerramientas();
        ConfigurarBocaRobot();
        CapturarPoseRobot();
        CrearEfectos();
        CrearArenaExplosion();

        rotacionRobotInicial = robot.localRotation;
        if (modeloRobot != null)
            posicionModeloInicial = modeloRobot.localPosition;
        if (barraBateria != null)
        {
            escalaBarraInicial = barraBateria.transform.localScale;
            posicionBarraInicial = barraBateria.transform.localPosition;
            baseInferiorBarra =
                posicionBarraInicial.y - escalaBarraInicial.y * 0.5f;
        }

        if (botonReintentar != null)
        {
            botonReintentar.onClick.RemoveListener(Reintentar);
            botonReintentar.onClick.AddListener(Reintentar);
            botonReintentar.gameObject.SetActive(false);
        }

        practica.FeedbackCambiado -= AlCambiarFeedback;
        practica.FeedbackCambiado += AlCambiarFeedback;
        tiempoRestante = segundosAntesDeExplosion;
        tiempoPracticaRestante = duracionMaximaPractica;
        iaAgradecioReparacion = false;
        diagrama = FindFirstObjectByType<AlgoLabClassDiagramController>(
            FindObjectsInactive.Include
        );
        panelProgreso = FindFirstObjectByType<AlgoLabProgressPanel>(
            FindObjectsInactive.Include
        );
        inicializado = true;
    }

    private void VincularAutomaticamente()
    {
        visualRoot = visualRoot != null
            ? visualRoot
            : BuscarHijoDirecto(transform, "RobotPracticeVisual");
        if (visualRoot == null)
            visualRoot = transform;

        robot = robot != null ? robot : BuscarRecursivo(visualRoot, "Robot");
        modeloRobot = modeloRobot != null
            ? modeloRobot
            : BuscarRecursivo(robot, "ModeloRobotRigged");
        panelHerramientas = panelHerramientas != null
            ? panelHerramientas
            : BuscarRecursivo(visualRoot, "PanelHerramientasPublicas");
        modeloMonitor = modeloMonitor != null
            ? modeloMonitor
            : BuscarRecursivo(panelHerramientas, "ModeloMonitor");

        cargador = cargador != null
            ? cargador
            : BuscarPrimero(
                panelHerramientas,
                "MetodoPublico_Cargar",
                "Cargador"
            );
        ventilador = ventilador != null
            ? ventilador
            : BuscarPrimero(
                panelHerramientas,
                "MetodoPublico_Enfriar_Ventilador",
                "ventilador"
            );
        puntaCargador = puntaCargador != null
            ? puntaCargador
            : BuscarPrimero(cargador, "PuntaConector", "punta");
        anclaCableCargador = anclaCableCargador != null
            ? anclaCableCargador
            : BuscarPrimero(cargador, "cable_cargador1", "AnclaCableCargador");
        anclaCableMonitor = anclaCableMonitor != null
            ? anclaCableMonitor
            : BuscarRecursivo(modeloMonitor, "cableCargador2");
        aspasVentilador = aspasVentilador != null
            ? aspasVentilador
            : BuscarPrimero(ventilador, "Aspas", "aspas");
        puertoCarga = puertoCarga != null
            ? puertoCarga
            : BuscarPrimero(
                robot,
                "compartimientoCargar",
                "PuertoCarga"
            );
        objetivoTemperatura = objetivoTemperatura != null
            ? objetivoTemperatura
            : BuscarRecursivo(robot, "ObjetivoModuloTemperatura");
        bateriaRepuestoPrivada = bateriaRepuestoPrivada != null
            ? bateriaRepuestoPrivada
            : BuscarRecursivo(panelHerramientas, "RepuestoPrivado_Bateria");
        temperaturaRepuestoPrivada = temperaturaRepuestoPrivada != null
            ? temperaturaRepuestoPrivada
            : BuscarRecursivo(
                panelHerramientas,
                "RepuestoPrivado_Temperatura"
            );

        Transform boton = BuscarRecursivo(modeloMonitor, "botonApagar");
        botonEnergia = botonEnergia != null
            ? botonEnergia
            : boton != null
                ? boton.GetComponent<AlgoLabLevel3PhysicalButton>()
                : null;
        Transform leverY = BuscarRecursivo(modeloMonitor, "palanca1Y");
        Transform leverX = BuscarRecursivo(modeloMonitor, "palanca2X");
        palancaY = palancaY != null
            ? palancaY
            : leverY != null
                ? leverY.GetComponent<AlgoLabLevel3RobotLever>()
                : null;
        palancaX = palancaX != null
            ? palancaX
            : leverX != null
                ? leverX.GetComponent<AlgoLabLevel3RobotLever>()
                : null;

        Transform pantalla = BuscarRecursivo(
            BuscarRecursivo(modeloMonitor, "monitorpc"),
            "pantalla"
        );
        textoAdvertencia = ObtenerTexto(textoAdvertencia, pantalla, "Advertencia");
        textoMensaje = ObtenerTexto(textoMensaje, pantalla, "mensaje");
        textoAccion = ObtenerTexto(textoAccion, pantalla, "Apagar");

        Transform seccionBateria = BuscarHijoDirecto(modeloMonitor, "Bateria");
        Transform seccionTemperatura = BuscarHijoDirecto(modeloMonitor, "Temperatura");
        textoBateriaMonitor = ObtenerTexto(
            textoBateriaMonitor,
            seccionBateria,
            "bateria"
        );
        textoTemperaturaMonitor = ObtenerTexto(
            textoTemperaturaMonitor,
            seccionTemperatura,
            "temperatura"
        );
        textoTemperaturaRobot = ObtenerTexto(
            textoTemperaturaRobot,
            BuscarRecursivo(robot, "PantallaTemperatura"),
            "Temperatura"
        );

        Transform apagado = BuscarHijoDirecto(modeloMonitor, "Apagado");
        Transform encendido = BuscarHijoDirecto(modeloMonitor, "Encendido");
        pantallaApagado = pantallaApagado != null
            ? pantallaApagado
            : apagado != null ? apagado.gameObject : null;
        pantallaEncendido = pantallaEncendido != null
            ? pantallaEncendido
            : encendido != null ? encendido.gameObject : null;
        textoEstadoApagado = textoEstadoApagado != null
            ? textoEstadoApagado
            : ObtenerPrimerTexto(apagado);
        textoEstadoEncendido = textoEstadoEncendido != null
            ? textoEstadoEncendido
            : ObtenerPrimerTexto(encendido);
        luzMonitorApagado = luzMonitorApagado != null
            ? luzMonitorApagado
            : ObtenerRenderer(apagado, "LuzApagado");
        luzMonitorEncendido = luzMonitorEncendido != null
            ? luzMonitorEncendido
            : ObtenerRenderer(encendido, "LuzEncendido");

        Transform status = BuscarRecursivo(robot, "StatusRobot");
        luzError = luzError != null ? luzError : ObtenerRenderer(status, "LuzError");
        luzPreparando = luzPreparando != null
            ? luzPreparando
            : ObtenerRenderer(status, "LuzPreparando");
        luzArreglado = luzArreglado != null
            ? luzArreglado
            : ObtenerRenderer(status, "LuzAreglado");

        Transform barra = BuscarRecursivo(robot, "PorcentajeCargado");
        barraBateria = barraBateria != null
            ? barraBateria
            : barra != null ? barra.GetComponent<Renderer>() : null;

        Transform retry = BuscarRecursivo(pantalla, "BotonReintentar");
        botonReintentar = botonReintentar != null
            ? botonReintentar
            : retry != null ? retry.GetComponent<Button>() : null;
    }

    private void ConfigurarHerramientas()
    {
        cargadorGrab = AsegurarAgarrable(cargador, 0.18f);
        ventiladorGrab = AsegurarAgarrable(ventilador, 0.12f);
        bateriaRepuestoGrab = AsegurarAgarrable(
            bateriaRepuestoPrivada,
            0.16f
        );
        temperaturaRepuestoGrab = AsegurarAgarrable(
            temperaturaRepuestoPrivada,
            0.10f
        );
        ConfigurarAgarreCercano(cargador, 0.045f);
        ConfigurarAgarreCercano(ventilador, 0.038f);
        ConfigurarAgarreCercano(bateriaRepuestoPrivada, 0.032f);
        ConfigurarAgarreCercano(temperaturaRepuestoPrivada, 0.030f);
        AlgoLabGrabProximityGate gateCargador =
            cargador != null
                ? cargador.GetComponent<AlgoLabGrabProximityGate>()
                : null;
        if (gateCargador != null)
        {
            gateCargador.Configurar(
                0.045f,
                null,
                cargador
            );
            // El cargador se puede tomar desde cualquiera de sus superficies,
            // no solamente desde el extremo donde nace el cable.
            gateCargador.usarSoloPuntoRespaldo = false;
        }

        EstabilizarEnBase(cargadorGrab);
        EstabilizarEnBase(ventiladorGrab);
        EstabilizarEnBase(bateriaRepuestoGrab);
        EstabilizarEnBase(temperaturaRepuestoGrab);
        ConfigurarFisicaAlSoltar(bateriaRepuestoGrab);
        ConfigurarFisicaAlSoltar(temperaturaRepuestoGrab);
        bateriaOriginal = BuscarRecursivo(robot, "BateriaExtraible");
        temperaturaOriginal =
            BuscarRecursivo(robot, "ModuloTemperaturaExtraible");
        Transform vidrioBateriaTransform =
            BuscarRecursivo(robot, "VidrioBateria");
        Transform vidrioTemperaturaTransform =
            BuscarRecursivo(robot, "VidrioTemperatura");
        vidrioBateria = vidrioBateriaTransform != null
            ? vidrioBateriaTransform.GetComponent<AlgoLabRobotBreakableGlass>()
            : null;
        vidrioTemperatura = vidrioTemperaturaTransform != null
            ? vidrioTemperaturaTransform.GetComponent<AlgoLabRobotBreakableGlass>()
            : null;
        bateriaOriginalGrab = AsegurarAgarrable(bateriaOriginal, 0.14f);
        temperaturaOriginalGrab =
            AsegurarAgarrable(temperaturaOriginal, 0.10f);
        ConfigurarComponenteInterno(
            bateriaOriginal,
            bateriaOriginalGrab,
            vidrioBateria,
            out bateriaOriginalPadre,
            out bateriaOriginalPosicionLocal,
            out bateriaOriginalRotacionLocal
        );
        bateriaOriginalEscalaLocal = bateriaOriginal != null
            ? bateriaOriginal.localScale
            : Vector3.one;
        ConfigurarComponenteInterno(
            temperaturaOriginal,
            temperaturaOriginalGrab,
            vidrioTemperatura,
            out temperaturaOriginalPadre,
            out temperaturaOriginalPosicionLocal,
            out temperaturaOriginalRotacionLocal
        );
        temperaturaOriginalEscalaLocal = temperaturaOriginal != null
            ? temperaturaOriginal.localScale
            : Vector3.one;
        if (bateriaRepuestoPrivada != null)
        {
            bateriaRepuestoPosicionInicial =
                bateriaRepuestoPrivada.localPosition;
            bateriaRepuestoRotacionInicial =
                bateriaRepuestoPrivada.localRotation;
            bateriaRepuestoEscalaInicial =
                bateriaRepuestoPrivada.localScale;
        }
        if (temperaturaRepuestoPrivada != null)
        {
            temperaturaRepuestoPosicionInicial =
                temperaturaRepuestoPrivada.localPosition;
            temperaturaRepuestoRotacionInicial =
                temperaturaRepuestoPrivada.localRotation;
            temperaturaRepuestoEscalaInicial =
                temperaturaRepuestoPrivada.localScale;
        }

        if (botonEnergia != null)
            botonEnergia.runtime = this;
        if (palancaX != null)
        {
            palancaX.runtime = this;
            palancaX.ejeRobot = AlgoLabLevel3RobotLever.EjeRobot.InclinacionX;
        }
        if (palancaY != null)
        {
            palancaY.runtime = this;
            palancaY.ejeRobot = AlgoLabLevel3RobotLever.EjeRobot.GiroY;
        }

        if (cable == null && panelHerramientas != null)
        {
            Transform existente = BuscarRecursivo(panelHerramientas, "CableDinamico");
            GameObject cableGo;
            if (existente == null)
            {
                cableGo = new GameObject("CableDinamico");
                cableGo.transform.SetParent(panelHerramientas, false);
                cableGo.AddComponent<LineRenderer>();
                cable = cableGo.AddComponent<AlgoLabLevel3FlexibleCable>();
            }
            else
            {
                cableGo = existente.gameObject;
                if (cableGo.GetComponent<LineRenderer>() == null)
                    cableGo.AddComponent<LineRenderer>();
                cable = cableGo.GetComponent<AlgoLabLevel3FlexibleCable>();
                if (cable == null)
                    cable = cableGo.AddComponent<AlgoLabLevel3FlexibleCable>();
            }
        }
        if (cable != null)
        {
            cable.extremoMonitor = anclaCableMonitor;
            cable.extremoCargador = anclaCableCargador;
        }

        AlgoLabRobotWorkshopVisual visual =
            visualRoot != null
                ? visualRoot.GetComponent<AlgoLabRobotWorkshopVisual>()
                : null;
        if (visual != null)
            visual.InteraccionesHerramientasExternas = true;
    }

    private void ActualizarHerramientas(float dt)
    {
        if (ventiladorGrab != null && ventiladorGrab.IsGrabbed)
        {
            SeleccionarMetodo("enfriar");
            if (aspasVentilador != null)
                aspasVentilador.Rotate(0f, 0f, 1080f * dt, Space.Self);

            SincronizarVidrioTemperaturaRoto();
            float distanciaActual = DistanciaVentiladorAlModulo();
            bool dentroZona = distanciaActual <= distanciaVentilador;
            if (dentroZona)
            {
                if (practica.Encendido && !ventiladorEnZona)
                {
                    practica.RegistrarError(
                        "ventilador_robot_encendido",
                        "+enfriar bloqueado: primero apaga el robot."
                    );
                }
                else if (!practica.VidrioTemperaturaRoto &&
                         !ventiladorEnZona)
                {
                    practica.RegistrarError(
                        "ventilador_vidrio_cerrado",
                        "El vidrio aun protege la temperatura."
                    );
                }
                else if (temperaturaOriginalInstalada || temperaturaRepuestoAcoplada)
                {
                    // El ventilador solo puede actuar sobre un modulo que este
                    // realmente instalado en el robot. Si el usuario lo dejo
                    // en el piso o en su mano, acercar el ventilador al hueco
                    // no modifica la temperatura.
                    float intensidad =
                        CalcularIntensidadVentilador(distanciaActual);
                    if (practica.AplicarEnfriamientoFisico(
                            enfriamientoPorSegundo * intensidad * dt))
                    {
                        MarcarMetodoCorrecto("enfriar");
                        if (practica.Temperatura <=
                            practica.temperaturaMaximaEncendido)
                        {
                            MarcarAtributoCorrecto("temperatura");
                        }
                    }
                }
            }
            ventiladorEnZona = dentroZona;
        }
        else
        {
            ventiladorEnZona = false;
        }

        if (cargadorGrab != null &&
            puntaCargador != null &&
            puertoCarga != null)
        {
            // La deteccion se hace contra la boca visible del puerto. La
            // profundidad se usa solamente al encajar, de modo que el usuario
            // no tenga que atravesar el robot para lograr la conexion.
            float distancia = Vector3.Distance(
                puntaCargador.position,
                puertoCarga.position
            );

            if (cargadorConectado && cargadorGrab.IsGrabbed &&
                (cargadorFueSoltadoTrasConexion ||
                 distancia > distanciaDesconexionCargador))
            {
                DesconectarCargador(true);
                cargadorDebeAlejarseAntesReconectar = true;
            }

            if (cargadorDebeAlejarseAntesReconectar &&
                distancia > Mathf.Max(
                    distanciaConexionCargador * 1.75f,
                    0.11f))
            {
                cargadorDebeAlejarseAntesReconectar = false;
            }

            if (!cargadorConectado &&
                cargadorGrab.IsGrabbed &&
                !cargadorDebeAlejarseAntesReconectar &&
                distancia <= distanciaConexionCargador)
            {
                cargadorConectado = true;
                cargadorFueSoltadoTrasConexion = false;
                SeleccionarMetodo("cargar");
                EncajarCargador();
                if (practica.Encendido)
                {
                    practica.RegistrarError(
                        "cargador_robot_encendido",
                        "+cargar bloqueado: primero apaga el robot."
                    );
                }
                MostrarMensajeTemporal("CARGADOR CONECTADO", 1.5f);
            }

            if (cargadorConectado)
            {
                if (!cargadorGrab.IsGrabbed)
                {
                    cargadorFueSoltadoTrasConexion = true;
                    EncajarCargador();
                }
                // Sin la bateria original dentro del robot no existe energia
                // que cargar. El cargador puede permanecer conectado, pero la
                // barra no avanza hasta que la bateria vuelva a su ranura.
                if (bateriaOriginalInstalada &&
                    practica.AplicarCargaFisica(cargaPorSegundo * dt))
                {
                    MarcarMetodoCorrecto("cargar");
                    if (practica.Energia >= practica.energiaMinimaEncendido)
                        MarcarAtributoCorrecto("bateria");
                }
            }
        }

        ActualizarComponentesOriginales();
        ActualizarRepuestosPrivados();
    }

    public float CalcularIntensidadVentilador(float distancia)
    {
        if (distancia > distanciaVentilador)
            return 0f;
        float cercania = Mathf.Clamp01(
            1f - Mathf.Max(0f, distancia) /
            Mathf.Max(0.01f, distanciaVentilador)
        );
        return Mathf.Lerp(
            0.12f,
            1.35f,
            cercania * cercania
        );
    }

    private void SincronizarVidrioTemperaturaRoto()
    {
        if (practica == null || practica.VidrioTemperaturaRoto)
            return;
        if (vidrioTemperatura != null && vidrioTemperatura.Roto)
        {
            practica.NotificarVidrioRoto(
                AlgoLabRobotBreakableGlass.Compartimiento.Temperatura
            );
        }
    }

    private float DistanciaVentiladorAlModulo()
    {
        if (ventilador == null)
            return float.PositiveInfinity;

        // No se usa el ancla vacia del hueco como si fuera el modulo: cuando
        // el componente esta fuera del robot el ventilador no debe enfriarlo.
        Transform moduloInstalado = null;
        if (temperaturaOriginalInstalada && temperaturaOriginal != null &&
            temperaturaOriginal.gameObject.activeInHierarchy)
        {
            moduloInstalado = temperaturaOriginal;
        }
        else if (temperaturaRepuestoAcoplada &&
                 temperaturaRepuestoPrivada != null &&
                 temperaturaRepuestoPrivada.gameObject.activeInHierarchy)
        {
            moduloInstalado = temperaturaRepuestoPrivada;
        }

        if (moduloInstalado == null)
            return float.PositiveInfinity;

        return DistanciaEntreSuperficies(ventilador, moduloInstalado);
    }

    private static float DistanciaEntreSuperficies(
        Transform origen,
        Transform destino)
    {
        if (origen == null || destino == null)
            return float.PositiveInfinity;

        Collider[] collidersOrigen =
            origen.GetComponentsInChildren<Collider>(true);
        Collider[] collidersDestino =
            destino.GetComponentsInChildren<Collider>(true);
        float mejor = float.PositiveInfinity;

        for (int i = 0; i < collidersOrigen.Length; i++)
        {
            Collider a = collidersOrigen[i];
            if (a == null || !a.enabled)
                continue;
            for (int j = 0; j < collidersDestino.Length; j++)
            {
                Collider b = collidersDestino[j];
                if (b == null || !b.enabled)
                    continue;
                Vector3 puntoA = a.ClosestPoint(b.bounds.center);
                Vector3 puntoB = b.ClosestPoint(puntoA);
                mejor = Mathf.Min(
                    mejor,
                    Vector3.Distance(puntoA, puntoB)
                );
            }
        }

        if (!float.IsPositiveInfinity(mejor))
            return mejor;

        Renderer[] renderersOrigen =
            origen.GetComponentsInChildren<Renderer>(true);
        Renderer[] renderersDestino =
            destino.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderersOrigen.Length; i++)
        {
            Renderer a = renderersOrigen[i];
            if (a == null || !a.enabled)
                continue;
            for (int j = 0; j < renderersDestino.Length; j++)
            {
                Renderer b = renderersDestino[j];
                if (b == null || !b.enabled)
                    continue;
                float desdeA = Mathf.Sqrt(
                    a.bounds.SqrDistance(b.bounds.center)
                );
                float desdeB = Mathf.Sqrt(
                    b.bounds.SqrDistance(a.bounds.center)
                );
                mejor = Mathf.Min(mejor, Mathf.Min(desdeA, desdeB));
            }
        }

        return float.IsPositiveInfinity(mejor)
            ? Vector3.Distance(origen.position, destino.position)
            : mejor;
    }

    private void ActualizarRepuestosPrivados()
    {
        ActualizarRepuestoPrivado(
            bateriaRepuestoGrab,
            bateriaRepuestoPrivada,
            BuscarRecursivo(robot, "RanuraBateria"),
            BuscarRecursivo(robot, "BateriaExtraible"),
            practica.VidrioBateriaRoto,
            ref bateriaRepuestoManipulada,
            ref bateriaRepuestoAcoplada,
            bateriaRepuestoPosicionInicial,
            bateriaRepuestoRotacionInicial,
            bateriaRepuestoEscalaInicial,
            bateriaOriginalRotacionLocal,
            practica.NotificarReemplazoPrivadoBateria,
            practica.NotificarRetiroReemplazoPrivadoBateria,
            "BATERÍA: DATO PRIVADO"
        );
        ActualizarRepuestoPrivado(
            temperaturaRepuestoGrab,
            temperaturaRepuestoPrivada,
            objetivoTemperatura,
            BuscarRecursivo(robot, "ModuloTemperaturaExtraible"),
            practica.VidrioTemperaturaRoto,
            ref temperaturaRepuestoManipulada,
            ref temperaturaRepuestoAcoplada,
            temperaturaRepuestoPosicionInicial,
            temperaturaRepuestoRotacionInicial,
            temperaturaRepuestoEscalaInicial,
            temperaturaOriginalRotacionLocal,
            practica.NotificarReemplazoPrivadoTemperatura,
            practica.NotificarRetiroReemplazoPrivadoTemperatura,
            "TEMPERATURA: DATO PRIVADO"
        );
    }

    private void ActualizarRepuestoPrivado(
        SimpleMRGrabbable grab,
        Transform repuesto,
        Transform objetivo,
        Transform original,
        bool vidrioRoto,
        ref bool fueManipulado,
        ref bool acoplado,
        Vector3 posicionBase,
        Quaternion rotacionBase,
        Vector3 escalaBase,
        Quaternion rotacionLocalInstalada,
        Action notificar,
        Action notificarRetiro,
        string mensaje)
    {
        if (grab == null || repuesto == null || objetivo == null)
            return;

        if (grab.IsGrabbed)
        {
            fueManipulado = true;
            if (acoplado)
                notificarRetiro?.Invoke();
            acoplado = false;
            SepararComponenteDelRobot(repuesto);
            ConfigurarFisicaAlSoltar(grab);
            SeleccionarAtributo(
                repuesto == bateriaRepuestoPrivada
                    ? "bateria"
                    : "temperatura"
            );
            return;
        }
        if (!fueManipulado || acoplado)
            return;

        // Los sistemas de manos pueden devolver el objeto al padre que tenia
        // cuando se inicio el agarre. Si ya no esta acoplado, se separa otra
        // vez para que no herede el giro del robot mientras esta afuera.
        SepararComponenteDelRobot(repuesto);

        Transform padreBase = panelHerramientas != null
            ? panelHerramientas
            : repuesto.parent;
        if (padreBase != null)
        {
            Vector3 baseMundo = padreBase.TransformPoint(posicionBase);
            if (Vector3.Distance(repuesto.position, baseMundo) <= 0.10f)
            {
                if (repuesto.parent != padreBase)
                    repuesto.SetParent(padreBase, true);
                DevolverRepuesto(
                    grab,
                    repuesto,
                    posicionBase,
                    rotacionBase,
                    escalaBase
                );
                ConfigurarFisicaAlSoltar(grab);
                fueManipulado = false;
                acoplado = false;
                notificarRetiro?.Invoke();
                MostrarMensajeTemporal(
                    "REPUESTO DEVUELTO AL PANEL",
                    1.3f
                );
                return;
            }
        }

        float distancia = Vector3.Distance(repuesto.position, objetivo.position);
        if (distancia > 0.13f)
            return;
        if (!vidrioRoto)
        {
            practica.NotificarHerramientaBloqueada(
                "El vidrio protege este atributo privado."
            );
            return;
        }
        if (original != null &&
            original.gameObject.activeInHierarchy &&
            Vector3.Distance(original.position, objetivo.position) < 0.10f)
        {
            MostrarMensajeTemporal("RETIRA PRIMERO EL COMPONENTE", 1.8f);
            return;
        }

        // El repuesto pasa a formar parte del mismo compartimiento que el
        // objetivo. Asi sigue al robot al girarlo y conserva la orientacion
        // horizontal del componente original, aunque haya sido tomado del
        // panel con otra rotacion.
        AcoplarRepuestoEnObjetivo(
            repuesto,
            objetivo,
            rotacionLocalInstalada
        );
        if (grab.Rigidbody != null)
        {
            grab.Rigidbody.linearVelocity = Vector3.zero;
            grab.Rigidbody.angularVelocity = Vector3.zero;
            grab.Rigidbody.useGravity = false;
            grab.Rigidbody.isKinematic = true;
        }
        acoplado = true;
        SeleccionarAtributo(
            repuesto == bateriaRepuestoPrivada
                ? "bateria"
                : "temperatura"
        );
        notificar?.Invoke();
        MostrarMensajeTemporal(mensaje, 2.4f);
    }

    private void ActualizarComponentesOriginales()
    {
        ActualizarComponenteOriginal(
            bateriaOriginal,
            bateriaOriginalGrab,
            BuscarRecursivo(robot, "RanuraBateria"),
            bateriaOriginalPadre,
            bateriaOriginalPosicionLocal,
            bateriaOriginalRotacionLocal,
            bateriaOriginalEscalaLocal,
            bateriaRepuestoAcoplada,
            ref bateriaOriginalManipulada,
            ref bateriaOriginalInstalada,
            "bateria"
        );
        ActualizarComponenteOriginal(
            temperaturaOriginal,
            temperaturaOriginalGrab,
            objetivoTemperatura,
            temperaturaOriginalPadre,
            temperaturaOriginalPosicionLocal,
            temperaturaOriginalRotacionLocal,
            temperaturaOriginalEscalaLocal,
            temperaturaRepuestoAcoplada,
            ref temperaturaOriginalManipulada,
            ref temperaturaOriginalInstalada,
            "temperatura"
        );
    }

    private void ActualizarComponenteOriginal(
        Transform componente,
        SimpleMRGrabbable grab,
        Transform objetivo,
        Transform padreOriginal,
        Vector3 posicionLocal,
        Quaternion rotacionLocal,
        Vector3 escalaLocal,
        bool repuestoOcupaRanura,
        ref bool manipulado,
        ref bool instalado,
        string atributo)
    {
        if (componente == null || grab == null || objetivo == null)
            return;

        if (grab.IsGrabbed)
        {
            manipulado = true;
            instalado = false;
            SepararComponenteDelRobot(componente);
            ConfigurarFisicaAlSoltar(grab);
            SeleccionarAtributo(atributo);
            return;
        }
        if (!instalado)
        {
            // El usuario puede soltarlo sin que el primer frame de agarre haya
            // pasado por el bloque anterior. Nunca debe quedar bajo el robot,
            // porque entonces heredaria el giro de las palancas.
            SepararComponenteDelRobot(componente);
        }
        if (!manipulado || instalado)
            return;
        if (Vector3.Distance(componente.position, objetivo.position) > 0.13f)
            return;
        if (repuestoOcupaRanura)
        {
            MostrarMensajeTemporal("RETIRA PRIMERO EL REPUESTO", 1.6f);
            return;
        }

        if (padreOriginal != null)
            componente.SetParent(padreOriginal, false);
        grab.PermitirMovimientoExterno();
        componente.localPosition = posicionLocal;
        componente.localRotation = rotacionLocal;
        componente.localScale = escalaLocal;
        EstabilizarEnBase(grab);
        instalado = true;
        manipulado = false;
        MarcarAtributoCorrecto(atributo);
        MostrarMensajeTemporal(
            atributo.ToUpperInvariant() + " ORIGINAL RESTAURADA",
            1.5f
        );
    }

    private void ReiniciarComponentesOriginales()
    {
        ReiniciarComponenteOriginal(
            bateriaOriginal,
            bateriaOriginalGrab,
            bateriaOriginalPadre,
            bateriaOriginalPosicionLocal,
            bateriaOriginalRotacionLocal,
            bateriaOriginalEscalaLocal
        );
        ReiniciarComponenteOriginal(
            temperaturaOriginal,
            temperaturaOriginalGrab,
            temperaturaOriginalPadre,
            temperaturaOriginalPosicionLocal,
            temperaturaOriginalRotacionLocal,
            temperaturaOriginalEscalaLocal
        );
        bateriaOriginalInstalada = bateriaOriginal != null;
        temperaturaOriginalInstalada = temperaturaOriginal != null;
        bateriaOriginalManipulada = false;
        temperaturaOriginalManipulada = false;
    }

    private void SepararComponenteDelRobot(Transform componente)
    {
        if (componente == null || robot == null ||
            !componente.IsChildOf(robot))
        {
            return;
        }

        // visualRoot es un padre estable que no gira con el robot. Si no esta
        // disponible usamos el objeto del runtime como raiz externa.
        Transform padreExterno = visualRoot != null &&
                                 !visualRoot.IsChildOf(robot)
            ? visualRoot
            : transform;
        CambiarPadrePreservandoEscalaMundo(componente, padreExterno);
    }

    private static void AcoplarRepuestoEnObjetivo(
        Transform repuesto,
        Transform objetivo,
        Quaternion rotacionLocalInstalada)
    {
        if (repuesto == null || objetivo == null)
            return;

        Vector3 escalaMundo = repuesto.lossyScale;
        Transform padreObjetivo = objetivo.parent;
        if (padreObjetivo != null)
        {
            repuesto.SetParent(padreObjetivo, true);
            repuesto.localPosition = objetivo.localPosition;
            // La pose original guardada es la fuente de verdad incluso
            // despues de que el componente original haya sido retirado.
            repuesto.localRotation = rotacionLocalInstalada;
            RestaurarEscalaMundo(repuesto, escalaMundo);
        }
        else
        {
            repuesto.SetParent(objetivo, true);
            repuesto.localPosition = Vector3.zero;
            repuesto.localRotation = rotacionLocalInstalada;
            RestaurarEscalaMundo(repuesto, escalaMundo);
        }
    }

    private static void CambiarPadrePreservandoEscalaMundo(
        Transform objetivo,
        Transform nuevoPadre)
    {
        if (objetivo == null)
            return;

        Vector3 escalaMundo = objetivo.lossyScale;
        objetivo.SetParent(nuevoPadre, true);
        RestaurarEscalaMundo(objetivo, escalaMundo);
    }

    private static void RestaurarEscalaMundo(
        Transform objetivo,
        Vector3 escalaMundo)
    {
        if (objetivo == null)
            return;

        Vector3 actual = objetivo.lossyScale;
        Vector3 local = objetivo.localScale;
        local.x *= DivisionSegura(escalaMundo.x, actual.x);
        local.y *= DivisionSegura(escalaMundo.y, actual.y);
        local.z *= DivisionSegura(escalaMundo.z, actual.z);
        objetivo.localScale = local;
    }

    private static float DivisionSegura(float numerador, float denominador)
    {
        return Mathf.Abs(denominador) > 0.00001f
            ? numerador / denominador
            : 1f;
    }

    private static void ConfigurarFisicaAlSoltar(SimpleMRGrabbable grab)
    {
        if (grab == null)
            return;

        grab.releaseMode = SimpleMRGrabbable.ReleaseMode.Physics;
        grab.useGravityOnRelease = true;
        // Los agarres de manos y controles ya llaman BeginGrab/EndGrab. Los
        // cambios de padre hechos para acoplar al robot no son agarres.
        grab.detectarAgarrePorCambioDePadre = false;
        grab.mantenerFlotandoAlSoltar = false;
        grab.hacerKinematicCuandoNoAgarrado = false;
        grab.congelarRigidbodyCuandoNoAgarrado = false;
        grab.conservarImpulsoAlSoltar = true;

        // Solo se evita la colision durante el spawn inicial. Despues del
        // primer agarre el objeto recupera colision y gravedad normales para
        // caer al suelo y poder ser recogido de nuevo.
        grab.sinColisionFisica = false;
        grab.sinColisionSoloCuandoNoAgarrado = false;
        grab.sinColisionInicialHastaPrimerAgarre = true;
        grab.congelarMientrasEsperaPrimerAgarre = true;
        grab.mantenerColliderNormalParaAgarre = true;
        grab.desactivarGravedadCuandoNoColisiona = true;
        grab.ignorarColisionesSolidasDeEscena = false;
    }

    private void DevolverRepuestoAlPanel(
        SimpleMRGrabbable grab,
        Transform repuesto,
        Vector3 posicion,
        Quaternion rotacion,
        Vector3 escala)
    {
        if (repuesto != null && panelHerramientas != null &&
            repuesto.parent != panelHerramientas)
        {
            repuesto.SetParent(panelHerramientas, false);
        }

        DevolverRepuesto(grab, repuesto, posicion, rotacion, escala);
        ConfigurarFisicaAlSoltar(grab);
    }

    private static void ReiniciarComponenteOriginal(
        Transform componente,
        SimpleMRGrabbable grab,
        Transform padre,
        Vector3 posicionLocal,
        Quaternion rotacionLocal,
        Vector3 escalaLocal)
    {
        if (componente == null)
            return;
        if (padre != null)
            componente.SetParent(padre, false);
        if (grab != null)
            grab.PermitirMovimientoExterno();
        componente.localPosition = posicionLocal;
        componente.localRotation = rotacionLocal;
        componente.localScale = escalaLocal;
        EstabilizarEnBase(grab);
    }

    private static void DevolverRepuesto(
        SimpleMRGrabbable grab,
        Transform repuesto,
        Vector3 posicion,
        Quaternion rotacion,
        Vector3 escala)
    {
        if (repuesto == null)
            return;
        if (grab != null)
            grab.PermitirMovimientoExterno();
        repuesto.localPosition = posicion;
        repuesto.localRotation = rotacion;
        repuesto.localScale = escala;
        if (grab != null && grab.Rigidbody != null)
        {
            grab.Rigidbody.isKinematic = false;
            grab.Rigidbody.linearVelocity = Vector3.zero;
            grab.Rigidbody.angularVelocity = Vector3.zero;
            grab.Rigidbody.useGravity = false;
            grab.Rigidbody.isKinematic = true;
        }
    }

    private void EncajarCargador()
    {
        if (cargador == null || puntaCargador == null || puertoCarga == null)
            return;

        Vector3 destinoPunta =
            puertoCarga.position +
            puertoCarga.forward * profundidadInsercionCargador;
        Vector3 ejeConector = puntaCargador.position - cargador.position;
        if (ejeConector.sqrMagnitude > 0.000001f)
        {
            Quaternion alinear = Quaternion.FromToRotation(
                ejeConector.normalized,
                puertoCarga.forward
            );
            cargador.rotation = alinear * cargador.rotation;
        }
        cargador.position += destinoPunta - puntaCargador.position;

        if (cargadorGrab != null && cargadorGrab.Rigidbody != null)
        {
            Rigidbody rb = cargadorGrab.Rigidbody;
            if (!rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            rb.useGravity = false;
            rb.isKinematic = true;
        }
    }

    private void DesconectarCargador(bool mostrarMensaje)
    {
        if (!cargadorConectado)
            return;
        cargadorConectado = false;
        cargadorFueSoltadoTrasConexion = false;
        if (cargadorGrab != null && cargadorGrab.Rigidbody != null)
        {
            Rigidbody rb = cargadorGrab.Rigidbody;
            rb.isKinematic = false;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        if (mostrarMensaje)
            MostrarMensajeTemporal("CARGADOR DESCONECTADO", 1.2f);
    }

    private void CapturarPoseRobot()
    {
        huesos.Clear();
        AgregarHueso("Arm.L", EjeHueso.X, 150f, 0.1f);
        AgregarHueso("Arm.R", EjeHueso.X, 150f, 1.7f);
        AgregarHueso("Leg.L", EjeHueso.X, 38f, 0.8f);
        AgregarHueso("Leg.R", EjeHueso.X, 38f, 2.3f);
        AgregarHueso("head", EjeHueso.Y, 75f, 1.1f);
    }

    private void AgregarHueso(
        string nombre,
        EjeHueso eje,
        float amplitud,
        float fase)
    {
        Transform hueso = BuscarRecursivo(modeloRobot, nombre);
        if (hueso == null)
            return;
        huesos.Add(new HuesoAnimado
        {
            transform = hueso,
            rotacionInicial = hueso.localRotation,
            eje = eje,
            amplitud = amplitud,
            fase = fase
        });
    }

    private void AnimarAveria(float dt)
    {
        float t = Time.time;
        for (int i = 0; i < huesos.Count; i++)
        {
            HuesoAnimado hueso = huesos[i];
            if (hueso.transform == null)
                continue;
            float ruido =
                Mathf.PerlinNoise(hueso.fase, t * 5.5f + hueso.fase) * 2f - 1f;
            float golpe = Mathf.Sin(t * (10f + i * 1.7f) + hueso.fase) * 0.35f;
            float angulo = (ruido + golpe) * hueso.amplitud;
            Quaternion giro = hueso.eje == EjeHueso.X
                ? Quaternion.Euler(angulo, 0f, 0f)
                : Quaternion.Euler(0f, angulo, 0f);
            Quaternion objetivo = giro * hueso.rotacionInicial;
            hueso.transform.localRotation = Quaternion.Slerp(
                hueso.transform.localRotation,
                objetivo,
                14f * dt
            );
        }

        if (modeloRobot != null)
        {
            Vector3 sacudida = new Vector3(
                Mathf.Sin(t * 22f),
                Mathf.Sin(t * 17f + 0.7f),
                Mathf.Sin(t * 19f + 1.6f)
            ) * 0.006f;
            modeloRobot.localPosition = posicionModeloInicial + sacudida;
        }
    }

    private void AnimarBaile(float dt)
    {
        float t = Time.time;
        for (int i = 0; i < huesos.Count; i++)
        {
            HuesoAnimado hueso = huesos[i];
            if (hueso.transform == null)
                continue;
            float amplitud = hueso.eje == EjeHueso.Y ? 45f :
                i < 2 ? 65f : 28f;
            float angulo = Mathf.Sin(t * 5.2f + hueso.fase) * amplitud;
            Quaternion giro = hueso.eje == EjeHueso.X
                ? Quaternion.Euler(angulo, 0f, 0f)
                : Quaternion.Euler(0f, angulo, 0f);
            Quaternion objetivo = giro * hueso.rotacionInicial;
            hueso.transform.localRotation = Quaternion.Slerp(
                hueso.transform.localRotation,
                objetivo,
                12f * dt
            );
        }

        if (modeloRobot != null)
        {
            Vector3 p = posicionModeloInicial;
            p.y += Mathf.Abs(Mathf.Sin(t * 5.2f)) * 0.035f;
            modeloRobot.localPosition = Vector3.Lerp(
                modeloRobot.localPosition,
                p,
                14f * dt
            );
        }
    }

    private void RestaurarPoseSuave(float dt)
    {
        for (int i = 0; i < huesos.Count; i++)
        {
            HuesoAnimado hueso = huesos[i];
            if (hueso.transform != null)
            {
                hueso.transform.localRotation = Quaternion.Slerp(
                    hueso.transform.localRotation,
                    hueso.rotacionInicial,
                    10f * dt
                );
            }
        }
        if (modeloRobot != null)
        {
            modeloRobot.localPosition = Vector3.Lerp(
                modeloRobot.localPosition,
                posicionModeloInicial,
                10f * dt
            );
        }
    }

    private void RestaurarPoseInmediata()
    {
        for (int i = 0; i < huesos.Count; i++)
        {
            if (huesos[i].transform != null)
                huesos[i].transform.localRotation = huesos[i].rotacionInicial;
        }
    }

    private void ExplotarRobot()
    {
        if (explotado || practica == null || practica.PracticaCompletada)
            return;

        explotado = true;
        cargadorConectado = false;
        practica.NotificarExplosion();
        PrepararPartesExplosion();

        Vector3 centro = ObtenerCentroRobot();
        for (int i = 0; i < partesExplosion.Count; i++)
        {
            ParteExplosion parte = partesExplosion[i];
            if (parte.transform == null)
                continue;

            parte.transform.SetParent(transform, true);
            Collider col = parte.transform.GetComponent<Collider>();
            if (col == null)
            {
                col = parte.transform.gameObject.AddComponent<BoxCollider>();
                parte.colliderAgregado = col;
            }
            Rigidbody rb = parte.transform.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = parte.transform.gameObject.AddComponent<Rigidbody>();
                parte.rigidbodyAgregado = rb;
            }
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.mass = 0.45f;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.linearDamping = 0.05f;
            rb.angularDamping = 0.08f;

            SimpleMRGrabbable agarrable =
                parte.transform.GetComponent<SimpleMRGrabbable>();
            if (agarrable == null)
            {
                agarrable =
                    parte.transform.gameObject.AddComponent<SimpleMRGrabbable>();
                parte.agarrableAgregado = agarrable;
            }
            ConfigurarParteExplosionAgarrable(agarrable);

            Vector3 direccion =
                (parte.transform.position - centro).normalized +
                Vector3.up * UnityEngine.Random.Range(0.35f, 0.9f) +
                UnityEngine.Random.insideUnitSphere * 0.45f;
            rb.AddForce(
                direccion.normalized *
                UnityEngine.Random.Range(fuerzaExplosion * 0.75f, fuerzaExplosion * 1.25f),
                ForceMode.Impulse
            );
            rb.AddTorque(
                UnityEngine.Random.insideUnitSphere * fuerzaExplosion * 0.9f,
                ForceMode.Impulse
            );
        }

        OcultarAccesoriosExplosion();
        if (arenaExplosion != null)
            arenaExplosion.SetActive(true);
        if (particulasExplosion != null)
        {
            particulasExplosion.transform.position = centro;
            particulasExplosion.Play(true);
        }
        if (botonReintentar != null)
            botonReintentar.gameObject.SetActive(true);
        MostrarMensajeTemporal("ROBOT DESTRUIDO", 1000f);

        AlgoLabVRUIButtonClicker clicker =
            FindFirstObjectByType<AlgoLabVRUIButtonClicker>(FindObjectsInactive.Include);
        if (clicker != null)
            clicker.ActualizarListaInteractuables();
    }

    private void PrepararPartesExplosion()
    {
        if (partesExplosion.Count > 0)
            return;

        string[] nombres =
        {
            "Brazo.L", "Brazo.R", "cabeza",
            "pierna.L", "pierna.R", "torso"
        };
        for (int i = 0; i < nombres.Length; i++)
        {
            Transform parte = BuscarRecursivo(modeloRobot, nombres[i]);
            if (parte == null)
                continue;
            partesExplosion.Add(new ParteExplosion
            {
                transform = parte,
                padre = parte.parent,
                posicionLocal = parte.localPosition,
                rotacionLocal = parte.localRotation,
                escalaLocal = parte.localScale
            });
        }
    }

    private void OcultarAccesoriosExplosion()
    {
        objetosOcultosExplosion.Clear();
        string[] nombres =
        {
            "CompartimientoTemperatura",
            "CompartimientoBateriaTrasero",
            "PorcentajeCargado",
            "PantallaTemperatura",
            "bocaRobot",
            "PantallaCorazon",
            "StatusRobot",
            "StatusBateria"
        };
        for (int i = 0; i < nombres.Length; i++)
        {
            Transform t = BuscarRecursivo(robot, nombres[i]);
            if (t != null && t.gameObject.activeSelf)
            {
                objetosOcultosExplosion.Add(t.gameObject);
                t.gameObject.SetActive(false);
            }
        }
    }

    private void RestaurarExplosion()
    {
        for (int i = 0; i < partesExplosion.Count; i++)
        {
            ParteExplosion parte = partesExplosion[i];
            if (parte.transform == null)
                continue;
            if (parte.agarrableAgregado != null)
                LiberarDeTodosLosControles(parte.agarrableAgregado);

            Rigidbody rb = parte.transform.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }
            parte.transform.SetParent(parte.padre, false);
            parte.transform.localPosition = parte.posicionLocal;
            parte.transform.localRotation = parte.rotacionLocal;
            parte.transform.localScale = parte.escalaLocal;

            if (parte.rigidbodyAgregado != null)
                Destroy(parte.rigidbodyAgregado);
            if (parte.colliderAgregado != null)
                Destroy(parte.colliderAgregado);
            if (parte.agarrableAgregado != null)
            {
                Destroy(parte.agarrableAgregado);
            }
            parte.rigidbodyAgregado = null;
            parte.colliderAgregado = null;
            parte.agarrableAgregado = null;
        }

        for (int i = 0; i < objetosOcultosExplosion.Count; i++)
        {
            if (objetosOcultosExplosion[i] != null)
                objetosOcultosExplosion[i].SetActive(true);
        }
        objetosOcultosExplosion.Clear();
    }

    private void CrearEfectos()
    {
        if (robot == null)
            return;

        Transform explosion = BuscarRecursivo(transform, "ExplosionRobotFX");
        if (explosion == null)
        {
            GameObject go = new GameObject("ExplosionRobotFX");
            go.transform.SetParent(transform, false);
            particulasExplosion = go.AddComponent<ParticleSystem>();
            particulasExplosion.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear
            );
            ParticleSystem.MainModule main = particulasExplosion.main;
            main.loop = false;
            main.duration = 0.75f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 1.2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.3f, 5.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.13f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.12f, 0.01f),
                new Color(1f, 0.82f, 0.05f)
            );
            main.maxParticles = 100;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;
            main.gravityModifier = 0.45f;
            ParticleSystem.EmissionModule emission = particulasExplosion.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[]
            {
                new ParticleSystem.Burst(0f, 72)
            });
            ParticleSystem.ShapeModule shape = particulasExplosion.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.16f;
            ConfigurarMaterialParticulasExplosion(particulasExplosion);
        }
        else
        {
            particulasExplosion = explosion.GetComponent<ParticleSystem>();
            if (particulasExplosion != null)
            {
                particulasExplosion.Stop(
                    true,
                    ParticleSystemStopBehavior.StopEmittingAndClear
                );
                ConfigurarMaterialParticulasExplosion(particulasExplosion);
            }
        }

        // El antiguo humo de temperatura no tenía material compatible con
        // URP y aparecía morado aun antes de una explosión. La temperatura ya
        // se comunica mediante la pantalla y el color rojo del módulo.
        if (objetivoTemperatura != null)
        {
            Transform calor = BuscarRecursivo(objetivoTemperatura, "CalorModuloFX");
            if (calor != null)
                calor.gameObject.SetActive(false);
            particulasCalor = null;
        }
    }

    private void CrearArenaExplosion()
    {
        Transform existente = BuscarHijoDirecto(transform, "ArenaExplosion");
        if (existente != null)
        {
            arenaExplosion = existente.gameObject;
            arenaExplosion.SetActive(false);
            return;
        }

        arenaExplosion = new GameObject("ArenaExplosion");
        arenaExplosion.transform.SetParent(transform, false);
        float piso = ObtenerPisoLocalRobot();
        CrearParedFisica(
            arenaExplosion.transform,
            "Piso",
            new Vector3(0f, piso - 0.05f, 0f),
            new Vector3(5f, 0.10f, 5f)
        );
        CrearParedFisica(
            arenaExplosion.transform,
            "ParedIzquierda",
            new Vector3(-2.5f, piso + 1.5f, 0f),
            new Vector3(0.10f, 3f, 5f)
        );
        CrearParedFisica(
            arenaExplosion.transform,
            "ParedDerecha",
            new Vector3(2.5f, piso + 1.5f, 0f),
            new Vector3(0.10f, 3f, 5f)
        );
        CrearParedFisica(
            arenaExplosion.transform,
            "ParedFrontal",
            new Vector3(0f, piso + 1.5f, -2.5f),
            new Vector3(5f, 3f, 0.10f)
        );
        CrearParedFisica(
            arenaExplosion.transform,
            "ParedTrasera",
            new Vector3(0f, piso + 1.5f, 2.5f),
            new Vector3(5f, 3f, 0.10f)
        );
        CrearParedFisica(
            arenaExplosion.transform,
            "Techo",
            new Vector3(0f, piso + 3.05f, 0f),
            new Vector3(5f, 0.10f, 5f)
        );
        arenaExplosion.SetActive(false);
    }

    private void ActualizarInterfaz()
    {
        if (practica == null)
            return;

        string tiempoGlobal = FormatearTiempo(tiempoPracticaRestante);
        if (panelProgreso == null)
        {
            panelProgreso = FindFirstObjectByType<AlgoLabProgressPanel>(
                FindObjectsInactive.Include
            );
        }
        if (panelProgreso != null)
        {
            panelProgreso.timeRemaining = tiempoGlobal;
            if (panelProgreso.timerText != null &&
                panelProgreso.currentMode ==
                AlgoLabProgressPanel.ModoActual.Practica)
            {
                panelProgreso.timerText.gameObject.SetActive(true);
                panelProgreso.timerText.text = tiempoGlobal;
            }
        }

        if (textoBateriaMonitor != null)
            textoBateriaMonitor.text = "BATERÍA\n" + practica.Energia + "%";
        if (textoTemperaturaMonitor != null)
            textoTemperaturaMonitor.text =
                "TEMPERATURA\n" + practica.Temperatura + " °C";
        if (textoTemperaturaRobot != null)
            textoTemperaturaRobot.text = practica.Temperatura + "°";

        if (textoAdvertencia != null)
        {
            if (explotado)
                textoAdvertencia.text = "FALLO TOTAL";
            else if (practica.PracticaCompletada)
                textoAdvertencia.text = "REPARACIÓN COMPLETA";
            else if (practica.Encendido && practica.Averiado)
                textoAdvertencia.text =
                    "FALLA CRÍTICA  //  " + tiempoRestante.ToString("0.0") + " s";
            else
                textoAdvertencia.text = "MODO SEGURO  //  ROBOT APAGADO";

        }

        if (textoAccion != null)
        {
            if (explotado)
                textoAccion.text = "PULSA REINTENTAR";
            else if (practica.PracticaCompletada)
                textoAccion.text = "ROBOT REPARADO";
            else if (practica.Encendido)
                textoAccion.text = "APÁGALO ANTES DE REPARAR";
            else
                textoAccion.text =
                    "OBJETIVO: 100%  //  " + practica.temperaturaObjetivo + " °C";
        }

        if (textoMensaje != null)
        {
            textoMensaje.gameObject.SetActive(true);
            string detalle = Time.unscaledTime <= mensajeHasta
                ? mensajeCorto
                : practica.Encendido && practica.Averiado
                    ? "PUNTOS " + practica.Puntaje + "  //  APAGA EL ROBOT"
                    : "PUNTOS " + practica.Puntaje;
            textoMensaje.text = detalle;
        }

        bool encendido = practica.Encendido && !explotado;
        if (pantallaApagado != null)
            pantallaApagado.SetActive(true);
        if (pantallaEncendido != null)
            pantallaEncendido.SetActive(true);
        if (textoEstadoApagado != null)
            textoEstadoApagado.gameObject.SetActive(!encendido);
        if (textoEstadoEncendido != null)
            textoEstadoEncendido.gameObject.SetActive(encendido);
        if (luzMonitorApagado != null)
            luzMonitorApagado.gameObject.SetActive(!encendido);
        if (luzMonitorEncendido != null)
            luzMonitorEncendido.gameObject.SetActive(encendido);

        AplicarLuz(
            luzError,
            new Color(1f, 0.025f, 0.01f),
            encendido && practica.Averiado
        );
        AplicarLuz(
            luzPreparando,
            new Color(1f, 0.72f, 0.02f),
            !encendido && !practica.PracticaCompletada && !explotado
        );
        AplicarLuz(
            luzArreglado,
            new Color(0.02f, 1f, 0.24f),
            practica.PracticaCompletada && !explotado
        );
        AplicarLuz(
            luzMonitorApagado,
            new Color(1f, 0.68f, 0.02f),
            !encendido
        );
        AplicarLuz(
            luzMonitorEncendido,
            practica.PracticaCompletada
                ? new Color(0.02f, 1f, 0.24f)
                : new Color(1f, 0.08f, 0.02f),
            encendido
        );

        ActualizarBarraBateria();
        if (botonReintentar != null)
            botonReintentar.gameObject.SetActive(explotado);
    }

    private static string FormatearTiempo(float segundos)
    {
        int total = Mathf.Max(0, Mathf.CeilToInt(segundos));
        return (total / 60).ToString("00") + ":" +
               (total % 60).ToString("00");
    }

    private void ActualizarBarraBateria()
    {
        if (barraBateria == null)
            return;

        float progreso = Mathf.Clamp01(practica.Energia / 100f);
        Transform barra = barraBateria.transform;
        Vector3 escala = escalaBarraInicial;
        escala.y = escalaBarraInicial.y * Mathf.Max(0.015f, progreso);
        barra.localScale = escala;
        Vector3 posicion = posicionBarraInicial;
        posicion.y = baseInferiorBarra + escala.y * 0.5f;
        barra.localPosition = posicion;
        AplicarColorRenderer(
            barraBateria,
            Color.Lerp(
                new Color(1f, 0.025f, 0.01f),
                new Color(0.02f, 1f, 0.20f),
                progreso
            )
        );
    }

    private void ActualizarEfectoCalor()
    {
        if (particulasCalor != null)
            particulasCalor.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear
            );
    }

    private void AlCambiarFeedback(string mensaje)
    {
        if (string.IsNullOrWhiteSpace(mensaje))
            return;

        string minuscula = mensaje.ToLowerInvariant();
        if (minuscula.Contains("privad") ||
            minuscula.Contains("fallo de arranque") ||
            minuscula.Contains("acceso denegado"))
        {
            MostrarMensajeTemporal("ERROR: DATO PRIVADO  -10 PUNTOS", 3.2f);
        }
        else if (minuscula.Contains("bloqueado") ||
                 minuscula.Contains("rechazado"))
        {
            MostrarMensajeTemporal("ACCIÓN BLOQUEADA", 2.2f);
        }
        else if (minuscula.Contains("vidrio"))
        {
            MostrarMensajeTemporal("COMPARTIMIENTO EXPUESTO", 2.2f);
        }
        else
        {
            string corto = mensaje.Length > 62
                ? mensaje.Substring(0, 59) + "..."
                : mensaje;
            MostrarMensajeTemporal(corto.ToUpperInvariant(), 2.4f);
        }
    }

    private void MostrarMensajeTemporal(string mensaje, float duracion)
    {
        mensajeCorto = mensaje;
        mensajeHasta = Time.unscaledTime + Mathf.Max(0.1f, duracion);
    }

    private static void ConfigurarAgarreCercano(
        Transform objetivo,
        float distancia)
    {
        if (objetivo == null)
            return;
        AlgoLabGrabProximityGate gate =
            objetivo.GetComponent<AlgoLabGrabProximityGate>();
        if (gate == null)
            gate = objetivo.gameObject.AddComponent<AlgoLabGrabProximityGate>();
        gate.Configurar(distancia, null, objetivo);
    }

    private static void EstabilizarEnBase(SimpleMRGrabbable grab)
    {
        if (grab == null || grab.Rigidbody == null)
            return;
        grab.Rigidbody.linearVelocity = Vector3.zero;
        grab.Rigidbody.angularVelocity = Vector3.zero;
        grab.Rigidbody.useGravity = false;
        grab.Rigidbody.isKinematic = true;
    }

    private static void ConfigurarSoltadoFlotante(SimpleMRGrabbable grab)
    {
        if (grab == null)
            return;
        grab.releaseMode = SimpleMRGrabbable.ReleaseMode.FloatInPlace;
        grab.useGravityOnRelease = false;
        grab.mantenerFlotandoAlSoltar = true;
        grab.conservarImpulsoAlSoltar = false;
    }

    private static void ConfigurarSinColisionCuandoNoAgarrado(
        SimpleMRGrabbable grab)
    {
        if (grab == null)
            return;
        grab.sinColisionFisica = true;
        grab.sinColisionInicialHastaPrimerAgarre = true;
        grab.congelarMientrasEsperaPrimerAgarre = true;
        grab.sinColisionSoloCuandoNoAgarrado = true;
        grab.usarTriggerParaNoColisionar = false;
        grab.mantenerColliderNormalParaAgarre = true;
        grab.desactivarGravedadCuandoNoColisiona = true;
        grab.ignorarColisionesSolidasDeEscena = true;
    }

    private static void ConfigurarComponenteInternoEstable(Transform componente)
    {
        if (componente == null)
            return;
        SimpleMRGrabbable grab = componente.GetComponent<SimpleMRGrabbable>();
        if (grab == null)
            return;
        ConfigurarSinColisionCuandoNoAgarrado(grab);
        EstabilizarEnBase(grab);
    }

    private static void ConfigurarComponenteInterno(
        Transform componente,
        SimpleMRGrabbable grab,
        AlgoLabRobotBreakableGlass vidrio,
        out Transform padre,
        out Vector3 posicionLocal,
        out Quaternion rotacionLocal)
    {
        padre = componente != null ? componente.parent : null;
        posicionLocal = componente != null
            ? componente.localPosition
            : Vector3.zero;
        rotacionLocal = componente != null
            ? componente.localRotation
            : Quaternion.identity;
        if (componente == null || grab == null)
            return;

        ConfigurarFisicaAlSoltar(grab);
        EstabilizarEnBase(grab);
        AlgoLabGrabProximityGate gate =
            componente.GetComponent<AlgoLabGrabProximityGate>();
        if (gate == null)
            gate = componente.gameObject.AddComponent<AlgoLabGrabProximityGate>();
        // La distancia sigue siendo de agarre directo, pero deja un margen
        // suficiente para el volumen real del mando dentro del hueco.
        gate.Configurar(0.065f, vidrio, componente);
        gate.usarSoloPuntoRespaldo = false;

        Collider collider = componente.GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = true;
            collider.isTrigger = false;
            if (collider is BoxCollider box)
            {
                box.size = new Vector3(
                    Mathf.Max(0.045f, box.size.x),
                    Mathf.Max(0.045f, box.size.y),
                    Mathf.Max(0.045f, box.size.z)
                );
            }
        }
    }

    private static void ConfigurarParteExplosionAgarrable(
        SimpleMRGrabbable grab)
    {
        if (grab == null)
            return;
        grab.perfilUso = SimpleMRGrabbable.PerfilUso.Personalizado;
        grab.releaseMode = SimpleMRGrabbable.ReleaseMode.Physics;
        grab.useGravityOnRelease = true;
        grab.ponerKinematicMientrasEstaAgarrado = true;
        grab.detectarAgarrePorCambioDePadre = true;
        grab.mantenerFlotandoAlSoltar = false;
        grab.hacerKinematicCuandoNoAgarrado = false;
        grab.congelarRigidbodyCuandoNoAgarrado = false;
        grab.sinColisionFisica = false;
        grab.sinColisionInicialHastaPrimerAgarre = false;
        grab.conservarImpulsoAlSoltar = true;
        grab.mostrarDebug = false;
    }

    private static void LiberarDeTodosLosControles(SimpleMRGrabbable objetivo)
    {
        if (objetivo == null)
            return;
        SimpleOvRGrabber[] controles = FindObjectsByType<SimpleOvRGrabber>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );
        for (int i = 0; i < controles.Length; i++)
        {
            if (controles[i] != null)
                controles[i].SoltarSiEstaAgarrando(objetivo);
        }
        if (objetivo.IsGrabbed)
            objetivo.ForcePhysicsRelease();
    }

    private void ConfigurarMaterialParticulasExplosion(
        ParticleSystem sistema)
    {
        if (sistema == null)
            return;
        ParticleSystemRenderer renderer =
            sistema.GetComponent<ParticleSystemRenderer>();
        if (renderer == null)
            return;

        Shader shader =
            Shader.Find("Universal Render Pipeline/Particles/Unlit") ??
            Shader.Find("Universal Render Pipeline/Unlit") ??
            Shader.Find("Sprites/Default");
        if (shader == null)
        {
            renderer.enabled = false;
            return;
        }

        if (materialParticulasExplosion != null)
            Destroy(materialParticulasExplosion);
        materialParticulasExplosion = new Material(shader)
        {
            name = "AlgoLabExplosionRuntimeMaterial",
            color = Color.white
        };
        if (materialParticulasExplosion.HasProperty("_BaseColor"))
            materialParticulasExplosion.SetColor("_BaseColor", Color.white);
        renderer.sharedMaterial = materialParticulasExplosion;
        renderer.enabled = true;
    }

    private static SimpleMRGrabbable AsegurarAgarrable(
        Transform objetivo,
        float masa)
    {
        if (objetivo == null)
            return null;

        Collider collider = objetivo.GetComponent<Collider>();
        if (collider == null)
        {
            BoxCollider box = objetivo.gameObject.AddComponent<BoxCollider>();
            AjustarBoxColliderAVisuales(objetivo, box);
        }

        Rigidbody rb = objetivo.GetComponent<Rigidbody>();
        if (rb == null)
            rb = objetivo.gameObject.AddComponent<Rigidbody>();
        rb.mass = masa;
        rb.useGravity = false;
        rb.isKinematic = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.linearDamping = 0.25f;
        rb.angularDamping = 0.30f;

        SimpleMRGrabbable grab = objetivo.GetComponent<SimpleMRGrabbable>();
        if (grab == null)
            grab = objetivo.gameObject.AddComponent<SimpleMRGrabbable>();
        grab.perfilUso = SimpleMRGrabbable.PerfilUso.Personalizado;
        grab.releaseMode = SimpleMRGrabbable.ReleaseMode.Physics;
        grab.useGravityOnRelease = true;
        grab.ponerKinematicMientrasEstaAgarrado = false;
        grab.detectarAgarrePorCambioDePadre = true;
        grab.mantenerFlotandoAlSoltar = false;
        grab.hacerKinematicCuandoNoAgarrado = false;
        grab.congelarRigidbodyCuandoNoAgarrado = false;
        grab.sinColisionFisica = false;
        grab.sinColisionInicialHastaPrimerAgarre = false;
        grab.conservarImpulsoAlSoltar = true;
        grab.mostrarDebug = false;
        return grab;
    }

    private static void AjustarBoxColliderAVisuales(
        Transform raiz,
        BoxCollider box)
    {
        Renderer[] renderers = raiz.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return;

        Bounds local = new Bounds(
            raiz.InverseTransformPoint(renderers[0].bounds.center),
            Vector3.zero
        );
        for (int i = 0; i < renderers.Length; i++)
        {
            Bounds b = renderers[i].bounds;
            Vector3 c = b.center;
            Vector3 e = b.extents;
            for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
            for (int z = -1; z <= 1; z += 2)
            {
                local.Encapsulate(
                    raiz.InverseTransformPoint(
                        c + Vector3.Scale(e, new Vector3(x, y, z))
                    )
                );
            }
        }
        box.center = local.center;
        box.size = local.size;
    }

    private static void AplicarLuz(
        Renderer renderer,
        Color colorActivo,
        bool activa)
    {
        if (renderer == null)
            return;
        Color color = activa
            ? colorActivo
            : new Color(0.018f, 0.022f, 0.025f);
        AplicarColorRenderer(renderer, color);
    }

    private static void AplicarColorRenderer(Renderer renderer, Color color)
    {
        if (renderer == null)
            return;
        MaterialPropertyBlock bloque = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(bloque);
        bloque.SetColor("_BaseColor", color);
        bloque.SetColor("_Color", color);
        bloque.SetColor("_EmissionColor", color * 2.8f);
        renderer.SetPropertyBlock(bloque);
    }

    private Vector3 ObtenerCentroRobot()
    {
        Renderer[] renderers = modeloRobot != null
            ? modeloRobot.GetComponentsInChildren<Renderer>(true)
            : Array.Empty<Renderer>();
        if (renderers.Length == 0)
            return robot != null ? robot.position : transform.position;
        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            b.Encapsulate(renderers[i].bounds);
        return b.center;
    }

    private float ObtenerPisoLocalRobot()
    {
        Renderer[] renderers = modeloRobot != null
            ? modeloRobot.GetComponentsInChildren<Renderer>(true)
            : Array.Empty<Renderer>();
        if (renderers.Length == 0)
            return -0.75f;
        float minimo = float.PositiveInfinity;
        for (int i = 0; i < renderers.Length; i++)
        {
            Vector3 p = transform.InverseTransformPoint(
                new Vector3(
                    renderers[i].bounds.center.x,
                    renderers[i].bounds.min.y,
                    renderers[i].bounds.center.z
                )
            );
            minimo = Mathf.Min(minimo, p.y);
        }
        return float.IsFinite(minimo) ? minimo : -0.75f;
    }

    private static void CrearParedFisica(
        Transform parent,
        string nombre,
        Vector3 posicion,
        Vector3 tamano)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = posicion;
        BoxCollider box = go.AddComponent<BoxCollider>();
        box.size = tamano;
    }

    private static TMP_Text ObtenerTexto(
        TMP_Text actual,
        Transform raiz,
        string nombre)
    {
        if (actual != null)
            return actual;
        Transform t = BuscarRecursivo(raiz, nombre);
        return t != null ? t.GetComponent<TMP_Text>() : null;
    }

    private static TMP_Text ObtenerPrimerTexto(Transform raiz)
    {
        if (raiz == null)
            return null;
        TMP_Text[] textos = raiz.GetComponentsInChildren<TMP_Text>(true);
        return textos != null && textos.Length > 0 ? textos[0] : null;
    }

    private static Renderer ObtenerRenderer(Transform raiz, string nombre)
    {
        Transform t = BuscarRecursivo(raiz, nombre);
        return t != null ? t.GetComponent<Renderer>() : null;
    }

    private static Transform BuscarPrimero(
        Transform raiz,
        params string[] nombres)
    {
        if (raiz == null)
            return null;
        for (int i = 0; i < nombres.Length; i++)
        {
            Transform encontrado = BuscarRecursivo(raiz, nombres[i]);
            if (encontrado != null)
                return encontrado;
        }
        return null;
    }

    private static Transform BuscarHijoDirecto(Transform raiz, string nombre)
    {
        if (raiz == null)
            return null;
        for (int i = 0; i < raiz.childCount; i++)
        {
            Transform hijo = raiz.GetChild(i);
            if (string.Equals(
                    hijo.name,
                    nombre,
                    StringComparison.OrdinalIgnoreCase))
                return hijo;
        }
        return null;
    }

    private static Transform BuscarRecursivo(Transform raiz, string nombre)
    {
        if (raiz == null)
            return null;
        if (string.Equals(
                raiz.name,
                nombre,
                StringComparison.OrdinalIgnoreCase))
            return raiz;

        for (int i = 0; i < raiz.childCount; i++)
        {
            Transform encontrado = BuscarRecursivo(raiz.GetChild(i), nombre);
            if (encontrado != null)
                return encontrado;
        }
        return null;
    }
}
