#if UNITY_EDITOR
using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Ensambla el prefab ejecutable de la práctica 3 usando exclusivamente el
/// monitor y el robot editables guardados por el diseñador.
/// </summary>
public static class AlgoLabLevel3PracticeAssembler
{
    // Distribucion final delante del usuario. Se aplica solo al prefab
    // generado; los objetos editables del disenador permanecen intactos.
    private static readonly Vector3 PosicionPanel =
        new Vector3(0.22f, -0.50f, 0.20f);
    private static readonly Quaternion RotacionPanel =
        Quaternion.identity;
    private static readonly Vector3 PosicionRobot =
        new Vector3(0.22f, 0.46f, 0.54f);
    private const float EscalaPanel = 0.88f;
    private const float EscalaRobot = 0.76f;
    private const float EscalaBateriaRepuesto = 0.66f;

    private const string MonitorScene =
        "Assets/Scenes/Nivel3_Monitor_Editable.unity";
    private const string RobotPrefab =
        "Assets/__Algolab/Resources/Level3/RobotWorkshop/Prefabs/RobotNivel3Editado.prefab";
    private const string OutputPrefab =
        "Assets/__Algolab/Resources/Level3/AlgoLabRobotPractice.prefab";

    [MenuItem("AlgoLab/Nivel 3/Ensamblar practica del robot")]
    public static void Run()
    {
        Scene escena = EditorSceneManager.OpenScene(
            MonitorScene,
            OpenSceneMode.Single
        );

        GameObject monitorFuente = BuscarRaiz(
            escena,
            "MonitorNivel3_EDITABLE"
        );
        GameObject cargadorFuente = BuscarRaiz(escena, "Cargador");
        GameObject temperaturaFuente = BuscarRaiz(
            escena,
            "temperaturaRespaldo"
        );
        GameObject ventiladorFuente = BuscarRaiz(escena, "ventilador");
        GameObject robotAsset = AssetDatabase.LoadAssetAtPath<GameObject>(
            RobotPrefab
        );

        if (monitorFuente == null ||
            cargadorFuente == null ||
            temperaturaFuente == null ||
            ventiladorFuente == null ||
            robotAsset == null)
        {
            throw new InvalidOperationException(
                "No se encontraron todos los objetos editables del nivel 3."
            );
        }

        GameObject root = new GameObject("AlgoLabRobotPractice");
        AlgoLabEncapsulationRobotPractice practica =
            root.AddComponent<AlgoLabEncapsulationRobotPractice>();
        AlgoLabLevel3RobotPracticeRuntime runtime =
            root.AddComponent<AlgoLabLevel3RobotPracticeRuntime>();
        practica.energiaInicial = 25;
        practica.temperaturaInicial = 85;
        practica.energiaMinimaEncendido = 100;
        practica.temperaturaObjetivo = 10;
        practica.temperaturaMaximaEncendido = 10;
        practica.esperaAntesDeCompletarNivel = 5.5f;
        practica.penalizacionAccesoPrivado = 10;
        runtime.segundosAntesDeExplosion = 60f;
        runtime.duracionMaximaPractica = 300f;
        runtime.distanciaConexionCargador = 0.05f;
        runtime.distanciaDesconexionCargador = 0.18f;
        runtime.distanciaVentilador = 0.24f;
        runtime.profundidadInsercionCargador = 0.105f;
        runtime.velocidadRotacionRobot = 82f;

        GameObject visual = new GameObject("RobotPracticeVisual");
        visual.transform.SetParent(root.transform, false);
        visual.AddComponent<AlgoLabRobotWorkshopVisual>();

        GameObject robot = PrefabUtility.InstantiatePrefab(
            robotAsset,
            visual.transform
        ) as GameObject;
        if (robot == null)
            throw new InvalidOperationException("No se pudo instanciar el robot.");
        robot.name = "Robot";
        robot.transform.localPosition = PosicionRobot;
        robot.transform.localScale = Vector3.one * EscalaRobot;

        GameObject panel = new GameObject("PanelHerramientasPublicas");
        panel.transform.SetParent(visual.transform, false);
        panel.transform.localPosition = PosicionPanel;
        panel.transform.localRotation = RotacionPanel;
        panel.transform.localScale = Vector3.one * EscalaPanel;

        GameObject monitor = UnityEngine.Object.Instantiate(monitorFuente);
        monitor.name = "MonitorNivel3_EDITABLE";
        ReparentConLocalOriginal(monitor.transform, panel.transform);

        GameObject cargador = UnityEngine.Object.Instantiate(cargadorFuente);
        cargador.name = "MetodoPublico_Cargar";
        ReparentConLocalOriginal(cargador.transform, panel.transform);
        PrepararAgarrable(cargador, 0.18f);

        GameObject ventilador = UnityEngine.Object.Instantiate(ventiladorFuente);
        ventilador.name = "MetodoPublico_Enfriar_Ventilador";
        ReparentConLocalOriginal(ventilador.transform, panel.transform);
        Transform aspas = BuscarRecursivo(ventilador.transform, "aspas");
        if (aspas != null)
            aspas.name = "Aspas";
        PrepararAgarrable(ventilador, 0.12f);

        GameObject temperaturaRepuesto =
            UnityEngine.Object.Instantiate(temperaturaFuente);
        temperaturaRepuesto.name = "RepuestoPrivado_Temperatura";
        ReparentConLocalOriginal(temperaturaRepuesto.transform, panel.transform);
        PrepararAgarrable(temperaturaRepuesto, 0.10f, true);

        Transform modeloMonitor = BuscarRecursivo(
            monitor.transform,
            "ModeloMonitor"
        );
        ConfigurarPantallaMonitor(modeloMonitor);
        Transform punta = BuscarRecursivo(cargador.transform, "PuntaConector");
        if (punta == null)
        {
            GameObject puntaGo = new GameObject("PuntaConector");
            punta = puntaGo.transform;
            punta.SetParent(cargador.transform, false);
            punta.localPosition = new Vector3(0f, -3.05f, 0f);
            punta.localRotation = Quaternion.identity;
        }

        Transform botonTransform = BuscarRecursivo(
            modeloMonitor,
            "botonApagar"
        );
        AlgoLabLevel3PhysicalButton boton =
            PrepararBoton(botonTransform, runtime);
        AlgoLabLevel3RobotLever leverY = PrepararPalanca(
            BuscarRecursivo(modeloMonitor, "palanca1Y"),
            runtime,
            AlgoLabLevel3RobotLever.EjeRobot.GiroY,
            Vector3.right
        );
        Transform palancaXTransform = BuscarRecursivo(
            modeloMonitor,
            "palanca2X"
        );
        CorregirCarasPalancaX(palancaXTransform);
        AlgoLabLevel3RobotLever leverX = PrepararPalanca(
            palancaXTransform,
            runtime,
            AlgoLabLevel3RobotLever.EjeRobot.InclinacionX,
            Vector3.forward
        );
        PrepararColisionMesa(BuscarRecursivo(modeloMonitor, "mesa"));

        Transform spawnBateria = BuscarRecursivo(
            modeloMonitor,
            "spaunear bateria remplazo"
        );
        GameObject bateriaRepuesto = CrearBateriaRepuesto(
            robot.transform,
            panel.transform,
            spawnBateria
        );

        Button retry = CrearBotonReintentar(modeloMonitor);

        GameObject cableGo = new GameObject("CableDinamico");
        cableGo.transform.SetParent(panel.transform, false);
        cableGo.AddComponent<LineRenderer>();
        AlgoLabLevel3FlexibleCable cable =
            cableGo.AddComponent<AlgoLabLevel3FlexibleCable>();
        cable.extremoMonitor = BuscarRecursivo(
            modeloMonitor,
            "cableCargador2"
        );
        cable.extremoCargador = BuscarRecursivo(
            cargador.transform,
            "cable_cargador1"
        );

        runtime.practica = practica;
        runtime.visualRoot = visual.transform;
        runtime.robot = robot.transform;
        runtime.modeloRobot = BuscarRecursivo(
            robot.transform,
            "ModeloRobotRigged"
        );
        runtime.panelHerramientas = panel.transform;
        runtime.modeloMonitor = modeloMonitor;
        runtime.cargador = cargador.transform;
        runtime.puntaCargador = punta;
        runtime.anclaCableMonitor = cable.extremoMonitor;
        runtime.anclaCableCargador = cable.extremoCargador;
        runtime.ventilador = ventilador.transform;
        runtime.aspasVentilador = aspas;
        runtime.puertoCarga = BuscarRecursivo(
            robot.transform,
            "compartimientoCargar"
        );
        if (runtime.puertoCarga == null)
        {
            runtime.puertoCarga = BuscarRecursivo(
                robot.transform,
                "PuertoCarga"
            );
        }
        runtime.objetivoTemperatura = BuscarRecursivo(
            robot.transform,
            "ObjetivoModuloTemperatura"
        );
        runtime.botonEnergia = boton;
        runtime.palancaX = leverX;
        runtime.palancaY = leverY;
        runtime.cable = cable;
        runtime.botonReintentar = retry;
        runtime.bateriaRepuestoPrivada = bateriaRepuesto != null
            ? bateriaRepuesto.transform
            : null;
        runtime.temperaturaRepuestoPrivada = temperaturaRepuesto.transform;

        PrefabUtility.SaveAsPrefabAsset(root, OutputPrefab);
        UnityEngine.Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        ValidarPrefab();
        Debug.Log(
            "ALGOLAB_LEVEL3_ASSEMBLY_OK: " + OutputPrefab
        );
    }

