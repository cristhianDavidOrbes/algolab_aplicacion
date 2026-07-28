using System;
using System.Collections;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Meta.XR.BuildingBlocks.AIBlocks;

[CreateAssetMenu(menuName = "AlgoLab/IA/Proveedor de voz local")]
public sealed class AlgoLabLocalVoiceProvider : AIProviderBase, ISpeechToTextTask, ITextToSpeechTask
{
    [SerializeField] private string apiBaseUrl = "https://appetite-tuesday-empty.ngrok-free.dev";
    [SerializeField] private string idioma = "es";
    [SerializeField] private string voz = "es-CO-SalomeNeural";
    [SerializeField, Range(5, 120)] private int timeoutSegundos = 90;

    private static readonly HttpClient Http = new HttpClient();

    [Serializable]
    private sealed class TranscripcionResponse
    {
        public string texto;
    }

    [Serializable]
    private sealed class VozPayload
    {
        public string texto;
        public string voz;
    }

    protected override InferenceType DefaultSupportedTypes => InferenceType.LocalServer;

    public async Task<string> TranscribeAsync(
        byte[] audioBytes,
        string language = null,
        CancellationToken ct = default)
    {
        if (audioBytes == null || audioBytes.Length == 0)
        {
            throw new ArgumentException("AlgoLab STT: el audio esta vacio.");
        }

        string url = Url("/api/voz/transcribir");
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        using var multipart = new MultipartFormDataContent();
        using var audio = new ByteArrayContent(audioBytes);
        audio.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/wav");
        multipart.Add(audio, "archivo", "grabacion.wav");
        multipart.Add(new StringContent(string.IsNullOrWhiteSpace(language) ? idioma : language), "idioma");
        request.Content = multipart;
        request.Headers.TryAddWithoutValidation("ngrok-skip-browser-warning", "true");

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(Mathf.Clamp(timeoutSegundos, 5, 120)));
        using HttpResponseMessage response = await Http
            .SendAsync(request, timeout.Token)
            .ConfigureAwait(false);
        string json = await response.Content
            .ReadAsStringAsync()
            .ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"AlgoLab STT HTTP {(int)response.StatusCode}: {json}");
        }

        TranscripcionResponse resultado = JsonUtility.FromJson<TranscripcionResponse>(json);
        return resultado != null ? (resultado.texto ?? string.Empty).Trim() : string.Empty;
    }

    public IEnumerator SynthesizeStreamCoroutine(
        string text,
        string voice = null,
        Action<AudioClip> onReady = null)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            Debug.LogWarning("AlgoLab TTS: el texto esta vacio.");
            yield break;
        }

        var payload = new VozPayload
        {
            texto = text.Trim(),
            voz = string.IsNullOrWhiteSpace(voice) ? voz : voice.Trim(),
        };
        byte[] body = System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(payload));

        using var request = new UnityWebRequest(Url("/api/voz/sintetizar"), UnityWebRequest.kHttpVerbPOST);
        request.uploadHandler = new UploadHandlerRaw(body);
        request.downloadHandler = new DownloadHandlerAudioClip(request.url, AudioType.WAV);
        request.timeout = Mathf.Clamp(timeoutSegundos, 5, 120);
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Accept", "audio/wav");
        request.SetRequestHeader("ngrok-skip-browser-warning", "true");

        yield return request.SendWebRequest();
        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"AlgoLab TTS HTTP {request.responseCode}: {request.error}");
            yield break;
        }

        AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
        if (clip == null)
        {
            Debug.LogError("AlgoLab TTS: no se pudo decodificar el audio recibido.");
            yield break;
        }

        onReady?.Invoke(clip);
    }

    private string Url(string ruta)
    {
        string baseUrl = string.IsNullOrWhiteSpace(apiBaseUrl)
            ? "https://appetite-tuesday-empty.ngrok-free.dev"
            : apiBaseUrl.Trim().TrimEnd('/');
        return baseUrl + ruta;
    }
}
