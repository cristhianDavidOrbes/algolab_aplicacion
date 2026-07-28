using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class AlgoLabProjectValidator
{
    private const string MenuPath = "Tools/AlgoLab/Validar proyecto";
    private const string ReportPath = "Logs/AlgoLabProjectAudit.txt";

    private sealed class AuditResult
    {
        public readonly List<string> errors = new List<string>();
        public readonly List<string> warnings = new List<string>();
        public int sceneCount;
        public int prefabCount;
        public int gameObjectCount;
        public int componentCount;
    }

    [MenuItem(MenuPath)]
    public static void ValidateFromMenu()
    {
        AuditResult result = RunAudit();
        WriteReport(result);

        string message = BuildSummary(result);
        if (result.errors.Count > 0)
        {
            Debug.LogError(message);
            EditorUtility.DisplayDialog("Validacion de AlgoLab", message, "Aceptar");
            return;
        }

        Debug.Log(message);
        EditorUtility.DisplayDialog("Validacion de AlgoLab", message, "Aceptar");
    }

    // Entry point for: Unity -batchmode -executeMethod AlgoLabProjectValidator.ValidateBatch
    public static void ValidateBatch()
    {
        AuditResult result = RunAudit();
        WriteReport(result);

        string message = BuildSummary(result);
        if (result.errors.Count > 0)
        {
            throw new BuildFailedException(message);
        }

        Debug.Log(message);
    }

    private static AuditResult RunAudit()
    {
        AuditResult result = new AuditResult();
        SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();

        try
        {
            ValidateBuildScenes(result);
            ValidateAlgoLabPrefabs(result);
        }
        finally
        {
            if (previousSetup != null && previousSetup.Length > 0)
            {
                EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
            }
        }

        return result;
    }

    private static void ValidateBuildScenes(AuditResult result)
    {
        EditorBuildSettingsScene[] buildScenes = EditorBuildSettings.scenes;
        int enabledScenes = 0;

        for (int i = 0; i < buildScenes.Length; i++)
        {
            EditorBuildSettingsScene entry = buildScenes[i];
            if (!entry.enabled)
            {
                continue;
            }

            enabledScenes++;
            if (string.IsNullOrWhiteSpace(entry.path) || !File.Exists(entry.path))
            {
                result.errors.Add("Escena habilitada inexistente: " + entry.path);
                continue;
            }

            Scene scene = EditorSceneManager.OpenScene(entry.path, OpenSceneMode.Single);
            result.sceneCount++;
            ValidateScene(scene, result);
        }

        if (enabledScenes == 0)
        {
            result.errors.Add("No hay escenas habilitadas en Build Settings.");
        }
    }

    private static void ValidateScene(Scene scene, AuditResult result)
    {
        string context = "Escena " + scene.path;
        GameObject[] roots = scene.GetRootGameObjects();
        List<GameObject> allObjects = new List<GameObject>();

        for (int i = 0; i < roots.Length; i++)
        {
            CollectHierarchy(roots[i], allObjects);
        }

        ValidateHierarchy(allObjects, context, result);
        ValidateUniqueComponents<AlgoLabSessionManager>(allObjects, context, result);
        ValidateUniqueComponents<AlgoLabBackendClient>(allObjects, context, result);
        ValidateUniqueComponents<AlgoLabPanelPocketManager>(allObjects, context, result);
        ValidateUniqueComponents<AlgoLabManualPanelSpawnManager>(allObjects, context, result);
        ValidateUniqueComponents<AlgoLabSelectionManager>(allObjects, context, result);
        ValidateUniqueComponents<AlgoLabTutorialPanelController>(allObjects, context, result);
        ValidateUniqueComponents<AlgoLabSettingsMenuController>(allObjects, context, result);
        ValidateSettingsMenu(allObjects, context, result);
        ValidateTutorial(allObjects, context, result);
        ValidateLevelTwoPractice(allObjects, context, result);
        ValidateControllerPointers(allObjects, context, result);
        ValidateVoiceAssistant(allObjects, context, result);
        ValidateProgressLevels(allObjects, context, result);

        int activeEventSystems = CountActiveComponents<EventSystem>(allObjects);
        if (activeEventSystems > 1)
        {
            result.errors.Add(context + " tiene " + activeEventSystems + " EventSystem activos.");
        }

        int activeAudioListeners = CountActiveComponents<AudioListener>(allObjects);
        if (activeAudioListeners > 1)
        {
            result.errors.Add(context + " tiene " + activeAudioListeners + " AudioListener activos.");
        }
    }

    private static void ValidateLevelTwoPractice(
        List<GameObject> allObjects,
        string context,
        AuditResult result)
    {
        List<AlgoLabLevel02PracticeController> practices =
            FindComponents<AlgoLabLevel02PracticeController>(allObjects);

        if (practices.Count != 1)
        {
            result.errors.Add(context + " debe contener exactamente una práctica de nivel 2; tiene " + practices.Count + ".");
            return;
        }

        AlgoLabLevel02PracticeController practice = practices[0];
        string practiceContext = context + " / " + GetObjectPath(practice.transform);

        if (practice.numeroNivelReal != 2)
            result.errors.Add(practiceContext + " tiene numeroNivelReal distinto de 2.");
        if (practice.indiceNivelEnProgressPanel != 1)
            result.errors.Add(practiceContext + " debe apuntar al índice visual 1.");

        RequireReference(practice.modeManager, "modeManager", practiceContext, result);
        RequireReference(practice.progressPanel, "progressPanel", practiceContext, result);
        RequireReference(practice.garageController, "garageController", practiceContext, result);
        RequireReference(practice.tutorialMultimedia, "tutorialMultimedia", practiceContext, result);
        ValidatePositiveFinite(practice.duracionPracticaSegundos, "duracionPracticaSegundos", practiceContext, result);
        if (practice.cantidadVehiculosAleatorios != 5)
            result.errors.Add(practiceContext + " debe generar cinco vehículos aleatorios por intento.");
        if (!practice.garantizarTodosLosEstados)
            result.errors.Add(practiceContext + " no garantiza nuevo, seminuevo y usado en cada intento.");

        if (practice.vehiculosRequeridos == null || practice.vehiculosRequeridos.Count == 0)
        {
            result.errors.Add(practiceContext + " no contiene vehículos requeridos.");
        }
        else
        {
            HashSet<string> signatures = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < practice.vehiculosRequeridos.Count; i++)
            {
                AlgoLabLevel02PracticeController.VehiculoRequerido vehicle = practice.vehiculosRequeridos[i];
                string vehicleContext = practiceContext + " / Vehículo requerido " + i;
                if (vehicle == null)
                {
                    result.errors.Add(vehicleContext + " es nulo.");
                    continue;
                }

                string signature = (vehicle.color + "|" + vehicle.modelo + "|" + vehicle.carcasa + "|" +
                                    vehicle.estado + "|" + vehicle.metodo).Trim();
                if (signature.Replace("|", string.Empty).Length == 0)
                    result.errors.Add(vehicleContext + " no tiene configuración educativa.");
                else if (!signatures.Add(signature))
                    result.warnings.Add(vehicleContext + " repite exactamente otro vehículo requerido.");
            }
        }

        AlgoLabLevel02GarageController garage = practice.garageController as AlgoLabLevel02GarageController;
        if (garage == null)
        {
            result.errors.Add(practiceContext + " referencia un garageController de tipo incorrecto.");
            return;
        }

        string garageContext = context + " / " + GetObjectPath(garage.transform);
        RequireReference(garage.garageRoot, "garageRoot", garageContext, result);
        RequireReference(garage.puertaGaraje, "puertaGaraje", garageContext, result);
        RequireReference(garage.carSpawnPoint, "carSpawnPoint", garageContext, result);
        RequireReference(garage.carExitPoint, "carExitPoint", garageContext, result);
        RequireReference(garage.carsRoot, "carsRoot", garageContext, result);
        RequireReference(garage.prefabVehiculo, "prefabVehiculo", garageContext, result);

        if (garage.prefabVehiculo != null)
        {
            GameObject prefab = garage.prefabVehiculo;
            if (prefab.GetComponent<Rigidbody>() == null)
                result.errors.Add(garageContext + " / prefabVehiculo no tiene Rigidbody.");
            if (prefab.GetComponentInChildren<Collider>(true) == null)
                result.errors.Add(garageContext + " / prefabVehiculo no tiene Collider.");
            SimpleMRGrabbable grabbable = prefab.GetComponent<SimpleMRGrabbable>();
            if (grabbable == null)
                result.errors.Add(garageContext + " / prefabVehiculo no tiene SimpleMRGrabbable.");
            else
            {
                if (grabbable.releaseMode != SimpleMRGrabbable.ReleaseMode.Physics ||
                    !grabbable.useGravityOnRelease)
                    result.errors.Add(garageContext + " / prefabVehiculo no usa liberación física con gravedad.");
                if (!grabbable.conservarImpulsoAlSoltar ||
                    grabbable.velocidadLinealMaximaAlSoltar <= 0f)
                    result.errors.Add(garageContext + " / prefabVehiculo no conserva el impulso del lanzamiento.");
            }

            AlgoLabLevel02VehicleObject vehicleObject =
                prefab.GetComponent<AlgoLabLevel02VehicleObject>();
            if (vehicleObject == null)
                result.errors.Add(garageContext + " / prefabVehiculo no tiene AlgoLabLevel02VehicleObject.");
            else
            {
                if (vehicleObject.impactoMaximoSeminuevo > 6f ||
                    vehicleObject.impactoMaximoUsado > 3f ||
                    vehicleObject.impactoMaximoUsado >= vehicleObject.impactoMaximoSeminuevo)
                    result.errors.Add(garageContext + " / fragilidad por impacto de seminuevo/usado es incorrecta.");
                if (vehicleObject.alturaMinimaCaidaSeminuevo > 0.65f ||
                    vehicleObject.alturaMinimaCaidaUsado > 0.3f ||
                    vehicleObject.alturaMinimaCaidaUsado >= vehicleObject.alturaMinimaCaidaSeminuevo)
                    result.errors.Add(garageContext + " / fragilidad por caída de seminuevo/usado es incorrecta.");
            }
        }

        List<AlgoLabVehicleRoomCommandController> commands =
            FindComponents<AlgoLabVehicleRoomCommandController>(allObjects);
        int leftControllers = 0;
        int rightControllers = 0;

        for (int i = 0; i < commands.Count; i++)
        {
            AlgoLabVehicleRoomCommandController command = commands[i];
            string commandContext = context + " / " + GetObjectPath(command.transform);
            RequireReference(command.rayOrigin, "rayOrigin", commandContext, result);
            ValidatePositiveFinite(command.maxDistance, "maxDistance", commandContext, result);
            ValidatePositiveFinite(command.intervaloActualizarDestino, "intervaloActualizarDestino", commandContext, result);
            if (command.triggerThreshold < 0.2f || command.triggerThreshold > 0.75f)
                result.errors.Add(commandContext + " tiene un umbral inválido para el gatillo índice principal.");

            bool hostedByHand = command.GetComponent<OVRHand>() != null;
            if (hostedByHand)
            {
                if (!command.desactivarEnTrackingDeManos)
                    result.errors.Add(commandContext + " duplicará comandos porque está en OVRHand y no se desactiva.");
                continue;
            }

            if (!command.gameObject.activeInHierarchy || !command.enabled)
                continue;

            if (command.handSide == AlgoLabVehicleRoomCommandController.HandSide.Left)
                leftControllers++;
            else
                rightControllers++;
        }

        if (leftControllers != 1 || rightControllers != 1)
        {
            result.errors.Add(
                context + " debe tener un comando de vehículo físico por mando. Izquierdo=" +
                leftControllers + ", derecho=" + rightControllers + "."
            );
        }
    }

    private static void ValidateControllerPointers(
        List<GameObject> allObjects,
        string context,
        AuditResult result)
    {
        List<PointerSelector> pointers = FindComponents<PointerSelector>(allObjects);
        int left = 0;
        int right = 0;

        for (int i = 0; i < pointers.Count; i++)
        {
            PointerSelector pointer = pointers[i];
            if (pointer.tipoEntrada != PointerSelector.TipoEntrada.Controlador ||
                !pointer.gameObject.activeInHierarchy || !pointer.enabled)
                continue;

            string pointerContext = context + " / " + GetObjectPath(pointer.transform);
            RequireReference(pointer.rayOrigin, "rayOrigin", pointerContext, result);
            if (pointer.capasSeleccionables.value == 0)
                result.errors.Add(pointerContext + " no tiene capas seleccionables.");

            if (pointer.controladorOVR == OVRInput.Controller.LTouch) left++;
            if (pointer.controladorOVR == OVRInput.Controller.RTouch) right++;
        }

        if (left == 0 || right == 0)
            result.errors.Add(context + " no tiene punteros activos para ambos mandos. Izquierdo=" + left + ", derecho=" + right + ".");
    }

    private static void ValidateVoiceAssistant(
        List<GameObject> allObjects,
        string context,
        AuditResult result)
    {
        List<AlgoLabVoiceAssistant> assistants = FindComponents<AlgoLabVoiceAssistant>(allObjects);
        if (assistants.Count != 1)
        {
            result.errors.Add(context + " debe contener exactamente un asistente de voz; tiene " + assistants.Count + ".");
            return;
        }

        AlgoLabVoiceAssistant assistant = assistants[0];
        string voiceContext = context + " / " + GetObjectPath(assistant.transform);
        RequireReference(assistant.iaClient, "iaClient", voiceContext, result);
        RequireReference(assistant.panelRevisionIA, "panelRevisionIA", voiceContext, result);
        RequireReference(assistant.subtitulosIA, "subtitulosIA", voiceContext, result);
        RequireReference(assistant.textToSpeech, "textToSpeech", voiceContext, result);

        if (assistant.tiempoMinimoGrabacion <= 0f ||
            assistant.tiempoMaximoGrabacion <= assistant.tiempoMinimoGrabacion)
            result.errors.Add(voiceContext + " tiene límites de grabación inválidos.");
        ValidatePositiveFinite(assistant.tiempoMaximoRespuestaIA, "tiempoMaximoRespuestaIA", voiceContext, result);

        AlgoLabSpeechToTextClient stt = assistant.speechToTextLocal != null
            ? assistant.speechToTextLocal
            : assistant.GetComponent<AlgoLabSpeechToTextClient>();
        RequireReference(stt, "speechToTextLocal", voiceContext, result);

        if (assistant.iaClient != null && stt != null)
        {
            if (!TryGetHttpsAuthority(assistant.iaClient.iaApiUrl, out string iaAuthority))
                result.errors.Add(voiceContext + " tiene una URL de IA inválida o no HTTPS.");
            if (!TryGetHttpsAuthority(stt.apiUrl, out string sttAuthority))
                result.errors.Add(voiceContext + " tiene una URL de STT inválida o no HTTPS.");
            if (!string.IsNullOrEmpty(iaAuthority) && !string.IsNullOrEmpty(sttAuthority) &&
                !string.Equals(iaAuthority, sttAuthority, StringComparison.OrdinalIgnoreCase))
                result.errors.Add(voiceContext + " usa dominios distintos para IA y STT.");
        }
    }

    private static bool TryGetHttpsAuthority(string value, out string authority)
    {
        authority = string.Empty;
        if (!Uri.TryCreate(value, UriKind.Absolute, out Uri uri) || uri.Scheme != Uri.UriSchemeHttps)
            return false;
        authority = uri.Authority;
        return !string.IsNullOrWhiteSpace(authority);
    }

    private static void ValidateProgressLevels(
        List<GameObject> allObjects,
        string context,
        AuditResult result)
    {
        List<AlgoLabProgressPanel> panels = FindComponents<AlgoLabProgressPanel>(allObjects);
        if (panels.Count == 0)
            return;

        AlgoLabProgressPanel panel = panels[0];
        string panelContext = context + " / " + GetObjectPath(panel.transform);
        if (panel.levels == null || panel.levels.Length < 6)
        {
            result.errors.Add(panelContext + " debe contener los seis niveles del recorrido.");
            return;
        }

        HashSet<string> names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < panel.levels.Length; i++)
        {
            AlgoLabProgressPanel.LevelVisual level = panel.levels[i];
            string levelContext = panelContext + " / Nivel " + (i + 1);
            if (level == null)
            {
                result.errors.Add(levelContext + " es nulo.");
                continue;
            }

            RequireReference(level.levelObject, "levelObject", levelContext, result);
            AlgoLabProgressLevelInfo info = level.levelInfo != null
                ? level.levelInfo
                : level.levelObject != null
                    ? level.levelObject.GetComponent<AlgoLabProgressLevelInfo>()
                    : null;
            RequireReference(info, "levelInfo", levelContext, result);

            if (info != null)
            {
                if (string.IsNullOrWhiteSpace(info.nombreNivel))
                    result.errors.Add(levelContext + " no tiene nombre.");
                else if (!names.Add(info.nombreNivel.Trim()))
                    result.warnings.Add(levelContext + " repite el nombre '" + info.nombreNivel + "'.");
            }
        }

        RequireReference(panel.leftRayOrigin, "leftRayOrigin", panelContext, result);
        RequireReference(panel.rightRayOrigin, "rightRayOrigin", panelContext, result);
    }

    private static void ValidateSettingsMenu(
        List<GameObject> allObjects,
        string context,
        AuditResult result)
    {
        List<AlgoLabSettingsMenuController> controllers =
            FindComponents<AlgoLabSettingsMenuController>(allObjects);

        if (controllers.Count == 0)
        {
            result.errors.Add(context + " no contiene el menu de configuracion serializado.");
            return;
        }

        AlgoLabSettingsMenuController controller = controllers[0];
        GameObject root = controller.gameObject;

        if (root.name != "[ALGOLAB_SETTINGS_MENU]")
        {
            result.errors.Add(context + " tiene el menu de configuracion con nombre incorrecto: " + root.name + ".");
        }

        if (root.transform.parent != null)
        {
            result.errors.Add(context + " / [ALGOLAB_SETTINGS_MENU] debe ser un objeto raiz de la escena.");
        }

        if (!root.activeSelf || !controller.enabled)
        {
            result.errors.Add(context + " / [ALGOLAB_SETTINGS_MENU] esta desactivado en la escena.");
        }

        Transform canvasTransform = FindDescendant(root.transform, "AlgoLabSettingsCanvas");
        if (canvasTransform == null)
        {
            result.errors.Add(context + " / [ALGOLAB_SETTINGS_MENU] no contiene AlgoLabSettingsCanvas.");
            return;
        }

        Canvas settingsCanvas = canvasTransform.GetComponent<Canvas>();
        GraphicRaycaster raycaster = canvasTransform.GetComponent<GraphicRaycaster>();
        BoxCollider menuCollider = canvasTransform.GetComponent<BoxCollider>();
        RectTransform canvasRect = canvasTransform as RectTransform;

        if (!canvasTransform.gameObject.activeSelf)
        {
            result.errors.Add(context + " / AlgoLabSettingsCanvas no es visible en la escena.");
        }

        if (settingsCanvas == null || !settingsCanvas.enabled)
        {
            result.errors.Add(context + " / AlgoLabSettingsCanvas no tiene un Canvas activo.");
        }
        else if (settingsCanvas.renderMode != RenderMode.WorldSpace)
        {
            result.errors.Add(context + " / AlgoLabSettingsCanvas debe usar World Space.");
        }

        if (raycaster == null || !raycaster.enabled)
        {
            result.errors.Add(context + " / AlgoLabSettingsCanvas no tiene GraphicRaycaster activo.");
        }

        if (menuCollider == null || !menuCollider.enabled)
        {
            result.errors.Add(context + " / AlgoLabSettingsCanvas no tiene BoxCollider activo para detener el rayo.");
        }
        else if (menuCollider.isTrigger)
        {
            result.errors.Add(context + " / el BoxCollider del menu no debe ser trigger.");
        }

        if (canvasRect == null || canvasRect.sizeDelta.x <= 0f || canvasRect.sizeDelta.y <= 0f)
        {
            result.errors.Add(context + " / AlgoLabSettingsCanvas tiene dimensiones invalidas.");
        }
        else
        {
            float anchoMundo = canvasRect.sizeDelta.x * Mathf.Abs(canvasRect.lossyScale.x);
            float altoMundo = canvasRect.sizeDelta.y * Mathf.Abs(canvasRect.lossyScale.y);
            if (anchoMundo > 1.25f || altoMundo > 0.85f)
            {
                result.errors.Add(
                    context + " / AlgoLabSettingsCanvas sigue demasiado grande en mundo: " +
                    anchoMundo.ToString("0.00") + " x " + altoMundo.ToString("0.00") + " m."
                );
            }
        }

        Transform guias = FindDescendant(root.transform, "[GUIAS_ALTURA_PANELES]");
        if (guias == null || guias.GetComponent<AlgoLabHeightGuideRings>() == null)
        {
            result.errors.Add(context + " / faltan las guias de altura sentado/de pie del menu.");
        }

        string[] requiredViews =
        {
            "VistaPaneles",
            "VistaSonido",
            "VistaGraficos",
            "VistaSesion",
            "VistaRanking"
        };

        int activeViews = 0;
        for (int i = 0; i < requiredViews.Length; i++)
        {
            Transform view = FindDescendant(canvasTransform, requiredViews[i]);
            if (view == null)
            {
                result.errors.Add(context + " / menu de configuracion no contiene " + requiredViews[i] + ".");
                continue;
            }

            if (view.gameObject.activeSelf)
            {
                activeViews++;
            }
        }

        if (activeViews != 1)
        {
            result.errors.Add(
                context + " / menu de configuracion debe tener exactamente una vista previa activa; tiene " +
                activeViews + "."
            );
        }
    }

    private static void ValidateTutorial(
        List<GameObject> allObjects,
        string context,
        AuditResult result)
    {
        List<AlgoLabTutorialPanelController> controllers =
            FindComponents<AlgoLabTutorialPanelController>(allObjects);

        if (controllers.Count == 0)
        {
            result.errors.Add(context + " no contiene AlgoLabTutorialPanelController.");
            return;
        }

        AlgoLabTutorialPanelController tutorial = controllers[0];
        string tutorialContext = context + " / " + GetObjectPath(tutorial.transform);

        RequireReference(tutorial.panelRoot, "panelRoot", tutorialContext, result);
        RequireReference(tutorial.tutorialMainPanel, "tutorialMainPanel", tutorialContext, result);
        RequireReference(tutorial.audioSource, "audioSource", tutorialContext, result);
        RequireReference(tutorial.videoRawImage, "videoRawImage", tutorialContext, result);
        RequireReference(tutorial.tutorialVideoPlayer, "tutorialVideoPlayer", tutorialContext, result);
        RequireReference(tutorial.tutorialRenderTexture, "tutorialRenderTexture", tutorialContext, result);
        RequireReference(tutorial.mandoController, "mandoController", tutorialContext, result);
        RequireReference(tutorial.rootParaUbicar, "rootParaUbicar", tutorialContext, result);
        RequireReference(tutorial.grabHandleTutorialPocket, "grabHandleTutorialPocket", tutorialContext, result);
        RequireReference(tutorial.puntoMiradaContraido, "puntoMiradaContraido", tutorialContext, result);

        AlgoLabPanelGrabHandle tutorialGrab = tutorial.grabHandleTutorialPocket;
        if (tutorialGrab != null)
        {
            if (tutorial.rootParaUbicar != null && tutorialGrab.panelRoot != tutorial.rootParaUbicar)
            {
                result.errors.Add(tutorialContext + " mueve un panelRoot distinto de rootParaUbicar.");
            }

            if (!tutorialGrab.usarPuntoExactoDeAgarre)
            {
                result.errors.Add(tutorialContext + " no usa el agarre preciso comun de los paneles.");
            }

            if (tutorialGrab.usarMovimientoAncladoTutorial ||
                tutorialGrab.reanclarTutorialDespuesDeMirada)
            {
                result.errors.Add(tutorialContext + " conserva activa la ruta antigua de doble ancla.");
            }
        }

        List<AlgoLabSpawnedHeightAutoRegister> ajustesAltura =
            FindComponents<AlgoLabSpawnedHeightAutoRegister>(allObjects);

        for (int i = 0; i < ajustesAltura.Count; i++)
        {
            AlgoLabSpawnedHeightAutoRegister ajuste = ajustesAltura[i];
            AlgoLabTutorialPanelController propietario =
                ajuste.GetComponentInParent<AlgoLabTutorialPanelController>(true);

            if (propietario == tutorial &&
                tutorial.rootParaUbicar != null &&
                ajuste.objetoRoot != tutorial.rootParaUbicar)
            {
                result.errors.Add(tutorialContext + " ajusta la altura de un hijo distinto del root real.");
            }
        }

        if (!tutorial.usarProteccionAntiBloqueoTutorial)
        {
            result.errors.Add(tutorialContext + " tiene desactivada la proteccion anti bloqueo.");
        }

        if (!tutorial.detenerVideoAlMostrarImagenPanel)
        {
            result.errors.Add(tutorialContext + " no detiene el video antes de mostrar imagenes de panel.");
        }

        if (!tutorial.reiniciarTutorialSiempreAlSacarDelPanelOpciones)
        {
            result.errors.Add(tutorialContext + " no reinicia al salir del panel de opciones.");
        }

        if (tutorial.usarRootComoPuntoMiradaEstableSiempre)
        {
            result.errors.Add(tutorialContext + " fuerza el root y anula los pivotes contraido/expandido.");
        }

        if (!tutorial.usarPuntosMiradaMientrasAgarrado)
        {
            result.errors.Add(tutorialContext + " no conserva el pivote visual correcto durante el agarre.");
        }

        ValidatePositiveFinite(tutorial.tiempoMaximoPrepararVideo, "tiempoMaximoPrepararVideo", tutorialContext, result);
        ValidatePositiveFinite(tutorial.tiempoMaximoEsperaFinVideo, "tiempoMaximoEsperaFinVideo", tutorialContext, result);
        ValidatePositiveFinite(tutorial.tiempoMaximoEsperarInicioVideo, "tiempoMaximoEsperarInicioVideo", tutorialContext, result);

        if (tutorial.completarAccionInteractivaSiExcedeTiempo)
        {
            ValidatePositiveFinite(
                tutorial.tiempoMaximoEsperaAccionInteractiva,
                "tiempoMaximoEsperaAccionInteractiva",
                tutorialContext,
                result
            );
        }

        if (tutorial.eventos == null || tutorial.eventos.Count == 0)
        {
            result.errors.Add(tutorialContext + " no contiene eventos de tutorial.");
            return;
        }

        HashSet<string> timelineKeys = new HashSet<string>();
        bool hasTerminalEvent = false;
        List<AlgoLabTutorialPanelController.EventoTutorial> sortedEvents =
            new List<AlgoLabTutorialPanelController.EventoTutorial>();

        for (int i = 0; i < tutorial.eventos.Count; i++)
        {
            AlgoLabTutorialPanelController.EventoTutorial tutorialEvent = tutorial.eventos[i];
            string eventContext = tutorialContext + " / Evento " + i;

            if (tutorialEvent == null)
            {
                result.errors.Add(eventContext + " es nulo.");
                continue;
            }

            sortedEvents.Add(tutorialEvent);

            if (!IsFinite(tutorialEvent.tiempo) || tutorialEvent.tiempo < 0f)
            {
                result.errors.Add(eventContext + " tiene un tiempo invalido.");
            }

            if (!IsFinite(tutorialEvent.duracionReproduccionVideo) ||
                tutorialEvent.duracionReproduccionVideo < 0f)
            {
                result.errors.Add(eventContext + " tiene una duracion de video invalida.");
            }

            string key = tutorialEvent.tiempo.ToString("R") + ":" + tutorialEvent.orden;
            if (!timelineKeys.Add(key))
            {
                result.errors.Add(eventContext + " repite tiempo y orden " + key + ".");
            }

            switch (tutorialEvent.tipoEvento)
            {
                case AlgoLabTutorialPanelController.TipoEventoTutorial.ReproducirAudioClip:
                    RequireReference(tutorialEvent.audioClip, "audioClip", eventContext, result);
                    break;

                case AlgoLabTutorialPanelController.TipoEventoTutorial.ReproducirVideoClip:
                    RequireReference(tutorialEvent.videoClip, "videoClip", eventContext, result);
                    break;

                case AlgoLabTutorialPanelController.TipoEventoTutorial.CambiarImagen:
                    RequireReference(tutorialEvent.imagen, "imagen", eventContext, result);
                    break;

                case AlgoLabTutorialPanelController.TipoEventoTutorial.ActivarObjeto:
                case AlgoLabTutorialPanelController.TipoEventoTutorial.DesactivarObjeto:
                    RequireReference(tutorialEvent.objeto, "objeto", eventContext, result);
                    break;

                case AlgoLabTutorialPanelController.TipoEventoTutorial.EsperarAccionInteractiva:
                    if (tutorialEvent.accionEsperada == AlgoLabTutorialPanelController.AccionTutorialInteractiva.Ninguna)
                    {
                        result.errors.Add(eventContext + " espera una accion interactiva sin accionEsperada.");
                    }
                    break;

                case AlgoLabTutorialPanelController.TipoEventoTutorial.CerrarPanel:
                case AlgoLabTutorialPanelController.TipoEventoTutorial.FinalizarTutorial:
                case AlgoLabTutorialPanelController.TipoEventoTutorial.ContinuarAplicacion:
                    hasTerminalEvent = true;
                    break;
            }
        }

        if (!hasTerminalEvent)
        {
            result.errors.Add(tutorialContext + " no contiene un evento terminal de cierre o finalizacion.");
        }

        if (tutorial.reemplazarAudioElemento2PorAudio21AlVolverDesdePanelOpciones)
        {
            RequireReference(
                tutorial.audio21VolverIntentarlo,
                "audio21VolverIntentarlo",
                tutorialContext,
                result
            );

            sortedEvents.Sort((a, b) =>
            {
                int byTime = a.tiempo.CompareTo(b.tiempo);
                return byTime != 0 ? byTime : a.orden.CompareTo(b.orden);
            });

            int index = tutorial.indiceEventoAudioReemplazablePanelOpciones;
            if (index < 0 || index >= sortedEvents.Count)
            {
                result.errors.Add(tutorialContext + " tiene indice de audio reemplazable fuera de rango.");
            }
            else if (sortedEvents[index].tipoEvento !=
                     AlgoLabTutorialPanelController.TipoEventoTutorial.ReproducirAudioClip)
            {
                result.errors.Add(tutorialContext + " apunta el reemplazo de audio 21 a un evento que no reproduce audio.");
            }
        }
    }

    private static void ValidateAlgoLabPrefabs(AuditResult result)
    {
        string[] prefabGuids = AssetDatabase.FindAssets(
            "t:Prefab",
            new[] { "Assets/__Algolab/Prefabs" }
        );

        for (int i = 0; i < prefabGuids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
            GameObject root = null;

            try
            {
                root = PrefabUtility.LoadPrefabContents(path);
                result.prefabCount++;

                List<GameObject> allObjects = new List<GameObject>();
                CollectHierarchy(root, allObjects);
                ValidateHierarchy(allObjects, "Prefab " + path, result);
            }
            catch (Exception exception)
            {
                result.errors.Add("No se pudo validar el prefab " + path + ": " + exception.Message);
            }
            finally
            {
                if (root != null)
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }
        }
    }

    private static void ValidateHierarchy(
        List<GameObject> allObjects,
        string context,
        AuditResult result)
    {
        result.gameObjectCount += allObjects.Count;

        for (int i = 0; i < allObjects.Count; i++)
        {
            GameObject gameObject = allObjects[i];
            string objectPath = GetObjectPath(gameObject.transform);

            int missingScripts = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(gameObject);
            if (missingScripts > 0)
            {
                result.errors.Add(
                    context + " / " + objectPath + " tiene " + missingScripts + " script(s) perdido(s)."
                );
            }

            ValidateFiniteTransform(gameObject.transform, context, objectPath, result);

            Component[] components = gameObject.GetComponents<Component>();
            result.componentCount += components.Length;
            for (int componentIndex = 0; componentIndex < components.Length; componentIndex++)
            {
                Component component = components[componentIndex];
                if (component == null)
                {
                    continue;
                }

                ValidateBrokenObjectReferences(component, context, objectPath, result);
            }

            SimpleMRGrabbable grabbable = gameObject.GetComponent<SimpleMRGrabbable>();
            if (grabbable != null)
            {
                if (gameObject.GetComponent<Rigidbody>() == null)
                {
                    result.errors.Add(context + " / " + objectPath + " no tiene Rigidbody para SimpleMRGrabbable.");
                }

                if (gameObject.GetComponent<Collider>() == null)
                {
                    result.errors.Add(context + " / " + objectPath + " no tiene Collider para SimpleMRGrabbable.");
                }
            }
        }
    }

    private static void ValidateBrokenObjectReferences(
        Component component,
        string context,
        string objectPath,
        AuditResult result)
    {
        SerializedObject serializedObject;
        try
        {
            serializedObject = new SerializedObject(component);
        }
        catch
        {
            return;
        }

        SerializedProperty property = serializedObject.GetIterator();
        bool enterChildren = true;

        while (property.NextVisible(enterChildren))
        {
            enterChildren = false;
            if (property.propertyType != SerializedPropertyType.ObjectReference)
            {
                continue;
            }

            if (property.objectReferenceValue == null && property.objectReferenceInstanceIDValue != 0)
            {
                result.errors.Add(
                    context + " / " + objectPath + " / " + component.GetType().Name +
                    " tiene referencia perdida en " + property.propertyPath + "."
                );
            }
        }
    }

    private static void ValidateFiniteTransform(
        Transform transform,
        string context,
        string objectPath,
        AuditResult result)
    {
        if (!IsFinite(transform.localPosition) ||
            !IsFinite(transform.localScale) ||
            !IsFinite(transform.localRotation))
        {
            result.errors.Add(context + " / " + objectPath + " tiene una transformacion NaN o infinita.");
        }

        Vector3 scale = transform.localScale;
        if (Mathf.Abs(scale.x) < 0.000001f ||
            Mathf.Abs(scale.y) < 0.000001f ||
            Mathf.Abs(scale.z) < 0.000001f)
        {
            result.warnings.Add(context + " / " + objectPath + " tiene escala cero en algun eje.");
        }
    }

    private static void ValidateUniqueComponents<T>(
        List<GameObject> allObjects,
        string context,
        AuditResult result) where T : Component
    {
        int count = 0;
        for (int i = 0; i < allObjects.Count; i++)
        {
            count += allObjects[i].GetComponents<T>().Length;
        }

        if (count > 1)
        {
            result.errors.Add(context + " tiene " + count + " componentes " + typeof(T).Name + ".");
        }
    }

    private static int CountActiveComponents<T>(List<GameObject> allObjects) where T : Component
    {
        int count = 0;
        for (int i = 0; i < allObjects.Count; i++)
        {
            if (!allObjects[i].activeInHierarchy)
            {
                continue;
            }

            T[] components = allObjects[i].GetComponents<T>();
            for (int componentIndex = 0; componentIndex < components.Length; componentIndex++)
            {
                Behaviour behaviour = components[componentIndex] as Behaviour;
                if (behaviour == null || behaviour.enabled)
                {
                    count++;
                }
            }
        }

        return count;
    }

    private static List<T> FindComponents<T>(List<GameObject> allObjects) where T : Component
    {
        List<T> result = new List<T>();

        for (int i = 0; i < allObjects.Count; i++)
        {
            T[] components = allObjects[i].GetComponents<T>();
            for (int componentIndex = 0; componentIndex < components.Length; componentIndex++)
            {
                result.Add(components[componentIndex]);
            }
        }

        return result;
    }

    private static Transform FindDescendant(Transform root, string objectName)
    {
        if (root == null)
        {
            return null;
        }

        if (root.name == objectName)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform match = FindDescendant(root.GetChild(i), objectName);
            if (match != null)
            {
                return match;
            }
        }

        return null;
    }

    private static void RequireReference(
        UnityEngine.Object value,
        string fieldName,
        string context,
        AuditResult result)
    {
        if (value == null)
        {
            result.errors.Add(context + " no tiene asignado " + fieldName + ".");
        }
    }

    private static void ValidatePositiveFinite(
        float value,
        string fieldName,
        string context,
        AuditResult result)
    {
        if (!IsFinite(value) || value <= 0f)
        {
            result.errors.Add(context + " tiene " + fieldName + " invalido; debe ser mayor que cero.");
        }
    }

    private static void CollectHierarchy(GameObject root, List<GameObject> destination)
    {
        destination.Add(root);
        Transform transform = root.transform;
        for (int i = 0; i < transform.childCount; i++)
        {
            CollectHierarchy(transform.GetChild(i).gameObject, destination);
        }
    }

    private static string GetObjectPath(Transform transform)
    {
        StringBuilder path = new StringBuilder(transform.name);
        Transform parent = transform.parent;
        while (parent != null)
        {
            path.Insert(0, parent.name + "/");
            parent = parent.parent;
        }

        return path.ToString();
    }

    private static bool IsFinite(Vector3 value)
    {
        return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
    }

    private static bool IsFinite(Quaternion value)
    {
        return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z) && IsFinite(value.w);
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }

    private static void WriteReport(AuditResult result)
    {
        StringBuilder report = new StringBuilder();
        report.AppendLine("AlgoLab project audit");
        report.AppendLine("Generated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        report.AppendLine(BuildSummary(result));
        report.AppendLine();
        report.AppendLine("ERRORS");

        if (result.errors.Count == 0)
        {
            report.AppendLine("None");
        }
        else
        {
            for (int i = 0; i < result.errors.Count; i++)
            {
                report.AppendLine("- " + result.errors[i]);
            }
        }

        report.AppendLine();
        report.AppendLine("WARNINGS");
        if (result.warnings.Count == 0)
        {
            report.AppendLine("None");
        }
        else
        {
            for (int i = 0; i < result.warnings.Count; i++)
            {
                report.AppendLine("- " + result.warnings[i]);
            }
        }

        Directory.CreateDirectory(Path.GetDirectoryName(ReportPath) ?? "Logs");
        File.WriteAllText(ReportPath, report.ToString(), new UTF8Encoding(false));
    }

    private static string BuildSummary(AuditResult result)
    {
        return "Escenas: " + result.sceneCount +
               " | Prefabs: " + result.prefabCount +
               " | Objetos: " + result.gameObjectCount +
               " | Componentes: " + result.componentCount +
               " | Errores: " + result.errors.Count +
               " | Advertencias: " + result.warnings.Count +
               "\nReporte: " + ReportPath;
    }
}
