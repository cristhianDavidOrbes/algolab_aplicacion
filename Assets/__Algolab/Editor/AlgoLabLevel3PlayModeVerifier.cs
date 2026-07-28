#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class AlgoLabLevel3PlayModeVerifier
{
    private const string ActiveKey = "AlgoLab.Level3.PlayModeVerifier.Active";
    private const string ResultPath = "Logs/level3-playmode-result.txt";
    private const string PrefabPath =
        "Assets/__Algolab/Resources/Level3/AlgoLabRobotPractice.prefab";

    private static int fase;
    private static double siguientePaso;
    private static bool finalizado;
    private static string resultado = string.Empty;
    private static float tiempoAlApagar;
    private static float tiempoAlReencender;
    private static AudioSource fuenteVozPrueba;
    private static int muestraVozAlApagar;

    static AlgoLabLevel3PlayModeVerifier()
    {
        if (EditorPrefs.GetBool(ActiveKey, false))
            ConectarEventos();
    }

    public static void Begin()
    {
        Directory.CreateDirectory("Logs");
        File.WriteAllText(ResultPath, "INICIANDO\n");
        EditorPrefs.SetBool(ActiveKey, true);
        fase = 0;
        finalizado = false;
        resultado = string.Empty;
        ConectarEventos();

        Scene escena = EditorSceneManager.NewScene(
            NewSceneSetup.EmptyScene,
            NewSceneMode.Single
        );
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null)
            Fallar("No se encontro el prefab de practica.");
        PrefabUtility.InstantiatePrefab(prefab, escena);
        EditorSceneManager.SaveScene(
            escena,
            "Assets/Scenes/__Level3RobotRuntimeTest.unity"
        );
        EditorApplication.isPlaying = true;
    }

    private static void ConectarEventos()
    {
        EditorApplication.playModeStateChanged -= AlCambiarModo;
        EditorApplication.playModeStateChanged += AlCambiarModo;
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
    }

    private static void AlCambiarModo(PlayModeStateChange cambio)
    {
        if (!EditorPrefs.GetBool(ActiveKey, false))
            return;

        if (cambio == PlayModeStateChange.EnteredPlayMode)
        {
            fase = 1;
            siguientePaso = EditorApplication.timeSinceStartup + 0.35d;
        }
        else if (cambio == PlayModeStateChange.EnteredEditMode && finalizado)
        {
            TerminarProceso();
        }
    }

    private static void Tick()
    {
        if (!EditorPrefs.GetBool(ActiveKey, false) ||
            !EditorApplication.isPlaying ||
            EditorApplication.timeSinceStartup < siguientePaso)
        {
            return;
        }

        try
        {
            AlgoLabEncapsulationRobotPractice practica =
                UnityEngine.Object.FindFirstObjectByType<
                    AlgoLabEncapsulationRobotPractice
                >(FindObjectsInactive.Include);
            AlgoLabLevel3RobotPracticeRuntime runtime =
                UnityEngine.Object.FindFirstObjectByType<
                    AlgoLabLevel3RobotPracticeRuntime
                >(FindObjectsInactive.Include);

            Exigir(practica != null, "No aparecio el controlador de practica.");
            Exigir(runtime != null, "No aparecio el runtime del robot.");

            if (fase == 1)
            {
                Exigir(practica.PracticaIniciada, "La practica no inicio.");
                Exigir(practica.Encendido, "El robot no inicio encendido.");
                Exigir(practica.Energia == 25, "La bateria inicial no es 25%.");
                Exigir(practica.Temperatura == 85, "La temperatura inicial no es 85 C.");
                Exigir(!runtime.Explotado, "El robot exploto al iniciar.");
                VerificarDistribucionEInteracciones(runtime);
                VerificarNuevasReglasRobot(runtime);
                VerificarComponentesInternosAgarrables(runtime);
                VerificarPantallasBoton(runtime, true);
                PrepararVozPrueba(runtime);

                runtime.PulsarBotonEnergia();
                Exigir(!practica.Encendido, "El boton fisico no apago el robot.");
                VerificarPantallasBoton(runtime, false);
                tiempoAlApagar = runtime.TiempoRestante;
                fase = 2;
                siguientePaso = EditorApplication.timeSinceStartup + 1.55d;
                return;
            }

            if (fase == 2)
            {
                Exigir(
                    Mathf.Abs(runtime.TiempoRestante - tiempoAlApagar) < 0.035f,
                    "El contador no se pauso al apagar el robot."
                );
                Exigir(
                    runtime.TieneVozPausada &&
                    fuenteVozPrueba != null &&
                    !fuenteVozPrueba.isPlaying &&
                    fuenteVozPrueba.timeSamples >= muestraVozAlApagar,
                    "La voz no conservo su posicion al apagar el robot."
                );
                runtime.PulsarBotonEnergia();
                Exigir(
                    practica.Encendido &&
                    practica.Averiado &&
                    !practica.PracticaCompletada,
                    "El robot averiado no volvio a encender."
                );
                VerificarPantallasBoton(runtime, true);
                tiempoAlReencender = runtime.TiempoRestante;
                fase = 3;
                siguientePaso = EditorApplication.timeSinceStartup + 0.32d;
                return;
            }

            if (fase == 3)
            {
                Exigir(
                    runtime.TiempoRestante < tiempoAlReencender - 0.05f,
                    "El contador no continuo desde el tiempo guardado."
                );
                Exigir(
                    fuenteVozPrueba != null &&
                    fuenteVozPrueba.isPlaying &&
                    fuenteVozPrueba.volume > 0.01f &&
                    fuenteVozPrueba.pitch > 0.43f,
                    "La voz no reanudo suavemente al encender el robot."
                );
                runtime.PulsarBotonEnergia();
                Exigir(
                    !practica.Encendido,
                    "El robot no se apago por segunda vez."
                );
                PrepararCargaFisica(runtime);
                fase = 30;
                siguientePaso = EditorApplication.timeSinceStartup + 0.25d;
                return;
            }

            if (fase == 30)
            {
                Exigir(
                    runtime.CargadorConectado && practica.Energia > 25,
                    "El cargador fisico no se conecto o no aumento la bateria."
                );
                runtime.cargadorGrab.EndGrab();
                fase = 300;
                siguientePaso = EditorApplication.timeSinceStartup + 0.15d;
                return;
            }

            if (fase == 300)
            {
                Exigir(
                    runtime.CargadorConectado,
                    "El cargador se desconecto al soltarlo dentro del puerto."
                );
                runtime.cargadorGrab.BeginGrab();
                fase = 301;
                siguientePaso = EditorApplication.timeSinceStartup + 0.15d;
                return;
            }

            if (fase == 301)
            {
                Exigir(
                    !runtime.CargadorConectado,
                    "El cargador no se pudo desenchufar al volver a agarrarlo."
                );
                runtime.cargadorGrab.EndGrab();
                practica.IniciarPractica();
                runtime.PulsarBotonEnergia();
                practica.NotificarVidrioRoto(
                    AlgoLabRobotBreakableGlass.Compartimiento.Temperatura
                );
                PrepararEnfriamientoFisico(runtime);
                fase = 31;
                siguientePaso = EditorApplication.timeSinceStartup + 0.25d;
                return;
            }

            if (fase == 31)
            {
                Exigir(
                    practica.Temperatura < 85,
                    "El ventilador fisico no redujo la temperatura."
                );
                runtime.ventiladorGrab.EndGrab();
                practica.IniciarPractica();
                runtime.PulsarBotonEnergia();
                practica.NotificarVidrioRoto(
                    AlgoLabRobotBreakableGlass.Compartimiento.Bateria
                );
                practica.NotificarVidrioRoto(
                    AlgoLabRobotBreakableGlass.Compartimiento.Temperatura
                );
                practica.AplicarCargaFisica(500f);
                practica.AplicarEnfriamientoFisico(500f);
                Exigir(practica.Energia == 100, "La carga no llego al 100%.");
                Exigir(practica.Temperatura == 10, "El enfriamiento no llego a 10 C.");

                runtime.PulsarBotonEnergia();
                Exigir(practica.Encendido, "El robot reparado no encendio.");
                Exigir(
                    practica.PracticaCompletada,
                    "La reparacion no completo la practica."
                );
                fase = 4;
            }
            else if (fase == 4)
            {
                runtime.segundosAntesDeExplosion = 0.25f;
                practica.IniciarPractica();
                fase = 5;
                siguientePaso = EditorApplication.timeSinceStartup + 0.55d;
                return;
            }
            else if (fase == 5)
            {
                Exigir(
                    runtime.Explotado,
                    "La explosion no cambio el estado del runtime."
                );
                Exigir(
                    practica.PracticaFallida,
                    "La explosion no marco la practica fallida."
                );
                Exigir(
                    runtime.botonReintentar != null &&
                    runtime.botonReintentar.gameObject.activeInHierarchy,
                    "El boton Reintentar no aparecio."
                );
                VerificarPartesExplosionAgarrables(runtime);
                VerificarTextoReintentar(runtime);

                runtime.Reintentar();
                Exigir(!runtime.Explotado, "Reintentar no restauro el robot.");
                Exigir(
                    !practica.PracticaFallida,
                    "Reintentar dejo la practica fallida."
                );
                Exigir(
                    practica.Energia == 25,
                    "Reintentar no restauro la bateria."
                );
                Exigir(
                    practica.Temperatura == 85,
                    "Reintentar no restauro la temperatura."
                );
                Exigir(
                    practica.Puntaje == 100,
                    "Reintentar no restauro el puntaje."
                );

                runtime.segundosAntesDeExplosion = 60f;
                runtime.duracionMaximaPractica = 0.25f;
                practica.IniciarPractica();
                runtime.PulsarBotonEnergia();
                Exigir(
                    !practica.Encendido,
                    "No se pudo apagar el robot para probar el limite global."
                );
                fase = 6;
                siguientePaso = EditorApplication.timeSinceStartup + 0.55d;
                return;
            }

            if (fase == 6)
            {
                Exigir(
                    runtime.Explotado && practica.PracticaFallida,
                    "El limite global de cinco minutos no provoca la explosion."
                );

                resultado =
                    "OK\n" +
                    "- Inicio: 25% / 85 C / encendido\n" +
                    "- Boton fisico: apagado correcto\n" +
                    "- Reencendido averiado: correcto\n" +
                    "- Contador pausado y reanudado: correcto\n" +
                    "- Cargador fisico conectado y cargando: correcto\n" +
                    "- Ventilador fisico enfriando: correcto\n" +
                    "- Carga: 100%\n" +
                    "- Enfriamiento: 10 C\n" +
                    "- Encendido reparado y finalizacion: correctos\n" +
                    "- Explosion, partes fisicas y Reintentar: correctos\n" +
                    "- Desconexion del cargador y limite global: correctos\n";
                File.WriteAllText(ResultPath, resultado);
                finalizado = true;
                EditorApplication.isPlaying = false;
            }

            siguientePaso = EditorApplication.timeSinceStartup + 0.12d;
        }
        catch (Exception ex)
        {
            Fallar(ex.ToString());
        }
    }

    private static void Exigir(bool condicion, string mensaje)
    {
        if (!condicion)
            throw new InvalidOperationException(mensaje);
    }

    private static void VerificarDistribucionEInteracciones(
        AlgoLabLevel3RobotPracticeRuntime runtime)
    {
        Exigir(runtime.robot != null, "Falta el robot.");
        Exigir(runtime.panelHerramientas != null, "Falta el monitor.");
        Exigir(
            Vector3.Distance(
                runtime.robot.localPosition,
                new Vector3(0.22f, 0.46f, 0.54f)
            ) < 0.002f,
            "La posicion final del robot no es la aprobada."
        );
        Exigir(
            Vector3.Distance(
                runtime.robot.localScale,
                Vector3.one * 0.76f
            ) < 0.002f,
            "El robot no quedo ligeramente mas pequeno."
        );
        Exigir(
            Vector3.Distance(
                runtime.panelHerramientas.localPosition,
                new Vector3(0.22f, -0.50f, 0.20f)
            ) < 0.002f &&
            Quaternion.Angle(
                runtime.panelHerramientas.localRotation,
                Quaternion.identity
            ) < 0.1f,
            "El monitor no quedo bajo y con su orientacion original."
        );

        Exigir(
            runtime.botonEnergia != null &&
            Vector3.Dot(
                runtime.botonEnergia.ejePresionLocal.normalized,
                Vector3.down
            ) > 0.99f &&
            runtime.botonEnergia.radioContacto <= 0.04f,
            "El boton no esta configurado para hundirse con contacto cercano."
        );
        VerificarPalanca(runtime.palancaX, "X");
        VerificarPalanca(runtime.palancaY, "Y");
        MeshFilter palancaXMesh = runtime.palancaX != null
            ? runtime.palancaX.GetComponent<MeshFilter>()
            : null;
        Exigir(
            palancaXMesh != null &&
            palancaXMesh.sharedMesh != null &&
            palancaXMesh.sharedMesh.name.Contains("CarasCorregidas"),
            "La palanca X conserva las caras invertidas."
        );
        VerificarAgarreCercano(runtime.ventilador, 0.039f, "ventilador");
        VerificarAgarreCargadorCompleto(runtime);
        VerificarAgarreCercano(
            runtime.temperaturaRepuestoPrivada,
            0.031f,
            "repuesto de temperatura"
        );
        VerificarAgarreCercano(
            runtime.bateriaRepuestoPrivada,
            0.033f,
            "bateria de repuesto"
        );
        VerificarSinColisionEnBase(
            runtime.temperaturaRepuestoPrivada,
            "repuesto de temperatura"
        );
        VerificarSinColisionEnBase(
            runtime.bateriaRepuestoPrivada,
            "bateria de repuesto"
        );

        Exigir(
            runtime.textoAdvertencia != null &&
            runtime.textoAccion != null &&
            runtime.textoMensaje != null,
            "Faltan textos de la pantalla del monitor."
        );
        RectTransform advertencia =
            runtime.textoAdvertencia.rectTransform;
        RectTransform accion = runtime.textoAccion.rectTransform;
        RectTransform mensaje = runtime.textoMensaje.rectTransform;
        Exigir(
            advertencia.anchorMin.y > accion.anchorMax.y &&
            accion.anchorMin.y > mensaje.anchorMax.y,
            "Los textos de la pantalla del monitor se superponen."
        );

        Renderer[] bateriaRenderers =
            runtime.bateriaRepuestoPrivada != null
                ? runtime.bateriaRepuestoPrivada.GetComponentsInChildren<
                    Renderer
                >(true)
                : Array.Empty<Renderer>();
        Exigir(
            bateriaRenderers.Length > 0,
            "La bateria de repuesto no tiene modelo."
        );
        Bounds bateriaBounds = bateriaRenderers[0].bounds;
        for (int i = 1; i < bateriaRenderers.Length; i++)
            bateriaBounds.Encapsulate(bateriaRenderers[i].bounds);
        float largoHorizontal = Mathf.Max(
            bateriaBounds.size.x,
            bateriaBounds.size.z
        );
        Exigir(
            largoHorizontal > bateriaBounds.size.y * 1.35f,
            "La bateria de repuesto no quedo horizontal."
        );
        Exigir(
            runtime.panelHerramientas != null &&
            Mathf.Abs(Vector3.Dot(
                runtime.bateriaRepuestoPrivada.forward.normalized,
                runtime.panelHerramientas.up.normalized
            )) > 0.995f &&
            Mathf.Abs(Vector3.Dot(
                runtime.bateriaRepuestoPrivada.up.normalized,
                runtime.panelHerramientas.right.normalized
            )) > 0.995f,
            "La bateria de repuesto conserva una inclinacion torcida."
        );
        Exigir(
            runtime.bateriaRepuestoPrivada.localScale.x <= 0.665f,
            "La bateria de repuesto sigue demasiado grande."
        );
        Exigir(
            runtime.distanciaVentilador >= 0.20f &&
            runtime.distanciaVentilador <= 0.28f,
            "El ventilador no conserva el rango corto solicitado."
        );
        Transform calor = BuscarRecursivo(
            runtime.objetivoTemperatura,
            "CalorModuloFX"
        );
        Exigir(
            calor == null || !calor.gameObject.activeInHierarchy,
            "El efecto morado de temperatura sigue activo."
        );

        VerificarAcopleCargador(runtime);
        Quaternion rotacionAntes = runtime.robot.localRotation;
        runtime.AplicarEntradaRotacion(
            AlgoLabLevel3RobotLever.EjeRobot.InclinacionX,
            1f,
            1.2f
        );
        Exigir(
            Quaternion.Angle(
                rotacionAntes,
                runtime.robot.localRotation
            ) > 120f,
            "El eje X todavia esta limitado a una inclinacion corta."
        );
        runtime.robot.localRotation = rotacionAntes;
    }

    private static void VerificarPalanca(
        AlgoLabLevel3RobotLever palanca,
        string nombre)
    {
        Exigir(palanca != null, "Falta la palanca " + nombre + ".");
        Exigir(
            Vector3.Dot(
                palanca.ejeMovimientoEnPadre.normalized,
                Vector3.forward
            ) > 0.99f &&
            palanca.radioAgarre <= 0.029f &&
            palanca.distanciaVisualMaxima >= 0.074f &&
            palanca.distanciaMovimientoCompleto >= 0.119f,
            "La palanca " + nombre +
            " no sigue adelante/atras o permite agarre lejano."
        );
    }

    private static void VerificarPantallasBoton(
        AlgoLabLevel3RobotPracticeRuntime runtime,
        bool encendido)
    {
        Exigir(
            runtime.pantallaApagado != null &&
            runtime.pantallaEncendido != null &&
            runtime.pantallaApagado.activeSelf &&
            runtime.pantallaEncendido.activeSelf,
            "Se esta desactivando una pantalla completa del boton."
        );
        Exigir(
            runtime.textoEstadoApagado != null &&
            runtime.textoEstadoEncendido != null &&
            runtime.textoEstadoApagado.gameObject.activeSelf == !encendido &&
            runtime.textoEstadoEncendido.gameObject.activeSelf == encendido,
            "Los textos Encendido/Apagado no alternan correctamente."
        );
        Exigir(
            runtime.luzMonitorApagado != null &&
            runtime.luzMonitorEncendido != null &&
            runtime.luzMonitorApagado.gameObject.activeSelf == !encendido &&
            runtime.luzMonitorEncendido.gameObject.activeSelf == encendido,
            "Las luces Encendido/Apagado no alternan correctamente."
        );
    }

    private static void VerificarAgarreCercano(
        Transform objeto,
        float maximo,
        string nombre)
    {
        AlgoLabGrabProximityGate gate =
            objeto != null
                ? objeto.GetComponent<AlgoLabGrabProximityGate>()
                : null;
        Exigir(
            gate != null &&
            gate.distanciaMaximaSuperficie <= maximo &&
            !gate.exigirVidrioRoto,
            "El " + nombre + " no exige contacto fisico cercano."
        );
    }

    private static void VerificarAgarreCargadorCompleto(
        AlgoLabLevel3RobotPracticeRuntime runtime)
    {
        AlgoLabGrabProximityGate gate =
            runtime.cargador != null
                ? runtime.cargador.GetComponent<AlgoLabGrabProximityGate>()
                : null;
        Collider collider =
            runtime.cargador != null
                ? runtime.cargador.GetComponent<Collider>()
                : null;
        Exigir(
            gate != null &&
            collider != null &&
            !gate.usarSoloPuntoRespaldo &&
            gate.distanciaMaximaSuperficie >= 0.044f,
            "El cargador sigue limitado a un unico punto de agarre."
        );
        Exigir(
            gate.PuedeAgarrarseDesde(collider.bounds.center) &&
            gate.PuedeAgarrarseDesde(collider.bounds.min) &&
            gate.PuedeAgarrarseDesde(collider.bounds.max),
            "No se puede agarrar el cargador desde toda su superficie."
        );
    }

    private static void PrepararVozPrueba(
        AlgoLabLevel3RobotPracticeRuntime runtime)
    {
        GameObject go = new GameObject("VozRobotPrueba");
        go.transform.SetParent(runtime.transform, false);
        fuenteVozPrueba = go.AddComponent<AudioSource>();
        fuenteVozPrueba.clip = AudioClip.Create(
            "VozRobotPruebaClip",
            80000,
            1,
            8000,
            false
        );
        fuenteVozPrueba.volume = 1f;
        fuenteVozPrueba.pitch = 1f;
        fuenteVozPrueba.Play();
        runtime.NotificarInicioVozRobot(fuenteVozPrueba);
        muestraVozAlApagar = fuenteVozPrueba.timeSamples;
    }

    private static void VerificarNuevasReglasRobot(
        AlgoLabLevel3RobotPracticeRuntime runtime)
    {
        Exigir(
            Mathf.Abs(runtime.segundosAntesDeExplosion - 60f) < 0.01f,
            "El contador critico del robot no es de 60 segundos."
        );
        Exigir(
            runtime.textoAdvertencia == null ||
            !runtime.textoAdvertencia.text.Contains("NIVEL"),
            "El monitor sigue mostrando el temporizador global duplicado."
        );
        Exigir(
            runtime.textoMensaje == null ||
            !runtime.textoMensaje.text.Contains(":"),
            "El mensaje del monitor sigue mostrando el tiempo global."
        );

        runtime.SeleccionarAtributo("bateria");
        AlgoLabClassDiagramCardUI tarjeta =
            UnityEngine.Object.FindFirstObjectByType<
                AlgoLabClassDiagramCardUI
            >(FindObjectsInactive.Include);
        if (tarjeta != null)
        {
            Exigir(
                tarjeta.colorResaltadoTMP.a < 0.40f &&
                tarjeta.textoAtributos != null &&
                tarjeta.textoAtributos.text.Contains("#000000FF") &&
                tarjeta.textoAtributos.text.Contains("bateria"),
                "El resaltado del diagrama aun puede tapar las letras negras."
            );
        }

        float cerca = runtime.CalcularIntensidadVentilador(0.01f);
        float medio = runtime.CalcularIntensidadVentilador(
            runtime.distanciaVentilador * 0.55f
        );
        float fuera = runtime.CalcularIntensidadVentilador(
            runtime.distanciaVentilador + 0.01f
        );
        Exigir(
            cerca > medio && medio > 0f && Mathf.Approximately(fuera, 0f),
            "El enfriamiento no aumenta al acercar el ventilador."
        );

        Transform boca = BuscarRecursivo(runtime.robot, "LineaVozRobot");
        Exigir(
            boca != null &&
            boca.GetComponent<AlgoLabRobotMouthWaveform>() != null,
            "La boca del robot no tiene la linea de voz dinamica."
        );

        string pregunta = runtime.ConstruirPreguntaParaRobot("Como te reparo?");
        string respuesta = runtime.PrepararRespuestaDelRobot(
            "Necesito una reparacion segura."
        );
        Exigir(
            pregunta.Contains("puerto externo") &&
            pregunta.Contains("10 grados") &&
            respuesta.Contains("ttttienes") &&
            !respuesta.Contains("*"),
            "La IA no recibe el estado o la voz averiada del robot."
        );
    }

    private static void VerificarComponentesInternosAgarrables(
        AlgoLabLevel3RobotPracticeRuntime runtime)
    {
        VerificarComponenteInterno(
            runtime,
            "bateriaOriginal",
            "bateriaOriginalGrab",
            "vidrioBateria",
            "bateria"
        );
        VerificarComponenteInterno(
            runtime,
            "temperaturaOriginal",
            "temperaturaOriginalGrab",
            "vidrioTemperatura",
            "temperatura"
        );
    }

    private static void VerificarComponenteInterno(
        AlgoLabLevel3RobotPracticeRuntime runtime,
        string campoTransform,
        string campoGrab,
        string campoVidrio,
        string nombre)
    {
        BindingFlags flags =
            BindingFlags.Instance | BindingFlags.NonPublic;
        Transform componente = typeof(AlgoLabLevel3RobotPracticeRuntime)
            .GetField(campoTransform, flags)
            ?.GetValue(runtime) as Transform;
        SimpleMRGrabbable grab = typeof(AlgoLabLevel3RobotPracticeRuntime)
            .GetField(campoGrab, flags)
            ?.GetValue(runtime) as SimpleMRGrabbable;
        AlgoLabRobotBreakableGlass vidrio =
            typeof(AlgoLabLevel3RobotPracticeRuntime)
            .GetField(campoVidrio, flags)
            ?.GetValue(runtime) as AlgoLabRobotBreakableGlass;
        AlgoLabGrabProximityGate gate =
            componente != null
                ? componente.GetComponent<AlgoLabGrabProximityGate>()
                : null;
        Collider collider =
            componente != null
                ? componente.GetComponent<Collider>()
                : null;

        Exigir(
            componente != null && grab != null &&
            vidrio != null && gate != null && collider != null,
            "Falta la interaccion interna de " + nombre + "."
        );
        Exigir(
            !gate.PuedeAgarrarseDesde(collider.bounds.center),
            "El componente " + nombre + " se agarra con el vidrio intacto."
        );
        vidrio.Romper();
        Exigir(
            collider.enabled &&
            gate.PuedeAgarrarseDesde(collider.bounds.center),
            "El componente interno " + nombre +
            " no se puede agarrar despues de romper el vidrio."
        );
        GameObject mandoPrueba = new GameObject(
            "MandoPruebaComponente_" + nombre
        );
        mandoPrueba.transform.position = collider.bounds.center;
        SimpleOvRGrabber controlador =
            mandoPrueba.AddComponent<SimpleOvRGrabber>();
        controlador.grabPoint = mandoPrueba.transform;
        controlador.grabRadius = 0.065f;
        controlador.mostrarDebug = false;
        MethodInfo intentar = typeof(SimpleOvRGrabber).GetMethod(
            "TryGrab",
            BindingFlags.Instance | BindingFlags.NonPublic
        );
        Exigir(
            intentar != null,
            "No se encontro la rutina real de agarre."
        );
        intentar.Invoke(controlador, null);
        FieldInfo campoAgarrado = typeof(SimpleOvRGrabber).GetField(
            "heldObject",
            BindingFlags.Instance | BindingFlags.NonPublic
        );
        SimpleMRGrabbable agarrado =
            campoAgarrado?.GetValue(controlador) as SimpleMRGrabbable;
        Exigir(
            agarrado == grab && grab.IsGrabbed,
            "El mando no logra agarrar el componente interno " + nombre + "."
        );
        controlador.SoltarSiEstaAgarrando(grab);
        mandoPrueba.SetActive(false);
        UnityEngine.Object.Destroy(mandoPrueba);
        vidrio.ReiniciarVidrio();
    }

    private static void VerificarSinColisionEnBase(
        Transform objeto,
        string nombre)
    {
        SimpleMRGrabbable grab =
            objeto != null ? objeto.GetComponent<SimpleMRGrabbable>() : null;
        Exigir(
            grab != null &&
            grab.sinColisionFisica &&
            grab.sinColisionInicialHastaPrimerAgarre &&
            grab.sinColisionSoloCuandoNoAgarrado &&
            grab.mantenerColliderNormalParaAgarre,
            "El " + nombre +
            " conserva colision fisica antes de ser agarrado."
        );
    }

    private static void VerificarAcopleCargador(
        AlgoLabLevel3RobotPracticeRuntime runtime)
    {
        Exigir(
            runtime.cargador != null &&
            runtime.puntaCargador != null &&
            runtime.puertoCarga != null,
            "Faltan referencias del cargador."
        );
        Exigir(
            runtime.puertoCarga.name == "compartimientoCargar",
            "El cargador no usa el objeto vacio compartimientoCargar."
        );

        Vector3 posicion = runtime.cargador.position;
        Quaternion rotacion = runtime.cargador.rotation;
        MethodInfo encajar = typeof(AlgoLabLevel3RobotPracticeRuntime)
            .GetMethod(
                "EncajarCargador",
                BindingFlags.Instance | BindingFlags.NonPublic
            );
        Exigir(encajar != null, "No existe la rutina de acople del cargador.");
        encajar.Invoke(runtime, null);

        Exigir(
            Vector3.Distance(
                runtime.puntaCargador.position,
                runtime.puertoCarga.position +
                runtime.puertoCarga.forward *
                runtime.profundidadInsercionCargador
            ) < 0.001f,
            "El cargador no queda insertado a la profundidad configurada."
        );
        Vector3 eje = (
            runtime.puntaCargador.position -
            runtime.cargador.position
        ).normalized;
        Exigir(
            Vector3.Dot(eje, runtime.puertoCarga.forward) > 0.995f,
            "El cargador encaja con una orientacion incorrecta."
        );

        runtime.cargador.position = posicion;
        runtime.cargador.rotation = rotacion;
        if (runtime.cargadorGrab != null &&
            runtime.cargadorGrab.Rigidbody != null)
        {
            runtime.cargadorGrab.Rigidbody.isKinematic = true;
            runtime.cargadorGrab.Rigidbody.useGravity = false;
        }
    }

    private static void PrepararCargaFisica(
        AlgoLabLevel3RobotPracticeRuntime runtime)
    {
        Exigir(
            runtime.cargadorGrab != null,
            "Falta el componente agarrable del cargador."
        );
        runtime.cargadorGrab.BeginGrab();
        runtime.cargador.position +=
            runtime.puertoCarga.position -
            runtime.puntaCargador.position;
    }

    private static void PrepararEnfriamientoFisico(
        AlgoLabLevel3RobotPracticeRuntime runtime)
    {
        Exigir(
            runtime.ventiladorGrab != null &&
            runtime.ventilador != null &&
            runtime.objetivoTemperatura != null,
            "Faltan referencias fisicas del ventilador."
        );
        runtime.ventiladorGrab.BeginGrab();
        Transform punto = runtime.aspasVentilador != null
            ? runtime.aspasVentilador
            : runtime.ventilador;
        runtime.ventilador.position +=
            runtime.objetivoTemperatura.position - punto.position;
    }

    private static void VerificarPartesExplosionAgarrables(
        AlgoLabLevel3RobotPracticeRuntime runtime)
    {
        string[] nombres =
        {
            "Brazo.L", "Brazo.R", "cabeza",
            "pierna.L", "pierna.R", "torso"
        };
        for (int i = 0; i < nombres.Length; i++)
        {
            Transform parte = BuscarRecursivo(runtime.transform, nombres[i]);
            Exigir(
                parte != null &&
                parte.GetComponent<Rigidbody>() != null &&
                parte.GetComponent<Collider>() != null &&
                parte.GetComponent<SimpleMRGrabbable>() != null,
                "La parte explotada " + nombres[i] +
                " no quedo fisica y agarrable."
            );
        }
    }

    private static void VerificarTextoReintentar(
        AlgoLabLevel3RobotPracticeRuntime runtime)
    {
        TMP_Text texto = runtime.botonReintentar != null
            ? runtime.botonReintentar.GetComponentInChildren<TMP_Text>(true)
            : null;
        Exigir(
            texto != null &&
            texto.color.grayscale < 0.2f &&
            texto.color.a > 0.95f,
            "El texto Reintentar no tiene contraste oscuro."
        );
    }

    private static Transform BuscarRecursivo(Transform raiz, string nombre)
    {
        if (raiz == null)
            return null;
        if (string.Equals(
                raiz.name,
                nombre,
                StringComparison.OrdinalIgnoreCase))
        {
            return raiz;
        }
        for (int i = 0; i < raiz.childCount; i++)
        {
            Transform encontrado = BuscarRecursivo(raiz.GetChild(i), nombre);
            if (encontrado != null)
                return encontrado;
        }
        return null;
    }

    private static void Fallar(string mensaje)
    {
        resultado = "FALLO\n" + mensaje;
        Directory.CreateDirectory("Logs");
        File.WriteAllText(ResultPath, resultado);
        finalizado = true;
        if (EditorApplication.isPlaying)
            EditorApplication.isPlaying = false;
        else
            TerminarProceso();
    }

    private static void TerminarProceso()
    {
        bool ok = resultado.StartsWith("OK", StringComparison.Ordinal);
        EditorPrefs.DeleteKey(ActiveKey);
        EditorApplication.playModeStateChanged -= AlCambiarModo;
        EditorApplication.update -= Tick;
        AssetDatabase.DeleteAsset(
            "Assets/Scenes/__Level3RobotRuntimeTest.unity"
        );
        AssetDatabase.DeleteAsset(
            "Assets/Scenes/__Level3RobotRuntimeTest.unity.meta"
        );
        AssetDatabase.Refresh();
        Debug.Log(
            ok
                ? "ALGOLAB_LEVEL3_PLAYMODE_OK"
                : "ALGOLAB_LEVEL3_PLAYMODE_FAILED: " + resultado
        );
        EditorApplication.Exit(ok ? 0 : 1);
    }
}
#endif
