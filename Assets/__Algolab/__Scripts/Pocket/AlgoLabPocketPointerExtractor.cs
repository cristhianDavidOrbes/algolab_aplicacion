using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AlgoLabPocketPointerExtractor : MonoBehaviour
{
    [Header("Referencias")]
    public AlgoLabPanelPocketManager pocketManager;
    public Transform rightHand;
    public Transform leftPocketWorldPoint;
    public Transform pointerDot;
    public GameObject pointerVisualRoot;

    [Header("Detección")]
    public float distanciaMostrarPuntero = 0.30f;
    public float distanciaTotalmenteVisible = 0.20f;

    [Tooltip("Radio de detección de la esfera. La card solo se activa cuando el puntero está prácticamente encima.")]
    public float radioDeteccionMiniCard = 0.015f;

    public LayerMask capasMiniCard = ~0;
    public QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Collide;

    [Tooltip("Activado = además de Physics.OverlapSphere, busca por distancia a todas las mini cards visibles. Esto corrige cuando el collider UI no entra bien en el overlap.")]
    public bool usarBusquedaRespaldoPorDistancia = true;

    [Tooltip("Distancia de respaldo si OverlapSphere no detecta. Se mantiene corta para evitar seleccionar cards desde lejos.")]
    public float distanciaRespaldoMiniCard = 0.022f;

    [Header("Bloqueos de agarre")]
    [Tooltip("Activado = respeta el cooldown del Manager y no deja agarrar cards mientras hay un panel real agarrado.")]
    public bool respetarBloqueoDelManager = true;

    [Tooltip("Activado = mientras arrastras una mini card, desactiva temporalmente los GrabHandle de paneles para no llevar dos cosas a la vez.")]
    public bool desactivarGrabHandlesMientrasCardAgarrada = true;

    [Tooltip("Busca objetos/scripts que tengan este texto en el nombre. Normalmente GrabHandleBottom, GrabHandleBottom2, etc.")]
    public string filtroNombreGrabHandle = "GrabHandle";

    [Header("Agarre de mini card")]
    public bool usarGripDerecho = true;
    public bool usarGatilloDerecho = true;

    [Tooltip("Usa también Any controller por si OVRInput no está leyendo RTouch correctamente.")]
    public bool usarAnyControllerComoRespaldo = true;

    public float umbralBoton = 0.45f;

    [Header("Botones del panel de opciones")]
    public bool permitirClickBotonesPanelOpciones = true;
    public float radioDeteccionBotonesPanelOpciones = 0.025f;
    public bool bloquearMiniCardsCuandoHoverBoton = true;

    [Tooltip("La card agarrada se pega a la esfera.")]
    public bool cardSigueSpherePoint = true;

    [Tooltip("Si al soltar la card se restaura el panel.")]
    public bool soltarParaRestaurar = true;

    [Tooltip("Distancia mínima para considerar que el usuario ya la sacó del arco.")]
    public float distanciaMinimaArrastreParaSacar = 0.025f;

    [Header("Visual al agarrar card")]
    [Tooltip("Activado = cuando agarras una mini card, crece un poco.")]
    public bool crecerCardAlAgarrar = true;

    [Tooltip("1.2 = crece 20% al agarrarla.")]
    public float escalaCardAgarrada = 1.2f;

    [Tooltip("Activado = si el usuario ya tiene una mini card agarrada, esa card se mantiene visible aunque se aleje del arco.")]
    public bool mantenerCardVisibleMientrasAgarrada = true;

    [Header("Visual")]
    [Tooltip("Activado = la bola aparece siempre que exista al menos un panel guardado.")]
    public bool mostrarBolaSiempreSiHayPanelesGuardados = false;

    [Header("Mostrar arco por cercanía")]
    [Tooltip("Activado = aunque no haya paneles guardados, reporta al Manager la cercanía del mando derecho al arco/mando izquierdo para que aparezca el arco.")]
    public bool reportarCercaniaAlManagerAunqueNoHayaPaneles = true;

    [Tooltip("Activado = calcula la cercanía por distancia entre PointerDot y LeftPocketWorldPoint.")]
    public bool usarDistanciaParaReportarArco = true;

    [Header("Modo muy cerca - forzado")]
    [Tooltip("Activado = fuerza en runtime las distancias 0.30 / 0.20 aunque Unity tenga valores viejos guardados en el Inspector.")]
    public bool forzarDistanciasMuyCercaEnRuntime = true;

    [Tooltip("Activado = la bola del puntero solo aparece si hay paneles guardados Y el mando derecho está muy cerca del punto izquierdo.")]
    public bool mostrarBolaSoloSiMandoDerechoMuyCerca = true;

    [Tooltip("Si está activado, la bola solo aparece cuando el mando derecho está cerca del arco.")]
    public bool usarDistanciaParaMostrarBola = true;

    [Tooltip("No apagues el GameObject del SpherePoint, porque si se apaga el script deja de revisar si ya hay paneles guardados.")]
    public bool nuncaDesactivarEsteObjeto = true;

    public bool ocultarRenderersPuntero = true;
    public Renderer[] renderersPuntero;
    public float velocidadFade = 12f;

    [Header("Debug")]
    public bool mostrarDebug = true;
    public bool dibujarGizmos = true;

    private AlgoLabPocketMiniCardView cardHover;
    private AlgoLabPocketMiniCardView cardAgarrada;
    private AlgoLabPocketPanelItem panelCardAgarrada;
    private Button botonPanelOpcionesHover;
    private bool botonPanelOpcionesPresionadoAnterior;

    private readonly List<Behaviour> behavioursGrabDesactivados = new List<Behaviour>();
    private readonly List<Collider> collidersGrabDesactivados = new List<Collider>();
    private Vector3 posicionInicioArrastre;
    private Quaternion rotacionOriginalCard;
    private Vector3 posicionOriginalCard;
    private Vector3 escalaOriginalCard;
    private Transform parentOriginalCard;
    private float alphaOriginalCard;
    private float alphaPunteroActual;
    private float ultimoDebugBloqueo = -999f;
    private AlgoLabPocketMiniCardView[] cardsRespaldo = System.Array.Empty<AlgoLabPocketMiniCardView>();
    private float proximaBusquedaCardsRespaldo;
    private readonly Dictionary<Renderer, RendererFadeState> estadosFadeRenderers =
        new Dictionary<Renderer, RendererFadeState>();
    private MaterialPropertyBlock bloquePropiedades;

    private class RendererFadeState
    {
        public int[] propiedadesColor;
        public Color[] coloresBase;
    }

    private void Awake()
    {
        bloquePropiedades = new MaterialPropertyBlock();
        AplicarConfiguracionMuyCercaRuntime();
        AutoBuscarReferencias();

        if (ocultarRenderersPuntero)
        {
            SetRenderersAlpha(0f);
        }
    }

    private void OnEnable()
    {
        AplicarConfiguracionMuyCercaRuntime();
        AutoBuscarReferencias();
    }

    private void Update()
    {
        AplicarConfiguracionMuyCercaRuntime();
        AutoBuscarReferencias();

        if (!PuedeUsarPocketPorJuego())
        {
            OcultarPunteroPorJuegoNoIniciado();
            return;
        }

        ActualizarVisibilidadPuntero();

        bool sobreBotonPanelOpciones = ProcesarBotonesPanelOpciones();

        if (sobreBotonPanelOpciones && bloquearMiniCardsCuandoHoverBoton && cardAgarrada == null)
        {
            LimpiarHoverMiniCard();
            return;
        }

        DetectarMiniCard();
        ProcesarAgarreCard();
    }

    [ContextMenu("Auto buscar referencias")]
    public void AutoBuscarReferencias()
    {
        if (pocketManager == null)
        {
            pocketManager = AlgoLabPanelPocketManager.Instance;
        }

        if (pointerDot == null)
        {
            pointerDot = transform;
        }

        if (rightHand == null)
        {
            rightHand = transform.parent;
        }

        if (pocketManager != null)
        {
            if (leftPocketWorldPoint == null)
            {
                leftPocketWorldPoint = pocketManager.leftPocketWorldPoint;
            }
        }

        if (pointerVisualRoot == null)
        {
            pointerVisualRoot = gameObject;
        }

        if (renderersPuntero == null || renderersPuntero.Length == 0)
        {
            renderersPuntero = GetComponentsInChildren<Renderer>(true);
        }
    }


    private bool PuedeUsarPocketPorJuego()
    {
        if (pocketManager == null)
        {
            return false;
        }

        return pocketManager.ArcoDisponibleParaInteraccion || (cardAgarrada != null && mantenerCardVisibleMientrasAgarrada);
    }

    private void OcultarPunteroPorJuegoNoIniciado()
    {
        if (pocketManager != null)
        {
            pocketManager.ReportarCercaniaMandoDerecho(0f);
        }

        alphaPunteroActual = Mathf.MoveTowards(
            alphaPunteroActual,
            0f,
            Time.unscaledDeltaTime * Mathf.Max(1f, velocidadFade)
        );

        if (pointerVisualRoot != null && !(nuncaDesactivarEsteObjeto && pointerVisualRoot == gameObject))
        {
            pointerVisualRoot.SetActive(false);
        }

        if (ocultarRenderersPuntero)
        {
            SetRenderersAlpha(alphaPunteroActual);
        }

        if (cardHover != null)
        {
            cardHover.SetHover(false);
            cardHover = null;
        }

        botonPanelOpcionesHover = null;
        botonPanelOpcionesPresionadoAnterior = false;
    }

    private void AplicarConfiguracionMuyCercaRuntime()
    {
        if (!forzarDistanciasMuyCercaEnRuntime)
        {
            return;
        }

        distanciaMostrarPuntero = 0.30f;
        distanciaTotalmenteVisible = 0.20f;
        radioDeteccionMiniCard = 0.015f;
        distanciaRespaldoMiniCard = 0.022f;
        radioDeteccionBotonesPanelOpciones = 0.025f;

        mostrarBolaSiempreSiHayPanelesGuardados = false;
        usarDistanciaParaMostrarBola = true;
        reportarCercaniaAlManagerAunqueNoHayaPaneles = true;
        usarDistanciaParaReportarArco = true;
        mantenerCardVisibleMientrasAgarrada = true;
    }

    private void ActualizarVisibilidadPuntero()
    {
        if (!PuedeUsarPocketPorJuego())
        {
            OcultarPunteroPorJuegoNoIniciado();
            return;
        }

        bool hayPaneles = pocketManager != null && pocketManager.HayPanelesGuardados();
        float objetivo = 0f;

        if (hayPaneles)
        {
            if (mostrarBolaSoloSiMandoDerechoMuyCerca && pointerDot != null && leftPocketWorldPoint != null)
            {
                float distancia = Vector3.Distance(pointerDot.position, leftPocketWorldPoint.position);
                objetivo = CalcularCercania01(distancia);
            }
            else if (mostrarBolaSiempreSiHayPanelesGuardados)
            {
                objetivo = 1f;
            }
            else if (usarDistanciaParaMostrarBola && pointerDot != null && leftPocketWorldPoint != null)
            {
                float distancia = Vector3.Distance(pointerDot.position, leftPocketWorldPoint.position);
                objetivo = CalcularCercania01(distancia);
            }
            else
            {
                objetivo = 1f;
            }
        }

        float cercaniaParaManager = objetivo;

        if (reportarCercaniaAlManagerAunqueNoHayaPaneles &&
            usarDistanciaParaReportarArco &&
            pointerDot != null &&
            leftPocketWorldPoint != null)
        {
            float distancia = Vector3.Distance(pointerDot.position, leftPocketWorldPoint.position);
            cercaniaParaManager = Mathf.Max(cercaniaParaManager, CalcularCercania01(distancia));
        }

        if (cardAgarrada != null && mantenerCardVisibleMientrasAgarrada)
        {
            // Si la card ya está en la mano, la bola/card no debe desaparecer aunque
            // el mando se aleje del arco.
            objetivo = 1f;
            cercaniaParaManager = Mathf.Max(cercaniaParaManager, 1f);
            cardAgarrada.SetAlpha(1f);
        }

        if (pocketManager != null)
        {
            pocketManager.ReportarCercaniaMandoDerecho(cercaniaParaManager);
        }

        alphaPunteroActual = Mathf.MoveTowards(
            alphaPunteroActual,
            objetivo,
            Time.unscaledDeltaTime * Mathf.Max(1f, velocidadFade)
        );

        bool debeVerse = alphaPunteroActual > 0.01f || cardAgarrada != null;

        if (pointerVisualRoot != null && !(nuncaDesactivarEsteObjeto && pointerVisualRoot == gameObject))
        {
            pointerVisualRoot.SetActive(debeVerse);
        }

        if (ocultarRenderersPuntero)
        {
            SetRenderersAlpha(alphaPunteroActual);
        }
    }

    private float CalcularCercania01(float distancia)
    {
        if (distanciaMostrarPuntero <= distanciaTotalmenteVisible)
        {
            return distancia <= distanciaMostrarPuntero ? 1f : 0f;
        }

        if (distancia >= distanciaMostrarPuntero) return 0f;
        if (distancia <= distanciaTotalmenteVisible) return 1f;

        return Mathf.Clamp01(Mathf.InverseLerp(distanciaMostrarPuntero, distanciaTotalmenteVisible, distancia));
    }

    private void SetRenderersAlpha(float alpha)
    {
        if (bloquePropiedades == null)
        {
            bloquePropiedades = new MaterialPropertyBlock();
        }

        if (renderersPuntero == null) return;

        alpha = Mathf.Clamp01(alpha);

        for (int i = 0; i < renderersPuntero.Length; i++)
        {
            Renderer r = renderersPuntero[i];

            if (r == null) continue;

            r.enabled = alpha > 0.01f || cardAgarrada != null;

            if (!estadosFadeRenderers.TryGetValue(r, out RendererFadeState estado))
            {
                Material[] materiales = r.sharedMaterials;
                estado = new RendererFadeState
                {
                    propiedadesColor = new int[materiales.Length],
                    coloresBase = new Color[materiales.Length]
                };

                for (int m = 0; m < materiales.Length; m++)
                {
                    Material material = materiales[m];
                    if (material == null) continue;

                    int propiedad = material.HasProperty("_BaseColor")
                        ? Shader.PropertyToID("_BaseColor")
                        : material.HasProperty("_Color")
                            ? Shader.PropertyToID("_Color")
                            : 0;

                    estado.propiedadesColor[m] = propiedad;
                    if (propiedad != 0) estado.coloresBase[m] = material.GetColor(propiedad);
                }

                estadosFadeRenderers[r] = estado;
            }

            for (int m = 0; m < estado.propiedadesColor.Length; m++)
            {
                int propiedad = estado.propiedadesColor[m];
                if (propiedad == 0) continue;

                bloquePropiedades.Clear();
                r.GetPropertyBlock(bloquePropiedades, m);
                Color color = estado.coloresBase[m];
                color.a *= alpha;
                bloquePropiedades.SetColor(propiedad, color);
                r.SetPropertyBlock(bloquePropiedades, m);
            }
        }
    }

    private bool ProcesarBotonesPanelOpciones()
    {
        bool botonPresionado = BotonAgarrarPresionado();
        Button boton = null;

        if (permitirClickBotonesPanelOpciones &&
            cardAgarrada == null &&
            pocketManager != null &&
            pointerDot != null)
        {
            boton = pocketManager.ObtenerBotonPanelOpcionesEnPunto(
                pointerDot.position,
                radioDeteccionBotonesPanelOpciones
            );
        }

        if (botonPanelOpcionesHover != boton)
        {
            botonPanelOpcionesHover = boton;

            if (mostrarDebug && botonPanelOpcionesHover != null)
            {
                Debug.Log("POCKET POINTER: boton del panel de opciones detectado: " + botonPanelOpcionesHover.name);
            }
        }

        if (boton != null && botonPresionado && !botonPanelOpcionesPresionadoAnterior)
        {
            pocketManager.ClickBotonPanelOpciones(boton);
        }

        botonPanelOpcionesPresionadoAnterior = botonPresionado;
        return boton != null;
    }

    private void LimpiarHoverMiniCard()
    {
        if (cardHover == null)
        {
            return;
        }

        cardHover.SetHover(false);
        cardHover = null;
    }

    private void DetectarMiniCard()
    {
        if (cardAgarrada != null)
        {
            return;
        }

        if (pointerDot == null)
        {
            return;
        }

        if (!PuedeInteractuarConMiniCards())
        {
            if (cardHover != null)
            {
                cardHover.SetHover(false);
                cardHover = null;
            }

            return;
        }

        AlgoLabPocketMiniCardView mejor = BuscarPorPhysics();

        if (mejor == null && usarBusquedaRespaldoPorDistancia)
        {
            mejor = BuscarPorDistancia();
        }

        if (cardHover != null && cardHover != mejor)
        {
            cardHover.SetHover(false);
        }

        bool cambioHover = cardHover != mejor;
        cardHover = mejor;

        if (cardHover != null)
        {
            cardHover.SetHover(true);

            if (mostrarDebug && cambioHover)
            {
                Debug.Log("POCKET POINTER: mini card detectada: " + (cardHover.Panel != null ? cardHover.Panel.nombreCorto : "sin panel"));
            }
        }
    }

    private AlgoLabPocketMiniCardView BuscarPorPhysics()
    {
        Collider[] hits = Physics.OverlapSphere(pointerDot.position, radioDeteccionMiniCard, capasMiniCard, triggerInteraction);
        AlgoLabPocketMiniCardView mejor = null;
        float mejorDist = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider c = hits[i];

            if (c == null || !c.enabled)
            {
                continue;
            }

            AlgoLabPocketMiniCardView card = c.GetComponentInParent<AlgoLabPocketMiniCardView>();

            if (card == null || card.Panel == null || !card.gameObject.activeInHierarchy)
            {
                continue;
            }

            float d = Vector3.Distance(pointerDot.position, c.ClosestPoint(pointerDot.position));

            if (d < mejorDist)
            {
                mejorDist = d;
                mejor = card;
            }
        }

        return mejor;
    }

    private AlgoLabPocketMiniCardView BuscarPorDistancia()
    {
        if (Time.unscaledTime >= proximaBusquedaCardsRespaldo)
        {
            cardsRespaldo = FindObjectsByType<AlgoLabPocketMiniCardView>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );
            proximaBusquedaCardsRespaldo = Time.unscaledTime + 0.5f;
        }

        AlgoLabPocketMiniCardView mejor = null;
        float mejorDist = float.MaxValue;

        for (int i = 0; i < cardsRespaldo.Length; i++)
        {
            AlgoLabPocketMiniCardView card = cardsRespaldo[i];

            if (card == null || card.Panel == null || !card.gameObject.activeInHierarchy)
            {
                continue;
            }

            float alpha = card.Alpha;

            if (alpha <= 0.15f)
            {
                continue;
            }

            Vector3 puntoCard = card.transform.position;

            Collider col = card.GetComponent<Collider>();
            if (col != null && col.enabled)
            {
                puntoCard = col.ClosestPoint(pointerDot.position);
            }

            float d = Vector3.Distance(pointerDot.position, puntoCard);

            if (d <= distanciaRespaldoMiniCard && d < mejorDist)
            {
                mejorDist = d;
                mejor = card;
            }
        }

        return mejor;
    }

    private void ProcesarAgarreCard()
    {
        bool botonPresionado = BotonAgarrarPresionado();

        if (cardAgarrada == null)
        {
            if (cardHover != null && botonPresionado && PuedeInteractuarConMiniCards())
            {
                IniciarAgarre(cardHover);
            }

            return;
        }

        if (cardSigueSpherePoint && pointerDot != null)
        {
            cardAgarrada.transform.position = pointerDot.position;
            cardAgarrada.transform.rotation = pointerDot.rotation;
        }

        if (mantenerCardVisibleMientrasAgarrada)
        {
            cardAgarrada.SetAlpha(1f);
        }

        if (!botonPresionado)
        {
            SoltarCard();
        }
    }

    private bool PuedeInteractuarConMiniCards()
    {
        if (!respetarBloqueoDelManager)
        {
            return true;
        }

        if (pocketManager == null)
        {
            pocketManager = AlgoLabPanelPocketManager.Instance;
        }

        if (pocketManager == null)
        {
            return true;
        }

        if (!pocketManager.ArcoDisponibleParaInteraccion)
        {
            return false;
        }

        bool puede = pocketManager.PuedeAgarrarCards();

        if (!puede && mostrarDebug && Time.unscaledTime - ultimoDebugBloqueo > 0.75f)
        {
            ultimoDebugBloqueo = Time.unscaledTime;
            string motivo = pocketManager.MotivoBloqueoAgarrarCards();
            if (!string.IsNullOrEmpty(motivo))
            {
                Debug.Log("POCKET POINTER: no puede agarrar mini card porque " + motivo);
            }
        }

        return puede;
    }

    private bool BotonAgarrarPresionado()
    {
        float gripR = 0f;
        float triggerR = 0f;
        float gripAny = 0f;
        float triggerAny = 0f;

        try
        {
            gripR = OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, OVRInput.Controller.RTouch);
            triggerR = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, OVRInput.Controller.RTouch);

            if (usarAnyControllerComoRespaldo)
            {
                gripAny = OVRInput.Get(OVRInput.Axis1D.SecondaryHandTrigger, OVRInput.Controller.All);
                triggerAny = OVRInput.Get(OVRInput.Axis1D.SecondaryIndexTrigger, OVRInput.Controller.All);
            }
        }
        catch
        {
            gripR = 0f;
            triggerR = 0f;
            gripAny = 0f;
            triggerAny = 0f;
        }

