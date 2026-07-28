using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Events;

public class AlgoLabProgressPanel : MonoBehaviour
{
    public enum ModoActual
    {
        Aprendiendo,
        Practica
    }

    public enum CategoriaUsuario
    {
        Junior,
        SemiSenior,
        Senior
    }

    public enum EstadoNivelVisual
    {
        Ninguno,
        Ok200,
        Warning,
        Error
    }

    public enum EstadoFlujoNivel
    {
        Ninguno,
        NivelSeleccionado,
        TemaEnCurso,
        TemaTerminado,
        PracticaPreparada,
        PracticaEnCurso,
        PracticaTerminada
    }

    [System.Serializable]
    public class LevelVisual
    {
        [Header("Objeto general del nivel")]
        public GameObject levelObject;

        [Header("Información del nivel")]
        public AlgoLabProgressLevelInfo levelInfo;

        [Header("Estados visuales")]
        public GameObject ok200;
        public GameObject warning;
        public GameObject error;
    }

    [System.Serializable]
    public class CaminoVisual
    {
        public string nombreCamino = "camino";
        public List<TMP_Text> partes = new List<TMP_Text>();
    }

    [Header("Vista base siempre visible")]
    public GameObject collapsedView;
    public RectTransform collapsedCard;

    [Header("Vista extendida solo niveles")]
    public GameObject expandedView;
    public RectTransform expandedCard;

    [Header("Botón único expandir / contraer")]
    public Button btnToggleExpandCollapse;
    public RectTransform iconoToggle;
    public float rotacionIconoContraido = 0f;
    public float rotacionIconoExpandido = 180f;

    [Header("Billboard del panel")]
    public AlgoLabDiagramBillboard panelBillboard;

    [Header("Billboard - sincronización expandido/contraído")]
    [Tooltip("Recomendado activado. Fuerza que el Billboard use el punto contraído cuando el panel está contraído y el punto expandido cuando está expandido. Evita que se quede pegado al punto anterior.")]
    public bool sincronizarBillboardConEstadoCadaFrame = true;

    [Tooltip("Recomendado activado. Cambia el punto de mirada del Billboard desde el primer frame de la animación de expandir/contraer, no al final.")]
    public bool sincronizarBillboardAlIniciarTransicion = true;

    [Header("Información del usuario")]
    public Image imageUser;
    public TMP_Text nameUserText;
    public TMP_Text categoryText;

    [Header("Datos usuario por defecto / backend")]
    public Sprite userImageDefault;
    public string userName = "Cristhian";
    public CategoriaUsuario userCategory = CategoriaUsuario.Junior;

    [Header("Formato usuario")]
    public bool mostrarSoloPrimerNombre = true;

    [Header("Información del nivel")]
    public TMP_Text levelNameText;
    public TMP_Text currentModeText;
    public TMP_Text descriptionOrTaskText;
    public TMP_Text timerText;

    [Header("Botón de acción")]
    public Button btnPractice;
    public TMP_Text textPractice;

    public string textoBotonIniciarTema = "Iniciar";
    public string textoBotonIrPractica = "Práctica";
    public string textoBotonIniciarPractica = "Iniciar";

    [Header("Animación botón de acción")]
    public float buttonTransitionDuration = 0.25f;
    public float buttonHiddenScale = 0.85f;

    [Header("Puntaje - solo expandido")]
    public TMP_Text scoreTextExpanded;

    [Header("Niveles visuales")]
    public LevelVisual[] levels;

    [Tooltip("0 = Nivel 1 actual, 1 = Nivel 2 actual, 2 = Nivel 3 actual. Si es igual al total, todos los niveles quedan en verde.")]
    public int currentLevelIndex = 0;

    [Header("Modo de prueba oculto")]
    [SerializeField] private bool desbloqueoPruebaTodosLosNiveles;
    public bool DesbloqueoPruebaTodosLosNiveles =>
        desbloqueoPruebaTodosLosNiveles && EsSesionInvitadaActual();

    [Header("Colores de niveles")]
    public Color colorOk200 = new Color(0.466f, 1f, 0.541f, 1f);
    public Color colorWarning = new Color(0.976f, 0.961f, 0.506f, 1f);
    public Color colorError = new Color(1f, 0.365f, 0.345f, 1f);

    [Tooltip("Actívalo solo si también quieres pintar los textos TMP dentro del nivel.")]
    public bool recolorizarTextosNivel = false;

    [Header("Animación de cambio de estado")]
    public float levelTransitionDuration = 0.22f;
    public float levelExitScale = 1.15f;
    public float levelEnterStartScale = 0.75f;

    [Header("Selección de niveles con puntero")]
    public bool activarSeleccionNiveles = true;
    public Transform leftRayOrigin;
    public Transform rightRayOrigin;
    public float distanciaMaximaSeleccionNivel = 5f;
    public float hoverYOffset = 4f;
    public float hoverScale = 1.03f;
    public float hoverSmooth = 12f;
    public float hoverWorldRadiusFallback = 0.08f;

    [Header("Gatillo para seleccionar nivel")]
    public bool seleccionarNivelConGatillo = true;
    public OVRInput.Button botonGatilloDerecho = OVRInput.Button.PrimaryIndexTrigger;
    public OVRInput.Button botonGatilloIzquierdo = OVRInput.Button.SecondaryIndexTrigger;

    [Header("Protección de flujo")]
    [Tooltip("Si está activo, no permite cambiar la información del collapse ni seleccionar otro nivel mientras hay tema, guía o práctica activa.")]
    public bool bloquearSeleccionNivelMientrasFlujoActivo = true;

    [Tooltip("Bloqueo fuerte: mientras el flujo actual pertenece a un nivel, tocar otro nivel NO cambia collapse, botón ni nivel visual.")]
    public bool ignorarOtrosNivelesMientrasFlujoActualActivo = true;

    [Tooltip("Si está activo, cuando termina una práctica deja desbloqueado el flujo para que se puedan presionar otra vez los botones de iniciar.")]
    public bool liberarBotonesAlTerminarPractica = true;

    [Tooltip("Si está activo, al completar una práctica selecciona automáticamente el siguiente nivel disponible y muestra el botón Iniciar.")]
    public bool seleccionarSiguienteNivelAlCompletarPractica = true;

    [Tooltip("Si está activo, al terminar un tema se limpian los objetos frontales que ese tema dejó, por ejemplo puertas o carros.")]
    public bool limpiarObjetosAlTerminarTema = true;

    [Tooltip("Si está activo, al terminar una práctica se limpian objetos frontales/escenarios, por ejemplo el garage del nivel 2.")]
    public bool limpiarObjetosAlTerminarPractica = true;

    [Tooltip("Si está activo, al terminar un tema o práctica espera unos segundos antes de limpiar, para que el usuario alcance a ver el resultado final.")]
    public bool retrasarLimpiezaAlTerminarFlujo = true;

    [Tooltip("Segundos de espera antes de limpiar al terminar un tema o una práctica.")]
    public float segundosEsperaAntesDeLimpiarAlTerminar = 2f;

    [Header("Carga de escena opcional")]
    [Tooltip("Si el nivel tiene nombre de escena, se carga al iniciar el tema.")]
    public bool cargarEscenaAlIniciarTema = true;

    [Header("Controlador del tema")]
    public AlgoLabTemaPOOController temaNivel1Controller;
    public AlgoLabTemaPOOController temaNivel2Controller;

    [Header("Controlador de pilares POO (niveles 3-6)")]
    public AlgoLabPillarLevelController pillarLevelController;
    public string textoBotonTerminarPilar = "Continuar";
    public string textoBotonCompletarPilar = "Completar pr\u00E1ctica";

    [Header("Eventos práctica nivel 2")]
    public UnityEvent onPrepararPracticaNivel2;
    public UnityEvent onIniciarPracticaNivel2;

    [Tooltip("Si está activo, cuando termina el tema aparece automáticamente el botón de práctica.")]
    public bool mostrarPracticaAlTerminarTema = true;

    [Tooltip("Si está activo, los niveles con estado Error no se pueden seleccionar.")]
    public bool bloquearNivelesError = true;

    [Header("Camino por objetos")]
    public bool usarSistemaCaminoPorObjetos = true;

    [Tooltip("Arrastra aquí ExpandedCard/caminos. Si queda vacío, el script lo busca solo.")]
    public Transform caminosRoot;

    [Tooltip("Si está activo, busca camino1/part1, camino1/part2, camino2/part1, etc.")]
    public bool autoCapturarCaminosDesdeJerarquia = true;

    [Tooltip("Lista manual. Cada elemento es un camino, y dentro van sus part1, part2, part3.")]
    public List<CaminoVisual> caminosEnOrden = new List<CaminoVisual>();

    [Tooltip("Mantiene el texto original de cada part cuando está pendiente.")]
    public bool conservarTextoOriginalCaminoPendiente = true;

    [Tooltip("Cuando se completa, reemplaza carácter por carácter sin cambiar la longitud del texto.")]
    public bool reemplazarCaracteresSinCambiarLongitud = true;

    public float pathStepDelay = 0.2f;

    [Header("Camino antiguo - compatibilidad")]
    public TMP_Text[] pathPartsInOrder;
    public int[] pathPartCounts = new int[] { 3, 1, 5, 1, 3 };

    [Header("Controlador práctica nivel 1")]
    public AlgoLabCarPracticeController practicaNivel1Controller;

    [Header("Control de flujo seguro")]
    public AlgoLabFlowStateManager flowStateManager;
    public bool usarFlowStateManager = true;

    [Header("Datos de prueba visual")]
    public string levelName = "POO";
    public ModoActual currentMode = ModoActual.Aprendiendo;

    [TextArea(2, 4)]
    public string learningDescription = "Aprende cómo una clase define atributos y métodos para crear objetos.";

    [TextArea(2, 4)]
    public string practiceTask = "Selecciona los atributos y métodos del carro.";

    public string timeRemaining = "01:20";
    public int score = 0;

    [Header("Animación expandir / contraer")]
    public float transitionDuration = 0.45f;

    [Header("Barra inferior de agarre")]
    public Transform grabHandleBottom;

    public Vector3 grabHandleExpandedPosition = new Vector3(0f, -0.32f, 0f);
    public Vector3 grabHandleExpandedScale = new Vector3(0.02f, 0.178552f, 0.02f);

    public Vector3 grabHandleCollapsedPosition = new Vector3(-0.329f, -0.32f, 0f);
    public Vector3 grabHandleCollapsedScale = new Vector3(0.02f, 0.0624932f, 0.02f);

    public Vector2 expandedClosedPosition = new Vector2(-175.1f, 0f);
    public Vector2 expandedOpenPosition = new Vector2(149.6f, 0f);
    public Vector2 expandedClosedSize = new Vector2(0f, 520f);
    public Vector2 expandedOpenSize = new Vector2(650.7f, 520f);

    public bool usarMascaraEnExpandedCard = true;
    public Vector4 mascaraPadding = new Vector4(25f, 25f, 25f, 25f);

    public bool startCollapsed = true;
    public bool animatePathOnExpand = true;

    [Header("Prueba con botón A")]
    public bool activarPruebaBotonA = true;
    public OVRInput.Button botonPruebaSiguienteNivel = OVRInput.Button.One;
    public bool botonASimulaFlujoTemaPractica = false;
    public bool reiniciarAlLlegarAlFinal = true;
    public int puntosPorNivelPrueba = 100;

    private CanvasGroup collapsedGroup;
    private CanvasGroup expandedGroup;
    private CanvasGroup buttonActionGroup;
    private RectTransform buttonActionRect;

    private Coroutine transitionRoutine;
    private Coroutine pathRoutine;
    private Coroutine buttonRoutine;

    private Coroutine[] levelRoutines;
    private EstadoNivelVisual[] estadoActualPorNivel;

    private readonly Dictionary<GameObject, Vector3> escalasOriginales = new Dictionary<GameObject, Vector3>();
    private readonly Dictionary<Transform, Vector3> hoverPosicionesOriginales = new Dictionary<Transform, Vector3>();
    private readonly Dictionary<Transform, Vector3> hoverEscalasOriginales = new Dictionary<Transform, Vector3>();

    private readonly Dictionary<TMP_Text, string> textoOriginalCamino = new Dictionary<TMP_Text, string>();
    private readonly Dictionary<TMP_Text, int> slotsPorTextoCamino = new Dictionary<TMP_Text, int>();

    private bool isExpanded;
    private bool botonAccionDebeEstarVisible;
    private int caminosDibujados;
    private int nivelVisualActual;
    private int nivelSeleccionadoActual = -1;
    private int nivelActivoActual = -1;

    private EstadoFlujoNivel estadoFlujoNivel = EstadoFlujoNivel.Ninguno;

    private readonly string[] binaryPattern =
    {
        "0",
        "1",
        "1",
        "0"
    };

