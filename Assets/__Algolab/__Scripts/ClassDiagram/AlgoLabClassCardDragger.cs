using UnityEngine;

public class AlgoLabClassCardDragger : MonoBehaviour
{
    [Header("Contenedor donde están las tarjetas")]
    public RectTransform cardsContainer;

    [Header("Área real del panel")]
    public RectTransform panelArea;

    [Header("Rayos de los controles")]
    public Transform leftRayOrigin;
    public Transform rightRayOrigin;

    [Header("Botones para arrastrar")]
    public OVRInput.Button botonArrastrarIzquierda = OVRInput.Button.PrimaryIndexTrigger;
    public OVRInput.Button botonArrastrarDerecha = OVRInput.Button.SecondaryIndexTrigger;

    [Header("Configuración")]
    public float distanciaMaxima = 5f;
    public bool limitarDentroDelPanel = true;
    public float margenInterno = 10f;

    [Header("Debug")]
    public bool mostrarDebug = false;

    private RectTransform tarjetaArrastrada;
    private Transform rayOriginActivo;

    private Vector2 offsetPanelLocal;
    private bool arrastrando;

    private LadoActivo ladoActivo = LadoActivo.Ninguno;

    private enum LadoActivo
    {
        Ninguno,
        Izquierdo,
        Derecho
    }

    private void Awake()
    {
        if (panelArea == null)
        {
            panelArea = cardsContainer;
        }
    }

    private void Update()
    {
        if (!arrastrando)
        {
            IntentarIniciarArrastre();
        }
        else
        {
            ActualizarArrastre();
        }
    }

    private void IntentarIniciarArrastre()
    {
        if (cardsContainer == null || panelArea == null)
        {
            return;
        }

        if (leftRayOrigin != null && OVRInput.GetDown(botonArrastrarIzquierda))
        {
            if (IntentarTomarTarjeta(leftRayOrigin, LadoActivo.Izquierdo))
            {
                return;
            }
        }

        if (rightRayOrigin != null && OVRInput.GetDown(botonArrastrarDerecha))
        {
            IntentarTomarTarjeta(rightRayOrigin, LadoActivo.Derecho);
        }
    }

    private bool IntentarTomarTarjeta(Transform rayOrigin, LadoActivo lado)
    {
        if (!ObtenerPuntoEnPanel(rayOrigin, out Vector3 puntoMundo, out Vector2 puntoPanelLocal))
        {
            return false;
        }

        RectTransform tarjeta = BuscarTarjetaBajoPunto(puntoMundo);

        if (tarjeta == null)
        {
            if (mostrarDebug)
            {
                Debug.Log("No hay tarjeta bajo el rayo.");
            }

            return false;
        }

        tarjetaArrastrada = tarjeta;
        rayOriginActivo = rayOrigin;
        ladoActivo = lado;
        arrastrando = true;

        Vector3 tarjetaPanelLocal3D = panelArea.InverseTransformPoint(tarjetaArrastrada.position);
        Vector2 tarjetaPanelLocal = new Vector2(tarjetaPanelLocal3D.x, tarjetaPanelLocal3D.y);

        offsetPanelLocal = tarjetaPanelLocal - puntoPanelLocal;

        tarjetaArrastrada.SetAsLastSibling();

        if (mostrarDebug)
        {
            Debug.Log("Arrastrando tarjeta: " + tarjetaArrastrada.name);
        }

        return true;
    }