#if UNITY_EDITOR
        if (Input.GetMouseButton(0) || Input.GetKey(KeyCode.G))
        {
            return true;
        }
#endif

        bool gripOk = usarGripDerecho && (gripR >= umbralBoton || gripAny >= umbralBoton);
        bool triggerOk = usarGatilloDerecho && (triggerR >= umbralBoton || triggerAny >= umbralBoton);

        return gripOk || triggerOk;
    }

    private void IniciarAgarre(AlgoLabPocketMiniCardView card)
    {
        if (card == null || pointerDot == null)
        {
            return;
        }

        if (card.Panel != null && card.Panel.esAccionConfiguracion)
        {
            if (pocketManager == null)
            {
                pocketManager = AlgoLabPanelPocketManager.Instance;
            }

            card.SetHover(false);
            cardHover = null;
            pocketManager?.IntentarActivarAccionConfiguracionDesdeCard(card);
            return;
        }

        cardAgarrada = card;
        panelCardAgarrada = card.Panel;
        cardHover = null;

        parentOriginalCard = card.transform.parent;
        posicionOriginalCard = card.transform.position;
        rotacionOriginalCard = card.transform.rotation;
        escalaOriginalCard = card.transform.localScale;
        alphaOriginalCard = card.Alpha;
        posicionInicioArrastre = pointerDot.position;

        if (crecerCardAlAgarrar)
        {
            card.transform.localScale = escalaOriginalCard * Mathf.Max(1f, escalaCardAgarrada);
        }

        card.SetHover(true);

        if (mantenerCardVisibleMientrasAgarrada)
        {
            card.SetAlpha(1f);
        }

        if (pocketManager == null)
        {
            pocketManager = AlgoLabPanelPocketManager.Instance;
        }

        if (pocketManager != null)
        {
            pocketManager.NotificarMiniCardAgarrada(true);
        }

        if (desactivarGrabHandlesMientrasCardAgarrada)
        {
            DesactivarGrabHandlesTemporalmente();
        }

        if (mostrarDebug)
        {
            Debug.Log("POCKET POINTER: agarró card " + (card.Panel != null ? card.Panel.nombreCorto : "sin panel"));
        }
    }

    private void SoltarCard()
    {
        if (cardAgarrada == null)
        {
            return;
        }

        AlgoLabPocketMiniCardView card = cardAgarrada;
        AlgoLabPocketPanelItem panelAgarrado = panelCardAgarrada;
        cardAgarrada = null;
        panelCardAgarrada = null;

        float distanciaArrastre = pointerDot != null ? Vector3.Distance(posicionInicioArrastre, pointerDot.position) : 0f;

        if (soltarParaRestaurar && pocketManager != null && distanciaArrastre >= distanciaMinimaArrastreParaSacar)
        {
            card.SetHover(false);

            // IMPORTANTE:
            // Congelamos la pose exactamente en el frame de suelta.
            // No mandamos pointerDot como Transform vivo porque durante la animación
            // el usuario puede bajar el mando y el panel terminaría apareciendo bajo.
            ObtenerPoseCongeladaDeSuelta(out Vector3 posicionSuelta, out Quaternion rotacionSuelta);
            bool restauracionIniciada = pocketManager.IntentarRestaurarPanelDesdePoseCongelada(
                panelAgarrado,
                posicionSuelta,
                rotacionSuelta,
                card
            );

            if (!restauracionIniciada)
            {
                RestaurarCardAgarradaAlArco(card);
            }
        }
        else
        {
            RestaurarCardAgarradaAlArco(card);

            if (pocketManager != null)
            {
                pocketManager.ProbarMostrarCarrusel();
            }
        }

        RestaurarGrabHandlesTemporales();

        if (pocketManager != null)
        {
            pocketManager.NotificarMiniCardAgarrada(false);
        }

        if (mostrarDebug)
        {
            Debug.Log("POCKET POINTER: soltó card. Distancia arrastre=" + distanciaArrastre.ToString("F2"));
        }
    }

    private void RestaurarCardAgarradaAlArco(AlgoLabPocketMiniCardView card)
    {
        if (card == null)
        {
            return;
        }

        card.transform.SetParent(parentOriginalCard, true);
        card.transform.position = posicionOriginalCard;
        card.transform.rotation = rotacionOriginalCard;
        card.transform.localScale = escalaOriginalCard;
        card.SetAlpha(alphaOriginalCard);
        card.SetHover(false);
    }

    private void ObtenerPoseCongeladaDeSuelta(
        out Vector3 posicionSuelta,
        out Quaternion rotacionSuelta)
    {
        // IMPORTANTE:
        // La pose de restauración debe venir del SpherePoint / pointerDot,
        // no de la mini card. La mini card puede quedar con offsets del arco/canvas
        // y por eso algunos paneles, sobre todo el tutorial, aparecen arriba o abajo.
        if (pointerDot != null)
        {
            posicionSuelta = pointerDot.position;
            rotacionSuelta = pointerDot.rotation;
            return;
        }

        posicionSuelta = transform.position;
        rotacionSuelta = transform.rotation;
    }

    private void DesactivarGrabHandlesTemporalmente()
    {
        RestaurarGrabHandlesTemporales();

        string filtro = string.IsNullOrEmpty(filtroNombreGrabHandle) ? "grabhandle" : filtroNombreGrabHandle.ToLower();

        AlgoLabPanelGrabHandle[] behaviours = FindObjectsByType<AlgoLabPanelGrabHandle>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < behaviours.Length; i++)
        {
            AlgoLabPanelGrabHandle b = behaviours[i];

            if (b == null || b == this || !b.enabled)
            {
                continue;
            }

            string texto = (b.gameObject.name + " " + b.GetType().Name).ToLower();

            if (texto.Contains(filtro) && !texto.Contains("pocketmini") && !texto.Contains("spherepoint"))
            {
                b.enabled = false;
                behavioursGrabDesactivados.Add(b);
            }
        }

        Collider[] colliders = FindObjectsByType<Collider>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider c = colliders[i];

            if (c == null || !c.enabled)
            {
                continue;
            }

            AlgoLabPanelGrabHandle handle = c.GetComponentInParent<AlgoLabPanelGrabHandle>(true);
            string texto = (c.gameObject.name + " " + c.GetType().Name).ToLower();

            if (handle != null && texto.Contains(filtro) &&
                !texto.Contains("pocketmini") && !texto.Contains("spherepoint"))
            {
                c.enabled = false;
                collidersGrabDesactivados.Add(c);
            }
        }

        if (mostrarDebug)
        {
            Debug.Log("POCKET POINTER: grab handles desactivados temporalmente. Scripts=" +
                      behavioursGrabDesactivados.Count + " Colliders=" + collidersGrabDesactivados.Count);
        }
    }

    private void RestaurarGrabHandlesTemporales()
    {
        for (int i = 0; i < behavioursGrabDesactivados.Count; i++)
        {
            Behaviour b = behavioursGrabDesactivados[i];
            if (b != null)
            {
                b.enabled = true;
            }
        }

        for (int i = 0; i < collidersGrabDesactivados.Count; i++)
        {
            Collider c = collidersGrabDesactivados[i];
            if (c != null)
            {
                c.enabled = true;
            }
        }

        behavioursGrabDesactivados.Clear();
        collidersGrabDesactivados.Clear();
    }

    private void OnDisable()
    {
        if (cardAgarrada != null)
        {
            AlgoLabPocketMiniCardView card = cardAgarrada;
            cardAgarrada = null;
            panelCardAgarrada = null;
            RestaurarCardAgarradaAlArco(card);
        }

        panelCardAgarrada = null;

        LimpiarHoverMiniCard();
        RestaurarGrabHandlesTemporales();
        botonPanelOpcionesHover = null;
        botonPanelOpcionesPresionadoAnterior = false;

        if (pocketManager != null)
        {
            pocketManager.NotificarMiniCardAgarrada(false);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!dibujarGizmos || pointerDot == null)
        {
            return;
        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(pointerDot.position, radioDeteccionMiniCard);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(pointerDot.position, distanciaRespaldoMiniCard);
    }
}
