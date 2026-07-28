using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Practica fisica de Encapsulamiento del nivel 3.
/// El robot expone metodos publicos en el panel de herramientas y protege sus
/// atributos internos. La solucion correcta exige apagar, reparar, recargar,
/// enfriar y volver a encender sin modificar directamente el estado privado.
/// </summary>
public class AlgoLabEncapsulationRobotPractice : MonoBehaviour
{
    public event System.Action<string> FeedbackCambiado;

    [Header("Estado inicial")]
    [Range(0, 100)] public int energiaInicial = 25;
    [Range(0, 120)] public int temperaturaInicial = 85;
    public bool averiaInicial = true;
    [Min(1)] public int puntajeInicial = 100;
    [Min(1)] public int penalizacionAccesoPrivado = 10;

    [Header("Condiciones seguras")]
    [Range(1, 100)] public int energiaMinimaEncendido = 80;
    [Range(0, 100)] public int temperaturaMaximaEncendido = 10;
    [Range(0, 40)] public int temperaturaObjetivo = 10;

    [Header("Flujo")]
    public bool completarNivelAutomaticamente = true;
    public float esperaAntesDeCompletarNivel = 2.2f;
    public bool mostrarDebug = true;

    private int energia;
    private int temperatura;
    private int puntaje;
    private int puntajeBaseTiempo;
    private int penalizacionAcumulada;
    private int erroresTotales;
    private int erroresPrivados;
    private bool encendido;
    private bool averiado;
    private float energiaContinua;
    private float temperaturaContinua;
    private bool reemplazoBateriaPrivado;
    private bool reemplazoTemperaturaPrivado;
    private bool vidrioBateriaRoto;
    private bool vidrioTemperaturaRoto;
    private float siguienteFeedbackHerramienta;
    private bool practicaIniciada;
    private bool practicaCompletada;
    private bool practicaFallida;
    private readonly Dictionary<string, float> proximosErrores =
        new Dictionary<string, float>();
    private bool visualConstruido;
    private Coroutine rutinaFinal;

    private Transform visualRoot;
    private AlgoLabRobotWorkshopVisual workshopVisual;
    private Renderer[] ojos;
    private Renderer bateriaRenderer;
    private Transform cargaBateriaVisual;
    private Renderer temperaturaRenderer;
    private Renderer moduloRenderer;
    private Renderer luzEstadoRenderer;

    private TMP_Text statusText;
    private TMP_Text scoreText;
    private TMP_Text feedbackText;
    private AlgoLabObjetoEducativo robotData;
    private AlgoLabClassDiagramController diagramController;
    private AlgoLabProgressPanel progressPanel;
    private AlgoLabLevel3RobotPracticeRuntime runtimeNivel3;

    private Material materialRobot;
    private Material materialOscuro;
    private Material materialVerde;
    private Material materialRojo;
    private Material materialAzul;
    private Material materialAmarillo;
    private Material materialBlanco;

    public int Energia => energia;
    public int Temperatura => temperatura;
    public int Puntaje => puntaje;
    public int ErroresPrivados => erroresPrivados;
    public int ErroresTotales => erroresTotales;
    public bool Encendido => encendido;
    public bool Averiado => averiado;
    public bool PracticaCompletada => practicaCompletada;
    public bool PracticaIniciada => practicaIniciada;
    public bool PracticaFallida => practicaFallida;
    public bool ReemplazoBateriaPrivado => reemplazoBateriaPrivado;
    public bool ReemplazoTemperaturaPrivado => reemplazoTemperaturaPrivado;
    public bool VidrioBateriaRoto => vidrioBateriaRoto;
    public bool VidrioTemperaturaRoto => vidrioTemperaturaRoto;

    private void Awake()
    {
        runtimeNivel3 = GetComponent<AlgoLabLevel3RobotPracticeRuntime>();
        ConstruirObjetosSiFaltan();
    }

    private void Start()
    {
        if (!practicaIniciada)
            IniciarPractica();
    }

    private void Update()
    {
        if (!visualConstruido)
            return;

        if (workshopVisual != null)
            workshopVisual.Tick(Time.unscaledDeltaTime);

        if (luzEstadoRenderer != null)
        {
            float pulso = encendido
                ? 0.72f + Mathf.Sin(Time.unscaledTime * 4.5f) * 0.18f
                : 0.18f;
            Color baseColor = encendido
                ? new Color(0.05f, 1f, 0.55f)
                : new Color(0.12f, 0.16f, 0.18f);
            AplicarColorEmision(luzEstadoRenderer, baseColor * pulso);
        }
    }

    [ContextMenu("Construir objetos de practica")]
    public void ConstruirObjetosSiFaltan()
    {
        if (visualConstruido && visualRoot != null)
            return;

        Transform existente = transform.Find("RobotPracticeVisual");
        if (existente != null)
        {
            visualRoot = existente;
            workshopVisual =
                visualRoot.GetComponent<AlgoLabRobotWorkshopVisual>();
            if (workshopVisual == null)
            {
                workshopVisual =
                    visualRoot.gameObject.AddComponent<AlgoLabRobotWorkshopVisual>();
            }

            workshopVisual.Inicializar(this);
            workshopVisual.InteraccionesHerramientasExternas = runtimeNivel3 != null;
            visualConstruido = true;
            ActualizarVisuales();
            return;
        }

        visualRoot = new GameObject("RobotPracticeVisual").transform;
        visualRoot.SetParent(transform, false);
        workshopVisual = visualRoot.gameObject.AddComponent<AlgoLabRobotWorkshopVisual>();
        workshopVisual.Inicializar(this);
        workshopVisual.InteraccionesHerramientasExternas = runtimeNivel3 != null;

        visualConstruido = true;
        ActualizarVisuales();
    }

