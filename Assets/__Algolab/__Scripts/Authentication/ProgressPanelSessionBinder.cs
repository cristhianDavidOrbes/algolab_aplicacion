using System;
using System.Reflection;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

[DefaultExecutionOrder(100)]
public class ProgressPanelSessionBinder : MonoBehaviour
{
    [Serializable]
    public class AvatarPresetVisual
    {
        public string id = "orbita";
        public Sprite sprite;
    }

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

    [Header("Avatar personalizado")]
    [Tooltip("Descarga avatarUrl del perfil. Si falla, conserva el preset/default del panel.")]
    public bool cargarAvatarPersonalizado = true;

    [Range(3, 60)]
    public int timeoutAvatarSegundos = 15;

    [Range(256, 4096)]
    public int dimensionMaximaAvatar = 2048;

    [Tooltip("Mapeo opcional de los ids de avatar preset. Si queda vacío, se usa el sprite actual del panel.")]
    public AvatarPresetVisual[] avataresPreset = new AvatarPresetVisual[0];

    [Header("Debug")]
    public bool mostrarDebug = true;

    private string ultimoNombreAplicado = "";
    private int ultimoNivelBackendAplicado = -1;
    private int ultimoPuntajeAplicado = -1;
    private bool ultimoFueInvitado = false;
    private string ultimoAvatarPresetAplicado = "";
    private string ultimoAvatarUrlAplicado = "";
    private string ultimoAvatarVersionAplicado = "";
    private string ultimoResumenPerfilAplicado = "";
    private Coroutine rutinaActualizacion;
    private Coroutine rutinaAplicacionInicial;
    private Coroutine rutinaAvatar;
    private AlgoLabSessionManager sessionManagerTipado;
    private Sprite spriteAvatarPredeterminado;
    private Color colorAvatarPredeterminado = Color.white;
    private Image.Type tipoImagenAvatarPredeterminado = Image.Type.Simple;
    private bool preservarAspectoAvatarPredeterminado;
    private bool visualAvatarPredeterminadoCapturado;
    private Sprite spriteAvatarRemoto;
    private Texture2D texturaAvatarRemoto;
    private Sprite spriteAvatarInvitado;
    private bool spriteAvatarInvitadoCreadoEnRuntime;
    private string claveAvatarRemotoAplicado = "";
    private string claveAvatarEnDescarga = "";
    private int generacionSolicitudAvatar;

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

