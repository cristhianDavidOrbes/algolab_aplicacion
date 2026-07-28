using System.Collections;
using System.Reflection;
using UnityEngine;

public class AlgoLabPocketPanelItem : MonoBehaviour
{
    private enum ManoDetectada
    {
        Ninguna,
        Izquierda,
        Derecha,
        Desconocida
    }

    [Header("Datos compactos")]
    public string nombreCorto = "Panel";
    public Sprite iconoMini;

    [Header("Reglas")]
    public bool puedeGuardarse = true;
    public bool esPanelPrincipal = false;

    [Tooltip("Elemento virtual del carrusel que abre Configuracion y no representa un panel guardado.")]
    public bool esAccionConfiguracion = false;

    [Header("Referencias")]
    public Transform panelRoot;
    public AlgoLabPanelPocketManager pocketManager;

    [Header("Punto para medir guardado")]
    [Tooltip("Arrastra aquí el GrabHandle del panel. La distancia al arco se mide desde este punto, no desde el centro del panel.")]
    public Transform puntoMedicionGuardado;

    public bool usarPuntoMedicionGuardado = true;
    public bool usarCentroVisualSiNoHayPunto = true;

    [Header("Restauración precisa")]
    [Tooltip("Activado = cuando sale del pocket, la posición donde sueltas la mini card queda alineada con el Punto Medición Guardado/GrabHandle, no con el centro del root. Esto evita que el tutorial aparezca lejos por el offset del Canvas.")]
    public bool usarPuntoMedicionComoAnclaAlRestaurar = true;

    [Header("Guardado automático")]
    public bool autoGuardarAlEstarCercaDelBolsillo = true;

    [Tooltip("Tiempo que el GrabHandle debe estar cerca del arco para guardar.")]
    public float tiempoCercaParaGuardar = 0.20f;

    [Tooltip("Margen extra sobre la distancia del Manager. Si quieres regular la cercanía, normalmente toca Distancia Guardar Panel en el Manager.")]
    public float margenExtraAutoGuardar = 0.05f;

    [Header("Nueva regla: no guardar con mano izquierda")]
    [Tooltip("Activado = si el panel lo sostiene la mano izquierda, NO se guarda aunque esté tocando el arco.")]
    public bool bloquearGuardadoSiLoSostieneManoIzquierda = true;

    [Tooltip("Activado = solo permite guardar si el panel lo lleva la mano derecha.")]
    public bool soloGuardarConManoDerecha = true;

    [Tooltip("Permite guardar mientras el panel está agarrado con la mano derecha y el GrabHandle entra al arco.")]
    public bool guardarMientrasManoDerechaEstaCerca = true;

    [Tooltip("Permite guardar si sueltas el panel cerca del arco después de haberlo llevado con la mano derecha.")]
    public bool guardarAlSoltarCercaDespuesDeManoDerecha = true;

    [Tooltip("Si no puede saber qué mano lo sostiene, NO guarda. Déjalo desactivado para evitar guardados accidentales.")]
    public bool permitirGuardarSiManoDesconocida = false;

    [Header("Referencias de manos para validar")]
    [Tooltip("Arrastra RightControllerAnchor o el objeto del mando derecho.")]
    public Transform rightHandParaValidarGuardado;

    [Tooltip("Arrastra PocketWorldPoint o el mando izquierdo. Recomendado: PocketWorldPoint del arco.")]
    public Transform leftHandParaValidarGuardado;

    [Tooltip("Distancia máxima para considerar que el GrabHandle está realmente pegado a una mano.")]
    public float distanciaMaximaParaConsiderarMano = 0.28f;

    [Tooltip("Diferencia mínima para decidir si está más cerca de una mano que de la otra.")]
    public float toleranciaComparacionMano = 0.03f;

    [Header("Detección de agarre")]
    public bool detectarAgarrePorGrabHandle = true;

    [Tooltip("Si tu panel usa AlgoLabPanelGrabHandle, arrástralo aquí. Si lo dejas vacío, se busca solo.")]
    public MonoBehaviour grabHandleReferencia;

    [Tooltip("Desactivado = si no encuentra GrabHandle ni evento de agarre, NO asume que está agarrado. Evita que bloquee las cards para siempre.")]
    public bool siNoHayGrabHandleAsumirAgarrado = false;

    [Header("Bloqueo de cards cerca del arco")]
    [Tooltip("Activado = mientras este panel esté agarrado y cerca del arco, bloquea agarrar mini cards.")]
    public bool bloquearCardsMientrasEstePanelEstaCercaDelArco = true;

    [Tooltip("Usa la misma distancia de guardado del Manager. Puedes subirlo a 1.2 si quieres bloquear un poco antes.")]
    public float multiplicadorDistanciaBloqueoCards = 1f;

    [Header("Restauración de escala")]
    [Tooltip("IMPORTANTE: activado evita que el panel vuelva chiquito después de guardarse.")]
    public bool protegerEscalaOriginal = true;

    [Tooltip("Si está activado, fuerza la escala que tenía el panel antes de encogerse.")]
    public bool restaurarEscalaOriginal = true;

    [Tooltip("Úsalo solo si quieres escribir manualmente la escala normal. Normalmente déjalo en 0,0,0.")]
    public Vector3 escalaOriginalManual = Vector3.zero;

    [Tooltip("Activado = mientras el panel está visible, guarda la escala más grande observada como escala normal.")]
    public bool usarEscalaMasGrandeObservada = true;

    [Tooltip("Evita que una escala chiquita sobreescriba la escala normal.")]
    public float margenActualizarEscalaMayor = 1.03f;

    [Header("Restauración desde pocket / anti pelea")]
    [Tooltip("Activado = al restaurar desde pocket, desregistra este root del ajuste de altura dinámica para que el ManualPanelSpawnManager no lo baje ni lo mueva después.")]
    public bool bloquearAlturaDinamicaDespuesDeRestaurar = true;

    [Tooltip("Activado = avisa al TutorialPanelController cuando se restaura/guarda. OJO: si el TutorialPanelController mueve posición, puede pelear con el Pocket.")]
    public bool avisarTutorialAlRestaurar = true;

    [Tooltip("Activado = bloquea los avisos al TutorialPanelController al guardar/restaurar para que ningún script externo recoloque el root. Recomendado para el tutorial.")]
    public bool bloquearAvisosTutorialQuePuedenMoverRoot = true;

