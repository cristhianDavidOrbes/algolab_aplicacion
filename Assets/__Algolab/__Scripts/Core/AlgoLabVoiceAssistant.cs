using System.Collections;
using TMPro;
using UnityEngine;
using Meta.XR.BuildingBlocks.AIBlocks;
using UnityEngine.Android;

public class AlgoLabVoiceAssistant : MonoBehaviour
{
    [Header("Speech To Text - AI Building Block")]
    public SpeechToTextAgent speechToText;

    [Header("Speech To Text - AlgoLab gratuito")]
    public AlgoLabSpeechToTextClient speechToTextLocal;

    [Header("Text To Speech - AI Building Block")]
    public TextToSpeechAgent textToSpeech;

    [Header("IA AlgoLab")]
    public AlgoLabIAClient iaClient;

    [Header("Panel de revisión IA")]
    public AlgoLabIAReviewPanel panelRevisionIA;

    [Header("Subtitulos IA")]
    public AlgoLabAISubtitlePanel subtitulosIA;

    [Header("UI Opcional")]
    public TMP_Text textoEscuchado;
    public TMP_Text textoRespuesta;
    public TMP_Text textoEstado;

    [Header("Botón para hablar")]
    public OVRInput.Button botonHablar = OVRInput.Button.Two;
    public OVRInput.Controller controladorBoton = OVRInput.Controller.RTouch;
    public bool usarControladorEspecifico = true;

    [Header("Micrófono")]
    public bool pedirPermisoMicrofonoAlIniciar = true;
    public float tiempoMinimoGrabacion = 1.0f;
    public float tiempoMaximoGrabacion = 12f;

    [Header("Regrabación")]
    [Tooltip("Si está apagado, el botón Regrabar solo deja listo el panel. El usuario debe mantener B otra vez.")]
    public bool regrabarIniciaEscuchaAutomaticamente = false;

    [Header("Tiempo máximo IA")]
    public float tiempoMaximoRespuestaIA = 25f;

    [Header("Errores IA")]
    public bool mostrarErrorEnSubtitulos = true;
    public bool leerErroresConVoz = false;

    [Header("Debug")]
    public bool mostrarDebug = true;
    public bool debugBotonMientrasPresionado = true;
    public float intervaloDebugBotonMantenido = 0.5f;
    public bool mostrarDebugMicrofonos = true;

    private bool esperandoRespuesta;
    private bool escuchando;
    private bool esperandoStopPorGrabacionCorta;
    private bool listenersConectados;

    private float tiempoInicioEscucha;
    private float siguienteDebugBoton;
    private int contadorIntentosEscucha;
    private int contadorTranscripciones;
    private int contadorTranscripcionesVacias;
    private int solicitudIAActual;

    private Coroutine rutinaTimeoutIA;
    private Coroutine rutinaAutoStopGrabacion;
    private Coroutine rutinaStopMinimo;
    private Coroutine rutinaCambioRespuestaVoz;
    private AlgoLabGameSettings ajustesSalidaIA;
    private AlgoLabLevel3RobotPracticeRuntime robotPracticeIA;

    private const string TAG = "voice: ";

    private void Awake()
    {
        if (speechToTextLocal == null)
        {
            speechToTextLocal = GetComponent<AlgoLabSpeechToTextClient>();
        }

        if (speechToTextLocal == null)
        {
            speechToTextLocal = gameObject.AddComponent<AlgoLabSpeechToTextClient>();
        }

        Log("AWAKE ejecutado.");
        LogReferencias("Awake");
    }

    private void Start()
    {
        Log("START ejecutado.");
        LogSistemaInicial();
        ConectarAjustesSalidaIA();

#if UNITY_ANDROID && !UNITY_EDITOR
        if (pedirPermisoMicrofonoAlIniciar)
        {
            RevisarPermisoMicrofono(true);
        }
#else
        Log("No está en Android real. El permiso de micrófono se revisa principalmente en Quest.");
#endif

        if (mostrarDebugMicrofonos)
        {
            MostrarMicrofonosDisponibles();
        }

        ActualizarEstadoUI("Listo. Mantén B para hablar.");
    }

    private void OnEnable()
    {
        Log("ON ENABLE ejecutado.");
        ConectarListeners();
        ConectarAjustesSalidaIA();
    }

    private void OnDisable()
    {
        Log("ON DISABLE ejecutado.");

        if (escuchando)
        {
            ForzarDetenerEscucha("OnDisable");
        }

        if (esperandoRespuesta && iaClient != null)
        {
            iaClient.CancelarSolicitudActual();
        }

        esperandoRespuesta = false;
        solicitudIAActual++;

        DesconectarListeners();
        DesconectarAjustesSalidaIA();
        DetenerTimeoutIA();
        DetenerRutinasEscucha();
        DetenerCambioRespuestaVoz();
    }