    private void ActualizarArrastre()
    {
        if (tarjetaArrastrada == null || rayOriginActivo == null)
        {
            TerminarArrastre();
            return;
        }

        if (!BotonSiguePresionado())
        {
            TerminarArrastre();
            return;
        }

        if (!ObtenerPuntoEnPanel(rayOriginActivo, out Vector3 puntoMundo, out Vector2 puntoPanelLocal))
        {
            return;
        }

        Vector2 nuevaPosicionPanelLocal = puntoPanelLocal + offsetPanelLocal;

        if (limitarDentroDelPanel)
        {
            nuevaPosicionPanelLocal = LimitarDentroDelPanel(tarjetaArrastrada, nuevaPosicionPanelLocal);
        }

        Vector3 nuevaPosicionMundo = panelArea.TransformPoint(
            new Vector3(nuevaPosicionPanelLocal.x, nuevaPosicionPanelLocal.y, 0f)
        );

        tarjetaArrastrada.position = nuevaPosicionMundo;
    }

    private bool BotonSiguePresionado()
    {
        if (ladoActivo == LadoActivo.Izquierdo)
        {
            return OVRInput.Get(botonArrastrarIzquierda);
        }

        if (ladoActivo == LadoActivo.Derecho)
        {
            return OVRInput.Get(botonArrastrarDerecha);
        }

        return false;
    }

    private void TerminarArrastre()
    {
        if (mostrarDebug && tarjetaArrastrada != null)
        {
            Debug.Log("Tarjeta soltada: " + tarjetaArrastrada.name);
        }

        tarjetaArrastrada = null;
        rayOriginActivo = null;
        ladoActivo = LadoActivo.Ninguno;
        arrastrando = false;
    }

    private bool ObtenerPuntoEnPanel(
        Transform rayOrigin,
        out Vector3 puntoMundo,
        out Vector2 puntoPanelLocal)
    {
        puntoMundo = Vector3.zero;
        puntoPanelLocal = Vector2.zero;

        if (rayOrigin == null || panelArea == null)
        {
            return false;
        }

        Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);

        Plane planoPanel = new Plane(panelArea.forward, panelArea.position);

        if (!planoPanel.Raycast(ray, out float distancia))
        {
            return false;
        }

        if (distancia < 0f || distancia > distanciaMaxima)
        {
            return false;
        }

        puntoMundo = ray.GetPoint(distancia);

        Vector3 local3D = panelArea.InverseTransformPoint(puntoMundo);
        puntoPanelLocal = new Vector2(local3D.x, local3D.y);

        return true;
    }

    private RectTransform BuscarTarjetaBajoPunto(Vector3 puntoMundo)
    {
        if (cardsContainer == null)
        {
            return null;
        }

        for (int i = cardsContainer.childCount - 1; i >= 0; i--)
        {
            RectTransform tarjeta = cardsContainer.GetChild(i) as RectTransform;

            if (tarjeta == null || !tarjeta.gameObject.activeInHierarchy)
            {
                continue;
            }

            Vector3 puntoLocalTarjeta3D = tarjeta.InverseTransformPoint(puntoMundo);
            Vector2 puntoLocalTarjeta = new Vector2(puntoLocalTarjeta3D.x, puntoLocalTarjeta3D.y);

            if (tarjeta.rect.Contains(puntoLocalTarjeta))
            {
                return tarjeta;
            }
        }

        return null;
    }

    private Vector2 LimitarDentroDelPanel(RectTransform tarjeta, Vector2 posicionPanelLocal)
    {
        Rect rectPanel = panelArea.rect;
        Rect rectTarjeta = tarjeta.rect;

        Vector2 pivot = tarjeta.pivot;

        float minX = rectPanel.xMin + margenInterno + rectTarjeta.width * pivot.x;
        float maxX = rectPanel.xMax - margenInterno - rectTarjeta.width * (1f - pivot.x);

        float minY = rectPanel.yMin + margenInterno + rectTarjeta.height * pivot.y;
        float maxY = rectPanel.yMax - margenInterno - rectTarjeta.height * (1f - pivot.y);

        posicionPanelLocal.x = Mathf.Clamp(posicionPanelLocal.x, minX, maxX);
        posicionPanelLocal.y = Mathf.Clamp(posicionPanelLocal.y, minY, maxY);

        return posicionPanelLocal;
    }
}