using System;
using System.Reflection;
using System.Collections;
using UnityEngine;

[DefaultExecutionOrder(100)]
public class ProgressPanelSessionBinder : MonoBehaviour
{
    [Header("Referencias")]
    public AlgoLabProgressPanel progressPanel;

    [Tooltip("Arrastra aquí tu SessionManager o AlgoLabSessionManager.")]
    public MonoBehaviour sessionManager;

    [Header("Aplicación automática")]
    public bool aplicarAlIniciar = true;
    public bool actualizarPeriodicamente = true;
    public float intervaloActualizacion = 0.75f;

    [Header("Invitado")]
    public bool aplicarInvitadoCuandoEsModoInvitado = true;
    public bool aplicarInvitadoSiNoHaySesion = false;
    public string nombreInvitado = "Invitado";
    public string categoriaInvitado = "Junior";
    public int nivelBackendInvitado = 1;
    public int puntajeInvitado = 0;

    [Header("Usuario autenticado")]
    public string categoriaPorDefecto = "Junior";

    [Tooltip("Cuando viene de login/backend se sincroniza directo sin animar.")]
    public bool sincronizarBackendSinAnimar = true;

    [Tooltip("Cuando se llama AplicarProgresoGuardadoDesdeBackend puede animar el avance visual.")]
    public bool animarCuandoSeGuardaProgreso = true;

    [Header("Debug")]
    public bool mostrarDebug = true;

    private string ultimoNombreAplicado = "";
    private int ultimoNivelBackendAplicado = -1;
    private int ultimoPuntajeAplicado = -1;
    private bool ultimoFueInvitado = false;
    private Coroutine rutinaActualizacion;
    private Coroutine rutinaAplicacionInicial;
    private AlgoLabSessionManager sessionManagerTipado;

    private void Awake()
    {
        BuscarReferencias();
    }

    private void OnEnable()
    {
        BuscarReferencias();
        ConectarEventosSesion();

        if (aplicarAlIniciar)
        {
            rutinaAplicacionInicial = StartCoroutine(AplicarDespuesDeFrame());
        }

        if (actualizarPeriodicamente && sessionManagerTipado == null)
        {
            if (rutinaActualizacion != null)
            {
                StopCoroutine(rutinaActualizacion);
            }

            rutinaActualizacion = StartCoroutine(ActualizarPeriodicamenteRutina());
        }
    }

    private void OnDisable()
    {
        DesconectarEventosSesion();

        if (rutinaAplicacionInicial != null)
        {
            StopCoroutine(rutinaAplicacionInicial);
            rutinaAplicacionInicial = null;
        }

        if (rutinaActualizacion != null)
        {
            StopCoroutine(rutinaActualizacion);
            rutinaActualizacion = null;
        }
    }

    private IEnumerator AplicarDespuesDeFrame()
    {
        yield return null;
        yield return null;

        ActualizarDesdeSesion(true);
        rutinaAplicacionInicial = null;
    }

