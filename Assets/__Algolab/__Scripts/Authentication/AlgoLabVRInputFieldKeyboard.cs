using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class AlgoLabVRInputFieldKeyboard : MonoBehaviour
{
    [Header("Rayos de los controles")]
    public Transform leftRayOrigin;
    public Transform rightRayOrigin;

    [Header("Raíces UI")]
    public List<RectTransform> uiRoots = new List<RectTransform>();

    [Header("Inputs detectados")]
    public List<TMP_InputField> inputFields = new List<TMP_InputField>();

    [Header("Búsqueda automática")]
    public bool buscarAutomaticamente = true;
    public bool actualizarCadaFrame = true;
    public bool buscarEnTodaLaEscena = true;

    [Tooltip("Intervalo minimo entre busquedas globales de campos de texto.")]
    [Min(0.1f)]
    public float intervaloActualizacionAutomatica = 0.5f;

    [Header("Configuración")]
    public float distanciaMaxima = 6f;
    public float umbralGatillo = 0.55f;

    [Header("Teclado")]
    [Tooltip("En Quest el teclado VR integrado siempre está activo. Este flag solo afecta al fallback del sistema Android (que queda congelado en modo VR inmersivo).")]
    public bool cerrarTecladoAlTocarFuera = false;

    [Header("Teclado del sistema Meta (overlay oficial)")]
    [Tooltip("FALSE = usa el teclado overlay oficial de Meta (requiere 'oculus.software.overlay_keyboard' en AndroidManifest + focusaware=true, ya configurados). TRUE = usa el teclado VR integrado en la escena como respaldo.")]
    public bool usarTecladoVirtualIntegradoEnQuest = false;

    [Tooltip("Permite probar el teclado VR en Play Mode del editor.")]
    public bool mostrarTecladoVirtualIntegradoEnEditor = false;

    [Header("Configuración del teclado VR integrado (respaldo)")]
    public Vector2 tamanoTecladoVirtual = new Vector2(500f, 245f);
    public Vector2 posicionTecladoEnCanvas = new Vector2(0f, -265f);
    public Color colorFondoTeclado = new Color(0.025f, 0.045f, 0.055f, 0.98f);
    public Color colorTecla = new Color(0.10f, 0.15f, 0.17f, 1f);
    public Color colorTeclaHover = new Color(0.10f, 0.72f, 0.55f, 1f);
    public Color colorTextoTecla = Color.white;


    [Header("Visual opcional")]
    public bool cambiarColorAlApuntar = true;
    public Color colorNormal = Color.white;
    public Color colorHover = new Color(0.15f, 0.85f, 1f, 1f);
    public Color colorSeleccionado = new Color(0.2f, 1f, 0.65f, 1f);

    [Header("Debug")]
    public bool mostrarDebug = false;

    private TMP_InputField inputSeleccionado;
    private TMP_InputField inputHoverActual;

    private bool gatilloIzquierdoAnterior;
    private bool gatilloDerechoAnterior;

    private TouchScreenKeyboard tecladoSistema;
    private RectTransform tecladoVirtualRoot;
    private readonly List<TeclaVirtual> teclasVirtuales = new List<TeclaVirtual>();
    private TeclaVirtual teclaHoverActual;
    private bool mayusculasActivas;
    private float proximaActualizacionAutomatica;
    private int ultimoConteoInputs = -1;
    private TMP_InputField inputHoverDebugAnterior;

    private sealed class TeclaVirtual
    {
        public RectTransform rect;
        public Image fondo;
        public TMP_Text texto;
        public string valor;
    }

    private void Awake()
    {
        AsegurarEventSystem();
        ActualizarListaInputs();
    }

    private void Update()
    {
        if (inputSeleccionado != null &&
            (!inputSeleccionado.gameObject.activeInHierarchy ||
             !inputSeleccionado.interactable || inputSeleccionado.readOnly))
        {
            DeseleccionarInput();
        }

        if (buscarAutomaticamente && actualizarCadaFrame &&
            Time.unscaledTime >= proximaActualizacionAutomatica)
        {
            ActualizarListaInputs();
            proximaActualizacionAutomatica = Time.unscaledTime +
                Mathf.Max(0.1f, intervaloActualizacionAutomatica);
        }

        inputHoverActual = null;

        bool clickIzquierdo = EsGatilloIzquierdoPresionado();
        bool clickDerecho = EsGatilloDerechoPresionado();

        RevisarRayo(leftRayOrigin, clickIzquierdo, "IZQUIERDO");
        RevisarRayo(rightRayOrigin, clickDerecho, "DERECHO");

        if (mostrarDebug && inputHoverDebugAnterior != inputHoverActual)
        {
            inputHoverDebugAnterior = inputHoverActual;

            if (inputHoverActual != null)
            {
                Debug.Log("VR INPUT FIELD: rayo sobre input: " + inputHoverActual.name);
            }
        }

        ActualizarTecladoSistema();
        ActualizarColoresTeclas();

        if (cambiarColorAlApuntar)
        {
            ActualizarColoresInputs();
        }
    }

    [ContextMenu("Actualizar lista inputs")]
    public void ActualizarListaInputs()
    {
        if (!buscarAutomaticamente)
        {
            return;
        }

        inputFields.Clear();

        for (int i = 0; i < uiRoots.Count; i++)
        {
            if (uiRoots[i] != null)
            {
                AgregarInputsDesdeRaiz(uiRoots[i]);
            }
        }

        if (buscarEnTodaLaEscena)
        {
            TMP_InputField[] encontrados = FindObjectsByType<TMP_InputField>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

            for (int i = 0; i < encontrados.Length; i++)
            {
                AgregarInput(encontrados[i]);
            }
        }

        if (mostrarDebug && ultimoConteoInputs != inputFields.Count)
        {
            Debug.Log("VR INPUT FIELD: inputs encontrados: " + inputFields.Count);
        }

        ultimoConteoInputs = inputFields.Count;
    }

    private void AgregarInputsDesdeRaiz(RectTransform raiz)
    {
        TMP_InputField[] encontrados = raiz.GetComponentsInChildren<TMP_InputField>(true);

        for (int i = 0; i < encontrados.Length; i++)
        {
            AgregarInput(encontrados[i]);
        }
    }

    private void AgregarInput(TMP_InputField input)
    {
        if (input == null)
        {
            return;
        }

        if (!inputFields.Contains(input))
        {
            inputFields.Add(input);
        }

        PrepararInput(input);
    }

    private void PrepararInput(TMP_InputField input)
    {
        if (input == null)
        {
            return;
        }

        if (input.textComponent != null)
        {
            input.textComponent.raycastTarget = false;
        }

        if (input.placeholder != null)
        {
            Graphic placeholderGraphic = input.placeholder as Graphic;

            if (placeholderGraphic != null)
            {
                placeholderGraphic.raycastTarget = false;
            }
        }

        // Algunas versiones de la escena guardaron la contraseña como texto
        // estándar. Se corrige por nombre para evitar mostrar la clave escrita.
        string nombreNormalizado = input.name.ToLowerInvariant();
        if (nombreNormalizado.Contains("contrase") || nombreNormalizado.Contains("password"))
        {
            input.contentType = TMP_InputField.ContentType.Password;
            input.inputType = TMP_InputField.InputType.Password;
            input.ForceLabelUpdate();
        }
        else if (nombreNormalizado.Contains("correo") || nombreNormalizado.Contains("email"))
        {
            input.contentType = TMP_InputField.ContentType.EmailAddress;
            input.keyboardType = TouchScreenKeyboardType.EmailAddress;
        }
    }

    private void RevisarRayo(Transform rayOrigin, bool presionoGatillo, string nombreControl)
    {
        if (rayOrigin == null)
        {
            return;
        }

        TeclaVirtual tecla = ObtenerTeclaBajoRayo(rayOrigin);
        if (tecla != null)
        {
            teclaHoverActual = tecla;
            if (presionoGatillo)
            {
                ProcesarTeclaVirtual(tecla.valor);
            }

            return;
        }

        TMP_InputField inputDetectado = ObtenerInputBajoRayo(rayOrigin);

        if (inputDetectado != null)
        {
            inputHoverActual = inputDetectado;

            if (presionoGatillo)
            {
                SeleccionarInput(inputDetectado);
            }

            return;
        }

        if (presionoGatillo && cerrarTecladoAlTocarFuera)
        {
            DeseleccionarInput();
        }
    }

    private TMP_InputField ObtenerInputBajoRayo(Transform rayOrigin)
    {
        Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);

        TMP_InputField mejorInput = null;
        float mejorDistancia = float.MaxValue;

        for (int i = inputFields.Count - 1; i >= 0; i--)
        {
            TMP_InputField input = inputFields[i];

            if (input == null)
            {
                continue;
            }

            if (!input.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (!input.interactable || input.readOnly)
            {
                continue;
            }

            RectTransform rect = input.GetComponent<RectTransform>();

            if (rect == null)
            {
                continue;
            }

            if (!RayoTocaRect(ray, rect, out float distancia))
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
                mejorInput = input;
            }
        }

        return mejorInput;
    }

    private bool RayoTocaRect(Ray ray, RectTransform rect, out float distancia)
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

    private void SeleccionarInput(TMP_InputField input)
    {
        if (input == null || !input.interactable || input.readOnly)
        {
            return;
        }

        inputSeleccionado = input;

        AsegurarEventSystem();

        EventSystem.current.SetSelectedGameObject(input.gameObject);

        input.Select();
        input.ActivateInputField();

        input.caretPosition = input.text.Length;
        input.selectionAnchorPosition = input.text.Length;
        input.selectionFocusPosition = input.text.Length;

        AbrirTeclado(input);

        if (mostrarDebug)
        {
            Debug.Log("VR INPUT FIELD: input seleccionado: " + input.name);
        }
    }

    private void DeseleccionarInput()
    {
        if (inputSeleccionado != null)
        {
            inputSeleccionado.DeactivateInputField();
        }

        inputSeleccionado = null;

        if (tecladoSistema != null)
        {
            tecladoSistema.active = false;
            tecladoSistema = null;
        }

        OcultarTecladoVirtual();

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    private void AbrirTeclado(TMP_InputField input)
    {
        if (input == null)
        {
            return;
        }

        // Si el teclado VR integrado está activado como respaldo, úsalo.
        if (DebeUsarTecladoVirtualIntegrado())
        {
            MostrarTecladoVirtual(input);
            return;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        // Meta System Keyboard Overlay: funciona en Quest con:
        //  - <uses-feature android:name="oculus.software.overlay_keyboard"/> en AndroidManifest
        //  - <meta-data android:name="com.oculus.vr.focusaware" android:value="true"/>
        // Ambos ya están configurados en Assets/Plugins/Android/AndroidManifest.xml
        TouchScreenKeyboardType tipoTeclado = TouchScreenKeyboardType.Default;
        bool seguro = false;

        if (input.contentType == TMP_InputField.ContentType.EmailAddress)
        {
            tipoTeclado = TouchScreenKeyboardType.EmailAddress;
        }

        if (input.contentType == TMP_InputField.ContentType.Password)
        {
            tipoTeclado = TouchScreenKeyboardType.Default;
            seguro = true;
        }

        string placeholder = input.placeholder != null
            ? (input.placeholder.GetComponent<TMP_Text>()?.text ?? "")
            : "";

        tecladoSistema = TouchScreenKeyboard.Open(
            input.text,
            tipoTeclado,
            false,   // autoCorrection
            false,   // multiline
            seguro,  // secure (oculta el texto)
            false,   // alert
            placeholder
        );

        if (mostrarDebug)
        {
            Debug.Log("VR INPUT FIELD: abriendo teclado overlay de Meta. Status: " +
                (tecladoSistema != null ? tecladoSistema.status.ToString() : "null"));
        }
#else
        if (mostrarDebug)
        {
            Debug.Log("VR INPUT FIELD: editor detectado. Usa el teclado físico del PC.");
        }
#endif
    }

    private bool DebeUsarTecladoVirtualIntegrado()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return usarTecladoVirtualIntegradoEnQuest;
#else
        return mostrarTecladoVirtualIntegradoEnEditor;
#endif
    }

    private void MostrarTecladoVirtual(TMP_InputField input)
    {
        Canvas canvas = input != null ? input.GetComponentInParent<Canvas>(true) : null;
        if (canvas == null)
        {
            Debug.LogWarning("VR INPUT FIELD: no se encontró el Canvas para crear el teclado VR.");
            return;
        }

        canvas = canvas.rootCanvas != null ? canvas.rootCanvas : canvas;
        if (tecladoVirtualRoot == null || tecladoVirtualRoot.parent != canvas.transform)
        {
            DestruirTecladoVirtual();
            CrearTecladoVirtual(canvas, input.textComponent != null ? input.textComponent.font : null);
        }

        if (tecladoVirtualRoot == null)
        {
            return;
        }

        tecladoVirtualRoot.anchoredPosition = posicionTecladoEnCanvas;
        tecladoVirtualRoot.localRotation = Quaternion.identity;
        tecladoVirtualRoot.localScale = Vector3.one;
        tecladoVirtualRoot.SetAsLastSibling();
        tecladoVirtualRoot.gameObject.SetActive(true);
        ActualizarEtiquetasMayusculas();
    }

    private void CrearTecladoVirtual(Canvas canvas, TMP_FontAsset fuente)
    {
        GameObject root = new GameObject(
            "TecladoVR_Login_Runtime",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Outline)
        );

        tecladoVirtualRoot = root.GetComponent<RectTransform>();
        tecladoVirtualRoot.SetParent(canvas.transform, false);
        tecladoVirtualRoot.anchorMin = tecladoVirtualRoot.anchorMax = new Vector2(0.5f, 0.5f);
        tecladoVirtualRoot.pivot = new Vector2(0.5f, 0.5f);
        tecladoVirtualRoot.sizeDelta = tamanoTecladoVirtual;
        tecladoVirtualRoot.anchoredPosition = posicionTecladoEnCanvas;

        Image fondo = root.GetComponent<Image>();
        fondo.color = colorFondoTeclado;
        fondo.raycastTarget = false;

        Outline borde = root.GetComponent<Outline>();
        borde.effectColor = new Color(0.10f, 0.90f, 0.68f, 0.9f);
        borde.effectDistance = new Vector2(2f, -2f);

        string[][] filas =
        {
            new[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0" },
            new[] { "Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P" },
            new[] { "A", "S", "D", "F", "G", "H", "J", "K", "L" },
            new[] { "Z", "X", "C", "V", "B", "N", "M" },
            new[] { "@", ".", "-", "_", "!", "MAYÚS", "ESPACIO", "BORRAR", "LISTO" }
        };

        const float margenX = 12f;
        const float margenY = 12f;
        const float separacion = 5f;
        const float altoTecla = 39f;
        float yInicial = tamanoTecladoVirtual.y * 0.5f - margenY - altoTecla * 0.5f;

        for (int fila = 0; fila < filas.Length; fila++)
        {
            string[] valores = filas[fila];
            float pesoTotal = 0f;
            for (int i = 0; i < valores.Length; i++)
            {
                pesoTotal += PesoTecla(valores[i]);
            }

            float anchoDisponible = tamanoTecladoVirtual.x - margenX * 2f -
                separacion * (valores.Length - 1);
            float unidad = anchoDisponible / Mathf.Max(1f, pesoTotal);
            float anchoFila = anchoDisponible + separacion * (valores.Length - 1);
            float x = -anchoFila * 0.5f;

            for (int i = 0; i < valores.Length; i++)
            {
                float ancho = unidad * PesoTecla(valores[i]);
                CrearTeclaVirtual(
                    tecladoVirtualRoot,
                    valores[i],
                    new Vector2(x + ancho * 0.5f, yInicial - fila * (altoTecla + separacion)),
                    new Vector2(ancho, altoTecla),
                    fuente
                );
                x += ancho + separacion;
            }
        }

        tecladoVirtualRoot.gameObject.SetActive(false);
    }

    private static float PesoTecla(string valor)
    {
        switch (valor)
        {
            case "MAYÚS": return 1.8f;
            case "ESPACIO": return 2.4f;
            case "BORRAR": return 2f;
            case "LISTO": return 1.6f;
            default: return 1f;
        }
    }

    private void CrearTeclaVirtual(
        RectTransform padre,
        string valor,
        Vector2 posicion,
        Vector2 tamano,
        TMP_FontAsset fuente)
    {
        GameObject objeto = new GameObject(
            "Tecla_" + valor,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Outline)
        );
        RectTransform rect = objeto.GetComponent<RectTransform>();
        rect.SetParent(padre, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = posicion;
        rect.sizeDelta = tamano;

        Image imagen = objeto.GetComponent<Image>();
        imagen.color = colorTecla;
        imagen.raycastTarget = false;

        Outline borde = objeto.GetComponent<Outline>();
        borde.effectColor = new Color(0.45f, 0.62f, 0.66f, 0.65f);
        borde.effectDistance = new Vector2(1f, -1f);

        GameObject objetoTexto = new GameObject(
            "Texto",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI)
        );
        RectTransform rectTexto = objetoTexto.GetComponent<RectTransform>();
        rectTexto.SetParent(rect, false);
        rectTexto.anchorMin = Vector2.zero;
        rectTexto.anchorMax = Vector2.one;
        rectTexto.offsetMin = Vector2.zero;
        rectTexto.offsetMax = Vector2.zero;

        TMP_Text texto = objetoTexto.GetComponent<TMP_Text>();
        texto.text = valor;
        texto.font = fuente;
        texto.fontSize = valor.Length > 4 ? 15f : 20f;
        texto.color = colorTextoTecla;
        texto.alignment = TextAlignmentOptions.Center;
        texto.raycastTarget = false;
        texto.textWrappingMode = TextWrappingModes.NoWrap;

        teclasVirtuales.Add(new TeclaVirtual
        {
            rect = rect,
            fondo = imagen,
            texto = texto,
            valor = valor
        });
    }

    private TeclaVirtual ObtenerTeclaBajoRayo(Transform rayOrigin)
    {
        if (rayOrigin == null || tecladoVirtualRoot == null ||
            !tecladoVirtualRoot.gameObject.activeInHierarchy)
        {
            return null;
        }

        Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
        TeclaVirtual mejor = null;
        float mejorDistancia = float.MaxValue;

        for (int i = 0; i < teclasVirtuales.Count; i++)
        {
            TeclaVirtual tecla = teclasVirtuales[i];
            if (tecla == null || tecla.rect == null || !tecla.rect.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (RayoTocaRect(ray, tecla.rect, out float distancia) &&
                distancia >= 0f && distancia <= distanciaMaxima && distancia < mejorDistancia)
            {
                mejor = tecla;
                mejorDistancia = distancia;
            }
        }

        return mejor;
    }

    private void ProcesarTeclaVirtual(string valor)
    {
        TMP_InputField input = inputSeleccionado;
        if (input == null || !input.interactable || input.readOnly)
        {
            return;
        }

        switch (valor)
        {
            case "MAYÚS":
                mayusculasActivas = !mayusculasActivas;
                ActualizarEtiquetasMayusculas();
                return;

            case "BORRAR":
                if (!string.IsNullOrEmpty(input.text))
                {
                    input.text = input.text.Substring(0, input.text.Length - 1);
                }
                break;

            case "ESPACIO":
                AgregarCaracterSiCabe(input, " ");
                break;

            case "LISTO":
                input.onSubmit.Invoke(input.text);
                input.onEndEdit.Invoke(input.text);
                DeseleccionarInput();
                return;

            default:
                string texto = valor;
                if (valor.Length == 1 && char.IsLetter(valor[0]))
                {
                    texto = mayusculasActivas ? valor.ToUpperInvariant() : valor.ToLowerInvariant();
                    if (mayusculasActivas)
                    {
                        mayusculasActivas = false;
                        ActualizarEtiquetasMayusculas();
                    }
                }

                AgregarCaracterSiCabe(input, texto);
                break;
        }

        input.caretPosition = input.text.Length;
        input.selectionAnchorPosition = input.text.Length;
        input.selectionFocusPosition = input.text.Length;
        input.Select();
        input.ActivateInputField();
    }

    private static void AgregarCaracterSiCabe(TMP_InputField input, string valor)
    {
        if (input == null || string.IsNullOrEmpty(valor))
        {
            return;
        }

        if (input.characterLimit > 0 && input.text.Length + valor.Length > input.characterLimit)
        {
            return;
        }

        input.text += valor;
    }

    private void ActualizarEtiquetasMayusculas()
    {
        for (int i = 0; i < teclasVirtuales.Count; i++)
        {
            TeclaVirtual tecla = teclasVirtuales[i];
            if (tecla == null || tecla.texto == null || string.IsNullOrEmpty(tecla.valor))
            {
                continue;
            }

            if (tecla.valor.Length == 1 && char.IsLetter(tecla.valor[0]))
            {
                tecla.texto.text = mayusculasActivas
                    ? tecla.valor.ToUpperInvariant()
                    : tecla.valor.ToLowerInvariant();
            }
        }
    }

    private void ActualizarColoresTeclas()
    {
        for (int i = 0; i < teclasVirtuales.Count; i++)
        {
            TeclaVirtual tecla = teclasVirtuales[i];
            if (tecla == null || tecla.fondo == null)
            {
                continue;
            }

            bool esMayusActiva = tecla.valor == "MAYÚS" && mayusculasActivas;
            tecla.fondo.color = tecla == teclaHoverActual || esMayusActiva
                ? colorTeclaHover
                : colorTecla;
        }
    }

    private void OcultarTecladoVirtual()
    {
        teclaHoverActual = null;
        mayusculasActivas = false;
        if (tecladoVirtualRoot != null)
        {
            tecladoVirtualRoot.gameObject.SetActive(false);
        }
    }

    private void DestruirTecladoVirtual()
    {
        teclasVirtuales.Clear();
        teclaHoverActual = null;
        if (tecladoVirtualRoot != null)
        {
            Destroy(tecladoVirtualRoot.gameObject);
            tecladoVirtualRoot = null;
        }
    }

    private void ActualizarTecladoSistema()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (tecladoSistema == null || inputSeleccionado == null)
        {
            return;
        }

        if (tecladoSistema.status == TouchScreenKeyboard.Status.Visible)
        {
            inputSeleccionado.text = tecladoSistema.text;
            inputSeleccionado.caretPosition = inputSeleccionado.text.Length;
        }

        if (tecladoSistema.status == TouchScreenKeyboard.Status.Done)
        {
            inputSeleccionado.text = tecladoSistema.text;
            inputSeleccionado.caretPosition = inputSeleccionado.text.Length;
            TMP_InputField inputFinalizado = inputSeleccionado;
            tecladoSistema = null;
            inputFinalizado.onEndEdit.Invoke(inputFinalizado.text);
            DeseleccionarInput();
            return;
        }

        if (tecladoSistema != null &&
            (tecladoSistema.status == TouchScreenKeyboard.Status.Canceled ||
             tecladoSistema.status == TouchScreenKeyboard.Status.LostFocus))
        {
            DeseleccionarInput();
        }
#endif
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

        return inicioPresion;
    }

    private void ActualizarColoresInputs()
    {
        for (int i = 0; i < inputFields.Count; i++)
        {
            TMP_InputField input = inputFields[i];

            if (input == null)
            {
                continue;
            }

            if (!input.interactable || input.readOnly)
            {
                continue;
            }

            Image image = input.GetComponent<Image>();

            if (image == null)
            {
                continue;
            }

            if (input == inputSeleccionado)
            {
                image.color = colorSeleccionado;
            }
            else if (input == inputHoverActual)
            {
                image.color = colorHover;
            }
            else
            {
                image.color = colorNormal;
            }
        }
    }

    private void AsegurarEventSystem()
    {
        if (EventSystem.current != null)
        {
            return;
        }

        GameObject eventSystemGO = new GameObject(
            "EventSystem",
            typeof(EventSystem),
            typeof(StandaloneInputModule)
        );

        if (mostrarDebug)
        {
            Debug.Log("VR INPUT FIELD: EventSystem creado automáticamente.");
        }
    }

    private void OnDisable()
    {
        DeseleccionarInput();
        inputHoverActual = null;
        teclaHoverActual = null;
        inputHoverDebugAnterior = null;
        gatilloIzquierdoAnterior = false;
        gatilloDerechoAnterior = false;
    }
}
