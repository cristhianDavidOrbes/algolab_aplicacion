using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

[DefaultExecutionOrder(10000)]
[RequireComponent(typeof(BoxCollider))]
public class AlgoLabPanelGrabHandle : MonoBehaviour
{
    private static readonly Dictionary<Transform, AlgoLabPanelGrabHandle> AgarresPorPanel =
        new Dictionary<Transform, AlgoLabPanelGrabHandle>();

    [Header("Panel que se va a mover")]
    public Transform panelRoot;

    [Header("Billboard opcional")]
    public AlgoLabDiagramBillboard billboard;

    [Header("Controladores")]
    public Transform leftController;
    public Transform rightController;

    [Header("Manos opcional")]
    public OVRHand leftHand;
    public Transform leftHandTransform;

    public OVRHand rightHand;
    public Transform rightHandTransform;

    [Header("Agarre")]
    public float distanciaMaximaAgarre = 0.03f;
    public float umbralGrip = 0.45f;
    public float umbralPinza = 0.75f;
    public bool permitirControladores = true;
    public bool permitirManos = false;

    [Header("Agarre preciso")]
    [Tooltip("Si está activo, el panel se mueve desde el punto exacto donde se agarró la barra.")]
    public bool usarPuntoExactoDeAgarre = true;

    [Tooltip("Mantiene el billboard activo mientras se agarra, para que el panel siga mirando al usuario.")]
    public bool mantenerBillboardActivoDuranteAgarre = true;

    [Header("Movimiento")]
    public bool movimientoDirecto = true;
    public float suavizadoMovimiento = 20f;

    [Header("Paneles normales")]
    [Tooltip("Compatibilidad antigua: ya no se usa. Los paneles normales vuelven a la lógica original del grab con punto exacto, aunque tengan DiagramBillboard.")]
    public bool usarOffsetEstableSiBillboardActivoEnPanelNormal = false;

    [Header("Panel de diagrama - agarre estable")]
    [Tooltip("Activado = si este handle pertenece al panel de diagrama, usa una lógica estable para que el punto agarrado siga al control sin movimientos raros.")]
    public bool usarAgarreEstablePanelDiagrama = true;

    [Tooltip("Detecta automáticamente paneles cuyo root o padres tengan nombres como Diagrama, Diagram o ClassDiagram.")]
    public bool autoDetectarSiEsteHandleEsPanelDiagrama = true;

    [Tooltip("Actívalo SOLO en el GrabHandle del panel de diagrama si la detección automática no lo reconoce.")]
    public bool forzarEsteHandleComoPanelDiagrama = false;

    [Tooltip("Actívalo en paneles normales si por error se detectan como diagrama.")]
    public bool bloquearModoDiagramaEnEsteHandle = false;

    [Tooltip("Si está activo, pausa el DiagramBillboard mientras se agarra el diagrama. Déjalo DESACTIVADO si quieres que el panel de diagrama siga mirando al jugador mientras lo agarras.")]
    public bool apagarBillboardMientrasAgarraDiagrama = false;

    [Tooltip("Activado = el panel de diagrama sigue mirando al jugador mientras lo agarras. Esta opción tiene prioridad sobre Apagar Billboard Mientras Agarra Diagrama.")]
    public bool mantenerDiagramaMirandoMientrasAgarra = true;

    [Tooltip("RECOMENDADO ACTIVADO. Mientras agarras el diagrama, apaga temporalmente el DiagramBillboard y este GrabHandle rota el panel hacia el jugador manteniendo fijo el punto agarrado. Evita la pelea entre Billboard y agarre.")]
    public bool rotarDiagramaDesdeGrabHandleMientrasAgarra = true;

    [Tooltip("Activado = mientras se agarra el panel de diagrama, solo rota sobre Y. Apagado permite que mire tambien arriba/abajo.")]
    public bool forzarSoloRotacionYDiagramaMientrasAgarra = false;

    [Tooltip("Activado = si el panel queda casi vertical respecto a la cabeza, conserva su rotacion actual para evitar vueltas bruscas.")]
    public bool protegerDiagramaDeFlipVerticalMientrasAgarra = true;

    [Tooltip("Que tan cerca de vertical debe estar la direccion para congelar la rotacion y evitar flip. 0.92 permite mirar arriba/abajo, solo bloquea casos extremos.")]
    [Range(0.75f, 0.99f)]
    public float umbralFlipVerticalDiagramaMientrasAgarra = 0.96f;

