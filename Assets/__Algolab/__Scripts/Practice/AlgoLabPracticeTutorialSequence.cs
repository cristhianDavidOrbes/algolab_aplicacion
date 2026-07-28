using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Video;

public class AlgoLabPracticeTutorialSequence : MonoBehaviour
{
    public enum TipoPractica
    {
        Nivel1AtributosYMetodos,
        Nivel2CrearObjetos,
        Nivel3Encapsulamiento
    }

    [Header("Configuracion")]
    public TipoPractica tipoPractica;
    public AlgoLabTutorialPanelController tutorialPanel;
    public VideoClip videoTutorial;
    public List<AudioClip> narraciones = new List<AudioClip>();

    [Header("Sincronizacion")]
    public float retrasoMultimedia = 0.55f;
    public float separacionAudios = 0.05f;
    public float esperaFinal = 0.75f;

    [Header("Controles de la guia")]
    [Tooltip("Tiempo maximo entre las dos pulsaciones de A que omiten esta guia.")]
    [Min(0.25f)] public float tiempoDobleA = 2f;

    [Header("Nivel 3 preparado")]
    [Tooltip("Duracion de la guia provisional de texto mientras no haya video ni narraciones asignadas.")]
    [Min(6f)] public float duracionGuiaTextoNivel3 = 13f;

    private Coroutine rutina;
    private UnityAction callbackFinal;
    private bool reproduciendo;
    private List<AlgoLabTutorialPanelController.EventoTutorial> eventosOriginales;
    private string nombreOriginal;
    private bool omitirOriginal;
    private bool botonAOriginal;
    private bool teclaAOriginal;
    private float tiempoDobleAOriginal;
    private bool cerrarConIniciarOriginal;
    private bool iniciarPracticaOriginal;
    private bool guardarAlOmitirOriginal;
    private bool omitirAlGuardarOriginal;
    private bool repetirAlRestaurarOriginal;
    private bool reiniciarAlRestaurarOriginal;

    public bool PuedeReproducir
    {
        get
        {
            if (!isActiveAndEnabled || tutorialPanel == null)
                return false;

            if (tipoPractica == TipoPractica.Nivel3Encapsulamiento)
                return true;

            if (videoTutorial == null || narraciones == null)
                return false;

            return narraciones.Exists(clip => clip != null);
        }
    }

    private void Awake() => BuscarTutorial();
    private void OnDisable() => Detener(false);

    public void Reproducir(UnityAction alTerminar)
    {
        BuscarTutorial();
        if (!PuedeReproducir)
        {
            alTerminar?.Invoke();
            return;
        }

        Detener(false);
        callbackFinal = alTerminar;
        rutina = StartCoroutine(ReproducirRutina());
    }

    public void Detener(bool notificarFinal = false)
    {
        if (rutina != null)
        {
            StopCoroutine(rutina);
            rutina = null;
        }

        if (reproduciendo && tutorialPanel != null)
            tutorialPanel.OcultarPanelInstantaneo();

        reproduciendo = false;
        RestaurarTutorial();
        UnityAction callback = callbackFinal;
        callbackFinal = null;
        if (notificarFinal) callback?.Invoke();
    }

    private IEnumerator ReproducirRutina()
    {
        reproduciendo = true;
        tutorialPanel.PrepararInicioAutomaticoExterno();
        GuardarTutorial();

        float tiempoCierre;
        tutorialPanel.eventos = ConstruirEventos(out tiempoCierre);
        tutorialPanel.nombreTutorial = tipoPractica == TipoPractica.Nivel1AtributosYMetodos
            ? "Pr\u00e1ctica nivel 1: atributos y m\u00e9todos"
            : tipoPractica == TipoPractica.Nivel2CrearObjetos
                ? "Pr\u00e1ctica nivel 2: crear objetos"
                : "Pr\u00e1ctica nivel 3: encapsulamiento";
        AplicarConfiguracionTemporalDePractica();
        tutorialPanel.PermitirReubicarTutorialUnaVez();
        tutorialPanel.IniciarTutorial();

        float tiempo = 0f;
        while (tutorialPanel != null && tutorialPanel.TutorialEnCurso && tiempo < tiempoCierre + 4f)
        {
            // El root real del tutorial se desactiva mientras esta dentro del
            // panel de opciones. No consumir el timeout externo en esa pausa.
            if (tutorialPanel.isActiveAndEnabled)
                tiempo += Time.unscaledDeltaTime;
            yield return null;
        }

        if (tutorialPanel != null && tutorialPanel.TutorialEnCurso)
            tutorialPanel.OcultarPanelInstantaneo();

        reproduciendo = false;
        rutina = null;
        RestaurarTutorial();
        UnityAction callback = callbackFinal;
        callbackFinal = null;
        callback?.Invoke();
    }

