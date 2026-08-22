using System;
using UnityEngine;

public class AlgoLabSessionManager : MonoBehaviour
{
    public static AlgoLabSessionManager Instance { get; private set; }

    [Header("Estado de sesión")]
    [SerializeField] private bool sesionIniciada = false;
    [SerializeField] private bool modoInvitado = false;

    [Header("Datos del usuario")]
    [SerializeField] private int usuarioId = 0;
    [SerializeField] private string nombreUsuario = "";
    [SerializeField] private string aliasUsuario = "";
    [SerializeField] private string correoUsuario = "";
    [SerializeField] private string rolUsuario = "";
    [SerializeField] private int nivelActual = 1;
    [SerializeField] private int puntaje = 0;
    [SerializeField] private string biografia = "";
    [SerializeField] private string institucion = "";
    [SerializeField] private string programa = "";
    [SerializeField] private string avatar = "orbita";
    [SerializeField] private string avatarUrl = "";
    [SerializeField] private string avatarVersion = "";
    [SerializeField] private bool tutorialCompletado = false;

    [Header("Token")]
    [TextArea(2, 5)]
    [SerializeField] private string tokenJwt = "";

    [Header("Configuración")]
    [Tooltip("Compatibilidad con escenas antiguas. La preferencia Recordar sesión tiene prioridad en tiempo de ejecución.")]
    public bool cargarSesionGuardadaAlIniciar = true;
    [Tooltip("Compatibilidad con escenas antiguas. La preferencia Recordar sesión tiene prioridad en tiempo de ejecución.")]
    public bool guardarSesionEnPlayerPrefs = true;
    public bool mantenerEntreEscenas = true;

    [Header("Persistencia elegida por el usuario")]
    [SerializeField] private bool recordarSesion = false;

    [Header("Debug")]
    public bool mostrarDebug = true;

    public bool SesionIniciada => sesionIniciada;
    public bool ModoInvitado => modoInvitado;
    public bool EstaAutenticado => sesionIniciada && !modoInvitado && !string.IsNullOrWhiteSpace(tokenJwt);
    public bool PuedeGuardarProgreso => EstaAutenticado;

    public int UsuarioId => usuarioId;
    public string NombreUsuario => nombreUsuario;
    public string AliasUsuario => aliasUsuario;
    public string CorreoUsuario => correoUsuario;
    public string RolUsuario => rolUsuario;
    public int NivelActual => nivelActual;
    public int Puntaje => puntaje;
    public string Biografia => biografia;
    public string Institucion => institucion;
    public string Programa => programa;
    public string Avatar => avatar;
    public string AvatarUrl => avatarUrl;
    public string AvatarVersion => avatarVersion;
    public bool TutorialCompletado => tutorialCompletado;
    public string TokenJwt => tokenJwt;
    public bool RecordarSesion => recordarSesion;

    public event Action OnSesionCambiada;
    public event Action OnSesionIniciada;
    public event Action OnSesionInvitado;
    public event Action OnSesionCerrada;
    public event Action<bool> OnRecordarSesionCambiado;

    private const string KEY_SESION_INICIADA = "ALGOLAB_SESION_INICIADA";
    private const string KEY_MODO_INVITADO = "ALGOLAB_MODO_INVITADO";
    private const string KEY_TOKEN = "ALGOLAB_TOKEN";
    private const string KEY_USUARIO_ID = "ALGOLAB_USUARIO_ID";
    private const string KEY_NOMBRE = "ALGOLAB_NOMBRE";
    private const string KEY_ALIAS = "ALGOLAB_ALIAS";
    private const string KEY_CORREO = "ALGOLAB_CORREO";
    private const string KEY_ROL = "ALGOLAB_ROL";
    private const string KEY_NIVEL_ACTUAL = "ALGOLAB_NIVEL_ACTUAL";
    private const string KEY_PUNTAJE = "ALGOLAB_PUNTAJE";
    private const string KEY_BIOGRAFIA = "ALGOLAB_BIOGRAFIA";
    private const string KEY_INSTITUCION = "ALGOLAB_INSTITUCION";
    private const string KEY_PROGRAMA = "ALGOLAB_PROGRAMA";
    private const string KEY_AVATAR = "ALGOLAB_AVATAR";
    private const string KEY_AVATAR_URL = "ALGOLAB_AVATAR_URL";
    private const string KEY_AVATAR_VERSION = "ALGOLAB_AVATAR_VERSION";
    private const string KEY_TUTORIAL_COMPLETADO = "ALGOLAB_TUTORIAL_COMPLETADO";
    private const string KEY_RECORDAR_SESION = "ALGOLAB_RECORDAR_SESION";

