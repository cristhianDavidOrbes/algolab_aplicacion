using System.Collections;
using UnityEngine;

/// <summary>
/// Ajusta la altura de cualquier objeto spawneado según la altura global sentado/parado
/// del AlgoLabManualPanelSpawnManager, sin pelearse con el grab.
///
/// Comportamiento:
/// - Mientras el usuario agarra el panel/objeto, este script NO mueve el objeto.
/// - Cuando el usuario suelta, guarda la posición donde quedó.
/// - Si después el usuario se sienta o se para, solo ajusta la altura Y manteniendo
///   la misma diferencia vertical respecto a la altura base.
///
/// Úsalo en prefabs spawneados que NO estén ya controlados por la lista Paneles
/// del AlgoLabManualPanelSpawnManager.
/// </summary>
public class AlgoLabSpawnedHeightAutoRegister : MonoBehaviour
{
    [Header("Objeto a ajustar")]
    [Tooltip("Root real que se moverá en Y. Si está vacío, se usa este transform.")]
    public Transform objetoRoot;

    [Header("Manager de altura")]
    [Tooltip("Arrastra aquí el AlgoLabManualPanelSpawnManager. Si queda vacío, se busca automáticamente.")]
    public AlgoLabManualPanelSpawnManager spawnManager;

    public bool buscarManagerAutomaticamente = true;

    [Tooltip("Usa la altura actual de la referencia manual del manager. Recomendado, porque ya tiene smooth sentado/parado.")]
    public bool usarAlturaReferenciaManualManager = true;

    [Header("Ajuste dinámico")]
    public bool ajustarAlturaEnTiempoReal = true;

    [Tooltip("Si está activo, solo modifica Y. X y Z quedan donde el usuario dejó el objeto.")]
    public bool ajustarSoloY = true;

    [Tooltip("Al activarse/spawnear, guarda la diferencia Y entre el objeto y la altura base actual.")]
    public bool guardarOffsetInicialAlActivar = true;

    [Tooltip("Espera un frame antes de guardar el offset inicial, útil cuando el prefab acaba de spawnear.")]
    public bool esperarUnFrameAlActivar = true;

    [Header("Compatibilidad con grab")]
    [Tooltip("Si está activo, busca AlgoLabPanelGrabHandle en hijos y padres para pausar el ajuste mientras se agarra.")]
    public bool detectarGrabAutomaticamente = true;

    [Tooltip("Mientras el objeto esté agarrado, este script no lo mueve para no pelearse con el grab.")]
    public bool pausarMientrasEstaAgarrado = true;

    [Tooltip("Cuando se suelta, guarda la nueva diferencia Y desde la posición donde el usuario dejó el objeto.")]
    public bool recalcularOffsetAlSoltar = true;

    [Tooltip("Espera un frame después de soltar antes de guardar el offset, para tomar la posición final real del grab.")]
    public bool esperarUnFrameAlSoltar = true;

    [Header("Grab handles detectados")]
    public AlgoLabPanelGrabHandle[] grabHandles;

    [Header("Smooth")]
    public bool usarSmooth = true;
    public float tiempoSmooth = 0.35f;
    public float velocidadMaxima = 4f;
    public float umbralActualizar = 0.003f;

    [Header("Debug")]
    public bool mostrarDebug = false;

    private bool inicializado;
    private bool estaAgarrado;
    private float offsetYRespectoAlturaBase;
    private float velocidadSmoothY;
    private Coroutine rutinaInicializar;
    private Coroutine rutinaSoltar;
    private AlgoLabTutorialPanelController tutorialPropietario;

    private void Awake()
    {
        PrepararReferencias();
    }

    private void OnEnable()
    {
        PrepararReferencias();
        ConectarGrabHandles();

        if (guardarOffsetInicialAlActivar)
        {
            if (rutinaInicializar != null)
            {
                StopCoroutine(rutinaInicializar);
            }

            rutinaInicializar = StartCoroutine(InicializarOffsetRutina());
        }
    }