    private void OnDestroy()
    {
        Log("ON DESTROY ejecutado.");

        if (escuchando)
        {
            ForzarDetenerEscucha("OnDestroy");
        }

        if (esperandoRespuesta && iaClient != null)
        {
            iaClient.CancelarSolicitudActual();
        }

        esperandoRespuesta = false;
        solicitudIAActual++;
        DesconectarListeners();
        DesconectarAjustesSalidaIA();
        DetenerCambioRespuestaVoz();
    }

    private void ConectarAjustesSalidaIA()
    {
        AlgoLabGameSettings servicio = AlgoLabGameSettings.Instance;
        if (ajustesSalidaIA == servicio)
        {
            AplicarModoSalidaIA();
            return;
        }

        DesconectarAjustesSalidaIA();
        ajustesSalidaIA = servicio;

        if (ajustesSalidaIA != null)
        {
            ajustesSalidaIA.AjustesCambiaron += AplicarModoSalidaIA;
        }

        AplicarModoSalidaIA();
    }

    private void DesconectarAjustesSalidaIA()
    {
        if (ajustesSalidaIA != null)
        {
            ajustesSalidaIA.AjustesCambiaron -= AplicarModoSalidaIA;
            ajustesSalidaIA = null;
        }
    }

    private void AplicarModoSalidaIA()
    {
        bool mostrarSubtitulos = ajustesSalidaIA == null || ajustesSalidaIA.MostrarSubtitulosIA;
        if (subtitulosIA != null)
        {
            subtitulosIA.SetSubtitulosActivos(mostrarSubtitulos);
        }
    }

    private void Update()
    {
        bool botonDown = ObtenerBotonDown();
        bool botonHold = ObtenerBotonHold();
        bool botonUp = ObtenerBotonUp();

        if (botonDown)
        {
            Log(
                "BOTÓN HABLAR DOWN | Botón: " + botonHablar +
                " | Controller: " + controladorBoton +
                " | ActiveController: " + OVRInput.GetActiveController()
            );

            IniciarEscucha();
        }

        if (botonHold && debugBotonMientrasPresionado && Time.unscaledTime >= siguienteDebugBoton)
        {
            siguienteDebugBoton = Time.unscaledTime + Mathf.Max(0.1f, intervaloDebugBotonMantenido);

            Log(
                "BOTÓN HABLAR MANTENIDO | escuchando=" + escuchando +
                " | esperandoRespuesta=" + esperandoRespuesta +
                " | tiempoGrabando=" + ObtenerDuracionGrabacion().ToString("0.00")
            );
        }

        if (botonUp)
        {
            Log(
                "BOTÓN HABLAR UP | duración=" +
                ObtenerDuracionGrabacion().ToString("0.00") + " segundos"
            );

            DetenerEscucha();
        }
    }

    private bool ObtenerBotonDown()
    {
        if (usarControladorEspecifico)
        {
            return OVRInput.GetDown(botonHablar, controladorBoton);
        }

        return OVRInput.GetDown(botonHablar);
    }

    private bool ObtenerBotonHold()
    {
        if (usarControladorEspecifico)
        {
            return OVRInput.Get(botonHablar, controladorBoton);
        }

        return OVRInput.Get(botonHablar);
    }

    private bool ObtenerBotonUp()
    {
        if (usarControladorEspecifico)
        {
            return OVRInput.GetUp(botonHablar, controladorBoton);
        }

        return OVRInput.GetUp(botonHablar);
    }

    private void ConectarListeners()
    {
        if (listenersConectados)
        {
            LogWarning("Los listeners ya estaban conectados. Se evita duplicarlos.");
            return;
        }

        if (speechToTextLocal != null)
        {
            speechToTextLocal.onTranscript.RemoveListener(OnTextoReconocido);
            speechToTextLocal.onTranscript.AddListener(OnTextoReconocido);
            speechToTextLocal.onError.RemoveListener(OnErrorSTT);
            speechToTextLocal.onError.AddListener(OnErrorSTT);
            Log("Listeners conectados: speechToTextLocal.");
        }
        else
        {
            LogError("AlgoLabSpeechToTextClient está NULL en OnEnable.");
        }

        if (iaClient != null)
        {
            iaClient.OnRespuestaIA.RemoveListener(OnRespuestaIA);
            iaClient.OnRespuestaIA.AddListener(OnRespuestaIA);

            iaClient.OnErrorIA.RemoveListener(OnErrorIA);
            iaClient.OnErrorIA.AddListener(OnErrorIA);

            Log("Listeners conectados: iaClient.OnRespuestaIA y iaClient.OnErrorIA.");
        }
        else
        {
            LogWarning("AlgoLabIAClient está NULL en OnEnable.");
        }

        if (textToSpeech != null)
        {
            textToSpeech.onSpeakStarting.RemoveListener(OnComenzoDeHablar);
            textToSpeech.onSpeakStarting.AddListener(OnComenzoDeHablar);
            textToSpeech.onSpeakFinished.RemoveListener(OnTerminoDeHablar);
            textToSpeech.onSpeakFinished.AddListener(OnTerminoDeHablar);
            Log("Listener conectado: textToSpeech.onSpeakFinished.");
        }
        else
        {
            LogWarning("TextToSpeechAgent está NULL en OnEnable.");
        }

        listenersConectados = true;
    }