    private void AplicarConfiguracionTemporalDePractica()
    {
        tutorialPanel.iniciarPracticaAlFinalizar = false;
        tutorialPanel.cerrarTutorialSiPresionaBotonIniciar = false;
        tutorialPanel.permitirOmitirConDobleA = true;
        tutorialPanel.tiempoMaximoDobleA = Mathf.Max(0.25f, tiempoDobleA);
        tutorialPanel.permitirBotonAOVR = true;
        tutorialPanel.permitirTeclaAEnEditor = true;

        // Durante una guia de practica, guardar el panel debe pausar la
        // reproduccion. Al sacarlo se conserva el evento, el audio y el video
        // exactos en vez de omitir o reiniciar la secuencia.
        tutorialPanel.guardarTutorialEnPanelOpcionesAlOmitir = false;
        tutorialPanel.omitirTutorialAlGuardarEnPanelOpciones = false;
        tutorialPanel.repetirTutorialAlSacarTutorialOmitidoDesdePanelOpciones = false;
        tutorialPanel.reiniciarTutorialSiempreAlSacarDelPanelOpciones = false;
    }

    private List<AlgoLabTutorialPanelController.EventoTutorial> ConstruirEventos(out float tiempoCierre)
    {
        var eventos = new List<AlgoLabTutorialPanelController.EventoTutorial>();
        Agregar(eventos, 0f, 0, AlgoLabTutorialPanelController.TipoEventoTutorial.MostrarPanel);
        Agregar(eventos, 0f, 1, AlgoLabTutorialPanelController.TipoEventoTutorial.CambiarInstruccion,
            tipoPractica == TipoPractica.Nivel1AtributosYMetodos
                ? "Mira la demostraci\u00f3n y sigue el mando derecho paso a paso."
                : tipoPractica == TipoPractica.Nivel2CrearObjetos
                    ? "Mira c\u00f3mo configurar y enviar cada veh\u00edculo con el mando derecho."
                    : "Repara el robot usando el panel de herramientas y observa los signos de acceso.");
        Agregar(eventos, 0.08f, 0, AlgoLabTutorialPanelController.TipoEventoTutorial.MostrarPanelMando);
        Agregar(eventos, 0.12f, 0, AlgoLabTutorialPanelController.TipoEventoTutorial.CambiarMandoLateralConTransicion);
        Agregar(eventos, 0.18f, 0, AlgoLabTutorialPanelController.TipoEventoTutorial.ExpandirMando);
        Agregar(eventos, 0.22f, 0, AlgoLabTutorialPanelController.TipoEventoTutorial.MostrarMandoIdle);

        if (videoTutorial != null)
        {
            Agregar(eventos, 0.35f, 0, AlgoLabTutorialPanelController.TipoEventoTutorial.RevelarVideo);
            Agregar(eventos, retrasoMultimedia, 0,
                AlgoLabTutorialPanelController.TipoEventoTutorial.ReproducirVideoClip, video: videoTutorial);
        }

        string[] textos = TextosNarracion();
        float inicioAudio = retrasoMultimedia;
        if (narraciones != null)
        {
            for (int i = 0; i < narraciones.Count; i++)
            {
                AudioClip clip = narraciones[i];
                if (clip == null) continue;
                string texto = i < textos.Length ? textos[i] : "Sigue la demostraci\u00f3n del video.";
                Agregar(eventos, inicioAudio, 1,
                    AlgoLabTutorialPanelController.TipoEventoTutorial.CambiarInstruccion, texto);
                Agregar(eventos, inicioAudio, 2,
                    AlgoLabTutorialPanelController.TipoEventoTutorial.ReproducirAudioClip, audio: clip);
                inicioAudio += clip.length + Mathf.Max(0f, separacionAudios);
            }
        }

        if (tipoPractica == TipoPractica.Nivel1AtributosYMetodos)
        {
            PulsoGatillo(eventos, retrasoMultimedia + 19.1f, 10);
            PulsoGatillo(eventos, retrasoMultimedia + 24.6f, 20);
            PulsoGatillo(eventos, retrasoMultimedia + 45f, 30);
        }
        else if (tipoPractica == TipoPractica.Nivel2CrearObjetos)
        {
            PulsoGatillo(eventos, retrasoMultimedia + 25f, 10);
            PulsoGatillo(eventos, retrasoMultimedia + 35.2f, 20);
            PulsoGatillo(eventos, retrasoMultimedia + 49.2f, 30);
            PulsoGatillo(eventos, retrasoMultimedia + 58.3f, 40);
        }
        else if (videoTutorial == null && (narraciones == null || !narraciones.Exists(clip => clip != null)))
        {
            // Guia provisional funcional del nivel 3. Cuando se asignen el
            // video y los audios definitivos, la misma secuencia los utilizara.
            Agregar(eventos, 4.5f, 0,
                AlgoLabTutorialPanelController.TipoEventoTutorial.CambiarInstruccion,
                "El signo menos identifica atributos privados. Intentar cambiarlos directamente resta puntos.");
            Agregar(eventos, 8.5f, 0,
                AlgoLabTutorialPanelController.TipoEventoTutorial.CambiarInstruccion,
                "Usa las herramientas con signo m\u00e1s y termina dejando el robot encendido.");
            PulsoGatillo(eventos, 9.2f, 10);
            inicioAudio = Mathf.Max(inicioAudio, duracionGuiaTextoNivel3);
        }

        float duracionVideo = videoTutorial != null ? (float)videoTutorial.length : 0f;
        float fin = Mathf.Max(inicioAudio, retrasoMultimedia + duracionVideo);
        Agregar(eventos, fin + 0.05f, 0,
            AlgoLabTutorialPanelController.TipoEventoTutorial.CambiarInstruccion,
            tipoPractica == TipoPractica.Nivel1AtributosYMetodos
                ? "Listo: elige una etiqueta y clasif\u00edcala como atributo o m\u00e9todo."
                : tipoPractica == TipoPractica.Nivel2CrearObjetos
                    ? "Listo: lee el panel, configura el veh\u00edculo y pulsa Crear objeto."
                    : "Listo: diagnostica el robot y reparalo sin acceder directamente a sus atributos.");
        Agregar(eventos, fin + 0.1f, 0,
            AlgoLabTutorialPanelController.TipoEventoTutorial.OcultarIndicadoresGatillos);
        Agregar(eventos, fin + 0.15f, 0,
            AlgoLabTutorialPanelController.TipoEventoTutorial.OcultarPanelMando);
        tiempoCierre = fin + Mathf.Max(0.25f, esperaFinal);
        Agregar(eventos, tiempoCierre, 0, AlgoLabTutorialPanelController.TipoEventoTutorial.CerrarPanel);
        return eventos;
    }