    [Serializable]
    public class UsuarioSesion
    {
        public int id;
        public string nombre;
        public string nombreUsuario;
        public string correo;
        public string rol;
        public int nivelActual = 1;
        public int puntaje = 0;
        public string biografia;
        public string institucion;
        public string programa;
        public string avatar;
        public string avatarUrl;
        public string avatarVersion;
        public bool tutorialCompletado;
    }

    private void Awake()
    {
        if (!ConfigurarSingleton())
        {
            return;
        }

        CargarPreferenciaRecordarSesion();

        if (recordarSesion && cargarSesionGuardadaAlIniciar)
        {
            CargarSesionGuardada();
        }
        else
        {
            // Migración segura: las versiones anteriores guardaban siempre el JWT.
            // Si el usuario no activó expresamente "Recordar sesión", se elimina
            // cualquier credencial antigua antes de mostrar la pantalla inicial.
            BorrarSesionGuardada();
            LimpiarDatosEnMemoria();
        }
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

    public void IniciarSesionConUsuario(string token, UsuarioSesion usuario)
    {
        if (usuario == null)
        {
            Debug.LogError("SESSION MANAGER: No se puede iniciar sesión porque el usuario llegó vacío.");
            return;
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            Debug.LogError("SESSION MANAGER: No se puede iniciar sesión porque el token llegó vacío.");
            return;
        }

        sesionIniciada = true;
        modoInvitado = false;

        tokenJwt = token.Trim();
        usuarioId = usuario.id;
        nombreUsuario = TextoSeguro(usuario.nombre);
        aliasUsuario = TextoSeguro(usuario.nombreUsuario);
        correoUsuario = TextoSeguro(usuario.correo);
        rolUsuario = TextoSeguro(usuario.rol);
        // Un inicio de sesión nuevo reemplaza por completo la identidad previa.
        // Conservar el máximo anterior filtraba progreso entre dos cuentas si el
        // usuario cambiaba de credenciales tras una validación temporal fallida.
        nivelActual = Mathf.Max(1, usuario.nivelActual);
        puntaje = Mathf.Max(0, usuario.puntaje);
        biografia = TextoSeguro(usuario.biografia);
        institucion = TextoSeguro(usuario.institucion);
        programa = TextoSeguro(usuario.programa);
        avatar = string.IsNullOrWhiteSpace(usuario.avatar) ? "orbita" : usuario.avatar.Trim();
        avatarUrl = TextoSeguro(usuario.avatarUrl);
        avatarVersion = TextoSeguro(usuario.avatarVersion);
        tutorialCompletado = usuario.tutorialCompletado;

        if (DebePersistirSesion())
        {
            GuardarSesionAutenticada();
        }
        else
        {
            BorrarSesionGuardada();
        }

        DebugLog("SESSION MANAGER: sesión iniciada como usuario: " + nombreUsuario);

        OnSesionCambiada?.Invoke();
        OnSesionIniciada?.Invoke();
    }

    public void IniciarComoInvitado()
    {
        sesionIniciada = true;
        modoInvitado = true;

        tokenJwt = "";
        usuarioId = 0;
        nombreUsuario = "Invitado";
        aliasUsuario = "";
        correoUsuario = "";
        rolUsuario = "INVITADO";
        nivelActual = 1;
        puntaje = 0;
        biografia = "";
        institucion = "";
        programa = "";
        avatar = "orbita";
        avatarUrl = "";
        avatarVersion = "";
        tutorialCompletado = false;

        // IMPORTANTE:
        // El invitado NO se guarda.
        // Si había una sesión invitada vieja guardada, se elimina.
        BorrarSesionGuardada();

        DebugLog("SESSION MANAGER: sesión iniciada como invitado. No se guardará al cerrar el juego.");

        OnSesionCambiada?.Invoke();
        OnSesionInvitado?.Invoke();
    }

    [ContextMenu("Cerrar sesión")]
    public void CerrarSesion()
    {
        sesionIniciada = false;
        modoInvitado = false;

        tokenJwt = "";
        usuarioId = 0;
        nombreUsuario = "";
        aliasUsuario = "";
        correoUsuario = "";
        rolUsuario = "";
        nivelActual = 1;
        puntaje = 0;
        biografia = "";
        institucion = "";
        programa = "";
        avatar = "orbita";
        avatarUrl = "";
        avatarVersion = "";
        tutorialCompletado = false;

        BorrarSesionGuardada();

        DebugLog("SESSION MANAGER: sesión cerrada.");

        OnSesionCambiada?.Invoke();
        OnSesionCerrada?.Invoke();
    }

    public void ActualizarProgresoLocal(int nuevoNivelActual, int nuevoPuntaje)
    {
        if (!EstaAutenticado)
        {
            return;
        }

        int nivelNormalizado = Mathf.Max(nivelActual, Mathf.Max(1, nuevoNivelActual));
        int puntajeNormalizado = Mathf.Max(puntaje, Mathf.Max(0, nuevoPuntaje));

        if (nivelActual == nivelNormalizado && puntaje == puntajeNormalizado)
        {
            return;
        }

        nivelActual = nivelNormalizado;
        puntaje = puntajeNormalizado;

        if (DebePersistirSesion())
        {
            GuardarSesionAutenticada();
        }

        DebugLog("SESSION MANAGER: progreso local actualizado. Nivel: " + nivelActual + " Puntaje: " + puntaje);

        OnSesionCambiada?.Invoke();
    }

    public void ActualizarUsuarioLocal(UsuarioSesion usuario)
    {
        if (usuario == null || !EstaAutenticado)
        {
            return;
        }

        if (usuarioId > 0 && usuario.id > 0 && usuario.id != usuarioId)
        {
            Debug.LogWarning(
                "SESSION MANAGER: se ignoró un perfil que no pertenece a la sesión actual."
            );
            return;
        }

        usuarioId = usuario.id;
        nombreUsuario = TextoSeguro(usuario.nombre);
        aliasUsuario = TextoSeguro(usuario.nombreUsuario);
        correoUsuario = TextoSeguro(usuario.correo);
        rolUsuario = TextoSeguro(usuario.rol);
        // Dentro de la misma cuenta el progreso es monotónico. Así una respuesta
        // /me más antigua no puede pisar el resultado posterior de /progreso/me.
        nivelActual = Mathf.Max(nivelActual, Mathf.Max(1, usuario.nivelActual));
        puntaje = Mathf.Max(puntaje, Mathf.Max(0, usuario.puntaje));
        biografia = TextoSeguro(usuario.biografia);
        institucion = TextoSeguro(usuario.institucion);
        programa = TextoSeguro(usuario.programa);
        avatar = string.IsNullOrWhiteSpace(usuario.avatar) ? "orbita" : usuario.avatar.Trim();
        avatarUrl = TextoSeguro(usuario.avatarUrl);
        avatarVersion = TextoSeguro(usuario.avatarVersion);
        // El tutorial es un hito monotónico: una respuesta antigua o temporal
        // del servidor nunca debe hacer que vuelva a mostrarse en esta sesión.
        tutorialCompletado = tutorialCompletado || usuario.tutorialCompletado;

        if (DebePersistirSesion())
        {
            GuardarSesionAutenticada();
        }

        DebugLog("SESSION MANAGER: datos del usuario actualizados.");

        OnSesionCambiada?.Invoke();
    }

    public void MarcarTutorialCompletadoLocalmente()
    {
        if (!EstaAutenticado || tutorialCompletado)
        {
            return;
        }

        tutorialCompletado = true;

        if (DebePersistirSesion())
        {
            GuardarSesionAutenticada();
        }

        DebugLog("SESSION MANAGER: tutorial principal marcado como completado.");
        OnSesionCambiada?.Invoke();
    }

    public string ObtenerAuthorizationHeader()
    {
        if (!EstaAutenticado)
        {
            return "";
        }

        return "Bearer " + tokenJwt;
    }

    public bool NivelDesbloqueado(int nivel)
    {
        if (modoInvitado)
        {
            return true;
        }

        return nivel <= nivelActual;
    }

    /// <summary>
    /// Decide si la sesión autenticada debe sobrevivir al cierre de la app.
    /// Desactivarlo borra inmediatamente el JWT persistido, pero conserva la
    /// sesión actual en memoria hasta que la aplicación se cierre.
    /// </summary>
    public void EstablecerRecordarSesion(bool recordar)
    {
        recordarSesion = recordar;
        cargarSesionGuardadaAlIniciar = recordar;
        guardarSesionEnPlayerPrefs = recordar;

        PlayerPrefs.SetInt(KEY_RECORDAR_SESION, recordar ? 1 : 0);

        if (recordar && EstaAutenticado)
        {
            GuardarSesionAutenticada();
        }
        else if (!recordar)
        {
            BorrarSesionGuardada();
        }
        else
        {
            PlayerPrefs.Save();
        }

        DebugLog(
            recordar
                ? "SESSION MANAGER: Recordar sesión activado."
                : "SESSION MANAGER: Recordar sesión desactivado; el acceso actual solo vive en memoria."
        );

        OnRecordarSesionCambiado?.Invoke(recordarSesion);
    }

    public void AlternarRecordarSesion()
    {
        EstablecerRecordarSesion(!recordarSesion);
    }

    private bool DebePersistirSesion()
    {
        return recordarSesion && guardarSesionEnPlayerPrefs && EstaAutenticado;
    }

    private void CargarPreferenciaRecordarSesion()
    {
        recordarSesion = PlayerPrefs.GetInt(KEY_RECORDAR_SESION, 0) == 1;

        // Los valores serializados de escenas antiguas ya no fuerzan la
        // persistencia. Se mantienen públicos solo para no romper prefabs.
        cargarSesionGuardadaAlIniciar = recordarSesion;
        guardarSesionEnPlayerPrefs = recordarSesion;
    }

    private void OnApplicationPause(bool pausada)
    {
        if (pausada && !recordarSesion)
        {
            BorrarSesionGuardada();
        }
    }

    private void OnApplicationQuit()
    {
        if (!recordarSesion)
        {
            BorrarSesionGuardada();
        }
    }

    private void GuardarSesionAutenticada()
    {
        if (modoInvitado)
        {
            DebugLog("SESSION MANAGER: no se guarda sesión porque es invitado.");
            return;
        }

        if (string.IsNullOrWhiteSpace(tokenJwt))
        {
            DebugLog("SESSION MANAGER: no se guarda sesión porque no hay token.");
            return;
        }

        PlayerPrefs.SetInt(KEY_SESION_INICIADA, 1);
        PlayerPrefs.SetInt(KEY_MODO_INVITADO, 0);
        PlayerPrefs.SetString(KEY_TOKEN, tokenJwt);
        PlayerPrefs.SetInt(KEY_USUARIO_ID, usuarioId);
        PlayerPrefs.SetString(KEY_NOMBRE, TextoSeguro(nombreUsuario));
        PlayerPrefs.SetString(KEY_ALIAS, TextoSeguro(aliasUsuario));
        PlayerPrefs.SetString(KEY_CORREO, TextoSeguro(correoUsuario));
        PlayerPrefs.SetString(KEY_ROL, TextoSeguro(rolUsuario));
        PlayerPrefs.SetInt(KEY_NIVEL_ACTUAL, nivelActual);
        PlayerPrefs.SetInt(KEY_PUNTAJE, puntaje);
        PlayerPrefs.SetString(KEY_BIOGRAFIA, TextoSeguro(biografia));
        PlayerPrefs.SetString(KEY_INSTITUCION, TextoSeguro(institucion));
        PlayerPrefs.SetString(KEY_PROGRAMA, TextoSeguro(programa));
        PlayerPrefs.SetString(KEY_AVATAR, string.IsNullOrWhiteSpace(avatar) ? "orbita" : avatar);
        PlayerPrefs.SetString(KEY_AVATAR_URL, TextoSeguro(avatarUrl));
        PlayerPrefs.SetString(KEY_AVATAR_VERSION, TextoSeguro(avatarVersion));
        PlayerPrefs.SetInt(KEY_TUTORIAL_COMPLETADO, tutorialCompletado ? 1 : 0);
        PlayerPrefs.Save();

        DebugLog("SESSION MANAGER: sesión autenticada guardada en PlayerPrefs.");
    }

    private void CargarSesionGuardada()
    {
        bool existeSesion = PlayerPrefs.GetInt(KEY_SESION_INICIADA, 0) == 1;

        if (!existeSesion)
        {
            DebugLog("SESSION MANAGER: no hay sesión guardada.");
            return;
        }

        bool eraInvitado = PlayerPrefs.GetInt(KEY_MODO_INVITADO, 0) == 1;

        if (eraInvitado)
        {
            DebugLog("SESSION MANAGER: había invitado guardado. Se elimina porque invitado no debe persistir.");
            BorrarSesionGuardada();
            LimpiarDatosEnMemoria();
            return;
        }

        string tokenGuardado = PlayerPrefs.GetString(KEY_TOKEN, "");

        if (string.IsNullOrWhiteSpace(tokenGuardado))
        {
            DebugLog("SESSION MANAGER: había sesión guardada sin token. Se elimina.");
            BorrarSesionGuardada();
            LimpiarDatosEnMemoria();
            return;
        }

        sesionIniciada = true;
        modoInvitado = false;

        tokenJwt = tokenGuardado.Trim();
        usuarioId = Mathf.Max(0, PlayerPrefs.GetInt(KEY_USUARIO_ID, 0));
        nombreUsuario = TextoSeguro(PlayerPrefs.GetString(KEY_NOMBRE, ""));
        aliasUsuario = TextoSeguro(PlayerPrefs.GetString(KEY_ALIAS, ""));
        correoUsuario = TextoSeguro(PlayerPrefs.GetString(KEY_CORREO, ""));
        rolUsuario = TextoSeguro(PlayerPrefs.GetString(KEY_ROL, ""));
        nivelActual = Mathf.Max(1, PlayerPrefs.GetInt(KEY_NIVEL_ACTUAL, 1));
        puntaje = Mathf.Max(0, PlayerPrefs.GetInt(KEY_PUNTAJE, 0));
        biografia = TextoSeguro(PlayerPrefs.GetString(KEY_BIOGRAFIA, ""));
        institucion = TextoSeguro(PlayerPrefs.GetString(KEY_INSTITUCION, ""));
        programa = TextoSeguro(PlayerPrefs.GetString(KEY_PROGRAMA, ""));
        avatar = TextoSeguro(PlayerPrefs.GetString(KEY_AVATAR, "orbita"));
        avatarUrl = TextoSeguro(PlayerPrefs.GetString(KEY_AVATAR_URL, ""));
        avatarVersion = TextoSeguro(PlayerPrefs.GetString(KEY_AVATAR_VERSION, ""));
        tutorialCompletado = PlayerPrefs.GetInt(KEY_TUTORIAL_COMPLETADO, 0) == 1;

        DebugLog("SESSION MANAGER: sesión autenticada cargada. Usuario: " + nombreUsuario);

        OnSesionCambiada?.Invoke();
    }

    private void LimpiarDatosEnMemoria()
    {
        sesionIniciada = false;
        modoInvitado = false;

        tokenJwt = "";
        usuarioId = 0;
        nombreUsuario = "";
        aliasUsuario = "";
        correoUsuario = "";
        rolUsuario = "";
        nivelActual = 1;
        puntaje = 0;
        biografia = "";
        institucion = "";
        programa = "";
        avatar = "orbita";
        avatarUrl = "";
        avatarVersion = "";
        tutorialCompletado = false;
    }

    [ContextMenu("Borrar sesión guardada")]
    public void BorrarSesionGuardada()
    {
        PlayerPrefs.DeleteKey(KEY_SESION_INICIADA);
        PlayerPrefs.DeleteKey(KEY_MODO_INVITADO);
        PlayerPrefs.DeleteKey(KEY_TOKEN);
        PlayerPrefs.DeleteKey(KEY_USUARIO_ID);
        PlayerPrefs.DeleteKey(KEY_NOMBRE);
        PlayerPrefs.DeleteKey(KEY_ALIAS);
        PlayerPrefs.DeleteKey(KEY_CORREO);
        PlayerPrefs.DeleteKey(KEY_ROL);
        PlayerPrefs.DeleteKey(KEY_NIVEL_ACTUAL);
        PlayerPrefs.DeleteKey(KEY_PUNTAJE);
        PlayerPrefs.DeleteKey(KEY_BIOGRAFIA);
        PlayerPrefs.DeleteKey(KEY_INSTITUCION);
        PlayerPrefs.DeleteKey(KEY_PROGRAMA);
        PlayerPrefs.DeleteKey(KEY_AVATAR);
        PlayerPrefs.DeleteKey(KEY_AVATAR_URL);
        PlayerPrefs.DeleteKey(KEY_AVATAR_VERSION);
        PlayerPrefs.DeleteKey(KEY_TUTORIAL_COMPLETADO);
        PlayerPrefs.Save();

        DebugLog("SESSION MANAGER: sesión eliminada de PlayerPrefs.");
    }

    [ContextMenu("Borrar todos los datos de sesión de AlgoLab")]
    public void BorrarTodosLosPlayerPrefs()
    {
        // Conserva preferencias de gráficos, audio y cualquier paquete externo.
        // Este método mantiene su nombre público por compatibilidad con eventos ya serializados.
        BorrarSesionGuardada();
        LimpiarDatosEnMemoria();

        DebugLog("SESSION MANAGER: se eliminaron únicamente los datos de sesión de AlgoLab.");

        OnSesionCambiada?.Invoke();
        OnSesionCerrada?.Invoke();
    }

    private void DebugLog(string mensaje)
    {
        if (mostrarDebug)
        {
            Debug.Log(mensaje);
        }
    }

    private static string TextoSeguro(string valor)
    {
        return valor ?? string.Empty;
    }
}
