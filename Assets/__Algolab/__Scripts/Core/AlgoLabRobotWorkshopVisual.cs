using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Construye la estacion 3D de la practica de encapsulamiento.
/// Todo se genera con geometria ligera para mantener buen rendimiento en Quest.
/// </summary>
public class AlgoLabRobotWorkshopVisual : MonoBehaviour
{
    private const string RecursoRobot =
        "Level3/RobotWorkshop/Models/Robot/AlgoLabRobot";
    private const string RecursoBateria =
        "Level3/RobotWorkshop/Models/Battery/Battery_Small";
    private const string RecursoModuloTemperatura =
        "Level3/RobotWorkshop/Models/Temperature/AlgoLabTemperatureModule";
    private const string TexturaTorso =
        "Level3/RobotWorkshop/Textures/Robot/RobotTexture";
    private const string TexturaCabeza =
        "Level3/RobotWorkshop/Textures/Robot/CabezaImagen";
    private const string TexturaBrazoIzquierdo =
        "Level3/RobotWorkshop/Textures/Robot/TextureBrazoL";
    private const string TexturaBrazoDerecho =
        "Level3/RobotWorkshop/Textures/Robot/BrazoR";
    private const string TexturaPiernas =
        "Level3/RobotWorkshop/Textures/Robot/PiernaImagen";

    private AlgoLabEncapsulationRobotPractice practica;
    private bool construido;

    private Transform robot;
    private Transform modeloRobot;
    private Transform moduloTemperaturaObjetivo;
    private Transform puertoCargaObjetivo;
    private Transform ranuraBateriaObjetivo;
    private Transform bateriaPadreDock;
    private Transform moduloPadreDock;
    private Transform aspasVentilador;
    private Transform puntaCargador;
    private Transform bateriaRepuesto;
    private Transform moduloRepuesto;

    private SimpleMRGrabbable ventiladorGrab;
    private SimpleMRGrabbable cargadorGrab;
    private SimpleMRGrabbable bateriaGrab;
    private SimpleMRGrabbable moduloGrab;

    private Vector3 bateriaDockLocal;
    private Quaternion bateriaDockRotation;
    private Vector3 moduloDockLocal;
    private Quaternion moduloDockRotation;
    private Vector3 ventiladorDockLocal;
    private Quaternion ventiladorDockRotation;
    private Vector3 cargadorDockLocal;
    private Quaternion cargadorDockRotation;

    private Renderer[] ojos;
    private Renderer luzEstado;
    private Renderer bateriaInterna;
    private Renderer moduloTemperaturaInterno;
    private Renderer[] brilloTemperatura;
    private Renderer[] medidorBateria;
    private GameObject bateriaOriginalVisual;
    private GameObject moduloOriginalVisual;
    private bool bateriaAcoplada;
    private bool bateriaFueRetirada;
    private bool moduloAcoplado;
    private bool moduloFueRetirado;

    private TMP_Text estadoText;
    private TMP_Text puntajeText;
    private TMP_Text feedbackText;

    private AlgoLabRobotBreakableGlass vidrioTemperatura;
    private AlgoLabRobotBreakableGlass vidrioBateria;

    private Material matAzul;
    private Material matAzulClaro;
    private Material matOscuro;
    private Material matMetal;
    private Material matAmarillo;
    private Material matRojo;
    private Material matVerde;
    private Material matCian;
    private Material matVidrio;
    private Material matMarcoVidrio;
    private Material matPanel;
    private readonly List<Material> materiales = new List<Material>();

    public bool Construido => construido;
    public bool VidrioTemperaturaRoto => vidrioTemperatura != null && vidrioTemperatura.Roto;
    public bool VidrioBateriaRoto => vidrioBateria != null && vidrioBateria.Roto;
    public bool InteraccionesHerramientasExternas { get; set; }

    public void Inicializar(AlgoLabEncapsulationRobotPractice controlador)
    {
        practica = controlador;
        if (construido)
            return;

        if (transform.Find("Robot") != null &&
            transform.Find("PanelHerramientasPublicas") != null)
        {
            VincularJerarquiaEditable();
            return;
        }

        Construir();
    }

    public void Construir()
    {
        if (construido)
            return;

        CrearMateriales();
        ConstruirRobot();
        ConstruirPanelCurvo();
        ConstruirPantallaEstado();
        construido = true;
    }