    [Tooltip("RECOMENDADO ACTIVADO. Usa PanelRoot como punto base de mirada mientras agarras. Evita usar puntos hijos que cambian con la rotación y generan movimiento raro.")]
    public bool usarRootComoPuntoMiradaDiagramaMientrasAgarra = true;

    [Tooltip("Si está activo, suaviza la rotación manual del diagrama mientras se agarra. Para un agarre más firme déjalo apagado.")]
    public bool suavizarMiradaDiagramaMientrasAgarra = false;

    [Tooltip("Velocidad del suavizado de mirada manual del diagrama si Suavizar Mirada está activado.")]
    public float suavizadoMiradaDiagramaMientrasAgarra = 20f;

    [Tooltip("Activado = si el DiagramBillboard queda activo mientras agarras el diagrama, este script vuelve a pegar el punto agarrado al control en LateUpdate, después de la rotación del billboard. Solo se usa si Rotar Diagrama Desde GrabHandle está apagado.")]
    public bool reanclarDiagramaDespuesDeBillboard = true;

    [Tooltip("Activado = usa el centro del GrabHandle como punto de agarre. Desactivado = usa el punto más cercano del BoxCollider.")]
    public bool usarCentroDelGrabHandleComoAnclaDiagrama = false;

    [Tooltip("Desactivado = el punto agarrado queda exactamente en el control. Activado = conserva la distancia inicial entre control y punto agarrado.")]
    public bool mantenerOffsetInicialControlDiagrama = true;

    [Tooltip("Activado = el diagrama se mueve directo al control, sin interpolación elástica.")]
    public bool forzarMovimientoDirectoDiagrama = true;

    [Header("Tutorial - compatibilidad antigua")]
    [Tooltip("Compatibilidad con escenas antiguas. El tutorial usa ahora el mismo agarre preciso que los paneles normales.")]
    public bool usarMovimientoAncladoTutorial = false;

    [Tooltip("Activado = el centro de GrabHandleBottom2 es el ancla que se pega al control. Recomendado para el tutorial.")]
    public bool usarCentroDelGrabHandleComoAnclaTutorial = false;

    [Tooltip("Activado = después de que el TutorialPanelController rota el panel para mirar al jugador, este script vuelve a pegar el ancla al control en LateUpdate.")]
    public bool reanclarTutorialDespuesDeMirada = true;

    [Tooltip("Desactivado = el centro de GrabHandleBottom2 queda exactamente en la posición del control. Activado = conserva la pequeña distancia inicial entre control y handle.")]
    public bool mantenerOffsetInicialControlTutorial = true;

    [Tooltip("Offset opcional en mundo para el ancla del tutorial. Normalmente 0,0,0.")]
    public Vector3 offsetMundoExtraAnclaTutorial = Vector3.zero;

    [Tooltip("Activado = el tutorial se mueve directo al control. Evita sensación elástica.")]
    public bool forzarMovimientoDirectoTutorial = true;

    [Header("Tutorial - aislamiento del resto de paneles")]
    [Tooltip("Activado = este script decide automáticamente si este GrabHandle es el del tutorial. Si NO es tutorial, usa la lógica antigua normal.")]
    public bool autoDetectarSiEsteHandleEsTutorial = true;

    [Tooltip("Actívalo SOLO en GrabHandleBottom2 del tutorial si la detección automática no lo reconoce.")]
    public bool forzarEsteHandleComoTutorial = false;

    [Tooltip("Actívalo en paneles normales si por error los detecta como tutorial. Fuerza la lógica antigua.")]
    public bool bloquearModoTutorialEnEsteHandle = false;

    [Header("Tutorial interactivo")]
    public bool notificarTutorialInteractivo = true;

    [Tooltip("Si está vacío, se busca automáticamente el AlgoLabTutorialPanelController en la escena.")]
    public AlgoLabTutorialPanelController tutorialController;

    [Tooltip("Distancia mínima que debe moverse el panel para considerar que el usuario sí lo arrastró.")]
    public float distanciaMinimaMovimientoTutorial = 0.05f;

    [Tooltip("Evita avisar muchas veces que el panel se movió.")]
    public bool notificarMovimientoSoloUnaVez = true;

    [Header("Eventos Unity opcionales")]
    public UnityEvent alIniciarAgarre;
    public UnityEvent alMoverPanel;
    public UnityEvent alSoltarPanel;

    [Header("Debug")]
    public bool mostrarDebug = false;

    private BoxCollider boxCollider;

    private bool agarrando;
    private Transform transformAgarreActivo;

    private Vector3 offsetPanel;

