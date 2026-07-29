using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DoorScript;

public static class AlgoLabLogicSmokeTests
{
    private const string ScenePath = "Assets/Scenes/version_estable14.unity";
    private const string ReportPath = "Logs/AlgoLabLogicSmokeTests.txt";

    [MenuItem("Tools/AlgoLab/Pruebas rápidas de lógica")]
    public static void RunFromMenu()
    {
        Run(false);
    }

    public static void RunFromCommandLine()
    {
        Run(true);
    }

    private static void Run(bool throwOnFailure)
    {
        var failures = new List<string>();
        var checks = new List<string>();

        try
        {
            TestLevel02VehicleDestination(checks, failures);
            TestRandomVehicleRequirements(checks, failures);
            TestDoorStateBeforeStart(checks, failures);
            TestLevel1DoorDiagramData(checks, failures);
            TestPracticeTutorials(checks, failures);
            TestLevel3RobotPractice(checks, failures);
            TestEditableAuthoringContent(checks, failures);
        }
        catch (Exception exception)
        {
            failures.Add("Excepción no controlada: " + exception);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(ReportPath) ?? ".");
        string report =
            "ALGOLAB LOGIC SMOKE TESTS\n" +
            "Comprobaciones: " + checks.Count + "\n" +
            "Fallos: " + failures.Count + "\n\n" +
            string.Join("\n", checks) +
            (failures.Count > 0
                ? "\n\nFALLOS\n" + string.Join("\n", failures)
                : "\n\nRESULTADO: OK");

        File.WriteAllText(ReportPath, report);
        AssetDatabase.Refresh();

        if (failures.Count > 0)
        {
            Debug.LogError(report);
            if (throwOnFailure)
            {
                throw new InvalidOperationException(
                    "Fallaron " + failures.Count + " pruebas rápidas de lógica."
                );
            }
        }
        else
        {
            Debug.Log(report);
        }
    }

    private static void TestLevel02VehicleDestination(
        List<string> checks,
        List<string> failures)
    {
        GameObject prefab = FindVehiclePrefab();
        if (prefab == null)
        {
            failures.Add("Nivel 2: no se encontró el prefab de vehículo configurado.");
            return;
        }

        Scene testScene = EditorSceneManager.NewScene(
            NewSceneSetup.EmptyScene,
            NewSceneMode.Additive
        );
        testScene.name = "AlgoLab_Level02_LogicTest";

        SimulationMode previousSimulationMode = Physics.simulationMode;
        Physics.simulationMode = SimulationMode.Script;

        try
        {
            PhysicsScene physicsScene = testScene.GetPhysicsScene();
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "LogicTestGround";
            ground.transform.position = new Vector3(0f, -0.1f, 1f);
            ground.transform.localScale = new Vector3(12f, 0.2f, 12f);
            SceneManager.MoveGameObjectToScene(ground, testScene);

            GameObject instance = UnityEngine.Object.Instantiate(prefab);
            instance.name = "LogicTestVehicle";
            SceneManager.MoveGameObjectToScene(instance, testScene);
            instance.SetActive(true);

            AlgoLabLevel02VehicleObject vehicle =
                instance.GetComponent<AlgoLabLevel02VehicleObject>();
            SimpleMRGrabbable grabbable = instance.GetComponent<SimpleMRGrabbable>();
            Rigidbody body = instance.GetComponent<Rigidbody>();

            Require(vehicle != null, "Nivel 2: el prefab no tiene AlgoLabLevel02VehicleObject.", failures);
            Require(grabbable != null, "Nivel 2: el prefab no tiene SimpleMRGrabbable.", failures);
            Require(body != null, "Nivel 2: el prefab no tiene Rigidbody.", failures);
            if (vehicle == null || grabbable == null || body == null)
            {
                return;
            }

            vehicle.mostrarDebug = false;
            vehicle.usarParticulasRuedas = false;
            vehicle.capasSuelo = ~0;
            vehicle.velocidadDestino = Mathf.Max(1.1f, vehicle.velocidadDestino);
            vehicle.distanciaLlegadaDestino = 0.12f;

            instance.transform.SetPositionAndRotation(
                Vector3.zero,
                Quaternion.identity
            );
            Physics.SyncTransforms();

            if (TryGetCombinedBounds(instance, out Bounds initialBounds))
            {
                instance.transform.position +=
                    Vector3.up * (0.015f - initialBounds.min.y);
            }
            else
            {
                instance.transform.position = new Vector3(0f, 0.15f, 0f);
            }
            Physics.SyncTransforms();

            vehicle.Configurar(
                null,
                AlgoLabLevel02GarageController.EstadoVehiculo.Nuevo,
                Color.red,
                "encender()",
                null,
                true,
                true,
                true
            );

            Vector3 start = body.position;
            Vector3 destination = start + Vector3.forward * 2f;
            vehicle.OrdenarMoverADestino(destination);

            Require(
                (body.constraints & RigidbodyConstraints.FreezePosition) == 0,
                "Nivel 2: la orden programática no liberó la posición del Rigidbody.",
                failures
            );

            MethodInfo fixedUpdate = typeof(AlgoLabLevel02VehicleObject).GetMethod(
                "FixedUpdate",
                BindingFlags.Instance | BindingFlags.NonPublic
            );
            Require(fixedUpdate != null, "Nivel 2: no se encontró FixedUpdate del vehículo.", failures);
            if (fixedUpdate == null)
            {
                return;
            }

            for (int i = 0; i < 70; i++)
            {
                fixedUpdate.Invoke(vehicle, null);
                physicsScene.Simulate(Time.fixedDeltaTime);
            }

            float displacement = Vector3.Distance(
                new Vector3(start.x, 0f, start.z),
                new Vector3(body.position.x, 0f, body.position.z)
            );
            bool touchingGround = InvokePrivateBool(vehicle, "EstaTocandoSuelo");
            bool upright = InvokePrivateBool(vehicle, "EstaDerecho");
            bool destinationActive = ReadPrivateBool(vehicle, "movimientoPorDestinoActivo");
            string groundDiagnostic = BuildGroundDiagnostic(vehicle);
            Require(
                displacement >= 0.75f,
                "Nivel 2: el vehículo recibió destino pero solo avanzó " +
                displacement.ToString("F3") + " m. " +
                "Posición=" + body.position +
                ", tocaSuelo=" + touchingGround +
                ", derecho=" + upright +
                ", destinoActivo=" + destinationActive +
                ", restricciones=" + body.constraints +
                ", cinemático=" + body.isKinematic +
                ". " + groundDiagnostic,
                failures
            );
            if (displacement >= 0.75f)
            {
                checks.Add("OK Nivel 2: vehículo sin agarre previo avanzó " +
                           displacement.ToString("F3") + " m hacia el destino.");
            }

            TestThrowImpulse(vehicle, grabbable, body, checks, failures);
            TestControllerGrabRelease(testScene, grabbable, body, checks, failures);
            TestVehicleFragility(vehicle, checks, failures);
            TestRoomRayForBothHands(testScene, vehicle, checks, failures);
        }
        finally
        {
            Physics.simulationMode = previousSimulationMode;
            EditorSceneManager.CloseScene(testScene, true);
        }
    }

