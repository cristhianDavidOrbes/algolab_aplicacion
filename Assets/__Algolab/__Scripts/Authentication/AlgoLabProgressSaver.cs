using System;
using UnityEngine;

public class AlgoLabProgressSaver : MonoBehaviour
{
    public static AlgoLabProgressSaver Instance { get; private set; }

    [Header("Backend")]
    public string backendBaseUrl = "https://backendfrontendpaginawebmr-production.up.railway.app";

    [Header("Sesión")]
    public AlgoLabSessionManager sessionManager;
    public AlgoLabBackendClient backendClient;

    [Header("Debug")]
    public bool mostrarDebug = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (sessionManager == null)
        {
            sessionManager = AlgoLabSessionManager.Instance;
        }

        if (sessionManager == null)
        {
            sessionManager = FindFirstObjectByType<AlgoLabSessionManager>();
        }

        if (backendClient == null)
        {
            backendClient = AlgoLabBackendClient.Instance != null
                ? AlgoLabBackendClient.Instance
                : FindFirstObjectByType<AlgoLabBackendClient>();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void GuardarProgresoSiAplica(
        int nivel,
        bool completado,
        int puntaje,
        int tiempoRestante,
        int intentos
    )
    {
        if (sessionManager == null)
        {
            sessionManager = AlgoLabSessionManager.Instance;
        }

        if (sessionManager == null)
        {
            Debug.LogWarning("PROGRESS SAVER: no hay SessionManager.");
            return;
        }

        if (!sessionManager.PuedeGuardarProgreso)
        {
            DebugLog("PROGRESS SAVER: modo invitado o sin sesión. No se guarda progreso.");
            return;
        }

        if (backendClient == null)
        {
            backendClient = AlgoLabBackendClient.Instance != null
                ? AlgoLabBackendClient.Instance
                : FindFirstObjectByType<AlgoLabBackendClient>();
        }

        if (backendClient == null)
        {
            Debug.LogWarning("PROGRESS SAVER: no hay BackendClient.");
            return;
        }

        backendClient.GuardarProgreso(
            Mathf.Max(1, nivel),
            completado,
            Mathf.Max(0, puntaje),
            Mathf.Max(0, tiempoRestante),
            Mathf.Max(0, intentos),
            (ok, mensaje, _) =>
            {
                if (ok)
                {
                    DebugLog("PROGRESS SAVER: " + mensaje);
                }
                else
                {
                    Debug.LogWarning("PROGRESS SAVER: " + mensaje);
                }
            }
        );
    }

    /// <summary>
    /// Guarda el progreso acumulado usando la sesión actual y avisa cuando la
    /// petición terminó. Debe llamarse antes de limpiar el token; de lo
    /// contrario el backend rechaza la respuesta porque la sesión ya cambió.
    /// </summary>
    public void GuardarProgresoAntesDeCerrarSesion(Action<bool> callback)
    {
        if (sessionManager == null)
        {
            sessionManager = AlgoLabSessionManager.Instance ??
                FindFirstObjectByType<AlgoLabSessionManager>();
        }

        if (sessionManager == null || !sessionManager.PuedeGuardarProgreso)
        {
            callback?.Invoke(true);
            return;
        }

        if (backendClient == null)
        {
            backendClient = AlgoLabBackendClient.Instance ??
                FindFirstObjectByType<AlgoLabBackendClient>();
        }

        if (backendClient == null)
        {
            Debug.LogWarning("PROGRESS SAVER: no hay BackendClient para guardar al cerrar sesión.");
            callback?.Invoke(false);
            return;
        }

        int nivel = Mathf.Max(1, sessionManager.NivelActual);
        int puntaje = Mathf.Max(0, sessionManager.Puntaje);

        backendClient.GuardarProgreso(
            nivel,
            false,
            puntaje,
            0,
            0,
            (ok, mensaje, _) =>
            {
                if (!ok)
                {
                    Debug.LogWarning("PROGRESS SAVER: no se pudo guardar antes de cerrar sesión: " + mensaje);
                }

                callback?.Invoke(ok);
            }
        );
    }

    private void DebugLog(string mensaje)
    {
        if (mostrarDebug)
        {
            Debug.Log(mensaje);
        }
    }
}
