using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class PointerSelector : MonoBehaviour
{
    public enum TipoEntrada
    {
        Controlador,
        Mano
    }

    [Header("Tipo de entrada")]
    public TipoEntrada tipoEntrada = TipoEntrada.Controlador;

    [Header("Controlador OVR")]
    public OVRInput.Controller controladorOVR = OVRInput.Controller.RTouch;

    [Header("Raycast")]
    public Transform rayOrigin;
    public float distanciaMaxima = 3f;
    public LayerMask capasSeleccionables;

    [Header("Botón controlador")]
    public OVRInput.Button botonSeleccion = OVRInput.Button.PrimaryIndexTrigger;

    [Header("Mano")]
    public OVRHand ovrHand;
    public OVRHand.HandFinger dedoPinza = OVRHand.HandFinger.Index;
    public float umbralPinza = 0.75f;

    [Header("Paneles UI detectables")]
    [Tooltip("Arrastra aquí los RectTransform de los paneles: PanelBackground, ExpandedCard, VistaRevision, etc.")]
    public RectTransform[] panelesUI;

    [Tooltip("Si está activo, cuando el rayo toca un panel no selecciona objetos 3D detrás.")]
    public bool bloquearObjetosDetrasDelPanel = true;

    [Header("Cursor dentro del panel")]
    [Tooltip("Objeto visual pequeño que se verá sobre el panel. Puede ser una Image UI o un Quad pequeño.")]
    public Transform cursorPanel;

    [Tooltip("Separación mínima para que el cursor no parpadee dentro del panel.")]
    public float offsetCursorPanel = 0.002f;

    public Vector3 escalaCursorNormal = new Vector3(0.025f, 0.025f, 0.025f);
    public bool ocultarCursorSiNoHayPanel = true;

    [Header("Visual de la línea")]
    public Color colorNormal = Color.white;
    public Color colorHover = Color.cyan;
    public Color colorSeleccion = Color.blue;
    public Color colorPanel = Color.green;
    public float grosorLinea = 0.008f;

    [Header("Orden visual sobre paneles")]
    [Tooltip("Separa el final de la linea de la superficie para evitar que quede oculto por el panel.")]
    [Min(0f)] public float retrocesoImpactoPanel = 0.006f;
    [Tooltip("Diferencia de orden usada para dibujar el rayo delante o detras segun la cara alcanzada.")]
    [Min(1)] public int margenOrdenRayoPanel = 2;
    public bool ajustarOrdenRayoSegunCaraPanel = true;

    private LineRenderer lineRenderer;
    private Material materialLineaRuntime;
    private int sortingLayerLineaOriginal;
    private int sortingOrderLineaOriginal;
    private AlgoLabObjetoEducativo objetoApuntado;
    private bool pinzaAnterior;

    private bool apuntandoPanel;
    private RectTransform panelApuntado;
    private Vector3 puntoPanelMundo;

    public void RegistrarPanelUI(RectTransform panel)
    {
        if (panel == null)
        {
            return;
        }

        List<RectTransform> paneles = new List<RectTransform>();
        if (panelesUI != null)
        {
            for (int i = 0; i < panelesUI.Length; i++)
            {
                RectTransform existente = panelesUI[i];
                if (existente != null && !paneles.Contains(existente))
                {
                    paneles.Add(existente);
                }
            }
        }

        if (!paneles.Contains(panel))
        {
            paneles.Add(panel);
        }

        panelesUI = paneles.ToArray();
    }

    public void DesregistrarPanelUI(RectTransform panel)
    {
        if (panelesUI == null)
        {
            return;
        }

        List<RectTransform> paneles = new List<RectTransform>();
        for (int i = 0; i < panelesUI.Length; i++)
        {
            RectTransform existente = panelesUI[i];
            if (existente != null && existente != panel && !paneles.Contains(existente))
            {
                paneles.Add(existente);
            }
        }

        panelesUI = paneles.ToArray();
    }

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        sortingLayerLineaOriginal = lineRenderer.sortingLayerID;
        sortingOrderLineaOriginal = lineRenderer.sortingOrder;

        lineRenderer.positionCount = 2;
        lineRenderer.startWidth = grosorLinea;
        lineRenderer.endWidth = grosorLinea;
        lineRenderer.useWorldSpace = true;

        if (lineRenderer.sharedMaterial == null)
        {
            Shader shader = Shader.Find("Sprites/Default");

            if (shader != null)
            {
                materialLineaRuntime = new Material(shader);
                lineRenderer.sharedMaterial = materialLineaRuntime;
            }
        }

        if (rayOrigin == null)
        {
            rayOrigin = transform;
        }

        if (cursorPanel != null)
        {
            cursorPanel.localScale = escalaCursorNormal;

            if (ocultarCursorSiNoHayPanel)
            {
                cursorPanel.gameObject.SetActive(false);
            }
        }
    }

    private void OnDestroy()
    {
        if (materialLineaRuntime != null)
        {
            Destroy(materialLineaRuntime);
            materialLineaRuntime = null;
        }
    }

    private void Update()
    {
        ActualizarRayo();

        if (PresionoSeleccion())
        {
            SeleccionarObjetoApuntado();
        }
    }

    private void ActualizarRayo()
    {
        Vector3 origen = rayOrigin.position;
        Vector3 direccion = rayOrigin.forward;
        Vector3 puntoFinal = origen + direccion * distanciaMaxima;

        objetoApuntado = null;
        apuntandoPanel = false;
        panelApuntado = null;

        bool golpeoPanel = BuscarPanelApuntado(
            origen,
            direccion,
            out RectTransform panelHit,
            out Vector3 puntoPanel,
            out float distanciaPanel
        );

        bool golpeoObjeto = Physics.Raycast(
            origen,
            direccion,
            out RaycastHit hit,
            distanciaMaxima,
            capasSeleccionables
        );

        float distanciaObjeto = golpeoObjeto ? hit.distance : Mathf.Infinity;
        bool colliderPerteneceAlPanel = golpeoPanel &&
                                        golpeoObjeto &&
                                        ColliderPerteneceAlPanel(hit.collider, panelHit);

        if (golpeoPanel && (colliderPerteneceAlPanel || distanciaPanel <= distanciaObjeto))
        {
            apuntandoPanel = true;
            panelApuntado = panelHit;
            puntoPanelMundo = colliderPerteneceAlPanel ? hit.point : puntoPanel;
            puntoFinal = Vector3.MoveTowards(
                puntoPanelMundo,
                origen,
                Mathf.Max(0f, retrocesoImpactoPanel)
            );
            ActualizarOrdenVisualRayo(panelHit, origen);

            if (bloquearObjetosDetrasDelPanel)
            {
                objetoApuntado = null;
            }
            else if (golpeoObjeto)
            {
                objetoApuntado = hit.collider.GetComponentInParent<AlgoLabObjetoEducativo>();
            }

            MostrarCursorPanel(panelHit, puntoPanelMundo, origen);
        }
        else
        {
            if (golpeoObjeto)
            {
                puntoFinal = hit.point;
                objetoApuntado = hit.collider.GetComponentInParent<AlgoLabObjetoEducativo>();
            }

            OcultarCursorPanel();
            RestaurarOrdenVisualRayo();
        }

        lineRenderer.SetPosition(0, origen);
        lineRenderer.SetPosition(1, puntoFinal);

        Color colorActual = colorNormal;

        if (apuntandoPanel)
        {
            colorActual = colorPanel;
        }
        else if (objetoApuntado != null)
        {
            colorActual = colorHover;
        }

        if (AlgoLabSelectionManager.Instance != null &&
            AlgoLabSelectionManager.Instance.HayObjetoSeleccionado() &&
            objetoApuntado == AlgoLabSelectionManager.Instance.objetoSeleccionado)
        {
            colorActual = colorSeleccion;
        }

        lineRenderer.startColor = colorActual;
        lineRenderer.endColor = colorActual;
    }

    private void ActualizarOrdenVisualRayo(RectTransform panel, Vector3 origenRayo)
    {
        if (!ajustarOrdenRayoSegunCaraPanel || panel == null)
        {
            RestaurarOrdenVisualRayo();
            return;
        }

        Canvas canvasPanel = panel.GetComponentInParent<Canvas>();
        if (canvasPanel == null)
        {
            RestaurarOrdenVisualRayo();
            return;
        }

        int margen = Mathf.Max(1, margenOrdenRayoPanel);
        bool rayoLlegaPorDetras = Vector3.Dot(
            panel.forward,
            origenRayo - panel.position
        ) < 0f;

        lineRenderer.sortingLayerID = canvasPanel.sortingLayerID;
        lineRenderer.sortingOrder = canvasPanel.sortingOrder +
                                    (rayoLlegaPorDetras ? margen : -margen);
    }

    private void RestaurarOrdenVisualRayo()
    {
        lineRenderer.sortingLayerID = sortingLayerLineaOriginal;
        lineRenderer.sortingOrder = sortingOrderLineaOriginal;
    }

    private bool ColliderPerteneceAlPanel(Collider collider, RectTransform panel)
    {
        if (collider == null || panel == null)
        {
            return false;
        }

        Transform colliderTransform = collider.transform;
        return colliderTransform == panel ||
               colliderTransform.IsChildOf(panel) ||
               panel.IsChildOf(colliderTransform);
    }

    private bool BuscarPanelApuntado(
        Vector3 origen,
        Vector3 direccion,
        out RectTransform panelEncontrado,
        out Vector3 puntoMundo,
        out float distancia)
    {
        panelEncontrado = null;
        puntoMundo = Vector3.zero;
        distancia = Mathf.Infinity;

        if (panelesUI == null || panelesUI.Length == 0)
        {
            return false;
        }

        Ray ray = new Ray(origen, direccion);

        for (int i = 0; i < panelesUI.Length; i++)
        {
            RectTransform panel = panelesUI[i];

            if (panel == null || !panel.gameObject.activeInHierarchy)
            {
                continue;
            }

            Plane planoPanel = new Plane(panel.forward, panel.position);

            if (!planoPanel.Raycast(ray, out float distanciaTemp))
            {
                continue;
            }

            if (distanciaTemp < 0f || distanciaTemp > distanciaMaxima)
            {
                continue;
            }

            Vector3 puntoTemp = ray.GetPoint(distanciaTemp);

            if (!PuntoEstaDentroDelRect(panel, puntoTemp))
            {
                continue;
            }

            if (distanciaTemp < distancia)
            {
                distancia = distanciaTemp;
                puntoMundo = puntoTemp;
                panelEncontrado = panel;
            }
        }

        return panelEncontrado != null;
    }

    private bool PuntoEstaDentroDelRect(RectTransform rectTransform, Vector3 puntoMundo)
    {
        Vector3 puntoLocal = rectTransform.InverseTransformPoint(puntoMundo);
        Vector2 puntoLocal2D = new Vector2(puntoLocal.x, puntoLocal.y);

        return rectTransform.rect.Contains(puntoLocal2D);
    }

    private void MostrarCursorPanel(
        RectTransform panel,
        Vector3 puntoMundo,
        Vector3 origenRayo)
    {
        if (cursorPanel == null || panel == null)
        {
            return;
        }

        if (!cursorPanel.gameObject.activeSelf)
        {
            cursorPanel.gameObject.SetActive(true);
        }

        Vector3 normalVisible = panel.forward;
        if (Vector3.Dot(normalVisible, origenRayo - puntoMundo) < 0f)
        {
            normalVisible = -normalVisible;
        }

        cursorPanel.position = puntoMundo + normalVisible * offsetCursorPanel;
        cursorPanel.rotation = panel.rotation;
        cursorPanel.localScale = escalaCursorNormal;
    }

    private void OcultarCursorPanel()
    {
        if (cursorPanel == null || !ocultarCursorSiNoHayPanel)
        {
            return;
        }

        if (cursorPanel.gameObject.activeSelf)
        {
            cursorPanel.gameObject.SetActive(false);
        }
    }

    private bool PresionoSeleccion()
    {
        if (tipoEntrada == TipoEntrada.Controlador)
        {
            return OVRInput.GetDown(botonSeleccion, controladorOVR);
        }

        if (tipoEntrada == TipoEntrada.Mano && ovrHand != null)
        {
            float fuerzaPinza = ovrHand.GetFingerPinchStrength(dedoPinza);
            bool haciendoPinza = fuerzaPinza >= umbralPinza;

            bool inicioPinza = haciendoPinza && !pinzaAnterior;
            pinzaAnterior = haciendoPinza;

            return inicioPinza;
        }

        return false;
    }

    private void SeleccionarObjetoApuntado()
    {
        if (apuntandoPanel)
        {
            Debug.Log("Apuntando a panel: " + panelApuntado.name);
            return;
        }

        if (objetoApuntado == null)
        {
            Debug.Log("No hay objeto educativo apuntado desde: " + gameObject.name);
            return;
        }

        if (AlgoLabSelectionManager.Instance == null)
        {
            Debug.LogError("Falta AlgoLabSelectionManager en la escena.");
            return;
        }

        Debug.Log("Seleccionando con " + controladorOVR + ": " + objetoApuntado.nombreObjeto);

        AlgoLabSelectionManager.Instance.ToggleSeleccion(objetoApuntado);
    }
}
