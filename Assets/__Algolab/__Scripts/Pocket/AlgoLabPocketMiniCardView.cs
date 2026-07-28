using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class AlgoLabPocketMiniCardView : MonoBehaviour
{
    [Header("Referencias")]
    public Image icono;
    public TMP_Text textoNombre;
    public Image fondo;
    public Button boton;
    public BoxCollider boxCollider;

    [Header("Respetar visual del prefab")]
    public bool respetarVisualDelPrefab = true;

    [Header("Comportamiento")]
    public bool ocultarTextoSiHayIcono = false;
    public bool ocultarIconoSiNoHaySprite = true;
    public bool usarNombreCortoComoTexto = true;
    public bool ajustarColliderAutomaticamente = true;
    public float profundidadCollider = 12f;

    [Header("Visual opcional si NO respetas el prefab")]
    public bool ocultarFondo = false;
    public Color colorNormal = new Color(1f, 1f, 1f, 0.85f);
    public Color colorSeleccionado = new Color(0.04f, 0.82f, 0.48f, 1f);
    public Color colorHover = new Color(0.20f, 1f, 0.64f, 1f);
    public Color colorTextoNormal = new Color(0.15f, 0.15f, 0.15f, 1f);

    [Header("Tarjeta de Configuracion")]
    public Color colorConfiguracion = new Color(0.02f, 0.52f, 0.29f, 1f);
    public Color colorConfiguracionSeleccionada = new Color(0.08f, 0.95f, 0.50f, 1f);
    public Color colorTextoConfiguracion = Color.white;

    private AlgoLabPocketPanelItem panel;
    private AlgoLabPanelPocketManager manager;
    private CanvasGroup canvasGroup;
    private RectTransform rect;
    private bool hoverActual;
    private Color colorFondoPrefab = Color.white;
    private bool colorFondoPrefabCapturado;
    private Color colorTextoPrefab = Color.white;
    private FontStyles estiloTextoPrefab = FontStyles.Normal;
    private bool visualTextoPrefabCapturado;

    public AlgoLabPocketPanelItem Panel => panel;

    public RectTransform Rect
    {
        get
        {
            if (rect == null)
            {
                rect = GetComponent<RectTransform>();
            }

            return rect;
        }
    }

    public float Alpha
    {
        get
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            return canvasGroup != null ? canvasGroup.alpha : 1f;
        }
    }

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        AutoBuscarReferencias();
        CapturarColorFondoPrefab();
        CapturarVisualTextoPrefab();

        if (boton != null)
        {
            boton.onClick.RemoveListener(OnClickMiniCard);
            boton.onClick.AddListener(OnClickMiniCard);
        }

        AplicarFondo();
        AjustarColliderSiCorresponde();
    }

    private void OnDestroy()
    {
        if (boton != null)
            boton.onClick.RemoveListener(OnClickMiniCard);
    }

    private void Reset()
    {
        AutoBuscarReferencias();
        AjustarColliderSiCorresponde();
    }

    private void OnValidate()
    {
        AutoBuscarReferencias();
        AplicarFondo();
        AjustarColliderSiCorresponde();
    }

    [ContextMenu("Auto buscar referencias")]
    public void AutoBuscarReferencias()
    {
        if (boton == null)
        {
            boton = GetComponent<Button>();

            if (boton == null)
            {
                boton = GetComponentInChildren<Button>(true);
            }
        }

        if (fondo == null)
        {
            if (boton != null)
            {
                fondo = boton.GetComponent<Image>();
            }

            if (fondo == null)
            {
                fondo = GetComponent<Image>();
            }
        }

        if (textoNombre == null)
        {
            textoNombre = GetComponentInChildren<TMP_Text>(true);
        }

        if (!EsReferenciaIconoDedicada(icono))
        {
            Image[] imagenes = GetComponentsInChildren<Image>(true);
            icono = null;

            for (int i = 0; i < imagenes.Length; i++)
            {
                Image img = imagenes[i];

                if (img == null)
                {
                    continue;
                }

                if (fondo != null && img == fondo)
                {
                    continue;
                }

                if (img.gameObject == gameObject)
                {
                    continue;
                }

                icono = img;
                break;
            }

            if (icono == null && Application.isPlaying)
            {
                icono = CrearIconoDedicadoRuntime();
            }
        }

        if (boxCollider == null)
        {
            boxCollider = GetComponent<BoxCollider>();

            if (boxCollider == null)
            {
                boxCollider = gameObject.AddComponent<BoxCollider>();
            }

            boxCollider.isTrigger = true;
        }

        CapturarColorFondoPrefab();
        CapturarVisualTextoPrefab();
    }

    private bool EsReferenciaIconoDedicada(Image referencia)
    {
        return referencia != null &&
               referencia != fondo &&
               referencia.gameObject != gameObject;
    }

    private Image CrearIconoDedicadoRuntime()
    {
        Transform parent = boton != null ? boton.transform : transform;
        GameObject root = new GameObject(
            "IconoMiniCard",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );

        RectTransform iconRect = root.GetComponent<RectTransform>();
        iconRect.SetParent(parent, false);
        iconRect.anchorMin = new Vector2(0.18f, 0.18f);
        iconRect.anchorMax = new Vector2(0.82f, 0.82f);
        iconRect.offsetMin = Vector2.zero;
        iconRect.offsetMax = Vector2.zero;
        iconRect.SetAsLastSibling();

        Image imagen = root.GetComponent<Image>();
        imagen.preserveAspect = true;
        imagen.raycastTarget = false;
        return imagen;
    }

    private void CapturarColorFondoPrefab()
    {
        if (!colorFondoPrefabCapturado && fondo != null)
        {
            colorFondoPrefab = fondo.color;
            colorFondoPrefabCapturado = true;
        }
    }

    private void CapturarVisualTextoPrefab()
    {
        if (!visualTextoPrefabCapturado && textoNombre != null)
        {
            colorTextoPrefab = textoNombre.color;
            estiloTextoPrefab = textoNombre.fontStyle;
            visualTextoPrefabCapturado = true;
        }
    }

    public void AjustarColliderSiCorresponde()
    {
        if (!ajustarColliderAutomaticamente)
        {
            return;
        }

        if (boxCollider == null)
        {
            boxCollider = GetComponent<BoxCollider>();
        }

        if (boxCollider == null || Rect == null)
        {
            return;
        }

        Vector2 size = Rect.rect.size;

        if (size.x <= 1f || size.y <= 1f)
        {
            size = Rect.sizeDelta;
        }

        if (size.x <= 1f) size.x = 42f;
        if (size.y <= 1f) size.y = 30f;

        boxCollider.center = Vector3.zero;
        boxCollider.size = new Vector3(size.x, size.y, profundidadCollider);
        boxCollider.isTrigger = true;
    }

    public void ConfigurarManager(AlgoLabPanelPocketManager nuevoManager)
    {
        manager = nuevoManager;
    }

    public void Configurar(AlgoLabPocketPanelItem nuevoPanel, bool seleccionado)
    {
        AutoBuscarReferencias();

        panel = nuevoPanel;

        string nombre = panel != null ? panel.nombreCorto : "";
        Sprite sprite = panel != null ? panel.iconoMini : null;
        bool tieneIcono = sprite != null;

        if (icono != null)
        {
            icono.sprite = sprite;
            icono.color = Color.white;
            icono.preserveAspect = true;
            icono.raycastTarget = false;

            if (ocultarIconoSiNoHaySprite)
            {
                if (icono.gameObject == gameObject)
                {
                    icono.enabled = tieneIcono;
                }
                else
                {
                    icono.gameObject.SetActive(tieneIcono);
                }
            }
            else
            {
                icono.enabled = tieneIcono;
            }
        }

        if (textoNombre != null)
        {
            if (usarNombreCortoComoTexto)
            {
                textoNombre.text = nombre;
            }

            bool esTarjetaConfiguracion = panel != null && panel.esAccionConfiguracion;
            textoNombre.gameObject.SetActive(
                !esTarjetaConfiguracion && (!ocultarTextoSiHayIcono || !tieneIcono)
            );
        }

        SetSeleccionado(seleccionado);
        SetHover(false);
        AjustarColliderSiCorresponde();
    }

    public void SetSeleccionado(bool seleccionado)
    {
        AplicarFondo();
    }

    public void SetHover(bool hover)
    {
        hoverActual = hover;
        AplicarFondo();
    }

    private void AplicarFondo()
    {
        if (fondo != null && panel != null && panel.esAccionConfiguracion)
        {
            fondo.color = hoverActual
                ? colorConfiguracionSeleccionada
                : colorFondoPrefabCapturado ? colorFondoPrefab : colorNormal;
            fondo.raycastTarget = true;
        }
        else if (fondo != null && respetarVisualDelPrefab)
        {
            if (hoverActual)
            {
                fondo.color = colorHover;
            }
            else if (colorFondoPrefabCapturado)
            {
                fondo.color = colorFondoPrefab;
            }
        }
        else if (fondo != null && ocultarFondo)
        {
            Color transparente = Color.white;
            transparente.a = 0f;
            fondo.color = transparente;
            fondo.raycastTarget = false;
        }
        else if (fondo != null && hoverActual)
        {
            fondo.color = colorHover;
        }
        else if (fondo != null)
        {
            fondo.color = colorNormal;
        }

        AplicarTexto();
    }

    private void AplicarTexto()
    {
        if (textoNombre == null)
        {
            return;
        }

        if (hoverActual)
        {
            textoNombre.color = Color.white;
            textoNombre.fontStyle = FontStyles.Bold;
            return;
        }

        if (respetarVisualDelPrefab && visualTextoPrefabCapturado)
        {
            textoNombre.color = colorTextoPrefab;
            textoNombre.fontStyle = estiloTextoPrefab;
            return;
        }

        textoNombre.color = colorTextoNormal;
        textoNombre.fontStyle = FontStyles.Normal;
    }

    public void SetAlpha(float alpha)
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = alpha;
            canvasGroup.blocksRaycasts = alpha > 0.2f;
            canvasGroup.interactable = alpha > 0.2f;
        }

        if (boxCollider != null)
        {
            boxCollider.enabled = alpha > 0.2f;
        }
    }

    private void OnClickMiniCard()
    {
        if (panel != null && panel.esAccionConfiguracion && manager != null)
        {
            manager.IntentarActivarAccionConfiguracionDesdeCard(this);
        }
    }
}
