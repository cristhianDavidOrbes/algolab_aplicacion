using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Meta.XR.MRUtilityKit;
using UnityEngine;
using UnityEngine.UI;

public class AlgoLabRoomScanGuard : MonoBehaviour
{
    public static bool JuegoBloqueadoPorEscaneo { get; private set; }
    public static bool CuartoValido { get; private set; }

    private enum RoomState
    {
        Valid,
        WaitingForMRUK,
        NoRoom,
        MissingFloor,
        MissingWalls,
        OutsideRoom
    }

    [Header("Revision")]
    [SerializeField] private float intervaloRevision = 1f;
    [SerializeField] private float retrasoInicial = 0.5f;
    [SerializeField] private float tiempoMaximoEsperaMRUK = 4f;
    [SerializeField] private bool revisarSoloConSesionIniciada = false;
    [SerializeField] private bool exigirPiso = true;
    [SerializeField] private bool exigirParedes = true;
    [SerializeField] private bool exigirEstarDentroDelCuarto = true;

    [Header("Escaneo")]
    [SerializeField] private bool abrirEscaneoAutomaticamente = true;
    [SerializeField] private float intervaloEntreSolicitudesEscaneo = 10f;

    [Header("Proteccion de objetos")]
    [SerializeField] private bool protegerObjetosSiCuartoNoEsValido = true;
    [SerializeField] private bool rescatarObjetosCaidos = true;
    [SerializeField] private float limiteYCaida = -3f;
    [SerializeField] private float distanciaRescateFrenteUsuario = 1.1f;
    [SerializeField] private float intervaloBusquedaObjetos = 1f;

    [Header("Aviso")]
    [SerializeField] private bool mostrarAvisoMundoDeRespaldo = true;
    [SerializeField] private float distanciaAviso = 1.25f;
    [SerializeField] private Vector2 tamanoAviso = new Vector2(0.95f, 0.28f);
    [SerializeField] private string mensajeSinCuarto =
        "No se detecta un cuarto escaneado. Escanea el lugar para recuperar piso y paredes.";
    [SerializeField] private string mensajeFueraCuarto =
        "Estas fuera del cuarto escaneado. Vuelve al area escaneada o escanea este lugar.";
    [SerializeField] private string mensajeSinPiso =
        "El cuarto escaneado no tiene piso valido. Escanea el lugar nuevamente.";
    [SerializeField] private string mensajeSinParedes =
        "El cuarto escaneado no tiene paredes validas. Escanea el lugar nuevamente.";

    [Header("Debug")]
    [SerializeField] private bool mostrarDebug = true;

    private readonly Dictionary<Rigidbody, RigidbodyState> rigidbodiesProtegidos = new Dictionary<Rigidbody, RigidbodyState>();
    private readonly Dictionary<Rigidbody, SafePose> ultimasPosicionesSeguras = new Dictionary<Rigidbody, SafePose>();
    private readonly List<Rigidbody> clavesTemporales = new List<Rigidbody>();

    private AlgoLabAISubtitlePanel subtitlePanel;
    private AlgoLabSessionManager sessionManager;
    private Transform cabezaUsuario;
    private Canvas avisoCanvas;
    private CanvasGroup avisoCanvasGroup;
    private Text avisoTexto;
    private Coroutine revisionRoutine;
    private bool cuartoValido;
    private bool pausaAplicada;
    private float escalaTiempoAntesDeBloquear = 1f;
#if UNITY_ANDROID && !UNITY_EDITOR
    private bool solicitudEscaneoEnCurso;
    private bool cargaEscaneoGuardadoEnCurso;
#endif
    private bool validacionSolicitada;
    private float proximaSolicitudEscaneo;
    private float inicioEsperaMRUK = -1f;
    private float proximaBusquedaObjetos;
    private string ultimoMensajeMostrado = "";
    private bool esInstanciaPrincipal;

    private static AlgoLabRoomScanGuard instancia;

    private struct RigidbodyState
    {
        public bool useGravity;
        public bool isKinematic;
        public RigidbodyConstraints constraints;
        public float linearDamping;
        public float angularDamping;
    }

