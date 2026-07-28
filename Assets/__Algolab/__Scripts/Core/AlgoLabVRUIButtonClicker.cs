using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class AlgoLabVRUIButtonClicker : MonoBehaviour
{
    public enum ModoDropdownVR
    {
        AbrirListaYSeleccionar,
        CiclarOpcionesConClick
    }

    [Header("Área principal opcional")]
    public RectTransform panelArea;

    [Header("Raíces UI adicionales opcionales")]
    public List<RectTransform> uiRoots = new List<RectTransform>();

    [Header("Rayos de los controles")]
    public Transform leftRayOrigin;
    public Transform rightRayOrigin;

    [Header("Interactuables UI")]
    public List<Button> botones = new List<Button>();
    public List<TMP_Dropdown> dropdowns = new List<TMP_Dropdown>();
    public List<Slider> sliders = new List<Slider>();

    [Header("Tutorial")]
    [Tooltip("Controlador del tutorial. Si está vacío, se busca automáticamente en la escena.")]
    public AlgoLabTutorialPanelController tutorialController;

    [Tooltip("Si está activo, cada click VR sobre un botón se notifica al tutorial.")]
    public bool notificarClicksAlTutorial = true;

    [Tooltip("Busca automáticamente el TutorialPanelController aunque esté inactivo.")]
    public bool buscarTutorialAutomaticamente = true;

    [Tooltip("Si está activo, se avisa al tutorial antes de ejecutar el OnClick del botón. Recomendado para que el tutorial se oculte antes de iniciar un nivel.")]
    public bool notificarTutorialAntesDelOnClick = true;

    [Header("Búsqueda automática")]
    public bool buscarAutomaticamente = true;
    public bool actualizarCadaFrame = true;
    public bool buscarEnTodaLaEscena = true;

    [Tooltip("Intervalo minimo entre busquedas globales. Evita recorrer toda la escena en cada frame.")]
    [Min(0.1f)]
    public float intervaloActualizacionAutomatica = 0.5f;

    [Header("Dropdowns VR")]
    public ModoDropdownVR modoDropdownVR = ModoDropdownVR.AbrirListaYSeleccionar;
    public bool cerrarDropdownAlClickFuera = true;

    [Header("Menú dropdown personalizado")]
    public bool usarMenuDropdownPersonalizado = true;
    public float anchoMenuDropdown = 180f;
    public float altoItemDropdown = 36f;
    public float espacioEntreDropdownYMenu = 8f;
    public float escalaMenuDropdown = 1f;

    public Color colorFondoMenuDropdown = Color.white;
    public Color colorTextoMenuDropdown = Color.black;
    public Color colorItemMenuDropdown = Color.white;
    public Color colorItemSeleccionadoMenuDropdown = new Color(0.85f, 0.85f, 0.85f, 1f);

    [Header("Configuración")]
    public float distanciaMaxima = 6f;
    public float umbralGatillo = 0.55f;

    [Header("Colores botones normales")]
    public bool cambiarColoresBotones = true;
    public Color colorHover = new Color(0.1f, 0.8f, 1f, 1f);
    public Color colorNormal = new Color(0.25f, 0.28f, 0.35f, 1f);
    public Color colorNormalEnviar = new Color(0f, 0.65f, 0.45f, 1f);
    public Color colorNormalRegrabar = new Color(0.25f, 0.28f, 0.35f, 1f);

    [Header("Colores botones de métodos")]
    public bool usarColoresEspecialesMetodos = true;

    public Color colorMetodoNormal = new Color(0.10f, 0.14f, 0.22f, 1f);
    public Color colorMetodoHover = new Color(0.05f, 0.75f, 1f, 1f);
    public Color colorMetodoSeleccionado = new Color(0f, 0.85f, 0.55f, 1f);

    public Color colorTextoMetodoNormal = Color.white;
    public Color colorTextoMetodoSeleccionado = Color.black;

    [Header("Botones especiales del aviso invitado")]
    [Tooltip("Si está activo, no modifica el fondo/color de botones que tengan AlgoLabBracketHoverButton.")]
    public bool ignorarBotonesBracketHover = true;

    [Header("Debug")]
    public bool mostrarDebug = false;

    private bool gatilloIzquierdoAnterior;
    private bool gatilloDerechoAnterior;

    private Button botonHoverActual;
    private Button botonHoverWarningAnterior;
    private Button botonHoverConfiguracionAnterior;

    private TMP_Dropdown dropdownHoverActual;
    private TMP_Dropdown dropdownAbierto;
    private Slider sliderArrastradoIzquierdo;
    private Slider sliderArrastradoDerecho;

    private GameObject menuDropdownRuntime;
    private RectTransform menuDropdownRuntimeRect;
    private TMP_Dropdown dropdownRuntimeActual;

    private string metodoSeleccionadoVisual = "";

    private PointerEventData pointerEventDataRuntime;

    private readonly HashSet<GameObject> interactuablesActivadosEsteFrame = new HashSet<GameObject>();
    private float proximaActualizacionAutomatica;
    private int ultimoConteoBotones = -1;
    private int ultimoConteoDropdowns = -1;
    private int ultimoConteoSliders = -1;
    private bool coloresSucios = true;
    private Button botonHoverDebugAnterior;
    private TMP_Dropdown dropdownHoverDebugAnterior;

    private void Awake()
    {
        PrepararTutorialController();
        ActualizarListaInteractuables();
    }

    private void PrepararTutorialController()
    {
        if (!notificarClicksAlTutorial)
        {
            return;
        }

        if (tutorialController != null)
        {
            return;
        }

        if (!buscarTutorialAutomaticamente)
        {
            return;
        }

        tutorialController = FindFirstObjectByType<AlgoLabTutorialPanelController>(
            FindObjectsInactive.Include
        );
    }

    private bool NotificarClickAlTutorial(Button boton)
    {
        if (!notificarClicksAlTutorial || boton == null)
        {
            return false;
        }

        if (tutorialController == null)
        {
            PrepararTutorialController();
        }

        if (tutorialController != null)
        {
            return tutorialController.NotificarBotonUIClicado(boton);
        }

        return false;
    }

    private void Update()
    {
        if (buscarAutomaticamente && actualizarCadaFrame &&
            Time.unscaledTime >= proximaActualizacionAutomatica)
        {
            ActualizarListaInteractuables();
            proximaActualizacionAutomatica = Time.unscaledTime +
                Mathf.Max(0.1f, intervaloActualizacionAutomatica);
        }

        Button botonHoverAnterior = botonHoverActual;
        TMP_Dropdown dropdownHoverAnterior = dropdownHoverActual;

        botonHoverActual = null;
        dropdownHoverActual = null;
        interactuablesActivadosEsteFrame.Clear();

        bool clickIzquierdo = EsGatilloIzquierdoPresionado();
        bool clickDerecho = EsGatilloDerechoPresionado();
        bool gatilloIzquierdoSostenido = gatilloIzquierdoAnterior;
        bool gatilloDerechoSostenido = gatilloDerechoAnterior;

        RevisarRayo(
            leftRayOrigin,
            clickIzquierdo,
            gatilloIzquierdoSostenido,
            ref sliderArrastradoIzquierdo,
            "IZQUIERDO"
        );
        RevisarRayo(
            rightRayOrigin,
            clickDerecho,
            gatilloDerechoSostenido,
            ref sliderArrastradoDerecho,
            "DERECHO"
        );

        ActualizarHoverBotonesWarningInvitado();
        ActualizarHoverBotonesConfiguracion();

        bool cambioHover = botonHoverAnterior != botonHoverActual ||
                           dropdownHoverAnterior != dropdownHoverActual;

        if (mostrarDebug)
        {
            ActualizarDebugHover();
        }

        if (cambiarColoresBotones && (coloresSucios || cambioHover))
        {
            ActualizarColoresBotones();
            coloresSucios = false;
        }
    }

    [ContextMenu("Actualizar lista interactuables")]
    public void ActualizarListaInteractuables()
    {
        if (!buscarAutomaticamente)
        {
            return;
        }

        botones.Clear();
        dropdowns.Clear();
        sliders.Clear();

        if (panelArea != null)
        {
            AgregarInteractuablesDesdeRaiz(panelArea);
        }

        for (int i = 0; i < uiRoots.Count; i++)
        {
            if (uiRoots[i] != null)
            {
                AgregarInteractuablesDesdeRaiz(uiRoots[i]);
            }
        }

        if (buscarEnTodaLaEscena)
        {
            Button[] botonesEncontrados = FindObjectsByType<Button>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

            TMP_Dropdown[] dropdownsEncontrados = FindObjectsByType<TMP_Dropdown>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

            Slider[] slidersEncontrados = FindObjectsByType<Slider>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

            for (int i = 0; i < botonesEncontrados.Length; i++)
            {
                AgregarBoton(botonesEncontrados[i]);
            }

            for (int i = 0; i < dropdownsEncontrados.Length; i++)
            {
                AgregarDropdown(dropdownsEncontrados[i]);
            }

            for (int i = 0; i < slidersEncontrados.Length; i++)
            {
                AgregarSlider(slidersEncontrados[i]);
            }
        }

        if (menuDropdownRuntime != null)
        {
            AgregarBotonesMenuRuntimeALista();
        }

        coloresSucios = true;

        if (mostrarDebug &&
            (ultimoConteoBotones != botones.Count ||
             ultimoConteoDropdowns != dropdowns.Count ||
             ultimoConteoSliders != sliders.Count))
        {
            Debug.Log(
                "UI encontrados | Botones: " + botones.Count +
                " | Dropdowns: " + dropdowns.Count +
                " | Sliders: " + sliders.Count
            );
        }

        ultimoConteoBotones = botones.Count;
        ultimoConteoDropdowns = dropdowns.Count;
        ultimoConteoSliders = sliders.Count;
    }

    private void AgregarInteractuablesDesdeRaiz(RectTransform raiz)
    {
        if (raiz == null)
        {
            return;
        }

        Button[] botonesEncontrados = raiz.GetComponentsInChildren<Button>(true);
        TMP_Dropdown[] dropdownsEncontrados = raiz.GetComponentsInChildren<TMP_Dropdown>(true);
        Slider[] slidersEncontrados = raiz.GetComponentsInChildren<Slider>(true);

        for (int i = 0; i < botonesEncontrados.Length; i++)
        {
            AgregarBoton(botonesEncontrados[i]);
        }

        for (int i = 0; i < dropdownsEncontrados.Length; i++)
        {
            AgregarDropdown(dropdownsEncontrados[i]);
        }

        for (int i = 0; i < slidersEncontrados.Length; i++)
        {
            AgregarSlider(slidersEncontrados[i]);
        }
    }

    private void AgregarBoton(Button boton)
    {
        if (boton == null)
        {
            return;
        }

        if (!botones.Contains(boton))
        {
            botones.Add(boton);
        }
    }

    private void AgregarDropdown(TMP_Dropdown dropdown)
    {
        if (dropdown == null)
        {
            return;
        }

        if (!dropdowns.Contains(dropdown))
        {
            dropdowns.Add(dropdown);
        }

        PrepararDropdownVisual(dropdown);
    }

    private void AgregarSlider(Slider slider)
    {
        if (slider != null && !sliders.Contains(slider))
        {
            sliders.Add(slider);
        }
    }

    private void PrepararDropdownVisual(TMP_Dropdown dropdown)
    {
        if (dropdown == null)
        {
            return;
        }

        if (dropdown.captionText != null)
        {
            dropdown.captionText.color = Color.black;
            dropdown.captionText.textWrappingMode = TextWrappingModes.NoWrap;
        }

        if (dropdown.itemText != null)
        {
            dropdown.itemText.color = Color.black;
            dropdown.itemText.textWrappingMode = TextWrappingModes.NoWrap;
        }

        Image image = dropdown.GetComponent<Image>();

        if (image != null)
        {
            image.color = Color.white;
            image.raycastTarget = true;
        }
    }

    private void RevisarRayo(
        Transform rayOrigin,
        bool presionoGatillo,
        bool gatilloSostenido,
        ref Slider sliderArrastrado,
        string nombreControl)
    {
        if (!gatilloSostenido)
        {
            sliderArrastrado = null;
        }

        if (rayOrigin == null)
        {
            return;
        }

        if (sliderArrastrado != null)
        {
            if (!sliderArrastrado.gameObject.activeInHierarchy ||
                !sliderArrastrado.enabled ||
                !sliderArrastrado.interactable)
            {
                sliderArrastrado = null;
            }
            else
            {
                ActualizarSliderDesdeRayo(sliderArrastrado, rayOrigin);
                return;
            }
        }

        Slider sliderDetectado = ObtenerSliderBajoRayo(rayOrigin);

        if (sliderDetectado != null)
        {
            if (presionoGatillo && sliderDetectado.interactable)
            {
                if (!interactuablesActivadosEsteFrame.Add(sliderDetectado.gameObject))
                {
                    return;
                }

                sliderArrastrado = sliderDetectado;
                ActualizarSliderDesdeRayo(sliderArrastrado, rayOrigin);

                if (mostrarDebug)
                {
                    Debug.Log("ARRASTRE VR " + nombreControl + " sobre slider: " + sliderDetectado.name);
                }
            }

            return;
        }

        Button botonDetectado = ObtenerBotonBajoRayo(rayOrigin);

        if (botonDetectado != null)
        {
            botonHoverActual = botonDetectado;

            if (presionoGatillo && botonDetectado.interactable)
            {
                if (!interactuablesActivadosEsteFrame.Add(botonDetectado.gameObject))
                {
                    return;
                }

                if (EsBotonMetodo(botonDetectado))
                {
                    metodoSeleccionadoVisual = ObtenerTextoBoton(botonDetectado);
                    coloresSucios = true;
                }

                if (EsBotonWarningInvitado(botonDetectado))
                {
                    EjecutarPointerDownUp(botonDetectado);
                }

                if (mostrarDebug)
                {
                    Debug.Log("CLICK VR " + nombreControl + " sobre botón: " + botonDetectado.name);
                }

                bool clickConsumidoPorTutorial = false;

                if (notificarTutorialAntesDelOnClick)
                {
                    clickConsumidoPorTutorial = NotificarClickAlTutorial(botonDetectado);
                }

                if (clickConsumidoPorTutorial)
                {
                    return;
                }

                botonDetectado.onClick.Invoke();

                if (!notificarTutorialAntesDelOnClick)
                {
                    NotificarClickAlTutorial(botonDetectado);
                }
            }

            return;
        }

        TMP_Dropdown dropdownDetectado = ObtenerDropdownBajoRayo(rayOrigin);

        if (dropdownDetectado != null)
        {
            dropdownHoverActual = dropdownDetectado;

            if (presionoGatillo && dropdownDetectado.interactable)
            {
                if (!interactuablesActivadosEsteFrame.Add(dropdownDetectado.gameObject))
                {
                    return;
                }

                InteractuarDropdown(dropdownDetectado);
            }

            return;
        }

        if (presionoGatillo && cerrarDropdownAlClickFuera)
        {
            CerrarMenuDropdownPersonalizado();

            if (dropdownAbierto != null)
            {
                dropdownAbierto.Hide();
                dropdownAbierto = null;
            }
        }
    }

    private Slider ObtenerSliderBajoRayo(Transform rayOrigin)
    {
        Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
        Slider mejorSlider = null;
        float mejorDistancia = float.MaxValue;

        for (int i = sliders.Count - 1; i >= 0; i--)
        {
            Slider slider = sliders[i];

            if (slider == null ||
                !slider.gameObject.activeInHierarchy ||
                !slider.enabled ||
                !slider.interactable)
            {
                continue;
            }

            RectTransform rectSlider = slider.GetComponent<RectTransform>();

            if (rectSlider == null ||
                !RayoTocaRect(ray, rectSlider, out float distancia, out _) ||
                distancia < 0f ||
                distancia > distanciaMaxima)
            {
                continue;
            }

            if (distancia < mejorDistancia)
            {
                mejorDistancia = distancia;
                mejorSlider = slider;
            }
        }

        return mejorSlider;
    }

    private bool ActualizarSliderDesdeRayo(Slider slider, Transform rayOrigin)
    {
        if (slider == null || rayOrigin == null)
        {
            return false;
        }

        RectTransform areaDeslizamiento = null;

        if (slider.handleRect != null)
        {
            areaDeslizamiento = slider.handleRect.parent as RectTransform;
        }

        if (areaDeslizamiento == null && slider.fillRect != null)
        {
            areaDeslizamiento = slider.fillRect.parent as RectTransform;
        }

        if (areaDeslizamiento == null)
        {
            areaDeslizamiento = slider.GetComponent<RectTransform>();
        }

        if (areaDeslizamiento == null)
        {
            return false;
        }

        Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
        Plane plano = new Plane(areaDeslizamiento.forward, areaDeslizamiento.position);

        if (!plano.Raycast(ray, out float distancia) ||
            distancia < 0f ||
            distancia > distanciaMaxima)
        {
            return false;
        }

        Vector3 puntoLocal = areaDeslizamiento.InverseTransformPoint(ray.GetPoint(distancia));
        Rect rect = areaDeslizamiento.rect;
        float valorNormalizado;

        switch (slider.direction)
        {
            case Slider.Direction.RightToLeft:
                valorNormalizado = 1f - Mathf.InverseLerp(rect.xMin, rect.xMax, puntoLocal.x);
                break;

            case Slider.Direction.BottomToTop:
                valorNormalizado = Mathf.InverseLerp(rect.yMin, rect.yMax, puntoLocal.y);
                break;

            case Slider.Direction.TopToBottom:
                valorNormalizado = 1f - Mathf.InverseLerp(rect.yMin, rect.yMax, puntoLocal.y);
                break;

            default:
                valorNormalizado = Mathf.InverseLerp(rect.xMin, rect.xMax, puntoLocal.x);
                break;
        }

        slider.normalizedValue = Mathf.Clamp01(valorNormalizado);
        return true;
    }

    private Button ObtenerBotonBajoRayo(Transform rayOrigin)
    {
        Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);

        Button mejorBoton = null;
        float mejorDistancia = float.MaxValue;

        for (int i = botones.Count - 1; i >= 0; i--)
        {
            Button boton = botones[i];

            if (boton == null)
            {
                continue;
            }

            if (!boton.gameObject.activeInHierarchy)
            {
                continue;
            }

            RectTransform rectBoton = boton.GetComponent<RectTransform>();

            if (rectBoton == null)
            {
                continue;
            }

            if (!RayoTocaRect(ray, rectBoton, out float distancia, out _))
            {
                continue;
            }

            if (distancia < 0f || distancia > distanciaMaxima)
            {
                continue;
            }

            if (distancia < mejorDistancia)
            {
                mejorDistancia = distancia;
                mejorBoton = boton;
            }
        }

        return mejorBoton;
    }

    private TMP_Dropdown ObtenerDropdownBajoRayo(Transform rayOrigin)
    {
        Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);

        TMP_Dropdown mejorDropdown = null;
        float mejorDistancia = float.MaxValue;

        for (int i = dropdowns.Count - 1; i >= 0; i--)
        {
            TMP_Dropdown dropdown = dropdowns[i];

            if (dropdown == null)
            {
                continue;
            }

            if (!dropdown.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (!dropdown.interactable)
            {
                continue;
            }

            RectTransform rectDropdown = dropdown.GetComponent<RectTransform>();

            if (rectDropdown == null)
            {
                continue;
            }

            if (!RayoTocaRect(ray, rectDropdown, out float distancia, out _))
            {
                continue;
            }

            if (distancia < 0f || distancia > distanciaMaxima)
            {
                continue;
            }

            if (distancia < mejorDistancia)
            {
                mejorDistancia = distancia;
                mejorDropdown = dropdown;
            }
        }

        return mejorDropdown;
    }

    private bool RayoTocaRect(
        Ray ray,
        RectTransform rect,
        out float distancia,
        out Vector3 puntoMundo)
    {
        distancia = 0f;
        puntoMundo = Vector3.zero;

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

        puntoMundo = ray.GetPoint(distancia);

        Vector3 puntoLocal3D = rect.InverseTransformPoint(puntoMundo);
        Vector2 puntoLocal = new Vector2(puntoLocal3D.x, puntoLocal3D.y);

        return rect.rect.Contains(puntoLocal);
    }

    private void InteractuarDropdown(TMP_Dropdown dropdown)
    {
        if (dropdown == null)
        {
            return;
        }

        if (dropdown.options == null || dropdown.options.Count == 0)
        {
            return;
        }

        PrepararDropdownVisual(dropdown);

        if (modoDropdownVR == ModoDropdownVR.CiclarOpcionesConClick)
        {
            int siguiente = dropdown.value + 1;

            if (siguiente >= dropdown.options.Count)
            {
                siguiente = 0;
            }

            dropdown.value = siguiente;
            dropdown.RefreshShownValue();

            if (mostrarDebug)
            {
                Debug.Log(
                    "Dropdown cambiado por ciclo: " +
                    dropdown.name + " = " + dropdown.options[dropdown.value].text
                );
            }

            return;
        }

        if (usarMenuDropdownPersonalizado)
        {
            if (dropdownRuntimeActual == dropdown && menuDropdownRuntime != null)
            {
                CerrarMenuDropdownPersonalizado();
                return;
            }

            AbrirMenuDropdownPersonalizado(dropdown);
            return;
        }

        if (dropdownAbierto != null && dropdownAbierto != dropdown)
        {
            dropdownAbierto.Hide();
        }

        dropdownAbierto = dropdown;
        dropdown.Show();

        if (mostrarDebug)
        {
            Debug.Log("Dropdown abierto con TMP nativo: " + dropdown.name);
        }
    }

    private void AbrirMenuDropdownPersonalizado(TMP_Dropdown dropdown)
    {
        if (dropdown == null)
        {
            return;
        }

        CerrarMenuDropdownPersonalizado();

        dropdownRuntimeActual = dropdown;

        Canvas canvas = dropdown.GetComponentInParent<Canvas>();

        if (canvas == null)
        {
            Debug.LogWarning("No se encontró Canvas para abrir menú dropdown personalizado.");
            return;
        }

        RectTransform dropdownRect = dropdown.GetComponent<RectTransform>();

        if (dropdownRect == null)
        {
            return;
        }

        float altoTotal = Mathf.Max(
            dropdown.options.Count * altoItemDropdown + 4f,
            altoItemDropdown + 4f
        );

        menuDropdownRuntime = new GameObject(
            "AlgoLabVRDropdownRuntimeList_" + dropdown.name,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );

        menuDropdownRuntimeRect = menuDropdownRuntime.GetComponent<RectTransform>();
        menuDropdownRuntimeRect.SetParent(canvas.transform, false);
        menuDropdownRuntimeRect.sizeDelta = new Vector2(anchoMenuDropdown, altoTotal);
        menuDropdownRuntimeRect.localScale = Vector3.one * escalaMenuDropdown;

        Vector3 mundoDebajoDropdown = dropdownRect.TransformPoint(
            new Vector3(
                0f,
                -dropdownRect.rect.height * 0.5f - espacioEntreDropdownYMenu - altoTotal * 0.5f,
                -0.01f
            )
        );

        menuDropdownRuntimeRect.position = mundoDebajoDropdown;
        menuDropdownRuntimeRect.rotation = dropdownRect.rotation;
        menuDropdownRuntimeRect.SetAsLastSibling();

        Image fondo = menuDropdownRuntime.GetComponent<Image>();
        fondo.color = colorFondoMenuDropdown;
        fondo.raycastTarget = true;

        LayoutElement layoutElement = menuDropdownRuntime.AddComponent<LayoutElement>();
        layoutElement.ignoreLayout = true;

        VerticalLayoutGroup layout = menuDropdownRuntime.AddComponent<VerticalLayoutGroup>();
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.spacing = 0f;
        layout.padding = new RectOffset(2, 2, 2, 2);

        for (int i = 0; i < dropdown.options.Count; i++)
        {
            CrearItemDropdownPersonalizado(dropdown, i);
        }

        AgregarBotonesMenuRuntimeALista();

        if (mostrarDebug)
        {
            Debug.Log("Dropdown abierto con menú personalizado: " + dropdown.name);
        }
    }

    private void CrearItemDropdownPersonalizado(TMP_Dropdown dropdown, int indice)
    {
        if (menuDropdownRuntimeRect == null || dropdown == null)
        {
            return;
        }

        GameObject item = new GameObject(
            "DropdownItem_" + indice + "_" + dropdown.options[indice].text,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button)
        );

        RectTransform itemRect = item.GetComponent<RectTransform>();
        itemRect.SetParent(menuDropdownRuntimeRect, false);
        itemRect.sizeDelta = new Vector2(anchoMenuDropdown, altoItemDropdown);

        Image itemImage = item.GetComponent<Image>();
        itemImage.color = indice == dropdown.value
            ? colorItemSeleccionadoMenuDropdown
            : colorItemMenuDropdown;
        itemImage.raycastTarget = true;

        Button button = item.GetComponent<Button>();

        ColorBlock colors = button.colors;
        colors.normalColor = itemImage.color;
        colors.highlightedColor = colorItemSeleccionadoMenuDropdown;
        colors.pressedColor = colorItemSeleccionadoMenuDropdown;
        colors.selectedColor = colorItemSeleccionadoMenuDropdown;
        colors.disabledColor = new Color(0.65f, 0.65f, 0.65f, 1f);
        button.colors = colors;

        int indiceCopia = indice;

        button.onClick.AddListener(() =>
        {
            SeleccionarOpcionDropdownPersonalizado(dropdown, indiceCopia);
        });

        GameObject textoGO = new GameObject(
            "Text",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI)
        );

        RectTransform textoRect = textoGO.GetComponent<RectTransform>();
        textoRect.SetParent(itemRect, false);
        textoRect.anchorMin = Vector2.zero;
        textoRect.anchorMax = Vector2.one;
        textoRect.offsetMin = new Vector2(8f, 0f);
        textoRect.offsetMax = new Vector2(-8f, 0f);

        TextMeshProUGUI texto = textoGO.GetComponent<TextMeshProUGUI>();
        texto.text = dropdown.options[indice].text;
        texto.color = colorTextoMenuDropdown;
        texto.fontSize = 24f;
        texto.alignment = TextAlignmentOptions.MidlineLeft;
        texto.textWrappingMode = TextWrappingModes.NoWrap;
        texto.raycastTarget = false;
    }

    private void AgregarBotonesMenuRuntimeALista()
    {
        if (menuDropdownRuntime == null)
        {
            return;
        }

        Button[] botonesMenu = menuDropdownRuntime.GetComponentsInChildren<Button>(true);

        for (int i = 0; i < botonesMenu.Length; i++)
        {
            AgregarBoton(botonesMenu[i]);
        }
    }

    private void SeleccionarOpcionDropdownPersonalizado(TMP_Dropdown dropdown, int indice)
    {
        if (dropdown == null || dropdown.options == null || dropdown.options.Count == 0)
        {
            return;
        }

        indice = Mathf.Clamp(indice, 0, dropdown.options.Count - 1);

        dropdown.value = indice;
        dropdown.RefreshShownValue();

        if (mostrarDebug)
        {
            Debug.Log(
                "Opción seleccionada en menú personalizado: " +
                dropdown.name + " = " + dropdown.options[indice].text
            );
        }

        CerrarMenuDropdownPersonalizado();
    }

    private void CerrarMenuDropdownPersonalizado()
    {
        if (menuDropdownRuntime != null)
        {
            Button[] botonesMenu = menuDropdownRuntime.GetComponentsInChildren<Button>(true);

            for (int i = 0; i < botonesMenu.Length; i++)
            {
                botones.Remove(botonesMenu[i]);
            }

            Destroy(menuDropdownRuntime);
        }

        menuDropdownRuntime = null;
        menuDropdownRuntimeRect = null;
        dropdownRuntimeActual = null;
        dropdownAbierto = null;
    }

    private bool EsGatilloIzquierdoPresionado()
    {
        float valorLTouch = OVRInput.Get(
            OVRInput.Axis1D.PrimaryIndexTrigger,
            OVRInput.Controller.LTouch
        );

        float valorTouch = OVRInput.Get(
            OVRInput.Axis1D.PrimaryIndexTrigger,
            OVRInput.Controller.Touch
        );

        float valorFinal = Mathf.Max(valorLTouch, valorTouch);

        bool presionadoAhora = valorFinal >= umbralGatillo;
        bool inicioPresion = presionadoAhora && !gatilloIzquierdoAnterior;

        gatilloIzquierdoAnterior = presionadoAhora;

        if (mostrarDebug && valorFinal > 0.05f)
        {
            Debug.Log("Gatillo izquierdo: " + valorFinal.ToString("F2"));
        }

        return inicioPresion;
    }

    private bool EsGatilloDerechoPresionado()
    {
        float valorRTouch = OVRInput.Get(
            OVRInput.Axis1D.PrimaryIndexTrigger,
            OVRInput.Controller.RTouch
        );

        float valorTouch = OVRInput.Get(
            OVRInput.Axis1D.SecondaryIndexTrigger,
            OVRInput.Controller.Touch
        );

        float valorFinal = Mathf.Max(valorRTouch, valorTouch);

        bool presionadoAhora = valorFinal >= umbralGatillo;
        bool inicioPresion = presionadoAhora && !gatilloDerechoAnterior;

        gatilloDerechoAnterior = presionadoAhora;

        if (mostrarDebug && valorFinal > 0.05f)
        {
            Debug.Log("Gatillo derecho: " + valorFinal.ToString("F2"));
        }

        return inicioPresion;
    }

    private void ActualizarDebugHover()
    {
        if (botonHoverDebugAnterior == botonHoverActual &&
            dropdownHoverDebugAnterior == dropdownHoverActual)
        {
            return;
        }

        botonHoverDebugAnterior = botonHoverActual;
        dropdownHoverDebugAnterior = dropdownHoverActual;

        if (botonHoverActual != null)
        {
            Debug.Log("Rayo VR sobre botón: " + botonHoverActual.name);
        }
        else if (dropdownHoverActual != null)
        {
            Debug.Log("Rayo VR sobre dropdown: " + dropdownHoverActual.name);
        }
    }

    private void ActualizarHoverBotonesWarningInvitado()
    {
        if (!ignorarBotonesBracketHover)
        {
            return;
        }

        Button warningHoverActual = EsBotonWarningInvitado(botonHoverActual)
            ? botonHoverActual
            : null;

        if (warningHoverActual == botonHoverWarningAnterior)
        {
            return;
        }

        PointerEventData data = ObtenerPointerData();

        if (botonHoverWarningAnterior != null)
        {
            ExecuteEvents.Execute(
                botonHoverWarningAnterior.gameObject,
                data,
                ExecuteEvents.pointerExitHandler
            );
        }

        botonHoverWarningAnterior = warningHoverActual;

        if (botonHoverWarningAnterior != null)
        {
            ExecuteEvents.Execute(
                botonHoverWarningAnterior.gameObject,
                data,
                ExecuteEvents.pointerEnterHandler
            );
        }
    }

    private void ActualizarHoverBotonesConfiguracion()
    {
        Button nuevoHover = EsBotonConfiguracion(botonHoverActual)
            ? botonHoverActual
            : null;

        if (nuevoHover == botonHoverConfiguracionAnterior)
        {
            return;
        }

        PointerEventData data = ObtenerPointerData();

        if (botonHoverConfiguracionAnterior != null)
        {
            ExecuteEvents.Execute(
                botonHoverConfiguracionAnterior.gameObject,
                data,
                ExecuteEvents.pointerExitHandler
            );
        }

        botonHoverConfiguracionAnterior = nuevoHover;

        if (botonHoverConfiguracionAnterior != null)
        {
            ExecuteEvents.Execute(
                botonHoverConfiguracionAnterior.gameObject,
                data,
                ExecuteEvents.pointerEnterHandler
            );
        }
    }

    private void EjecutarPointerDownUp(Button boton)
    {
        if (boton == null)
        {
            return;
        }

        PointerEventData data = ObtenerPointerData();

        ExecuteEvents.Execute(
            boton.gameObject,
            data,
            ExecuteEvents.pointerDownHandler
        );

        ExecuteEvents.Execute(
            boton.gameObject,
            data,
            ExecuteEvents.pointerUpHandler
        );
    }

    private PointerEventData ObtenerPointerData()
    {
        if (pointerEventDataRuntime == null)
        {
            EventSystem eventSystem = EventSystem.current;

            if (eventSystem == null)
            {
                GameObject eventSystemGO = new GameObject(
                    "EventSystem",
                    typeof(EventSystem),
                    typeof(StandaloneInputModule)
                );

                eventSystem = eventSystemGO.GetComponent<EventSystem>();
            }

            pointerEventDataRuntime = new PointerEventData(eventSystem);
        }

        return pointerEventDataRuntime;
    }

    private void ActualizarColoresBotones()
    {
        for (int i = 0; i < botones.Count; i++)
        {
            Button boton = botones[i];

            if (boton == null)
            {
                continue;
            }

            Image imagen = boton.GetComponent<Image>();

            if (imagen == null)
            {
                continue;
            }

            if (EsBotonWarningInvitado(boton))
            {
                PrepararBotonWarningInvitado(boton, imagen);
                continue;
            }

            if (EsComponentePorNombre(boton.gameObject, "AlgoLabPracticeLabel"))
            {
                continue;
            }

            if (EsComponentePorNombre(boton.gameObject, "AlgoLabPracticeClassificationZone"))
            {
                continue;
            }

            if (menuDropdownRuntime != null && boton.transform.IsChildOf(menuDropdownRuntime.transform))
            {
                continue;
            }

            if (EsBotonConfiguracion(boton))
            {
                continue;
            }

            AlgoLabRobotPracticeButton botonRobot =
                boton.GetComponent<AlgoLabRobotPracticeButton>();
            if (botonRobot != null)
            {
                botonRobot.SetHovered(boton == botonHoverActual && boton.interactable);
                continue;
            }

            if (usarColoresEspecialesMetodos && EsBotonMetodo(boton))
            {
                AplicarColorBotonMetodo(boton, imagen);
                continue;
            }

            if (boton == botonHoverActual && boton.interactable)
            {
                imagen.color = colorHover;
            }
            else
            {
                string nombre = boton.name.ToLower();

                if (nombre.Contains("enviar") ||
                    nombre.Contains("enter") ||
                    nombre.Contains("crear") ||
                    nombre.Contains("iniciar"))
                {
                    imagen.color = colorNormalEnviar;
                }
                else if (nombre.Contains("regrabar") ||
                         nombre.Contains("quitar"))
                {
                    imagen.color = colorNormalRegrabar;
                }
                else
                {
                    imagen.color = colorNormal;
                }
            }
        }
    }

    private void PrepararBotonWarningInvitado(Button boton, Image imagen)
    {
        if (boton == null || imagen == null)
        {
            return;
        }

        Color transparente = imagen.color;
        transparente.a = 0f;
        imagen.color = transparente;
        imagen.raycastTarget = true;

        boton.transition = Selectable.Transition.None;
        boton.interactable = true;

        TMP_Text[] textos = boton.GetComponentsInChildren<TMP_Text>(true);

        for (int i = 0; i < textos.Length; i++)
        {
            Color c = textos[i].color;
            c.a = 1f;
            textos[i].color = c;
            textos[i].raycastTarget = false;
        }
    }

    private bool EsBotonWarningInvitado(Button boton)
    {
        if (boton == null)
        {
            return false;
        }

        if (boton.GetComponent<AlgoLabBracketHoverButton>() != null)
        {
            return true;
        }

        string nombre = boton.name.ToLower();

        return nombre.Contains("confirmarinvitado") ||
               nombre.Contains("cancelarinvitado") ||
               nombre.Contains("continuar") ||
               nombre.Contains("cancelar");
    }

    private bool EsBotonConfiguracion(Button boton)
    {
        return boton != null &&
               boton.GetComponentInParent<AlgoLabSettingsMenuController>(true) != null;
    }

    private bool EsComponentePorNombre(GameObject obj, string nombreComponente)
    {
        if (obj == null)
        {
            return false;
        }

        Component[] componentes = obj.GetComponents<Component>();

        for (int i = 0; i < componentes.Length; i++)
        {
            if (componentes[i] == null)
            {
                continue;
            }

            if (componentes[i].GetType().Name == nombreComponente)
            {
                return true;
            }
        }

        return false;
    }

    private bool EsBotonMetodo(Button boton)
    {
        if (boton == null)
        {
            return false;
        }

        string texto = ObtenerTextoBoton(boton).ToLower();
        string nombre = boton.name.ToLower();

        return texto.Contains("encender") ||
               texto.Contains("acelerar") ||
               texto.Contains("frenar") ||
               texto.Contains("apagar") ||
               nombre.Contains("encender") ||
               nombre.Contains("acelerar") ||
               nombre.Contains("frenar") ||
               nombre.Contains("apagar") ||
               nombre.Contains("metodo") ||
               nombre.Contains("método");
    }

    private string ObtenerTextoBoton(Button boton)
    {
        if (boton == null)
        {
            return "";
        }

        TMP_Text tmp = boton.GetComponentInChildren<TMP_Text>(true);

        if (tmp != null)
        {
            return tmp.text.Trim();
        }

        Text textoNormal = boton.GetComponentInChildren<Text>(true);

        if (textoNormal != null)
        {
            return textoNormal.text.Trim();
        }

        return boton.name.Trim();
    }

    private void AplicarColorBotonMetodo(Button boton, Image imagen)
    {
        if (boton == null || imagen == null)
        {
            return;
        }

        string textoBoton = ObtenerTextoBoton(boton);

        bool estaSeleccionado =
            !string.IsNullOrWhiteSpace(metodoSeleccionadoVisual) &&
            textoBoton.Trim().ToLower() == metodoSeleccionadoVisual.Trim().ToLower();

        if (boton == botonHoverActual && boton.interactable)
        {
            imagen.color = colorMetodoHover;
            CambiarColorTextoBoton(boton, colorTextoMetodoSeleccionado);
            return;
        }

        if (estaSeleccionado)
        {
            imagen.color = colorMetodoSeleccionado;
            CambiarColorTextoBoton(boton, colorTextoMetodoSeleccionado);
        }
        else
        {
            imagen.color = colorMetodoNormal;
            CambiarColorTextoBoton(boton, colorTextoMetodoNormal);
        }
    }

    private void CambiarColorTextoBoton(Button boton, Color color)
    {
        if (boton == null)
        {
            return;
        }

        TMP_Text[] textosTMP = boton.GetComponentsInChildren<TMP_Text>(true);

        for (int i = 0; i < textosTMP.Length; i++)
        {
            textosTMP[i].color = color;
        }

        Text[] textosUI = boton.GetComponentsInChildren<Text>(true);

        for (int i = 0; i < textosUI.Length; i++)
        {
            textosUI[i].color = color;
        }
    }

    private void OnDisable()
    {
        if (botonHoverWarningAnterior != null)
        {
            ExecuteEvents.Execute(
                botonHoverWarningAnterior.gameObject,
                ObtenerPointerData(),
                ExecuteEvents.pointerExitHandler
            );

            botonHoverWarningAnterior = null;
        }

        if (botonHoverConfiguracionAnterior != null)
        {
            ExecuteEvents.Execute(
                botonHoverConfiguracionAnterior.gameObject,
                ObtenerPointerData(),
                ExecuteEvents.pointerExitHandler
            );

            botonHoverConfiguracionAnterior = null;
        }

        CerrarMenuDropdownPersonalizado();

        if (dropdownAbierto != null)
        {
            dropdownAbierto.Hide();
            dropdownAbierto = null;
        }

        gatilloIzquierdoAnterior = false;
        gatilloDerechoAnterior = false;
        botonHoverActual = null;
        dropdownHoverActual = null;
        botonHoverDebugAnterior = null;
        dropdownHoverDebugAnterior = null;
        interactuablesActivadosEsteFrame.Clear();
        coloresSucios = true;
    }
}