    private string[] TextosNarracion()
    {
        if (tipoPractica == TipoPractica.Nivel1AtributosYMetodos)
            return new[] { "Apunta con el rayo y pulsa el gatillo principal: primero una etiqueta y luego Atributos o M\u00e9todos." };

        if (tipoPractica == TipoPractica.Nivel2CrearObjetos)
        {
            return new[]
            {
                "Objetivo: crea cinco veh\u00edculos siguiendo exactamente el panel de progreso.",
                "Lee primero el color, modelo, carcasa, estado y m\u00e9todo solicitados.",
                "Apunta a cada desplegable y pulsa el gatillo principal para elegir los atributos.",
                "Apunta al m\u00e9todo requerido y pulsa el gatillo principal para seleccionarlo.",
                "Comprueba todo y pulsa Crear objeto para enviarlo al garaje.",
                "Cuando est\u00e9s listo, apunta a Iniciar y pulsa el gatillo principal."
            };
        }

        return new[]
        {
            "Identifica los atributos privados del robot.",
            "Evita modificar directamente energia, temperatura o estado.",
            "Usa las herramientas publicas y vuelve a encender el robot."
        };
    }

    private static void PulsoGatillo(List<AlgoLabTutorialPanelController.EventoTutorial> eventos, float tiempo, int orden)
    {
        Agregar(eventos, tiempo, orden,
            AlgoLabTutorialPanelController.TipoEventoTutorial.PresionarGatilloPrincipal);
        Agregar(eventos, tiempo + 0.75f, orden + 1,
            AlgoLabTutorialPanelController.TipoEventoTutorial.SoltarGatilloPrincipal);
    }

