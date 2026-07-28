using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AlgoLabSettingsMenuController : MonoBehaviour
{
    public static AlgoLabSettingsMenuController Instance { get; private set; }

    private enum VistaMenu
    {
        Paneles,
        Sonido,
        Graficos,
        Sesion,
        Ranking
    }

    [Header("Presentacion en mundo")]
    [Range(0.0005f, 0.0015f)]
    public float escalaMenuMundo = 0.00086f;
    public float distanciaMenuUsuario = 0.95f;
    public float offsetVerticalMenu = -0.02f;
    public float profundidadColliderMenu = 16f;

    private static readonly Color Fondo = ColorHex("15181B");
    private static readonly Color FondoLateral = ColorHex("20252A");
    private static readonly Color Banda = ColorHex("292F34");
    private static readonly Color BandaAlterna = ColorHex("23282D");
    private static readonly Color Texto = ColorHex("F2F4F5");
    private static readonly Color TextoSuave = ColorHex("AEB8BE");
    private static readonly Color Verde = ColorHex("2ED6A1");
    private static readonly Color VerdeOscuro = ColorHex("167A64");
    private static readonly Color Amarillo = ColorHex("F0B35C");
    private static readonly Color Rojo = ColorHex("E96868");
    private static readonly Color Bronce = ColorHex("C98758");
    private static readonly Color Plata = ColorHex("BBC4CC");
    private const float AlturaSentadoMinUI = 0.85f;
    private const float AlturaSentadoMaxUI = 1.55f;
    private const float AlturaParadoMinUI = 1.15f;
    private const float AlturaParadoMaxUI = 2.1f;
    private const float DuracionTransicionPaneles = 0.22f;

    private sealed class EstadoPanelTemporal
    {
        public GameObject panel;
        public bool activoOriginal;
        public Vector3 escalaOriginal;
        public CanvasGroup grupo;
        public bool grupoCreado;
        public float alphaOriginal;
        public bool interactableOriginal;
        public bool blocksRaycastsOriginal;
        public Coroutine animacion;
    }

    private AlgoLabPanelPocketManager pocketManager;
    private AlgoLabManualPanelSpawnManager panelSpawnManager;
    private AlgoLabSessionManager sessionManager;
    private AlgoLabBackendClient backendClient;
    private AlgoLabStartUIController startUIController;
    private AlgoLabTutorialPanelController tutorialController;
    private AlgoLabProgressPanel progressPanel;
    private AlgoLabGameSettings ajustes;
    private AlgoLabHeightGuideRings guiasAltura;

    private RectTransform canvasRect;
    private Canvas canvas;
    private GameObject menuRoot;
    private Sprite spriteUI;
    private VistaMenu vistaActual;
    private bool menuAbierto;
    private AlgoLabPanelPocketManager pocketManagerConAccionConfiguracion;
    private float proximaBusqueda;
    private float proximaActualizacionEstado;

    private readonly Dictionary<VistaMenu, GameObject> vistas =
        new Dictionary<VistaMenu, GameObject>();
    private readonly Dictionary<VistaMenu, Button> botonesNavegacion =
        new Dictionary<VistaMenu, Button>();
    private readonly Dictionary<GameObject, EstadoPanelTemporal> estadosPaneles =
        new Dictionary<GameObject, EstadoPanelTemporal>();

    private TMP_Text tituloVista;
    private TMP_Text textoEstadoPostura;
    private TMP_Text textoSesion;
    private TMP_Text textoSesionNivel;
    private TMP_Text textoSesionPuntaje;
    private TMP_Text textoSesionEstado;
    private TMP_Text textoEstadoRanking;
    private TMP_Text textoBotonCerrarSesion;
    private TMP_Text textoFpsActual;
    private TMP_Text textoFrecuenciaVisor;
    private RectTransform contenidoRanking;
    private ScrollRect scrollRanking;

    private Slider sliderAlturaSentado;
    private Slider sliderAlturaParado;
    private Slider sliderVolumenGeneral;
    private Slider sliderVolumenVoz;
    private Slider sliderVolumenEfectos;
    private Slider sliderEscalaRender;
    private TMP_Text valorAlturaSentado;
    private TMP_Text valorAlturaParado;
    private TMP_Text valorVolumenGeneral;
    private TMP_Text valorVolumenVoz;
    private TMP_Text valorVolumenEfectos;
    private TMP_Text valorEscalaRender;
    private Button botonSuavizado;
    private Button botonMostrarPaneles;
    private Button botonRankingSesion;
    private Button botonSalirNivel;
    private readonly List<Button> botonesPostura = new List<Button>();
    private readonly List<Button> botonesSalidaIA = new List<Button>();
    private readonly List<Button> botonesPerfil = new List<Button>();
    private readonly List<Button> botonesFps = new List<Button>();
    private bool mostrarPanelesCalibracion;
    private bool pausaNivelAplicada;
    private float escalaTiempoAnterior = 1f;
    private bool audioPausadoAnterior;
    private Coroutine rutinaRestauracionPaneles;
    private float fpsActualMedido;
    private float tiempoMuestraFps;
    private int cuadrosMuestraFps;
    private int generacionRanking;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InstalarEnEscena()
    {
        AlgoLabSettingsMenuController existente =
            FindFirstObjectByType<AlgoLabSettingsMenuController>(FindObjectsInactive.Include);

        if (existente == null)
        {
            GameObject root = new GameObject("[ALGOLAB_SETTINGS_MENU]");
            root.AddComponent<AlgoLabSettingsMenuController>();
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            enabled = false;
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // UISprite.psd dejó de estar disponible como recurso integrado en el Player
        // de Unity 6. Las imágenes del menú funcionan como rectángulos de color sin
        // sprite y así se evita un error de recurso cada vez que inicia la aplicación.
        spriteUI = null;

        BuscarReferencias();
        CrearMenu(false);
    }

    private IEnumerator Start()
    {
        yield return null;
        yield return null;
        BuscarReferencias();
        PrepararBotonAcceso();

        if (ajustes != null)
        {
            ajustes.AjustesCambiaron += RefrescarControles;
            ajustes.AplicarPaneles();
        }

        RefrescarControles();
    }

    private void OnDestroy()
    {
        generacionRanking++;

        if (pocketManagerConAccionConfiguracion != null)
        {
            pocketManagerConAccionConfiguracion.DesregistrarAccionConfiguracion(AbrirConfiguracion);
            pocketManagerConAccionConfiguracion = null;
        }

        if (ajustes != null)
        {
            ajustes.AjustesCambiaron -= RefrescarControles;
        }

        if (menuAbierto || estadosPaneles.Count > 0)
        {
            RestaurarPanelesOcultos(true);
            if (pocketManager != null)
            {
                pocketManager.SetInterfazModalActiva(false);
            }
        }

        ReanudarNivel();

        DesregistrarMenuDeRayosControl();

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void OnDisable()
    {
        generacionRanking++;

        bool estabaAbierto = menuAbierto;
        menuAbierto = false;

        if (estabaAbierto || estadosPaneles.Count > 0)
        {
            RestaurarPanelesOcultos(true);

            if (pocketManager != null)
            {
                pocketManager.SetInterfazModalActiva(false);
            }
        }

        ReanudarNivel();

        pocketManagerConAccionConfiguracion?.SetAccionConfiguracionVisible(true);
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.F10))
        {
            if (menuAbierto) CerrarConfiguracion();
            else AbrirConfiguracion();
        }
#endif

        if (menuAbierto)
        {
            ActualizarMedicionFps();
        }

        if (Time.unscaledTime >= proximaBusqueda)
        {
            proximaBusqueda = Time.unscaledTime + 1f;
            BuscarReferencias();

            if (pocketManagerConAccionConfiguracion != pocketManager)
            {
                PrepararBotonAcceso();
            }

            ActualizarDisponibilidadBoton();

            if (menuAbierto)
            {
                RegistrarMenuEnRayosControl();
            }
        }

        if (menuAbierto && Time.unscaledTime >= proximaActualizacionEstado)
        {
            proximaActualizacionEstado = Time.unscaledTime + 0.25f;
            // El menú pausa el nivel con tiempo normal, pero la calibración
            // debe seguir leyendo la cabeza en tiempo real. Esto evita que al
            // cambiar la altura de pie mientras el usuario se levanta se quede
            // aplicado el objetivo sentado.
            panelSpawnManager?.ActualizarPosturaYAlturaAhora();
            ActualizarEstadoPostura();
            ActualizarDatosSesion();
        }
    }

    private void LateUpdate()
    {
        if (!menuAbierto || canvasRect == null)
        {
            return;
        }

        Camera camara = ObtenerCamara();
        if (camara == null)
        {
            return;
        }

        Vector3 direccion = canvasRect.position - camara.transform.position;
        if (AlgoLabPanelFacing.TryGetStableRotation(
                direccion,
                true,
                Quaternion.identity,
                false,
                out Quaternion rotacionObjetivo))
        {
            canvasRect.rotation = Quaternion.Slerp(
                canvasRect.rotation,
                rotacionObjetivo,
                Time.unscaledDeltaTime * 5f
            );
        }
    }

    private void BuscarReferencias()
    {
        if (pocketManager == null)
        {
            pocketManager = AlgoLabPanelPocketManager.Instance;
        }

        if (pocketManager == null)
        {
            pocketManager = FindFirstObjectByType<AlgoLabPanelPocketManager>(FindObjectsInactive.Include);
        }

        if (panelSpawnManager == null)
        {
            panelSpawnManager = AlgoLabManualPanelSpawnManager.Instance;
        }

        if (panelSpawnManager == null)
        {
            panelSpawnManager = FindFirstObjectByType<AlgoLabManualPanelSpawnManager>(FindObjectsInactive.Include);
        }

        if (sessionManager == null)
        {
            sessionManager = AlgoLabSessionManager.Instance;
        }

        if (sessionManager == null)
        {
            sessionManager = FindFirstObjectByType<AlgoLabSessionManager>(FindObjectsInactive.Include);
        }

        if (backendClient == null)
        {
            backendClient = AlgoLabBackendClient.Instance;
        }

        if (backendClient == null)
        {
            backendClient = FindFirstObjectByType<AlgoLabBackendClient>(FindObjectsInactive.Include);
        }

        if (startUIController == null)
        {
            startUIController = FindFirstObjectByType<AlgoLabStartUIController>(FindObjectsInactive.Include);
        }

        if (tutorialController == null)
        {
            tutorialController = FindFirstObjectByType<AlgoLabTutorialPanelController>(FindObjectsInactive.Include);
        }

        if (progressPanel == null)
        {
            progressPanel = FindFirstObjectByType<AlgoLabProgressPanel>(FindObjectsInactive.Include);
        }

        if (ajustes == null)
        {
            ajustes = AlgoLabGameSettings.Instance;
        }
    }

    private void PrepararBotonAcceso()
    {
        if (pocketManager == null)
        {
            return;
        }

        pocketManager.AutoBuscarReferencias();
        if (pocketManager.pocketVisualRoot == null || pocketManager.miniCardsParent == null)
        {
            return;
        }

        // Desactiva el acceso fijo de una recarga anterior y usa la card especial.
        Transform regionExistente = pocketManager.pocketVisualRoot.transform.Find("RegionBotonesOpciones");
        if (regionExistente != null)
        {
            regionExistente.gameObject.SetActive(false);
        }

        if (pocketManagerConAccionConfiguracion != null &&
            pocketManagerConAccionConfiguracion != pocketManager)
        {
            pocketManagerConAccionConfiguracion.DesregistrarAccionConfiguracion(AbrirConfiguracion);
        }

        pocketManager.RegistrarAccionConfiguracion(AbrirConfiguracion);
        pocketManagerConAccionConfiguracion = pocketManager;
        ActualizarDisponibilidadBoton();
    }

    private void ActualizarDisponibilidadBoton()
    {
        if (pocketManagerConAccionConfiguracion == null)
        {
            return;
        }

        pocketManagerConAccionConfiguracion.SetAccionConfiguracionVisible(!menuAbierto);
    }

    public void AbrirConfiguracion()
    {
        if (menuAbierto)
        {
            return;
        }

        BuscarReferencias();

        if (menuRoot == null)
        {
            CrearMenu(false);
        }

        if (menuRoot == null)
        {
            return;
        }

        if (pocketManager != null &&
            (pocketManager.EstaGuardandoOAnimando() || pocketManager.EstaMiniCardAgarrada()))
        {
            return;
        }

        RestaurarPanelesOcultos(true);
        menuAbierto = true;
        mostrarPanelesCalibracion = false;
        fpsActualMedido = 0f;
        tiempoMuestraFps = 0f;
        cuadrosMuestraFps = 0;
        if (pocketManager != null)
        {
            pocketManager.SetInterfazModalActiva(true);
        }

        CapturarYOcultarPaneles();
        PausarNivel();

        ColocarMenuFrenteAlUsuario();
        menuRoot.SetActive(true);
        RegistrarMenuEnRayosControl();
        ActualizarInteraccionesVR();
        MostrarVista(VistaMenu.Paneles);
        RefrescarControles();
        ActualizarDisponibilidadBoton();
    }

    public void CerrarConfiguracion()
    {
        if (!menuAbierto)
        {
            return;
        }

        menuAbierto = false;
        generacionRanking++;
        if (menuRoot != null)
        {
            menuRoot.SetActive(false);
        }
        guiasAltura?.OcultarSuavemente();
        RestaurarPanelesOcultos(false);
        ReanudarNivel();

        if (pocketManager != null)
        {
            pocketManager.SetInterfazModalActiva(false);
        }

        ActualizarDisponibilidadBoton();
    }

    private void CapturarYOcultarPaneles()
    {
        estadosPaneles.Clear();

        AlgoLabPocketPanelItem[] items = FindObjectsByType<AlgoLabPocketPanelItem>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] != null && !items[i].esAccionConfiguracion)
            {
                RegistrarPanelParaOcultar(items[i].panelRoot != null
                    ? items[i].panelRoot.gameObject
                    : items[i].gameObject);
            }
        }

        if (panelSpawnManager != null && panelSpawnManager.paneles != null)
        {
            for (int i = 0; i < panelSpawnManager.paneles.Count; i++)
            {
                AlgoLabManualPanelSpawnManager.PanelManualInfo info = panelSpawnManager.paneles[i];
                if (info != null && info.panelRoot != null)
                {
                    RegistrarPanelParaOcultar(info.panelRoot.gameObject);
                }
            }
        }

        foreach (KeyValuePair<GameObject, EstadoPanelTemporal> par in estadosPaneles)
        {
            EstadoPanelTemporal estado = par.Value;
            if (estado.panel != null && estado.activoOriginal)
            {
                PrepararPanelSoloComoVistaPrevia(estado);
                estado.grupo.alpha = 0f;
                estado.panel.transform.localScale = estado.escalaOriginal;
                estado.panel.SetActive(false);
            }
        }
    }

    private void RegistrarPanelParaOcultar(GameObject panel)
    {
        if (panel == null || estadosPaneles.ContainsKey(panel))
        {
            return;
        }

        if (menuRoot != null && panel.transform.IsChildOf(menuRoot.transform))
        {
            return;
        }

        if (pocketManager != null &&
            pocketManager.pocketVisualRoot != null &&
            panel == pocketManager.pocketVisualRoot)
        {
            return;
        }

        CanvasGroup grupo = panel.GetComponent<CanvasGroup>();
        bool grupoCreado = false;
        if (grupo == null)
        {
            grupo = panel.AddComponent<CanvasGroup>();
            grupoCreado = true;
        }

        estadosPaneles.Add(panel, new EstadoPanelTemporal
        {
            panel = panel,
            activoOriginal = panel.activeSelf,
            escalaOriginal = panel.transform.localScale,
            grupo = grupo,
            grupoCreado = grupoCreado,
            alphaOriginal = grupo.alpha,
            interactableOriginal = grupo.interactable,
            blocksRaycastsOriginal = grupo.blocksRaycasts
        });
    }

    private void AlternarVistaPreviaPaneles()
    {
        mostrarPanelesCalibracion = !mostrarPanelesCalibracion;
        ActualizarVistaPreviaPaneles(true);
        RefrescarControles();
    }

    private void ActualizarVistaPreviaPaneles(bool animar)
    {
        bool mostrar = menuAbierto &&
                       vistaActual == VistaMenu.Paneles &&
                       mostrarPanelesCalibracion;

        foreach (KeyValuePair<GameObject, EstadoPanelTemporal> par in estadosPaneles)
        {
            EstadoPanelTemporal estado = par.Value;
            if (estado.panel == null || !estado.activoOriginal)
            {
                continue;
            }

            if (estado.animacion != null)
            {
                StopCoroutine(estado.animacion);
                estado.animacion = null;
            }

            if (!animar)
            {
                AplicarVistaPreviaPanelInmediata(estado, mostrar);
                continue;
            }

            estado.animacion = StartCoroutine(AnimarVistaPreviaPanel(estado, mostrar));
        }
    }

    private IEnumerator AnimarVistaPreviaPanel(EstadoPanelTemporal estado, bool mostrar)
    {
        if (estado.panel == null)
        {
            yield break;
        }

        if (!mostrar && !estado.panel.activeSelf)
        {
            AplicarVistaPreviaPanelInmediata(estado, false);
            estado.animacion = null;
            yield break;
        }

        bool estabaActivo = estado.panel.activeSelf;
        if (!estabaActivo)
        {
            estado.panel.SetActive(true);
            estado.panel.transform.localScale = estado.escalaOriginal * 0.96f;
            estado.grupo.alpha = 0f;
        }

        PrepararPanelSoloComoVistaPrevia(estado);

        Vector3 escalaInicio = estado.panel.transform.localScale;
        Vector3 escalaDestino = mostrar
            ? estado.escalaOriginal
            : estado.escalaOriginal * 0.96f;
        float alphaInicio = estado.grupo.alpha;
        float alphaDestino = mostrar ? estado.alphaOriginal : 0f;
        float tiempo = 0f;

        while (tiempo < DuracionTransicionPaneles && estado.panel != null)
        {
            tiempo += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(tiempo / DuracionTransicionPaneles));
            estado.panel.transform.localScale = Vector3.LerpUnclamped(escalaInicio, escalaDestino, t);
            estado.grupo.alpha = Mathf.LerpUnclamped(alphaInicio, alphaDestino, t);
            yield return null;
        }

        if (estado.panel != null)
        {
            estado.panel.transform.localScale = mostrar ? estado.escalaOriginal : estado.escalaOriginal;
            estado.grupo.alpha = alphaDestino;
            if (!mostrar)
            {
                estado.panel.SetActive(false);
            }
        }

        estado.animacion = null;
    }

    private void AplicarVistaPreviaPanelInmediata(EstadoPanelTemporal estado, bool mostrar)
    {
        if (estado.panel == null)
        {
            return;
        }

        if (mostrar)
        {
            estado.panel.SetActive(true);
            estado.panel.transform.localScale = estado.escalaOriginal;
            estado.grupo.alpha = estado.alphaOriginal;
            PrepararPanelSoloComoVistaPrevia(estado);
        }
        else
        {
            estado.panel.transform.localScale = estado.escalaOriginal;
            estado.grupo.alpha = 0f;
            estado.panel.SetActive(false);
        }
    }

    private void PrepararPanelSoloComoVistaPrevia(EstadoPanelTemporal estado)
    {
        if (estado.grupo == null)
        {
            return;
        }

        estado.grupo.interactable = false;
        estado.grupo.blocksRaycasts = false;
    }

    private void RestaurarPanelesOcultos(bool inmediato = false)
    {
        if (rutinaRestauracionPaneles != null)
        {
            StopCoroutine(rutinaRestauracionPaneles);
            rutinaRestauracionPaneles = null;
            inmediato = true;
        }

        foreach (KeyValuePair<GameObject, EstadoPanelTemporal> par in estadosPaneles)
        {
            if (par.Value.animacion != null)
            {
                StopCoroutine(par.Value.animacion);
                par.Value.animacion = null;
            }
        }

        if (estadosPaneles.Count == 0)
        {
            return;
        }

        if (!inmediato && isActiveAndEnabled)
        {
            rutinaRestauracionPaneles = StartCoroutine(RestaurarPanelesAnimado());
            return;
        }

        foreach (KeyValuePair<GameObject, EstadoPanelTemporal> par in estadosPaneles)
        {
            FinalizarRestauracionPanel(par.Value);
        }

        estadosPaneles.Clear();
    }

    private IEnumerator RestaurarPanelesAnimado()
    {
        List<EstadoPanelTemporal> lista = new List<EstadoPanelTemporal>(estadosPaneles.Values);
        Vector3[] escalasInicio = new Vector3[lista.Count];
        float[] alphasInicio = new float[lista.Count];

        for (int i = 0; i < lista.Count; i++)
        {
            EstadoPanelTemporal estado = lista[i];
            if (estado.panel == null || !estado.activoOriginal)
            {
                continue;
            }

            if (!estado.panel.activeSelf)
            {
                estado.panel.SetActive(true);
                estado.panel.transform.localScale = estado.escalaOriginal * 0.96f;
                estado.grupo.alpha = 0f;
            }

            PrepararPanelSoloComoVistaPrevia(estado);
            escalasInicio[i] = estado.panel.transform.localScale;
            alphasInicio[i] = estado.grupo.alpha;
        }

        float tiempo = 0f;
        while (tiempo < DuracionTransicionPaneles)
        {
            tiempo += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(tiempo / DuracionTransicionPaneles));

            for (int i = 0; i < lista.Count; i++)
            {
                EstadoPanelTemporal estado = lista[i];
                if (estado.panel == null || !estado.activoOriginal)
                {
                    continue;
                }

                estado.panel.transform.localScale = Vector3.LerpUnclamped(
                    escalasInicio[i],
                    estado.escalaOriginal,
                    t
                );
                estado.grupo.alpha = Mathf.LerpUnclamped(alphasInicio[i], estado.alphaOriginal, t);
            }

            yield return null;
        }

        for (int i = 0; i < lista.Count; i++)
        {
            FinalizarRestauracionPanel(lista[i]);
        }

        estadosPaneles.Clear();
        rutinaRestauracionPaneles = null;
    }

    private void FinalizarRestauracionPanel(EstadoPanelTemporal estado)
    {
        if (estado.panel == null)
        {
            return;
        }

        estado.panel.transform.localScale = estado.escalaOriginal;
        if (estado.grupo != null)
        {
            estado.grupo.alpha = estado.alphaOriginal;
            estado.grupo.interactable = estado.interactableOriginal;
            estado.grupo.blocksRaycasts = estado.blocksRaycastsOriginal;
        }

        estado.panel.SetActive(estado.activoOriginal);
    }

    private void PausarNivel()
    {
        if (pausaNivelAplicada)
        {
            return;
        }

        escalaTiempoAnterior = Time.timeScale;
        audioPausadoAnterior = AudioListener.pause;
        Time.timeScale = 0f;
        AudioListener.pause = true;
        pausaNivelAplicada = true;
    }

    private void ReanudarNivel()
    {
        if (!pausaNivelAplicada)
        {
            return;
        }

        Time.timeScale = escalaTiempoAnterior;
        AudioListener.pause = audioPausadoAnterior;
        pausaNivelAplicada = false;
    }

    private void ActualizarMedicionFps()
    {
        float delta = Time.unscaledDeltaTime;
        if (delta <= 0f || delta > 0.5f)
        {
            return;
        }

        cuadrosMuestraFps++;
        tiempoMuestraFps += delta;
        if (tiempoMuestraFps < 0.5f)
        {
            return;
        }

        fpsActualMedido = cuadrosMuestraFps / Mathf.Max(0.001f, tiempoMuestraFps);
        cuadrosMuestraFps = 0;
        tiempoMuestraFps = 0f;
        ActualizarTextoRendimiento();
    }

    private void ActualizarTextoRendimiento()
    {
        if (textoFpsActual != null)
        {
            textoFpsActual.text = fpsActualMedido > 0f
                ? "FPS actuales  " + Mathf.RoundToInt(fpsActualMedido)
                : "FPS actuales  Midiendo...";
        }

        if (textoFrecuenciaVisor != null)
        {
            float frecuencia = ajustes != null
                ? ajustes.ObtenerFrecuenciaPantallaActual()
                : 0f;
            string visor = frecuencia > 1f
                ? Mathf.RoundToInt(frecuencia) + " Hz"
                : "detectando";
            int objetivo = ajustes != null ? ajustes.FpsObjetivo : Application.targetFrameRate;
            textoFrecuenciaVisor.text = "Visor  " + visor + "     Objetivo  " + objetivo + " FPS";
        }
    }

    private void ColocarMenuFrenteAlUsuario()
    {
        Camera camara = ObtenerCamara();
        if (camara == null || canvasRect == null)
        {
            return;
        }

        Vector3 frente = Vector3.ProjectOnPlane(camara.transform.forward, Vector3.up);
        if (frente.sqrMagnitude < 0.001f)
        {
            frente = Vector3.forward;
        }

        frente.Normalize();
        canvasRect.position = camara.transform.position +
                              frente * Mathf.Max(0.4f, distanciaMenuUsuario) +
                              Vector3.up * offsetVerticalMenu;
        canvasRect.rotation = Quaternion.LookRotation(frente, Vector3.up);
        canvas.worldCamera = camara;
        PrepararGuiasAltura();
    }

    private Camera ObtenerCamara()
    {
        if (pocketManager != null && pocketManager.camaraJugador != null)
        {
            return pocketManager.camaraJugador;
        }

        return Camera.main;
    }

    private void PrepararGuiasAltura()
    {
        if (guiasAltura == null)
        {
            Transform existente = transform.Find("[GUIAS_ALTURA_PANELES]");
            if (existente != null)
            {
                guiasAltura = existente.GetComponent<AlgoLabHeightGuideRings>();
            }
        }

        if (guiasAltura == null)
        {
            GameObject root = new GameObject("[GUIAS_ALTURA_PANELES]");
            root.transform.SetParent(transform, false);
            guiasAltura = root.AddComponent<AlgoLabHeightGuideRings>();
        }

        Camera camara = ObtenerCamara();
        guiasAltura.Configurar(
            ajustes,
            camara != null ? camara.transform : null,
            canvas
        );
    }

    private void NotificarCambioAltura()
    {
        PrepararGuiasAltura();
        guiasAltura?.NotificarCambioAltura();
    }

    public void ReconstruirVistaPreviaEnEditor()
    {
        if (Application.isPlaying)
        {
            return;
        }

        CrearMenu(true);
    }

    private void CrearMenu(bool mostrarComoVistaPrevia)
    {
        Transform menuExistente = transform.Find("AlgoLabSettingsCanvas");

        if (menuExistente != null)
        {
            menuExistente.gameObject.SetActive(false);

            if (Application.isPlaying)
            {
                Destroy(menuExistente.gameObject);
            }
            else
            {
                DestroyImmediate(menuExistente.gameObject);
            }
        }

        vistas.Clear();
        botonesNavegacion.Clear();
        botonesPostura.Clear();
        botonesSalidaIA.Clear();
        botonesPerfil.Clear();
        botonesFps.Clear();

        menuRoot = new GameObject("AlgoLabSettingsCanvas", typeof(RectTransform));
        menuRoot.transform.SetParent(transform, false);
        canvasRect = menuRoot.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(1180f, 760f);
        canvasRect.localScale = Vector3.one * Mathf.Clamp(escalaMenuMundo, 0.0005f, 0.0015f);

        canvas = menuRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 80;

        CanvasScaler scaler = menuRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.dynamicPixelsPerUnit = 12f;
        menuRoot.AddComponent<GraphicRaycaster>();

        BoxCollider colliderMenu = menuRoot.AddComponent<BoxCollider>();
        colliderMenu.center = Vector3.zero;
        colliderMenu.size = new Vector3(
            canvasRect.sizeDelta.x,
            canvasRect.sizeDelta.y,
            Mathf.Max(2f, profundidadColliderMenu)
        );
        colliderMenu.isTrigger = false;

        PrepararGuiasAltura();

        Image fondo = CrearImagen("Fondo", canvasRect, Fondo);
        Estirar((RectTransform)fondo.transform, 0f, 0f, 0f, 0f);
        AgregarBorde(fondo.gameObject, ColorHex("3A4248"), 2f);

        Image lateral = CrearImagen("Navegacion", canvasRect, FondoLateral);
        RectTransform lateralRect = (RectTransform)lateral.transform;
        lateralRect.anchorMin = new Vector2(0f, 0f);
        lateralRect.anchorMax = new Vector2(0f, 1f);
        lateralRect.pivot = new Vector2(0f, 0.5f);
        lateralRect.anchoredPosition = Vector2.zero;
        lateralRect.sizeDelta = new Vector2(230f, 0f);

        TMP_Text marca = CrearTexto("Marca", lateralRect, "AlgoLab", 34f, Texto, TextAlignmentOptions.Left);
        Fijar((RectTransform)marca.transform, new Vector2(0f, 1f), new Vector2(28f, -34f), new Vector2(174f, 46f), new Vector2(0f, 1f));
        marca.fontStyle = FontStyles.Bold;

        TMP_Text etiqueta = CrearTexto("Etiqueta", lateralRect, "CONFIGURACIÓN", 14f, Verde, TextAlignmentOptions.Left);
        Fijar((RectTransform)etiqueta.transform, new Vector2(0f, 1f), new Vector2(29f, -80f), new Vector2(174f, 24f), new Vector2(0f, 1f));
        etiqueta.fontStyle = FontStyles.Bold;

        CrearBotonNavegacion(lateralRect, VistaMenu.Paneles, "Paneles", -132f);
        CrearBotonNavegacion(lateralRect, VistaMenu.Sonido, "Audio e IA", -198f);
        CrearBotonNavegacion(lateralRect, VistaMenu.Graficos, "Gráficos", -264f);
        CrearBotonNavegacion(lateralRect, VistaMenu.Sesion, "Sesión", -330f);
        CrearBotonNavegacion(lateralRect, VistaMenu.Ranking, "Ranking", -396f);

        TMP_Text version = CrearTexto("Version", lateralRect, "AlgoLab 1", 15f, TextoSuave, TextAlignmentOptions.Left);
        version.rectTransform.anchorMin = new Vector2(0f, 0f);
        version.rectTransform.anchorMax = new Vector2(0f, 0f);
        version.rectTransform.pivot = new Vector2(0f, 0f);
        version.rectTransform.anchoredPosition = new Vector2(29f, 24f);
        version.rectTransform.sizeDelta = new Vector2(170f, 24f);

        tituloVista = CrearTexto("TituloVista", canvasRect, "Paneles", 32f, Texto, TextAlignmentOptions.Left);
        Fijar((RectTransform)tituloVista.transform, new Vector2(0f, 1f), new Vector2(270f, -32f), new Vector2(730f, 50f), new Vector2(0f, 1f));
        tituloVista.fontStyle = FontStyles.Bold;

        Button cerrar = CrearBoton("BtnCerrar", canvasRect, "×", Banda, 34f);
        Fijar((RectTransform)cerrar.transform, new Vector2(1f, 1f), new Vector2(-30f, -28f), new Vector2(52f, 52f), new Vector2(1f, 1f));
        cerrar.onClick.AddListener(CerrarConfiguracion);

        RectTransform contenido = CrearRect("Contenido", canvasRect);
        contenido.anchorMin = Vector2.zero;
        contenido.anchorMax = Vector2.one;
        contenido.offsetMin = new Vector2(270f, 34f);
        contenido.offsetMax = new Vector2(-34f, -112f);

        CrearVistaPaneles(contenido);
        CrearVistaSonido(contenido);
        CrearVistaGraficos(contenido);
        CrearVistaSesion(contenido);
        CrearVistaRanking(contenido);

        MostrarVista(VistaMenu.Paneles);
        RegistrarMenuEnRayosControl();
        menuRoot.SetActive(mostrarComoVistaPrevia && !Application.isPlaying);
    }

    private void RegistrarMenuEnRayosControl()
    {
        if (!Application.isPlaying || canvasRect == null)
        {
            return;
        }

        PointerSelector[] selectores = FindObjectsByType<PointerSelector>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < selectores.Length; i++)
        {
            PointerSelector selector = selectores[i];
            if (selector == null)
            {
                continue;
            }

            selector.RegistrarPanelUI(canvasRect);
        }
    }

    private void DesregistrarMenuDeRayosControl()
    {
        if (!Application.isPlaying || canvasRect == null)
        {
            return;
        }

        PointerSelector[] selectores = FindObjectsByType<PointerSelector>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < selectores.Length; i++)
        {
            PointerSelector selector = selectores[i];
            if (selector == null)
            {
                continue;
            }

            selector.DesregistrarPanelUI(canvasRect);
        }
    }

    private void ActualizarInteraccionesVR()
    {
        AlgoLabVRUIButtonClicker[] clickers = FindObjectsByType<AlgoLabVRUIButtonClicker>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < clickers.Length; i++)
        {
            if (clickers[i] != null)
            {
                clickers[i].ActualizarListaInteractuables();
            }
        }
    }

    private void CrearBotonNavegacion(RectTransform parent, VistaMenu vista, string texto, float y)
    {
        Button boton = CrearBoton("Nav" + vista, parent, texto, FondoLateral, 20f);
        Fijar((RectTransform)boton.transform, new Vector2(0f, 1f), new Vector2(24f, y), new Vector2(182f, 52f), new Vector2(0f, 1f));
        boton.GetComponentInChildren<TMP_Text>().alignment = TextAlignmentOptions.Left;
        boton.onClick.AddListener(() => MostrarVista(vista));
        botonesNavegacion[vista] = boton;
    }

    private GameObject CrearPagina(string nombre, RectTransform parent, VistaMenu vista)
    {
        RectTransform pagina = CrearRect(nombre, parent);
        Estirar(pagina, 0f, 0f, 0f, 0f);
        vistas[vista] = pagina.gameObject;
        return pagina.gameObject;
    }

    private void CrearVistaPaneles(RectTransform parent)
    {
        GameObject pagina = CrearPagina("VistaPaneles", parent, VistaMenu.Paneles);
        RectTransform root = (RectTransform)pagina.transform;

        textoEstadoPostura = CrearTexto("EstadoPostura", root, "", 19f, Texto, TextAlignmentOptions.Left);
        Image estadoFondo = CrearImagen("EstadoFondo", root, Banda);
        Fijar((RectTransform)estadoFondo.transform, new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(870f, 66f), new Vector2(0f, 1f));
        textoEstadoPostura.transform.SetParent(estadoFondo.transform, false);
        Estirar((RectTransform)textoEstadoPostura.transform, 22f, 10f, 22f, 10f);

        CrearEtiqueta(root, "Modo de postura", -82f);
        string[] modos = { "Automático", "Sentado", "De pie" };
        const float anchoBotonPostura = 283.333f;
        for (int i = 0; i < modos.Length; i++)
        {
            int indice = i;
            Button boton = CrearBoton("Postura" + i, root, modos[i], Banda, 18f);
            Fijar(
                (RectTransform)boton.transform,
                new Vector2(0f, 1f),
                new Vector2(i * (anchoBotonPostura + 10f), -116f),
                new Vector2(anchoBotonPostura, 48f),
                new Vector2(0f, 1f)
            );
            boton.onClick.AddListener(() => ajustes?.SetModoPostura(indice));
            botonesPostura.Add(boton);
        }

        CrearEtiqueta(root, "Alturas de calibración", -182f);

        sliderAlturaSentado = CrearSliderFila(
            root,
            "Altura sentado",
            -214f,
            AlturaSentadoMinUI,
            AlturaSentadoMaxUI,
            out valorAlturaSentado,
            valor =>
            {
                ajustes?.SetAlturaSentado(valor);
                NotificarCambioAltura();
            }
        );

        sliderAlturaParado = CrearSliderFila(
            root,
            "Altura de pie",
            -288f,
            AlturaParadoMinUI,
            AlturaParadoMaxUI,
            out valorAlturaParado,
            valor =>
            {
                ajustes?.SetAlturaParado(valor);
                NotificarCambioAltura();
            }
        );

        CrearEtiqueta(root, "Vista previa y posición", -370f);

        botonMostrarPaneles = CrearBoton("BtnMostrarPaneles", root, "[ ] Mostrar paneles", Banda, 18f);
        Fijar((RectTransform)botonMostrarPaneles.transform, new Vector2(0f, 1f), new Vector2(0f, -404f), new Vector2(280f, 48f), new Vector2(0f, 1f));
        botonMostrarPaneles.onClick.AddListener(AlternarVistaPreviaPaneles);

        botonSuavizado = CrearBoton("BtnSuavizado", root, "Suavizado", Banda, 18f);
        Fijar((RectTransform)botonSuavizado.transform, new Vector2(0f, 1f), new Vector2(300f, -404f), new Vector2(280f, 48f), new Vector2(0f, 1f));
        botonSuavizado.onClick.AddListener(() =>
        {
            if (ajustes != null) ajustes.SetSuavizarAltura(!ajustes.SuavizarAltura);
        });

        Button recolocar = CrearBoton("BtnRecolocar", root, "Recolocar paneles", VerdeOscuro, 18f);
        Fijar((RectTransform)recolocar.transform, new Vector2(0f, 1f), new Vector2(600f, -404f), new Vector2(270f, 48f), new Vector2(0f, 1f));
        recolocar.onClick.AddListener(() => ajustes?.RecolocarPaneles());
    }

    private void CrearVistaSonido(RectTransform parent)
    {
        GameObject pagina = CrearPagina("VistaSonido", parent, VistaMenu.Sonido);
        RectTransform root = (RectTransform)pagina.transform;

        CrearCabeceraBanda(root, "Mezcla de audio", "Voz y efectos", -4f);

        sliderVolumenGeneral = CrearSliderFila(
            root,
            "Volumen general",
            -82f,
            0f,
            1f,
            out valorVolumenGeneral,
            valor => ajustes?.SetVolumenGeneral(valor)
        );

        sliderVolumenVoz = CrearSliderFila(
            root,
            "Narración e IA",
            -154f,
            0f,
            1f,
            out valorVolumenVoz,
            valor => ajustes?.SetVolumenVoz(valor)
        );

        sliderVolumenEfectos = CrearSliderFila(
            root,
            "Efectos",
            -226f,
            0f,
            1f,
            out valorVolumenEfectos,
            valor => ajustes?.SetVolumenEfectos(valor)
        );

        CrearEtiqueta(root, "Respuesta de la IA", -308f);
        string[] salidasIA = { "Subtítulos", "Audio", "Ambos" };
        const float anchoBotonSalidaIA = 283.333f;
        for (int i = 0; i < salidasIA.Length; i++)
        {
            int indice = i;
            Button boton = CrearBoton("SalidaIA" + i, root, salidasIA[i], Banda, 18f);
            Fijar(
                (RectTransform)boton.transform,
                new Vector2(0f, 1f),
                new Vector2(i * (anchoBotonSalidaIA + 10f), -344f),
                new Vector2(anchoBotonSalidaIA, 50f),
                new Vector2(0f, 1f)
            );
            boton.onClick.AddListener(() => ajustes?.SetModoSalidaIA(indice));
            botonesSalidaIA.Add(boton);
        }

        Image explicacion = CrearImagen("AyudaSalidaIA", root, BandaAlterna);
        Fijar((RectTransform)explicacion.transform, new Vector2(0f, 1f), new Vector2(0f, -414f), new Vector2(870f, 68f), new Vector2(0f, 1f));
        TMP_Text textoExplicacion = CrearTexto(
            "TextoAyudaSalidaIA",
            explicacion.transform,
            "Elige si la respuesta aparece como subtítulos, se reproduce por voz o usa las dos opciones.",
            16f,
            TextoSuave,
            TextAlignmentOptions.Left
        );
        Estirar((RectTransform)textoExplicacion.transform, 20f, 10f, 20f, 10f);
    }

    private void CrearVistaGraficos(RectTransform parent)
    {
        GameObject pagina = CrearPagina("VistaGraficos", parent, VistaMenu.Graficos);
        RectTransform root = (RectTransform)pagina.transform;

        Image estadoFps = CrearImagen("EstadoFPS", root, Banda);
        Fijar((RectTransform)estadoFps.transform, new Vector2(0f, 1f), Vector2.zero, new Vector2(870f, 66f), new Vector2(0f, 1f));
        textoFpsActual = CrearTexto("FpsActual", estadoFps.transform, "FPS actuales  Midiendo...", 20f, Texto, TextAlignmentOptions.Left);
        Fijar((RectTransform)textoFpsActual.transform, new Vector2(0f, 0.5f), new Vector2(22f, 0f), new Vector2(300f, 42f), new Vector2(0f, 0.5f));
        textoFpsActual.fontStyle = FontStyles.Bold;
        textoFrecuenciaVisor = CrearTexto("FrecuenciaVisor", estadoFps.transform, "Visor  detectando", 17f, Verde, TextAlignmentOptions.Right);
        Fijar((RectTransform)textoFrecuenciaVisor.transform, new Vector2(1f, 0.5f), new Vector2(-22f, 0f), new Vector2(450f, 42f), new Vector2(1f, 0.5f));

        CrearEtiqueta(root, "Perfil gráfico", -86f);
        string[] perfiles = { "Rendimiento", "Equilibrado", "Calidad" };
        const float anchoBotonGrafico = 283.333f;
        for (int i = 0; i < perfiles.Length; i++)
        {
            int indice = i;
            Button boton = CrearBoton("Perfil" + i, root, perfiles[i], Banda, 18f);
            Fijar(
                (RectTransform)boton.transform,
                new Vector2(0f, 1f),
                new Vector2(i * (anchoBotonGrafico + 10f), -120f),
                new Vector2(anchoBotonGrafico, 50f),
                new Vector2(0f, 1f)
            );
            boton.onClick.AddListener(() => ajustes?.SetPerfilGrafico(indice));
            botonesPerfil.Add(boton);
        }

        sliderEscalaRender = CrearSliderFila(
            root,
            "Resolución VR",
            -194f,
            0.75f,
            1.2f,
            out valorEscalaRender,
            valor => ajustes?.SetEscalaRender(valor)
        );

        CrearEtiqueta(root, "Frecuencia objetivo del visor", -278f);
        int[] opcionesFps = { 60, 66, 72 };
        for (int i = 0; i < opcionesFps.Length; i++)
        {
            int fps = opcionesFps[i];
            Button boton = CrearBoton("Fps" + fps, root, fps + " FPS", Banda, 20f);
            Fijar(
                (RectTransform)boton.transform,
                new Vector2(0f, 1f),
                new Vector2(i * (anchoBotonGrafico + 10f), -314f),
                new Vector2(anchoBotonGrafico, 50f),
                new Vector2(0f, 1f)
            );
            boton.onClick.AddListener(() => ajustes?.SetFpsObjetivo(fps));
            botonesFps.Add(boton);
        }

        Image ayuda = CrearImagen("AyudaRendimiento", root, BandaAlterna);
        Fijar((RectTransform)ayuda.transform, new Vector2(0f, 1f), new Vector2(0f, -384f), new Vector2(870f, 72f), new Vector2(0f, 1f));
        TMP_Text textoAyuda = CrearTexto(
            "TextoAyudaRendimiento",
            ayuda.transform,
            "El perfil cambia la calidad y resolución. Los FPS cambian el límite de renderizado y la frecuencia real del visor Quest.",
            16f,
            TextoSuave,
            TextAlignmentOptions.Left
        );
        Estirar((RectTransform)textoAyuda.transform, 20f, 10f, 20f, 10f);
    }

    private void CrearVistaSesion(RectTransform parent)
    {
        GameObject pagina = CrearPagina("VistaSesion", parent, VistaMenu.Sesion);
        RectTransform root = (RectTransform)pagina.transform;

        Image identidad = CrearImagen("Identidad", root, Banda);
        Fijar((RectTransform)identidad.transform, new Vector2(0f, 1f), Vector2.zero, new Vector2(870f, 128f), new Vector2(0f, 1f));
        TMP_Text etiquetaCuenta = CrearTexto("EtiquetaCuenta", identidad.transform, "CUENTA ACTUAL", 13f, Verde, TextAlignmentOptions.Left);
        Fijar((RectTransform)etiquetaCuenta.transform, new Vector2(0f, 1f), new Vector2(24f, -14f), new Vector2(300f, 24f), new Vector2(0f, 1f));
        etiquetaCuenta.fontStyle = FontStyles.Bold;
        textoSesion = CrearTexto(
            "DatosSesion",
            identidad.transform,
            "Usuario de AlgoLab\ncorreo@ejemplo.com  ·  ESTUDIANTE",
            20f,
            Texto,
            TextAlignmentOptions.Left
        );
        Fijar((RectTransform)textoSesion.transform, new Vector2(0f, 1f), new Vector2(24f, -42f), new Vector2(820f, 70f), new Vector2(0f, 1f));

        CrearEtiqueta(root, "Resumen de progreso", -148f);
        textoSesionNivel = CrearTarjetaDatoSesion(root, "DatoNivel", "NIVEL", "Selector", 0f);
        textoSesionPuntaje = CrearTarjetaDatoSesion(root, "DatoPuntaje", "PUNTAJE", "0", 295f);
        textoSesionEstado = CrearTarjetaDatoSesion(root, "DatoEstado", "ESTADO", "Sin nivel activo", 590f);

        CrearEtiqueta(root, "Acciones", -286f);
        botonRankingSesion = CrearBoton("BtnRanking", root, "Ver ranking", VerdeOscuro, 20f);
        Fijar((RectTransform)botonRankingSesion.transform, new Vector2(0f, 1f), new Vector2(0f, -320f), new Vector2(870f, 56f), new Vector2(0f, 1f));
        botonRankingSesion.onClick.AddListener(() => MostrarVista(VistaMenu.Ranking));

        botonSalirNivel = CrearBoton("BtnSalirNivel", root, "Salir del nivel", Amarillo, 19f);
        Fijar((RectTransform)botonSalirNivel.transform, new Vector2(0f, 1f), new Vector2(440f, -320f), new Vector2(430f, 56f), new Vector2(0f, 1f));
        botonSalirNivel.onClick.AddListener(SalirDelNivelYMostrarSelector);
        botonSalirNivel.gameObject.SetActive(false);

        CrearEtiqueta(root, "Cuenta y configuración", -404f);
        Button restablecer = CrearBoton("BtnRestablecer", root, "Restablecer configuración", Banda, 18f);
        Fijar((RectTransform)restablecer.transform, new Vector2(0f, 1f), new Vector2(0f, -438f), new Vector2(425f, 56f), new Vector2(0f, 1f));
        restablecer.onClick.AddListener(() => ajustes?.RestablecerPredeterminados());

        Button cerrarSesion = CrearBoton("BtnCerrarSesion", root, "Cerrar sesión", Rojo, 19f);
        Fijar((RectTransform)cerrarSesion.transform, new Vector2(0f, 1f), new Vector2(445f, -438f), new Vector2(425f, 56f), new Vector2(0f, 1f));
        textoBotonCerrarSesion = cerrarSesion.GetComponentInChildren<TMP_Text>();
        cerrarSesion.onClick.AddListener(CerrarSesionYVolverInicio);
    }

    private TMP_Text CrearTarjetaDatoSesion(
        RectTransform parent,
        string nombre,
        string etiqueta,
        string valorInicial,
        float x)
    {
        Image tarjeta = CrearImagen(nombre, parent, BandaAlterna);
        Fijar((RectTransform)tarjeta.transform, new Vector2(0f, 1f), new Vector2(x, -180f), new Vector2(280f, 88f), new Vector2(0f, 1f));

        TMP_Text titulo = CrearTexto("Etiqueta", tarjeta.transform, etiqueta, 13f, TextoSuave, TextAlignmentOptions.Left);
        Fijar((RectTransform)titulo.transform, new Vector2(0f, 1f), new Vector2(18f, -12f), new Vector2(240f, 22f), new Vector2(0f, 1f));
        titulo.fontStyle = FontStyles.Bold;

        TMP_Text valor = CrearTexto("Valor", tarjeta.transform, valorInicial, 22f, Texto, TextAlignmentOptions.Left);
        Fijar((RectTransform)valor.transform, new Vector2(0f, 1f), new Vector2(18f, -38f), new Vector2(244f, 36f), new Vector2(0f, 1f));
        valor.fontStyle = FontStyles.Bold;
        return valor;
    }

    private void CrearVistaRanking(RectTransform parent)
    {
        GameObject pagina = CrearPagina("VistaRanking", parent, VistaMenu.Ranking);
        RectTransform root = (RectTransform)pagina.transform;

        textoEstadoRanking = CrearTexto("EstadoRanking", root, "Pulsa actualizar", 17f, TextoSuave, TextAlignmentOptions.Left);
        Fijar((RectTransform)textoEstadoRanking.transform, new Vector2(0f, 1f), new Vector2(0f, -4f), new Vector2(500f, 34f), new Vector2(0f, 1f));

        Button actualizar = CrearBoton("BtnActualizarRanking", root, "Actualizar", VerdeOscuro, 17f);
        Fijar((RectTransform)actualizar.transform, new Vector2(1f, 1f), new Vector2(-4f, 0f), new Vector2(180f, 44f), new Vector2(1f, 1f));
        actualizar.onClick.AddListener(CargarRanking);

        Image cabecera = CrearImagen("CabeceraTabla", root, Banda);
        Fijar((RectTransform)cabecera.transform, new Vector2(0f, 1f), new Vector2(0f, -64f), new Vector2(870f, 48f), new Vector2(0f, 1f));
        CrearCeldaTabla(cabecera.transform, "POS.", 0f, 90f, TextAlignmentOptions.Center, true, TextoSuave);
        CrearCeldaTabla(cabecera.transform, "ESTUDIANTE", 90f, 490f, TextAlignmentOptions.Left, true, TextoSuave);
        CrearCeldaTabla(cabecera.transform, "NIVEL", 580f, 120f, TextAlignmentOptions.Center, true, TextoSuave);
        CrearCeldaTabla(cabecera.transform, "PUNTAJE", 700f, 150f, TextAlignmentOptions.Center, true, TextoSuave);

        RectTransform scrollRoot = CrearRect("RankingScroll", root);
        Fijar(scrollRoot, new Vector2(0f, 1f), new Vector2(0f, -122f), new Vector2(870f, 430f), new Vector2(0f, 1f));
        scrollRanking = scrollRoot.gameObject.AddComponent<ScrollRect>();
        scrollRanking.horizontal = false;
        scrollRanking.vertical = true;
        scrollRanking.movementType = ScrollRect.MovementType.Clamped;
        scrollRanking.scrollSensitivity = 24f;

        RectTransform viewport = CrearRect("Viewport", scrollRoot);
        Estirar(viewport, 0f, 0f, 12f, 0f);
        Image viewportImage = viewport.gameObject.AddComponent<Image>();
        viewportImage.color = new Color(0f, 0f, 0f, 0.01f);
        viewport.gameObject.AddComponent<RectMask2D>();

        contenidoRanking = CrearRect("Contenido", viewport);
        contenidoRanking.anchorMin = new Vector2(0f, 1f);
        contenidoRanking.anchorMax = new Vector2(1f, 1f);
        contenidoRanking.pivot = new Vector2(0.5f, 1f);
        contenidoRanking.anchoredPosition = Vector2.zero;
        contenidoRanking.sizeDelta = Vector2.zero;

        VerticalLayoutGroup layout = contenidoRanking.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 6f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlHeight = false;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        ContentSizeFitter fitter = contenidoRanking.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRanking.viewport = viewport;
        scrollRanking.content = contenidoRanking;

        Button subir = CrearBoton("BtnSubirRanking", root, "▲", Banda, 20f);
        Fijar((RectTransform)subir.transform, new Vector2(1f, 1f), new Vector2(-106f, -578f), new Vector2(82f, 42f), new Vector2(1f, 1f));
        subir.onClick.AddListener(() => DesplazarRanking(0.24f));

        Button bajar = CrearBoton("BtnBajarRanking", root, "▼", Banda, 20f);
        Fijar((RectTransform)bajar.transform, new Vector2(1f, 1f), new Vector2(-4f, -578f), new Vector2(82f, 42f), new Vector2(1f, 1f));
        bajar.onClick.AddListener(() => DesplazarRanking(-0.24f));
    }

    private void MostrarVista(VistaMenu vista)
    {
        vistaActual = vista;

        foreach (KeyValuePair<VistaMenu, GameObject> par in vistas)
        {
            par.Value.SetActive(par.Key == vista);
        }

        foreach (KeyValuePair<VistaMenu, Button> par in botonesNavegacion)
        {
            AplicarColorBoton(par.Value, par.Key == vista ? VerdeOscuro : FondoLateral);
            TMP_Text label = par.Value.GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                label.color = par.Key == vista ? Texto : TextoSuave;
                label.fontStyle = par.Key == vista ? FontStyles.Bold : FontStyles.Normal;
            }
        }

        tituloVista.text = TituloParaVista(vista);

        if (vista == VistaMenu.Ranking)
        {
            CargarRanking();
        }

        ActualizarVistaPreviaPaneles(true);

        RefrescarControles();
    }

    private string TituloParaVista(VistaMenu vista)
    {
        switch (vista)
        {
            case VistaMenu.Sonido: return "Audio e IA";
            case VistaMenu.Graficos: return "Gráficos";
            case VistaMenu.Sesion: return "Sesión";
            case VistaMenu.Ranking: return "Ranking de estudiantes";
            default: return "Paneles";
        }
    }

    private void RefrescarControles()
    {
        if (ajustes == null)
        {
            return;
        }

        SincronizarLimitesSlidersAltura();
        AjustarSlider(sliderAlturaSentado, valorAlturaSentado, ajustes.AlturaSentado, "0.00 m");
        AjustarSlider(sliderAlturaParado, valorAlturaParado, ajustes.AlturaParado, "0.00 m");
        AjustarSlider(sliderVolumenGeneral, valorVolumenGeneral, ajustes.VolumenGeneral, "0%");
        AjustarSlider(sliderVolumenVoz, valorVolumenVoz, ajustes.VolumenVoz, "0%");
        AjustarSlider(sliderVolumenEfectos, valorVolumenEfectos, ajustes.VolumenEfectos, "0%");
        AjustarSlider(sliderEscalaRender, valorEscalaRender, ajustes.EscalaRender, "0.00x");

        ActualizarGrupoSegmentado(botonesPostura, ajustes.ModoPostura);
        ActualizarGrupoSegmentado(botonesSalidaIA, ajustes.ModoSalidaIA);
        ActualizarGrupoSegmentado(botonesPerfil, ajustes.PerfilGrafico);

        for (int i = 0; i < botonesFps.Count; i++)
        {
            int fps = i == 0 ? 60 : i == 1 ? 66 : 72;
            AplicarColorBoton(botonesFps[i], ajustes.FpsObjetivo == fps ? VerdeOscuro : Banda);
        }

        if (botonSuavizado != null)
        {
            AplicarColorBoton(botonSuavizado, ajustes.SuavizarAltura ? VerdeOscuro : Banda);
            botonSuavizado.GetComponentInChildren<TMP_Text>().text =
                ajustes.SuavizarAltura ? "Suavizado: Activado" : "Suavizado: Desactivado";
        }

        if (botonMostrarPaneles != null)
        {
            AplicarColorBoton(botonMostrarPaneles, mostrarPanelesCalibracion ? VerdeOscuro : Banda);
            TMP_Text label = botonMostrarPaneles.GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                label.text = mostrarPanelesCalibracion
                    ? "[X] Mostrar paneles"
                    : "[ ] Mostrar paneles";
            }
        }

        ActualizarEstadoPostura();
        ActualizarTextoRendimiento();
        ActualizarDatosSesion();
    }

    private void SincronizarLimitesSlidersAltura()
    {
        if (sliderAlturaSentado != null)
        {
            sliderAlturaSentado.minValue = AlturaSentadoMinUI;
            sliderAlturaSentado.maxValue = Mathf.Clamp(
                ajustes.AlturaParado,
                AlturaSentadoMinUI,
                AlturaSentadoMaxUI
            );
        }

        if (sliderAlturaParado != null)
        {
            sliderAlturaParado.minValue = Mathf.Clamp(
                ajustes.AlturaSentado,
                AlturaParadoMinUI,
                AlturaParadoMaxUI
            );
            sliderAlturaParado.maxValue = AlturaParadoMaxUI;
        }
    }

    private void AjustarSlider(Slider slider, TMP_Text valorTexto, float valor, string formato)
    {
        if (slider != null)
        {
            slider.SetValueWithoutNotify(valor);
        }

        if (valorTexto != null)
        {
            if (formato == "0%")
            {
                valorTexto.text = Mathf.RoundToInt(valor * 100f) + "%";
            }
            else
            {
                valorTexto.text = valor.ToString(formato);
            }
        }
    }

    private void ActualizarGrupoSegmentado(List<Button> botones, int seleccionado)
    {
        for (int i = 0; i < botones.Count; i++)
        {
            AplicarColorBoton(botones[i], i == seleccionado ? VerdeOscuro : Banda);
        }
    }

    private void ActualizarEstadoPostura()
    {
        if (textoEstadoPostura == null)
        {
            return;
        }

        if (panelSpawnManager == null)
        {
            textoEstadoPostura.text = "Postura: sin referencia de altura";
            return;
        }

        string modo;
        if (ajustes == null || ajustes.ModoPostura == 0)
        {
            modo = panelSpawnManager.PosturaDetectadaParado ? "De pie" : "Sentado";
        }
        else
        {
            modo = ajustes.ModoPostura == 2 ? "De pie (manual)" : "Sentado (manual)";
        }

        textoEstadoPostura.text =
            "Postura: " + modo +
            "     Ojos: " + panelSpawnManager.AlturaOjosActual.ToString("0.00") + " m" +
            "     Paneles: " + panelSpawnManager.AlturaPanelesActual.ToString("0.00") + " m";
    }

    private void ActualizarDatosSesion()
    {
        if (textoSesion == null)
        {
            return;
        }

        bool haySesion = sessionManager != null;
        bool esInvitado = !haySesion || sessionManager.ModoInvitado;
        string nombre = !haySesion || string.IsNullOrWhiteSpace(sessionManager.NombreUsuario)
            ? "Invitado"
            : sessionManager.NombreUsuario;
        string correo = !haySesion || string.IsNullOrWhiteSpace(sessionManager.CorreoUsuario)
            ? "Progreso local"
            : sessionManager.CorreoUsuario;
        string rol = !haySesion || string.IsNullOrWhiteSpace(sessionManager.RolUsuario)
            ? "INVITADO"
            : sessionManager.RolUsuario.ToUpperInvariant();
        int nivelDisponible = haySesion ? Mathf.Max(1, sessionManager.NivelActual) : 1;
        int puntaje = haySesion ? Mathf.Max(0, sessionManager.Puntaje) : 0;
        int nivelActivo = progressPanel != null
            ? progressPanel.ObtenerNivelActivoRealActual()
            : -1;
        bool estaDentroDeNivel = nivelActivo > 0;

        textoSesion.text = nombre + "\n" + correo + "  ·  " + rol;

        if (textoSesionNivel != null)
        {
            textoSesionNivel.text = estaDentroDeNivel
                ? "Nivel " + nivelActivo
                : "Nivel " + nivelDisponible;
        }

        if (textoSesionPuntaje != null)
        {
            textoSesionPuntaje.text = puntaje.ToString("N0");
        }

        if (textoSesionEstado != null)
        {
            textoSesionEstado.text = estaDentroDeNivel
                ? "En curso"
                : "Selector disponible";
            textoSesionEstado.color = estaDentroDeNivel ? Amarillo : Verde;
        }

        if (botonSalirNivel != null)
        {
            botonSalirNivel.gameObject.SetActive(estaDentroDeNivel);
        }

        if (botonRankingSesion != null)
        {
            float anchoRanking = estaDentroDeNivel ? 425f : 870f;
            Fijar(
                (RectTransform)botonRankingSesion.transform,
                new Vector2(0f, 1f),
                new Vector2(0f, -320f),
                new Vector2(anchoRanking, 56f),
                new Vector2(0f, 1f)
            );
        }

        if (textoBotonCerrarSesion != null)
        {
            textoBotonCerrarSesion.text = esInvitado
                ? "Salir al menú principal"
                : "Cerrar sesión";
        }
    }

    private void CargarRanking()
    {
        BuscarReferencias();
        int solicitudActual = ++generacionRanking;

        if (textoEstadoRanking != null)
        {
            textoEstadoRanking.text = "Actualizando...";
            textoEstadoRanking.color = TextoSuave;
        }

        if (backendClient == null)
        {
            MostrarErrorRanking("Backend no disponible");
            return;
        }

        backendClient.ConsultarRanking((ok, mensaje, respuesta) =>
        {
            if (this == null ||
                solicitudActual != generacionRanking ||
                !menuAbierto ||
                vistaActual != VistaMenu.Ranking)
            {
                return;
            }

            if (!ok || respuesta == null)
            {
                MostrarErrorRanking("No se pudo cargar el ranking");
                return;
            }

            DibujarRanking(respuesta.estudiantes);
            textoEstadoRanking.text = respuesta.total == 1
                ? "1 estudiante"
                : respuesta.total + " estudiantes";
            textoEstadoRanking.color = TextoSuave;
        });
    }

    private void DesplazarRanking(float delta)
    {
        if (scrollRanking == null)
        {
            return;
        }

        scrollRanking.verticalNormalizedPosition = Mathf.Clamp01(
            scrollRanking.verticalNormalizedPosition + delta
        );
    }

    private void MostrarErrorRanking(string mensaje)
    {
        if (textoEstadoRanking != null)
        {
            textoEstadoRanking.text = mensaje;
            textoEstadoRanking.color = Rojo;
        }

        DibujarRanking(Array.Empty<AlgoLabBackendClient.RankingEstudianteDTO>());
    }

    private void DibujarRanking(AlgoLabBackendClient.RankingEstudianteDTO[] estudiantes)
    {
        if (contenidoRanking == null)
        {
            return;
        }

        for (int i = contenidoRanking.childCount - 1; i >= 0; i--)
        {
            Destroy(contenidoRanking.GetChild(i).gameObject);
        }

        if (estudiantes == null || estudiantes.Length == 0)
        {
            TMP_Text vacio = CrearTexto("RankingVacio", contenidoRanking, "Aún no hay puntajes registrados.", 19f, TextoSuave, TextAlignmentOptions.Center);
            LayoutElement layoutVacio = vacio.gameObject.AddComponent<LayoutElement>();
            layoutVacio.preferredHeight = 80f;
            return;
        }

        int usuarioActual = sessionManager != null ? sessionManager.UsuarioId : 0;

        for (int i = 0; i < estudiantes.Length; i++)
        {
            AlgoLabBackendClient.RankingEstudianteDTO estudiante = estudiantes[i];
            bool esActual = usuarioActual > 0 && estudiante.usuarioId == usuarioActual;
            Color fondoFila = esActual
                ? new Color(VerdeOscuro.r, VerdeOscuro.g, VerdeOscuro.b, 0.72f)
                : (i % 2 == 0 ? Banda : BandaAlterna);

            Image fila = CrearImagen("Puesto" + estudiante.posicion, contenidoRanking, fondoFila);
            LayoutElement layout = fila.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = 58f;
            layout.minHeight = 58f;

            Color colorPuesto = Texto;
            if (estudiante.posicion == 1) colorPuesto = Amarillo;
            else if (estudiante.posicion == 2) colorPuesto = Plata;
            else if (estudiante.posicion == 3) colorPuesto = Bronce;

            string nombre = !string.IsNullOrWhiteSpace(estudiante.nombre)
                ? estudiante.nombre
                : estudiante.nombreUsuario;

            CrearCeldaTabla(fila.transform, estudiante.posicion.ToString(), 0f, 90f, TextAlignmentOptions.Center, true, colorPuesto);
            CrearCeldaTabla(fila.transform, nombre, 90f, 490f, TextAlignmentOptions.Left, esActual, Texto);
            CrearCeldaTabla(fila.transform, estudiante.nivelActual.ToString(), 580f, 120f, TextAlignmentOptions.Center, false, Texto);
            CrearCeldaTabla(fila.transform, estudiante.puntaje.ToString("N0"), 700f, 150f, TextAlignmentOptions.Center, true, Texto);
        }
    }

    private void SalirDelNivelYMostrarSelector()
    {
        BuscarReferencias();
        AlgoLabProgressPanel selector = progressPanel;
        CerrarConfiguracion();

        if (selector != null)
        {
            selector.SalirDelNivelActual();
        }
        else
        {
            Debug.LogWarning("CONFIGURACIÓN: no se encontró el panel selector de niveles.");
        }
    }

    private void CerrarSesionYVolverInicio()
    {
        CerrarConfiguracion();
        BuscarReferencias();

        if (startUIController != null)
        {
            startUIController.CerrarSesionYVolverInicio();
            return;
        }

        if (sessionManager != null)
        {
            sessionManager.CerrarSesion();
        }

        AlgoLabGameAccessController acceso =
            FindFirstObjectByType<AlgoLabGameAccessController>(FindObjectsInactive.Include);
        if (acceso != null)
        {
            acceso.BloquearAccesoJuego();
        }
    }

    private Slider CrearSliderFila(
        RectTransform parent,
        string etiqueta,
        float y,
        float minimo,
        float maximo,
        out TMP_Text valorTexto,
        UnityEngine.Events.UnityAction<float> alCambiar)
    {
        Image fila = CrearImagen("Fila" + etiqueta, parent, BandaAlterna);
        Fijar((RectTransform)fila.transform, new Vector2(0f, 1f), new Vector2(0f, y), new Vector2(870f, 64f), new Vector2(0f, 1f));

        TMP_Text label = CrearTexto("Label", fila.transform, etiqueta, 18f, Texto, TextAlignmentOptions.Left);
        Fijar((RectTransform)label.transform, new Vector2(0f, 0.5f), new Vector2(20f, 0f), new Vector2(230f, 42f), new Vector2(0f, 0.5f));

        RectTransform sliderRoot = CrearRect("Slider", fila.transform);
        Fijar(sliderRoot, new Vector2(0f, 0.5f), new Vector2(312f, 0f), new Vector2(310f, 40f), new Vector2(0f, 0.5f));

        Slider slider = sliderRoot.gameObject.AddComponent<Slider>();
        slider.minValue = minimo;
        slider.maxValue = maximo;
        slider.wholeNumbers = false;

        Image fondoSlider = CrearImagen("Background", sliderRoot, ColorHex("4A535A"));
        Fijar((RectTransform)fondoSlider.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(286f, 8f));

        RectTransform fillArea = CrearRect("Fill Area", sliderRoot);
        Estirar(fillArea, 10f, 11f, 10f, 11f);
        Image fill = CrearImagen("Fill", fillArea, Verde);
        Estirar((RectTransform)fill.transform, 0f, 0f, 0f, 0f);

        RectTransform handleArea = CrearRect("Handle Slide Area", sliderRoot);
        Estirar(handleArea, 10f, 0f, 10f, 0f);
        Image handle = CrearImagen("Handle", handleArea, Texto);
        Fijar((RectTransform)handle.transform, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(24f, 24f));

        slider.fillRect = (RectTransform)fill.transform;
        slider.handleRect = (RectTransform)handle.transform;
        slider.targetGraphic = handle;
        slider.direction = Slider.Direction.LeftToRight;
        slider.onValueChanged.AddListener(valor =>
        {
            alCambiar?.Invoke(valor);
        });

        const float paso = 0.05f;
        Button disminuir = CrearBoton("Disminuir", fila.transform, "−", Banda, 22f);
        Fijar((RectTransform)disminuir.transform, new Vector2(0f, 0.5f), new Vector2(260f, 0f), new Vector2(42f, 42f), new Vector2(0f, 0.5f));
        disminuir.onClick.AddListener(() => slider.value = Mathf.Max(slider.minValue, slider.value - paso));

        Button aumentar = CrearBoton("Aumentar", fila.transform, "+", Banda, 22f);
        Fijar((RectTransform)aumentar.transform, new Vector2(0f, 0.5f), new Vector2(632f, 0f), new Vector2(42f, 42f), new Vector2(0f, 0.5f));
        aumentar.onClick.AddListener(() => slider.value = Mathf.Min(slider.maxValue, slider.value + paso));

        valorTexto = CrearTexto("Valor", fila.transform, "", 18f, Verde, TextAlignmentOptions.Center);
        Fijar((RectTransform)valorTexto.transform, new Vector2(1f, 0.5f), new Vector2(-20f, 0f), new Vector2(120f, 42f), new Vector2(1f, 0.5f));
        valorTexto.fontStyle = FontStyles.Bold;

        return slider;
    }

    private void CrearCabeceraBanda(RectTransform parent, string izquierda, string derecha, float y)
    {
        Image banda = CrearImagen("Cabecera" + izquierda, parent, Banda);
        Fijar((RectTransform)banda.transform, new Vector2(0f, 1f), new Vector2(0f, y), new Vector2(870f, 64f), new Vector2(0f, 1f));
        TMP_Text izq = CrearTexto("Izquierda", banda.transform, izquierda, 20f, Texto, TextAlignmentOptions.Left);
        Estirar((RectTransform)izq.transform, 22f, 10f, 160f, 10f);
        izq.fontStyle = FontStyles.Bold;
        TMP_Text der = CrearTexto("Derecha", banda.transform, derecha, 17f, TextoSuave, TextAlignmentOptions.Right);
        Estirar((RectTransform)der.transform, 650f, 10f, 22f, 10f);
    }

    private void CrearEtiqueta(RectTransform parent, string texto, float y)
    {
        TMP_Text label = CrearTexto("Label" + texto, parent, texto.ToUpperInvariant(), 15f, TextoSuave, TextAlignmentOptions.Left);
        Fijar((RectTransform)label.transform, new Vector2(0f, 1f), new Vector2(0f, y), new Vector2(500f, 28f), new Vector2(0f, 1f));
        label.fontStyle = FontStyles.Bold;
    }

    private TMP_Text CrearCeldaTabla(
        Transform parent,
        string texto,
        float x,
        float ancho,
        TextAlignmentOptions alineacion,
        bool negrita,
        Color color)
    {
        TMP_Text celda = CrearTexto("Celda", parent, texto, 17f, color, alineacion);
        RectTransform rect = (RectTransform)celda.transform;
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = new Vector2(x, 0f);
        rect.sizeDelta = new Vector2(ancho, 0f);
        rect.offsetMin = new Vector2(rect.offsetMin.x + 12f, 6f);
        rect.offsetMax = new Vector2(rect.offsetMax.x - 12f, -6f);
        celda.fontStyle = negrita ? FontStyles.Bold : FontStyles.Normal;
        celda.overflowMode = TextOverflowModes.Ellipsis;
        celda.textWrappingMode = TextWrappingModes.NoWrap;
        return celda;
    }

    private Button CrearBoton(
        string nombre,
        Transform parent,
        string texto,
        Color color,
        float tamanoTexto)
    {
        Image imagen = CrearImagen(nombre, parent, color);
        Button boton = imagen.gameObject.AddComponent<Button>();
        boton.targetGraphic = imagen;
        boton.navigation = new Navigation { mode = Navigation.Mode.None };

        ColorBlock colores = boton.colors;
        colores.normalColor = Color.white;
        colores.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
        colores.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
        colores.selectedColor = Color.white;
        colores.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.7f);
        colores.colorMultiplier = 1f;
        boton.colors = colores;

        TMP_Text label = CrearTexto("Texto", imagen.transform, texto, tamanoTexto, Texto, TextAlignmentOptions.Center);
        Estirar((RectTransform)label.transform, 12f, 4f, 12f, 4f);
        label.fontStyle = FontStyles.Bold;
        label.raycastTarget = false;
        return boton;
    }

    private Image CrearImagen(string nombre, Transform parent, Color color)
    {
        RectTransform rect = CrearRect(nombre, parent);
        Image imagen = rect.gameObject.AddComponent<Image>();
        imagen.color = color;
        imagen.raycastTarget = true;

        if (spriteUI != null)
        {
            imagen.sprite = spriteUI;
            imagen.type = Image.Type.Sliced;
        }

        return imagen;
    }

    private TMP_Text CrearTexto(
        string nombre,
        Transform parent,
        string contenido,
        float tamano,
        Color color,
        TextAlignmentOptions alineacion)
    {
        RectTransform rect = CrearRect(nombre, parent);
        TextMeshProUGUI texto = rect.gameObject.AddComponent<TextMeshProUGUI>();
        texto.text = contenido;
        texto.fontSize = tamano;
        texto.color = color;
        texto.alignment = alineacion;
        texto.textWrappingMode = TextWrappingModes.Normal;
        texto.overflowMode = TextOverflowModes.Ellipsis;
        texto.raycastTarget = false;
        texto.characterSpacing = 0f;
        return texto;
    }

    private RectTransform CrearRect(string nombre, Transform parent)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
        rect.localPosition = Vector3.zero;
        return rect;
    }

    private void AplicarColorBoton(Button boton, Color color)
    {
        if (boton == null)
        {
            return;
        }

        Image imagen = boton.targetGraphic as Image;
        if (imagen != null)
        {
            imagen.color = color;
        }
    }

    private void AgregarBorde(GameObject objeto, Color color, float distancia)
    {
        Outline borde = objeto.AddComponent<Outline>();
        borde.effectColor = color;
        borde.effectDistance = new Vector2(distancia, -distancia);
        borde.useGraphicAlpha = true;
    }

    private static void Estirar(RectTransform rect, float izquierda, float abajo, float derecha, float arriba)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(izquierda, abajo);
        rect.offsetMax = new Vector2(-derecha, -arriba);
    }

    private static void Fijar(
        RectTransform rect,
        Vector2 ancla,
        Vector2 posicion,
        Vector2 tamano,
        Vector2? pivote = null)
    {
        rect.anchorMin = ancla;
        rect.anchorMax = ancla;
        rect.pivot = pivote ?? new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = posicion;
        rect.sizeDelta = tamano;
    }

    private static Color ColorHex(string hex)
    {
        if (ColorUtility.TryParseHtmlString("#" + hex, out Color color))
        {
            return color;
        }

        return Color.white;
    }
}