    private void OnDisable()
    {
        DesconectarGrabHandles();

        if (rutinaInicializar != null)
        {
            StopCoroutine(rutinaInicializar);
            rutinaInicializar = null;
        }

        if (rutinaSoltar != null)
        {
            StopCoroutine(rutinaSoltar);
            rutinaSoltar = null;
        }
    }

    private void LateUpdate()
    {
        if (!ajustarAlturaEnTiempoReal || !inicializado)
        {
            return;
        }

        if (pausarMientrasEstaAgarrado && estaAgarrado)
        {
            return;
        }

        AjustarAlturaAhora(false);
    }

    private void PrepararReferencias()
    {
        AlinearRootConTutorialSiCorresponde();

        if (objetoRoot == null)
        {
            objetoRoot = transform;
        }

        if (spawnManager == null && buscarManagerAutomaticamente)
        {
            spawnManager = AlgoLabManualPanelSpawnManager.Instance;
        }

        if (spawnManager == null && buscarManagerAutomaticamente)
        {
            spawnManager = FindFirstObjectByType<AlgoLabManualPanelSpawnManager>(
                FindObjectsInactive.Include
            );
        }
    }

    private void AlinearRootConTutorialSiCorresponde()
    {
        if (tutorialPropietario == null)
        {
            tutorialPropietario = GetComponentInParent<AlgoLabTutorialPanelController>(true);
        }

        if (tutorialPropietario == null || tutorialPropietario.rootParaUbicar == null)
        {
            return;
        }

        Transform rootTutorial = tutorialPropietario.rootParaUbicar;

        if (objetoRoot == rootTutorial)
        {
            return;
        }

        // El agarre y el pocket mueven [TUTORIAL_SYSTEM]. Ajustar la altura de un
        // hijo distinto divide la pose entre dos transforms y acumula desfase al
        // soltar, guardar y restaurar varias veces.
        objetoRoot = rootTutorial;
        velocidadSmoothY = 0f;
    }

    private IEnumerator InicializarOffsetRutina()
    {
        if (esperarUnFrameAlActivar)
        {
            yield return null;
        }

        GuardarOffsetDesdePosicionActual();
        rutinaInicializar = null;
    }

    [ContextMenu("Guardar offset desde posición actual")]
    public void GuardarOffsetDesdePosicionActual()
    {
        PrepararReferencias();

        if (objetoRoot == null)
        {
            return;
        }

        float alturaBase = ObtenerAlturaBaseActual();
        offsetYRespectoAlturaBase = objetoRoot.position.y - alturaBase;
        inicializado = true;

        if (mostrarDebug)
        {
            Debug.Log(
                "HEIGHT AUTO REGISTER: offset guardado en " + gameObject.name +
                " | objetoY: " + objetoRoot.position.y.ToString("0.00") +
                " | alturaBase: " + alturaBase.ToString("0.00") +
                " | offset: " + offsetYRespectoAlturaBase.ToString("0.00")
            );
        }
    }

    [ContextMenu("Ajustar altura ahora")]
    public void AjustarAlturaAhoraManual()
    {
        AjustarAlturaAhora(true);
    }

    private void AjustarAlturaAhora(bool forzar)
    {
        PrepararReferencias();

        if (objetoRoot == null)
        {
            return;
        }

        float alturaBase = ObtenerAlturaBaseActual();
        float objetivoY = alturaBase + offsetYRespectoAlturaBase;
        Vector3 posicionActual = objetoRoot.position;

        if (!forzar && Mathf.Abs(posicionActual.y - objetivoY) <= umbralActualizar)
        {
            return;
        }

        float nuevaY;

        if (usarSmooth && !forzar)
        {
            nuevaY = Mathf.SmoothDamp(
                posicionActual.y,
                objetivoY,
                ref velocidadSmoothY,
                Mathf.Max(0.01f, tiempoSmooth),
                Mathf.Max(0.01f, velocidadMaxima),
                Mathf.Max(0.0001f, Time.unscaledDeltaTime)
            );
        }
        else
        {
            nuevaY = objetivoY;
        }

        if (ajustarSoloY)
        {
            posicionActual.y = nuevaY;
            objetoRoot.position = posicionActual;
        }
        else
        {
            Vector3 posicionObjetivo = posicionActual;
            posicionObjetivo.y = nuevaY;
            objetoRoot.position = posicionObjetivo;
        }
    }