        DetenerCargaAvatar();
    }

    private void OnDestroy()
    {
        DetenerCargaAvatar();
        AplicarAvatarPreset(ultimoAvatarPresetAplicado);
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

        CapturarAvatarPredeterminado();

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
            else
            {
                SincronizarAvatar("orbita", "", "");
                ultimoAvatarPresetAplicado = "orbita";
                ultimoAvatarUrlAplicado = "";
                ultimoAvatarVersionAplicado = "";
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
            nombre = !string.IsNullOrWhiteSpace(datos.alias) ? datos.alias.Trim() : "Usuario";
        }

        string categoriaCalculada = NormalizarCategoria(datos.categoria);

        int nivelBackend = Mathf.Max(1, datos.nivelActualBackend);
        int puntaje = Mathf.Max(0, datos.puntajeTotal);
        int nivelVisual = ConvertirNivelBackendAIndiceVisual(nivelBackend);
        string resumenPerfil = categoriaCalculada;

        bool cambio =
            nombre != ultimoNombreAplicado ||
            nivelBackend != ultimoNivelBackendAplicado ||
            puntaje != ultimoPuntajeAplicado ||
            datos.avatarPreset != ultimoAvatarPresetAplicado ||
            datos.avatarUrl != ultimoAvatarUrlAplicado ||
            datos.avatarVersion != ultimoAvatarVersionAplicado ||
            resumenPerfil != ultimoResumenPerfilAplicado ||
            ultimoFueInvitado;

        if (!forzar && !cambio)
        {
            return;
        }

        progressPanel.AplicarDatosUsuarioDesdeBackend(nombre, categoriaCalculada, null);

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

        SincronizarAvatar(
            datos.avatarPreset,
            datos.avatarUrl,
            datos.avatarVersion
        );

        ultimoNombreAplicado = nombre;
        ultimoNivelBackendAplicado = nivelBackend;
        ultimoPuntajeAplicado = puntaje;
        ultimoAvatarPresetAplicado = datos.avatarPreset;
        ultimoAvatarUrlAplicado = datos.avatarUrl;
        ultimoAvatarVersionAplicado = datos.avatarVersion;
        ultimoResumenPerfilAplicado = resumenPerfil;
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
            "Junior",
            null
        );

        progressPanel.SetPuntaje(puntaje);
        progressPanel.SetNivelActual(nivelVisual);
        progressPanel.ActualizarTodo();
        SincronizarAvatarInvitado();

        ultimoNombreAplicado = nombreInvitado;
        ultimoNivelBackendAplicado = nivelBackend;
        ultimoPuntajeAplicado = puntaje;
        ultimoAvatarPresetAplicado = "orbita";
        ultimoAvatarUrlAplicado = "";
        ultimoAvatarVersionAplicado = "";
        ultimoResumenPerfilAplicado = "";
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

        datos.alias = LeerTextoDesdeObjeto(
            manager,
            "",
            "AliasUsuario",
            "aliasUsuario",
            "Alias",
            "alias",
            "NombreUsuario",
            "nombreUsuario"
        );

        datos.programa = LeerTextoDesdeObjeto(
            manager,
            "",
            "Programa",
            "programa",
            "Carrera",
            "carrera"
        );

        datos.institucion = LeerTextoDesdeObjeto(
            manager,
            "",
            "Institucion",
            "institucion",
            "Universidad",
            "universidad"
        );

        datos.rol = LeerTextoDesdeObjeto(
            manager,
            "",
            "RolUsuario",
            "rolUsuario",
            "Rol",
            "rol"
        );

        datos.avatarPreset = LeerTextoDesdeObjeto(
            manager,
            "",
            "Avatar",
            "avatar",
            "AvatarPreset",
            "avatarPreset"
        );

        datos.avatarUrl = LeerTextoDesdeObjeto(
            manager,
            "",
            "AvatarUrl",
            "avatarUrl",
            "AvatarURL",
            "avatarURL"
        );

        datos.avatarVersion = LeerTextoDesdeObjeto(
            manager,
            "",
            "AvatarVersion",
            "avatarVersion"
        );

        if (usuario != null)
        {
            if (string.IsNullOrWhiteSpace(datos.alias))
            {
                datos.alias = LeerTextoDesdeObjeto(
                    usuario,
                    "",
                    "nombreUsuario",
                    "NombreUsuario",
                    "alias",
                    "Alias"
                );
            }

            if (string.IsNullOrWhiteSpace(datos.programa))
            {
                datos.programa = LeerTextoDesdeObjeto(
                    usuario,
                    "",
                    "programa",
                    "Programa",
                    "carrera",
                    "Carrera"
                );
            }

            if (string.IsNullOrWhiteSpace(datos.institucion))
            {
                datos.institucion = LeerTextoDesdeObjeto(
                    usuario,
                    "",
                    "institucion",
                    "Institucion",
                    "universidad",
                    "Universidad"
                );
            }

            if (string.IsNullOrWhiteSpace(datos.rol))
            {
                datos.rol = LeerTextoDesdeObjeto(
                    usuario,
                    "",
                    "rol",
                    "Rol"
                );
            }

            if (string.IsNullOrWhiteSpace(datos.avatarPreset))
            {
                datos.avatarPreset = LeerTextoDesdeObjeto(
                    usuario,
                    "",
                    "avatar",
                    "Avatar",
                    "avatarPreset",
                    "AvatarPreset"
                );
            }

            if (string.IsNullOrWhiteSpace(datos.avatarUrl))
            {
                datos.avatarUrl = LeerTextoDesdeObjeto(
                    usuario,
                    "",
                    "avatarUrl",
                    "AvatarUrl",
                    "avatarURL",
                    "AvatarURL"
                );
            }

            if (string.IsNullOrWhiteSpace(datos.avatarVersion))
            {
                datos.avatarVersion = LeerTextoDesdeObjeto(
                    usuario,
                    "",
                    "avatarVersion",
                    "AvatarVersion"
                );
            }
        }

        datos.avatarPreset = string.IsNullOrWhiteSpace(datos.avatarPreset)
            ? "orbita"
            : datos.avatarPreset.Trim();
        datos.avatarUrl = datos.avatarUrl == null ? "" : datos.avatarUrl.Trim();
        datos.avatarVersion = datos.avatarVersion == null ? "" : datos.avatarVersion.Trim();

        datos.categoria = LeerTextoDesdeObjeto(
            manager,
            "Junior",
            "CategoriaUsuario",
            "categoriaUsuario",
            "Categoria",
            "categoria"
        );

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

    private void CapturarAvatarPredeterminado()
    {
        if (progressPanel == null)
        {
            return;
        }

        if (!visualAvatarPredeterminadoCapturado && progressPanel.imageUser != null)
        {
            Color colorActual = progressPanel.imageUser.color;
            if (colorActual.a <= 0.05f || (colorActual.r <= 0.05f && colorActual.g <= 0.05f && colorActual.b <= 0.05f))
            {
                colorAvatarPredeterminado = Color.white;
            }
            else
            {
                colorAvatarPredeterminado = colorActual;
            }
            tipoImagenAvatarPredeterminado = progressPanel.imageUser.type;
            preservarAspectoAvatarPredeterminado = true;
            visualAvatarPredeterminadoCapturado = true;
        }

        if (spriteAvatarPredeterminado != null)
        {
            return;
        }

        if (progressPanel.userImageDefault != null)
        {
            spriteAvatarPredeterminado = progressPanel.userImageDefault;
            return;
        }

        if (progressPanel.imageUser != null &&
            progressPanel.imageUser.sprite != null &&
            progressPanel.imageUser.sprite != spriteAvatarRemoto)
        {
            spriteAvatarPredeterminado = progressPanel.imageUser.sprite;
            return;
        }

        Sprite recursoDefault = Resources.Load<Sprite>("UI/AvatarInvitado");
        if (recursoDefault != null)
        {
            spriteAvatarPredeterminado = recursoDefault;
        }
    }

    private void SincronizarAvatar(string avatarPreset, string avatarUrl, string avatarVersion)
    {
        CapturarAvatarPredeterminado();

        string presetSeguro = string.IsNullOrWhiteSpace(avatarPreset)
            ? "orbita"
            : avatarPreset.Trim();
        string rutaSegura = avatarUrl == null ? "" : avatarUrl.Trim();
        string versionSegura = avatarVersion == null ? "" : avatarVersion.Trim();

        if (!cargarAvatarPersonalizado || string.IsNullOrWhiteSpace(rutaSegura))
        {
            DetenerCargaAvatar();
            AplicarAvatarPreset(presetSeguro);
            return;
        }

        if (rutaSegura.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
        {
            DetenerCargaAvatar();
            if (CargarAvatarDesdeBase64(rutaSegura, presetSeguro))
            {
                return;
            }
        }

        string urlResuelta = ResolverUrlAvatar(rutaSegura);
        if (string.IsNullOrWhiteSpace(urlResuelta))
        {
            DetenerCargaAvatar();
            AplicarAvatarPreset(presetSeguro);
            DebugLog("PROGRESS BINDER: avatarUrl inválida; se conserva el avatar preset.");
            return;
        }

        urlResuelta = AgregarVersionAvatarSiHaceFalta(urlResuelta, versionSegura);
        string clave = urlResuelta + "|" + versionSegura;

        if (spriteAvatarRemoto != null && claveAvatarRemotoAplicado == clave)
        {
            ConfigurarVisualAvatarRemoto();
            progressPanel.AplicarImagenUsuario(spriteAvatarRemoto);
            return;
        }

        if (rutinaAvatar != null && claveAvatarEnDescarga == clave)
        {
            return;
        }

        DetenerCargaAvatar();
        AplicarAvatarPreset(presetSeguro);

        int generacion = generacionSolicitudAvatar;
        claveAvatarEnDescarga = clave;
        rutinaAvatar = StartCoroutine(
            DescargarAvatarRutina(
                urlResuelta,
                clave,
                presetSeguro,
                generacion
            )
        );
    }

    private void SincronizarAvatarInvitado()
    {
        DetenerCargaAvatar();

        if (spriteAvatarInvitado == null)
        {
            spriteAvatarInvitado = Resources.Load<Sprite>("UI/AvatarInvitado");
            if (spriteAvatarInvitado == null)
            {
                Texture2D textura = Resources.Load<Texture2D>("UI/AvatarInvitado");
                if (textura != null)
                {
                    spriteAvatarInvitado = Sprite.Create(
                        textura,
                        new Rect(0f, 0f, textura.width, textura.height),
                        new Vector2(0.5f, 0.5f),
                        100f
                    );
                    spriteAvatarInvitado.name = "AlgoLab_AvatarInvitado_Sprite";
                    spriteAvatarInvitadoCreadoEnRuntime = true;
                }
            }
        }

        if (spriteAvatarInvitado != null)
        {
            ConfigurarVisualAvatarRemoto();
            progressPanel.AplicarImagenUsuario(spriteAvatarInvitado);
            return;
        }

        AplicarAvatarPreset("orbita");
    }

    private static string NormalizarCategoria(string categoria)
    {
        if (string.IsNullOrWhiteSpace(categoria))
        {
            return "Junior";
        }

        string normalizada = categoria.Trim().ToLowerInvariant();
        if (normalizada.Contains("fullstack") || normalizada.Contains("full stack"))
        {
            return "Fullstack";
        }

        return normalizada.Contains("senior") ? "Senior" : "Junior";
    }

    private bool CargarAvatarDesdeBase64(string dataUri, string avatarPreset)
    {
        try
        {
            int commaIndex = dataUri.IndexOf(',');
            if (commaIndex < 0)
                return false;

            string base64 = dataUri.Substring(commaIndex + 1);
            byte[] bytes = Convert.FromBase64String(base64);
            if (bytes == null || bytes.Length == 0)
                return false;

            Texture2D textura = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!ImageConversion.LoadImage(textura, bytes))
            {
                Destroy(textura);
                return false;
            }

            textura.name = "AlgoLab_Avatar_Base64";
            textura.wrapMode = TextureWrapMode.Clamp;
            textura.filterMode = FilterMode.Bilinear;

            Sprite nuevoSprite = Sprite.Create(
                textura,
                new Rect(0f, 0f, textura.width, textura.height),
                new Vector2(0.5f, 0.5f),
                100f
            );
            nuevoSprite.name = "AlgoLab_Avatar_Base64_Sprite";

            LiberarAvatarRemoto();
            texturaAvatarRemoto = textura;
            spriteAvatarRemoto = nuevoSprite;
            claveAvatarRemotoAplicado = "base64_" + dataUri.Length;
            ConfigurarVisualAvatarRemoto();
            progressPanel.AplicarImagenUsuario(spriteAvatarRemoto);
            DebugLog("PROGRESS BINDER: avatar base64 aplicado.");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogWarning("PROGRESS BINDER: error al decodificar avatar base64: " + ex.Message);
            AplicarAvatarPreset(avatarPreset);
            return false;
        }
    }

    private IEnumerator DescargarAvatarRutina(
        string url,
        string clave,
        string avatarPreset,
        int generacion
    )
    {
        using UnityWebRequest request = UnityWebRequestTexture.GetTexture(url, true);
        string authorization = sessionManagerTipado != null
            ? sessionManagerTipado.ObtenerAuthorizationHeader()
            : "";
        if (!string.IsNullOrWhiteSpace(authorization))
        {
            request.SetRequestHeader("Authorization", authorization);
        }
        request.SetRequestHeader("ngrok-skip-browser-warning", "algolab");
        request.SetRequestHeader("Accept", "image/png,image/jpeg,image/*,*/*");
        request.timeout = Mathf.Clamp(timeoutAvatarSegundos, 3, 60);
        yield return request.SendWebRequest();

        if (generacion != generacionSolicitudAvatar || !isActiveAndEnabled)
        {
            yield break;
        }

        if (request.result != UnityWebRequest.Result.Success)
        {
            AplicarAvatarPreset(avatarPreset);
            FinalizarSolicitudAvatar(generacion);
            DebugLog(
                "PROGRESS BINDER: no se pudo descargar el avatar; se usa el preset. " +
                request.error
            );
            yield break;
        }

        string contentType = request.GetResponseHeader("Content-Type");
        bool tipoValido =
            string.IsNullOrWhiteSpace(contentType) ||
            contentType.StartsWith("image/png", StringComparison.OrdinalIgnoreCase) ||
            contentType.StartsWith("image/jpeg", StringComparison.OrdinalIgnoreCase) ||
            contentType.StartsWith("image/jpg", StringComparison.OrdinalIgnoreCase);

        Texture2D textura = DownloadHandlerTexture.GetContent(request);
        int dimensionMaxima = Mathf.Clamp(dimensionMaximaAvatar, 256, 4096);
        bool texturaValida =
            tipoValido &&
            textura != null &&
            textura.width >= 2 &&
            textura.height >= 2 &&
            textura.width <= dimensionMaxima &&
            textura.height <= dimensionMaxima;

        if (!texturaValida)
        {
            if (textura != null)
            {
                Destroy(textura);
            }

            AplicarAvatarPreset(avatarPreset);
            FinalizarSolicitudAvatar(generacion);
            DebugLog("PROGRESS BINDER: el archivo de avatar no es PNG/JPEG válido o excede el tamaño permitido.");
            yield break;
        }

        textura.name = "AlgoLab_Avatar_Remoto";
        textura.wrapMode = TextureWrapMode.Clamp;
        textura.filterMode = FilterMode.Bilinear;

        Sprite nuevoSprite = Sprite.Create(
            textura,
            new Rect(0f, 0f, textura.width, textura.height),
            new Vector2(0.5f, 0.5f),
            100f
        );
        nuevoSprite.name = "AlgoLab_Avatar_Remoto_Sprite";

        LiberarAvatarRemoto();
        texturaAvatarRemoto = textura;
        spriteAvatarRemoto = nuevoSprite;
        claveAvatarRemotoAplicado = clave;
        ConfigurarVisualAvatarRemoto();
        progressPanel.AplicarImagenUsuario(spriteAvatarRemoto);
        FinalizarSolicitudAvatar(generacion);

        DebugLog("PROGRESS BINDER: avatar personalizado aplicado al panel de progreso.");
    }

    private string ResolverUrlAvatar(string avatarUrl)
    {
        AlgoLabBackendClient backendClient = AlgoLabBackendClient.Instance;
        if (backendClient == null)
        {
            backendClient = FindFirstObjectByType<AlgoLabBackendClient>(
                FindObjectsInactive.Include
            );
        }

        if (backendClient != null)
        {
            return backendClient.ResolverUrlBackend(avatarUrl);
        }

        if (Uri.TryCreate(avatarUrl, UriKind.Absolute, out Uri absoluta) &&
            (absoluta.Scheme == Uri.UriSchemeHttp || absoluta.Scheme == Uri.UriSchemeHttps))
        {
            return absoluta.AbsoluteUri;
        }

        return "";
    }

    private string AgregarVersionAvatarSiHaceFalta(string url, string avatarVersion)
    {
        if (string.IsNullOrWhiteSpace(url) ||
            string.IsNullOrWhiteSpace(avatarVersion) ||
            url.IndexOf("v=", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return url;
        }

        string separador = url.Contains("?") ? "&" : "?";
        return url + separador + "v=" + UnityWebRequest.EscapeURL(avatarVersion);
    }

    private void AplicarAvatarPreset(string avatarPreset)
    {
        if (progressPanel == null)
        {
            LiberarAvatarRemoto();
            return;
        }

        RestaurarVisualAvatarPredeterminado();
        Sprite preset = BuscarSpriteAvatarPreset(avatarPreset);
        if (preset == null)
        {
            preset = spriteAvatarPredeterminado != null
                ? spriteAvatarPredeterminado
                : Resources.Load<Sprite>("UI/AvatarInvitado");
        }

        if (preset != null)
        {
            progressPanel.AplicarImagenUsuario(preset);
        }
        else if (progressPanel.imageUser != null)
        {
            progressPanel.imageUser.color = Color.white;
            progressPanel.imageUser.preserveAspect = true;
            progressPanel.imageUser.enabled = progressPanel.imageUser.sprite != null;
        }

        LiberarAvatarRemoto();
    }

    private void ConfigurarVisualAvatarRemoto()
    {
        if (progressPanel == null || progressPanel.imageUser == null)
        {
            return;
        }

        progressPanel.imageUser.color = Color.white;
        progressPanel.imageUser.type = Image.Type.Simple;
        progressPanel.imageUser.preserveAspect = true;
        progressPanel.imageUser.enabled = true;
    }

    private void RestaurarVisualAvatarPredeterminado()
    {
        if (progressPanel == null || progressPanel.imageUser == null)
        {
            return;
        }

        Color colorSeguro = colorAvatarPredeterminado;
        if (colorSeguro.a <= 0.05f || (colorSeguro.r <= 0.05f && colorSeguro.g <= 0.05f && colorSeguro.b <= 0.05f))
        {
            colorSeguro = Color.white;
        }

        progressPanel.imageUser.color = colorSeguro;
        progressPanel.imageUser.type = tipoImagenAvatarPredeterminado;
        progressPanel.imageUser.preserveAspect = true;
        progressPanel.imageUser.enabled = true;
    }

    private Sprite BuscarSpriteAvatarPreset(string avatarPreset)
    {
        string idBuscado = string.IsNullOrWhiteSpace(avatarPreset)
            ? "orbita"
            : avatarPreset.Trim();

        if (avataresPreset != null)
        {
            for (int i = 0; i < avataresPreset.Length; i++)
            {
                AvatarPresetVisual candidato = avataresPreset[i];
                if (candidato != null &&
                    candidato.sprite != null &&
                    string.Equals(candidato.id, idBuscado, StringComparison.OrdinalIgnoreCase))
                {
                    return candidato.sprite;
                }
            }
        }

        if (spriteAvatarPredeterminado != null)
        {
            return spriteAvatarPredeterminado;
        }

        return Resources.Load<Sprite>("UI/AvatarInvitado");
    }

    private void DetenerCargaAvatar()
    {
        generacionSolicitudAvatar++;
        claveAvatarEnDescarga = "";

        if (rutinaAvatar != null)
        {
            StopCoroutine(rutinaAvatar);
            rutinaAvatar = null;
        }
    }

    private void FinalizarSolicitudAvatar(int generacion)
    {
        if (generacion != generacionSolicitudAvatar)
        {
            return;
        }

        rutinaAvatar = null;
        claveAvatarEnDescarga = "";
    }

    private void LiberarAvatarRemoto()
    {
        if (spriteAvatarRemoto != null)
        {
            Destroy(spriteAvatarRemoto);
            spriteAvatarRemoto = null;
        }

        if (texturaAvatarRemoto != null)
        {
            Destroy(texturaAvatarRemoto);
            texturaAvatarRemoto = null;
        }

        claveAvatarRemotoAplicado = "";
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
        public string alias;
        public string programa;
        public string institucion;
        public string rol;
        public string avatarPreset;
        public string avatarUrl;
        public string avatarVersion;
        public string categoria;
        public int nivelActualBackend;
        public int puntajeTotal;
    }
}