    private void DesconectarListeners()
    {
        if (!listenersConectados)
        {
            return;
        }

        if (speechToTextLocal != null)
        {
            speechToTextLocal.onTranscript.RemoveListener(OnTextoReconocido);
            speechToTextLocal.onError.RemoveListener(OnErrorSTT);
            Log("Listeners removidos: speechToTextLocal.");
        }

        if (iaClient != null)
        {
            iaClient.OnRespuestaIA.RemoveListener(OnRespuestaIA);
            iaClient.OnErrorIA.RemoveListener(OnErrorIA);
            Log("Listeners removidos: iaClient.OnRespuestaIA y iaClient.OnErrorIA.");
        }

        if (textToSpeech != null)
        {
            textToSpeech.onSpeakStarting.RemoveListener(OnComenzoDeHablar);
            textToSpeech.onSpeakFinished.RemoveListener(OnTerminoDeHablar);
            Log("Listener removido: textToSpeech.onSpeakFinished.");
        }

        listenersConectados = false;
    }

    public void IniciarEscucha()
    {
        contadorIntentosEscucha++;

        Log(
            "IniciarEscucha llamado. Intento #" + contadorIntentosEscucha +
            " | escuchando=" + escuchando +
            " | esperandoRespuesta=" + esperandoRespuesta +
            " | esperandoStopPorGrabacionCorta=" + esperandoStopPorGrabacionCorta
        );

        LogReferencias("IniciarEscucha");

        if (speechToTextLocal == null)
        {
            LogError("No se pudo crear AlgoLabSpeechToTextClient.");

            if (panelRevisionIA != null)
            {
                panelRevisionIA.MostrarErrorYTerminar(
                    "No se encontró el sistema de reconocimiento de voz."
                );
            }

            return;
        }

        if (speechToTextLocal.Procesando)
        {
            LogWarning("La transcripción anterior todavía se está procesando.");
            ActualizarEstadoUI("Procesando la grabación anterior...");
            return;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        if (!RevisarPermisoMicrofono(true))
        {
            ActualizarEstadoUI("Activa el permiso de micrófono.");
            return;
        }
#endif

        if (esperandoRespuesta)
        {
            LogWarning("No se inicia escucha porque la IA todavía está respondiendo.");
            ActualizarEstadoUI("La IA todavía está respondiendo.");
            return;
        }

        if (escuchando)
        {
            LogWarning("Ya estaba escuchando. Se evita iniciar otra grabación encima.");
            ActualizarEstadoUI("Ya estoy escuchando...");
            return;
        }

        DetenerRutinasEscucha();

        escuchando = true;
        esperandoStopPorGrabacionCorta = false;
        tiempoInicioEscucha = Time.unscaledTime;
        siguienteDebugBoton = Time.unscaledTime;

        ActualizarEstadoUI("Escuchando...");

        if (panelRevisionIA != null)
        {
            panelRevisionIA.MostrarGrabando();
            Log("Panel revisión: MostrarGrabando().");
        }
        else
        {
            LogWarning("panelRevisionIA está NULL.");
        }

        try
        {
            Log("Llamando speechToTextLocal.StartListening().");
            speechToTextLocal.StartListening();
            Log("speechToTextLocal.StartListening() ejecutado sin excepción.");

            rutinaAutoStopGrabacion = StartCoroutine(AutoStopGrabacion());
        }
        catch (System.Exception ex)
        {
            escuchando = false;
            LogError("Excepción en StartListening(): " + ex.Message + "\n" + ex.StackTrace);
            ActualizarEstadoUI("Error iniciando micrófono.");
        }
    }

    public void DetenerEscucha()
    {
        Log(
            "DetenerEscucha llamado | speechToTextLocal null=" + (speechToTextLocal == null) +
            " | escuchando=" + escuchando +
            " | duración=" + ObtenerDuracionGrabacion().ToString("0.00")
        );

        if (speechToTextLocal == null)
        {
            LogWarning("No se puede detener porque speechToTextLocal está NULL.");
            return;
        }

        if (!escuchando)
        {
            LogWarning("No se detiene porque escuchando=false.");
            return;
        }

        float duracion = ObtenerDuracionGrabacion();

        if (duracion < tiempoMinimoGrabacion)
        {
            if (!esperandoStopPorGrabacionCorta)
            {
                float tiempoRestante = tiempoMinimoGrabacion - duracion;
                esperandoStopPorGrabacionCorta = true;

                LogWarning(
                    "Grabación muy corta: " + duracion.ToString("0.00") +
                    " s. Se esperará " + tiempoRestante.ToString("0.00") +
                    " s antes de detener."
                );

                ActualizarEstadoUI("Grabación muy corta, espera un momento...");

                rutinaStopMinimo = StartCoroutine(DetenerCuandoCumplaMinimo(tiempoRestante));
            }

            return;
        }

        DetenerEscuchaAhora("DetenerEscucha normal");
    }

    private IEnumerator DetenerCuandoCumplaMinimo(float tiempoRestante)
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, tiempoRestante));

        if (escuchando)
        {
            DetenerEscuchaAhora("Detención automática después de tiempo mínimo");
        }

        esperandoStopPorGrabacionCorta = false;
        rutinaStopMinimo = null;
    }

    private IEnumerator AutoStopGrabacion()
    {
        float duracionMaxima = Mathf.Max(tiempoMinimoGrabacion, tiempoMaximoGrabacion, 0.1f);
        yield return new WaitForSecondsRealtime(duracionMaxima);

        if (escuchando)
        {
            LogWarning("Tiempo máximo de grabación alcanzado. Se detiene automáticamente.");
            DetenerEscuchaAhora("AutoStop por tiempo máximo");
        }

        rutinaAutoStopGrabacion = null;
    }

    private void DetenerEscuchaAhora(string motivo)
    {
        Log(
            "DetenerEscuchaAhora ejecutado. Motivo: " + motivo +
            " | duraciónFinal=" + ObtenerDuracionGrabacion().ToString("0.00")
        );

        if (speechToTextLocal == null)
        {
            LogError("speechToTextLocal está NULL al intentar StopNow().");
            escuchando = false;
            return;
        }

        escuchando = false;
        esperandoStopPorGrabacionCorta = false;

        if (rutinaAutoStopGrabacion != null)
        {
            StopCoroutine(rutinaAutoStopGrabacion);
            rutinaAutoStopGrabacion = null;
        }

        ActualizarEstadoUI("Procesando voz...");

        if (panelRevisionIA != null)
        {
            panelRevisionIA.MostrarProcesando();
            Log("Panel revisión: MostrarProcesando().");
        }

        try
        {
            Log("Llamando speechToTextLocal.StopNow().");
            speechToTextLocal.StopNow();
            Log("speechToTextLocal.StopNow() ejecutado sin excepción.");
        }
        catch (System.Exception ex)
        {
            LogError("Excepción en StopNow(): " + ex.Message + "\n" + ex.StackTrace);
            ActualizarEstadoUI("Error procesando voz.");
        }
    }

    private void ForzarDetenerEscucha(string motivo)
    {
        LogWarning("ForzarDetenerEscucha llamado. Motivo: " + motivo);

        if (speechToTextLocal != null)
        {
            try
            {
                speechToTextLocal.StopNow();
                Log("StopNow forzado ejecutado.");
            }
            catch (System.Exception ex)
            {
                LogError("Error en StopNow forzado: " + ex.Message);
            }
        }

        escuchando = false;
        esperandoStopPorGrabacionCorta = false;
        DetenerRutinasEscucha();
    }

    private void OnTextoReconocido(string texto)
    {
        contadorTranscripciones++;
        escuchando = false;
        esperandoStopPorGrabacionCorta = false;

        int longitud = string.IsNullOrEmpty(texto) ? 0 : texto.Length;

        Log(
            "OnTextoReconocido recibido. #" + contadorTranscripciones +
            " | longitud=" + longitud +
            " | texto=[" + texto + "]"
        );

        DetenerRutinasEscucha();

        if (string.IsNullOrWhiteSpace(texto))
        {
            contadorTranscripcionesVacias++;

            LogWarning(
                "Speech To Text devolvió texto vacío. Vacíos=" +
                contadorTranscripcionesVacias + "/" + contadorTranscripciones
            );

            ActualizarEstadoUI("No se reconoció ningún mensaje.");

            if (panelRevisionIA != null)
            {
                panelRevisionIA.MostrarRevision(
                    "No se reconoció ningún mensaje. Puedes regrabar.",
                    OnMensajeConfirmadoDesdePanel,
                    RegrabarDesdePanel
                );

                Log("Panel revisión: mensaje vacío mostrado.");
            }
            else
            {
                LogWarning("No hay panelRevisionIA para mostrar texto vacío.");
            }

            return;
        }

        Log("Texto reconocido correctamente: " + texto);

        if (textoEscuchado != null)
        {
            textoEscuchado.text = "Dijiste: " + texto;
        }

        ActualizarEstadoUI("Mensaje reconocido.");

        if (panelRevisionIA != null)
        {
            panelRevisionIA.MostrarRevision(
                texto,
                OnMensajeConfirmadoDesdePanel,
                RegrabarDesdePanel
            );

            Log("Panel revisión: MostrarRevision(texto reconocido).");
        }
        else
        {
            Log("No hay panelRevisionIA. Se confirma automáticamente.");
            OnMensajeConfirmadoDesdePanel(texto);
        }
    }

    private void OnErrorSTT(string error)
    {
        escuchando = false;
        esperandoStopPorGrabacionCorta = false;
        DetenerRutinasEscucha();

        string mensaje = string.IsNullOrWhiteSpace(error)
            ? "No se pudo transcribir la grabación."
            : error;
        LogError("Error STT: " + mensaje);
        ActualizarEstadoUI(mensaje);

        if (panelRevisionIA != null)
        {
            panelRevisionIA.MostrarErrorYTerminar(mensaje);
        }
    }

    private void OnMensajeConfirmadoDesdePanel(string textoFinal)
    {
        Log("OnMensajeConfirmadoDesdePanel llamado con texto: [" + textoFinal + "]");

        if (string.IsNullOrWhiteSpace(textoFinal))
        {
            LogWarning("No se puede enviar texto vacío a la IA.");

            if (panelRevisionIA != null)
            {
                panelRevisionIA.MostrarErrorYTerminar(
                    "No se puede enviar un mensaje vacío."
                );
            }

            return;
        }

        if (iaClient == null)
        {
            LogError("No asignaste AlgoLabIAClient.");

            if (panelRevisionIA != null)
            {
                panelRevisionIA.MostrarErrorYTerminar(
                    "La IA no está configurada en este momento."
                );
            }

            return;
        }

        solicitudIAActual++;
        esperandoRespuesta = true;

        ActualizarEstadoUI("Consultando IA...");

        if (panelRevisionIA != null)
        {
            panelRevisionIA.MostrarCargandoIA();
            Log("Panel revisión: MostrarCargandoIA().");
        }

        string preguntaFinal = textoFinal;

        if (AlgoLabSelectionManager.Instance != null &&
            AlgoLabSelectionManager.Instance.HayObjetoSeleccionado())
        {
            string contextoObjeto =
                AlgoLabSelectionManager.Instance.ObtenerContextoObjetoSeleccionado();

            preguntaFinal =
                contextoObjeto +
                "\nPregunta del estudiante:\n" +
                textoFinal;

            Log("Se agregó contexto del objeto seleccionado.");
        }
        else
        {
            Log("No hay objeto seleccionado. Se envía pregunta sin contexto extra.");
        }

        AlgoLabLevel3RobotPracticeRuntime robotIA =
            ObtenerRobotPracticeActivo();
        if (robotIA != null)
        {
            preguntaFinal =
                robotIA.ConstruirPreguntaParaRobot(preguntaFinal);
            Log("Se agrego el estado del robot de la practica 3.");
        }

        Log("Enviando pregunta final a IA:\n" + preguntaFinal);

        IniciarTimeoutIA();

        try
        {
            iaClient.PreguntarDesdeTexto(preguntaFinal);
            Log("iaClient.PreguntarDesdeTexto() ejecutado.");
        }
        catch (System.Exception ex)
        {
            esperandoRespuesta = false;
            DetenerTimeoutIA();

            LogError("Error enviando pregunta a la IA: " + ex.Message + "\n" + ex.StackTrace);

            MostrarErrorIA("La IA no está disponible en este momento.");
        }
    }

    private void RegrabarDesdePanel()
    {
        Log("RegrabarDesdePanel llamado.");

        if (esperandoRespuesta)
        {
            LogWarning("No se puede regrabar mientras la IA responde.");
            ActualizarEstadoUI("No se puede regrabar mientras la IA responde.");
            return;
        }

        if (escuchando)
        {
            LogWarning("Ya estaba escuchando al presionar regrabar.");
            return;
        }

        if (regrabarIniciaEscuchaAutomaticamente)
        {
            Log("Regrabar inicia escucha automáticamente.");
            IniciarEscucha();
        }
        else
        {
            Log("Regrabar NO inicia escucha. El usuario debe mantener B otra vez.");
            ActualizarEstadoUI("Mantén B para grabar de nuevo.");

            if (panelRevisionIA != null)
            {
                panelRevisionIA.MostrarErrorYTerminar(
                    "Mantén B para grabar de nuevo."
                );
            }
        }
    }

    private void OnRespuestaIA(string respuesta)
    {
        if (!esperandoRespuesta)
        {
            LogWarning("Respuesta IA ignorada porque ya no se esperaba respuesta.");
            return;
        }

        esperandoRespuesta = false;
        DetenerTimeoutIA();

        int longitud = string.IsNullOrEmpty(respuesta) ? 0 : respuesta.Length;

        Log(
            "OnRespuestaIA recibido. Longitud=" + longitud +
            " | respuesta=[" + respuesta + "]"
        );

        if (string.IsNullOrWhiteSpace(respuesta))
        {
            LogWarning("La IA respondió vacío.");
            MostrarErrorIA("La IA no generó una respuesta válida.");
            return;
        }

        AlgoLabLevel3RobotPracticeRuntime robotIA =
            ObtenerRobotPracticeActivo();
        if (robotIA != null)
            respuesta = robotIA.PrepararRespuestaDelRobot(respuesta);

        if (textoRespuesta != null)
        {
            textoRespuesta.text = respuesta;
        }

        AlgoLabGameSettings preferencias = AlgoLabGameSettings.Instance;
        bool mostrarSubtitulos = preferencias == null || preferencias.MostrarSubtitulosIA;
        bool reproducirAudio = preferencias == null || preferencias.ReproducirAudioIA;

        ActualizarEstadoUI(reproducirAudio
            ? "Leyendo respuesta..."
            : "Respuesta lista. Mantén B para hablar otra vez.");

        if (panelRevisionIA != null)
        {
            panelRevisionIA.MostrarProcesoCompletado();
            Log("Panel revisión: MostrarProcesoCompletado().");
        }

        if (subtitulosIA != null)
        {
            subtitulosIA.SetSubtitulosActivos(mostrarSubtitulos);
            if (mostrarSubtitulos)
            {
                subtitulosIA.ShowSubtitle(respuesta);
                Log("Subtítulos IA mostrados.");
            }
        }
        else if (mostrarSubtitulos)
        {
            LogWarning("subtitulosIA está NULL.");
        }

        bool vozRobotPermitida =
            robotIA == null || robotIA.PuedeReproducirVozRobot;
        if (reproducirAudio && vozRobotPermitida && textToSpeech != null)
        {
            IniciarRespuestaDeVoz(respuesta, robotIA, true);
        }
        else if (reproducirAudio && !vozRobotPermitida)
        {
            if (textToSpeech != null && robotIA != null)
                IniciarRespuestaDeVoz(respuesta, robotIA, false);
            Log("Robot apagado: la respuesta queda solo en subtitulos.");
            ActualizarEstadoUI("Robot apagado. Respuesta en subtitulos.");
        }
        else if (reproducirAudio)
        {
            LogWarning("No se asignó TextToSpeechAgent.");
            ActualizarEstadoUI("Respuesta recibida, pero no hay voz configurada.");
        }
    }

    private void IniciarRespuestaDeVoz(
        string respuesta,
        AlgoLabLevel3RobotPracticeRuntime robotIA,
        bool reproducirAlFinal)
    {
        DetenerCambioRespuestaVoz();
        rutinaCambioRespuestaVoz = StartCoroutine(
            CambiarRespuestaDeVozRutina(
                respuesta,
                robotIA,
                reproducirAlFinal
            )
        );
    }

    private IEnumerator CambiarRespuestaDeVozRutina(
        string respuesta,
        AlgoLabLevel3RobotPracticeRuntime robotIA,
        bool reproducirAlFinal)
    {
        if (textToSpeech == null)
        {
            rutinaCambioRespuestaVoz = null;
            yield break;
        }

        AudioSource fuente = textToSpeech.GetComponent<AudioSource>();
        if (robotIA != null)
            yield return robotIA.DesvanecerRespuestaActual(fuente);

        try
        {
            // Cancela también una síntesis anterior que aún no hubiera
            // empezado a sonar. El fundido del clip audible ya terminó.
            textToSpeech.StopSpeaking();

            if (reproducirAlFinal)
            {
                Log("Iniciando nueva voz después del fundido.");
                textToSpeech.SpeakText(respuesta);
            }
        }
        catch (System.Exception ex)
        {
            LogError(
                "Error cambiando la respuesta de voz: " +
                ex.Message + "\n" + ex.StackTrace
            );
            ActualizarEstadoUI(
                "Respuesta recibida, pero falló la voz."
            );
        }

        rutinaCambioRespuestaVoz = null;
    }

    private void DetenerCambioRespuestaVoz()
    {
        if (rutinaCambioRespuestaVoz == null)
            return;
        StopCoroutine(rutinaCambioRespuestaVoz);
        rutinaCambioRespuestaVoz = null;
    }

    private void OnErrorIA(string error)
    {
        if (!esperandoRespuesta)
        {
            LogWarning("Error IA ignorado porque ya no se esperaba respuesta: " + error);
            return;
        }

        esperandoRespuesta = false;
        DetenerTimeoutIA();

        if (string.IsNullOrWhiteSpace(error))
        {
            error = "La IA no pudo responder en este momento.";
        }

        LogWarning("OnErrorIA recibido: " + error);

        MostrarErrorIA(error);
    }

    private void MostrarErrorIA(string mensaje)
    {
        ActualizarEstadoUI("Error de IA.");

        if (textoRespuesta != null)
        {
            textoRespuesta.text = mensaje;
        }

        if (panelRevisionIA != null)
        {
            panelRevisionIA.MostrarErrorYTerminar(mensaje);
        }

        AlgoLabGameSettings preferencias = AlgoLabGameSettings.Instance;
        bool mostrarSubtitulos = preferencias == null || preferencias.MostrarSubtitulosIA;
        bool reproducirAudio = preferencias == null || preferencias.ReproducirAudioIA;

        if (subtitulosIA != null && mostrarErrorEnSubtitulos && mostrarSubtitulos)
        {
            subtitulosIA.ShowErrorSubtitle(mensaje);
        }

        if (leerErroresConVoz && reproducirAudio && textToSpeech != null)
        {
            try
            {
                textToSpeech.SpeakText(mensaje);
            }
            catch (System.Exception ex)
            {
                LogError("Error intentando leer mensaje de error con voz: " + ex.Message);
            }
        }
    }

    private void OnTerminoDeHablar()
    {
        ObtenerRobotPracticeActivo()?.NotificarFinVozRobot();
        ActualizarEstadoUI("Listo. Mantén B para hablar otra vez.");
        Log("Text To Speech terminó de hablar.");
    }

    private void OnComenzoDeHablar(string texto)
    {
        AlgoLabLevel3RobotPracticeRuntime robotIA =
            ObtenerRobotPracticeActivo();
        if (robotIA == null || textToSpeech == null)
            return;

        robotIA.NotificarInicioVozRobot(
            textToSpeech.GetComponent<AudioSource>()
        );
    }

    private AlgoLabLevel3RobotPracticeRuntime ObtenerRobotPracticeActivo()
    {
        if (robotPracticeIA != null &&
            robotPracticeIA.IAActivaEnEstaPractica)
        {
            return robotPracticeIA;
        }

        robotPracticeIA =
            FindFirstObjectByType<AlgoLabLevel3RobotPracticeRuntime>(
                FindObjectsInactive.Exclude
            );
        return robotPracticeIA != null &&
               robotPracticeIA.IAActivaEnEstaPractica
            ? robotPracticeIA
            : null;
    }

    private void IniciarTimeoutIA()
    {
        Log("IniciarTimeoutIA: " + tiempoMaximoRespuestaIA + " segundos.");
        DetenerTimeoutIA();
        rutinaTimeoutIA = StartCoroutine(TimeoutRespuestaIA(solicitudIAActual));
    }

    private void DetenerTimeoutIA()
    {
        if (rutinaTimeoutIA != null)
        {
            StopCoroutine(rutinaTimeoutIA);
            rutinaTimeoutIA = null;
            Log("Timeout IA detenido.");
        }
    }

    private IEnumerator TimeoutRespuestaIA(int solicitudId)
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(1f, tiempoMaximoRespuestaIA));

        if (!esperandoRespuesta)
        {
            Log("Timeout IA terminó, pero ya no se esperaba respuesta.");
            yield break;
        }

        if (solicitudId != solicitudIAActual)
        {
            Log("Timeout IA ignorado porque pertenece a una solicitud vieja.");
            yield break;
        }

        esperandoRespuesta = false;
        rutinaTimeoutIA = null;

        if (iaClient != null)
        {
            iaClient.CancelarSolicitudActual();
        }

        LogWarning("Timeout: la IA no respondió a tiempo.");

        MostrarErrorIA("La IA tardó demasiado en responder.");
    }

    private void DetenerRutinasEscucha()
    {
        if (rutinaAutoStopGrabacion != null)
        {
            StopCoroutine(rutinaAutoStopGrabacion);
            rutinaAutoStopGrabacion = null;
            Log("Rutina AutoStopGrabacion detenida.");
        }

        if (rutinaStopMinimo != null)
        {
            StopCoroutine(rutinaStopMinimo);
            rutinaStopMinimo = null;
            Log("Rutina StopMinimo detenida.");
        }
    }

    private float ObtenerDuracionGrabacion()
    {
        if (!escuchando)
        {
            return 0f;
        }

        return Time.unscaledTime - tiempoInicioEscucha;
    }

    private bool RevisarPermisoMicrofono(bool solicitarSiNoExiste)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        bool tienePermiso = Permission.HasUserAuthorizedPermission(Permission.Microphone);

        Log("Permiso micrófono actual: " + tienePermiso);

        if (!tienePermiso && solicitarSiNoExiste)
        {
            LogWarning("No hay permiso de micrófono. Solicitando permiso...");
            Permission.RequestUserPermission(Permission.Microphone);
        }

        return tienePermiso;