    public void IniciarPractica()
    {
        ConstruirObjetosSiFaltan();

        if (rutinaFinal != null)
        {
            StopCoroutine(rutinaFinal);
            rutinaFinal = null;
        }

        energia = Mathf.Clamp(energiaInicial, 0, 100);
        temperatura = Mathf.Clamp(temperaturaInicial, 0, 120);
        energiaContinua = energia;
        temperaturaContinua = temperatura;
        puntaje = Mathf.Max(0, puntajeInicial);
        puntajeBaseTiempo = puntaje;
        penalizacionAcumulada = 0;
        erroresTotales = 0;
        proximosErrores.Clear();
        erroresPrivados = 0;
        encendido = true;
        averiado = averiaInicial;
        reemplazoBateriaPrivado = false;
        reemplazoTemperaturaPrivado = false;
        vidrioBateriaRoto = false;
        vidrioTemperaturaRoto = false;
        siguienteFeedbackHerramienta = 0f;
        practicaIniciada = true;
        practicaCompletada = false;
        practicaFallida = false;

        if (workshopVisual != null)
            workshopVisual.ReiniciarTaller();
        if (runtimeNivel3 == null)
            runtimeNivel3 = GetComponent<AlgoLabLevel3RobotPracticeRuntime>();
        if (runtimeNivel3 != null)
            runtimeNivel3.ReiniciarDesdeControlador();
        ResolverReferenciasEscena();
        PrepararDiagramaRobot();
        AlgoLabVRUIButtonClicker clicker =
            FindFirstObjectByType<AlgoLabVRUIButtonClicker>(FindObjectsInactive.Include);
        if (clicker != null)
            clicker.ActualizarListaInteractuables();
        MostrarFeedback("Robot encendido con fallas. Diagnostica y repara usando el panel.");
        ActualizarVisuales();
        DebugLog("practica iniciada");
    }

    public void MetodoApagar()
    {
        if (!PuedeInteractuar())
            return;

        if (!encendido)
        {
            RegistrarError(
                "apagar_ya_apagado",
                "apagar() rechazado: el robot ya esta apagado."
            );
            return;
        }

        encendido = false;
        runtimeNivel3?.MarcarMetodoCorrecto("apagar");
        runtimeNivel3?.RestaurarErrorDiagramaComoSeleccion();
        MostrarFeedback("apagar() ejecutado. El robot quedo en modo seguro.");
        ActualizarVisuales();
    }

    public void MetodoRecargar()
    {
        if (!PuedeInteractuar())
            return;

        if (!ValidarRobotApagado("recargar()"))
            return;

        energia = 100;
        energiaContinua = energia;
        ActualizarAveriaDesdeEstado();
        MostrarFeedback("cargar() ejecutado. Bateria restaurada al 100%.");
        ActualizarVisuales();
    }

    public void MetodoEnfriar()
    {
        if (!PuedeInteractuar())
            return;

        if (!ValidarRobotApagado("enfriar()"))
            return;

        temperatura = temperaturaObjetivo;
        temperaturaContinua = temperatura;
        ActualizarAveriaDesdeEstado();
        MostrarFeedback("enfriar() ejecutado. Temperatura estabilizada en " +
            temperaturaObjetivo + " C.");
        ActualizarVisuales();
    }

    public void MetodoReparar()
    {
        if (!PuedeInteractuar())
            return;

        if (!ValidarRobotApagado("reparar()"))
            return;

        ActualizarAveriaDesdeEstado();
        MostrarFeedback(
            averiado
                ? "El robot aun necesita carga o enfriamiento."
                : "Diagnostico correcto: los sistemas internos son seguros."
        );
        ActualizarVisuales();
    }