    private Vector3 puntoAgarreLocalEnPanel;
    private Vector3 offsetAgarradorAPuntoAgarre;
    private bool puntoAgarreValido;

    private bool billboardEstabaActivo;

    private TipoAgarre tipoAgarreActual = TipoAgarre.Ninguno;

    private Vector3 posicionPanelAlIniciarAgarre;
    private bool movimientoTutorialNotificado;

    private bool diagramaAncladoActivo;
    private Vector3 anclaLocalDiagramaEnHandle;
    private Vector3 offsetInicialAgarradorAnclaDiagrama;
    private bool billboardApagadoPorDiagrama;


    public bool EstaAgarrando => agarrando;
    public Transform AgarradorActivo => transformAgarreActivo;

    private enum TipoAgarre
    {
        Ninguno,
        ControladorIzquierdo,
        ControladorDerecho,
        ManoIzquierda,
        ManoDerecha
    }

    private void Awake()
    {
        boxCollider = GetComponent<BoxCollider>();

        if (panelRoot == null)
        {
            panelRoot = transform.root;
        }

        if (billboard != null && panelRoot != null &&
            billboard.transform != panelRoot &&
            !billboard.transform.IsChildOf(panelRoot))
        {
            if (mostrarDebug)
            {
                Debug.LogWarning(
                    "GrabHandle ignoró un Billboard que pertenece a otro panel: " + name
                );
            }

            billboard = null;
        }

        if (billboard == null && panelRoot != null)
        {
            billboard = panelRoot.GetComponentInChildren<AlgoLabDiagramBillboard>(true);
        }

        BuscarTutorialController();
    }

    private void Update()
    {
        if (!agarrando)
        {
            IntentarIniciarAgarre();
        }
        else
        {
            ActualizarAgarre();
        }
    }

    private void LateUpdate()
    {
        ReanclarDiagramaAlControlDespuesDeBillboard();
        ReanclarPanelNormalDespuesDeOrientacion();
    }

    private void OnDisable()
    {
        // Si el Pocket desactiva el panel mientras estaba agarrado,
        // limpiamos el estado para no dejar sistemas externos pegados.
        if (agarrando)
        {
            TerminarAgarre();
        }

        LiberarPropiedadAgarre();
    }

    private void OnDestroy()
    {
        LiberarPropiedadAgarre();
    }

    public void CancelarAgarreForzadoDesdeExterno(bool notificarSoltado = true)
    {
        if (!agarrando)
        {
            diagramaAncladoActivo = false;
            billboardApagadoPorDiagrama = false;
            transformAgarreActivo = null;
            tipoAgarreActual = TipoAgarre.Ninguno;
            puntoAgarreValido = false;
            LiberarPropiedadAgarre();
            return;
        }

        if (notificarSoltado)
        {
            TerminarAgarre();
            return;
        }

        agarrando = false;
        transformAgarreActivo = null;
        tipoAgarreActual = TipoAgarre.Ninguno;
        puntoAgarreValido = false;
        diagramaAncladoActivo = false;
        offsetInicialAgarradorAnclaDiagrama = Vector3.zero;
        anclaLocalDiagramaEnHandle = Vector3.zero;
        LiberarPropiedadAgarre();

        if (billboard != null && (billboardApagadoPorDiagrama || !mantenerBillboardActivoDuranteAgarre))
        {
            billboard.enabled = billboardEstabaActivo;
        }

        billboardApagadoPorDiagrama = false;
    }

    private void BuscarTutorialController()
    {
        if (tutorialController != null)
        {
            return;
        }

        tutorialController = FindFirstObjectByType<AlgoLabTutorialPanelController>(
            FindObjectsInactive.Include
        );
    }

    private void IntentarIniciarAgarre()
    {
        if (permitirControladores)
        {
            RevisarControlador(
                leftController,
                TipoAgarre.ControladorIzquierdo,
                ObtenerGripIzquierdo(),
                "IZQUIERDO"
            );

            RevisarControlador(
                rightController,
                TipoAgarre.ControladorDerecho,
                ObtenerGripDerecho(),
                "DERECHO"
            );
        }

        if (permitirManos)
        {
            RevisarMano(
                leftHand,
                leftHandTransform,
                TipoAgarre.ManoIzquierda,
                "MANO IZQUIERDA"
            );

            RevisarMano(
                rightHand,
                rightHandTransform,
                TipoAgarre.ManoDerecha,
                "MANO DERECHA"
            );
        }
    }

