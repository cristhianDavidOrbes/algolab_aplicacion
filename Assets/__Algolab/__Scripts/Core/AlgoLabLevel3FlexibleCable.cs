using UnityEngine;

/// <summary>
/// Cable visual con simulación Verlet: los extremos quedan unidos al monitor y
/// al cargador mientras los puntos intermedios responden a gravedad e inercia.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(LineRenderer))]
public class AlgoLabLevel3FlexibleCable : MonoBehaviour
{
    public Transform extremoMonitor;
    public Transform extremoCargador;
    [Range(6, 32)] public int segmentos = 16;
    [Range(1, 12)] public int iteraciones = 5;
    [Min(0.001f)] public float grosor = 0.012f;
    [Range(0f, 1f)] public float amortiguacion = 0.985f;
    public Vector3 gravedad = new Vector3(0f, -1.8f, 0f);
    public Color color = new Color(0.035f, 0.045f, 0.055f, 1f);

    private LineRenderer linea;
    private Vector3[] puntos;
    private Vector3[] anteriores;
    private float longitudSegmento;
    private Material materialRuntime;

    private void Awake()
    {
        linea = GetComponent<LineRenderer>();
        ConfigurarLinea();
    }

    private void OnEnable()
    {
        InicializarPuntos();
    }

    private void LateUpdate()
    {
        if (extremoMonitor == null || extremoCargador == null)
            return;
        if (puntos == null || puntos.Length != segmentos + 1)
            InicializarPuntos();

        Simular(Mathf.Min(Time.deltaTime, 0.033f));
        linea.positionCount = puntos.Length;
        linea.SetPositions(puntos);
    }

    public void ReiniciarCable()
    {
        InicializarPuntos();
    }

    private void ConfigurarLinea()
    {
        if (linea == null)
            return;

        linea.useWorldSpace = true;
        linea.startWidth = grosor;
        linea.endWidth = grosor;
        linea.numCapVertices = 4;
        linea.numCornerVertices = 3;
        linea.startColor = color;
        linea.endColor = color;

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");
        if (shader != null)
        {
            materialRuntime = new Material(shader)
            {
                name = "AlgoLab_CableRobot_Runtime"
            };
            if (materialRuntime.HasProperty("_BaseColor"))
                materialRuntime.SetColor("_BaseColor", color);
            if (materialRuntime.HasProperty("_Color"))
                materialRuntime.SetColor("_Color", color);
            linea.material = materialRuntime;
        }
    }

    private void InicializarPuntos()
    {
        if (extremoMonitor == null || extremoCargador == null)
            return;

        segmentos = Mathf.Clamp(segmentos, 6, 32);
        puntos = new Vector3[segmentos + 1];
        anteriores = new Vector3[puntos.Length];
        for (int i = 0; i < puntos.Length; i++)
        {
            float t = i / (float)segmentos;
            Vector3 p = Vector3.Lerp(
                extremoMonitor.position,
                extremoCargador.position,
                t
            );
            p.y -= Mathf.Sin(t * Mathf.PI) * 0.10f;
            puntos[i] = p;
            anteriores[i] = p;
        }
        longitudSegmento = Mathf.Max(
            0.005f,
            Vector3.Distance(extremoMonitor.position, extremoCargador.position) /
            segmentos
        );
    }

    private void Simular(float dt)
    {
        if (dt <= 0f)
            return;

        longitudSegmento = Mathf.Max(
            0.005f,
            Vector3.Distance(extremoMonitor.position, extremoCargador.position) /
            segmentos
        );
        float dt2 = dt * dt;

        for (int i = 1; i < puntos.Length - 1; i++)
        {
            Vector3 actual = puntos[i];
            Vector3 velocidad = (actual - anteriores[i]) * amortiguacion;
            puntos[i] = actual + velocidad + gravedad * dt2;
            anteriores[i] = actual;
        }

        for (int paso = 0; paso < iteraciones; paso++)
        {
            puntos[0] = extremoMonitor.position;
            puntos[puntos.Length - 1] = extremoCargador.position;

            for (int i = 0; i < puntos.Length - 1; i++)
            {
                Vector3 delta = puntos[i + 1] - puntos[i];
                float distancia = Mathf.Max(0.0001f, delta.magnitude);
                Vector3 correccion =
                    delta * ((distancia - longitudSegmento) / distancia);

                if (i == 0)
                {
                    puntos[i + 1] -= correccion;
                }
                else if (i + 1 == puntos.Length - 1)
                {
                    puntos[i] += correccion;
                }
                else
                {
                    puntos[i] += correccion * 0.5f;
                    puntos[i + 1] -= correccion * 0.5f;
                }
            }
        }
    }

    private void OnDestroy()
    {
        if (materialRuntime != null)
            Destroy(materialRuntime);
    }
}
