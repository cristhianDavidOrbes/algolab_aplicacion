using System.Collections;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

public class AlgoLabIAClient : MonoBehaviour
{
    [Header("API IA")]
    public string iaApiUrl = "https://appetite-tuesday-empty.ngrok-free.dev/api/ia/responder";

    [Header("Nivel")]
    public int nivelId = 1;
    public bool usarNivelId = false;

    [Header("UI Opcional")]
    public TMP_Text textoPregunta;
    public TMP_Text textoRespuesta;
    public TMP_Text textoEstado;

    [Header("Configuración")]
    public int timeoutSegundos = 60;

    [Header("Eventos")]
    public UnityEvent<string> OnRespuestaIA;
    public UnityEvent<string> OnErrorIA;

    [Header("Debug")]
    public bool mostrarDebug = true;

    [System.Serializable]
    public class IAResponse
    {
        public string modelo;
        public string respuesta;
        public string mensaje;
        public string error;
    }

    [Serializable]
    private class IARequest
    {
        public string pregunta;
    }

    [Serializable]
    private class IARequestConNivel
    {
        public string pregunta;
        public int nivel_id;
    }

    private Coroutine rutinaActual;
    private UnityWebRequest requestActual;
    private bool cancelacionSolicitada;
    private int solicitudActualId = 0;

    private void OnDisable()
    {
        CancelarSolicitudActual(false);
    }

    private void OnDestroy()
    {
        CancelarSolicitudActual(false);
    }

    public void PreguntarDesdeTexto(string pregunta)
    {
        if (string.IsNullOrWhiteSpace(pregunta))
        {
            string error = "La pregunta está vacía.";
            Debug.LogWarning(error);
            OnErrorIA?.Invoke(error);
            return;
        }

        if (textoPregunta != null)
        {
            textoPregunta.text = "Pregunta: " + pregunta;
        }

        CancelarSolicitudActual(false);

        solicitudActualId++;
        cancelacionSolicitada = false;
        rutinaActual = StartCoroutine(EnviarPregunta(pregunta, solicitudActualId));
    }

    public void CancelarSolicitudActual()
    {
        CancelarSolicitudActual(true);
    }

    private void CancelarSolicitudActual(bool mostrarLog)
    {
        solicitudActualId++;
        cancelacionSolicitada = true;

        if (requestActual != null)
        {
            try
            {
                requestActual.Abort();
            }
            catch
            {
            }

            requestActual = null;
        }

        if (rutinaActual != null)
        {
            StopCoroutine(rutinaActual);
            rutinaActual = null;
        }

        if (mostrarLog && mostrarDebug)
        {
            Debug.Log("IA CLIENT: solicitud cancelada.");
        }
    }

    private IEnumerator EnviarPregunta(string pregunta, int solicitudId)
    {
        string url = string.IsNullOrWhiteSpace(iaApiUrl)
            ? "https://appetite-tuesday-empty.ngrok-free.dev/api/ia/responder"
            : iaApiUrl.Trim();

        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            NotificarError("La URL de la IA no es válida.");
            rutinaActual = null;
            yield break;
        }

        string json;

