using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AlgoLabGameAccessController : MonoBehaviour
{
    [Header("Sesión")]
    public AlgoLabSessionManager sessionManager;

    [Header("Objetos del juego que se activan después de entrar")]
    [Tooltip("Aquí arrastra los roots principales del juego: [PROGRESS], [EDUCATIONAL_OBJECTS], etc. El panel principal puede ir aquí.")]
    public List<GameObject> objetosJuego = new List<GameObject>();

    [Header("Objetos que deben ocultarse cuando empieza el juego")]
    [Tooltip("Opcional. Aquí puedes poner [LOGIN_UI] si quieres ocultarlo desde este controlador.")]
    public List<GameObject> objetosLogin = new List<GameObject>();

    [Header("Paneles especiales del tutorial")]
    [Tooltip("Activa esta opción para que el panel de diagramas y el panel de IA se oculten apenas el usuario entra al juego.")]
    public bool ocultarPanelesEspecialesDuranteTutorial = true;

    [Tooltip("Aquí NO pongas el panel principal. Solo pon el panel de diagramas y el panel de IA.")]
    public List<GameObject> panelesEspecialesTutorial = new List<GameObject>();

    [Tooltip("Si está activo, al bloquear/cerrar sesión también se apagan estos paneles.")]
    public bool ocultarPanelesEspecialesAlBloquearJuego = true;

    [Tooltip("Si está activo, al terminar u omitir el tutorial estos paneles se activan sí o sí.")]
    public bool activarPanelesEspecialesAlTerminarTutorial = true;

    [Header("Configuración")]
    public bool ocultarJuegoAlIniciar = true;
    public bool activarJuegoSiYaHaySesionGuardada = true;
    public bool ocultarLoginCuandoEntra = true;
    public bool mantenerEntreEscenas = false;

    [Header("Debug")]
    public bool mostrarDebug = true;

    private bool juegoActivado = false;
    private bool panelesEspecialesActivados = false;

    private void Awake()
    {
        BuscarReferencias();

        if (mantenerEntreEscenas)
        {
            DontDestroyOnLoad(gameObject);
        }

        if (ocultarJuegoAlIniciar)
        {
            BloquearAccesoJuego();
        }
    }

    private void Start()
    {
        BuscarReferencias();

        if (sessionManager != null)
        {
            sessionManager.OnSesionIniciada += PermitirAccesoJuego;
            sessionManager.OnSesionInvitado += PermitirAccesoJuego;
            sessionManager.OnSesionCerrada += BloquearAccesoJuego;
        }

        if (activarJuegoSiYaHaySesionGuardada &&
            sessionManager != null &&
            sessionManager.SesionIniciada)
        {
            PermitirAccesoJuego();
        }
    }

    private void OnDestroy()
    {
        if (sessionManager != null)
        {
            sessionManager.OnSesionIniciada -= PermitirAccesoJuego;
            sessionManager.OnSesionInvitado -= PermitirAccesoJuego;
            sessionManager.OnSesionCerrada -= BloquearAccesoJuego;
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

    [ContextMenu("Permitir acceso juego")]
    public void PermitirAccesoJuego()
    {
        juegoActivado = true;

        // Activa el juego normal. Aquí puede ir el panel principal.
        ActivarLista(objetosJuego, true);

        // Pero durante el tutorial mantiene ocultos IA y diagramas.
        if (ocultarPanelesEspecialesDuranteTutorial)
        {
            OcultarPanelesEspecialesDuranteTutorial();
        }

        if (ocultarLoginCuandoEntra)
        {
            ActivarLista(objetosLogin, false);
        }

        DebugLog("GAME ACCESS: acceso permitido. Juego activado. Paneles especiales preparados para tutorial.");
    }

    [ContextMenu("Bloquear acceso juego")]
    public void BloquearAccesoJuego()
    {
        juegoActivado = false;
        panelesEspecialesActivados = false;

        ActivarLista(objetosJuego, false);

        if (ocultarPanelesEspecialesAlBloquearJuego)
        {
            ActivarLista(panelesEspecialesTutorial, false);
        }

        DebugLog("GAME ACCESS: acceso bloqueado. Objetos del juego ocultos.");
    }

    public void CerrarSesionYBloquearJuego()
    {
        if (sessionManager != null)
        {
            sessionManager.CerrarSesion();
        }

        BloquearAccesoJuego();

        ActivarLista(objetosLogin, true);

        DebugLog("GAME ACCESS: sesión cerrada y juego bloqueado.");
    }

    public bool JuegoActivado()
    {
        return juegoActivado;
    }

    public bool PanelesEspecialesActivados()
    {
        return panelesEspecialesActivados;
    }

    [ContextMenu("Ocultar paneles especiales durante tutorial")]
    public void OcultarPanelesEspecialesDuranteTutorial()
    {
        if (!ocultarPanelesEspecialesDuranteTutorial)
        {
            return;
        }

        ActivarLista(panelesEspecialesTutorial, false);
        panelesEspecialesActivados = false;

        DebugLog("GAME ACCESS: paneles de IA/diagramas ocultos durante el tutorial.");
    }

    [ContextMenu("Activar paneles después del tutorial")]
    public void ActivarPanelesDespuesDelTutorial()
    {
        if (!activarPanelesEspecialesAlTerminarTutorial)
        {
            return;
        }

        if (!juegoActivado)
        {
            // Por seguridad, si se llama desde el tutorial pero el juego no quedó marcado activo.
            juegoActivado = true;
            ActivarLista(objetosJuego, true);
        }

        ActivarLista(panelesEspecialesTutorial, true);
        ActivarPanelesRealesEspecialesInmediatamente();
        panelesEspecialesActivados = true;

        DebugLog("GAME ACCESS: paneles de IA/diagramas activados después del tutorial.");
    }

    [ContextMenu("Activar paneles especiales ahora")]
    public void ActivarPanelesEspecialesAhora()
    {
        ActivarPanelesDespuesDelTutorial();
    }

    private void ActivarPanelesRealesEspecialesInmediatamente()
    {
        AlgoLabPanelPocketManager pocketManager = AlgoLabPanelPocketManager.Instance;
        if (pocketManager == null)
        {
            pocketManager = FindFirstObjectByType<AlgoLabPanelPocketManager>(
                FindObjectsInactive.Include
            );
        }

        for (int i = 0; i < panelesEspecialesTutorial.Count; i++)
        {
            GameObject root = panelesEspecialesTutorial[i];
            if (root == null)
            {
                continue;
            }

            AlgoLabPocketPanelItem[] paneles = root.GetComponentsInChildren<AlgoLabPocketPanelItem>(true);
            for (int j = 0; j < paneles.Length; j++)
            {
                AlgoLabPocketPanelItem panel = paneles[j];
                if (panel == null)
                {
                    continue;
                }

                pocketManager?.NotificarPanelActivadoExternamente(panel);
                panel.LimpiarEstadoPocketSinActivar();

                Transform panelRoot = panel.ObtenerPanelRoot();
                if (panelRoot != null)
                {
                    panelRoot.gameObject.SetActive(true);
                }
            }
        }
    }

    private void ActivarLista(List<GameObject> lista, bool activo)
    {
        if (lista == null)
        {
            return;
        }

        for (int i = 0; i < lista.Count; i++)
        {
            GameObject obj = lista[i];

            if (obj == null)
            {
                continue;
            }

            obj.SetActive(activo);
        }
    }

    public void AgregarObjetoJuego(GameObject obj)
    {
        if (obj == null)
        {
            return;
        }

        if (!objetosJuego.Contains(obj))
        {
            objetosJuego.Add(obj);
        }
    }

    public void AgregarObjetoLogin(GameObject obj)
    {
        if (obj == null)
        {
            return;
        }

        if (!objetosLogin.Contains(obj))
        {
            objetosLogin.Add(obj);
        }
    }

    public void AgregarPanelEspecialTutorial(GameObject obj)
    {
        if (obj == null)
        {
            return;
        }

        if (!panelesEspecialesTutorial.Contains(obj))
        {
            panelesEspecialesTutorial.Add(obj);
        }
    }

    private void DebugLog(string mensaje)
    {
        if (mostrarDebug)
        {
            Debug.Log(mensaje);
        }
    }
}