    private void RevisarControlador(
        Transform controlador,
        TipoAgarre tipo,
        float grip,
        string nombre)
    {
        if (agarrando || controlador == null)
        {
            return;
        }

        float distancia = ObtenerDistanciaAlHandle(controlador.position);

        if (mostrarDebug)
        {
            Debug.Log(
                "PanelGrab " + nombre +
                " | Distancia: " + distancia.ToString("F3") +
                " | Grip: " + grip.ToString("F2")
            );
        }

        bool estaCerca = distancia <= distanciaMaximaAgarre;
        bool estaPresionando = grip >= umbralGrip;

        if (estaCerca && estaPresionando)
        {
            IniciarAgarre(controlador, tipo);
        }
    }

    private void RevisarMano(
        OVRHand mano,
        Transform manoTransform,
        TipoAgarre tipo,
        string nombre)
    {
        if (agarrando || mano == null || manoTransform == null)
        {
            return;
        }

        float fuerzaPinza = mano.GetFingerPinchStrength(OVRHand.HandFinger.Index);
        float distancia = ObtenerDistanciaAlHandle(manoTransform.position);

        if (mostrarDebug)
        {
            Debug.Log(
                "PanelGrab " + nombre +
                " | Distancia: " + distancia.ToString("F3") +
                " | Pinza: " + fuerzaPinza.ToString("F2")
            );
        }

        bool estaCerca = distancia <= distanciaMaximaAgarre;
        bool estaHaciendoPinza = fuerzaPinza >= umbralPinza;

        if (estaCerca && estaHaciendoPinza)
        {
            IniciarAgarre(manoTransform, tipo);
        }
    }

    private void IniciarAgarre(Transform agarrador, TipoAgarre tipo)
    {
        if (panelRoot == null || agarrador == null)
        {
            Debug.LogError("Falta Panel Root o agarrador.");
            return;
        }

        if (!IntentarTomarPropiedadAgarre())
        {
            return;
        }

        agarrando = true;
        transformAgarreActivo = agarrador;
        tipoAgarreActual = tipo;

        offsetPanel = panelRoot.position - agarrador.position;

        puntoAgarreValido = false;

        if (usarPuntoExactoDeAgarre)
        {
            Vector3 puntoAgarreMundo = ObtenerPuntoCercanoAlHandle(agarrador.position);

            puntoAgarreLocalEnPanel = panelRoot.InverseTransformPoint(puntoAgarreMundo);
            offsetAgarradorAPuntoAgarre = puntoAgarreMundo - agarrador.position;

            puntoAgarreValido = true;
        }

        PrepararAnclaDiagramaSiCorresponde(agarrador);

        if (billboard != null)
        {
            billboardEstabaActivo = billboard.enabled;
            billboardApagadoPorDiagrama = false;

            if (diagramaAncladoActivo && mantenerDiagramaMirandoMientrasAgarra && rotarDiagramaDesdeGrabHandleMientrasAgarra)
            {
                // FIX FINAL DIAGRAMA:
                // No dejamos que DiagramBillboard rote el mismo root mientras el usuario lo agarra,
                // porque eso mueve el punto agarrado y produce el movimiento raro.
                // En su lugar, este GrabHandle rota manualmente hacia el jugador y compensa la posición
                // para que el punto agarrado quede fijo en el control.
                billboard.enabled = false;
                billboardApagadoPorDiagrama = true;
            }
            else if (diagramaAncladoActivo && mantenerDiagramaMirandoMientrasAgarra)
            {
                // Modo alternativo antiguo: deja activo el billboard y reancla después.
                // Si notas movimiento raro, usa Rotar Diagrama Desde GrabHandle.
                billboard.enabled = billboardEstabaActivo;
                billboardApagadoPorDiagrama = false;
            }
            else if (diagramaAncladoActivo && apagarBillboardMientrasAgarraDiagrama)
            {
                billboard.enabled = false;
                billboardApagadoPorDiagrama = true;
            }
            else if (!mantenerBillboardActivoDuranteAgarre)
            {
                billboard.enabled = false;
            }
        }

        posicionPanelAlIniciarAgarre = panelRoot.position;
        movimientoTutorialNotificado = false;

        NotificarTutorialAgarrado();

        if (mostrarDebug)
        {
            Debug.Log("AGARRANDO PANEL: " + tipo);
        }
    }

    private void ActualizarAgarre()
    {
        if (panelRoot == null || transformAgarreActivo == null)
        {
            TerminarAgarre();
            return;
        }

        if (!DebeSeguirAgarrando())
        {
            TerminarAgarre();
            return;
        }

        Vector3 posicionObjetivo = CalcularPosicionObjetivoAgarre();
        AplicarPosicionObjetivo(posicionObjetivo);

        RevisarMovimientoParaTutorial();
    }

