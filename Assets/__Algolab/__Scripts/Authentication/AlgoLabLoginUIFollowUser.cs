using UnityEngine;

public class AlgoLabLoginUIFollowUser : MonoBehaviour
{
    [Header("Referencia cabeza / cámara")]
    public Transform cabezaUsuario;
    public bool buscarCamaraPrincipalAutomaticamente = true;

    [Header("Distancia frente al usuario")]
    [Tooltip("Distancia ideal a la que debe quedar el LOGIN_UI frente al usuario.")]
    public float distanciaIdeal = 1.45f;

    [Tooltip("Si el usuario está más cerca que esto, el panel se aleja.")]
    public float distanciaMinima = 1.15f;

    [Tooltip("Si el usuario está más lejos que esto, el panel se acerca.")]
    public float distanciaMaxima = 1.75f;

    public float alturaOffset = -0.05f;
    public bool usarSoloDireccionHorizontal = true;

    [Header("Rango para seguir mirada")]
    [Tooltip("Si el usuario gira más de este ángulo, el panel vuelve al frente.")]
    public float anguloMaximoAntesDeRecentrar = 25f;

    [Tooltip("Si la posición ideal está más lejos que esto, el panel se mueve.")]
    public float distanciaMaximaAlObjetivo = 0.25f;

    [Header("Smooth")]
    [Tooltip("Menor valor = se mueve más rápido. Mayor valor = más suave.")]
    public float tiempoSuavizadoPosicion = 0.45f;

    [Tooltip("Velocidad máxima al moverse.")]
    public float velocidadMaxima = 3.5f;

    [Tooltip("Velocidad de rotación hacia el usuario.")]
    public float velocidadRotacion = 4f;

    [Header("Comportamiento")]
    public bool ajustarDistanciaSiempre = true;
    public bool mirarSiempreAlUsuario = true;
    public bool recenterAlActivarse = true;
    public bool recenterAlIniciar = true;

    [Tooltip("Si el panel queda de espaldas, prueba 0,180,0.")]
    public Vector3 rotacionExtraEuler = Vector3.zero;

    [Header("Debug")]
    public bool mostrarDebug = false;

    private Vector3 velocidadSmooth;
    private Vector3 posicionObjetivo;
    private Quaternion rotacionObjetivo;

    private void Awake()
    {
        BuscarCabezaUsuario();
    }

    private void Start()
    {
        BuscarCabezaUsuario();

        if (recenterAlIniciar)
        {
            RecentrarAhora();
        }
    }

    private void OnEnable()
    {
        BuscarCabezaUsuario();

        if (recenterAlActivarse)
        {
            RecentrarAhora();
        }
    }

    private void LateUpdate()
    {
        if (cabezaUsuario == null)
        {
            BuscarCabezaUsuario();

            if (cabezaUsuario == null)
            {
                return;
            }
        }

        CalcularObjetivo();

        if (DebeActualizarPosicion())
        {
            MoverSuave();
        }

        if (mirarSiempreAlUsuario)
        {
            RotarSuave();
        }
    }

    private void BuscarCabezaUsuario()
    {
        if (cabezaUsuario != null)
        {
            return;
        }

        if (!buscarCamaraPrincipalAutomaticamente)
        {
            return;
        }

        Camera cam = Camera.main;

        if (cam != null)
        {
            cabezaUsuario = cam.transform;
            return;
        }

        Camera[] camaras = FindObjectsByType<Camera>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < camaras.Length; i++)
        {
            if (camaras[i] != null && camaras[i].enabled)
            {
                cabezaUsuario = camaras[i].transform;
                return;
            }
        }
    }

    private void CalcularObjetivo()
    {
        Vector3 forward = cabezaUsuario.forward;

        if (usarSoloDireccionHorizontal)
        {
            forward.y = 0f;
        }

        if (forward.sqrMagnitude < 0.001f)
        {
            forward = Vector3.forward;
        }

        forward.Normalize();

        posicionObjetivo =
            cabezaUsuario.position +
            forward * distanciaIdeal +
            Vector3.up * alturaOffset;

        Vector3 direccionDesdeCabeza = posicionObjetivo - cabezaUsuario.position;

        if (usarSoloDireccionHorizontal)
        {
            direccionDesdeCabeza.y = 0f;
        }

        if (direccionDesdeCabeza.sqrMagnitude < 0.001f)
        {
            direccionDesdeCabeza = forward;
        }

        direccionDesdeCabeza.Normalize();

        rotacionObjetivo =
            Quaternion.LookRotation(direccionDesdeCabeza, Vector3.up) *
            Quaternion.Euler(rotacionExtraEuler);
    }

    private bool DebeActualizarPosicion()
    {
        Vector3 cabezaPos = cabezaUsuario.position;
        Vector3 panelPos = transform.position;

        Vector3 direccionActual = panelPos - cabezaPos;
        Vector3 direccionIdeal = posicionObjetivo - cabezaPos;

        if (usarSoloDireccionHorizontal)
        {
            direccionActual.y = 0f;
            direccionIdeal.y = 0f;
        }

        float distanciaActual = direccionActual.magnitude;
        float distanciaAlObjetivo = Vector3.Distance(transform.position, posicionObjetivo);

        if (ajustarDistanciaSiempre)
        {
            if (distanciaActual < distanciaMinima)
            {
                DebugLog("LOGIN_UI: panel muy cerca. Ajustando distancia.");
                return true;
            }

            if (distanciaActual > distanciaMaxima)
            {
                DebugLog("LOGIN_UI: panel muy lejos. Ajustando distancia.");
                return true;
            }
        }

        if (distanciaAlObjetivo > distanciaMaximaAlObjetivo)
        {
            return true;
        }

        if (direccionActual.sqrMagnitude < 0.001f || direccionIdeal.sqrMagnitude < 0.001f)
        {
            return true;
        }

        direccionActual.Normalize();
        direccionIdeal.Normalize();

        float angulo = Vector3.Angle(direccionActual, direccionIdeal);

        if (angulo > anguloMaximoAntesDeRecentrar)
        {
            DebugLog("LOGIN_UI: fuera del ángulo permitido. Ángulo: " + angulo);
            return true;
        }

        return false;
    }

    private void MoverSuave()
    {
        transform.position = Vector3.SmoothDamp(
            transform.position,
            posicionObjetivo,
            ref velocidadSmooth,
            tiempoSuavizadoPosicion,
            velocidadMaxima
        );
    }

    private void RotarSuave()
    {
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            rotacionObjetivo,
            Time.deltaTime * velocidadRotacion
        );
    }

    [ContextMenu("Recentrar ahora")]
    public void RecentrarAhora()
    {
        BuscarCabezaUsuario();

        if (cabezaUsuario == null)
        {
            Debug.LogWarning("LOGIN_UI: no se encontró la cabeza o cámara del usuario.");
            return;
        }

        CalcularObjetivo();

        transform.position = posicionObjetivo;
        transform.rotation = rotacionObjetivo;
        velocidadSmooth = Vector3.zero;

        DebugLog("LOGIN_UI: recenter inmediato.");
    }

    private void DebugLog(string mensaje)
    {
        if (mostrarDebug)
        {
            Debug.Log(mensaje);
        }
    }
}