#else
        Log("RevisarPermisoMicrofono fuera de Android. Se retorna true.");
        return true;
#endif
    }

    private void MostrarMicrofonosDisponibles()
    {
        string[] dispositivos = Microphone.devices;

        if (dispositivos == null || dispositivos.Length == 0)
        {
            LogWarning("Microphone.devices está vacío. No se detectaron micrófonos.");
            return;
        }

        Log("Cantidad de micrófonos detectados: " + dispositivos.Length);

        for (int i = 0; i < dispositivos.Length; i++)
        {
            Log("Micrófono [" + i + "]: " + dispositivos[i]);
        }
    }

    private void LogSistemaInicial()
    {
        Log("===== DIAGNÓSTICO INICIAL =====");
        Log("Objeto activo: " + gameObject.name);
        Log("Application.platform: " + Application.platform);
        Log("InternetReachability: " + Application.internetReachability);
        Log("AudioSettings.outputSampleRate: " + AudioSettings.outputSampleRate);
        Log("Time.frameCount: " + Time.frameCount);
        Log("botonHablar: " + botonHablar);
        Log("controladorBoton: " + controladorBoton);
        Log("usarControladorEspecifico: " + usarControladorEspecifico);
        Log("tiempoMinimoGrabacion: " + tiempoMinimoGrabacion);
        Log("tiempoMaximoGrabacion: " + tiempoMaximoGrabacion);
        Log("tiempoMaximoRespuestaIA: " + tiempoMaximoRespuestaIA);
        Log("===== FIN DIAGNÓSTICO INICIAL =====");
    }

    private void LogReferencias(string origen)
    {
        Log(
            "Referencias desde " + origen +
            " | speechToTextLocal=" + NombreObjeto(speechToTextLocal) +
            " | speechToTextMeta=" + NombreObjeto(speechToText) +
            " | textToSpeech=" + NombreObjeto(textToSpeech) +
            " | iaClient=" + NombreObjeto(iaClient) +
            " | panelRevisionIA=" + NombreObjeto(panelRevisionIA) +
            " | subtitulosIA=" + NombreObjeto(subtitulosIA) +
            " | textoEstado=" + NombreObjeto(textoEstado)
        );
    }

    private string NombreObjeto(Object obj)
    {
        if (obj == null)
        {
            return "NULL";
        }

        return obj.name;
    }

    private void ActualizarEstadoUI(string mensaje)
    {
        if (textoEstado != null)
        {
            textoEstado.text = mensaje;
        }

        Log("Estado UI: " + mensaje);
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        Log("OnApplicationFocus: " + hasFocus);

        if (!hasFocus && escuchando)
        {
            ForzarDetenerEscucha("La app perdió focus");
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        Log("OnApplicationPause: " + pauseStatus);

        if (pauseStatus && escuchando)
        {
            ForzarDetenerEscucha("La app fue pausada");
        }

        if (pauseStatus && esperandoRespuesta && iaClient != null)
        {
            iaClient.CancelarSolicitudActual();
            esperandoRespuesta = false;
            DetenerTimeoutIA();
        }

        if (pauseStatus)
        {
            solicitudIAActual++;
        }
    }

    private void Log(string mensaje)
    {
        if (mostrarDebug)
        {
            Debug.Log(TAG + mensaje);
        }
    }

    private void LogWarning(string mensaje)
    {
        if (mostrarDebug)
        {
            Debug.LogWarning(TAG + mensaje);
        }
    }

    private void LogError(string mensaje)
    {
        Debug.LogError(TAG + mensaje);
    }
}