    private struct SafePose
    {
        public Vector3 position;
        public Quaternion rotation;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InstalarAutomaticamente()
    {
        if (FindFirstObjectByType<AlgoLabRoomScanGuard>(FindObjectsInactive.Include) != null)
        {
            return;
        }

        GameObject guardObject = new GameObject("[ALGOLAB_ROOM_SCAN_GUARD]");
        DontDestroyOnLoad(guardObject);
        guardObject.AddComponent<AlgoLabRoomScanGuard>();
    }

    public static void NotificarJugarPresionado()
    {
        if (instancia != null)
        {
            instancia.IniciarValidacionDesdeJugar();
        }
    }

    public static void NotificarRetornoAlInicio()
    {
        if (instancia != null)
        {
            instancia.SuspenderValidacionHastaJugar();
        }
    }

    public static void NotificarMenuConfiguracionCerrado()
    {
        if (instancia == null)
        {
            return;
        }

        if (instancia.cuartoValido || !instancia.validacionSolicitada)
        {
            instancia.RestaurarJuegoTrasEscaneo();
            return;
        }

        // El menú restaura su propia pausa antes de notificarnos. Si el
        // escaneo sigue siendo inválido, recuperamos inmediatamente el bloqueo
        // para que cerrar Configuración nunca permita jugar fuera del cuarto.
        instancia.BloquearJuegoPorEscaneo();
    }

    private void Awake()
    {
        if (instancia != null && instancia != this)
        {
            Destroy(gameObject);
            return;
        }

        instancia = this;
        esInstanciaPrincipal = true;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        if (!esInstanciaPrincipal)
        {
            return;
        }

        BuscarReferencias();

        if (revisionRoutine == null)
        {
            revisionRoutine = StartCoroutine(RevisionPeriodica());
        }
    }

    private void OnDisable()
    {
        if (!esInstanciaPrincipal)
        {
            return;
        }

        if (revisionRoutine != null)
        {
            StopCoroutine(revisionRoutine);
            revisionRoutine = null;
        }

        RestaurarFisicaProtegida();
        RestaurarJuegoTrasEscaneo();
        ConfigurarVisibilidadLimite(false);
        OcultarAviso();
    }

    private void OnDestroy()
    {
        if (!esInstanciaPrincipal)
        {
            return;
        }

        if (instancia == this)
        {
            instancia = null;
        }

        RestaurarFisicaProtegida();
        RestaurarJuegoTrasEscaneo();
        ConfigurarVisibilidadLimite(false);

        if (avisoCanvas != null)
        {
            Destroy(avisoCanvas.gameObject);
        }
    }

    private void Update()
    {
        if (avisoCanvas != null && avisoCanvasGroup != null && avisoCanvasGroup.alpha > 0.01f)
        {
            ActualizarAvisoFrenteUsuario();
        }
    }

    private void FixedUpdate()
    {
        if (!cuartoValido && protegerObjetosSiCuartoNoEsValido)
        {
            MantenerObjetosProtegidos();
        }
    }

    private IEnumerator RevisionPeriodica()
    {
        if (retrasoInicial > 0f)
        {
            yield return new WaitForSecondsRealtime(retrasoInicial);
        }

        WaitForSecondsRealtime espera = new WaitForSecondsRealtime(Mathf.Max(1f, intervaloRevision));

        while (enabled)
        {
            RevisarCuarto();
            yield return espera;
        }
    }