    [Tooltip("Activado = aunque se bloqueen avisos que podrían mover el root, sí avisa al tutorial que entró/salió del arco. Esto permite saltar/repetir el tutorial sin recolocar el panel.")]
    public bool avisarTutorialAunqueBloqueeMovimiento = true;

    [Header("Estado dentro del arco")]
    [Tooltip("Activado = cuando este panel está guardado dentro del arco, se apaga el GameObject real del panel. Solo se ve su mini card.")]
    public bool desactivarRootMientrasEstaEnArco = true;

    [Tooltip("Activado = mientras está guardado, también apaga renderers, colliders, canvases y raycasts por seguridad.")]
    public bool desactivarComponentesMientrasEstaEnArco = true;

    [Tooltip("Activado = cuando sale del arco, el GameObject real y sus componentes vuelven a activarse.")]
    public bool reactivarRootAlSalirDelArco = true;

    [Header("Debug")]
    public bool mostrarDebug = true;
    public bool mostrarDebugMano = false;

    private Transform padreOriginal;
    private Vector3 posicionOriginal;
    private Quaternion rotacionOriginal;
    private Vector3 escalaOriginal;

    private bool escalaOriginalCapturada;
    private bool estaGuardado;
    private bool desactivadoPorPocket;
    private bool animandoEncogido;

    // FIX FINAL: durante la animación de restaurar desde el pocket hay un rebote de escala
    // (por ejemplo 1.08). Si Update captura esa escala como "escala original",
    // cada ciclo pocket -> sacar agranda el panel un poco más y el grab se vuelve raro.
    private bool restaurandoDesdePocket;

    private float tiempoCercaBolsillo;

    private bool agarradoPorEvento;
    private ManoDetectada ultimaManoDetectada = ManoDetectada.Ninguna;
    private ManoDetectada ultimaManoValidaMientrasAgarrado = ManoDetectada.Ninguna;
    private bool ultimoAgarreFueValidoParaGuardar;

    private Renderer[] renderers;
    private Collider[] colliders;
    private Canvas[] canvases;
    private CanvasGroup[] canvasGroups;
    private bool[] estadoRenderersAntesPocket;
    private bool[] estadoCollidersAntesPocket;
    private bool[] estadoCanvasesAntesPocket;
    private float[] alphaCanvasGroupsAntesPocket;
    private bool[] interactableCanvasGroupsAntesPocket;
    private bool[] raycastsCanvasGroupsAntesPocket;
    private bool estadosVisualesCapturados;

    private Transform padreAntesAnimacionGuardado;
    private Vector3 posicionAntesAnimacionGuardado;
    private Quaternion rotacionAntesAnimacionGuardado;
    private Vector3 escalaAntesAnimacionGuardado;
    private bool poseAntesAnimacionGuardadoValida;

    private void Awake()
    {
        if (panelRoot == null)
        {
            panelRoot = transform;
        }

        if (pocketManager == null)
        {
            pocketManager = AlgoLabPanelPocketManager.Instance;
        }

        AutoCompletarReferenciasManos();
        CacheComponentesVisuales();
        CapturarEstadoOriginalSiHaceFalta(true);
    }

    private void OnEnable()
    {
        if (panelRoot == null)
        {
            panelRoot = transform;
        }

        AutoCompletarReferenciasManos();
        CapturarEstadoOriginalSiHaceFalta(false);
        SincronizarEstadoPocketAlActivarse();
    }

    private void OnDisable()
    {
        // Al guardarse en el pocket el root puede desactivarse mientras está encogido.
        // No queremos que estados temporales queden pegados para el siguiente ciclo.
        tiempoCercaBolsillo = 0f;
        restaurandoDesdePocket = false;
    }

    private void Update()
    {
        AutoCompletarReferenciasManos();
        ActualizarEscalaOriginalSiCorresponde();
        SincronizarEstadoPocketAlActivarse();

        if (!autoGuardarAlEstarCercaDelBolsillo)
        {
            return;
        }

        if (estaGuardado || !puedeGuardarse || esPanelPrincipal)
        {
            tiempoCercaBolsillo = 0f;
            return;
        }

        if (pocketManager == null)
        {
            pocketManager = AlgoLabPanelPocketManager.Instance;
        }

        if (pocketManager == null || pocketManager.leftPocketWorldPoint == null)
        {
            tiempoCercaBolsillo = 0f;
            return;
        }

        bool agarrado = EstaAgarrado();

        if (!agarrado)
        {
            tiempoCercaBolsillo = 0f;
            return;
        }

        if (pocketManager != null)
        {
            // Heartbeat: mantiene el bloqueo solo mientras de verdad sigue agarrado.
            pocketManager.ActualizarPanelRealAgarrado(this);
        }

        ReportarSiPanelAgarradoEstaCercaDelArco(agarrado);

        ManoDetectada manoActual = DeterminarManoQueSostiene();
        ultimaManoDetectada = manoActual;

        bool manoValida = EsManoValidaParaGuardar(manoActual);

        if (manoValida)
        {
            ultimaManoValidaMientrasAgarrado = manoActual;
            ultimoAgarreFueValidoParaGuardar = true;
        }
        else
        {
            // Si se detecta izquierda, bloquea el guardado de este agarre.
            if (manoActual == ManoDetectada.Izquierda)
            {
                ultimoAgarreFueValidoParaGuardar = false;
            }
        }

        if (!guardarMientrasManoDerechaEstaCerca || !manoValida)
        {
            tiempoCercaBolsillo = 0f;
            return;
        }

        float distancia = DistanciaGrabAlBolsillo();
        float distanciaPermitida = DistanciaPermitidaParaGuardar();

        if (distancia <= distanciaPermitida)
        {
            tiempoCercaBolsillo += Time.unscaledDeltaTime;

            if (tiempoCercaBolsillo >= tiempoCercaParaGuardar)
            {
                if (mostrarDebug)
                {
                    Debug.Log("POCKET ITEM: guardar con mano válida. Panel=" + nombreCorto +
                              " Mano=" + manoActual +
                              " Distancia=" + distancia.ToString("F2") +
                              " Permitida=" + distanciaPermitida.ToString("F2"));
                }

                pocketManager.IntentarGuardarPanel(this);
                tiempoCercaBolsillo = 0f;
            }
        }
        else
        {
            tiempoCercaBolsillo = 0f;
        }
    }