    public void MetodoEncender()
    {
        if (!PuedeInteractuar())
            return;

        if (encendido)
        {
            RegistrarError(
                "encender_ya_encendido",
                "encender() rechazado: el robot ya esta encendido."
            );
            return;
        }

        bool bateriaIncorrecta =
            reemplazoBateriaPrivado ||
            (runtimeNivel3 != null && !runtimeNivel3.BateriaOriginalInstalada);
        bool temperaturaIncorrecta =
            reemplazoTemperaturaPrivado ||
            (runtimeNivel3 != null && !runtimeNivel3.TemperaturaOriginalInstalada);
        if (bateriaIncorrecta || temperaturaIncorrecta)
        {
            string componentes = string.Empty;
            if (bateriaIncorrecta)
            {
                componentes = "-bateria";
                RegistrarError(
                    "arranque_bateria_privada",
                    "Fallo: bateria privada reemplazada o ausente.",
                    true,
                    0f
                );
                runtimeNivel3?.MarcarAtributoError("bateria");
            }
            if (temperaturaIncorrecta)
            {
                componentes += string.IsNullOrEmpty(componentes)
                    ? "-temperatura"
                    : " y -temperatura";
                RegistrarError(
                    "arranque_temperatura_privada",
                    "Fallo: temperatura privada reemplazada o ausente.",
                    true,
                    0f
                );
                runtimeNivel3?.MarcarAtributoError("temperatura");
            }

            encendido = true;
            averiado = true;
            MostrarFeedback(
                "FALLO DE ARRANQUE: " + componentes +
                " son privados. Apaga el robot y restaura los originales."
            );
            ActualizarVisuales();
            return;
        }

        ActualizarAveriaDesdeEstado();
        if (averiado)
        {
            // El usuario puede volver a encender el robot aun averiado. El
            // runtime conserva el tiempo restante mientras esta apagado y
            // reanuda la cuenta regresiva desde ese mismo punto.
            encendido = true;
            practicaCompletada = false;
            MostrarFeedback(
                "Robot encendido con fallas. El contador continua."
            );
            ActualizarVisuales();
            return;
        }

        if (energia < energiaMinimaEncendido)
        {
            RegistrarError(
                "encender_energia_insuficiente",
                "encender() rechazado: energia insuficiente."
            );
            return;
        }

        if (temperatura > temperaturaMaximaEncendido)
        {
            RegistrarError(
                "encender_temperatura_insegura",
                "encender() rechazado: temperatura insegura."
            );
            return;
        }

        encendido = true;
        practicaCompletada = true;
        runtimeNivel3?.MarcarAtributoCorrecto("estado");
        MostrarFeedback("Robot reparado y encendido correctamente.");
        ActualizarVisuales();

        if (Application.isPlaying && rutinaFinal == null)
            rutinaFinal = StartCoroutine(FinalizarPracticaRutina());
    }

    public void IntentarModificarEnergiaPrivada()
    {
        RegistrarAccesoPrivado("energia");
    }

    public void IntentarModificarTemperaturaPrivada()
    {
        RegistrarAccesoPrivado("temperatura");
    }

    public void IntentarModificarEstadoPrivado()
    {
        RegistrarAccesoPrivado("estado");
    }

    /// <summary>
    /// Carga progresiva aplicada por el conector fisico del metodo publico.
    /// </summary>
    public bool AplicarCargaFisica(float cantidad)
    {
        if (!PuedeInteractuar())
            return false;
        if (!ValidarRobotApagadoSilencioso("+cargar"))
            return false;
        if (cantidad <= 0f || energia >= 100)
            return false;

        int anterior = energia;
        energiaContinua = Mathf.Clamp(energiaContinua + cantidad, 0f, 100f);
        energia = Mathf.RoundToInt(energiaContinua);
        ActualizarAveriaDesdeEstado();
        ActualizarVisuales();

        if ((anterior < energiaMinimaEncendido && energia >= energiaMinimaEncendido) ||
            (anterior < 100 && energia >= 100))
        {
            MostrarFeedback(
                energia >= 100
                    ? "Bateria cargada al 100% mediante +cargar."
                    : "Bateria en rango seguro. Puedes retirar el cargador."
            );
        }
        return true;
    }

    /// <summary>
    /// Enfriamiento progresivo aplicado por el ventilador fisico.
    /// </summary>
    public bool AplicarEnfriamientoFisico(float cantidad)
    {
        if (!PuedeInteractuar())
            return false;
        if (!ValidarRobotApagadoSilencioso("+enfriar"))
            return false;
        if (!vidrioTemperaturaRoto)
        {
            NotificarHerramientaBloqueada("El vidrio frontal aun protege el modulo caliente.");
            return false;
        }
        if (cantidad <= 0f || temperatura <= temperaturaObjetivo)
            return false;

        int anterior = temperatura;
        temperaturaContinua = Mathf.Clamp(
            temperaturaContinua - cantidad,
            temperaturaObjetivo,
            120f
        );
        temperatura = Mathf.RoundToInt(temperaturaContinua);
        ActualizarAveriaDesdeEstado();
        ActualizarVisuales();

        if (anterior > temperaturaMaximaEncendido &&
            temperatura <= temperaturaMaximaEncendido)
        {
            MostrarFeedback("Temperatura segura mediante +enfriar. El resplandor rojo desaparecio.");
        }
        return true;
    }

    /// <summary>
    /// Bloquea todas las interacciones cuando el robot explota. La práctica
    /// vuelve a quedar disponible únicamente mediante Reintentar.
    /// </summary>
    public void NotificarExplosion()
    {
        if (!practicaIniciada || practicaCompletada || practicaFallida)
            return;

        practicaFallida = true;
        encendido = false;
        averiado = true;
        MostrarFeedback("ROBOT DESTRUIDO. Pulsa REINTENTAR.");
        ActualizarVisuales();
    }

    public void NotificarVidrioRoto(AlgoLabRobotBreakableGlass.Compartimiento compartimiento)
    {
        if (compartimiento == AlgoLabRobotBreakableGlass.Compartimiento.Temperatura)
        {
            vidrioTemperaturaRoto = true;
            MostrarFeedback("Modulo de temperatura expuesto. No lo reemplaces: usa el ventilador +enfriar.");
        }
        else
        {
            vidrioBateriaRoto = true;
            MostrarFeedback("Bateria privada expuesta. Usa el puerto externo +cargar, no la reemplaces.");
        }
    }