    private void RevisarCuarto()
    {
        BuscarReferencias();

#if UNITY_EDITOR
        // El editor no dispone del Room Setup del visor. Las pruebas locales
        // no deben quedar pausadas por una capacidad exclusiva de Quest.
        MarcarCuartoValido();
#else
        if (!validacionSolicitada)
        {
            MantenerGuardiaEnEspera();
            return;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        if (cargaEscaneoGuardadoEnCurso)
        {
            MantenerEstadoMientrasMRUKCarga();
            return;
        }
#endif

        if (DebeEsperarSesion())
        {
            MarcarCuartoValido();
            return;
        }

        RoomState estado = ObtenerEstadoCuarto();

        if (estado == RoomState.WaitingForMRUK)
        {
            if (inicioEsperaMRUK < 0f)
            {
                inicioEsperaMRUK = Time.unscaledTime;
            }

            if (Time.unscaledTime - inicioEsperaMRUK < Mathf.Max(1f, tiempoMaximoEsperaMRUK))
            {
                MantenerEstadoMientrasMRUKCarga();
                return;
            }

            estado = RoomState.NoRoom;
        }
        else
        {
            inicioEsperaMRUK = -1f;
        }

        if (estado == RoomState.Valid)
        {
            MarcarCuartoValido();
            return;
        }

        cuartoValido = false;
        CuartoValido = false;
        BloquearJuegoPorEscaneo();
        ConfigurarVisibilidadLimite(false);

        string mensaje = ObtenerMensaje(estado);
        MostrarAviso(mensaje);

        if (protegerObjetosSiCuartoNoEsValido)
        {
            AplicarProteccionFisica();
        }

        SolicitarEscaneoSiHaceFalta();
#endif
    }

    private bool DebeEsperarSesion()
    {
        if (!revisarSoloConSesionIniciada)
        {
            return false;
        }

        if (sessionManager == null)
        {
            return false;
        }

        return !sessionManager.SesionIniciada;
    }

    private RoomState ObtenerEstadoCuarto()
    {
        MRUK mruk = MRUK.Instance;

        if (mruk == null)
        {
            return RoomState.WaitingForMRUK;
        }

        if (!mruk.IsInitialized)
        {
            return RoomState.WaitingForMRUK;
        }

        MRUKRoom room = mruk.GetCurrentRoom();

        if (room == null)
        {
            return RoomState.NoRoom;
        }

        if (exigirPiso && room.FloorAnchors.Count == 0)
        {
            return RoomState.MissingFloor;
        }

        if (exigirParedes && room.WallAnchors.Count == 0)
        {
            return RoomState.MissingWalls;
        }

        if (exigirEstarDentroDelCuarto)
        {
            Vector3 posicionUsuario = ObtenerPosicionUsuario();

            if (!room.IsPositionInRoom(posicionUsuario, false))
            {
                return RoomState.OutsideRoom;
            }
        }

        return RoomState.Valid;
    }

    private void MarcarCuartoValido()
    {
        cuartoValido = true;
        CuartoValido = true;
        inicioEsperaMRUK = -1f;
        ultimoMensajeMostrado = "";
        OcultarAviso();
        ActualizarUltimasPosicionesSeguras();
        RestaurarFisicaProtegida();
        RestaurarJuegoTrasEscaneo();
        ConfigurarVisibilidadLimite(true);
    }

    private void MantenerEstadoMientrasMRUKCarga()
    {
        cuartoValido = false;
        CuartoValido = false;
        BloquearJuegoPorEscaneo();
        ConfigurarVisibilidadLimite(false);
        // Todavía no sabemos si falta un escaneo: Meta puede estar cargando
        // el modelo guardado. Evita mostrar un falso aviso en esta espera.
        OcultarAviso();

        if (protegerObjetosSiCuartoNoEsValido)
        {
            AplicarProteccionFisica();
        }
    }

    private void MantenerGuardiaEnEspera()
    {
        cuartoValido = false;
        CuartoValido = false;
        inicioEsperaMRUK = -1f;
        ultimoMensajeMostrado = "";
        OcultarAviso();
        RestaurarFisicaProtegida();
        RestaurarJuegoTrasEscaneo();
        ConfigurarVisibilidadLimite(false);
    }

    private void IniciarValidacionDesdeJugar()
    {
        validacionSolicitada = true;
        inicioEsperaMRUK = -1f;
        ultimoMensajeMostrado = "";
        OcultarAviso();

#if UNITY_ANDROID && !UNITY_EDITOR
        if (!cargaEscaneoGuardadoEnCurso)
        {
            _ = CargarEscaneoGuardadoAntesDeValidarAsync();
        }
#else
        RevisarCuarto();
#endif
    }

    private void SuspenderValidacionHastaJugar()
    {
        validacionSolicitada = false;
        MantenerGuardiaEnEspera();
    }

    private string ObtenerMensaje(RoomState estado)
    {
        switch (estado)
        {
            case RoomState.OutsideRoom:
                return mensajeFueraCuarto;
            case RoomState.MissingFloor:
                return mensajeSinPiso;
            case RoomState.MissingWalls:
                return mensajeSinParedes;
            default:
                return mensajeSinCuarto;
        }
    }

    private void MostrarAviso(string mensaje)
    {
        if (string.IsNullOrWhiteSpace(mensaje))
        {
            return;
        }

        bool mensajeCambio = ultimoMensajeMostrado != mensaje;

        if (mensajeCambio && subtitlePanel != null)
        {
            subtitlePanel.ShowSystemSubtitle(mensaje);
        }

        if (!mostrarAvisoMundoDeRespaldo)
        {
            ultimoMensajeMostrado = mensaje;
            return;
        }

        PrepararAvisoMundo();

        if (avisoCanvasGroup == null || avisoTexto == null)
        {
            return;
        }

        avisoTexto.text = mensaje;
        avisoCanvasGroup.alpha = 1f;
        avisoCanvasGroup.blocksRaycasts = true;
        avisoCanvasGroup.interactable = true;
        ActualizarAvisoFrenteUsuario();

        if (mostrarDebug && mensajeCambio)
        {
            Debug.Log("[AlgoLabRoomScanGuard] " + mensaje);
        }

        ultimoMensajeMostrado = mensaje;
    }

    private void OcultarAviso()
    {
        if (avisoCanvasGroup != null)
        {
            avisoCanvasGroup.alpha = 0f;
            avisoCanvasGroup.blocksRaycasts = false;
            avisoCanvasGroup.interactable = false;
        }
    }

    private void PrepararAvisoMundo()
    {
        if (avisoCanvas != null)
        {
            return;
        }

        GameObject canvasObject = new GameObject("[ALGOLAB_ROOM_SCAN_WARNING]");
        DontDestroyOnLoad(canvasObject);

        avisoCanvas = canvasObject.AddComponent<Canvas>();
        avisoCanvas.renderMode = RenderMode.WorldSpace;
        avisoCanvas.sortingOrder = 5000;

        avisoCanvasGroup = canvasObject.AddComponent<CanvasGroup>();
        avisoCanvasGroup.alpha = 0f;
        avisoCanvasGroup.blocksRaycasts = false;
        avisoCanvasGroup.interactable = false;

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(tamanoAviso.x * 1000f, tamanoAviso.y * 1000f);

        GameObject fondoObject = new GameObject("Fondo");
        fondoObject.transform.SetParent(canvasObject.transform, false);

        Image fondo = fondoObject.AddComponent<Image>();
        fondo.color = new Color(0.02f, 0.02f, 0.02f, 0.82f);

        RectTransform fondoRect = fondoObject.GetComponent<RectTransform>();
        fondoRect.anchorMin = Vector2.zero;
        fondoRect.anchorMax = Vector2.one;
        fondoRect.offsetMin = Vector2.zero;
        fondoRect.offsetMax = Vector2.zero;

        GameObject textoObject = new GameObject("Texto");
        textoObject.transform.SetParent(canvasObject.transform, false);

        avisoTexto = textoObject.AddComponent<Text>();
        avisoTexto.alignment = TextAnchor.MiddleCenter;
        avisoTexto.color = Color.white;
        avisoTexto.fontSize = 34;
        avisoTexto.resizeTextForBestFit = true;
        avisoTexto.resizeTextMinSize = 18;
        avisoTexto.resizeTextMaxSize = 34;
        avisoTexto.horizontalOverflow = HorizontalWrapMode.Wrap;
        avisoTexto.verticalOverflow = VerticalWrapMode.Truncate;
        avisoTexto.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        if (avisoTexto.font == null)
        {
            avisoTexto.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        RectTransform textoRect = textoObject.GetComponent<RectTransform>();
        textoRect.anchorMin = Vector2.zero;
        textoRect.anchorMax = Vector2.one;
        textoRect.offsetMin = new Vector2(24f, 12f);
        textoRect.offsetMax = new Vector2(-24f, -12f);
    }

    private void ActualizarAvisoFrenteUsuario()
    {
        BuscarCabezaUsuario();

        if (avisoCanvas == null || cabezaUsuario == null)
        {
            return;
        }

        Vector3 forward = cabezaUsuario.forward;

        if (forward.sqrMagnitude < 0.001f)
        {
            forward = Vector3.forward;
        }

        avisoCanvas.transform.position = cabezaUsuario.position + forward.normalized * distanciaAviso;
        avisoCanvas.transform.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
        avisoCanvas.transform.localScale = Vector3.one * 0.001f;
    }

    private void AplicarProteccionFisica()
    {
        if (Time.unscaledTime < proximaBusquedaObjetos)
        {
            return;
        }

        proximaBusquedaObjetos = Time.unscaledTime + Mathf.Max(0.1f, intervaloBusquedaObjetos);

        SimpleMRGrabbable[] grabbables = FindObjectsByType<SimpleMRGrabbable>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < grabbables.Length; i++)
        {
            SimpleMRGrabbable grabbable = grabbables[i];

            if (grabbable == null || grabbable.IsGrabbed)
            {
                continue;
            }

            Rigidbody rb = ObtenerRigidbody(grabbable);

            if (rb == null)
            {
                continue;
            }

            ProtegerRigidbody(rb);
        }
    }

    private void BloquearJuegoPorEscaneo()
    {
        if (!pausaAplicada)
        {
            AlgoLabSettingsMenuController settings =
                AlgoLabSettingsMenuController.Instance;
            escalaTiempoAntesDeBloquear =
                settings != null && settings.MenuAbierto
                    ? settings.EscalaTiempoAntesDePausa
                    : Time.timeScale;
            pausaAplicada = true;
            LiberarObjetosAgarrados();
        }
        JuegoBloqueadoPorEscaneo = true;
        Time.timeScale = 0f;
    }

    private void RestaurarJuegoTrasEscaneo()
    {
        JuegoBloqueadoPorEscaneo = false;
        if (!pausaAplicada)
            return;

        AlgoLabSettingsMenuController settings =
            AlgoLabSettingsMenuController.Instance;
        if (settings != null && settings.MenuAbierto)
        {
            // La validación terminó, pero el menú sigue siendo dueño de la
            // pausa. La escala se restaura cuando se cierre la configuración.
            return;
        }

        Time.timeScale = escalaTiempoAntesDeBloquear;
        pausaAplicada = false;
    }

    private static void LiberarObjetosAgarrados()
    {
        SimpleOvRGrabber[] controles =
            FindObjectsByType<SimpleOvRGrabber>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );
        for (int i = 0; i < controles.Length; i++)
            controles[i]?.SoltarObjetoActualSinImpulso();

        SimpleOVRHandGrabber[] manos =
            FindObjectsByType<SimpleOVRHandGrabber>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );
        for (int i = 0; i < manos.Length; i++)
            manos[i]?.SoltarObjetoActualSinImpulso();
    }

