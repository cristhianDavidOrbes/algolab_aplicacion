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
    [SerializeField] private string correoUsuario = "";
    [SerializeField] private string rolUsuario = "";
    [SerializeField] private int nivelActual = 1;
    [SerializeField] private int puntaje = 0;

    [Header("Token")]
    [TextArea(2, 5)]
    [SerializeField] private string tokenJwt = "";

    [Header("Configuración")]
    public bool cargarSesionGuardadaAlIniciar = true;
    public bool guardarSesionEnPlayerPrefs = true;
    public bool mantenerEntreEscenas = true;

    [Header("Debug")]
    public bool mostrarDebug = true;

    public bool SesionIniciada => sesionIniciada;
    public bool ModoInvitado => modoInvitado;
    public bool EstaAutenticado => sesionIniciada && !modoInvitado && !string.IsNullOrWhiteSpace(tokenJwt);
    public bool PuedeGuardarProgreso => EstaAutenticado;

    public int UsuarioId => usuarioId;
    public string NombreUsuario => nombreUsuario;
    public string CorreoUsuario => correoUsuario;
    public string RolUsuario => rolUsuario;
    public int NivelActual => nivelActual;
    public int Puntaje => puntaje;
    public string TokenJwt => tokenJwt;

    public event Action OnSesionCambiada;
    public event Action OnSesionIniciada;
    public event Action OnSesionInvitado;
    public event Action OnSesionCerrada;

    private const string KEY_SESION_INICIADA = "ALGOLAB_SESION_INICIADA";
    private const string KEY_MODO_INVITADO = "ALGOLAB_MODO_INVITADO";
    private const string KEY_TOKEN = "ALGOLAB_TOKEN";
    private const string KEY_USUARIO_ID = "ALGOLAB_USUARIO_ID";
    private const string KEY_NOMBRE = "ALGOLAB_NOMBRE";
    private const string KEY_CORREO = "ALGOLAB_CORREO";
    private const string KEY_ROL = "ALGOLAB_ROL";
    private const string KEY_NIVEL_ACTUAL = "ALGOLAB_NIVEL_ACTUAL";
    private const string KEY_PUNTAJE = "ALGOLAB_PUNTAJE";

    [Serializable]
    public class UsuarioSesion
    {
        public int id;
        public string nombre;
        public string correo;
        public string rol;
        public int nivelActual = 1;
        public int puntaje = 0;
    }

    private void Awake()
    {
        if (!ConfigurarSingleton())
        {
            return;
        }

        if (cargarSesionGuardadaAlIniciar)
        {
            CargarSesionGuardada();
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
        correoUsuario = TextoSeguro(usuario.correo);
        rolUsuario = TextoSeguro(usuario.rol);
        nivelActual = Mathf.Max(1, usuario.nivelActual);
        puntaje = Mathf.Max(0, usuario.puntaje);

        if (guardarSesionEnPlayerPrefs)
        {
            GuardarSesionAutenticada();
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
        correoUsuario = "";
        rolUsuario = "INVITADO";
        nivelActual = 1;
        puntaje = 0;

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
        correoUsuario = "";
        rolUsuario = "";
        nivelActual = 1;
        puntaje = 0;

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

        if (guardarSesionEnPlayerPrefs && EstaAutenticado)
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

        usuarioId = usuario.id;
        nombreUsuario = TextoSeguro(usuario.nombre);
        correoUsuario = TextoSeguro(usuario.correo);
        rolUsuario = TextoSeguro(usuario.rol);
        nivelActual = Mathf.Max(1, usuario.nivelActual);
        puntaje = Mathf.Max(0, usuario.puntaje);

        if (guardarSesionEnPlayerPrefs && EstaAutenticado)
        {
            GuardarSesionAutenticada();
        }

        DebugLog("SESSION MANAGER: datos del usuario actualizados.");

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
        PlayerPrefs.SetString(KEY_CORREO, TextoSeguro(correoUsuario));
        PlayerPrefs.SetString(KEY_ROL, TextoSeguro(rolUsuario));
        PlayerPrefs.SetInt(KEY_NIVEL_ACTUAL, nivelActual);
        PlayerPrefs.SetInt(KEY_PUNTAJE, puntaje);
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
        correoUsuario = TextoSeguro(PlayerPrefs.GetString(KEY_CORREO, ""));
        rolUsuario = TextoSeguro(PlayerPrefs.GetString(KEY_ROL, ""));
        nivelActual = Mathf.Max(1, PlayerPrefs.GetInt(KEY_NIVEL_ACTUAL, 1));
        puntaje = Mathf.Max(0, PlayerPrefs.GetInt(KEY_PUNTAJE, 0));

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
        correoUsuario = "";
        rolUsuario = "";
        nivelActual = 1;
        puntaje = 0;
    }

    [ContextMenu("Borrar sesión guardada")]
    public void BorrarSesionGuardada()
    {
        PlayerPrefs.DeleteKey(KEY_SESION_INICIADA);
        PlayerPrefs.DeleteKey(KEY_MODO_INVITADO);
        PlayerPrefs.DeleteKey(KEY_TOKEN);
        PlayerPrefs.DeleteKey(KEY_USUARIO_ID);
        PlayerPrefs.DeleteKey(KEY_NOMBRE);
        PlayerPrefs.DeleteKey(KEY_CORREO);
        PlayerPrefs.DeleteKey(KEY_ROL);
        PlayerPrefs.DeleteKey(KEY_NIVEL_ACTUAL);
        PlayerPrefs.DeleteKey(KEY_PUNTAJE);
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
