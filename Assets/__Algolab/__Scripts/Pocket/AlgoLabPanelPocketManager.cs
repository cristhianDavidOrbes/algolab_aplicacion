using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;

public class AlgoLabPanelPocketManager : MonoBehaviour
{
    public static AlgoLabPanelPocketManager Instance { get; private set; }

    [Header("Referencias principales")]
    public AlgoLabPocketMiniCardView miniCardPrefab;
    public RectTransform miniCardsParent;

    [Header("Regiones del panel de opciones")]
    public RectTransform regionPanelesGuardados;
    public RectTransform regionBotonesOpciones;
    public List<Button> botonesPanelOpciones = new List<Button>();
    public bool autoBuscarRegionBotonesOpciones = true;
    public bool autoRegistrarBotonesDeRegion = true;
    public bool bloquearBotonesCuandoPanelOpcionesNoVisible = true;

    [Header("Slots del arco en orden")]
    public RectTransform pointIzquierdo3;
    public RectTransform pointIzquierdo2;
    public RectTransform pointIzquierdo1;
    public RectTransform pointCenter;
    public RectTransform pointDerecho1;
    public RectTransform pointDerecho2;
    public RectTransform pointDerecho3;

    [Header("Visual del bolsillo / arco")]
    public GameObject pocketVisualRoot;
    public CanvasGroup pocketVisualCanvasGroup;
    public bool ocultarCarruselAlIniciar = true;
    public bool mostrarCarruselSiControlesCercaAunqueEsteVacio = true;
    public bool mantenerVisibleSiHayPanelesGuardados = false;
    public float segundosVisibleTrasGuardar = 3f;
    public float distanciaMostrarCarrusel = 0.30f;
    public float distanciaCarruselTotalmenteVisible = 0.20f;
    public float velocidadFadeCarrusel = 10f;

    [Header("Mostrar arco acercando mandos")]
    [Tooltip("Activado = el arco aparece cuando el mando derecho se acerca al mando izquierdo, aunque todavía no haya paneles guardados.")]
    public bool mostrarArcoCuandoMandoDerechoSeAcercaAlIzquierdo = true;

    [Tooltip("Activado = si no hay mini cards guardadas, igual permite mostrar el arco por cercanía entre mandos.")]
    public bool mostrarArcoAunqueNoHayaPanelesGuardados = true;

    [Tooltip("Activado = NO apaga el GameObject del arco al ocultarlo; solo baja el alpha. Esto evita que el Manager se desactive y ya no pueda detectar los mandos.")]
    public bool mantenerPocketVisualRootActivoAlOcultar = true;

    [Tooltip("Activado = busca RightControllerAnchor y LeftControllerAnchor automáticamente si no están asignados.")]
    public bool autoBuscarMandosParaMostrarArco = true;

    [Tooltip("Tiempo que se conserva la cercanía reportada desde el SpherePoint/PointerExtractor.")]
    public float tiempoRecordarCercaniaMandoDerecho = 0.35f;

    [Header("Modo muy cerca - forzado")]
    [Tooltip("Activado = fuerza en runtime las distancias 0.30 / 0.20 aunque Unity tenga valores viejos guardados en el Inspector.")]
    public bool forzarDistanciasMuyCercaEnRuntime = true;

    [Tooltip("Activado = el carrusel SOLO aparece por cercanía real entre el mando derecho y el mando izquierdo/PocketWorldPoint. Ignora que ya haya paneles guardados.")]
    public bool mostrarCarruselSoloSiMandosMuyCerca = true;

    [Tooltip("Activado = ignora la cercania del SpherePoint. Debe quedar desactivado para responder a la punta real del mando.")]
    public bool ignorarReportesPointerParaMostrarCarrusel = false;

    [Header("Mini card agarrada")]
    [Tooltip("Activado = si el usuario ya agarró una mini card, el arco/carrusel no se oculta aunque aleje la mano del arco.")]
    public bool mantenerCarruselVisibleMientrasMiniCardAgarrada = true;

    [Tooltip("Activado = mientras una mini card está agarrada, no se apaga el GameObject del arco. Esto evita que la card desaparezca por estar dentro del canvas del carrusel.")]
    public bool noDesactivarRootMientrasMiniCardAgarrada = true;

    [Header("Acceso al juego")]
    [Tooltip("Activado = el arco/carrusel queda totalmente oculto mientras el usuario está en el inicio/login y aún no entró al juego.")]
    public bool ocultarArcoHastaEntrarAlJuego = true;

    [Tooltip("Activado = busca automáticamente el AlgoLabSessionManager para saber si ya se entró como usuario o invitado.")]
    public bool autoBuscarSessionManagerParaArco = true;

    [Tooltip("Referencia de sesión. Si está vacía, se busca sola.")]
    public AlgoLabSessionManager sessionManager;

    [Tooltip("Activado = al terminar u omitir el tutorial, el panel de opciones queda usable aunque la sesion tarde un frame en reportarse iniciada.")]
    public bool permitirPanelOpcionesTrasSalirTutorialAunqueSesionTarde = true;

    [SerializeField, Tooltip("Estado interno: el tutorial ya dejo habilitado el panel de opciones para el resto de la app.")]
    private bool panelOpcionesHabilitadoTrasTutorial = false;

    [Header("Tutorial interactivo")]
    public AlgoLabTutorialPanelController tutorialController;
    public bool autoBuscarTutorialController = true;

    [Header("Control del arco por tutorial")]
    [Tooltip("Activado = aunque ya estés dentro del juego, el arco queda bloqueado hasta que un evento del tutorial lo habilite.")]
    public bool bloquearArcoHastaEventoTutorial = true;

    [Tooltip("Activado = al iniciar la escena el panel de opciones queda bloqueado aunque haya quedado habilitado en una escena vieja.")]
    public bool iniciarPanelOpcionesBloqueado = true;

    [Tooltip("Estado interno. Si Bloquear Arco Hasta Evento Tutorial está activo, este valor debe estar activo para que el arco funcione.")]
    [SerializeField] private bool arcoHabilitadoPorEventoTutorial = false;

    [Tooltip("Activado = al deshabilitar el arco desde el tutorial también se oculta visualmente el carrusel.")]
    public bool ocultarCarruselAlDeshabilitarArcoPorTutorial = true;

    [Header("Puntos de mundo")]
    public Transform leftPocketWorldPoint;
    public Transform rightHandParaMostrarCarrusel;

    [Tooltip("Opcional. Si está vacío, se usa LeftPocketWorldPoint como respaldo para comparar con el mando derecho.")]
    public Transform leftHandParaMostrarCarrusel;

    [Tooltip("Activado = si no se asigna Left Hand Para Mostrar Carrusel, usa LeftPocketWorldPoint como punto izquierdo para mostrar el arco.")]
    public bool usarLeftPocketWorldPointComoRespaldoMandoIzquierdo = true;

    public Camera camaraJugador;

    [Header("Guardado")]
    public float distanciaGuardarPanel = 0.35f;
    public bool soloGuardarSiPanelEstaAgarrado = true;
    public bool ocultarPanelRealAlGuardar = true;

    [Header("Paneles ocultos")]
    [Tooltip("Activado = si un panel guardable ya esta desactivado en la escena, aparece como mini card en el panel de opciones.")]
    public bool autoRegistrarPanelesDesactivadosEnPanelOpciones = true;

    [Tooltip("Activado = solo registra automaticamente paneles marcados como guardables y que no sean panel principal.")]
    public bool soloRegistrarPanelesGuardablesDesactivados = true;

    [Tooltip("Activado = cada vez que el tutorial habilita el panel de opciones, vuelve a buscar paneles desactivados para mostrarlos como cards.")]
    public bool registrarPanelesDesactivadosAlHabilitarPanelOpciones = true;

    [Tooltip("Cada cuantos segundos se vuelve a buscar si hay paneles guardables desactivados para mostrarlos en opciones. 0 = solo al iniciar/habilitar.")]
    public float intervaloRegistrarPanelesDesactivados = 0.5f;

    [Tooltip("Activado = mientras el tutorial esta activo, no auto-registra paneles desactivados que no esten realmente guardados. Evita que paneles que el tutorial muestra/oculta aparezcan solos en el arco.")]
    public bool pausarAutoRegistroPanelesDesactivadosMientrasTutorialActivo = true;

    [Tooltip("Activado = si un panel guardable esta desactivado por otro sistema y no esta marcado como guardado, igual aparece como card. Se pausa durante el tutorial.")]
    public bool registrarPanelesDesactivadosExternamente = true;

    [Tooltip("Activado = si un panel de la lista ya no esta marcado como guardado y esta activo en escena, el arco lo quita de sus cards en vez de volverlo a esconder.")]
    public bool reconciliarListaConEstadoRealPaneles = true;

    [Tooltip("Tiempo que un panel queda protegido contra auto-registro despues de sacarlo desde una mini card.")]
    public float tiempoIgnorarAutoRegistroTrasRestaurar = 2f;

    [Header("Paneles dentro del arco")]
    [Tooltip("Activado = todo panel que esté guardado dentro del arco queda con su GameObject real desactivado. Solo queda visible la mini card.")]
    public bool forzarPanelesGuardadosDesactivadosEnArco = true;

    [Tooltip("Activado = revisa cada frame que los paneles guardados sigan desactivados. Evita que otro script los reactive mientras están en el arco.")]
    public bool sincronizarPanelesGuardadosDesactivadosCadaFrame = true;

    [Tooltip("Activado = cuando sacas una mini card, el panel real se vuelve a activar automáticamente.")]
    public bool activarPanelRealAlSalirDelArco = true;

    public float distanciaRestaurarFrenteJugador = 1.15f;
    public float alturaRestaurarRespectoCamara = -0.05f;

    [Header("Animación guardar")]
    public bool animarPanelAlGuardar = true;
    public float duracionEncogerPanelAlGuardar = 0.28f;
    public float escalaFinalPanelGuardado = 0.01f;
    public float duracionAparecerCardGuardada = 0.36f;
    public float escalaInicialCardGuardada = 0.05f;
    public float reboteEscalaCardGuardada = 1.22f;
    public bool apartarCardCentralAlGuardar = true;

    [Header("Respeto del prefab")]
    public bool respetarTamanoYFormaDelPrefab = true;
    public bool respetarEscalaDelPrefab = true;
    public Vector2 tamanoForzadoMiniCard = new Vector2(18f, 12f);
    public float escalaCentro = 1f;
    public float escalaLateral = 0.9f;
    public float alphaCentro = 1f;
    public float alphaLateral = 0.85f;

    [Header("Animación carrusel")]
    public float duracionAnimacion = 0.35f;
    public bool bloquearInputMientrasAnima = true;
    public bool repetirPanelCuandoSoloHayDos = true;

    [Header("Rotación por slots")]
    public bool rotarMiniCardsPorSlot = true;
    public float rotacionMaximaExtremos = 90f;
    public bool invertirRotacionMiniCard = false;

    [Header("Palanca izquierda")]
    public bool girarConPalancaIzquierda = true;
    public float umbralStick = 0.65f;
    public float cooldownGiro = 0.35f;
    public bool invertirGiroStick = false;

    [Header("Restaurar con puntero")]
    public bool restaurarPunteroFrenteAlJugador = true;

    [Tooltip("Activado = el panel aparece exactamente donde sueltas la mini card / SpherePoint.")]
    public bool restaurarPanelExactamenteDondeSueltaCard = true;

    [Tooltip("Ajuste opcional en mundo al restaurar desde card. Normalmente 0,0,0.")]
    public Vector3 offsetMundoAlRestaurarDesdeCard = Vector3.zero;

    [Tooltip("Solo se usa si Restaurar Panel Exactamente Donde Suelta Card está apagado.")]
    public float distanciaRestaurarDesdePuntero = 0.18f;

    public bool animarSacarPanel = true;
    public float duracionEncogerCardAlSacar = 0.18f;
    public float duracionCrecerPanelAlSacar = 0.32f;
    public float escalaInicialPanelRestaurado = 0.03f;
    public float reboteEscalaPanelRestaurado = 1.08f;

    [Header("Bloqueo de mini cards")]
    [Tooltip("Después de guardar un panel, bloquea por este tiempo el agarre de mini cards.")]
    public float cooldownAgarrarCardDespuesDeGuardarPanel = 1f;

    [Tooltip("Activado = mientras un panel real esté agarrado, NO se puede agarrar una mini card.")]
    public bool bloquearAgarreCardsMientrasPanelRealAgarrado = true;

    [Tooltip("Activado = mientras una mini card esté agarrada, se avisa al sistema para evitar dobles agarres.")]
    public bool registrarMiniCardAgarrada = true;

    [Tooltip("Si un panel real quedó registrado como agarrado pero no se actualiza, se limpia solo. Esto evita que no puedas agarrar cards nunca.")]
    public bool limpiarBloqueoPanelAgarradoPegado = true;

    [Tooltip("Tiempo máximo sin actualización para considerar que el bloqueo de panel agarrado quedó pegado.")]
    public float tiempoMaximoPanelAgarradoSinActualizar = 0.20f;

    [Tooltip("Si una animación queda pegada por error, después de este tiempo deja agarrar cards otra vez.")]
    public float tiempoMaximoAnimacionBloqueandoCards = 2.5f;

    [Header("Bloqueo por panel cerca del arco")]
    [Tooltip("Activado = si un panel guardable está agarrado y cerca del arco, NO deja agarrar mini cards.")]
    public bool bloquearCardsSiPanelGuardableCercaDelArco = true;

