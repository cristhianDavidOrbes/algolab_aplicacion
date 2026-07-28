using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class AlgoLabBackendClient : MonoBehaviour
{
    public static AlgoLabBackendClient Instance { get; private set; }
    private const string BackendUrlPredeterminada =
        "https://backendfrontendpaginawebmr-production.up.railway.app";

    [Header("Backend")]
    public string backendBaseUrl = "https://backendfrontendpaginawebmr-production.up.railway.app";

    [Header("Referencias")]
    public AlgoLabSessionManager sessionManager;

    [Header("Configuración")]
    public bool mantenerEntreEscenas = true;
    public int timeoutSegundos = 20;

    [Header("Debug")]
    public bool mostrarDebug = true;

    [Serializable]
    public class LoginRequest
    {
        public string correo;
        public string contrasena;
    }

    [Serializable]
    public class LoginResponse
    {
        public bool exitoso;
        public string mensaje;
        public string token;
        public AlgoLabSessionManager.UsuarioSesion usuario;
    }

    [Serializable]
    public class ProgresoNivelDTO
    {
        public int nivel;
        public bool completado;
        public int puntaje;
        public int tiempoRestante;
        public int intentos;
    }

    [Serializable]
    public class ProgresoUsuarioDTO
    {
        public int usuarioId;
        public int nivelActual;
        public int puntajeTotal;
        public ProgresoNivelDTO[] niveles;
    }

    [Serializable]
    public class GuardarProgresoRequest
    {
        public int nivel;
        public bool completado;
        public int puntaje;
        public int tiempoRestante;
        public int intentos;
    }

    [Serializable]
    public class RankingEstudianteDTO
    {
        public int posicion;
        public int usuarioId;
        public string nombre;
        public string nombreUsuario;
        public int nivelActual;
        public int puntaje;
    }

    [Serializable]
    public class RankingRespuestaDTO
    {
        public int total;
        public RankingEstudianteDTO[] estudiantes;
    }

    private int generacionInicioSesion;

    private void Awake()
    {
        if (!ConfigurarSingleton())
        {
            return;
        }

        BuscarReferencias();
        NormalizarBackendUrl();
    }

    private bool ConfigurarSingleton()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return false;
        }

        Instance = this;

        if (mantenerEntreEscenas)
        {
            DontDestroyOnLoad(gameObject);
        }

        return true;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void BuscarReferencias()
    {
        if (sessionManager == null)
        {
            sessionManager = AlgoLabSessionManager.Instance;
        }

        if (sessionManager == null)
        {
            sessionManager = FindFirstObjectByType<AlgoLabSessionManager>();
        }
    }

    private void NormalizarBackendUrl()
    {
        if (string.IsNullOrWhiteSpace(backendBaseUrl))
        {
            backendBaseUrl = BackendUrlPredeterminada;
        }

        backendBaseUrl = backendBaseUrl.Trim();

        while (backendBaseUrl.EndsWith("/"))
        {
            backendBaseUrl = backendBaseUrl.Substring(0, backendBaseUrl.Length - 1);
        }

        if (!Uri.TryCreate(backendBaseUrl, UriKind.Absolute, out Uri uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            Debug.LogWarning(
                "BACKEND CLIENT: URL inválida. Se usará la dirección predeterminada."
            );
            backendBaseUrl = BackendUrlPredeterminada;
        }
    }

    public void IniciarSesion(
        string correo,
        string contrasena,
        Action<bool, string, LoginResponse> callback
    )
    {
        int generacion = ++generacionInicioSesion;
        StartCoroutine(IniciarSesionRutina(correo, contrasena, callback, generacion));
    }

    public void CancelarInicioSesionPendiente()
    {
        generacionInicioSesion++;
    }

    private IEnumerator IniciarSesionRutina(
        string correo,
        string contrasena,
        Action<bool, string, LoginResponse> callback,
        int generacion
    )
    {
        BuscarReferencias();
        NormalizarBackendUrl();

        if (string.IsNullOrWhiteSpace(correo))
        {
            callback?.Invoke(false, "Debes escribir el correo.", null);
            yield break;
        }

        if (string.IsNullOrWhiteSpace(contrasena))
        {
            callback?.Invoke(false, "Debes escribir la contraseña.", null);
            yield break;
        }

        LoginRequest body = new LoginRequest
        {
            correo = correo.Trim(),
            contrasena = contrasena
        };

        string json = JsonUtility.ToJson(body);
        string url = CrearUrl("/api/usuarios/iniciar-sesion");

        using UnityWebRequest request = CrearPostJson(url, json, false);

        DebugLog("BACKEND CLIENT: iniciando sesión en " + url);

        yield return request.SendWebRequest();

        if (generacion != generacionInicioSesion)
        {
            yield break;
        }

        string respuestaTexto = request.downloadHandler != null
            ? request.downloadHandler.text
            : "";

        if (!RespuestaExitosa(request))
        {
            string error = ConstruirMensajeError("No se pudo iniciar sesión.", request, respuestaTexto);
            Debug.LogError(error);
            callback?.Invoke(false, error, null);
            yield break;
        }

        LoginResponse respuesta = null;

        try
        {
            respuesta = JsonUtility.FromJson<LoginResponse>(respuestaTexto);
        }
        catch (Exception e)
        {
            string error = "No se pudo leer la respuesta del login: " + e.Message;
            Debug.LogError(error + "\nRespuesta: " + LimitarTextoParaLog(respuestaTexto));
            callback?.Invoke(false, error, null);
            yield break;
        }

        if (respuesta == null)
        {
            callback?.Invoke(false, "El backend respondió vacío.", null);
            yield break;
        }

        if (!respuesta.exitoso)
        {
            string mensaje = string.IsNullOrWhiteSpace(respuesta.mensaje)
                ? "Credenciales incorrectas."
                : respuesta.mensaje;

            callback?.Invoke(false, mensaje, respuesta);
            yield break;
        }

        if (string.IsNullOrWhiteSpace(respuesta.token))
        {
            callback?.Invoke(false, "El backend no devolvió token.", respuesta);
            yield break;
        }

        if (respuesta.usuario == null)
        {
            callback?.Invoke(false, "El backend no devolvió datos del usuario.", respuesta);
            yield break;
        }

        if (sessionManager != null)
        {
            sessionManager.IniciarSesionConUsuario(respuesta.token, respuesta.usuario);
        }

        DebugLog(
            "BACKEND CLIENT: login correcto. Usuario: " +
            respuesta.usuario.nombre +
            " | Nivel: " +
            respuesta.usuario.nivelActual +
            " | Puntaje: " +
            respuesta.usuario.puntaje
        );

        callback?.Invoke(true, "Inicio de sesión correcto.", respuesta);
    }

    public void ConsultarUsuarioActual(
        Action<bool, string, AlgoLabSessionManager.UsuarioSesion> callback
    )
    {
        StartCoroutine(ConsultarUsuarioActualRutina(callback));
    }

    private IEnumerator ConsultarUsuarioActualRutina(
        Action<bool, string, AlgoLabSessionManager.UsuarioSesion> callback
    )
    {
        BuscarReferencias();
        NormalizarBackendUrl();

        if (!TieneSesionAutenticada())
        {
            callback?.Invoke(false, "No hay sesión autenticada.", null);
            yield break;
        }

        string tokenSolicitud = sessionManager.TokenJwt;
        string url = CrearUrl("/api/usuarios/me");

        using UnityWebRequest request = CrearGet(url, true);

        DebugLog("BACKEND CLIENT: consultando usuario actual.");

        yield return request.SendWebRequest();

        string respuestaTexto = request.downloadHandler != null
            ? request.downloadHandler.text
            : "";

        if (!RespuestaExitosa(request))
        {
            string error = ConstruirMensajeError("No se pudo consultar el usuario actual.", request, respuestaTexto);
            Debug.LogError(error);
            callback?.Invoke(false, error, null);
            yield break;
        }

        AlgoLabSessionManager.UsuarioSesion usuario = null;

        try
        {
            usuario = JsonUtility.FromJson<AlgoLabSessionManager.UsuarioSesion>(respuestaTexto);
        }
        catch (Exception e)
        {
            string error = "No se pudo leer el usuario actual: " + e.Message;
            Debug.LogError(error + "\nRespuesta: " + LimitarTextoParaLog(respuestaTexto));
            callback?.Invoke(false, error, null);
            yield break;
        }

        if (usuario == null)
        {
            callback?.Invoke(false, "El backend devolvió usuario vacío.", null);
            yield break;
        }

        if (!SesionCoincide(tokenSolicitud))
        {
            callback?.Invoke(false, "La sesión cambió durante la consulta.", null);
            yield break;
        }

        if (sessionManager != null)
        {
            sessionManager.ActualizarUsuarioLocal(usuario);
        }

        callback?.Invoke(true, "Usuario actualizado.", usuario);
    }

    public void ConsultarProgreso(
        Action<bool, string, ProgresoUsuarioDTO> callback
    )
    {
        StartCoroutine(ConsultarProgresoRutina(callback));
    }

    private IEnumerator ConsultarProgresoRutina(
        Action<bool, string, ProgresoUsuarioDTO> callback
    )
    {
        BuscarReferencias();
        NormalizarBackendUrl();

        if (!TieneSesionAutenticada())
        {
            callback?.Invoke(false, "No hay sesión autenticada.", null);
            yield break;
        }

        string tokenSolicitud = sessionManager.TokenJwt;
        string url = CrearUrl("/api/progreso/me");

        using UnityWebRequest request = CrearGet(url, true);

        DebugLog("BACKEND CLIENT: consultando progreso.");

        yield return request.SendWebRequest();

        string respuestaTexto = request.downloadHandler != null
            ? request.downloadHandler.text
            : "";

        if (!RespuestaExitosa(request))
        {
            string error = ConstruirMensajeError("No se pudo consultar el progreso.", request, respuestaTexto);
            Debug.LogError(error);
            callback?.Invoke(false, error, null);
            yield break;
        }

        ProgresoUsuarioDTO progreso = null;

        try
        {
            progreso = JsonUtility.FromJson<ProgresoUsuarioDTO>(respuestaTexto);
        }
        catch (Exception e)
        {
            string error = "No se pudo leer el progreso: " + e.Message;
            Debug.LogError(error + "\nRespuesta: " + LimitarTextoParaLog(respuestaTexto));
            callback?.Invoke(false, error, null);
            yield break;
        }

        if (progreso == null)
        {
            callback?.Invoke(false, "El backend devolvió progreso vacío.", null);
            yield break;
        }

        if (!SesionCoincide(tokenSolicitud))
        {
            callback?.Invoke(false, "La sesión cambió durante la consulta.", null);
            yield break;
        }

        if (sessionManager != null)
        {
            sessionManager.ActualizarProgresoLocal(
                progreso.nivelActual,
                progreso.puntajeTotal
            );
        }

        DebugLog(
            "BACKEND CLIENT: progreso recibido. Nivel actual: " +
            progreso.nivelActual +
            " | Puntaje total: " +
            progreso.puntajeTotal
        );

        callback?.Invoke(true, "Progreso consultado.", progreso);
    }

    public void GuardarProgreso(
        int nivel,
        bool completado,
        int puntaje,
        int tiempoRestante,
        int intentos,
        Action<bool, string, ProgresoUsuarioDTO> callback = null
    )
    {
        StartCoroutine(GuardarProgresoRutina(
            nivel,
            completado,
            puntaje,
            tiempoRestante,
            intentos,
            callback
        ));
    }

    private IEnumerator GuardarProgresoRutina(
        int nivel,
        bool completado,
        int puntaje,
        int tiempoRestante,
        int intentos,
        Action<bool, string, ProgresoUsuarioDTO> callback
    )
    {
        BuscarReferencias();
        NormalizarBackendUrl();

        if (sessionManager != null && sessionManager.ModoInvitado)
        {
            DebugLog("BACKEND CLIENT: modo invitado. No se guarda progreso.");
            callback?.Invoke(true, "Modo invitado: el progreso no se guardó.", null);
            yield break;
        }

        if (!TieneSesionAutenticada())
        {
            callback?.Invoke(false, "No hay sesión autenticada para guardar progreso.", null);
            yield break;
        }

        string tokenSolicitud = sessionManager.TokenJwt;

        GuardarProgresoRequest body = new GuardarProgresoRequest
        {
            nivel = Mathf.Max(1, nivel),
            completado = completado,
            puntaje = Mathf.Max(0, puntaje),
            tiempoRestante = Mathf.Max(0, tiempoRestante),
            intentos = Mathf.Max(0, intentos)
        };

        string json = JsonUtility.ToJson(body);
        string url = CrearUrl("/api/progreso");

        using UnityWebRequest request = CrearPostJson(url, json, true);

        DebugLog("BACKEND CLIENT: guardando progreso: " + json);

        yield return request.SendWebRequest();

        string respuestaTexto = request.downloadHandler != null
            ? request.downloadHandler.text
            : "";

        if (!RespuestaExitosa(request))
        {
            string error = ConstruirMensajeError("No se pudo guardar el progreso.", request, respuestaTexto);
            Debug.LogError(error);
            callback?.Invoke(false, error, null);
            yield break;
        }

        ProgresoUsuarioDTO progreso = null;

        try
        {
            progreso = JsonUtility.FromJson<ProgresoUsuarioDTO>(respuestaTexto);
        }
        catch (Exception e)
        {
            string error = "El progreso se envió, pero no se pudo leer la respuesta: " + e.Message;
            Debug.LogError(error + "\nRespuesta: " + LimitarTextoParaLog(respuestaTexto));
            callback?.Invoke(false, error, null);
            yield break;
        }

        if (!SesionCoincide(tokenSolicitud))
        {
            callback?.Invoke(false, "La sesión cambió mientras se guardaba el progreso.", null);
            yield break;
        }

        if (progreso != null && sessionManager != null)
        {
            sessionManager.ActualizarProgresoLocal(
                progreso.nivelActual,
                progreso.puntajeTotal
            );
        }

        DebugLog("BACKEND CLIENT: progreso guardado correctamente.");

        callback?.Invoke(true, "Progreso guardado.", progreso);
    }

    public void ConsultarRanking(Action<bool, string, RankingRespuestaDTO> callback)
    {
        StartCoroutine(ConsultarRankingRutina(callback));
    }

    private IEnumerator ConsultarRankingRutina(
        Action<bool, string, RankingRespuestaDTO> callback
    )
    {
        NormalizarBackendUrl();

        string url = CrearUrl("/api/ranking");

        using UnityWebRequest request = CrearGet(url, false);
        DebugLog("BACKEND CLIENT: consultando ranking.");

        yield return request.SendWebRequest();

        string respuestaTexto = request.downloadHandler != null
            ? request.downloadHandler.text
            : "";

        if (!RespuestaExitosa(request))
        {
            string error = ConstruirMensajeError(
                "No se pudo consultar el ranking.",
                request,
                respuestaTexto
            );
            Debug.LogWarning(error);
            callback?.Invoke(false, error, null);
            yield break;
        }

        RankingRespuestaDTO ranking = null;

        try
        {
            ranking = JsonUtility.FromJson<RankingRespuestaDTO>(respuestaTexto);
        }
        catch (Exception e)
        {
            callback?.Invoke(false, "No se pudo leer el ranking: " + e.Message, null);
            yield break;
        }

        if (ranking == null)
        {
            callback?.Invoke(false, "El backend devolvió un ranking vacío.", null);
            yield break;
        }

        if (ranking.estudiantes == null)
        {
            ranking.estudiantes = Array.Empty<RankingEstudianteDTO>();
        }

        Array.Sort(ranking.estudiantes, (a, b) =>
        {
            if (ReferenceEquals(a, b)) return 0;
            if (a == null) return 1;
            if (b == null) return -1;

            int porPuntaje = b.puntaje.CompareTo(a.puntaje);
            return porPuntaje != 0 ? porPuntaje : a.usuarioId.CompareTo(b.usuarioId);
        });

        for (int i = 0; i < ranking.estudiantes.Length; i++)
        {
            if (ranking.estudiantes[i] != null)
            {
                ranking.estudiantes[i].posicion = i + 1;
            }
        }

        ranking.total = ranking.estudiantes.Length;

        callback?.Invoke(true, "Ranking actualizado.", ranking);
    }

    private UnityWebRequest CrearGet(string url, bool requiereToken)
    {
        UnityWebRequest request = UnityWebRequest.Get(url);
        request.timeout = Mathf.Clamp(timeoutSegundos, 1, 120);
        request.downloadHandler = new DownloadHandlerBuffer();

        request.SetRequestHeader("Accept", "application/json");

        if (requiereToken)
        {
            AgregarAuthorization(request);
        }

        return request;
    }

    private UnityWebRequest CrearPostJson(string url, string json, bool requiereToken)
    {
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        request.timeout = Mathf.Clamp(timeoutSegundos, 1, 120);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();

        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Accept", "application/json");

        if (requiereToken)
        {
            AgregarAuthorization(request);
        }

        return request;
    }

    private void AgregarAuthorization(UnityWebRequest request)
    {
        BuscarReferencias();

        if (sessionManager == null)
        {
            return;
        }

        string header = sessionManager.ObtenerAuthorizationHeader();

        if (!string.IsNullOrWhiteSpace(header))
        {
            request.SetRequestHeader("Authorization", header);
        }
    }

    private bool TieneSesionAutenticada()
    {
        BuscarReferencias();

        if (sessionManager == null)
        {
            Debug.LogWarning("BACKEND CLIENT: no existe AlgoLabSessionManager.");
            return false;
        }

        return sessionManager.EstaAutenticado;
    }

    private bool SesionCoincide(string tokenSolicitud)
    {
        BuscarReferencias();
        return sessionManager != null &&
               sessionManager.EstaAutenticado &&
               string.Equals(sessionManager.TokenJwt, tokenSolicitud, StringComparison.Ordinal);
    }

    private string CrearUrl(string endpoint)
    {
        NormalizarBackendUrl();

        if (string.IsNullOrWhiteSpace(endpoint))
        {
            return backendBaseUrl;
        }

        if (!endpoint.StartsWith("/"))
        {
            endpoint = "/" + endpoint;
        }

        return backendBaseUrl + endpoint;
    }

    private bool RespuestaExitosa(UnityWebRequest request)
    {
        if (request == null)
        {
            return false;
        }

        return request.result == UnityWebRequest.Result.Success &&
               request.responseCode >= 200 &&
               request.responseCode < 300;
    }

    private string ConstruirMensajeError(
        string mensajeBase,
        UnityWebRequest request,
        string respuestaTexto
    )
    {
        long codigo = request != null ? request.responseCode : 0;
        string errorUnity = request != null ? request.error : "";

        StringBuilder sb = new StringBuilder();

        sb.Append(mensajeBase);
        sb.Append(" Código HTTP: ");
        sb.Append(codigo);

        if (!string.IsNullOrWhiteSpace(errorUnity))
        {
            sb.Append(" | Error: ");
            sb.Append(errorUnity);
        }

        if (!string.IsNullOrWhiteSpace(respuestaTexto))
        {
            sb.Append(" | Respuesta: ");
            sb.Append(LimitarTextoParaLog(respuestaTexto));
        }

        return sb.ToString();
    }

    private static string LimitarTextoParaLog(string texto)
    {
        if (string.IsNullOrEmpty(texto))
        {
            return string.Empty;
        }

        const int maximo = 800;
        string limpio = texto.Replace('\r', ' ').Replace('\n', ' ');
        return limpio.Length <= maximo ? limpio : limpio.Substring(0, maximo) + "...";
    }

    private void DebugLog(string mensaje)
    {
        if (mostrarDebug)
        {
            Debug.Log(mensaje);
        }
    }
}