    private static void ConfigurarVisibilidadLimite(bool suprimir)
    {
        if (OVRManager.instance != null)
        {
            OVRManager.instance.shouldBoundaryVisibilityBeSuppressed =
                suprimir;
        }
    }

    private void MantenerObjetosProtegidos()
    {
        AplicarProteccionFisica();

        clavesTemporales.Clear();

        foreach (KeyValuePair<Rigidbody, RigidbodyState> kvp in rigidbodiesProtegidos)
        {
            clavesTemporales.Add(kvp.Key);
        }

        for (int i = 0; i < clavesTemporales.Count; i++)
        {
            Rigidbody rb = clavesTemporales[i];

            if (rb == null)
            {
                rigidbodiesProtegidos.Remove(rb);
                continue;
            }

            SimpleMRGrabbable grabbable = rb.GetComponent<SimpleMRGrabbable>();
            if (grabbable != null && grabbable.IsGrabbed)
            {
                RestaurarRigidbodyProtegido(rb);
                continue;
            }

            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
            rb.constraints = RigidbodyConstraints.FreezeAll;

            if (rescatarObjetosCaidos && rb.position.y < limiteYCaida)
            {
                RescatarRigidbody(rb);
            }
        }
    }

    private void ProtegerRigidbody(Rigidbody rb)
    {
        if (!rigidbodiesProtegidos.ContainsKey(rb))
        {
            rigidbodiesProtegidos.Add(rb, new RigidbodyState
            {
                useGravity = rb.useGravity,
                isKinematic = rb.isKinematic,
                constraints = rb.constraints,
                linearDamping = rb.linearDamping,
                angularDamping = rb.angularDamping
            });
        }

        rb.useGravity = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;
        rb.constraints = RigidbodyConstraints.FreezeAll;

        if (rescatarObjetosCaidos && rb.position.y < limiteYCaida)
        {
            RescatarRigidbody(rb);
        }
    }

