using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

public sealed class AlgoLabSpeechToTextClient : MonoBehaviour
{
    [Serializable] public sealed class TextoEvent : UnityEvent<string> { }

    [Header("API de voz")]
    public string apiUrl = "https://appetite-tuesday-empty.ngrok-free.dev/api/voz/transcribir";
    public string idioma = "es";
    public int timeoutSegundos = 120;

    [Header("Micrófono")]
    public int frecuencia = 16000;
    public int duracionMaxima = 15;

    [Header("Eventos")]
    public TextoEvent onTranscript = new TextoEvent();
    public TextoEvent onError = new TextoEvent();

    public bool Procesando => rutinaEnvio != null;
    public bool Grabando => clipGrabacion != null;

    private AudioClip clipGrabacion;
    private string dispositivo;
    private Coroutine rutinaEnvio;
    private UnityWebRequest solicitudActual;

    public void StartListening()
    {
        if (Grabando || Procesando)
        {
            Debug.LogWarning("stt-local: ya existe una grabación o transcripción en curso.");
            return;
        }

        string[] dispositivos = Microphone.devices;
        dispositivo = dispositivos != null && dispositivos.Length > 0 ? dispositivos[0] : null;
        clipGrabacion = Microphone.Start(
            dispositivo,
            false,
            Mathf.Clamp(duracionMaxima, 2, 30),
            Mathf.Clamp(frecuencia, 8000, 48000));

        if (clipGrabacion == null)
        {
            NotificarError("No se pudo iniciar el micrófono.");
            return;
        }

        Debug.Log($"stt-local: grabación iniciada | dispositivo={dispositivo ?? "predeterminado"} | frecuencia={clipGrabacion.frequency} | canales={clipGrabacion.channels}");
    }

    public void StopNow()
    {
        if (!Grabando)
        {
            return;
        }

        int posicion = Microphone.GetPosition(dispositivo);
        AudioClip clip = clipGrabacion;
        clipGrabacion = null;
        Microphone.End(dispositivo);

        if (posicion <= 0)
        {
            Destroy(clip);
            NotificarError("El micrófono no capturó audio.");
            return;
        }

        int canales = Mathf.Max(1, clip.channels);
        float[] muestras = new float[posicion * canales];
        if (!clip.GetData(muestras, 0))
        {
            Destroy(clip);
            NotificarError("No se pudo leer la grabación.");
            return;
        }

        int frecuenciaReal = clip.frequency;
        Destroy(clip);
        byte[] wav = CodificarWav(muestras, frecuenciaReal, canales);
        Debug.Log($"stt-local: grabación lista | muestras={muestras.Length} | wavBytes={wav.Length}");
        rutinaEnvio = StartCoroutine(EnviarAudio(wav));
    }

    private IEnumerator EnviarAudio(byte[] wav)
    {
        var secciones = new List<IMultipartFormSection>
        {
            new MultipartFormFileSection("archivo", wav, "grabacion.wav", "audio/wav"),
            new MultipartFormDataSection("idioma", string.IsNullOrWhiteSpace(idioma) ? "es" : idioma),
        };

        string url = string.IsNullOrWhiteSpace(apiUrl)
            ? "https://appetite-tuesday-empty.ngrok-free.dev/api/voz/transcribir"
            : apiUrl.Trim();

        using UnityWebRequest request = UnityWebRequest.Post(url, secciones);
        solicitudActual = request;
        request.timeout = Mathf.Clamp(timeoutSegundos, 10, 180);
        request.SetRequestHeader("Accept", "application/json");
        request.SetRequestHeader("ngrok-skip-browser-warning", "true");

        Debug.Log("stt-local: enviando audio a " + url);
        yield return request.SendWebRequest();

        solicitudActual = null;
        rutinaEnvio = null;

        string cuerpo = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;
        if (request.result != UnityWebRequest.Result.Success)
        {
            NotificarError($"No se pudo transcribir. HTTP {request.responseCode}: {request.error}");
            yield break;
        }

        TranscripcionResponse respuesta = null;
        try
        {
            respuesta = JsonUtility.FromJson<TranscripcionResponse>(cuerpo);
        }
        catch (Exception ex)
        {
            Debug.LogError("stt-local: respuesta inválida: " + ex.Message + " | " + cuerpo);
        }

        string texto = respuesta != null ? (respuesta.texto ?? string.Empty).Trim() : string.Empty;
        Debug.Log("stt-local: transcripción recibida=[" + texto + "]");
        onTranscript.Invoke(texto);
    }

    private void OnDisable()
    {
        if (Grabando)
        {
            Microphone.End(dispositivo);
            Destroy(clipGrabacion);
            clipGrabacion = null;
        }

        if (solicitudActual != null)
        {
            solicitudActual.Abort();
            solicitudActual = null;
        }

        if (rutinaEnvio != null)
        {
            StopCoroutine(rutinaEnvio);
            rutinaEnvio = null;
        }
    }

    private void NotificarError(string mensaje)
    {
        Debug.LogError("stt-local: " + mensaje);
        onError.Invoke(mensaje);
    }

    private static byte[] CodificarWav(float[] muestras, int frecuencia, int canales)
    {
        const short bits = 16;
        int bytesDatos = muestras.Length * 2;
        using var memoria = new MemoryStream(44 + bytesDatos);
        using var writer = new BinaryWriter(memoria);

        writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
        writer.Write(36 + bytesDatos);
        writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
        writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)canales);
        writer.Write(frecuencia);
        writer.Write(frecuencia * canales * bits / 8);
        writer.Write((short)(canales * bits / 8));
        writer.Write(bits);
        writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
        writer.Write(bytesDatos);

        for (int i = 0; i < muestras.Length; i++)
        {
            short valor = (short)Mathf.RoundToInt(Mathf.Clamp(muestras[i], -1f, 1f) * short.MaxValue);
            writer.Write(valor);
        }

        writer.Flush();
        return memoria.ToArray();
    }

    [Serializable]
    private sealed class TranscripcionResponse
    {
        public string texto;
    }
}