    private IEnumerator ActualizarPeriodicamenteRutina()
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(Mathf.Max(0.1f, intervaloActualizacion));
            ActualizarDesdeSesion(false);
        }
    }

    [ContextMenu("Buscar referencias")]
    public void BuscarReferencias()
    {
        if (progressPanel == null)
        {
            progressPanel = FindFirstObjectByType<AlgoLabProgressPanel>(
                FindObjectsInactive.Include
            );
        }

        if (sessionManager == null)
        {
            sessionManager = BuscarSessionManager();
        }

        ConectarEventosSesion();
    }

    private MonoBehaviour BuscarSessionManager()
    {
        if (AlgoLabSessionManager.Instance != null)
        {
            return AlgoLabSessionManager.Instance;
        }

        MonoBehaviour[] componentes = FindObjectsByType<MonoBehaviour>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < componentes.Length; i++)
        {
            if (componentes[i] == null)
            {
                continue;
            }

            string nombreTipo = componentes[i].GetType().Name;
            string nombreObjeto = componentes[i].gameObject.name;

            if (nombreTipo == "AlgoLabSessionManager" ||
                nombreTipo == "SessionManager" ||
                nombreObjeto == "SessionManager" ||
                nombreObjeto == "AlgoLabSessionManager")
            {
                return componentes[i];
            }
        }

        return null;
    }

    private void ConectarEventosSesion()
    {
        AlgoLabSessionManager nuevo = sessionManager as AlgoLabSessionManager;
        if (nuevo == sessionManagerTipado)
        {
            return;
        }

        DesconectarEventosSesion();
        sessionManagerTipado = nuevo;

        if (sessionManagerTipado != null && isActiveAndEnabled)
        {
            sessionManagerTipado.OnSesionCambiada += AlCambiarSesion;
        }
    }

    private void DesconectarEventosSesion()
    {
        if (sessionManagerTipado != null)
        {
            sessionManagerTipado.OnSesionCambiada -= AlCambiarSesion;
        }

        sessionManagerTipado = null;
    }

    private void AlCambiarSesion()
    {
        ActualizarDesdeSesion(true);
    }

    [ContextMenu("Actualizar desde sesión")]
    public void ActualizarDesdeSesionManual()
    {
        ActualizarDesdeSesion(true);
    }

    public void ActualizarDesdeSesion(bool forzar)
    {
        BuscarReferencias();

        if (progressPanel == null)
        {
            DebugLog("PROGRESS BINDER: no hay ProgressPanel asignado.");
            return;
        }

        DatosSesion datos = LeerDatosSesion();

        if (!datos.haySesion && !datos.esInvitado)
        {
            if (aplicarInvitadoSiNoHaySesion)
            {
                AplicarInvitado(forzar);
            }

            return;
        }

        if (datos.esInvitado && !datos.autenticado)
        {
            if (aplicarInvitadoCuandoEsModoInvitado)
            {
                AplicarInvitado(forzar);
            }

            return;
        }

        AplicarUsuarioAutenticado(datos, forzar);
    }

    private void AplicarUsuarioAutenticado(DatosSesion datos, bool forzar)
    {
        string nombre = datos.nombre;

        if (string.IsNullOrWhiteSpace(nombre))
        {
            nombre = "Usuario";
        }

        int nivelBackend = Mathf.Max(1, datos.nivelActualBackend);
        int puntaje = Mathf.Max(0, datos.puntajeTotal);
        int nivelVisual = ConvertirNivelBackendAIndiceVisual(nivelBackend);

        bool cambio =
            nombre != ultimoNombreAplicado ||
            nivelBackend != ultimoNivelBackendAplicado ||
            puntaje != ultimoPuntajeAplicado ||
            ultimoFueInvitado;

        if (!forzar && !cambio)
        {
            return;
        }

        progressPanel.AplicarDatosUsuarioDesdeBackend(
            nombre,
            categoriaPorDefecto,
            null
        );

        progressPanel.SetPuntaje(puntaje);

        if (sincronizarBackendSinAnimar)
        {
            progressPanel.SetNivelActual(nivelVisual);
        }
        else
        {
            progressPanel.SetNivelActualConAnimacion(nivelVisual);
        }

        progressPanel.ActualizarTodo();

        ultimoNombreAplicado = nombre;
        ultimoNivelBackendAplicado = nivelBackend;
        ultimoPuntajeAplicado = puntaje;
        ultimoFueInvitado = false;

        DebugLog(
            "PROGRESS BINDER: usuario autenticado aplicado. Nombre: " +
            nombre +
            " | Nivel backend: " +
            nivelBackend +
            " | Nivel visual: " +
            nivelVisual +
            " | Puntaje: " +
            puntaje
        );
    }

    public void AplicarInvitado(bool forzar = true)
    {
        if (progressPanel == null)
        {
            BuscarReferencias();
        }

        if (progressPanel == null)
        {
            return;
        }

        int nivelBackend = Mathf.Max(1, nivelBackendInvitado);
        int nivelVisual = ConvertirNivelBackendAIndiceVisual(nivelBackend);
        int puntaje = Mathf.Max(0, puntajeInvitado);

        bool cambio =
            !ultimoFueInvitado ||
            ultimoNombreAplicado != nombreInvitado ||
            ultimoNivelBackendAplicado != nivelBackend ||
            ultimoPuntajeAplicado != puntaje;

        if (!forzar && !cambio)
        {
            return;
        }

        progressPanel.AplicarDatosUsuarioDesdeBackend(
            nombreInvitado,
            categoriaInvitado,
            null
        );

        progressPanel.SetPuntaje(puntaje);
        progressPanel.SetNivelActual(nivelVisual);
        progressPanel.ActualizarTodo();

        ultimoNombreAplicado = nombreInvitado;
        ultimoNivelBackendAplicado = nivelBackend;
        ultimoPuntajeAplicado = puntaje;
        ultimoFueInvitado = true;

        DebugLog(
            "PROGRESS BINDER: invitado aplicado. Nivel backend: " +
            nivelBackend +
            " | Nivel visual: " +
            nivelVisual +
            " | Puntaje: " +
            puntaje
        );
    }

    public void AplicarProgresoGuardadoDesdeBackend(int nivelActualBackend, int puntajeTotalBackend)
    {
        if (progressPanel == null)
        {
            BuscarReferencias();
        }

        if (progressPanel == null)
        {
            DebugLog("PROGRESS BINDER: no se puede aplicar progreso porque no hay panel.");
            return;
        }

        int nivelBackend = Mathf.Max(1, nivelActualBackend);
        int puntaje = Mathf.Max(0, puntajeTotalBackend);
        int nivelVisual = ConvertirNivelBackendAIndiceVisual(nivelBackend);

        progressPanel.SetPuntaje(puntaje);

        if (animarCuandoSeGuardaProgreso)
        {
            progressPanel.SetNivelActualConAnimacion(nivelVisual);
        }
        else
        {
            progressPanel.SetNivelActual(nivelVisual);
        }

        progressPanel.ActualizarTodo();

        ultimoNivelBackendAplicado = nivelBackend;
        ultimoPuntajeAplicado = puntaje;
        ultimoFueInvitado = false;

        DebugLog(
            "PROGRESS BINDER: progreso guardado aplicado. Nivel backend: " +
            nivelBackend +
            " | Nivel visual: " +
            nivelVisual +
            " | Puntaje total: " +
            puntaje
        );
    }

    public void AplicarProgresoGuardadoDesdeBackend(object respuestaBackend)
    {
        if (respuestaBackend == null)
        {
            return;
        }

        int nivelBackend = LeerEnteroDesdeObjeto(
            respuestaBackend,
            1,
            "nivelActual",
            "NivelActual",
            "currentLevel",
            "CurrentLevel"
        );

        int puntaje = LeerEnteroDesdeObjeto(
            respuestaBackend,
            0,
            "puntajeTotal",
            "PuntajeTotal",
            "puntaje",
            "Puntaje",
            "score",
            "Score"
        );

        AplicarProgresoGuardadoDesdeBackend(nivelBackend, puntaje);
    }

    public int ConvertirNivelBackendAIndiceVisual(int nivelBackend)
    {
        return Mathf.Max(0, nivelBackend - 1);
    }

    private DatosSesion LeerDatosSesion()
    {
        DatosSesion datos = new DatosSesion();

        if (sessionManager == null)
        {
            sessionManager = BuscarSessionManager();
        }

        if (sessionManager == null)
        {
            datos.haySesion = false;
            return datos;
        }

        object manager = sessionManager;
        object usuario = LeerObjetoUsuario(manager);

        datos.esInvitado = LeerBooleanoDesdeObjeto(
            manager,
            false,
            "EsInvitado",
            "esInvitado",
            "ModoInvitado",
            "modoInvitado",
            "IsGuest",
            "isGuest",
            "GuestMode",
            "guestMode"
        );

        string token = LeerTextoDesdeObjeto(
            manager,
            "",
            "Token",
            "token",
            "JwtToken",
            "jwtToken",
            "AccessToken",
            "accessToken"
        );

        bool autenticadoExplicito = LeerBooleanoDesdeObjeto(
            manager,
            false,
            "EstaAutenticado",
            "estaAutenticado",
            "Autenticado",
            "autenticado",
            "SesionActiva",
            "sesionActiva",
            "HaySesion",
            "haySesion",
            "IsLoggedIn",
            "isLoggedIn",
            "LoggedIn",
            "loggedIn"
        );

        datos.autenticado =
            autenticadoExplicito ||
            !string.IsNullOrWhiteSpace(token) ||
            (usuario != null && !datos.esInvitado);

        datos.haySesion = datos.autenticado || datos.esInvitado || usuario != null;

        string nombre = LeerTextoDesdeObjeto(
            manager,
            "",
            "NombreUsuario",
            "nombreUsuario",
            "Nombre",
            "nombre",
            "UserName",
            "username",
            "UsuarioNombre",
            "usuarioNombre"
        );

        if (string.IsNullOrWhiteSpace(nombre) && usuario != null)
        {
            nombre = LeerTextoDesdeObjeto(
                usuario,
                "",
                "nombre",
                "Nombre",
                "nombreUsuario",
                "NombreUsuario",
                "username",
                "UserName",
                "correo",
                "Correo",
                "email",
                "Email"
            );
        }

        string correo = "";

        if (usuario != null)
        {
            correo = LeerTextoDesdeObjeto(
                usuario,
                "",
                "correo",
                "Correo",
                "email",
                "Email"
            );
        }

        if (string.IsNullOrWhiteSpace(nombre) && !string.IsNullOrWhiteSpace(correo))
        {
            int indiceArroba = correo.IndexOf("@", StringComparison.Ordinal);

            if (indiceArroba > 0)
            {
                nombre = correo.Substring(0, indiceArroba);
            }
            else
            {
                nombre = correo;
            }
        }

        datos.nombre = nombre;

        int nivelManager = LeerEnteroDesdeObjeto(
            manager,
            -1,
            "NivelActual",
            "nivelActual",
            "CurrentLevel",
            "currentLevel",
            "Nivel",
            "nivel"
        );

        int puntajeManager = LeerEnteroDesdeObjeto(
            manager,
            -1,
            "Puntaje",
            "puntaje",
            "PuntajeTotal",
            "puntajeTotal",
            "Score",
            "score"
        );

        int nivelUsuario = -1;
        int puntajeUsuario = -1;

        if (usuario != null)
        {
            nivelUsuario = LeerEnteroDesdeObjeto(
                usuario,
                -1,
                "nivelActual",
                "NivelActual",
                "currentLevel",
                "CurrentLevel",
                "nivel",
                "Nivel"
            );

            puntajeUsuario = LeerEnteroDesdeObjeto(
                usuario,
                -1,
                "puntaje",
                "Puntaje",
                "puntajeTotal",
                "PuntajeTotal",
                "score",
                "Score"
            );
        }

        datos.nivelActualBackend = nivelManager > 0 ? nivelManager : nivelUsuario;
        datos.puntajeTotal = puntajeManager >= 0 ? puntajeManager : puntajeUsuario;

        if (datos.nivelActualBackend <= 0)
        {
            datos.nivelActualBackend = 1;
        }

        if (datos.puntajeTotal < 0)
        {
            datos.puntajeTotal = 0;
        }

        return datos;
    }

    private object LeerObjetoUsuario(object target)
    {
        if (target == null)
        {
            return null;
        }

        object usuario = LeerMiembro(
            target,
            "UsuarioActual",
            "usuarioActual",
            "UsuarioSesion",
            "usuarioSesion",
            "Usuario",
            "usuario",
            "CurrentUser",
            "currentUser",
            "User",
            "user"
        );

        return usuario;
    }

    private object LeerMiembro(object target, params string[] nombres)
    {
        if (target == null)
        {
            return null;
        }

        Type tipo = target.GetType();

        for (int i = 0; i < nombres.Length; i++)
        {
            string nombre = nombres[i];

            PropertyInfo propiedad = tipo.GetProperty(
                nombre,
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic
            );

            if (propiedad != null && propiedad.GetIndexParameters().Length == 0)
            {
                try
                {
                    return propiedad.GetValue(target);
                }
                catch
                {
                }
            }

            FieldInfo campo = tipo.GetField(
                nombre,
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic
            );

            if (campo != null)
            {
                try
                {
                    return campo.GetValue(target);
                }
                catch
                {
                }
            }

            MethodInfo metodo = tipo.GetMethod(
                nombre,
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic,
                null,
                Type.EmptyTypes,
                null
            );

            if (metodo != null)
            {
                try
                {
                    return metodo.Invoke(target, null);
                }
                catch
                {
                }
            }
        }

        return null;
    }

    private string LeerTextoDesdeObjeto(object target, string valorDefecto, params string[] nombres)
    {
        object valor = LeerMiembro(target, nombres);

        if (valor == null)
        {
            return valorDefecto;
        }

        return valor.ToString();
    }

    private int LeerEnteroDesdeObjeto(object target, int valorDefecto, params string[] nombres)
    {
        object valor = LeerMiembro(target, nombres);

        if (valor == null)
        {
            return valorDefecto;
        }

        if (valor is int entero)
        {
            return entero;
        }

        if (valor is long largo)
        {
            return (int)largo;
        }

        if (valor is float flotante)
        {
            return Mathf.RoundToInt(flotante);
        }

        if (valor is double doble)
        {
            return Mathf.RoundToInt((float)doble);
        }

        string texto = valor.ToString();

        if (int.TryParse(texto, out int resultado))
        {
            return resultado;
        }

        if (float.TryParse(texto, out float resultadoFloat))
        {
            return Mathf.RoundToInt(resultadoFloat);
        }

        return valorDefecto;
    }

    private bool LeerBooleanoDesdeObjeto(object target, bool valorDefecto, params string[] nombres)
    {
        object valor = LeerMiembro(target, nombres);

        if (valor == null)
        {
            return valorDefecto;
        }

        if (valor is bool booleano)
        {
            return booleano;
        }

        if (valor is int entero)
        {
            return entero != 0;
        }

        string texto = valor.ToString().Trim().ToLower();

        if (texto == "true" || texto == "1" || texto == "si" || texto == "sí")
        {
            return true;
        }

        if (texto == "false" || texto == "0" || texto == "no")
        {
            return false;
        }

        return valorDefecto;
    }

    private void DebugLog(string mensaje)
    {
        if (mostrarDebug)
        {
            Debug.Log(mensaje);
        }
    }

    private class DatosSesion
    {
        public bool haySesion;
        public bool autenticado;
        public bool esInvitado;
        public string nombre;
        public int nivelActualBackend;
        public int puntajeTotal;
    }
}