        if (usarNivelId)
        {
            json = JsonUtility.ToJson(new IARequestConNivel
            {
                pregunta = pregunta,
                nivel_id = Mathf.Max(0, nivelId)
            });
        }
        else
        {
            json = JsonUtility.ToJson(new IARequest { pregunta = pregunta });
        }

        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);

        using UnityWebRequest request = new UnityWebRequest(url, "POST");
        requestActual = request;

        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.timeout = Mathf.Clamp(timeoutSegundos, 1, 120);

        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Accept", "application/json");
        request.SetRequestHeader("ngrok-skip-browser-warning", "true");

        if (textoEstado != null)
        {
            textoEstado.text = "Consultando IA...";
        }

        DebugLog("IA CLIENT: enviando solicitud a " + uri.GetLeftPart(UriPartial.Path));

        yield return request.SendWebRequest();

        if (cancelacionSolicitada || solicitudId != solicitudActualId)
        {
            DebugLog("IA CLIENT: respuesta ignorada porque la solicitud fue cancelada o ya no es actual.");
            LimpiarSolicitud(request, solicitudId);
            yield break;
        }

        string textoDescargado = request.downloadHandler != null
            ? request.downloadHandler.text
            : "";

        if (request.result != UnityWebRequest.Result.Success)
        {
            string error =
                "Error conectando con IA. HTTP " +
                request.responseCode +
                " | " +
                request.error;

            if (!string.IsNullOrWhiteSpace(textoDescargado))
            {
                error += "\n" + Truncar(textoDescargado, 500);
            }

            Debug.LogError(error);

            if (textoRespuesta != null)
            {
                textoRespuesta.text = error;
            }

            if (textoEstado != null)
            {
                textoEstado.text = "Error";
            }

            OnErrorIA?.Invoke(error);

            LimpiarSolicitud(request, solicitudId);
            yield break;
        }

        DebugLog("IA CLIENT: respuesta RAW: " + Truncar(textoDescargado, 800));

        bool respuestaValida = ProcesarRespuesta(textoDescargado, out string respuestaFinal, out string errorRespuesta);

        if (!respuestaValida)
        {
            string error = string.IsNullOrWhiteSpace(errorRespuesta)
                ? "La IA no generó una respuesta válida."
                : errorRespuesta;

            Debug.LogWarning("IA CLIENT: " + error);

            if (textoRespuesta != null)
            {
                textoRespuesta.text = error;
            }

            if (textoEstado != null)
            {
                textoEstado.text = "Error";
            }

            OnErrorIA?.Invoke(error);

            LimpiarSolicitud(request, solicitudId);
            yield break;
        }

        if (textoRespuesta != null)
        {
            textoRespuesta.text = respuestaFinal;
        }

        if (textoEstado != null)
        {
            textoEstado.text = "Respuesta recibida";
        }

        OnRespuestaIA?.Invoke(respuestaFinal);

        LimpiarSolicitud(request, solicitudId);
    }

    private void LimpiarSolicitud(UnityWebRequest request, int solicitudId)
    {
        if (requestActual == request)
        {
            requestActual = null;
        }

        if (solicitudId == solicitudActualId)
        {
            rutinaActual = null;
            cancelacionSolicitada = false;
        }
    }

    private bool ProcesarRespuesta(string json, out string respuestaFinal, out string errorFinal)
    {
        respuestaFinal = "";
        errorFinal = "";

        if (string.IsNullOrWhiteSpace(json))
        {
            errorFinal = "La IA respondió vacío.";
            return false;
        }

        try
        {
            IAResponse response = JsonUtility.FromJson<IAResponse>(json);

            if (response != null)
            {
                if (!string.IsNullOrWhiteSpace(response.error))
                {
                    errorFinal = response.error;
                    return false;
                }

                if (!string.IsNullOrWhiteSpace(response.respuesta))
                {
                    respuestaFinal = response.respuesta.Trim();
                    return true;
                }

                if (!string.IsNullOrWhiteSpace(response.mensaje))
                {
                    respuestaFinal = response.mensaje.Trim();
                    return true;
                }
            }

            respuestaFinal = json.Trim();
            return !string.IsNullOrWhiteSpace(respuestaFinal);
        }
        catch
        {
            respuestaFinal = json.Trim();
            return !string.IsNullOrWhiteSpace(respuestaFinal);
        }
    }

    private void NotificarError(string error)
    {
        if (textoRespuesta != null)
        {
            textoRespuesta.text = error;
        }

        if (textoEstado != null)
        {
            textoEstado.text = "Error";
        }

        Debug.LogError("IA CLIENT: " + error);
        OnErrorIA?.Invoke(error);
    }

    private static string Truncar(string texto, int maximo)
    {
        if (string.IsNullOrEmpty(texto) || texto.Length <= maximo)
        {
            return texto ?? "";
        }

        return texto.Substring(0, maximo) + "...";
    }

    private void DebugLog(string mensaje)
    {
        if (mostrarDebug)
        {
            Debug.Log(mensaje);
        }
    }
}