    private void PrepararAnclaDiagramaSiCorresponde(Transform agarrador)
    {
        diagramaAncladoActivo = false;
        anclaLocalDiagramaEnHandle = Vector3.zero;
        offsetInicialAgarradorAnclaDiagrama = Vector3.zero;
        billboardApagadoPorDiagrama = false;

        if (!usarAgarreEstablePanelDiagrama || panelRoot == null || agarrador == null)
        {
            return;
        }

        if (EsPanelTutorial())
        {
            // Nunca mezclamos la lógica del diagrama con la del tutorial.
            return;
        }

        if (!EsPanelDiagrama())
        {
            return;
        }

        mantenerDiagramaMirandoMientrasAgarra = true;
        rotarDiagramaDesdeGrabHandleMientrasAgarra = false;
        apagarBillboardMientrasAgarraDiagrama = false;
        reanclarDiagramaDespuesDeBillboard = true;
        mantenerOffsetInicialControlDiagrama = true;

        Vector3 anclaMundo = usarCentroDelGrabHandleComoAnclaDiagrama
            ? transform.position
            : ObtenerPuntoCercanoAlHandle(agarrador.position);

        anclaLocalDiagramaEnHandle = transform.InverseTransformPoint(anclaMundo);

        offsetInicialAgarradorAnclaDiagrama = mantenerOffsetInicialControlDiagrama
            ? anclaMundo - agarrador.position
            : Vector3.zero;

        diagramaAncladoActivo = true;
    }

    private bool EsPanelTutorial()
    {
        if (bloquearModoTutorialEnEsteHandle)
        {
            return false;
        }

        if (forzarEsteHandleComoTutorial)
        {
            // Protección extra: aunque el Inspector quede mal en otro panel,
            // solo aceptamos el forzado si este handle realmente pertenece al root del tutorial
            // o si el TutorialPanelController lo tiene asignado como GrabHandleBottom2.
            BuscarTutorialController();

            if (tutorialController != null)
            {
                if (tutorialController.grabHandleTutorialPocket == this)
                {
                    return true;
                }

                Transform rootTutorialForzado = tutorialController.rootParaUbicar != null
                    ? tutorialController.rootParaUbicar
                    : tutorialController.transform;

                if (rootTutorialForzado != null && panelRoot == rootTutorialForzado && transform.IsChildOf(rootTutorialForzado))
                {
                    return true;
                }
            }

            return false;
        }

        if (!autoDetectarSiEsteHandleEsTutorial)
        {
            return false;
        }

        BuscarTutorialController();

        if (tutorialController == null || panelRoot == null)
        {
            return false;
        }

        // La regla más segura: el TutorialPanelController sabe cuál es su GrabHandleBottom2.
        // Si este NO es ese handle, no debe usar la lógica especial del tutorial.
        if (tutorialController.grabHandleTutorialPocket == this)
        {
            return true;
        }

        Transform miTransform = transform;
        Transform barraTutorial = tutorialController.barraInferior;

        if (barraTutorial != null)
        {
            if (miTransform == barraTutorial || miTransform.IsChildOf(barraTutorial))
            {
                return true;
            }

            AlgoLabPanelGrabHandle handleEnBarra = barraTutorial.GetComponent<AlgoLabPanelGrabHandle>();
            if (handleEnBarra == this)
            {
                return true;
            }
        }

        Transform rootTutorial = tutorialController.rootParaUbicar != null
            ? tutorialController.rootParaUbicar
            : tutorialController.transform;

        if (rootTutorial == null)
        {
            return false;
        }

        // IMPORTANTE:
        // Antes se aceptaba que el tutorial fuera hijo de panelRoot.
        // Eso era demasiado amplio: si un panel normal tenía como root un padre grande,
        // también podía activar la lógica especial del tutorial y se movía raro.
        // Ahora solo se acepta si panelRoot ES exactamente el root del tutorial
        // y este GrabHandle está dentro de ese root.
        if (panelRoot == rootTutorial && (miTransform == rootTutorial || miTransform.IsChildOf(rootTutorial)))
        {
            return true;
        }

        return false;
    }