    public void NotificarReemplazoPrivadoTemperatura()
    {
        if (!PuedeInteractuar() || reemplazoTemperaturaPrivado)
            return;
        reemplazoTemperaturaPrivado = true;
        MostrarFeedback("Modulo colocado. Su acceso es privado; el error se validara al encender.");
        ActualizarVisuales();
    }

    public void NotificarReemplazoPrivadoBateria()
    {
        if (!PuedeInteractuar() || reemplazoBateriaPrivado)
            return;
        reemplazoBateriaPrivado = true;
        MostrarFeedback("Bateria reemplazada directamente. El arranque comprobara el encapsulamiento.");
        ActualizarVisuales();
    }

    public void NotificarRetiroReemplazoPrivadoTemperatura()
    {
        reemplazoTemperaturaPrivado = false;
        ActualizarVisuales();
    }

    public void NotificarRetiroReemplazoPrivadoBateria()
    {
        reemplazoBateriaPrivado = false;
        ActualizarVisuales();
    }

    public void ActualizarPuntajePorTiempo(float restante, float total)
    {
        if (!practicaIniciada || practicaCompletada || practicaFallida)
            return;

        float proporcion = Mathf.Clamp01(restante / Mathf.Max(1f, total));
        puntajeBaseTiempo = Mathf.RoundToInt(
            Mathf.Max(0, puntajeInicial) * proporcion
        );
        RecalcularPuntaje();
        ActualizarVisuales();
    }

    public bool RegistrarError(
        string clave,
        string mensaje,
        bool esAccesoPrivado = false,
        float cooldown = 0.8f)
    {
        if (!PuedeInteractuar())
            return false;

        string claveSegura = string.IsNullOrWhiteSpace(clave)
            ? "error_generico"
            : clave;
        if (proximosErrores.TryGetValue(claveSegura, out float proximo) &&
            Time.unscaledTime < proximo)
        {
            return false;
        }

        proximosErrores[claveSegura] =
            Time.unscaledTime + Mathf.Max(0f, cooldown);
        erroresTotales++;
        if (esAccesoPrivado)
            erroresPrivados++;
        penalizacionAcumulada += Mathf.Max(1, penalizacionAccesoPrivado);
        RecalcularPuntaje();
        MostrarFeedback(
            mensaje + " -" + Mathf.Max(1, penalizacionAccesoPrivado) +
            " puntos."
        );
        ActualizarVisuales();
        return true;
    }

    public void NotificarHerramientaBloqueada(string mensaje)
    {
        if (Time.unscaledTime < siguienteFeedbackHerramienta)
            return;
        siguienteFeedbackHerramienta = Time.unscaledTime + 1.2f;
        MostrarFeedback(mensaje);
    }

    private bool PuedeInteractuar()
    {
        return practicaIniciada && !practicaCompletada && !practicaFallida;
    }

    private bool ValidarRobotApagado(string metodo)
    {
        if (!encendido)
            return true;

        RegistrarError(
            "metodo_encendido_" + metodo,
            metodo + " rechazado: apaga el robot antes de intervenirlo."
        );
        return false;
    }

    private bool ValidarRobotApagadoSilencioso(string metodo)
    {
        if (!encendido)
            return true;
        NotificarHerramientaBloqueada(metodo + " bloqueado: primero usa +apagar.");
        return false;
    }

    private void ActualizarAveriaDesdeEstado()
    {
        averiado =
            energia < energiaMinimaEncendido ||
            temperatura > temperaturaMaximaEncendido;
    }

    private void RegistrarAccesoPrivado(string atributo)
    {
        if (!PuedeInteractuar())
            return;

        RegistrarError(
            "privado_" + atributo,
            "Acceso denegado: -" + atributo + " es privado.",
            true,
            0.25f
        );
        DebugLog("intento privado sobre " + atributo + " | puntaje=" + puntaje);
    }

    private void RecalcularPuntaje()
    {
        puntaje = Mathf.Max(0, puntajeBaseTiempo - penalizacionAcumulada);
    }

    private IEnumerator FinalizarPracticaRutina()
    {
        float tiempo = 0f;
        while (tiempo < Mathf.Max(0.25f, esperaAntesDeCompletarNivel))
        {
            tiempo += Time.unscaledDeltaTime;
            float escala = 1f + Mathf.Sin(tiempo * 9f) * 0.025f;
            if (visualRoot != null && runtimeNivel3 == null)
                visualRoot.localScale = Vector3.one * escala;
            yield return null;
        }

        if (visualRoot != null && runtimeNivel3 == null)
            visualRoot.localScale = Vector3.one;

        GuardarProgreso();

        if (completarNivelAutomaticamente)
        {
            if (progressPanel == null)
                progressPanel = FindFirstObjectByType<AlgoLabProgressPanel>(FindObjectsInactive.Include);

            if (progressPanel != null)
                progressPanel.TerminarPracticaActual();
        }

        rutinaFinal = null;
    }

    private void GuardarProgreso()
    {
        AlgoLabProgressSaver saver = AlgoLabProgressSaver.Instance;
        if (saver == null)
            saver = FindFirstObjectByType<AlgoLabProgressSaver>(FindObjectsInactive.Include);

        if (saver != null)
            saver.GuardarProgresoSiAplica(3, true, puntaje, 0, 1);
    }