    [MenuItem("AlgoLab/Nivel 3/Validar practica del robot")]
    public static void ValidarPrefab()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            OutputPrefab
        );
        if (prefab == null)
            throw new InvalidOperationException("No existe el prefab final.");

        string[] obligatorios =
        {
            "RobotPracticeVisual",
            "Robot",
            "PanelHerramientasPublicas",
            "MonitorNivel3_EDITABLE",
            "MetodoPublico_Cargar",
            "MetodoPublico_Enfriar_Ventilador",
            "RepuestoPrivado_Bateria",
            "RepuestoPrivado_Temperatura",
            "botonApagar",
            "palanca1Y",
            "palanca2X",
            "PuertoCarga",
            "ObjetivoModuloTemperatura",
            "BotonReintentar"
        };

        for (int i = 0; i < obligatorios.Length; i++)
        {
            if (BuscarRecursivo(prefab.transform, obligatorios[i]) == null)
            {
                throw new InvalidOperationException(
                    "Falta en el prefab final: " + obligatorios[i]
                );
            }
        }

        if (prefab.GetComponent<AlgoLabEncapsulationRobotPractice>() == null ||
            prefab.GetComponent<AlgoLabLevel3RobotPracticeRuntime>() == null)
        {
            throw new InvalidOperationException(
                "El prefab no contiene los controladores de práctica."
            );
        }

        Transform robot = BuscarRecursivo(prefab.transform, "Robot");
        Transform panel = BuscarRecursivo(
            prefab.transform,
            "PanelHerramientasPublicas"
        );
        if (robot == null ||
            Vector3.Distance(robot.localPosition, PosicionRobot) > 0.001f ||
            Vector3.Distance(
                robot.localScale,
                Vector3.one * EscalaRobot
            ) > 0.001f)
        {
            throw new InvalidOperationException(
                "La escala final del robot no coincide con la distribucion."
            );
        }

        if (panel == null ||
            Vector3.Distance(panel.localPosition, PosicionPanel) > 0.001f ||
            Quaternion.Angle(panel.localRotation, RotacionPanel) > 0.1f ||
            Vector3.Distance(
                panel.localScale,
                Vector3.one * EscalaPanel
            ) > 0.001f)
        {
            throw new InvalidOperationException(
                "La posicion, orientacion o escala del monitor es incorrecta."
            );
        }
    }

    private static GameObject CrearBateriaRepuesto(
        Transform robot,
        Transform panel,
        Transform spawn)
    {
        Transform bateriaOriginal = BuscarRecursivo(
            robot,
            "BateriaExtraible"
        );
        if (bateriaOriginal == null || bateriaOriginal.childCount == 0)
            return null;

        GameObject root = new GameObject("RepuestoPrivado_Bateria");
        root.transform.SetParent(panel, false);
        if (spawn != null)
        {
            root.transform.position = spawn.position;
            // Ignorar la inclinacion heredada del marcador del monitor. La
            // bateria queda plana, horizontal y alineada con el panel.
            root.transform.rotation =
                panel.rotation *
                Quaternion.AngleAxis(90f, Vector3.forward) *
                Quaternion.AngleAxis(90f, Vector3.up);
        }
        root.transform.localScale = Vector3.one * EscalaBateriaRepuesto;

        Transform modeloOriginal = bateriaOriginal.GetChild(0);
        GameObject modelo = UnityEngine.Object.Instantiate(
            modeloOriginal.gameObject,
            root.transform,
            false
        );
        modelo.name = "ModeloBateriaRepuesto";
        CentrarVisualEnPivote(root.transform, modelo.transform);
        PrepararAgarrable(root, 0.16f, true);
        return root;
    }

    private static void CentrarVisualEnPivote(
        Transform pivote,
        Transform visual)
    {
        if (pivote == null || visual == null)
            return;
        Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);
        visual.position += pivote.position - bounds.center;
    }

    private static Button CrearBotonReintentar(Transform modeloMonitor)
    {
        Transform pantalla = BuscarRecursivo(
            BuscarRecursivo(modeloMonitor, "monitorpc"),
            "pantalla"
        );
        if (pantalla == null)
            return null;

        Transform existente = BuscarRecursivo(pantalla, "BotonReintentar");
        if (existente != null)
            UnityEngine.Object.DestroyImmediate(existente.gameObject);

        GameObject go = new GameObject(
            "BotonReintentar",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button)
        );
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(pantalla, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(7.5f, 1.45f);
        rect.anchoredPosition = new Vector2(0f, -2.7f);

        Image image = go.GetComponent<Image>();
        image.color = new Color(0.05f, 0.90f, 0.57f, 1f);
        Button button = go.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.7f, 1f, 0.9f);
        colors.pressedColor = new Color(0.35f, 0.85f, 0.67f);
        button.colors = colors;

        GameObject textoGo = new GameObject(
            "Texto",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI)
        );
        RectTransform textoRect = textoGo.GetComponent<RectTransform>();
        textoRect.SetParent(rect, false);
        textoRect.anchorMin = Vector2.zero;
        textoRect.anchorMax = Vector2.one;
        textoRect.offsetMin = Vector2.zero;
        textoRect.offsetMax = Vector2.zero;
        TextMeshProUGUI texto = textoGo.GetComponent<TextMeshProUGUI>();
        texto.text = "REINTENTAR";
        texto.alignment = TextAlignmentOptions.Center;
        texto.enableAutoSizing = true;
        texto.fontSizeMin = 0.45f;
        texto.fontSizeMax = 1.15f;
        texto.fontStyle = FontStyles.Bold;
        texto.color = new Color(0.005f, 0.025f, 0.020f, 1f);
        go.SetActive(false);
        return button;
    }

    private static AlgoLabLevel3PhysicalButton PrepararBoton(
        Transform boton,
        AlgoLabLevel3RobotPracticeRuntime runtime)
    {
        if (boton == null)
            return null;
        BoxCollider box = boton.GetComponent<BoxCollider>();
        if (box == null)
            box = boton.gameObject.AddComponent<BoxCollider>();
        AjustarBoxColliderAVisuales(boton, box);
        AlgoLabLevel3PhysicalButton componente =
            boton.GetComponent<AlgoLabLevel3PhysicalButton>();
        if (componente == null)
            componente = boton.gameObject.AddComponent<AlgoLabLevel3PhysicalButton>();
        componente.runtime = runtime;
        componente.superficie = box;
        componente.ejePresionLocal = Vector3.down;
        componente.recorrido = 0.035f;
        componente.radioContacto = 0.035f;
        return componente;
    }

    private static AlgoLabLevel3RobotLever PrepararPalanca(
        Transform palanca,
        AlgoLabLevel3RobotPracticeRuntime runtime,
        AlgoLabLevel3RobotLever.EjeRobot eje,
        Vector3 ejeVisual)
    {
        if (palanca == null)
            return null;
        BoxCollider box = palanca.GetComponent<BoxCollider>();
        if (box == null)
            box = palanca.gameObject.AddComponent<BoxCollider>();
        AjustarBoxColliderAVisuales(palanca, box);
        AlgoLabLevel3RobotLever componente =
            palanca.GetComponent<AlgoLabLevel3RobotLever>();
        if (componente == null)
            componente = palanca.gameObject.AddComponent<AlgoLabLevel3RobotLever>();
        componente.runtime = runtime;
        componente.ejeRobot = eje;
        componente.zonaAgarre = box;
        componente.ejeMovimientoEnPadre = Vector3.forward;
        componente.ejeRotacionVisualLocal = ejeVisual;
        componente.distanciaMovimientoCompleto = 0.12f;
        componente.distanciaVisualMaxima = 0.075f;
        componente.radioAgarre = 0.028f;
        return componente;
    }

    private static void CorregirCarasPalancaX(Transform palanca)
    {
        if (palanca == null)
            return;

        const string folder =
            "Assets/__Algolab/Resources/Level3/RobotWorkshop/Generated";
        AsegurarCarpetaAsset(folder);

        MeshFilter[] filtros =
            palanca.GetComponentsInChildren<MeshFilter>(true);
        for (int indiceFiltro = 0; indiceFiltro < filtros.Length; indiceFiltro++)
        {
            MeshFilter filtro = filtros[indiceFiltro];
            Mesh origen = filtro != null ? filtro.sharedMesh : null;
            if (origen == null)
                continue;

            Mesh corregida = UnityEngine.Object.Instantiate(origen);
            corregida.name = "Palanca2X_CarasCorregidas_" + indiceFiltro;
            for (int subMesh = 0; subMesh < corregida.subMeshCount; subMesh++)
            {
                int[] triangulos = corregida.GetTriangles(subMesh);
                for (int i = 0; i + 2 < triangulos.Length; i += 3)
                {
                    int temporal = triangulos[i + 1];
                    triangulos[i + 1] = triangulos[i + 2];
                    triangulos[i + 2] = temporal;
                }
                corregida.SetTriangles(triangulos, subMesh);
            }
            corregida.RecalculateNormals();
            corregida.RecalculateTangents();
            corregida.RecalculateBounds();

            string assetPath =
                folder + "/Palanca2X_CarasCorregidas_" +
                indiceFiltro + ".asset";
            Mesh asset = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
            if (asset == null)
            {
                AssetDatabase.CreateAsset(corregida, assetPath);
                asset = corregida;
            }
            else
            {
                EditorUtility.CopySerialized(corregida, asset);
                UnityEngine.Object.DestroyImmediate(corregida);
                EditorUtility.SetDirty(asset);
            }
            filtro.sharedMesh = asset;
        }
    }

    private static void AsegurarCarpetaAsset(string ruta)
    {
        string[] partes = ruta.Split('/');
        string actual = partes[0];
        for (int i = 1; i < partes.Length; i++)
        {
            string siguiente = actual + "/" + partes[i];
            if (!AssetDatabase.IsValidFolder(siguiente))
                AssetDatabase.CreateFolder(actual, partes[i]);
            actual = siguiente;
        }
    }

    private static void PrepararAgarrable(
        GameObject go,
        float masa,
        bool sinColisionEnBase = false)
    {
        BoxCollider box = go.GetComponent<BoxCollider>();
        if (box == null)
            box = go.AddComponent<BoxCollider>();
        AjustarBoxColliderAVisuales(go.transform, box);

        Rigidbody rb = go.GetComponent<Rigidbody>();
        if (rb == null)
            rb = go.AddComponent<Rigidbody>();
        rb.mass = masa;
        rb.useGravity = false;
        rb.isKinematic = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.linearDamping = 0.25f;
        rb.angularDamping = 0.30f;

        SimpleMRGrabbable grab = go.GetComponent<SimpleMRGrabbable>();
        if (grab == null)
            grab = go.AddComponent<SimpleMRGrabbable>();
        grab.perfilUso = SimpleMRGrabbable.PerfilUso.Personalizado;
        grab.releaseMode = SimpleMRGrabbable.ReleaseMode.Physics;
        grab.useGravityOnRelease = true;
        grab.conservarImpulsoAlSoltar = true;
        grab.mostrarDebug = false;
        if (sinColisionEnBase)
        {
            grab.sinColisionFisica = true;
            grab.sinColisionInicialHastaPrimerAgarre = true;
            grab.congelarMientrasEsperaPrimerAgarre = true;
            grab.sinColisionSoloCuandoNoAgarrado = true;
            grab.usarTriggerParaNoColisionar = false;
            grab.mantenerColliderNormalParaAgarre = true;
            grab.desactivarGravedadCuandoNoColisiona = true;
            grab.ignorarColisionesSolidasDeEscena = true;
        }

        AlgoLabGrabProximityGate gate =
            go.GetComponent<AlgoLabGrabProximityGate>();
        if (gate == null)
            gate = go.AddComponent<AlgoLabGrabProximityGate>();
        gate.Configurar(0.030f, null, go.transform);
    }

    private static void ConfigurarPantallaMonitor(Transform modeloMonitor)
    {
        Transform pantalla = BuscarRecursivo(
            BuscarRecursivo(modeloMonitor, "monitorpc"),
            "pantalla"
        );
        if (pantalla == null)
            return;

        Image fondo = pantalla.GetComponent<Image>();
        if (fondo != null)
            fondo.color = new Color(0.01f, 0.025f, 0.035f, 0.98f);

        ConfigurarTextoPantalla(
            BuscarRecursivo(pantalla, "Advertencia"),
            new Vector2(0.05f, 0.69f),
            new Vector2(0.95f, 0.94f),
            0.50f,
            1.25f,
            new Color(0.08f, 0.95f, 1f)
        );
        ConfigurarTextoPantalla(
            BuscarRecursivo(pantalla, "Apagar"),
            new Vector2(0.05f, 0.39f),
            new Vector2(0.95f, 0.65f),
            0.46f,
            1.10f,
            Color.white
        );
        ConfigurarTextoPantalla(
            BuscarRecursivo(pantalla, "mensaje"),
            new Vector2(0.05f, 0.08f),
            new Vector2(0.95f, 0.34f),
            0.40f,
            0.92f,
            new Color(1f, 0.82f, 0.12f)
        );

        Transform alerta = BuscarRecursivo(pantalla, "Alerta");
        if (alerta != null)
            alerta.gameObject.SetActive(false);
    }

    private static void ConfigurarTextoPantalla(
        Transform transformTexto,
        Vector2 anchorMin,
        Vector2 anchorMax,
        float tamanoMinimo,
        float tamanoMaximo,
        Color color)
    {
        if (transformTexto == null)
            return;

        RectTransform rect = transformTexto as RectTransform;
        TMP_Text texto = transformTexto.GetComponent<TMP_Text>();
        if (rect == null || texto == null)
            return;

        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one;

        texto.enableAutoSizing = true;
        texto.fontSizeMin = tamanoMinimo;
        texto.fontSizeMax = tamanoMaximo;
        texto.alignment = TextAlignmentOptions.Center;
        texto.textWrappingMode = TextWrappingModes.Normal;
        texto.overflowMode = TextOverflowModes.Ellipsis;
        texto.color = color;
        texto.raycastTarget = false;
    }

    private static void PrepararColisionMesa(Transform mesa)
    {
        if (mesa == null || mesa.GetComponent<Collider>() != null)
            return;
        MeshFilter filter = mesa.GetComponent<MeshFilter>();
        if (filter != null && filter.sharedMesh != null)
        {
            MeshCollider collider = mesa.gameObject.AddComponent<MeshCollider>();
            collider.sharedMesh = filter.sharedMesh;
            collider.convex = false;
        }
    }

    private static void AjustarBoxColliderAVisuales(
        Transform raiz,
        BoxCollider box)
    {
        Renderer[] renderers = raiz.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return;

        Bounds local = new Bounds(
            raiz.InverseTransformPoint(renderers[0].bounds.center),
            Vector3.zero
        );
        for (int i = 0; i < renderers.Length; i++)
        {
            Bounds b = renderers[i].bounds;
            Vector3 c = b.center;
            Vector3 e = b.extents;
            for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
            for (int z = -1; z <= 1; z += 2)
            {
                local.Encapsulate(
                    raiz.InverseTransformPoint(
                        c + Vector3.Scale(e, new Vector3(x, y, z))
                    )
                );
            }
        }
        box.center = local.center;
        box.size = local.size;
    }

    private static void ReparentConLocalOriginal(
        Transform objeto,
        Transform nuevoPadre)
    {
        Vector3 posicion = objeto.localPosition;
        Quaternion rotacion = objeto.localRotation;
        Vector3 escala = objeto.localScale;
        objeto.SetParent(nuevoPadre, false);
        objeto.localPosition = posicion;
        objeto.localRotation = rotacion;
        objeto.localScale = escala;
    }

    private static GameObject BuscarRaiz(Scene escena, string nombre)
    {
        GameObject[] roots = escena.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (string.Equals(
                    roots[i].name,
                    nombre,
                    StringComparison.OrdinalIgnoreCase))
                return roots[i];
        }
        return null;
    }

    private static Transform BuscarRecursivo(Transform raiz, string nombre)
    {
        if (raiz == null)
            return null;
        if (string.Equals(
                raiz.name,
                nombre,
                StringComparison.OrdinalIgnoreCase))
            return raiz;
        for (int i = 0; i < raiz.childCount; i++)
        {
            Transform encontrado = BuscarRecursivo(raiz.GetChild(i), nombre);
            if (encontrado != null)
                return encontrado;
        }
        return null;
    }
}
#endif