    private bool EsPanelDiagrama()
    {
        if (bloquearModoDiagramaEnEsteHandle)
        {
            return false;
        }

        if (forzarEsteHandleComoPanelDiagrama)
        {
            return true;
        }

        if (!autoDetectarSiEsteHandleEsPanelDiagrama)
        {
            return false;
        }

        if (NombreJerarquiaContiene(transform, "diagrama") ||
            NombreJerarquiaContiene(transform, "diagram") ||
            NombreJerarquiaContiene(transform, "classdiagram"))
        {
            return true;
        }

        if (panelRoot != null)
        {
            if (NombreJerarquiaContiene(panelRoot, "diagrama") ||
                NombreJerarquiaContiene(panelRoot, "diagram") ||
                NombreJerarquiaContiene(panelRoot, "classdiagram"))
            {
                return true;
            }

            if (panelRoot.GetComponentInChildren<AlgoLabClassDiagramController>(true) != null ||
                panelRoot.GetComponentInChildren<AlgoLabClassDiagramModeManager>(true) != null)
            {
                return true;
            }
        }

        return false;
    }

    private bool NombreJerarquiaContiene(Transform origen, string texto)
    {
        if (origen == null || string.IsNullOrWhiteSpace(texto))
        {
            return false;
        }

        string buscar = texto.ToLower();
        Transform actual = origen;

        while (actual != null)
        {
            if (!string.IsNullOrEmpty(actual.name) && actual.name.ToLower().Contains(buscar))
            {
                return true;
            }

            actual = actual.parent;
        }

        return false;
    }

    private bool DebeUsarAgarreEstableDiagrama()
    {
        return diagramaAncladoActivo &&
               usarAgarreEstablePanelDiagrama &&
               panelRoot != null &&
               transformAgarreActivo != null;
    }

    private bool DebeUsarOffsetEstablePorBillboardNormal()
    {
        // IMPORTANTE:
        // Esta lógica se desactivó porque dañaba el agarre de los paneles normales
        // como Diagrama, Principal y Progreso. Aunque el Inspector tenga este campo
        // activado por serialización anterior, aquí siempre devuelve false.
        // Los paneles normales vuelven a la lógica antigua: punto exacto de agarre.
        return false;
    }

    private Vector3 CalcularPosicionObjetivoAgarre()
    {
        if (DebeUsarAgarreEstableDiagrama())
        {
            return CalcularPosicionObjetivoAncladaDiagrama();
        }

        if (usarPuntoExactoDeAgarre && puntoAgarreValido)
        {
            Vector3 puntoDeseadoMundo = transformAgarreActivo.position + offsetAgarradorAPuntoAgarre;
            Vector3 puntoActualMundo = panelRoot.TransformPoint(puntoAgarreLocalEnPanel);
            Vector3 delta = puntoDeseadoMundo - puntoActualMundo;

            return panelRoot.position + delta;
        }

        return transformAgarreActivo.position + offsetPanel;
    }

    private Vector3 CalcularPosicionObjetivoAncladaDiagrama()
    {
        Vector3 anclaDeseadaMundo = ObtenerAnclaDeseadaMundoDiagrama();
        Vector3 anclaActualMundo = transform.TransformPoint(anclaLocalDiagramaEnHandle);
        Vector3 delta = anclaDeseadaMundo - anclaActualMundo;

        return panelRoot.position + delta;
    }

    private Vector3 ObtenerAnclaDeseadaMundoDiagrama()
    {
        if (transformAgarreActivo == null)
        {
            return transform.TransformPoint(anclaLocalDiagramaEnHandle);
        }

        return transformAgarreActivo.position + offsetInicialAgarradorAnclaDiagrama;
    }

    private void AplicarPosicionObjetivo(Vector3 posicionObjetivo)
    {
        bool movimientoDirectoFinal = movimientoDirecto ||
                                      (DebeUsarAgarreEstableDiagrama() && forzarMovimientoDirectoDiagrama);

        if (movimientoDirectoFinal)
        {
            panelRoot.position = posicionObjetivo;
        }
        else
        {
            panelRoot.position = Vector3.Lerp(
                panelRoot.position,
                posicionObjetivo,
                Mathf.Clamp01(Time.unscaledDeltaTime * Mathf.Max(0f, suavizadoMovimiento))
            );
        }
    }

    private void ReanclarDiagramaAlControlDespuesDeBillboard()
    {
        if (!agarrando || !DebeUsarAgarreEstableDiagrama())
        {
            return;
        }

        if (mantenerDiagramaMirandoMientrasAgarra && rotarDiagramaDesdeGrabHandleMientrasAgarra)
        {
            ActualizarDiagramaMirandoAlJugadorMientrasAgarra();
            return;
        }

        if (!reanclarDiagramaDespuesDeBillboard)
        {
            return;
        }

        // Modo alternativo: si el DiagramBillboard queda activo, reanclamos después de su LateUpdate.
        // Recomendado solo si Rotar Diagrama Desde GrabHandle está apagado.
        Vector3 posicionObjetivo = CalcularPosicionObjetivoAncladaDiagrama();
        panelRoot.position = posicionObjetivo;
    }

