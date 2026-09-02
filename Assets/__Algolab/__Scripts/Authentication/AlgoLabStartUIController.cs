using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class AlgoLabStartUIController : MonoBehaviour
{
    [Header("Referencias principales")]
    public AlgoLabSessionManager sessionManager;
    public AlgoLabBackendClient backendClient;
    public AlgoLabProgressSaver progressSaver;

    [Header("Pantallas")]
    public GameObject pantallaBienvenida;
    public GameObject pantallaSeleccionModo;
    public GameObject pantallaLogin;
    public GameObject pantallaAvisoInvitado;

    [Header("Campos login")]
    public TMP_InputField inputCorreo;
    public TMP_InputField inputContrasena;

    [Header("Segundo factor (se crea automáticamente si falta)")]
    public GameObject panelSegundoFactor;
    public TMP_InputField inputCodigoSegundoFactor;
    public TMP_Text textoTituloSegundoFactor;
    public TMP_Text textoDestinoSegundoFactor;
    public TMP_Text textoEstadoSegundoFactor;
    public TMP_Text textoTiempoSegundoFactor;
    public Button btnVerificarSegundoFactor;
    public Button btnReenviarSegundoFactor;
    public Button btnVolverSegundoFactor;
    [Tooltip("CORREO es el canal seguro predeterminado. Usa SMS solo si el perfil tiene celular verificado y Twilio está configurado.")]
    public string canalSegundoFactor = "CORREO";

    [Header("Textos")]
    public TMP_Text textoMensajeLogin;
    public TMP_Text textoMensajeInvitado;
    public TMP_Text textoNombreApp;

    [Header("Efecto texto invitado")]
    public AlgoLabTypewriterText textoInvitadoTypewriter;

    [Header("Botones")]
    public Button btnJugar;
    public Button btnIrLogin;
    public Button btnJugarInvitado;
    public Button btnEntrarLogin;
    public Button btnVolverDesdeLogin;
    public Button btnConfirmarInvitado;
    public Button btnCancelarInvitado;
    public Button btnCerrarSesion;

    [Header("Configuración")]
    public string nombreApp = "AlgoLab";
    public bool mostrarBienvenidaAlIniciar = true;

    [Tooltip("Solo entra directo si hay sesión real con token. El invitado nunca entra automático.")]
    public bool entrarAutomaticamenteSiYaHaySesion = true;

    public bool consultarProgresoDespuesDeLogin = true;
    public bool limpiarCamposDespuesDeLogin = true;

    [TextArea(2, 4)]
    public string mensajeInvitado =
        "Estás entrando como invitado.\nTu progreso no se guardará.\nInicia sesión para guardar información.";

    [Header("Transición de pantallas")]
    public bool usarTransicionSuave = true;
    public float duracionTransicionPantallas = 2f;
    public bool escalarDuranteTransicion = true;
    public float escalaOculta = 0.92f;

    [Header("Bloqueo seguro de botones")]
    [Tooltip("Activado = los botones de pantallas que no son la pantalla actual quedan deshabilitados incluso mientras desaparecen con animación.")]
    public bool deshabilitarBotonesPantallaNoActiva = true;

    [Tooltip("Activado = desactiva los colliders que estén dentro de botones ocultos. Útil en VR si el rayo detecta colliders aunque el CanvasGroup esté en alpha 0.")]
    public bool deshabilitarCollidersBotonesPantallaNoActiva = true;

    [Tooltip("Activado = la pantalla que está entrando habilita botones solo al terminar la transición. Evita doble click mientras se anima.")]
    public bool bloquearBotonesDuranteTransicion = true;

    [Header("Eventos")]
    public UnityEvent OnAccesoPermitido;
    public UnityEvent OnLoginCorrecto;
    public UnityEvent OnInvitadoCorrecto;
    public UnityEvent OnCerrarSesion;

    [Header("Tutorial de bienvenida")]
    public AlgoLabTutorialPanelController tutorialBienvenida;
    public bool mostrarTutorialDespuesDeEntrar = true;
    public float retrasoTutorialDespuesDeEntrar = 2f;

    [Tooltip("Compatibilidad visual. El hito real del tutorial pertenece a la cuenta y siempre se respeta.")]
    public bool mostrarTutorialConSesionGuardada = true;

    [Header("Debug")]
    public bool mostrarDebug = true;

    private bool procesandoLogin = false;
    private bool cerrandoSesion = false;
    private bool puedeReintentarValidacionSesionGuardada = false;
    private bool esperandoSegundoFactor = false;
    private string desafioSegundoFactorId = string.Empty;
    private float segundoFactorExpiraEn;
    private float segundoFactorReenvioEn;
    private Coroutine rutinaAnimacionSegundoFactor;
    private Coroutine rutinaTutorialBienvenida;
    private int generacionOperacion;

    private Dictionary<GameObject, Coroutine> rutinasTransicion =
        new Dictionary<GameObject, Coroutine>();
    private readonly Dictionary<Button, bool> estadoBotonesBloqueados = new Dictionary<Button, bool>();
    private readonly Dictionary<TMP_InputField, bool> estadoInputsBloqueados = new Dictionary<TMP_InputField, bool>();
    private readonly Dictionary<Collider, bool> estadoCollidersBloqueados = new Dictionary<Collider, bool>();
    private readonly Dictionary<Image, bool> estadoImagenesBloqueadas = new Dictionary<Image, bool>();

    private void OnDisable()
    {
        bool habiaLoginPendiente = procesandoLogin;
        generacionOperacion++;
        procesandoLogin = false;

        if (habiaLoginPendiente && backendClient != null)
        {
            backendClient.CancelarInicioSesionPendiente();
        }

        if (rutinaTutorialBienvenida != null)
        {
            StopCoroutine(rutinaTutorialBienvenida);
            rutinaTutorialBienvenida = null;
        }

        if (rutinaAnimacionSegundoFactor != null)
        {
            StopCoroutine(rutinaAnimacionSegundoFactor);
            rutinaAnimacionSegundoFactor = null;
        }

        foreach (KeyValuePair<GameObject, Coroutine> par in rutinasTransicion)
        {
            if (par.Value != null)
            {
                StopCoroutine(par.Value);
            }
        }

        rutinasTransicion.Clear();
    }

    private void OnDestroy()
    {
        DesconectarBoton(btnJugar, MostrarPantallaSeleccionModo);
        DesconectarBoton(btnIrLogin, MostrarPantallaLogin);
        DesconectarBoton(btnJugarInvitado, MostrarPantallaAvisoInvitado);
        DesconectarBoton(btnEntrarLogin, IniciarSesionDesdeUI);
        DesconectarBoton(btnVolverDesdeLogin, VolverDesdeLoginAlInicio);
        DesconectarBoton(btnConfirmarInvitado, EntrarComoInvitado);
        DesconectarBoton(btnCancelarInvitado, CancelarInvitadoYVolverInicio);
        DesconectarBoton(btnCerrarSesion, CerrarSesionYVolverInicio);
        DesconectarBoton(btnVerificarSegundoFactor, VerificarSegundoFactorDesdeUI);
        DesconectarBoton(btnReenviarSegundoFactor, ReenviarSegundoFactorDesdeUI);
        DesconectarBoton(btnVolverSegundoFactor, VolverDesdeSegundoFactor);
    }

    private static void DesconectarBoton(Button boton, UnityEngine.Events.UnityAction accion)
    {
        if (boton != null)
            boton.onClick.RemoveListener(accion);
    }

    private void Awake()
    {
        BuscarReferencias();
        AsegurarBotonVolverDesdeLogin();
        AsegurarInterfazSegundoFactor();
        PrepararPantallasParaTransicion();
        ConectarBotones();
    }

    private void Start()
    {
        BuscarReferencias();
        ConfigurarTextosIniciales();

        // Solo el usuario autenticado con token entra automáticamente.
        // El invitado siempre vuelve a la pantalla inicial cuando se abre el juego.
        if (entrarAutomaticamenteSiYaHaySesion &&
            sessionManager != null &&
            sessionManager.EstaAutenticado)
        {
            DebugLog(
                "START UI: existe sesión autenticada guardada. Se validará antes de permitir acceso."
            );
            ValidarSesionGuardadaAntesDeEntrar();
            return;
        }

        if (sessionManager != null &&
            sessionManager.SesionIniciada &&
            sessionManager.ModoInvitado)
        {
            DebugLog("START UI: sesión invitada detectada al iniciar. Se cierra para volver al inicio.");
            sessionManager.CerrarSesion();
        }

        if (mostrarBienvenidaAlIniciar)
        {
            MostrarPantallaBienvenida();
        }
        else
        {
            MostrarPantallaSeleccionModo();
        }
    }

    private void Update()
    {
        if (!esperandoSegundoFactor || panelSegundoFactor == null || !panelSegundoFactor.activeInHierarchy)
        {
            return;
        }

        float restante = Mathf.Max(0f, segundoFactorExpiraEn - Time.unscaledTime);
        float reenvio = Mathf.Max(0f, segundoFactorReenvioEn - Time.unscaledTime);

        if (textoTiempoSegundoFactor != null)
        {
            int segundos = Mathf.CeilToInt(restante);
            textoTiempoSegundoFactor.text = restante > 0f
                ? "Código vigente  " + (segundos / 60).ToString("00") + ":" + (segundos % 60).ToString("00")
                : "El código venció. Solicita uno nuevo.";
            textoTiempoSegundoFactor.color = restante > 20f
                ? new Color(0.48f, 1f, 0.78f, 1f)
                : new Color(1f, 0.48f, 0.42f, 1f);
        }

        if (btnReenviarSegundoFactor != null)
        {
            btnReenviarSegundoFactor.interactable = !procesandoLogin && reenvio <= 0f;
            TMP_Text etiqueta = btnReenviarSegundoFactor.GetComponentInChildren<TMP_Text>(true);
            if (etiqueta != null)
            {
                etiqueta.text = reenvio > 0f
                    ? "Reenviar (" + Mathf.CeilToInt(reenvio) + ")"
                    : "Reenviar código";
            }
        }
    }

    private void BuscarReferencias()
    {
        if (sessionManager == null)
        {
            sessionManager = AlgoLabSessionManager.Instance;
        }

        if (sessionManager == null)
        {
            sessionManager = FindFirstObjectByType<AlgoLabSessionManager>();
        }

        if (backendClient == null)
        {
            backendClient = AlgoLabBackendClient.Instance;
        }

        if (backendClient == null)
        {
            backendClient = FindFirstObjectByType<AlgoLabBackendClient>();
        }

        if (progressSaver == null)
        {
            progressSaver = AlgoLabProgressSaver.Instance;
        }

        if (progressSaver == null)
        {
            progressSaver = FindFirstObjectByType<AlgoLabProgressSaver>(
                FindObjectsInactive.Include
            );
        }

        if (tutorialBienvenida == null)
        {
            tutorialBienvenida = FindFirstObjectByType<AlgoLabTutorialPanelController>(
                FindObjectsInactive.Include
            );
        }
    }

    private void AsegurarBotonVolverDesdeLogin()
    {
        if (pantallaLogin == null)
        {
            return;
        }

        if (btnVolverDesdeLogin == null)
        {
            Button[] botonesLogin = pantallaLogin.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < botonesLogin.Length; i++)
            {
                string nombreBoton = botonesLogin[i].name;
                if (nombreBoton.Equals("BtnVolverLogin", System.StringComparison.OrdinalIgnoreCase) ||
                    nombreBoton.Equals("Volver", System.StringComparison.OrdinalIgnoreCase))
                {
                    btnVolverDesdeLogin = botonesLogin[i];
                    break;
                }
            }
        }

        if (btnEntrarLogin == null)
        {
            Debug.LogWarning("START UI: no se pudo crear el botón de volver porque falta el botón de iniciar sesión.");
            return;
        }

        RectTransform rectEntrar = btnEntrarLogin.transform as RectTransform;
        RectTransform contenedorActual = btnEntrarLogin.transform.parent as RectTransform;
        if (rectEntrar == null || contenedorActual == null)
        {
            Debug.LogWarning("START UI: la estructura del botón de iniciar sesión no es válida.");
            return;
        }

        const float anchoFila = 430f;
        const float altoFila = 40f;
        const float separacion = 12f;

        RectTransform filaBotones;
        if (contenedorActual.name.Equals("LoginButtonRow", System.StringComparison.OrdinalIgnoreCase))
        {
            filaBotones = contenedorActual;
        }
        else
        {
            int indiceOriginal = btnEntrarLogin.transform.GetSiblingIndex();
            Transform filaExistente = contenedorActual.Find("LoginButtonRow");
            if (filaExistente != null)
            {
                filaBotones = filaExistente as RectTransform;
            }
            else
            {
                GameObject objetoFila = new GameObject(
                    "LoginButtonRow",
                    typeof(RectTransform),
                    typeof(HorizontalLayoutGroup),
                    typeof(LayoutElement)
                );
                filaBotones = objetoFila.GetComponent<RectTransform>();
                filaBotones.SetParent(contenedorActual, false);
                filaBotones.SetSiblingIndex(indiceOriginal);
            }

            btnEntrarLogin.transform.SetParent(filaBotones, false);
        }

        HorizontalLayoutGroup distribucion = filaBotones.GetComponent<HorizontalLayoutGroup>();
        if (distribucion == null)
        {
            distribucion = filaBotones.gameObject.AddComponent<HorizontalLayoutGroup>();
        }

        distribucion.padding = new RectOffset(0, 0, 0, 0);
        distribucion.spacing = separacion;
        distribucion.childAlignment = TextAnchor.MiddleCenter;
        distribucion.childControlWidth = true;
        distribucion.childControlHeight = true;
        distribucion.childForceExpandWidth = true;
        distribucion.childForceExpandHeight = true;

        LayoutElement layoutFila = filaBotones.GetComponent<LayoutElement>();
        if (layoutFila == null)
        {
            layoutFila = filaBotones.gameObject.AddComponent<LayoutElement>();
        }

        layoutFila.minWidth = anchoFila;
        layoutFila.preferredWidth = anchoFila;
        layoutFila.minHeight = altoFila;
        layoutFila.preferredHeight = altoFila;
        layoutFila.flexibleWidth = 0f;
        layoutFila.flexibleHeight = 0f;
        filaBotones.sizeDelta = new Vector2(anchoFila, altoFila);

        if (btnVolverDesdeLogin == null)
        {
            GameObject objetoVolver = Instantiate(btnEntrarLogin.gameObject, filaBotones, false);
            objetoVolver.name = "BtnVolverLogin";
            objetoVolver.SetActive(true);
            btnVolverDesdeLogin = objetoVolver.GetComponent<Button>();
        }
        else if (btnVolverDesdeLogin.transform.parent != filaBotones)
        {
            btnVolverDesdeLogin.transform.SetParent(filaBotones, false);
        }

        btnEntrarLogin.transform.SetSiblingIndex(0);
        btnVolverDesdeLogin.transform.SetSiblingIndex(1);
        btnVolverDesdeLogin.onClick.RemoveAllListeners();

        float anchoBoton = (anchoFila - separacion) * 0.5f;
        ConfigurarBotonDeFila(btnEntrarLogin, anchoBoton, altoFila);
        ConfigurarBotonDeFila(btnVolverDesdeLogin, anchoBoton, altoFila);

        Image fondoVolver = btnVolverDesdeLogin.GetComponent<Image>();
        if (fondoVolver != null)
        {
            fondoVolver.color = new Color(0.12f, 0.15f, 0.17f, 1f);
        }

        ColorBlock coloresVolver = btnVolverDesdeLogin.colors;
        coloresVolver.normalColor = Color.white;
        coloresVolver.highlightedColor = new Color(0.84f, 1f, 0.94f, 1f);
        coloresVolver.pressedColor = new Color(0.62f, 0.88f, 0.79f, 1f);
        coloresVolver.selectedColor = coloresVolver.highlightedColor;
        coloresVolver.disabledColor = new Color(0.55f, 0.58f, 0.60f, 0.55f);
        coloresVolver.colorMultiplier = 1f;
        btnVolverDesdeLogin.colors = coloresVolver;

        Outline bordeVolver = btnVolverDesdeLogin.GetComponent<Outline>();
        if (bordeVolver == null)
        {
            bordeVolver = btnVolverDesdeLogin.gameObject.AddComponent<Outline>();
        }

        bordeVolver.effectColor = new Color(0.11f, 0.72f, 0.54f, 0.7f);
        bordeVolver.effectDistance = new Vector2(1f, -1f);
        bordeVolver.useGraphicAlpha = true;

        TMP_Text textoVolver = btnVolverDesdeLogin.GetComponentInChildren<TMP_Text>(true);
        if (textoVolver != null)
        {
            textoVolver.text = "Volver";
            textoVolver.alignment = TextAlignmentOptions.Center;
        }
    }

    private void AsegurarInterfazSegundoFactor()
    {
        if (pantallaLogin == null || inputCorreo == null || btnEntrarLogin == null)
        {
            return;
        }

        Transform existente = pantallaLogin.transform.Find("PanelSegundoFactorSeguro");
        if (panelSegundoFactor == null && existente != null)
        {
            panelSegundoFactor = existente.gameObject;
        }

        if (panelSegundoFactor == null)
        {
            panelSegundoFactor = new GameObject(
                "PanelSegundoFactorSeguro",
                typeof(RectTransform),
                typeof(Image),
                typeof(Outline),
                typeof(CanvasGroup)
            );
            RectTransform panelRect = panelSegundoFactor.GetComponent<RectTransform>();
            panelRect.SetParent(pantallaLogin.transform, false);
            panelRect.anchorMin = new Vector2(0.04f, 0.05f);
            panelRect.anchorMax = new Vector2(0.96f, 0.95f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            panelRect.localScale = Vector3.one;

            Image fondo = panelSegundoFactor.GetComponent<Image>();
            fondo.color = new Color(0.018f, 0.055f, 0.052f, 0.985f);
            Outline borde = panelSegundoFactor.GetComponent<Outline>();
            borde.effectColor = new Color(0.12f, 1f, 0.68f, 0.9f);
            borde.effectDistance = new Vector2(2f, -2f);

            textoTituloSegundoFactor = CrearTextoSegundoFactor(
                "TituloSegundoFactor",
                "VERIFICACIÓN EN DOS PASOS",
                new Vector2(0.08f, 0.79f),
                new Vector2(0.92f, 0.94f),
                32f,
                new Color(0.43f, 1f, 0.78f, 1f)
            );
            textoDestinoSegundoFactor = CrearTextoSegundoFactor(
                "DestinoSegundoFactor",
                "Enviamos un código de 6 dígitos.",
                new Vector2(0.08f, 0.66f),
                new Vector2(0.92f, 0.79f),
                20f,
                new Color(0.82f, 0.92f, 0.9f, 1f)
            );

            GameObject codigoObjeto = Instantiate(inputCorreo.gameObject, panelSegundoFactor.transform, false);
            codigoObjeto.name = "InputCodigoSegundoFactor";
            codigoObjeto.SetActive(true);
            inputCodigoSegundoFactor = codigoObjeto.GetComponent<TMP_InputField>();
            RectTransform codigoRect = codigoObjeto.transform as RectTransform;
            ConfigurarRectAnclado(
                codigoRect,
                new Vector2(0.18f, 0.43f),
                new Vector2(0.82f, 0.64f)
            );
            inputCodigoSegundoFactor.onValueChanged.RemoveAllListeners();
            inputCodigoSegundoFactor.onEndEdit.RemoveAllListeners();
            inputCodigoSegundoFactor.text = string.Empty;
            inputCodigoSegundoFactor.characterLimit = 6;
            inputCodigoSegundoFactor.contentType = TMP_InputField.ContentType.IntegerNumber;
            inputCodigoSegundoFactor.lineType = TMP_InputField.LineType.SingleLine;
            if (inputCodigoSegundoFactor.textComponent != null)
            {
                inputCodigoSegundoFactor.textComponent.alignment = TextAlignmentOptions.Center;
                inputCodigoSegundoFactor.textComponent.fontSize = 42f;
                inputCodigoSegundoFactor.textComponent.characterSpacing = 20f;
                inputCodigoSegundoFactor.textComponent.color = Color.white;
            }
            TMP_Text placeholder = inputCodigoSegundoFactor.placeholder as TMP_Text;
            if (placeholder != null)
            {
                placeholder.text = "•  •  •  •  •  •";
                placeholder.alignment = TextAlignmentOptions.Center;
            }
            Outline bordeCodigo = codigoObjeto.GetComponent<Outline>();
            if (bordeCodigo == null)
                bordeCodigo = codigoObjeto.AddComponent<Outline>();
            bordeCodigo.effectColor = new Color(0.18f, 1f, 0.68f, 0.85f);
            bordeCodigo.effectDistance = new Vector2(2f, -2f);

            textoTiempoSegundoFactor = CrearTextoSegundoFactor(
                "TiempoSegundoFactor",
                "Código vigente  05:00",
                new Vector2(0.1f, 0.34f),
                new Vector2(0.9f, 0.43f),
                17f,
                new Color(0.48f, 1f, 0.78f, 1f)
            );
            textoEstadoSegundoFactor = CrearTextoSegundoFactor(
                "EstadoSegundoFactor",
                string.Empty,
                new Vector2(0.08f, 0.25f),
                new Vector2(0.92f, 0.35f),
                16f,
                Color.white
            );

            btnVerificarSegundoFactor = CrearBotonSegundoFactor(
                "BtnVerificarSegundoFactor",
                "Verificar y entrar",
                new Vector2(0.08f, 0.08f),
                new Vector2(0.42f, 0.22f),
                new Color(0.09f, 0.72f, 0.49f, 1f)
            );
            btnReenviarSegundoFactor = CrearBotonSegundoFactor(
                "BtnReenviarSegundoFactor",
                "Reenviar código",
                new Vector2(0.44f, 0.08f),
                new Vector2(0.7f, 0.22f),
                new Color(0.09f, 0.27f, 0.24f, 1f)
            );
            btnVolverSegundoFactor = CrearBotonSegundoFactor(
                "BtnVolverSegundoFactor",
                "Volver",
                new Vector2(0.72f, 0.08f),
                new Vector2(0.92f, 0.22f),
                new Color(0.14f, 0.17f, 0.18f, 1f)
            );
        }

        if (inputCodigoSegundoFactor != null)
        {
            inputCodigoSegundoFactor.onValueChanged.RemoveListener(FiltrarCodigoSegundoFactor);
            inputCodigoSegundoFactor.onValueChanged.AddListener(FiltrarCodigoSegundoFactor);
        }
        panelSegundoFactor.transform.SetAsLastSibling();
        panelSegundoFactor.SetActive(false);

        AlgoLabVRInputFieldKeyboard[] teclados = FindObjectsByType<AlgoLabVRInputFieldKeyboard>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );
        for (int i = 0; i < teclados.Length; i++)
            teclados[i].ActualizarListaInputs();
    }

    private TMP_Text CrearTextoSegundoFactor(
        string nombre,
        string contenido,
        Vector2 anclaMin,
        Vector2 anclaMax,
        float tamano,
        Color color
    )
    {
        GameObject objeto = new GameObject(nombre, typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform rect = objeto.GetComponent<RectTransform>();
        rect.SetParent(panelSegundoFactor.transform, false);
        ConfigurarRectAnclado(rect, anclaMin, anclaMax);
        TMP_Text texto = objeto.GetComponent<TMP_Text>();
        if (textoMensajeLogin != null && textoMensajeLogin.font != null)
            texto.font = textoMensajeLogin.font;
        texto.text = contenido;
        texto.fontSize = tamano;
        texto.color = color;
        texto.alignment = TextAlignmentOptions.Center;
        texto.textWrappingMode = TextWrappingModes.Normal;
        texto.raycastTarget = false;
        return texto;
    }

    private Button CrearBotonSegundoFactor(
        string nombre,
        string etiqueta,
        Vector2 anclaMin,
        Vector2 anclaMax,
        Color color
    )
    {
        GameObject objeto = Instantiate(btnEntrarLogin.gameObject, panelSegundoFactor.transform, false);
        objeto.name = nombre;
        objeto.SetActive(true);
        RectTransform rect = objeto.transform as RectTransform;
        ConfigurarRectAnclado(rect, anclaMin, anclaMax);
        Button boton = objeto.GetComponent<Button>();
        boton.onClick.RemoveAllListeners();
        Image imagen = objeto.GetComponent<Image>();
        if (imagen != null)
            imagen.color = color;
        TMP_Text texto = objeto.GetComponentInChildren<TMP_Text>(true);
        if (texto != null)
        {
            texto.text = etiqueta;
            texto.alignment = TextAlignmentOptions.Center;
            texto.fontSize = Mathf.Min(texto.fontSize, 17f);
        }
        return boton;
    }

    private static void ConfigurarRectAnclado(RectTransform rect, Vector2 anclaMin, Vector2 anclaMax)
    {
        if (rect == null)
            return;
        rect.anchorMin = anclaMin;
        rect.anchorMax = anclaMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
        rect.localScale = Vector3.one;
        LayoutElement layout = rect.GetComponent<LayoutElement>();
        if (layout != null)
            layout.ignoreLayout = true;
    }

    private void FiltrarCodigoSegundoFactor(string valor)
    {
        if (inputCodigoSegundoFactor == null)
            return;
        System.Text.StringBuilder limpio = new System.Text.StringBuilder(6);
        for (int i = 0; i < valor.Length && limpio.Length < 6; i++)
        {
            if (char.IsDigit(valor[i]))
                limpio.Append(valor[i]);
        }
        string resultado = limpio.ToString();
        if (!string.Equals(valor, resultado, System.StringComparison.Ordinal))
            inputCodigoSegundoFactor.SetTextWithoutNotify(resultado);
    }

    private static void ConfigurarBotonDeFila(Button boton, float ancho, float alto)
    {
        if (boton == null)
        {
            return;
        }

        RectTransform rect = boton.transform as RectTransform;
        if (rect != null)
        {
            rect.localScale = Vector3.one;
            rect.anchoredPosition = Vector2.zero;
        }

        LayoutElement layout = boton.GetComponent<LayoutElement>();
        if (layout == null)
        {
            layout = boton.gameObject.AddComponent<LayoutElement>();
        }

        layout.ignoreLayout = false;
        layout.minWidth = 0f;
        layout.preferredWidth = ancho;
        layout.flexibleWidth = 1f;
        layout.minHeight = alto;
        layout.preferredHeight = alto;
        layout.flexibleHeight = 0f;
    }

    private void PrepararPantallasParaTransicion()
    {
        PrepararPantalla(pantallaBienvenida);
        PrepararPantalla(pantallaSeleccionModo);
        PrepararPantalla(pantallaLogin);
        PrepararPantalla(pantallaAvisoInvitado);
    }

    private void PrepararPantalla(GameObject pantalla)
    {
        if (pantalla == null)
        {
            return;
        }

        CanvasGroup canvasGroup = ObtenerCanvasGroup(pantalla);
        canvasGroup.alpha = pantalla.activeSelf ? 1f : 0f;

        AplicarInteraccionPantalla(pantalla, pantalla.activeSelf);
    }

    private void ConectarBotones()
    {
        if (btnJugar != null)
        {
            btnJugar.onClick.RemoveListener(MostrarPantallaSeleccionModo);
            btnJugar.onClick.AddListener(MostrarPantallaSeleccionModo);
        }

        if (btnIrLogin != null)
        {
            btnIrLogin.onClick.RemoveListener(MostrarPantallaLogin);
            btnIrLogin.onClick.AddListener(MostrarPantallaLogin);
        }

        if (btnJugarInvitado != null)
        {
            btnJugarInvitado.onClick.RemoveListener(MostrarPantallaAvisoInvitado);
            btnJugarInvitado.onClick.AddListener(MostrarPantallaAvisoInvitado);
        }

        if (btnEntrarLogin != null)
        {
            btnEntrarLogin.onClick.RemoveListener(IniciarSesionDesdeUI);
            btnEntrarLogin.onClick.AddListener(IniciarSesionDesdeUI);
        }

        if (btnVolverDesdeLogin != null)
        {
            btnVolverDesdeLogin.onClick.RemoveListener(VolverDesdeLoginAlInicio);
            btnVolverDesdeLogin.onClick.AddListener(VolverDesdeLoginAlInicio);
        }

        if (btnConfirmarInvitado != null)
        {
            btnConfirmarInvitado.onClick.RemoveListener(EntrarComoInvitado);
            btnConfirmarInvitado.onClick.AddListener(EntrarComoInvitado);
        }

        if (btnCancelarInvitado != null)
        {
            btnCancelarInvitado.onClick.RemoveListener(CancelarInvitadoYVolverInicio);
            btnCancelarInvitado.onClick.AddListener(CancelarInvitadoYVolverInicio);
        }

        if (btnCerrarSesion != null)
        {
            btnCerrarSesion.onClick.RemoveListener(CerrarSesionYVolverInicio);
            btnCerrarSesion.onClick.AddListener(CerrarSesionYVolverInicio);
        }

        if (btnVerificarSegundoFactor != null)
        {
            btnVerificarSegundoFactor.onClick.RemoveListener(VerificarSegundoFactorDesdeUI);
            btnVerificarSegundoFactor.onClick.AddListener(VerificarSegundoFactorDesdeUI);
        }

        if (btnReenviarSegundoFactor != null)
        {
            btnReenviarSegundoFactor.onClick.RemoveListener(ReenviarSegundoFactorDesdeUI);
            btnReenviarSegundoFactor.onClick.AddListener(ReenviarSegundoFactorDesdeUI);
        }

        if (btnVolverSegundoFactor != null)
        {
            btnVolverSegundoFactor.onClick.RemoveListener(VolverDesdeSegundoFactor);
            btnVolverSegundoFactor.onClick.AddListener(VolverDesdeSegundoFactor);
        }
    }

    private void ConfigurarTextosIniciales()
    {
        if (textoNombreApp != null)
        {
            textoNombreApp.text = nombreApp;
        }

        if (textoMensajeLogin != null)
        {
            textoMensajeLogin.text = "";
        }

        if (textoMensajeInvitado != null)
        {
            textoMensajeInvitado.text = mensajeInvitado;
        }
    }

    public void MostrarPantallaBienvenida()
    {
        AlgoLabRoomScanGuard.NotificarRetornoAlInicio();
        CambiarPantalla(pantallaBienvenida);
        LimpiarMensajeLogin();

        DebugLog("START UI: pantalla bienvenida.");
    }

    public void MostrarPantallaSeleccionModo()
    {
        AlgoLabRoomScanGuard.NotificarJugarPresionado();
        CambiarPantalla(pantallaSeleccionModo);
        LimpiarMensajeLogin();

        DebugLog("START UI: pantalla selección de modo.");
    }

    public void MostrarPantallaLogin()
    {
        CambiarPantalla(pantallaLogin);
        LimpiarMensajeLogin();

        if (!esperandoSegundoFactor)
        {
            MostrarElementosCredenciales(true);
            if (panelSegundoFactor != null)
                panelSegundoFactor.SetActive(false);
        }

        if (inputCorreo != null)
        {
            inputCorreo.Select();
        }

        DebugLog("START UI: pantalla login.");
    }

    public void MostrarPantallaAvisoInvitado()
    {
        CambiarPantalla(pantallaAvisoInvitado);

        if (textoInvitadoTypewriter != null)
        {
            textoInvitadoTypewriter.Reproducir(mensajeInvitado);
        }
        else if (textoMensajeInvitado != null)
        {
            textoMensajeInvitado.text = mensajeInvitado;
        }

        DebugLog("START UI: pantalla aviso invitado.");
    }

    public void CancelarInvitadoYVolverInicio()
    {
        MostrarPantallaBienvenida();

        DebugLog("START UI: invitado cancelado. Volviendo a pantalla principal.");
    }

    public void VolverDesdeLoginAlInicio()
    {
        generacionOperacion++;

        if (procesandoLogin && backendClient != null)
        {
            backendClient.CancelarInicioSesionPendiente();
        }

        procesandoLogin = false;
        ReiniciarSegundoFactor(false);
        ActivarInteractableLogin(true);
        LimpiarCamposLogin();
        MostrarPantallaBienvenida();

        DebugLog("START UI: login cancelado. Volviendo a pantalla principal.");
    }

    private void CambiarPantalla(GameObject pantallaActiva)
    {
        BloquearInteraccionDeTodasLasPantallas();

        SetPantalla(pantallaBienvenida, pantallaBienvenida == pantallaActiva);
        SetPantalla(pantallaSeleccionModo, pantallaSeleccionModo == pantallaActiva);
        SetPantalla(pantallaLogin, pantallaLogin == pantallaActiva);
        SetPantalla(pantallaAvisoInvitado, pantallaAvisoInvitado == pantallaActiva);
    }

    public void IniciarSesionDesdeUI()
    {
        if (procesandoLogin)
        {
            return;
        }

        BuscarReferencias();

        if (backendClient == null)
        {
            MostrarErrorLogin("No se encontró el cliente del backend.");
            return;
        }

        string correo = inputCorreo != null ? inputCorreo.text : "";
        string contrasena = inputContrasena != null ? inputContrasena.text : "";

        if (puedeReintentarValidacionSesionGuardada &&
            sessionManager != null &&
            sessionManager.EstaAutenticado &&
            string.IsNullOrWhiteSpace(correo) &&
            string.IsNullOrWhiteSpace(contrasena))
        {
            ValidarSesionGuardadaAntesDeEntrar();
            return;
        }

        if (string.IsNullOrWhiteSpace(correo))
        {
            MostrarErrorLogin("Escribe tu correo.");
            return;
        }

        if (string.IsNullOrWhiteSpace(contrasena))
        {
            MostrarErrorLogin("Escribe tu contraseña.");
            return;
        }

        correo = correo.Trim().ToLowerInvariant();
        if (!correo.Contains("@") || !correo.Contains(".") || correo.IndexOf("@", System.StringComparison.Ordinal) < 1)
        {
            MostrarErrorLogin("Escribe un correo electrónico válido (ejemplo: usuario@dominio.com).");
            return;
        }

        procesandoLogin = true;
        int operacion = ++generacionOperacion;
        ActivarInteractableLogin(false);
        MostrarMensajeLogin("Verificando credenciales...");

        backendClient.IniciarSesion(
            correo,
            contrasena,
            (ok, mensaje, respuesta) =>
            {
                if (operacion != generacionOperacion || !isActiveAndEnabled)
                {
                    return;
                }

                if (!ok)
                {
                    procesandoLogin = false;
                    ActivarInteractableLogin(true);
                    MostrarErrorLogin(mensaje);
                    return;
                }

                esperandoSegundoFactor = false;
                desafioSegundoFactorId = string.Empty;
                if (panelSegundoFactor != null)
                {
                    panelSegundoFactor.SetActive(false);
                }

                MostrarMensajeLogin(
                    string.IsNullOrWhiteSpace(mensaje)
                        ? "Inicio de sesión correcto."
                        : mensaje
                );

                if (consultarProgresoDespuesDeLogin)
                {
                    ConsultarProgresoYEntrar(operacion);
                }
                else
                {
                    FinalizarEntradaPorLogin(operacion);
                }
            }
        );
    }

    private void MostrarDesafioSegundoFactor(
        AlgoLabBackendClient.DesafioSegundoFactorResponse desafio,
        string mensaje
    )
    {
        AsegurarInterfazSegundoFactor();
        if (desafio == null || string.IsNullOrWhiteSpace(desafio.desafioId) || panelSegundoFactor == null)
        {
            MostrarErrorLogin("No se pudo abrir la verificación de seguridad. Inténtalo de nuevo.");
            ActivarInteractableLogin(true);
            return;
        }

        esperandoSegundoFactor = true;
        desafioSegundoFactorId = desafio.desafioId;
        segundoFactorExpiraEn = Time.unscaledTime + Mathf.Max(1, desafio.expiraEnSegundos);
        segundoFactorReenvioEn = Time.unscaledTime + Mathf.Max(0, desafio.reenvioDisponibleEnSegundos);
        MostrarElementosCredenciales(false);
        panelSegundoFactor.SetActive(true);
        panelSegundoFactor.transform.SetAsLastSibling();

        string canal = string.Equals(desafio.canal, "SMS", System.StringComparison.OrdinalIgnoreCase)
            ? "mensaje de texto"
            : "correo electrónico";
        if (textoDestinoSegundoFactor != null)
        {
            textoDestinoSegundoFactor.text =
                "Enviamos 6 dígitos por " + canal + " a\n" +
                (string.IsNullOrWhiteSpace(desafio.destinoEnmascarado)
                    ? "tu destino verificado"
                    : desafio.destinoEnmascarado);
        }
        if (textoEstadoSegundoFactor != null)
        {
            textoEstadoSegundoFactor.color = new Color(0.75f, 0.92f, 0.87f, 1f);
            textoEstadoSegundoFactor.text = string.IsNullOrWhiteSpace(mensaje)
                ? "Escribe el código para abrir tu sesión."
                : mensaje;
        }
        if (inputCodigoSegundoFactor != null)
        {
            inputCodigoSegundoFactor.SetTextWithoutNotify(string.Empty);
            inputCodigoSegundoFactor.interactable = true;
            inputCodigoSegundoFactor.Select();
        }
        ActivarInteractableSegundoFactor(true);

        if (rutinaAnimacionSegundoFactor != null)
            StopCoroutine(rutinaAnimacionSegundoFactor);
        rutinaAnimacionSegundoFactor = StartCoroutine(AnimarEntradaSegundoFactor());

        AlgoLabVRInputFieldKeyboard[] teclados = FindObjectsByType<AlgoLabVRInputFieldKeyboard>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );
        for (int i = 0; i < teclados.Length; i++)
            teclados[i].ActualizarListaInputs();
    }

    public void VerificarSegundoFactorDesdeUI()
    {
        if (procesandoLogin || !esperandoSegundoFactor || backendClient == null)
            return;

        if (Time.unscaledTime >= segundoFactorExpiraEn)
        {
            MostrarErrorSegundoFactor("El código venció. Pulsa Reenviar código.");
            return;
        }

        string codigo = inputCodigoSegundoFactor != null
            ? inputCodigoSegundoFactor.text.Trim()
            : string.Empty;
        if (codigo.Length != 6)
        {
            MostrarErrorSegundoFactor("Escribe los 6 dígitos del código.");
            return;
        }

        procesandoLogin = true;
        int operacion = ++generacionOperacion;
        ActivarInteractableSegundoFactor(false);
        MostrarEstadoSegundoFactor("Comprobando el código de forma segura...", false);
        backendClient.VerificarSegundoFactor(
            desafioSegundoFactorId,
            codigo,
            (ok, mensaje, respuesta) =>
            {
                if (operacion != generacionOperacion || !isActiveAndEnabled)
                    return;

                if (!ok)
                {
                    procesandoLogin = false;
                    ActivarInteractableSegundoFactor(true);
                    MostrarErrorSegundoFactor(mensaje);
                    if (inputCodigoSegundoFactor != null)
                    {
                        inputCodigoSegundoFactor.SetTextWithoutNotify(string.Empty);
                        inputCodigoSegundoFactor.Select();
                    }
                    return;
                }

                esperandoSegundoFactor = false;
                desafioSegundoFactorId = string.Empty;
                if (panelSegundoFactor != null)
                    panelSegundoFactor.SetActive(false);
                MostrarMensajeLogin("Sesión protegida iniciada correctamente.");

                if (consultarProgresoDespuesDeLogin)
                    ConsultarProgresoYEntrar(operacion);
                else
                    FinalizarEntradaPorLogin(operacion);
            }
        );
    }

    public void ReenviarSegundoFactorDesdeUI()
    {
        if (procesandoLogin ||
            !esperandoSegundoFactor ||
            backendClient == null ||
            Time.unscaledTime < segundoFactorReenvioEn)
        {
            return;
        }

        procesandoLogin = true;
        int operacion = ++generacionOperacion;
        ActivarInteractableSegundoFactor(false);
        MostrarEstadoSegundoFactor("Enviando un código nuevo...", false);
        backendClient.ReenviarSegundoFactor(
            desafioSegundoFactorId,
            (ok, mensaje, desafio) =>
            {
                if (operacion != generacionOperacion || !isActiveAndEnabled)
                    return;
                procesandoLogin = false;
                if (!ok)
                {
                    ActivarInteractableSegundoFactor(true);
                    MostrarErrorSegundoFactor(mensaje);
                    return;
                }
                MostrarDesafioSegundoFactor(desafio, "Código renovado. Usa únicamente el más reciente.");
            }
        );
    }

    public void VolverDesdeSegundoFactor()
    {
        generacionOperacion++;
        backendClient?.CancelarInicioSesionPendiente();
        procesandoLogin = false;
        ReiniciarSegundoFactor(true);
        if (inputContrasena != null)
            inputContrasena.text = string.Empty;
        ActivarInteractableLogin(true);
        MostrarMensajeLogin("Verificación cancelada. Puedes iniciar de nuevo.");
    }

    private void ReiniciarSegundoFactor(bool mostrarCredenciales)
    {
        esperandoSegundoFactor = false;
        desafioSegundoFactorId = string.Empty;
        segundoFactorExpiraEn = 0f;
        segundoFactorReenvioEn = 0f;
        if (rutinaAnimacionSegundoFactor != null)
        {
            StopCoroutine(rutinaAnimacionSegundoFactor);
            rutinaAnimacionSegundoFactor = null;
        }
        if (inputCodigoSegundoFactor != null)
            inputCodigoSegundoFactor.SetTextWithoutNotify(string.Empty);
        if (panelSegundoFactor != null)
            panelSegundoFactor.SetActive(false);
        MostrarElementosCredenciales(mostrarCredenciales);
    }

    private void MostrarElementosCredenciales(bool mostrar)
    {
        if (inputCorreo != null)
            inputCorreo.gameObject.SetActive(mostrar);
        if (inputContrasena != null)
            inputContrasena.gameObject.SetActive(mostrar);
        if (btnEntrarLogin != null && btnEntrarLogin.transform.parent != null)
            btnEntrarLogin.transform.parent.gameObject.SetActive(mostrar);
        else if (btnEntrarLogin != null)
            btnEntrarLogin.gameObject.SetActive(mostrar);
    }

    private void ActivarInteractableSegundoFactor(bool activo)
    {
        if (inputCodigoSegundoFactor != null)
            inputCodigoSegundoFactor.interactable = activo;
        if (btnVerificarSegundoFactor != null)
            btnVerificarSegundoFactor.interactable = activo;
        if (btnVolverSegundoFactor != null)
            btnVolverSegundoFactor.interactable = activo;
        if (btnReenviarSegundoFactor != null)
            btnReenviarSegundoFactor.interactable = activo && Time.unscaledTime >= segundoFactorReenvioEn;
    }

    private void MostrarEstadoSegundoFactor(string mensaje, bool error)
    {
        if (textoEstadoSegundoFactor == null)
            return;
        textoEstadoSegundoFactor.text = mensaje;
        textoEstadoSegundoFactor.color = error
            ? new Color(1f, 0.35f, 0.32f, 1f)
            : new Color(0.75f, 0.92f, 0.87f, 1f);
    }

    private void MostrarErrorSegundoFactor(string mensaje)
    {
        MostrarEstadoSegundoFactor(mensaje, true);
        Debug.LogWarning("START UI 2FA: " + mensaje);
    }

    private IEnumerator AnimarEntradaSegundoFactor()
    {
        if (panelSegundoFactor == null)
            yield break;
        CanvasGroup grupo = panelSegundoFactor.GetComponent<CanvasGroup>();
        RectTransform rect = panelSegundoFactor.transform as RectTransform;
        float inicio = Time.unscaledTime;
        const float duracion = 0.45f;
        while (Time.unscaledTime - inicio < duracion)
        {
            float t = Mathf.Clamp01((Time.unscaledTime - inicio) / duracion);
            float suave = t * t * (3f - 2f * t);
            if (grupo != null)
                grupo.alpha = suave;
            if (rect != null)
                rect.localScale = Vector3.one * Mathf.Lerp(0.82f, 1f, suave);
            yield return null;
        }
        if (grupo != null)
            grupo.alpha = 1f;
        if (rect != null)
            rect.localScale = Vector3.one;

        while (esperandoSegundoFactor && panelSegundoFactor.activeInHierarchy)
        {
            if (textoTituloSegundoFactor != null)
            {
                float pulso = 1f + Mathf.Sin(Time.unscaledTime * 3.2f) * 0.012f;
                textoTituloSegundoFactor.rectTransform.localScale = Vector3.one * pulso;
            }
            yield return null;
        }
        rutinaAnimacionSegundoFactor = null;
    }

    private void ValidarSesionGuardadaAntesDeEntrar()
    {
        if (procesandoLogin)
        {
            return;
        }

        BuscarReferencias();
        puedeReintentarValidacionSesionGuardada = false;

        // La validación siempre se muestra en una pantalla interactiva conocida;
        // nunca se habilitan paneles del juego mientras el token no esté aprobado.
        MostrarPantallaLogin();

        if (backendClient == null)
        {
            puedeReintentarValidacionSesionGuardada = true;
            MostrarErrorLogin(
                "No se pudo validar la sesión guardada. Revisa la conexión y pulsa Entrar para reintentar."
            );
            return;
        }

        procesandoLogin = true;
        int operacion = ++generacionOperacion;
        ActivarInteractableLogin(false);
        MostrarMensajeLogin("Validando sesión guardada...");

        backendClient.ValidarSesionActual((estado, mensaje, usuario) =>
        {
            if (operacion != generacionOperacion || !isActiveAndEnabled)
            {
                return;
            }

            procesandoLogin = false;
            ActivarInteractableLogin(true);

            if (estado == AlgoLabBackendClient.EstadoValidacionSesion.Valida)
            {
                FinalizarEntradaPorSesionGuardada();
                return;
            }

            if (estado == AlgoLabBackendClient.EstadoValidacionSesion.CredencialesInvalidas ||
                estado == AlgoLabBackendClient.EstadoValidacionSesion.SinSesion)
            {
                puedeReintentarValidacionSesionGuardada = false;
                sessionManager?.CerrarSesion();
                MostrarPantallaLogin();
                MostrarErrorLogin(
                    "Tu sesión guardada venció o ya no es válida. Inicia sesión nuevamente."
                );
                return;
            }

            // Una caída de red, timeout o respuesta temporalmente inválida no
            // invalida el token local. El botón Entrar reintenta si los campos
            // siguen vacíos y también permite escribir credenciales nuevas.
            puedeReintentarValidacionSesionGuardada = true;
            MostrarPantallaLogin();
            MostrarErrorLogin(
                "No pudimos validar tu sesión por un problema de conexión. " +
                "Pulsa Entrar para reintentar o escribe tus credenciales."
            );
            Debug.LogWarning("START UI: validación temporal fallida. " + mensaje);
        });
    }

    private void FinalizarEntradaPorSesionGuardada()
    {
        puedeReintentarValidacionSesionGuardada = false;
        AlgoLabRoomScanGuard.NotificarJugarPresionado();
        OcultarTodasLasPantallasInmediato();
        OnAccesoPermitido?.Invoke();

        // Una sesión recordada no significa que el tutorial se haya terminado.
        // La decisión pertenece al hito sincronizado de la cuenta.
        if (DebeMostrarTutorialBienvenida())
        {
            ProgramarTutorialBienvenida();
        }
        else
        {
            AsegurarPanelesCompletosSinTutorial();
        }

        DebugLog("START UI: sesión guardada validada; acceso permitido.");
    }

    /// <summary>
    /// Expulsa de forma segura una sesión que el backend acaba de declarar
    /// vencida o revocada, sin intentar guardar otra vez con ese mismo token.
    /// Los fallos temporales de red nunca llaman este método.
    /// </summary>
    public void ManejarSesionRemotaInvalida(string detalle = null)
    {
        generacionOperacion++;
        procesandoLogin = false;
        cerrandoSesion = false;
        BuscarReferencias();

        if (rutinaTutorialBienvenida != null)
        {
            StopCoroutine(rutinaTutorialBienvenida);
            rutinaTutorialBienvenida = null;
        }

        ReiniciarEstadoDelJuego();
        sessionManager?.CerrarSesion();
        PrepararRetornoAlMenuPrincipal();
        LimpiarCamposLogin();
        OnCerrarSesion?.Invoke();
        MostrarPantallaLogin();
        ActivarInteractableLogin(true);
        MostrarErrorLogin("Tu sesión venció o fue cerrada. Inicia sesión nuevamente.");
        Debug.LogWarning("START UI: sesión remota inválida. " + (detalle ?? string.Empty));
    }

    private void ConsultarProgresoYEntrar(int operacion)
    {
        if (backendClient == null)
        {
            FinalizarEntradaPorLogin(operacion);
            return;
        }

        MostrarMensajeLogin("Cargando progreso...");

        backendClient.ConsultarProgreso((ok, mensaje, progreso) =>
        {
            if (operacion != generacionOperacion || !isActiveAndEnabled)
            {
                return;
            }

            if (!ok)
            {
                Debug.LogWarning(
                    "START UI: no se pudo consultar progreso. Se entra igual. Detalle: " +
                    mensaje
                );
            }

            FinalizarEntradaPorLogin(operacion);
        });
    }

    private void FinalizarEntradaPorLogin(int operacion)
    {
        if (operacion != generacionOperacion || !isActiveAndEnabled)
        {
            return;
        }

        procesandoLogin = false;
        ActivarInteractableLogin(true);

        if (limpiarCamposDespuesDeLogin)
        {
            LimpiarCamposLogin();
        }

        OcultarTodasLasPantallas();

        OnLoginCorrecto?.Invoke();
        OnAccesoPermitido?.Invoke();

        try
        {
            ProgressPanelSessionBinder binder = FindFirstObjectByType<ProgressPanelSessionBinder>(
                FindObjectsInactive.Include
            );
            binder?.ActualizarDesdeSesion(true);

            AlgoLabProgressPanel progressPanel = FindFirstObjectByType<AlgoLabProgressPanel>(
                FindObjectsInactive.Include
            );
            progressPanel?.ActualizarTodo();
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("START UI: error sincronizando panel tras login: " + ex.Message);
        }

        if (mostrarTutorialDespuesDeEntrar && DebeMostrarTutorialBienvenida())
        {
            ProgramarTutorialBienvenida();
        }
        else
        {
            AsegurarPanelesCompletosSinTutorial();
        }

        DebugLog("START UI: acceso permitido por login.");
    }

    public void EntrarComoInvitado()
    {
        generacionOperacion++;
        BuscarReferencias();

        if (sessionManager == null)
        {
            Debug.LogError("START UI: no se encontró AlgoLabSessionManager.");
            return;
        }

        sessionManager.IniciarComoInvitado();

        OcultarTodasLasPantallas();

        OnInvitadoCorrecto?.Invoke();
        OnAccesoPermitido?.Invoke();

        try
        {
            ProgressPanelSessionBinder binder = FindFirstObjectByType<ProgressPanelSessionBinder>(
                FindObjectsInactive.Include
            );
            binder?.ActualizarDesdeSesion(true);

            AlgoLabProgressPanel progressPanel = FindFirstObjectByType<AlgoLabProgressPanel>(
                FindObjectsInactive.Include
            );
            progressPanel?.ActualizarTodo();
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("START UI: error sincronizando panel como invitado: " + ex.Message);
        }

        if (mostrarTutorialDespuesDeEntrar)
        {
            ProgramarTutorialBienvenida();
        }
        else
        {
            AsegurarPanelesCompletosSinTutorial();
        }

        DebugLog("START UI: acceso permitido como invitado. No se guardará progreso.");
    }

    private void ProgramarTutorialBienvenida()
    {
        if (!mostrarTutorialDespuesDeEntrar)
        {
            AsegurarPanelesCompletosSinTutorial();
            return;
        }

        BuscarReferencias();

        if (tutorialBienvenida == null)
        {
            DebugLog("START UI: no hay tutorial de bienvenida asignado.");
            AsegurarPanelesCompletosSinTutorial();
            return;
        }

        if (!DebeMostrarTutorialBienvenida())
        {
            DebugLog("START UI: tutorial ya completado/omitido para esta cuenta.");
            AsegurarPanelesCompletosSinTutorial();
            return;
        }

        AlgoLabGameAccessController acceso = FindFirstObjectByType<AlgoLabGameAccessController>(
            FindObjectsInactive.Include
        );
        acceso?.PrepararPanelesParaTutorial();

        if (rutinaTutorialBienvenida != null)
        {
            StopCoroutine(rutinaTutorialBienvenida);
        }

        rutinaTutorialBienvenida = StartCoroutine(
            MostrarTutorialBienvenidaDespuesDeTiempo()
        );
    }

    private bool DebeMostrarTutorialBienvenida()
    {
        BuscarReferencias();
        return tutorialBienvenida == null || tutorialBienvenida.DebeMostrarTutorialParaSesionActual();
    }

    private void AsegurarPanelesCompletosSinTutorial()
    {
        AlgoLabGameAccessController acceso = FindFirstObjectByType<AlgoLabGameAccessController>(
            FindObjectsInactive.Include
        );
        acceso?.ActivarPanelesDespuesDelTutorial();
    }

    private IEnumerator MostrarTutorialBienvenidaDespuesDeTiempo()
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, retrasoTutorialDespuesDeEntrar));

        if (tutorialBienvenida != null)
        {
            tutorialBienvenida.PrepararInicioAutomaticoExterno();
            tutorialBienvenida.IniciarTutorial();
            DebugLog("START UI: tutorial de bienvenida iniciado.");
        }

        rutinaTutorialBienvenida = null;
    }

    public void CerrarSesionYVolverInicio()
    {
        if (cerrandoSesion)
        {
            return;
        }

        cerrandoSesion = true;
        generacionOperacion++;
        ActivarJerarquia(transform);
        BuscarReferencias();

        if (rutinaTutorialBienvenida != null)
        {
            StopCoroutine(rutinaTutorialBienvenida);
            rutinaTutorialBienvenida = null;
        }

        StartCoroutine(CerrarSesionGuardandoYReiniciando());

        DebugLog("START UI: sesión cerrada y vuelta al inicio.");
    }

    private IEnumerator CerrarSesionGuardandoYReiniciando()
    {
        bool respuestaRecibida = false;
        ReiniciarEstadoDelJuego();

        if (progressSaver != null &&
            sessionManager != null &&
            sessionManager.PuedeGuardarProgreso)
        {
            progressSaver.GuardarProgresoAntesDeCerrarSesion(_ =>
            {
                respuestaRecibida = true;
            });

            float limite = 3f;
            while (!respuestaRecibida && limite > 0f)
            {
                limite -= Time.unscaledDeltaTime;
                yield return null;
            }

            if (!respuestaRecibida)
            {
                Debug.LogWarning("START UI: se agoto el tiempo de guardado; se continuara con el cierre local.");
            }
        }

        if (sessionManager != null)
        {
            sessionManager.CerrarSesion();
        }

        PrepararRetornoAlMenuPrincipal();
        LimpiarCamposLogin();
        LimpiarMensajeLogin();
        OnCerrarSesion?.Invoke();
        MostrarPantallaBienvenida();
        cerrandoSesion = false;
        DebugLog("START UI: sesion cerrada de forma segura y vuelta al inicio.");
    }

    private void PrepararRetornoAlMenuPrincipal()
    {
        AlgoLabSettingsMenuController configuracion = AlgoLabSettingsMenuController.Instance;
        if (configuracion == null)
        {
            configuracion = FindFirstObjectByType<AlgoLabSettingsMenuController>(
                FindObjectsInactive.Include
            );
        }
        configuracion?.CerrarConfiguracion();

        AlgoLabPanelPocketManager panelOpciones = AlgoLabPanelPocketManager.Instance;
        if (panelOpciones == null)
        {
            panelOpciones = FindFirstObjectByType<AlgoLabPanelPocketManager>(
                FindObjectsInactive.Include
            );
        }

        if (panelOpciones != null)
        {
            panelOpciones.SetInterfazModalActiva(false);
            panelOpciones.DeshabilitarPanelOpciones();
        }

        AlgoLabGameAccessController acceso = FindFirstObjectByType<AlgoLabGameAccessController>(
            FindObjectsInactive.Include
        );
        acceso?.BloquearAccesoJuego();

        // El controlador vive fuera de [LOGIN_UI]. Activar solo su jerarquia no
        // vuelve visible el titulo; se activa la cadena de la bienvenida completa.
        ActivarJerarquia(
            pantallaBienvenida != null ? pantallaBienvenida.transform : transform
        );
    }

    private void ReiniciarEstadoDelJuego()
    {
        AlgoLabProgressPanel selector = FindFirstObjectByType<AlgoLabProgressPanel>(
            FindObjectsInactive.Include
        );
        selector?.SalirDelNivelActual();

        AlgoLabManualPanelSpawnManager paneles = AlgoLabManualPanelSpawnManager.Instance;
        if (paneles == null)
        {
            paneles = FindFirstObjectByType<AlgoLabManualPanelSpawnManager>(
                FindObjectsInactive.Include
            );
        }
        paneles?.RecolocarPanelesPredeterminados();

        if (tutorialBienvenida != null)
        {
            tutorialBienvenida.CerrarPanel();
            tutorialBienvenida.OcultarImagenesPanelesTutorial();
        }

        AlgoLabGameAccessController acceso = FindFirstObjectByType<AlgoLabGameAccessController>(
            FindObjectsInactive.Include
        );
        acceso?.BloquearAccesoJuego();
    }

    private void ActivarJerarquia(Transform objetivo)
    {
        if (objetivo == null)
        {
            return;
        }

        if (objetivo.parent != null)
        {
            ActivarJerarquia(objetivo.parent);
        }

        objetivo.gameObject.SetActive(true);
    }

    public void OcultarTodasLasPantallas()
    {
        SetPantalla(pantallaBienvenida, false);
        SetPantalla(pantallaSeleccionModo, false);
        SetPantalla(pantallaLogin, false);
        SetPantalla(pantallaAvisoInvitado, false);
    }

    public void OcultarTodasLasPantallasInmediato()
    {
        SetPantallaInmediato(pantallaBienvenida, false);
        SetPantallaInmediato(pantallaSeleccionModo, false);
        SetPantallaInmediato(pantallaLogin, false);
        SetPantallaInmediato(pantallaAvisoInvitado, false);
    }

    private void SetPantalla(GameObject pantalla, bool activa)
    {
        if (pantalla == null)
        {
            return;
        }

        if (!usarTransicionSuave)
        {
            SetPantallaInmediato(pantalla, activa);
            return;
        }

        if (rutinasTransicion.ContainsKey(pantalla) &&
            rutinasTransicion[pantalla] != null)
        {
            StopCoroutine(rutinasTransicion[pantalla]);
        }

        rutinasTransicion[pantalla] = StartCoroutine(
            TransicionPantallaRutina(pantalla, activa)
        );
    }

    private void SetPantallaInmediato(GameObject pantalla, bool activa)
    {
        if (pantalla == null)
        {
            return;
        }

        CanvasGroup canvasGroup = ObtenerCanvasGroup(pantalla);

        pantalla.SetActive(activa);

        canvasGroup.alpha = activa ? 1f : 0f;
        AplicarInteraccionPantalla(pantalla, activa);

        if (escalarDuranteTransicion)
        {
            pantalla.transform.localScale = activa
                ? Vector3.one
                : Vector3.one * escalaOculta;
        }
    }

    private IEnumerator TransicionPantallaRutina(GameObject pantalla, bool mostrar)
    {
        CanvasGroup canvasGroup = ObtenerCanvasGroup(pantalla);

        if (mostrar)
        {
            pantalla.SetActive(true);
        }

        AplicarInteraccionPantalla(pantalla, mostrar && !bloquearBotonesDuranteTransicion);

        float alphaInicial = canvasGroup.alpha;
        float alphaFinal = mostrar ? 1f : 0f;

        Vector3 escalaInicial = pantalla.transform.localScale;
        Vector3 escalaFinal = mostrar ? Vector3.one : Vector3.one * escalaOculta;

        if (escalarDuranteTransicion && mostrar && escalaInicial == Vector3.zero)
        {
            escalaInicial = Vector3.one * escalaOculta;
            pantalla.transform.localScale = escalaInicial;
        }

        float tiempo = 0f;

        float duracion = Mathf.Max(0.01f, duracionTransicionPantallas);

        while (tiempo < duracion)
        {
            tiempo += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(tiempo / duracion);
            float suavizado = t * t * (3f - 2f * t);

            canvasGroup.alpha = Mathf.Lerp(alphaInicial, alphaFinal, suavizado);

            if (escalarDuranteTransicion)
            {
                pantalla.transform.localScale = Vector3.Lerp(
                    escalaInicial,
                    escalaFinal,
                    suavizado
                );
            }

            yield return null;
        }

        canvasGroup.alpha = alphaFinal;

        if (escalarDuranteTransicion)
        {
            pantalla.transform.localScale = escalaFinal;
        }

        AplicarInteraccionPantalla(pantalla, mostrar);

        if (!mostrar)
        {
            pantalla.SetActive(false);
        }

        if (rutinasTransicion.ContainsKey(pantalla))
        {
            rutinasTransicion[pantalla] = null;
        }
    }


    private void BloquearInteraccionDeTodasLasPantallas()
    {
        AplicarInteraccionPantalla(pantallaBienvenida, false);
        AplicarInteraccionPantalla(pantallaSeleccionModo, false);
        AplicarInteraccionPantalla(pantallaLogin, false);
        AplicarInteraccionPantalla(pantallaAvisoInvitado, false);
    }

    private void AplicarInteraccionPantalla(GameObject pantalla, bool interactiva)
    {
        if (pantalla == null)
        {
            return;
        }

        CanvasGroup canvasGroup = ObtenerCanvasGroup(pantalla);
        canvasGroup.interactable = interactiva;
        canvasGroup.blocksRaycasts = interactiva;

        if (!deshabilitarBotonesPantallaNoActiva)
        {
            return;
        }

        Button[] botones = pantalla.GetComponentsInChildren<Button>(true);

        for (int i = 0; i < botones.Length; i++)
        {
            Button boton = botones[i];

            if (boton == null)
            {
                continue;
            }

            AplicarEstadoTemporal(boton, interactiva);

            Image imagenBoton = boton.GetComponent<Image>();
            if (imagenBoton != null)
            {
                AplicarEstadoTemporal(imagenBoton, interactiva);
            }

            if (deshabilitarCollidersBotonesPantallaNoActiva)
            {
                Collider[] colliders = boton.GetComponentsInChildren<Collider>(true);

                for (int c = 0; c < colliders.Length; c++)
                {
                    if (colliders[c] != null)
                    {
                        AplicarEstadoTemporal(colliders[c], interactiva);
                    }
                }
            }
        }

        TMP_InputField[] inputs = pantalla.GetComponentsInChildren<TMP_InputField>(true);

        for (int i = 0; i < inputs.Length; i++)
        {
            TMP_InputField input = inputs[i];

            if (input == null)
            {
                continue;
            }

            AplicarEstadoTemporal(input, interactiva);

            Image imagenInput = input.GetComponent<Image>();
            if (imagenInput != null)
            {
                AplicarEstadoTemporal(imagenInput, interactiva);
            }
        }
    }

    private void AplicarEstadoTemporal(Button boton, bool habilitar)
    {
        if (!habilitar)
        {
            if (!estadoBotonesBloqueados.ContainsKey(boton))
            {
                estadoBotonesBloqueados[boton] = boton.interactable;
            }

            boton.interactable = false;
        }
        else if (estadoBotonesBloqueados.TryGetValue(boton, out bool estado))
        {
            boton.interactable = estado;
            estadoBotonesBloqueados.Remove(boton);
        }
    }

    private void AplicarEstadoTemporal(TMP_InputField input, bool habilitar)
    {
        if (!habilitar)
        {
            if (!estadoInputsBloqueados.ContainsKey(input))
            {
                estadoInputsBloqueados[input] = input.interactable;
            }

            input.interactable = false;
        }
        else if (estadoInputsBloqueados.TryGetValue(input, out bool estado))
        {
            input.interactable = estado;
            estadoInputsBloqueados.Remove(input);
        }
    }

    private void AplicarEstadoTemporal(Collider collider, bool habilitar)
    {
        if (!habilitar)
        {
            if (!estadoCollidersBloqueados.ContainsKey(collider))
            {
                estadoCollidersBloqueados[collider] = collider.enabled;
            }

            collider.enabled = false;
        }
        else if (estadoCollidersBloqueados.TryGetValue(collider, out bool estado))
        {
            collider.enabled = estado;
            estadoCollidersBloqueados.Remove(collider);
        }
    }

    private void AplicarEstadoTemporal(Image imagen, bool habilitar)
    {
        if (!habilitar)
        {
            if (!estadoImagenesBloqueadas.ContainsKey(imagen))
            {
                estadoImagenesBloqueadas[imagen] = imagen.raycastTarget;
            }

            imagen.raycastTarget = false;
        }
        else if (estadoImagenesBloqueadas.TryGetValue(imagen, out bool estado))
        {
            imagen.raycastTarget = estado;
            estadoImagenesBloqueadas.Remove(imagen);
        }
    }

    private CanvasGroup ObtenerCanvasGroup(GameObject pantalla)
    {
        CanvasGroup canvasGroup = pantalla.GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup = pantalla.AddComponent<CanvasGroup>();
        }

        return canvasGroup;
    }

    private void MostrarMensajeLogin(string mensaje)
    {
        if (textoMensajeLogin != null)
        {
            textoMensajeLogin.color = Color.white;
            textoMensajeLogin.text = mensaje;
        }
    }

    private void MostrarErrorLogin(string mensaje)
    {
        if (textoMensajeLogin != null)
        {
            textoMensajeLogin.color = Color.red;
            textoMensajeLogin.text = mensaje;
        }

        Debug.LogWarning("START UI LOGIN: " + mensaje);
    }

    private void LimpiarMensajeLogin()
    {
        if (textoMensajeLogin != null)
        {
            textoMensajeLogin.text = "";
            textoMensajeLogin.color = Color.white;
        }
    }

    private void LimpiarCamposLogin()
    {
        if (inputCorreo != null)
        {
            inputCorreo.text = "";
        }

        if (inputContrasena != null)
        {
            inputContrasena.text = "";
        }

        if (inputCodigoSegundoFactor != null)
        {
            inputCodigoSegundoFactor.SetTextWithoutNotify(string.Empty);
        }
    }

    private void ActivarInteractableLogin(bool activo)
    {
        if (esperandoSegundoFactor)
        {
            ActivarInteractableSegundoFactor(activo);
            return;
        }

        if (inputCorreo != null)
        {
            inputCorreo.interactable = activo;
        }

        if (inputContrasena != null)
        {
            inputContrasena.interactable = activo;
        }

        if (btnEntrarLogin != null)
        {
            btnEntrarLogin.interactable = activo;
        }

        if (btnVolverDesdeLogin != null)
        {
            btnVolverDesdeLogin.interactable = activo;
        }

        if (btnIrLogin != null)
        {
            btnIrLogin.interactable = activo;
        }

        if (btnJugarInvitado != null)
        {
            btnJugarInvitado.interactable = activo;
        }

        if (btnConfirmarInvitado != null)
        {
            btnConfirmarInvitado.interactable = activo;
        }

        if (btnCancelarInvitado != null)
        {
            btnCancelarInvitado.interactable = activo;
        }
    }

    private void DebugLog(string mensaje)
    {
        if (mostrarDebug)
        {
            Debug.Log(mensaje);
        }
    }
}