    [Tooltip("Cuánto tiempo se recuerda que un panel estuvo cerca del arco. Debe ser corto para que no se quede bloqueado.")]
    public float tiempoRecordarPanelCercaDelArco = 0.35f;

    [Header("Debug")]
    public bool mostrarDebug = true;

    private readonly List<AlgoLabPocketPanelItem> panelesGuardados = new List<AlgoLabPocketPanelItem>();
    private readonly List<AlgoLabPocketMiniCardView> slotCards = new List<AlgoLabPocketMiniCardView>();
    private readonly List<Vector3> escalasOriginales = new List<Vector3>();

    private int indiceSeleccionado;
    private float ultimoGiro = -999f;
    private float alphaCarruselActual;
    private float visibleForzadoHasta = -999f;
    private float cercaniaReportada;
    private float tiempoCercaniaReportada = -999f;
    private bool animando;
    private Coroutine rutina;
    private TipoOperacion operacionActiva = TipoOperacion.Ninguna;
    private AlgoLabPocketPanelItem panelOperacionActiva;
    private AlgoLabPocketPanelItem itemAccionConfiguracion;
    private Sprite iconoAccionConfiguracion;
    private Action accionConfiguracion;
    private bool accionConfiguracionVisible = true;

    private float tiempoBloqueoCardsHasta = -999f;
    private bool miniCardAgarrada;
    private float tiempoInicioAnimando = -999f;
    private float proximaRevisionPanelesDesactivados = -999f;

    private readonly HashSet<AlgoLabPocketPanelItem> panelesRealesAgarrados = new HashSet<AlgoLabPocketPanelItem>();
    private readonly Dictionary<AlgoLabPocketPanelItem, float> ultimaActualizacionPanelRealAgarrado = new Dictionary<AlgoLabPocketPanelItem, float>();

    private readonly Dictionary<AlgoLabPocketPanelItem, float> panelesGuardablesCercaDelArco = new Dictionary<AlgoLabPocketPanelItem, float>();
    private readonly Dictionary<AlgoLabPocketPanelItem, float> autoRegistroBloqueadoHasta = new Dictionary<AlgoLabPocketPanelItem, float>();
    private readonly Dictionary<Button, bool> interactableOriginalBotonesPanelOpciones = new Dictionary<Button, bool>();
    private bool interfazModalActiva;

    private const int EXT_IZQ = 0;
    private const int VIS_IZQ = 1;
    private const int CENTRO = 3;
    private const int VIS_DER = 5;
    private const int EXT_DER = 6;

    private enum TipoOperacion
    {
        Ninguna,
        Guardar,
        Restaurar,
        Girar
    }

