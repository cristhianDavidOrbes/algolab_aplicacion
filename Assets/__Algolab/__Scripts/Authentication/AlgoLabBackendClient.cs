using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class AlgoLabBackendClient : MonoBehaviour
{
    public enum EstadoValidacionSesion
    {
        Valida,
        SinSesion,
        CredencialesInvalidas,
        ErrorTemporal
    }

    public static AlgoLabBackendClient Instance { get; private set; }
    private const string BackendUrlPredeterminada =
        "https://backendfrontendpaginawebmr-production.up.railway.app";

    [Header("Backend")]
    public string backendBaseUrl = "https://backendfrontendpaginawebmr-production.up.railway.app";

    [Header("Reportes pedagógicos con IA")]
    [Tooltip("Se intenta usar primero la URL configurada en AlgoLabIAClient y esta queda como respaldo.")]
    public string iaReporteUrl = "https://appetite-tuesday-empty.ngrok-free.dev/api/ia/reporte-nivel";
    public bool generarReporteAlCompletarNivel = true;

    [Header("Referencias")]
    public AlgoLabSessionManager sessionManager;

    [Header("Configuración")]
    public bool mantenerEntreEscenas = true;
    public int timeoutSegundos = 20;
    public bool sincronizarPerfilAlIniciar = true;
    [Min(15f)]
    public float intervaloMinimoSincronizacionPerfil = 30f;

    [Header("Debug")]
    public bool mostrarDebug = true;

    [Serializable]
    public class LoginRequest
    {
        public string correo;
        public string contrasena;
        public string canal;
    }

    [Serializable]
    public class DesafioSegundoFactorResponse
    {
        public bool exitoso;
        public bool requiereSegundoFactor;
        public string mensaje;
        public string desafioId;
        public string canal;
        public string destinoEnmascarado;
        public int expiraEnSegundos;
        public int reenvioDisponibleEnSegundos;
    }

    [Serializable]
    private class VerificarSegundoFactorRequest
    {
        public string desafioId;
        public string codigo;
    }

    [Serializable]
    private class ReenviarSegundoFactorRequest
    {
        public string desafioId;
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
    private class ReporteIARequest
    {
        public string usuario_nombre;
        public int nivel;
        public int puntaje;
        public int tiempo_restante;
        public int intentos;
        public bool completado;
        public string[] errores;
    }

    [Serializable]
    private class ReporteIAResponse
    {
        public int dominio;
        public string resumen;
        public string[] fortalezas;
        public string[] aspectos_mejora;
        public string[] recomendaciones;
    }

    [Serializable]
    private class ActualizarReporteBackendRequest
    {
        public int dominio;
        public string resumen;
        public string[] fortalezas;
        public string[] aspectosMejora;
        public string[] recomendaciones;
        public int puntajeBase;
        public int tiempoRestanteBase;
        public int intentosBase;
        public bool completadoBase;
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
    private float proximaSincronizacionPerfilPermitida;
    private bool sincronizacionTutorialPendiente;
    private bool sincronizacionTutorialEnCurso;
    private string tokenSincronizacionTutorial = "";

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
        SolicitarSegundoFactor(
            correo,
            contrasena,
            "CORREO",
            (ok, mensaje, desafio) =>
            {
                callback?.Invoke(
                    false,
                    ok
                        ? "Se envió un código de seguridad. Verifícalo para completar el inicio de sesión."
                        : mensaje,
                    null
                );
            }
        );
    }

    public void SolicitarSegundoFactor(
        string correo,
        string contrasena,
        string canal,
        Action<bool, string, DesafioSegundoFactorResponse> callback
    )
    {
        int generacion = ++generacionInicioSesion;
        StartCoroutine(
            SolicitarSegundoFactorRutina(correo, contrasena, canal, callback, generacion)
        );
    }

    public void VerificarSegundoFactor(
        string desafioId,
        string codigo,
        Action<bool, string, LoginResponse> callback
    )
    {
        int generacion = ++generacionInicioSesion;
        StartCoroutine(VerificarSegundoFactorRutina(desafioId, codigo, callback, generacion));
    }

    public void ReenviarSegundoFactor(
        string desafioId,
        Action<bool, string, DesafioSegundoFactorResponse> callback
    )
    {
        int generacion = ++generacionInicioSesion;
        StartCoroutine(ReenviarSegundoFactorRutina(desafioId, callback, generacion));
    }

    private IEnumerator Start()
    {
        yield return null;
        BuscarReferencias();
        if (sincronizarPerfilAlIniciar && TieneSesionAutenticada())
        {
            SincronizarPerfilYProgresoAhora();

            // Compatibilidad con instalaciones que ya guardaron el hito en
            // PlayerPrefs antes de que existiera el campo en el backend.
            if (sessionManager != null && sessionManager.TutorialCompletado)
            {
                MarcarTutorialPrincipalCompletado();
            }
        }
    }

    private void OnApplicationFocus(bool tieneFoco)
    {
        if (tieneFoco)
        {
            SincronizarPerfilYProgresoAhora(false);
        }
    }

    private void OnApplicationPause(bool pausada)
    {
        if (!pausada)
        {
            SincronizarPerfilYProgresoAhora(false);
        }
    }

    /// <summary>
    /// Vuelve a leer el perfil web y el progreso. Se llama al iniciar, al volver
    /// a la aplicación y desde Configuración, de modo que nombre, alias,
    /// programa, institución y avatar se reflejen en el panel sin otro login.
    /// </summary>
    public void SincronizarPerfilYProgresoAhora(bool forzar = true)
    {
        BuscarReferencias();

        if (!sincronizarPerfilAlIniciar || !TieneSesionAutenticada())
        {
            return;
        }

        float ahora = Time.realtimeSinceStartup;
        if (!forzar && ahora < proximaSincronizacionPerfilPermitida)
        {
            return;
        }

        proximaSincronizacionPerfilPermitida =
            ahora + Mathf.Max(15f, intervaloMinimoSincronizacionPerfil);

        // Primero valida la credencial. Esto evita dos problemas: que un JWT
        // vencido mantenga el juego abierto y que /me llegue después que
        // /progreso/me con un snapshot viejo que haga retroceder la interfaz.
        ValidarSesionActual((estado, mensaje, _) =>
        {
            if (estado == EstadoValidacionSesion.Valida)
            {
                ConsultarProgreso((_, _, _) => { });
                if (sincronizacionTutorialPendiente)
                {
                    ReintentarSincronizacionTutorialSiCorresponde();
                }
                return;
            }

            if (estado == EstadoValidacionSesion.CredencialesInvalidas ||
                estado == EstadoValidacionSesion.SinSesion)
            {
                sincronizacionTutorialPendiente = false;
                tokenSincronizacionTutorial = "";
                AlgoLabStartUIController inicio = FindFirstObjectByType<AlgoLabStartUIController>(
                    FindObjectsInactive.Include
                );
                if (inicio != null)
                {
                    inicio.ManejarSesionRemotaInvalida(mensaje);
                }
                else
                {
                    sessionManager?.CerrarSesion();
                    AlgoLabGameAccessController acceso = FindFirstObjectByType<AlgoLabGameAccessController>(
                        FindObjectsInactive.Include
                    );
                    acceso?.BloquearAccesoJuego();
                }
            }
            // ErrorTemporal conserva la sesión local: una caída de red nunca
            // debe borrar credenciales válidas ni expulsar al estudiante.
        });
    }

    /// <summary>
    /// Guarda el hito del tutorial en memoria inmediatamente y lo sincroniza
    /// con la cuenta. Es idempotente y conserva el pendiente si hay una caída
    /// temporal de red, para reintentarlo al recuperar foco.
    /// </summary>
    public void MarcarTutorialPrincipalCompletado(Action<bool, string> callback = null)
    {
        BuscarReferencias();
        if (!TieneSesionAutenticada())
        {
            callback?.Invoke(false, "No hay sesión autenticada.");
            return;
        }

        sessionManager.MarcarTutorialCompletadoLocalmente();
        sincronizacionTutorialPendiente = true;
        tokenSincronizacionTutorial = sessionManager.TokenJwt;

        if (!sincronizacionTutorialEnCurso)
        {
            StartCoroutine(MarcarTutorialPrincipalCompletadoRutina(callback));
        }
    }

    private void ReintentarSincronizacionTutorialSiCorresponde()
    {
        if (sincronizacionTutorialEnCurso || !TieneSesionAutenticada())
        {
            return;
        }

        if (!string.Equals(
                tokenSincronizacionTutorial,
                sessionManager.TokenJwt,
                StringComparison.Ordinal))
        {
            sincronizacionTutorialPendiente = false;
            tokenSincronizacionTutorial = "";
            return;
        }

        StartCoroutine(MarcarTutorialPrincipalCompletadoRutina(null));
    }

    private IEnumerator MarcarTutorialPrincipalCompletadoRutina(Action<bool, string> callback)
    {
        sincronizacionTutorialEnCurso = true;
        string tokenSolicitud = tokenSincronizacionTutorial;
        string url = CrearUrl("/api/usuarios/me/tutorial-completado");

        using UnityWebRequest request = CrearJsonRequest(url, "PATCH", "{}", true);
        yield return request.SendWebRequest();

        string respuestaTexto = request.downloadHandler != null
            ? request.downloadHandler.text
            : "";

        if (!RespuestaExitosa(request) || !SesionCoincide(tokenSolicitud))
        {
            sincronizacionTutorialEnCurso = false;
            string error = ConstruirMensajeError(
                "No se pudo sincronizar el tutorial completado.",
                request,
                respuestaTexto
            );
            Debug.LogWarning(error);
            callback?.Invoke(false, error);
            yield break;
        }

        try
        {
            AlgoLabSessionManager.UsuarioSesion usuario =
                JsonUtility.FromJson<AlgoLabSessionManager.UsuarioSesion>(respuestaTexto);
            if (usuario != null)
            {
                sessionManager.ActualizarUsuarioLocal(usuario);
            }
        }
        catch (Exception excepcion)
        {
            Debug.LogWarning(
                "BACKEND CLIENT: el tutorial se guardó, pero no se pudo leer el perfil: " +
                excepcion.Message
            );
        }

        sincronizacionTutorialPendiente = false;
        sincronizacionTutorialEnCurso = false;
        tokenSincronizacionTutorial = "";
        callback?.Invoke(true, "Tutorial sincronizado.");
    }

    public void CancelarInicioSesionPendiente()
    {
        generacionInicioSesion++;
    }

    private IEnumerator SolicitarSegundoFactorRutina(
        string correo,
        string contrasena,
        string canal,
        Action<bool, string, DesafioSegundoFactorResponse> callback,
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

        correo = correo.Trim().ToLowerInvariant();
        if (!correo.EndsWith("@campusucc.edu.co", StringComparison.OrdinalIgnoreCase))
        {
            callback?.Invoke(
                false,
                "Usa tu correo institucional terminado en @campusucc.edu.co.",
                null
            );
            yield break;
        }

        canal = string.Equals(canal, "SMS", StringComparison.OrdinalIgnoreCase)
            ? "SMS"
            : "CORREO";

        LoginRequest body = new LoginRequest
        {
            correo = correo,
            contrasena = contrasena,
            canal = canal
        };

        string json = JsonUtility.ToJson(body);
        string url = CrearUrl("/api/usuarios/iniciar-sesion");

        using UnityWebRequest request = CrearPostJson(url, json, false);

        DebugLog("BACKEND CLIENT: solicitando segundo factor en " + url);

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
            string error = ConstruirMensajeError(
                "No se pudo iniciar sesión.",
                request,
                respuestaTexto
            );
            Debug.LogError(error);
            callback?.Invoke(false, error, null);
            yield break;
        }

        DesafioSegundoFactorResponse respuesta = null;

        try
        {
            respuesta = JsonUtility.FromJson<DesafioSegundoFactorResponse>(respuestaTexto);
        }
        catch (Exception e)
        {
            string error = "No se pudo leer el desafío de seguridad: " + e.Message;
            Debug.LogError(error + "\nRespuesta: " + LimitarTextoParaLog(respuestaTexto));
            callback?.Invoke(false, error, null);
            yield break;
        }

        if (respuesta == null)
        {
            callback?.Invoke(false, "El backend respondió vacío.", null);
            yield break;
        }

        if (!respuesta.exitoso ||
            !respuesta.requiereSegundoFactor ||
            string.IsNullOrWhiteSpace(respuesta.desafioId))
        {
            string mensaje = string.IsNullOrWhiteSpace(respuesta.mensaje)
                ? "No se pudo crear el desafío de seguridad."
                : respuesta.mensaje;

            callback?.Invoke(false, mensaje, respuesta);
            yield break;
        }

        callback?.Invoke(
            true,
            string.IsNullOrWhiteSpace(respuesta.mensaje)
                ? "Código de seguridad enviado."
                : respuesta.mensaje,
            respuesta
        );
    }

    private IEnumerator VerificarSegundoFactorRutina(
        string desafioId,
        string codigo,
        Action<bool, string, LoginResponse> callback,
        int generacion
    )
    {
        BuscarReferencias();
        NormalizarBackendUrl();

        desafioId = desafioId != null ? desafioId.Trim() : string.Empty;
        codigo = codigo != null ? codigo.Trim() : string.Empty;
        if (string.IsNullOrWhiteSpace(desafioId))
        {
            callback?.Invoke(false, "El desafío de seguridad ya no es válido.", null);
            yield break;
        }

        if (codigo.Length != 6)
        {
            callback?.Invoke(false, "El código debe tener 6 dígitos.", null);
            yield break;
        }

        VerificarSegundoFactorRequest body = new VerificarSegundoFactorRequest
        {
            desafioId = desafioId,
            codigo = codigo
        };
        string url = CrearUrl("/api/usuarios/segundo-factor/verificar");
        using UnityWebRequest request = CrearPostJson(url, JsonUtility.ToJson(body), false);
        yield return request.SendWebRequest();

        if (generacion != generacionInicioSesion)
            yield break;

        string respuestaTexto = request.downloadHandler != null
            ? request.downloadHandler.text
            : string.Empty;
        if (!RespuestaExitosa(request))
        {
            callback?.Invoke(
                false,
                ConstruirMensajeError("No se pudo verificar el código.", request, respuestaTexto),
                null
            );
            yield break;
        }

        LoginResponse respuesta = null;
        try
        {
            respuesta = JsonUtility.FromJson<LoginResponse>(respuestaTexto);
        }
        catch (Exception e)
        {
            callback?.Invoke(false, "No se pudo leer la sesión verificada: " + e.Message, null);
            yield break;
        }

        if (respuesta == null ||
            !respuesta.exitoso ||
            string.IsNullOrWhiteSpace(respuesta.token) ||
            respuesta.usuario == null)
        {
            callback?.Invoke(
                false,
                respuesta != null && !string.IsNullOrWhiteSpace(respuesta.mensaje)
                    ? respuesta.mensaje
                    : "La verificación no devolvió una sesión válida.",
                respuesta
            );
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

    private IEnumerator ReenviarSegundoFactorRutina(
        string desafioId,
        Action<bool, string, DesafioSegundoFactorResponse> callback,
        int generacion
    )
    {
        BuscarReferencias();
        NormalizarBackendUrl();
        desafioId = desafioId != null ? desafioId.Trim() : string.Empty;
        if (string.IsNullOrWhiteSpace(desafioId))
        {
            callback?.Invoke(false, "El desafío de seguridad ya no es válido.", null);
            yield break;
        }

        ReenviarSegundoFactorRequest body = new ReenviarSegundoFactorRequest
        {
            desafioId = desafioId
        };
        string url = CrearUrl("/api/usuarios/segundo-factor/reenviar");
        using UnityWebRequest request = CrearPostJson(url, JsonUtility.ToJson(body), false);
        yield return request.SendWebRequest();

        if (generacion != generacionInicioSesion)
            yield break;

        string respuestaTexto = request.downloadHandler != null
            ? request.downloadHandler.text
            : string.Empty;
        if (!RespuestaExitosa(request))
        {
            callback?.Invoke(
                false,
                ConstruirMensajeError("No se pudo reenviar el código.", request, respuestaTexto),
                null
            );
            yield break;
        }

        DesafioSegundoFactorResponse respuesta = null;
        try
        {
            respuesta = JsonUtility.FromJson<DesafioSegundoFactorResponse>(respuestaTexto);
        }
        catch (Exception e)
        {
            callback?.Invoke(false, "No se pudo leer el nuevo desafío: " + e.Message, null);
            yield break;
        }

        if (respuesta == null ||
            !respuesta.exitoso ||
            string.IsNullOrWhiteSpace(respuesta.desafioId))
        {
            callback?.Invoke(
                false,
                respuesta != null && !string.IsNullOrWhiteSpace(respuesta.mensaje)
                    ? respuesta.mensaje
                    : "No se pudo reenviar el código.",
                respuesta
            );
            yield break;
        }

        callback?.Invoke(true, respuesta.mensaje, respuesta);
    }

    public void ConsultarUsuarioActual(
        Action<bool, string, AlgoLabSessionManager.UsuarioSesion> callback
    )
    {
        ValidarSesionActual((estado, mensaje, usuario) =>
        {
            callback?.Invoke(estado == EstadoValidacionSesion.Valida, mensaje, usuario);
        });
    }

    /// <summary>
    /// Comprueba el token guardado contra /api/usuarios/me y distingue una
    /// credencial vencida de una indisponibilidad temporal. El consumidor solo
    /// debe borrar la sesión cuando recibe CredencialesInvalidas o SinSesion.
    /// </summary>
    public void ValidarSesionActual(
        Action<EstadoValidacionSesion, string, AlgoLabSessionManager.UsuarioSesion> callback
    )
    {
        StartCoroutine(ValidarSesionActualRutina(callback));
    }

    private IEnumerator ValidarSesionActualRutina(
        Action<EstadoValidacionSesion, string, AlgoLabSessionManager.UsuarioSesion> callback
    )
    {
        BuscarReferencias();
        NormalizarBackendUrl();

        if (!TieneSesionAutenticada())
        {
            callback?.Invoke(
                EstadoValidacionSesion.SinSesion,
                "No hay sesión autenticada.",
                null
            );
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
            EstadoValidacionSesion estado = ClasificarFalloValidacionSesion(request.responseCode);

            if (estado == EstadoValidacionSesion.CredencialesInvalidas)
            {
                Debug.LogWarning(error);
            }
            else
            {
                Debug.LogError(error);
            }

            callback?.Invoke(estado, error, null);
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
            callback?.Invoke(EstadoValidacionSesion.ErrorTemporal, error, null);
            yield break;
        }

        if (usuario == null)
        {
            callback?.Invoke(
                EstadoValidacionSesion.ErrorTemporal,
                "El backend devolvió usuario vacío.",
                null
            );
            yield break;
        }

        if (!SesionCoincide(tokenSolicitud))
        {
            // No se borra aquí: puede haberse iniciado otra sesión mientras la
            // solicitud anterior seguía en vuelo.
            callback?.Invoke(
                EstadoValidacionSesion.ErrorTemporal,
                "La sesión cambió durante la consulta.",
                null
            );
            yield break;
        }

        if (sessionManager != null)
        {
            sessionManager.ActualizarUsuarioLocal(usuario);
        }

        callback?.Invoke(EstadoValidacionSesion.Valida, "Usuario actualizado.", usuario);
    }

    public static EstadoValidacionSesion ClasificarFalloValidacionSesion(long codigoHttp)
    {
        return codigoHttp == 401 || codigoHttp == 403
            ? EstadoValidacionSesion.CredencialesInvalidas
            : EstadoValidacionSesion.ErrorTemporal;
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
        GuardarProgreso(nivel, completado, puntaje, tiempoRestante, intentos, null, callback);
    }

    public void GuardarProgreso(
        int nivel,
        bool completado,
        int puntaje,
        int tiempoRestante,
        int intentos,
        string[] errores,
        Action<bool, string, ProgresoUsuarioDTO> callback = null
    )
    {
        StartCoroutine(GuardarProgresoRutina(
            nivel,
            completado,
            puntaje,
            tiempoRestante,
            intentos,
            errores,
            callback
        ));
    }

    private IEnumerator GuardarProgresoRutina(
        int nivel,
        bool completado,
        int puntaje,
        int tiempoRestante,
        int intentos,
        string[] errores,
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

        if (completado && generarReporteAlCompletarNivel && SesionCoincide(tokenSolicitud))
        {
            StartCoroutine(GenerarYGuardarReporteNivelRutina(
                nivel,
                puntaje,
                tiempoRestante,
                intentos,
                errores,
                tokenSolicitud
            ));
        }
    }

    private IEnumerator GenerarYGuardarReporteNivelRutina(
        int nivel,
        int puntaje,
        int tiempoRestante,
        int intentos,
        string[] errores,
        string tokenSolicitud)
    {
        string urlIA = ResolverUrlReporteIA();
        if (string.IsNullOrWhiteSpace(urlIA))
        {
            yield break;
        }

        ReporteIARequest cuerpoIA = new ReporteIARequest
        {
            usuario_nombre = sessionManager != null ? sessionManager.NombreUsuario : "Estudiante",
            nivel = Mathf.Max(1, nivel),
            puntaje = Mathf.Max(0, puntaje),
            tiempo_restante = Mathf.Max(0, tiempoRestante),
            intentos = Mathf.Max(1, intentos),
            completado = true,
            errores = errores != null && errores.Length > 0 ? errores : Array.Empty<string>()
        };

        using UnityWebRequest requestIA = CrearJsonRequest(
            urlIA,
            "POST",
            JsonUtility.ToJson(cuerpoIA),
            false
        );
        requestIA.timeout = Mathf.Clamp(timeoutSegundos * 4, 30, 120);
        requestIA.SetRequestHeader("ngrok-skip-browser-warning", "algolab");

        DebugLog("BACKEND CLIENT: generando reporte IA del nivel " + nivel + ".");
        yield return requestIA.SendWebRequest();

        if (!RespuestaExitosa(requestIA) || !SesionCoincide(tokenSolicitud))
        {
            Debug.LogWarning(
                "REPORTE IA: no se pudo enriquecer el informe. " +
                "El reporte base del backend se conserva."
            );
            yield break;
        }

        ReporteIAResponse respuestaIA;
        try
        {
            respuestaIA = JsonUtility.FromJson<ReporteIAResponse>(requestIA.downloadHandler.text);
        }
        catch (Exception excepcion)
        {
            Debug.LogWarning("REPORTE IA: respuesta inválida: " + excepcion.Message);
            yield break;
        }

        if (respuestaIA == null || string.IsNullOrWhiteSpace(respuestaIA.resumen))
        {
            yield break;
        }

        ActualizarReporteBackendRequest cuerpoBackend = new ActualizarReporteBackendRequest
        {
            dominio = Mathf.Clamp(respuestaIA.dominio, 0, 100),
            resumen = respuestaIA.resumen,
            fortalezas = respuestaIA.fortalezas ?? Array.Empty<string>(),
            aspectosMejora = respuestaIA.aspectos_mejora ?? Array.Empty<string>(),
            recomendaciones = respuestaIA.recomendaciones ?? Array.Empty<string>(),
            puntajeBase = Mathf.Max(0, puntaje),
            tiempoRestanteBase = Mathf.Max(0, tiempoRestante),
            intentosBase = Mathf.Max(1, intentos),
            completadoBase = true
        };

        string urlBackend = CrearUrl("/api/reportes-nivel/" + Mathf.Max(1, nivel) + "/ia");
        using UnityWebRequest requestBackend = CrearJsonRequest(
            urlBackend,
            "PUT",
            JsonUtility.ToJson(cuerpoBackend),
            true
        );
        yield return requestBackend.SendWebRequest();

        if (RespuestaExitosa(requestBackend))
        {
            DebugLog("REPORTE IA: informe guardado para consulta web.");
        }
        else
        {
            Debug.LogWarning("REPORTE IA: se generó, pero no se pudo guardar la versión enriquecida.");
        }
    }

    private string ResolverUrlReporteIA()
    {
        AlgoLabIAClient clienteIA = FindFirstObjectByType<AlgoLabIAClient>(FindObjectsInactive.Include);
        if (clienteIA != null && !string.IsNullOrWhiteSpace(clienteIA.iaApiUrl))
        {
            string configurada = clienteIA.iaApiUrl.Trim();
            int indiceApi = configurada.IndexOf("/api/", StringComparison.OrdinalIgnoreCase);
            if (indiceApi > 0)
            {
                return configurada.Substring(0, indiceApi).TrimEnd('/') + "/api/ia/reporte-nivel";
            }
        }

        if (!Uri.TryCreate(iaReporteUrl, UriKind.Absolute, out Uri uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return "";
        }

        return iaReporteUrl.Trim();
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
        return CrearJsonRequest(url, "POST", json, requiereToken);
    }

    private UnityWebRequest CrearJsonRequest(string url, string metodo, string json, bool requiereToken)
    {
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        UnityWebRequest request = new UnityWebRequest(url, metodo);
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

    /// <summary>
    /// Resuelve una ruta devuelta por el backend (por ejemplo, /api/usuarios/1/avatar)
    /// sin impedir que en el futuro el servidor entregue una URL absoluta de CDN.
    /// Solo admite HTTP/HTTPS para evitar que datos de perfil activen otros esquemas.
    /// </summary>
    public string ResolverUrlBackend(string rutaOUrl)
    {
        if (string.IsNullOrWhiteSpace(rutaOUrl))
        {
            return "";
        }

        string valor = rutaOUrl.Trim();
        if (Uri.TryCreate(valor, UriKind.Absolute, out Uri absoluta))
        {
            return absoluta.Scheme == Uri.UriSchemeHttp || absoluta.Scheme == Uri.UriSchemeHttps
                ? absoluta.AbsoluteUri
                : "";
        }

        if (valor.StartsWith("//", StringComparison.Ordinal))
        {
            return "";
        }

        return CrearUrl("/" + valor.TrimStart('/'));
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
