using UnityEngine;

public class AlgoLabHeightGuideRings : MonoBehaviour
{
    [Header("Geometria")]
    public float radio = 0.78f;
    public float grosor = 0.012f;
    [Range(32, 160)] public int segmentos = 96;

    [Header("Transicion")]
    public float tiempoVisibleDespuesCambio = 1.1f;
    public float duracionAparecer = 0.18f;
    public float duracionDesvanecer = 0.45f;

    [Header("Colores")]
    public Color colorSentado = new Color(0.11f, 0.76f, 0.88f, 0.9f);
    public Color colorParado = new Color(1f, 0.66f, 0.20f, 0.9f);

    private AlgoLabGameSettings ajustes;
    private Transform cabeza;
    private Canvas canvasConfiguracion;
    private LineRenderer aroSentado;
    private LineRenderer aroParado;
    private Material materialRuntime;
    private float alphaActual;
    private float visibleHasta = -999f;

    public void Configurar(
        AlgoLabGameSettings nuevosAjustes,
        Transform nuevaCabeza,
        Canvas nuevoCanvas)
    {
        ajustes = nuevosAjustes;
        cabeza = nuevaCabeza;
        canvasConfiguracion = nuevoCanvas;
        PrepararAros();
        AplicarOrdenDibujo();
        ActualizarGeometria();
        AplicarAlpha(alphaActual);
    }

    public void NotificarCambioAltura()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        PrepararReferencias();
        PrepararAros();
        visibleHasta = Time.unscaledTime + Mathf.Max(0.1f, tiempoVisibleDespuesCambio);
        ActualizarGeometria();
    }

    public void OcultarSuavemente()
    {
        visibleHasta = -999f;
    }

    private void Update()
    {
        PrepararReferencias();

        if (ajustes == null || cabeza == null)
        {
            AplicarAlpha(0f);
            return;
        }

        PrepararAros();
        ActualizarGeometria();

        bool debeVerse = Time.unscaledTime <= visibleHasta;
        float objetivo = debeVerse ? 1f : 0f;
        float duracion = debeVerse ? duracionAparecer : duracionDesvanecer;
        float paso = duracion <= 0.001f
            ? 1f
            : Time.unscaledDeltaTime / duracion;

        alphaActual = Mathf.MoveTowards(alphaActual, objetivo, paso);
        AplicarAlpha(alphaActual);
    }

    private void PrepararReferencias()
    {
        if (ajustes == null)
        {
            ajustes = AlgoLabGameSettings.Instance;
        }

        if (cabeza == null && Camera.main != null)
        {
            cabeza = Camera.main.transform;
        }
    }

    private void PrepararAros()
    {
        if (materialRuntime == null)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                materialRuntime = new Material(shader)
                {
                    name = "AlgoLab Height Rings Runtime"
                };
            }
        }

        aroSentado = PrepararAro(aroSentado, "AroAlturaSentado");
        aroParado = PrepararAro(aroParado, "AroAlturaParado");
        AplicarOrdenDibujo();
    }

    private LineRenderer PrepararAro(LineRenderer aro, string nombre)
    {
        if (aro == null)
        {
            Transform existente = transform.Find(nombre);
            if (existente != null)
            {
                aro = existente.GetComponent<LineRenderer>();
            }
        }

        if (aro == null)
        {
            GameObject root = new GameObject(nombre);
            root.transform.SetParent(transform, false);
            aro = root.AddComponent<LineRenderer>();
        }

        aro.useWorldSpace = true;
        aro.loop = true;
        aro.positionCount = Mathf.Clamp(segmentos, 32, 160);
        aro.startWidth = Mathf.Max(0.002f, grosor);
        aro.endWidth = Mathf.Max(0.002f, grosor);
        aro.numCornerVertices = 4;
        aro.numCapVertices = 2;
        aro.textureMode = LineTextureMode.Stretch;
        if (materialRuntime != null)
        {
            aro.sharedMaterial = materialRuntime;
        }

        return aro;
    }

    private void AplicarOrdenDibujo()
    {
        int orden = canvasConfiguracion != null ? canvasConfiguracion.sortingOrder - 2 : 78;
        int capa = canvasConfiguracion != null ? canvasConfiguracion.sortingLayerID : 0;

        AplicarOrdenAro(aroSentado, capa, orden);
        AplicarOrdenAro(aroParado, capa, orden);
    }

    private static void AplicarOrdenAro(LineRenderer aro, int capa, int orden)
    {
        if (aro == null)
        {
            return;
        }

        aro.sortingLayerID = capa;
        aro.sortingOrder = orden;
    }

    private void ActualizarGeometria()
    {
        if (ajustes == null || cabeza == null || aroSentado == null || aroParado == null)
        {
            return;
        }

        Vector3 centro = cabeza.position;
        ActualizarAro(aroSentado, centro, ajustes.AlturaSentado);
        ActualizarAro(aroParado, centro, ajustes.AlturaParado);
    }

    private void ActualizarAro(LineRenderer aro, Vector3 centro, float altura)
    {
        int cantidad = aro.positionCount;
        float radioSeguro = Mathf.Max(0.2f, radio);

        for (int i = 0; i < cantidad; i++)
        {
            float angulo = i * Mathf.PI * 2f / cantidad;
            aro.SetPosition(
                i,
                new Vector3(
                    centro.x + Mathf.Cos(angulo) * radioSeguro,
                    altura,
                    centro.z + Mathf.Sin(angulo) * radioSeguro
                )
            );
        }
    }

    private void AplicarAlpha(float alpha)
    {
        AplicarColor(aroSentado, colorSentado, alpha);
        AplicarColor(aroParado, colorParado, alpha);
    }

    private static void AplicarColor(LineRenderer aro, Color baseColor, float alpha)
    {
        if (aro == null)
        {
            return;
        }

        Color color = baseColor;
        color.a *= Mathf.Clamp01(alpha);
        aro.startColor = color;
        aro.endColor = color;
        aro.enabled = alpha > 0.001f;
    }

    private void OnDestroy()
    {
        if (materialRuntime != null)
        {
            if (Application.isPlaying)
            {
                Destroy(materialRuntime);
            }
            else
            {
                DestroyImmediate(materialRuntime);
            }
        }
    }
}