    private void RestaurarFisicaProtegida()
    {
        if (rigidbodiesProtegidos.Count == 0)
        {
            return;
        }

        clavesTemporales.Clear();

        foreach (KeyValuePair<Rigidbody, RigidbodyState> kvp in rigidbodiesProtegidos)
        {
            clavesTemporales.Add(kvp.Key);
        }

        for (int i = 0; i < clavesTemporales.Count; i++)
        {
            Rigidbody rb = clavesTemporales[i];

            if (rb == null)
            {
                rigidbodiesProtegidos.Remove(rb);
                continue;
            }

            SimpleMRGrabbable grabbable = rb.GetComponent<SimpleMRGrabbable>();

            if (grabbable != null && grabbable.IsGrabbed)
            {
                continue;
            }

            RestaurarRigidbodyProtegido(rb);
        }
    }

    private void RestaurarRigidbodyProtegido(Rigidbody rb)
    {
        if (rb == null || !rigidbodiesProtegidos.TryGetValue(rb, out RigidbodyState estadoOriginal))
        {
            return;
        }

        rb.useGravity = estadoOriginal.useGravity;
        rb.isKinematic = estadoOriginal.isKinematic;
        rb.constraints = estadoOriginal.constraints;
        rb.linearDamping = estadoOriginal.linearDamping;
        rb.angularDamping = estadoOriginal.angularDamping;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rigidbodiesProtegidos.Remove(rb);
    }

