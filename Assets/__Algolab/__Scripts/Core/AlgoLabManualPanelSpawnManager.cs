using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class AlgoLabManualPanelSpawnManager : MonoBehaviour
{
    public static AlgoLabManualPanelSpawnManager Instance { get; private set; }

    [System.Serializable]
    public class PanelManualInfo
    {
        [Header("Panel")]
        public Transform panelRoot;

        [Tooltip("Si est  apagado, este panel no se ubica autom ticamente.")]
        public bool activo = true;

        [Header("Posici n manual respecto a la cabeza")]
        [Tooltip("X = derecha/izquierda, Y = altura, Z = frente/atr s respecto a donde mira la cabeza.")]
        public Vector3 posicionLocal = new Vector3(0f, 0f, 0.6f);

        [Header("Rotaci n manual opcional")]
        [Tooltip("Si est  apagado, el panel mirar  hacia la cabeza.")]
        public bool usarRotacionManual = false;

        [Tooltip("Rotaci n local respecto a la direcci n inicial de la cabeza.")]
        public Vector3 rotacionLocalEuler = Vector3.zero;

        [Header("Ajustes del panel")]
        [Tooltip("Act valo si el panel queda mirando al rev s.")]
        public bool invertirFrente = false;

        [Header("Altura despu s de mover con grab")]
        [Tooltip("Si est  activo, cuando el usuario suelte este panel se guarda la posici n actual y luego solo se ajusta la altura Y al sentarse o pararse.")]
        public bool ajustarAlturaDespuesDeMover = true;

        [Tooltip("Si est  activo, despu s de soltar el panel mantiene X/Z donde el usuario lo dej  y solo cambia Y.")]
        public bool mantenerDondeUsuarioLoDejo = true;

        [HideInInspector] public bool estaAgarradoPorUsuario = false;
        [HideInInspector] public bool fueMovidoPorUsuario = false;
        [HideInInspector] public Vector3 posicionMundoAlSoltar = Vector3.zero;
        [HideInInspector] public float alturaReferenciaAlSoltar = 0f;

        [HideInInspector] public bool layoutInicialAplicado = false;
        [HideInInspector] public Vector3 ultimaPosicionAplicadaPorManager = Vector3.zero;
    }

    [System.Serializable]
    public class ObjetoFrontalInfo
    {
        [Header("Identificaci n")]
        public string nombreObjeto = "cubo";

        [Header("Prefab")]
        public GameObject prefab;

        [Header("Escala")]
        [Tooltip("Si est  activo, el objeto aparecer  con la escala real/original del prefab.")]
        public bool usarEscalaOriginalDelPrefab = true;

        [Tooltip("Solo se usa si Usar Escala Original Del Prefab est  apagado.")]
        public Vector3 escalaManual = Vector3.one;

        [Header("Rotaci n")]
        [Tooltip("Si est  activo, el objeto se orienta hacia la cabeza.")]
        public bool mirarHaciaLaCabeza = false;

        public bool soloRotacionY = true;

        [Tooltip("Act valo si el objeto queda mirando al rev s.")]
        public bool invertirFrente = false;

        [Tooltip("Solo se usa si Mirar Hacia La Cabeza est  apagado.")]
        public Vector3 rotacionLocalEuler = Vector3.zero;
    }


    [System.Serializable]
    public class ObjetoAlturaDinamicaInfo
    {
        [Header("Objeto registrado")]
        public Transform objetivo;

        [Tooltip("Si est  activo, solo se ajusta la altura Y y se mantiene X/Z donde qued  el objeto.")]
        public bool ajustarSoloY = true;

        [Tooltip("Diferencia de altura entre el objeto y la referencia manual. Se calcula al registrarlo.")]
        public float offsetYRespectoReferencia = 0f;

        [Tooltip("Si est  activo, este objeto se seguir  ajustando cuando el usuario se siente o se pare.")]
        public bool activo = true;

        [HideInInspector] public Vector3 posicionLocalRespectoReferencia;

        [HideInInspector] public Vector3 posicionMundoAlRegistrar;
        [HideInInspector] public float alturaReferenciaAlRegistrar;
        [HideInInspector] public bool pausadoPorGrab = false;
        [HideInInspector] public bool registradoDesdeGrabHandle = false;
    }

    [Header("Cabeza / c mara")]
    [Tooltip("Asigna aqu  CenterEyeAnchor.")]
    public Transform headReference;

    [Header("Referencia vac a")]
    [Tooltip("Objeto vac o que se pondr  en la posici n de la cabeza al iniciar. Si est  vac o, se usa este mismo objeto.")]
    public Transform referenciaManual;

    [Header("Paneles")]
    public List<PanelManualInfo> paneles = new List<PanelManualInfo>();

    [Header("Configuraci n inicial")]
    [Tooltip("Si est  activo, la referencia se coloca en la cabeza cuando inicia la escena.")]
    public bool colocarReferenciaEnCabezaAlIniciar = true;

    [Tooltip("Si est  activo, los paneles se ubican autom ticamente al iniciar.")]
    public bool ubicarPanelesAlIniciar = true;

    [Tooltip("Si est  activo, solo se usa la rotaci n horizontal de la cabeza.")]
    public bool usarSoloRotacionY = true;

    [Tooltip("Altura final global del punto de referencia. Sentado recomendado: 1.1. Parado recomendado: 2.0.")]
    public float offsetAlturaGlobal = 1.1f;

    [Tooltip("Si est  activo, Offset Altura Global se usa como altura final del mundo. Recomendado para MR/Quest. Si est  apagado, se suma a la altura actual de la cabeza como antes.")]
    public bool usarAlturaGlobalComoAlturaFinal = true;

    [Header("Entrada usuario / login")]
    [Tooltip("Arrastra aqu  el SessionManager. Si queda vac o, se busca autom ticamente.")]
    public AlgoLabSessionManager sessionManager;

    [Tooltip("Si est  activo, cuando el usuario inicia sesi n o entra como invitado, los paneles se colocan UNA SOLA VEZ al frente de donde est  mirando en ese momento.")]
    public bool reubicarFrenteUnaVezAlEntrar = true;

    [Tooltip("Peque a espera para que se oculte el login y la c mara termine de actualizar antes de reubicar.")]
    public float retrasoReubicarDespuesDeEntrar = 0.15f;

    [Tooltip("Si est  activo, al entrar tambi n se spawnea el objeto frontal inicial configurado abajo. Esto se hace solo una vez al entrar.")]
    public bool spawnearObjetoFrontalAlEntrar = false;

    [Header("Altura autom tica sentado / parado")]
    [Tooltip("Si est  activo, revisa todo el tiempo si el usuario est  sentado o parado y mueve los paneles suavemente hacia la altura correcta.")]
    public bool ajustarAlturaEnTiempoReal = true;

    [Tooltip("Si est  activo, usa la altura local de la cabeza para detectar sentado/parado. Recomendado con CenterEyeAnchor en Quest.")]
    public bool usarAlturaLocalCabezaParaPostura = true;

    [Tooltip("Si la altura de ojos es menor o igual a este valor, se considera sentado.")]
    public float alturaMaximaSentado = 1.35f;

    [Tooltip("Si la altura de ojos es mayor o igual a este valor, se considera parado. Deja separaci n para evitar errores.")]
    public float alturaMinimaParado = 1.60f;

    [Tooltip("Compatibilidad. Si quieres una sola l nea de corte, usa este valor. Normalmente d jalo igual o cerca de Altura Minima Parado.")]
    public float umbralAlturaCabezaParado = 1.60f;

    [Tooltip("Altura final cuando el usuario est  sentado.")]
    public float alturaGlobalSentado = 1.1f;

    [Tooltip("Altura final cuando el usuario est  parado.")]
    public float alturaGlobalParado = 2f;

    [Tooltip("Si la altura cae en la zona dudosa, antes de detectar una postura se asume sentado. Evita que sentado se confunda con parado.")]
    public bool asumirSentadoAntesDePrimeraDeteccion = true;

    [Tooltip("Si est  activo, el cambio de altura sube o baja con suavizado.")]
    public bool usarSmoothCambioAltura = true;

    [Tooltip("Tiempo del suavizado vertical al cambiar entre sentado/parado.")]
    public float tiempoSmoothCambioAltura = 0.35f;

    [Tooltip("Velocidad m xima vertical del movimiento de altura.")]
    public float velocidadMaximaCambioAltura = 4f;

    [Tooltip("Diferencia m nima para reubicar paneles mientras cambia la altura.")]
    public float umbralActualizarAltura = 0.005f;

    [Header("Rotaci n hacia usuario")]
    [Tooltip("Si est  activo, los paneles miran hacia la cabeza al ubicarse.")]
    public bool hacerQueMirenALaCabeza = true;

    [Tooltip("Si est  activo, al mirar a la cabeza solo rotan en Y.")]
    public bool soloRotacionYPanel = true;

    [Header("Objeto frontal")]
    [Tooltip("Si est  activo, este manager tambi n puede spawnear objetos al frente.")]
    public bool usarObjetoFrontal = true;

    [Tooltip("Si est  activo, el objeto frontal usa la referencia manual fija. Si est  apagado, usa la cabeza actual.")]
    public bool objetoFrontalUsarReferenciaManual = true;

    [Tooltip("Offset vertical extra solo para el objeto frontal. No afecta los paneles.")]
    public float offsetAlturaObjetoFrontal = 0f;

    [Tooltip("Si est  activo, los objetos que se spawnean al frente tambi n suben o bajan con la altura sentado/parado.")]
    public bool objetoFrontalAfectadoPorAlturaPostura = true;

    [Tooltip("Si est  activo y el objeto frontal NO usa la referencia manual, usa la altura suavizada de la referencia para no quedar pegado a la altura cruda de la cabeza.")]
    public bool objetoFrontalUsarAlturaSuavizadaReferencia = true;

    [Header("Altura din mica para cualquier objeto spawneado")]
    [Tooltip("Si est  activo, cualquier objeto registrado se corrige en altura cuando el usuario se sienta o se para.")]
    public bool actualizarObjetosRegistradosEnTiempoReal = true;

    [Tooltip("Si est  activo, cada objeto frontal que spawnea este manager queda registrado autom ticamente para ajustar su altura en tiempo real.")]
    public bool registrarObjetosFrontalesAutomaticamente = true;

    [Tooltip("Si est  activo, los objetos registrados mantienen X/Z y solo suben o bajan en Y. Recomendado para que no se muevan de frente todo el tiempo.")]
    public bool objetosRegistradosAjustarSoloY = true;

    [Tooltip("Si est  activo, NO registra los paneles que ya est n en la lista Paneles, para evitar temblores o doble movimiento.")]
    public bool noRegistrarPanelesControladosPorManager = true;

    [Tooltip("Si est  activo, los objetos registrados usan una base estable: Y inicial + cambio de altura de la referencia. Evita que suban y bajen sin control.")]
    public bool usarRegistroAlturaEstable = true;

    [Tooltip("Si est  activo, al registrar un objeto se corrige de inmediato. Si ves saltos raros, d jalo apagado.")]
    public bool corregirObjetoInmediatamenteAlRegistrar = false;

    [Tooltip("Lista de objetos que deben seguir la altura sentado/parado. Puedes arrastrar aqu  objetos spawneados si otro script los crea. No metas aqu  los paneles ya controlados por la lista Paneles.")]
    public List<ObjetoAlturaDinamicaInfo> objetosAlturaDinamicaRegistrados = new List<ObjetoAlturaDinamicaInfo>();

    [Header("Integraci n con agarre de paneles")]
    [Tooltip("Si est  activo, este mismo manager se conecta a los AlgoLabPanelGrabHandle para pausar la correcci n de altura mientras se agarra y recalcularla al soltar.")]
    public bool integrarGrabDePanelesEnManager = true;

    [Tooltip("Busca autom ticamente los GrabHandle de los paneles configurados y de objetos spawneados.")]
    public bool buscarGrabHandlesAutomaticamente = true;

    [Tooltip("Si aparecen paneles nuevos despu s de iniciar, el manager seguir  buscando GrabHandle cada cierto tiempo.")]
    public bool buscarGrabHandlesDinamicosEnTiempoReal = true;

    [Tooltip("Cada cu ntos segundos busca nuevos GrabHandle creados en runtime.")]
    public float intervaloBuscarGrabHandles = 0.75f;

    [Tooltip("Si un GrabHandle no pertenece a un panel de la lista Paneles, se registra como objeto din mico para ajustar solo altura.")]
    public bool registrarGrabHandlesExternosComoAlturaDinamica = true;

    [Tooltip("Mientras el usuario tenga agarrado el panel u objeto, la altura din mica se pausa para no pelear con el grab.")]
    public bool pausarAlturaMientrasEstaAgarrado = true;

    [Tooltip("Cuando el usuario suelta, se recalcula la diferencia de altura desde la posici n donde lo dej .")]
    public bool recalcularAlturaAlSoltar = true;

    [Tooltip("Si est  activo, los paneles manipulables en la lista Paneles tambi n mantienen su X/Z actual y solo ajustan Y al sentarse o pararse.")]
    public bool panelesManipulablesMantienenXZAlSoltar = true;

    [Tooltip("Si está activo, cualquier panel de la lista Paneles que tenga AlgoLabPanelGrabHandle se marca automáticamente como manipulable, aunque el check no esté activado en el Inspector.")]
    public bool activarModoManipulableAutomaticoParaPanelesConGrab = true;

    [Tooltip("Si está activo, el manager revisa internamente si el GrabHandle está agarrando, aunque el UnityEvent del GrabHandle no se dispare.")]
    public bool detectarGrabPorPollingInterno = true;

    [Tooltip("Si está activo, cuando el panel está agarrado se bloquea completamente el layout del spawner para que no lo devuelva a su posición inicial.")]
    public bool bloquearLayoutMientrasPanelAgarrado = true;

    [Tooltip("Si está activo, el panel manipulado nunca vuelve al layout del spawner después de que el usuario lo movió; solo cambia su altura Y.")]
    public bool noRegresarPanelManipuladoAlLayout = true;

    [Tooltip("Si est  activo, el panel se vuelve a mirar hacia la cabeza cuando se suelta. D jalo apagado si el usuario debe conservar la rotaci n que dej .")]
    public bool rotarPanelManipuladoAlSoltarHaciaCabeza = false;

    [Tooltip("Si est  activo, el manager detecta si un panel manipulable cambi  de posici n aunque el GrabHandle no dispare eventos. Esto evita que el panel vuelva al layout del spawner.")]
    public bool detectarPanelMovidoSinEventoGrab = true;

    [Tooltip("Distancia m nima para considerar que el usuario movi  manualmente un panel. Recomendado 0.03 a 0.08.")]
    public float distanciaMinimaPanelMovidoPorUsuario = 0.04f;

    [Tooltip("Si est  activo, la detecci n de movimiento manual se hace en LateUpdate, despu s de que el GrabHandle haya movido el panel.")]
    public bool detectarPanelMovidoEnLateUpdate = true;

    [Tooltip("Si est  activo, cuando el manager detecta que el panel fue movido por el usuario, guarda esa posici n como nueva base.")]
    public bool guardarPosicionActualCuandoDetectaMovimiento = true;

    [Tooltip("Si est  activo, al entrar con login o invitado los paneles manipulados vuelven al layout frontal inicial.")]
    public bool resetearPanelesManipuladosAlEntrar = true;

    private readonly List<AlgoLabPanelGrabHandle> grabHandlesConectados =
        new List<AlgoLabPanelGrabHandle>();
    private readonly Dictionary<AlgoLabPanelGrabHandle, UnityAction> accionesInicioGrab =
        new Dictionary<AlgoLabPanelGrabHandle, UnityAction>();
    private readonly Dictionary<AlgoLabPanelGrabHandle, UnityAction> accionesFinGrab =
        new Dictionary<AlgoLabPanelGrabHandle, UnityAction>();

    private float proximaBusquedaGrabHandles = 0f;

    [Tooltip("Posici n fija del objeto frontal respecto a la referencia manual o cabeza. X lados, Y altura, Z frente.")]
    public Vector3 posicionLocalObjetoFrontal = new Vector3(0f, -0.4f, 1.4f);

    [Tooltip("Si est  activo, aparece un objeto frontal al iniciar.")]
    public bool spawnearObjetoFrontalAlIniciar = false;

    public int indiceObjetoFrontalInicial = 0;

    [Header("Objetos frontales disponibles")]
    public List<ObjetoFrontalInfo> objetosFrontales = new List<ObjetoFrontalInfo>();

    [Header("Animaci n objeto frontal")]
    public float duracionAparecerObjeto = 0.35f;
    public float duracionDesaparecerObjeto = 0.25f;

    [Tooltip("Multiplicador inicial de escala para la animaci n. 0.05 = aparece peque o y crece hasta su escala real.")]
    public float escalaInicialObjeto = 0.05f;

    [Tooltip("Si est  activo, destruye el objeto anterior al cambiar. Si est  apagado, solo lo desactiva.")]
    public bool destruirObjetoAnterior = true;

    [Header("Gizmos")]
    public bool dibujarGizmosSiempre = true;

    [Header("Debug")]
    public bool mostrarDebug = false;

    private GameObject objetoFrontalActual;
    private Coroutine rutinaObjetoFrontal;
    private int indiceObjetoFrontalActual = -1;
    private ObjetoFrontalInfo infoObjetoFrontalActual;

    private Coroutine rutinaReubicarDespuesDeEntrar;
    private bool eventosSesionConectados;
    private bool posturaInicializada;
    private bool usuarioDetectadoParado;
    private float velocidadSmoothAltura;
    private float alturaObjetivoActual;

    public GameObject ObjetoFrontalActual => objetoFrontalActual;
    public int IndiceObjetoFrontalActual => indiceObjetoFrontalActual;
    public bool PosturaDetectadaParado => usuarioDetectadoParado;
    public float AlturaOjosActual => ObtenerAlturaOjosParaPostura();
    public float AlturaPanelesActual => referenciaManual != null
        ? referenciaManual.position.y
        : offsetAlturaGlobal;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        BuscarSessionManager();
    }

    private void OnEnable()
    {
        ConectarEventosSesion();
    }

    private void OnDisable()
    {
        DesconectarEventosSesion();
    }

    private void OnDestroy()
    {
        DesconectarEventosSesion();
        DesconectarGrabHandlesPanelesYObjetos();

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Start()
    {
        BuscarCabeza();

        if (referenciaManual == null)
        {
            referenciaManual = transform;
        }

        BuscarSessionManager();
        ConectarEventosSesion();
        ConectarGrabHandlesPanelesYObjetos();

        ActualizarPosturaDetectada(true);
        offsetAlturaGlobal = ObtenerAlturaObjetivoSegunPostura();
        alturaObjetivoActual = offsetAlturaGlobal;

        if (colocarReferenciaEnCabezaAlIniciar)
        {
            ActualizarReferenciaDesdeCabeza();
        }

        if (ubicarPanelesAlIniciar)
        {
            UbicarPaneles();
        }

        if (usarObjetoFrontal && spawnearObjetoFrontalAlIniciar)
        {
            CambiarObjetoFrontalPorIndice(indiceObjetoFrontalInicial);
        }

        if (reubicarFrenteUnaVezAlEntrar &&
            sessionManager != null &&
            sessionManager.SesionIniciada)
        {
            ReubicarFrenteUnaVezDespuesDeEntrar();
        }
    }

    private void Update()
    {
        if (integrarGrabDePanelesEnManager &&
            buscarGrabHandlesDinamicosEnTiempoReal &&
            Time.time >= proximaBusquedaGrabHandles)
        {
            proximaBusquedaGrabHandles = Time.time + Mathf.Max(0.1f, intervaloBuscarGrabHandles);
            ConectarGrabHandlesPanelesYObjetos();
        }

        // Primero detectamos si el usuario está agarrando o soltó un panel.
        // Así el cambio de altura no vuelve a mandar el panel al layout del spawner.
        ActualizarEstadoGrabHandlesPorPolling();

        ActualizarAlturaSegunPosturaEnTiempoReal();

        if (actualizarObjetosRegistradosEnTiempoReal)
        {
            ActualizarObjetosAlturaDinamicaRegistrados();
        }
    }

    private void LateUpdate()
    {
        // LateUpdate corre después del Update del GrabHandle.
        // Aquí guardamos exactamente la posición donde el usuario soltó el panel.
        ActualizarEstadoGrabHandlesPorPolling();

        if (detectarPanelMovidoEnLateUpdate)
        {
            DetectarPanelesMovidosPorUsuarioSinEventoGrab();
        }
    }

    private void BuscarCabeza()
    {
        if (headReference != null)
        {
            // Si por accidente asignaste el rig/padre, usa mejor el CenterEyeAnchor hijo.
            Transform centerEyeHijo = BuscarHijoPorNombre(headReference, "CenterEyeAnchor");

            if (centerEyeHijo != null && headReference.name != "CenterEyeAnchor")
            {
                headReference = centerEyeHijo;
            }

            return;
        }

        GameObject centerEye = GameObject.Find("CenterEyeAnchor");

        if (centerEye != null)
        {
            headReference = centerEye.transform;
            return;
        }

        if (Camera.main != null)
        {
            headReference = Camera.main.transform;
        }
    }

    private Transform BuscarHijoPorNombre(Transform raiz, string nombre)
    {
        if (raiz == null || string.IsNullOrWhiteSpace(nombre))
        {
            return null;
        }

        Transform[] hijos = raiz.GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < hijos.Length; i++)
        {
            if (hijos[i] != null && hijos[i].name == nombre)
            {
                return hijos[i];
            }
        }

        return null;
    }

    private void BuscarSessionManager()
    {
        if (sessionManager != null)
        {
            return;
        }

        sessionManager = AlgoLabSessionManager.Instance;

        if (sessionManager == null)
        {
            sessionManager = FindFirstObjectByType<AlgoLabSessionManager>(
                FindObjectsInactive.Include
            );
        }
    }

    private void ConectarEventosSesion()
    {
        if (!reubicarFrenteUnaVezAlEntrar || eventosSesionConectados)
        {
            return;
        }

        BuscarSessionManager();

        if (sessionManager == null)
        {
            return;
        }

        sessionManager.OnSesionIniciada += ReubicarFrenteUnaVezDespuesDeEntrar;
        sessionManager.OnSesionInvitado += ReubicarFrenteUnaVezDespuesDeEntrar;

        eventosSesionConectados = true;
    }

    private void DesconectarEventosSesion()
    {
        if (!eventosSesionConectados || sessionManager == null)
        {
            return;
        }

        sessionManager.OnSesionIniciada -= ReubicarFrenteUnaVezDespuesDeEntrar;
        sessionManager.OnSesionInvitado -= ReubicarFrenteUnaVezDespuesDeEntrar;

        eventosSesionConectados = false;
    }

    [ContextMenu("Reubicar frente una vez al entrar")]
    public void ReubicarFrenteUnaVezDespuesDeEntrar()
    {
        if (rutinaReubicarDespuesDeEntrar != null)
        {
            StopCoroutine(rutinaReubicarDespuesDeEntrar);
        }

        rutinaReubicarDespuesDeEntrar = StartCoroutine(
            ReubicarFrenteUnaVezDespuesDeEntrarRutina()
        );
    }

    private IEnumerator ReubicarFrenteUnaVezDespuesDeEntrarRutina()
    {
        if (retrasoReubicarDespuesDeEntrar > 0f)
        {
            yield return new WaitForSeconds(retrasoReubicarDespuesDeEntrar);
        }

        ActualizarPosturaDetectada(true);
        offsetAlturaGlobal = ObtenerAlturaObjetivoSegunPostura();
        alturaObjetivoActual = offsetAlturaGlobal;

        if (resetearPanelesManipuladosAlEntrar)
        {
            ResetearEstadoPanelesManipulados();
        }

        // Esto s  toma la direcci n actual de la cabeza, pero solo se llama al entrar.
        ReubicarDesdeCabezaActual();

        if (usarObjetoFrontal && spawnearObjetoFrontalAlEntrar)
        {
            CambiarObjetoFrontalPorIndice(indiceObjetoFrontalInicial);
        }

        rutinaReubicarDespuesDeEntrar = null;
    }

    private void ActualizarAlturaSegunPosturaEnTiempoReal()
    {
        if (!ajustarAlturaEnTiempoReal)
        {
            return;
        }

        BuscarCabeza();

        if (headReference == null || referenciaManual == null)
        {
            return;
        }

        bool cambioPostura = ActualizarPosturaDetectada(false);
        float nuevaAlturaObjetivo = ObtenerAlturaObjetivoSegunPostura();

        if (cambioPostura)
        {
            velocidadSmoothAltura = 0f;
        }

        offsetAlturaGlobal = nuevaAlturaObjetivo;
        alturaObjetivoActual = nuevaAlturaObjetivo;

        float alturaActual = referenciaManual.position.y;

        if (Mathf.Abs(alturaActual - alturaObjetivoActual) <= umbralActualizarAltura)
        {
            if (cambioPostura)
            {
                AplicarAlturaReferenciaYReubicar(alturaObjetivoActual);
            }

            return;
        }

        float nuevaY;

        if (usarSmoothCambioAltura)
        {
            nuevaY = Mathf.SmoothDamp(
                alturaActual,
                alturaObjetivoActual,
                ref velocidadSmoothAltura,
                Mathf.Max(0.01f, tiempoSmoothCambioAltura),
                Mathf.Max(0.01f, velocidadMaximaCambioAltura),
                Mathf.Max(0.0001f, Time.unscaledDeltaTime)
            );
        }
        else
        {
            nuevaY = alturaObjetivoActual;
        }

        AplicarAlturaReferenciaYReubicar(nuevaY);
    }

    /// <summary>
    /// Fuerza una lectura de la altura de los ojos incluso cuando el menú de
    /// opciones tiene el nivel pausado. Se usa al editar las alturas para que
    /// levantarse durante la calibración cambie de inmediato al objetivo de pie
    /// y no conserve una velocidad/objetivo viejo de sentado.
    /// </summary>
    public void ActualizarPosturaYAlturaAhora()
    {
        if (!ajustarAlturaEnTiempoReal || referenciaManual == null)
        {
            return;
        }

        BuscarCabeza();
        if (headReference == null)
        {
            return;
        }

        bool cambioPostura = ActualizarPosturaDetectada(false);
        float objetivo = ObtenerAlturaObjetivoSegunPostura();
        offsetAlturaGlobal = objetivo;
        alturaObjetivoActual = objetivo;

        if (cambioPostura)
        {
            // SmoothDamp conserva la velocidad anterior. Al cambiar de sentado
            // a de pie esa velocidad puede seguir apuntando hacia abajo y dejar
            // los paneles temporalmente en la configuración equivocada.
            velocidadSmoothAltura = 0f;
        }

        float actual = referenciaManual.position.y;
        if (Mathf.Abs(actual - objetivo) <= umbralActualizarAltura)
        {
            if (cambioPostura)
            {
                AplicarAlturaReferenciaYReubicar(objetivo);
            }

            return;
        }

        float nuevaY = usarSmoothCambioAltura && !cambioPostura
            ? Mathf.SmoothDamp(
                actual,
                objetivo,
                ref velocidadSmoothAltura,
                Mathf.Max(0.01f, tiempoSmoothCambioAltura),
                Mathf.Max(0.01f, velocidadMaximaCambioAltura),
                Mathf.Max(0.0001f, Time.unscaledDeltaTime)
            )
            : objetivo;

        AplicarAlturaReferenciaYReubicar(nuevaY);
    }

    private bool ActualizarPosturaDetectada(bool forzarPrimeraDeteccion)
    {
        BuscarCabeza();

        if (headReference == null)
        {
            return false;
        }

        float alturaOjos = ObtenerAlturaOjosParaPostura();
        bool estadoAnterior = usuarioDetectadoParado;

        if (!posturaInicializada || forzarPrimeraDeteccion)
        {
            if (alturaOjos >= alturaMinimaParado)
            {
                usuarioDetectadoParado = true;
            }
            else if (alturaOjos <= alturaMaximaSentado)
            {
                usuarioDetectadoParado = false;
            }
            else
            {
                usuarioDetectadoParado = !asumirSentadoAntesDePrimeraDeteccion;
            }

            posturaInicializada = true;
        }
        else
        {
            // Histeresis: para pasar a parado exige una altura m s alta.
            // Para volver a sentado exige una altura m s baja.
            // As  no se confunde sentado con parado por peque as variaciones.
            if (!usuarioDetectadoParado && alturaOjos >= alturaMinimaParado)
            {
                usuarioDetectadoParado = true;
            }
            else if (usuarioDetectadoParado && alturaOjos <= alturaMaximaSentado)
            {
                usuarioDetectadoParado = false;
            }
        }

        if (mostrarDebug && estadoAnterior != usuarioDetectadoParado)
        {
            Debug.Log(
                "POSTURA: " +
                (usuarioDetectadoParado ? "parado" : "sentado") +
                " | altura ojos usada: " + alturaOjos.ToString("0.00") +
                " | localY: " + headReference.localPosition.y.ToString("0.00") +
                " | worldY: " + headReference.position.y.ToString("0.00")
            );
        }

        return estadoAnterior != usuarioDetectadoParado;
    }

    private float ObtenerAlturaOjosParaPostura()
    {
        BuscarCabeza();

        if (headReference == null)
        {
            return 0f;
        }

        float localY = headReference.localPosition.y;

        if (usarAlturaLocalCabezaParaPostura && localY > 0.25f)
        {
            return localY;
        }

        return headReference.position.y;
    }

    private float ObtenerAlturaObjetivoSegunPostura()
    {
        if (!ajustarAlturaEnTiempoReal)
        {
            return offsetAlturaGlobal;
        }

        return usuarioDetectadoParado
            ? alturaGlobalParado
            : alturaGlobalSentado;
    }

    private void AplicarAlturaReferenciaYReubicar(float nuevaY)
    {
        if (referenciaManual == null)
        {
            referenciaManual = transform;
        }

        Vector3 posicion = referenciaManual.position;
        posicion.y = nuevaY;
        referenciaManual.position = posicion;

        UbicarPanelesInterno(false);
        ReubicarObjetoFrontal(false);
        ActualizarObjetosAlturaDinamicaRegistrados();
    }

    [ContextMenu("Actualizar referencia desde cabeza")]
    public void ActualizarReferenciaDesdeCabeza()
    {
        BuscarCabeza();

        if (headReference == null)
        {
            Debug.LogWarning("No hay Head Reference asignado. Asigna CenterEyeAnchor.");
            return;
        }

        if (referenciaManual == null)
        {
            referenciaManual = transform;
        }

        ActualizarPosturaDetectada(false);
        float alturaObjetivo = ObtenerAlturaObjetivoSegunPostura();
        offsetAlturaGlobal = alturaObjetivo;
        alturaObjetivoActual = alturaObjetivo;

        Vector3 posicionReferencia = headReference.position;

        if (usarAlturaGlobalComoAlturaFinal)
        {
            posicionReferencia.y = alturaObjetivo;
        }
        else
        {
            posicionReferencia.y += alturaObjetivo;
        }

        Quaternion rotacionReferencia;

        if (usarSoloRotacionY)
        {
            Vector3 forward = headReference.forward;
            forward.y = 0f;

            if (forward.sqrMagnitude < 0.001f)
            {
                forward = Vector3.forward;
            }

            forward.Normalize();
            rotacionReferencia = Quaternion.LookRotation(forward, Vector3.up);
        }
        else
        {
            rotacionReferencia = headReference.rotation;
        }

        referenciaManual.position = posicionReferencia;
        referenciaManual.rotation = rotacionReferencia;

        if (mostrarDebug)
        {
            Debug.Log(
                "Referencia manual ubicada una vez frente a la cabeza: " +
                referenciaManual.name +
                " | altura objetivo: " + alturaObjetivo.ToString("0.00") +
                " | postura: " + (usuarioDetectadoParado ? "parado" : "sentado")
            );
        }
    }

    [ContextMenu("Ubicar paneles")]
    public void UbicarPaneles()
    {
        UbicarPanelesInterno(true);
    }

    private void UbicarPanelesInterno(bool detectarMovimientoManualAntesDelLayout)
    {
        if (referenciaManual == null)
        {
            referenciaManual = transform;
        }

        if (paneles == null || paneles.Count == 0)
        {
            Debug.LogWarning("No hay paneles asignados.");
            return;
        }

        for (int i = 0; i < paneles.Count; i++)
        {
            PanelManualInfo panel = paneles[i];

            if (panel == null || !panel.activo || panel.panelRoot == null)
            {
                continue;
            }

            // Si este panel es manipulable, primero revisa si el usuario lo movi .
            // Esto es importante porque algunos GrabHandle no disparan alSoltarPanel
            // cuando Notificar Tutorial Interactivo est  apagado.
            if (panel.ajustarAlturaDespuesDeMover && panel.mantenerDondeUsuarioLoDejo)
            {
                // Cuando el propio manager acaba de cambiar la altura de la referencia,
                // el panel todavia conserva durante este instante su posicion anterior.
                // Compararlo contra el nuevo layout lo marcaba falsamente como movido
                // por el usuario y bloqueaba la calibracion automatica.
                if (detectarMovimientoManualAntesDelLayout)
                {
                    DetectarMovimientoManualDePanel(panel);
                }

                // Si el usuario est  agarrando este panel, NO lo muevas desde aqu .
                // As  no pelea contra AlgoLabPanelGrabHandle.
                if (panel.estaAgarradoPorUsuario && bloquearLayoutMientrasPanelAgarrado)
                {
                    continue;
                }

                // Si el usuario ya lo movi , no lo devuelvas al layout inicial.
                // Mantiene X/Z donde lo dej  y solo se ajusta Y cuando cambia sentado/parado.
                if (panel.fueMovidoPorUsuario && noRegresarPanelManipuladoAlLayout)
                {
                    AjustarAlturaPanelManualManipulado(panel);
                    continue;
                }
            }

            Vector3 posicionMundo = referenciaManual.TransformPoint(panel.posicionLocal);
            panel.panelRoot.position = posicionMundo;
            panel.layoutInicialAplicado = true;
            panel.ultimaPosicionAplicadaPorManager = panel.panelRoot.position;

            AplicarRotacionPanelManual(panel);

            if (mostrarDebug)
            {
                Debug.Log(
                    "Panel ubicado: " + panel.panelRoot.name +
                    " | Posici n mundo: " + panel.panelRoot.position
                );
            }
        }
    }

    private void AplicarRotacionPanelManual(PanelManualInfo panel)
    {
        if (panel == null || panel.panelRoot == null)
        {
            return;
        }

        if (hacerQueMirenALaCabeza && !panel.usarRotacionManual)
        {
            RotarPanelHaciaCabeza(panel.panelRoot, panel.invertirFrente);
        }
        else
        {
            Quaternion rotacionMundo =
                referenciaManual.rotation * Quaternion.Euler(panel.rotacionLocalEuler);

            if (panel.invertirFrente)
            {
                rotacionMundo *= Quaternion.Euler(0f, 180f, 0f);
            }

            panel.panelRoot.rotation = rotacionMundo;
        }
    }

    private void AjustarAlturaPanelManualManipulado(PanelManualInfo panel)
    {
        if (panel == null || panel.panelRoot == null)
        {
            return;
        }

        float alturaBase = ObtenerAlturaBaseDinamicaActual();
        float deltaAltura = alturaBase - panel.alturaReferenciaAlSoltar;

        Vector3 posicion = panel.panelRoot.position;
        posicion.x = panel.posicionMundoAlSoltar.x;
        posicion.z = panel.posicionMundoAlSoltar.z;
        posicion.y = panel.posicionMundoAlSoltar.y + deltaAltura;
        panel.panelRoot.position = posicion;
        panel.ultimaPosicionAplicadaPorManager = panel.panelRoot.position;
    }

    private void DetectarPanelesMovidosPorUsuarioSinEventoGrab()
    {
        if (!detectarPanelMovidoSinEventoGrab || paneles == null)
        {
            return;
        }

        for (int i = 0; i < paneles.Count; i++)
        {
            PanelManualInfo panel = paneles[i];

            if (panel == null || panel.panelRoot == null || !panel.activo)
            {
                continue;
            }

            if (!panel.ajustarAlturaDespuesDeMover || !panel.mantenerDondeUsuarioLoDejo)
            {
                continue;
            }

            DetectarMovimientoManualDePanel(panel);
        }
    }

    private void DetectarMovimientoManualDePanel(PanelManualInfo panel)
    {
        if (!detectarPanelMovidoSinEventoGrab || panel == null || panel.panelRoot == null)
        {
            return;
        }

        if (!panel.ajustarAlturaDespuesDeMover || !panel.mantenerDondeUsuarioLoDejo)
        {
            return;
        }

        if (!guardarPosicionActualCuandoDetectaMovimiento)
        {
            return;
        }

        float umbral = Mathf.Max(0.001f, distanciaMinimaPanelMovidoPorUsuario);

        // Caso 1: el panel todav a no se hab a marcado como movido.
        // Compara contra la posici n del layout del spawner.
        if (!panel.fueMovidoPorUsuario)
        {
            if (!panel.layoutInicialAplicado)
            {
                return;
            }

            Vector3 posicionEsperadaLayout = referenciaManual.TransformPoint(panel.posicionLocal);
            float distanciaAlLayout = Vector3.Distance(panel.panelRoot.position, posicionEsperadaLayout);

            if (distanciaAlLayout >= umbral)
            {
                GuardarEstadoPanelManipulado(panel);
            }

            return;
        }

        // Caso 2: el panel ya fue movido. Si el usuario lo vuelve a mover,
        // guarda la nueva posici n como la nueva base. Comparamos contra la  ltima
        // posici n que puso el manager para no confundir el ajuste autom tico de altura
        // con un movimiento del usuario.
        float distanciaDesdeUltimaAplicada = Vector3.Distance(
            panel.panelRoot.position,
            panel.ultimaPosicionAplicadaPorManager
        );

        if (distanciaDesdeUltimaAplicada >= umbral)
        {
            GuardarEstadoPanelManipulado(panel);
        }
    }

    private void GuardarEstadoPanelManipulado(PanelManualInfo panel)
    {
        if (panel == null || panel.panelRoot == null)
        {
            return;
        }

        if (referenciaManual == null)
        {
            referenciaManual = transform;
        }

        panel.estaAgarradoPorUsuario = false;
        panel.fueMovidoPorUsuario = true;
        panel.layoutInicialAplicado = true;
        panel.posicionMundoAlSoltar = panel.panelRoot.position;
        panel.alturaReferenciaAlSoltar = ObtenerAlturaBaseDinamicaActual();
        panel.ultimaPosicionAplicadaPorManager = panel.panelRoot.position;

        if (rotarPanelManipuladoAlSoltarHaciaCabeza &&
            hacerQueMirenALaCabeza &&
            !panel.usarRotacionManual)
        {
            RotarPanelHaciaCabeza(panel.panelRoot, panel.invertirFrente);
        }
    }

    [ContextMenu("Resetear paneles manipulados")]
    public void ResetearEstadoPanelesManipulados()
    {
        if (paneles == null)
        {
            return;
        }

        for (int i = 0; i < paneles.Count; i++)
        {
            PanelManualInfo panel = paneles[i];

            if (panel == null)
            {
                continue;
            }

            panel.estaAgarradoPorUsuario = false;
            panel.fueMovidoPorUsuario = false;
            panel.posicionMundoAlSoltar = Vector3.zero;
            panel.alturaReferenciaAlSoltar = ObtenerAlturaBaseDinamicaActual();
            panel.layoutInicialAplicado = false;
            panel.ultimaPosicionAplicadaPorManager = Vector3.zero;
        }
    }

    [ContextMenu("Reubicar todo desde cabeza actual")]
    public void ReubicarDesdeCabezaActual()
    {
        ActualizarReferenciaDesdeCabeza();
        UbicarPanelesInterno(false);
        ReubicarObjetoFrontal(true);
    }

    public void ReubicarPaneles()
    {
        UbicarPaneles();
    }

    public void ReubicarPanelesDesdeCabeza()
    {
        ReubicarDesdeCabezaActual();
    }

    public void AplicarConfiguracionAltura(
        int modoPostura,
        float alturaSentado,
        float alturaParado,
        bool suavizar)
    {
        alturaGlobalSentado = Mathf.Clamp(alturaSentado, 0.75f, 1.8f);
        alturaGlobalParado = Mathf.Clamp(alturaParado, 1f, 2.4f);
        usarSmoothCambioAltura = suavizar;

        if (modoPostura == 0)
        {
            ajustarAlturaEnTiempoReal = true;
            posturaInicializada = false;
            ActualizarPosturaDetectada(true);
            offsetAlturaGlobal = ObtenerAlturaObjetivoSegunPostura();
            velocidadSmoothAltura = 0f;
        }
        else
        {
            ajustarAlturaEnTiempoReal = false;
            usuarioDetectadoParado = modoPostura == 2;
            posturaInicializada = true;
            offsetAlturaGlobal = usuarioDetectadoParado
                ? alturaGlobalParado
                : alturaGlobalSentado;
        }

        alturaObjetivoActual = offsetAlturaGlobal;
        AplicarAlturaReferenciaYReubicar(offsetAlturaGlobal);
    }

    public void RecolocarPanelesPredeterminados()
    {
        ResetearEstadoPanelesManipulados();
        ReubicarDesdeCabezaActual();
    }

    [ContextMenu("Reubicar solo objeto frontal")]
    public void ReubicarSoloObjetoFrontalDesdeCabezaActual()
    {
        ReubicarObjetoFrontal();
    }

    [ContextMenu("Spawnear objeto frontal inicial")]
    public void SpawnearObjetoFrontalInicial()
    {
        CambiarObjetoFrontalPorIndice(indiceObjetoFrontalInicial);
    }

    public void CambiarObjetoFrontalPorIndice(int indice)
    {
        if (!usarObjetoFrontal)
        {
            Debug.LogWarning("Objeto frontal est  desactivado en el manager.");
            return;
        }

        if (objetosFrontales == null || objetosFrontales.Count == 0)
        {
            Debug.LogWarning("No hay objetos frontales configurados.");
            return;
        }

        if (indice < 0 || indice >= objetosFrontales.Count)
        {
            Debug.LogWarning(" ndice de objeto frontal fuera de rango: " + indice);
            return;
        }

        CambiarObjetoFrontal(objetosFrontales[indice], indice);
    }

    public void CambiarObjetoFrontalPorNombre(string nombreObjeto)
    {
        if (!usarObjetoFrontal)
        {
            Debug.LogWarning("Objeto frontal est  desactivado en el manager.");
            return;
        }

        if (string.IsNullOrWhiteSpace(nombreObjeto))
        {
            Debug.LogWarning("Nombre de objeto frontal vac o.");
            return;
        }

        if (objetosFrontales == null || objetosFrontales.Count == 0)
        {
            Debug.LogWarning("No hay objetos frontales configurados.");
            return;
        }

        for (int i = 0; i < objetosFrontales.Count; i++)
        {
            if (objetosFrontales[i] != null &&
                objetosFrontales[i].nombreObjeto == nombreObjeto)
            {
                CambiarObjetoFrontal(objetosFrontales[i], i);
                return;
            }
        }

        Debug.LogWarning("No se encontr  objeto frontal con nombre: " + nombreObjeto);
    }

    public void CambiarObjetoFrontalDesdePrefab(GameObject prefab)
    {
        if (prefab == null)
        {
            Debug.LogWarning("Prefab vac o.");
            return;
        }

        ObjetoFrontalInfo infoTemporal = new ObjetoFrontalInfo();
        infoTemporal.nombreObjeto = prefab.name;
        infoTemporal.prefab = prefab;
        infoTemporal.usarEscalaOriginalDelPrefab = true;
        infoTemporal.escalaManual = Vector3.one;
        infoTemporal.mirarHaciaLaCabeza = false;
        infoTemporal.soloRotacionY = true;
        infoTemporal.invertirFrente = false;
        infoTemporal.rotacionLocalEuler = Vector3.zero;

        CambiarObjetoFrontal(infoTemporal, -1);
    }

    public void CambiarObjetoFrontalDesdePrefabConEscala(GameObject prefab, Vector3 escalaManual)
    {
        if (prefab == null)
        {
            Debug.LogWarning("Prefab vac o.");
            return;
        }

        ObjetoFrontalInfo infoTemporal = new ObjetoFrontalInfo();
        infoTemporal.nombreObjeto = prefab.name;
        infoTemporal.prefab = prefab;
        infoTemporal.usarEscalaOriginalDelPrefab = false;
        infoTemporal.escalaManual = escalaManual;
        infoTemporal.mirarHaciaLaCabeza = false;
        infoTemporal.soloRotacionY = true;
        infoTemporal.invertirFrente = false;
        infoTemporal.rotacionLocalEuler = Vector3.zero;

        CambiarObjetoFrontal(infoTemporal, -1);
    }

    public void AgregarObjetoFrontal(string nombreObjeto, GameObject prefab)
    {
        if (prefab == null)
        {
            Debug.LogWarning("No se puede agregar un objeto frontal sin prefab.");
            return;
        }

        if (objetosFrontales == null)
        {
            objetosFrontales = new List<ObjetoFrontalInfo>();
        }

        ObjetoFrontalInfo nuevo = new ObjetoFrontalInfo();
        nuevo.nombreObjeto = string.IsNullOrWhiteSpace(nombreObjeto) ? prefab.name : nombreObjeto;
        nuevo.prefab = prefab;
        nuevo.usarEscalaOriginalDelPrefab = true;
        nuevo.escalaManual = Vector3.one;
        nuevo.mirarHaciaLaCabeza = false;
        nuevo.soloRotacionY = true;
        nuevo.invertirFrente = false;
        nuevo.rotacionLocalEuler = Vector3.zero;

        objetosFrontales.Add(nuevo);

        if (mostrarDebug)
        {
            Debug.Log("Objeto frontal agregado con escala original: " + nuevo.nombreObjeto);
        }
    }

    public void AgregarObjetoFrontalConEscala(string nombreObjeto, GameObject prefab, Vector3 escalaManual)
    {
        if (prefab == null)
        {
            Debug.LogWarning("No se puede agregar un objeto frontal sin prefab.");
            return;
        }

        if (objetosFrontales == null)
        {
            objetosFrontales = new List<ObjetoFrontalInfo>();
        }

        ObjetoFrontalInfo nuevo = new ObjetoFrontalInfo();
        nuevo.nombreObjeto = string.IsNullOrWhiteSpace(nombreObjeto) ? prefab.name : nombreObjeto;
        nuevo.prefab = prefab;
        nuevo.usarEscalaOriginalDelPrefab = false;
        nuevo.escalaManual = escalaManual;
        nuevo.mirarHaciaLaCabeza = false;
        nuevo.soloRotacionY = true;
        nuevo.invertirFrente = false;
        nuevo.rotacionLocalEuler = Vector3.zero;

        objetosFrontales.Add(nuevo);

        if (mostrarDebug)
        {
            Debug.Log("Objeto frontal agregado con escala manual: " + nuevo.nombreObjeto);
        }
    }

    public void QuitarObjetoFrontalActual()
    {
        if (rutinaObjetoFrontal != null)
        {
            StopCoroutine(rutinaObjetoFrontal);
        }

        rutinaObjetoFrontal = StartCoroutine(DesaparecerYQuitarObjetoFrontalActual());
    }

    public void ReubicarObjetoFrontal()
    {
        ReubicarObjetoFrontal(true);
    }

    private void ReubicarObjetoFrontal(bool forzarRecolocacion)
    {
        if (objetoFrontalActual == null)
        {
            return;
        }

        // El taller del nivel 3 contiene robot, monitor y herramientas dentro
        // de un único root. No debe subir/bajar cada vez que cambia la postura,
        // pero sí debe recolocarse como conjunto cuando el usuario pulsa
        // "Recolocar paneles".
        if (!forzarRecolocacion &&
            EsObjetoFijoAnteCambioDePostura(objetoFrontalActual.transform))
        {
            return;
        }

        objetoFrontalActual.transform.position = ObtenerPosicionMundoObjetoFrontal();

        if (infoObjetoFrontalActual != null)
        {
            AplicarRotacionObjetoFrontal(
                objetoFrontalActual.transform,
                infoObjetoFrontalActual
            );
        }
    }


    private void ActualizarEstadoGrabHandlesPorPolling()
    {
        if (!integrarGrabDePanelesEnManager || !detectarGrabPorPollingInterno)
        {
            return;
        }

        if (grabHandlesConectados == null || grabHandlesConectados.Count == 0)
        {
            return;
        }

        for (int i = grabHandlesConectados.Count - 1; i >= 0; i--)
        {
            AlgoLabPanelGrabHandle handle = grabHandlesConectados[i];

            if (handle == null)
            {
                grabHandlesConectados.RemoveAt(i);
                continue;
            }

            Transform root = ObtenerRootDesdeGrabHandle(handle);

            if (root == null)
            {
                continue;
            }

            bool agarrandoAhora = EstaGrabHandleAgarrando(handle);
            PanelManualInfo panel = BuscarPanelManualPorTransform(root);

            if (panel != null)
            {
                PrepararPanelManipulableAutomatico(panel);

                if (!panel.ajustarAlturaDespuesDeMover || !panel.mantenerDondeUsuarioLoDejo)
                {
                    continue;
                }

                if (agarrandoAhora)
                {
                    panel.estaAgarradoPorUsuario = true;
                    continue;
                }

                // Si antes estaba agarrado y ahora no, significa que el usuario lo soltó.
                // Guardamos la posición real donde lo dejó para que no vuelva al spawner.
                if (panel.estaAgarradoPorUsuario)
                {
                    panel.estaAgarradoPorUsuario = false;

                    if (panelesManipulablesMantienenXZAlSoltar)
                    {
                        GuardarEstadoPanelManipulado(panel);
                    }
                }

                continue;
            }

            ObjetoAlturaDinamicaInfo info = BuscarObjetoAlturaDinamica(root);

            if (info == null && registrarGrabHandlesExternosComoAlturaDinamica)
            {
                RegistrarObjetoParaAlturaDinamica(root, objetosRegistradosAjustarSoloY);
                info = BuscarObjetoAlturaDinamica(root);
            }

            if (info == null)
            {
                continue;
            }

            if (agarrandoAhora)
            {
                if (pausarAlturaMientrasEstaAgarrado)
                {
                    info.pausadoPorGrab = true;
                }

                continue;
            }

            if (info.pausadoPorGrab)
            {
                info.pausadoPorGrab = false;

                if (recalcularAlturaAlSoltar)
                {
                    RecalcularObjetoAlturaDinamicaDesdePosicionActual(info);
                }
            }
        }
    }

    private bool EstaGrabHandleAgarrando(AlgoLabPanelGrabHandle handle)
    {
        return handle != null && handle.EstaAgarrando;
    }

    private void PrepararPanelManipulableAutomatico(PanelManualInfo panel)
    {
        if (panel == null || !activarModoManipulableAutomaticoParaPanelesConGrab)
        {
            return;
        }

        panel.ajustarAlturaDespuesDeMover = true;
        panel.mantenerDondeUsuarioLoDejo = true;
    }

    [ContextMenu("Conectar grab handles de paneles")]
    public void ConectarGrabHandlesPanelesYObjetos()
    {
        if (!integrarGrabDePanelesEnManager || !buscarGrabHandlesAutomaticamente)
        {
            return;
        }

        AlgoLabPanelGrabHandle[] handles = FindObjectsByType<AlgoLabPanelGrabHandle>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < handles.Length; i++)
        {
            ConectarGrabHandle(handles[i]);
        }
    }

    private void ConectarGrabHandle(AlgoLabPanelGrabHandle handle)
    {
        if (handle == null)
        {
            return;
        }

        if (grabHandlesConectados.Contains(handle))
        {
            return;
        }

        Transform root = ObtenerRootDesdeGrabHandle(handle);

        if (root == null || root == transform || root == referenciaManual)
        {
            return;
        }

        PanelManualInfo panelManualDelHandle = BuscarPanelManualPorTransform(root);

        if (panelManualDelHandle != null)
        {
            PrepararPanelManipulableAutomatico(panelManualDelHandle);
        }

        UnityAction accionInicio = () => NotificarGrabAlturaIniciado(handle);
        UnityAction accionFin = () => NotificarGrabAlturaSoltado(handle);

        handle.alIniciarAgarre.AddListener(accionInicio);
        handle.alSoltarPanel.AddListener(accionFin);
        accionesInicioGrab[handle] = accionInicio;
        accionesFinGrab[handle] = accionFin;

        grabHandlesConectados.Add(handle);

        if (registrarGrabHandlesExternosComoAlturaDinamica && !EsPanelControladoPorManager(root))
        {
            RegistrarObjetoParaAlturaDinamica(root, objetosRegistradosAjustarSoloY);
            ObjetoAlturaDinamicaInfo info = BuscarObjetoAlturaDinamica(root);

            if (info != null)
            {
                info.registradoDesdeGrabHandle = true;
            }
        }

        if (mostrarDebug)
        {
            Debug.Log("GRAB ALTURA: conectado " + handle.name + " -> " + root.name);
        }
    }

    private void DesconectarGrabHandlesPanelesYObjetos()
    {
        foreach (KeyValuePair<AlgoLabPanelGrabHandle, UnityAction> par in accionesInicioGrab)
        {
            if (par.Key != null)
            {
                par.Key.alIniciarAgarre.RemoveListener(par.Value);
            }
        }

        foreach (KeyValuePair<AlgoLabPanelGrabHandle, UnityAction> par in accionesFinGrab)
        {
            if (par.Key != null)
            {
                par.Key.alSoltarPanel.RemoveListener(par.Value);
            }
        }

        accionesInicioGrab.Clear();
        accionesFinGrab.Clear();
        grabHandlesConectados.Clear();
    }

    private Transform ObtenerRootDesdeGrabHandle(AlgoLabPanelGrabHandle handle)
    {
        if (handle == null)
        {
            return null;
        }

        if (handle.panelRoot != null)
        {
            return handle.panelRoot;
        }

        return handle.transform.root;
    }

    private void NotificarGrabAlturaIniciado(AlgoLabPanelGrabHandle handle)
    {
        Transform root = ObtenerRootDesdeGrabHandle(handle);

        if (root == null)
        {
            return;
        }

        PanelManualInfo panel = BuscarPanelManualPorTransform(root);

        if (panel != null)
        {
            PrepararPanelManipulableAutomatico(panel);

            if (panel.ajustarAlturaDespuesDeMover)
            {
                panel.estaAgarradoPorUsuario = true;
                return;
            }
        }

        ObjetoAlturaDinamicaInfo info = BuscarObjetoAlturaDinamica(root);

        if (info == null && registrarGrabHandlesExternosComoAlturaDinamica)
        {
            RegistrarObjetoParaAlturaDinamica(root, objetosRegistradosAjustarSoloY);
            info = BuscarObjetoAlturaDinamica(root);
        }

        if (info != null && pausarAlturaMientrasEstaAgarrado)
        {
            info.pausadoPorGrab = true;
        }
    }

    private void NotificarGrabAlturaSoltado(AlgoLabPanelGrabHandle handle)
    {
        Transform root = ObtenerRootDesdeGrabHandle(handle);

        if (root == null)
        {
            return;
        }

        PanelManualInfo panel = BuscarPanelManualPorTransform(root);

        if (panel != null)
        {
            PrepararPanelManipulableAutomatico(panel);

            if (panel.ajustarAlturaDespuesDeMover)
            {
                panel.estaAgarradoPorUsuario = false;

                if (panelesManipulablesMantienenXZAlSoltar)
                {
                    GuardarEstadoPanelManipulado(panel);
                }

                return;
            }
        }

        ObjetoAlturaDinamicaInfo info = BuscarObjetoAlturaDinamica(root);

        if (info == null && registrarGrabHandlesExternosComoAlturaDinamica)
        {
            RegistrarObjetoParaAlturaDinamica(root, objetosRegistradosAjustarSoloY);
            info = BuscarObjetoAlturaDinamica(root);
        }

        if (info != null)
        {
            info.pausadoPorGrab = false;

            if (recalcularAlturaAlSoltar)
            {
                RecalcularObjetoAlturaDinamicaDesdePosicionActual(info);
            }
        }
    }

    private PanelManualInfo BuscarPanelManualPorTransform(Transform objetivo)
    {
        if (objetivo == null || paneles == null)
        {
            return null;
        }

        for (int i = 0; i < paneles.Count; i++)
        {
            PanelManualInfo panel = paneles[i];

            if (panel == null || panel.panelRoot == null)
            {
                continue;
            }

            if (objetivo == panel.panelRoot || objetivo.IsChildOf(panel.panelRoot))
            {
                return panel;
            }
        }

        return null;
    }

    private ObjetoAlturaDinamicaInfo BuscarObjetoAlturaDinamica(Transform objetivo)
    {
        if (objetivo == null || objetosAlturaDinamicaRegistrados == null)
        {
            return null;
        }

        for (int i = 0; i < objetosAlturaDinamicaRegistrados.Count; i++)
        {
            ObjetoAlturaDinamicaInfo info = objetosAlturaDinamicaRegistrados[i];

            if (info != null && info.objetivo == objetivo)
            {
                return info;
            }
        }

        return null;
    }

    private void RecalcularObjetoAlturaDinamicaDesdePosicionActual(ObjetoAlturaDinamicaInfo info)
    {
        if (info == null || info.objetivo == null)
        {
            return;
        }

        if (referenciaManual == null)
        {
            referenciaManual = transform;
        }

        float alturaBaseActual = ObtenerAlturaBaseDinamicaActual();

        info.activo = true;
        info.offsetYRespectoReferencia = info.objetivo.position.y - alturaBaseActual;
        info.posicionMundoAlRegistrar = info.objetivo.position;
        info.alturaReferenciaAlRegistrar = alturaBaseActual;
        info.posicionLocalRespectoReferencia = referenciaManual.InverseTransformPoint(info.objetivo.position);
    }

    public void RegistrarObjetoFrontalExternoParaAlturaDinamica(GameObject objeto)
    {
        if (objeto == null)
        {
            return;
        }

        RegistrarObjetoParaAlturaDinamica(objeto.transform, objetosRegistradosAjustarSoloY);
    }

    public void RegistrarObjetoFrontalExternoParaAlturaDinamica(Transform objetivo)
    {
        RegistrarObjetoParaAlturaDinamica(objetivo, objetosRegistradosAjustarSoloY);
    }

    public void RegistrarObjetoParaAlturaDinamica(Transform objetivo)
    {
        RegistrarObjetoParaAlturaDinamica(objetivo, objetosRegistradosAjustarSoloY);
    }

    public void RegistrarObjetoParaAlturaDinamica(Transform objetivo, bool ajustarSoloY)
    {
        if (objetivo == null)
        {
            return;
        }

        if (EsObjetoFijoAnteCambioDePostura(objetivo))
        {
            DesregistrarObjetoParaAlturaDinamica(objetivo);
            return;
        }

        if (objetivo.GetComponentInChildren<AlgoLabTutorialPanelController>(true) != null ||
            objetivo.GetComponentInParent<AlgoLabTutorialPanelController>(true) != null)
        {
            DesregistrarObjetoParaAlturaDinamica(objetivo);
            return;
        }

        if (objetivo == transform || objetivo == referenciaManual)
        {
            return;
        }

        if (noRegistrarPanelesControladosPorManager && EsPanelControladoPorManager(objetivo))
        {
            if (mostrarDebug)
            {
                Debug.Log("ALTURA DIN MICA: no se registra " + objetivo.name + " porque ya lo controla la lista Paneles.");
            }

            return;
        }

        if (referenciaManual == null)
        {
            referenciaManual = transform;
        }

        if (objetosAlturaDinamicaRegistrados == null)
        {
            objetosAlturaDinamicaRegistrados = new List<ObjetoAlturaDinamicaInfo>();
        }

        float alturaBaseActual = ObtenerAlturaBaseDinamicaActual();

        for (int i = 0; i < objetosAlturaDinamicaRegistrados.Count; i++)
        {
            ObjetoAlturaDinamicaInfo existente = objetosAlturaDinamicaRegistrados[i];

            if (existente != null && existente.objetivo == objetivo)
            {
                existente.activo = true;
                existente.ajustarSoloY = ajustarSoloY;

                // IMPORTANTE: no recalculamos todo cada frame. Solo actualizamos la base al registrarlo otra vez.
                existente.offsetYRespectoReferencia = objetivo.position.y - alturaBaseActual;
                existente.posicionMundoAlRegistrar = objetivo.position;
                existente.alturaReferenciaAlRegistrar = alturaBaseActual;
                existente.posicionLocalRespectoReferencia = referenciaManual.InverseTransformPoint(objetivo.position);

                if (corregirObjetoInmediatamenteAlRegistrar)
                {
                    ActualizarObjetoAlturaDinamica(existente);
                }

                return;
            }
        }

        ObjetoAlturaDinamicaInfo nuevo = new ObjetoAlturaDinamicaInfo
        {
            objetivo = objetivo,
            ajustarSoloY = ajustarSoloY,
            activo = true,
            offsetYRespectoReferencia = objetivo.position.y - alturaBaseActual,
            posicionLocalRespectoReferencia = referenciaManual.InverseTransformPoint(objetivo.position),
            posicionMundoAlRegistrar = objetivo.position,
            alturaReferenciaAlRegistrar = alturaBaseActual
        };

        objetosAlturaDinamicaRegistrados.Add(nuevo);

        if (corregirObjetoInmediatamenteAlRegistrar)
        {
            ActualizarObjetoAlturaDinamica(nuevo);
        }

        if (mostrarDebug)
        {
            Debug.Log("ALTURA DIN MICA: objeto registrado estable: " + objetivo.name);
        }
    }

    private bool EsPanelControladoPorManager(Transform objetivo)
    {
        if (objetivo == null || paneles == null)
        {
            return false;
        }

        for (int i = 0; i < paneles.Count; i++)
        {
            PanelManualInfo panel = paneles[i];

            if (panel == null || panel.panelRoot == null)
            {
                continue;
            }

            if (objetivo == panel.panelRoot || objetivo.IsChildOf(panel.panelRoot))
            {
                return true;
            }
        }

        return false;
    }

    public void DesregistrarObjetoParaAlturaDinamica(Transform objetivo)
    {
        if (objetivo == null || objetosAlturaDinamicaRegistrados == null)
        {
            return;
        }

        for (int i = objetosAlturaDinamicaRegistrados.Count - 1; i >= 0; i--)
        {
            if (objetosAlturaDinamicaRegistrados[i] == null ||
                objetosAlturaDinamicaRegistrados[i].objetivo == objetivo)
            {
                objetosAlturaDinamicaRegistrados.RemoveAt(i);
            }
        }
    }

    private void ActualizarObjetosAlturaDinamicaRegistrados()
    {
        if (!actualizarObjetosRegistradosEnTiempoReal ||
            objetosAlturaDinamicaRegistrados == null ||
            objetosAlturaDinamicaRegistrados.Count == 0)
        {
            return;
        }

        for (int i = objetosAlturaDinamicaRegistrados.Count - 1; i >= 0; i--)
        {
            ObjetoAlturaDinamicaInfo info = objetosAlturaDinamicaRegistrados[i];

            if (info == null || info.objetivo == null)
            {
                objetosAlturaDinamicaRegistrados.RemoveAt(i);
                continue;
            }

            if (EsObjetoFijoAnteCambioDePostura(info.objetivo))
            {
                objetosAlturaDinamicaRegistrados.RemoveAt(i);
                continue;
            }

            if (!info.activo || info.pausadoPorGrab)
            {
                continue;
            }

            ActualizarObjetoAlturaDinamica(info);
        }
    }

    private void ActualizarObjetoAlturaDinamica(ObjetoAlturaDinamicaInfo info)
    {
        if (info == null || info.objetivo == null)
        {
            return;
        }

        if (referenciaManual == null)
        {
            referenciaManual = transform;
        }

        if (noRegistrarPanelesControladosPorManager && EsPanelControladoPorManager(info.objetivo))
        {
            return;
        }

        float alturaBase = ObtenerAlturaBaseDinamicaActual();

        if (usarRegistroAlturaEstable)
        {
            float deltaAltura = alturaBase - info.alturaReferenciaAlRegistrar;

            if (info.ajustarSoloY)
            {
                Vector3 posicion = info.objetivo.position;
                posicion.y = info.posicionMundoAlRegistrar.y + deltaAltura;
                info.objetivo.position = posicion;
                return;
            }

            Vector3 posicionMundo = referenciaManual.TransformPoint(info.posicionLocalRespectoReferencia);
            posicionMundo.y = info.posicionMundoAlRegistrar.y + deltaAltura;
            info.objetivo.position = posicionMundo;
            return;
        }

        if (info.ajustarSoloY)
        {
            Vector3 posicion = info.objetivo.position;
            posicion.y = alturaBase + info.offsetYRespectoReferencia;
            info.objetivo.position = posicion;
            return;
        }

        Vector3 posicionMundoNoEstable = referenciaManual.TransformPoint(info.posicionLocalRespectoReferencia);
        posicionMundoNoEstable.y = alturaBase + info.offsetYRespectoReferencia;
        info.objetivo.position = posicionMundoNoEstable;
    }

    private float ObtenerAlturaBaseDinamicaActual()
    {
        if (referenciaManual != null)
        {
            return referenciaManual.position.y;
        }

        if (alturaObjetivoActual > 0.01f)
        {
            return alturaObjetivoActual;
        }

        return ObtenerAlturaObjetivoSegunPostura();
    }

    private void CambiarObjetoFrontal(ObjetoFrontalInfo nuevoObjetoInfo, int nuevoIndice)
    {
        if (nuevoObjetoInfo == null || nuevoObjetoInfo.prefab == null)
        {
            Debug.LogWarning("El objeto frontal no tiene prefab asignado.");
            return;
        }

        if (referenciaManual == null)
        {
            referenciaManual = transform;
        }

        if (rutinaObjetoFrontal != null)
        {
            StopCoroutine(rutinaObjetoFrontal);
        }

        rutinaObjetoFrontal = StartCoroutine(
            CambiarObjetoFrontalSmooth(nuevoObjetoInfo, nuevoIndice)
        );
    }

    private IEnumerator CambiarObjetoFrontalSmooth(
        ObjetoFrontalInfo nuevoObjetoInfo,
        int nuevoIndice)
    {
        if (objetoFrontalActual != null)
        {
            GameObject anterior = objetoFrontalActual;

            yield return DesaparecerObjetoFrontal(anterior);

            if (destruirObjetoAnterior)
            {
                Destroy(anterior);
            }
            else
            {
                anterior.SetActive(false);
            }

            if (objetoFrontalActual == anterior)
            {
                objetoFrontalActual = null;
                infoObjetoFrontalActual = null;
            }
        }

        GameObject nuevoObjeto = Instantiate(nuevoObjetoInfo.prefab);
        nuevoObjeto.name = nuevoObjetoInfo.nombreObjeto;

        objetoFrontalActual = nuevoObjeto;
        indiceObjetoFrontalActual = nuevoIndice;
        infoObjetoFrontalActual = nuevoObjetoInfo;

        Vector3 escalaFinalReal = ObtenerEscalaFinalObjeto(nuevoObjetoInfo, nuevoObjeto);

        nuevoObjeto.transform.position = ObtenerPosicionMundoObjetoFrontal();
        AplicarRotacionObjetoFrontal(nuevoObjeto.transform, nuevoObjetoInfo);

        nuevoObjeto.transform.localScale = escalaFinalReal * escalaInicialObjeto;
        nuevoObjeto.SetActive(true);

        if (registrarObjetosFrontalesAutomaticamente &&
            !EsObjetoFijoAnteCambioDePostura(nuevoObjeto.transform))
        {
            RegistrarObjetoParaAlturaDinamica(nuevoObjeto.transform, objetosRegistradosAjustarSoloY);
        }

        yield return AparecerObjetoFrontal(nuevoObjeto, escalaFinalReal);

        if (mostrarDebug)
        {
            Debug.Log(
                "Objeto frontal cambiado a: " + nuevoObjetoInfo.nombreObjeto +
                " | Posici n mundo: " + nuevoObjeto.transform.position +
                " | Escala final real: " + escalaFinalReal
            );
        }
    }

    private Vector3 ObtenerEscalaFinalObjeto(ObjetoFrontalInfo info, GameObject instancia)
    {
        if (info == null)
        {
            return Vector3.one;
        }

        if (info.usarEscalaOriginalDelPrefab)
        {
            if (instancia != null)
            {
                Vector3 escalaOriginal = instancia.transform.localScale;

                if (EscalaValida(escalaOriginal))
                {
                    return escalaOriginal;
                }
            }

            if (info.prefab != null)
            {
                Vector3 escalaPrefab = info.prefab.transform.localScale;

                if (EscalaValida(escalaPrefab))
                {
                    return escalaPrefab;
                }
            }

            return Vector3.one * 0.2f;
        }

        if (EscalaValida(info.escalaManual))
        {
            return info.escalaManual;
        }

        if (instancia != null && EscalaValida(instancia.transform.localScale))
        {
            return instancia.transform.localScale;
        }

        return Vector3.one * 0.2f;
    }

    private bool EscalaValida(Vector3 escala)
    {
        return Mathf.Abs(escala.x) > 0.0001f &&
               Mathf.Abs(escala.y) > 0.0001f &&
               Mathf.Abs(escala.z) > 0.0001f;
    }

    private static bool EsObjetoFijoAnteCambioDePostura(Transform objetivo)
    {
        if (objetivo == null)
            return false;
        return objetivo.GetComponent<AlgoLabLevel3RobotPracticeRuntime>() != null ||
               objetivo.GetComponentInParent<AlgoLabLevel3RobotPracticeRuntime>() != null;
    }

    private Vector3 ObtenerPosicionMundoObjetoFrontal()
    {
        if (objetoFrontalUsarReferenciaManual)
        {
            if (referenciaManual == null)
            {
                referenciaManual = transform;
            }

            return referenciaManual.TransformPoint(posicionLocalObjetoFrontal);
        }

        if (headReference == null && Camera.main != null)
        {
            headReference = Camera.main.transform;
        }

        if (headReference == null)
        {
            if (referenciaManual == null)
            {
                referenciaManual = transform;
            }

            return referenciaManual.TransformPoint(posicionLocalObjetoFrontal);
        }

        Vector3 forward = headReference.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.001f)
        {
            forward = Vector3.forward;
        }

        forward.Normalize();

        Quaternion rotacionY = Quaternion.LookRotation(forward, Vector3.up);

        Vector3 posicionBase = headReference.position;

        if (objetoFrontalAfectadoPorAlturaPostura)
        {
            posicionBase.y = ObtenerAlturaBaseObjetoFrontal();
        }

        Vector3 posicion =
            posicionBase +
            rotacionY * posicionLocalObjetoFrontal +
            Vector3.up * offsetAlturaObjetoFrontal;

        return posicion;
    }

    private float ObtenerAlturaBaseObjetoFrontal()
    {
        if (!objetoFrontalAfectadoPorAlturaPostura)
        {
            if (headReference != null)
            {
                return headReference.position.y;
            }

            return referenciaManual != null ? referenciaManual.position.y : transform.position.y;
        }

        if (objetoFrontalUsarAlturaSuavizadaReferencia && referenciaManual != null)
        {
            return referenciaManual.position.y;
        }

        if (alturaObjetivoActual > 0.01f)
        {
            return alturaObjetivoActual;
        }

        return ObtenerAlturaObjetivoSegunPostura();
    }

    private Quaternion ObtenerRotacionBaseObjetoFrontal()
    {
        if (objetoFrontalUsarReferenciaManual)
        {
            if (referenciaManual == null)
            {
                referenciaManual = transform;
            }

            return referenciaManual.rotation;
        }

        if (headReference == null && Camera.main != null)
        {
            headReference = Camera.main.transform;
        }

        if (headReference == null)
        {
            return transform.rotation;
        }

        Vector3 forward = headReference.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.001f)
        {
            forward = Vector3.forward;
        }

        forward.Normalize();

        return Quaternion.LookRotation(forward, Vector3.up);
    }

    private void AplicarRotacionObjetoFrontal(
        Transform objeto,
        ObjetoFrontalInfo info)
    {
        if (objeto == null || info == null)
        {
            return;
        }

        if (info.mirarHaciaLaCabeza)
        {
            RotarObjetoHaciaCabeza(
                objeto,
                info.soloRotacionY,
                info.invertirFrente
            );
        }
        else
        {
            Quaternion rotacionBase = ObtenerRotacionBaseObjetoFrontal();

            Quaternion rotacionMundo =
                rotacionBase * Quaternion.Euler(info.rotacionLocalEuler);

            if (info.invertirFrente)
            {
                rotacionMundo *= Quaternion.Euler(0f, 180f, 0f);
            }

            objeto.rotation = rotacionMundo;
        }
    }

    private IEnumerator AparecerObjetoFrontal(GameObject objeto, Vector3 escalaFinal)
    {
        if (objeto == null)
        {
            yield break;
        }

        float tiempo = 0f;
        Vector3 escalaInicio = escalaFinal * escalaInicialObjeto;

        while (tiempo < duracionAparecerObjeto)
        {
            tiempo += Time.deltaTime;
            float t = Mathf.Clamp01(tiempo / duracionAparecerObjeto);
            float smooth = Mathf.SmoothStep(0f, 1f, t);

            if (objeto != null)
            {
                objeto.transform.localScale = Vector3.Lerp(
                    escalaInicio,
                    escalaFinal,
                    smooth
                );
            }

            yield return null;
        }

        if (objeto != null)
        {
            objeto.transform.localScale = escalaFinal;
        }
    }

    private IEnumerator DesaparecerObjetoFrontal(GameObject objeto)
    {
        if (objeto == null)
        {
            yield break;
        }

        float tiempo = 0f;
        Vector3 escalaInicio = objeto.transform.localScale;
        Vector3 escalaFinal = escalaInicio * escalaInicialObjeto;

        while (tiempo < duracionDesaparecerObjeto)
        {
            tiempo += Time.deltaTime;
            float t = Mathf.Clamp01(tiempo / duracionDesaparecerObjeto);
            float smooth = Mathf.SmoothStep(0f, 1f, t);

            if (objeto != null)
            {
                objeto.transform.localScale = Vector3.Lerp(
                    escalaInicio,
                    escalaFinal,
                    smooth
                );
            }

            yield return null;
        }

        if (objeto != null)
        {
            objeto.transform.localScale = escalaFinal;
        }
    }

    private IEnumerator DesaparecerYQuitarObjetoFrontalActual()
    {
        if (objetoFrontalActual == null)
        {
            yield break;
        }

        GameObject objetoParaQuitar = objetoFrontalActual;

        yield return DesaparecerObjetoFrontal(objetoParaQuitar);

        if (objetoParaQuitar != null)
        {
            DesregistrarObjetoParaAlturaDinamica(objetoParaQuitar.transform);
            Destroy(objetoParaQuitar);
        }

        if (objetoFrontalActual == objetoParaQuitar)
        {
            objetoFrontalActual = null;
            indiceObjetoFrontalActual = -1;
            infoObjetoFrontalActual = null;
        }
    }

    private void RotarPanelHaciaCabeza(Transform panel, bool invertirFrente)
    {
        if (panel == null || headReference == null)
        {
            return;
        }

        Vector3 direccion = panel.position - headReference.position;

        if (soloRotacionYPanel)
        {
            direccion.y = 0f;
        }

        if (direccion.sqrMagnitude < 0.001f)
        {
            return;
        }

        Quaternion rotacionObjetivo = Quaternion.LookRotation(
            direccion.normalized,
            Vector3.up
        );

        if (invertirFrente)
        {
            rotacionObjetivo *= Quaternion.Euler(0f, 180f, 0f);
        }

        panel.rotation = rotacionObjetivo;
    }

    private void RotarObjetoHaciaCabeza(
        Transform objeto,
        bool soloRotacionY,
        bool invertirFrente)
    {
        if (objeto == null || headReference == null)
        {
            return;
        }

        Vector3 direccion = objeto.position - headReference.position;

        if (soloRotacionY)
        {
            direccion.y = 0f;
        }

        if (direccion.sqrMagnitude < 0.001f)
        {
            return;
        }

        Quaternion rotacionObjetivo = Quaternion.LookRotation(
            direccion.normalized,
            Vector3.up
        );

        if (invertirFrente)
        {
            rotacionObjetivo *= Quaternion.Euler(0f, 180f, 0f);
        }

        objeto.rotation = rotacionObjetivo;
    }

    private void OnDrawGizmos()
    {
        if (!dibujarGizmosSiempre)
        {
            return;
        }

        DibujarGizmos();
    }

    private void OnDrawGizmosSelected()
    {
        DibujarGizmos();
    }

    private void DibujarGizmos()
    {
        Transform referencia = referenciaManual != null ? referenciaManual : transform;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(referencia.position, 0.08f);

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(
            referencia.position,
            referencia.position + referencia.forward * 0.8f
        );

        Gizmos.color = Color.red;
        Gizmos.DrawLine(
            referencia.position,
            referencia.position + referencia.right * 0.5f
        );

        if (paneles != null)
        {
            Gizmos.color = Color.cyan;

            for (int i = 0; i < paneles.Count; i++)
            {
                PanelManualInfo panel = paneles[i];

                if (panel == null || !panel.activo)
                {
                    continue;
                }

                Vector3 posicion = referencia.TransformPoint(panel.posicionLocal);
                Gizmos.DrawWireCube(posicion, Vector3.one * 0.12f);
                Gizmos.DrawLine(referencia.position, posicion);
            }
        }

        if (usarObjetoFrontal)
        {
            Gizmos.color = Color.magenta;

            Vector3 posicionObjeto;

            if (objetoFrontalUsarReferenciaManual)
            {
                posicionObjeto = referencia.TransformPoint(posicionLocalObjetoFrontal);
            }
            else if (headReference != null)
            {
                Vector3 forward = headReference.forward;
                forward.y = 0f;

                if (forward.sqrMagnitude < 0.001f)
                {
                    forward = Vector3.forward;
                }

                forward.Normalize();

                Quaternion rotacionY = Quaternion.LookRotation(forward, Vector3.up);

                Vector3 posicionBase = headReference.position;

                if (objetoFrontalAfectadoPorAlturaPostura)
                {
                    posicionBase.y = ObtenerAlturaBaseObjetoFrontal();
                }

                posicionObjeto =
                    posicionBase +
                    rotacionY * posicionLocalObjetoFrontal +
                    Vector3.up * offsetAlturaObjetoFrontal;
            }
            else
            {
                posicionObjeto = referencia.TransformPoint(posicionLocalObjetoFrontal);
            }

            Gizmos.DrawWireCube(posicionObjeto, Vector3.one * 0.25f);
            Gizmos.DrawLine(referencia.position, posicionObjeto);
        }
    }
}
