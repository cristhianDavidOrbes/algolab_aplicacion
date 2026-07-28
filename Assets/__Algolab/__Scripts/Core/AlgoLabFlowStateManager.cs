using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class AlgoLabFlowStateManager : MonoBehaviour
{
    public static AlgoLabFlowStateManager Instance { get; private set; }

    public enum EstadoFlujoAlgolab
    {
        Ninguno,
        Tutorial,
        TemaNivel1,
        GuiaPracticaNivel1,
        PracticaNivel1,
        TemaNivel2,
        GuiaPracticaNivel2,
        PracticaNivel2,
        TemaPilares,
        GuiaPracticaPilares,
        PracticaPilares,
        IA
    }

    [Header("Estado actual")]
    public EstadoFlujoAlgolab estadoActual = EstadoFlujoAlgolab.Ninguno;

    [Header("Referencias principales")]
    public AlgoLabTemaPOOController temaNivel1Controller;
    public AlgoLabTemaPOOController temaNivel2Controller;
    public AlgoLabCarPracticeController practicaNivel1Controller;
    public AlgoLabLevel02PracticeController practicaNivel2Controller;
    public AlgoLabPillarLevelController pillarLevelController;
    public AlgoLabTutorialPanelController tutorialController;
    public AlgoLabVoiceAssistant voiceAssistant;

    [Header("Limpieza de objetos spawneados")]
    [Tooltip("Script que limpia objetos con tag Objeto y el ObjetoFrontalActual del ManualPanelSpawnManager.")]
    public AlgoLabLevelSmoothCleaner levelSmoothCleaner;

    [Tooltip("Garages o escenarios especiales que no siempre tienen tag Objeto. Se ocultan cuando se cambia de tema/práctica o cuando termina una práctica.")]
    public List<AlgoLabLevel02GarageController> garageControllers = new List<AlgoLabLevel02GarageController>();

    [Tooltip("Si está activo, busca automáticamente AlgoLabLevelSmoothCleaner y garages en la escena.")]
    public bool buscarLimpiadoresAutomaticamente = true;

    [Tooltip("Limpia objetos al iniciar tema, guía o práctica para que no queden objetos del flujo anterior.")]
    public bool limpiarObjetosAlCambiarFlujo = true;

    [Tooltip("Oculta garages cuando se hace limpieza.")]
    public bool ocultarGaragesAlLimpiar = true;

    [Tooltip("Si está activo usa la desaparición smooth del cleaner. Si está apagado, limpia inmediato.")]
    public bool limpiarConSmooth = true;

    [Tooltip("Segundos que espera antes de limpiar cuando TERMINA un tema o una práctica. No se usa en cambio manual de nivel para evitar mezclar flujos.")]
    public float retrasoAntesDeLimpiarAlTerminarFlujo = 2f;

    [Header("Búsqueda automática")]
    public bool buscarReferenciasAutomaticamente = true;

    [Header("Audio extra que también se debe detener")]
    public List<AudioSource> audioSourcesExtra = new List<AudioSource>();

    [Header("Reglas")]
    public bool detenerTutorialAudioYVideoAlCambiarFlujo = true;
    public bool detenerIAAlCambiarFlujo = true;
    public bool detenerAudiosExtraAlCambiarFlujo = true;

    [Header("Debug")]
    public bool mostrarDebug = true;

    private Coroutine rutinaLimpiezaRetardada;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        BuscarReferenciasSiHaceFalta();
    }

    private void OnDestroy()
    {
        if (rutinaLimpiezaRetardada != null)
        {
            StopCoroutine(rutinaLimpiezaRetardada);
            rutinaLimpiezaRetardada = null;
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void BuscarReferenciasSiHaceFalta()
    {
        if (!buscarReferenciasAutomaticamente)
        {
            return;
        }

        if (practicaNivel1Controller == null)
        {
            practicaNivel1Controller = FindFirstObjectByType<AlgoLabCarPracticeController>(FindObjectsInactive.Include);
        }

        if (practicaNivel2Controller == null)
        {
            practicaNivel2Controller = FindFirstObjectByType<AlgoLabLevel02PracticeController>(FindObjectsInactive.Include);
        }

        if (pillarLevelController == null)
        {
            pillarLevelController = FindFirstObjectByType<AlgoLabPillarLevelController>(FindObjectsInactive.Include);
        }

        if (tutorialController == null)
        {
            tutorialController = FindFirstObjectByType<AlgoLabTutorialPanelController>(FindObjectsInactive.Include);
        }

        if (voiceAssistant == null)
        {
            voiceAssistant = FindFirstObjectByType<AlgoLabVoiceAssistant>(FindObjectsInactive.Include);
        }

        if (temaNivel1Controller == null || temaNivel2Controller == null)
        {
            AlgoLabTemaPOOController[] temas = FindObjectsByType<AlgoLabTemaPOOController>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            for (int i = 0; i < temas.Length; i++)
            {
                AlgoLabTemaPOOController tema = temas[i];

                if (tema == null)
                {
                    continue;
                }

                string nombre = tema.name.ToLowerInvariant();

                if (temaNivel1Controller == null && (nombre.Contains("nivel1") || nombre.Contains("level1") || nombre.Contains("poo")))
                {
                    temaNivel1Controller = tema;
                }
                else if (temaNivel2Controller == null && (nombre.Contains("nivel2") || nombre.Contains("level2")))
                {
                    temaNivel2Controller = tema;
                }
            }
        }

        BuscarLimpiadoresSiHaceFalta();
    }

    private void BuscarLimpiadoresSiHaceFalta()
    {
        if (!buscarLimpiadoresAutomaticamente)
        {
            return;
        }

        if (levelSmoothCleaner == null)
        {
            levelSmoothCleaner = FindFirstObjectByType<AlgoLabLevelSmoothCleaner>(FindObjectsInactive.Include);
        }

        if (garageControllers == null)
        {
            garageControllers = new List<AlgoLabLevel02GarageController>();
        }

        if (garageControllers.Count == 0)
        {
            AlgoLabLevel02GarageController[] garages = FindObjectsByType<AlgoLabLevel02GarageController>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

            for (int i = 0; i < garages.Length; i++)
            {
                if (garages[i] != null && !garageControllers.Contains(garages[i]))
                {
                    garageControllers.Add(garages[i]);
                }
            }
        }
    }

    public void PrepararInicioTema(int nivelReal)
    {
        BuscarReferenciasSiHaceFalta();

        DetenerTutorialLigero();
        DetenerIA();
        CancelarGuiasYExplicaciones();
        DetenerTemas();
        DetenerAudiosExtra();
        LimpiarObjetosDeNivelSiCorresponde();

        estadoActual = nivelReal == 1
            ? EstadoFlujoAlgolab.TemaNivel1
            : nivelReal == 2
                ? EstadoFlujoAlgolab.TemaNivel2
                : EstadoFlujoAlgolab.TemaPilares;

        DebugLog("FLOW: preparando inicio de tema nivel " + nivelReal + ".");
    }

    public void PrepararGuiaPractica(int nivelReal)
    {
        BuscarReferenciasSiHaceFalta();

        DetenerTutorialLigero();
        DetenerIA();
        DetenerTemas();
        CancelarGuiasYExplicaciones();
        DetenerAudiosExtra();
        LimpiarObjetosDeNivelSiCorresponde();

        estadoActual = nivelReal == 1
            ? EstadoFlujoAlgolab.GuiaPracticaNivel1
            : nivelReal == 2
                ? EstadoFlujoAlgolab.GuiaPracticaNivel2
                : EstadoFlujoAlgolab.GuiaPracticaPilares;

        DebugLog("FLOW: preparando guía de práctica nivel " + nivelReal + ".");
    }

    public void PrepararInicioPractica(int nivelReal)
    {
        BuscarReferenciasSiHaceFalta();

        DetenerTutorialLigero();
        DetenerIA();
        DetenerTemas();
        CancelarGuiasYExplicaciones();
        DetenerAudiosExtra();
        LimpiarObjetosDeNivelSiCorresponde();

        estadoActual = nivelReal == 1
            ? EstadoFlujoAlgolab.PracticaNivel1
            : nivelReal == 2
                ? EstadoFlujoAlgolab.PracticaNivel2
                : EstadoFlujoAlgolab.PracticaPilares;

        DebugLog("FLOW: preparando práctica nivel " + nivelReal + ".");
    }

    public void DetenerTodoPorCambioDeNivel()
    {
        BuscarReferenciasSiHaceFalta();

        DetenerTutorialLigero();
        DetenerIA();
        DetenerTemas();
        CancelarGuiasYExplicaciones();
        DetenerAudiosExtra();
        LimpiarObjetosDeNivelSiCorresponde();

        estadoActual = EstadoFlujoAlgolab.Ninguno;
        DebugLog("FLOW: todo detenido por cambio de nivel/estado.");
    }

    public void DetenerTodoPorCambioManualDeNivel(int nuevoNivelReal)
    {
        BuscarReferenciasSiHaceFalta();

        DetenerTutorialLigero();
        DetenerIA();
        DetenerTemas();
        CancelarGuiasYExplicaciones();
        DetenerAudiosExtra();

        // Cuando el usuario selecciona otro nivel manualmente debe quedar limpio de inmediato.
        // Esto evita que el garage del nivel 2 siga visible o vuelva a escribir el collapse
        // mientras ya se escogió nivel 1 u otro nivel.
        LimpiarObjetosDeNivel(true);

        estadoActual = EstadoFlujoAlgolab.Ninguno;
        DebugLog("FLOW: flujo anterior cancelado por selección manual. Nuevo nivel real: " + nuevoNivelReal);
    }

    public void MarcarTutorialActivo()
    {
        estadoActual = EstadoFlujoAlgolab.Tutorial;
    }

    public void MarcarIAActiva()
    {
        estadoActual = EstadoFlujoAlgolab.IA;
    }

    public void MarcarFlujoLibre()
    {
        estadoActual = EstadoFlujoAlgolab.Ninguno;
    }

    public void LimpiarObjetosDeNivelSiCorresponde()
    {
        if (!limpiarObjetosAlCambiarFlujo)
        {
            return;
        }

        LimpiarObjetosDeNivel(false);
    }

    [ContextMenu("Limpiar objetos de nivel con smooth")]
    public void LimpiarObjetosDeNivelConSmooth()
    {
        LimpiarObjetosDeNivel(false);
    }

    [ContextMenu("Limpiar objetos de nivel inmediato")]
    public void LimpiarObjetosDeNivelInmediato()
    {
        LimpiarObjetosDeNivel(true);
    }

    [ContextMenu("Limpiar objetos con retraso y smooth")]
    public void LimpiarObjetosDeNivelConRetrasoSmooth()
    {
        LimpiarObjetosDeNivelConRetraso(retrasoAntesDeLimpiarAlTerminarFlujo);
    }

    public void LimpiarObjetosDeNivelConRetraso(float segundosRetraso)
    {
        BuscarLimpiadoresSiHaceFalta();

        if (rutinaLimpiezaRetardada != null)
        {
            StopCoroutine(rutinaLimpiezaRetardada);
            rutinaLimpiezaRetardada = null;
        }

        rutinaLimpiezaRetardada = StartCoroutine(
            LimpiarObjetosDeNivelConRetrasoRutina(segundosRetraso)
        );
    }

    private IEnumerator LimpiarObjetosDeNivelConRetrasoRutina(float segundosRetraso)
    {
        float espera = Mathf.Max(0f, segundosRetraso);

        if (espera > 0f)
        {
            yield return new WaitForSeconds(espera);
        }

        rutinaLimpiezaRetardada = null;

        // Después de esperar, se limpia con smooth.
        LimpiarObjetosDeNivel(false);
    }

    public void LimpiarObjetosDeNivel(bool inmediato)
    {
        if (rutinaLimpiezaRetardada != null)
        {
            StopCoroutine(rutinaLimpiezaRetardada);
            rutinaLimpiezaRetardada = null;
        }

        BuscarLimpiadoresSiHaceFalta();

        if (ocultarGaragesAlLimpiar)
        {
            OcultarGarages(inmediato);
        }

        if (levelSmoothCleaner != null)
        {
            if (inmediato || !limpiarConSmooth)
            {
                levelSmoothCleaner.LimpiarObjetosInmediato();
            }
            else
            {
                levelSmoothCleaner.LimpiarObjetosConSmooth();
            }
        }
        else
        {
            DebugLog("FLOW: no hay AlgoLabLevelSmoothCleaner asignado para limpiar objetos.");
        }
    }

    private void OcultarGarages(bool inmediato)
    {
        if (garageControllers == null)
        {
            return;
        }

        for (int i = 0; i < garageControllers.Count; i++)
        {
            AlgoLabLevel02GarageController garage = garageControllers[i];

            if (garage == null)
            {
                continue;
            }

            if (inmediato || !limpiarConSmooth)
            {
                garage.OcultarGarageInstantaneo();
            }
            else
            {
                garage.OcultarGarage();
            }
        }
    }

    private void DetenerTemas()
    {
        if (temaNivel1Controller != null)
        {
            temaNivel1Controller.DetenerTema();
        }

        if (temaNivel2Controller != null && temaNivel2Controller != temaNivel1Controller)
        {
            temaNivel2Controller.DetenerTema();
        }

        if (pillarLevelController != null)
        {
            pillarLevelController.DetenerFlujo();
        }
    }

    private void CancelarGuiasYExplicaciones()
    {
        if (practicaNivel1Controller != null)
        {
            practicaNivel1Controller.CancelarExplicacionPracticaPorCambioDeFlujo();
        }

        if (practicaNivel2Controller != null)
        {
            practicaNivel2Controller.CancelarTodoNivel2PorCambioDeFlujo(true);
        }
    }

    private void DetenerTutorialLigero()
    {
        if (!detenerTutorialAudioYVideoAlCambiarFlujo || tutorialController == null)
        {
            return;
        }

        tutorialController.DetenerAudioActual();
        tutorialController.DetenerVideoActual();
    }

    private void DetenerIA()
    {
        if (!detenerIAAlCambiarFlujo || voiceAssistant == null)
        {
            return;
        }

        voiceAssistant.DetenerEscucha();

        object tts = voiceAssistant.textToSpeech;
        InvocarSiExiste(tts, "StopSpeaking");
        InvocarSiExiste(tts, "Stop");
        InvocarSiExiste(tts, "Cancel");
        InvocarSiExiste(tts, "CancelSpeak");
    }

    private void DetenerAudiosExtra()
    {
        if (!detenerAudiosExtraAlCambiarFlujo || audioSourcesExtra == null)
        {
            return;
        }

        for (int i = 0; i < audioSourcesExtra.Count; i++)
        {
            AudioSource audioSource = audioSourcesExtra[i];

            if (audioSource != null)
            {
                audioSource.Stop();
            }
        }
    }

    private void InvocarSiExiste(object objetivo, string nombreMetodo)
    {
        if (objetivo == null || string.IsNullOrWhiteSpace(nombreMetodo))
        {
            return;
        }

        MethodInfo metodo = objetivo.GetType().GetMethod(
            nombreMetodo,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
        );

        if (metodo == null || metodo.GetParameters().Length > 0)
        {
            return;
        }

        try
        {
            metodo.Invoke(objetivo, null);
        }
        catch
        {
        }
    }

    private void DebugLog(string mensaje)
    {
        if (mostrarDebug)
        {
            Debug.Log(mensaje);
        }
    }
}