    private void ResolverReferenciasEscena()
    {
        progressPanel = FindFirstObjectByType<AlgoLabProgressPanel>(FindObjectsInactive.Include);
        diagramController = FindFirstObjectByType<AlgoLabClassDiagramController>(FindObjectsInactive.Include);
    }

    private void PrepararDiagramaRobot()
    {
        if (robotData == null)
            robotData = GetComponent<AlgoLabObjetoEducativo>();
        if (robotData == null)
            robotData = gameObject.AddComponent<AlgoLabObjetoEducativo>();

        robotData.nombreObjeto = "Robot de mantenimiento";
        robotData.nombreClase = "Robot";
        robotData.descripcionObjeto =
            "Robot reparable cuyos atributos internos solo cambian mediante metodos publicos validados.";
        robotData.atributos = new[]
        {
            "- bateria",
            "- temperatura",
            "- estado"
        };
        robotData.metodos = new[]
        {
            "+ apagar()",
            "+ cargar()",
            "+ enfriar()"
        };

        if (diagramController == null)
            return;

        diagramController.nombreClasePreferidaPractica = "Robot";
        diagramController.CambiarAModoPracticaConObjeto(robotData);

        for (int i = 0; i < robotData.atributos.Length; i++)
            diagramController.RegistrarAtributoEncontrado(robotData, robotData.atributos[i]);
        for (int i = 0; i < robotData.metodos.Length; i++)
            diagramController.RegistrarMetodoEncontrado(robotData, robotData.metodos[i]);
    }

    private void ConstruirRobot()
    {
        Transform robot = CrearAncla("Robot", visualRoot, new Vector3(-0.58f, 0f, 0f));

        CrearPrimitiva("Cabeza", PrimitiveType.Cube, robot,
            new Vector3(0f, 0.46f, 0f), new Vector3(0.34f, 0.24f, 0.28f), materialRobot);
        CrearPrimitiva("Cuello", PrimitiveType.Cylinder, robot,
            new Vector3(0f, 0.29f, 0f), new Vector3(0.12f, 0.07f, 0.12f), materialOscuro);

        CrearPrimitiva("TorsoSuperior", PrimitiveType.Cube, robot,
            new Vector3(0f, 0.18f, 0.05f), new Vector3(0.52f, 0.08f, 0.25f), materialRobot);
        CrearPrimitiva("TorsoInferior", PrimitiveType.Cube, robot,
            new Vector3(0f, -0.25f, 0.05f), new Vector3(0.52f, 0.08f, 0.25f), materialRobot);
        CrearPrimitiva("MarcoIzquierdo", PrimitiveType.Cube, robot,
            new Vector3(-0.23f, -0.03f, 0.05f), new Vector3(0.07f, 0.38f, 0.25f), materialRobot);
        CrearPrimitiva("MarcoDerecho", PrimitiveType.Cube, robot,
            new Vector3(0.23f, -0.03f, 0.05f), new Vector3(0.07f, 0.38f, 0.25f), materialRobot);

        CrearPrimitiva("BrazoIzquierdo", PrimitiveType.Capsule, robot,
            new Vector3(-0.39f, 0f, 0f), new Vector3(0.11f, 0.34f, 0.11f), materialRobot);
        CrearPrimitiva("BrazoDerecho", PrimitiveType.Capsule, robot,
            new Vector3(0.39f, 0f, 0f), new Vector3(0.11f, 0.34f, 0.11f), materialRobot);
        CrearPrimitiva("PiernaIzquierda", PrimitiveType.Capsule, robot,
            new Vector3(-0.14f, -0.53f, 0f), new Vector3(0.13f, 0.36f, 0.13f), materialOscuro);
        CrearPrimitiva("PiernaDerecha", PrimitiveType.Capsule, robot,
            new Vector3(0.14f, -0.53f, 0f), new Vector3(0.13f, 0.36f, 0.13f), materialOscuro);

        Renderer ojoIzq = CrearPrimitiva("OjoIzquierdo", PrimitiveType.Sphere, robot,
            new Vector3(-0.075f, 0.48f, -0.145f), Vector3.one * 0.055f, materialVerde);
        Renderer ojoDer = CrearPrimitiva("OjoDerecho", PrimitiveType.Sphere, robot,
            new Vector3(0.075f, 0.48f, -0.145f), Vector3.one * 0.055f, materialVerde);
        ojos = new[] { ojoIzq, ojoDer };

        luzEstadoRenderer = CrearPrimitiva("LuzEstado", PrimitiveType.Sphere, robot,
            new Vector3(0f, 0.19f, -0.13f), Vector3.one * 0.06f, materialVerde);

        bateriaRenderer = CrearPrimitiva("AtributoPrivado_Bateria", PrimitiveType.Cube, robot,
            new Vector3(-0.115f, -0.04f, -0.09f), new Vector3(0.15f, 0.24f, 0.12f), materialAmarillo);
        cargaBateriaVisual = CrearPrimitiva("CargaBateria", PrimitiveType.Cube, robot,
            new Vector3(-0.115f, -0.04f, -0.158f), new Vector3(0.105f, 0.18f, 0.025f), materialVerde).transform;
        temperaturaRenderer = CrearPrimitiva("AtributoPrivado_Temperatura", PrimitiveType.Sphere, robot,
            new Vector3(0.115f, 0.01f, -0.10f), Vector3.one * 0.13f, materialRojo);
        moduloRenderer = CrearPrimitiva("AtributoPrivado_Estado", PrimitiveType.Cube, robot,
            new Vector3(0.115f, -0.15f, -0.10f), new Vector3(0.16f, 0.10f, 0.11f), materialAzul);
    }

