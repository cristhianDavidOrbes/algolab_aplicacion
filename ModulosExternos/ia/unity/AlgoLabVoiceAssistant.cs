using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

#if UNITY_STANDALONE_WIN || UNITY_WSA || UNITY_EDITOR_WIN
using UnityEngine.Windows.Speech;
#endif

public class AlgoLabVoiceAssistant : MonoBehaviour
{
    [Header("API IA")]
    [SerializeField] private string iaApiUrl = "https://appetite-tuesday-empty.ngrok-free.dev/api/ia/responder";
    [SerializeField] private int nivelId = 1;
    [SerializeField] private bool enviarNivelId = true;

    [Header("UI opcional")]
    [SerializeField] private TMP_Text estadoTexto;
    [SerializeField] private TMP_Text transcripcionTexto;
    [SerializeField] private TMP_Text respuestaTexto;

    [Header("Eventos opcionales")]
    public UnityEvent<string> alTranscribir;
    public UnityEvent<string> alResponderIa;
    public UnityEvent<string> alError;

#if UNITY_STANDALONE_WIN || UNITY_WSA || UNITY_EDITOR_WIN
    private DictationRecognizer dictationRecognizer;
#endif

    private bool escuchando;
    private bool enviando;
    private bool reiniciarReconocimientoDeFrases;
    private string textoParcial = "";

    private void Awake()
    {
        InicializarReconocimiento();
        CambiarEstado("Listo");
    }

    private void OnDestroy()
    {
#if UNITY_STANDALONE_WIN || UNITY_WSA || UNITY_EDITOR_WIN
        if (dictationRecognizer != null)
        {
            if (dictationRecognizer.Status == SpeechSystemStatus.Running)
            {
                dictationRecognizer.Stop();
            }

            dictationRecognizer.Dispose();
            dictationRecognizer = null;
        }

        RestaurarReconocimientoDeFrases();
#endif
    }

    public void AlternarGrabacion()
    {
        if (escuchando)
        {
            DetenerGrabacion();
        }
        else
        {
            IniciarGrabacion();
        }
    }

    public void IniciarGrabacion()
    {
        if (enviando)
        {
            return;
        }

#if UNITY_STANDALONE_WIN || UNITY_WSA || UNITY_EDITOR_WIN
        if (dictationRecognizer == null)
        {
            InicializarReconocimiento();
        }

        textoParcial = "";
        escuchando = true;
        PrepararReconocimientoDeVoz();
        CambiarEstado("Escuchando...");
        CambiarTexto(transcripcionTexto, "");

        if (dictationRecognizer.Status != SpeechSystemStatus.Running)
        {
            dictationRecognizer.Start();
        }
#else
        ReportarError("DictationRecognizer solo esta disponible en Windows/UWP. Para Quest o Android conviene usar Whisper en la API.");
#endif
    }

    public void DetenerGrabacion()
    {
#if UNITY_STANDALONE_WIN || UNITY_WSA || UNITY_EDITOR_WIN
        if (dictationRecognizer != null && dictationRecognizer.Status == SpeechSystemStatus.Running)
        {
            dictationRecognizer.Stop();
            return;
        }
#endif

        escuchando = false;
        CambiarEstado("Procesando...");

        if (string.IsNullOrWhiteSpace(textoParcial))
        {
            CambiarEstado("No se detecto texto");
            return;
        }

        StartCoroutine(EnviarPreguntaAI(textoParcial.Trim()));
    }