    private void ActualizarUltimasPosicionesSeguras()
    {
        clavesTemporales.Clear();

        foreach (KeyValuePair<Rigidbody, SafePose> kvp in ultimasPosicionesSeguras)
        {
            if (kvp.Key == null)
            {
                clavesTemporales.Add(kvp.Key);
            }
        }

        for (int i = 0; i < clavesTemporales.Count; i++)
        {
            ultimasPosicionesSeguras.Remove(clavesTemporales[i]);
        }

        SimpleMRGrabbable[] grabbables = FindObjectsByType<SimpleMRGrabbable>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < grabbables.Length; i++)
        {
            Rigidbody rb = ObtenerRigidbody(grabbables[i]);

            if (rb == null || rb.position.y < limiteYCaida)
            {
                continue;
            }

            ultimasPosicionesSeguras[rb] = new SafePose
            {
                position = rb.position,
                rotation = rb.rotation
            };
        }
    }

    private void RescatarRigidbody(Rigidbody rb)
    {
        if (rb == null)
        {
            return;
        }

        Vector3 posicionRescate;
        Quaternion rotacionRescate;

        if (ultimasPosicionesSeguras.TryGetValue(rb, out SafePose pose))
        {
            posicionRescate = pose.position;
            rotacionRescate = pose.rotation;
        }
        else
        {
            BuscarCabezaUsuario();

            if (cabezaUsuario != null)
            {
                Vector3 forwardPlano = Vector3.ProjectOnPlane(cabezaUsuario.forward, Vector3.up);

                if (forwardPlano.sqrMagnitude < 0.001f)
                {
                    forwardPlano = Vector3.forward;
                }

                posicionRescate = cabezaUsuario.position + forwardPlano.normalized * distanciaRescateFrenteUsuario;
                posicionRescate.y = cabezaUsuario.position.y - 0.25f;
                rotacionRescate = Quaternion.LookRotation(forwardPlano.normalized, Vector3.up);
            }
            else
            {
                posicionRescate = Vector3.up;
                rotacionRescate = Quaternion.identity;
            }
        }

        rb.position = posicionRescate;
        rb.rotation = rotacionRescate;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    private Rigidbody ObtenerRigidbody(SimpleMRGrabbable grabbable)
    {
        if (grabbable == null)
        {
            return null;
        }

        if (grabbable.Rigidbody != null)
        {
            return grabbable.Rigidbody;
        }

        return grabbable.GetComponent<Rigidbody>();
    }

    private Vector3 ObtenerPosicionUsuario()
    {
        BuscarCabezaUsuario();

        if (cabezaUsuario != null)
        {
            return cabezaUsuario.position;
        }

        return Vector3.zero;
    }

    private void BuscarReferencias()
    {
        if (subtitlePanel == null)
        {
            subtitlePanel = FindFirstObjectByType<AlgoLabAISubtitlePanel>(FindObjectsInactive.Include);
        }

        if (sessionManager == null)
        {
            sessionManager = AlgoLabSessionManager.Instance != null
                ? AlgoLabSessionManager.Instance
                : FindFirstObjectByType<AlgoLabSessionManager>(FindObjectsInactive.Include);
        }

        BuscarCabezaUsuario();
    }

    private void BuscarCabezaUsuario()
    {
        if (cabezaUsuario != null)
        {
            return;
        }

        Camera mainCamera = Camera.main;

        if (mainCamera != null)
        {
            cabezaUsuario = mainCamera.transform;
            return;
        }

        Camera[] cameras = FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        for (int i = 0; i < cameras.Length; i++)
        {
            if (cameras[i] != null && cameras[i].enabled)
            {
                cabezaUsuario = cameras[i].transform;
                return;
            }
        }
    }

    private void SolicitarEscaneoSiHaceFalta()
    {
        if (!abrirEscaneoAutomaticamente)
        {
            return;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        if (solicitudEscaneoEnCurso)
        {
            return;
        }
#endif

        if (Time.unscaledTime < proximaSolicitudEscaneo)
        {
            return;
        }

        proximaSolicitudEscaneo = Time.unscaledTime + Mathf.Max(10f, intervaloEntreSolicitudesEscaneo);

#if UNITY_ANDROID && !UNITY_EDITOR
        _ = SolicitarEscaneoAndroidAsync();
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private async Task CargarEscaneoGuardadoAntesDeValidarAsync()
    {
        cargaEscaneoGuardadoEnCurso = true;
        BloquearJuegoPorEscaneo();
        OcultarAviso();

        try
        {
            float limite = Time.realtimeSinceStartup + 6f;
            while (MRUK.Instance == null &&
                   Time.realtimeSinceStartup < limite)
            {
                await Task.Yield();
            }

            MRUK mruk = MRUK.Instance;
            if (mruk == null)
            {
                return;
            }

            bool yaHayCuarto =
                mruk.IsInitialized &&
                mruk.GetCurrentRoom() != null;

            if (!yaHayCuarto && await MRUK.HasSceneModel())
            {
                await mruk.LoadSceneFromDevice(false);
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning(
                "[AlgoLabRoomScanGuard] No se pudo cargar el escaneo guardado antes de jugar: " +
                ex.Message
            );
        }
        finally
        {
            cargaEscaneoGuardadoEnCurso = false;
            inicioEsperaMRUK = -1f;
            RevisarCuarto();
        }
    }

    private async Task SolicitarEscaneoAndroidAsync()
    {
        solicitudEscaneoEnCurso = true;

        try
        {
            MRUK mruk = MRUK.Instance;

            // Antes de abrir Room Setup, intenta recuperar el modelo ya
            // guardado en el visor. Así un cuarto válido no vuelve a pedir
            // escaneo cada vez que se abre la aplicación.
            if (mruk != null && await MRUK.HasSceneModel())
            {
                await mruk.LoadSceneFromDevice(false);

                if (ObtenerEstadoCuarto() == RoomState.Valid)
                {
                    MarcarCuartoValido();
                    return;
                }
            }

            bool espacioCapturado = await OVRScene.RequestSpaceSetup();

            if (!espacioCapturado || MRUK.Instance == null)
            {
                return;
            }

            if (await MRUK.HasSceneModel())
            {
                await MRUK.Instance.LoadSceneFromDevice(false);
                RevisarCuarto();
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[AlgoLabRoomScanGuard] No se pudo abrir o recargar el escaneo: " + ex.Message);
        }
        finally
        {
            solicitudEscaneoEnCurso = false;
        }
    }
#endif
}