    private static void TestThrowImpulse(
        AlgoLabLevel02VehicleObject vehicle,
        SimpleMRGrabbable grabbable,
        Rigidbody body,
        List<string> checks,
        List<string> failures)
    {
        grabbable.BeginGrab();
        SetPrivateField(grabbable, "velocidadLinealMuestreada", new Vector3(2.4f, 1.1f, 0.6f));
        SetPrivateField(grabbable, "velocidadAngularMuestreada", new Vector3(0f, 3f, 0f));
        SetPrivateField(grabbable, "muestreoLanzamientoValido", true);
        SetPrivateField(grabbable, "ultimoTiempoMuestreo", Time.unscaledTime);
        SetPrivateField(grabbable, "ultimaPosicionMuestreo", grabbable.transform.position);
        SetPrivateField(grabbable, "ultimaRotacionMuestreo", grabbable.transform.rotation);
        grabbable.EndGrab();

        bool keptLinearImpulse = body.linearVelocity.magnitude >= 2f && body.linearVelocity.y > 0.5f;
        bool keptAngularImpulse = body.angularVelocity.magnitude >= 2f;
        Require(
            keptLinearImpulse && keptAngularImpulse && body.useGravity && !body.isKinematic,
            "Nivel 2: al soltar se perdió el impulso. Lineal=" + body.linearVelocity +
            ", angular=" + body.angularVelocity +
            ", gravedad=" + body.useGravity +
            ", cinemático=" + body.isKinematic + ".",
            failures
        );
        if (keptLinearImpulse && keptAngularImpulse && body.useGravity && !body.isKinematic)
        {
            checks.Add(
                "OK Nivel 2: el lanzamiento conservó impulso lineal " +
                body.linearVelocity + " y angular " + body.angularVelocity + "."
            );
        }
    }

    private static void TestVehicleFragility(
        AlgoLabLevel02VehicleObject vehicle,
        List<string> checks,
        List<string> failures)
    {
        bool valid = vehicle.impactoMaximoSeminuevo <= 6f &&
                     vehicle.impactoMaximoUsado <= 3f &&
                     vehicle.alturaMinimaCaidaSeminuevo <= 0.65f &&
                     vehicle.alturaMinimaCaidaUsado <= 0.3f &&
                     vehicle.impactoMaximoUsado < vehicle.impactoMaximoSeminuevo &&
                     vehicle.alturaMinimaCaidaUsado < vehicle.alturaMinimaCaidaSeminuevo;
        Require(
            valid,
            "Nivel 2: la fragilidad no diferencia correctamente seminuevos y usados.",
            failures
        );
        if (valid)
        {
            checks.Add("OK Nivel 2: seminuevos y usados tienen fragilidad progresiva.");
        }
    }