    public void EnviarTextoManual(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto) || enviando)
        {
            return;
        }

        StartCoroutine(EnviarPreguntaAI(texto.Trim()));
    }

    private void InicializarReconocimiento()
    {
#if UNITY_STANDALONE_WIN || UNITY_WSA || UNITY_EDITOR_WIN
        dictationRecognizer = new DictationRecognizer(ConfidenceLevel.Low);

        dictationRecognizer.DictationHypothesis += texto =>
        {
            textoParcial = texto;
            CambiarTexto(transcripcionTexto, texto);
        };

        dictationRecognizer.DictationResult += (texto, confidence) =>
        {
            textoParcial = texto;
            CambiarTexto(transcripcionTexto, texto);
            alTranscribir?.Invoke(texto);
        };

        dictationRecognizer.DictationComplete += completionCause =>
        {
            escuchando = false;
            RestaurarReconocimientoDeFrases();

            if (completionCause != DictationCompletionCause.Complete &&
                completionCause != DictationCompletionCause.TimeoutExceeded)
            {
                ReportarError("Reconocimiento finalizado: " + completionCause);
                return;
            }

            if (!string.IsNullOrWhiteSpace(textoParcial) && !enviando)
            {
                StartCoroutine(EnviarPreguntaAI(textoParcial.Trim()));
            }
            else if (!enviando)
            {
                CambiarEstado("No se detecto texto");
            }
        };

        dictationRecognizer.DictationError += (error, hresult) =>
        {
            escuchando = false;
            RestaurarReconocimientoDeFrases();
            ReportarError("Error de voz: " + error + " (" + hresult + ")");
        };
#endif
    }

    private void PrepararReconocimientoDeVoz()
    {
#if UNITY_STANDALONE_WIN || UNITY_WSA || UNITY_EDITOR_WIN
        reiniciarReconocimientoDeFrases = PhraseRecognitionSystem.Status == SpeechSystemStatus.Running;
        if (reiniciarReconocimientoDeFrases)
        {
            PhraseRecognitionSystem.Shutdown();
        }
#endif
    }

    private void RestaurarReconocimientoDeFrases()
    {
#if UNITY_STANDALONE_WIN || UNITY_WSA || UNITY_EDITOR_WIN
        if (reiniciarReconocimientoDeFrases)
        {
            PhraseRecognitionSystem.Restart();
            reiniciarReconocimientoDeFrases = false;
        }
#endif
    }

    private IEnumerator EnviarPreguntaAI(string pregunta)
    {
        enviando = true;
        CambiarEstado("Consultando IA...");
        CambiarTexto(respuestaTexto, "");

        string json = CrearJsonPregunta(pregunta);
        using (UnityWebRequest request = new UnityWebRequest(iaApiUrl, UnityWebRequest.kHttpVerbPOST))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json; charset=utf-8");
            request.SetRequestHeader("ngrok-skip-browser-warning", "true");

            yield return request.SendWebRequest();

            enviando = false;

            if (request.result != UnityWebRequest.Result.Success)
            {
                ReportarError("Error llamando IA: " + request.error);
                yield break;
            }

            AiResponse response = JsonUtility.FromJson<AiResponse>(request.downloadHandler.text);
            if (response == null || string.IsNullOrWhiteSpace(response.respuesta))
            {
                ReportarError("La IA respondio sin texto valido.");
                yield break;
            }

            CambiarEstado("Respuesta lista");
            CambiarTexto(respuestaTexto, response.respuesta);
            alResponderIa?.Invoke(response.respuesta);
        }
    }

    private string CrearJsonPregunta(string pregunta)
    {
        string preguntaEscapada = JsonEscape(pregunta);
        if (!enviarNivelId)
        {
            return "{\"pregunta\":\"" + preguntaEscapada + "\"}";
        }

        return "{\"pregunta\":\"" + preguntaEscapada + "\",\"nivel_id\":" + nivelId + "}";
    }

    private static string JsonEscape(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }

    private void CambiarEstado(string texto)
    {
        CambiarTexto(estadoTexto, texto);
    }

    private static void CambiarTexto(TMP_Text campo, string texto)
    {
        if (campo != null)
        {
            campo.text = texto;
        }
    }

    private void ReportarError(string mensaje)
    {
        CambiarEstado(mensaje);
        Debug.LogError(mensaje);
        alError?.Invoke(mensaje);
    }

    [Serializable]
    private class AiResponse
    {
        public string modelo;
        public string respuesta;
    }
}