    private void ConstruirPanelHerramientas()
    {
        RectTransform panel = CrearCanvasPanel(
            "PanelHerramientasPublicas",
            visualRoot,
            new Vector3(0.68f, 0.02f, -0.03f),
            new Vector2(520f, 700f),
            0.00135f,
            new Color(0.025f, 0.045f, 0.065f, 0.97f)
        );

        CrearTexto(panel, "Titulo", "TALLER DEL ROBOT", new Vector2(0f, -38f),
            new Vector2(460f, 52f), 30f, Color.white, TextAlignmentOptions.Center);
        CrearTexto(panel, "Subtitulo", "HERRAMIENTAS  (+ publicas)", new Vector2(0f, -92f),
            new Vector2(460f, 42f), 22f, new Color(0.2f, 1f, 0.72f), TextAlignmentOptions.Center);

        CrearBotonMetodo(panel, "Metodo_Apagar", "+", "Apagar", -154f, MetodoApagar);
        CrearBotonMetodo(panel, "Metodo_Recargar", "+", "Recargar bateria", -230f, MetodoRecargar);
        CrearBotonMetodo(panel, "Metodo_Enfriar", "+", "Enfriar", -306f, MetodoEnfriar);
        CrearBotonMetodo(panel, "Metodo_Reparar", "+", "Reparar modulo", -382f, MetodoReparar);
        CrearBotonMetodo(panel, "Metodo_Encender", "+", "Encender", -458f, MetodoEncender);

        statusText = CrearTexto(panel, "EstadoRobot", string.Empty, new Vector2(0f, -550f),
            new Vector2(455f, 82f), 21f, Color.white, TextAlignmentOptions.Center);
        scoreText = CrearTexto(panel, "Puntaje", string.Empty, new Vector2(0f, -618f),
            new Vector2(455f, 40f), 23f, new Color(1f, 0.84f, 0.25f), TextAlignmentOptions.Center);
        feedbackText = CrearTexto(panel, "Feedback", string.Empty, new Vector2(0f, -665f),
            new Vector2(470f, 55f), 18f, new Color(0.70f, 0.88f, 1f), TextAlignmentOptions.Center);
    }

    private void ConstruirPanelAtributosPrivados()
    {
        RectTransform panel = CrearCanvasPanel(
            "PanelComponentesInternos",
            visualRoot,
            new Vector3(-1.08f, -0.06f, -0.20f),
            new Vector2(360f, 260f),
            0.00105f,
            new Color(0.10f, 0.025f, 0.035f, 0.94f)
        );

        CrearTexto(panel, "TituloPrivados", "COMPONENTES INTERNOS", new Vector2(0f, -30f),
            new Vector2(330f, 38f), 20f, Color.white, TextAlignmentOptions.Center);
        CrearBotonPrivado(panel, "AtributoPrivado_Energia", "- energia", -90f,
            IntentarModificarEnergiaPrivada);
        CrearBotonPrivado(panel, "AtributoPrivado_Temperatura", "- temperatura", -148f,
            IntentarModificarTemperaturaPrivada);
        CrearBotonPrivado(panel, "AtributoPrivado_Estado", "- estado", -206f,
            IntentarModificarEstadoPrivado);
    }

    private void ConstruirHerramientasFisicas()
    {
        Transform tools = CrearAncla("HerramientasFisicas", visualRoot, Vector3.zero);

        Transform cargador = CrearAncla("Cargador", tools, new Vector3(0.31f, 0.15f, -0.08f));
        CrearPrimitiva("Cuerpo", PrimitiveType.Cube, cargador, Vector3.zero,
            new Vector3(0.10f, 0.15f, 0.08f), materialAzul);
        CrearPrimitiva("Conector", PrimitiveType.Cylinder, cargador, new Vector3(0f, -0.11f, 0f),
            new Vector3(0.035f, 0.06f, 0.035f), materialOscuro);

        Transform refrigerante = CrearAncla("Refrigerante", tools, new Vector3(0.31f, -0.07f, -0.08f));
        CrearPrimitiva("Botella", PrimitiveType.Cylinder, refrigerante, Vector3.zero,
            new Vector3(0.07f, 0.14f, 0.07f), materialAzul);
        CrearPrimitiva("Tapa", PrimitiveType.Cylinder, refrigerante, new Vector3(0f, 0.1f, 0f),
            new Vector3(0.045f, 0.025f, 0.045f), materialBlanco);

        Transform llave = CrearAncla("Llave", tools, new Vector3(0.31f, -0.29f, -0.08f));
        Transform mango = CrearPrimitiva("Mango", PrimitiveType.Cube, llave, Vector3.zero,
            new Vector3(0.045f, 0.24f, 0.035f), materialBlanco).transform;
        mango.localRotation = Quaternion.Euler(0f, 0f, -32f);
        CrearPrimitiva("Cabeza", PrimitiveType.Sphere, llave, new Vector3(-0.07f, 0.10f, 0f),
            Vector3.one * 0.075f, materialBlanco);
    }

