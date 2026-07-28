using UnityEngine;
using UnityEngine.EventSystems;

public class AlgoLabCircularLoginButton : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler
{
    [Header("Animación hover")]
    public float alturaHover = 18f;
    public float escalaHover = 1.12f;
    public float escalaPressed = 0.92f;
    public float velocidadAnimacion = 10f;

    [Header("Movimiento extra")]
    public bool moverHaciaAdelante = true;
    public float profundidadHover = -8f;

    [Header("Debug")]
    public bool mostrarDebug = false;

    private RectTransform rectTransform;
    private Vector3 posicionInicial;
    private Vector3 escalaInicial;

    private bool apuntando = false;
    private bool presionado = false;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        if (rectTransform != null)
        {
            posicionInicial = rectTransform.localPosition;
            escalaInicial = rectTransform.localScale;
        }
    }

    private void OnEnable()
    {
        apuntando = false;
        presionado = false;

        if (rectTransform != null)
        {
            rectTransform.localPosition = posicionInicial;
            rectTransform.localScale = escalaInicial;
        }
    }

    private void Update()
    {
        if (rectTransform == null)
        {
            return;
        }

        Vector3 posicionObjetivo = posicionInicial;
        Vector3 escalaObjetivo = escalaInicial;

        if (apuntando)
        {
            posicionObjetivo += Vector3.up * alturaHover;

            if (moverHaciaAdelante)
            {
                posicionObjetivo += Vector3.forward * profundidadHover;
            }

            escalaObjetivo = escalaInicial * escalaHover;
        }

        if (presionado)
        {
            escalaObjetivo = escalaInicial * escalaPressed;
        }

        rectTransform.localPosition = Vector3.Lerp(
            rectTransform.localPosition,
            posicionObjetivo,
            Time.deltaTime * velocidadAnimacion
        );

        rectTransform.localScale = Vector3.Lerp(
            rectTransform.localScale,
            escalaObjetivo,
            Time.deltaTime * velocidadAnimacion
        );
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        apuntando = true;

        if (mostrarDebug)
        {
            Debug.Log("BOTON LOGIN UI: apuntando " + gameObject.name);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        apuntando = false;
        presionado = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        presionado = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        presionado = false;
    }
}