    private float ObtenerAlturaBaseActual()
    {
        PrepararReferencias();

        if (spawnManager != null)
        {
            if (usarAlturaReferenciaManualManager && spawnManager.referenciaManual != null)
            {
                return spawnManager.referenciaManual.position.y;
            }

            return spawnManager.offsetAlturaGlobal;
        }

        Camera cam = Camera.main;

        if (cam != null)
        {
            return cam.transform.position.y;
        }

        return objetoRoot != null ? objetoRoot.position.y : transform.position.y;
    }

    private void ConectarGrabHandles()
    {
        if (!detectarGrabAutomaticamente)
        {
            return;
        }

        if (grabHandles == null || grabHandles.Length == 0)
        {
            grabHandles = BuscarGrabHandlesCercanos();
        }

        if (grabHandles == null)
        {
            return;
        }

        for (int i = 0; i < grabHandles.Length; i++)
        {
            AlgoLabPanelGrabHandle grab = grabHandles[i];

            if (grab == null)
            {
                continue;
            }

            grab.alIniciarAgarre.RemoveListener(NotificarInicioGrab);
            grab.alSoltarPanel.RemoveListener(NotificarFinGrab);

            grab.alIniciarAgarre.AddListener(NotificarInicioGrab);
            grab.alSoltarPanel.AddListener(NotificarFinGrab);
        }
    }

    private void DesconectarGrabHandles()
    {
        if (grabHandles == null)
        {
            return;
        }

        for (int i = 0; i < grabHandles.Length; i++)
        {
            AlgoLabPanelGrabHandle grab = grabHandles[i];

            if (grab == null)
            {
                continue;
            }

            grab.alIniciarAgarre.RemoveListener(NotificarInicioGrab);
            grab.alSoltarPanel.RemoveListener(NotificarFinGrab);
        }
    }

    private AlgoLabPanelGrabHandle[] BuscarGrabHandlesCercanos()
    {
        AlgoLabPanelGrabHandle[] enHijos = GetComponentsInChildren<AlgoLabPanelGrabHandle>(true);

        if (enHijos != null && enHijos.Length > 0)
        {
            return enHijos;
        }

        AlgoLabPanelGrabHandle[] enPadres = GetComponentsInParent<AlgoLabPanelGrabHandle>(true);

        if (enPadres != null && enPadres.Length > 0)
        {
            return enPadres;
        }

        return new AlgoLabPanelGrabHandle[0];
    }

    public void NotificarInicioGrab()
    {
        estaAgarrado = true;
        velocidadSmoothY = 0f;

        if (rutinaSoltar != null)
        {
            StopCoroutine(rutinaSoltar);
            rutinaSoltar = null;
        }

        if (mostrarDebug)
        {
            Debug.Log("HEIGHT AUTO REGISTER: pausado por grab en " + gameObject.name);
        }
    }

    public void NotificarFinGrab()
    {
        if (rutinaSoltar != null)
        {
            StopCoroutine(rutinaSoltar);
        }

        rutinaSoltar = StartCoroutine(FinGrabRutina());
    }

    private IEnumerator FinGrabRutina()
    {
        if (esperarUnFrameAlSoltar)
        {
            yield return null;
        }

        estaAgarrado = false;
        velocidadSmoothY = 0f;

        if (recalcularOffsetAlSoltar)
        {
            GuardarOffsetDesdePosicionActual();
        }

        if (mostrarDebug)
        {
            Debug.Log("HEIGHT AUTO REGISTER: reactivado después de soltar en " + gameObject.name);
        }

        rutinaSoltar = null;
    }
}