    private void CrearBotonMetodo(
        RectTransform parent,
        string nombre,
        string signo,
        string etiqueta,
        float y,
        UnityEngine.Events.UnityAction accion)
    {
        Button button = CrearBotonBase(
            parent, nombre, signo, etiqueta, y,
            new Color(0.04f, 0.28f, 0.24f, 0.98f),
            new Color(0.04f, 0.80f, 0.62f, 1f),
            new Color(0.12f, 0.95f, 0.66f, 1f)
        );
        button.onClick.AddListener(accion);
    }

    private void CrearBotonPrivado(
        RectTransform parent,
        string nombre,
        string etiqueta,
        float y,
        UnityEngine.Events.UnityAction accion)
    {
        Button button = CrearBotonBase(
            parent, nombre, "-", etiqueta.Substring(2), y,
            new Color(0.40f, 0.07f, 0.09f, 0.98f),
            new Color(0.88f, 0.14f, 0.18f, 1f),
            new Color(1f, 0.30f, 0.32f, 1f),
            new Vector2(315f, 48f),
            18f
        );
        button.onClick.AddListener(accion);
    }

    private Button CrearBotonBase(
        RectTransform parent,
        string nombre,
        string signo,
        string etiqueta,
        float y,
        Color normal,
        Color hover,
        Color badgeColor,
        Vector2? size = null,
        float fontSize = 22f)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer),
            typeof(Image), typeof(Button), typeof(AlgoLabRobotPracticeButton));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size ?? new Vector2(440f, 62f);
        rect.anchoredPosition = new Vector2(0f, y);

        Image image = go.GetComponent<Image>();
        image.color = normal;
        image.raycastTarget = true;

        Button button = go.GetComponent<Button>();
        button.transition = Selectable.Transition.None;

        AlgoLabRobotPracticeButton marker = go.GetComponent<AlgoLabRobotPracticeButton>();
        marker.background = image;
        marker.normalColor = normal;
        marker.hoverColor = hover;

        RectTransform badge = CrearRect("SignoAcceso", rect, new Vector2(-184f, 0f),
            new Vector2(54f, rect.sizeDelta.y - 8f));
        Image badgeImage = badge.gameObject.AddComponent<Image>();
        badgeImage.color = badgeColor;
        badgeImage.raycastTarget = false;
        CrearTextoCentrado(badge, "TextoSigno", signo, Vector2.zero, badge.sizeDelta,
            fontSize + 8f, Color.black, TextAlignmentOptions.Center);

        CrearTextoCentrado(rect, "Texto", etiqueta, new Vector2(25f, 0f),
            new Vector2(rect.sizeDelta.x - 90f, rect.sizeDelta.y), fontSize,
            Color.white, TextAlignmentOptions.Center);
        return button;
    }

    private RectTransform CrearCanvasPanel(
        string nombre,
        Transform parent,
        Vector3 localPosition,
        Vector2 size,
        float scale,
        Color background)
    {
        GameObject canvasGo = new GameObject(nombre, typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup), typeof(Image));
        RectTransform rect = canvasGo.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.localPosition = localPosition;
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one * scale;
        rect.sizeDelta = size;

        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;
        canvas.sortingOrder = 40;

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 12f;

        Image image = canvasGo.GetComponent<Image>();
        image.color = background;
        image.raycastTarget = false;
        return rect;
    }

    private TMP_Text CrearTexto(
        RectTransform parent,
        string nombre,
        string texto,
        Vector2 posicion,
        Vector2 size,
        float fontSize,
        Color color,
        TextAlignmentOptions alignment)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = posicion;

        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = texto;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        tmp.raycastTarget = false;
        return tmp;
    }

    private TMP_Text CrearTextoCentrado(
        RectTransform parent,
        string nombre,
        string texto,
        Vector2 posicion,
        Vector2 size,
        float fontSize,
        Color color,
        TextAlignmentOptions alignment)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = posicion;

        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = texto;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static RectTransform CrearRect(
        string nombre,
        RectTransform parent,
        Vector2 posicion,
        Vector2 size)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = posicion;
        return rect;
    }

    private static Transform CrearAncla(
        string nombre,
        Transform parent,
        Vector3 localPosition)
    {
        Transform anchor = new GameObject(nombre).transform;
        anchor.SetParent(parent, false);
        anchor.localPosition = localPosition;
        return anchor;
    }

    private static Renderer CrearPrimitiva(
        string nombre,
        PrimitiveType tipo,
        Transform parent,
        Vector3 localPosition,
        Vector3 localScale,
        Material material)
    {
        GameObject go = GameObject.CreatePrimitive(tipo);
        go.name = nombre;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localScale = localScale;

        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = material;

        Collider collider = go.GetComponent<Collider>();
        if (collider != null)
            collider.enabled = false;
        return renderer;
    }

    private void CrearMateriales()
    {
        materialRobot = CrearMaterial("Robot_Carcasa", new Color(0.035f, 0.24f, 0.42f), 0.35f, 0.72f);
        materialOscuro = CrearMaterial("Robot_Oscuro", new Color(0.035f, 0.06f, 0.09f), 0.75f, 0.55f);
        materialVerde = CrearMaterialUnlit("Robot_Verde", new Color(0.05f, 0.95f, 0.55f));
        materialRojo = CrearMaterialUnlit("Robot_Rojo", new Color(0.95f, 0.08f, 0.10f));
        materialAzul = CrearMaterialUnlit("Robot_Azul", new Color(0.05f, 0.55f, 1f));
        materialAmarillo = CrearMaterialUnlit("Robot_Amarillo", new Color(1f, 0.70f, 0.05f));
        materialBlanco = CrearMaterial("Robot_Metal", new Color(0.72f, 0.80f, 0.86f), 0.88f, 0.58f);
    }

    private static Material CrearMaterial(
        string nombre,
        Color color,
        float metallic,
        float smoothness)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");
        Material material = new Material(shader) { name = nombre, color = color };
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);
        if (material.HasProperty("_Metallic"))
            material.SetFloat("_Metallic", metallic);
        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", smoothness);
        return material;
    }

    private static Material CrearMaterialUnlit(string nombre, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            return CrearMaterial(nombre, color, 0f, 0f);

        Material material = new Material(shader) { name = nombre, color = color };
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);
        return material;
    }

    private void ActualizarVisuales()
    {
        if (!visualConstruido)
            return;

        if (workshopVisual != null)
            workshopVisual.RefrescarEstado();

        if (cargaBateriaVisual != null)
        {
            float factor = Mathf.Clamp01(energia / 100f);
            Vector3 scale = cargaBateriaVisual.localScale;
            scale.y = 0.18f * Mathf.Max(0.05f, factor);
            cargaBateriaVisual.localScale = scale;
            Vector3 pos = cargaBateriaVisual.localPosition;
            pos.y = -0.13f + 0.09f * factor;
            cargaBateriaVisual.localPosition = pos;
        }

        Color colorEnergia = energia >= energiaMinimaEncendido
            ? new Color(0.05f, 1f, 0.55f)
            : new Color(1f, 0.62f, 0.05f);
        AplicarColor(bateriaRenderer, colorEnergia);

        Color colorTemperatura = temperatura <= temperaturaMaximaEncendido
            ? new Color(0.05f, 0.72f, 1f)
            : new Color(1f, 0.06f, 0.04f);
        AplicarColorEmision(temperaturaRenderer, colorTemperatura);
        AplicarColor(moduloRenderer, averiado
            ? new Color(1f, 0.08f, 0.12f)
            : new Color(0.05f, 0.92f, 0.55f));

        if (ojos != null)
        {
            Color eyeColor = encendido
                ? new Color(0.05f, 1f, 0.55f)
                : new Color(0.05f, 0.10f, 0.12f);
            for (int i = 0; i < ojos.Length; i++)
                AplicarColorEmision(ojos[i], eyeColor);
        }

        if (statusText != null)
        {
            statusText.text =
                "Energia " + energia + "%   |   " +
                temperatura + " C\n" +
                (averiado ? "SISTEMA AVERIADO" : "SISTEMA REPARADO") +
                "   |   " + (encendido ? "ENCENDIDO" : "APAGADO");
        }

        if (scoreText != null)
            scoreText.text = "PUNTOS: " + puntaje;
    }

    private void MostrarFeedback(string mensaje)
    {
        if (workshopVisual != null)
            workshopVisual.MostrarFeedback(mensaje);
        if (feedbackText != null)
            feedbackText.text = mensaje;
        FeedbackCambiado?.Invoke(mensaje);
        DebugLog(mensaje);
    }

    private static void AplicarColor(Renderer renderer, Color color)
    {
        if (renderer == null || renderer.material == null)
            return;
        Material material = renderer.material;
        material.color = color;
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);
    }

    private static void AplicarColorEmision(Renderer renderer, Color color)
    {
        if (renderer == null || renderer.material == null)
            return;

        Material material = renderer.material;
        material.color = color;
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);
        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * 1.8f);
        }
    }

    private static void DestruirSeguro(GameObject go)
    {
        if (go == null)
            return;
#if UNITY_EDITOR
        if (!Application.isPlaying)
            DestroyImmediate(go);
        else
            Destroy(go);
#else
        Destroy(go);
#endif
    }

    private void OnDestroy()
    {
        DestruirMaterialSeguro(materialRobot);
        DestruirMaterialSeguro(materialOscuro);
        DestruirMaterialSeguro(materialVerde);
        DestruirMaterialSeguro(materialRojo);
        DestruirMaterialSeguro(materialAzul);
        DestruirMaterialSeguro(materialAmarillo);
        DestruirMaterialSeguro(materialBlanco);
    }

    private static void DestruirMaterialSeguro(Material material)
    {
        if (material == null)
            return;
#if UNITY_EDITOR
        if (!Application.isPlaying)
            DestroyImmediate(material);
        else
            Destroy(material);
#else
        Destroy(material);
#endif
    }

    private void DebugLog(string message)
    {
        if (mostrarDebug)
            Debug.Log("PRACTICA ROBOT NIVEL 3: " + message);
    }
}
