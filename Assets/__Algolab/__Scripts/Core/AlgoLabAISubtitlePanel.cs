using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public class AlgoLabAISubtitlePanel : MonoBehaviour
{
    [Header("Control de subtítulos")]
    [SerializeField] private bool subtitulosActivados = true;
    [SerializeField] private bool mostrarErroresAunqueSubtitulosEstenDesactivados = true;
    [SerializeField] private bool ocultarAlDesactivar = true;

    [Header("Referencias")]
    [SerializeField] private RectTransform panelRect;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text textoIA;

    [Header("Posición local frente a la cabeza")]
    [SerializeField] private Vector3 localPosition = new Vector3(0f, -0.10f, 0.55f);
    [SerializeField] private Vector3 localRotation = Vector3.zero;

    [Header("Animación de aparición")]
    [SerializeField] private float initialScale = 0.12f;
    [SerializeField] private float finalScale = 0.75f;
    [SerializeField] private float appearDuration = 0.55f;
    [SerializeField] private float waitBeforeTyping = 0.5f;
    [SerializeField] private float disappearDuration = 0.35f;

    [Header("Escritura")]
    [SerializeField] private float charDelay = 0.025f;
    [SerializeField] private float cursorBlinkSpeed = 0.35f;
    [SerializeField] private string cursorSymbol = "|";
    [SerializeField] private float waitBetweenPages = 4.5f;

    [Header("Límites visuales")]
    [SerializeField] private int maxCaracteresPorBloque = 68;
    [SerializeField] private int maxLineasVisibles = 2;

    [Header("Mensajes de error")]
    [SerializeField] private string mensajeIAError = "La IA no pudo responder en este momento.";
    [SerializeField] private string mensajeIAFueraServicio = "La IA no está disponible en este momento.";
    [SerializeField] private string mensajeRespuestaVacia = "La IA no generó una respuesta válida.";
    [SerializeField] private string mensajeTimeout = "La IA tardó demasiado en responder.";

    [Header("Debug")]
    [SerializeField] private bool mostrarLogsDebug = true;

    [Header("Prueba")]
    [TextArea(4, 10)]
    [SerializeField]
    private string testMessage =
        "Hola. Este panel muestra subtítulos de la IA. " +
        "El texto se divide en bloques cortos para que no se salga. " +
        "Además, el cursor sigue parpadeando mientras espera el siguiente bloque.";

    private Coroutine subtitleRoutine;
    private Coroutine cursorRoutine;

    private bool cursorVisible;
    private bool cursorParpadeando;
    private string textoBaseActual = "";

    public bool SubtitulosActivados => subtitulosActivados;

    private void Awake()
    {
        PrepararPosicionLocal();
        ConfigurarTexto();
        HideImmediate();
    }

    private void OnEnable()
    {
        PrepararPosicionLocal();
        ConfigurarTexto();
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        subtitleRoutine = null;
        cursorRoutine = null;
        cursorParpadeando = false;
        cursorVisible = false;

        if (ocultarAlDesactivar)
            HideImmediate();
    }

    [ContextMenu("Probar subtítulo")]
    public void TestSubtitle()
    {
        ShowSubtitle(testMessage);
    }

    [ContextMenu("Probar error IA")]
    public void TestErrorSubtitle()
    {
        ShowErrorSubtitle(mensajeIAError);
    }

    [ContextMenu("Activar subtítulos")]
    public void ActivarSubtitulos()
    {
        SetSubtitulosActivos(true);
    }

    [ContextMenu("Desactivar subtítulos")]
    public void DesactivarSubtitulos()
    {
        SetSubtitulosActivos(false);
    }

    public void ToggleSubtitulos()
    {
        SetSubtitulosActivos(!subtitulosActivados);
    }

    public void SetSubtitulosActivos(bool activo)
    {
        subtitulosActivados = activo;

        if (mostrarLogsDebug)
        {
            Debug.Log("Subtítulos IA activos: " + subtitulosActivados);
        }

        if (!subtitulosActivados && ocultarAlDesactivar)
        {
            HideSubtitleNow();
        }
    }

    public void ShowSubtitle(string fullMessage)
    {
        if (!subtitulosActivados)
        {
            if (mostrarLogsDebug)
            {
                Debug.Log("Subtítulos desactivados. No se muestra el mensaje.");
            }

            return;
        }

        MostrarMensajeInterno(fullMessage, false);
    }

    public void ShowErrorSubtitle(string errorMessage)
    {
        if (!subtitulosActivados && !mostrarErroresAunqueSubtitulosEstenDesactivados)
        {
            if (mostrarLogsDebug)
            {
                Debug.Log("Subtítulos y errores visuales desactivados.");
            }

            return;
        }

        if (string.IsNullOrWhiteSpace(errorMessage))
        {
            errorMessage = mensajeIAError;
        }

        string mensajeFinal = "ERROR: " + errorMessage + " Terminando proceso.";

        MostrarMensajeInterno(mensajeFinal, true);
    }

    public void ShowIAUnavailable()
    {
        ShowErrorSubtitle(mensajeIAFueraServicio);
    }

    public void ShowEmptyResponse()
    {
        ShowErrorSubtitle(mensajeRespuestaVacia);
    }

    public void ShowTimeout()
    {
        ShowErrorSubtitle(mensajeTimeout);
    }

    public void ShowSystemSubtitle(string mensaje)
    {
        if (!subtitulosActivados)
        {
            return;
        }

        MostrarMensajeInterno(mensaje, false);
    }

    public void HideSubtitleNow()
    {
        if (subtitleRoutine != null && isActiveAndEnabled)
        {
            StopCoroutine(subtitleRoutine);
        }

        subtitleRoutine = null;

        DetenerCursor();
        HideImmediate();
    }

    private void MostrarMensajeInterno(string fullMessage, bool esError)
    {
        if (string.IsNullOrWhiteSpace(fullMessage))
        {
            if (esError)
            {
                fullMessage = mensajeIAError;
            }
            else
            {
                Debug.LogWarning("No se puede mostrar subtítulo porque el texto está vacío.");
                return;
            }
        }

        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        PrepararPosicionLocal();
        ConfigurarTexto();

        if (subtitleRoutine != null)
        {
            StopCoroutine(subtitleRoutine);
            subtitleRoutine = null;
        }

        DetenerCursor();

        if (mostrarLogsDebug)
        {
            Debug.Log("AISubtitlePanel recibió texto: " + fullMessage);
        }

        subtitleRoutine = StartCoroutine(SubtitleSequence(fullMessage));
    }

    private IEnumerator SubtitleSequence(string fullMessage)
    {
        PrepararPosicionLocal();
        ConfigurarTexto();

        textoBaseActual = "";
        cursorVisible = false;

        if (textoIA != null)
        {
            textoIA.text = "";
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (panelRect != null)
        {
            panelRect.localScale = Vector3.one * initialScale;
        }

        yield return AnimateAppear();

        yield return new WaitForSecondsRealtime(Mathf.Max(0f, waitBeforeTyping));

        List<string> bloques = DividirEnBloques(fullMessage);

        for (int i = 0; i < bloques.Count; i++)
        {
            yield return EscribirBloque(bloques[i]);

            yield return new WaitForSecondsRealtime(Mathf.Max(0f, waitBetweenPages));
        }

        DetenerCursor();

        yield return AnimateDisappear();

        HideImmediate();
        subtitleRoutine = null;
    }

    private void PrepararPosicionLocal()
    {
        transform.localPosition = localPosition;
        transform.localRotation = Quaternion.Euler(localRotation);
        transform.localScale = Vector3.one;
    }

    private void ConfigurarTexto()
    {
        if (textoIA == null)
        {
            return;
        }

        textoIA.textWrappingMode = TextWrappingModes.Normal;
        textoIA.overflowMode = TextOverflowModes.Truncate;
        textoIA.maxVisibleLines = maxLineasVisibles;
    }

    private IEnumerator AnimateAppear()
    {
        float time = 0f;

        while (time < appearDuration)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / appearDuration);
            float eased = EaseOutBack(t);

            if (canvasGroup != null)
            {
                canvasGroup.alpha = t;
            }

            if (panelRect != null)
            {
                panelRect.localScale = Vector3.one * Mathf.LerpUnclamped(
                    initialScale,
                    finalScale,
                    eased
                );
            }

            yield return null;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        if (panelRect != null)
        {
            panelRect.localScale = Vector3.one * finalScale;
        }
    }

    private IEnumerator AnimateDisappear()
    {
        float time = 0f;

        Vector3 startScale = panelRect != null ? panelRect.localScale : Vector3.one;
        float startAlpha = canvasGroup != null ? canvasGroup.alpha : 1f;

        while (time < disappearDuration)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / disappearDuration);
            float eased = EaseInOut(t);

            if (canvasGroup != null)
            {
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, eased);
            }

            if (panelRect != null)
            {
                panelRect.localScale = Vector3.Lerp(
                    startScale,
                    Vector3.one * initialScale,
                    eased
                );
            }

            yield return null;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (panelRect != null)
        {
            panelRect.localScale = Vector3.one * initialScale;
        }
    }

    private IEnumerator EscribirBloque(string bloque)
    {
        textoBaseActual = "";
        cursorVisible = true;

        IniciarCursor();

        for (int i = 0; i < bloque.Length; i++)
        {
            textoBaseActual += bloque[i];
            ActualizarTextoConCursor();

            yield return new WaitForSecondsRealtime(Mathf.Max(0f, charDelay));
        }

        ActualizarTextoConCursor();
    }

    private void IniciarCursor()
    {
        if (cursorRoutine != null)
        {
            StopCoroutine(cursorRoutine);
            cursorRoutine = null;
        }

        cursorParpadeando = true;
        cursorRoutine = StartCoroutine(CursorParpadeo());
    }

    private void DetenerCursor()
    {
        cursorParpadeando = false;

        if (cursorRoutine != null)
        {
            StopCoroutine(cursorRoutine);
            cursorRoutine = null;
        }

        cursorVisible = false;
        ActualizarTextoConCursor();
    }

    private IEnumerator CursorParpadeo()
    {
        while (cursorParpadeando)
        {
            cursorVisible = !cursorVisible;
            ActualizarTextoConCursor();

            yield return new WaitForSecondsRealtime(Mathf.Max(0.01f, cursorBlinkSpeed));
        }
    }

    private void ActualizarTextoConCursor()
    {
        if (textoIA == null)
        {
            return;
        }

        textoIA.text = textoBaseActual + (cursorVisible ? cursorSymbol : "");
    }

    private List<string> DividirEnBloques(string textoCompleto)
    {
        List<string> bloques = new List<string>();

        if (string.IsNullOrWhiteSpace(textoCompleto))
        {
            return bloques;
        }

        string textoLimpio = LimpiarTexto(textoCompleto);
        string[] palabras = textoLimpio.Split(' ');

        StringBuilder bloqueActual = new StringBuilder();

        foreach (string palabra in palabras)
        {
            if (string.IsNullOrWhiteSpace(palabra))
            {
                continue;
            }

            int longitudNueva = bloqueActual.Length + palabra.Length + 1;

            bool bloqueLleno = longitudNueva > maxCaracteresPorBloque;
            bool terminaEnPunto =
                bloqueActual.ToString().EndsWith(".") ||
                bloqueActual.ToString().EndsWith("?") ||
                bloqueActual.ToString().EndsWith("!");

            if (bloqueLleno && bloqueActual.Length > 0)
            {
                bloques.Add(bloqueActual.ToString().Trim());
                bloqueActual.Clear();
            }

            bloqueActual.Append(palabra).Append(" ");

            if (terminaEnPunto && bloqueActual.Length >= maxCaracteresPorBloque * 0.65f)
            {
                bloques.Add(bloqueActual.ToString().Trim());
                bloqueActual.Clear();
            }
        }

        if (bloqueActual.Length > 0)
        {
            bloques.Add(bloqueActual.ToString().Trim());
        }

        return bloques;
    }

    private string LimpiarTexto(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return "";
        }

        string limpio = texto.Replace("\n", " ");
        limpio = limpio.Replace("\r", " ");

        while (limpio.Contains("  "))
        {
            limpio = limpio.Replace("  ", " ");
        }

        return limpio.Trim();
    }

    private void HideImmediate()
    {
        cursorVisible = false;
        cursorParpadeando = false;
        textoBaseActual = "";

        if (textoIA != null)
        {
            textoIA.text = "";
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (panelRect != null)
        {
            panelRect.localScale = Vector3.one * initialScale;
        }
    }

    private float EaseOutBack(float x)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;

        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }

    private float EaseInOut(float x)
    {
        return x < 0.5f
            ? 2f * x * x
            : 1f - Mathf.Pow(-2f * x + 2f, 2f) / 2f;
    }
}
