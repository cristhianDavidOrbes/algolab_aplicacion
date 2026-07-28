using System.Collections;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class AlgoLabTitleWaveAnimator : MonoBehaviour
{
    [Header("Animación de ola")]
    public bool reproducirAlActivar = true;

    [Tooltip("Cada cuántos segundos se repite la ola.")]
    public float intervaloSegundos = 10f;

    [Tooltip("Cuánto dura cada animación de ola.")]
    public float duracionOla = 1.5f;

    [Tooltip("Altura de la ola en el texto.")]
    public float amplitud = 12f;

    [Tooltip("Velocidad interna de la onda.")]
    public float velocidadOla = 7f;

    [Tooltip("Separación de la onda entre letra y letra.")]
    public float separacionEntreLetras = 0.45f;

    [Header("Inicio")]
    public float esperaInicial = 1f;

    [Header("Debug")]
    public bool mostrarDebug = false;

    private TMP_Text texto;
    private Coroutine rutinaOla;

    private void Awake()
    {
        texto = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        if (reproducirAlActivar)
        {
            IniciarAnimacion();
        }
    }

    private void OnDisable()
    {
        DetenerAnimacion();
    }

    [ContextMenu("Iniciar animación")]
    public void IniciarAnimacion()
    {
        if (rutinaOla != null)
        {
            StopCoroutine(rutinaOla);
        }

        rutinaOla = StartCoroutine(RutinaOlaCadaCiertoTiempo());
    }

    [ContextMenu("Detener animación")]
    public void DetenerAnimacion()
    {
        if (rutinaOla != null)
        {
            StopCoroutine(rutinaOla);
            rutinaOla = null;
        }

        RestaurarTexto();
    }

    [ContextMenu("Reproducir una ola")]
    public void ReproducirUnaOla()
    {
        StartCoroutine(ReproducirOla());
    }

    private IEnumerator RutinaOlaCadaCiertoTiempo()
    {
        yield return new WaitForSeconds(esperaInicial);

        while (true)
        {
            yield return ReproducirOla();
            yield return new WaitForSeconds(intervaloSegundos);
        }
    }

    private IEnumerator ReproducirOla()
    {
        if (texto == null)
        {
            yield break;
        }

        if (mostrarDebug)
        {
            Debug.Log("TITLE WAVE: iniciando ola en " + gameObject.name);
        }

        float tiempo = 0f;

        while (tiempo < duracionOla)
        {
            tiempo += Time.deltaTime;

            texto.ForceMeshUpdate();

            TMP_TextInfo textInfo = texto.textInfo;
            int characterCount = textInfo.characterCount;

            if (characterCount == 0)
            {
                yield return null;
                continue;
            }

            for (int i = 0; i < characterCount; i++)
            {
                TMP_CharacterInfo charInfo = textInfo.characterInfo[i];

                if (!charInfo.isVisible)
                {
                    continue;
                }

                int materialIndex = charInfo.materialReferenceIndex;
                int vertexIndex = charInfo.vertexIndex;

                Vector3[] vertices = textInfo.meshInfo[materialIndex].vertices;

                float progreso = tiempo / duracionOla;

                float entradaSalida = Mathf.Sin(progreso * Mathf.PI);

                float onda = Mathf.Sin(
                    tiempo * velocidadOla - i * separacionEntreLetras
                );

                float desplazamientoY = onda * amplitud * entradaSalida;

                Vector3 offset = new Vector3(0f, desplazamientoY, 0f);

                vertices[vertexIndex + 0] += offset;
                vertices[vertexIndex + 1] += offset;
                vertices[vertexIndex + 2] += offset;
                vertices[vertexIndex + 3] += offset;
            }

            texto.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);

            yield return null;
        }

        RestaurarTexto();
    }

    private void RestaurarTexto()
    {
        if (texto == null)
        {
            return;
        }

        texto.ForceMeshUpdate();
        texto.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
    }
}