    private void ReanclarPanelNormalDespuesDeOrientacion()
    {
        if (!agarrando || panelRoot == null || transformAgarreActivo == null)
        {
            return;
        }

        if (DebeUsarAgarreEstableDiagrama())
        {
            return;
        }

        if (!usarPuntoExactoDeAgarre || !puntoAgarreValido)
        {
            return;
        }

        Vector3 puntoDeseadoMundo = transformAgarreActivo.position + offsetAgarradorAPuntoAgarre;
        Vector3 puntoActualMundo = panelRoot.TransformPoint(puntoAgarreLocalEnPanel);
        panelRoot.position += puntoDeseadoMundo - puntoActualMundo;
    }

    private void ActualizarDiagramaMirandoAlJugadorMientrasAgarra()
    {
        if (panelRoot == null || transformAgarreActivo == null)
        {
            return;
        }

        // 1) Primero aseguramos que el punto agarrado esté exactamente en el control.
        Vector3 anclaDeseadaMundo = ObtenerAnclaDeseadaMundoDiagrama();
        panelRoot.position = CalcularPosicionObjetivoAncladaDiagrama();

        // 2) Rotamos el panel hacia el jugador, pero sin usar DiagramBillboard para evitar doble dueño.
        if (CalcularRotacionObjetivoDiagrama(out Quaternion rotacionObjetivo))
        {
            if (suavizarMiradaDiagramaMientrasAgarra)
            {
                panelRoot.rotation = Quaternion.Slerp(
                    panelRoot.rotation,
                    rotacionObjetivo,
                    Mathf.Clamp01(
                        Time.unscaledDeltaTime * Mathf.Max(0.01f, suavizadoMiradaDiagramaMientrasAgarra)
                    )
                );
            }
            else
            {
                panelRoot.rotation = rotacionObjetivo;
            }

            // 3) La rotación puede mover el punto agarrado si el pivot del panel no está en la barra.
            // Compensamos la posición para que el punto agarrado siga fijo en el control.
            Vector3 anclaActualMundo = transform.TransformPoint(anclaLocalDiagramaEnHandle);
            panelRoot.position += anclaDeseadaMundo - anclaActualMundo;
        }
    }

    private bool CalcularRotacionObjetivoDiagrama(out Quaternion rotacionObjetivo)
    {
        rotacionObjetivo = panelRoot != null ? panelRoot.rotation : Quaternion.identity;

        Transform objetivoCamara = null;

        if (billboard != null && billboard.objetivoCamara != null)
        {
            objetivoCamara = billboard.objetivoCamara;
        }
        else if (Camera.main != null)
        {
            objetivoCamara = Camera.main.transform;
        }

        if (objetivoCamara == null || panelRoot == null)
        {
            return false;
        }

        Vector3 puntoBase = panelRoot.position;

        if (!usarRootComoPuntoMiradaDiagramaMientrasAgarra && billboard != null && billboard.puntoMiradaActual != null)
        {
            puntoBase = billboard.puntoMiradaActual.position;
        }

        Vector3 direccion = puntoBase - objetivoCamara.position;

        bool soloY = forzarSoloRotacionYDiagramaMientrasAgarra ||
                     (billboard != null && billboard.soloEjeY);

        if (soloY)
        {
            direccion.y = 0f;
        }

        if (direccion.sqrMagnitude < 0.001f)
        {
            return false;
        }

        return AlgoLabPanelFacing.TryGetStableRotation(
            direccion,
            soloY,
            Quaternion.identity,
            billboard != null && billboard.invertirFrente,
            out rotacionObjetivo
        );
    }

    private void RevisarMovimientoParaTutorial()
    {
        if (panelRoot == null)
        {
            return;
        }

        if (notificarMovimientoSoloUnaVez && movimientoTutorialNotificado)
        {
            return;
        }

        float distanciaMovida = Vector3.Distance(
            posicionPanelAlIniciarAgarre,
            panelRoot.position
        );

        if (distanciaMovida >= distanciaMinimaMovimientoTutorial)
        {
            movimientoTutorialNotificado = true;
            NotificarTutorialMovido();
        }
    }