    private void Awake()
    {
        // El modo de prueba es deliberadamente temporal. Debe activarse de nuevo
        // en cada sesion invitada mediante el codigo Konami.
        desbloqueoPruebaTodosLosNiveles = false;

        collapsedGroup = GetOrAddCanvasGroup(collapsedView);
        expandedGroup = GetOrAddCanvasGroup(expandedView);

        PrepararMascaraExpandedCard();
        PrepararRuntimeLevels();
        AsegurarPillarLevelController();
        PrepararSistemaCaminos();
        PrepararBotonAccion();
        ConectarEventosTema();

        currentLevelIndex = Mathf.Clamp(currentLevelIndex, 0, ObtenerCantidadNiveles());
        nivelVisualActual = currentLevelIndex;
        caminosDibujados = currentLevelIndex;

        if (btnToggleExpandCollapse != null)
        {
            btnToggleExpandCollapse.onClick.RemoveListener(ToggleExpandirContraer);
            btnToggleExpandCollapse.onClick.AddListener(ToggleExpandirContraer);
        }

        if (btnPractice != null)
        {
            btnPractice.onClick.RemoveListener(OnPracticePressed);
            btnPractice.onClick.AddListener(OnPracticePressed);
        }

        ActualizarUsuario();
        ActualizarNivel();
        ActualizarBotonPractica();
        ActualizarPuntaje();

        ActualizarLevelsInmediato();
        AplicarCaminosHastaNivelActualInmediato();

        OcultarBotonAccionInmediato();

        if (startCollapsed)
        {
            MostrarContraidoInmediato();
        }
        else
        {
            MostrarExpandidoInmediato();
        }
    }

    private void OnEnable()
    {
        AplicarEstadoVisualInterrumpibleInmediato();
        ActualizarTodo();
        ActualizarIconoToggleInmediato();
        SincronizarBillboardConEstadoActual();
    }

    private void OnDisable()
    {
        DetenerRutinasVisuales(true);
    }

    private void OnDestroy()
    {
        DetenerRutinasVisuales(false);

        if (btnToggleExpandCollapse != null)
            btnToggleExpandCollapse.onClick.RemoveListener(ToggleExpandirContraer);

        if (btnPractice != null)
            btnPractice.onClick.RemoveListener(OnPracticePressed);

        if (temaNivel1Controller != null)
            temaNivel1Controller.OnTemaTerminado.RemoveListener(OnTemaNivelTerminado);

        if (temaNivel2Controller != null)
            temaNivel2Controller.OnTemaTerminado.RemoveListener(OnTemaNivelTerminado);

        if (pillarLevelController != null)
            pillarLevelController.OnTemaTerminado.RemoveListener(OnTemaNivelTerminado);
    }

    private void Update()
    {
        if (activarPruebaBotonA && OVRInput.GetDown(botonPruebaSiguienteNivel))
        {
            if (botonASimulaFlujoTemaPractica)
            {
                ProbarSiguientePasoFlujo();
            }
            else
            {
                AvanzarNivelPrueba();
            }
        }

        ActualizarSeleccionNivel();

        if (sincronizarBillboardConEstadoCadaFrame)
        {
            SincronizarBillboardConEstadoActual();
        }
    }

    private int ObtenerCantidadNiveles()
    {
        return levels != null ? levels.Length : 0;
    }

    private void ConectarEventosTema()
    {
        if (temaNivel1Controller != null)
        {
            temaNivel1Controller.OnTemaTerminado.RemoveListener(OnTemaNivelTerminado);
            temaNivel1Controller.OnTemaTerminado.AddListener(OnTemaNivelTerminado);
        }

        if (temaNivel2Controller != null)
        {
            temaNivel2Controller.OnTemaTerminado.RemoveListener(OnTemaNivelTerminado);
            temaNivel2Controller.OnTemaTerminado.AddListener(OnTemaNivelTerminado);
        }

        if (pillarLevelController != null)
        {
            pillarLevelController.OnTemaTerminado.RemoveListener(OnTemaNivelTerminado);
            pillarLevelController.OnTemaTerminado.AddListener(OnTemaNivelTerminado);
        }
    }

    public void MarcarPracticaEnCursoDesdeControlador()
    {
        estadoFlujoNivel = EstadoFlujoNivel.PracticaEnCurso;
        currentMode = ModoActual.Practica;

        RefrescarTextoFlujoActual();
        OcultarBotonAccion();

        Debug.Log("Práctica iniciada desde controlador externo.");
    }

    public void MostrarBotonIniciarPracticaDespuesDeAudio()
    {
        if (estadoFlujoNivel != EstadoFlujoNivel.PracticaPreparada)
        {
            return;
        }

        currentMode = ModoActual.Practica;

        RefrescarTextoFlujoActual();
        MostrarBotonAccion(textoBotonIniciarPractica);

        Debug.Log("Audio de práctica terminado. Ya puede iniciar la práctica.");
    }

    public void MarcarPracticaPerdidaDesdeControlador()
    {
        estadoFlujoNivel = EstadoFlujoNivel.PracticaPreparada;
        currentMode = ModoActual.Practica;

        RefrescarTextoFlujoActual();

        if (descriptionOrTaskText != null)
        {
            descriptionOrTaskText.text = "Tiempo agotado. Intenta clasificar nuevamente los atributos y métodos.";
        }

        MostrarBotonAccion(textoBotonIniciarPractica);

        Debug.Log("Práctica perdida. Puede intentarlo otra vez.");
    }

    private void OnTemaNivelTerminado()
    {
        if (!mostrarPracticaAlTerminarTema)
        {
            return;
        }

        TerminarTemaActual();
    }

    [ContextMenu("Actualizar panel")]
    public void ActualizarTodo()
    {
        ActualizarUsuario();

        if (estadoFlujoNivel == EstadoFlujoNivel.Ninguno)
        {
            ActualizarNivel();
        }
        else
        {
            RefrescarTextoFlujoActual();
        }

        ActualizarBotonPractica();
        ActualizarPuntaje();

        if (pathRoutine == null)
        {
            currentLevelIndex = Mathf.Clamp(currentLevelIndex, 0, ObtenerCantidadNiveles());
            nivelVisualActual = currentLevelIndex;
            caminosDibujados = currentLevelIndex;

            ActualizarLevelsInmediato();
            AplicarCaminosHastaNivelActualInmediato();
        }
        else
        {
            ActualizarLevelsInmediato();
        }
    }

    public void AplicarDatosUsuarioDesdeBackend(string nombreBackend, string categoriaBackend, Sprite imagenBackend)
    {
        if (!string.IsNullOrWhiteSpace(nombreBackend))
        {
            userName = mostrarSoloPrimerNombre
                ? ObtenerPrimerNombre(nombreBackend)
                : nombreBackend.Trim();
        }

        if (string.IsNullOrWhiteSpace(userName))
        {
            userName = "Usuario";
        }

        if (!string.IsNullOrWhiteSpace(categoriaBackend))
        {
            string categoriaNormalizada = categoriaBackend.Trim().ToLower();

            if (categoriaNormalizada.Contains("semi"))
            {
                userCategory = CategoriaUsuario.SemiSenior;
            }
            else if (categoriaNormalizada.Contains("senior"))
            {
                userCategory = CategoriaUsuario.Senior;
            }
            else
            {
                userCategory = CategoriaUsuario.Junior;
            }
        }

        if (imagenBackend != null && imageUser != null)
        {
            imageUser.sprite = imagenBackend;
            imageUser.enabled = true;
        }

        ActualizarUsuario();
    }

    public void AplicarDatosUsuario(string nombre, CategoriaUsuario categoria, Sprite imagen)
    {
        if (!string.IsNullOrWhiteSpace(nombre))
        {
            userName = mostrarSoloPrimerNombre
                ? ObtenerPrimerNombre(nombre)
                : nombre.Trim();
        }

        if (string.IsNullOrWhiteSpace(userName))
        {
            userName = "Usuario";
        }

        userCategory = categoria;

        if (imagen != null && imageUser != null)
        {
            imageUser.sprite = imagen;
            imageUser.enabled = true;
        }

        ActualizarUsuario();
    }

    public void AplicarDatosSesionBackend(
        string nombreBackend,
        string categoriaBackend,
        Sprite imagenBackend,
        int nivelActualBackend,
        int puntajeTotalBackend
    )
    {
        AplicarDatosUsuarioDesdeBackend(nombreBackend, categoriaBackend, imagenBackend);
        SetPuntaje(puntajeTotalBackend);

        int indiceVisual = Mathf.Max(0, nivelActualBackend - 1);
        SetNivelActual(indiceVisual);

        Debug.Log(
            "PROGRESS: datos backend aplicados | usuario=" + userName +
            " | nivelBackend=" + nivelActualBackend +
            " | indiceVisual=" + indiceVisual +
            " | puntaje=" + puntajeTotalBackend
        );
    }

    public void AplicarProgresoBackendConAnimacion(int nivelActualBackend, int puntajeTotalBackend)
    {
        SetPuntaje(puntajeTotalBackend);

        int indiceVisual = Mathf.Max(0, nivelActualBackend - 1);
        SetNivelActualConAnimacion(indiceVisual);

        Debug.Log(
            "PROGRESS: progreso backend animado | nivelBackend=" +
            nivelActualBackend +
            " | indiceVisual=" +
            indiceVisual +
            " | puntaje=" +
            puntajeTotalBackend
        );
    }

    [ContextMenu("Toggle expandir / contraer")]
    public void ToggleExpandirContraer()
    {
        if (isExpanded)
        {
            Contraer();
        }
        else
        {
            Expandir();
        }
    }

    [ContextMenu("Expandir")]
    public void Expandir()
    {
        if (isExpanded)
        {
            return;
        }

        isExpanded = true;
        SincronizarBillboardConEstadoActual();

        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
        }