    private void AutoCompletarReferenciasManos()
    {
        if (pocketManager == null)
        {
            pocketManager = AlgoLabPanelPocketManager.Instance;
        }

        if (pocketManager == null)
        {
            return;
        }

        if (leftHandParaValidarGuardado == null)
        {
            leftHandParaValidarGuardado = pocketManager.leftPocketWorldPoint;
        }

        if (rightHandParaValidarGuardado == null)
        {
            rightHandParaValidarGuardado = pocketManager.rightHandParaMostrarCarrusel;
        }
    }

    private void CacheComponentesVisuales()
    {
        Transform raiz = panelRoot != null ? panelRoot : transform;
        renderers = raiz.GetComponentsInChildren<Renderer>(true);
        colliders = raiz.GetComponentsInChildren<Collider>(true);
        canvases = raiz.GetComponentsInChildren<Canvas>(true);
        canvasGroups = raiz.GetComponentsInChildren<CanvasGroup>(true);
    }

    private void CapturarEstadoOriginalSiHaceFalta(bool forzarPosicionYPadre)
    {
        if (panelRoot == null)
        {
            return;
        }

        if (forzarPosicionYPadre || padreOriginal == null)
        {
            padreOriginal = panelRoot.parent;
            posicionOriginal = panelRoot.position;
            rotacionOriginal = panelRoot.rotation;
        }

        if (!escalaOriginalCapturada || !protegerEscalaOriginal)
        {
            RegistrarEscalaNormalSiMayor(escalaOriginalManual != Vector3.zero ? escalaOriginalManual : panelRoot.localScale);
        }
    }

    private void ActualizarPadreOriginalSinTocarEscala()
    {
        if (panelRoot == null)
        {
            return;
        }

        if (padreOriginal == null)
        {
            padreOriginal = panelRoot.parent;
        }

        posicionOriginal = panelRoot.position;
        rotacionOriginal = panelRoot.rotation;
    }

    public Vector3 ObtenerPosicionMundo()
    {
        if (usarPuntoMedicionGuardado && puntoMedicionGuardado != null)
        {
            return puntoMedicionGuardado.position;
        }

        if (usarCentroVisualSiNoHayPunto)
        {
            Vector3 centro;
            if (IntentarObtenerCentroVisual(out centro))
            {
                return centro;
            }
        }

        if (panelRoot != null)
        {
            return panelRoot.position;
        }

        return transform.position;
    }

    private bool IntentarObtenerCentroVisual(out Vector3 centro)
    {
        centro = Vector3.zero;

        Transform raiz = panelRoot != null ? panelRoot : transform;
        bool tieneBounds = false;
        Bounds bounds = new Bounds(raiz.position, Vector3.zero);

        Renderer[] rs = raiz.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < rs.Length; i++)
        {
            Renderer r = rs[i];

            if (r == null || !r.enabled)
            {
                continue;
            }

            if (!tieneBounds)
            {
                bounds = r.bounds;
                tieneBounds = true;
            }
            else
            {
                bounds.Encapsulate(r.bounds);
            }
        }