    private bool DebeSeguirAgarrando()
    {
        switch (tipoAgarreActual)
        {
            case TipoAgarre.ControladorIzquierdo:
                return ObtenerGripIzquierdo() >= 0.15f;

            case TipoAgarre.ControladorDerecho:
                return ObtenerGripDerecho() >= 0.15f;

            case TipoAgarre.ManoIzquierda:
                return leftHand != null &&
                       leftHand.GetFingerPinchStrength(OVRHand.HandFinger.Index) >= 0.35f;

            case TipoAgarre.ManoDerecha:
                return rightHand != null &&
                       rightHand.GetFingerPinchStrength(OVRHand.HandFinger.Index) >= 0.35f;
        }

        return false;
    }

    private void TerminarAgarre()
    {
        agarrando = false;
        transformAgarreActivo = null;
        tipoAgarreActual = TipoAgarre.Ninguno;
        puntoAgarreValido = false;
        diagramaAncladoActivo = false;
        offsetInicialAgarradorAnclaDiagrama = Vector3.zero;
        anclaLocalDiagramaEnHandle = Vector3.zero;

        if (billboard != null && (billboardApagadoPorDiagrama || !mantenerBillboardActivoDuranteAgarre))
        {
            billboard.enabled = billboardEstabaActivo;
        }

        billboardApagadoPorDiagrama = false;
        LiberarPropiedadAgarre();

        NotificarTutorialSoltado();

        if (mostrarDebug)
        {
            Debug.Log("PANEL SOLTADO");
        }
    }

    private bool IntentarTomarPropiedadAgarre()
    {
        if (panelRoot == null)
        {
            return false;
        }

        if (AgarresPorPanel.TryGetValue(panelRoot, out AlgoLabPanelGrabHandle actual))
        {
            if (actual != null && actual != this && actual.agarrando && actual.isActiveAndEnabled)
            {
                return false;
            }

            AgarresPorPanel.Remove(panelRoot);
        }

        AgarresPorPanel[panelRoot] = this;
        return true;
    }

    private void LiberarPropiedadAgarre()
    {
        if (panelRoot == null)
        {
            return;
        }

        if (AgarresPorPanel.TryGetValue(panelRoot, out AlgoLabPanelGrabHandle actual) && actual == this)
        {
            AgarresPorPanel.Remove(panelRoot);
        }
    }

    private void NotificarTutorialAgarrado()
    {
        if (notificarTutorialInteractivo)
        {
            BuscarTutorialController();

            if (tutorialController != null)
            {
                tutorialController.NotificarPanelAgarrado(this);
            }
        }

        // Estos eventos son usados por Pocket / SpawnManager.
        // No deben depender del modo tutorial interactivo.
        alIniciarAgarre?.Invoke();
    }

    private void NotificarTutorialMovido()
    {
        if (notificarTutorialInteractivo)
        {
            BuscarTutorialController();

            if (tutorialController != null)
            {
                tutorialController.NotificarPanelMovido(this);
            }
        }

        alMoverPanel?.Invoke();

        if (mostrarDebug)
        {
            Debug.Log("PANEL MOVIDO PARA TUTORIAL");
        }
    }

    private void NotificarTutorialSoltado()
    {
        if (notificarTutorialInteractivo)
        {
            BuscarTutorialController();

            if (tutorialController != null)
            {
                tutorialController.NotificarPanelSoltado(this);
            }
        }

        alSoltarPanel?.Invoke();
    }

    private Vector3 ObtenerPuntoCercanoAlHandle(Vector3 posicion)
    {
        if (boxCollider == null)
        {
            return transform.position;
        }

        return boxCollider.ClosestPoint(posicion);
    }

    private float ObtenerDistanciaAlHandle(Vector3 posicion)
    {
        Vector3 puntoCercano = ObtenerPuntoCercanoAlHandle(posicion);
        return Vector3.Distance(posicion, puntoCercano);
    }

    private float ObtenerGripIzquierdo()
    {
        float valor1 = OVRInput.Get(
            OVRInput.Axis1D.PrimaryHandTrigger,
            OVRInput.Controller.LTouch
        );

        float valor2 = OVRInput.Get(
            OVRInput.Axis1D.PrimaryHandTrigger,
            OVRInput.Controller.Touch
        );

        return Mathf.Max(valor1, valor2);
    }

    private float ObtenerGripDerecho()
    {
        float valor1 = OVRInput.Get(
            OVRInput.Axis1D.PrimaryHandTrigger,
            OVRInput.Controller.RTouch
        );

        float valor2 = OVRInput.Get(
            OVRInput.Axis1D.SecondaryHandTrigger,
            OVRInput.Controller.Touch
        );

        return Mathf.Max(valor1, valor2);
    }
}