    public bool ArcoPermitidoPorJuego => JuegoPermiteMostrarArco();
    public bool ArcoHabilitadoPorEventoTutorial => EventoTutorialPermiteUsarArco();
    public bool ArcoDisponibleParaInteraccion =>
        !interfazModalActiva && JuegoPermiteMostrarArco() && EventoTutorialPermiteUsarArco();
    public bool InterfazModalActiva => interfazModalActiva;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        AplicarConfiguracionMuyCercaRuntime();
        if (iniciarPanelOpcionesBloqueado) arcoHabilitadoPorEventoTutorial = false;
        AutoBuscarReferencias();
        if (camaraJugador == null) camaraJugador = Camera.main;
        if (ocultarCarruselAlIniciar) SetCarruselAlphaInmediato(0f);
    }

    private void Start()
    {
        AplicarConfiguracionMuyCercaRuntime();
        AutoBuscarReferencias();
        RegistrarPanelesDesactivadosEnPanelOpciones();
        RegistrarAccionConfiguracion(AbrirConfiguracionAutonoma);
        if (ocultarCarruselAlIniciar) SetCarruselAlphaInmediato(0f);
        else SetCarruselAlphaInmediato(1f);
    }

    private void Update()
    {
        VerificarOperacionAtascada();
        AplicarConfiguracionMuyCercaRuntime();
        AutoBuscarSessionManagerParaArco();
        AsegurarPresenciaAccionConfiguracion();

        if (interfazModalActiva)
        {
            OcultarCarruselPorArcoBloqueado();
            ActualizarEstadoBotonesPanelOpciones();
            return;
        }

        SincronizarPanelesGuardadosDesactivados();
        RefrescarPanelesDesactivadosSiCorresponde();

        if (!ArcoDisponibleParaInteraccion)
        {
            OcultarCarruselPorArcoBloqueado();
            ActualizarEstadoBotonesPanelOpciones();
            return;
        }

        if (girarConPalancaIzquierda && !miniCardAgarrada) LeerStickIzquierdo();
        ActualizarVisibilidadCarrusel();
        ActualizarEstadoBotonesPanelOpciones();
    }

    private void OnDisable()
    {
        CancelarOperacionActivaYReconciliar();
        miniCardAgarrada = false;
        panelesRealesAgarrados.Clear();
        ultimaActualizacionPanelRealAgarrado.Clear();
        panelesGuardablesCercaDelArco.Clear();
    }

    private void OnDestroy()
    {
        accionConfiguracion = null;

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void VerificarOperacionAtascada()
    {
        if (!animando)
        {
            if (rutina != null && operacionActiva != TipoOperacion.Ninguna)
            {
                CancelarOperacionActivaYReconciliar();
            }

            return;
        }

        float limite = Mathf.Max(0.5f, tiempoMaximoAnimacionBloqueandoCards);
        if (Time.unscaledTime - tiempoInicioAnimando > limite)
        {
            CancelarOperacionActivaYReconciliar();
        }
    }

    private void CancelarOperacionActivaYReconciliar()
    {
        Coroutine rutinaCancelada = rutina;
        TipoOperacion tipoCancelado = operacionActiva;
        AlgoLabPocketPanelItem panelCancelado = panelOperacionActiva;

        rutina = null;
        animando = false;
        operacionActiva = TipoOperacion.Ninguna;
        panelOperacionActiva = null;

        if (rutinaCancelada != null)
        {
            StopCoroutine(rutinaCancelada);
        }

        if (panelCancelado != null)
        {
            bool sigueGuardado = panelesGuardados.Contains(panelCancelado);

            if (tipoCancelado == TipoOperacion.Guardar)
            {
                if (sigueGuardado)
                {
                    panelCancelado.ForzarEstadoDentroDelArco(true);
                }
                else
                {
                    panelCancelado.CancelarGuardadoNoConfirmado();
                }
            }
            else if (tipoCancelado == TipoOperacion.Restaurar)
            {
                if (sigueGuardado)
                {
                    panelCancelado.ForzarEstadoDentroDelArco(true);
                }
                else
                {
                    panelCancelado.CompletarRestauracionInterrumpida();
                    BloquearAutoRegistroTemporal(panelCancelado, tiempoIgnorarAutoRegistroTrasRestaurar);
                    NotificarTutorialPanelRestaurado(panelCancelado);
                }
            }
        }

        miniCardAgarrada = false;

        if (isActiveAndEnabled)
        {
            indiceSeleccionado = Mathf.Clamp(
                indiceSeleccionado,
                0,
                Mathf.Max(0, panelesGuardados.Count - 1)
            );
            ActualizarContenidoReposo();
            AplicarLayoutReposoInmediato();
        }
    }

    private void FinalizarOperacionActiva()
    {
        animando = false;
        rutina = null;
        operacionActiva = TipoOperacion.Ninguna;
        panelOperacionActiva = null;
    }

    [ContextMenu("Auto buscar referencias")]
    public void AutoBuscarReferencias()
    {
        if (autoBuscarRegionBotonesOpciones && regionPanelesGuardados == null)
        {
            regionPanelesGuardados = BuscarRect("regionpanelesguardados", "panelesguardados", "cardscontainer", "minicardsparent", "cards", "cardsparent");
        }

        if (miniCardsParent == null && regionPanelesGuardados != null) miniCardsParent = regionPanelesGuardados;
        if (miniCardsParent == null) miniCardsParent = BuscarRect("cardscontainer", "minicardsparent", "cards", "cardsparent");
        if (pointIzquierdo3 == null) pointIzquierdo3 = BuscarRect("pointizquierdo3", "izquierdo3", "left3");
        if (pointIzquierdo2 == null) pointIzquierdo2 = BuscarRect("pointizquierdo2", "izquierdo2", "left2");
        if (pointIzquierdo1 == null) pointIzquierdo1 = BuscarRect("pointizquierdo1", "izquierdo1", "left1");
        if (pointCenter == null) pointCenter = BuscarRect("pointcenter", "center", "centro");
        if (pointDerecho1 == null) pointDerecho1 = BuscarRect("pointderecho1", "derecho1", "right1");
        if (pointDerecho2 == null) pointDerecho2 = BuscarRect("pointderecho2", "derecho2", "right2");
        if (pointDerecho3 == null) pointDerecho3 = BuscarRect("pointderecho3", "derecho3", "right3");
        if (leftPocketWorldPoint == null) leftPocketWorldPoint = BuscarTransform("pocketworldpoint", "leftpocketworldpoint", "worldpoint");
        if (autoBuscarMandosParaMostrarArco) AutoBuscarMandosParaArco();
        if (pocketVisualRoot == null)
        {
            Transform canvas = BuscarTransform("pocketcanvas", "canvas");
            pocketVisualRoot = canvas != null ? canvas.gameObject : gameObject;
        }
        if (pocketVisualRoot != null && pocketVisualCanvasGroup == null)
        {
            pocketVisualCanvasGroup = pocketVisualRoot.GetComponent<CanvasGroup>();
            if (pocketVisualCanvasGroup == null) pocketVisualCanvasGroup = pocketVisualRoot.AddComponent<CanvasGroup>();
        }
        if (miniCardsParent == null && pointCenter != null && pointCenter.parent != null) miniCardsParent = pointCenter.parent as RectTransform;
        if (regionPanelesGuardados == null && miniCardsParent != null) regionPanelesGuardados = miniCardsParent;

        if (autoBuscarRegionBotonesOpciones && regionBotonesOpciones == null)
        {
            regionBotonesOpciones = BuscarRect("regionbotonesopciones", "botonesopciones", "botonespanelopciones", "botones", "botonesaccion", "optionsbuttons", "buttonscontainer", "buttoncontainer");
        }

        RegistrarBotonesPanelOpcionesDesdeRegion();
        ActualizarEstadoBotonesPanelOpciones();
        AutoBuscarSessionManagerParaArco();
    }


    private void AutoBuscarSessionManagerParaArco()
    {
        if (!autoBuscarSessionManagerParaArco)
        {
            return;
        }

        if (sessionManager != null)
        {
            return;
        }

        sessionManager = AlgoLabSessionManager.Instance;

        if (sessionManager == null)
        {
            sessionManager = FindFirstObjectByType<AlgoLabSessionManager>(FindObjectsInactive.Include);
        }
    }

    private void AutoBuscarTutorialController()
    {
        if (!autoBuscarTutorialController || tutorialController != null)
        {
            return;
        }

        tutorialController = FindFirstObjectByType<AlgoLabTutorialPanelController>(FindObjectsInactive.Include);
    }

    private void NotificarTutorialPanelGuardado(AlgoLabPocketPanelItem panel)
    {
        if (panel == null)
        {
            return;
        }

        AutoBuscarTutorialController();

        if (tutorialController != null)
        {
            tutorialController.NotificarPanelGuardadoEnPanelOpciones(panel);
        }
    }

    private void NotificarTutorialPanelRestaurado(AlgoLabPocketPanelItem panel)
    {
        if (panel == null)
        {
            return;
        }

        AutoBuscarTutorialController();

        if (tutorialController != null)
        {
            tutorialController.NotificarPanelRestauradoDesdePanelOpciones(panel);
        }
    }

    public void BloquearAutoRegistroTemporal(AlgoLabPocketPanelItem panel, float segundos)
    {
        if (panel == null)
        {
            return;
        }

        float duracion = Mathf.Max(0.1f, segundos);
        autoRegistroBloqueadoHasta[panel] = Time.unscaledTime + duracion;
    }

    public void BloquearAutoRegistroPanelesDeObjeto(GameObject objeto, float segundos)
    {
        if (objeto == null)
        {
            return;
        }

        AlgoLabPocketPanelItem[] paneles = objeto.GetComponentsInChildren<AlgoLabPocketPanelItem>(true);

        for (int i = 0; i < paneles.Length; i++)
        {
            BloquearAutoRegistroTemporal(paneles[i], segundos);
        }
    }

    public void PrepararPanelesDeObjetoParaControlTutorial(GameObject objeto, float segundosBloqueo, bool activarPanelReal)
    {
        if (objeto == null)
        {
            return;
        }

        AlgoLabPocketPanelItem[] paneles = objeto.GetComponentsInChildren<AlgoLabPocketPanelItem>(true);
        bool listaCambio = false;

        for (int i = 0; i < paneles.Length; i++)
        {
            AlgoLabPocketPanelItem panel = paneles[i];

            if (panel == null)
            {
                continue;
            }

            BloquearAutoRegistroTemporal(panel, segundosBloqueo);

            int index = panelesGuardados.IndexOf(panel);
            if (index >= 0)
            {
                panelesGuardados.RemoveAt(index);
                listaCambio = true;
            }

            if (activarPanelReal)
            {
                panel.ForzarEstadoDentroDelArco(false);
            }
            else
            {
                panel.LimpiarEstadoPocketSinActivar();
            }
        }

        if (listaCambio)
        {
            indiceSeleccionado = Mathf.Clamp(indiceSeleccionado, 0, Mathf.Max(0, panelesGuardados.Count - 1));
            CrearCardsVisuales();
            ActualizarContenidoReposo();
            AplicarLayoutReposoInmediato();
        }
    }

    private bool AutoRegistroBloqueadoParaPanel(AlgoLabPocketPanelItem panel)
    {
        if (panel == null)
        {
            return true;
        }

        if (autoRegistroBloqueadoHasta.TryGetValue(panel, out float bloqueadoHasta))
        {
            if (Time.unscaledTime < bloqueadoHasta)
            {
                return true;
            }

            autoRegistroBloqueadoHasta.Remove(panel);
        }

        if (pausarAutoRegistroPanelesDesactivadosMientrasTutorialActivo)
        {
            AutoBuscarTutorialController();

            if (tutorialController != null &&
                tutorialController.TutorialEnCurso &&
                !panel.EstaGuardado())
            {
                return true;
            }
        }

        return false;
    }

    private bool JuegoPermiteMostrarArco()
    {
        if (!ocultarArcoHastaEntrarAlJuego)
        {
            return true;
        }

        if (permitirPanelOpcionesTrasSalirTutorialAunqueSesionTarde && panelOpcionesHabilitadoTrasTutorial)
        {
            return true;
        }

        AutoBuscarSessionManagerParaArco();

        if (sessionManager == null)
        {
            return false;
        }

        // SesionIniciada se activa tanto con login real como con invitado.
        // Antes de entrar al juego/login inicial queda false, por eso el arco no aparece.
        return sessionManager.SesionIniciada;
    }

    private bool EventoTutorialPermiteUsarArco()
    {
        if (!bloquearArcoHastaEventoTutorial)
        {
            return true;
        }

        return arcoHabilitadoPorEventoTutorial;
    }

    [ContextMenu("Arco tutorial - habilitar")]
    public void HabilitarArcoPorEventoTutorial()
    {
        arcoHabilitadoPorEventoTutorial = true;

        AutoBuscarReferencias();

        if (registrarPanelesDesactivadosAlHabilitarPanelOpciones)
        {
            RegistrarPanelesDesactivadosEnPanelOpciones();
        }

        ActualizarEstadoBotonesPanelOpciones();

        if (mostrarDebug)
        {
            Debug.Log("POCKET MANAGER: arco habilitado por evento del tutorial.");
        }
    }

    [ContextMenu("Arco tutorial - deshabilitar")]
    public void DeshabilitarArcoPorEventoTutorial()
    {
        arcoHabilitadoPorEventoTutorial = false;
        panelOpcionesHabilitadoTrasTutorial = false;
        cercaniaReportada = 0f;
        tiempoCercaniaReportada = -999f;
        visibleForzadoHasta = -999f;

        if (ocultarCarruselAlDeshabilitarArcoPorTutorial)
        {
            SetCarruselAlphaInmediato(0f);
        }

        if (mostrarDebug)
        {
            Debug.Log("POCKET MANAGER: arco deshabilitado por tutorial.");
        }
    }

    public void SetArcoHabilitadoPorEventoTutorial(bool habilitado)
    {
        if (habilitado) HabilitarArcoPorEventoTutorial();
        else DeshabilitarArcoPorEventoTutorial();
    }

    [ContextMenu("Panel opciones tutorial - habilitar")]
    public void HabilitarPanelOpciones()
    {
        HabilitarArcoPorEventoTutorial();
    }

    public void HabilitarPanelOpcionesTrasTutorial(bool mostrarTemporalmente = true)
    {
        if (permitirPanelOpcionesTrasSalirTutorialAunqueSesionTarde)
        {
            panelOpcionesHabilitadoTrasTutorial = true;
        }

        HabilitarArcoPorEventoTutorial();
        AutoBuscarReferencias();
        RegistrarPanelesDesactivadosEnPanelOpciones();
        ActualizarContenidoReposo();
        AplicarLayoutReposoInmediato();
        ActualizarEstadoBotonesPanelOpciones();

        if (pocketVisualRoot != null)
        {
            ActivarCadena(pocketVisualRoot.transform);

            if (mantenerPocketVisualRootActivoAlOcultar || panelesGuardados.Count > 0)
            {
                pocketVisualRoot.SetActive(true);
            }
        }

        if (miniCardsParent != null)
        {
            ActivarCadena(miniCardsParent);
            miniCardsParent.gameObject.SetActive(true);
        }

        if (mostrarTemporalmente && panelesGuardados.Count > 0)
        {
            ForzarMostrarCarruselTemporal(segundosVisibleTrasGuardar);
        }
    }

    [ContextMenu("Panel opciones tutorial - deshabilitar")]
    public void DeshabilitarPanelOpciones()
    {
        DeshabilitarArcoPorEventoTutorial();
    }

    public void SetPanelOpcionesHabilitadoPorEventoTutorial(bool habilitado)
    {
        SetArcoHabilitadoPorEventoTutorial(habilitado);
    }

    public void HabilitarArco()
    {
        HabilitarArcoPorEventoTutorial();
    }

    public void DeshabilitarArco()
    {
        DeshabilitarArcoPorEventoTutorial();
    }

    public void SetInterfazModalActiva(bool activa)
    {
        if (interfazModalActiva == activa)
        {
            return;
        }

        interfazModalActiva = activa;
        cercaniaReportada = 0f;
        tiempoCercaniaReportada = -999f;
        visibleForzadoHasta = -999f;

        if (activa)
        {
            SetCarruselAlphaInmediato(0f);
        }
        else
        {
            ActualizarContenidoReposo();
            AplicarLayoutReposoInmediato();
            ActualizarEstadoBotonesPanelOpciones();
        }
    }

    private void OcultarCarruselPorArcoBloqueado()
    {
        cercaniaReportada = 0f;
        tiempoCercaniaReportada = -999f;
        visibleForzadoHasta = -999f;

        float objetivo = 0f;

        if (miniCardAgarrada && mantenerCarruselVisibleMientrasMiniCardAgarrada)
        {
            // Si ya está arrastrando una mini card, no dejamos que se apague su canvas.
            objetivo = 1f;
        }

        alphaCarruselActual = Mathf.MoveTowards(
            alphaCarruselActual,
            objetivo,
            Time.unscaledDeltaTime * Mathf.Max(0.1f, velocidadFadeCarrusel)
        );

        AplicarAlphaCarrusel(alphaCarruselActual);
    }

    private RectTransform BuscarRect(params string[] nombres)
    {
        Transform t = BuscarTransform(nombres);
        return t as RectTransform;
    }

    private Transform BuscarTransform(params string[] nombres)
    {
        Transform[] hijos = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < hijos.Length; i++)
        {
            string n = Normalizar(hijos[i].name);
            for (int j = 0; j < nombres.Length; j++)
            {
                string b = Normalizar(nombres[j]);
                if (n == b || n.Contains(b)) return hijos[i];
            }
        }
        return null;
    }

    private void AutoBuscarMandosParaArco()
    {
        if (rightHandParaMostrarCarrusel == null)
        {
            rightHandParaMostrarCarrusel = BuscarTransformEnEscena(
                "rightcontrolleranchor",
                "rightcontroller",
                "righthandanchor",
                "righthand",
                "right hand",
                "controller right",
                "rtouch"
            );
        }

        if (leftHandParaMostrarCarrusel == null)
        {
            leftHandParaMostrarCarrusel = BuscarTransformEnEscena(
                "leftcontrolleranchor",
                "leftcontroller",
                "lefthandanchor",
                "lefthand",
                "left hand",
                "controller left",
                "ltouch"
            );
        }

        if (leftPocketWorldPoint == null)
        {
            leftPocketWorldPoint = BuscarTransformEnEscena(
                "pocketworldpoint",
                "leftpocketworldpoint",
                "pocket world point",
                "arcopocket",
                "arco"
            );
        }
    }

    private Transform BuscarTransformEnEscena(params string[] nombres)
    {
        Transform[] todos = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        for (int i = 0; i < todos.Length; i++)
        {
            if (todos[i] == null)
            {
                continue;
            }

            string n = Normalizar(todos[i].name);

            for (int j = 0; j < nombres.Length; j++)
            {
                string b = Normalizar(nombres[j]);

                if (n == b || n.Contains(b))
                {
                    return todos[i];
                }
            }
        }

        return null;
    }

    private string Normalizar(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.ToLower().Replace(" ", "").Replace("_", "").Replace("-", "");
    }

    private void AsegurarListaBotonesPanelOpciones()
    {
        if (botonesPanelOpciones == null)
        {
            botonesPanelOpciones = new List<Button>();
        }
    }

    private void RegistrarBotonesPanelOpcionesDesdeRegion()
    {
        AsegurarListaBotonesPanelOpciones();

        if (!autoRegistrarBotonesDeRegion || regionBotonesOpciones == null)
        {
            LimpiarBotonesNulosPanelOpciones();
            return;
        }

        Button[] encontrados = regionBotonesOpciones.GetComponentsInChildren<Button>(true);

        for (int i = 0; i < encontrados.Length; i++)
        {
            Button boton = encontrados[i];

            if (boton == null || boton.GetComponentInParent<AlgoLabPocketMiniCardView>(true) != null)
            {
                continue;
            }

            if (!botonesPanelOpciones.Contains(boton))
            {
                botonesPanelOpciones.Add(boton);
            }

            RegistrarInteractableOriginalBoton(boton);
        }

        LimpiarBotonesNulosPanelOpciones();
    }

    private void LimpiarBotonesNulosPanelOpciones()
    {
        AsegurarListaBotonesPanelOpciones();

        for (int i = botonesPanelOpciones.Count - 1; i >= 0; i--)
        {
            if (botonesPanelOpciones[i] == null)
            {
                botonesPanelOpciones.RemoveAt(i);
            }
        }
    }

    private void RegistrarInteractableOriginalBoton(Button boton)
    {
        if (boton == null || interactableOriginalBotonesPanelOpciones.ContainsKey(boton))
        {
            return;
        }

        interactableOriginalBotonesPanelOpciones.Add(boton, boton.interactable);
    }

    private bool PanelOpcionesVisibleParaBotones()
    {
        if (!ArcoDisponibleParaInteraccion)
        {
            return false;
        }

        if (pocketVisualRoot != null && !pocketVisualRoot.activeInHierarchy)
        {
            return false;
        }

        if (pocketVisualCanvasGroup != null && pocketVisualCanvasGroup.alpha <= 0.01f)
        {
            return false;
        }

        return true;
    }

    public bool BotonesPanelOpcionesInteractivos()
    {
        return PanelOpcionesVisibleParaBotones();
    }

    private void ActualizarEstadoBotonesPanelOpciones()
    {
        AsegurarListaBotonesPanelOpciones();

        bool visible = PanelOpcionesVisibleParaBotones();

        for (int i = botonesPanelOpciones.Count - 1; i >= 0; i--)
        {
            Button boton = botonesPanelOpciones[i];

            if (boton == null)
            {
                botonesPanelOpciones.RemoveAt(i);
                continue;
            }

            RegistrarInteractableOriginalBoton(boton);

            if (!bloquearBotonesCuandoPanelOpcionesNoVisible)
            {
                if (interactableOriginalBotonesPanelOpciones.TryGetValue(boton, out bool original))
                {
                    boton.interactable = original;
                }

                continue;
            }

            bool interactableOriginal = true;
            interactableOriginalBotonesPanelOpciones.TryGetValue(boton, out interactableOriginal);
            boton.interactable = visible && interactableOriginal;
        }
    }

    public Button ObtenerBotonPanelOpcionesEnPunto(Vector3 puntoMundo, float radioMundo)
    {
        if (!BotonesPanelOpcionesInteractivos())
        {
            return null;
        }

        AsegurarListaBotonesPanelOpciones();

        if (camaraJugador == null) camaraJugador = Camera.main;

        Camera camara = camaraJugador != null ? camaraJugador : Camera.main;
        Vector2 puntoPantalla = camara != null
            ? RectTransformUtility.WorldToScreenPoint(camara, puntoMundo)
            : Vector2.zero;

        Button mejor = null;
        float mejorDistancia = float.MaxValue;

        for (int i = 0; i < botonesPanelOpciones.Count; i++)
        {
            Button boton = botonesPanelOpciones[i];

            if (!EsBotonPanelOpcionesValido(boton))
            {
                continue;
            }

            RectTransform rect = boton.transform as RectTransform;
            if (rect == null)
            {
                continue;
            }

            bool dentro = camara != null && RectTransformUtility.RectangleContainsScreenPoint(rect, puntoPantalla, camara);

            if (!dentro && radioMundo > 0f)
            {
                dentro = Vector3.Distance(puntoMundo, rect.position) <= radioMundo;
            }

            if (!dentro)
            {
                continue;
            }

            float distancia = Vector3.Distance(puntoMundo, rect.position);

            if (distancia < mejorDistancia)
            {
                mejorDistancia = distancia;
                mejor = boton;
            }
        }

        return mejor;
    }

    public bool IntentarClickBotonPanelOpciones(Vector3 puntoMundo, float radioMundo, out Button boton)
    {
        boton = ObtenerBotonPanelOpcionesEnPunto(puntoMundo, radioMundo);

        if (boton == null)
        {
            return false;
        }

        return ClickBotonPanelOpciones(boton);
    }

    public bool ClickBotonPanelOpciones(Button boton)
    {
        if (!EsBotonPanelOpcionesValido(boton) || !BotonesPanelOpcionesInteractivos())
        {
            return false;
        }

        boton.onClick.Invoke();

        if (mostrarDebug)
        {
            Debug.Log("POCKET MANAGER: click en boton del panel de opciones -> " + boton.name);
        }

        return true;
    }

    private bool EsBotonPanelOpcionesValido(Button boton)
    {
        if (boton == null || !boton.gameObject.activeInHierarchy || !boton.interactable)
        {
            return false;
        }

        if (boton.GetComponentInParent<AlgoLabPocketMiniCardView>(true) != null)
        {
            return false;
        }

        if (botonesPanelOpciones != null && botonesPanelOpciones.Count > 0 && !botonesPanelOpciones.Contains(boton))
        {
            return false;
        }

        return true;
    }

    private void AplicarConfiguracionMuyCercaRuntime()
    {
        if (!forzarDistanciasMuyCercaEnRuntime)
        {
            return;
        }

        distanciaMostrarCarrusel = 0.30f;
        distanciaCarruselTotalmenteVisible = 0.20f;

        mostrarArcoCuandoMandoDerechoSeAcercaAlIzquierdo = true;
        mostrarArcoAunqueNoHayaPanelesGuardados = true;
        mostrarCarruselSiControlesCercaAunqueEsteVacio = true;

        // Esto es clave: si queda activado, el carrusel se ve aunque los mandos estén lejos
        // cuando ya hay paneles guardados. Para tu caso debe quedar apagado.
        mantenerVisibleSiHayPanelesGuardados = false;

        mantenerPocketVisualRootActivoAlOcultar = true;
        mantenerCarruselVisibleMientrasMiniCardAgarrada = true;
        noDesactivarRootMientrasMiniCardAgarrada = true;
        repetirPanelCuandoSoloHayDos = true;
        autoBuscarMandosParaMostrarArco = true;
        ignorarReportesPointerParaMostrarCarrusel = false;
        ocultarArcoHastaEntrarAlJuego = true;
        autoBuscarSessionManagerParaArco = true;

        // Nuevo: el tutorial decide en qué momento se puede usar el arco.
        bloquearArcoHastaEventoTutorial = true;
        ocultarCarruselAlDeshabilitarArcoPorTutorial = true;

        // Importante para tu último ajuste:
        // los paneles reales guardados en el arco quedan apagados;
        // al sacarlos vuelven a prenderse normal.
        ocultarPanelRealAlGuardar = true;
        forzarPanelesGuardadosDesactivadosEnArco = true;
        sincronizarPanelesGuardadosDesactivadosCadaFrame = true;
        activarPanelRealAlSalirDelArco = true;
    }

    public void ReportarCercaniaMandoDerecho(float cercania01)
    {
        if (!ArcoDisponibleParaInteraccion)
        {
            cercaniaReportada = 0f;
            tiempoCercaniaReportada = -999f;
            return;
        }

        if (ignorarReportesPointerParaMostrarCarrusel)
        {
            cercaniaReportada = 0f;
            tiempoCercaniaReportada = -999f;
            return;
        }

        cercaniaReportada = Mathf.Clamp01(cercania01);
        tiempoCercaniaReportada = Time.unscaledTime;
    }

    private void ActualizarVisibilidadCarrusel()
    {
        if (autoBuscarMandosParaMostrarArco &&
            (rightHandParaMostrarCarrusel == null ||
             (leftHandParaMostrarCarrusel == null && leftPocketWorldPoint == null)))
        {
            AutoBuscarMandosParaArco();
        }

        float objetivo = 0f;
        bool hay = panelesGuardados.Count > 0;
        float cercaniaMandos = ObtenerCercaniaMandosParaMostrarArco();
        float cercaniaPointer = ObtenerCercaniaPointerVigente();

        if (mostrarCarruselSoloSiMandosMuyCerca)
        {
            // Modo estricto: el arco/carrusel solo aparece por distancia REAL entre mandos.
            // No importa si hay paneles guardados ni si el PointerExtractor reporta cercanía.
            objetivo = Mathf.Max(cercaniaMandos, cercaniaPointer);
        }
        else
        {
            if (mantenerVisibleSiHayPanelesGuardados && hay)
            {
                objetivo = 1f;
            }

            if (Time.unscaledTime <= visibleForzadoHasta)
            {
                objetivo = 1f;
            }

            if (cercaniaMandos > 0f)
            {
                bool permitirMostrar =
                    hay ||
                    mostrarCarruselSiControlesCercaAunqueEsteVacio ||
                    mostrarArcoAunqueNoHayaPanelesGuardados;

                if (permitirMostrar)
                {
                    objetivo = Mathf.Max(objetivo, cercaniaMandos);
                }
            }

            if (cercaniaPointer > 0f)
            {
                bool permitirMostrar =
                    hay ||
                    mostrarCarruselSiControlesCercaAunqueEsteVacio ||
                    mostrarArcoAunqueNoHayaPanelesGuardados;

                if (permitirMostrar)
                {
                    objetivo = Mathf.Max(objetivo, cercaniaPointer);
                }
            }
        }

        if (miniCardAgarrada && mantenerCarruselVisibleMientrasMiniCardAgarrada)
        {
            objetivo = 1f;
        }

        alphaCarruselActual = Mathf.MoveTowards(
            alphaCarruselActual,
            objetivo,
            Time.unscaledDeltaTime * Mathf.Max(0.1f, velocidadFadeCarrusel)
        );

        AplicarAlphaCarrusel(alphaCarruselActual);
    }

    private float ObtenerCercaniaMandosParaMostrarArco()
    {
        if (!mostrarArcoCuandoMandoDerechoSeAcercaAlIzquierdo)
        {
            return 0f;
        }

        Transform derecha = rightHandParaMostrarCarrusel;
        Transform izquierda = usarLeftPocketWorldPointComoRespaldoMandoIzquierdo && leftPocketWorldPoint != null
            ? leftPocketWorldPoint
            : leftHandParaMostrarCarrusel;

        if (derecha == null || izquierda == null)
        {
            return 0f;
        }

        float distancia = Vector3.Distance(derecha.position, izquierda.position);
        return Cercania01(distancia);
    }

    private float ObtenerCercaniaPointerVigente()
    {
        if (ignorarReportesPointerParaMostrarCarrusel)
        {
            return 0f;
        }

        float memoria = Mathf.Max(0.05f, tiempoRecordarCercaniaMandoDerecho);
        if (Time.unscaledTime - tiempoCercaniaReportada > memoria)
        {
            return 0f;
        }

        return Mathf.Clamp01(cercaniaReportada);
    }

    private float Cercania01(float d)
    {
        if (d >= distanciaMostrarCarrusel) return 0f;
        if (d <= distanciaCarruselTotalmenteVisible) return 1f;
        return Mathf.Clamp01(Mathf.InverseLerp(distanciaMostrarCarrusel, distanciaCarruselTotalmenteVisible, d));
    }

    private void AplicarAlphaCarrusel(float a)
    {
        if (miniCardAgarrada && mantenerCarruselVisibleMientrasMiniCardAgarrada)
        {
            a = 1f;
        }

        bool visible = a > 0.01f || (miniCardAgarrada && mantenerCarruselVisibleMientrasMiniCardAgarrada);
        if (pocketVisualRoot != null)
        {
            if (visible && !pocketVisualRoot.activeSelf) { ActivarCadena(pocketVisualRoot.transform); pocketVisualRoot.SetActive(true); }
            if (!visible && ocultarCarruselAlIniciar && panelesGuardados.Count == 0 && Time.unscaledTime > visibleForzadoHasta && PuedeDesactivarPocketVisualRoot()) pocketVisualRoot.SetActive(false);
        }
        if (pocketVisualCanvasGroup != null)
        {
            pocketVisualCanvasGroup.alpha = a;
            pocketVisualCanvasGroup.interactable = visible;
            pocketVisualCanvasGroup.blocksRaycasts = visible;
        }
    }

    private bool PuedeDesactivarPocketVisualRoot()
    {
        if (pocketVisualRoot == null)
        {
            return false;
        }

        if (mantenerPocketVisualRootActivoAlOcultar)
        {
            return false;
        }

        if (miniCardAgarrada && noDesactivarRootMientrasMiniCardAgarrada)
        {
            return false;
        }

        // Defensa: si el visual es el mismo GameObject del Manager, apagarlo detiene este Update
        // y luego el arco no puede volver a aparecer cuando acercas los mandos.
        if (pocketVisualRoot == gameObject)
        {
            return false;
        }

        return true;
    }

    private void SetCarruselAlphaInmediato(float a)
    {
        if (miniCardAgarrada && mantenerCarruselVisibleMientrasMiniCardAgarrada)
        {
            a = 1f;
        }

        alphaCarruselActual = Mathf.Clamp01(a);
        if (pocketVisualRoot != null && alphaCarruselActual > 0.01f) { ActivarCadena(pocketVisualRoot.transform); pocketVisualRoot.SetActive(true); }
        if (pocketVisualCanvasGroup != null)
        {
            pocketVisualCanvasGroup.alpha = alphaCarruselActual;
            pocketVisualCanvasGroup.interactable = alphaCarruselActual > 0.01f;
            pocketVisualCanvasGroup.blocksRaycasts = alphaCarruselActual > 0.01f;
        }
        if (alphaCarruselActual <= 0.01f && pocketVisualRoot != null && ocultarCarruselAlIniciar && panelesGuardados.Count == 0 && PuedeDesactivarPocketVisualRoot()) pocketVisualRoot.SetActive(false);
        ActualizarEstadoBotonesPanelOpciones();
    }

    private void ForzarMostrarCarruselTemporal(float segundos)
    {
        if (!ArcoDisponibleParaInteraccion)
        {
            return;
        }

        if (mostrarCarruselSoloSiMandosMuyCerca &&
            Mathf.Max(ObtenerCercaniaMandosParaMostrarArco(), ObtenerCercaniaPointerVigente()) <= 0f)
        {
            return;
        }

        visibleForzadoHasta = Time.unscaledTime + Mathf.Max(0.25f, segundos);
        if (pocketVisualRoot != null) { ActivarCadena(pocketVisualRoot.transform); pocketVisualRoot.SetActive(true); }
        if (miniCardsParent != null) { ActivarCadena(miniCardsParent); miniCardsParent.gameObject.SetActive(true); }
        SetCarruselAlphaInmediato(1f);
    }

    private void ActivarCadena(Transform t)
    {
        if (t == null) return;
        if (t.parent != null) ActivarCadena(t.parent);
        if (!t.gameObject.activeSelf) t.gameObject.SetActive(true);
    }

    private void LeerStickIzquierdo()
    {
        if (panelesGuardados.Count <= 1) return;
        if (bloquearInputMientrasAnima && animando) return;
        if (Time.unscaledTime - ultimoGiro < cooldownGiro) return;

        float x = 0f;
        try { x = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.LTouch).x; } catch { }
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.LeftArrow)) x = -1f;
        else if (Input.GetKeyDown(KeyCode.RightArrow)) x = 1f;