    private static void Agregar(
        List<AlgoLabTutorialPanelController.EventoTutorial> eventos,
        float tiempo,
        int orden,
        AlgoLabTutorialPanelController.TipoEventoTutorial tipo,
        string texto = null,
        AudioClip audio = null,
        VideoClip video = null)
    {
        eventos.Add(new AlgoLabTutorialPanelController.EventoTutorial
        {
            tiempo = Mathf.Max(0f, tiempo),
            orden = orden,
            tipoEvento = tipo,
            texto = texto ?? string.Empty,
            audioClip = audio,
            videoClip = video,
            repetirVideo = false,
            reiniciarVideoDesdeInicio = true
        });
    }

    private void BuscarTutorial()
    {
        if (tutorialPanel == null)
            tutorialPanel = FindFirstObjectByType<AlgoLabTutorialPanelController>(FindObjectsInactive.Include);
    }

    private void GuardarTutorial()
    {
        eventosOriginales = tutorialPanel.eventos;
        nombreOriginal = tutorialPanel.nombreTutorial;
        omitirOriginal = tutorialPanel.permitirOmitirConDobleA;
        botonAOriginal = tutorialPanel.permitirBotonAOVR;
        teclaAOriginal = tutorialPanel.permitirTeclaAEnEditor;
        tiempoDobleAOriginal = tutorialPanel.tiempoMaximoDobleA;
        cerrarConIniciarOriginal = tutorialPanel.cerrarTutorialSiPresionaBotonIniciar;
        iniciarPracticaOriginal = tutorialPanel.iniciarPracticaAlFinalizar;
        guardarAlOmitirOriginal = tutorialPanel.guardarTutorialEnPanelOpcionesAlOmitir;
        omitirAlGuardarOriginal = tutorialPanel.omitirTutorialAlGuardarEnPanelOpciones;
        repetirAlRestaurarOriginal = tutorialPanel.repetirTutorialAlSacarTutorialOmitidoDesdePanelOpciones;
        reiniciarAlRestaurarOriginal = tutorialPanel.reiniciarTutorialSiempreAlSacarDelPanelOpciones;
    }

    private void RestaurarTutorial()
    {
        if (tutorialPanel == null || eventosOriginales == null) return;
        tutorialPanel.eventos = eventosOriginales;
        tutorialPanel.nombreTutorial = nombreOriginal;
        tutorialPanel.permitirOmitirConDobleA = omitirOriginal;
        tutorialPanel.permitirBotonAOVR = botonAOriginal;
        tutorialPanel.permitirTeclaAEnEditor = teclaAOriginal;
        tutorialPanel.tiempoMaximoDobleA = tiempoDobleAOriginal;
        tutorialPanel.cerrarTutorialSiPresionaBotonIniciar = cerrarConIniciarOriginal;
        tutorialPanel.iniciarPracticaAlFinalizar = iniciarPracticaOriginal;
        tutorialPanel.guardarTutorialEnPanelOpcionesAlOmitir = guardarAlOmitirOriginal;
        tutorialPanel.omitirTutorialAlGuardarEnPanelOpciones = omitirAlGuardarOriginal;
        tutorialPanel.repetirTutorialAlSacarTutorialOmitidoDesdePanelOpciones = repetirAlRestaurarOriginal;
        tutorialPanel.reiniciarTutorialSiempreAlSacarDelPanelOpciones = reiniciarAlRestaurarOriginal;
        eventosOriginales = null;
    }
}