    private static void TestControllerGrabRelease(
        Scene testScene,
        SimpleMRGrabbable grabbable,
        Rigidbody body,
        List<string> checks,
        List<string> failures)
    {
        GameObject grabberObject = new GameObject("LogicTestControllerGrabber");
        SceneManager.MoveGameObjectToScene(grabberObject, testScene);

        try
        {
            SimpleOvRGrabber grabber = grabberObject.AddComponent<SimpleOvRGrabber>();
            grabber.mostrarDebug = false;
            grabber.aplicarVelocidadAlSoltar = true;
            grabber.physicsThrowMultiplier = 1f;
            grabber.physicsAngularMultiplier = 1f;

            Vector3 expectedLinear = new Vector3(3.2f, 1.4f, -0.3f);
            Vector3 expectedAngular = new Vector3(0f, 4.2f, 0f);

            grabbable.BeginGrab();
            SetPrivateField(grabber, "heldObject", grabbable);
            SetPrivateField(grabber, "controllerVelocity", expectedLinear);
            SetPrivateField(grabber, "controllerAngularVelocity", expectedAngular);

            MethodInfo release = typeof(SimpleOvRGrabber).GetMethod(
                "Release",
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new[] { typeof(bool) },
                null
            );
            Require(
                release != null,
                "Nivel 2: no se encontrÃ³ la liberaciÃ³n del agarre por control.",
                failures
            );
            if (release == null)
            {
                return;
            }

            release.Invoke(grabber, new object[] { true });

            bool linearPreserved = Vector3.Distance(body.linearVelocity, expectedLinear) < 0.01f;
            bool angularPreserved = Vector3.Distance(body.angularVelocity, expectedAngular) < 0.01f;
            Require(
                linearPreserved && angularPreserved && body.useGravity && !body.isKinematic,
                "Nivel 2: el gatillo secundario no conservÃ³ la velocidad real al soltar. " +
                "Lineal=" + body.linearVelocity + ", angular=" + body.angularVelocity + ".",
                failures
            );
            if (linearPreserved && angularPreserved && body.useGravity && !body.isKinematic)
            {
                checks.Add(
                    "OK Nivel 2: el agarre por gatillo secundario conservÃ³ la velocidad " +
                    body.linearVelocity + " al lanzar."
                );
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(grabberObject);
        }
    }

    private static void TestRandomVehicleRequirements(
        List<string> checks,
        List<string> failures)
    {
        GameObject root = new GameObject("LogicTestRandomPractice");
        root.SetActive(false);
        try
        {
            AlgoLabLevel02PracticeController practice =
                root.AddComponent<AlgoLabLevel02PracticeController>();
            practice.cantidadVehiculosAleatorios = 5;
            practice.garantizarTodosLosEstados = true;

            practice.CargarVehiculosRequeridosNivel2();
            HashSet<string> first = ValidateRandomList(practice, failures, "primer intento");

            practice.CargarVehiculosRequeridosNivel2();
            HashSet<string> second = ValidateRandomList(practice, failures, "segundo intento");

            bool changed = !first.SetEquals(second);
            Require(changed, "Nivel 2: dos intentos aleatorios produjeron la misma lista completa.", failures);
            if (changed && first.Count == 5 && second.Count == 5)
            {
                checks.Add("OK Nivel 2: cada intento generó cinco vehículos aleatorios sin duplicados.");
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static void TestDoorStateBeforeStart(
        List<string> checks,
        List<string> failures)
    {
        GameObject root = new GameObject("LogicTestDoorRoot");
        GameObject variantRoot = new GameObject("LogicTestDoorVariant");
        variantRoot.transform.SetParent(root.transform, false);

        try
        {
            variantRoot.AddComponent<AudioSource>();
            Door door = variantRoot.AddComponent<Door>();
            door.asource = null;

            AlgoLabThemeDoorController controller =
                root.AddComponent<AlgoLabThemeDoorController>();
            controller.mostrarDebug = false;
            controller.variantes.Add(new AlgoLabThemeDoorController.VariantePuerta
            {
                nombre = "Prueba",
                root = variantRoot,
                doorScript = door,
                renderersParaColor = Array.Empty<Renderer>()
            });
            controller.CambiarVariante(0);

            controller.AbrirPuerta();
            bool openedSafely = door.open && door.asource != null;
            controller.CerrarPuerta();
            bool closedSafely = !door.open && door.asource != null;

            Require(
                openedSafely && closedSafely,
                "Nivel 2: una puerta configurada antes de Start no pudo abrirse y cerrarse con seguridad.",
                failures
            );
            if (openedSafely && closedSafely)
            {
                checks.Add("OK Nivel 2: las puertas abren y cierran antes de Start sin NullReferenceException.");
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static void TestPracticeTutorials(
        List<string> checks,
        List<string> failures)
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        AlgoLabPracticeTutorialSequence[] sequences =
            UnityEngine.Object.FindObjectsByType<AlgoLabPracticeTutorialSequence>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        var byType = new Dictionary<AlgoLabPracticeTutorialSequence.TipoPractica,
            AlgoLabPracticeTutorialSequence>();
        for (int i = 0; i < sequences.Length; i++)
        {
            if (sequences[i] != null)
                byType[sequences[i].tipoPractica] = sequences[i];
        }

        foreach (AlgoLabPracticeTutorialSequence.TipoPractica type in
                 Enum.GetValues(typeof(AlgoLabPracticeTutorialSequence.TipoPractica)))
        {
            Require(
                byType.ContainsKey(type),
                "Tutoriales: falta la secuencia de " + type + " en la escena.",
                failures
            );
        }

        if (!byType.TryGetValue(
                AlgoLabPracticeTutorialSequence.TipoPractica.Nivel3Encapsulamiento,
                out AlgoLabPracticeTutorialSequence level3))
        {
            return;
        }

        AlgoLabPillarLevelController pillars =
            UnityEngine.Object.FindFirstObjectByType<AlgoLabPillarLevelController>(
                FindObjectsInactive.Include
            );
        Require(
            pillars != null && pillars.tutorialPracticaNivel3 == level3,
            "Nivel 3: la guia de practica no esta conectada al controlador de pilares.",
            failures
        );
        Require(
            level3.tutorialPanel != null && level3.PuedeReproducir,
            "Nivel 3: la guia provisional no puede reproducirse sin multimedia.",
            failures
        );

        MethodInfo save = typeof(AlgoLabPracticeTutorialSequence).GetMethod(
            "GuardarTutorial",
            BindingFlags.Instance | BindingFlags.NonPublic
        );
        MethodInfo configure = typeof(AlgoLabPracticeTutorialSequence).GetMethod(
            "AplicarConfiguracionTemporalDePractica",
            BindingFlags.Instance | BindingFlags.NonPublic
        );
        MethodInfo restore = typeof(AlgoLabPracticeTutorialSequence).GetMethod(
            "RestaurarTutorial",
            BindingFlags.Instance | BindingFlags.NonPublic
        );
        Require(
            save != null && configure != null && restore != null,
            "Tutoriales: no se encontro la configuracion temporal verificable.",
            failures
        );
        if (save == null || configure == null || restore == null || level3.tutorialPanel == null)
        {
            return;
        }

        AlgoLabTutorialPanelController panel = level3.tutorialPanel;
        save.Invoke(level3, null);
        try
        {
            configure.Invoke(level3, null);
            bool skipEnabled =
                panel.permitirOmitirConDobleA &&
                panel.permitirBotonAOVR &&
                panel.permitirTeclaAEnEditor &&
                panel.tiempoMaximoDobleA <= 2.01f;
            bool pocketResumes =
                !panel.omitirTutorialAlGuardarEnPanelOpciones &&
                !panel.repetirTutorialAlSacarTutorialOmitidoDesdePanelOpciones &&
                !panel.reiniciarTutorialSiempreAlSacarDelPanelOpciones;

            Require(
                skipEnabled,
                "Tutoriales: el doble A no quedo habilitado con intervalo de dos segundos.",
                failures
            );
            Require(
                pocketResumes,
                "Tutoriales: guardar la guia todavia puede omitirla o reiniciarla.",
                failures
            );

            if (skipEnabled && pocketResumes)
            {
                checks.Add(
                    "OK Tutoriales: doble A habilitado y pocket configurado para reanudar el punto exacto."
                );
            }
        }
        finally
        {
            restore.Invoke(level3, null);
        }

        MethodInfo buildEvents = typeof(AlgoLabPracticeTutorialSequence).GetMethod(
            "ConstruirEventos",
            BindingFlags.Instance | BindingFlags.NonPublic
        );
        Require(
            buildEvents != null,
            "Nivel 3: no se encontro el constructor de la guia provisional.",
            failures
        );
        if (buildEvents == null)
        {
            return;
        }

        object[] arguments = { 0f };
        var events =
            buildEvents.Invoke(level3, arguments) as
                List<AlgoLabTutorialPanelController.EventoTutorial>;
        bool validTimeline =
            events != null &&
            events.Count >= 10 &&
            (float)arguments[0] >= level3.duracionGuiaTextoNivel3;
        Require(
            validTimeline,
            "Nivel 3: la guia provisional no tiene una linea de tiempo completa.",
            failures
        );
        if (validTimeline)
        {
            checks.Add("OK Nivel 3: guia de practica provisional conectada y lista para multimedia.");
        }
    }

    private static void TestLevel1DoorDiagramData(
        List<string> checks,
        List<string> failures)
    {
        const string prefabPath =
            "Assets/__Algolab/Prefabs/Objects/level1/PuertaTemaRoot.prefab";
        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            AlgoLabObjetoEducativo[] objects =
                root.GetComponentsInChildren<AlgoLabObjetoEducativo>(true);
            AlgoLabObjetoEducativo door = Array.Find(
                objects,
                item =>
                    item != null &&
                    string.Equals(
                        item.nombreClase,
                        "Puerta",
                        StringComparison.OrdinalIgnoreCase
                    )
            );

            bool complete =
                door != null &&
                door.atributos != null &&
                door.atributos.Length >= 3 &&
                door.metodos != null &&
                door.metodos.Length >= 2;
            Require(
                complete,
                "Nivel 1: el diagrama de Puerta no contiene sus atributos y metodos.",
                failures
            );
            if (complete)
            {
                checks.Add(
                    "OK Nivel 1: Puerta publica atributos y metodos completos en el diagrama."
                );
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void TestEditableAuthoringContent(
        List<string> checks,
        List<string> failures)
    {
        const string level3Path =
            "Assets/__Algolab/Prefabs/Objects/level3/EncapsulationTheme_Audios01_03.prefab";
        const string level4Path =
            "Assets/__Algolab/Prefabs/Objects/level4/AbstractionTheme_Audios01_06.prefab";

        GameObject level3 = PrefabUtility.LoadPrefabContents(level3Path);
        try
        {
            AlgoLabEncapsulationThemeVisual visual =
                level3.GetComponent<AlgoLabEncapsulationThemeVisual>();
            Require(
                visual != null,
                "Autoría: falta el controlador visual del nivel 3.",
                failures
            );
            if (visual != null)
            {
                visual.PrepareEditableHierarchy();
                visual.PrepareEditableHierarchy();
            }

            Transform theme =
                level3.transform.Find(
                    "VisualesEncapsulamiento_Audios01_03"
                );
            Transform bank =
                level3.transform.Find(
                    "VisualesFisicos_CuentaBancaria_Audios04_10"
                );
            bool complete =
                theme != null &&
                theme.Find("Pilar_4/IconoPilar_4") != null &&
                theme.Find("Acceso_3/IconoAcceso_3") != null &&
                bank != null &&
                bank.Find(
                    "ObjetosFisicos/Objeto_CuentaBancaria/" +
                    "Variable_valor_Oro_DentroCajaFuerte"
                ) != null &&
                CountDirectChildren(
                    level3.transform,
                    "VisualesEncapsulamiento_Audios01_03"
                ) == 1 &&
                CountDirectChildren(
                    level3.transform,
                    "VisualesFisicos_CuentaBancaria_Audios04_10"
                ) == 1;
            Require(
                complete,
                "Autoría: la jerarquia editable del tema del nivel 3 esta incompleta o se duplica.",
                failures
            );
            if (complete)
            {
                checks.Add(
                    "OK Autoría: nivel 3 tiene pilares, accesos y ejemplo bancario editables sin duplicados."
                );
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(level3);
        }

        GameObject level4 = PrefabUtility.LoadPrefabContents(level4Path);
        try
        {
            AlgoLabAbstractionThemeVisual visual =
                level4.GetComponent<AlgoLabAbstractionThemeVisual>();
            Require(
                visual != null,
                "Autoría: falta el controlador visual del nivel 4.",
                failures
            );
            if (visual != null)
            {
                visual.PrepareEditableHierarchy();
                visual.PrepareEditableHierarchy();
            }

            Transform theme =
                level4.transform.Find("VisualesAbstraccion_Audios01_08");
            bool complete =
                theme != null &&
                theme.Find(
                    "01_CuatroPilares_ReutilizadosNivel3/Pilar_4"
                ) != null &&
                theme.Find("02_Cancion_Centro/Vinilo") != null &&
                theme.Find("03_Tienda_LadoIzquierdo/Tienda") != null &&
                theme.Find(
                    "04_Aplicacion_LadoDerecho/Telefono"
                ) != null &&
                theme.Find(
                    "04_Aplicacion_LadoDerecho/" +
                    "PlacaInternaDentroDelTelefono"
                ) != null &&
                theme.Find(
                    "Diagrama_Abstraccion_CancionAplicacion"
                ) != null &&
                CountDirectChildren(
                    level4.transform,
                    "VisualesAbstraccion_Audios01_08"
                ) == 1;
            Require(
                complete,
                "Autoría: la jerarquia editable del tema del nivel 4 esta incompleta o se duplica.",
                failures
            );
            if (complete)
            {
                checks.Add(
                    "OK Autoría: nivel 4 tiene pilares, vinilo, tienda, telefono y placa editables sin duplicados."
                );
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(level4);
        }

        Scene scene =
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject catalog = Array.Find(
            scene.GetRootGameObjects(),
            item => item.name == "[CONTENIDO_EDITABLE_ALGOLAB]"
        );
        AlgoLabEditableContentCatalog catalogComponent =
            catalog != null
                ? catalog.GetComponent<AlgoLabEditableContentCatalog>()
                : null;
        bool catalogComplete =
            catalog != null &&
            catalog.CompareTag("EditorOnly") &&
            !catalog.activeSelf &&
            catalogComponent != null &&
            catalogComponent.contenidoEditable != null &&
            catalogComponent.contenidoEditable.Length >= 6 &&
            catalogComponent.tutorialesEnEscena != null &&
            catalogComponent.tutorialesEnEscena.Length > 0 &&
            catalogComponent.controladorNiveles != null &&
            catalogComponent.administradorObjetos != null;
        Require(
            catalogComplete,
            "Autoría: el catalogo EditorOnly no enlaza todos los niveles, tutoriales y controladores.",
            failures
        );
        if (catalogComplete)
        {
            checks.Add(
                "OK Autoría: catálogo EditorOnly enlaza niveles, tutoriales y controladores sin entrar al APK."
            );
        }
    }

    private static int CountDirectChildren(
        Transform parent,
        string childName)
    {
        int count = 0;
        for (int i = 0; i < parent.childCount; i++)
        {
            if (parent.GetChild(i).name == childName)
            {
                count++;
            }
        }
        return count;
    }

    private static void TestLevel3RobotPractice(
        List<string> checks,
        List<string> failures)
    {
        GameObject root = new GameObject("LogicTestLevel3Robot");
        try
        {
            AlgoLabEncapsulationRobotPractice practice =
                root.AddComponent<AlgoLabEncapsulationRobotPractice>();
            practice.mostrarDebug = false;
            practice.completarNivelAutomaticamente = false;
            practice.IniciarPractica();

            bool initialState =
                practice.Encendido &&
                practice.Averiado &&
                practice.Energia == practice.energiaInicial &&
                practice.Temperatura == practice.temperaturaInicial &&
                practice.Puntaje == practice.puntajeInicial;
            Require(
                initialState,
                "Nivel 3 robot: el estado inicial no representa un robot encendido con fallas.",
                failures
            );
            practice.ActualizarPuntajePorTiempo(150f, 300f);
            Require(
                practice.Puntaje == 50,
                "Nivel 3 robot: el tiempo restante no se refleja en la puntuacion.",
                failures
            );
            practice.IniciarPractica();

            int initialEnergy = practice.Energia;
            practice.MetodoRecargar();
            Require(
                practice.Energia == initialEnergy &&
                practice.Puntaje ==
                    practice.puntajeInicial - practice.penalizacionAccesoPrivado,
                "Nivel 3 robot: recargar() encendido no fue bloqueado y penalizado.",
                failures
            );

            practice.IntentarModificarEnergiaPrivada();
            Require(
                practice.Puntaje ==
                    practice.puntajeInicial -
                    practice.penalizacionAccesoPrivado * 2 &&
                practice.ErroresPrivados == 1 &&
                practice.ErroresTotales == 2,
                "Nivel 3 robot: el acceso directo privado no aplico la penalizacion.",
                failures
            );

            practice.MetodoApagar();
            practice.MetodoEncender();
            Require(
                practice.Encendido &&
                practice.Averiado &&
                !practice.PracticaCompletada,
                "Nivel 3 robot: no permite reencender el robot averiado.",
                failures
            );
            practice.MetodoApagar();
            int puntajeAntesReemplazo = practice.Puntaje;
            practice.NotificarReemplazoPrivadoBateria();
            practice.MetodoEncender();
            Require(
                !practice.PracticaCompletada &&
                practice.Encendido &&
                practice.Averiado &&
                practice.Puntaje ==
                    puntajeAntesReemplazo - practice.penalizacionAccesoPrivado &&
                practice.ReemplazoBateriaPrivado,
                "Nivel 3 robot: reemplazar la bateria privada no fallo ni desconto puntos al encender.",
                failures
            );
            practice.MetodoApagar();
            practice.NotificarRetiroReemplazoPrivadoBateria();

            practice.MetodoRecargar();
            practice.MetodoEnfriar();
            practice.MetodoReparar();
            practice.MetodoEncender();

            bool repaired =
                practice.PracticaCompletada &&
                practice.Encendido &&
                !practice.Averiado &&
                practice.Energia == 100 &&
                practice.Temperatura <= practice.temperaturaMaximaEncendido;
            Require(
                repaired,
                "Nivel 3 robot: la secuencia publica valida no completo la reparacion.",
                failures
            );

            AlgoLabObjetoEducativo data =
                root.GetComponent<AlgoLabObjetoEducativo>();
            bool diagramReady =
                data != null &&
                data.nombreClase == "Robot" &&
                data.atributos != null &&
                data.atributos.Length == 3 &&
                data.metodos != null &&
                data.metodos.Length == 3 &&
                data.atributos[0].StartsWith("-") &&
                data.metodos[0].StartsWith("+");
            Require(
                diagramReady,
                "Nivel 3 robot: faltan atributos o metodos en el diagrama de clase.",
                failures
            );

            Transform importedRobot = root.transform.Find(
                "RobotPracticeVisual/Robot/ModeloRobotRigged"
            );
            bool visualReady =
                root.transform.Find("RobotPracticeVisual/Robot") != null &&
                importedRobot != null &&
                root.transform.Find("RobotPracticeVisual/PanelHerramientasPublicas") != null &&
                root.transform.Find(
                    "RobotPracticeVisual/Robot/CompartimientoTemperatura/ModuloTemperaturaExtraible"
                ) != null &&
                root.GetComponentsInChildren<Button>(true).Length >= 2 &&
                root.GetComponentsInChildren<SimpleMRGrabbable>(true).Length >= 4 &&
                root.GetComponentsInChildren<AlgoLabRobotBreakableGlass>(true).Length == 2 &&
                root.GetComponentInChildren<AlgoLabRobotRigAxisConstraint>(true) != null;
            Require(
                visualReady,
                "Nivel 3 robot: faltan el robot, sus internos o el panel de herramientas.",
                failures
            );

            Renderer[] robotRenderers = importedRobot != null
                ? importedRobot.GetComponentsInChildren<Renderer>(true)
                : System.Array.Empty<Renderer>();
            Bounds robotBounds = default;
            bool boundsReady = false;
            bool renderersTexturizados = robotRenderers.Length >= 6;
            for (int i = 0; i < robotRenderers.Length; i++)
            {
                if (!boundsReady)
                {
                    robotBounds = robotRenderers[i].bounds;
                    boundsReady = true;
                }
                else
                {
                    robotBounds.Encapsulate(robotRenderers[i].bounds);
                }

                Material material = robotRenderers[i].sharedMaterial;
                Texture texture = null;
                if (material != null && material.HasProperty("_BaseMap"))
                    texture = material.GetTexture("_BaseMap");
                if (texture == null && material != null && material.HasProperty("_MainTex"))
                    texture = material.GetTexture("_MainTex");
                renderersTexturizados &= texture != null;
            }
            bool robotVerticalYEscalado =
                boundsReady &&
                robotBounds.size.y >= 1.45f &&
                robotBounds.size.y <= 1.90f &&
                robotBounds.size.y > robotBounds.size.x * 1.45f &&
                robotBounds.size.y > robotBounds.size.z * 2.5f;
            Require(
                robotVerticalYEscalado && renderersTexturizados,
                "Nivel 3 robot: el FBX no quedo vertical, con escala correcta y todas sus texturas.",
                failures
            );

            Transform battery = root.transform.Find(
                "RobotPracticeVisual/Robot/CompartimientoBateriaTrasero/BateriaExtraible"
            );
            AlgoLabGrabProximityGate gate = battery != null
                ? battery.GetComponent<AlgoLabGrabProximityGate>()
                : null;
            AlgoLabRobotBreakableGlass rearGlass = null;
            AlgoLabRobotBreakableGlass[] glasses =
                root.GetComponentsInChildren<AlgoLabRobotBreakableGlass>(true);
            for (int i = 0; i < glasses.Length; i++)
            {
                if (glasses[i].compartimiento ==
                    AlgoLabRobotBreakableGlass.Compartimiento.Bateria)
                {
                    rearGlass = glasses[i];
                    break;
                }
            }

            bool blockedByGlass =
                gate != null &&
                battery != null &&
                !gate.PuedeAgarrarseDesde(battery.position);
            if (rearGlass != null)
                rearGlass.Romper();

            Collider batteryCollider = battery != null
                ? battery.GetComponent<Collider>()
                : null;
            Vector3 nearBattery = batteryCollider != null
                ? batteryCollider.bounds.center
                : (battery != null ? battery.position : Vector3.zero);
            bool strictProximity =
                gate != null &&
                gate.PuedeAgarrarseDesde(nearBattery) &&
                !gate.PuedeAgarrarseDesde(nearBattery + Vector3.one * 0.5f);
            Rigidbody[] shards = rearGlass != null && rearGlass.fragmentos != null
                ? rearGlass.fragmentos.GetComponentsInChildren<Rigidbody>(true)
                : System.Array.Empty<Rigidbody>();
            bool physicalShards =
                shards.Length >= 9 &&
                System.Array.Exists(shards, body => body != null && !body.isKinematic);

            Transform temperatureModule = root.transform.Find(
                "RobotPracticeVisual/Robot/CompartimientoTemperatura/ModuloTemperaturaExtraible"
            );
            AlgoLabGrabProximityGate temperatureGate = temperatureModule != null
                ? temperatureModule.GetComponent<AlgoLabGrabProximityGate>()
                : null;
            AlgoLabRobotBreakableGlass frontGlass = null;
            for (int i = 0; i < glasses.Length; i++)
            {
                if (glasses[i].compartimiento ==
                    AlgoLabRobotBreakableGlass.Compartimiento.Temperatura)
                {
                    frontGlass = glasses[i];
                    break;
                }
            }
            bool temperatureBlocked =
                temperatureGate != null &&
                temperatureModule != null &&
                !temperatureGate.PuedeAgarrarseDesde(temperatureModule.position);
            if (frontGlass != null)
                frontGlass.Romper();
            Collider temperatureCollider = temperatureModule != null
                ? temperatureModule.GetComponent<Collider>()
                : null;
            Vector3 nearTemperature = temperatureCollider != null
                ? temperatureCollider.bounds.center
                : (temperatureModule != null ? temperatureModule.position : Vector3.zero);
            bool temperatureProximity =
                temperatureGate != null &&
                temperatureGate.PuedeAgarrarseDesde(nearTemperature) &&
                !temperatureGate.PuedeAgarrarseDesde(nearTemperature + Vector3.one * 0.5f);

            Transform frontFrame = root.transform.Find(
                "RobotPracticeVisual/Robot/CompartimientoTemperatura/MarcoVidrioTemperatura"
            );
            Transform rearFrame = root.transform.Find(
                "RobotPracticeVisual/Robot/CompartimientoBateriaTrasero/MarcoVidrioBateria"
            );
            bool visibleGlassFrames =
                frontFrame != null &&
                rearFrame != null &&
                frontFrame.GetComponentsInChildren<Renderer>(true).Length == 4 &&
                rearFrame.GetComponentsInChildren<Renderer>(true).Length == 4;

            MethodInfo segmentMethod = typeof(AlgoLabRobotBreakableGlass).GetMethod(
                "SegmentoIntersecaBounds",
                BindingFlags.Static | BindingFlags.NonPublic
            );
            Bounds sweepBounds = new Bounds(Vector3.zero, Vector3.one * 0.2f);
            bool sweepHit =
                segmentMethod != null &&
                (bool)segmentMethod.Invoke(
                    null,
                    new object[]
                    {
                        new Vector3(0f, 0f, -1f),
                        new Vector3(0f, 0f, 1f),
                        sweepBounds
                    }
                );

            Require(
                blockedByGlass && strictProximity && physicalShards &&
                temperatureBlocked && temperatureProximity &&
                visibleGlassFrames && sweepHit,
                "Nivel 3 robot: los componentes privados no exigen cercania/vidrio roto o el vidrio no genera fragmentos fisicos.",
                failures
            );

            if (initialState && repaired && diagramReady && visualReady &&
                robotVerticalYEscalado && renderersTexturizados &&
                blockedByGlass && strictProximity && physicalShards &&
                temperatureBlocked && temperatureProximity &&
                visibleGlassFrames && sweepHit)
            {
                checks.Add(
                    "OK Nivel 3: robot rigged, bateria y temperatura por proximidad, con vidrios fisicos validados."
                );
            }

            GameObject prefab =
                Resources.Load<GameObject>("Level3/AlgoLabRobotPractice");
            Require(
                prefab != null &&
                prefab.GetComponent<AlgoLabEncapsulationRobotPractice>() != null &&
                prefab.transform.Find(
                    "RobotPracticeVisual/Robot/ModeloRobotRigged"
                ) != null &&
                prefab.transform.Find(
                    "RobotPracticeVisual/Robot/CompartimientoTemperatura/VidrioTemperatura"
                ) != null &&
                prefab.transform.Find(
                    "RobotPracticeVisual/Robot/CompartimientoBateriaTrasero/VidrioBateria"
                ) != null,
                "Nivel 3 robot: el prefab editable no contiene toda la jerarquia usada por el ManualSpawner.",
                failures
            );

            Require(
                AssetDatabase.LoadAssetAtPath<SceneAsset>(
                    "Assets/Scenes/Nivel3_Monitor_Editable.unity"
                ) != null &&
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/__Algolab/Resources/Level3/RobotWorkshop/Prefabs/RobotNivel3Editado.prefab"
                ) != null,
                "Nivel 3 robot: faltan la escena editable del monitor o el prefab editable del robot.",
                failures
            );

            Require(
                Resources.Load<GameObject>(
                    "Level3/RobotWorkshop/Models/Robot/AlgoLabRobot"
                ) != null &&
                Resources.Load<GameObject>(
                    "Level3/RobotWorkshop/Models/Battery/Battery_Small"
                ) != null &&
                Resources.Load<GameObject>(
                    "Level3/RobotWorkshop/Models/Temperature/AlgoLabTemperatureModule"
                ) != null &&
                Resources.Load<Texture2D>(
                    "Level3/RobotWorkshop/Textures/Robot/RobotTexture"
                ) != null &&
                Resources.Load<Texture2D>(
                    "Level3/RobotWorkshop/Textures/Robot/CabezaImagen"
                ) != null &&
                Resources.Load<Texture2D>(
                    "Level3/RobotWorkshop/Textures/Robot/TextureBrazoL"
                ) != null &&
                Resources.Load<Texture2D>(
                    "Level3/RobotWorkshop/Textures/Robot/BrazoR"
                ) != null &&
                Resources.Load<Texture2D>(
                    "Level3/RobotWorkshop/Textures/Robot/PiernaImagen"
                ) != null,
                "Nivel 3 robot: faltan modelos FBX o texturas extraidas en Resources.",
                failures
            );
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static HashSet<string> ValidateRandomList(
        AlgoLabLevel02PracticeController practice,
        List<string> failures,
        string label)
    {
        var signatures = new HashSet<string>();
        var states = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        Require(
            practice.vehiculosRequeridos != null && practice.vehiculosRequeridos.Count == 5,
            "Nivel 2: " + label + " no generó cinco vehículos.",
            failures
        );

        if (practice.vehiculosRequeridos == null)
        {
            return signatures;
        }

        for (int i = 0; i < practice.vehiculosRequeridos.Count; i++)
        {
            AlgoLabLevel02PracticeController.VehiculoRequerido item =
                practice.vehiculosRequeridos[i];
            string signature = item.color + "|" + item.modelo + "|" + item.carcasa +
                               "|" + item.estado + "|" + item.metodo;
            Require(
                signatures.Add(signature),
                "Nivel 2: " + label + " contiene una combinación duplicada.",
                failures
            );
            states.Add(item.estado);
        }

        Require(
            states.Contains("nuevo") && states.Contains("seminuevo") && states.Contains("usado"),
            "Nivel 2: " + label + " no incluye los tres estados.",
            failures
        );
        return signatures;
    }

    private static void TestRoomRayForBothHands(
        Scene testScene,
        AlgoLabLevel02VehicleObject vehicle,
        List<string> checks,
        List<string> failures)
    {
        GameObject commandObject = new GameObject("LogicTestVehicleCommand");
        SceneManager.MoveGameObjectToScene(commandObject, testScene);
        commandObject.transform.position = new Vector3(1.5f, 1.2f, -1f);
        commandObject.transform.rotation = Quaternion.LookRotation(
            new Vector3(0f, -0.3f, 1f).normalized,
            Vector3.up
        );

        AlgoLabVehicleRoomCommandController command =
            commandObject.AddComponent<AlgoLabVehicleRoomCommandController>();
        command.rayOrigin = commandObject.transform;
        command.maxDistance = 8f;
        command.roomLayers = ~0;
        command.panelBlockerLayers = 0;
        command.bloquearSiRayoTocaPanelUI = false;
        command.buscarPanelesAutomaticamente = false;
        command.mostrarMarcadorDestino = false;
        command.mostrarDebug = false;

        MethodInfo rayMethod = typeof(AlgoLabVehicleRoomCommandController).GetMethod(
            "ObtenerPuntoValidoDelCuarto",
            BindingFlags.Instance | BindingFlags.NonPublic
        );
        Require(rayMethod != null, "Nivel 2: no se encontró el cálculo privado del destino.", failures);
        if (rayMethod == null)
        {
            return;
        }

        foreach (AlgoLabVehicleRoomCommandController.HandSide hand in
                 new[]
                 {
                     AlgoLabVehicleRoomCommandController.HandSide.Left,
                     AlgoLabVehicleRoomCommandController.HandSide.Right
                 })
        {
            command.handSide = hand;
            object[] arguments = { Vector3.zero };
            bool hit = (bool)rayMethod.Invoke(command, arguments);
            Vector3 point = (Vector3)arguments[0];
            Require(
                hit && point.z > commandObject.transform.position.z,
                "Nivel 2: el rayo de destino de la mano " + hand + " no encontró el piso.",
                failures
            );
            if (hit && point.z > commandObject.transform.position.z)
            {
                checks.Add("OK Nivel 2: rayo de destino " + hand + " encontró " + point + ".");
            }
        }
    }

    private static GameObject FindVehiclePrefab()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        AlgoLabLevel02GarageController[] garages =
            UnityEngine.Object.FindObjectsByType<AlgoLabLevel02GarageController>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        for (int i = 0; i < garages.Length; i++)
        {
            if (garages[i] != null && garages[i].prefabVehiculo != null)
            {
                return garages[i].prefabVehiculo;
            }
        }

        return null;
    }

    private static void Require(bool condition, string message, List<string> failures)
    {
        if (!condition)
        {
            failures.Add(message);
        }
    }

    private static bool InvokePrivateBool(object target, string methodName)
    {
        MethodInfo method = target.GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic
        );
        return method != null && (bool)method.Invoke(target, null);
    }

    private static bool ReadPrivateBool(object target, string fieldName)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic
        );
        return field != null && (bool)field.GetValue(target);
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic
        );
        if (field == null)
        {
            throw new MissingFieldException(target.GetType().Name, fieldName);
        }
        field.SetValue(target, value);
    }

    private static string BuildGroundDiagnostic(AlgoLabLevel02VehicleObject vehicle)
    {
        MethodInfo method = typeof(AlgoLabLevel02VehicleObject).GetMethod(
            "IntentarObtenerBoundsFisicos",
            BindingFlags.Instance | BindingFlags.NonPublic
        );
        if (method == null)
        {
            return "Sin método de bounds.";
        }

        object[] arguments = { new Bounds() };
        if (!(bool)method.Invoke(vehicle, arguments))
        {
            return "Sin bounds físicos.";
        }

        Bounds bounds = (Bounds)arguments[0];
        float distance = Mathf.Max(vehicle.distanciaRaycastSuelo, bounds.extents.y + 0.18f);
        RaycastHit[] hits = Physics.RaycastAll(
            bounds.center,
            Vector3.down,
            distance,
            vehicle.capasSuelo,
            QueryTriggerInteraction.Ignore
        );
        var names = new List<string>();
        for (int i = 0; i < hits.Length; i++)
        {
            names.Add(hits[i].collider.name + "@" + hits[i].distance.ToString("F3"));
        }

        var colliderDescriptions = new List<string>();
        Collider[] vehicleColliders = vehicle.GetComponentsInChildren<Collider>(false);
        for (int i = 0; i < vehicleColliders.Length; i++)
        {
            Collider current = vehicleColliders[i];
            string extra = current is MeshCollider mesh
                ? ",convexo=" + mesh.convex
                : string.Empty;
            colliderDescriptions.Add(
                current.name + ":" + current.GetType().Name +
                ",trigger=" + current.isTrigger + extra +
                ",minY=" + current.bounds.min.y.ToString("F3") +
                ",maxY=" + current.bounds.max.y.ToString("F3")
            );
        }

        return "Bounds=" + bounds +
               ", distanciaRayo=" + distance.ToString("F3") +
               ", impactos=[" + string.Join(",", names) + "]" +
               ", colliders=[" + string.Join(";", colliderDescriptions) + "].";
    }

    private static bool TryGetCombinedBounds(GameObject root, out Bounds bounds)
    {
        bounds = new Bounds(root.transform.position, Vector3.zero);
        Collider[] colliders = root.GetComponentsInChildren<Collider>(false);
        bool found = false;
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider current = colliders[i];
            if (current == null || !current.enabled || current.isTrigger)
            {
                continue;
            }

            if (!found)
            {
                bounds = current.bounds;
                found = true;
            }
            else
            {
                bounds.Encapsulate(current.bounds);
            }
        }

        return found;
    }
}
