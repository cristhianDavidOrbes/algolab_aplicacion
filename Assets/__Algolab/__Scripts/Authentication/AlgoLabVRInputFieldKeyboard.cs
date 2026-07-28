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
    public bool abrirTecladoSistemaEnQuest = true;
    public bool cerrarTecladoAlTocarFuera = false;

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
    private float proximaActualizacionAutomatica;
    private int ultimoConteoInputs = -1;
    private TMP_InputField inputHoverDebugAnterior;

    private void Awake()
    {
        AsegurarEventSystem();
        ActualizarListaInputs();
    }

    private void Update()
    {
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
    }

    private void RevisarRayo(Transform rayOrigin, bool presionoGatillo, string nombreControl)
    {
        if (rayOrigin == null)
        {
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

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    private void AbrirTeclado(TMP_InputField input)
    {
        if (!abrirTecladoSistemaEnQuest || input == null)
        {
            return;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
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

        tecladoSistema = TouchScreenKeyboard.Open(
            input.text,
            tipoTeclado,
            false,
            false,
            seguro,
            false,
            input.placeholder != null ? input.placeholder.GetComponent<TMP_Text>()?.text : ""
        );
#else
        if (mostrarDebug)
        {
            Debug.Log("VR INPUT FIELD: en editor usa el teclado físico del PC.");
        }
#endif
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
        inputHoverDebugAnterior = null;
        gatilloIzquierdoAnterior = false;
        gatilloDerechoAnterior = false;
    }
}
