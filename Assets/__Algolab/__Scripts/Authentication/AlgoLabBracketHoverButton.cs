using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class AlgoLabBracketHoverButton : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler
{
    [Header("Texto")]
    public TMP_Text textoBoton;
    public string textoBase = "Continuar";

    [Header("Formato")]
    public string formatoNormal = "[ {0} ]";
    public string formatoHover = "[{0}]";
    public string formatoPresionado = "> {0} <";

    [Header("Color del texto")]
    public bool cambiarColorTexto = true;
    public Color colorTextoNormal = Color.white;
    public Color colorTextoHover = Color.cyan;
    public Color colorTextoPresionado = Color.green;

    [Header("Fondo invisible")]
    public bool ocultarFondoBoton = true;

    [Tooltip("Aunque sea invisible, el Image queda activo para detectar el rayo.")]
    [Range(0f, 1f)]
    public float alphaFondo = 0f;

    [Header("Tamaño fijo")]
    public bool usarTamanioFijo = true;
    public float anchoBoton = 210f;
    public float altoBoton = 55f;
    public float fontSize = 24f;

    [Header("Animación")]
    [Tooltip("Déjalo desactivado si el botón está dentro de Horizontal Layout Group.")]
    public bool animarTransform = false;

    public float escalaHover = 1.04f;
    public float escalaPresionado = 0.96f;
    public float velocidadAnimacion = 10f;

    private RectTransform rectTransform;
    private Image imageBoton;
    private Button button;
    private Vector3 escalaInicial;

    private bool apuntando;
    private bool presionado;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        imageBoton = GetComponent<Image>();
        button = GetComponent<Button>();

        if (textoBoton == null)
        {
            textoBoton = GetComponentInChildren<TMP_Text>(true);
        }

        if (rectTransform != null)
        {
            escalaInicial = rectTransform.localScale;
        }

        ConfigurarVisual();
        ActualizarVisual();
    }

    private void OnEnable()
    {
        apuntando = false;
        presionado = false;

        if (rectTransform != null)
        {
            rectTransform.localScale = escalaInicial;
        }

        ConfigurarVisual();
        ActualizarVisual();
    }

    private void Update()
    {
        if (!animarTransform || rectTransform == null)
        {
            return;
        }

        Vector3 escalaObjetivo = escalaInicial;

        if (apuntando)
        {
            escalaObjetivo = escalaInicial * escalaHover;
        }

        if (presionado)
        {
            escalaObjetivo = escalaInicial * escalaPresionado;
        }

        rectTransform.localScale = Vector3.Lerp(
            rectTransform.localScale,
            escalaObjetivo,
            Time.deltaTime * velocidadAnimacion
        );
    }

    private void ConfigurarVisual()
    {
        ConfigurarFondoInvisible();
        ConfigurarTexto();
        ConfigurarTamanioFijo();
    }

    private void ConfigurarFondoInvisible()
    {
        if (imageBoton == null)
        {
            imageBoton = GetComponent<Image>();
        }

        if (imageBoton != null && ocultarFondoBoton)
        {
            Color color = imageBoton.color;
            color.a = alphaFondo;
            imageBoton.color = color;

            // Importante: se deja activado para que el rayo detecte el botón.
            imageBoton.raycastTarget = true;
        }

        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (button != null)
        {
            // Esto evita que Unity vuelva a poner el botón blanco al apuntarlo.
            button.transition = Selectable.Transition.None;
            button.interactable = true;
        }
    }

    private void ConfigurarTexto()
    {
        if (textoBoton == null)
        {
            textoBoton = GetComponentInChildren<TMP_Text>(true);
        }

        if (textoBoton == null)
        {
            return;
        }

        textoBoton.enableAutoSizing = false;
        textoBoton.fontSize = fontSize;
        textoBoton.alignment = TextAlignmentOptions.Center;
        textoBoton.raycastTarget = false;

        // Asegura que el texto nunca quede invisible.
        Color color = colorTextoNormal;
        color.a = 1f;
        textoBoton.color = color;
    }

    private void ConfigurarTamanioFijo()
    {
        if (!usarTamanioFijo)
        {
            return;
        }

        LayoutElement layoutElement = GetComponent<LayoutElement>();

        if (layoutElement == null)
        {
            layoutElement = gameObject.AddComponent<LayoutElement>();
        }

        layoutElement.minWidth = anchoBoton;
        layoutElement.minHeight = altoBoton;
        layoutElement.preferredWidth = anchoBoton;
        layoutElement.preferredHeight = altoBoton;
        layoutElement.flexibleWidth = 0f;
        layoutElement.flexibleHeight = 0f;

        if (rectTransform != null)
        {
            rectTransform.sizeDelta = new Vector2(anchoBoton, altoBoton);
        }

        ContentSizeFitter fitter = GetComponent<ContentSizeFitter>();

        if (fitter != null)
        {
            fitter.enabled = false;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        apuntando = true;
        ActualizarVisual();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        apuntando = false;
        presionado = false;
        ActualizarVisual();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        presionado = true;
        ActualizarVisual();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        presionado = false;
        ActualizarVisual();
    }

    private void ActualizarVisual()
    {
        if (textoBoton == null)
        {
            return;
        }

        if (presionado)
        {
            textoBoton.text = string.Format(formatoPresionado, textoBase);

            if (cambiarColorTexto)
            {
                Color c = colorTextoPresionado;
                c.a = 1f;
                textoBoton.color = c;
            }

            return;
        }

        if (apuntando)
        {
            textoBoton.text = string.Format(formatoHover, textoBase);

            if (cambiarColorTexto)
            {
                Color c = colorTextoHover;
                c.a = 1f;
                textoBoton.color = c;
            }

            return;
        }

        textoBoton.text = string.Format(formatoNormal, textoBase);

        if (cambiarColorTexto)
        {
            Color c = colorTextoNormal;
            c.a = 1f;
            textoBoton.color = c;
        }
    }
}