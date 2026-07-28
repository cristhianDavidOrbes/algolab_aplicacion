using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public class AlgoLabProgressTutorialGuard : MonoBehaviour
{
    [Header("Referencias principales")]
    public AlgoLabProgressPanel progressPanel;
    public AlgoLabFlowStateManager flowStateManager;
    public AlgoLabTutorialPanelController tutorialController;
    public AlgoLabManualPanelSpawnManager spawnManager;

    [Header("Bloqueo de niveles")]
    public bool buscarReferenciasAutomaticamente = true;
    public bool bloquearNivelesMientrasFlujoActivo = true;
    public bool bloquearNivelesMientrasTutorialActivo = false;
    public bool bloquearBotonAccionDuranteTutorial = false;

    [Header("Proteccion de audio del tutorial")]
    [Tooltip("Si esta activo, cuando el tutorial inicial esta corriendo permite presionar niveles, pero evita que el FlowStateManager corte el audio/video del tutorial.")]
    public bool protegerAudioTutorialMientrasSeIniciaNivel = true;

    [Tooltip("Si esta activo, al presionar el boton Iniciar de un nivel mientras el tutorial esta activo, el tutorial se omite/cierra automaticamente.")]
    public bool saltarTutorialAlIniciarNivel = true;

    [Tooltip("Si esta activo, el estado NivelSeleccionado NO cuenta como flujo activo. Esto permite escoger nivel antes de iniciar el tema/practica.")]
    public bool nivelSeleccionadoNoBloqueaBotones = true;

    [Tooltip("Si esta activo, aunque Bloquear Niveles Mientras Tutorial Activo quede prendido por accidente, el tutorial inicial nunca bloquea los botones de niveles.")]
    public bool permitirSeleccionNivelesDuranteTutorial = true;

    [Tooltip("Nombre del campo booleano del FlowStateManager que detiene el tutorial al cambiar de flujo. Normalmente no lo cambies.")]
    public string campoDetenerTutorialEnFlowState = "detenerTutorialAudioYVideoAlCambiarFlujo";

    [Tooltip("Si esta activo, al terminar el tutorial restaura la configuracion original del FlowStateManager.")]
    public bool restaurarProteccionAudioAlTerminarTutorial = true;

    [Tooltip("Desactiva botones/colliders bajo cada objeto de nivel para que ningun onClick externo pueda cambiar el collapse.")]
    public bool desactivarComponentesInteractivosDeNiveles = true;

    [Header("Asegurar paneles")]
    public bool asegurarPanelesAutomaticamente = true;
    public float segundosParaRevisarPaneles = 5f;

    [Tooltip("Arrastra aqui ProgressPanelRoot, ClassDiagramRoot, AIPanel, etc. Si se dejan vacios, se intentan buscar por nombre.")]
    public List<GameObject> panelesNecesarios = new List<GameObject>();

    [Tooltip("Si esta activo, despues de activar paneles llama UbicarPaneles() del ManualPanelSpawnManager.")]
    public bool reubicarPanelesConSpawnManager = true;

    [Header("Nombres para busqueda automatica de paneles")]
    public string[] nombresPanelesNecesarios = new string[]
    {
        "ProgressPanelRoot",
        "ClassDiagramRoot",
        "AIPanel",
        "VoicePanel",
        "AIReviewPanel"
    };

    [Header("Debug")]
    public bool mostrarDebug = true;

    private bool bloqueoAplicado;
    private bool flowDetenerTutorialOriginal;
    private bool flowDetenerTutorialOriginalGuardado;
    private bool proteccionAudioTutorialAplicada;
    private bool valorOriginalActivarSeleccionNiveles = true;
    private bool valorOriginalBtnPracticeInteractable = true;
    private bool tutorialActivoAnterior;
    private bool flujoActivoAnterior;
    private Coroutine rutinaAsegurarPaneles;

    private readonly Dictionary<Behaviour, bool> estadosBehaviour = new Dictionary<Behaviour, bool>();
    private readonly Dictionary<Collider, bool> estadosCollider = new Dictionary<Collider, bool>();
    private readonly Dictionary<Collider2D, bool> estadosCollider2D = new Dictionary<Collider2D, bool>();
    private readonly Dictionary<CanvasGroup, EstadoCanvasGroup> estadosCanvasGroup = new Dictionary<CanvasGroup, EstadoCanvasGroup>();

    private struct EstadoCanvasGroup
    {
        public bool interactable;
        public bool blocksRaycasts;
    }

    private void Awake()
    {
        BuscarReferenciasSiHaceFalta();
        GuardarEstadosBase();
    }

    private void Start()
    {
        BuscarReferenciasSiHaceFalta();
        GuardarEstadosBase();
        BuscarPanelesAutomaticamenteSiHaceFalta();
    }

    private void Update()
    {
        if (buscarReferenciasAutomaticamente)
        {
            BuscarReferenciasSiHaceFalta();
        }

        bool tutorialActivo = TutorialEstaActivo();
        bool flujoActivo = FlujoDeNivelEstaActivo();
        AplicarProteccionAudioTutorialSiHaceFalta(tutorialActivo);

        bool bloqueoPorTutorial = bloquearNivelesMientrasTutorialActivo && tutorialActivo && !permitirSeleccionNivelesDuranteTutorial;
        bool bloqueoPorFlujo = bloquearNivelesMientrasFlujoActivo && flujoActivo;

        bool debeBloquear = bloqueoPorTutorial || bloqueoPorFlujo;

        if (debeBloquear && !bloqueoAplicado)
        {
            AplicarBloqueoInteraccionNiveles(tutorialActivo);
        }
        else if (!debeBloquear && bloqueoAplicado)
        {
            RestaurarInteraccionNiveles();
        }

        if (saltarTutorialAlIniciarNivel && tutorialActivo && flujoActivo && !flujoActivoAnterior)
        {
            SaltarTutorialSiEstaActivo();
            tutorialActivo = TutorialEstaActivo();
        }

        if (asegurarPanelesAutomaticamente)
        {
            if (!tutorialActivo && tutorialActivoAnterior)
            {
                AsegurarPanelesDespuesDe5Segundos();
            }

            if (flujoActivo && !flujoActivoAnterior)
            {
                AsegurarPanelesDespuesDe5Segundos();
            }
        }

        tutorialActivoAnterior = tutorialActivo;
        flujoActivoAnterior = flujoActivo;
    }

    [ContextMenu("Asegurar paneles en 5 segundos")]
    public void AsegurarPanelesDespuesDe5Segundos()
    {
        if (rutinaAsegurarPaneles != null)
        {
            StopCoroutine(rutinaAsegurarPaneles);
        }

        rutinaAsegurarPaneles = StartCoroutine(AsegurarPanelesRutina());
    }

    [ContextMenu("Asegurar paneles ahora")]
    public void AsegurarPanelesAhora()
    {
        BuscarPanelesAutomaticamenteSiHaceFalta();

        int activados = 0;

        for (int i = 0; i < panelesNecesarios.Count; i++)
        {
            GameObject panel = panelesNecesarios[i];

            if (panel == null)
            {
                continue;
            }

            if (!panel.activeSelf)
            {
                panel.SetActive(true);
                activados++;
            }

            CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
        }

        if (reubicarPanelesConSpawnManager)
        {
            BuscarReferenciasSiHaceFalta();
            if (spawnManager != null)
            {
                spawnManager.UbicarPaneles();
            }
        }

        if (mostrarDebug)
        {
            Debug.Log("GUARD: paneles asegurados. Activados: " + activados);
        }
    }

    public void NotificarTutorialSaltadoOContinuado()
    {
        AsegurarPanelesDespuesDe5Segundos();
    }

    public void NotificarInicioDeNivel()
    {
        if (saltarTutorialAlIniciarNivel)
        {
            SaltarTutorialSiEstaActivo();
        }

        AsegurarPanelesDespuesDe5Segundos();
    }

    [ContextMenu("Saltar tutorial si esta activo")]
    public void SaltarTutorialSiEstaActivo()
    {
        BuscarReferenciasSiHaceFalta();

        if (tutorialController == null || !TutorialEstaActivo())
        {
            return;
        }

        RestaurarProteccionAudioTutorialSiHaceFalta(true);

        try
        {
            tutorialController.OmitirTutorial();
        }
        catch
        {
            try
            {
                tutorialController.CerrarPanel();
            }
            catch
            {
                tutorialController.gameObject.SetActive(false);
            }
        }

        AsegurarPanelesDespuesDe5Segundos();

        if (mostrarDebug)
        {
            Debug.Log("GUARD: tutorial omitido porque se inicio un nivel.");
        }
    }

    private IEnumerator AsegurarPanelesRutina()
    {
        yield return new WaitForSeconds(Mathf.Max(0f, segundosParaRevisarPaneles));
        AsegurarPanelesAhora();
        rutinaAsegurarPaneles = null;
    }

    private void AplicarBloqueoInteraccionNiveles(bool tutorialActivo)
    {
        BuscarReferenciasSiHaceFalta();
        GuardarEstadosBase();

        if (progressPanel != null)
        {
            progressPanel.activarSeleccionNiveles = false;

            if (tutorialActivo && bloquearBotonAccionDuranteTutorial && progressPanel.btnPractice != null)
            {
                progressPanel.btnPractice.interactable = false;
            }
        }

        if (desactivarComponentesInteractivosDeNiveles && progressPanel != null && progressPanel.levels != null)
        {
            for (int i = 0; i < progressPanel.levels.Length; i++)
            {
                AlgoLabProgressPanel.LevelVisual nivel = progressPanel.levels[i];
                if (nivel == null)
                {
                    continue;
                }

                BloquearObjetoNivel(nivel.levelObject);
                BloquearObjetoNivel(nivel.ok200);
                BloquearObjetoNivel(nivel.warning);
                BloquearObjetoNivel(nivel.error);
            }
        }

        bloqueoAplicado = true;

        if (mostrarDebug)
        {
            Debug.Log("GUARD: niveles bloqueados. Tutorial activo: " + tutorialActivo);
        }
    }

    private void RestaurarInteraccionNiveles()
    {
        if (progressPanel != null)
        {
            progressPanel.activarSeleccionNiveles = valorOriginalActivarSeleccionNiveles;

            if (progressPanel.btnPractice != null)
            {
                progressPanel.btnPractice.interactable = valorOriginalBtnPracticeInteractable;
            }
        }

        foreach (KeyValuePair<Behaviour, bool> item in estadosBehaviour)
        {
            if (item.Key != null)
            {
                item.Key.enabled = item.Value;
            }
        }

        foreach (KeyValuePair<Collider, bool> item in estadosCollider)
        {
            if (item.Key != null)
            {
                item.Key.enabled = item.Value;
            }
        }

        foreach (KeyValuePair<Collider2D, bool> item in estadosCollider2D)
        {
            if (item.Key != null)
            {
                item.Key.enabled = item.Value;
            }
        }

        foreach (KeyValuePair<CanvasGroup, EstadoCanvasGroup> item in estadosCanvasGroup)
        {
            if (item.Key != null)
            {
                item.Key.interactable = item.Value.interactable;
                item.Key.blocksRaycasts = item.Value.blocksRaycasts;
            }
        }

        bloqueoAplicado = false;

        if (mostrarDebug)
        {
            Debug.Log("GUARD: niveles desbloqueados.");
        }
    }

    private void BloquearObjetoNivel(GameObject root)
    {
        if (root == null)
        {
            return;
        }

        Button[] buttons = root.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null)
            {
                continue;
            }

            if (!estadosBehaviour.ContainsKey(buttons[i]))
            {
                estadosBehaviour.Add(buttons[i], buttons[i].enabled);
            }

            buttons[i].enabled = false;
            buttons[i].interactable = false;
        }

        Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] == null)
            {
                continue;
            }

            if (!estadosCollider.ContainsKey(colliders[i]))
            {
                estadosCollider.Add(colliders[i], colliders[i].enabled);
            }

            colliders[i].enabled = false;
        }

        Collider2D[] colliders2D = root.GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < colliders2D.Length; i++)
        {
            if (colliders2D[i] == null)
            {
                continue;
            }

            if (!estadosCollider2D.ContainsKey(colliders2D[i]))
            {
                estadosCollider2D.Add(colliders2D[i], colliders2D[i].enabled);
            }

            colliders2D[i].enabled = false;
        }

        CanvasGroup[] groups = root.GetComponentsInChildren<CanvasGroup>(true);
        for (int i = 0; i < groups.Length; i++)
        {
            if (groups[i] == null)
            {
                continue;
            }

            if (!estadosCanvasGroup.ContainsKey(groups[i]))
            {
                EstadoCanvasGroup estado = new EstadoCanvasGroup();
                estado.interactable = groups[i].interactable;
                estado.blocksRaycasts = groups[i].blocksRaycasts;
                estadosCanvasGroup.Add(groups[i], estado);
            }

            groups[i].interactable = false;
            groups[i].blocksRaycasts = false;
        }
    }

    private void GuardarEstadosBase()
    {
        if (progressPanel != null)
        {
            valorOriginalActivarSeleccionNiveles = progressPanel.activarSeleccionNiveles;

            if (progressPanel.btnPractice != null)
            {
                valorOriginalBtnPracticeInteractable = progressPanel.btnPractice.interactable;
            }
        }
    }

    private void AplicarProteccionAudioTutorialSiHaceFalta(bool tutorialActivo)
    {
        if (!protegerAudioTutorialMientrasSeIniciaNivel)
        {
            RestaurarProteccionAudioTutorialSiHaceFalta(true);
            return;
        }

        BuscarReferenciasSiHaceFalta();

        if (flowStateManager == null || string.IsNullOrWhiteSpace(campoDetenerTutorialEnFlowState))
        {
            return;
        }

        if (tutorialActivo)
        {
            FieldInfo campo = flowStateManager.GetType().GetField(
                campoDetenerTutorialEnFlowState,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
            );

            if (campo == null || campo.FieldType != typeof(bool))
            {
                return;
            }

            if (!flowDetenerTutorialOriginalGuardado)
            {
                flowDetenerTutorialOriginal = (bool)campo.GetValue(flowStateManager);
                flowDetenerTutorialOriginalGuardado = true;
            }

            if ((bool)campo.GetValue(flowStateManager))
            {
                campo.SetValue(flowStateManager, false);
            }

            proteccionAudioTutorialAplicada = true;
        }
        else if (restaurarProteccionAudioAlTerminarTutorial)
        {
            RestaurarProteccionAudioTutorialSiHaceFalta(false);
        }
    }

    private void RestaurarProteccionAudioTutorialSiHaceFalta(bool forzar)
    {
        if (!flowDetenerTutorialOriginalGuardado || flowStateManager == null)
        {
            return;
        }

        if (!forzar && !proteccionAudioTutorialAplicada)
        {
            return;
        }

        FieldInfo campo = flowStateManager.GetType().GetField(
            campoDetenerTutorialEnFlowState,
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
        );

        if (campo != null && campo.FieldType == typeof(bool))
        {
            campo.SetValue(flowStateManager, flowDetenerTutorialOriginal);
        }

        proteccionAudioTutorialAplicada = false;
        flowDetenerTutorialOriginalGuardado = false;
    }

    private bool FlujoDeNivelEstaActivo()
    {
        if (progressPanel != null)
        {
            object estado = ObtenerCampoPrivado(progressPanel, "estadoFlujoNivel");
            if (estado != null)
            {
                string estadoTexto = estado.ToString();
                if ((!nivelSeleccionadoNoBloqueaBotones && estadoTexto == "NivelSeleccionado") ||
                    estadoTexto == "TemaEnCurso" ||
                    estadoTexto == "TemaTerminado" ||
                    estadoTexto == "PracticaPreparada" ||
                    estadoTexto == "PracticaEnCurso")
                {
                    return true;
                }
            }
        }

        if (flowStateManager != null)
        {
            string estadoFlow = flowStateManager.estadoActual.ToString();
            if (estadoFlow != "Ninguno" && estadoFlow != "IA" && estadoFlow != "Tutorial")
            {
                return true;
            }
        }

        return false;
    }

    private bool TutorialEstaActivo()
    {
        if (tutorialController == null)
        {
            return false;
        }

        object activo = ObtenerCampoPrivado(tutorialController, "tutorialActivo");
        object finalizado = ObtenerCampoPrivado(tutorialController, "tutorialFinalizado");

        bool estaActivo = activo is bool && (bool)activo;
        bool estaFinalizado = finalizado is bool && (bool)finalizado;

        return estaActivo && !estaFinalizado;
    }

    private object ObtenerCampoPrivado(object instancia, string nombreCampo)
    {
        if (instancia == null)
        {
            return null;
        }

        FieldInfo field = instancia.GetType().GetField(
            nombreCampo,
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
        );

        if (field == null)
        {
            return null;
        }

        return field.GetValue(instancia);
    }

    private void OnDisable()
    {
        RestaurarProteccionAudioTutorialSiHaceFalta(true);

        if (bloqueoAplicado)
        {
            RestaurarInteraccionNiveles();
        }
    }

    private void BuscarReferenciasSiHaceFalta()
    {
        if (progressPanel == null)
        {
            progressPanel = FindFirstObjectByType<AlgoLabProgressPanel>();
        }

        if (flowStateManager == null)
        {
            flowStateManager = FindFirstObjectByType<AlgoLabFlowStateManager>();
        }

        if (tutorialController == null)
        {
            tutorialController = FindFirstObjectByType<AlgoLabTutorialPanelController>();
        }

        if (spawnManager == null)
        {
            spawnManager = AlgoLabManualPanelSpawnManager.Instance;
        }
    }

    private void BuscarPanelesAutomaticamenteSiHaceFalta()
    {
        if (panelesNecesarios == null)
        {
            panelesNecesarios = new List<GameObject>();
        }

        if (nombresPanelesNecesarios == null)
        {
            return;
        }

        for (int i = 0; i < nombresPanelesNecesarios.Length; i++)
        {
            string nombre = nombresPanelesNecesarios[i];

            if (string.IsNullOrWhiteSpace(nombre))
            {
                continue;
            }

            if (YaExistePanelConNombre(nombre))
            {
                continue;
            }

            GameObject encontrado = BuscarGameObjectPorNombreIncluyendoInactivos(nombre);
            if (encontrado != null)
            {
                panelesNecesarios.Add(encontrado);
            }
        }
    }

    private bool YaExistePanelConNombre(string nombre)
    {
        for (int i = 0; i < panelesNecesarios.Count; i++)
        {
            if (panelesNecesarios[i] != null && panelesNecesarios[i].name == nombre)
            {
                return true;
            }
        }

        return false;
    }

    private GameObject BuscarGameObjectPorNombreIncluyendoInactivos(string nombre)
    {
        Transform[] todos = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        for (int i = 0; i < todos.Length; i++)
        {
            if (todos[i] != null && todos[i].name == nombre)
            {
                return todos[i].gameObject;
            }
        }

        return null;
    }
}
