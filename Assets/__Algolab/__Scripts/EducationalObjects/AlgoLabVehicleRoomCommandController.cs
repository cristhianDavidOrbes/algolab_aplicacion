using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AlgoLabVehicleRoomCommandController : MonoBehaviour
{
    public enum HandSide
    {
        Left,
        Right
    }

    [Header("Control")]
    public HandSide handSide = HandSide.Right;

    [Tooltip("Evita que una copia colocada en OVRHand duplique los comandos del mando fisico.")]
    public bool desactivarEnTrackingDeManos = true;

    [Tooltip("Normalmente es el mismo objeto del controlador o el origen del rayo.")]
    public Transform rayOrigin;

    [Header("Gatillo delantero")]
    public float triggerThreshold = 0.55f;
    public float maxDistance = 8f;
    public float intervaloActualizarDestino = 0.08f;

    [Header("Capas")]
    [Tooltip("Aquí deben estar las capas del cuarto: piso, paredes, malla MRUK, RoomMesh, etc.")]
    public LayerMask roomLayers = ~0;

    [Tooltip("Aquí pon la capa de paneles/UI si tus paneles tienen Collider.")]
    public LayerMask panelBlockerLayers = 0;

    [Header("Bloqueo por paneles UI")]
    public bool bloquearSiRayoTocaPanelUI = true;

    [Tooltip("Arrastra aquí ClassDiagramRoot, ProgressPanel, VoicePanel o cualquier panel principal.")]
    public List<RectTransform> panelRoots = new List<RectTransform>();

    public bool buscarPanelesAutomaticamente = true;
    public bool soloCanvasWorldSpace = true;

    [Header("Vehículos")]
    public bool comandarTodosLosVehiculos = true;
    public bool ignorarVehiculosDestruidos = true;

    [Header("Marcador visual opcional")]
    public bool mostrarMarcadorDestino = true;
    public float escalaMarcador = 0.08f;
    public Color colorMarcador = new Color(0f, 1f, 0.75f, 1f);

    [Header("Respaldo si el cuarto no tiene collider")]
    [Tooltip("Si el rayo no encuentra la malla del cuarto, usa un plano horizontal a la altura de los vehiculos.")]
    public bool usarPlanoHorizontalDeRespaldo = true;

    [Tooltip("Evita aceptar un punto de respaldo demasiado cerca del mando.")]
    [Min(0f)] public float distanciaMinimaDestino = 0.25f;

    [Header("Debug")]
    public bool mostrarDebug = true;

    private float tiempoUltimoComando;
    private GameObject marcadorDestino;

    private OVRInput.Axis1D TriggerAxis
    {
        get
        {
            // Al consultar un controlador concreto, PrimaryIndexTrigger representa
            // el gatillo de ESE controlador. SecondaryIndexTrigger + RTouch no tiene
            // mapeo en OVRInput y siempre devolvia cero en el mando derecho.
            return OVRInput.Axis1D.PrimaryIndexTrigger;
        }
    }

    private OVRInput.Controller Controller
    {
        get
        {
            return handSide == HandSide.Left
                ? OVRInput.Controller.LTouch
                : OVRInput.Controller.RTouch;
        }
    }

    private void Awake()
    {
        // Algunas versiones antiguas de la escena añadieron este componente tanto
        // al mando como a la mano rastreada. Las dos copias leian el mismo gatillo
        // OVR y enviaban cada destino dos veces. Este controlador no implementa
        // pinza de manos, por lo que la copia en OVRHand debe permanecer inactiva.
        if (desactivarEnTrackingDeManos && GetComponent<OVRHand>() != null)
        {
            enabled = false;
            return;
        }

        if (rayOrigin == null)
        {
            rayOrigin = transform;
        }

        if (buscarPanelesAutomaticamente)
        {
            BuscarPanelesAutomaticamente();
        }

        CrearMarcadorSiHaceFalta();
    }

    private void Update()
    {
        if (rayOrigin == null)
        {
            return;
        }

        float triggerValue = LeerValorGatillo();
        bool manteniendoGatillo = triggerValue >= triggerThreshold;

        if (!manteniendoGatillo)
        {
            OcultarMarcador();
            return;
        }

        if (Time.time - tiempoUltimoComando < intervaloActualizarDestino)
        {
            return;
        }

        tiempoUltimoComando = Time.time;

        if (!ObtenerPuntoValidoDelCuarto(out Vector3 puntoDestino))
        {
            OcultarMarcador();
            return;
        }

        MostrarMarcador(puntoDestino);
        EnviarVehiculosADestino(puntoDestino);
    }

    private float LeerValorGatillo()
    {
        float valor = 0f;

        try
        {
            valor = OVRInput.Get(TriggerAxis, Controller);

            OVRInput.RawAxis1D rawAxis = handSide == HandSide.Left
                ? OVRInput.RawAxis1D.LIndexTrigger
                : OVRInput.RawAxis1D.RIndexTrigger;

            valor = Mathf.Max(valor, OVRInput.Get(rawAxis));

            OVRInput.Axis1D touchAxis = handSide == HandSide.Left
                ? OVRInput.Axis1D.PrimaryIndexTrigger
                : OVRInput.Axis1D.SecondaryIndexTrigger;

            valor = Mathf.Max(valor, OVRInput.Get(touchAxis, OVRInput.Controller.Touch));
        }
        catch (System.Exception exception)
        {
            if (mostrarDebug)
            {
                Debug.LogWarning("COMANDO VEHICULOS: no se pudo leer el gatillo de " +
                                 handSide + ": " + exception.Message);
            }
        }

        return valor;
    }

    private bool ObtenerPuntoValidoDelCuarto(out Vector3 puntoDestino)
    {
        puntoDestino = Vector3.zero;

        Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);

        float distanciaPanel = float.MaxValue;

        if (panelBlockerLayers.value != 0)
        {
            if (Physics.Raycast(
                    ray,
                    out RaycastHit hitPanel,
                    maxDistance,
                    panelBlockerLayers,
                    QueryTriggerInteraction.Collide
                ))
            {
                distanciaPanel = hitPanel.distance;
            }
        }

        if (bloquearSiRayoTocaPanelUI)
        {
            if (RayoTocaAlgunPanelUI(ray, out float distanciaUIPanel))
            {
                distanciaPanel = Mathf.Min(distanciaPanel, distanciaUIPanel);
            }
        }

        RaycastHit[] hits = Physics.RaycastAll(
            ray,
            maxDistance,
            roomLayers,
            QueryTriggerInteraction.Ignore
        );

        if (hits != null && hits.Length > 0)
        {
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit hit = hits[i];

                if (hit.collider == null)
                {
                    continue;
                }

                if (hit.distance > distanciaPanel)
                {
                    return false;
                }

                if (ColliderPerteneceAlControl(hit.collider))
                {
                    continue;
                }

                if (hit.collider.GetComponentInParent<AlgoLabLevel02VehicleObject>() != null)
                {
                    continue;
                }

                if (hit.collider.GetComponentInParent<SimpleMRGrabbable>() != null)
                {
                    continue;
                }

                if (hit.collider.GetComponentInParent<Canvas>() != null)
                {
                    continue;
                }

                puntoDestino = hit.point;

                if (mostrarDebug)
                {
                    Debug.Log("COMANDO VEHICULOS: punto válido del cuarto = " + puntoDestino);
                }

                return true;
            }
        }

        if (distanciaPanel < float.MaxValue)
        {
            return false;
        }

        return IntentarPuntoEnPlanoHorizontal(ray, out puntoDestino);
    }

    private bool ColliderPerteneceAlControl(Collider collider)
    {
        if (collider == null)
        {
            return false;
        }

        Transform hitTransform = collider.transform;
        return hitTransform == transform ||
               hitTransform.IsChildOf(transform) ||
               transform.IsChildOf(hitTransform);
    }

    private bool IntentarPuntoEnPlanoHorizontal(Ray ray, out Vector3 puntoDestino)
    {
        puntoDestino = Vector3.zero;

        if (!usarPlanoHorizontalDeRespaldo)
        {
            return false;
        }

        AlgoLabLevel02VehicleObject[] vehiculos =
            FindObjectsByType<AlgoLabLevel02VehicleObject>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );

        if (vehiculos == null || vehiculos.Length == 0)
        {
            return false;
        }

        float alturaPromedio = 0f;
        int cantidad = 0;

        for (int i = 0; i < vehiculos.Length; i++)
        {
            if (vehiculos[i] == null)
            {
                continue;
            }

            alturaPromedio += vehiculos[i].transform.position.y;
            cantidad++;
        }

        if (cantidad == 0)
        {
            return false;
        }

        Plane plano = new Plane(Vector3.up, new Vector3(0f, alturaPromedio / cantidad, 0f));
        if (!plano.Raycast(ray, out float distancia) ||
            distancia < distanciaMinimaDestino ||
            distancia > maxDistance)
        {
            return false;
        }

        puntoDestino = ray.GetPoint(distancia);

        if (mostrarDebug)
        {
            Debug.Log("COMANDO VEHICULOS: usando plano horizontal de respaldo = " + puntoDestino);
        }

        return true;
    }

    private bool RayoTocaAlgunPanelUI(Ray ray, out float distanciaMasCercana)
    {
        distanciaMasCercana = float.MaxValue;

        if (panelRoots == null || panelRoots.Count == 0)
        {
            return false;
        }

        bool encontroPanel = false;

        for (int i = 0; i < panelRoots.Count; i++)
        {
            RectTransform panel = panelRoots[i];

            if (panel == null)
            {
                continue;
            }

            if (!panel.gameObject.activeInHierarchy || !PanelEsVisible(panel))
            {
                continue;
            }

            if (RayoTocaContenidoVisibleDelPanel(ray, panel, out float distancia))
            {
                if (distancia >= 0f && distancia <= maxDistance)
                {
                    encontroPanel = true;
                    distanciaMasCercana = Mathf.Min(distanciaMasCercana, distancia);
                }
            }
        }

        return encontroPanel;
    }

    private bool PanelEsVisible(RectTransform panel)
    {
        Canvas canvas = panel.GetComponent<Canvas>();
        if (canvas != null && !canvas.enabled)
        {
            return false;
        }

        CanvasGroup[] groups = panel.GetComponentsInParent<CanvasGroup>(true);
        float alpha = 1f;

        for (int i = 0; i < groups.Length; i++)
        {
            if (!groups[i].gameObject.activeInHierarchy)
            {
                return false;
            }

            alpha *= groups[i].alpha;
        }

        return alpha > 0.05f;
    }

    private bool RayoTocaContenidoVisibleDelPanel(
        Ray ray,
        RectTransform panel,
        out float distanciaMasCercana)
    {
        distanciaMasCercana = float.MaxValue;
        Graphic[] graphics = panel.GetComponentsInChildren<Graphic>(false);
        bool encontro = false;

        for (int i = 0; i < graphics.Length; i++)
        {
            Graphic graphic = graphics[i];
            if (graphic == null || !graphic.enabled || !graphic.raycastTarget ||
                graphic.color.a <= 0.01f)
            {
                continue;
            }

            RectTransform rect = graphic.rectTransform;
            if (rect == null || !PanelEsVisible(rect))
            {
                continue;
            }

            if (RayoTocaRectTransform(ray, rect, out float distancia) &&
                distancia >= 0f && distancia <= maxDistance)
            {
                encontro = true;
                distanciaMasCercana = Mathf.Min(distanciaMasCercana, distancia);
            }
        }

        return encontro;
    }

    private bool RayoTocaRectTransform(
        Ray ray,
        RectTransform rect,
        out float distancia
    )
    {
        distancia = 0f;

        if (rect == null)
        {
            return false;
        }

        Plane plano = new Plane(rect.forward, rect.position);

        if (!plano.Raycast(ray, out distancia))
        {
            return false;
        }

        if (distancia < 0f)
        {
            return false;
        }

        Vector3 puntoMundo = ray.GetPoint(distancia);
        Vector3 puntoLocal3D = rect.InverseTransformPoint(puntoMundo);
        Vector2 puntoLocal = new Vector2(puntoLocal3D.x, puntoLocal3D.y);

        return rect.rect.Contains(puntoLocal);
    }

    private void EnviarVehiculosADestino(Vector3 puntoDestino)
    {
        AlgoLabLevel02VehicleObject[] vehiculos =
            FindObjectsByType<AlgoLabLevel02VehicleObject>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );

        for (int i = 0; i < vehiculos.Length; i++)
        {
            AlgoLabLevel02VehicleObject vehiculo = vehiculos[i];

            if (vehiculo == null)
            {
                continue;
            }

            vehiculo.OrdenarMoverADestino(puntoDestino);
        }
    }

    private void BuscarPanelesAutomaticamente()
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];

            if (canvas == null)
            {
                continue;
            }

            if (soloCanvasWorldSpace && canvas.renderMode != RenderMode.WorldSpace)
            {
                continue;
            }

            RectTransform rect = canvas.GetComponent<RectTransform>();

            if (rect == null)
            {
                continue;
            }

            if (!panelRoots.Contains(rect))
            {
                panelRoots.Add(rect);
            }
        }
    }

    private void CrearMarcadorSiHaceFalta()
    {
        if (!mostrarMarcadorDestino)
        {
            return;
        }

        marcadorDestino = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        marcadorDestino.name = "MarcadorDestinoVehiculos";
        marcadorDestino.transform.SetParent(transform, false);
        marcadorDestino.transform.localScale = Vector3.one * escalaMarcador;

        Collider col = marcadorDestino.GetComponent<Collider>();

        if (col != null)
        {
            Destroy(col);
        }

        Renderer renderer = marcadorDestino.GetComponent<Renderer>();

        if (renderer != null)
        {
            renderer.material.color = colorMarcador;
        }

        marcadorDestino.SetActive(false);
    }

    private void MostrarMarcador(Vector3 posicion)
    {
        if (!mostrarMarcadorDestino || marcadorDestino == null)
        {
            return;
        }

        marcadorDestino.transform.position = posicion + Vector3.up * 0.03f;
        marcadorDestino.SetActive(true);
    }

    private void OcultarMarcador()
    {
        if (marcadorDestino != null)
        {
            marcadorDestino.SetActive(false);
        }
    }
}
