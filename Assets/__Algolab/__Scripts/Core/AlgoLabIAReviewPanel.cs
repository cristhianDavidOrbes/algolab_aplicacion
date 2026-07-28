using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AlgoLabIAReviewPanel : MonoBehaviour
{
    [Header("Referencia cabeza")]
    public Transform headReference;

    [Header("Panel")]
    public RectTransform panelRect;
    public CanvasGroup panelGroup;

    [Header("Vistas")]
    public GameObject vistaInicial;
    public GameObject vistaRevision;
    public CanvasGroup vistaInicialGroup;
    public CanvasGroup vistaRevisionGroup;

    [Header("Vista inicial")]
    public RectTransform micContainer;
    public RectTransform textoInstruccionRect;
    public TMP_Text textoInstruccion;

    [Header("Vista revisión / CMD")]
    public TMP_Text cmdPrefix;
    public TMP_Text textoMensaje;

    [Header("Botones")]
    public Button btnRegrabar;
    public Button btnEnviar;

    [Header("Aparición opcional frente al usuario")]
    public bool ubicarFrenteAlMostrar = false;
    public float distanciaFrenteCabeza = 0.65f;
    public Vector3 offsetFrenteCabeza = new Vector3(0.28f, -0.03f, 0f);

    [Header("Mirar al usuario")]
    public bool mirarSiempreAlUsuario = true;
    public bool invertirFrente = false;
    public float suavizadoRotacion = 10f;

    [Header("Animación")]
    public float duracionCambioVista = 0.35f;
    public float escalaNormal = 1f;
    public float escalaPulso = 1.08f;
    public float escalaOculto = 0.85f;

    [Header("Texto CMD")]
    public string prefijoCMD = @"C:\Users\usuario>";
    public string cursor = "|";

    [Header("Efecto escritura")]
    public float velocidadEscritura = 0.025f;
    public float velocidadCursor = 0.35f;

    [Header("Carga IA")]
    public float velocidadSpinner = 0.18f;
    public float tiempoAntesDeVolverInicio = 2f;

    private readonly string[] framesCarga = { "|", "/", "-", "\\" };

    private Action<string> onEnviar;
    private Action onRegrabar;

    private Coroutine rutinaActual;
    private Coroutine rutinaEscritura;
    private Coroutine rutinaCursor;
    private Coroutine rutinaSpinner;
    private Coroutine rutinaVolverInicio;

    private string mensajeActual = "";
    private string textoBaseActual = "";

    private bool cursorVisible = true;
    private bool cursorActivo = false;
    private bool spinnerActivo = false;

    private Vector2 posicionOriginalTextoInstruccion;

    private void Awake()
    {
        if (panelGroup == null && panelRect != null)
            panelGroup = panelRect.GetComponent<CanvasGroup>();

        if (vistaInicialGroup == null && vistaInicial != null)
            vistaInicialGroup = vistaInicial.GetComponent<CanvasGroup>();

        if (vistaRevisionGroup == null && vistaRevision != null)
            vistaRevisionGroup = vistaRevision.GetComponent<CanvasGroup>();

        if (textoInstruccionRect != null)
            posicionOriginalTextoInstruccion = textoInstruccionRect.anchoredPosition;

        if (btnEnviar != null)
        {
            btnEnviar.onClick.RemoveListener(EnviarMensaje);
            btnEnviar.onClick.AddListener(EnviarMensaje);
        }

        if (btnRegrabar != null)
        {
            btnRegrabar.onClick.RemoveListener(RegrabarMensaje);
            btnRegrabar.onClick.AddListener(RegrabarMensaje);
        }

        MostrarVistaInicialInmediata();
    }

    private void OnDisable()
    {
        DetenerRutinaActual();
        DetenerTodosLosEfectos();
        onEnviar = null;
        onRegrabar = null;
    }

    private void OnDestroy()
    {
        if (btnEnviar != null)
            btnEnviar.onClick.RemoveListener(EnviarMensaje);

        if (btnRegrabar != null)
            btnRegrabar.onClick.RemoveListener(RegrabarMensaje);
    }

    private void LateUpdate()
    {
        if (!mirarSiempreAlUsuario || headReference == null)
            return;

        MirarAlUsuario();
    }

    public void MostrarVistaInicial()
    {
        DetenerTodosLosEfectos();

        DetenerRutinaActual();

        if (!isActiveAndEnabled)
        {
            MostrarVistaInicialInmediata();
            return;
        }

        rutinaActual = StartCoroutine(AnimarAVistaInicial());
    }

    public void MostrarGrabando()
    {
        if (ubicarFrenteAlMostrar)
            UbicarFrenteALaCabeza();

        DetenerRutinaActual();

        if (!isActiveAndEnabled)
            return;

        rutinaActual = StartCoroutine(AnimarGrabando());
    }

    public void MostrarProcesando()
    {
        PrepararCMDVacio();
    }

    public void MostrarRevision(string mensajeReconocido, Action<string> callbackEnviar, Action callbackRegrabar)
    {
        onEnviar = callbackEnviar;
        onRegrabar = callbackRegrabar;
        mensajeActual = mensajeReconocido;

        DetenerRutinaActual();

        if (!isActiveAndEnabled)
        {
            onEnviar = null;
            onRegrabar = null;
            return;
        }

        rutinaActual = StartCoroutine(AnimarAVistaRevisionYEscribir(mensajeReconocido));
    }

    public void MostrarCargandoIA()
    {
        DetenerTodosLosEfectos();

        if (btnEnviar != null)
            btnEnviar.interactable = false;

        if (btnRegrabar != null)
            btnRegrabar.interactable = false;

        if (cmdPrefix != null)
            cmdPrefix.text = prefijoCMD;

        textoBaseActual = "Enviando mensaje a la IA...\nPensando";
        spinnerActivo = true;

        if (isActiveAndEnabled)
            rutinaSpinner = StartCoroutine(AnimarSpinner());
    }

    public void MostrarProcesoCompletado()
    {
        DetenerTodosLosEfectos();

        if (btnEnviar != null)
            btnEnviar.interactable = false;

        if (btnRegrabar != null)
            btnRegrabar.interactable = false;

        if (cmdPrefix != null)
            cmdPrefix.text = prefijoCMD;

        textoBaseActual = "Respuesta recibida.\nCompletando proceso...\nTerminando proceso.";
        ActualizarTextoDirecto(textoBaseActual);

        if (rutinaVolverInicio != null)
            StopCoroutine(rutinaVolverInicio);

        if (isActiveAndEnabled)
            rutinaVolverInicio = StartCoroutine(VolverAInicioDespuesDeEspera());
    }

    public void MostrarErrorYTerminar(string mensajeError)
    {
        DetenerTodosLosEfectos();

        if (btnEnviar != null)
            btnEnviar.interactable = false;

        if (btnRegrabar != null)
            btnRegrabar.interactable = false;

        if (cmdPrefix != null)
            cmdPrefix.text = prefijoCMD;

        if (string.IsNullOrWhiteSpace(mensajeError))
            mensajeError = "La IA no pudo responder o no está disponible en este momento.";

        textoBaseActual = "ERROR: " + mensajeError + "\nTerminando proceso.";
        ActualizarTextoDirecto(textoBaseActual);

        if (rutinaVolverInicio != null)
            StopCoroutine(rutinaVolverInicio);

        if (isActiveAndEnabled)
            rutinaVolverInicio = StartCoroutine(VolverAInicioDespuesDeEspera());
    }

    private void EnviarMensaje()
    {
        string mensajeFinal = mensajeActual.Trim();

        if (string.IsNullOrWhiteSpace(mensajeFinal))
        {
            MostrarErrorYTerminar("No se puede enviar un mensaje vacío.");
            return;
        }

        DetenerTodosLosEfectos();

        if (btnEnviar != null)
            btnEnviar.interactable = false;

        if (btnRegrabar != null)
            btnRegrabar.interactable = false;

        onEnviar?.Invoke(mensajeFinal);
    }

    private void RegrabarMensaje()
    {
        mensajeActual = "";
        DetenerTodosLosEfectos();

        onRegrabar?.Invoke();

        DetenerRutinaActual();

        if (!isActiveAndEnabled)
            return;

        rutinaActual = StartCoroutine(AnimarGrabando());
    }

    private void MostrarVistaInicialInmediata()
    {
        DetenerTodosLosEfectos();

        if (panelGroup != null)
        {
            panelGroup.alpha = 1f;
            panelGroup.interactable = true;
            panelGroup.blocksRaycasts = true;
        }

        if (panelRect != null)
            panelRect.localScale = Vector3.one * escalaNormal;

        if (vistaInicial != null)
            vistaInicial.SetActive(true);

        if (vistaRevision != null)
            vistaRevision.SetActive(false);

        SetCanvasGroup(vistaInicialGroup, 1f, true);
        SetCanvasGroup(vistaRevisionGroup, 0f, false);

        if (micContainer != null)
            micContainer.localScale = Vector3.one;

        if (textoInstruccionRect != null)
            textoInstruccionRect.anchoredPosition = posicionOriginalTextoInstruccion;

        if (textoInstruccion != null)
            textoInstruccion.alpha = 1f;

        if (cmdPrefix != null)
            cmdPrefix.text = prefijoCMD;

        if (textoMensaje != null)
            textoMensaje.text = "";
    }

    private IEnumerator AnimarGrabando()
    {
        if (vistaInicial != null)
            vistaInicial.SetActive(true);

        if (vistaRevision != null)
            vistaRevision.SetActive(false);

        SetCanvasGroup(vistaInicialGroup, 1f, true);
        SetCanvasGroup(vistaRevisionGroup, 0f, false);

        if (btnEnviar != null)
            btnEnviar.interactable = false;

        if (btnRegrabar != null)
            btnRegrabar.interactable = true;

        PrepararCMDVacio();

        float tiempo = 0f;

        Vector3 escalaMicInicio = micContainer != null ? micContainer.localScale : Vector3.one;

        Vector2 textoInicio = textoInstruccionRect != null
            ? textoInstruccionRect.anchoredPosition
            : Vector2.zero;

        Vector2 textoFinal = textoInicio + new Vector2(0f, -80f);

        while (tiempo < duracionCambioVista)
        {
            tiempo += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(tiempo / duracionCambioVista);
            float smooth = Mathf.SmoothStep(0f, 1f, t);

            if (panelRect != null)
            {
                float escala = Mathf.Lerp(escalaNormal, escalaPulso, Mathf.Sin(t * Mathf.PI));
                panelRect.localScale = Vector3.one * escala;
            }

            if (micContainer != null)
            {
                float escalaMic = Mathf.Lerp(1f, 1.18f, Mathf.Sin(t * Mathf.PI));
                micContainer.localScale = escalaMicInicio * escalaMic;
            }

            if (textoInstruccionRect != null)
                textoInstruccionRect.anchoredPosition = Vector2.Lerp(textoInicio, textoFinal, smooth);

            if (textoInstruccion != null)
                textoInstruccion.alpha = Mathf.Lerp(1f, 0f, smooth);

            if (vistaInicialGroup != null)
                vistaInicialGroup.alpha = Mathf.Lerp(1f, 0f, smooth);

            yield return null;
        }

        if (vistaInicial != null)
            vistaInicial.SetActive(false);

        if (vistaRevision != null)
            vistaRevision.SetActive(true);

        SetCanvasGroup(vistaInicialGroup, 0f, false);
        SetCanvasGroup(vistaRevisionGroup, 1f, true);

        if (panelRect != null)
            panelRect.localScale = Vector3.one * escalaNormal;

        if (textoInstruccionRect != null)
            textoInstruccionRect.anchoredPosition = posicionOriginalTextoInstruccion;

        if (textoInstruccion != null)
            textoInstruccion.alpha = 1f;

        rutinaActual = null;
    }

    private IEnumerator AnimarAVistaRevisionYEscribir(string mensaje)
    {
        if (vistaInicial != null)
            vistaInicial.SetActive(false);

        if (vistaRevision != null)
            vistaRevision.SetActive(true);

        SetCanvasGroup(vistaInicialGroup, 0f, false);

        if (vistaRevisionGroup != null)
        {
            vistaRevisionGroup.alpha = 0f;
            vistaRevisionGroup.interactable = true;
            vistaRevisionGroup.blocksRaycasts = true;
        }

        if (btnEnviar != null)
            btnEnviar.interactable = false;

        if (btnRegrabar != null)
            btnRegrabar.interactable = true;

        if (panelRect != null)
            panelRect.localScale = Vector3.one * escalaOculto;

        float tiempo = 0f;

        while (tiempo < duracionCambioVista)
        {
            tiempo += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(tiempo / duracionCambioVista);
            float smooth = Mathf.SmoothStep(0f, 1f, t);

            if (vistaRevisionGroup != null)
                vistaRevisionGroup.alpha = smooth;

            if (panelRect != null)
            {
                panelRect.localScale = Vector3.Lerp(
                    Vector3.one * escalaOculto,
                    Vector3.one * escalaNormal,
                    smooth
                );
            }

            yield return null;
        }

        SetCanvasGroup(vistaRevisionGroup, 1f, true);

        if (panelRect != null)
            panelRect.localScale = Vector3.one * escalaNormal;

        EscribirMensaje(mensaje);
        rutinaActual = null;
    }

    private IEnumerator AnimarAVistaInicial()
    {
        if (vistaInicial != null)
            vistaInicial.SetActive(true);

        if (vistaRevision != null)
            vistaRevision.SetActive(false);

        SetCanvasGroup(vistaRevisionGroup, 0f, false);

        if (vistaInicialGroup != null)
        {
            vistaInicialGroup.alpha = 0f;
            vistaInicialGroup.interactable = true;
            vistaInicialGroup.blocksRaycasts = true;
        }

        if (panelRect != null)
            panelRect.localScale = Vector3.one * escalaOculto;

        float tiempo = 0f;

        while (tiempo < duracionCambioVista)
        {
            tiempo += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(tiempo / duracionCambioVista);
            float smooth = Mathf.SmoothStep(0f, 1f, t);

            if (vistaInicialGroup != null)
                vistaInicialGroup.alpha = smooth;

            if (panelRect != null)
            {
                panelRect.localScale = Vector3.Lerp(
                    Vector3.one * escalaOculto,
                    Vector3.one * escalaNormal,
                    smooth
                );
            }

            yield return null;
        }

        SetCanvasGroup(vistaInicialGroup, 1f, true);

        if (panelRect != null)
            panelRect.localScale = Vector3.one * escalaNormal;

        rutinaActual = null;
    }

    private void PrepararCMDVacio()
    {
        DetenerTodosLosEfectos();

        mensajeActual = "";
        textoBaseActual = "";
        cursorActivo = true;
        cursorVisible = true;

        if (cmdPrefix != null)
            cmdPrefix.text = prefijoCMD;

        ActualizarTextoConCursor();

        rutinaCursor = StartCoroutine(ParpadearCursor());
    }

    private void EscribirMensaje(string mensaje)
    {
        DetenerTodosLosEfectos();

        mensajeActual = mensaje;
        textoBaseActual = "";
        cursorVisible = true;
        cursorActivo = true;

        if (cmdPrefix != null)
            cmdPrefix.text = prefijoCMD;

        rutinaCursor = StartCoroutine(ParpadearCursor());
        rutinaEscritura = StartCoroutine(EscribirCaracteres(mensaje));
    }

    private IEnumerator EscribirCaracteres(string mensaje)
    {
        for (int i = 0; i < mensaje.Length; i++)
        {
            textoBaseActual += mensaje[i];
            ActualizarTextoConCursor();

            yield return new WaitForSecondsRealtime(Mathf.Max(0f, velocidadEscritura));
        }

        textoBaseActual = mensaje;
        ActualizarTextoConCursor();

        if (btnEnviar != null)
            btnEnviar.interactable = true;

        rutinaEscritura = null;
    }

    private IEnumerator ParpadearCursor()
    {
        while (cursorActivo)
        {
            cursorVisible = !cursorVisible;
            ActualizarTextoConCursor();

            yield return new WaitForSecondsRealtime(Mathf.Max(0.01f, velocidadCursor));
        }
    }

    private IEnumerator AnimarSpinner()
    {
        int index = 0;

        while (spinnerActivo)
        {
            string frameActual = framesCarga[index % framesCarga.Length];

            if (textoMensaje != null)
                textoMensaje.text = textoBaseActual + " " + frameActual;

            index++;

            yield return new WaitForSecondsRealtime(Mathf.Max(0.01f, velocidadSpinner));
        }
    }

    private IEnumerator VolverAInicioDespuesDeEspera()
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, tiempoAntesDeVolverInicio));
        rutinaVolverInicio = null;
        MostrarVistaInicial();
    }

    private void ActualizarTextoConCursor()
    {
        if (textoMensaje == null)
            return;

        textoMensaje.text = textoBaseActual + (cursorVisible ? cursor : "");
    }

    private void ActualizarTextoDirecto(string texto)
    {
        if (textoMensaje == null)
            return;

        textoMensaje.text = texto;
    }

    private void DetenerTodosLosEfectos()
    {
        cursorActivo = false;
        spinnerActivo = false;

        if (rutinaCursor != null)
        {
            StopCoroutine(rutinaCursor);
            rutinaCursor = null;
        }

        if (rutinaEscritura != null)
        {
            StopCoroutine(rutinaEscritura);
            rutinaEscritura = null;
        }

        if (rutinaSpinner != null)
        {
            StopCoroutine(rutinaSpinner);
            rutinaSpinner = null;
        }

        if (rutinaVolverInicio != null)
        {
            StopCoroutine(rutinaVolverInicio);
            rutinaVolverInicio = null;
        }

        cursorVisible = false;
    }

    private void DetenerRutinaActual()
    {
        if (rutinaActual == null)
            return;

        StopCoroutine(rutinaActual);
        rutinaActual = null;
    }

    private void UbicarFrenteALaCabeza()
    {
        if (headReference == null)
            return;

        Vector3 posicion = headReference.position + headReference.forward * distanciaFrenteCabeza;
        posicion += headReference.right * offsetFrenteCabeza.x;
        posicion += headReference.up * offsetFrenteCabeza.y;

        transform.position = posicion;

        MirarAlUsuarioInmediato();
    }

    private void MirarAlUsuario()
    {
        if (headReference == null)
            return;

        Vector3 direccion = transform.position - headReference.position;

        if (direccion.sqrMagnitude < 0.001f)
            return;

        Quaternion rotacionObjetivo = Quaternion.LookRotation(direccion.normalized, Vector3.up);

        if (invertirFrente)
            rotacionObjetivo *= Quaternion.Euler(0f, 180f, 0f);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            rotacionObjetivo,
            Time.unscaledDeltaTime * suavizadoRotacion
        );
    }

    private void MirarAlUsuarioInmediato()
    {
        if (headReference == null)
            return;

        Vector3 direccion = transform.position - headReference.position;

        if (direccion.sqrMagnitude < 0.001f)
            return;

        Quaternion rotacionObjetivo = Quaternion.LookRotation(direccion.normalized, Vector3.up);

        if (invertirFrente)
            rotacionObjetivo *= Quaternion.Euler(0f, 180f, 0f);

        transform.rotation = rotacionObjetivo;
    }

    private void SetCanvasGroup(CanvasGroup group, float alpha, bool interactivo)
    {
        if (group == null)
            return;

        group.alpha = alpha;
        group.interactable = interactivo;
        group.blocksRaycasts = interactivo;
    }
}
