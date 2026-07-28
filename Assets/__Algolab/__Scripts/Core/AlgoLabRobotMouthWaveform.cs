using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Dibuja la boca del robot como una linea negra. Mientras la voz TTS esta
/// sonando, la linea reacciona al audio con picos bajos y altos.
/// </summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class AlgoLabRobotMouthWaveform : MaskableGraphic
{
    [Range(5, 21)] public int puntos = 13;
    [Min(0.02f)] public float grosor = 0.34f;
    [Range(0.05f, 0.60f)] public float amplitudMaxima = 0.52f;
    [Min(1f)] public float velocidad = 14f;

    private readonly float[] muestras = new float[128];
    private AudioSource fuente;
    private float amplitudSuavizada;
    private bool hablandoForzado;

    public bool EstaHablando =>
        hablandoForzado ||
        (fuente != null && fuente.isPlaying && fuente.volume > 0.015f);

    protected override void Awake()
    {
        base.Awake();
        raycastTarget = false;
        color = Color.black;
    }

    public void ComenzarHablar(AudioSource nuevaFuente)
    {
        fuente = nuevaFuente;
        hablandoForzado = true;
        amplitudSuavizada = Mathf.Max(amplitudSuavizada, 0.72f);
        SetVerticesDirty();
    }

    public void VincularFuente(AudioSource nuevaFuente)
    {
        ComenzarHablar(nuevaFuente);
    }

    public void Reposo()
    {
        fuente = null;
        hablandoForzado = false;
        amplitudSuavizada = 0f;
        SetVerticesDirty();
    }

    private void Update()
    {
        float objetivo = 0f;
        if (fuente != null && fuente.isPlaying)
        {
            fuente.GetOutputData(muestras, 0);
            float suma = 0f;
            for (int i = 0; i < muestras.Length; i++)
                suma += muestras[i] * muestras[i];
            objetivo = Mathf.Clamp01(Mathf.Sqrt(suma / muestras.Length) * 9f);

            // Algunos dispositivos no entregan muestras del mezclador a
            // tiempo. Mantiene una onda discreta mientras el clip si suena.
            if (objetivo < 0.025f)
                objetivo = 0.72f;
        }
        else if (hablandoForzado)
        {
            // El evento de inicio de TTS puede llegar uno o dos cuadros antes
            // de que AudioSource.isPlaying cambie en Android. La boca debe
            // responder desde el primer instante visible.
            objetivo = 0.78f;
        }

        amplitudSuavizada = Mathf.MoveTowards(
            amplitudSuavizada,
            objetivo,
            Time.unscaledDeltaTime * 8.5f
        );
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect rect = rectTransform.rect;
        int total = Mathf.Max(5, puntos | 1);
        float anchoPaso = rect.width / (total - 1);
        float mitadGrosor = Mathf.Max(0.01f, grosor) * 0.5f;
        bool hablando = EstaHablando || amplitudSuavizada > 0.025f;

        Vector2[] linea = new Vector2[total];
        for (int i = 0; i < total; i++)
        {
            float x = rect.xMin + anchoPaso * i;
            float y = rect.center.y;
            if (hablando && i > 0 && i < total - 1)
            {
                float patron = i % 6 == 0
                    ? 1f
                    : i % 3 == 0
                        ? -0.94f
                        : i % 2 == 0 ? 0.62f : -0.72f;
                float pulso = 0.78f +
                    Mathf.Sin(Time.unscaledTime * velocidad + i * 1.73f) * 0.22f;
                y += rect.height * amplitudMaxima *
                    amplitudSuavizada * patron * pulso;
            }
            linea[i] = new Vector2(x, y);
        }

        for (int i = 0; i < total - 1; i++)
            AgregarSegmento(vh, linea[i], linea[i + 1], mitadGrosor);
    }

    private void AgregarSegmento(
        VertexHelper vh,
        Vector2 inicio,
        Vector2 fin,
        float mitadGrosor)
    {
        Vector2 direccion = (fin - inicio).normalized;
        Vector2 normal = new Vector2(-direccion.y, direccion.x) * mitadGrosor;
        int baseVertice = vh.currentVertCount;
        Color32 tono = color;
        vh.AddVert(inicio - normal, tono, Vector2.zero);
        vh.AddVert(inicio + normal, tono, Vector2.up);
        vh.AddVert(fin + normal, tono, Vector2.one);
        vh.AddVert(fin - normal, tono, Vector2.right);
        vh.AddTriangle(baseVertice, baseVertice + 1, baseVertice + 2);
        vh.AddTriangle(baseVertice, baseVertice + 2, baseVertice + 3);
    }
}
