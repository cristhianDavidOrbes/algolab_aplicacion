using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class AlgoLabClassDiagramModeManager : MonoBehaviour
{
    public static AlgoLabClassDiagramModeManager Instance { get; private set; }

    public enum ModoPanel
    {
        Diagrama,
        Objeto
    }

    [Serializable]
    public class AtributoConfig
    {
        public string nombreAtributo = "color";

        public List<string> opciones = new List<string>
        {
            "rojo",
            "azul",
            "verde"
        };
    }

    [Serializable]
    public class MetodoConfig
    {
        public string nombreMetodo = "acelerar()";
    }

    [Serializable]
    public class DatosObjetoModo
    {
        public string nombreClase;
        public List<string> nombresAtributos = new List<string>();
        public List<string> valoresAtributos = new List<string>();
        public string metodoSeleccionado;

        public string ObtenerValorAtributo(string nombreAtributo)
        {
            for (int i = 0; i < nombresAtributos.Count; i++)
            {
                if (nombresAtributos[i] == nombreAtributo)
                {
                    return valoresAtributos[i];
                }
            }

            return "";
        }

        public string ObtenerResumen()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("Objeto:");
            sb.AppendLine("Clase: " + nombreClase);
            sb.AppendLine("Atributos:");

            for (int i = 0; i < nombresAtributos.Count; i++)
            {
                sb.AppendLine("- " + nombresAtributos[i] + ": " + valoresAtributos[i]);
            }

            sb.AppendLine("Método seleccionado: " + metodoSeleccionado);

            return sb.ToString();
        }
    }

    [Serializable]
    public class EventoDatosObjeto : UnityEvent<DatosObjetoModo>
    {
    }

    [Header("Modo actual")]
    public ModoPanel modoActual = ModoPanel.Diagrama;

    [Header("Referencias modo diagrama / modo objeto")]
    public GameObject modoDiagramaRoot;
    public GameObject modoObjetoRoot;
    public AlgoLabClassDiagramController classDiagramController;

    [Header("Fondo del panel")]
    public Image imagenFondoPanel;
    public Sprite fondoModoDiagrama;
    public Sprite fondoModoObjeto;

    [Header("Botón cambiar modo")]
    public Button btnCambiarModo;
    public TMP_Text textoBtnCambiarModo;
    public string textoIrModoObjeto = "Modo objeto";
    public string textoIrModoDiagrama = "Modo diagrama";

    [Tooltip("Si está activo, el botón para cambiar entre modo diagrama y modo objeto no aparece. Por defecto está apagado para que el botón sí se vea.")]
    public bool ocultarBotonCambiarModo = false;

    [Header("Animación cambio de modo")]
    public bool usarAnimacionCambioModo = true;
    public float duracionCambioModo = 0.35f;
    public float escalaOcultaModo = 0.92f;

    [Tooltip("Si está activo, bloquea interacción durante la animación.")]
    public bool bloquearInteraccionDuranteAnimacion = true;

    [Header("Datos del objeto")]
    public string nombreClaseObjeto = "Carro";

    public List<AtributoConfig> atributos = new List<AtributoConfig>
    {
        new AtributoConfig
        {
            nombreAtributo = "color",
            opciones = new List<string> { "rojo", "azul", "verde", "negro" }
        },
        new AtributoConfig
        {
            nombreAtributo = "modelo",
            opciones = new List<string> { "Toyota", "Hyundai", "Mazda", "Chevrolet" }
        },
        new AtributoConfig
        {
            nombreAtributo = "carcasa",
            opciones = new List<string> { "metálica", "plástica", "fibra" }
        }
    };

    public List<MetodoConfig> metodos = new List<MetodoConfig>
    {
        new MetodoConfig { nombreMetodo = "acelerar()" },
        new MetodoConfig { nombreMetodo = "frenar()" }
    };

    [Header("Contenedores modo objeto")]
    public RectTransform contenedorAtributos;
    public RectTransform contenedorMetodos;

    [Header("Prefabs modo objeto")]
    [Tooltip("Prefab padre que contiene TextoNombreAtributo y DropdownOpcionesAtributo.")]
    public GameObject prefabAtributoObjeto;

    [Tooltip("Prefab del botón usado para cada método.")]
    public Button prefabBotonMetodo;

    [Header("Tamaños forzados")]
    public Vector2 tamanoAtributo = new Vector2(260f, 95f);
    public Vector2 tamanoMetodo = new Vector2(170f, 50f);

    [Header("Botón crear objeto")]
    public Button btnCrearObjeto;
    public TMP_Text textoBtnCrearObjeto;
    public string textoCrearObjeto = "Crear Objeto";

    [Header("Colores botones método")]
    public Color colorMetodoNormal = new Color(0.20f, 0.22f, 0.28f, 1f);
    public Color colorMetodoActivo = new Color(0.10f, 0.75f, 1f, 1f);
    public Color colorTextoMetodoNormal = Color.white;
    public Color colorTextoMetodoActivo = Color.black;

    [Header("Eventos para otros scripts")]
    public UnityEvent OnModoCambio;
    public UnityEvent OnDatosCambio;

    [Tooltip("Este evento se dispara cuando se presiona Crear Objeto.")]
    public EventoDatosObjeto OnCrearObjetoSolicitado;

    [Header("Debug")]
    public bool mostrarDebug = false;

    private readonly Dictionary<string, TMP_Dropdown> dropdownsPorAtributo =
        new Dictionary<string, TMP_Dropdown>();

    private readonly Dictionary<string, string> valoresAtributos =
        new Dictionary<string, string>();

    private readonly List<Button> botonesMetodoRuntime = new List<Button>();
    private readonly Dictionary<Button, string> metodoPorBoton = new Dictionary<Button, string>();

    private string metodoSeleccionado = "";

    private CanvasGroup grupoModoDiagrama;
    private CanvasGroup grupoModoObjeto;

    private Vector3 escalaOriginalDiagrama = Vector3.one;
    private Vector3 escalaOriginalObjeto = Vector3.one;

    private Coroutine rutinaCambioModo;
    private bool cambiandoModo;

    public ModoPanel ModoActual => modoActual;
    public string MetodoSeleccionado => metodoSeleccionado;
    public bool EstaCambiandoModo => cambiandoModo;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        PrepararCanvasGroups();

        if (btnCambiarModo != null)
        {
            btnCambiarModo.onClick.RemoveListener(ToggleModo);
            btnCambiarModo.onClick.AddListener(ToggleModo);
        }

        if (btnCrearObjeto != null)
        {
            btnCrearObjeto.onClick.RemoveListener(CrearObjeto);
            btnCrearObjeto.onClick.AddListener(CrearObjeto);
        }

        ConstruirModoObjeto();
        AplicarModoInmediato(modoActual);
        ActualizarVisibilidadBotonCambiarModo();
    }

    private void OnDestroy()
    {
        if (btnCambiarModo != null)
        {
            btnCambiarModo.onClick.RemoveListener(ToggleModo);
        }

        if (btnCrearObjeto != null)
        {
            btnCrearObjeto.onClick.RemoveListener(CrearObjeto);
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void OnDisable()
    {
        if (rutinaCambioModo != null)
        {
            StopCoroutine(rutinaCambioModo);
            rutinaCambioModo = null;
        }

        cambiandoModo = false;

        if (Application.isPlaying && Instance == this)
        {
            AplicarModoInmediato(modoActual, false);
        }
    }

    private void OnValidate()
    {
        ActualizarVisibilidadBotonCambiarModo();
        ActualizarTextoBotonCambio();
    }

    private void PrepararCanvasGroups()
    {
        grupoModoDiagrama = GetOrAddCanvasGroup(modoDiagramaRoot);
        grupoModoObjeto = GetOrAddCanvasGroup(modoObjetoRoot);

        if (modoDiagramaRoot != null)
        {
            escalaOriginalDiagrama = modoDiagramaRoot.transform.localScale;
        }

        if (modoObjetoRoot != null)
        {
            escalaOriginalObjeto = modoObjetoRoot.transform.localScale;
        }
    }

    [ContextMenu("Cambiar modo")]
    public void ToggleModo()
    {
        if (modoActual == ModoPanel.Diagrama)
        {
            CambiarAModoObjeto();
        }
        else
        {
            CambiarAModoDiagrama();
        }
    }

    [ContextMenu("Modo Diagrama")]
    public void CambiarAModoDiagrama()
    {
        SetModo(ModoPanel.Diagrama, true);
    }

    [ContextMenu("Modo Objeto")]
    public void CambiarAModoObjeto()
    {
        SetModo(ModoPanel.Objeto, true);
    }

    public void SetModo(ModoPanel nuevoModo)
    {
        SetModo(nuevoModo, true);
    }

    public void SetModoSinAnimacion(ModoPanel nuevoModo)
    {
        SetModo(nuevoModo, false);
    }

    public void SetModoObjeto()
    {
        SetModo(ModoPanel.Objeto, true);
    }

    public void SetModoDiagrama()
    {
        SetModo(ModoPanel.Diagrama, true);
    }

    private void SetModo(ModoPanel nuevoModo, bool animado)
    {
        if (modoActual == nuevoModo)
        {
            ActualizarTextoBotonCambio();
            ActualizarVisibilidadBotonCambiarModo();
            return;
        }

        ModoPanel modoAnterior = modoActual;
        modoActual = nuevoModo;

        if (rutinaCambioModo != null)
        {
            StopCoroutine(rutinaCambioModo);
            rutinaCambioModo = null;
        }

        if (Application.isPlaying && usarAnimacionCambioModo && animado)
        {
            rutinaCambioModo = StartCoroutine(AnimarCambioModo(modoAnterior, nuevoModo));
        }
        else
        {
            AplicarModoInmediato(nuevoModo);
        }
    }

    private void AplicarModoInmediato(ModoPanel nuevoModo, bool notificarCambio = true)
    {
        PrepararCanvasGroups();

        bool esModoDiagrama = nuevoModo == ModoPanel.Diagrama;
        bool esModoObjeto = nuevoModo == ModoPanel.Objeto;

        if (modoDiagramaRoot != null)
        {
            modoDiagramaRoot.SetActive(esModoDiagrama);
            modoDiagramaRoot.transform.localScale = escalaOriginalDiagrama;
        }

        if (modoObjetoRoot != null)
        {
            modoObjetoRoot.SetActive(esModoObjeto);
            modoObjetoRoot.transform.localScale = escalaOriginalObjeto;
        }

        SetCanvasGroup(grupoModoDiagrama, esModoDiagrama ? 1f : 0f, esModoDiagrama);
        SetCanvasGroup(grupoModoObjeto, esModoObjeto ? 1f : 0f, esModoObjeto);

        AplicarFondo(nuevoModo);
        AplicarEstadoController(nuevoModo);
        ActualizarTextoBotonCambio();
        ActualizarVisibilidadBotonCambiarModo();

        cambiandoModo = false;

        if (notificarCambio)
        {
            OnModoCambio?.Invoke();
        }

        if (mostrarDebug && notificarCambio)
        {
            Debug.Log("Modo aplicado inmediato: " + nuevoModo);
        }
    }

    private IEnumerator AnimarCambioModo(ModoPanel modoAnterior, ModoPanel modoNuevo)
    {
        cambiandoModo = true;

        GameObject rootSalida = ObtenerRootModo(modoAnterior);
        GameObject rootEntrada = ObtenerRootModo(modoNuevo);

        CanvasGroup grupoSalida = ObtenerGrupoModo(modoAnterior);
        CanvasGroup grupoEntrada = ObtenerGrupoModo(modoNuevo);

        Vector3 escalaSalidaNormal = ObtenerEscalaOriginal(modoAnterior);
        Vector3 escalaEntradaNormal = ObtenerEscalaOriginal(modoNuevo);

        if (rootEntrada != null)
        {
            rootEntrada.SetActive(true);
            rootEntrada.transform.localScale = escalaEntradaNormal * escalaOcultaModo;
        }

        if (rootSalida != null)
        {
            rootSalida.SetActive(true);
            rootSalida.transform.localScale = escalaSalidaNormal;
        }

        SetCanvasGroup(grupoEntrada, 0f, false);
        SetCanvasGroup(grupoSalida, 1f, false);

        AplicarFondo(modoNuevo);
        AplicarEstadoController(modoNuevo);
        ActualizarTextoBotonCambio();
        ActualizarVisibilidadBotonCambiarModo();

        float tiempo = 0f;

        while (tiempo < duracionCambioModo)
        {
            tiempo += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(tiempo / duracionCambioModo);
            float smooth = Mathf.SmoothStep(0f, 1f, t);

            if (grupoSalida != null)
            {
                grupoSalida.alpha = Mathf.Lerp(1f, 0f, smooth);
            }

            if (grupoEntrada != null)
            {
                grupoEntrada.alpha = Mathf.Lerp(0f, 1f, smooth);
            }

            if (rootSalida != null)
            {
                rootSalida.transform.localScale = Vector3.Lerp(
                    escalaSalidaNormal,
                    escalaSalidaNormal * escalaOcultaModo,
                    smooth
                );
            }

            if (rootEntrada != null)
            {
                rootEntrada.transform.localScale = Vector3.Lerp(
                    escalaEntradaNormal * escalaOcultaModo,
                    escalaEntradaNormal,
                    smooth
                );
            }

            yield return null;
        }

        if (rootSalida != null)
        {
            rootSalida.SetActive(false);
            rootSalida.transform.localScale = escalaSalidaNormal;
        }

        if (rootEntrada != null)
        {
            rootEntrada.SetActive(true);
            rootEntrada.transform.localScale = escalaEntradaNormal;
        }

        SetCanvasGroup(grupoSalida, 0f, false);
        SetCanvasGroup(grupoEntrada, 1f, true);

        cambiandoModo = false;
        rutinaCambioModo = null;

        OnModoCambio?.Invoke();

        if (mostrarDebug)
        {
            Debug.Log("Modo cambiado con animación: " + modoNuevo);
        }
    }

    private GameObject ObtenerRootModo(ModoPanel modo)
    {
        return modo == ModoPanel.Diagrama ? modoDiagramaRoot : modoObjetoRoot;
    }

    private CanvasGroup ObtenerGrupoModo(ModoPanel modo)
    {
        return modo == ModoPanel.Diagrama ? grupoModoDiagrama : grupoModoObjeto;
    }

    private Vector3 ObtenerEscalaOriginal(ModoPanel modo)
    {
        return modo == ModoPanel.Diagrama ? escalaOriginalDiagrama : escalaOriginalObjeto;
    }

    private void AplicarFondo(ModoPanel modo)
    {
        if (imagenFondoPanel == null)
        {
            return;
        }

        if (modo == ModoPanel.Objeto && fondoModoObjeto != null)
        {
            imagenFondoPanel.sprite = fondoModoObjeto;
        }
        else if (modo == ModoPanel.Diagrama && fondoModoDiagrama != null)
        {
            imagenFondoPanel.sprite = fondoModoDiagrama;
        }
    }

    private void AplicarEstadoController(ModoPanel modo)
    {
        if (classDiagramController != null)
        {
            classDiagramController.enabled = modo == ModoPanel.Diagrama;
        }
    }

    private void ActualizarTextoBotonCambio()
    {
        if (textoBtnCambiarModo == null)
        {
            return;
        }

        textoBtnCambiarModo.text = modoActual == ModoPanel.Diagrama
            ? textoIrModoObjeto
            : textoIrModoDiagrama;
    }

    private void ActualizarVisibilidadBotonCambiarModo()
    {
        if (btnCambiarModo == null)
        {
            return;
        }

        btnCambiarModo.gameObject.SetActive(!ocultarBotonCambiarModo);
    }

    public void SetMostrarBotonCambiarModo(bool mostrar)
    {
        ocultarBotonCambiarModo = !mostrar;
        ActualizarVisibilidadBotonCambiarModo();
    }

    public void MostrarBotonCambiarModo()
    {
        SetMostrarBotonCambiarModo(true);
    }

    public void OcultarBotonCambiarModo()
    {
        SetMostrarBotonCambiarModo(false);
    }

    public bool BotonCambiarModoVisible()
    {
        return !ocultarBotonCambiarModo;
    }

    [ContextMenu("Reconstruir modo objeto")]
    public void ConstruirModoObjeto()
    {
        LimpiarContenedor(contenedorAtributos);
        LimpiarContenedor(contenedorMetodos);

        dropdownsPorAtributo.Clear();
        valoresAtributos.Clear();
        botonesMetodoRuntime.Clear();
        metodoPorBoton.Clear();
        metodoSeleccionado = "";

        CrearAtributosUI();
        CrearMetodosUI();

        if (textoBtnCrearObjeto != null)
        {
            textoBtnCrearObjeto.text = textoCrearObjeto;
        }

        OnDatosCambio?.Invoke();
    }

    private void CrearAtributosUI()
    {
        if (contenedorAtributos == null)
        {
            Debug.LogWarning("Falta asignar Contenedor Atributos.");
            return;
        }

        if (prefabAtributoObjeto == null)
        {
            Debug.LogWarning("Falta asignar PrefabAtributoObjeto.");
            return;
        }

        for (int i = 0; i < atributos.Count; i++)
        {
            AtributoConfig atributo = atributos[i];

            if (atributo == null || string.IsNullOrWhiteSpace(atributo.nombreAtributo))
            {
                continue;
            }

            GameObject atributoGO = Instantiate(prefabAtributoObjeto, contenedorAtributos);
            atributoGO.name = "Atributo_" + atributo.nombreAtributo;
            atributoGO.SetActive(true);

            ForzarTamanoAtributo(atributoGO);

            TMP_Text textoNombre = BuscarTMPPorNombre(atributoGO.transform, "TextoNombreAtributo");
            TMP_Dropdown dropdown = BuscarDropdownPorNombre(atributoGO.transform, "DropdownOpcionesAtributo");

            if (textoNombre != null)
            {
                textoNombre.text = atributo.nombreAtributo + ":";
                textoNombre.textWrappingMode = TextWrappingModes.NoWrap;
                textoNombre.overflowMode = TextOverflowModes.Overflow;
            }
            else
            {
                Debug.LogWarning("No se encontró TextoNombreAtributo en el prefab.");
            }

            if (dropdown == null)
            {
                Debug.LogWarning("No se encontró DropdownOpcionesAtributo en el prefab.");
                continue;
            }

            RectTransform dropdownRect = dropdown.GetComponent<RectTransform>();

            if (dropdownRect != null)
            {
                dropdownRect.sizeDelta = new Vector2(240f, 40f);
            }

            dropdown.ClearOptions();

            List<string> opcionesLimpias = ObtenerOpcionesLimpias(atributo);

            dropdown.AddOptions(opcionesLimpias);
            dropdown.value = 0;
            dropdown.RefreshShownValue();

            string nombreCapturado = atributo.nombreAtributo;

            dropdownsPorAtributo[nombreCapturado] = dropdown;
            valoresAtributos[nombreCapturado] = opcionesLimpias[0];

            dropdown.onValueChanged.AddListener((indice) =>
            {
                CambiarValorAtributo(nombreCapturado, indice);
            });
        }
    }

    private void ForzarTamanoAtributo(GameObject atributoGO)
    {
        if (atributoGO == null)
        {
            return;
        }

        RectTransform atributoRect = atributoGO.GetComponent<RectTransform>();

        if (atributoRect != null)
        {
            atributoRect.sizeDelta = tamanoAtributo;
        }

        LayoutElement atributoLayout = atributoGO.GetComponent<LayoutElement>();

        if (atributoLayout == null)
        {
            atributoLayout = atributoGO.AddComponent<LayoutElement>();
        }

        atributoLayout.preferredWidth = tamanoAtributo.x;
        atributoLayout.preferredHeight = tamanoAtributo.y;
        atributoLayout.minWidth = tamanoAtributo.x;
        atributoLayout.minHeight = tamanoAtributo.y;
        atributoLayout.flexibleWidth = 0f;
        atributoLayout.flexibleHeight = 0f;
    }

    private List<string> ObtenerOpcionesLimpias(AtributoConfig atributo)
    {
        List<string> opcionesLimpias = new List<string>();

        if (atributo.opciones != null)
        {
            for (int i = 0; i < atributo.opciones.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(atributo.opciones[i]))
                {
                    opcionesLimpias.Add(atributo.opciones[i]);
                }
            }
        }

        if (opcionesLimpias.Count == 0)
        {
            opcionesLimpias.Add("Sin opción");
        }

        return opcionesLimpias;
    }

    private void CrearMetodosUI()
    {
        if (contenedorMetodos == null)
        {
            Debug.LogWarning("Falta asignar Contenedor Métodos.");
            return;
        }

        if (prefabBotonMetodo == null)
        {
            Debug.LogWarning("Falta asignar PrefabBotonMetodo.");
            return;
        }

        for (int i = 0; i < metodos.Count; i++)
        {
            MetodoConfig metodo = metodos[i];

            if (metodo == null || string.IsNullOrWhiteSpace(metodo.nombreMetodo))
            {
                continue;
            }

            Button boton = Instantiate(prefabBotonMetodo, contenedorMetodos);
            boton.name = "Metodo_" + metodo.nombreMetodo;
            boton.gameObject.SetActive(true);

            ForzarTamanoBotonMetodo(boton);

            TMP_Text texto = boton.GetComponentInChildren<TMP_Text>();

            if (texto != null)
            {
                texto.text = metodo.nombreMetodo;
                texto.textWrappingMode = TextWrappingModes.NoWrap;
                texto.overflowMode = TextOverflowModes.Overflow;
                texto.alignment = TextAlignmentOptions.Center;
            }

            string metodoCapturado = metodo.nombreMetodo;

            botonesMetodoRuntime.Add(boton);
            metodoPorBoton[boton] = metodoCapturado;

            boton.onClick.AddListener(() =>
            {
                SeleccionarMetodo(metodoCapturado);
            });
        }

        ActualizarVisualBotonesMetodo();
    }

    private void ForzarTamanoBotonMetodo(Button boton)
    {
        if (boton == null)
        {
            return;
        }

        RectTransform botonRect = boton.GetComponent<RectTransform>();

        if (botonRect != null)
        {
            botonRect.sizeDelta = tamanoMetodo;
        }

        LayoutElement botonLayout = boton.GetComponent<LayoutElement>();

        if (botonLayout == null)
        {
            botonLayout = boton.gameObject.AddComponent<LayoutElement>();
        }

        botonLayout.preferredWidth = tamanoMetodo.x;
        botonLayout.preferredHeight = tamanoMetodo.y;
        botonLayout.minWidth = tamanoMetodo.x;
        botonLayout.minHeight = tamanoMetodo.y;
        botonLayout.flexibleWidth = 0f;
        botonLayout.flexibleHeight = 0f;
    }

    private void CambiarValorAtributo(string nombreAtributo, int indice)
    {
        if (!dropdownsPorAtributo.ContainsKey(nombreAtributo))
        {
            return;
        }

        TMP_Dropdown dropdown = dropdownsPorAtributo[nombreAtributo];

        if (dropdown == null || dropdown.options == null || dropdown.options.Count == 0)
        {
            return;
        }

        indice = Mathf.Clamp(indice, 0, dropdown.options.Count - 1);

        valoresAtributos[nombreAtributo] = dropdown.options[indice].text;

        OnDatosCambio?.Invoke();

        if (mostrarDebug)
        {
            Debug.Log("Atributo cambiado: " + nombreAtributo + " = " + valoresAtributos[nombreAtributo]);
        }
    }

    public void SeleccionarMetodo(string nombreMetodo)
    {
        if (metodoSeleccionado == nombreMetodo)
        {
            metodoSeleccionado = "";
        }
        else
        {
            metodoSeleccionado = nombreMetodo;
        }

        ActualizarVisualBotonesMetodo();
        OnDatosCambio?.Invoke();

        if (mostrarDebug)
        {
            Debug.Log("Método seleccionado: " + metodoSeleccionado);
        }
    }

    private void ActualizarVisualBotonesMetodo()
    {
        for (int i = 0; i < botonesMetodoRuntime.Count; i++)
        {
            Button boton = botonesMetodoRuntime[i];

            if (boton == null)
            {
                continue;
            }

            string metodo = metodoPorBoton.ContainsKey(boton)
                ? metodoPorBoton[boton]
                : "";

            bool activo = metodo == metodoSeleccionado;

            Image image = boton.GetComponent<Image>();

            if (image != null)
            {
                image.color = activo ? colorMetodoActivo : colorMetodoNormal;
            }

            TMP_Text texto = boton.GetComponentInChildren<TMP_Text>();

            if (texto != null)
            {
                texto.color = activo ? colorTextoMetodoActivo : colorTextoMetodoNormal;
            }
        }
    }

    public void CrearObjeto()
    {
        DatosObjetoModo datos = ObtenerDatosObjetoActual();

        OnCrearObjetoSolicitado?.Invoke(datos);

        if (mostrarDebug)
        {
            Debug.Log("Crear objeto solicitado:\n" + datos.ObtenerResumen());
        }
    }

    public DatosObjetoModo ObtenerDatosObjetoActual()
    {
        DatosObjetoModo datos = new DatosObjetoModo();
        datos.nombreClase = nombreClaseObjeto;
        datos.metodoSeleccionado = metodoSeleccionado;

        foreach (KeyValuePair<string, string> par in valoresAtributos)
        {
            datos.nombresAtributos.Add(par.Key);
            datos.valoresAtributos.Add(par.Value);
        }

        return datos;
    }

    public Dictionary<string, string> ObtenerAtributosSeleccionados()
    {
        return new Dictionary<string, string>(valoresAtributos);
    }

    public string ObtenerValorAtributo(string nombreAtributo)
    {
        if (string.IsNullOrWhiteSpace(nombreAtributo))
        {
            return "";
        }

        if (valoresAtributos.ContainsKey(nombreAtributo))
        {
            return valoresAtributos[nombreAtributo];
        }

        return "";
    }

    public string ObtenerMetodoSeleccionado()
    {
        return metodoSeleccionado;
    }

    public bool EstaEnModoObjeto()
    {
        return modoActual == ModoPanel.Objeto;
    }

    public bool EstaEnModoDiagrama()
    {
        return modoActual == ModoPanel.Diagrama;
    }

    private void LimpiarContenedor(RectTransform contenedor)
    {
        if (contenedor == null)
        {
            return;
        }

        for (int i = contenedor.childCount - 1; i >= 0; i--)
        {
            Transform hijo = contenedor.GetChild(i);

            if (Application.isPlaying)
            {
                Destroy(hijo.gameObject);
            }
            else
            {
                DestroyImmediate(hijo.gameObject);
            }
        }
    }

    private CanvasGroup GetOrAddCanvasGroup(GameObject obj)
    {
        if (obj == null)
        {
            return null;
        }

        CanvasGroup group = obj.GetComponent<CanvasGroup>();

        if (group == null)
        {
            group = obj.AddComponent<CanvasGroup>();
        }

        return group;
    }

    private void SetCanvasGroup(CanvasGroup group, float alpha, bool interactable)
    {
        if (group == null)
        {
            return;
        }

        group.alpha = alpha;

        if (bloquearInteraccionDuranteAnimacion)
        {
            group.interactable = interactable;
            group.blocksRaycasts = interactable;
        }
        else
        {
            group.interactable = true;
            group.blocksRaycasts = true;
        }
    }

    private TMP_Text BuscarTMPPorNombre(Transform root, string nombre)
    {
        TMP_Text[] textos = root.GetComponentsInChildren<TMP_Text>(true);

        for (int i = 0; i < textos.Length; i++)
        {
            if (textos[i].name == nombre)
            {
                return textos[i];
            }
        }

        return root.GetComponentInChildren<TMP_Text>(true);
    }

    private TMP_Dropdown BuscarDropdownPorNombre(Transform root, string nombre)
    {
        TMP_Dropdown[] dropdowns = root.GetComponentsInChildren<TMP_Dropdown>(true);

        for (int i = 0; i < dropdowns.Length; i++)
        {
            if (dropdowns[i].name == nombre)
            {
                return dropdowns[i];
            }
        }

        return root.GetComponentInChildren<TMP_Dropdown>(true);
    }
}
