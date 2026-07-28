using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AlgoLabLevel02PracticeController : MonoBehaviour
{
    [System.Serializable]
    public class VehiculoRequerido
    {
        [Header("Identificación")]
        public string nombreVehiculo = "Vehículo 1 de 5";

        [Header("Atributos requeridos")]
        public string color = "rojo";
        public string modelo = "2024";
        public string carcasa = "Hatchback";
        public string estado = "nuevo";

        [Header("Método requerido")]
        public string metodo = "encender()";

        [Header("Estado")]
        public bool completado = false;
    }

    private static readonly string[] ColoresVehiculo =
        { "rojo", "azul", "negro", "blanco", "amarillo" };
    private static readonly string[] ModelosVehiculo =
        { "2020", "2021", "2022", "2023", "2024" };
    private static readonly string[] CarcasasVehiculo =
        { "Hatchback", "Pickup", "Towtruck", "Police" };
    private static readonly string[] EstadosVehiculo =
        { "nuevo", "seminuevo", "usado" };
    private static readonly string[] MetodosVehiculo =
        { "encender()", "acelerar()", "frenar()", "apagar()" };

    [Header("Anti doble envío")]
    public float tiempoAntiDobleEnvioCrear = 0.45f;

    private string ultimaFirmaVehiculoEnviado = "";
    private float tiempoUltimoVehiculoEnviado = -99f;

    [Header("Protección al crear vehículo correcto")]
    [Tooltip("Evita penalizar si el garaje destruye/desaparece el vehículo correcto después de crearlo.")]
    public float tiempoIgnorarDestruccionTrasCrearCorrecto = 1.25f;

    private float ignorarDestruccionesHasta = -99f;

    [Header("Nivel")]
    public int numeroNivelReal = 2;
    public int indiceNivelEnProgressPanel = 1;

    [Header("Guía de práctica")]
    public AlgoLabTemaPOOController guiaPracticaController;
    public bool mostrarBotonIniciarAlTerminarGuia = true;
    [Header("Tutorial multimedia de la practica")]
    public AlgoLabPracticeTutorialSequence tutorialMultimedia;


    [Header("Modo objeto")]
    public AlgoLabClassDiagramModeManager modeManager;

    [Header("Panel de progreso")]
    public MonoBehaviour progressPanel;

    [Header("Botón iniciar práctica directo")]
    public Button btnIniciarPractica;
    public TMP_Text textoBtnIniciarPractica;
    public string textoBotonIniciar = "Iniciar";

    [Header("Garaje")]
    public MonoBehaviour garageController;
    public string metodoCrearVehiculoEnGarage = "CrearVehiculoDesdeModoObjeto";

    [Header("Textos del progress panel")]
    public TMP_Text textoTituloCollapse;
    public TMP_Text textoCuerpoCollapse;
    public TMP_Text textoEstadoPractica;
    public TMP_Text textoCronometro;

    [Header("Configuración de práctica")]
    public bool cambiarAModoObjetoAlIniciar = true;
    public bool mostrarBotonCambiarModo = true;
    public bool prepararGarageAlIniciar = true;

    [Header("Orden automático de vehículos")]
    public bool cargarOrdenVehiculosNivel2AlIniciar = true;

    [Tooltip("Cada intento crea una lista nueva de combinaciones válidas.")]
    [Range(1, 10)] public int cantidadVehiculosAleatorios = 5;

    [Tooltip("Con tres o más vehículos garantiza al menos uno nuevo, uno seminuevo y uno usado.")]
    public bool garantizarTodosLosEstados = true;

    [Header("Cronómetro")]
    [Tooltip("Duración de la práctica en segundos. 240 segundos = 4 minutos.")]
    public float duracionPracticaSegundos = 240f;

    public bool detenerPracticaAlAcabarTiempo = true;

    public string mensajeTiempoAgotado =
        "No te preocupes.\nEl tiempo se terminó.\nPuedes volver a intentarlo.";

    [Header("Audio final práctica")]
    public bool reproducirAudioFinal = true;

    public AudioSource audioSourceFinal;
    public AudioClip audioFelicitacion;
    public AudioClip audioTiempoAgotado;

    public bool detenerAudioActualAntesDeAudioFinal = true;

    [Header("Reintento al perder")]
    public bool permitirReintentoAlPerder = true;

    [Tooltip("Si está activado, cuando pierde vuelve a iniciar la práctica automáticamente después del audio.")]
    public bool reiniciarAutomaticamenteAlPerder = false;

    [Tooltip("Si está activado, al reintentar vuelve a reproducir la guía. Si está desactivado, vuelve directo a la práctica.")]
    public bool repetirGuiaAlReintentar = false;

    public string mensajeReintentoDisponible =
        "No te preocupes.\nPresiona Iniciar para volver a intentarlo.";

    public float tiempoExtraDespuesAudioPerder = 0.3f;

    public bool limpiarGarageAlReintentar = true;
    public string metodoLimpiarGarageAlReintentar = "LimpiarVehiculosCreados";

    [Header("UI práctica")]
    public Color colorDescripcionPractica = Color.white;
    public float duracionMensajeError = 3f;
    public bool limpiarTextoEstadoParaNoTaparInstruccion = true;
    public bool ajustarTamanioDescripcion = true;
    public float tamanioDescripcion = 30f;
    public float espaciadoLineasDescripcion = 0f;

    [Header("Vehículos requeridos")]
    public List<VehiculoRequerido> vehiculosRequeridos = new List<VehiculoRequerido>();

    [Header("Puntaje y guardado")]
    public int puntosMenosPorVehiculoIncorrecto = 10;
    public int puntosMenosPorVehiculoDestruido = 60;
    public bool guardarProgresoAlCompletar = true;

    [Tooltip("Normalmente déjalo desactivado. Si se activa, también guarda cuando pierde con puntaje 0.")]
    public bool guardarIntentoFallido = false;

    [Header("Mensajes")]
    public string mensajeGuiaIniciada =
        "Escucha la guía de práctica.\nLuego presiona Iniciar para comenzar.";

    public string mensajePracticaLista =
        "Presiona Iniciar para comenzar la práctica.";

    public string mensajePracticaCompletada =
        "¡Felicidades!\nCompletaste correctamente la práctica del nivel 2.\nYa puedes continuar al siguiente nivel.";

    [Header("Debug")]
    public bool mostrarDebug = true;

    private int indiceVehiculoActual = 0;
    private int vehiculosCorrectos = 0;

    private bool practicaActiva = false;
    private bool guiaIniciada = false;
    private bool guiaTerminada = false;
    private bool guiaCanceladaPorCambioFlujo = false;

    private bool cronometroActivo = false;
    private float tiempoRestante;

    private bool resultadoFinalEmitido = false;

    private int penalizacionPuntaje = 0;
    private int erroresVehiculoIncorrecto = 0;
    private int vehiculosDestruidos = 0;
    private int intentosPractica = 0;

    private Coroutine rutinaMensajeError;
    private Coroutine rutinaReintentoAlPerder;

    private Color colorOriginalTitulo;
    private Color colorOriginalEstado;
    private bool coloresOriginalesGuardados = false;

    private bool PuedePrepararGuiaDesdeProgress()
    {
        return ConsultarBoolProgress("PuedePrepararGuiaDesdeControladorNivel", true);
    }

    private bool PuedeIniciarPracticaDesdeProgress()
    {
        return ConsultarBoolProgress("PuedeIniciarPracticaDesdeControladorNivel", true);
    }

    private bool PuedeModificarPanelProgress()
    {
        return ConsultarBoolProgress("PuedeControladorModificarPanelNivel", true);
    }

    private bool ConsultarBoolProgress(string nombreMetodo, bool valorSiNoExiste)
    {
        if (progressPanel == null || string.IsNullOrWhiteSpace(nombreMetodo))
        {
            return valorSiNoExiste;
        }

        MethodInfo metodo = progressPanel.GetType().GetMethod(
            nombreMetodo,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
        );

        if (metodo == null)
        {
            return valorSiNoExiste;
        }

        ParameterInfo[] parametros = metodo.GetParameters();

        if (parametros.Length != 1 || parametros[0].ParameterType != typeof(int))
        {
            return valorSiNoExiste;
        }

        try
        {
            object resultado = metodo.Invoke(progressPanel, new object[] { numeroNivelReal });

            if (resultado is bool permitido)
            {
                return permitido;
            }
        }
        catch
        {
        }

        return valorSiNoExiste;
    }

    private void SendMessageProgressSeguro(string nombreMetodo)
    {
        if (progressPanel == null || string.IsNullOrWhiteSpace(nombreMetodo))
        {
            return;
        }

        progressPanel.SendMessage(
            nombreMetodo,
            numeroNivelReal,
            SendMessageOptions.DontRequireReceiver
        );
    }

    private void Awake()
    {
        if (cargarOrdenVehiculosNivel2AlIniciar)
        {
            CargarVehiculosRequeridosNivel2();
        }

        BuscarReferencias();
        GuardarColoresOriginales();
        ConfigurarTextosVisuales();
        ReiniciarCronometroVisual();
    }

    private void OnEnable()
    {
        BuscarReferencias();
        GuardarColoresOriginales();
        ConfigurarTextosVisuales();
        ConectarEventoCrearObjeto();
        ConectarEventoFinGuia();
        ReiniciarCronometroVisual();
    }

    private void OnDisable()
    {
        DesconectarEventoCrearObjeto();
        DesconectarEventoFinGuia();

        if (btnIniciarPractica != null)
            btnIniciarPractica.onClick.RemoveListener(IniciarPracticaNivel2);

        CancelarTodoNivel2PorCambioDeFlujo(false);
    }

    private void Update()
    {
        ActualizarCronometro();
    }

    [ContextMenu("Cargar vehículos requeridos nivel 2")]
    public void CargarVehiculosRequeridosNivel2()
    {
        if (vehiculosRequeridos == null)
        {
            vehiculosRequeridos = new List<VehiculoRequerido>();
        }

        vehiculosRequeridos.Clear();

        int cantidad = Mathf.Clamp(cantidadVehiculosAleatorios, 1, 10);
        var estadosDelIntento = new List<string>(cantidad);

        if (garantizarTodosLosEstados && cantidad >= EstadosVehiculo.Length)
        {
            estadosDelIntento.AddRange(EstadosVehiculo);
        }

        while (estadosDelIntento.Count < cantidad)
        {
            estadosDelIntento.Add(ElegirAleatorio(EstadosVehiculo));
        }

        Mezclar(estadosDelIntento);
        var firmasUsadas = new HashSet<string>();

        for (int i = 0; i < cantidad; i++)
        {
            VehiculoRequerido requerido = null;

            // La cantidad de combinaciones posibles es muy superior a diez.
            // Aun así se limita el reintento para que una configuración futura
            // de pools pequeños nunca pueda crear un bucle infinito.
            for (int intento = 0; intento < 40; intento++)
            {
                var candidato = new VehiculoRequerido
                {
                    nombreVehiculo = "Vehículo " + (i + 1) + " de " + cantidad,
                    color = ElegirAleatorio(ColoresVehiculo),
                    modelo = ElegirAleatorio(ModelosVehiculo),
                    carcasa = ElegirAleatorio(CarcasasVehiculo),
                    estado = estadosDelIntento[i],
                    metodo = ElegirAleatorio(MetodosVehiculo),
                    completado = false
                };

                string firma = candidato.color + "|" + candidato.modelo + "|" +
                               candidato.carcasa + "|" + candidato.estado + "|" +
                               candidato.metodo;
                if (firmasUsadas.Add(firma))
                {
                    requerido = candidato;
                    break;
                }
            }

            if (requerido == null)
            {
                requerido = new VehiculoRequerido
                {
                    nombreVehiculo = "Vehículo " + (i + 1) + " de " + cantidad,
                    color = ColoresVehiculo[i % ColoresVehiculo.Length],
                    modelo = ModelosVehiculo[i % ModelosVehiculo.Length],
                    carcasa = CarcasasVehiculo[i % CarcasasVehiculo.Length],
                    estado = estadosDelIntento[i],
                    metodo = MetodosVehiculo[i % MetodosVehiculo.Length],
                    completado = false
                };
            }

            vehiculosRequeridos.Add(requerido);
        }

        DebugLog(
            "PRACTICA NIVEL 2: generado un orden aleatorio de " +
            vehiculosRequeridos.Count + " vehículos."
        );
    }

    private static string ElegirAleatorio(string[] opciones)
    {
        return opciones[Random.Range(0, opciones.Length)];
    }

    private static void Mezclar<T>(List<T> elementos)
    {
        for (int i = elementos.Count - 1; i > 0; i--)
        {
            int otro = Random.Range(0, i + 1);
            (elementos[i], elementos[otro]) = (elementos[otro], elementos[i]);
        }
    }

    private void BuscarReferencias()
    {
        if (guiaPracticaController == null)
        {
            guiaPracticaController = GetComponent<AlgoLabTemaPOOController>();
        }

        if (tutorialMultimedia == null)
        {
            tutorialMultimedia = GetComponent<AlgoLabPracticeTutorialSequence>();
        }

        if (modeManager == null)
        {
            modeManager = FindFirstObjectByType<AlgoLabClassDiagramModeManager>();
        }

        if (progressPanel == null)
        {
            AlgoLabProgressPanel panelEncontrado = FindFirstObjectByType<AlgoLabProgressPanel>();

            if (panelEncontrado != null)
            {
                progressPanel = panelEncontrado;
            }
        }

        if (audioSourceFinal == null)
        {
            audioSourceFinal = GetComponent<AudioSource>();
        }

        if (audioSourceFinal == null)
        {
            audioSourceFinal = gameObject.AddComponent<AudioSource>();
            audioSourceFinal.playOnAwake = false;
        }

        if (textoTituloCollapse == null)
        {
            textoTituloCollapse = BuscarTextoPorNombre("LevelNameText");
        }

        if (textoEstadoPractica == null)
        {
            textoEstadoPractica = BuscarTextoPorNombre("CurrentModeText");
        }

        if (textoCuerpoCollapse == null)
        {
            textoCuerpoCollapse = BuscarTextoPorNombre("DescriptionOrTaskText");
        }

        if (textoCronometro == null)
        {
            textoCronometro = BuscarTextoPorNombre("TimerText");
        }

        if (btnIniciarPractica == null)
        {
            btnIniciarPractica = BuscarBotonPorNombre(
                "BtnPracticeCollapsed",
                "BtnIniciarPractica",
                "BtnIniciar",
                "IniciarPractica",
                "Iniciar práctica",
                "Iniciar"
            );
        }

        if (btnIniciarPractica != null && textoBtnIniciarPractica == null)
        {
            textoBtnIniciarPractica = btnIniciarPractica.GetComponentInChildren<TMP_Text>(true);
        }
    }

    private TMP_Text BuscarTextoPorNombre(string nombre)
    {
        TMP_Text[] textos = FindObjectsByType<TMP_Text>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < textos.Length; i++)
        {
            if (textos[i] != null && textos[i].name == nombre)
            {
                return textos[i];
            }
        }

        return null;
    }

    private Button BuscarBotonPorNombre(params string[] nombres)
    {
        Button[] botones = FindObjectsByType<Button>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < botones.Length; i++)
        {
            if (botones[i] == null)
            {
                continue;
            }

            for (int j = 0; j < nombres.Length; j++)
            {
                if (botones[i].name == nombres[j])
                {
                    return botones[i];
                }
            }
        }

        return null;
    }

    private void GuardarColoresOriginales()
    {
        if (coloresOriginalesGuardados)
        {
            return;
        }

        if (textoTituloCollapse != null)
        {
            colorOriginalTitulo = textoTituloCollapse.color;
        }

        if (textoEstadoPractica != null)
        {
            colorOriginalEstado = textoEstadoPractica.color;
        }

        coloresOriginalesGuardados = true;
    }

    private void ConfigurarTextosVisuales()
    {
        RestaurarColoresTitulos();
        AplicarColorSoloDescripcion();

        if (textoCuerpoCollapse != null)
        {
            textoCuerpoCollapse.richText = true;
            textoCuerpoCollapse.textWrappingMode = TextWrappingModes.Normal;

            if (ajustarTamanioDescripcion)
            {
                textoCuerpoCollapse.fontSize = tamanioDescripcion;
                textoCuerpoCollapse.lineSpacing = espaciadoLineasDescripcion;
            }
        }

        if (textoCronometro != null)
        {
            textoCronometro.color = Color.white;
        }
    }

    private void RestaurarColoresTitulos()
    {
        if (!coloresOriginalesGuardados)
        {
            return;
        }

        if (textoTituloCollapse != null)
        {
            textoTituloCollapse.color = colorOriginalTitulo;
        }

        if (textoEstadoPractica != null)
        {
            textoEstadoPractica.color = colorOriginalEstado;
        }
    }

    private void AplicarColorSoloDescripcion()
    {
        if (textoCuerpoCollapse != null)
        {
            textoCuerpoCollapse.color = colorDescripcionPractica;
        }
    }

    private void LimpiarTextoEstado()
    {
        if (limpiarTextoEstadoParaNoTaparInstruccion && textoEstadoPractica != null)
        {
            textoEstadoPractica.text = "";
        }
    }

    private void ConectarEventoCrearObjeto()
    {
        BuscarReferencias();

        if (modeManager == null)
        {
            return;
        }

        modeManager.OnCrearObjetoSolicitado.RemoveListener(ValidarYCrearVehiculo);
        modeManager.OnCrearObjetoSolicitado.AddListener(ValidarYCrearVehiculo);
    }

    private void DesconectarEventoCrearObjeto()
    {
        if (modeManager == null)
        {
            return;
        }

        modeManager.OnCrearObjetoSolicitado.RemoveListener(ValidarYCrearVehiculo);
    }

    private void ConectarEventoFinGuia()
    {
        if (guiaPracticaController == null)
        {
            return;
        }

        guiaPracticaController.OnTemaTerminado.RemoveListener(MostrarBotonIniciarPracticaDespuesDeGuia);
        guiaPracticaController.OnTemaTerminado.AddListener(MostrarBotonIniciarPracticaDespuesDeGuia);
    }

    private void DesconectarEventoFinGuia()
    {
        if (guiaPracticaController == null)
        {
            return;
        }

        guiaPracticaController.OnTemaTerminado.RemoveListener(MostrarBotonIniciarPracticaDespuesDeGuia);
    }

    [ContextMenu("Iniciar guía práctica nivel 2")]
    public void IniciarGuiaPracticaNivel2()
    {
        BuscarReferencias();

        if (!PuedePrepararGuiaDesdeProgress())
        {
            DebugLog("PRACTICA NIVEL 2: guía ignorada porque el nivel 2 no es el flujo activo.");
            CancelarTodoNivel2PorCambioDeFlujo(true);
            return;
        }

        ConfigurarTextosVisuales();

        if (rutinaReintentoAlPerder != null)
        {
            StopCoroutine(rutinaReintentoAlPerder);
            rutinaReintentoAlPerder = null;
        }

        if (cargarOrdenVehiculosNivel2AlIniciar)
        {
            CargarVehiculosRequeridosNivel2();
        }

        ReiniciarDatosPractica();
        ReiniciarCronometroVisual();
        ReiniciarPuntajePractica();

        practicaActiva = false;
        guiaIniciada = true;
        guiaTerminada = false;
        guiaCanceladaPorCambioFlujo = false;
        resultadoFinalEmitido = false;

        MostrarMensajeGuiaIniciada();
        OcultarBotonIniciarPracticaDirecto();


        if (tutorialMultimedia != null && tutorialMultimedia.PuedeReproducir)
        {
            DebugLog("PRACTICA NIVEL 2: iniciando tutorial multimedia de practica.");
            tutorialMultimedia.Reproducir(MostrarBotonIniciarPracticaDespuesDeGuia);
            return;
        }

        if (guiaPracticaController == null)
        {
            Debug.LogError("PRACTICA NIVEL 2: falta asignar el AlgoLabTemaPOOController de la guía.");
            return;
        }

        // IMPORTANTE:
        // El FlowStateManager cancela guías anteriores antes de iniciar una nueva.
        // Eso remueve el listener y marca guiaCanceladaPorCambioFlujo = true.
        // Cuando esta guía sí va a empezar de verdad, debemos limpiar esa marca
        // y volver a conectar el evento de fin para que aparezca el botón Iniciar.
        ConectarEventoFinGuia();

        DebugLog("PRACTICA NIVEL 2: iniciando guía de práctica.");

        guiaPracticaController.ReproducirTema();
    }

    public void CancelarGuiaPracticaNivel2PorCambioDeFlujo(bool ocultarBoton = true)
    {
        guiaCanceladaPorCambioFlujo = true;

        if (guiaPracticaController != null)
        {
            guiaPracticaController.DetenerTema();
            guiaPracticaController.OnTemaTerminado.RemoveListener(MostrarBotonIniciarPracticaDespuesDeGuia);
        }


        if (tutorialMultimedia != null)
        {
            tutorialMultimedia.Detener(false);
        }

        if (ocultarBoton)
        {
            OcultarBotonIniciarPracticaDirecto();
        }
    }

    public void CancelarTodoNivel2PorCambioDeFlujo(bool ocultarGarage = true)
    {
        guiaCanceladaPorCambioFlujo = true;
        guiaIniciada = false;
        guiaTerminada = false;
        practicaActiva = false;
        cronometroActivo = false;
        resultadoFinalEmitido = false;

        if (rutinaMensajeError != null)
        {
            StopCoroutine(rutinaMensajeError);
            rutinaMensajeError = null;
        }

        if (rutinaReintentoAlPerder != null)
        {
            StopCoroutine(rutinaReintentoAlPerder);
            rutinaReintentoAlPerder = null;
        }

        if (guiaPracticaController != null)
        {
            guiaPracticaController.DetenerTema();
            guiaPracticaController.OnTemaTerminado.RemoveListener(MostrarBotonIniciarPracticaDespuesDeGuia);
        }


        if (tutorialMultimedia != null)
        {
            tutorialMultimedia.Detener(false);
        }

        if (audioSourceFinal != null)
        {
            audioSourceFinal.Stop();
        }

        OcultarBotonIniciarPracticaDirecto();

        if (ocultarGarage && garageController != null)
        {
            garageController.SendMessage("LimpiarVehiculosCreados", SendMessageOptions.DontRequireReceiver);
            garageController.SendMessage("OcultarGarageInstantaneo", SendMessageOptions.DontRequireReceiver);
            garageController.SendMessage("OcultarGarage", SendMessageOptions.DontRequireReceiver);
        }

        DebugLog("PRACTICA NIVEL 2: flujo cancelado completamente por cambio de nivel.");
    }

    [ContextMenu("Preparar práctica nivel 2")]
    public void PrepararPracticaNivel2()
    {
        IniciarGuiaPracticaNivel2();
    }

    [ContextMenu("Mostrar botón iniciar práctica después de guía")]
    public void MostrarBotonIniciarPracticaDespuesDeGuia()
    {
        if (!PuedeModificarPanelProgress())
        {
            return;
        }

        if (guiaCanceladaPorCambioFlujo)
        {
            return;
        }

        if (!guiaIniciada)
        {
            return;
        }

        if (guiaTerminada)
        {
            return;
        }

        guiaTerminada = true;
        practicaActiva = false;

        MostrarMensajePracticaLista();
        MostrarBotonIniciarPracticaDirecto();

        if (mostrarBotonIniciarAlTerminarGuia && progressPanel != null)
        {
            progressPanel.SendMessage(
                "MostrarBotonIniciarPracticaDespuesDeAudio",
                SendMessageOptions.DontRequireReceiver
            );
        }

        DebugLog("PRACTICA NIVEL 2: guía terminada. Ahora se puede presionar Iniciar.");
    }

    [ContextMenu("Iniciar práctica nivel 2")]
    public void IniciarPracticaNivel2()
    {
        BuscarReferencias();

        if (!PuedeIniciarPracticaDesdeProgress())
        {
            DebugLog("PRACTICA NIVEL 2: iniciar práctica ignorado porque el nivel 2 no es el flujo activo.");
            CancelarTodoNivel2PorCambioDeFlujo(true);
            return;
        }

        ConfigurarTextosVisuales();
        OcultarBotonIniciarPracticaDirecto();
        CancelarGuiaPracticaNivel2PorCambioDeFlujo(false);

        if (rutinaReintentoAlPerder != null)
        {
            StopCoroutine(rutinaReintentoAlPerder);
            rutinaReintentoAlPerder = null;
        }

        DebugLog("PRACTICA NIVEL 2: botón Iniciar presionado. Comienza la práctica real.");

        if (modeManager == null)
        {
            Debug.LogError("PRACTICA NIVEL 2: no se encontró AlgoLabClassDiagramModeManager.");
            return;
        }

        if (cargarOrdenVehiculosNivel2AlIniciar)
        {
            CargarVehiculosRequeridosNivel2();
        }

        LimpiarGarageParaReintento();
        ReiniciarDatosPractica();
        ReiniciarPuntajePractica();

        practicaActiva = true;
        resultadoFinalEmitido = false;

        intentosPractica++;

        IniciarCronometro();

        if (prepararGarageAlIniciar)
        {
            PrepararGarageAlFrente();
        }

        ConfigurarModoObjetoVehiculo();
        ConectarEventoCrearObjeto();

        if (mostrarBotonCambiarModo)
        {
            modeManager.MostrarBotonCambiarModo();
        }

        if (cambiarAModoObjetoAlIniciar)
        {
            modeManager.SetModoObjeto();
        }

        ActualizarPanelProgreso();

        if (progressPanel != null)
        {
            SendMessageProgressSeguro("MarcarPracticaEnCursoDesdeControladorNivel");
        }

        StartCoroutine(ActualizarPanelDespuesDeFrame());

        DebugLog("PRACTICA NIVEL 2: práctica real iniciada correctamente.");
    }

    [ContextMenu("Iniciar práctica nivel actual")]
    public void IniciarPracticaNivel()
    {
        IniciarPracticaNivel2();
    }

    private IEnumerator ActualizarPanelDespuesDeFrame()
    {
        yield return null;
        ActualizarPanelProgreso();

        yield return new WaitForSeconds(0.15f);
        ActualizarPanelProgreso();
    }

    private void PrepararGarageAlFrente()
    {
        if (!PuedeIniciarPracticaDesdeProgress())
        {
            DebugLog("PRACTICA NIVEL 2: no se prepara garage porque nivel 2 no es el flujo activo.");
            return;
        }

        if (garageController == null)
        {
            Debug.LogWarning("PRACTICA NIVEL 2: no hay Garage Controller asignado.");
            return;
        }

        garageController.SendMessage(
            "PrepararGarageParaPractica",
            SendMessageOptions.DontRequireReceiver
        );
    }

    [ContextMenu("Mostrar modo objeto vehículo")]
    public void MostrarModoObjetoVehiculo()
    {
        BuscarReferencias();

        if (modeManager == null)
        {
            Debug.LogError("PRACTICA NIVEL 2: no se encontró AlgoLabClassDiagramModeManager.");
            return;
        }

        ConfigurarModoObjetoVehiculo();
        modeManager.MostrarBotonCambiarModo();
        modeManager.SetModoObjeto();

        ActualizarPanelProgreso();

        DebugLog("PRACTICA NIVEL 2: modo objeto configurado para Vehículo.");
    }

    private void ConfigurarModoObjetoVehiculo()
    {
        if (modeManager == null)
        {
            return;
        }

        modeManager.nombreClaseObjeto = "Vehículo";

        modeManager.atributos.Clear();

        modeManager.atributos.Add(new AlgoLabClassDiagramModeManager.AtributoConfig
        {
            nombreAtributo = "color",
            opciones = new List<string> { "rojo", "azul", "negro", "blanco", "amarillo" }
        });

        modeManager.atributos.Add(new AlgoLabClassDiagramModeManager.AtributoConfig
        {
            nombreAtributo = "modelo",
            opciones = new List<string> { "2020", "2021", "2022", "2023", "2024" }
        });

        modeManager.atributos.Add(new AlgoLabClassDiagramModeManager.AtributoConfig
        {
            nombreAtributo = "carcasa",
            opciones = new List<string> { "Hatchback", "Pickup", "Towtruck", "Police" }
        });

        modeManager.atributos.Add(new AlgoLabClassDiagramModeManager.AtributoConfig
        {
            nombreAtributo = "estado",
            opciones = new List<string> { "nuevo", "seminuevo", "usado" }
        });

        modeManager.metodos.Clear();

        modeManager.metodos.Add(new AlgoLabClassDiagramModeManager.MetodoConfig
        {
            nombreMetodo = "encender()"
        });

        modeManager.metodos.Add(new AlgoLabClassDiagramModeManager.MetodoConfig
        {
            nombreMetodo = "acelerar()"
        });

        modeManager.metodos.Add(new AlgoLabClassDiagramModeManager.MetodoConfig
        {
            nombreMetodo = "frenar()"
        });

        modeManager.metodos.Add(new AlgoLabClassDiagramModeManager.MetodoConfig
        {
            nombreMetodo = "apagar()"
        });

        modeManager.ConstruirModoObjeto();
    }

    public void ValidarYCrearVehiculo(
        AlgoLabClassDiagramModeManager.DatosObjetoModo datos
    )
    {
        DebugLog("PRACTICA NIVEL 2: llegó solicitud de Crear Objeto.");

        if (!practicaActiva)
        {
            DebugLog("PRACTICA NIVEL 2: la práctica no está activa.");
            return;
        }

        if (resultadoFinalEmitido)
        {
            return;
        }

        if (datos == null)
        {
            Debug.LogWarning("PRACTICA NIVEL 2: los datos del modo objeto llegaron vacíos.");
            return;
        }

        if (indiceVehiculoActual >= vehiculosRequeridos.Count)
        {
            CompletarPracticaNivel2();
            return;
        }

        string firmaActual = ConstruirFirmaVehiculo(datos);

        if (EsDobleEnvio(firmaActual))
        {
            DebugLog("PRACTICA NIVEL 2: envío duplicado ignorado para evitar penalización falsa.");
            return;
        }

        ultimaFirmaVehiculoEnviado = firmaActual;
        tiempoUltimoVehiculoEnviado = Time.time;

        VehiculoRequerido requerido = vehiculosRequeridos[indiceVehiculoActual];

        bool correcto = ValidarVehiculo(datos, requerido);

        if (!correcto)
        {
            MostrarErrorValidacion(datos, requerido);
            return;
        }

        requerido.completado = true;
        vehiculosCorrectos++;

        ignorarDestruccionesHasta = Time.time + tiempoIgnorarDestruccionTrasCrearCorrecto;

        EnviarVehiculoAlGaraje(datos);

        indiceVehiculoActual++;

        if (indiceVehiculoActual >= vehiculosRequeridos.Count)
        {
            CompletarPracticaNivel2();
        }
        else
        {
            ActualizarPanelProgreso();
        }
    }

    private bool EsDobleEnvio(string firmaActual)
    {
        if (string.IsNullOrWhiteSpace(firmaActual))
        {
            return false;
        }

        bool mismaFirma = firmaActual == ultimaFirmaVehiculoEnviado;
        bool muyRapido = Time.time - tiempoUltimoVehiculoEnviado <= tiempoAntiDobleEnvioCrear;

        return mismaFirma && muyRapido;
    }

    private string ConstruirFirmaVehiculo(
        AlgoLabClassDiagramModeManager.DatosObjetoModo datos
    )
    {
        if (datos == null)
        {
            return "";
        }

        string color = Normalizar(datos.ObtenerValorAtributo("color"));
        string modelo = Normalizar(datos.ObtenerValorAtributo("modelo"));
        string carcasa = Normalizar(datos.ObtenerValorAtributo("carcasa"));
        string estado = Normalizar(datos.ObtenerValorAtributo("estado"));
        string metodo = NormalizarMetodo(datos.metodoSeleccionado);

        return color + "|" + modelo + "|" + carcasa + "|" + estado + "|" + metodo;
    }

    private bool ValidarVehiculo(
        AlgoLabClassDiagramModeManager.DatosObjetoModo datos,
        VehiculoRequerido requerido
    )
    {
        string colorActual = Normalizar(datos.ObtenerValorAtributo("color"));
        string modeloActual = Normalizar(datos.ObtenerValorAtributo("modelo"));
        string carcasaActual = Normalizar(datos.ObtenerValorAtributo("carcasa"));
        string estadoActual = Normalizar(datos.ObtenerValorAtributo("estado"));
        string metodoActual = NormalizarMetodo(datos.metodoSeleccionado);

        string colorRequerido = Normalizar(requerido.color);
        string modeloRequerido = Normalizar(requerido.modelo);
        string carcasaRequerida = Normalizar(requerido.carcasa);
        string estadoRequerido = Normalizar(requerido.estado);
        string metodoRequerido = NormalizarMetodo(requerido.metodo);

        bool correcto =
            colorActual == colorRequerido &&
            modeloActual == modeloRequerido &&
            carcasaActual == carcasaRequerida &&
            estadoActual == estadoRequerido &&
            metodoActual == metodoRequerido;

        if (!correcto)
        {
            Debug.Log(
                "PRACTICA NIVEL 2: validación fallida.\n" +
                "Color actual/requerido: " + colorActual + " / " + colorRequerido + "\n" +
                "Modelo actual/requerido: " + modeloActual + " / " + modeloRequerido + "\n" +
                "Carcasa actual/requerida: " + carcasaActual + " / " + carcasaRequerida + "\n" +
                "Estado actual/requerido: " + estadoActual + " / " + estadoRequerido + "\n" +
                "Método actual/requerido: " + metodoActual + " / " + metodoRequerido
            );
        }

        return correcto;
    }

    private void EnviarVehiculoAlGaraje(
        AlgoLabClassDiagramModeManager.DatosObjetoModo datos
    )
    {
        if (garageController == null)
        {
            Debug.LogError("PRACTICA NIVEL 2: falta asignar Garage Controller.");
            return;
        }

        garageController.SendMessage(
            metodoCrearVehiculoEnGarage,
            datos,
            SendMessageOptions.DontRequireReceiver
        );

        DebugLog("PRACTICA NIVEL 2: vehículo correcto enviado al garaje:\n" + datos.ObtenerResumen());
    }

    private void MostrarErrorValidacion(
        AlgoLabClassDiagramModeManager.DatosObjetoModo datos,
        VehiculoRequerido requerido
    )
    {
        RegistrarVehiculoIncorrecto();

        if (rutinaMensajeError != null)
        {
            StopCoroutine(rutinaMensajeError);
        }

        rutinaMensajeError = StartCoroutine(MostrarErrorTemporalRutina());

        DebugLog(
            "PRACTICA NIVEL 2: vehículo incorrecto.\n" +
            "Requerido:\n" +
            ConstruirTextoVehiculo(requerido) +
            "\nSelección actual:\n" +
            "color: " + datos.ObtenerValorAtributo("color") + "\n" +
            "modelo: " + datos.ObtenerValorAtributo("modelo") + "\n" +
            "carcasa: " + datos.ObtenerValorAtributo("carcasa") + "\n" +
            "estado: " + datos.ObtenerValorAtributo("estado") + "\n" +
            "método: " + datos.metodoSeleccionado
        );
    }

    private void RegistrarVehiculoIncorrecto()
    {
        if (!practicaActiva || resultadoFinalEmitido)
        {
            return;
        }

        erroresVehiculoIncorrecto++;
        penalizacionPuntaje += puntosMenosPorVehiculoIncorrecto;

        DebugLog(
            "PUNTAJE NIVEL 2: vehículo incorrecto. -" +
            puntosMenosPorVehiculoIncorrecto +
            " | Errores: " + erroresVehiculoIncorrecto +
            " | Penalización total: " + penalizacionPuntaje
        );

        ActualizarPanelProgreso();
    }

    public void RegistrarVehiculoDestruidoPorPractica()
    {
        if (!practicaActiva || resultadoFinalEmitido)
        {
            return;
        }

        if (Time.time <= ignorarDestruccionesHasta)
        {
            DebugLog("PUNTAJE NIVEL 2: destrucción ignorada porque corresponde a vehículo correcto aceptado.");
            return;
        }

        vehiculosDestruidos++;
        penalizacionPuntaje += puntosMenosPorVehiculoDestruido;

        DebugLog(
            "PUNTAJE NIVEL 2: vehículo destruido. -" +
            puntosMenosPorVehiculoDestruido +
            " | Destruidos: " + vehiculosDestruidos +
            " | Penalización total: " + penalizacionPuntaje
        );

        ActualizarPanelProgreso();
    }

    private IEnumerator MostrarErrorTemporalRutina()
    {
        ConfigurarTextosVisuales();
        LimpiarTextoEstado();

        if (textoCuerpoCollapse != null)
        {
            textoCuerpoCollapse.text =
                "Vehículo incorrecto.\n" +
                "Corrige atributos y método.\n" +
                "Penalización: -" + puntosMenosPorVehiculoIncorrecto + " puntos";
        }

        yield return new WaitForSeconds(duracionMensajeError);

        ActualizarPanelProgreso();

        rutinaMensajeError = null;
    }

    private void ActualizarPanelProgreso()
    {
        if (!PuedeModificarPanelProgress())
        {
            return;
        }

        ConfigurarTextosVisuales();

        if (textoTituloCollapse != null)
        {
            textoTituloCollapse.text = "Práctica Nivel " + numeroNivelReal;
        }

        LimpiarTextoEstado();

        if (textoCuerpoCollapse != null)
        {
            textoCuerpoCollapse.text = ConstruirTextoSimplePractica();
        }
        else
        {
            Debug.LogWarning("PRACTICA NIVEL 2: textoCuerpoCollapse está vacío. Asigna DescriptionOrTaskText.");
        }
    }

    private string ConstruirTextoSimplePractica()
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine("Faltan: " + ObtenerVehiculosFaltantes());

        if (indiceVehiculoActual < vehiculosRequeridos.Count)
        {
            VehiculoRequerido actual = vehiculosRequeridos[indiceVehiculoActual];
            sb.Append(ConstruirTextoVehiculo(actual));
        }

        sb.AppendLine("Puntaje: " + CalcularPuntajeFinalNivel2());

        if (penalizacionPuntaje > 0)
        {
            sb.AppendLine("Penalización: -" + penalizacionPuntaje);
        }

        return sb.ToString();
    }

    private string ConstruirTextoVehiculo(VehiculoRequerido vehiculo)
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine("color: " + TextoColorVehiculo(vehiculo.color));
        sb.AppendLine("modelo: " + vehiculo.modelo);
        sb.AppendLine("carcasa: " + vehiculo.carcasa);
        sb.AppendLine("estado: " + vehiculo.estado);
        sb.AppendLine("método: " + vehiculo.metodo);

        return sb.ToString();
    }

    private string TextoColorVehiculo(string color)
    {
        string hex = ObtenerHexColorTexto(color);
        return "<color=#" + hex + ">" + color + "</color>";
    }

    private string ObtenerHexColorTexto(string color)
    {
        string c = Normalizar(color);

        if (c == "rojo")
        {
            return "FF4040";
        }

        if (c == "azul")
        {
            return "40A0FF";
        }

        if (c == "negro")
        {
            return "BDBDBD";
        }

        if (c == "blanco")
        {
            return "FFFFFF";
        }

        if (c == "amarillo")
        {
            return "FFD84A";
        }

        if (c == "verde")
        {
            return "40FF40";
        }

        return "FFFFFF";
    }

    private int ObtenerVehiculosFaltantes()
    {
        return Mathf.Max(vehiculosRequeridos.Count - vehiculosCorrectos, 0);
    }

    private void CompletarPracticaNivel2()
    {
        if (resultadoFinalEmitido)
        {
            return;
        }

        resultadoFinalEmitido = true;
        practicaActiva = false;
        cronometroActivo = false;

        int puntajeFinal = CalcularPuntajeFinalNivel2();

        ConfigurarTextosVisuales();
        OcultarBotonIniciarPracticaDirecto();

        if (textoTituloCollapse != null)
        {
            textoTituloCollapse.text = "¡Felicidades!";
        }

        LimpiarTextoEstado();

        if (textoCuerpoCollapse != null)
        {
            textoCuerpoCollapse.text =
                mensajePracticaCompletada +
                "\nPuntaje obtenido: " + puntajeFinal +
                "\nTiempo restante: " + Mathf.CeilToInt(Mathf.Max(0f, tiempoRestante)) +
                "\nPenalización: -" + penalizacionPuntaje;
        }

        ActualizarTextoCronometro(tiempoRestante);

        GuardarProgresoNivel2(true);

        ReproducirAudioFinal(audioFelicitacion);

        DebugLog("PRACTICA NIVEL 2: práctica completada. Avisando al ProgressPanel.");

        if (progressPanel != null && PuedeModificarPanelProgress())
        {
            progressPanel.SendMessage(
                "TerminarPracticaActual",
                SendMessageOptions.DontRequireReceiver
            );
        }
    }

    private void MostrarMensajeGuiaIniciada()
    {
        if (!PuedeModificarPanelProgress())
        {
            return;
        }

        ConfigurarTextosVisuales();

        if (textoTituloCollapse != null)
        {
            textoTituloCollapse.text = "Guía práctica";
        }

        LimpiarTextoEstado();

        if (textoCuerpoCollapse != null)
        {
            textoCuerpoCollapse.text = mensajeGuiaIniciada;
        }
    }

    private void MostrarMensajePracticaLista()
    {
        if (!PuedeModificarPanelProgress())
        {
            return;
        }

        ConfigurarTextosVisuales();

        if (textoTituloCollapse != null)
        {
            textoTituloCollapse.text = "Práctica Nivel " + numeroNivelReal;
        }

        LimpiarTextoEstado();

        if (textoCuerpoCollapse != null)
        {
            textoCuerpoCollapse.text = mensajePracticaLista;
        }

        ReiniciarCronometroVisual();
    }

    private void ReiniciarDatosPractica()
    {
        indiceVehiculoActual = 0;
        vehiculosCorrectos = 0;
        resultadoFinalEmitido = false;

        ultimaFirmaVehiculoEnviado = "";
        tiempoUltimoVehiculoEnviado = -99f;
        ignorarDestruccionesHasta = -99f;

        if (vehiculosRequeridos == null)
        {
            vehiculosRequeridos = new List<VehiculoRequerido>();
        }

        for (int i = 0; i < vehiculosRequeridos.Count; i++)
        {
            vehiculosRequeridos[i].completado = false;
        }

        if (rutinaMensajeError != null)
        {
            StopCoroutine(rutinaMensajeError);
            rutinaMensajeError = null;
        }
    }

    private void ReiniciarPuntajePractica()
    {
        penalizacionPuntaje = 0;
        erroresVehiculoIncorrecto = 0;
        vehiculosDestruidos = 0;
    }

    private void IniciarCronometro()
    {
        tiempoRestante = duracionPracticaSegundos;
        cronometroActivo = true;
        ActualizarTextoCronometro(tiempoRestante);
    }

    private void ReiniciarCronometroVisual()
    {
        tiempoRestante = duracionPracticaSegundos;
        cronometroActivo = false;
        ActualizarTextoCronometro(tiempoRestante);
    }

    private void ActualizarCronometro()
    {
        if (!cronometroActivo)
        {
            return;
        }

        tiempoRestante -= Time.deltaTime;

        if (tiempoRestante <= 0f)
        {
            tiempoRestante = 0f;
            ActualizarTextoCronometro(tiempoRestante);
            TiempoAgotado();
            return;
        }

        ActualizarTextoCronometro(tiempoRestante);
    }

    private void ActualizarTextoCronometro(float segundos)
    {
        if (textoCronometro == null)
        {
            return;
        }

        int totalSegundos = Mathf.CeilToInt(segundos);
        int minutos = totalSegundos / 60;
        int segundosRestantes = totalSegundos % 60;

        textoCronometro.text = minutos.ToString("00") + ":" + segundosRestantes.ToString("00");
    }

    private void TiempoAgotado()
    {
        if (resultadoFinalEmitido)
        {
            return;
        }

        resultadoFinalEmitido = true;
        cronometroActivo = false;

        if (detenerPracticaAlAcabarTiempo)
        {
            practicaActiva = false;
        }

        ConfigurarTextosVisuales();
        OcultarBotonIniciarPracticaDirecto();
        LimpiarTextoEstado();

        if (textoTituloCollapse != null)
        {
            textoTituloCollapse.text = "Tiempo agotado";
        }

        if (textoCuerpoCollapse != null)
        {
            textoCuerpoCollapse.text =
                mensajeTiempoAgotado +
                "\nPuntaje obtenido: 0" +
                "\nVehículos correctos: " + vehiculosCorrectos +
                "\nErrores: " + erroresVehiculoIncorrecto +
                "\nVehículos destruidos: " + vehiculosDestruidos;
        }

        GuardarProgresoNivel2(false);

        ReproducirAudioFinal(audioTiempoAgotado);

        if (permitirReintentoAlPerder)
        {
            if (rutinaReintentoAlPerder != null)
            {
                StopCoroutine(rutinaReintentoAlPerder);
            }

            rutinaReintentoAlPerder = StartCoroutine(PrepararReintentoAlPerderRutina());
        }

        DebugLog("PRACTICA NIVEL 2: tiempo agotado. Preparando reintento.");
    }

    private IEnumerator PrepararReintentoAlPerderRutina()
    {
        float tiempoEspera = tiempoExtraDespuesAudioPerder;

        if (audioTiempoAgotado != null)
        {
            tiempoEspera += audioTiempoAgotado.length;
        }
        else
        {
            tiempoEspera += 2f;
        }

        yield return new WaitForSeconds(tiempoEspera);

        rutinaReintentoAlPerder = null;

        if (reiniciarAutomaticamenteAlPerder)
        {
            LimpiarGarageParaReintento();

            if (repetirGuiaAlReintentar)
            {
                IniciarGuiaPracticaNivel2();
            }
            else
            {
                IniciarPracticaNivel2();
            }

            yield break;
        }

        PrepararPracticaParaReintento();
    }

    private void PrepararPracticaParaReintento()
    {
        BuscarReferencias();

        if (cargarOrdenVehiculosNivel2AlIniciar)
        {
            CargarVehiculosRequeridosNivel2();
        }

        LimpiarGarageParaReintento();
        ReiniciarDatosPractica();
        ReiniciarPuntajePractica();
        ReiniciarCronometroVisual();

        practicaActiva = false;
        cronometroActivo = false;
        guiaIniciada = true;
        guiaTerminada = true;
        resultadoFinalEmitido = false;

        ConfigurarTextosVisuales();

        if (textoTituloCollapse != null)
        {
            textoTituloCollapse.text = "Intentar nuevamente";
        }

        LimpiarTextoEstado();

        if (textoCuerpoCollapse != null)
        {
            textoCuerpoCollapse.text = mensajeReintentoDisponible;
        }

        MostrarBotonIniciarPracticaDirecto();

        if (progressPanel != null)
        {
            progressPanel.SendMessage(
                "MostrarBotonIniciarPracticaDespuesDeAudio",
                SendMessageOptions.DontRequireReceiver
            );
        }

        DebugLog("PRACTICA NIVEL 2: reintento preparado. Esperando que el estudiante presione Iniciar.");
    }

    private void LimpiarGarageParaReintento()
    {
        if (!limpiarGarageAlReintentar)
        {
            return;
        }

        if (garageController == null)
        {
            return;
        }

        garageController.SendMessage(
            metodoLimpiarGarageAlReintentar,
            SendMessageOptions.DontRequireReceiver
        );
    }

    private void MostrarBotonIniciarPracticaDirecto()
    {
        BuscarReferencias();

        if (btnIniciarPractica == null)
        {
            Debug.LogWarning("PRACTICA NIVEL 2: no se encontró el botón Iniciar. Asígnalo manualmente en Btn Iniciar Practica.");
            return;
        }

        ActivarPadresDelBoton(btnIniciarPractica.transform);

        btnIniciarPractica.gameObject.SetActive(true);
        btnIniciarPractica.interactable = true;

        btnIniciarPractica.onClick.RemoveListener(IniciarPracticaNivel2);
        btnIniciarPractica.onClick.AddListener(IniciarPracticaNivel2);

        if (textoBtnIniciarPractica != null)
        {
            textoBtnIniciarPractica.text = textoBotonIniciar;
        }

        CanvasGroup canvasGroup = btnIniciarPractica.GetComponentInParent<CanvasGroup>();

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        DebugLog("PRACTICA NIVEL 2: botón Iniciar mostrado directamente.");
    }

    private void OcultarBotonIniciarPracticaDirecto()
    {
        BuscarReferencias();

        if (btnIniciarPractica == null)
        {
            return;
        }

        btnIniciarPractica.interactable = false;
        btnIniciarPractica.gameObject.SetActive(false);

        DebugLog("PRACTICA NIVEL 2: botón Iniciar ocultado.");
    }

    private void ActivarPadresDelBoton(Transform botonTransform)
    {
        if (botonTransform == null)
        {
            return;
        }

        Transform actual = botonTransform;

        while (actual != null)
        {
            actual.gameObject.SetActive(true);

            if (actual.GetComponent<Canvas>() != null)
            {
                break;
            }

            actual = actual.parent;
        }
    }

    private void ReproducirAudioFinal(AudioClip clip)
    {
        if (!reproducirAudioFinal)
        {
            return;
        }

        if (clip == null)
        {
            DebugLog("PRACTICA NIVEL 2: no hay audio final asignado.");
            return;
        }

        if (audioSourceFinal == null)
        {
            audioSourceFinal = GetComponent<AudioSource>();
        }

        if (audioSourceFinal == null)
        {
            audioSourceFinal = gameObject.AddComponent<AudioSource>();
            audioSourceFinal.playOnAwake = false;
        }

        if (detenerAudioActualAntesDeAudioFinal)
        {
            audioSourceFinal.Stop();
        }

        audioSourceFinal.clip = clip;
        audioSourceFinal.Play();

        DebugLog("PRACTICA NIVEL 2: reproduciendo audio final: " + clip.name);
    }

    private int CalcularPuntajeFinalNivel2()
    {
        int tiempo = Mathf.CeilToInt(Mathf.Max(0f, tiempoRestante));
        int puntaje = tiempo - penalizacionPuntaje;

        return Mathf.Max(0, puntaje);
    }

    private void GuardarProgresoNivel2(bool completada)
    {
        if (!guardarProgresoAlCompletar)
        {
            return;
        }

        if (!completada && !guardarIntentoFallido)
        {
            return;
        }

        int puntajeFinal = completada ? CalcularPuntajeFinalNivel2() : 0;
        int tiempoEntero = Mathf.CeilToInt(Mathf.Max(0f, tiempoRestante));

        if (AlgoLabProgressSaver.Instance != null)
        {
            AlgoLabProgressSaver.Instance.GuardarProgresoSiAplica(
                numeroNivelReal,
                completada,
                puntajeFinal,
                tiempoEntero,
                Mathf.Max(1, intentosPractica)
            );
        }
        else
        {
            Debug.LogWarning("PUNTAJE NIVEL 2: no existe AlgoLabProgressSaver en la escena.");
        }

        DebugLog(
            "PUNTAJE NIVEL 2: completada=" + completada +
            " | tiempo=" + tiempoEntero +
            " | penalización=" + penalizacionPuntaje +
            " | incorrectos=" + erroresVehiculoIncorrecto +
            " | destruidos=" + vehiculosDestruidos +
            " | puntajeFinal=" + puntajeFinal +
            " | intentos=" + intentosPractica
        );
    }

    private string NormalizarMetodo(string metodo)
    {
        string limpio = Normalizar(metodo);

        limpio = limpio.Replace("()", "");
        limpio = limpio.Replace("(", "");
        limpio = limpio.Replace(")", "");

        return limpio;
    }

    private string Normalizar(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return "";
        }

        string limpio = texto.Trim().ToLower();

        limpio = limpio.Replace("á", "a");
        limpio = limpio.Replace("é", "e");
        limpio = limpio.Replace("í", "i");
        limpio = limpio.Replace("ó", "o");
        limpio = limpio.Replace("ú", "u");
        limpio = limpio.Replace("ñ", "n");

        limpio = limpio.Replace("_", "");
        limpio = limpio.Replace("-", "");
        limpio = limpio.Replace(" ", "");

        return limpio;
    }

    private void DebugLog(string mensaje)
    {
        if (mostrarDebug)
        {
            Debug.Log(mensaje);
        }
    }
}