    /// <summary>
    /// Reconecta la lógica con una jerarquía ya guardada en el prefab o en una
    /// escena de autoría. Así el diseñador puede mover el robot, los módulos y
    /// los vidrios desde el Inspector sin que se destruyan al ejecutar.
    /// </summary>
    private void VincularJerarquiaEditable()
    {
        CrearMateriales();

        robot = transform.Find("Robot");
        modeloRobot = robot != null ? robot.Find("ModeloRobotRigged") : null;
        if (modeloRobot != null)
        {
            AplicarTexturasRobot(modeloRobot.gameObject);
            AlgoLabRobotRigAxisConstraint restriccion =
                modeloRobot.GetComponent<AlgoLabRobotRigAxisConstraint>();
            if (restriccion == null)
            {
                restriccion =
                    modeloRobot.gameObject.AddComponent<AlgoLabRobotRigAxisConstraint>();
            }
            restriccion.ConfigurarAutomaticamente(modeloRobot);
        }

        Transform temperaturaRoot = robot != null
            ? robot.Find("CompartimientoTemperatura")
            : null;
        if (temperaturaRoot != null)
        {
            moduloTemperaturaObjetivo =
                temperaturaRoot.Find("ObjetivoModuloTemperatura");
            moduloPadreDock = temperaturaRoot;
            moduloRepuesto =
                temperaturaRoot.Find("ModuloTemperaturaExtraible");
            if (moduloRepuesto != null)
            {
                moduloOriginalVisual = moduloRepuesto.gameObject;
                moduloGrab = moduloRepuesto.GetComponent<SimpleMRGrabbable>();
                moduloDockLocal = moduloRepuesto.localPosition;
                moduloDockRotation = moduloRepuesto.localRotation;
                Renderer[] moduloRenderers =
                    moduloRepuesto.GetComponentsInChildren<Renderer>(true);
                if (moduloRenderers.Length > 0)
                    moduloTemperaturaInterno = moduloRenderers[0];
            }

            Transform cristalTransform =
                temperaturaRoot.Find("VidrioTemperatura");
            vidrioTemperatura = cristalTransform != null
                ? cristalTransform.GetComponent<AlgoLabRobotBreakableGlass>()
                : null;
            if (vidrioTemperatura != null)
            {
                Renderer cristal = cristalTransform.GetComponent<Renderer>();
                Collider collider = cristalTransform.GetComponent<Collider>();
                Transform fragmentosTransform =
                    temperaturaRoot.Find("FragmentosTemperatura");
                vidrioTemperatura.Configurar(
                    practica,
                    AlgoLabRobotBreakableGlass.Compartimiento.Temperatura,
                    cristal,
                    collider,
                    fragmentosTransform != null
                        ? fragmentosTransform.gameObject
                        : vidrioTemperatura.fragmentos
                );
            }

            if (moduloRepuesto != null)
            {
                AlgoLabGrabProximityGate gate =
                    moduloRepuesto.GetComponent<AlgoLabGrabProximityGate>();
                if (gate == null)
                    gate = moduloRepuesto.gameObject.AddComponent<AlgoLabGrabProximityGate>();
                gate.Configurar(0.020f, vidrioTemperatura, moduloRepuesto);
            }

            var brillos = new List<Renderer>();
            Renderer[] renderers =
                temperaturaRoot.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i].name.StartsWith("Calor"))
                    brillos.Add(renderers[i]);
            }
            brilloTemperatura = brillos.ToArray();
        }

        Transform bateriaRoot = robot != null
            ? robot.Find("CompartimientoBateriaTrasero")
            : null;
        if (bateriaRoot != null)
        {
            ranuraBateriaObjetivo = bateriaRoot.Find("RanuraBateria");
            puertoCargaObjetivo = bateriaRoot.Find("PuertoCarga");
            bateriaPadreDock = bateriaRoot;
            bateriaRepuesto = bateriaRoot.Find("BateriaExtraible");
            if (bateriaRepuesto != null)
            {
                bateriaOriginalVisual = bateriaRepuesto.gameObject;
                bateriaGrab = bateriaRepuesto.GetComponent<SimpleMRGrabbable>();
                bateriaDockLocal = bateriaRepuesto.localPosition;
                bateriaDockRotation = bateriaRepuesto.localRotation;
                Renderer[] bateriaRenderers =
                    bateriaRepuesto.GetComponentsInChildren<Renderer>(true);
                if (bateriaRenderers.Length > 0)
                    bateriaInterna = bateriaRenderers[0];
            }

            Transform cristalTransform = bateriaRoot.Find("VidrioBateria");
            vidrioBateria = cristalTransform != null
                ? cristalTransform.GetComponent<AlgoLabRobotBreakableGlass>()
                : null;
            if (vidrioBateria != null)
            {
                Renderer cristal = cristalTransform.GetComponent<Renderer>();
                Collider collider = cristalTransform.GetComponent<Collider>();
                Transform fragmentosTransform =
                    bateriaRoot.Find("FragmentosBateria");
                vidrioBateria.Configurar(
                    practica,
                    AlgoLabRobotBreakableGlass.Compartimiento.Bateria,
                    cristal,
                    collider,
                    fragmentosTransform != null
                        ? fragmentosTransform.gameObject
                        : vidrioBateria.fragmentos
                );
            }

            if (bateriaRepuesto != null)
            {
                AlgoLabGrabProximityGate gate =
                    bateriaRepuesto.GetComponent<AlgoLabGrabProximityGate>();
                if (gate == null)
                    gate = bateriaRepuesto.gameObject.AddComponent<AlgoLabGrabProximityGate>();
                gate.Configurar(0.020f, vidrioBateria, bateriaRepuesto);
            }
        }

        Transform panel = transform.Find("PanelHerramientasPublicas");
        if (panel != null)
        {
            Transform ventilador =
                panel.Find("MetodoPublico_Enfriar_Ventilador");
            if (ventilador != null)
            {
                ventiladorGrab = ventilador.GetComponent<SimpleMRGrabbable>();
                aspasVentilador = ventilador.Find("Aspas");
                ventiladorDockLocal = ventilador.localPosition;
                ventiladorDockRotation = ventilador.localRotation;
            }

            Transform cargador = panel.Find("MetodoPublico_Cargar");
            if (cargador != null)
            {
                cargadorGrab = cargador.GetComponent<SimpleMRGrabbable>();
                puntaCargador = cargador.Find("PuntaConector");
                cargadorDockLocal = cargador.localPosition;
                cargadorDockRotation = cargador.localRotation;
            }

            Button[] botones = panel.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < botones.Length; i++)
            {
                if (botones[i].name == "Metodo_Apagar")
                {
                    botones[i].onClick.RemoveListener(practica.MetodoApagar);
                    botones[i].onClick.AddListener(practica.MetodoApagar);
                }
                else if (botones[i].name == "Metodo_Encender")
                {
                    botones[i].onClick.RemoveListener(practica.MetodoEncender);
                    botones[i].onClick.AddListener(practica.MetodoEncender);
                }
            }
        }

        Transform pantalla = transform.Find("PantallaEstadoRobot");
        if (pantalla != null)
        {
            Transform estado = pantalla.Find("Estado");
            Transform puntaje = pantalla.Find("Puntaje");
            Transform feedback = pantalla.Find("Feedback");
            estadoText = estado != null ? estado.GetComponent<TMP_Text>() : null;
            puntajeText = puntaje != null ? puntaje.GetComponent<TMP_Text>() : null;
            feedbackText = feedback != null ? feedback.GetComponent<TMP_Text>() : null;
        }

        Transform luz = robot != null ? robot.Find("LuzEstado") : null;
        luzEstado = luz != null ? luz.GetComponent<Renderer>() : null;
        ojos = System.Array.Empty<Renderer>();
        medidorBateria = null;

        if (bateriaGrab != null)
        {
            bateriaGrab.OnGrabStarted -= AlAgarrarBateria;
            bateriaGrab.OnGrabEnded -= AlSoltarBateria;
            bateriaGrab.OnGrabStarted += AlAgarrarBateria;
            bateriaGrab.OnGrabEnded += AlSoltarBateria;
        }
        if (moduloGrab != null)
        {
            moduloGrab.OnGrabStarted -= AlAgarrarModulo;
            moduloGrab.OnGrabEnded -= AlSoltarModulo;
            moduloGrab.OnGrabStarted += AlAgarrarModulo;
            moduloGrab.OnGrabEnded += AlSoltarModulo;
        }

        bateriaAcoplada = bateriaGrab != null;
        moduloAcoplado = moduloGrab != null;
        construido = true;
        RefrescarEstado();
    }

    public void ReiniciarTaller()
    {
        if (!construido)
            return;

        if (vidrioTemperatura != null)
            vidrioTemperatura.ReiniciarVidrio();
        if (vidrioBateria != null)
            vidrioBateria.ReiniciarVidrio();

        ReiniciarReemplazosPrivados();
        DevolverObjeto(ventiladorGrab, ventiladorDockLocal, ventiladorDockRotation);
        DevolverObjeto(cargadorGrab, cargadorDockLocal, cargadorDockRotation);
        RefrescarEstado();
    }

    public void ReiniciarReemplazosPrivados()
    {
        if (bateriaOriginalVisual != null)
            bateriaOriginalVisual.SetActive(true);
        if (moduloOriginalVisual != null)
            moduloOriginalVisual.SetActive(true);

        DevolverObjeto(bateriaGrab, bateriaDockLocal, bateriaDockRotation);
        bateriaAcoplada = bateriaGrab != null;
        bateriaFueRetirada = false;
        DevolverObjeto(moduloGrab, moduloDockLocal, moduloDockRotation);
        moduloAcoplado = moduloGrab != null;
        moduloFueRetirado = false;
    }

    public void Tick(float deltaTime)
    {
        if (!construido || practica == null)
            return;

        float dt = Mathf.Max(0f, deltaTime);

        if (!InteraccionesHerramientasExternas &&
            ventiladorGrab != null && ventiladorGrab.IsGrabbed)
        {
            if (aspasVentilador != null)
                aspasVentilador.Rotate(0f, 0f, 920f * dt, Space.Self);

            if (moduloTemperaturaObjetivo != null &&
                Vector3.Distance(aspasVentilador.position, moduloTemperaturaObjetivo.position) <= 0.27f)
            {
                if (!VidrioTemperaturaRoto)
                    practica.NotificarHerramientaBloqueada("Rompe primero el vidrio frontal para exponer el modulo.");
                else
                    practica.AplicarEnfriamientoFisico(24f * dt);
            }
        }

        if (!InteraccionesHerramientasExternas &&
            cargadorGrab != null && cargadorGrab.IsGrabbed &&
            puntaCargador != null && puertoCargaObjetivo != null &&
            Vector3.Distance(puntaCargador.position, puertoCargaObjetivo.position) <= 0.16f)
        {
            practica.AplicarCargaFisica(30f * dt);
        }

        if (!InteraccionesHerramientasExternas)
        {
            ActualizarBateriaExtraible();
            ActualizarModuloExtraible();
        }
        RecuperarSiSePerdio(ventiladorGrab, ventiladorDockLocal, ventiladorDockRotation);
        RecuperarSiSePerdio(cargadorGrab, cargadorDockLocal, cargadorDockRotation);

        if (bateriaGrab != null && !bateriaGrab.IsGrabbed &&
            Vector3.Distance(bateriaGrab.transform.position, transform.position) > 3f)
        {
            AcoplarBateria(false);
        }

        if (moduloGrab != null && !moduloGrab.IsGrabbed &&
            Vector3.Distance(moduloGrab.transform.position, transform.position) > 3f)
        {
            AcoplarModulo(false);
        }
    }

    public void RefrescarEstado()
    {
        if (!construido || practica == null)
            return;

        float energia01 = Mathf.Clamp01(practica.Energia / 100f);
        float calor01 = Mathf.InverseLerp(
            practica.temperaturaMaximaEncendido,
            100f,
            practica.Temperatura
        );

        Color colorEnergia = Color.Lerp(
            new Color(1f, 0.08f, 0.05f),
            new Color(0.08f, 1f, 0.36f),
            energia01
        );
        AplicarColor(bateriaInterna, colorEnergia, true);

        if (medidorBateria != null)
        {
            int activos = Mathf.CeilToInt(energia01 * medidorBateria.Length);
            for (int i = 0; i < medidorBateria.Length; i++)
            {
                Color color = i < activos
                    ? Color.Lerp(new Color(1f, 0.05f, 0.03f), new Color(0.05f, 1f, 0.35f),
                        medidorBateria.Length <= 1 ? 1f : i / (float)(medidorBateria.Length - 1))
                    : new Color(0.08f, 0.10f, 0.12f);
                AplicarColor(medidorBateria[i], color, i < activos);
            }
        }

        Color calor = Color.Lerp(
            new Color(0.08f, 0.55f, 1f),
            new Color(1f, 0.035f, 0.015f),
            calor01
        );
        AplicarColor(moduloTemperaturaInterno, calor, true);
        if (brilloTemperatura != null)
        {
            for (int i = 0; i < brilloTemperatura.Length; i++)
                AplicarColor(brilloTemperatura[i], calor, calor01 > 0.08f);
        }

        Color colorOjos = practica.Encendido
            ? new Color(1f, 0.20f, 0.08f)
            : new Color(0.06f, 0.10f, 0.13f);
        if (ojos != null)
        {
            for (int i = 0; i < ojos.Length; i++)
                AplicarColor(ojos[i], colorOjos, practica.Encendido);
        }
        AplicarColor(
            luzEstado,
            practica.PracticaCompletada
                ? new Color(0.05f, 1f, 0.38f)
                : practica.Encendido
                    ? new Color(1f, 0.12f, 0.05f)
                    : new Color(1f, 0.72f, 0.04f),
            true
        );

        if (estadoText != null)
        {
            estadoText.text =
                "BATERIA  " + practica.Energia + "%     |     TEMPERATURA  " +
                practica.Temperatura + " C     |     " +
                (practica.Encendido ? "ENCENDIDO" : "APAGADO");
        }
        if (puntajeText != null)
            puntajeText.text = "PUNTOS  " + practica.Puntaje;
    }

    public void MostrarFeedback(string mensaje)
    {
        if (feedbackText != null)
            feedbackText.text = mensaje;
    }

    private void ConstruirRobot()
    {
        robot = CrearAncla("Robot", transform, new Vector3(0f, 0.12f, 0.22f));

        if (!ConstruirModeloRobotImportado())
            ConstruirRobotProvisional();

        luzEstado = CrearPrimitiva("LuzEstado", PrimitiveType.Sphere, robot,
            new Vector3(0.24f, 0.23f, -0.255f), Vector3.one * 0.04f,
            Quaternion.identity, matRojo);

        ConstruirCompartimientoTemperatura();
        ConstruirCompartimientoBateria();
    }

    private bool ConstruirModeloRobotImportado()
    {
        GameObject prefab = Resources.Load<GameObject>(RecursoRobot);
        if (prefab == null)
            return false;

        GameObject instance = Instantiate(prefab, robot, false);
        instance.name = "ModeloRobotRigged";
        modeloRobot = instance.transform;
        NormalizarModelo(
            modeloRobot,
            robot,
            1.72f,
            new Vector3(0f, 0.04f, 0f),
            0.36f
        );
        AplicarTexturasRobot(instance);

        AlgoLabRobotRigAxisConstraint rigConstraint =
            instance.GetComponent<AlgoLabRobotRigAxisConstraint>();
        if (rigConstraint == null)
            rigConstraint = instance.AddComponent<AlgoLabRobotRigAxisConstraint>();
        rigConstraint.limitePiernas = 40f;
        rigConstraint.ConfigurarAutomaticamente(instance.transform);

        ojos = new Renderer[0];
        return true;
    }

    private void AplicarTexturasRobot(GameObject instance)
    {
        if (instance == null)
            return;

        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            string nombre = renderer.name.ToLowerInvariant();
            string recurso = null;

            if (nombre.Contains("torso"))
                recurso = TexturaTorso;
            else if (nombre.Contains("cabeza"))
                recurso = TexturaCabeza;
            else if (nombre.Contains("brazo.l") ||
                     nombre.Contains("brazo_l") ||
                     nombre.Contains("arm.l"))
                recurso = TexturaBrazoIzquierdo;
            else if (nombre.Contains("brazo.r") ||
                     nombre.Contains("brazo_r") ||
                     nombre.Contains("arm.r"))
                recurso = TexturaBrazoDerecho;
            else if (nombre.Contains("pierna"))
                recurso = TexturaPiernas;

            Texture2D textura = !string.IsNullOrEmpty(recurso)
                ? Resources.Load<Texture2D>(recurso)
                : null;
            if (textura == null)
                continue;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Universal Render Pipeline/Simple Lit");
            if (shader == null)
                shader = Shader.Find("Standard");
            if (shader == null)
                continue;

            Material material = new Material(shader)
            {
                name = "RobotTexturizado_" + renderer.name,
                color = Color.white,
                enableInstancing = true
            };
            if (material.HasProperty("_BaseMap"))
                material.SetTexture("_BaseMap", textura);
            if (material.HasProperty("_MainTex"))
                material.SetTexture("_MainTex", textura);
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", Color.white);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", Color.white);
            if (material.HasProperty("_Metallic"))
                material.SetFloat("_Metallic", 0.05f);
            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", 0.48f);

            renderer.sharedMaterial = material;
            materiales.Add(material);
        }
    }

    private void ConstruirRobotProvisional()
    {
        CrearCajaRedondeada("Cabeza", robot, new Vector3(0f, 0.57f, 0f),
            new Vector3(0.46f, 0.34f, 0.36f), 0.065f, matAzulClaro);
        CrearPrimitiva("Antena", PrimitiveType.Cylinder, robot,
            new Vector3(0f, 0.82f, 0f), new Vector3(0.025f, 0.09f, 0.025f),
            Quaternion.identity, matMetal);
        CrearPrimitiva("PuntaAntena", PrimitiveType.Sphere, robot,
            new Vector3(0f, 0.92f, 0f), Vector3.one * 0.075f,
            Quaternion.identity, matAmarillo);

        Renderer ojoIzq = CrearPrimitiva("OjoIzquierdo", PrimitiveType.Cylinder, robot,
            new Vector3(-0.105f, 0.61f, -0.195f), new Vector3(0.067f, 0.018f, 0.067f),
            Quaternion.Euler(90f, 0f, 0f), matRojo);
        Renderer ojoDer = CrearPrimitiva("OjoDerecho", PrimitiveType.Cylinder, robot,
            new Vector3(0.105f, 0.61f, -0.195f), new Vector3(0.067f, 0.018f, 0.067f),
            Quaternion.Euler(90f, 0f, 0f), matRojo);
        ojos = new[] { ojoIzq, ojoDer };

        for (int i = 0; i < 5; i++)
        {
            CrearPrimitiva("RejillaBoca_" + i, PrimitiveType.Cube, robot,
                new Vector3(-0.12f + i * 0.06f, 0.48f, -0.192f),
                new Vector3(0.025f, 0.07f, 0.018f), Quaternion.identity, matMetal);
        }

        CrearCajaRedondeada("TorsoVertical", robot, new Vector3(0f, 0.02f, 0f),
            new Vector3(0.60f, 0.76f, 0.38f), 0.06f, matAzul);
        CrearPrimitiva("PlacaPecho", PrimitiveType.Cube, robot,
            new Vector3(0.15f, 0.12f, -0.205f), new Vector3(0.20f, 0.29f, 0.018f),
            Quaternion.identity, matAzulClaro);
        ConstruirExtremidades();
    }

    private void ActualizarBateriaExtraible()
    {
        if (bateriaGrab == null || ranuraBateriaObjetivo == null)
            return;

        float distancia = Vector3.Distance(
            bateriaGrab.transform.position,
            ranuraBateriaObjetivo.position
        );

        if (bateriaGrab.IsGrabbed)
        {
            bateriaAcoplada = false;
            if (distancia >= 0.13f)
                bateriaFueRetirada = true;
            return;
        }

        if (!bateriaAcoplada && bateriaFueRetirada &&
            VidrioBateriaRoto && distancia <= 0.12f)
        {
            AcoplarBateria(true);
        }
    }

    private void ActualizarModuloExtraible()
    {
        if (moduloGrab == null || moduloTemperaturaObjetivo == null)
            return;

        float distancia = Vector3.Distance(
            moduloGrab.transform.position,
            moduloTemperaturaObjetivo.position
        );

        if (moduloGrab.IsGrabbed)
        {
            moduloAcoplado = false;
            if (distancia >= 0.13f)
                moduloFueRetirado = true;
            return;
        }

        if (!moduloAcoplado && moduloFueRetirado &&
            VidrioTemperaturaRoto && distancia <= 0.12f)
        {
            AcoplarModulo(true);
        }
    }

    private void AlAgarrarBateria()
    {
        bateriaAcoplada = false;
    }

    private void AlSoltarBateria()
    {
        ActualizarBateriaExtraible();
    }

    private void AlAgarrarModulo()
    {
        moduloAcoplado = false;
    }

    private void AlSoltarModulo()
    {
        ActualizarModuloExtraible();
    }

    private void AcoplarBateria(bool notificarAccesoPrivado)
    {
        if (bateriaGrab == null)
            return;

        bool fueRetirada = bateriaFueRetirada;
        if (bateriaPadreDock != null)
            bateriaGrab.transform.SetParent(bateriaPadreDock, false);

        DevolverObjeto(bateriaGrab, bateriaDockLocal, bateriaDockRotation);
        bateriaAcoplada = true;
        bateriaFueRetirada = false;

        // Volver a colocar la bateria original no viola el encapsulamiento.
        // Solo los repuestos externos se validan como un reemplazo privado.
    }

    private void AcoplarModulo(bool notificarAccesoPrivado)
    {
        if (moduloGrab == null)
            return;

        bool fueRetirado = moduloFueRetirado;
        if (moduloPadreDock != null)
            moduloGrab.transform.SetParent(moduloPadreDock, false);

        DevolverObjeto(moduloGrab, moduloDockLocal, moduloDockRotation);
        moduloAcoplado = true;
        moduloFueRetirado = false;

        // Volver a colocar el modulo original es una restauracion valida.
        // El runtime externo distingue este caso de un repuesto incorrecto.
    }

    private static void NormalizarModelo(
        Transform modelo,
        Transform espacio,
        float alturaObjetivo,
        Vector3 centroObjetivo,
        float escalaRespaldo = 0f)
    {
        if (modelo == null || espacio == null)
            return;

        Quaternion rotacionImportada = modelo.localRotation;
        modelo.localPosition = Vector3.zero;
        modelo.localRotation = rotacionImportada;
        modelo.localScale = Vector3.one;
        if (escalaRespaldo > 0f)
            AsegurarOrientacionVertical(modelo, espacio, rotacionImportada);

        if (!IntentarCalcularBounds(espacio, modelo, out Bounds bounds) ||
            bounds.size.y <= 0.0001f)
        {
            return;
        }

        float escala = Mathf.Max(0.0001f, alturaObjetivo) / bounds.size.y;
        if (escalaRespaldo > 0f &&
            (escala > escalaRespaldo * 1.7f ||
             escala < escalaRespaldo * 0.45f))
        {
            escala = escalaRespaldo;
        }
        modelo.localScale = Vector3.one * escala;

        if (!IntentarCalcularBounds(espacio, modelo, out bounds))
            return;

        modelo.localPosition += centroObjetivo - bounds.center;
    }

    private static void AsegurarOrientacionVertical(
        Transform modelo,
        Transform espacio,
        Quaternion rotacionImportada)
    {
        Quaternion[] candidatos =
        {
            rotacionImportada,
            rotacionImportada * Quaternion.Euler(0f, 0f, 180f),
            rotacionImportada * Quaternion.Euler(90f, 0f, 0f),
            rotacionImportada * Quaternion.Euler(-90f, 0f, 0f),
            rotacionImportada * Quaternion.Euler(0f, 0f, 90f),
            rotacionImportada * Quaternion.Euler(0f, 0f, -90f)
        };

        Transform cabeza = BuscarTransformRecursivo(modelo, "head");
        Transform piernaIzquierda = BuscarTransformRecursivo(modelo, "Leg.L");
        Transform piernaDerecha = BuscarTransformRecursivo(modelo, "Leg.R");
        Quaternion mejor = rotacionImportada;
        float mejorPuntaje = float.NegativeInfinity;

        for (int i = 0; i < candidatos.Length; i++)
        {
            modelo.localRotation = candidatos[i];
            if (!IntentarCalcularBounds(espacio, modelo, out Bounds bounds))
                continue;

            float anchoMayor = Mathf.Max(bounds.size.x, bounds.size.z);
            float puntaje = bounds.size.y * 3f - anchoMayor;
            if (cabeza != null &&
                piernaIzquierda != null &&
                piernaDerecha != null)
            {
                float alturaCabeza =
                    espacio.InverseTransformPoint(cabeza.position).y;
                float alturaPiernas =
                    (
                        espacio.InverseTransformPoint(piernaIzquierda.position).y +
                        espacio.InverseTransformPoint(piernaDerecha.position).y
                    ) * 0.5f;
                puntaje += (alturaCabeza - alturaPiernas) * 2f;
            }

            if (puntaje > mejorPuntaje)
            {
                mejorPuntaje = puntaje;
                mejor = candidatos[i];
            }
        }

        modelo.localRotation = mejor;
    }

    private static Transform BuscarTransformRecursivo(
        Transform root,
        string nombre)
    {
        Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i].name == nombre)
                return transforms[i];
        }
        return null;
    }

    private static void NormalizarModeloEnCaja(
        Transform modelo,
        Transform espacio,
        Vector3 tamanoMaximo,
        Vector3 centroObjetivo)
    {
        if (modelo == null || espacio == null)
            return;

        Quaternion rotacionImportada = modelo.localRotation;
        modelo.localPosition = Vector3.zero;
        modelo.localRotation = rotacionImportada;
        modelo.localScale = Vector3.one;

        if (!IntentarCalcularBounds(espacio, modelo, out Bounds bounds))
            return;

        float escalaX = bounds.size.x > 0.0001f
            ? Mathf.Max(0.0001f, tamanoMaximo.x) / bounds.size.x
            : float.PositiveInfinity;
        float escalaY = bounds.size.y > 0.0001f
            ? Mathf.Max(0.0001f, tamanoMaximo.y) / bounds.size.y
            : float.PositiveInfinity;
        float escalaZ = bounds.size.z > 0.0001f
            ? Mathf.Max(0.0001f, tamanoMaximo.z) / bounds.size.z
            : float.PositiveInfinity;
        float escala = Mathf.Min(escalaX, Mathf.Min(escalaY, escalaZ));
        if (float.IsInfinity(escala) || float.IsNaN(escala))
            return;

        modelo.localScale = Vector3.one * escala;
        if (IntentarCalcularBounds(espacio, modelo, out bounds))
            modelo.localPosition += centroObjetivo - bounds.center;
    }

    private static bool IntentarCalcularBounds(
        Transform espacio,
        Transform contenido,
        out Bounds bounds)
    {
        Renderer[] renderers = contenido.GetComponentsInChildren<Renderer>(true);
        bounds = new Bounds(Vector3.zero, Vector3.zero);
        bool iniciado = false;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
                continue;

            Bounds mundo = renderer.bounds;
            Vector3 min = mundo.min;
            Vector3 max = mundo.max;
            for (int x = 0; x <= 1; x++)
            for (int y = 0; y <= 1; y++)
            for (int z = 0; z <= 1; z++)
            {
                Vector3 esquinaMundo = new Vector3(
                    x == 0 ? min.x : max.x,
                    y == 0 ? min.y : max.y,
                    z == 0 ? min.z : max.z
                );
                Vector3 punto = espacio.InverseTransformPoint(esquinaMundo);
                if (!iniciado)
                {
                    bounds = new Bounds(punto, Vector3.zero);
                    iniciado = true;
                }
                else
                {
                    bounds.Encapsulate(punto);
                }
            }
        }

        return iniciado;
    }

    private void ConstruirCompartimientoTemperatura()
    {
        Transform hueco = CrearAncla("CompartimientoTemperatura", robot, Vector3.zero);
        moduloTemperaturaObjetivo = CrearAncla(
            "ObjetivoModuloTemperatura",
            hueco,
            new Vector3(0.105f, 0.015f, -0.145f)
        );

        // El modelo definitivo del módulo se añadirá a este ancla cuando esté
        // listo. Por ahora se conserva intacta la geometría interna creada en
        // Blender y solo se instala su vidrio protector.
        moduloPadreDock = hueco;

        moduloRepuesto = CrearObjetoAgarrable(
            "ModuloTemperaturaExtraible",
            hueco,
            moduloTemperaturaObjetivo.localPosition,
            new Vector3(0.15f, 0.15f, 0.18f),
            out moduloGrab
        );
        moduloOriginalVisual = moduloRepuesto.gameObject;
        moduloDockLocal = moduloRepuesto.localPosition;
        moduloDockRotation = Quaternion.identity;
        moduloRepuesto.localRotation = moduloDockRotation;

        GameObject prefabModulo = Resources.Load<GameObject>(RecursoModuloTemperatura);
        if (prefabModulo != null)
        {
            GameObject modelo = Instantiate(prefabModulo, moduloRepuesto, false);
            modelo.name = "ModeloModuloTemperatura";
            NormalizarModeloEnCaja(
                modelo.transform,
                moduloRepuesto,
                new Vector3(0.19f, 0.19f, 0.19f),
                Vector3.zero
            );
            moduloTemperaturaInterno = modelo.GetComponentInChildren<Renderer>(true);
        }
        else
        {
            moduloTemperaturaInterno = CrearCajaRedondeada(
                "ModuloTemperaturaFallback",
                moduloRepuesto,
                Vector3.zero,
                new Vector3(0.13f, 0.13f, 0.18f),
                0.025f,
                matRojo
            );
        }

        brilloTemperatura = new[]
        {
            CrearPrimitiva("CalorSuperior", PrimitiveType.Cube, hueco,
                new Vector3(0.105f, 0.107f, -0.244f), new Vector3(0.20f, 0.018f, 0.012f),
                Quaternion.identity, matRojo),
            CrearPrimitiva("CalorInferior", PrimitiveType.Cube, hueco,
                new Vector3(0.105f, -0.077f, -0.244f), new Vector3(0.20f, 0.018f, 0.012f),
                Quaternion.identity, matRojo),
            CrearPrimitiva("CalorIzquierdo", PrimitiveType.Cube, hueco,
                new Vector3(0.013f, 0.015f, -0.244f), new Vector3(0.018f, 0.17f, 0.012f),
                Quaternion.identity, matRojo),
            CrearPrimitiva("CalorDerecho", PrimitiveType.Cube, hueco,
                new Vector3(0.197f, 0.015f, -0.244f), new Vector3(0.018f, 0.17f, 0.012f),
                Quaternion.identity, matRojo)
        };

        Renderer cristal = CrearPrimitiva("VidrioTemperatura", PrimitiveType.Cube, hueco,
            new Vector3(0f, 0.075f, -0.255f), new Vector3(0.40f, 0.45f, 0.014f),
            Quaternion.identity, matVidrio, true);
        ConstruirMarcoVidrio(
            "MarcoVidrioTemperatura",
            hueco,
            new Vector3(0f, 0.075f, -0.264f),
            new Vector2(0.42f, 0.47f)
        );
        GameObject fragmentos = ConstruirFragmentos(
            "FragmentosTemperatura",
            hueco,
            new Vector3(0f, 0.075f, -0.262f),
            new Vector2(0.395f, 0.445f)
        );
        vidrioTemperatura = cristal.gameObject.AddComponent<AlgoLabRobotBreakableGlass>();
        vidrioTemperatura.Configurar(
            practica,
            AlgoLabRobotBreakableGlass.Compartimiento.Temperatura,
            cristal,
            cristal.GetComponent<Collider>(),
            fragmentos
        );

        AlgoLabGrabProximityGate gate =
            moduloRepuesto.gameObject.AddComponent<AlgoLabGrabProximityGate>();
        gate.Configurar(0.020f, vidrioTemperatura, moduloRepuesto);

        moduloGrab.OnGrabStarted += AlAgarrarModulo;
        moduloGrab.OnGrabEnded += AlSoltarModulo;
        moduloAcoplado = true;
        moduloFueRetirado = false;
    }

    private void ConstruirCompartimientoBateria()
    {
        Transform hueco = CrearAncla("CompartimientoBateriaTrasero", robot, Vector3.zero);
        ranuraBateriaObjetivo = CrearAncla(
            "RanuraBateria",
            hueco,
            new Vector3(0.075f, 0.055f, 0.225f)
        );
        bateriaPadreDock = hueco;

        bateriaRepuesto = CrearObjetoAgarrable(
            "BateriaExtraible",
            hueco,
            ranuraBateriaObjetivo.localPosition,
            new Vector3(0.16f, 0.29f, 0.10f),
            out bateriaGrab
        );
        bateriaOriginalVisual = bateriaRepuesto.gameObject;
        bateriaDockLocal = bateriaRepuesto.localPosition;
        bateriaDockRotation = Quaternion.identity;
        bateriaRepuesto.localRotation = bateriaDockRotation;

        GameObject prefabBateria = Resources.Load<GameObject>(RecursoBateria);
        if (prefabBateria != null)
        {
            GameObject modelo = Instantiate(prefabBateria, bateriaRepuesto, false);
            modelo.name = "Modelo_Battery_Quaternius_CC0";
            NormalizarModelo(modelo.transform, bateriaRepuesto, 0.245f, Vector3.zero);
            bateriaInterna = modelo.GetComponentInChildren<Renderer>(true);
        }
        else
        {
            bateriaInterna = CrearCajaRedondeada(
                "BateriaFallback",
                bateriaRepuesto,
                Vector3.zero,
                new Vector3(0.14f, 0.25f, 0.08f),
                0.025f,
                matRojo
            );
        }
        medidorBateria = null;

        puertoCargaObjetivo = CrearAncla(
            "PuertoCarga",
            hueco,
            new Vector3(0.075f, -0.225f, 0.235f)
        );

        Renderer cristal = CrearPrimitiva("VidrioBateria", PrimitiveType.Cube, hueco,
            new Vector3(0.075f, 0.055f, 0.285f), new Vector3(0.22f, 0.35f, 0.014f),
            Quaternion.identity, matVidrio, true);
        ConstruirMarcoVidrio(
            "MarcoVidrioBateria",
            hueco,
            new Vector3(0.075f, 0.055f, 0.294f),
            new Vector2(0.24f, 0.37f)
        );
        GameObject fragmentos = ConstruirFragmentos(
            "FragmentosBateria",
            hueco,
            new Vector3(0.075f, 0.055f, 0.292f),
            new Vector2(0.215f, 0.345f)
        );
        vidrioBateria = cristal.gameObject.AddComponent<AlgoLabRobotBreakableGlass>();
        vidrioBateria.Configurar(
            practica,
            AlgoLabRobotBreakableGlass.Compartimiento.Bateria,
            cristal,
            cristal.GetComponent<Collider>(),
            fragmentos
        );

        AlgoLabGrabProximityGate gate =
            bateriaRepuesto.gameObject.AddComponent<AlgoLabGrabProximityGate>();
        gate.Configurar(0.020f, vidrioBateria, bateriaRepuesto);

        bateriaGrab.OnGrabStarted += AlAgarrarBateria;
        bateriaGrab.OnGrabEnded += AlSoltarBateria;
        bateriaAcoplada = true;
        bateriaFueRetirada = false;
    }

    private void ConstruirExtremidades()
    {
        CrearCajaRedondeada("BrazoIzquierdo", robot, new Vector3(-0.41f, 0.08f, 0f),
            new Vector3(0.16f, 0.48f, 0.17f), 0.045f, matAzulClaro);
        CrearCajaRedondeada("BrazoDerecho", robot, new Vector3(0.41f, 0.08f, 0f),
            new Vector3(0.16f, 0.48f, 0.17f), 0.045f, matAzulClaro);
        CrearPrimitiva("HombroIzquierdo", PrimitiveType.Sphere, robot,
            new Vector3(-0.39f, 0.27f, 0f), Vector3.one * 0.18f,
            Quaternion.identity, matAmarillo);
        CrearPrimitiva("HombroDerecho", PrimitiveType.Sphere, robot,
            new Vector3(0.39f, 0.27f, 0f), Vector3.one * 0.18f,
            Quaternion.identity, matAmarillo);
        CrearPrimitiva("ManoIzquierda", PrimitiveType.Sphere, robot,
            new Vector3(-0.41f, -0.19f, 0f), Vector3.one * 0.16f,
            Quaternion.identity, matAmarillo);
        CrearPrimitiva("ManoDerecha", PrimitiveType.Sphere, robot,
            new Vector3(0.41f, -0.19f, 0f), Vector3.one * 0.16f,
            Quaternion.identity, matAmarillo);

        CrearCajaRedondeada("PiernaIzquierda", robot, new Vector3(-0.16f, -0.55f, 0f),
            new Vector3(0.17f, 0.35f, 0.18f), 0.04f, matAzulClaro);
        CrearCajaRedondeada("PiernaDerecha", robot, new Vector3(0.16f, -0.55f, 0f),
            new Vector3(0.17f, 0.35f, 0.18f), 0.04f, matAzulClaro);
        CrearCajaRedondeada("PieIzquierdo", robot, new Vector3(-0.16f, -0.76f, -0.045f),
            new Vector3(0.22f, 0.12f, 0.30f), 0.035f, matAmarillo);
        CrearCajaRedondeada("PieDerecho", robot, new Vector3(0.16f, -0.76f, -0.045f),
            new Vector3(0.22f, 0.12f, 0.30f), 0.035f, matAmarillo);
    }

    private void ConstruirPanelCurvo()
    {
        Transform panel = CrearAncla("PanelHerramientasPublicas", transform, Vector3.zero);

        float[] xs = { -0.98f, -0.49f, 0f, 0.49f, 0.98f };
        float[] zs = { -0.34f, -0.47f, -0.52f, -0.47f, -0.34f };
        string[] nombres =
        {
            "-  BATERIA",
            "+  CARGAR",
            "-  TEMPERATURA",
            "+  ENFRIAR",
            "+  CONTROL"
        };
        bool[] publicos = { false, true, false, true, true };

        for (int i = 0; i < xs.Length; i++)
        {
            Transform baseSegmento = CrearAncla("SegmentoPanel_" + i, panel,
                new Vector3(xs[i], -0.57f, zs[i]));
            baseSegmento.localRotation = Quaternion.Euler(0f, -xs[i] * 9f, 0f);
            CrearCajaRedondeada("Base", baseSegmento, Vector3.zero,
                new Vector3(0.45f, 0.12f, 0.36f), 0.035f, matPanel);
            CrearPrimitiva("LineaLuminosa", PrimitiveType.Cube, baseSegmento,
                new Vector3(0f, 0.071f, -0.17f), new Vector3(0.39f, 0.018f, 0.018f),
                Quaternion.identity, publicos[i] ? matVerde : matRojo);
            CrearEtiquetaAcceso(
                "Etiqueta_" + i,
                panel,
                nombres[i],
                new Vector3(xs[i], -0.28f, zs[i] - 0.05f),
                publicos[i]
            );
        }

        ConstruirCargador(panel, new Vector3(xs[1], -0.40f, zs[1] - 0.02f));
        ConstruirVentilador(panel, new Vector3(xs[3], -0.40f, zs[3] - 0.02f));
        ConstruirControl(panel, new Vector3(xs[4], -0.40f, zs[4] - 0.02f));
    }

    private void ConstruirBateriaRepuesto(Transform parent, Vector3 posicion)
    {
        bateriaRepuesto = CrearObjetoAgarrable(
            "RepuestoPrivado_Bateria",
            parent,
            posicion,
            new Vector3(0.18f, 0.28f, 0.12f),
            out bateriaGrab
        );
        CrearCajaRedondeada("CuerpoBateria", bateriaRepuesto, Vector3.zero,
            new Vector3(0.15f, 0.24f, 0.09f), 0.025f, matRojo);
        CrearPrimitiva("FranjaPrivada", PrimitiveType.Cube, bateriaRepuesto,
            new Vector3(0f, 0f, -0.055f), new Vector3(0.12f, 0.045f, 0.015f),
            Quaternion.identity, matOscuro);
        bateriaDockLocal = bateriaRepuesto.localPosition;
        bateriaDockRotation = bateriaRepuesto.localRotation;
    }

    private void ConstruirVentilador(Transform parent, Vector3 posicion)
    {
        Transform fan = CrearObjetoAgarrable(
            "MetodoPublico_Enfriar_Ventilador",
            parent,
            posicion,
            new Vector3(0.25f, 0.28f, 0.14f),
            out ventiladorGrab
        );
        CrearPrimitiva("Aro", PrimitiveType.Cylinder, fan,
            new Vector3(0f, 0.035f, 0f), new Vector3(0.115f, 0.035f, 0.115f),
            Quaternion.Euler(90f, 0f, 0f), matCian);
        CrearPrimitiva("Centro", PrimitiveType.Cylinder, fan,
            new Vector3(0f, 0.035f, -0.045f), new Vector3(0.035f, 0.05f, 0.035f),
            Quaternion.Euler(90f, 0f, 0f), matAmarillo);
        aspasVentilador = CrearAncla("Aspas", fan, new Vector3(0f, 0.035f, -0.078f));
        for (int i = 0; i < 3; i++)
        {
            Transform aspa = CrearPrimitiva("Aspa_" + i, PrimitiveType.Capsule,
                aspasVentilador, new Vector3(0f, 0.07f, 0f),
                new Vector3(0.035f, 0.075f, 0.018f),
                Quaternion.Euler(0f, 0f, i * 120f), matMetal).transform;
            aspa.localRotation = Quaternion.Euler(0f, 0f, i * 120f);
        }
        CrearCajaRedondeada("Mango", fan, new Vector3(0f, -0.105f, 0f),
            new Vector3(0.075f, 0.16f, 0.075f), 0.02f, matAmarillo);
        ventiladorDockLocal = fan.localPosition;
        ventiladorDockRotation = fan.localRotation;
    }

    private void ConstruirCargador(Transform parent, Vector3 posicion)
    {
        Transform cargador = CrearObjetoAgarrable(
            "MetodoPublico_Cargar",
            parent,
            posicion,
            new Vector3(0.22f, 0.30f, 0.14f),
            out cargadorGrab
        );
        CrearCajaRedondeada("Cuerpo", cargador, new Vector3(0f, -0.02f, 0f),
            new Vector3(0.14f, 0.19f, 0.09f), 0.025f, matCian);
        CrearCajaRedondeada("Mango", cargador, new Vector3(0f, -0.14f, 0.015f),
            new Vector3(0.075f, 0.13f, 0.07f), 0.018f, matAmarillo);
        puntaCargador = CrearAncla("PuntaConector", cargador, new Vector3(0f, 0.13f, 0f));
        CrearPrimitiva("Conector", PrimitiveType.Cylinder, puntaCargador,
            Vector3.zero, new Vector3(0.035f, 0.075f, 0.035f),
            Quaternion.identity, matMetal);
        CrearPrimitiva("LuzCargador", PrimitiveType.Sphere, cargador,
            new Vector3(0f, 0.015f, -0.055f), Vector3.one * 0.035f,
            Quaternion.identity, matVerde);
        cargadorDockLocal = cargador.localPosition;
        cargadorDockRotation = cargador.localRotation;
    }

    private void ConstruirControl(Transform parent, Vector3 posicion)
    {
        RectTransform canvas = CrearCanvas(
            "PanelControlPublico",
            parent,
            posicion + new Vector3(0f, 0.02f, -0.08f),
            new Vector2(260f, 190f),
            0.00115f,
            new Color(0.025f, 0.055f, 0.075f, 0.98f)
        );
        CrearTexto(canvas, "Titulo", "+  CONTROL PUBLICO", new Vector2(0f, -25f),
            new Vector2(235f, 38f), 18f, new Color(0.2f, 1f, 0.65f));
        CrearBoton(canvas, "Metodo_Apagar", "APAGAR", -78f,
            new Color(0.93f, 0.55f, 0.05f), practica.MetodoApagar);
        CrearBoton(canvas, "Metodo_Encender", "ENCENDER", -138f,
            new Color(0.05f, 0.82f, 0.44f), practica.MetodoEncender);
    }

    private void ConstruirPantallaEstado()
    {
        RectTransform panel = CrearCanvas(
            "PantallaEstadoRobot",
            transform,
            new Vector3(0f, 1.17f, 0.15f),
            new Vector2(920f, 175f),
            0.001f,
            new Color(0.018f, 0.045f, 0.065f, 0.97f)
        );
        CrearTexto(panel, "Titulo", "TALLER DE DIAGNOSTICO  //  CLASE ROBOT",
            new Vector2(0f, -27f), new Vector2(860f, 42f), 25f,
            new Color(0.25f, 0.90f, 1f));
        estadoText = CrearTexto(panel, "Estado", string.Empty,
            new Vector2(-80f, -75f), new Vector2(690f, 42f), 22f, Color.white);
        puntajeText = CrearTexto(panel, "Puntaje", string.Empty,
            new Vector2(350f, -75f), new Vector2(170f, 42f), 22f,
            new Color(1f, 0.82f, 0.20f));
        feedbackText = CrearTexto(panel, "Feedback", string.Empty,
            new Vector2(0f, -127f), new Vector2(860f, 52f), 18f,
            new Color(0.70f, 0.90f, 1f));
    }

    private Transform CrearObjetoAgarrable(
        string nombre,
        Transform parent,
        Vector3 localPosition,
        Vector3 colliderSize,
        out SimpleMRGrabbable grabbable)
    {
        GameObject go = new GameObject(nombre);
        Transform root = go.transform;
        root.SetParent(parent, false);
        root.localPosition = localPosition;

        BoxCollider collider = go.AddComponent<BoxCollider>();
        collider.size = colliderSize;
        Rigidbody rb = go.AddComponent<Rigidbody>();
        rb.mass = 0.25f;
        rb.useGravity = false;
        rb.isKinematic = true;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        grabbable = go.AddComponent<SimpleMRGrabbable>();
        grabbable.perfilUso = SimpleMRGrabbable.PerfilUso.Practica1FlotanteSinColision;
        grabbable.AplicarPresetDesdePerfil();
        grabbable.bloquearPosicionMundoAlSoltar = false;
        grabbable.bloquearRotacionMundoAlSoltar = false;
        grabbable.mostrarDebug = false;

        rb.useGravity = false;
        rb.isKinematic = true;
        rb.constraints = RigidbodyConstraints.FreezeAll;
        return root;
    }

    private void DevolverObjeto(
        SimpleMRGrabbable objeto,
        Vector3 localPosition,
        Quaternion localRotation)
    {
        if (objeto == null)
            return;
        if (objeto.IsGrabbed)
            objeto.EndGrab();
        objeto.PermitirMovimientoExterno();
        objeto.transform.localPosition = localPosition;
        objeto.transform.localRotation = localRotation;
        Rigidbody rb = objeto.Rigidbody;
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.useGravity = false;
            rb.isKinematic = true;
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }
    }

    private void RecuperarSiSePerdio(
        SimpleMRGrabbable objeto,
        Vector3 localPosition,
        Quaternion localRotation)
    {
        if (objeto == null || objeto.IsGrabbed)
            return;
        if (Vector3.Distance(objeto.transform.position, transform.position) > 3f)
            DevolverObjeto(objeto, localPosition, localRotation);
    }

    private void ConstruirMarcoVidrio(
        string nombre,
        Transform parent,
        Vector3 centro,
        Vector2 size)
    {
        Transform marco = CrearAncla(nombre, parent, centro);
        const float grosor = 0.018f;
        const float profundidad = 0.016f;
        float mitadX = size.x * 0.5f;
        float mitadY = size.y * 0.5f;

        CrearPrimitiva(
            "BordeSuperior",
            PrimitiveType.Cube,
            marco,
            new Vector3(0f, mitadY, 0f),
            new Vector3(size.x + grosor, grosor, profundidad),
            Quaternion.identity,
            matMarcoVidrio
        );
        CrearPrimitiva(
            "BordeInferior",
            PrimitiveType.Cube,
            marco,
            new Vector3(0f, -mitadY, 0f),
            new Vector3(size.x + grosor, grosor, profundidad),
            Quaternion.identity,
            matMarcoVidrio
        );
        CrearPrimitiva(
            "BordeIzquierdo",
            PrimitiveType.Cube,
            marco,
            new Vector3(-mitadX, 0f, 0f),
            new Vector3(grosor, size.y, profundidad),
            Quaternion.identity,
            matMarcoVidrio
        );
        CrearPrimitiva(
            "BordeDerecho",
            PrimitiveType.Cube,
            marco,
            new Vector3(mitadX, 0f, 0f),
            new Vector3(grosor, size.y, profundidad),
            Quaternion.identity,
            matMarcoVidrio
        );
    }

    private GameObject ConstruirFragmentos(
        string nombre,
        Transform parent,
        Vector3 centro,
        Vector2 size)
    {
        Transform root = CrearAncla(nombre, parent, centro);
        const int columnas = 4;
        const int filas = 3;
        Vector2 celda = new Vector2(size.x / columnas, size.y / filas);

        for (int fila = 0; fila < filas; fila++)
        for (int columna = 0; columna < columnas; columna++)
        {
            int indice = fila * columnas + columna;
            float x = -size.x * 0.5f + celda.x * (columna + 0.5f);
            float y = -size.y * 0.5f + celda.y * (fila + 0.5f);
            float variacion = 0.80f + (indice % 3) * 0.055f;
            Renderer fragmento = CrearPrimitiva(
                "Fragmento_" + indice,
                PrimitiveType.Cube,
                root,
                new Vector3(x, y, (indice % 2 == 0 ? -1f : 1f) * 0.004f),
                new Vector3(celda.x * variacion, celda.y * 0.84f, 0.009f),
                Quaternion.Euler(0f, 0f, -10f + indice * 3.1f),
                matVidrio,
                true
            );

            Rigidbody rb = fragmento.gameObject.AddComponent<Rigidbody>();
            rb.mass = 0.015f;
            rb.useGravity = false;
            rb.isKinematic = true;
            rb.constraints = RigidbodyConstraints.FreezeAll;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        }

        root.gameObject.SetActive(false);
        return root.gameObject;
    }

    private Renderer CrearCajaRedondeada(
        string nombre,
        Transform parent,
        Vector3 posicion,
        Vector3 size,
        float radius,
        Material material)
    {
        Transform root = CrearAncla(nombre, parent, posicion);
        float r = Mathf.Min(radius, Mathf.Min(size.x, Mathf.Min(size.y, size.z)) * 0.45f);

        Renderer principal = CrearPrimitiva("Centro", PrimitiveType.Cube, root, Vector3.zero,
            new Vector3(size.x - 2f * r, size.y, size.z - 2f * r),
            Quaternion.identity, material);
        CrearPrimitiva("CentroX", PrimitiveType.Cube, root, Vector3.zero,
            new Vector3(size.x, size.y - 2f * r, size.z - 2f * r),
            Quaternion.identity, material);
        CrearPrimitiva("CentroZ", PrimitiveType.Cube, root, Vector3.zero,
            new Vector3(size.x - 2f * r, size.y - 2f * r, size.z),
            Quaternion.identity, material);

        for (int xi = -1; xi <= 1; xi += 2)
        for (int yi = -1; yi <= 1; yi += 2)
        for (int zi = -1; zi <= 1; zi += 2)
        {
            CrearPrimitiva("Esquina", PrimitiveType.Sphere, root,
                new Vector3(
                    xi * (size.x * 0.5f - r),
                    yi * (size.y * 0.5f - r),
                    zi * (size.z * 0.5f - r)
                ),
                Vector3.one * (2f * r), Quaternion.identity, material);
        }
        return principal;
    }

    private void CrearEtiquetaAcceso(
        string nombre,
        Transform parent,
        string texto,
        Vector3 posicion,
        bool publico)
    {
        RectTransform panel = CrearCanvas(nombre, parent, posicion,
            new Vector2(245f, 58f), 0.0009f,
            publico
                ? new Color(0.025f, 0.25f, 0.18f, 0.98f)
                : new Color(0.38f, 0.045f, 0.065f, 0.98f));
        CrearTexto(panel, "Texto", texto, new Vector2(0f, -29f),
            new Vector2(225f, 50f), 20f, Color.white);
    }

    private RectTransform CrearCanvas(
        string nombre,
        Transform parent,
        Vector3 posicion,
        Vector2 size,
        float scale,
        Color background)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(Image));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.localPosition = posicion;
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one * scale;
        rect.sizeDelta = size;

        Canvas canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;
        canvas.sortingOrder = 45;
        Image image = go.GetComponent<Image>();
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
        Color color)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform),
            typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = posicion;
        rect.sizeDelta = size;
        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = texto;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        tmp.raycastTarget = false;
        return tmp;
    }

    private void CrearBoton(
        RectTransform parent,
        string nombre,
        string etiqueta,
        float y,
        Color normal,
        UnityEngine.Events.UnityAction accion)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Image), typeof(Button),
            typeof(AlgoLabRobotPracticeButton));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, y);
        rect.sizeDelta = new Vector2(220f, 48f);
        Image image = go.GetComponent<Image>();
        image.color = normal;
        Button button = go.GetComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.onClick.AddListener(accion);
        AlgoLabRobotPracticeButton marker = go.GetComponent<AlgoLabRobotPracticeButton>();
        marker.background = image;
        marker.normalColor = normal;
        marker.hoverColor = Color.Lerp(normal, Color.white, 0.28f);
        CrearTextoCentrado(rect, "Texto", "+  " + etiqueta, 20f, Color.white);
    }

    private void CrearTextoCentrado(
        RectTransform parent,
        string nombre,
        string texto,
        float fontSize,
        Color color)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform),
            typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = texto;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
    }

    private static Transform CrearAncla(string nombre, Transform parent, Vector3 posicion)
    {
        Transform anchor = new GameObject(nombre).transform;
        anchor.SetParent(parent, false);
        anchor.localPosition = posicion;
        return anchor;
    }

    private static Renderer CrearPrimitiva(
        string nombre,
        PrimitiveType tipo,
        Transform parent,
        Vector3 posicion,
        Vector3 escala,
        Quaternion rotacion,
        Material material,
        bool conservarCollider = false)
    {
        GameObject go = GameObject.CreatePrimitive(tipo);
        go.name = nombre;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = posicion;
        go.transform.localRotation = rotacion;
        go.transform.localScale = escala;
        Renderer renderer = go.GetComponent<Renderer>();
        renderer.sharedMaterial = material;
        Collider collider = go.GetComponent<Collider>();
        if (collider != null)
            collider.enabled = conservarCollider;
        return renderer;
    }

    private void CrearMateriales()
    {
        matAzul = CrearMaterialPlastico("Robot_Azul", new Color(0.02f, 0.42f, 0.72f));
        matAzulClaro = CrearMaterialPlastico("Robot_Cian", new Color(0.02f, 0.72f, 0.92f));
        matOscuro = CrearMaterial("Robot_Oscuro", new Color(0.015f, 0.025f, 0.04f), 0.2f, 0.40f);
        matMetal = CrearMaterial("Robot_Metal", new Color(0.62f, 0.72f, 0.82f), 0.88f, 0.72f);
        matAmarillo = CrearMaterialPlastico("Robot_Amarillo", new Color(1f, 0.70f, 0.04f));
        matRojo = CrearMaterialPlastico("Robot_Rojo", new Color(1f, 0.045f, 0.025f));
        matVerde = CrearMaterialPlastico("Robot_Verde", new Color(0.03f, 1f, 0.38f));
        matCian = CrearMaterialPlastico("Robot_CianEmisivo", new Color(0.02f, 0.75f, 1f));
        matPanel = CrearMaterial("Panel_Taller", new Color(0.025f, 0.075f, 0.11f), 0.25f, 0.62f);
        matVidrio = CrearMaterialVidrio();
        matMarcoVidrio = CrearMaterialEmisivo(
            "MarcoVidrio_Cian",
            new Color(0.04f, 0.88f, 1f)
        );
    }

    private Material CrearMaterial(
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
        if (material.HasProperty("_Metallic"))
            material.SetFloat("_Metallic", metallic);
        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", smoothness);
        materiales.Add(material);
        return material;
    }

    private Material CrearMaterialEmisivo(string nombre, Color color)
    {
        Material material = CrearMaterial(nombre, color, 0.12f, 0.65f);
        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * 1.8f);
        }
        return material;
    }

    private Material CrearMaterialPlastico(string nombre, Color color)
    {
        Shader shader = Shader.Find("AlgoLab/RobotPlastic");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Simple Lit");
        if (shader == null)
            return CrearMaterial(nombre, color, 0f, 0.58f);

        Material material = new Material(shader) { name = nombre, color = color };
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);
        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", 0.58f);
        if (material.HasProperty("_SpecColor"))
            material.SetColor("_SpecColor", new Color(0.18f, 0.22f, 0.26f, 1f));
        materiales.Add(material);
        return material;
    }

    private Material CrearMaterialVidrio()
    {
        Color color = new Color(0.28f, 0.86f, 1f, 0.46f);
        Material material = CrearMaterial("VidrioProtector", color, 0.05f, 0.95f);
        if (material.HasProperty("_Surface"))
            material.SetFloat("_Surface", 1f);
        if (material.HasProperty("_Blend"))
            material.SetFloat("_Blend", 0f);
        if (material.HasProperty("_SrcBlend"))
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (material.HasProperty("_DstBlend"))
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        if (material.HasProperty("_ZWrite"))
            material.SetFloat("_ZWrite", 0f);
        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", new Color(0.02f, 0.18f, 0.24f));
        }
        material.SetOverrideTag("RenderType", "Transparent");
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = 3000;
        return material;
    }

    private static void AplicarColor(Renderer renderer, Color color, bool emitir)
    {
        if (renderer == null)
            return;

        // En autoría se conservan los materiales persistentes del prefab. Crear
        // renderer.material fuera de Play Mode deja copias ocultas en la escena
        // y hace que Unity advierta sobre fugas de materiales.
        if (!Application.isPlaying)
            return;

        Material material = renderer.material;
        material.color = color;
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);
        if (material.HasProperty("_EmissionColor"))
        {
            if (emitir)
                material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", emitir ? color * 1.7f : Color.black);
        }
    }

    private void OnDestroy()
    {
        if (bateriaGrab != null)
        {
            bateriaGrab.OnGrabStarted -= AlAgarrarBateria;
            bateriaGrab.OnGrabEnded -= AlSoltarBateria;
        }
        if (moduloGrab != null)
        {
            moduloGrab.OnGrabStarted -= AlAgarrarModulo;
            moduloGrab.OnGrabEnded -= AlSoltarModulo;
        }

        for (int i = 0; i < materiales.Count; i++)
        {
            if (materiales[i] == null)
                continue;
#if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(materiales[i]);
            else
                Destroy(materiales[i]);
#else
            Destroy(materiales[i]);
#endif
        }
        materiales.Clear();
    }
}