        transitionRoutine = StartCoroutine(AnimarVistaExtendida(true));
    }

    [ContextMenu("Contraer")]
    public void Contraer()
    {
        if (!isExpanded)
        {
            return;
        }

        isExpanded = false;
        SincronizarBillboardConEstadoActual();

        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
        }

        transitionRoutine = StartCoroutine(AnimarVistaExtendida(false));
    }

    [ContextMenu("Avanzar nivel prueba")]
    public void AvanzarNivelPrueba()
    {
        int totalNiveles = ObtenerCantidadNiveles();

        if (totalNiveles <= 0)
        {
            Debug.LogWarning("No hay niveles configurados.");
            return;
        }

        int nivelAnterior = currentLevelIndex;
        int nuevoNivel = currentLevelIndex + 1;

        if (nuevoNivel > totalNiveles)
        {
            if (reiniciarAlLlegarAlFinal)
            {
                currentLevelIndex = 0;
                score = 0;
                caminosDibujados = 0;
                nivelVisualActual = 0;
                ReiniciarTodosLosCaminos();
                ActualizarLevelsInmediato();
                ActualizarPuntaje();
                return;
            }

            nuevoNivel = totalNiveles;
        }
        else
        {
            score += puntosPorNivelPrueba;
        }

        ActualizarPuntaje();
        AvanzarVisualmenteANivel(nivelAnterior, nuevoNivel);

        Debug.Log("Nivel actual de prueba: " + currentLevelIndex);
    }

    public void ProbarSiguientePasoFlujo()
    {
        if (estadoFlujoNivel == EstadoFlujoNivel.Ninguno)
        {
            int indiceNivel = nivelSeleccionadoActual != -1
                ? nivelSeleccionadoActual
                : Mathf.Clamp(currentLevelIndex, 0, levels != null ? levels.Length - 1 : 0);

            SeleccionarNivelParaIniciar(indiceNivel);
            return;
        }

        if (estadoFlujoNivel == EstadoFlujoNivel.NivelSeleccionado)
        {
            ComenzarTemaNivel();
            return;
        }

        if (estadoFlujoNivel == EstadoFlujoNivel.TemaEnCurso)
        {
            TerminarTemaActual();
            return;
        }

        if (estadoFlujoNivel == EstadoFlujoNivel.TemaTerminado)
        {
            PrepararPracticaNivel();
            return;
        }

        if (estadoFlujoNivel == EstadoFlujoNivel.PracticaPreparada)
        {
            ComenzarPracticaNivel();
            return;
        }

        if (estadoFlujoNivel == EstadoFlujoNivel.PracticaEnCurso)
        {
            TerminarPracticaActual();
        }
    }

    public void SetModoAprendiendo()
    {
        currentMode = ModoActual.Aprendiendo;
        ActualizarTodo();
    }

    public void SetModoPractica()
    {
        currentMode = ModoActual.Practica;
        ActualizarTodo();
    }

    public void SetNivelActual(int nuevoNivelIndex)
    {
        // Defensa: algunos botones/objetos pueden llamar este método directamente.
        // Si hay un flujo activo, ignoramos el cambio visual para que el collapse siga en el nivel actual.
        int indiceSolicitado = Mathf.Max(0, nuevoNivelIndex - 1);
        if (DebeIgnorarSolicitudDeOtroNivel(indiceSolicitado))
        {
            RefrescarTextoFlujoActual();
            RestaurarUIFlujoPendienteMismoNivel();
            Debug.Log("PROGRESS: SetNivelActual ignorado porque hay un flujo activo del nivel actual.");
            return;
        }

        DetenerAnimacionCamino();

        int totalNiveles = ObtenerCantidadNiveles();

        currentLevelIndex = Mathf.Clamp(nuevoNivelIndex, 0, totalNiveles);
        nivelVisualActual = currentLevelIndex;
        caminosDibujados = currentLevelIndex;

        AplicarCaminosHastaNivelActualInmediato();
        ActualizarLevelsInmediato();

        if (estadoFlujoNivel == EstadoFlujoNivel.Ninguno)
        {
            ActualizarNivel();
        }
        else
        {
            RefrescarTextoFlujoActual();
        }

        ActualizarBotonPractica();
        ActualizarPuntaje();

        Debug.Log(
            "PROGRESS: nivel sincronizado sin animar | nivelVisual=" +
            currentLevelIndex +
            " | caminosDibujados=" +
            caminosDibujados
        );
    }

    public void SetNivelActualConAnimacion(int nuevoNivelIndex)
    {
        int indiceSolicitado = Mathf.Max(0, nuevoNivelIndex - 1);
        if (DebeIgnorarSolicitudDeOtroNivel(indiceSolicitado))
        {
            RefrescarTextoFlujoActual();
            RestaurarUIFlujoPendienteMismoNivel();
            Debug.Log("PROGRESS: SetNivelActualConAnimacion ignorado porque hay un flujo activo del nivel actual.");
            return;
        }

        int nivelAnterior = currentLevelIndex;
        int totalNiveles = ObtenerCantidadNiveles();
        int nuevoNivel = Mathf.Clamp(nuevoNivelIndex, 0, totalNiveles);

        AvanzarVisualmenteANivel(nivelAnterior, nuevoNivel);
    }

    private void AvanzarVisualmenteANivel(int nivelAnterior, int nuevoNivel)
    {
        int totalNiveles = ObtenerCantidadNiveles();

        nivelAnterior = Mathf.Clamp(nivelAnterior, 0, totalNiveles);
        nuevoNivel = Mathf.Clamp(nuevoNivel, 0, totalNiveles);

        currentLevelIndex = nuevoNivel;

        if (isExpanded)
        {
            IniciarAnimacionCaminoDesdeNivelAnterior(nivelAnterior, nuevoNivel);
        }
        else
        {
            nivelVisualActual = nuevoNivel;
            caminosDibujados = nuevoNivel;
            AplicarCaminosHastaNivelActualInmediato();
            ActualizarLevelsInmediato();
        }

        if (estadoFlujoNivel == EstadoFlujoNivel.Ninguno)
        {
            ActualizarNivel();
        }
        else
        {
            RefrescarTextoFlujoActual();
        }

        ActualizarBotonPractica();
        ActualizarPuntaje();

        Debug.Log(
            "PROGRESS: avance visual | desde=" +
            nivelAnterior +
            " | hasta=" +
            nuevoNivel
        );
    }

    public void SetPuntaje(int nuevoPuntaje)
    {
        score = Mathf.Max(0, nuevoPuntaje);
        ActualizarPuntaje();
    }

    private void ActualizarUsuario()
    {
        if (nameUserText != null)
        {
            string nombreMostrar = mostrarSoloPrimerNombre
                ? ObtenerPrimerNombre(userName)
                : userName;

            if (string.IsNullOrWhiteSpace(nombreMostrar))
            {
                nombreMostrar = "Usuario";
            }

            nameUserText.text = nombreMostrar;
        }

        if (categoryText != null)
        {
            categoryText.text = ObtenerCategoriaTexto(userCategory);
        }

        if (imageUser != null && imageUser.sprite == null && userImageDefault != null)
        {
            imageUser.sprite = userImageDefault;
            imageUser.enabled = true;
        }
    }

    private void ActualizarNivel()
    {
        int indiceNivelMostrar = Mathf.Clamp(
            currentLevelIndex,
            0,
            levels != null && levels.Length > 0 ? levels.Length - 1 : 0
        );

        AlgoLabProgressLevelInfo info = ObtenerInfoNivel(indiceNivelMostrar);

        string nombreNivelMostrar = levelName;
        string descripcionAprendizaje = learningDescription;
        string tareaPracticaMostrar = practiceTask;
        string tiempoMostrar = timeRemaining;

        if (info != null)
        {
            if (!string.IsNullOrWhiteSpace(info.nombreNivel))
            {
                nombreNivelMostrar = info.nombreNivel;
            }

            if (!string.IsNullOrWhiteSpace(info.descripcionNivel))
            {
                descripcionAprendizaje = info.descripcionNivel;
            }

            if (!string.IsNullOrWhiteSpace(info.tareaPractica))
            {
                tareaPracticaMostrar = info.tareaPractica;
            }

            if (!string.IsNullOrWhiteSpace(info.tiempoPractica))
            {
                tiempoMostrar = info.tiempoPractica;
            }
        }

        string modoTexto = currentMode == ModoActual.Aprendiendo
            ? "Aprendiendo"
            : "Práctica";

        string mensaje = currentMode == ModoActual.Aprendiendo
            ? descripcionAprendizaje
            : tareaPracticaMostrar;

        bool estaEnPractica = currentMode == ModoActual.Practica;

        if (levelNameText != null)
        {
            levelNameText.text = nombreNivelMostrar;
        }

        if (currentModeText != null)
        {
            currentModeText.text = modoTexto;
        }

        if (descriptionOrTaskText != null)
        {
            descriptionOrTaskText.text = mensaje;
        }

        if (timerText != null)
        {
            timerText.gameObject.SetActive(estaEnPractica);
            timerText.text = tiempoMostrar;
        }
    }

    private void ActualizarBotonPractica()
    {
        if (textPractice == null)
        {
            return;
        }

        if (estadoFlujoNivel == EstadoFlujoNivel.NivelSeleccionado)
        {
            textPractice.text = textoBotonIniciarTema;
        }
        else if (estadoFlujoNivel == EstadoFlujoNivel.TemaTerminado)
        {
            textPractice.text = textoBotonIrPractica;
        }
        else if (estadoFlujoNivel == EstadoFlujoNivel.PracticaPreparada)
        {
            textPractice.text = textoBotonIniciarPractica;
        }
        else if (estadoFlujoNivel == EstadoFlujoNivel.TemaEnCurso && EsPilarActivo())
        {
            textPractice.text = textoBotonTerminarPilar;
        }
        else if (estadoFlujoNivel == EstadoFlujoNivel.PracticaEnCurso && EsPilarActivo())
        {
            textPractice.text = textoBotonCompletarPilar;
        }
        else
        {
            textPractice.text = textoBotonIniciarTema;
        }
    }

    private void ActualizarPuntaje()
    {
        if (scoreTextExpanded != null)
        {
            scoreTextExpanded.text = score.ToString();
        }
    }

    private void PrepararRuntimeLevels()
    {
        if (levels == null)
        {
            levelRoutines = new Coroutine[0];
            estadoActualPorNivel = new EstadoNivelVisual[0];
            return;
        }

        levelRoutines = new Coroutine[levels.Length];
        estadoActualPorNivel = new EstadoNivelVisual[levels.Length];

        for (int i = 0; i < estadoActualPorNivel.Length; i++)
        {
            estadoActualPorNivel[i] = EstadoNivelVisual.Ninguno;
        }
    }

    private void ActualizarLevelsInmediato()
    {
        if (levels == null)
        {
            return;
        }

        currentLevelIndex = Mathf.Clamp(currentLevelIndex, 0, levels.Length);
        nivelVisualActual = Mathf.Clamp(nivelVisualActual, 0, levels.Length);

        for (int i = 0; i < levels.Length; i++)
        {
            EstadoNivelVisual estado = CalcularEstadoNivel(i);
            AplicarEstadoNivelInmediato(i, estado);
        }
    }

    private void ActualizarLevelsSuave()
    {
        if (levels == null)
        {
            return;
        }

        currentLevelIndex = Mathf.Clamp(currentLevelIndex, 0, levels.Length);
        nivelVisualActual = Mathf.Clamp(nivelVisualActual, 0, levels.Length);

        for (int i = 0; i < levels.Length; i++)
        {
            EstadoNivelVisual nuevoEstado = CalcularEstadoNivel(i);

            if (estadoActualPorNivel == null || i >= estadoActualPorNivel.Length)
            {
                AplicarEstadoNivelInmediato(i, nuevoEstado);
                continue;
            }

            if (estadoActualPorNivel[i] == nuevoEstado)
            {
                PintarNivel(i, nuevoEstado);
                continue;
            }

            if (levelRoutines[i] != null)
            {
                StopCoroutine(levelRoutines[i]);
            }

            levelRoutines[i] = StartCoroutine(AnimarCambioEstadoNivel(i, nuevoEstado));
        }
    }

    private EstadoNivelVisual CalcularEstadoNivel(int index)
    {
        if (DesbloqueoPruebaTodosLosNiveles)
        {
            return EstadoNivelVisual.Ok200;
        }

        if (index < nivelVisualActual)
        {
            return EstadoNivelVisual.Ok200;
        }

        if (index == nivelVisualActual && nivelVisualActual < levels.Length)
        {
            return EstadoNivelVisual.Warning;
        }

        return EstadoNivelVisual.Error;
    }

    private void AplicarEstadoNivelInmediato(int index, EstadoNivelVisual estado)
    {
        if (levels == null || index < 0 || index >= levels.Length || levels[index] == null)
        {
            return;
        }

        GameObject ok = levels[index].ok200;
        GameObject warning = levels[index].warning;
        GameObject error = levels[index].error;

        SetActiveSafe(ok, estado == EstadoNivelVisual.Ok200);
        SetActiveSafe(warning, estado == EstadoNivelVisual.Warning);
        SetActiveSafe(error, estado == EstadoNivelVisual.Error);

        RestaurarEscala(ok);
        RestaurarEscala(warning);
        RestaurarEscala(error);

        if (estadoActualPorNivel != null && index < estadoActualPorNivel.Length)
        {
            estadoActualPorNivel[index] = estado;
        }

        PintarNivel(index, estado);
    }

    private IEnumerator AnimarCambioEstadoNivel(int index, EstadoNivelVisual nuevoEstado)
    {
        if (levels == null || index < 0 || index >= levels.Length || levels[index] == null)
        {
            yield break;
        }

        EstadoNivelVisual estadoAnterior = EstadoNivelVisual.Ninguno;

        if (estadoActualPorNivel != null && index < estadoActualPorNivel.Length)
        {
            estadoAnterior = estadoActualPorNivel[index];
        }

        GameObject anterior = ObtenerObjetoEstado(levels[index], estadoAnterior);
        GameObject nuevo = ObtenerObjetoEstado(levels[index], nuevoEstado);

        if (anterior != null && anterior != nuevo)
        {
            anterior.SetActive(true);

            Vector3 escalaOriginalAnterior = ObtenerEscalaOriginal(anterior);
            float tiempoSalida = 0f;
            float mitad = Mathf.Max(0.01f, levelTransitionDuration * 0.5f);

            while (tiempoSalida < mitad)
            {
                tiempoSalida += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(tiempoSalida / mitad);
                float smooth = Mathf.SmoothStep(0f, 1f, t);

                anterior.transform.localScale = Vector3.Lerp(
                    escalaOriginalAnterior,
                    escalaOriginalAnterior * levelExitScale,
                    smooth
                );

                yield return null;
            }

            anterior.SetActive(false);
            anterior.transform.localScale = escalaOriginalAnterior;
        }

        if (nuevo != null)
        {
            Vector3 escalaOriginalNuevo = ObtenerEscalaOriginal(nuevo);

            nuevo.SetActive(true);
            nuevo.transform.localScale = escalaOriginalNuevo * levelEnterStartScale;

            PintarNivel(index, nuevoEstado);

            float tiempoEntrada = 0f;
            float mitad = Mathf.Max(0.01f, levelTransitionDuration * 0.5f);

            while (tiempoEntrada < mitad)
            {
                tiempoEntrada += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(tiempoEntrada / mitad);
                float smooth = Mathf.SmoothStep(0f, 1f, t);

                nuevo.transform.localScale = Vector3.Lerp(
                    escalaOriginalNuevo * levelEnterStartScale,
                    escalaOriginalNuevo,
                    smooth
                );

                yield return null;
            }

            nuevo.transform.localScale = escalaOriginalNuevo;
        }

        OcultarEstadosNoActuales(index, nuevoEstado);

        if (estadoActualPorNivel != null && index < estadoActualPorNivel.Length)
        {
            estadoActualPorNivel[index] = nuevoEstado;
        }

        levelRoutines[index] = null;
    }

    private void OcultarEstadosNoActuales(int index, EstadoNivelVisual estado)
    {
        if (levels == null || index < 0 || index >= levels.Length || levels[index] == null)
        {
            return;
        }

        SetActiveSafe(levels[index].ok200, estado == EstadoNivelVisual.Ok200);
        SetActiveSafe(levels[index].warning, estado == EstadoNivelVisual.Warning);
        SetActiveSafe(levels[index].error, estado == EstadoNivelVisual.Error);
    }

    private GameObject ObtenerObjetoEstado(LevelVisual level, EstadoNivelVisual estado)
    {
        if (level == null)
        {
            return null;
        }

        switch (estado)
        {
            case EstadoNivelVisual.Ok200:
                return level.ok200;

            case EstadoNivelVisual.Warning:
                return level.warning;

            case EstadoNivelVisual.Error:
                return level.error;

            default:
                return null;
        }
    }

    private void PintarNivel(int index, EstadoNivelVisual estado)
    {
        if (levels == null || index < 0 || index >= levels.Length || levels[index] == null)
        {
            return;
        }

        Color color = ObtenerColorEstado(estado);

        GameObject objetoEstado = ObtenerObjetoEstado(levels[index], estado);

        if (objetoEstado != null)
        {
            PintarObjeto(objetoEstado, color);
        }

        if (levels[index].levelObject != null)
        {
            PintarObjeto(levels[index].levelObject, color);
        }
    }

    private Color ObtenerColorEstado(EstadoNivelVisual estado)
    {
        switch (estado)
        {
            case EstadoNivelVisual.Ok200:
                return colorOk200;

            case EstadoNivelVisual.Warning:
                return colorWarning;

            case EstadoNivelVisual.Error:
                return colorError;

            default:
                return Color.white;
        }
    }

    private void PintarObjeto(GameObject objetivo, Color color)
    {
        if (objetivo == null)
        {
            return;
        }

        Image[] imagenes = objetivo.GetComponentsInChildren<Image>(true);

        for (int i = 0; i < imagenes.Length; i++)
        {
            imagenes[i].color = color;
        }

        Renderer[] renderers = objetivo.GetComponentsInChildren<Renderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && renderers[i].material != null)
            {
                renderers[i].material.color = color;
            }
        }

        if (recolorizarTextosNivel)
        {
            TMP_Text[] textos = objetivo.GetComponentsInChildren<TMP_Text>(true);

            for (int i = 0; i < textos.Length; i++)
            {
                textos[i].color = color;
            }
        }
    }

    private Vector3 ObtenerEscalaOriginal(GameObject obj)
    {
        if (obj == null)
        {
            return Vector3.one;
        }

        if (!escalasOriginales.ContainsKey(obj))
        {
            escalasOriginales[obj] = obj.transform.localScale;
        }

        return escalasOriginales[obj];
    }

    private void RestaurarEscala(GameObject obj)
    {
        if (obj == null)
        {
            return;
        }

        obj.transform.localScale = ObtenerEscalaOriginal(obj);
    }

    private void PrepararBotonAccion()
    {
        if (btnPractice == null)
        {
            return;
        }

        buttonActionGroup = GetOrAddCanvasGroup(btnPractice.gameObject);
        buttonActionRect = btnPractice.GetComponent<RectTransform>();

        if (buttonActionRect != null && !hoverEscalasOriginales.ContainsKey(buttonActionRect))
        {
            hoverEscalasOriginales[buttonActionRect] = buttonActionRect.localScale;
        }
    }

    private void MostrarBotonAccion(string texto)
    {
        if (btnPractice == null)
        {
            return;
        }

        botonAccionDebeEstarVisible = true;

        if (textPractice != null)
        {
            textPractice.text = texto;
        }

        // Corrección importante: después de terminar una práctica, algunos CanvasGroup
        // o el propio Button podían quedar sin interacción. Cada vez que mostramos
        // el botón, lo reactivamos completamente.
        btnPractice.interactable = true;
        btnPractice.gameObject.SetActive(true);

        if (buttonActionGroup == null)
        {
            buttonActionGroup = GetOrAddCanvasGroup(btnPractice.gameObject);
        }

        if (buttonActionGroup != null)
        {
            buttonActionGroup.blocksRaycasts = true;
        }

        if (buttonRoutine != null)
        {
            StopCoroutine(buttonRoutine);
        }

        if (isActiveAndEnabled)
            buttonRoutine = StartCoroutine(AnimarBotonAccion(true));
        else
            AplicarBotonAccionInmediato(true);
    }

    private void OcultarBotonAccion()
    {
        if (btnPractice == null)
        {
            return;
        }

        botonAccionDebeEstarVisible = false;

        if (buttonRoutine != null)
        {
            StopCoroutine(buttonRoutine);
        }

        if (isActiveAndEnabled)
            buttonRoutine = StartCoroutine(AnimarBotonAccion(false));
        else
            AplicarBotonAccionInmediato(false);
    }

    private void OcultarBotonAccionInmediato()
    {
        botonAccionDebeEstarVisible = false;
        AplicarBotonAccionInmediato(false);
    }

    private void AplicarBotonAccionInmediato(bool mostrar)
    {
        if (btnPractice == null)
            return;

        if (buttonActionGroup == null)
            buttonActionGroup = GetOrAddCanvasGroup(btnPractice.gameObject);

        if (buttonActionRect == null)
            buttonActionRect = btnPractice.GetComponent<RectTransform>();

        Vector3 escalaBase = Vector3.one;

        if (buttonActionRect != null)
        {
            if (!hoverEscalasOriginales.ContainsKey(buttonActionRect))
                hoverEscalasOriginales[buttonActionRect] = buttonActionRect.localScale;

            escalaBase = hoverEscalasOriginales[buttonActionRect];
            buttonActionRect.localScale = mostrar
                ? escalaBase
                : escalaBase * buttonHiddenScale;
        }

        if (buttonActionGroup != null)
        {
            buttonActionGroup.alpha = mostrar ? 1f : 0f;
            buttonActionGroup.interactable = mostrar;
            buttonActionGroup.blocksRaycasts = mostrar;
        }

        btnPractice.interactable = mostrar;
        btnPractice.gameObject.SetActive(mostrar);
    }

    private IEnumerator AnimarBotonAccion(bool mostrar)
    {
        if (btnPractice == null)
        {
            yield break;
        }

        if (buttonActionGroup == null)
        {
            buttonActionGroup = GetOrAddCanvasGroup(btnPractice.gameObject);
        }

        if (buttonActionRect == null)
        {
            buttonActionRect = btnPractice.GetComponent<RectTransform>();
        }

        if (mostrar)
        {
            btnPractice.gameObject.SetActive(true);
            btnPractice.interactable = true;
        }

        float alphaInicio = buttonActionGroup != null ? buttonActionGroup.alpha : 0f;
        float alphaFinal = mostrar ? 1f : 0f;

        Vector3 escalaBase = Vector3.one;

        if (buttonActionRect != null)
        {
            if (!hoverEscalasOriginales.ContainsKey(buttonActionRect))
            {
                hoverEscalasOriginales[buttonActionRect] = buttonActionRect.localScale;
            }

            escalaBase = hoverEscalasOriginales[buttonActionRect];
        }

        Vector3 escalaInicio = buttonActionRect != null ? buttonActionRect.localScale : escalaBase;
        Vector3 escalaFinal = mostrar ? escalaBase : escalaBase * buttonHiddenScale;

        if (buttonActionGroup != null)
        {
            buttonActionGroup.interactable = false;
            buttonActionGroup.blocksRaycasts = false;
        }

        float tiempo = 0f;

        while (tiempo < buttonTransitionDuration)
        {
            tiempo += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(tiempo / buttonTransitionDuration);
            float smooth = Mathf.SmoothStep(0f, 1f, t);

            if (buttonActionGroup != null)
            {
                buttonActionGroup.alpha = Mathf.Lerp(alphaInicio, alphaFinal, smooth);
            }

            if (buttonActionRect != null)
            {
                buttonActionRect.localScale = Vector3.Lerp(escalaInicio, escalaFinal, smooth);
            }

            yield return null;
        }

        if (buttonActionGroup != null)
        {
            buttonActionGroup.alpha = alphaFinal;
            buttonActionGroup.interactable = mostrar;
            buttonActionGroup.blocksRaycasts = mostrar;
        }

        if (mostrar && btnPractice != null)
        {
            btnPractice.interactable = true;
        }

        if (buttonActionRect != null)
        {
            buttonActionRect.localScale = escalaFinal;
        }

        if (!mostrar)
        {
            btnPractice.gameObject.SetActive(false);
        }

        buttonRoutine = null;
    }

    private void SeleccionarNivelParaIniciar(int index)
    {
        if (levels == null || index < 0 || index >= levels.Length)
        {
            return;
        }

        if (!NivelDisponible(index))
        {
            Debug.Log("Este nivel no está disponible todavía: " + index);
            return;
        }

        if (DebeIgnorarSolicitudDeOtroNivel(index))
        {
            nivelSeleccionadoActual = -1;
            RestaurarHoverTodos();
            RefrescarTextoFlujoActual();
            RestaurarUIFlujoPendienteMismoNivel();
            Debug.Log("PROGRESS: selección ignorada. Hay un flujo activo y el usuario tocó otro nivel.");
            return;
        }

        // FIX FINAL: si hay tema, práctica o guía activa y el usuario toca OTRO nivel,
        // no se permite que el collapse ni el botón cambien de información.
        if (EsOtroNivelMientrasFlujoProtegido(index))
        {
            nivelSeleccionadoActual = -1;
            RestaurarHoverTodos();
            RefrescarTextoFlujoActual();
            RestaurarUIFlujoPendienteMismoNivel();
            Debug.Log("PROGRESS: cambio a otro nivel bloqueado porque el flujo actual todavía está activo.");
            return;
        }

        // FIX: si el usuario vuelve a presionar EL MISMO nivel después de terminar el tema,
        // NO se debe reiniciar el tema. Debe conservarse el botón de práctica.
        // Esto evita: Tema terminado -> tocar mismo nivel -> se pierde el botón Ir a práctica -> vuelve a iniciar tema.
        if (EsSeleccionDelMismoNivelConFlujoPendiente(index))
        {
            RestaurarUIFlujoPendienteMismoNivel();
            Debug.Log("PROGRESS: mismo nivel presionado con flujo pendiente. Se conserva el estado actual y el botón correcto.");
            return;
        }

        if (BloquearCambioNivelPorFlujoActivo())
        {
            RefrescarTextoFlujoActual();
            Debug.Log("PROGRESS: no se puede cambiar de nivel mientras hay un tema, guía o práctica activa.");
            return;
        }

        DetenerFlujoAnteriorPorSeleccionManual(index);

        nivelActivoActual = index;
        estadoFlujoNivel = EstadoFlujoNivel.NivelSeleccionado;
        currentMode = ModoActual.Aprendiendo;

        ActualizarTextoNivelSeleccionado(index);
        MostrarBotonAccion(textoBotonIniciarTema);

        Debug.Log("Nivel seleccionado: " + index);
    }

    private bool EsSeleccionDelMismoNivelConFlujoPendiente(int index)
    {
        if (index != nivelActivoActual)
        {
            return false;
        }

        return estadoFlujoNivel == EstadoFlujoNivel.TemaEnCurso ||
               estadoFlujoNivel == EstadoFlujoNivel.TemaTerminado ||
               estadoFlujoNivel == EstadoFlujoNivel.PracticaPreparada ||
               estadoFlujoNivel == EstadoFlujoNivel.PracticaEnCurso ||
               estadoFlujoNivel == EstadoFlujoNivel.PracticaTerminada;
    }

    private void RestaurarUIFlujoPendienteMismoNivel()
    {
        RefrescarTextoFlujoActual();

        if (estadoFlujoNivel == EstadoFlujoNivel.TemaTerminado)
        {
            MostrarBotonAccion(textoBotonIrPractica);
            return;
        }

        if (estadoFlujoNivel == EstadoFlujoNivel.PracticaPreparada)
        {
            MostrarBotonAccion(textoBotonIniciarPractica);
            return;
        }

        if (estadoFlujoNivel == EstadoFlujoNivel.TemaEnCurso ||
            estadoFlujoNivel == EstadoFlujoNivel.PracticaEnCurso)
        {
            OcultarBotonAccion();
            return;
        }

        if (estadoFlujoNivel == EstadoFlujoNivel.PracticaTerminada)
        {
            // Ya terminó: conserva el resultado/estado. El usuario puede escoger otro nivel.
            OcultarBotonAccion();
        }
    }

    private bool NivelDisponible(int index)
    {
        if (levels == null || index < 0 || index >= levels.Length)
        {
            return false;
        }

        if (DesbloqueoPruebaTodosLosNiveles)
        {
            return true;
        }

        if (!bloquearNivelesError)
        {
            return true;
        }

        EstadoNivelVisual estado = CalcularEstadoNivel(index);

        return estado != EstadoNivelVisual.Error;
    }

    [ContextMenu("Activar modo prueba: desbloquear todos los niveles")]
    public void ActivarDesbloqueoPruebaTodosLosNiveles()
    {
        desbloqueoPruebaTodosLosNiveles = true;

        DetenerAnimacionCamino();
        ActualizarLevelsInmediato();
        AplicarCaminosHastaNivelActualInmediato();

        if (levelNameText != null)
        {
            levelNameText.text = "MODO DE PRUEBA ACTIVADO";
        }

        if (descriptionOrTaskText != null)
        {
            descriptionOrTaskText.text =
                "Todos los niveles estan desbloqueados. El progreso real de la cuenta no fue modificado.";
        }

        Debug.Log("PROGRESS: todos los niveles desbloqueados localmente para pruebas.");
    }

    [ContextMenu("Desactivar modo prueba: usar progreso real")]
    public void DesactivarDesbloqueoPruebaTodosLosNiveles()
    {
        desbloqueoPruebaTodosLosNiveles = false;
        ActualizarTodo();
        Debug.Log("PROGRESS: modo de prueba desactivado; se usa el progreso real.");
    }

    public bool HayNivelSeleccionadoOEnCurso()
    {
        return nivelActivoActual >= 0 ||
               estadoFlujoNivel != EstadoFlujoNivel.Ninguno;
    }

    private static bool EsSesionInvitadaActual()
    {
        AlgoLabSessionManager session = AlgoLabSessionManager.Instance;
        if (session == null)
        {
            session = FindFirstObjectByType<AlgoLabSessionManager>(FindObjectsInactive.Include);
        }

        return session != null && session.SesionIniciada && session.ModoInvitado;
    }

    private bool EsPilarActivo()
    {
        int numeroNivelReal = nivelActivoActual + 1;
        return pillarLevelController != null &&
               pillarLevelController.EsNivelPilar(numeroNivelReal);
    }



    private bool FlowStateManagerTieneFlujoProtegido()
    {
        AsegurarFlowStateManager();

        if (!usarFlowStateManager || flowStateManager == null)
        {
            return false;
        }

        return flowStateManager.estadoActual != AlgoLabFlowStateManager.EstadoFlujoAlgolab.Ninguno &&
               flowStateManager.estadoActual != AlgoLabFlowStateManager.EstadoFlujoAlgolab.IA;
    }

    private bool EsOtroNivelMientrasFlujoProtegido(int index)
    {
        if (!bloquearSeleccionNivelMientrasFlujoActivo)
        {
            return false;
        }

        bool hayFlujoProtegido = HayFlujoActualProtegido();

        if (!hayFlujoProtegido)
        {
            return false;
        }

        // Si no sabemos cuál es el nivel activo, igual protegemos el collapse.
        if (nivelActivoActual < 0)
        {
            return true;
        }

        return index != nivelActivoActual;
    }

    private bool BloquearCambioNivelPorFlujoActivo()
    {
        if (!bloquearSeleccionNivelMientrasFlujoActivo)
        {
            return false;
        }

        return HayFlujoActualProtegido();
    }

    private bool HayFlujoActualProtegido()
    {
        if (!bloquearSeleccionNivelMientrasFlujoActivo)
        {
            return false;
        }

        // Bloqueo normal del flujo del panel.
        if (estadoFlujoNivel == EstadoFlujoNivel.TemaEnCurso ||
            estadoFlujoNivel == EstadoFlujoNivel.TemaTerminado ||
            estadoFlujoNivel == EstadoFlujoNivel.PracticaPreparada ||
            estadoFlujoNivel == EstadoFlujoNivel.PracticaEnCurso)
        {
            return true;
        }

        // Defensa extra: si por algún bug un controlador externo dejó el panel en modo práctica
        // pero el estado no quedó en PracticaEnCurso, igual bloqueamos otros niveles.
        if (ignorarOtrosNivelesMientrasFlujoActualActivo &&
            currentMode == ModoActual.Practica &&
            nivelActivoActual >= 0 &&
            estadoFlujoNivel != EstadoFlujoNivel.Ninguno &&
            estadoFlujoNivel != EstadoFlujoNivel.PracticaTerminada)
        {
            return true;
        }

        // Defensa extra desde el FlowStateManager.
        if (FlowStateManagerTieneFlujoProtegido())
        {
            return true;
        }

        return false;
    }

    private void DetenerFlujoAnteriorPorSeleccionManual(int nuevoIndiceNivel)
    {
        AsegurarFlowStateManager();

        if (usarFlowStateManager && flowStateManager != null)
        {
            flowStateManager.DetenerTodoPorCambioManualDeNivel(nuevoIndiceNivel + 1);
        }
        else
        {
            LimpiarObjetosSpawneadosDelFlujoActual(true);
        }
    }

    [ContextMenu("Salir del nivel actual")]
    public void SalirDelNivelActual()
    {
        int nivelAnteriorReal = nivelActivoActual >= 0 ? nivelActivoActual + 1 : -1;

        DetenerRutinasVisuales(false);
        AsegurarFlowStateManager();

        if (usarFlowStateManager && flowStateManager != null)
        {
            flowStateManager.DetenerTodoPorCambioManualDeNivel(
                nivelAnteriorReal > 0 ? nivelAnteriorReal : 0
            );
        }
        else
        {
            LimpiarObjetosSpawneadosDelFlujoActual(true);
        }

        if (pillarLevelController != null)
        {
            pillarLevelController.DetenerFlujo();
        }

        nivelActivoActual = -1;
        nivelSeleccionadoActual = -1;
        estadoFlujoNivel = EstadoFlujoNivel.Ninguno;
        currentMode = ModoActual.Aprendiendo;

        OcultarBotonAccionInmediato();
        RestaurarHoverTodosInmediato();
        LiberarInteraccionDespuesDeTerminarPractica();
        ActualizarTodo();

        isExpanded = true;
        SincronizarBillboardConEstadoActual();
        AplicarEstadoVisualInterrumpibleInmediato();

        Debug.Log(
            "PROGRESS: nivel abandonado sin marcarlo como completado. Nivel anterior real: " +
            nivelAnteriorReal
        );
    }

    public int ObtenerNivelActivoRealActual()
    {
        return nivelActivoActual >= 0 ? nivelActivoActual + 1 : -1;
    }

    public bool EsNivelActivoRealActual(int numeroNivelReal)
    {
        return nivelActivoActual == numeroNivelReal - 1;
    }

    public bool PuedeControladorModificarPanelNivel(int numeroNivelReal)
    {
        if (!EsNivelActivoRealActual(numeroNivelReal))
        {
            return false;
        }

        return estadoFlujoNivel == EstadoFlujoNivel.PracticaPreparada ||
               estadoFlujoNivel == EstadoFlujoNivel.PracticaEnCurso;
    }

    public bool PuedePrepararGuiaDesdeControladorNivel(int numeroNivelReal)
    {
        return EsNivelActivoRealActual(numeroNivelReal) &&
               estadoFlujoNivel == EstadoFlujoNivel.PracticaPreparada;
    }

    public bool PuedeIniciarPracticaDesdeControladorNivel(int numeroNivelReal)
    {
        return EsNivelActivoRealActual(numeroNivelReal) &&
               estadoFlujoNivel == EstadoFlujoNivel.PracticaEnCurso;
    }

    public void MarcarPracticaEnCursoDesdeControladorNivel(int numeroNivelReal)
    {
        if (!EsNivelActivoRealActual(numeroNivelReal))
        {
            Debug.LogWarning("PROGRESS: se ignoró una práctica de otro nivel. Nivel controlador: " + numeroNivelReal + " | Nivel activo: " + ObtenerNivelActivoRealActual());
            return;
        }

        MarcarPracticaEnCursoDesdeControlador();
    }

    private bool DebeIgnorarSolicitudDeOtroNivel(int indiceNivelSolicitado)
    {
        if (!ignorarOtrosNivelesMientrasFlujoActualActivo)
        {
            return false;
        }

        if (!HayFlujoActualProtegido())
        {
            return false;
        }

        if (nivelActivoActual < 0)
        {
            return true;
        }

        return indiceNivelSolicitado != nivelActivoActual;
    }

    private void AsegurarFlowStateManager()
    {
        if (!usarFlowStateManager)
        {
            return;
        }

        if (flowStateManager == null)
        {
            flowStateManager = AlgoLabFlowStateManager.Instance;
        }

        if (flowStateManager == null)
        {
            flowStateManager = FindFirstObjectByType<AlgoLabFlowStateManager>(FindObjectsInactive.Include);
        }
    }

    private void LimpiarObjetosSpawneadosDelFlujoActual(bool forzar = false, bool conRetraso = false)
    {
        AsegurarFlowStateManager();

        if (!usarFlowStateManager || flowStateManager == null)
        {
            return;
        }

        if (forzar)
        {
            flowStateManager.LimpiarObjetosDeNivelInmediato();
        }
        else if (conRetraso && retrasarLimpiezaAlTerminarFlujo)
        {
            float espera = Mathf.Max(0f, segundosEsperaAntesDeLimpiarAlTerminar);
            flowStateManager.LimpiarObjetosDeNivelConRetraso(espera);
        }
        else
        {
            flowStateManager.LimpiarObjetosDeNivelConSmooth();
        }
    }

    private void ComenzarTemaNivel()
    {
        if (nivelActivoActual < 0)
        {
            return;
        }

        if (!NivelDisponible(nivelActivoActual))
        {
            Debug.Log("No se puede iniciar un nivel bloqueado.");
            return;
        }

        AsegurarFlowStateManager();
        if (usarFlowStateManager && flowStateManager != null)
        {
            flowStateManager.PrepararInicioTema(nivelActivoActual + 1);
        }

        estadoFlujoNivel = EstadoFlujoNivel.TemaEnCurso;
        currentMode = ModoActual.Aprendiendo;

        ActualizarTextoNivelSeleccionado(nivelActivoActual);
        OcultarBotonAccion();

        AlgoLabProgressLevelInfo info = ObtenerInfoNivel(nivelActivoActual);

        if (cargarEscenaAlIniciarTema && info != null && !string.IsNullOrWhiteSpace(info.nombreEscena))
        {
            IntentarCargarEscenaNivel(nivelActivoActual);
        }

        if (nivelActivoActual == 0 && temaNivel1Controller != null)
        {
            temaNivel1Controller.ReproducirTema();
        }
        else if (nivelActivoActual == 1 && temaNivel2Controller != null)
        {
            temaNivel2Controller.ReproducirTema();
        }
        else if (EsPilarActivo())
        {
            int numeroNivelReal = nivelActivoActual + 1;
            pillarLevelController.IniciarTema(numeroNivelReal);

            if (descriptionOrTaskText != null)
            {
                descriptionOrTaskText.text = pillarLevelController.ObtenerTextoTema(numeroNivelReal);
            }

            // En los pilares el usuario decide cuándo terminó de leer la explicación.
            if (!pillarLevelController.TemaUsaSecuenciaAutomatica(numeroNivelReal))
            {
                MostrarBotonAccion(textoBotonTerminarPilar);
            }
        }
        else
        {
            Debug.LogWarning("No hay controlador de tema asignado para este nivel: " + nivelActivoActual);
        }

        Debug.Log("Tema iniciado en nivel: " + nivelActivoActual);
    }

    [ContextMenu("Terminar tema actual")]
    public void TerminarTemaActual()
    {
        if (estadoFlujoNivel != EstadoFlujoNivel.TemaEnCurso)
        {
            return;
        }

        estadoFlujoNivel = EstadoFlujoNivel.TemaTerminado;
        currentMode = ModoActual.Aprendiendo;

        if (EsPilarActivo())
        {
            pillarLevelController.TerminarTema();
        }

        RefrescarTextoFlujoActual();

        if (limpiarObjetosAlTerminarTema)
        {
            LimpiarObjetosSpawneadosDelFlujoActual(false, true);
        }

        MostrarBotonAccion(textoBotonIrPractica);

        Debug.Log("Tema terminado. Ya puede pasar a práctica.");
    }

    private void PrepararPracticaNivel()
    {
        if (nivelActivoActual < 0)
        {
            return;
        }

        AsegurarFlowStateManager();
        if (usarFlowStateManager && flowStateManager != null)
        {
            flowStateManager.PrepararGuiaPractica(nivelActivoActual + 1);
        }

        estadoFlujoNivel = EstadoFlujoNivel.PracticaPreparada;
        currentMode = ModoActual.Practica;

        RefrescarTextoFlujoActual();
        OcultarBotonAccion();

        Debug.Log("PROGRESS: guía de práctica preparada para nivel real: " + (nivelActivoActual + 1));

        if (nivelActivoActual == 0 && practicaNivel1Controller != null)
        {
            practicaNivel1Controller.IniciarExplicacionPractica();
        }
        else if (nivelActivoActual == 1)
        {
            if (onPrepararPracticaNivel2 != null)
            {
                onPrepararPracticaNivel2.Invoke();
            }
        }
        else if (EsPilarActivo())
        {
            int numeroNivelReal = nivelActivoActual + 1;
            pillarLevelController.PrepararPractica(numeroNivelReal);

            if (descriptionOrTaskText != null)
            {
                descriptionOrTaskText.text = pillarLevelController.ObtenerTextoPractica(numeroNivelReal);
            }

            if (!pillarLevelController.ReproducirTutorialPracticaSiDisponible(
                    numeroNivelReal,
                    MostrarBotonIniciarPracticaDespuesDeAudio))
            {
                MostrarBotonAccion(textoBotonIniciarPractica);
            }
        }
        else
        {
            MostrarBotonAccion(textoBotonIniciarPractica);
            Debug.LogWarning("No hay guía de práctica configurada para este nivel.");
        }
    }

    private void ComenzarPracticaNivel()
    {
        if (nivelActivoActual < 0)
        {
            return;
        }

        AsegurarFlowStateManager();
        if (usarFlowStateManager && flowStateManager != null)
        {
            flowStateManager.PrepararInicioPractica(nivelActivoActual + 1);
        }

        estadoFlujoNivel = EstadoFlujoNivel.PracticaEnCurso;
        currentMode = ModoActual.Practica;

        RefrescarTextoFlujoActual();
        OcultarBotonAccion();

        if (nivelActivoActual == 0 && practicaNivel1Controller != null)
        {
            practicaNivel1Controller.IniciarPracticaDesdeBoton();
        }
        else if (nivelActivoActual == 1)
        {
            if (onIniciarPracticaNivel2 != null)
            {
                onIniciarPracticaNivel2.Invoke();
            }
        }
        else if (EsPilarActivo())
        {
            int numeroNivelReal = nivelActivoActual + 1;
            pillarLevelController.IniciarPractica(numeroNivelReal);

            if (descriptionOrTaskText != null)
            {
                descriptionOrTaskText.text = pillarLevelController.ObtenerTextoPractica(numeroNivelReal);
            }

            if (!pillarLevelController.UsaPracticaInteractiva(numeroNivelReal))
            {
                MostrarBotonAccion(textoBotonCompletarPilar);
            }
        }
        else
        {
            Debug.LogWarning("No hay controlador de práctica asignado para este nivel.");
        }

        Debug.Log("PROGRESS: práctica iniciada en nivel real: " + (nivelActivoActual + 1));
    }

    private void LiberarInteraccionDespuesDeTerminarPractica()
    {
        if (!liberarBotonesAlTerminarPractica)
        {
            return;
        }

        AsegurarFlowStateManager();

        if (flowStateManager != null)
        {
            flowStateManager.MarcarFlujoLibre();
        }

        if (btnPractice != null)
        {
            btnPractice.interactable = true;
        }

        if (buttonActionGroup != null)
        {
            buttonActionGroup.interactable = true;
            buttonActionGroup.blocksRaycasts = true;
        }

        if (collapsedGroup != null)
        {
            collapsedGroup.interactable = true;
            collapsedGroup.blocksRaycasts = true;
        }

        if (expandedGroup != null && isExpanded)
        {
            expandedGroup.interactable = true;
            expandedGroup.blocksRaycasts = true;
        }
    }

    private void SeleccionarSiguienteNivelDisponibleDespuesDeCompletar(int nuevoNivelVisual)
    {
        if (!seleccionarSiguienteNivelAlCompletarPractica)
        {
            return;
        }

        if (levels == null || levels.Length == 0)
        {
            return;
        }

        if (nuevoNivelVisual >= levels.Length)
        {
            return;
        }

        int siguienteIndice = Mathf.Clamp(nuevoNivelVisual, 0, levels.Length - 1);

        if (!NivelDisponible(siguienteIndice))
        {
            return;
        }

        nivelActivoActual = siguienteIndice;
        nivelSeleccionadoActual = siguienteIndice;
        estadoFlujoNivel = EstadoFlujoNivel.NivelSeleccionado;
        currentMode = ModoActual.Aprendiendo;

        ActualizarTextoNivelSeleccionado(siguienteIndice);
        MostrarBotonAccion(textoBotonIniciarTema);

        Debug.Log("PROGRESS: siguiente nivel listo para iniciar: " + (siguienteIndice + 1));
    }

    [ContextMenu("Terminar práctica actual")]
    public void TerminarPracticaActual()
    {
        if (levels == null || levels.Length == 0)
        {
            Debug.LogWarning("PROGRESS: no hay niveles configurados.");
            return;
        }

        int nivelCompletado = nivelActivoActual;

        if (nivelCompletado < 0)
        {
            nivelCompletado = Mathf.Clamp(currentLevelIndex, 0, levels.Length - 1);
        }

        if (pillarLevelController != null &&
            pillarLevelController.EsNivelPilar(nivelCompletado + 1))
        {
            pillarLevelController.CompletarPractica(nivelCompletado + 1);
        }

        int nuevoNivel = Mathf.Clamp(
            nivelCompletado + 1,
            0,
            levels.Length
        );

        nivelActivoActual = -1;
        nivelSeleccionadoActual = -1;
        estadoFlujoNivel = EstadoFlujoNivel.Ninguno;
        currentMode = ModoActual.Aprendiendo;

        OcultarBotonAccion();

        bool conservarRobotReparado =
            nivelCompletado == 2 &&
            ExisteRobotNivel3ReparadoActivo();

        if (limpiarObjetosAlTerminarPractica && !conservarRobotReparado)
        {
            LimpiarObjetosSpawneadosDelFlujoActual(false, true);
        }

        AvanzarVisualmenteANivel(nivelCompletado, nuevoNivel);

        // Corrección: al completar práctica se libera el flujo y los CanvasGroup/botones
        // para poder volver a seleccionar niveles anteriores o continuar al siguiente.
        LiberarInteraccionDespuesDeTerminarPractica();

        // Si el siguiente nivel quedó desbloqueado, lo dejamos seleccionado y con botón Iniciar.
        SeleccionarSiguienteNivelDisponibleDespuesDeCompletar(nuevoNivel);

        Debug.Log(
            "PROGRESS: práctica terminada. Nivel real completado: " +
            (nivelCompletado + 1) +
            " | Nuevo nivel visual: " +
            nuevoNivel
        );
    }

    private static bool ExisteRobotNivel3ReparadoActivo()
    {
        AlgoLabLevel3RobotPracticeRuntime runtime =
            FindFirstObjectByType<AlgoLabLevel3RobotPracticeRuntime>(
                FindObjectsInactive.Exclude
            );
        return runtime != null &&
               runtime.practica != null &&
               runtime.practica.PracticaCompletada &&
               !runtime.Explotado;
    }

    private void RefrescarTextoFlujoActual()
    {
        if (nivelActivoActual < 0)
        {
            ActualizarNivel();
            return;
        }

        AlgoLabProgressLevelInfo info = ObtenerInfoNivel(nivelActivoActual);

        if (info == null)
        {
            ActualizarNivel();
            return;
        }

        if (levelNameText != null)
        {
            levelNameText.text = string.IsNullOrWhiteSpace(info.nombreNivel)
                ? "Nivel " + (nivelActivoActual + 1)
                : info.nombreNivel;
        }

        if (currentModeText != null)
        {
            currentModeText.text = currentMode == ModoActual.Aprendiendo
                ? "Aprendiendo"
                : "Práctica";
        }

        if (currentMode == ModoActual.Aprendiendo)
        {
            if (descriptionOrTaskText != null)
            {
                descriptionOrTaskText.text = string.IsNullOrWhiteSpace(info.descripcionNivel)
                    ? learningDescription
                    : info.descripcionNivel;
            }

            if (timerText != null)
            {
                timerText.gameObject.SetActive(false);
            }
        }
        else
        {
            if (descriptionOrTaskText != null)
            {
                descriptionOrTaskText.text = string.IsNullOrWhiteSpace(info.tareaPractica)
                    ? practiceTask
                    : info.tareaPractica;
            }

            if (timerText != null)
            {
                timerText.gameObject.SetActive(true);
                timerText.text = string.IsNullOrWhiteSpace(info.tiempoPractica)
                    ? timeRemaining
                    : info.tiempoPractica;
            }
        }
    }

    private IEnumerator AnimarVistaExtendida(bool expandir)
    {
        float rotacionInicioIcono = iconoToggle != null
            ? iconoToggle.localEulerAngles.z
            : 0f;

        float rotacionFinalIcono = expandir
            ? rotacionIconoExpandido
            : rotacionIconoContraido;

        if (collapsedView != null)
        {
            collapsedView.SetActive(true);
        }

        if (expandedView != null)
        {
            expandedView.SetActive(true);
        }

        SetGroup(collapsedGroup, 1f, true);

        if (expandedGroup != null)
        {
            expandedGroup.alpha = 1f;
            expandedGroup.interactable = expandir;
            expandedGroup.blocksRaycasts = expandir;
        }

        if (sincronizarBillboardAlIniciarTransicion)
        {
            SincronizarBillboardConEstado(expandir);
        }

        Vector2 posicionInicio = expandedCard != null
            ? expandedCard.anchoredPosition
            : Vector2.zero;

        Vector2 tamanoInicio = expandedCard != null
            ? expandedCard.sizeDelta
            : Vector2.zero;

        Vector2 posicionFinal = expandir
            ? expandedOpenPosition
            : expandedClosedPosition;

        Vector2 tamanoFinal = expandir
            ? expandedOpenSize
            : expandedClosedSize;

        Vector3 grabPosicionInicio = grabHandleBottom != null
            ? grabHandleBottom.localPosition
            : Vector3.zero;

        Vector3 grabEscalaInicio = grabHandleBottom != null
            ? grabHandleBottom.localScale
            : Vector3.one;

        Vector3 grabPosicionFinal = expandir
            ? grabHandleExpandedPosition
            : grabHandleCollapsedPosition;

        Vector3 grabEscalaFinal = expandir
            ? grabHandleExpandedScale
            : grabHandleCollapsedScale;

        if (expandir && expandedCard != null)
        {
            expandedCard.anchoredPosition = expandedClosedPosition;
            expandedCard.sizeDelta = expandedClosedSize;

            posicionInicio = expandedClosedPosition;
            tamanoInicio = expandedClosedSize;
        }

        float tiempo = 0f;

        while (tiempo < transitionDuration)
        {
            tiempo += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(tiempo / transitionDuration);
            float smooth = Mathf.SmoothStep(0f, 1f, t);

            if (expandedCard != null)
            {
                expandedCard.anchoredPosition = Vector2.Lerp(
                    posicionInicio,
                    posicionFinal,
                    smooth
                );

                expandedCard.sizeDelta = Vector2.Lerp(
                    tamanoInicio,
                    tamanoFinal,
                    smooth
                );
            }

            if (grabHandleBottom != null)
            {
                grabHandleBottom.localPosition = Vector3.Lerp(
                    grabPosicionInicio,
                    grabPosicionFinal,
                    smooth
                );

                grabHandleBottom.localScale = Vector3.Lerp(
                    grabEscalaInicio,
                    grabEscalaFinal,
                    smooth
                );
            }

            if (iconoToggle != null)
            {
                float rotacionZ = Mathf.LerpAngle(
                    rotacionInicioIcono,
                    rotacionFinalIcono,
                    smooth
                );

                iconoToggle.localRotation = Quaternion.Euler(0f, 0f, rotacionZ);
            }

            yield return null;
        }

        if (expandedCard != null)
        {
            expandedCard.anchoredPosition = posicionFinal;
            expandedCard.sizeDelta = tamanoFinal;
        }

        if (grabHandleBottom != null)
        {
            grabHandleBottom.localPosition = grabPosicionFinal;
            grabHandleBottom.localScale = grabEscalaFinal;
        }

        if (!expandir && expandedView != null)
        {
            expandedView.SetActive(false);
        }

        if (expandedGroup != null)
        {
            expandedGroup.alpha = expandir ? 1f : 0f;
            expandedGroup.interactable = expandir;
            expandedGroup.blocksRaycasts = expandir;
        }

        SincronizarBillboardConEstado(expandir);

        ActualizarIconoToggleInmediato();

        if (expandir)
        {
            if (animatePathOnExpand && caminosDibujados < currentLevelIndex)
            {
                IniciarAnimacionCaminoDesdeNivelAnterior(caminosDibujados, currentLevelIndex);
            }
            else
            {
                AplicarCaminosHastaNivelActualInmediato();
            }
        }

        transitionRoutine = null;
    }

    private void MostrarContraidoInmediato()
    {
        isExpanded = false;

        if (collapsedView != null)
        {
            collapsedView.SetActive(true);
        }

        if (expandedView != null)
        {
            expandedView.SetActive(false);
        }

        SetGroup(collapsedGroup, 1f, true);
        SetGroup(expandedGroup, 0f, false);

        if (expandedCard != null)
        {
            expandedCard.anchoredPosition = expandedClosedPosition;
            expandedCard.sizeDelta = expandedClosedSize;
        }

        SincronizarBillboardConEstado(false);

        AplicarGrabHandleInmediato(false);
        ActualizarIconoToggleInmediato();
    }

    private void MostrarExpandidoInmediato()
    {
        isExpanded = true;

        if (collapsedView != null)
        {
            collapsedView.SetActive(true);
        }

        if (expandedView != null)
        {
            expandedView.SetActive(true);
        }

        SetGroup(collapsedGroup, 1f, true);
        SetGroup(expandedGroup, 1f, true);

        if (expandedCard != null)
        {
            expandedCard.anchoredPosition = expandedOpenPosition;
            expandedCard.sizeDelta = expandedOpenSize;
        }

        SincronizarBillboardConEstado(true);

        AplicarGrabHandleInmediato(true);
        ActualizarIconoToggleInmediato();

        AplicarCaminosHastaNivelActualInmediato();
    }

    private void SincronizarBillboardConEstadoActual()
    {
        SincronizarBillboardConEstado(isExpanded);
    }

    private void SincronizarBillboardConEstado(bool expandido)
    {
        if (panelBillboard == null)
        {
            return;
        }

        panelBillboard.SetExpandido(expandido);
    }

    private void AplicarGrabHandleInmediato(bool expandido)
    {
        if (grabHandleBottom == null)
        {
            return;
        }

        grabHandleBottom.localPosition = expandido
            ? grabHandleExpandedPosition
            : grabHandleCollapsedPosition;

        grabHandleBottom.localScale = expandido
            ? grabHandleExpandedScale
            : grabHandleCollapsedScale;
    }

    private void ActualizarIconoToggleInmediato()
    {
        if (iconoToggle == null)
        {
            return;
        }

        float rotacionZ = isExpanded
            ? rotacionIconoExpandido
            : rotacionIconoContraido;

        iconoToggle.localRotation = Quaternion.Euler(0f, 0f, rotacionZ);
    }

    private void PrepararMascaraExpandedCard()
    {
        if (!usarMascaraEnExpandedCard || expandedCard == null)
        {
            return;
        }

        RectMask2D mascara = expandedCard.GetComponent<RectMask2D>();

        if (mascara == null)
        {
            mascara = expandedCard.gameObject.AddComponent<RectMask2D>();
        }

        mascara.padding = mascaraPadding;
    }

    private void PrepararSistemaCaminos()
    {
        textoOriginalCamino.Clear();
        slotsPorTextoCamino.Clear();

        if (usarSistemaCaminoPorObjetos)
        {
            if (autoCapturarCaminosDesdeJerarquia)
            {
                AutoCapturarCaminosDesdeJerarquia();
            }

            CapturarTextosOriginalesCaminoPorObjetos();
        }
        else
        {
            CapturarTextosOriginalesCaminoAntiguo();
        }
    }

    private void AutoCapturarCaminosDesdeJerarquia()
    {
        Transform root = caminosRoot;

        if (root == null && expandedCard != null)
        {
            root = expandedCard.Find("caminos");
        }

        if (root == null)
        {
            Debug.LogWarning("PROGRESS: no se encontró el objeto 'caminos'. Asigna Caminos Root.");
            return;
        }

        caminosRoot = root;
        caminosEnOrden.Clear();

        for (int caminoIndex = 1; caminoIndex <= 50; caminoIndex++)
        {
            Transform caminoTransform = root.Find("camino" + caminoIndex);

            if (caminoTransform == null)
            {
                break;
            }

            CaminoVisual camino = new CaminoVisual();
            camino.nombreCamino = caminoTransform.name;

            for (int parteIndex = 1; parteIndex <= 50; parteIndex++)
            {
                Transform parteTransform = caminoTransform.Find("part" + parteIndex);

                if (parteTransform == null)
                {
                    break;
                }

                TMP_Text texto = parteTransform.GetComponent<TMP_Text>();

                if (texto != null)
                {
                    camino.partes.Add(texto);
                }
            }

            if (camino.partes.Count > 0)
            {
                caminosEnOrden.Add(camino);
            }
        }

        Debug.Log("PROGRESS: caminos capturados automáticamente: " + caminosEnOrden.Count);
    }

    private void CapturarTextosOriginalesCaminoPorObjetos()
    {
        for (int i = 0; i < caminosEnOrden.Count; i++)
        {
            CaminoVisual camino = caminosEnOrden[i];

            if (camino == null || camino.partes == null)
            {
                continue;
            }

            for (int j = 0; j < camino.partes.Count; j++)
            {
                TMP_Text texto = camino.partes[j];

                if (texto == null)
                {
                    continue;
                }

                if (!textoOriginalCamino.ContainsKey(texto))
                {
                    textoOriginalCamino.Add(texto, texto.text);
                }

                int slots = ContarCaracteresEditables(texto.text);

                if (!slotsPorTextoCamino.ContainsKey(texto))
                {
                    slotsPorTextoCamino.Add(texto, Mathf.Max(1, slots));
                }
            }
        }
    }

    private void CapturarTextosOriginalesCaminoAntiguo()
    {
        if (pathPartsInOrder == null)
        {
            return;
        }

        for (int i = 0; i < pathPartsInOrder.Length; i++)
        {
            TMP_Text texto = pathPartsInOrder[i];

            if (texto == null)
            {
                continue;
            }

            if (!textoOriginalCamino.ContainsKey(texto))
            {
                textoOriginalCamino.Add(texto, texto.text);
            }

            int slots = ContarCaracteresEditables(texto.text);

            if (!slotsPorTextoCamino.ContainsKey(texto))
            {
                slotsPorTextoCamino.Add(texto, Mathf.Max(1, slots));
            }
        }
    }

    private int ContarCaracteresEditables(string texto)
    {
        if (string.IsNullOrEmpty(texto))
        {
            return 1;
        }

        int cantidad = 0;

        for (int i = 0; i < texto.Length; i++)
        {
            if (!char.IsWhiteSpace(texto[i]))
            {
                cantidad++;
            }
        }

        return Mathf.Max(1, cantidad);
    }

    private int ObtenerCantidadCaminos()
    {
        if (usarSistemaCaminoPorObjetos && caminosEnOrden != null && caminosEnOrden.Count > 0)
        {
            return caminosEnOrden.Count;
        }

        if (pathPartCounts != null)
        {
            return pathPartCounts.Length;
        }

        return 0;
    }

    private void IniciarAnimacionCaminoDesdeNivelAnterior(int nivelAnterior, int nivelNuevo)
    {
        DetenerAnimacionCamino();

        int cantidadCaminos = ObtenerCantidadCaminos();

        int desde = Mathf.Clamp(nivelAnterior, 0, cantidadCaminos);
        int hasta = Mathf.Clamp(nivelNuevo, 0, cantidadCaminos);

        if (hasta < desde)
        {
            desde = 0;
            nivelVisualActual = 0;
            caminosDibujados = 0;
            ReiniciarTodosLosCaminos();
            ActualizarLevelsInmediato();
        }

        if (hasta == desde)
        {
            AplicarCaminosInmediatosHasta(hasta);
            caminosDibujados = hasta;
            nivelVisualActual = hasta;
            currentLevelIndex = hasta;
            ActualizarLevelsInmediato();
            return;
        }

        pathRoutine = StartCoroutine(AnimarCaminosDesdeHasta(desde, hasta));
    }

    private IEnumerator AnimarCaminosDesdeHasta(int desdeCamino, int hastaCamino)
    {
        AplicarCaminosInmediatosHasta(desdeCamino);

        nivelVisualActual = desdeCamino;
        caminosDibujados = desdeCamino;
        ActualizarLevelsInmediato();

        for (int camino = desdeCamino; camino < hastaCamino; camino++)
        {
            List<TMP_Text> partes = ObtenerPartesDeCamino(camino);

            for (int i = 0; i < partes.Count; i++)
            {
                TMP_Text texto = partes[i];

                if (texto != null)
                {
                    yield return AnimarParteCamino(texto);
                }
            }

            caminosDibujados = camino + 1;
            nivelVisualActual = camino + 1;
            currentLevelIndex = camino + 1;

            ActualizarLevelsInmediato();

            yield return null;
        }

        AplicarCaminosInmediatosHasta(hastaCamino);

        caminosDibujados = hastaCamino;
        nivelVisualActual = hastaCamino;
        currentLevelIndex = hastaCamino;

        ActualizarLevelsInmediato();
        ActualizarNivel();
        ActualizarBotonPractica();
        ActualizarPuntaje();

        pathRoutine = null;

        Debug.Log(
            "PROGRESS: animación de camino terminada | nivelVisualActual=" +
            nivelVisualActual +
            " | currentLevelIndex=" +
            currentLevelIndex
        );
    }

    private IEnumerator AnimarParteCamino(TMP_Text texto)
    {
        if (texto == null)
        {
            yield break;
        }

        texto.text = ObtenerTextoOriginalCamino(texto);

        yield return new WaitForSecondsRealtime(Mathf.Max(0f, pathStepDelay));

        int slots = ObtenerSlotsTextoCamino(texto);

        for (int i = 0; i < slots; i++)
        {
            texto.text = CrearTextoCaminoConCaracteres(texto, i + 1);
            yield return new WaitForSecondsRealtime(Mathf.Max(0f, pathStepDelay));
        }
    }

    private void AplicarCaminosHastaNivelActualInmediato()
    {
        int cantidadCaminos = ObtenerCantidadCaminos();
        int hasta = Mathf.Clamp(currentLevelIndex, 0, cantidadCaminos);

        AplicarCaminosInmediatosHasta(hasta);

        caminosDibujados = hasta;
        nivelVisualActual = hasta;
    }

    private void AplicarCaminosInmediatosHasta(int hastaCamino)
    {
        int cantidadCaminos = ObtenerCantidadCaminos();

        for (int camino = 0; camino < cantidadCaminos; camino++)
        {
            bool completo = camino < hastaCamino;

            List<TMP_Text> partes = ObtenerPartesDeCamino(camino);

            for (int i = 0; i < partes.Count; i++)
            {
                TMP_Text texto = partes[i];

                if (texto == null)
                {
                    continue;
                }

                if (completo)
                {
                    int slots = ObtenerSlotsTextoCamino(texto);
                    texto.text = CrearTextoCaminoConCaracteres(texto, slots);
                }
                else
                {
                    texto.text = ObtenerTextoOriginalCamino(texto);
                }
            }
        }
    }

    private void ReiniciarTodosLosCaminos()
    {
        int cantidadCaminos = ObtenerCantidadCaminos();

        for (int camino = 0; camino < cantidadCaminos; camino++)
        {
            List<TMP_Text> partes = ObtenerPartesDeCamino(camino);

            for (int i = 0; i < partes.Count; i++)
            {
                TMP_Text texto = partes[i];

                if (texto != null)
                {
                    texto.text = ObtenerTextoOriginalCamino(texto);
                }
            }
        }

        caminosDibujados = 0;
    }

    private List<TMP_Text> ObtenerPartesDeCamino(int caminoIndex)
    {
        if (usarSistemaCaminoPorObjetos &&
            caminosEnOrden != null &&
            caminoIndex >= 0 &&
            caminoIndex < caminosEnOrden.Count &&
            caminosEnOrden[caminoIndex] != null &&
            caminosEnOrden[caminoIndex].partes != null)
        {
            return caminosEnOrden[caminoIndex].partes;
        }

        return ObtenerPartesDeCaminoAntiguo(caminoIndex);
    }

    private List<TMP_Text> ObtenerPartesDeCaminoAntiguo(int caminoIndex)
    {
        List<TMP_Text> resultado = new List<TMP_Text>();

        if (pathPartsInOrder == null || pathPartCounts == null)
        {
            return resultado;
        }

        int inicio = 0;

        for (int i = 0; i < caminoIndex && i < pathPartCounts.Length; i++)
        {
            inicio += pathPartCounts[i];
        }

        if (caminoIndex < 0 || caminoIndex >= pathPartCounts.Length)
        {
            return resultado;
        }

        int cantidad = pathPartCounts[caminoIndex];

        for (int i = 0; i < cantidad; i++)
        {
            int indice = inicio + i;

            if (indice >= 0 && indice < pathPartsInOrder.Length)
            {
                resultado.Add(pathPartsInOrder[indice]);
            }
        }

        return resultado;
    }

    private string ObtenerTextoOriginalCamino(TMP_Text texto)
    {
        if (texto == null)
        {
            return "";
        }

        if (conservarTextoOriginalCaminoPendiente &&
            textoOriginalCamino.ContainsKey(texto) &&
            !string.IsNullOrWhiteSpace(textoOriginalCamino[texto]))
        {
            return textoOriginalCamino[texto];
        }

        int slots = ObtenerSlotsTextoCamino(texto);
        return CrearTextoSimplePendiente(slots);
    }

    private int ObtenerSlotsTextoCamino(TMP_Text texto)
    {
        if (texto == null)
        {
            return 1;
        }

        if (slotsPorTextoCamino.ContainsKey(texto))
        {
            return Mathf.Max(1, slotsPorTextoCamino[texto]);
        }

        int slots = ContarCaracteresEditables(texto.text);
        slotsPorTextoCamino[texto] = slots;

        return Mathf.Max(1, slots);
    }

    private string CrearTextoCaminoConCaracteres(TMP_Text texto, int completados)
    {
        string original = ObtenerTextoOriginalCamino(texto);
        int slots = ObtenerSlotsTextoCamino(texto);

        completados = Mathf.Clamp(completados, 0, slots);

        if (!reemplazarCaracteresSinCambiarLongitud)
        {
            return CrearTextoSimpleCompletado(slots, completados);
        }

        char[] chars = original.ToCharArray();
        int indiceEditable = 0;

        for (int i = 0; i < chars.Length; i++)
        {
            if (char.IsWhiteSpace(chars[i]))
            {
                continue;
            }

            if (indiceEditable < completados)
            {
                chars[i] = binaryPattern[indiceEditable % binaryPattern.Length][0];
            }

            indiceEditable++;
        }

        return new string(chars);
    }

    private string CrearTextoSimplePendiente(int slots)
    {
        string resultado = "";

        for (int i = 0; i < slots; i++)
        {
            if (i > 0)
            {
                resultado += " ";
            }

            resultado += "-";
        }

        return resultado;
    }

    private string CrearTextoSimpleCompletado(int slots, int completados)
    {
        completados = Mathf.Clamp(completados, 0, slots);

        string resultado = "";

        for (int i = 0; i < slots; i++)
        {
            if (i > 0)
            {
                resultado += " ";
            }

            if (i < completados)
            {
                resultado += binaryPattern[i % binaryPattern.Length];
            }
            else
            {
                resultado += "-";
            }
        }

        return resultado;
    }

    private void DetenerAnimacionCamino()
    {
        if (pathRoutine != null)
        {
            StopCoroutine(pathRoutine);
            pathRoutine = null;
        }
    }

    private void ActualizarSeleccionNivel()
    {
        if (!activarSeleccionNiveles || !isExpanded || levels == null || expandedCard == null)
        {
            RestaurarHoverTodos();
            nivelSeleccionadoActual = -1;
            return;
        }

        int nivelBajoRayoIzquierdo = ObtenerNivelBajoRayo(leftRayOrigin);
        int nivelBajoRayoDerecho = ObtenerNivelBajoRayo(rightRayOrigin);
        int nivelBajoPuntero = nivelBajoRayoIzquierdo != -1
            ? nivelBajoRayoIzquierdo
            : nivelBajoRayoDerecho;

        bool presionoIzquierdo = PresionoGatilloIndice(OVRInput.Controller.LTouch);
        bool presionoDerecho = PresionoGatilloIndice(OVRInput.Controller.RTouch);
        int nivelConfirmadoPorGatillo = presionoIzquierdo && nivelBajoRayoIzquierdo != -1
            ? nivelBajoRayoIzquierdo
            : presionoDerecho && nivelBajoRayoDerecho != -1
                ? nivelBajoRayoDerecho
                : -1;

        if (BloquearCambioNivelPorFlujoActivo())
        {
            // Mientras una práctica/tema/guía sigue viva, el usuario puede mirar otros niveles,
            // pero NO deben cambiar ni el collapse ni el botón principal.
            nivelSeleccionadoActual = -1;
            RestaurarHoverTodos();

            bool presionoGatilloBloqueado =
                nivelConfirmadoPorGatillo != -1 && seleccionarNivelConGatillo;

            if (presionoGatilloBloqueado)
            {
                RefrescarTextoFlujoActual();
                RestaurarUIFlujoPendienteMismoNivel();
                Debug.Log("PROGRESS: selección de nivel ignorada porque hay un flujo activo.");
            }

            return;
        }

        if (nivelBajoPuntero != nivelSeleccionadoActual)
        {
            nivelSeleccionadoActual = nivelBajoPuntero;

            if (estadoFlujoNivel == EstadoFlujoNivel.Ninguno && nivelSeleccionadoActual != -1)
            {
                if (NivelDisponible(nivelSeleccionadoActual))
                {
                    ActualizarTextoNivelSeleccionado(nivelSeleccionadoActual);
                }
            }
        }

        ActualizarHoverNivel(nivelSeleccionadoActual);

        if (nivelConfirmadoPorGatillo != -1 && seleccionarNivelConGatillo)
        {
            // Cada gatillo confirma exclusivamente el nivel bajo el rayo de su
            // propio mando. Antes, cualquier gatillo podia aceptar el rayo de la
            // otra mano (y el rayo izquierdo siempre tenia prioridad).
            nivelSeleccionadoActual = nivelConfirmadoPorGatillo;
            ActualizarHoverNivel(nivelSeleccionadoActual);

            if (NivelDisponible(nivelSeleccionadoActual))
            {
                SeleccionarNivelParaIniciar(nivelSeleccionadoActual);
            }
            else
            {
                Debug.Log("Nivel bloqueado. No se puede seleccionar.");
            }
        }
    }

    private bool PresionoGatilloIndice(OVRInput.Controller controller)
    {
        try
        {
            // PrimaryIndexTrigger representa el indice del controlador concreto.
            return OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, controller);
        }
        catch
        {
            return false;
        }
    }

    private void ActualizarTextoNivelSeleccionado(int index)
    {
        if (index < 0 || levels == null || index >= levels.Length)
        {
            ActualizarNivel();
            return;
        }

        // Defensa extra: aunque otra función llame directamente a actualizar el texto,
        // si hay un flujo protegido no dejamos que un nivel distinto reemplace la UI actual.
        if (EsOtroNivelMientrasFlujoProtegido(index) || DebeIgnorarSolicitudDeOtroNivel(index))
        {
            RefrescarTextoFlujoActual();
            RestaurarUIFlujoPendienteMismoNivel();
            return;
        }

        AlgoLabProgressLevelInfo info = ObtenerInfoNivel(index);

        if (info == null)
        {
            if (levelNameText != null)
            {
                levelNameText.text = "Nivel " + (index + 1);
            }

            if (descriptionOrTaskText != null)
            {
                descriptionOrTaskText.text = "Sin descripción configurada para este nivel.";
            }

            if (timerText != null)
            {
                timerText.gameObject.SetActive(false);
            }

            return;
        }

        if (levelNameText != null)
        {
            levelNameText.text = string.IsNullOrWhiteSpace(info.nombreNivel)
                ? "Nivel " + (index + 1)
                : info.nombreNivel;
        }

        if (currentModeText != null)
        {
            currentModeText.text = "Aprendiendo";
        }

        if (descriptionOrTaskText != null)
        {
            descriptionOrTaskText.text = string.IsNullOrWhiteSpace(info.descripcionNivel)
                ? "Sin descripción configurada para este nivel."
                : info.descripcionNivel;
        }

        if (timerText != null)
        {
            timerText.gameObject.SetActive(false);
        }
    }

    private AlgoLabProgressLevelInfo ObtenerInfoNivel(int index)
    {
        if (levels == null || index < 0 || index >= levels.Length || levels[index] == null)
        {
            return null;
        }

        if (levels[index].levelInfo != null)
        {
            return levels[index].levelInfo;
        }

        if (levels[index].levelObject != null)
        {
            return levels[index].levelObject.GetComponent<AlgoLabProgressLevelInfo>();
        }

        return null;
    }

    private void IntentarCargarEscenaNivel(int index)
    {
        AlgoLabProgressLevelInfo info = ObtenerInfoNivel(index);

        if (info == null || string.IsNullOrWhiteSpace(info.nombreEscena))
        {
            Debug.Log("Este nivel no tiene escena configurada.");
            return;
        }

        try
        {
            SceneManager.LoadScene(info.nombreEscena);
        }
        catch (System.Exception error)
        {
            Debug.LogError(
                "No se pudo cargar la escena '" +
                info.nombreEscena +
                "'. Revisa que está en Build Settings. Error: " +
                error.Message
            );
        }
    }

    private int ObtenerNivelBajoRayos()
    {
        int izquierdo = ObtenerNivelBajoRayo(leftRayOrigin);

        if (izquierdo != -1)
        {
            return izquierdo;
        }

        return ObtenerNivelBajoRayo(rightRayOrigin);
    }

    private int ObtenerNivelBajoRayo(Transform rayOrigin)
    {
        if (rayOrigin == null || expandedCard == null || levels == null)
        {
            return -1;
        }

        Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
        Plane planoPanel = new Plane(expandedCard.forward, expandedCard.position);

        if (!planoPanel.Raycast(ray, out float distancia))
        {
            return -1;
        }

        if (distancia < 0f || distancia > distanciaMaximaSeleccionNivel)
        {
            return -1;
        }

        Vector3 puntoMundo = ray.GetPoint(distancia);

        for (int i = levels.Length - 1; i >= 0; i--)
        {
            RectTransform rectNivel = ObtenerRectTransformNivel(i);

            if (rectNivel != null)
            {
                Vector3 puntoLocal = rectNivel.InverseTransformPoint(puntoMundo);
                Vector2 puntoLocal2D = new Vector2(puntoLocal.x, puntoLocal.y);

                if (rectNivel.rect.Contains(puntoLocal2D))
                {
                    return i;
                }
            }
            else
            {
                Transform objetivo = ObtenerHoverTarget(i);

                if (objetivo != null)
                {
                    float distanciaRayo = Vector3.Cross(
                        ray.direction,
                        objetivo.position - ray.origin
                    ).magnitude;

                    if (distanciaRayo <= hoverWorldRadiusFallback)
                    {
                        return i;
                    }
                }
            }
        }

        return -1;
    }

    private RectTransform ObtenerRectTransformNivel(int index)
    {
        if (levels == null || index < 0 || index >= levels.Length || levels[index] == null)
        {
            return null;
        }

        GameObject activo = ObtenerObjetoEstado(levels[index], CalcularEstadoNivel(index));

        if (activo != null)
        {
            RectTransform rectActivo = activo.GetComponent<RectTransform>();

            if (rectActivo != null)
            {
                return rectActivo;
            }
        }

        if (levels[index].levelObject != null)
        {
            return levels[index].levelObject.GetComponent<RectTransform>();
        }

        return null;
    }

    private Transform ObtenerHoverTarget(int index)
    {
        if (levels == null || index < 0 || index >= levels.Length || levels[index] == null)
        {
            return null;
        }

        GameObject activo = ObtenerObjetoEstado(levels[index], CalcularEstadoNivel(index));

        if (activo != null)
        {
            return activo.transform;
        }

        if (levels[index].levelObject != null)
        {
            return levels[index].levelObject.transform;
        }

        return null;
    }

    private void ActualizarHoverNivel(int indexSeleccionado)
    {
        if (levels == null)
        {
            return;
        }

        for (int i = 0; i < levels.Length; i++)
        {
            Transform objetivo = ObtenerHoverTarget(i);

            if (objetivo == null)
            {
                continue;
            }

            RegistrarTransformHover(objetivo);

            Vector3 posicionBase = hoverPosicionesOriginales[objetivo];
            Vector3 escalaBase = hoverEscalasOriginales[objetivo];

            bool estaSeleccionado = i == indexSeleccionado;

            Vector3 posicionObjetivo = posicionBase + (estaSeleccionado ? Vector3.up * hoverYOffset : Vector3.zero);
            Vector3 escalaObjetivo = escalaBase * (estaSeleccionado ? hoverScale : 1f);

            objetivo.localPosition = Vector3.Lerp(
                objetivo.localPosition,
                posicionObjetivo,
                Time.unscaledDeltaTime * hoverSmooth
            );

            objetivo.localScale = Vector3.Lerp(
                objetivo.localScale,
                escalaObjetivo,
                Time.unscaledDeltaTime * hoverSmooth
            );
        }
    }

    private void RegistrarTransformHover(Transform objetivo)
    {
        if (objetivo == null)
        {
            return;
        }

        if (!hoverPosicionesOriginales.ContainsKey(objetivo))
        {
            hoverPosicionesOriginales[objetivo] = objetivo.localPosition;
        }

        if (!hoverEscalasOriginales.ContainsKey(objetivo))
        {
            hoverEscalasOriginales[objetivo] = objetivo.localScale;
        }
    }

    private void RestaurarHoverTodos()
    {
        foreach (KeyValuePair<Transform, Vector3> item in hoverPosicionesOriginales)
        {
            if (item.Key != null)
            {
                item.Key.localPosition = Vector3.Lerp(
                    item.Key.localPosition,
                    item.Value,
                    Time.unscaledDeltaTime * hoverSmooth
                );
            }
        }

        foreach (KeyValuePair<Transform, Vector3> item in hoverEscalasOriginales)
        {
            if (item.Key != null)
            {
                item.Key.localScale = Vector3.Lerp(
                    item.Key.localScale,
                    item.Value,
                    Time.unscaledDeltaTime * hoverSmooth
                );
            }
        }
    }

    private void AsegurarPillarLevelController()
    {
        if (pillarLevelController == null)
        {
            pillarLevelController = FindFirstObjectByType<AlgoLabPillarLevelController>(
                FindObjectsInactive.Include
            );
        }

        if (pillarLevelController == null)
        {
            pillarLevelController = gameObject.AddComponent<AlgoLabPillarLevelController>();
        }

        pillarLevelController.AsegurarNivelesPorDefecto();
    }

    private void RestaurarHoverTodosInmediato()
    {
        foreach (KeyValuePair<Transform, Vector3> item in hoverPosicionesOriginales)
        {
            if (item.Key != null)
                item.Key.localPosition = item.Value;
        }

        foreach (KeyValuePair<Transform, Vector3> item in hoverEscalasOriginales)
        {
            if (item.Key != null)
                item.Key.localScale = item.Value;
        }

        nivelSeleccionadoActual = -1;
    }

    private void DetenerRutinasVisuales(bool restaurarVisuales)
    {
        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
            transitionRoutine = null;
        }

        if (pathRoutine != null)
        {
            StopCoroutine(pathRoutine);
            pathRoutine = null;
        }

        if (buttonRoutine != null)
        {
            StopCoroutine(buttonRoutine);
            buttonRoutine = null;
        }

        if (levelRoutines != null)
        {
            for (int i = 0; i < levelRoutines.Length; i++)
            {
                if (levelRoutines[i] == null)
                    continue;

                StopCoroutine(levelRoutines[i]);
                levelRoutines[i] = null;
            }
        }

        if (restaurarVisuales)
            AplicarEstadoVisualInterrumpibleInmediato();
    }

    private void AplicarEstadoVisualInterrumpibleInmediato()
    {
        RestaurarHoverTodosInmediato();
        ActualizarLevelsInmediato();
        AplicarCaminosHastaNivelActualInmediato();
        AplicarBotonAccionInmediato(botonAccionDebeEstarVisible);

        if (isExpanded)
            MostrarExpandidoInmediato();
        else
            MostrarContraidoInmediato();
    }

    private void OnPracticePressed()
    {
        if (estadoFlujoNivel == EstadoFlujoNivel.TemaEnCurso && EsPilarActivo())
        {
            TerminarTemaActual();
            return;
        }

        if (estadoFlujoNivel == EstadoFlujoNivel.PracticaEnCurso && EsPilarActivo())
        {
            TerminarPracticaActual();
            return;
        }

        if (estadoFlujoNivel == EstadoFlujoNivel.NivelSeleccionado)
        {
            ComenzarTemaNivel();
            return;
        }

        if (estadoFlujoNivel == EstadoFlujoNivel.TemaTerminado)
        {
            PrepararPracticaNivel();
            return;
        }

        if (estadoFlujoNivel == EstadoFlujoNivel.PracticaPreparada)
        {
            ComenzarPracticaNivel();
            return;
        }

        if (currentMode == ModoActual.Aprendiendo)
        {
            currentMode = ModoActual.Practica;
            ActualizarTodo();
            Debug.Log("Cambiando a modo práctica.");
        }
        else
        {
            Debug.Log("Iniciando práctica del nivel: " + levelName);
        }
    }

    private string ObtenerCategoriaTexto(CategoriaUsuario categoria)
    {
        switch (categoria)
        {
            case CategoriaUsuario.Junior:
                return "Junior";

            case CategoriaUsuario.SemiSenior:
                return "Semi-Senior";

            case CategoriaUsuario.Senior:
                return "Senior";

            default:
                return "Junior";
        }
    }

    private string ObtenerPrimerNombre(string nombreCompleto)
    {
        if (string.IsNullOrWhiteSpace(nombreCompleto))
        {
            return "";
        }

        string limpio = nombreCompleto.Trim();

        int indiceEspacio = limpio.IndexOf(' ');

        if (indiceEspacio <= 0)
        {
            return limpio;
        }

        return limpio.Substring(0, indiceEspacio);
    }

    private CanvasGroup GetOrAddCanvasGroup(GameObject obj)
    {
        if (obj == null)
        {
            return null;
        }

        CanvasGroup group = obj.GetComponent<CanvasGroup>();

        if (group == null)
        {
            group = obj.AddComponent<CanvasGroup>();
        }

        return group;
    }

    private void SetGroup(CanvasGroup group, float alpha, bool interactable)
    {
        if (group == null)
        {
            return;
        }

        group.alpha = alpha;
        group.interactable = interactable;
        group.blocksRaycasts = interactable;
    }

    private void SetActiveSafe(GameObject obj, bool active)
    {
        if (obj != null)
        {
            obj.SetActive(active);
        }
    }
}