        Collider[] cs = raiz.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < cs.Length; i++)
        {
            Collider c = cs[i];

            if (c == null || !c.enabled)
            {
                continue;
            }

            if (!tieneBounds)
            {
                bounds = c.bounds;
                tieneBounds = true;
            }
            else
            {
                bounds.Encapsulate(c.bounds);
            }
        }

        if (!tieneBounds)
        {
            return false;
        }

        centro = bounds.center;
        return true;
    }

    private void ReportarSiPanelAgarradoEstaCercaDelArco(bool agarrado)
    {
        if (!bloquearCardsMientrasEstePanelEstaCercaDelArco)
        {
            return;
        }

        if (!agarrado || estaGuardado || pocketManager == null)
        {
            return;
        }

        float distancia = DistanciaGrabAlBolsillo();
        float permitida = DistanciaPermitidaParaGuardar() * Mathf.Max(0.1f, multiplicadorDistanciaBloqueoCards);

        if (distancia <= permitida)
        {
            pocketManager.ActualizarPanelGuardableCercaDelArco(this);
        }
    }

    private void ActualizarEscalaOriginalSiCorresponde()
    {
        if (!usarEscalaMasGrandeObservada || panelRoot == null || estaGuardado || animandoEncogido || restaurandoDesdePocket)
        {
            return;
        }

        RegistrarEscalaNormalSiMayor(panelRoot.localScale);
    }

    private void RegistrarEscalaNormalSiMayor(Vector3 candidata)
    {
        if (escalaOriginalManual != Vector3.zero)
        {
            escalaOriginal = escalaOriginalManual;
            escalaOriginalCapturada = true;
            return;
        }

        if (candidata == Vector3.zero)
        {
            return;
        }

        if (!escalaOriginalCapturada)
        {
            escalaOriginal = candidata;
            escalaOriginalCapturada = true;
            return;
        }

        float magActual = escalaOriginal.sqrMagnitude;
        float magNueva = candidata.sqrMagnitude;
        float margen = Mathf.Max(1.0001f, margenActualizarEscalaMayor);
        float margenCuadrado = margen * margen;

        if (magNueva > magActual * margenCuadrado)
        {
            escalaOriginal = candidata;
            escalaOriginalCapturada = true;

            if (mostrarDebug)
            {
                Debug.Log("POCKET ITEM: escala original actualizada por escala mayor. " + nombreCorto + " escala=" + escalaOriginal);
            }
        }
    }

    private float DistanciaGrabAlBolsillo()
    {
        if (pocketManager == null || pocketManager.leftPocketWorldPoint == null)
        {
            return float.MaxValue;
        }

        return Vector3.Distance(ObtenerPosicionMundo(), pocketManager.leftPocketWorldPoint.position);
    }

    private float DistanciaPermitidaParaGuardar()
    {
        if (pocketManager == null)
        {
            return margenExtraAutoGuardar;
        }

        return pocketManager.distanciaGuardarPanel + margenExtraAutoGuardar;
    }

    private ManoDetectada DeterminarManoQueSostiene()
    {
        Vector3 punto = ObtenerPosicionMundo();

        bool tieneDerecha = rightHandParaValidarGuardado != null;
        bool tieneIzquierda = leftHandParaValidarGuardado != null;

        if (!tieneDerecha && !tieneIzquierda)
        {
            return ManoDetectada.Desconocida;
        }

        float distDer = tieneDerecha ? Vector3.Distance(punto, rightHandParaValidarGuardado.position) : float.MaxValue;
        float distIzq = tieneIzquierda ? Vector3.Distance(punto, leftHandParaValidarGuardado.position) : float.MaxValue;

        bool cercaDer = distDer <= distanciaMaximaParaConsiderarMano;
        bool cercaIzq = distIzq <= distanciaMaximaParaConsiderarMano;

        if (mostrarDebugMano)
        {
            Debug.Log("POCKET ITEM MANO: " + nombreCorto +
                      " distDer=" + distDer.ToString("F2") +
                      " distIzq=" + distIzq.ToString("F2") +
                      " cercaDer=" + cercaDer +
                      " cercaIzq=" + cercaIzq);
        }

        if (cercaDer && !cercaIzq)
        {
            return ManoDetectada.Derecha;
        }

        if (cercaIzq && !cercaDer)
        {
            return ManoDetectada.Izquierda;
        }

        if (cercaDer && cercaIzq)
        {
            if (distDer + toleranciaComparacionMano < distIzq)
            {
                return ManoDetectada.Derecha;
            }

            if (distIzq + toleranciaComparacionMano < distDer)
            {
                return ManoDetectada.Izquierda;
            }

            // Si está casi igual de cerca de las dos, preferimos derecha si está permitido.
            // Esto permite llevar el panel con derecha hasta el arco izquierdo.
            return soloGuardarConManoDerecha ? ManoDetectada.Derecha : ManoDetectada.Desconocida;
        }

        return ManoDetectada.Desconocida;
    }

    private bool EsManoValidaParaGuardar(ManoDetectada mano)
    {
        if (mano == ManoDetectada.Izquierda && bloquearGuardadoSiLoSostieneManoIzquierda)
        {
            return false;
        }

        if (soloGuardarConManoDerecha)
        {
            return mano == ManoDetectada.Derecha || (mano == ManoDetectada.Desconocida && permitirGuardarSiManoDesconocida);
        }

        if (mano == ManoDetectada.Desconocida)
        {
            return permitirGuardarSiManoDesconocida;
        }

        return true;
    }

    public bool EstaGuardado()
    {
        return estaGuardado;
    }

    public bool EstaDesactivadoPorPocket()
    {
        return estaGuardado && desactivadoPorPocket;
    }

    public bool EstaDesactivadoExternamente()
    {
        return !EstaActivoEnEscena() && !desactivadoPorPocket;
    }

    private void SincronizarEstadoPocketAlActivarse()
    {
        if (!estaGuardado)
        {
            desactivadoPorPocket = false;
            return;
        }

        // Configuracion es una entrada virtual del carrusel: su root debe seguir
        // activo y nunca debe interpretarse como un panel real extraido del pocket.
        if (esAccionConfiguracion)
        {
            desactivadoPorPocket = true;
            tiempoCercaBolsillo = 0f;
            return;
        }

        if (pocketManager == null)
        {
            pocketManager = AlgoLabPanelPocketManager.Instance;
        }

        if (pocketManager != null && pocketManager.NotificarPanelActivadoExternamente(this))
        {
            estaGuardado = false;
            desactivadoPorPocket = false;
            tiempoCercaBolsillo = 0f;
            SetVisualActivo(true);
            return;
        }

        if (pocketManager != null && !pocketManager.EstaPanelRegistradoComoGuardado(this))
        {
            estaGuardado = false;
            desactivadoPorPocket = false;
            tiempoCercaBolsillo = 0f;
            SetVisualActivo(true);
        }
    }

    public Transform ObtenerPanelRoot()
    {
        if (panelRoot == null)
        {
            panelRoot = transform;
        }

        return panelRoot;
    }

    public bool EstaActivoEnEscena()
    {
        Transform root = ObtenerPanelRoot();
        return root != null && root.gameObject.activeInHierarchy;
    }

    public bool EstaAgarrado()
    {
        if (agarradoPorEvento)
        {
            return true;
        }

        if (!detectarAgarrePorGrabHandle)
        {
            return true;
        }

        if (grabHandleReferencia == null)
        {
            BuscarGrabHandleAutomatico();
        }

        if (grabHandleReferencia == null)
        {
            return agarradoPorEvento || siNoHayGrabHandleAsumirAgarrado;
        }

        System.Type tipo = grabHandleReferencia.GetType();
        BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        string[] nombresBool = new string[]
        {
            "EstaAgarrado",
            "estaAgarrado",
            "IsGrabbed",
            "isGrabbed",
            "agarrado",
            "estaSiendoAgarrado",
            "isGrabbing",
            "grabbing",
            "grabActivo",
            "grabbed"
        };

        for (int i = 0; i < nombresBool.Length; i++)
        {
            PropertyInfo prop = tipo.GetProperty(nombresBool[i], flags);
            if (prop != null && prop.PropertyType == typeof(bool))
            {
                return (bool)prop.GetValue(grabHandleReferencia, null);
            }

            FieldInfo field = tipo.GetField(nombresBool[i], flags);
            if (field != null && field.FieldType == typeof(bool))
            {
                return (bool)field.GetValue(grabHandleReferencia);
            }

            MethodInfo method = tipo.GetMethod(nombresBool[i], flags, null, System.Type.EmptyTypes, null);
            if (method != null && method.ReturnType == typeof(bool))
            {
                return (bool)method.Invoke(grabHandleReferencia, null);
            }
        }

        // MUY IMPORTANTE:
        // Si el GrabHandle existe pero no tiene una variable bool reconocible,
        // NO asumimos que está agarrado. Antes esto devolvía true y dejaba
        // bloqueadas las mini cards después de sacar la primera.
        return agarradoPorEvento || siNoHayGrabHandleAsumirAgarrado;
    }

    private void BuscarGrabHandleAutomatico()
    {
        Transform raiz = panelRoot != null ? panelRoot : transform;
        MonoBehaviour[] behaviours = raiz.GetComponentsInChildren<MonoBehaviour>(true);

        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour b = behaviours[i];

            if (b == null)
            {
                continue;
            }

            string nombre = b.GetType().Name.ToLower();

            if (nombre.Contains("grabhandle") || nombre.Contains("grab"))
            {
                grabHandleReferencia = b;
                return;
            }
        }
    }

    public void NotificarAgarreIniciado()
    {
        agarradoPorEvento = true;
        tiempoCercaBolsillo = 0f;

        if (pocketManager == null)
        {
            pocketManager = AlgoLabPanelPocketManager.Instance;
        }

        if (pocketManager != null)
        {
            pocketManager.NotificarPanelRealAgarrado(this);
        }

        ultimaManoDetectada = DeterminarManoQueSostiene();
        ultimoAgarreFueValidoParaGuardar = EsManoValidaParaGuardar(ultimaManoDetectada);

        if (ultimoAgarreFueValidoParaGuardar)
        {
            ultimaManoValidaMientrasAgarrado = ultimaManoDetectada;
        }

        if (mostrarDebug)
        {
            Debug.Log("POCKET ITEM: agarre iniciado " + nombreCorto + " mano=" + ultimaManoDetectada + " valido=" + ultimoAgarreFueValidoParaGuardar);
        }
    }

    public void NotificarSoltado()
    {
        bool estabaAgarradoValido = ultimoAgarreFueValidoParaGuardar;
        ManoDetectada manoAntesDeSoltar = ultimaManoValidaMientrasAgarrado;

        agarradoPorEvento = false;

        if (pocketManager == null)
        {
            pocketManager = AlgoLabPanelPocketManager.Instance;
        }

        if (pocketManager != null)
        {
            pocketManager.NotificarPanelRealSoltado(this);
        }

        if (!estaGuardado && pocketManager != null && guardarAlSoltarCercaDespuesDeManoDerecha)
        {
            float distancia = DistanciaGrabAlBolsillo();
            float permitida = DistanciaPermitidaParaGuardar();

            if (distancia <= permitida && estabaAgarradoValido)
            {
                if (mostrarDebug)
                {
                    Debug.Log("POCKET ITEM: guardar al soltar cerca. Panel=" + nombreCorto +
                              " ManoPrev=" + manoAntesDeSoltar +
                              " Distancia=" + distancia.ToString("F2") +
                              " Permitida=" + permitida.ToString("F2"));
                }

                pocketManager.GuardarPanelTrasSoltarValidado(this);
            }
            else if (mostrarDebug)
            {
                Debug.Log("POCKET ITEM: NO guardar al soltar. Panel=" + nombreCorto +
                          " valido=" + estabaAgarradoValido +
                          " ManoPrev=" + manoAntesDeSoltar +
                          " Distancia=" + distancia.ToString("F2") +
                          " Permitida=" + permitida.ToString("F2"));
            }
        }
        else if (!estaGuardado && pocketManager == null)
        {
            Debug.LogWarning("POCKET ITEM: no existe AlgoLabPanelPocketManager.");
        }

        if (pocketManager != null)
        {
            pocketManager.LimpiarPanelGuardableCercaDelArco(this);
        }

        tiempoCercaBolsillo = 0f;
        ultimoAgarreFueValidoParaGuardar = false;
        ultimaManoValidaMientrasAgarrado = ManoDetectada.Ninguna;
    }

    public IEnumerator AnimarEncogerHacia(Transform destino, float duracion, float escalaFinal)
    {
        if (panelRoot == null)
        {
            yield break;
        }

        if (!escalaOriginalCapturada || !protegerEscalaOriginal)
        {
            RegistrarEscalaNormalSiMayor(escalaOriginalManual != Vector3.zero ? escalaOriginalManual : panelRoot.localScale);
        }

        Vector3 posInicio = panelRoot.position;
        Quaternion rotInicio = panelRoot.rotation;
        Vector3 escalaInicio = panelRoot.localScale;

        padreAntesAnimacionGuardado = panelRoot.parent;
        posicionAntesAnimacionGuardado = posInicio;
        rotacionAntesAnimacionGuardado = rotInicio;
        escalaAntesAnimacionGuardado = escalaInicio;
        poseAntesAnimacionGuardadoValida = true;

        if (protegerEscalaOriginal)
        {
            RegistrarEscalaNormalSiMayor(escalaInicio);
        }

        animandoEncogido = true;

        Vector3 posFinal = destino != null ? destino.position : panelRoot.position;
        Quaternion rotFinal = destino != null ? destino.rotation : panelRoot.rotation;
        Vector3 escalaDestino = escalaInicio * Mathf.Clamp(escalaFinal, 0.001f, 1f);

        float tiempo = 0f;
        duracion = Mathf.Max(0.01f, duracion);

        while (tiempo < duracion)
        {
            tiempo += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(tiempo / duracion);
            float smooth = Mathf.SmoothStep(0f, 1f, t);

            panelRoot.position = Vector3.Lerp(posInicio, posFinal, smooth);
            panelRoot.rotation = Quaternion.Slerp(rotInicio, rotFinal, smooth);
            panelRoot.localScale = Vector3.Lerp(escalaInicio, escalaDestino, smooth);

            yield return null;
        }

        animandoEncogido = false;
    }

    public void GuardarEnPocket(bool ocultarPanelReal)
    {
        if (panelRoot == null)
        {
            panelRoot = transform;
        }

        ActualizarPadreOriginalSinTocarEscala();

        estaGuardado = true;
        desactivadoPorPocket = ocultarPanelReal || desactivarRootMientrasEstaEnArco || desactivarComponentesMientrasEstaEnArco;
        tiempoCercaBolsillo = 0f;
        animandoEncogido = false;
        restaurandoDesdePocket = false;
        poseAntesAnimacionGuardadoValida = false;

        if (pocketManager != null)
        {
            pocketManager.LimpiarPanelGuardableCercaDelArco(this);
        }

        AvisarTutorialGuardadoSiCorresponde();

        // Si está dentro del arco, el panel real debe quedar apagado.
        // Así no se puede apuntar, agarrar ni ver el panel grande mientras solo debe existir la mini card.
        if (desactivarComponentesMientrasEstaEnArco)
        {
            SetVisualActivo(false);
        }

        if ((ocultarPanelReal || desactivarRootMientrasEstaEnArco) && panelRoot != null)
        {
            panelRoot.gameObject.SetActive(false);
        }

        if (mostrarDebug)
        {
            Debug.Log("POCKET ITEM: guardado " + nombreCorto + " | escala original protegida: " + escalaOriginal);
        }
    }


    public void ForzarEstadoDentroDelArco(bool dentroDelArco)
    {
        if (panelRoot == null)
        {
            panelRoot = transform;
        }

        if (dentroDelArco)
        {
            estaGuardado = true;
            desactivadoPorPocket = true;
            tiempoCercaBolsillo = 0f;
            animandoEncogido = false;
            restaurandoDesdePocket = false;

            if (desactivarComponentesMientrasEstaEnArco)
            {
                SetVisualActivo(false);
            }

            if (desactivarRootMientrasEstaEnArco && panelRoot != null && panelRoot.gameObject.activeSelf)
            {
                panelRoot.gameObject.SetActive(false);
            }

            return;
        }

        if (panelRoot != null && reactivarRootAlSalirDelArco)
        {
            panelRoot.gameObject.SetActive(true);
        }

        estaGuardado = false;
        desactivadoPorPocket = false;
        tiempoCercaBolsillo = 0f;
        SetVisualActivo(true);
    }

    public void LimpiarEstadoPocketSinActivar()
    {
        if (panelRoot == null)
        {
            panelRoot = transform;
        }

        estaGuardado = false;
        desactivadoPorPocket = false;
        tiempoCercaBolsillo = 0f;
        animandoEncogido = false;
        restaurandoDesdePocket = false;

        SetVisualActivo(true);
    }

    public void CancelarGuardadoNoConfirmado()
    {
        if (panelRoot == null)
        {
            panelRoot = transform;
        }

        estaGuardado = false;
        desactivadoPorPocket = false;
        tiempoCercaBolsillo = 0f;
        animandoEncogido = false;
        restaurandoDesdePocket = false;

        if (panelRoot != null)
        {
            panelRoot.gameObject.SetActive(true);

            if (poseAntesAnimacionGuardadoValida)
            {
                panelRoot.SetParent(padreAntesAnimacionGuardado, true);
                panelRoot.SetPositionAndRotation(
                    posicionAntesAnimacionGuardado,
                    rotacionAntesAnimacionGuardado
                );
                panelRoot.localScale = escalaAntesAnimacionGuardado;
            }
            else
            {
                panelRoot.localScale = ObtenerEscalaFinalRestauracion();
            }
        }

        poseAntesAnimacionGuardadoValida = false;
        SetVisualActivo(true);
    }

    public void RestaurarDesdePocket(Vector3 posicionMundo, Quaternion rotacionMundo)
    {
        if (panelRoot == null)
        {
            panelRoot = transform;
        }

        if (padreOriginal != null)
        {
            panelRoot.SetParent(padreOriginal, true);
        }

        BloquearAlturaDinamicaSiCorresponde();

        restaurandoDesdePocket = true;
        animandoEncogido = true;

        Vector3 escalaFinal = ObtenerEscalaFinalRestauracion();
        Vector3 escalaParaCalculo = restaurarEscalaOriginal ? escalaFinal : panelRoot.localScale;

        ForzarEstadoDentroDelArco(false);

        AplicarPoseRootRestauracion(posicionMundo, rotacionMundo, escalaParaCalculo);

        estaGuardado = false;
        desactivadoPorPocket = false;
        tiempoCercaBolsillo = 0f;

        BloquearAlturaDinamicaSiCorresponde();
        AvisarTutorialRestauradoSiCorresponde();

        animandoEncogido = false;
        restaurandoDesdePocket = false;

        if (mostrarDebug)
        {
            Debug.Log("POCKET ITEM: restaurado " + nombreCorto +
                      " | ancla=" + (puntoMedicionGuardado != null ? puntoMedicionGuardado.name : "sin punto") +
                      " | root=" + panelRoot.name +
                      " | escala final: " + panelRoot.localScale);
        }
    }

    public IEnumerator RestaurarDesdePocketAnimado(
        Vector3 posicionMundo,
        Quaternion rotacionMundo,
        float duracion,
        float escalaInicial,
        float rebote,
        bool mantenerPoseRootFija = false)
    {
        if (panelRoot == null)
        {
            panelRoot = transform;
        }

        if (padreOriginal != null)
        {
            panelRoot.SetParent(padreOriginal, true);
        }

        BloquearAlturaDinamicaSiCorresponde();

        restaurandoDesdePocket = true;
        animandoEncogido = true;

        Vector3 escalaFinal = ObtenerEscalaFinalRestauracion();
        Vector3 escalaPequena = escalaFinal * Mathf.Clamp(escalaInicial, 0.001f, 1f);
        Vector3 escalaRebote = escalaFinal * Mathf.Max(1f, rebote);

        ForzarEstadoDentroDelArco(false);

        if (mantenerPoseRootFija)
        {
            ObtenerPoseRootParaRestaurarConAncla(
                posicionMundo,
                rotacionMundo,
                escalaFinal,
                out Vector3 posicionRootFija,
                out Quaternion rotacionRootFija
            );

            panelRoot.SetPositionAndRotation(posicionRootFija, rotacionRootFija);
            panelRoot.localScale = escalaPequena;
        }
        else
        {
            AplicarPoseRootRestauracion(posicionMundo, rotacionMundo, escalaPequena);
        }

        float tiempo = 0f;
        duracion = Mathf.Max(0.01f, duracion);

        while (tiempo < duracion)
        {
            tiempo += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(tiempo / duracion);

            Vector3 escalaActual;

            if (t < 0.72f)
            {
                float p = Mathf.SmoothStep(0f, 1f, t / 0.72f);
                escalaActual = Vector3.Lerp(escalaPequena, escalaRebote, p);
            }
            else
            {
                float p = Mathf.SmoothStep(0f, 1f, (t - 0.72f) / 0.28f);
                escalaActual = Vector3.Lerp(escalaRebote, escalaFinal, p);
            }

            if (mantenerPoseRootFija)
            {
                panelRoot.localScale = escalaActual;
            }
            else
            {
                AplicarPoseRootRestauracion(posicionMundo, rotacionMundo, escalaActual);
            }

            yield return null;
        }

        if (mantenerPoseRootFija)
        {
            panelRoot.localScale = escalaFinal;
        }
        else
        {
            AplicarPoseRootRestauracion(posicionMundo, rotacionMundo, escalaFinal);
        }
        estaGuardado = false;
        desactivadoPorPocket = false;
        tiempoCercaBolsillo = 0f;

        BloquearAlturaDinamicaSiCorresponde();
        AvisarTutorialRestauradoSiCorresponde();

        animandoEncogido = false;
        restaurandoDesdePocket = false;
        poseAntesAnimacionGuardadoValida = false;

        if (mostrarDebug)
        {
            Debug.Log("POCKET ITEM: restaurado animado " + nombreCorto + " | escala final fija: " + escalaFinal);
        }
    }

    public void CompletarRestauracionInterrumpida()
    {
        if (panelRoot == null)
        {
            panelRoot = transform;
        }

        panelRoot.gameObject.SetActive(true);
        panelRoot.localScale = ObtenerEscalaFinalRestauracion();
        estaGuardado = false;
        desactivadoPorPocket = false;
        animandoEncogido = false;
        restaurandoDesdePocket = false;
        poseAntesAnimacionGuardadoValida = false;
        tiempoCercaBolsillo = 0f;
        SetVisualActivo(true);
    }

    public void ForzarEscalaNormalRestaurada()
    {
        if (panelRoot == null)
        {
            panelRoot = transform;
        }

        Vector3 escalaObjetivo = ObtenerEscalaFinalRestauracion();
        if (!EsEscalaRestaurable(escalaObjetivo))
        {
            escalaObjetivo = EsEscalaRestaurable(escalaAntesAnimacionGuardado)
                ? escalaAntesAnimacionGuardado
                : Vector3.one;
        }

        panelRoot.localScale = escalaObjetivo;
        RegistrarEscalaNormalSiMayor(escalaObjetivo);
    }

    private void AplicarPoseRootRestauracion(
        Vector3 posicionAnclaMundo,
        Quaternion rotacionMundo,
        Vector3 escalaObjetivo)
    {
        if (panelRoot == null)
        {
            return;
        }

        ObtenerPoseRootParaRestaurarConAncla(
            posicionAnclaMundo,
            rotacionMundo,
            escalaObjetivo,
            out Vector3 posicionRoot,
            out Quaternion rotacionRoot
        );

        panelRoot.localScale = escalaObjetivo;
        panelRoot.SetPositionAndRotation(posicionRoot, rotacionRoot);
    }

    private void ObtenerPoseRootParaRestaurarConAncla(
        Vector3 posicionAnclaMundo,
        Quaternion rotacionMundo,
        Vector3 escalaObjetivo,
        out Vector3 posicionRoot,
        out Quaternion rotacionRoot)
    {
        posicionRoot = posicionAnclaMundo;
        rotacionRoot = rotacionMundo;

        if (!usarPuntoMedicionComoAnclaAlRestaurar)
        {
            return;
        }

        if (!usarPuntoMedicionGuardado || puntoMedicionGuardado == null || panelRoot == null)
        {
            return;
        }

        if (puntoMedicionGuardado == panelRoot)
        {
            return;
        }

        // Si el punto de medición no pertenece al panel, no lo usamos como ancla
        // porque podría traer offsets de otro sistema.
        if (!puntoMedicionGuardado.IsChildOf(panelRoot))
        {
            if (mostrarDebug)
            {
                Debug.LogWarning("POCKET ITEM: Punto Medición Guardado no es hijo de Panel Root. " +
                                 "No se usará como ancla al restaurar. Panel=" + nombreCorto);
            }

            return;
        }

        Vector3 posicionOriginalRoot = panelRoot.position;
        Quaternion rotacionOriginalRoot = panelRoot.rotation;
        Vector3 escalaOriginalRoot = panelRoot.localScale;

        // Calculamos el offset real del GrabHandle respecto al root con la rotación
        // y escala finales. Así, al soltar la card, el GrabHandle queda exactamente
        // donde estaba el SpherePoint/pointerDot.
        panelRoot.rotation = rotacionMundo;
        panelRoot.localScale = escalaObjetivo;

        Vector3 offsetAnclaDesdeRoot = puntoMedicionGuardado.position - panelRoot.position;

        panelRoot.SetPositionAndRotation(posicionOriginalRoot, rotacionOriginalRoot);
        panelRoot.localScale = escalaOriginalRoot;

        posicionRoot = posicionAnclaMundo - offsetAnclaDesdeRoot;
        rotacionRoot = rotacionMundo;
    }

    private AlgoLabManualPanelSpawnManager BuscarSpawnManagerParaAltura()
    {
        AlgoLabManualPanelSpawnManager manager = AlgoLabManualPanelSpawnManager.Instance;

        if (manager == null)
        {
            manager = FindFirstObjectByType<AlgoLabManualPanelSpawnManager>(
                FindObjectsInactive.Include
            );
        }

        return manager;
    }

    private void BloquearAlturaDinamicaSiCorresponde()
    {
        if (!bloquearAlturaDinamicaDespuesDeRestaurar || panelRoot == null)
        {
            return;
        }

        AlgoLabManualPanelSpawnManager manager = BuscarSpawnManagerParaAltura();

        if (manager != null)
        {
            manager.DesregistrarObjetoParaAlturaDinamica(panelRoot);
        }
    }

    private void AvisarTutorialRestauradoSiCorresponde()
    {
        if (bloquearAvisosTutorialQuePuedenMoverRoot && !avisarTutorialAunqueBloqueeMovimiento)
        {
            return;
        }

        if (!avisarTutorialAlRestaurar || panelRoot == null)
        {
            return;
        }

        AlgoLabTutorialPanelController tutorial =
            panelRoot.GetComponentInChildren<AlgoLabTutorialPanelController>(true);

        if (tutorial == null)
        {
            tutorial = panelRoot.GetComponentInParent<AlgoLabTutorialPanelController>(true);
        }

        if (tutorial != null)
        {
            tutorial.NotificarTutorialRestauradoDesdePocket();
        }
    }

    private void AvisarTutorialGuardadoSiCorresponde()
    {
        if (bloquearAvisosTutorialQuePuedenMoverRoot && !avisarTutorialAunqueBloqueeMovimiento)
        {
            return;
        }

        if (!avisarTutorialAlRestaurar || panelRoot == null)
        {
            return;
        }

        AlgoLabTutorialPanelController tutorial =
            panelRoot.GetComponentInChildren<AlgoLabTutorialPanelController>(true);

        if (tutorial == null)
        {
            tutorial = panelRoot.GetComponentInParent<AlgoLabTutorialPanelController>(true);
        }

        if (tutorial != null)
        {
            tutorial.NotificarTutorialGuardadoEnPocket();
        }
    }

    private static bool EsEscalaRestaurable(Vector3 escala)
    {
        return !float.IsNaN(escala.x) && !float.IsInfinity(escala.x) &&
               !float.IsNaN(escala.y) && !float.IsInfinity(escala.y) &&
               !float.IsNaN(escala.z) && !float.IsInfinity(escala.z) &&
               Mathf.Abs(escala.x) > 0.0001f &&
               Mathf.Abs(escala.y) > 0.0001f &&
               Mathf.Abs(escala.z) > 0.0001f;
    }

    private Vector3 ObtenerEscalaFinalRestauracion()
    {
        if (escalaOriginalManual != Vector3.zero)
        {
            return escalaOriginalManual;
        }

        if (escalaOriginalCapturada)
        {
            return escalaOriginal;
        }

        if (panelRoot != null)
        {
            return panelRoot.localScale;
        }

        return Vector3.one;
    }

    private void SetVisualActivo(bool activo)
    {
        if (renderers == null || colliders == null || canvases == null || canvasGroups == null)
        {
            CacheComponentesVisuales();
        }

        if (!activo)
        {
            CapturarEstadosVisualesAntesDePocket();
        }

        if (renderers != null)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                renderers[i].enabled = activo
                    ? ObtenerEstado(estadoRenderersAntesPocket, i, renderers[i].enabled)
                    : false;
            }
        }

        if (colliders != null)
        {
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] == null) continue;
                colliders[i].enabled = activo
                    ? ObtenerEstado(estadoCollidersAntesPocket, i, colliders[i].enabled)
                    : false;
            }
        }

        if (canvases != null)
        {
            for (int i = 0; i < canvases.Length; i++)
            {
                if (canvases[i] == null) continue;
                canvases[i].enabled = activo
                    ? ObtenerEstado(estadoCanvasesAntesPocket, i, canvases[i].enabled)
                    : false;
            }
        }

        if (canvasGroups != null)
        {
            for (int i = 0; i < canvasGroups.Length; i++)
            {
                if (canvasGroups[i] != null)
                {
                    if (activo && estadosVisualesCapturados)
                    {
                        canvasGroups[i].alpha = ObtenerEstado(alphaCanvasGroupsAntesPocket, i, canvasGroups[i].alpha);
                        canvasGroups[i].interactable = ObtenerEstado(interactableCanvasGroupsAntesPocket, i, canvasGroups[i].interactable);
                        canvasGroups[i].blocksRaycasts = ObtenerEstado(raycastsCanvasGroupsAntesPocket, i, canvasGroups[i].blocksRaycasts);
                    }
                    else if (!activo)
                    {
                        canvasGroups[i].alpha = 0f;
                        canvasGroups[i].interactable = false;
                        canvasGroups[i].blocksRaycasts = false;
                    }
                }
            }
        }

        if (activo)
        {
            estadosVisualesCapturados = false;
        }
    }

    private void CapturarEstadosVisualesAntesDePocket()
    {
        if (estadosVisualesCapturados)
        {
            return;
        }

        estadoRenderersAntesPocket = CapturarEstados(renderers);
        estadoCollidersAntesPocket = CapturarEstados(colliders);
        estadoCanvasesAntesPocket = CapturarEstados(canvases);

        int cantidadGrupos = canvasGroups != null ? canvasGroups.Length : 0;
        alphaCanvasGroupsAntesPocket = new float[cantidadGrupos];
        interactableCanvasGroupsAntesPocket = new bool[cantidadGrupos];
        raycastsCanvasGroupsAntesPocket = new bool[cantidadGrupos];

        for (int i = 0; i < cantidadGrupos; i++)
        {
            CanvasGroup grupo = canvasGroups[i];
            if (grupo == null) continue;
            alphaCanvasGroupsAntesPocket[i] = grupo.alpha;
            interactableCanvasGroupsAntesPocket[i] = grupo.interactable;
            raycastsCanvasGroupsAntesPocket[i] = grupo.blocksRaycasts;
        }

        estadosVisualesCapturados = true;
    }

    private static bool[] CapturarEstados(Renderer[] componentes)
    {
        bool[] estados = new bool[componentes != null ? componentes.Length : 0];
        for (int i = 0; i < estados.Length; i++) estados[i] = componentes[i] != null && componentes[i].enabled;
        return estados;
    }

    private static bool[] CapturarEstados(Collider[] componentes)
    {
        bool[] estados = new bool[componentes != null ? componentes.Length : 0];
        for (int i = 0; i < estados.Length; i++) estados[i] = componentes[i] != null && componentes[i].enabled;
        return estados;
    }

    private static bool[] CapturarEstados(Canvas[] componentes)
    {
        bool[] estados = new bool[componentes != null ? componentes.Length : 0];
        for (int i = 0; i < estados.Length; i++) estados[i] = componentes[i] != null && componentes[i].enabled;
        return estados;
    }

    private static bool ObtenerEstado(bool[] estados, int indice, bool respaldo)
    {
        return estados != null && indice >= 0 && indice < estados.Length ? estados[indice] : respaldo;
    }

    private static float ObtenerEstado(float[] estados, int indice, float respaldo)
    {
        return estados != null && indice >= 0 && indice < estados.Length ? estados[indice] : respaldo;
    }

    [ContextMenu("Forzar soltar panel / limpiar bloqueo")]
    public void ForzarSoltarPanelLimpiarBloqueo()
    {
        agarradoPorEvento = false;
        tiempoCercaBolsillo = 0f;
        ultimoAgarreFueValidoParaGuardar = false;
        ultimaManoValidaMientrasAgarrado = ManoDetectada.Ninguna;

        if (pocketManager == null)
        {
            pocketManager = AlgoLabPanelPocketManager.Instance;
        }

        if (pocketManager != null)
        {
            pocketManager.NotificarPanelRealSoltado(this);
            pocketManager.LimpiarPanelGuardableCercaDelArco(this);
        }

        if (mostrarDebug)
        {
            Debug.Log("POCKET ITEM: bloqueo limpiado manualmente -> " + nombreCorto);
        }
    }

    [ContextMenu("Forzar capturar escala actual como original")]
    public void ForzarCapturarEscalaActualComoOriginal()
    {
        if (panelRoot == null)
        {
            panelRoot = transform;
        }

        escalaOriginal = panelRoot.localScale;
        escalaOriginalManual = escalaOriginal;
        escalaOriginalCapturada = true;

        if (mostrarDebug)
        {
            Debug.Log("POCKET ITEM: escala original manual capturada: " + escalaOriginal);
        }
    }

    [ContextMenu("Forzar escala normal desde escala manual si existe")]
    public void ForzarEscalaNormalDesdeManual()
    {
        if (escalaOriginalManual != Vector3.zero)
        {
            escalaOriginal = escalaOriginalManual;
            escalaOriginalCapturada = true;
        }
    }

    [ContextMenu("Probar guardar")]
    public void ProbarGuardar()
    {
        if (pocketManager == null)
        {
            pocketManager = AlgoLabPanelPocketManager.Instance;
        }

        if (pocketManager != null)
        {
            pocketManager.GuardarPanel(this);
        }
    }

    [ContextMenu("Probar restaurar")]
    public void ProbarRestaurar()
    {
        if (pocketManager == null)
        {
            pocketManager = AlgoLabPanelPocketManager.Instance;
        }

        if (pocketManager != null)
        {
            pocketManager.RestaurarPanel(this);
        }
    }
}