#endif
        if (invertirGiroStick) x *= -1f;
        if (x >= umbralStick) { GirarCarrusel(1); ultimoGiro = Time.unscaledTime; }
        else if (x <= -umbralStick) { GirarCarrusel(-1); ultimoGiro = Time.unscaledTime; }
    }

    public bool RegistrarPanelEnOpciones(AlgoLabPocketPanelItem panel, bool ocultarPanelReal = true, bool mostrarTemporal = false)
    {
        if (panel == null)
        {
            return false;
        }

        if (soloRegistrarPanelesGuardablesDesactivados && (!panel.puedeGuardarse || panel.esPanelPrincipal))
        {
            return false;
        }

        AutoBuscarReferencias();
        EliminarEntradasInvalidasODuplicadas();

        if (miniCardPrefab == null || miniCardsParent == null)
        {
            Debug.LogError("POCKET: faltan referencias para registrar panel en opciones.");
            return false;
        }

        AlgoLabPocketPanelItem panelRegistrado = ObtenerPanelRegistradoConMismoRoot(panel);
        if (panelRegistrado == null)
        {
            panelesGuardados.Add(panel);
            indiceSeleccionado = panelesGuardados.Count - 1;
        }
        else
        {
            panel = panelRegistrado;
            indiceSeleccionado = Mathf.Clamp(panelesGuardados.IndexOf(panel), 0, Mathf.Max(0, panelesGuardados.Count - 1));
        }

        if (ocultarPanelReal)
        {
            panel.ForzarEstadoDentroDelArco(true);
        }

        CrearCardsVisuales();
        ActualizarContenidoReposo();
        AplicarLayoutReposoInmediato();

        if (mostrarTemporal && ArcoDisponibleParaInteraccion)
        {
            ForzarMostrarCarruselTemporal(segundosVisibleTrasGuardar);
        }

        return true;
    }

    private AlgoLabPocketPanelItem ObtenerPanelRegistradoConMismoRoot(AlgoLabPocketPanelItem panel)
    {
        if (panel == null)
        {
            return null;
        }

        Transform root = panel.ObtenerPanelRoot();
        for (int i = 0; i < panelesGuardados.Count; i++)
        {
            AlgoLabPocketPanelItem candidato = panelesGuardados[i];
            if (candidato != null &&
                !candidato.esAccionConfiguracion &&
                candidato.ObtenerPanelRoot() == root)
            {
                return candidato;
            }
        }

        return null;
    }

    public void RegistrarPanelesDesactivadosEnPanelOpciones()
    {
        if (interfazModalActiva || !autoRegistrarPanelesDesactivadosEnPanelOpciones)
        {
            return;
        }

        AlgoLabPocketPanelItem[] paneles = FindObjectsByType<AlgoLabPocketPanelItem>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        for (int i = 0; i < paneles.Length; i++)
        {
            AlgoLabPocketPanelItem panel = paneles[i];

            if (panel == null ||
                panelesGuardados.Contains(panel) ||
                AutoRegistroBloqueadoParaPanel(panel))
            {
                continue;
            }

            if (soloRegistrarPanelesGuardablesDesactivados && (!panel.puedeGuardarse || panel.esPanelPrincipal))
            {
                continue;
            }

            bool guardadoPorPocket = panel.EstaDesactivadoPorPocket();
            bool desactivadoExterno = registrarPanelesDesactivadosExternamente && panel.EstaDesactivadoExternamente();

            if (guardadoPorPocket || desactivadoExterno)
            {
                RegistrarPanelEnOpciones(panel, true, false);
            }
        }
    }

    public void RegistrarAccionConfiguracion(Action accion)
    {
        accionConfiguracion = accion ?? AbrirConfiguracionAutonoma;
        accionConfiguracionVisible = true;
        AsegurarItemAccionConfiguracion();
        ActualizarVisibilidadAccionConfiguracion();
    }

    public void DesregistrarAccionConfiguracion(Action accion)
    {
        if (accionConfiguracion == accion)
        {
            accionConfiguracion = AbrirConfiguracionAutonoma;
        }

        SetAccionConfiguracionVisible(true);
    }

    private void AbrirConfiguracionAutonoma()
    {
        AlgoLabSettingsMenuController configuracion = AlgoLabSettingsMenuController.Instance;
        if (configuracion == null)
        {
            configuracion = FindFirstObjectByType<AlgoLabSettingsMenuController>(
                FindObjectsInactive.Include
            );
        }

        if (configuracion == null)
        {
            GameObject root = new GameObject("[ALGOLAB_SETTINGS_MENU]");
            configuracion = root.AddComponent<AlgoLabSettingsMenuController>();
        }

        if (configuracion != null)
        {
            configuracion.AbrirConfiguracion();
        }
    }

    public void SetAccionConfiguracionVisible(bool visible)
    {
        accionConfiguracionVisible = visible;
        AsegurarItemAccionConfiguracion();
        ActualizarVisibilidadAccionConfiguracion();
    }

    private void AsegurarItemAccionConfiguracion()
    {
        if (accionConfiguracion == null)
        {
            accionConfiguracion = AbrirConfiguracionAutonoma;
        }

        if (itemAccionConfiguracion == null)
        {
            GameObject root = new GameObject("[ACCION_CONFIGURACION]");
            root.transform.SetParent(transform, false);
            itemAccionConfiguracion = root.AddComponent<AlgoLabPocketPanelItem>();
        }

        itemAccionConfiguracion.nombreCorto = "CONFIG.";
        itemAccionConfiguracion.iconoMini = CargarIconoAccionConfiguracion();
        itemAccionConfiguracion.panelRoot = itemAccionConfiguracion.transform;
        itemAccionConfiguracion.pocketManager = this;
        itemAccionConfiguracion.puedeGuardarse = false;
        itemAccionConfiguracion.esPanelPrincipal = true;
        itemAccionConfiguracion.esAccionConfiguracion = true;
        itemAccionConfiguracion.autoGuardarAlEstarCercaDelBolsillo = false;
        itemAccionConfiguracion.desactivarRootMientrasEstaEnArco = false;
        itemAccionConfiguracion.desactivarComponentesMientrasEstaEnArco = false;
        itemAccionConfiguracion.reactivarRootAlSalirDelArco = false;
        itemAccionConfiguracion.avisarTutorialAlRestaurar = false;
        itemAccionConfiguracion.mostrarDebug = false;
        itemAccionConfiguracion.ForzarEstadoDentroDelArco(true);

        if (!itemAccionConfiguracion.gameObject.activeSelf)
        {
            itemAccionConfiguracion.gameObject.SetActive(true);
        }
    }

    private Sprite CargarIconoAccionConfiguracion()
    {
        if (iconoAccionConfiguracion != null)
        {
            return iconoAccionConfiguracion;
        }

        iconoAccionConfiguracion = Resources.Load<Sprite>(
            "AlgoLab/engranaje-configuracion"
        );

        if (iconoAccionConfiguracion != null)
        {
            return iconoAccionConfiguracion;
        }

        Texture2D textura = Resources.Load<Texture2D>(
            "AlgoLab/engranaje-configuracion"
        );

        if (textura == null)
        {
            return null;
        }

        iconoAccionConfiguracion = Sprite.Create(
            textura,
            new Rect(0f, 0f, textura.width, textura.height),
            new Vector2(0.5f, 0.5f),
            100f,
            0u,
            SpriteMeshType.FullRect
        );
        iconoAccionConfiguracion.name = "EngranajeConfiguracionRuntime";
        return iconoAccionConfiguracion;
    }

    private void ActualizarVisibilidadAccionConfiguracion()
    {
        if (itemAccionConfiguracion == null)
        {
            return;
        }

        bool debeEstar = accionConfiguracionVisible && accionConfiguracion != null;
        bool esta = panelesGuardados.Contains(itemAccionConfiguracion);
        int entradasConfiguracion = 0;
        for (int i = 0; i < panelesGuardados.Count; i++)
        {
            if (panelesGuardados[i] != null && panelesGuardados[i].esAccionConfiguracion)
            {
                entradasConfiguracion++;
            }
        }

        bool estadoYaCorrecto = debeEstar
            ? esta && panelesGuardados.IndexOf(itemAccionConfiguracion) == 0 && entradasConfiguracion == 1
            : entradasConfiguracion == 0;

        if (estadoYaCorrecto)
        {
            if (debeEstar)
            {
                itemAccionConfiguracion.ForzarEstadoDentroDelArco(true);
            }

            return;
        }

        AlgoLabPocketPanelItem seleccionadoAntes = ObtenerPanelSeleccionadoSeguro();

        if (debeEstar)
        {
            itemAccionConfiguracion.ForzarEstadoDentroDelArco(true);
            panelesGuardados.RemoveAll(panel => panel != null && panel.esAccionConfiguracion);
            panelesGuardados.Insert(0, itemAccionConfiguracion);

            if (!esta || seleccionadoAntes == null || seleccionadoAntes == itemAccionConfiguracion)
            {
                indiceSeleccionado = 0;
            }
            else
            {
                indiceSeleccionado = Mathf.Max(0, panelesGuardados.IndexOf(seleccionadoAntes));
            }
        }
        else if (entradasConfiguracion > 0)
        {
            panelesGuardados.RemoveAll(panel => panel != null && panel.esAccionConfiguracion);
            indiceSeleccionado = seleccionadoAntes != null && seleccionadoAntes != itemAccionConfiguracion
                ? Mathf.Max(0, panelesGuardados.IndexOf(seleccionadoAntes))
                : Mathf.Clamp(indiceSeleccionado, 0, Mathf.Max(0, panelesGuardados.Count - 1));
        }

        if (isActiveAndEnabled)
        {
            ActualizarContenidoReposo();
            AplicarLayoutReposoInmediato();
        }
    }

    private void AsegurarPresenciaAccionConfiguracion()
    {
        AsegurarItemAccionConfiguracion();

        bool esta = panelesGuardados.Contains(itemAccionConfiguracion);
        bool estaPrimera = esta && panelesGuardados.IndexOf(itemAccionConfiguracion) == 0;
        if (esta != accionConfiguracionVisible || (accionConfiguracionVisible && !estaPrimera))
        {
            ActualizarVisibilidadAccionConfiguracion();
            return;
        }

        itemAccionConfiguracion.ForzarEstadoDentroDelArco(true);
    }

    private AlgoLabPocketPanelItem ObtenerPanelSeleccionadoSeguro()
    {
        if (panelesGuardados.Count == 0)
        {
            return null;
        }

        indiceSeleccionado = Mathf.Clamp(indiceSeleccionado, 0, panelesGuardados.Count - 1);
        return panelesGuardados[indiceSeleccionado];
    }

    private void RefrescarPanelesDesactivadosSiCorresponde()
    {
        if (!autoRegistrarPanelesDesactivadosEnPanelOpciones || intervaloRegistrarPanelesDesactivados <= 0f)
        {
            return;
        }

        if (Time.unscaledTime < proximaRevisionPanelesDesactivados)
        {
            return;
        }

        proximaRevisionPanelesDesactivados = Time.unscaledTime + Mathf.Max(0.1f, intervaloRegistrarPanelesDesactivados);
        RegistrarPanelesDesactivadosEnPanelOpciones();
    }

    public bool HayPanelesGuardados() { return panelesGuardados.Count > 0; }
    public int CantidadPanelesGuardados() { return panelesGuardados.Count; }
    public bool EstaGuardandoOAnimando() { return animando; }
    public bool EstaPanelRegistradoComoGuardado(AlgoLabPocketPanelItem panel)
    {
        return panel != null && panelesGuardados.Contains(panel);
    }

    public bool NotificarPanelActivadoExternamente(AlgoLabPocketPanelItem panel)
    {
        if (panel == null || !panelesGuardados.Contains(panel))
        {
            return false;
        }

        if (panel.esAccionConfiguracion || panel.EstaDesactivadoPorPocket())
        {
            return false;
        }

        if (operacionActiva == TipoOperacion.Restaurar && panelOperacionActiva == panel)
        {
            return false;
        }

        panelesGuardados.Remove(panel);
        BloquearAutoRegistroTemporal(panel, tiempoIgnorarAutoRegistroTrasRestaurar);
        indiceSeleccionado = Mathf.Clamp(
            indiceSeleccionado,
            0,
            Mathf.Max(0, panelesGuardados.Count - 1)
        );

        if (isActiveAndEnabled)
        {
            ActualizarContenidoReposo();
            AplicarLayoutReposoInmediato();
        }

        return true;
    }

    public bool HayPanelRealAgarrado()
    {
        LimpiarPanelesAgarradosNulos();
        LimpiarPanelesAgarradosVencidos();
        return panelesRealesAgarrados.Count > 0;
    }

    public bool EstaMiniCardAgarrada()
    {
        return miniCardAgarrada;
    }

    public void NotificarPanelRealAgarrado(AlgoLabPocketPanelItem panel)
    {
        if (panel == null)
        {
            return;
        }

        bool nuevo = panelesRealesAgarrados.Add(panel);
        ultimaActualizacionPanelRealAgarrado[panel] = Time.unscaledTime;

        if (mostrarDebug && nuevo)
        {
            Debug.Log("POCKET MANAGER: panel real agarrado -> " + panel.nombreCorto + " | total=" + panelesRealesAgarrados.Count);
        }
    }

    public void ActualizarPanelRealAgarrado(AlgoLabPocketPanelItem panel)
    {
        if (panel == null)
        {
            return;
        }

        panelesRealesAgarrados.Add(panel);
        ultimaActualizacionPanelRealAgarrado[panel] = Time.unscaledTime;
    }

    public void NotificarPanelRealSoltado(AlgoLabPocketPanelItem panel)
    {
        if (panel == null)
        {
            return;
        }

        if (panelesRealesAgarrados.Contains(panel))
        {
            panelesRealesAgarrados.Remove(panel);
        }

        if (ultimaActualizacionPanelRealAgarrado.ContainsKey(panel))
        {
            ultimaActualizacionPanelRealAgarrado.Remove(panel);
        }

        if (mostrarDebug)
        {
            Debug.Log("POCKET MANAGER: panel real soltado -> " + panel.nombreCorto + " | total=" + panelesRealesAgarrados.Count);
        }
    }

    private void LimpiarPanelesAgarradosNulos()
    {
        panelesRealesAgarrados.RemoveWhere(p => p == null);

        List<AlgoLabPocketPanelItem> nulos = new List<AlgoLabPocketPanelItem>();

        foreach (var kv in ultimaActualizacionPanelRealAgarrado)
        {
            if (kv.Key == null)
            {
                nulos.Add(kv.Key);
            }
        }

        for (int i = 0; i < nulos.Count; i++)
        {
            ultimaActualizacionPanelRealAgarrado.Remove(nulos[i]);
        }
    }

    private void LimpiarPanelesAgarradosVencidos()
    {
        if (!limpiarBloqueoPanelAgarradoPegado)
        {
            return;
        }

        List<AlgoLabPocketPanelItem> vencidos = new List<AlgoLabPocketPanelItem>();

        foreach (AlgoLabPocketPanelItem panel in panelesRealesAgarrados)
        {
            if (panel == null)
            {
                vencidos.Add(panel);
                continue;
            }

            float ultima;
            if (!ultimaActualizacionPanelRealAgarrado.TryGetValue(panel, out ultima))
            {
                vencidos.Add(panel);
                continue;
            }

            if (Time.unscaledTime - ultima > tiempoMaximoPanelAgarradoSinActualizar)
            {
                vencidos.Add(panel);
            }
        }

        for (int i = 0; i < vencidos.Count; i++)
        {
            AlgoLabPocketPanelItem p = vencidos[i];
            panelesRealesAgarrados.Remove(p);
            ultimaActualizacionPanelRealAgarrado.Remove(p);

            if (mostrarDebug && p != null)
            {
                Debug.Log("POCKET MANAGER: bloqueo viejo eliminado -> " + p.nombreCorto);
            }
        }
    }

    [ContextMenu("Liberar bloqueo mini cards")]
    public void LiberarBloqueoMiniCards()
    {
        animando = false;
        miniCardAgarrada = false;
        tiempoBloqueoCardsHasta = -999f;
        panelesRealesAgarrados.Clear();
        ultimaActualizacionPanelRealAgarrado.Clear();
        panelesGuardablesCercaDelArco.Clear();

        if (mostrarDebug)
        {
            Debug.Log("POCKET MANAGER: bloqueos de mini cards liberados manualmente.");
        }
    }

    private void LiberarBloqueosDespuesDeSacarCard(AlgoLabPocketPanelItem panelRestaurado)
    {
        miniCardAgarrada = false;

        if (panelRestaurado != null)
        {
            panelesRealesAgarrados.Remove(panelRestaurado);
            ultimaActualizacionPanelRealAgarrado.Remove(panelRestaurado);
            panelesGuardablesCercaDelArco.Remove(panelRestaurado);
        }

        // No tocamos tiempoBloqueoCardsHasta si viene de guardar un panel.
        // Solo limpiamos bloqueos propios de agarrar/sacar una mini card.
        LimpiarPanelesAgarradosVencidos();
        LimpiarPanelesCercaDelArcoVencidos();

        if (mostrarDebug)
        {
            Debug.Log("POCKET MANAGER: bloqueos liberados después de sacar card. Quedan guardados=" + panelesGuardados.Count);
        }
    }


    public void NotificarMiniCardAgarrada(bool agarrada)
    {
        miniCardAgarrada = agarrada;

        if (miniCardAgarrada && mantenerCarruselVisibleMientrasMiniCardAgarrada)
        {
            // Lo hacemos inmediato para que no alcance a parpadear/desaparecer si el usuario
            // saca la mini card rápido y la aleja del arco.
            SetCarruselAlphaInmediato(1f);
        }

        if (mostrarDebug)
        {
            Debug.Log("POCKET MANAGER: mini card agarrada = " + miniCardAgarrada);
        }
    }

    public void ActualizarPanelGuardableCercaDelArco(AlgoLabPocketPanelItem panel)
    {
        if (panel == null)
        {
            return;
        }

        if (!ArcoDisponibleParaInteraccion)
        {
            LimpiarPanelGuardableCercaDelArco(panel);
            return;
        }

        panelesGuardablesCercaDelArco[panel] = Time.unscaledTime;
    }

    public void LimpiarPanelGuardableCercaDelArco(AlgoLabPocketPanelItem panel)
    {
        if (panel == null)
        {
            return;
        }

        if (panelesGuardablesCercaDelArco.ContainsKey(panel))
        {
            panelesGuardablesCercaDelArco.Remove(panel);
        }
    }

    private void LimpiarPanelesCercaDelArcoVencidos()
    {
        List<AlgoLabPocketPanelItem> vencidos = new List<AlgoLabPocketPanelItem>();

        foreach (var kv in panelesGuardablesCercaDelArco)
        {
            AlgoLabPocketPanelItem panel = kv.Key;

            if (panel == null || panel.EstaGuardado() || Time.unscaledTime - kv.Value > tiempoRecordarPanelCercaDelArco)
            {
                vencidos.Add(panel);
            }
        }

        for (int i = 0; i < vencidos.Count; i++)
        {
            panelesGuardablesCercaDelArco.Remove(vencidos[i]);
        }
    }

    public bool HayPanelGuardableCercaDelArco()
    {
        if (!ArcoDisponibleParaInteraccion)
        {
            panelesGuardablesCercaDelArco.Clear();
            return false;
        }

        LimpiarPanelesCercaDelArcoVencidos();
        return panelesGuardablesCercaDelArco.Count > 0;
    }

    public void ActivarCooldownAgarrarCards(float segundos)
    {
        if (segundos <= 0f)
        {
            return;
        }

        tiempoBloqueoCardsHasta = Mathf.Max(tiempoBloqueoCardsHasta, Time.unscaledTime + segundos);

        if (mostrarDebug)
        {
            Debug.Log("POCKET MANAGER: cooldown mini cards activado por " + segundos.ToString("F2") + "s");
        }
    }

    public float SegundosRestantesBloqueoCards()
    {
        return Mathf.Max(0f, tiempoBloqueoCardsHasta - Time.unscaledTime);
    }

    public string MotivoBloqueoAgarrarCards()
    {
        if (!JuegoPermiteMostrarArco())
        {
            return "todavía no se entró al juego";
        }

        if (!EventoTutorialPermiteUsarArco())
        {
            return "el tutorial todavía no habilitó el arco";
        }

        if (animando)
        {
            VerificarOperacionAtascada();
            if (animando) return "el bolsillo está animando/guardando";
        }

        if (SegundosRestantesBloqueoCards() > 0f)
        {
            return "cooldown activo " + SegundosRestantesBloqueoCards().ToString("F2") + "s";
        }

        if (bloquearAgarreCardsMientrasPanelRealAgarrado && HayPanelRealAgarrado())
        {
            return "hay un panel real agarrado";
        }

        if (bloquearCardsSiPanelGuardableCercaDelArco && HayPanelGuardableCercaDelArco())
        {
            return "hay un panel cerca del arco pendiente de guardar";
        }

        return "";
    }

    public bool PuedeAgarrarCards()
    {
        if (!ArcoDisponibleParaInteraccion)
        {
            return false;
        }

        if (animando)
        {
            VerificarOperacionAtascada();
            if (animando) return false;
        }

        if (Time.unscaledTime < tiempoBloqueoCardsHasta)
        {
            return false;
        }

        if (bloquearAgarreCardsMientrasPanelRealAgarrado && HayPanelRealAgarrado())
        {
            return false;
        }

        if (bloquearCardsSiPanelGuardableCercaDelArco && HayPanelGuardableCercaDelArco())
        {
            return false;
        }

        return true;
    }


    private void SincronizarPanelesGuardadosDesactivados()
    {
        if (interfazModalActiva ||
            !forzarPanelesGuardadosDesactivadosEnArco ||
            !sincronizarPanelesGuardadosDesactivadosCadaFrame)
        {
            return;
        }

        bool listaCambio = EliminarEntradasInvalidasODuplicadas();

        for (int i = panelesGuardados.Count - 1; i >= 0; i--)
        {
            AlgoLabPocketPanelItem panel = panelesGuardados[i];

            if (panel == null)
            {
                panelesGuardados.RemoveAt(i);
                listaCambio = true;
                continue;
            }

            if (EsAccionConfiguracion(panel))
            {
                panel.ForzarEstadoDentroDelArco(true);
                continue;
            }

            // La lista del manager es la autoridad. Un SetActive temporal de otro
            // sistema no debe borrar la card ni dejar el panel imposible de sacar.
            panel.ForzarEstadoDentroDelArco(true);
        }

        if (listaCambio)
        {
            indiceSeleccionado = Mathf.Clamp(indiceSeleccionado, 0, Mathf.Max(0, panelesGuardados.Count - 1));
            CrearCardsVisuales();
            ActualizarContenidoReposo();
            AplicarLayoutReposoInmediato();
        }
    }

    private bool EliminarEntradasInvalidasODuplicadas()
    {
        bool cambio = false;
        bool accionConfiguracionEncontrada = false;
        HashSet<Transform> rootsEncontrados = new HashSet<Transform>();

        for (int i = 0; i < panelesGuardados.Count;)
        {
            AlgoLabPocketPanelItem panel = panelesGuardados[i];
            if (panel == null)
            {
                panelesGuardados.RemoveAt(i);
                cambio = true;
                continue;
            }

            if (panel.esAccionConfiguracion)
            {
                bool esAccionValida = panel == itemAccionConfiguracion &&
                                      accionConfiguracionVisible &&
                                      !accionConfiguracionEncontrada;
                if (!esAccionValida)
                {
                    panelesGuardados.RemoveAt(i);
                    cambio = true;
                    continue;
                }

                accionConfiguracionEncontrada = true;
                i++;
                continue;
            }

            Transform root = panel.ObtenerPanelRoot();
            if (root == null || !rootsEncontrados.Add(root))
            {
                panelesGuardados.RemoveAt(i);
                cambio = true;
                continue;
            }

            i++;
        }

        if (accionConfiguracionEncontrada)
        {
            int indiceAccion = panelesGuardados.IndexOf(itemAccionConfiguracion);
            if (indiceAccion > 0)
            {
                AlgoLabPocketPanelItem seleccionadoAntes = ObtenerPanelSeleccionadoSeguro();
                panelesGuardados.RemoveAt(indiceAccion);
                panelesGuardados.Insert(0, itemAccionConfiguracion);
                indiceSeleccionado = seleccionadoAntes != null
                    ? Mathf.Max(0, panelesGuardados.IndexOf(seleccionadoAntes))
                    : 0;
                cambio = true;
            }
        }

        return cambio;
    }

    private void ForzarPanelGuardadoDesactivado(AlgoLabPocketPanelItem panel)
    {
        if (!forzarPanelesGuardadosDesactivadosEnArco || panel == null)
        {
            return;
        }

        panel.ForzarEstadoDentroDelArco(true);
    }

    private void ActivarPanelRestauradoFueraDelArco(AlgoLabPocketPanelItem panel)
    {
        if (!activarPanelRealAlSalirDelArco || panel == null)
        {
            return;
        }

        panel.ForzarEstadoDentroDelArco(false);
    }

    public bool IntentarGuardarPanel(AlgoLabPocketPanelItem panel)
    {
        return IniciarGuardadoValidado(panel, false);
    }

    public void GuardarPanel(AlgoLabPocketPanelItem panel)
    {
        IniciarGuardadoValidado(panel, false);
    }

    public bool GuardarPanelTrasSoltarValidado(AlgoLabPocketPanelItem panel)
    {
        return IniciarGuardadoValidado(panel, true);
    }

    private bool IniciarGuardadoValidado(AlgoLabPocketPanelItem panel, bool agarreYaValidadoAlSoltar)
    {
        if (panel == null || animando || rutina != null)
        {
            return false;
        }

        EliminarEntradasInvalidasODuplicadas();

        if (!ArcoDisponibleParaInteraccion || !panel.puedeGuardarse || panel.esPanelPrincipal)
        {
            LimpiarPanelGuardableCercaDelArco(panel);
            return false;
        }

        if (soloGuardarSiPanelEstaAgarrado && !agarreYaValidadoAlSoltar && !panel.EstaAgarrado())
        {
            return false;
        }

        if (leftPocketWorldPoint == null ||
            Vector3.Distance(panel.ObtenerPosicionMundo(), leftPocketWorldPoint.position) >
            Mathf.Max(0.01f, distanciaGuardarPanel))
        {
            return false;
        }

        AlgoLabPocketPanelItem panelRegistrado = ObtenerPanelRegistradoConMismoRoot(panel);
        if (panelRegistrado != null)
        {
            panelRegistrado.ForzarEstadoDentroDelArco(true);
            ForzarMostrarCarruselTemporal(segundosVisibleTrasGuardar);
            return false;
        }

        if (!SlotsListos() || miniCardPrefab == null || miniCardsParent == null)
        {
            Debug.LogError("POCKET: faltan referencias del manager o slots.");
            return false;
        }

        LimpiarPanelGuardableCercaDelArco(panel);
        operacionActiva = TipoOperacion.Guardar;
        panelOperacionActiva = panel;
        rutina = StartCoroutine(GuardarPanelAnimado(panel));
        return true;
    }

    private IEnumerator GuardarPanelAnimado(AlgoLabPocketPanelItem panel)
    {
        animando = true; tiempoInicioAnimando = Time.unscaledTime;
        ForzarMostrarCarruselTemporal(segundosVisibleTrasGuardar);

        if (animarPanelAlGuardar) yield return panel.AnimarEncogerHacia(leftPocketWorldPoint, duracionEncogerPanelAlGuardar, escalaFinalPanelGuardado);

        if (panel == null)
        {
            FinalizarOperacionActiva();
            yield break;
        }

        if (!ArcoDisponibleParaInteraccion)
        {
            if (panel != null)
            {
                panel.CancelarGuardadoNoConfirmado();
            }

            FinalizarOperacionActiva();
            LimpiarPanelGuardableCercaDelArco(panel);
            yield break;
        }

        panel.GuardarEnPocket(ocultarPanelRealAlGuardar || forzarPanelesGuardadosDesactivadosEnArco);
        ForzarPanelGuardadoDesactivado(panel);
        NotificarTutorialPanelGuardado(panel);

        int indiceAnterior = indiceSeleccionado;
        bool habia = panelesGuardados.Count > 0;
        AlgoLabPocketPanelItem panelRegistrado = ObtenerPanelRegistradoConMismoRoot(panel);
        bool yaEstabaRegistrado = panelRegistrado != null;
        if (!yaEstabaRegistrado)
        {
            panelesGuardados.Add(panel);
        }
        else
        {
            panel = panelRegistrado;
            panel.ForzarEstadoDentroDelArco(true);
        }
        indiceSeleccionado = Mathf.Clamp(panelesGuardados.IndexOf(panel), 0, Mathf.Max(0, panelesGuardados.Count - 1));

        CrearCardsVisuales();

        if (!yaEstabaRegistrado && habia && apartarCardCentralAlGuardar) yield return AnimarEntradaNuevaCard(indiceAnterior, indiceSeleccionado);
        else
        {
            ActualizarContenidoReposo();
            AplicarLayoutReposoInmediato();
            yield return AnimarCardCentroDesdePequena(1);
        }

        ActualizarContenidoReposo();
        AplicarLayoutReposoInmediato();

        // Después de guardar un panel, esperamos 1 segundo antes de permitir sacar cards.
        ActivarCooldownAgarrarCards(cooldownAgarrarCardDespuesDeGuardarPanel);

        FinalizarOperacionActiva();
    }

    private IEnumerator AnimarEntradaNuevaCard(int indiceAnterior, int indiceNuevo)
    {
        // La card del centro anterior se aparta a la derecha.
        slotCards[1].Configurar(panelesGuardados[indiceAnterior], false);
        slotCards[1].gameObject.SetActive(true);
        AplicarPoseCard(slotCards[1], 1, CENTRO, alphaCentro);

        slotCards[3].Configurar(panelesGuardados[indiceNuevo], true);
        slotCards[3].gameObject.SetActive(true);
        AplicarPoseCard(slotCards[3], 3, CENTRO, 0f);
        slotCards[3].Rect.localScale = EscalaBaseCard(3) * escalaInicialCardGuardada;

        float tiempo = 0f;
        float dur = Mathf.Max(0.01f, duracionAparecerCardGuardada);
        while (tiempo < dur)
        {
            tiempo += Time.unscaledDeltaTime;
            float s = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(tiempo / dur));
            AplicarPoseCardInterpolada(slotCards[1], 1, CENTRO, VIS_DER, alphaCentro, alphaLateral, s);
            AplicarPoseCard(slotCards[3], 3, CENTRO, Mathf.Lerp(0f, alphaCentro, s));
            slotCards[3].Rect.localScale = EscalaBaseCard(3) * Rebote(s, escalaInicialCardGuardada, reboteEscalaCardGuardada, 1f);
            yield return null;
        }
    }

    private IEnumerator AnimarCardCentroDesdePequena(int slotIndex)
    {
        AlgoLabPocketMiniCardView card = slotCards[slotIndex];
        float tiempo = 0f;
        float dur = Mathf.Max(0.01f, duracionAparecerCardGuardada);
        while (tiempo < dur)
        {
            tiempo += Time.unscaledDeltaTime;
            float s = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(tiempo / dur));
            AplicarPoseCard(card, slotIndex, CENTRO, Mathf.Lerp(0f, alphaCentro, s));
            card.Rect.localScale = EscalaBaseCard(slotIndex) * Rebote(s, escalaInicialCardGuardada, reboteEscalaCardGuardada, 1f);
            yield return null;
        }
    }

    private float Rebote(float t, float inicio, float pico, float final)
    {
        if (t < 0.7f) return Mathf.Lerp(inicio, pico, Mathf.SmoothStep(0f, 1f, t / 0.7f));
        return Mathf.Lerp(pico, final, Mathf.SmoothStep(0f, 1f, (t - 0.7f) / 0.3f));
    }

    public void RestaurarPanelSeleccionado()
    {
        if (panelesGuardados.Count == 0) return;
        RestaurarPanel(panelesGuardados[Mathf.Clamp(indiceSeleccionado, 0, panelesGuardados.Count - 1)]);
    }

    public void RestaurarPanel(AlgoLabPocketPanelItem panel)
    {
        RestaurarPanelDesdePuntero(panel, null, null);
    }

    public void RestaurarPanelDesdePuntero(AlgoLabPocketPanelItem panel, Transform puntoPuntero, AlgoLabPocketMiniCardView cardOrigen = null)
    {
        IntentarRestaurarPanelDesdePuntero(panel, puntoPuntero, cardOrigen);
    }

    public bool IntentarRestaurarPanelDesdePuntero(
        AlgoLabPocketPanelItem panel,
        Transform puntoPuntero,
        AlgoLabPocketMiniCardView cardOrigen = null)
    {
        if (EsAccionConfiguracion(panel))
        {
            return IntentarEjecutarAccionConfiguracion(cardOrigen);
        }

        if (!PuedeIniciarRestauracion(panel, out int index)) return false;

        operacionActiva = TipoOperacion.Restaurar;
        panelOperacionActiva = panel;
        rutina = StartCoroutine(RestaurarAnimado(panel, index, puntoPuntero, cardOrigen));
        return true;
    }

    public void RestaurarDesdeMiniCard(AlgoLabPocketMiniCardView card, Transform puntoPuntero = null)
    {
        if (card == null) return;
        RestaurarPanelDesdePuntero(card.Panel, puntoPuntero, card);
    }

    // NUEVO: este método recibe la pose congelada en el instante exacto de soltar la mini card.
    // Así el panel no aparece bajo si el usuario baja el mando durante la animación de sacar la card.
    public void RestaurarPanelDesdePoseCongelada(
        AlgoLabPocketPanelItem panel,
        Vector3 posicionMundoSuelta,
        Quaternion rotacionMundoSuelta,
        AlgoLabPocketMiniCardView cardOrigen = null)
    {
        IntentarRestaurarPanelDesdePoseCongelada(
            panel,
            posicionMundoSuelta,
            rotacionMundoSuelta,
            cardOrigen
        );
    }

    public bool IntentarRestaurarPanelDesdePoseCongelada(
        AlgoLabPocketPanelItem panel,
        Vector3 posicionMundoSuelta,
        Quaternion rotacionMundoSuelta,
        AlgoLabPocketMiniCardView cardOrigen = null)
    {
        if (EsAccionConfiguracion(panel))
        {
            return IntentarEjecutarAccionConfiguracion(cardOrigen);
        }

        if (!PuedeIniciarRestauracion(panel, out int index)) return false;

        operacionActiva = TipoOperacion.Restaurar;
        panelOperacionActiva = panel;
        rutina = StartCoroutine(
            RestaurarAnimadoDesdePoseCongelada(
                panel,
                index,
                posicionMundoSuelta,
                rotacionMundoSuelta,
                cardOrigen
            )
        );
        return true;
    }

    private bool EsAccionConfiguracion(AlgoLabPocketPanelItem panel)
    {
        return panel != null &&
               panel == itemAccionConfiguracion &&
               panel.esAccionConfiguracion;
    }

    public bool IntentarActivarAccionConfiguracionDesdeCard(AlgoLabPocketMiniCardView card)
    {
        if (card == null || !EsAccionConfiguracion(card.Panel))
        {
            return false;
        }

        return IntentarEjecutarAccionConfiguracion(card);
    }

    private bool IntentarEjecutarAccionConfiguracion(AlgoLabPocketMiniCardView cardOrigen)
    {
        if (!ArcoDisponibleParaInteraccion || animando || accionConfiguracion == null)
        {
            return false;
        }

        operacionActiva = TipoOperacion.Restaurar;
        panelOperacionActiva = itemAccionConfiguracion;
        rutina = StartCoroutine(EjecutarAccionConfiguracionSiguienteFrame(cardOrigen));
        return true;
    }

    private IEnumerator EjecutarAccionConfiguracionSiguienteFrame(AlgoLabPocketMiniCardView cardOrigen)
    {
        animando = true;
        tiempoInicioAnimando = Time.unscaledTime;
        yield return null;

        if (cardOrigen != null && miniCardsParent != null)
        {
            cardOrigen.transform.SetParent(miniCardsParent, true);
        }

        miniCardAgarrada = false;
        FinalizarOperacionActiva();
        ActualizarContenidoReposo();
        AplicarLayoutReposoInmediato();

        Action accion = accionConfiguracion;
        accion?.Invoke();
    }

    public void RestaurarDesdeMiniCardEnPoseCongelada(
        AlgoLabPocketMiniCardView card,
        Vector3 posicionMundoSuelta,
        Quaternion rotacionMundoSuelta)
    {
        if (card == null) return;
        RestaurarPanelDesdePoseCongelada(card.Panel, posicionMundoSuelta, rotacionMundoSuelta, card);
    }

    private bool PuedeIniciarRestauracion(AlgoLabPocketPanelItem panel, out int index)
    {
        index = -1;

        if (panel == null) return false;
        if (!ArcoDisponibleParaInteraccion) return false;

        VerificarOperacionAtascada();

        index = panelesGuardados.IndexOf(panel);
        if (index < 0 || animando) return false;

        return true;
    }

    private IEnumerator RestaurarAnimado(AlgoLabPocketPanelItem panel, int index, Transform puntoPuntero, AlgoLabPocketMiniCardView cardOrigen)
    {
        animando = true; tiempoInicioAnimando = Time.unscaledTime;
        BloquearAutoRegistroTemporal(panel, tiempoIgnorarAutoRegistroTrasRestaurar);
        if (cardOrigen != null && animarSacarPanel) yield return AnimarEncogerMiniCard(cardOrigen);
        index = panelesGuardados.IndexOf(panel);
        if (index < 0)
        {
            FinalizarOperacionActiva();
            yield break;
        }

        panelesGuardados.RemoveAt(index);
        if (indiceSeleccionado >= panelesGuardados.Count) indiceSeleccionado = Mathf.Max(0, panelesGuardados.Count - 1);

        Vector3 pos;
        Quaternion rot;
        if (puntoPuntero == null) ObtenerPoseRestauracion(out pos, out rot);
        else ObtenerPoseRestauracionDesdePuntero(puntoPuntero, out pos, out rot);

        yield return RestaurarPanelYaCalculado(panel, pos, rot);
    }

    private IEnumerator RestaurarAnimadoDesdePoseCongelada(
        AlgoLabPocketPanelItem panel,
        int index,
        Vector3 posicionMundoSuelta,
        Quaternion rotacionMundoSuelta,
        AlgoLabPocketMiniCardView cardOrigen)
    {
        animando = true; tiempoInicioAnimando = Time.unscaledTime;
        BloquearAutoRegistroTemporal(panel, tiempoIgnorarAutoRegistroTrasRestaurar);

        // La animación puede tardar varios frames, pero la posición ya quedó congelada.
        if (cardOrigen != null && animarSacarPanel) yield return AnimarEncogerMiniCard(cardOrigen);

        index = panelesGuardados.IndexOf(panel);
        if (index < 0)
        {
            FinalizarOperacionActiva();
            yield break;
        }

        panelesGuardados.RemoveAt(index);
        if (indiceSeleccionado >= panelesGuardados.Count) indiceSeleccionado = Mathf.Max(0, panelesGuardados.Count - 1);

        ObtenerPoseRestauracionDesdePoseCongelada(
            posicionMundoSuelta,
            rotacionMundoSuelta,
            out Vector3 pos,
            out Quaternion rot
        );

        yield return RestaurarPanelYaCalculado(panel, pos, rot);
    }

    private IEnumerator RestaurarPanelYaCalculado(AlgoLabPocketPanelItem panel, Vector3 pos, Quaternion rot)
    {
        rot = AjustarRotacionRestauracionEspecial(panel, pos, rot);
        ActivarPanelRestauradoFueraDelArco(panel);

        AlgoLabTutorialPanelController tutorialRestaurado =
            panel.panelRoot != null
                ? panel.panelRoot.GetComponentInChildren<AlgoLabTutorialPanelController>(true)
                : null;

        if (animarSacarPanel)
        {
            yield return panel.RestaurarDesdePocketAnimado(
                pos,
                rot,
                duracionCrecerPanelAlSacar,
                escalaInicialPanelRestaurado,
                reboteEscalaPanelRestaurado,
                tutorialRestaurado != null
            );
        }
        else
        {
            panel.RestaurarDesdePocket(pos, rot);
        }

        ActivarPanelRestauradoFueraDelArco(panel);
        panel.ForzarEscalaNormalRestaurada();

        ActualizarContenidoReposo();
        AplicarLayoutReposoInmediato();

        // Importante: al terminar de sacar un panel desde una card,
        // se liberan bloqueos para poder sacar las demás cards guardadas.
        LiberarBloqueosDespuesDeSacarCard(panel);
        BloquearAutoRegistroTemporal(panel, tiempoIgnorarAutoRegistroTrasRestaurar);
        NotificarTutorialPanelRestaurado(panel);
        panel.ForzarEscalaNormalRestaurada();

        // Algunos controladores reaccionan a la notificación al final del frame.
        // Se reafirma una vez más la escala normal antes de liberar la operación.
        yield return null;
        panel.ForzarEscalaNormalRestaurada();

        FinalizarOperacionActiva();
    }

    private Quaternion AjustarRotacionRestauracionEspecial(
        AlgoLabPocketPanelItem panel,
        Vector3 posicion,
        Quaternion rotacionOriginal)
    {
        if (panel == null || panel.panelRoot == null)
        {
            return rotacionOriginal;
        }

        AlgoLabTutorialPanelController tutorial =
            panel.panelRoot.GetComponentInChildren<AlgoLabTutorialPanelController>(true);
        if (tutorial == null)
        {
            return rotacionOriginal;
        }

        if (camaraJugador == null)
        {
            camaraJugador = Camera.main;
        }

        if (camaraJugador == null)
        {
            return rotacionOriginal;
        }

        Vector3 direccion = posicion - camaraJugador.transform.position;
        bool soloRotacionY = tutorial.soloRotacionYTutorial;

        if (soloRotacionY)
        {
            direccion.y = 0f;
        }

        return AlgoLabPanelFacing.TryGetStableRotation(
            direccion,
            soloRotacionY,
            Quaternion.Euler(tutorial.rotacionLocalTutorialEuler),
            tutorial.invertirFrenteTutorial,
            out Quaternion rotacionEstable
        )
            ? rotacionEstable
            : rotacionOriginal;
    }

    private IEnumerator AnimarEncogerMiniCard(AlgoLabPocketMiniCardView card)
    {
        Vector3 e0 = card.Rect.localScale;
        float a0 = card.Alpha;
        float tiempo = 0f;
        float dur = Mathf.Max(0.01f, duracionEncogerCardAlSacar);
        while (tiempo < dur)
        {
            tiempo += Time.unscaledDeltaTime;
            float s = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(tiempo / dur));
            card.Rect.localScale = Vector3.Lerp(e0, e0 * 0.01f, s);
            card.SetAlpha(Mathf.Lerp(a0, 0f, s));
            yield return null;
        }
    }

    private void ObtenerPoseRestauracion(out Vector3 pos, out Quaternion rot)
    {
        if (camaraJugador == null) camaraJugador = Camera.main;
        if (camaraJugador == null) { pos = transform.position + transform.forward * 0.8f; rot = transform.rotation; return; }
        Vector3 f = Vector3.ProjectOnPlane(camaraJugador.transform.forward, Vector3.up).normalized;
        if (f.sqrMagnitude < 0.001f) f = camaraJugador.transform.forward;
        pos = camaraJugador.transform.position + f * distanciaRestaurarFrenteJugador + Vector3.up * alturaRestaurarRespectoCamara;
        Vector3 dir = camaraJugador.transform.position - pos;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) dir = -f;
        rot = Quaternion.LookRotation(-dir.normalized, Vector3.up);
    }

    private void ObtenerPoseRestauracionDesdePuntero(Transform p, out Vector3 pos, out Quaternion rot)
    {
        Vector3 posicionBase = p != null ? p.position : transform.position;
        Quaternion rotacionBase = p != null ? p.rotation : transform.rotation;

        ObtenerPoseRestauracionDesdePoseCongelada(posicionBase, rotacionBase, out pos, out rot);
    }

    private void ObtenerPoseRestauracionDesdePoseCongelada(
        Vector3 posicionBase,
        Quaternion rotacionBase,
        out Vector3 pos,
        out Quaternion rot)
    {
        if (camaraJugador == null) camaraJugador = Camera.main;

        if (restaurarPanelExactamenteDondeSueltaCard)
        {
            pos = posicionBase + offsetMundoAlRestaurarDesdeCard;
        }
        else
        {
            Vector3 f = rotacionBase * Vector3.forward;

            if (camaraJugador != null)
            {
                f = Vector3.ProjectOnPlane(camaraJugador.transform.forward, Vector3.up).normalized;
            }

            if (f.sqrMagnitude < 0.001f)
            {
                f = transform.forward;
            }

            pos = posicionBase + f.normalized * distanciaRestaurarDesdePuntero;
        }

        if (camaraJugador != null)
        {
            Vector3 dir = camaraJugador.transform.position - pos;
            dir.y = 0f;

            if (dir.sqrMagnitude > 0.001f)
            {
                rot = Quaternion.LookRotation(-dir.normalized, Vector3.up);
                return;
            }
        }

        rot = rotacionBase;
    }

    private void CrearCardsVisuales()
    {
        if (miniCardPrefab == null || miniCardsParent == null) return;
        ActivarCadena(miniCardsParent);
        miniCardsParent.gameObject.SetActive(true);
        while (slotCards.Count < 4)
        {
            AlgoLabPocketMiniCardView nueva = Instantiate(miniCardPrefab, miniCardsParent, false);
            nueva.gameObject.SetActive(true);
            nueva.ConfigurarManager(this);
            nueva.SetAlpha(1f);
            slotCards.Add(nueva);
            escalasOriginales.Add(nueva.transform.localScale);
        }
    }

    private void ActualizarContenidoReposo()
    {
        CrearCardsVisuales();
        int count = panelesGuardados.Count;

        for (int i = 0; i < slotCards.Count; i++)
        {
            AlgoLabPocketMiniCardView c = slotCards[i];
            if (c == null) continue;
            if (count <= 0) { c.gameObject.SetActive(false); continue; }
            bool activa = false;
            AlgoLabPocketPanelItem p = null;
            bool centro = false;
            if (count == 1)
            {
                activa = i == 1;
                p = panelesGuardados[indiceSeleccionado];
                centro = true;
            }
            else if (count == 2 && !repetirPanelCuandoSoloHayDos)
            {
                activa = i == 1 || i == 2;
                if (activa)
                {
                    p = i == 1
                        ? panelesGuardados[indiceSeleccionado]
                        : ObtenerPanelRelativo(1);
                    centro = i == 1;
                }
            }
            else
            {
                activa = i == 0 || i == 1 || i == 2;
                if (activa)
                {
                    p = ObtenerPanelRelativo(i == 0 ? -1 : i == 1 ? 0 : 1);
                    centro = i == 1;
                }
            }
            c.gameObject.SetActive(activa);
            if (activa) { c.Configurar(p, centro); c.SetAlpha(centro ? alphaCentro : alphaLateral); }
            else c.SetAlpha(0f);
        }
    }

    private AlgoLabPocketPanelItem ObtenerPanelRelativo(int offset)
    {
        if (panelesGuardados.Count == 0) return null;
        return panelesGuardados[ObtenerIndiceCircular(indiceSeleccionado + offset)];
    }

    private int ObtenerIndiceCircular(int i)
    {
        if (panelesGuardados.Count == 0) return 0;
        while (i < 0) i += panelesGuardados.Count;
        while (i >= panelesGuardados.Count) i -= panelesGuardados.Count;
        return i;
    }

    private bool SlotsListos()
    {
        return pointIzquierdo3 && pointIzquierdo2 && pointIzquierdo1 && pointCenter && pointDerecho1 && pointDerecho2 && pointDerecho3;
    }

    private RectTransform Slot(int i)
    {
        if (i == 0) return pointIzquierdo3;
        if (i == 1) return pointIzquierdo2;
        if (i == 2) return pointIzquierdo1;
        if (i == 3) return pointCenter;
        if (i == 4) return pointDerecho1;
        if (i == 5) return pointDerecho2;
        return pointDerecho3;
    }

    private Vector3 PosSlot(float f)
    {
        f = Mathf.Clamp(f, 0f, 6f);
        int a = Mathf.FloorToInt(f);
        int b = Mathf.CeilToInt(f);
        float t = f - a;
        return Vector3.Lerp(Slot(a).position, Slot(b).position, t);
    }

    private Quaternion RotSlot(float f)
    {
        f = Mathf.Clamp(f, 0f, 6f);
        int a = Mathf.FloorToInt(f);
        int b = Mathf.CeilToInt(f);
        float t = f - a;
        return Quaternion.Slerp(Slot(a).rotation, Slot(b).rotation, t);
    }

    private float RotZ(float slot)
    {
        float z = (CENTRO - slot) * (rotacionMaximaExtremos / 3f);
        return invertirRotacionMiniCard ? -z : z;
    }

    private Vector3 EscalaBaseCard(int idx)
    {
        if (respetarEscalaDelPrefab && idx >= 0 && idx < escalasOriginales.Count) return escalasOriginales[idx];
        return Vector3.one;
    }

    private void AplicarPoseCard(AlgoLabPocketMiniCardView c, int idx, float slot, float alpha)
    {
        if (c == null) return;
        c.Rect.position = PosSlot(slot);
        c.Rect.localScale = EscalaBaseCard(idx);
        Quaternion r = RotSlot(slot);
        c.Rect.rotation = rotarMiniCardsPorSlot ? r * Quaternion.Euler(0f, 0f, RotZ(slot)) : r;
        c.SetAlpha(alpha);
    }

    private void AplicarPoseCardInterpolada(AlgoLabPocketMiniCardView c, int idx, float a, float b, float alphaA, float alphaB, float t)
    {
        AplicarPoseCard(c, idx, Mathf.Lerp(a, b, t), Mathf.Lerp(alphaA, alphaB, t));
    }

    private void AplicarLayoutReposoInmediato()
    {
        CrearCardsVisuales();
        int count = panelesGuardados.Count;
        if (count <= 0) { for (int i = 0; i < slotCards.Count; i++) if (slotCards[i]) slotCards[i].gameObject.SetActive(false); return; }
        if (count == 1)
        {
            slotCards[0].gameObject.SetActive(false); slotCards[2].gameObject.SetActive(false); slotCards[3].gameObject.SetActive(false);
            slotCards[1].gameObject.SetActive(true); AplicarPoseCard(slotCards[1], 1, CENTRO, alphaCentro); return;
        }
        if (count == 2 && !repetirPanelCuandoSoloHayDos)
        {
            slotCards[0].gameObject.SetActive(false);
            slotCards[1].gameObject.SetActive(true);
            slotCards[2].gameObject.SetActive(true);
            slotCards[3].gameObject.SetActive(false);
            AplicarPoseCard(slotCards[1], 1, CENTRO, alphaCentro);
            AplicarPoseCard(slotCards[2], 2, VIS_DER, alphaLateral);
            return;
        }
        slotCards[0].gameObject.SetActive(true); slotCards[1].gameObject.SetActive(true); slotCards[2].gameObject.SetActive(true); slotCards[3].gameObject.SetActive(false);
        AplicarPoseCard(slotCards[0], 0, VIS_IZQ, alphaLateral);
        AplicarPoseCard(slotCards[1], 1, CENTRO, alphaCentro);
        AplicarPoseCard(slotCards[2], 2, VIS_DER, alphaLateral);
    }

    private void GirarCarrusel(int dir)
    {
        if (panelesGuardados.Count <= 1 || animando || rutina != null || miniCardAgarrada) return;
        ForzarMostrarCarruselTemporal(segundosVisibleTrasGuardar);

        if (panelesGuardados.Count == 2 && !repetirPanelCuandoSoloHayDos)
        {
            indiceSeleccionado = ObtenerIndiceCircular(indiceSeleccionado + dir);
            ActualizarContenidoReposo();
            AplicarLayoutReposoInmediato();
            return;
        }

        operacionActiva = TipoOperacion.Girar;
        panelOperacionActiva = null;
        rutina = StartCoroutine(AnimarGiro(dir));
    }

    private IEnumerator AnimarGiro(int dir)
    {
        animando = true; tiempoInicioAnimando = Time.unscaledTime;
        CrearCardsVisuales();
        int anterior = indiceSeleccionado;
        int nuevo = ObtenerIndiceCircular(indiceSeleccionado + dir);
        if (dir > 0)
        {
            slotCards[0].Configurar(PanelRelativoDesdeIndice(anterior, -1), false);
            slotCards[1].Configurar(PanelRelativoDesdeIndice(anterior, 0), false);
            slotCards[2].Configurar(PanelRelativoDesdeIndice(anterior, 1), true);
            slotCards[3].Configurar(PanelRelativoDesdeIndice(nuevo, 1), false);
        }
        else
        {
            slotCards[0].Configurar(PanelRelativoDesdeIndice(anterior, -1), true);
            slotCards[1].Configurar(PanelRelativoDesdeIndice(anterior, 0), false);
            slotCards[2].Configurar(PanelRelativoDesdeIndice(anterior, 1), false);
            slotCards[3].Configurar(PanelRelativoDesdeIndice(nuevo, -1), false);
        }
        for (int i = 0; i < 4; i++) slotCards[i].gameObject.SetActive(true);
        float[] aS = new float[4]; float[] bS = new float[4]; float[] aA = new float[4]; float[] bA = new float[4];
        if (dir > 0)
        {
            aS[0] = VIS_IZQ; bS[0] = EXT_IZQ; aA[0] = alphaLateral; bA[0] = 0f;
            aS[1] = CENTRO; bS[1] = VIS_IZQ; aA[1] = alphaCentro; bA[1] = alphaLateral;
            aS[2] = VIS_DER; bS[2] = CENTRO; aA[2] = alphaLateral; bA[2] = alphaCentro;
            aS[3] = EXT_DER; bS[3] = VIS_DER; aA[3] = 0f; bA[3] = alphaLateral;
        }
        else
        {
            aS[0] = VIS_IZQ; bS[0] = CENTRO; aA[0] = alphaLateral; bA[0] = alphaCentro;
            aS[1] = CENTRO; bS[1] = VIS_DER; aA[1] = alphaCentro; bA[1] = alphaLateral;
            aS[2] = VIS_DER; bS[2] = EXT_DER; aA[2] = alphaLateral; bA[2] = 0f;
            aS[3] = EXT_IZQ; bS[3] = VIS_IZQ; aA[3] = 0f; bA[3] = alphaLateral;
        }
        float tiempo = 0f; float dur = Mathf.Max(0.01f, duracionAnimacion);
        while (tiempo < dur)
        {
            tiempo += Time.unscaledDeltaTime;
            float s = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(tiempo / dur));
            for (int i = 0; i < 4; i++) AplicarPoseCard(slotCards[i], i, Mathf.Lerp(aS[i], bS[i], s), Mathf.Lerp(aA[i], bA[i], s));
            yield return null;
        }
        indiceSeleccionado = nuevo;
        ActualizarContenidoReposo();
        AplicarLayoutReposoInmediato();
        FinalizarOperacionActiva();
    }

    private AlgoLabPocketPanelItem PanelRelativoDesdeIndice(int baseIndex, int offset)
    {
        return panelesGuardados[ObtenerIndiceCircular(baseIndex + offset)];
    }

    [ContextMenu("Probar mostrar carrusel")]
    public void ProbarMostrarCarrusel()
    {
        ForzarMostrarCarruselTemporal(5f); ActualizarContenidoReposo(); AplicarLayoutReposoInmediato();
    }
    [ContextMenu("Probar giro derecha")] public void ProbarGiroDerecha() { GirarCarrusel(1); }
    [ContextMenu("Probar giro izquierda")] public void ProbarGiroIzquierda() { GirarCarrusel(-1); }
    [ContextMenu("Restaurar seleccionado")] public void ProbarRestaurarSeleccionado() { RestaurarPanelSeleccionado(); }
}
