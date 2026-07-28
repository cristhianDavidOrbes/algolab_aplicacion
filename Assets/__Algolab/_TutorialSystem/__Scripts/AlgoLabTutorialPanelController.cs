using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;

public class AlgoLabTutorialPanelController : MonoBehaviour
{
    public enum TipoEventoTutorial
    {
        MostrarPanel,
        RevelarVideo,
        CerrarPanel,

        CambiarInstruccion,
        OcultarInstruccion,

        ReproducirAudioClip,
        DetenerAudioActual,
        SilenciarTutorial,
        ActivarSonidoTutorial,
        AlternarSilencioTutorial,

        ReproducirVideoClip,
        DetenerVideoActual,
        PausarVideoActual,
        ReanudarVideoActual,

        MostrarPanelMando,
        OcultarPanelMando,
        ColapsarMando,
        ExpandirMando,

        CambiarMandoLateral,
        CambiarMandoFrontal,
        CambiarMandoLateralConTransicion,
        CambiarMandoFrontalConTransicion,

        MostrarMandoIdle,

        PresionarGatilloPrincipal,
        SoltarGatilloPrincipal,

        PresionarGatilloSecundario,
        SoltarGatilloSecundario,

        ForzarMantenerGatilloPrincipal,
        ForzarMantenerGatilloSecundario,

        PresionarBotonA,
        PresionarBotonB,

        MoverPalanca,
        SoltarPalanca,

        OmitirTutorial,
        FinalizarTutorial,
        IniciarPractica,
        ContinuarAplicacion,

        EsperarAccionInteractiva,
        EsperarFinVideoActual,
        OcultarIndicadoresGatillos,

        ActivarObjeto,
        DesactivarObjeto,
        CambiarImagen,
        EjecutarUnityEvent,

        MostrarImagenPanelPrincipal,
        OcultarImagenPanelPrincipal,
        MostrarImagenPanelDiagramas,
        OcultarImagenPanelDiagramas,
        MostrarImagenPanelIA,
        OcultarImagenPanelIA,
        OcultarImagenesPanelesTutorial,
        HabilitarPanelOpciones,
        DeshabilitarPanelOpciones,
        HabilitarArco,
        DeshabilitarArco
    }

    public enum AccionTutorialInteractiva
    {
        Ninguna,
        AgarrarPanel,
        MoverPanel,
        SoltarPanel,
        AgarrarMoverSoltarPanel,
        SeleccionarBotonNoIniciar,
        MeterPanelEnArco,
        SacarPanelDelArco,
        MeterYSacarPanelDelArco,
        SacarYMeterPanelDelArco
    }

    [System.Serializable]
    public class EventoTutorial
    {
        [Header("Tiempo")]
        public float tiempo = 0f;
        public int orden = 0;
        public TipoEventoTutorial tipoEvento;

        [Header("Texto")]
        [TextArea(1, 3)]
        public string texto;

        [Header("Multimedia")]
        public AudioClip audioClip;
        public VideoClip videoClip;
        public Texture imagen;

        [Header("Video Loop")]
        public bool repetirVideo = false;

        [Tooltip("Si es mayor que 0, el video se detiene después de esta duración.")]
        public float duracionReproduccionVideo = 0f;

        [Tooltip("Si está activo, el video inicia desde el comienzo.")]
        public bool reiniciarVideoDesdeInicio = true;

        [Header("Objetos")]
        public GameObject objeto;

        [Header("Evento personalizado")]
        public UnityEvent unityEvent;

        [Header("Acción interactiva")]
        public AccionTutorialInteractiva accionEsperada = AccionTutorialInteractiva.Ninguna;

        [Tooltip("Opcional. Si se asigna, solo este panel completa la acción. Si está vacío, cualquier panel sirve.")]
        public AlgoLabPanelGrabHandle panelEsperado;

        [HideInInspector] public bool ejecutado;
        [HideInInspector] public bool indicadorGatilloMostrado;
    }

    [Header("Panel principal")]
    public RectTransform panelRoot;
    public RectTransform tutorialMainPanel;
    public RectTransform introBlackPanel;
    public RectTransform tituloTutorialRect;

    [Header("Spawn inicial en cuadro verde del Manual Spawn")]
    public bool ubicarTutorialEnPuntoManual = true;

    [Tooltip("Activado = el tutorial solo se ubica una vez en el punto verde inicial. Si luego entra al arco/pocket y sale, NO vuelve al punto verde; queda donde sueltes la mini card.")]
    public bool ubicarTutorialSoloLaPrimeraVez = true;

    [Tooltip("Activado = cuando el tutorial vuelve del arco/pocket se marca como ya ubicado para que nunca vuelva a spawnear en el punto verde inicial.")]
    public bool noReubicarDespuesDeSalirDelPocket = true;

    [Tooltip("Activado = la primera aparicion usa la cabeza del jugador como referencia estable y no una referencia manual que puede estar muy baja.")]
    public bool ubicarPrimeraAparicionFrenteALaCabeza = false;

    [Tooltip("Distancia horizontal de la primera aparicion respecto a la cabeza.")]
    public float distanciaPrimeraAparicionFrenteCabeza = 0.9f;

    [Tooltip("Ajuste vertical de la primera aparicion respecto a los ojos.")]
    public float offsetVerticalPrimeraAparicion = -0.04f;

    [Tooltip("Si está vacío, se busca AlgoLabManualPanelSpawnManager.Instance.")]
    public AlgoLabManualPanelSpawnManager spawnManager;

    [Tooltip("Objeto que se moverá en el mundo. Recomendado: Canvas del tutorial.")]
    public Transform rootParaUbicar;

    [Tooltip("No se usa para mover la referencia. Se deja visible solo por compatibilidad con el Inspector.")]
    public bool actualizarReferenciaAntesDeMostrar = false;

    [Tooltip("Si está activo, usa la posición del objeto frontal del ManualPanelSpawnManager.")]
    public bool usarPosicionObjetoFrontalDelManager = true;

    [Tooltip("Ajuste extra sobre el cuadro verde. Déjalo en 0,0,0 si quieres exactamente el mismo punto.")]
    public Vector3 offsetLocalTutorialDesdeObjetoFrontal = Vector3.zero;

    [Tooltip("Solo se usa si Usar Posición Objeto Frontal Del Manager está apagado.")]
    public Vector3 posicionLocalTutorialManual = new Vector3(0f, 0f, 1.4f);

    [Header("Punto propio del tutorial")]
    [Tooltip("Si está activo, el tutorial usa su propio punto local y NO usa el punto rosado de los objetos frontales.")]
    public bool usarPuntoPropioTutorial = true;

    [Tooltip("Posición local propia del tutorial respecto a la referencia manual. X lados, Y altura, Z frente.")]
    public Vector3 posicionLocalTutorialPropia = new Vector3(0f, 0.25f, 1.15f);

    [Header("Gizmo del tutorial")]
    public bool dibujarGizmoTutorialSiempre = true;
    public bool dibujarLineaDesdeReferenciaTutorial = true;
    public Color colorGizmoTutorial = new Color(0f, 1f, 0.25f, 1f);
    public float tamanoGizmoTutorial = 0.22f;

    [Tooltip("Rotación local adicional.")]
    public Vector3 rotacionLocalTutorialEuler = Vector3.zero;

    [Tooltip("Actívalo si el tutorial queda mirando al revés.")]
    public bool invertirFrenteTutorial = false;

    [Tooltip("Si está activo, el tutorial rota para mirar directamente al jugador.")]
    public bool hacerQueTutorialMireAlJugador = true;

    [Tooltip("Si está activo, solo rota en el eje Y.")]
    public bool soloRotacionYTutorial = true;

    [Tooltip("Activado = el tutorial permanece vertical y nunca hereda inclinacion hacia el piso desde el mando o la mini card.")]
    public bool mantenerTutorialVertical = true;

    [Tooltip("Cámara/cabeza del jugador. Recomendado: CenterEyeAnchor.")]
    public Transform cabezaJugador;

    [Header("Billboard / mirar al jugador")]
    public bool mirarJugadorConstantemente = true;
    public bool mirarSoloCuandoTutorialActivo = true;
    public float suavizadoMirarJugador = 10f;

    [Tooltip("Desplaza hacia arriba el punto al que mira el tutorial cuando está expandido.")]
    [Range(-0.25f, 0.25f)]
    public float offsetVerticalMiradaExpandido = -0.12f;

    [Tooltip("Compatibilidad antigua. Si está activo y NO está activo Usar Puntos Mirada Mientras Agarrado, al agarrar se mira usando Root Para Ubicar.")]
    public bool usarRootComoPuntoMiradaMientrasAgarrado = true;

    [Tooltip("Activado = incluso cuando agarras el tutorial, la mirada usa PuntoMiradaContraido o el punto original expandido. Esto corrige que al agarrar no mire desde el punto contraído.")]
    public bool usarPuntosMiradaMientrasAgarrado = true;

    [Header("Seguridad mirada / anti teletransporte")]
    [Tooltip("Activado = valida que PuntoMiradaContraido o PuntoMiradaExpandido estén cerca del root. Si el punto queda raro por layout, animación o pocket, usa Root Para Ubicar como respaldo.")]
    public bool validarPuntoMiradaAntesDeUsarlo = true;

    [Tooltip("Distancia máxima permitida entre Root Para Ubicar y el punto de mirada. Si se supera, se usa el root como respaldo para evitar giros o saltos raros.")]
    public float distanciaMaximaPuntoMiradaDesdeRoot = 1.25f;

    [Tooltip("Distancia mínima entre la cabeza y el punto de mirada. Si el punto queda encima o demasiado cerca de la cabeza, se usa el root como respaldo.")]
    public float distanciaMinimaPuntoMiradaACabeza = 0.22f;

    [Tooltip("Activado = cuando el tutorial se restaura desde el pocket/arco, la primera mirada se calcula desde Root Para Ubicar para no mover ni forzar el punto contraído antes de que el Canvas termine de estabilizarse.")]
    public bool usarRootComoPuntoMiradaAlRestaurarPocket = false;

    [Tooltip("Activado = calcula siempre la mirada desde el root del tutorial. Evita inclinaciones causadas por puntos hijos que cambian con animaciones o layouts.")]
    public bool usarRootComoPuntoMiradaEstableSiempre = false;

    [Header("Pocket / agarre seguro")]
    [Tooltip("Compatibilidad antigua. Déjalo apagado si quieres que el panel siga mirando al usuario mientras se agarra.")]
    public bool pausarMiradaMientrasAgarraPocket = false;

    [Tooltip("Activado = el tutorial sigue mirando al jugador mientras lo agarras, como los demás paneles.")]
    public bool mantenerMiradaMientrasAgarraPocket = true;

    [Tooltip("Activado = elimina la pelea con el GrabHandle: aunque el tutorial esté agarrado, este controlador sigue siendo el único dueño de la rotación para mirar al jugador.")]
    public bool forzarMiradaMientrasAgarraPocket = true;

    [Tooltip("Activado = cuando empieza el agarre, fuerza Mantener Mirada = true y Pausar Mirada = false para evitar que otro script deje el tutorial sin mirar.")]
    public bool asegurarMiradaAlIniciarAgarrePocket = true;

    [Tooltip("Mientras agarras el tutorial, se pausa cualquier animación de GrabHandleBottom2 para que la barra no se mueva debajo de la mano.")]
    public bool pausarBarraMientrasAgarraPocket = true;

    [Tooltip("Detecta el agarre real desde GrabHandleBottom2 aunque no conectes eventos manuales.")]
    public bool detectarAgarrePocketPorPolling = true;

    [Tooltip("GrabHandleBottom2 del tutorial. Si está vacío, se busca automáticamente desde Barra Inferior.")]
    public AlgoLabPanelGrabHandle grabHandleTutorialPocket;

    [Tooltip("Si el tutorial se desactiva al guardarse en el pocket, limpia el estado de agarre.")]
    public bool limpiarAgarrePocketAlDesactivar = true;

    private bool tutorialAgarradoPocket;

    [Header("Textos")]
    public TMP_Text tituloTutorialText;
    public TMP_Text instruccionesText;

    [Header("Video tutorial")]
    public RawImage videoRawImage;
    public VideoPlayer tutorialVideoPlayer;
    public RenderTexture tutorialRenderTexture;

    [Header("Salida segura de video")]
    [Tooltip("Activado = al detener un video, si el RawImage estaba mostrando el RenderTexture del video, se limpia. Dejelo apagado para no dejar la pantalla en blanco entre video e imagen.")]
    public bool limpiarRenderAlDetenerVideoActual = false;

    [Tooltip("Activado = DetenerVideoActual pausa el video para conservar el ultimo frame visible hasta que entre una imagen o un nuevo video.")]
    public bool pausarVideoAlDetenerParaConservarFrame = true;

    [Tooltip("Activado = cuando un video sin loop termina solo, vuelve a mostrar la ultima imagen estatica usada por CambiarImagen.")]
    public bool restaurarUltimaImagenEstaticaAlTerminarVideo = false;

    [Header("Audio")]
    public AudioSource audioSource;

    [Header("Botón silenciar")]
    public Button muteButton;
    public Image muteIconImage;
    public Sprite iconoSonidoActivo;
    public Sprite iconoSonidoSilenciado;
    public TMP_Text muteButtonText;
    public string textoSonidoActivo = "ON";
    public string textoSonidoSilenciado = "OFF";

    [Header("Mando tutorial")]
    public AlgoLabTutorialControllerAnimationPanel mandoController;

    [Header("Barra inferior adaptable")]
    [Tooltip("Asigna GrabHandleBottom2. Si está vacío, se intenta buscar automáticamente por nombre.")]
    public Transform barraInferior;

    [Tooltip("La barra cambia de posición cuando el panel del mando se muestra u oculta.")]
    public bool ajustarBarraSegunMando = true;

    [Tooltip("Al comenzar el tutorial, la barra usa el estado contraído.")]
    public bool barraContraidaAlIniciar = true;

    [Tooltip("Activa una transición suave entre el estado contraído y expandido.")]
    public bool usarSmoothBarra = true;

    public float duracionAjusteBarra = 0.3f;

    [Header("Barra contraída - mando oculto")]
    public Vector3 posicionLocalBarraContraida = new Vector3(-95f, -200f, 6f);
    public Vector3 rotacionLocalBarraContraida = new Vector3(0f, 0f, 90f);
    public Vector3 escalaLocalBarraContraida = new Vector3(20f, 110f, 20f);

    [Header("Barra expandida - mando visible")]
    public Vector3 posicionLocalBarraExpandida = new Vector3(0f, -200f, 6f);
    public Vector3 rotacionLocalBarraExpandida = new Vector3(0f, 0f, 90f);
    public Vector3 escalaLocalBarraExpandida = new Vector3(20f, 110f, 20f);

    [Header("Puntos de mirada del tutorial")]
    [Tooltip("Si está activo, el tutorial usa un punto de mirada distinto cuando el mando está contraído o expandido.")]
    public bool usarPuntosMiradaSegunMando = true;

    [Tooltip("Objeto vacío ubicado en el centro visual del panel cuando el mando/control está oculto o contraído.")]
    public Transform puntoMiradaContraido;

    [Tooltip("Objeto vacío ubicado en el centro visual del panel cuando el mando/control está visible o expandido. Si Usar Punto Original Panel Cuando Expandido está activo, este campo queda como respaldo.")]
    public Transform puntoMiradaExpandido;

    [Tooltip("Compatibilidad con escenas antiguas. El estado expandido usa siempre un punto ubicado en el centro visual del panel.")]
    public bool usarPuntoOriginalPanelCuandoExpandido = false;

    [Tooltip("Activado = cuando el panel está contraído, GrabHandleBottom2 toma la X local calculada desde PuntoMiradaContraido.")]
    public bool usarXDePuntoMiradaContraidoParaBarra = false;

    [Tooltip("Activado = al contraer, la barra conserva Y/Z de la posición expandida. Así queda X del PuntoMiradaContraido, Y -200 y Z 6.")]
    public bool mantenerYZDeBarraExpandidaAlContraer = true;

    [Tooltip("Activado = al sacar el tutorial del arco/pocket se reaplica el estado actual de la barra, contraído o expandido, sin mandar el tutorial al spawn inicial.")]
    public bool mantenerEstadoBarraAlRestaurarDesdePocket = true;

    [Tooltip("Límite de seguridad para la X local del GrabHandleBottom2 cuando está contraído. Evita que un cálculo raro del PuntoMiradaContraido mande la barra muy lejos y luego el grab mueva todo el tutorial.")]
    public float limiteAbsXBarraContraida = 350f;

    [Tooltip("Si la X calculada desde PuntoMiradaContraido sale inválida o muy grande, usa Posicion Local Barra Contraida como respaldo.")]
    public bool usarPosicionContraidaComoRespaldoSiXFalla = true;

    [Tooltip("Dibuja gizmos para ver los dos puntos de mirada.")]
    public bool dibujarGizmosPuntosMirada = true;

    public Color colorGizmoMiradaContraida = new Color(1f, 0.35f, 0f, 1f);
    public Color colorGizmoMiradaExpandida = new Color(0f, 0.75f, 1f, 1f);
    public float tamanoGizmoPuntoMirada = 0.12f;


    [Header("Indicadores amarillos de gatillos")]
    public bool usarIndicadoresGatillos = true;

    [Tooltip("Círculo amarillo ubicado encima del gatillo principal.")]
    public GameObject circuloGatilloPrincipal;

    [Tooltip("Círculo amarillo ubicado encima del gatillo secundario.")]
    public GameObject circuloGatilloSecundario;

    [Tooltip("Cuántos segundos antes del evento de presionar aparece el círculo.")]
    public float segundosAntesDePresionarGatillo = 1f;

    [Tooltip("Cuántos segundos después del evento de soltar desaparece el círculo.")]
    public float segundosDespuesDeSoltarGatillo = 1f;

    [Tooltip("Duración del fade cuando aparece el círculo. No cambia escala.")]
    public float duracionAparecerCirculo = 0.25f;

    [Tooltip("Duración del fade cuando desaparece el círculo. No cambia escala.")]
    public float duracionDesaparecerCirculo = 0.25f;

    [Tooltip("Si está activo, se usa transparencia con CanvasGroup.")]
    public bool usarAlphaCirculos = true;

    [Tooltip("Si está activo, apaga los círculos al iniciar el tutorial.")]
    public bool ocultarCirculosAlIniciarTutorial = true;

    [Tooltip("Si está activo, asegura que la escala del círculo quede siempre como estaba en el editor.")]
    public bool mantenerEscalaOriginalCirculos = true;

    [Header("Tutorial interactivo")]
    public bool permitirTutorialInteractivo = true;

    [Tooltip("Texto que aparece mientras se espera una acción interactiva si el evento no tiene texto.")]
    public string textoEsperandoAccion = "Ahora intenta realizar la acción.";

    [Tooltip("Si está activo, al completar la acción el tutorial continúa automáticamente.")]
    public bool continuarAutomaticamenteAlCompletarAccion = true;

    [Tooltip("Pequeño avance de tiempo para que el siguiente evento se ejecute después de completar la acción.")]
    public float avanceTiempoAlCompletarAccion = 0.05f;

    [Header("Video al completar interacción")]
    [Tooltip("Si está activo, cuando el usuario complete la acción interactiva, el tutorial esperará a que el video actual termine su vuelta completa antes de continuar.")]
    public bool esperarFinVideoActualAlCompletarAccion = true;

    [Tooltip("Si el video actual está en bucle, al completar la interacción se desactiva el loop para dejarlo terminar una sola vez.")]
    public bool quitarLoopVideoActualAlCompletarAccion = true;

    [Tooltip("Al terminar de esperar el video actual, se detiene el VideoPlayer antes de reproducir el siguiente video.")]
    public bool detenerVideoActualDespuesDeEsperar = true;

    [Tooltip("Oculta los círculos de gatillos cuando se completa la interacción del usuario.")]
    public bool ocultarIndicadoresAlCompletarInteraccion = true;

    [Tooltip("Al completar la accion interactiva de seleccionar un boton, corta el video de seleccion para que la siguiente imagen estatica pueda mostrarse.")]
    public bool detenerVideoAlCompletarSeleccionBotonNoIniciar = true;

    [Tooltip("Tiempo máximo esperando a que un video empiece a reproducirse antes de continuar. Útil cuando el video se está preparando.")]
    public float tiempoMaximoEsperarInicioVideo = 2f;

    [Header("Seguridad anti bloqueo")]
    [Tooltip("Activado = agrega salidas de seguridad para que el tutorial no quede detenido por videos, eventos o estados visuales incompletos.")]
    public bool usarProteccionAntiBloqueoTutorial = true;

    [Tooltip("Tiempo maximo esperando a que un VideoPlayer termine de prepararse antes de continuar sin bloquear el tutorial.")]
    public float tiempoMaximoPrepararVideo = 4f;

    [Tooltip("Tiempo minimo de seguridad para esperar el final de un video. Si el clip dura mas, se usa la duracion del clip mas el margen.")]
    public float tiempoMaximoEsperaFinVideo = 45f;

    [Tooltip("Margen extra sobre la duracion real del video antes de cortar una espera que parece trabada.")]
    public float margenExtraEsperaFinVideo = 3f;

    [Tooltip("Si una accion interactiva queda esperando demasiado tiempo, el tutorial la completa para no quedarse detenido.")]
    public bool completarAccionInteractivaSiExcedeTiempo = true;

    [Tooltip("Tiempo maximo real esperando una accion interactiva antes de completar por seguridad. 0 = desactivado.")]
    public float tiempoMaximoEsperaAccionInteractiva = 120f;

    [Tooltip("Si el tutorial esta activo pero su panel visual quedo apagado por algun estado raro, lo reactiva.")]
    public bool reactivarPanelSiTutorialActivo = true;

    [Header("Interacción con botones UI")]
    [Tooltip("Si el usuario presiona un botón llamado Iniciar durante el tutorial, el tutorial se oculta para dejar comenzar el nivel.")]
    public bool cerrarTutorialSiPresionaBotonIniciar = true;

    [Tooltip("Si el usuario está en una espera interactiva de selección y presiona cualquier botón que NO sea Iniciar, se completa la interacción.")]
    public bool completarSeleccionSiPresionaBotonNoIniciar = true;

    [Tooltip("Texto opcional que aparece cuando el usuario selecciona correctamente un botón que no sea Iniciar.")]
    public string textoFelicitacionBotonNoIniciar = "¡Muy bien! Ya sabes seleccionar con el gatillo principal.";

    [Tooltip("Palabras usadas para detectar botones de inicio. Se revisa el nombre del objeto y el texto del botón.")]
    public string[] palabrasBotonIniciar = new string[]
    {
        "iniciar",
        "start",
        "comenzar",
        "empezar"
    };

    [Tooltip("Evento opcional cuando se presiona Iniciar mientras el tutorial está activo.")]
    public UnityEvent alPresionarBotonIniciarDuranteTutorial;

    [Header("Imagenes de paneles principales")]
    [Tooltip("Imagen u objeto visual que representa el panel principal. Opcional.")]
    public GameObject imagenPanelPrincipal;

    [Tooltip("Imagen u objeto visual que representa el panel de diagramas.")]
    public GameObject imagenPanelDiagramas;

    [Tooltip("Imagen u objeto visual que representa el panel de inteligencia artificial.")]
    public GameObject imagenPanelIA;

    [Tooltip("Si esta activo, al iniciar el tutorial se ocultan las imagenes de paneles para que solo se vea el tutorial.")]
    public bool ocultarImagenesPanelesAlIniciarTutorial = true;

    [Tooltip("Si esta activo, la imagen del panel principal aparece desde el inicio del tutorial. Si quieres que solo se vea el tutorial al principio, dejalo apagado.")]
    public bool mostrarImagenPanelPrincipalAlIniciarTutorial = false;

    [Tooltip("Si esta activo, al cerrar, omitir o finalizar el tutorial se apagan las imagenes de paneles.")]
    public bool ocultarImagenesPanelesAlCerrarTutorial = true;

    [Tooltip("Activado = al mostrar una imagen estatica de panel, detiene el video anterior para que no vuelva a aparecer encima.")]
    public bool detenerVideoAlMostrarImagenPanel = true;

    [Tooltip("Activado = al reproducir un video nuevo, oculta las imagenes estaticas de panel para evitar superposiciones.")]
    public bool ocultarImagenesPanelesAlReproducirVideo = true;

    [Header("Apertura y cierre del panel")]
    public bool ocultarAlIniciar = true;
    public float duracionMostrarPanel = 0.35f;
    public float duracionCerrarPanel = 0.35f;
    public float escalaInicialPanel = 0.05f;
    public Vector3 escalaVisiblePanel = Vector3.one;

    [Header("Ocultar completamente al inicio")]
    [Tooltip("Si está activo, el TutorialPanelRoot se desactiva completamente cuando el tutorial está oculto. Esto evita que se vea pequeño al iniciar la app.")]
    public bool desactivarGameObjectCuandoEstaOculto = true;

    [Tooltip("Si está activo, al cerrar el tutorial se espera la animación de cierre y luego se desactiva el objeto completo.")]
    public bool desactivarDespuesDeCerrarPanel = true;

    [Tooltip("Si algún padre del tutorial está desactivado, lo vuelve a activar cuando se llama IniciarTutorial().")]
    public bool activarPadresAlIniciarTutorial = true;

    [Header("Revelar video")]
    public float duracionRevelarVideo = 0.45f;
    public float margenSalidaIntro = 30f;

    [Header("Salida del título al revelar")]
    [Tooltip("El título sale completamente del panel cuando comienza el video.")]
    public bool ocultarTituloAlRevelar = true;

    [Tooltip("Al terminar la transición, el objeto del título se desactiva.")]
    public bool desactivarTituloAlTerminarRevelado = true;

    [Tooltip("Calcula automáticamente una posición fuera del panel.")]
    public bool calcularSalidaTituloAutomaticamente = true;

    [Tooltip("Solo se usa si el cálculo automático está apagado.")]
    public float tituloYFuera = 450f;

    public float margenSalidaTitulo = 80f;

    [HideInInspector]
    public float tituloYArriba = 135f;

    [Header("Timeline")]
    [Tooltip("IMPORTANTE: este campo ya no inicia el tutorial al arrancar. El tutorial solo inicia cuando otro script llama IniciarTutorial().")]
    public bool iniciarAutomaticamente = false;

    public string nombreTutorial = "Tutorial: Controles";
    public List<EventoTutorial> eventos = new List<EventoTutorial>();

    [Header("Omitir con doble A")]
    public bool permitirOmitirConDobleA = true;

    [Tooltip("Tiempo maximo entre la primera y segunda pulsacion de A para omitir el tutorial.")]
    public float tiempoMaximoDobleA = 3f;

    [Tooltip("Permite omitir el tutorial con el boton A del control Meta Quest.")]
    public bool permitirBotonAOVR = true;

    [Tooltip("Boton del control usado para omitir el tutorial. En Quest normalmente es A.")]
    public OVRInput.Button botonOmitirTutorialOVR = OVRInput.Button.One;

    [Tooltip("Para pruebas en PC. Con la tecla A puedes simular el botón A.")]
    public bool permitirTeclaAEnEditor = true;

    [Header("Final")]
    public bool iniciarPracticaAlFinalizar = false;
    public UnityEvent alFinalizarTutorial;
    public UnityEvent alOmitirTutorial;
    public UnityEvent alIniciarPractica;
    public UnityEvent alContinuarAplicacion;

    [Header("Panel de opciones")]
    public AlgoLabPanelPocketManager panelOpcionesManager;
    public bool autoBuscarPanelOpcionesManager = true;
    public bool habilitarPanelOpcionesAlOmitirTutorial = true;
    public bool habilitarPanelOpcionesAlFinalizarTutorial = true;
    public bool guardarTutorialEnPanelOpcionesAlOmitir = true;
    public bool omitirTutorialAlGuardarEnPanelOpciones = true;
    public bool repetirTutorialAlSacarTutorialOmitidoDesdePanelOpciones = false;
    [Tooltip("Activado = cada vez que el tutorial sale del panel de opciones, conserva la posicion soltada pero reinicia desde el primer evento.")]
    public bool reiniciarTutorialSiempreAlSacarDelPanelOpciones = true;
    public UnityEvent alSacarTutorialOmitidoDesdePanelOpciones;
    public AudioClip audioAlSacarTutorialOmitidoDesdePanelOpciones;
    [TextArea(1, 3)]
    public string textoAlSacarTutorialOmitidoDesdePanelOpciones = "";
    public bool reemplazarAudioElemento2PorAudio21AlVolverDesdePanelOpciones = true;
    public int indiceEventoAudioReemplazablePanelOpciones = 2;
    public AudioClip audio21VolverIntentarlo;
    public bool mantenerMiradaAlSalirDelPanelOpciones = true;

    [Tooltip("Tiempo que un objeto/panel activado o desactivado por eventos del tutorial queda protegido para que el panel de opciones no lo capture automaticamente.")]
    public float tiempoBloquearAutoRegistroObjetoTutorial = 60f;

    [Header("Salida segura del tutorial")]
    [Tooltip("Si esta activo, al omitir/finalizar el tutorial se busca automaticamente el GameAccessController para activar paneles aunque falte algun UnityEvent del inspector.")]
    public bool activarPanelesDespuesDelTutorialPorCodigo = true;

    [Tooltip("Si esta vacio, se busca automaticamente en la escena.")]
    public AlgoLabGameAccessController gameAccessController;

    [Tooltip("Si esta activo, despues de omitir/finalizar se vuelve a habilitar el panel de opciones al final del flujo, despues de guardar/cerrar el tutorial.")]
    public bool asegurarPanelOpcionesAlSalirDelTutorial = true;

    [Header("Debug")]
    public bool mostrarDebug = true;

    private bool tutorialActivo;
    private bool tutorialFinalizado;
    private bool estaSilenciado;
    private Texture ultimaImagenEstaticaTutorial;
    private float tiempoTutorial;
    private float ultimoTiempoBotonA = -999f;

    private bool esperandoAccionInteractiva;
    private bool esperandoFinVideoActual;
    private AccionTutorialInteractiva accionInteractivaActual = AccionTutorialInteractiva.Ninguna;
    private EventoTutorial eventoInteractivoActual;

    private bool accionPanelAgarrado;
    private bool accionPanelMovido;
    private bool accionPanelSoltado;
    private bool accionPanelMetidoEnArco;
    private bool accionPanelSacadoDelArco;
    private bool secuenciaPanelMeterYSacarDelArco;
    private bool secuenciaPanelSacarYMeterDelArco;

    private bool tutorialGuardadoEnPanelOpciones;
    private bool tutorialOmitidoDesdePanelOpciones;
    private bool avisoTutorialOmitidoDesdePanelOpcionesConsumido;
    private bool tutorialVistoAlMenosUnaVez;
    private bool tutorialOmitidoAlMenosUnaVez;
    private bool reemplazarProximoAudioElemento2PorAudio21;

    private bool cerrandoTutorialPorBotonIniciar;

    private Vector3 escalaOriginalPanel;
    private Vector2 posicionInicialIntroBlackPanel;
    private Vector2 posicionInicialTitulo;
    private Vector2 posicionTituloArriba;

    private Coroutine rutinaPanel;
    private Coroutine rutinaRevelarVideo;
    private Coroutine rutinaPrepararVideo;
    private Coroutine rutinaVideoDuracion;
    private Coroutine rutinaRestaurarImagenAlTerminarVideo;
    private Coroutine rutinaBarraInferior;
    private Coroutine rutinaReaplicarBarraSiguienteFrame;
    private Coroutine rutinaReiniciarDesdePanelOpciones;
    private Coroutine rutinaEstabilizarPoseRestauradaPocket;
    private Coroutine rutinaAsegurarUbicacionInicial;
    private bool barraPendienteAplicarCuandoSuelte;
    private Coroutine rutinaEsperarFinVideoActual;
    private float tiempoRealInicioEsperaAccion = -999f;
    private float tiempoRealInicioEsperaVideo = -999f;
    private int generacionVideo;
    private int generacionEsperaVideo;
    private bool reanudarAudioAlActivar;
    private bool reanudarVideoAlActivar;
    private bool reanudarEsperaVideoAlActivar;

    private bool estadosInicialesCapturados;
    private bool barraExpandidaActual;
    private bool tutorialYaFueUbicadoInicialmente;
    private bool tutorialPasoPorPocket;
    private bool restaurandoDesdePocket;
    private bool tutorialVisibleRestauradoDesdePocket;

    private Coroutine rutinaCirculoPrincipal;
    private Coroutine rutinaCirculoSecundario;
    private Coroutine rutinaOcultarCirculoPrincipalDespues;
    private Coroutine rutinaOcultarCirculoSecundarioDespues;

    private CanvasGroup canvasGroupCirculoPrincipal;
    private CanvasGroup canvasGroupCirculoSecundario;

    private Vector3 escalaOriginalCirculoPrincipal = Vector3.one;
    private Vector3 escalaOriginalCirculoSecundario = Vector3.one;

    public bool TutorialEnCurso => tutorialActivo && !tutorialFinalizado;
    public bool TutorialVistoAlMenosUnaVez => tutorialVistoAlMenosUnaVez;
    public bool TutorialOmitidoAlMenosUnaVez => tutorialOmitidoAlMenosUnaVez;
    public bool TutorialPrincipalCompletadoUOmitido =>
        tutorialVistoAlMenosUnaVez || tutorialOmitidoAlMenosUnaVez;

    private void Awake()
    {
        PrepararReferencias();
        PrepararIndicadoresGatillos();
        PrepararBarraInferior();
        PrepararPuntosMiradaTutorial();
        CapturarEstadosIniciales();

        if (muteButton != null)
        {
            muteButton.onClick.RemoveListener(AlternarSilencioTutorial);
            muteButton.onClick.AddListener(AlternarSilencioTutorial);
        }

        if (ocultarAlIniciar)
        {
            OcultarPanelInstantaneo();
        }
    }

    private void Start()
    {
        PrepararReferencias();
        PrepararIndicadoresGatillos();
        PrepararBarraInferior();
        PrepararPuntosMiradaTutorial();
        PrepararGrabHandleTutorialPocket();
        CapturarEstadosIniciales();
        ActualizarIconoMute();

        AplicarEstadoInicialBarra();

        if (ocultarAlIniciar && !tutorialActivo)
        {
            OcultarPanelInstantaneo();
        }
    }

    private void OnDisable()
    {
        bool conservarReproduccion =
            tutorialActivo &&
            !tutorialFinalizado &&
            tutorialGuardadoEnPanelOpciones;

        reanudarAudioAlActivar =
            conservarReproduccion &&
            audioSource != null &&
            audioSource.isPlaying;

        reanudarVideoAlActivar =
            conservarReproduccion &&
            tutorialVideoPlayer != null &&
            tutorialVideoPlayer.isPlaying;

        reanudarEsperaVideoAlActivar =
            conservarReproduccion &&
            esperandoFinVideoActual;

        if (reanudarAudioAlActivar)
        {
            audioSource.Pause();
        }

        if (reanudarVideoAlActivar)
        {
            tutorialVideoPlayer.Pause();
        }

        bool liberarEsperaVideo = esperandoFinVideoActual && !reanudarEsperaVideoAlActivar;

        StopAllCoroutines();
        LimpiarReferenciasRutinasDetenidas();

        generacionVideo++;
        generacionEsperaVideo++;
        esperandoFinVideoActual = false;
        tiempoRealInicioEsperaVideo = -999f;

        if (liberarEsperaVideo)
        {
            AvanzarTimelineDespuesDeEspera();
        }

        if (limpiarAgarrePocketAlDesactivar)
        {
            tutorialAgarradoPocket = false;
        }
    }

    private void OnDestroy()
    {
        if (muteButton != null)
            muteButton.onClick.RemoveListener(AlternarSilencioTutorial);
    }

    private void OnEnable()
    {
        if (!tutorialActivo || tutorialFinalizado)
        {
            reanudarAudioAlActivar = false;
            reanudarVideoAlActivar = false;
            reanudarEsperaVideoAlActivar = false;
            return;
        }

        if (reanudarAudioAlActivar && audioSource != null && audioSource.clip != null)
        {
            audioSource.UnPause();
        }

        if (reanudarVideoAlActivar &&
            tutorialVideoPlayer != null &&
            tutorialVideoPlayer.clip != null)
        {
            int videoActual = IniciarNuevaGeneracionVideo();

            if (tutorialVideoPlayer.isPrepared)
            {
                MostrarRenderVideoEnRawImage();
                tutorialVideoPlayer.Play();
            }
            else
            {
                VideoClip clip = tutorialVideoPlayer.clip;
                tutorialVideoPlayer.Prepare();
                rutinaPrepararVideo = StartCoroutine(
                    ReproducirVideoPreparadoRutina(clip, 0f, videoActual)
                );
            }
        }

        if (reanudarEsperaVideoAlActivar)
        {
            IniciarRutinaEsperaFinVideoActual(true);
        }

        reanudarAudioAlActivar = false;
        reanudarVideoAlActivar = false;
        reanudarEsperaVideoAlActivar = false;
    }

    private void LimpiarReferenciasRutinasDetenidas()
    {
        rutinaPanel = null;
        rutinaRevelarVideo = null;
        rutinaPrepararVideo = null;
        rutinaVideoDuracion = null;
        rutinaRestaurarImagenAlTerminarVideo = null;
        rutinaBarraInferior = null;
        rutinaReaplicarBarraSiguienteFrame = null;
        rutinaEstabilizarPoseRestauradaPocket = null;
        rutinaEsperarFinVideoActual = null;
        rutinaCirculoPrincipal = null;
        rutinaCirculoSecundario = null;
        rutinaOcultarCirculoPrincipalDespues = null;
        rutinaOcultarCirculoSecundarioDespues = null;
    }

    private void Update()
    {
        if (!tutorialActivo || tutorialFinalizado)
        {
            return;
        }

        AsegurarTutorialActivoVisibleSiCorresponde();
        RevisarProteccionAntiBloqueoTutorial();

        if (!esperandoAccionInteractiva && !esperandoFinVideoActual)
        {
            tiempoTutorial += Time.unscaledDeltaTime;

            RevisarIndicadoresGatillosAntesDeTiempo();

            EjecutarEventosPendientes();
        }

#if UNITY_EDITOR
        if (permitirTeclaAEnEditor && Input.GetKeyDown(KeyCode.A))
        {
            RegistrarBotonA();
        }
#endif

        if (permitirBotonAOVR && OVRInput.GetDown(botonOmitirTutorialOVR))
        {
            RegistrarBotonA();
        }
    }

    private void AsegurarTutorialActivoVisibleSiCorresponde()
    {
        if (!usarProteccionAntiBloqueoTutorial || !reactivarPanelSiTutorialActivo)
        {
            return;
        }

        if (tutorialGuardadoEnPanelOpciones)
        {
            return;
        }

        if (panelRoot != null && !panelRoot.gameObject.activeSelf)
        {
            panelRoot.gameObject.SetActive(true);
        }

        if (panelRoot != null &&
            rutinaPanel == null &&
            panelRoot.localScale.sqrMagnitude < 0.0001f)
        {
            panelRoot.localScale = escalaVisiblePanel;
        }
    }

    private void RevisarProteccionAntiBloqueoTutorial()
    {
        if (!usarProteccionAntiBloqueoTutorial)
        {
            return;
        }

        if (esperandoAccionInteractiva &&
            completarAccionInteractivaSiExcedeTiempo &&
            tiempoMaximoEsperaAccionInteractiva > 0f &&
            Time.unscaledTime - tiempoRealInicioEsperaAccion >= tiempoMaximoEsperaAccionInteractiva)
        {
            DebugLog("TUTORIAL: accion interactiva completada por seguridad para evitar bloqueo.");
            CompletarAccionInteractiva();
            return;
        }

        if (esperandoFinVideoActual &&
            rutinaEsperarFinVideoActual == null &&
            Time.unscaledTime - tiempoRealInicioEsperaVideo >= Mathf.Max(1f, tiempoMaximoEsperarInicioVideo))
        {
            DebugLog("TUTORIAL: espera de video liberada por seguridad.");
            esperandoFinVideoActual = false;
            tiempoRealInicioEsperaVideo = -999f;
            AvanzarTimelineDespuesDeEspera();
        }
    }

    private void LateUpdate()
    {
        ActualizarEstadoAgarrePocketPorPolling();

        if (!mirarJugadorConstantemente)
        {
            return;
        }

        bool puedeMirarPorRestauracionPocket =
            mantenerMiradaAlSalirDelPanelOpciones &&
            tutorialVisibleRestauradoDesdePocket;

        if (mirarSoloCuandoTutorialActivo &&
            !tutorialActivo &&
            !tutorialAgarradoPocket &&
            !puedeMirarPorRestauracionPocket)
        {
            return;
        }

        if (DebePausarMiradaPorAgarrePocket())
        {
            return;
        }

        RotarTutorialHaciaJugadorSuave();
    }

    private void PrepararReferencias()
    {
        if (panelRoot == null)
        {
            panelRoot = GetComponent<RectTransform>();
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();

            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        if (tutorialVideoPlayer != null && tutorialRenderTexture != null)
        {
            tutorialVideoPlayer.renderMode = VideoRenderMode.RenderTexture;
            tutorialVideoPlayer.targetTexture = tutorialRenderTexture;
            tutorialVideoPlayer.playOnAwake = false;
            tutorialVideoPlayer.audioOutputMode = VideoAudioOutputMode.None;
        }

        if (videoRawImage != null &&
            tutorialRenderTexture != null &&
            videoRawImage.texture == null &&
            tutorialVideoPlayer != null &&
            tutorialVideoPlayer.isPlaying)
        {
            videoRawImage.texture = tutorialRenderTexture;
        }

        if (tituloTutorialText != null && !string.IsNullOrWhiteSpace(nombreTutorial))
        {
            tituloTutorialText.text = nombreTutorial;
        }

        if (spawnManager == null)
        {
            spawnManager = AlgoLabManualPanelSpawnManager.Instance;
        }

        if (spawnManager == null)
        {
            spawnManager = FindFirstObjectByType<AlgoLabManualPanelSpawnManager>(
                FindObjectsInactive.Include
            );
        }

        BuscarPanelOpcionesManagerSiCorresponde();
#if UNITY_EDITOR
        AutoAsignarAudio21VolverIntentarloEditor();
#endif

        if (rootParaUbicar == null)
        {
            rootParaUbicar = panelRoot != null ? panelRoot.transform : transform;
        }

        if (cabezaJugador == null && Camera.main != null)
        {
            cabezaJugador = Camera.main.transform;
        }

        PrepararPuntosMiradaTutorial();
        PrepararGrabHandleTutorialPocket();
    }

    private void PrepararIndicadoresGatillos()
    {
        PrepararIndicador(
            circuloGatilloPrincipal,
            ref canvasGroupCirculoPrincipal,
            ref escalaOriginalCirculoPrincipal
        );

        PrepararIndicador(
            circuloGatilloSecundario,
            ref canvasGroupCirculoSecundario,
            ref escalaOriginalCirculoSecundario
        );
    }

    private void PrepararIndicador(
        GameObject circulo,
        ref CanvasGroup canvasGroup,
        ref Vector3 escalaOriginal
    )
    {
        if (circulo == null)
        {
            return;
        }

        escalaOriginal = circulo.transform.localScale;

        if (escalaOriginal.sqrMagnitude <= 0.0001f)
        {
            escalaOriginal = Vector3.one;
        }

        if (mantenerEscalaOriginalCirculos)
        {
            circulo.transform.localScale = escalaOriginal;
        }

        if (usarAlphaCirculos)
        {
            canvasGroup = circulo.GetComponent<CanvasGroup>();

            if (canvasGroup == null)
            {
                canvasGroup = circulo.AddComponent<CanvasGroup>();
            }

            canvasGroup.alpha = circulo.activeSelf ? 1f : 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        Graphic[] graficos = circulo.GetComponentsInChildren<Graphic>(true);

        for (int i = 0; i < graficos.Length; i++)
        {
            if (graficos[i] != null)
            {
                graficos[i].raycastTarget = false;
            }
        }
    }

    private void CapturarEstadosIniciales()
    {
        if (estadosInicialesCapturados)
        {
            return;
        }

        if (panelRoot != null)
        {
            escalaOriginalPanel = panelRoot.localScale;

            if (escalaOriginalPanel.sqrMagnitude <= 0.0001f)
            {
                escalaOriginalPanel = escalaVisiblePanel;
            }
        }

        if (introBlackPanel != null)
        {
            posicionInicialIntroBlackPanel = introBlackPanel.anchoredPosition;
        }

        if (tituloTutorialRect != null)
        {
            posicionInicialTitulo = tituloTutorialRect.anchoredPosition;
            posicionTituloArriba = new Vector2(posicionInicialTitulo.x, tituloYArriba);
        }

        estadosInicialesCapturados = true;
    }

    private void PrepararBarraInferior()
    {
        if (barraInferior != null)
        {
            return;
        }

        Transform[] hijos = GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < hijos.Length; i++)
        {
            if (hijos[i] != null && hijos[i].name == "GrabHandleBottom2")
            {
                barraInferior = hijos[i];
                break;
            }
        }
    }

    private void PrepararPuntosMiradaTutorial()
    {
        if (puntoMiradaContraido == null)
        {
            puntoMiradaContraido = BuscarHijoPorNombre(transform, "PuntoMiradaContraido");
        }

        if (puntoMiradaExpandido == null)
        {
            puntoMiradaExpandido = BuscarHijoPorNombre(transform, "PuntoMiradaExpandido");
        }

        if (puntoMiradaExpandido == null)
        {
            puntoMiradaExpandido = puntoMiradaContraido;
        }
    }

    private Transform BuscarHijoPorNombre(Transform raiz, string nombre)
    {
        if (raiz == null || string.IsNullOrWhiteSpace(nombre))
        {
            return null;
        }

        Transform[] hijos = raiz.GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < hijos.Length; i++)
        {
            if (hijos[i] != null && hijos[i].name == nombre)
            {
                return hijos[i];
            }
        }

        return null;
    }

    private void PrepararGrabHandleTutorialPocket()
    {
        if (grabHandleTutorialPocket == null)
        {
            PrepararBarraInferior();

            if (barraInferior == null)
            {
                return;
            }

            grabHandleTutorialPocket = barraInferior.GetComponent<AlgoLabPanelGrabHandle>();

            if (grabHandleTutorialPocket == null)
            {
                grabHandleTutorialPocket = barraInferior.GetComponentInChildren<AlgoLabPanelGrabHandle>(true);
            }

            if (grabHandleTutorialPocket == null)
            {
                grabHandleTutorialPocket = barraInferior.GetComponentInParent<AlgoLabPanelGrabHandle>();
            }
        }

        if (grabHandleTutorialPocket != null)
        {
            if (rootParaUbicar != null && grabHandleTutorialPocket.transform.IsChildOf(rootParaUbicar))
            {
                grabHandleTutorialPocket.panelRoot = rootParaUbicar;
            }

            grabHandleTutorialPocket.tutorialController = this;
            grabHandleTutorialPocket.permitirControladores = true;
            grabHandleTutorialPocket.usarPuntoExactoDeAgarre = true;
            grabHandleTutorialPocket.movimientoDirecto = true;
            grabHandleTutorialPocket.usarMovimientoAncladoTutorial = false;
            grabHandleTutorialPocket.reanclarTutorialDespuesDeMirada = false;
        }
    }

    private void ActualizarEstadoAgarrePocketPorPolling()
    {
        if (!detectarAgarrePocketPorPolling)
        {
            return;
        }

        PrepararGrabHandleTutorialPocket();

        if (grabHandleTutorialPocket == null)
        {
            return;
        }

        tutorialAgarradoPocket = grabHandleTutorialPocket.EstaAgarrando;
    }

    private bool DebePausarMiradaPorAgarrePocket()
    {
        if (!tutorialAgarradoPocket)
        {
            return false;
        }

        // FIX FINAL:
        // El GrabHandle SOLO mueve la posición del root.
        // La rotación para mirar al jugador pertenece a este TutorialPanelController.
        // Por eso, durante el agarre no se debe pausar la mirada.
        if (forzarMiradaMientrasAgarraPocket || mantenerMiradaMientrasAgarraPocket)
        {
            return false;
        }

        return pausarMiradaMientrasAgarraPocket;
    }

    private void AsegurarMiradaMientrasAgarraPocket()
    {
        if (!asegurarMiradaAlIniciarAgarrePocket)
        {
            return;
        }

        // No dejamos que ningún GrabHandle vuelva a poner estos valores al revés.
        mantenerMiradaMientrasAgarraPocket = true;
        pausarMiradaMientrasAgarraPocket = false;
        forzarMiradaMientrasAgarraPocket = true;
        usarRootComoPuntoMiradaEstableSiempre = false;
        usarPuntosMiradaMientrasAgarrado = true;
    }

    private void DetenerAnimacionBarraPorAgarrePocket()
    {
        if (!pausarBarraMientrasAgarraPocket)
        {
            return;
        }

        if (rutinaBarraInferior != null)
        {
            StopCoroutine(rutinaBarraInferior);
            rutinaBarraInferior = null;
        }
    }

    public void SincronizarPivoteConEstadoVisualAntesDeAgarre()
    {
        PrepararReferencias();
        PrepararBarraInferior();
        PrepararPuntosMiradaTutorial();

        if (mandoController != null)
        {
            barraExpandidaActual = mandoController.EstaExpandidoVisualmente();
        }

        if (rutinaBarraInferior != null)
        {
            StopCoroutine(rutinaBarraInferior);
            rutinaBarraInferior = null;
        }

        // No cambiamos la pose del handle antes de que el GrabHandle capture el
        // punto de agarre. Si habia una transicion en curso, el estado definitivo
        // se aplica al soltar sin mover el panel debajo de la mano.
        barraPendienteAplicarCuandoSuelte = true;
    }

    private void AplicarEstadoInicialBarra()
    {
        if (!ajustarBarraSegunMando)
        {
            return;
        }

        AjustarBarraInferior(!barraContraidaAlIniciar, true);
    }

    private void AplicarEstadoInicialBarraAlIniciarTutorial()
    {
        if (!ajustarBarraSegunMando)
        {
            return;
        }

        // Si el tutorial ya salió del pocket/arco,
        // no forzamos contraído/expandido otra vez. Conservamos el estado actual.
        if (ubicarTutorialSoloLaPrimeraVez && tutorialPasoPorPocket && noReubicarDespuesDeSalirDelPocket)
        {
            AplicarBarraInferiorInstantanea(barraExpandidaActual);
            return;
        }

        AplicarEstadoInicialBarra();
    }

    [ContextMenu("Barra - Vista contraída")]
    public void MostrarBarraContraidaEnEditor()
    {
        PrepararBarraInferior();
        AjustarBarraInferior(false, true);
    }

    [ContextMenu("Barra - Vista expandida")]
    public void MostrarBarraExpandidaEnEditor()
    {
        PrepararBarraInferior();
        AjustarBarraInferior(true, true);
    }

    public void AjustarBarraInferior(bool expandida, bool instantaneo = false)
    {
        if (!ajustarBarraSegunMando)
        {
            return;
        }

        // IMPORTANTE:
        // Guardamos el estado primero, incluso si el usuario tiene agarrado el tutorial.
        // Antes, si esta función se llamaba mientras estaba agarrado, salía antes de actualizar
        // barraExpandidaActual; por eso al soltar o sacar del pocket la barra podía quedar en
        // la posición del estado anterior.
        barraExpandidaActual = expandida;

        PrepararBarraInferior();

        if (barraInferior == null)
        {
            DebugLog("No se encontró GrabHandleBottom2 para ajustar la barra.");
            return;
        }

        ActualizarEstadoAgarrePocketPorPolling();

        if (pausarBarraMientrasAgarraPocket && tutorialAgarradoPocket)
        {
            barraPendienteAplicarCuandoSuelte = true;
            DebugLog("TUTORIAL: ajuste de barra guardado para aplicar al soltar. Estado expandido=" + barraExpandidaActual);
            return;
        }

        barraPendienteAplicarCuandoSuelte = false;

        if (Application.isPlaying && tutorialActivo && hacerQueTutorialMireAlJugador)
        {
            RotarTutorialHaciaJugador();
        }

        if (rutinaBarraInferior != null)
        {
            StopCoroutine(rutinaBarraInferior);
            rutinaBarraInferior = null;
        }

        if (instantaneo || !usarSmoothBarra || !Application.isPlaying)
        {
            AplicarBarraInferiorInstantanea(expandida);
            ReaplicarEstadoVisualTutorialSiguienteFrame();
            return;
        }

        rutinaBarraInferior = StartCoroutine(
            AjustarBarraInferiorRutina(expandida)
        );
    }

    private void AplicarBarraInferiorInstantanea(bool expandida)
    {
        barraExpandidaActual = expandida;
        PrepararBarraInferior();
        PrepararPuntosMiradaTutorial();

        if (barraInferior == null)
        {
            return;
        }

        barraInferior.localPosition = ObtenerPosicionLocalBarraPorEstado(expandida);

        barraInferior.localRotation = Quaternion.Euler(
            expandida
                ? rotacionLocalBarraExpandida
                : rotacionLocalBarraContraida
        );

        barraInferior.localScale = expandida
            ? escalaLocalBarraExpandida
            : escalaLocalBarraContraida;
    }

    private Vector3 ObtenerPosicionLocalBarraPorEstado(bool expandida)
    {
        if (expandida)
        {
            return posicionLocalBarraExpandida;
        }

        Vector3 posicion = mantenerYZDeBarraExpandidaAlContraer
            ? new Vector3(posicionLocalBarraContraida.x, posicionLocalBarraExpandida.y, posicionLocalBarraExpandida.z)
            : posicionLocalBarraContraida;

        if (usarXDePuntoMiradaContraidoParaBarra && puntoMiradaContraido != null && barraInferior != null)
        {
            Transform padreBarra = barraInferior.parent;
            Vector3 puntoLocalEnPadreBarra = padreBarra != null
                ? padreBarra.InverseTransformPoint(puntoMiradaContraido.position)
                : puntoMiradaContraido.position;

            float xCalculada = puntoLocalEnPadreBarra.x;
            float limiteX = Mathf.Max(1f, limiteAbsXBarraContraida);

            if (float.IsFinite(xCalculada) && Mathf.Abs(xCalculada) <= limiteX)
            {
                posicion.x = xCalculada;
            }
            else if (usarPosicionContraidaComoRespaldoSiXFalla)
            {
                posicion.x = posicionLocalBarraContraida.x;
                DebugLog("TUTORIAL: X de PuntoMiradaContraido ignorada por seguridad. X calculada: " + xCalculada.ToString("F2"));
            }
        }

        return posicion;
    }

    private IEnumerator AjustarBarraInferiorRutina(bool expandida)
    {
        if (barraInferior == null)
        {
            yield break;
        }

        Vector3 posicionInicio = barraInferior.localPosition;
        Quaternion rotacionInicio = barraInferior.localRotation;
        Vector3 escalaInicio = barraInferior.localScale;

        Vector3 posicionDestino = ObtenerPosicionLocalBarraPorEstado(expandida);

        Quaternion rotacionDestino = Quaternion.Euler(
            expandida
                ? rotacionLocalBarraExpandida
                : rotacionLocalBarraContraida
        );

        Vector3 escalaDestino = expandida
            ? escalaLocalBarraExpandida
            : escalaLocalBarraContraida;

        float tiempo = 0f;
        float duracion = Mathf.Max(0.01f, duracionAjusteBarra);

        while (tiempo < duracion)
        {
            tiempo += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(tiempo / duracion);
            float smooth = Mathf.SmoothStep(0f, 1f, t);

            barraInferior.localPosition = Vector3.Lerp(
                posicionInicio,
                posicionDestino,
                smooth
            );

            barraInferior.localRotation = Quaternion.Slerp(
                rotacionInicio,
                rotacionDestino,
                smooth
            );

            barraInferior.localScale = Vector3.Lerp(
                escalaInicio,
                escalaDestino,
                smooth
            );

            yield return null;
        }

        barraInferior.localPosition = posicionDestino;
        barraInferior.localRotation = rotacionDestino;
        barraInferior.localScale = escalaDestino;

        rutinaBarraInferior = null;
        ReaplicarEstadoVisualTutorialSiguienteFrame();
    }

    private void AplicarEstadoVisualBarraYMiradaActual(bool aplicarBarra, bool rotarMirada, bool repetirSiguienteFrame)
    {
        PrepararReferencias();
        PrepararPuntosMiradaTutorial();
        PrepararBarraInferior();

        if (rutinaBarraInferior != null)
        {
            StopCoroutine(rutinaBarraInferior);
            rutinaBarraInferior = null;
        }

        if (aplicarBarra)
        {
            AplicarBarraInferiorInstantanea(barraExpandidaActual);
        }

        if (rotarMirada && hacerQueTutorialMireAlJugador)
        {
            RotarTutorialHaciaJugador();
        }

        if (repetirSiguienteFrame)
        {
            ReaplicarEstadoVisualTutorialSiguienteFrame();
        }
    }

    private void ReaplicarEstadoVisualTutorialSiguienteFrame()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        if (rutinaReaplicarBarraSiguienteFrame != null)
        {
            StopCoroutine(rutinaReaplicarBarraSiguienteFrame);
        }

        rutinaReaplicarBarraSiguienteFrame = StartCoroutine(ReaplicarEstadoVisualTutorialSiguienteFrameRutina());
    }

    private IEnumerator ReaplicarEstadoVisualTutorialSiguienteFrameRutina()
    {
        yield return null;

        if (tutorialAgarradoPocket)
        {
            rutinaReaplicarBarraSiguienteFrame = null;
            yield break;
        }

        // Al salir del pocket/arco, algunos objetos vuelven a activarse un frame después.
        // Por eso se reaplica aquí la posición local del GrabHandleBottom2 y la mirada.
        AplicarEstadoVisualBarraYMiradaActual(true, true, false);

        yield return null;

        if (tutorialAgarradoPocket)
        {
            rutinaReaplicarBarraSiguienteFrame = null;
            yield break;
        }

        // Segundo frame de seguridad para cuando Canvas/Layout/animación del mando actualiza tarde.
        AplicarEstadoVisualBarraYMiradaActual(true, true, false);

        rutinaReaplicarBarraSiguienteFrame = null;
    }

    private void UbicarTutorialEnPuntoManualSiCorresponde()
    {
        if (ubicarTutorialSoloLaPrimeraVez && tutorialYaFueUbicadoInicialmente)
        {
            // Ya se ubicó una vez al frente. Si viene del arco/pocket,
            // NO movemos la posición; solo corregimos la mirada.
            PrepararReferencias();
            PrepararPuntosMiradaTutorial();
            AplicarBarraInferiorInstantanea(barraExpandidaActual);

            if (hacerQueTutorialMireAlJugador)
            {
                RotarTutorialHaciaJugador();
            }

            DebugLog("TUTORIAL: no se reubicó en el punto verde porque ya fue ubicado una vez.");
            return;
        }

        UbicarTutorialEnPuntoManual();
        tutorialYaFueUbicadoInicialmente = true;
    }

    private void UbicarTutorialEnPuntoManual()
    {
        if (!ubicarTutorialEnPuntoManual)
        {
            return;
        }

        if (rootParaUbicar == null)
        {
            rootParaUbicar = panelRoot != null ? panelRoot.transform : transform;
        }

        if (spawnManager == null)
        {
            spawnManager = AlgoLabManualPanelSpawnManager.Instance;
        }

        if (spawnManager != null)
        {
            Transform referencia = ObtenerReferenciaManualTutorial();

            if (referencia == null)
            {
                UbicarTutorialFallbackCamara();
                return;
            }

            Vector3 posicionLocal = ObtenerPosicionLocalTutorial();
            Vector3 posicionMundo = referencia.TransformPoint(posicionLocal);

            rootParaUbicar.position = posicionMundo;

            if (hacerQueTutorialMireAlJugador)
            {
                RotarTutorialHaciaJugador();
            }
            else
            {
                Quaternion rotacionMundo =
                    referencia.rotation * Quaternion.Euler(rotacionLocalTutorialEuler);

                if (invertirFrenteTutorial)
                {
                    rotacionMundo *= Quaternion.Euler(0f, 180f, 0f);
                }

                rootParaUbicar.rotation = rotacionMundo;
            }

            tutorialYaFueUbicadoInicialmente = true;
            DebugLog("Tutorial ubicado en su punto local propio.");
            return;
        }

        UbicarTutorialFallbackCamara();
    }

    private bool IntentarUbicarTutorialFrenteALaCabeza(bool requerirPoseValida = false)
    {
        if (cabezaJugador == null && Camera.main != null)
        {
            cabezaJugador = Camera.main.transform;
        }

        if (cabezaJugador == null || rootParaUbicar == null)
        {
            return false;
        }

        if (requerirPoseValida && !PoseCabezaValidaParaUbicacion())
        {
            return false;
        }

        Vector3 frente = Vector3.ProjectOnPlane(cabezaJugador.forward, Vector3.up);
        if (frente.sqrMagnitude < 0.0001f)
        {
            frente = Vector3.forward;
        }

        frente.Normalize();

        if (spawnManager == null)
        {
            spawnManager = AlgoLabManualPanelSpawnManager.Instance;
        }

        if (spawnManager != null)
        {
            spawnManager.DesregistrarObjetoParaAlturaDinamica(rootParaUbicar);
        }

        rootParaUbicar.position = cabezaJugador.position +
                                  frente * Mathf.Max(0.35f, distanciaPrimeraAparicionFrenteCabeza) +
                                  Vector3.up * offsetVerticalPrimeraAparicion;

        if (hacerQueTutorialMireAlJugador)
        {
            RotarTutorialHaciaJugador();
        }

        tutorialYaFueUbicadoInicialmente = true;
        DebugLog("TUTORIAL: primera aparicion ubicada frente a la cabeza.");
        return true;
    }

    private bool PoseCabezaValidaParaUbicacion()
    {
        if (cabezaJugador == null || !cabezaJugador.gameObject.activeInHierarchy)
        {
            return false;
        }

        Vector3 posicion = cabezaJugador.position;
        if (float.IsNaN(posicion.x) || float.IsNaN(posicion.y) || float.IsNaN(posicion.z) ||
            float.IsInfinity(posicion.x) || float.IsInfinity(posicion.y) || float.IsInfinity(posicion.z))
        {
            return false;
        }

        return Mathf.Abs(cabezaJugador.localPosition.y) > 0.2f || Mathf.Abs(posicion.y) > 0.2f;
    }

    private Transform ObtenerReferenciaManualTutorial()
    {
        if (spawnManager == null)
        {
            spawnManager = AlgoLabManualPanelSpawnManager.Instance;
        }

        if (spawnManager != null)
        {
            if (spawnManager.referenciaManual != null)
            {
                return spawnManager.referenciaManual;
            }

            return spawnManager.transform;
        }

        return transform;
    }

    private Vector3 ObtenerPosicionLocalTutorial()
    {
        if (usarPuntoPropioTutorial)
        {
            return posicionLocalTutorialPropia;
        }

        if (spawnManager != null && usarPosicionObjetoFrontalDelManager)
        {
            return spawnManager.posicionLocalObjetoFrontal + offsetLocalTutorialDesdeObjetoFrontal;
        }

        return posicionLocalTutorialManual;
    }

    private void UbicarTutorialFallbackCamara()
    {
        Camera camara = Camera.main;

        if (camara == null)
        {
            Debug.LogWarning("No se pudo ubicar el tutorial porque no existe ManualPanelSpawnManager ni Camera.main.");
            return;
        }

        Vector3 forward = camara.transform.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.001f)
        {
            forward = Vector3.forward;
        }

        forward.Normalize();

        Quaternion rotacionBase = Quaternion.LookRotation(forward, Vector3.up);

        Vector3 posicionMundoFallback =
            camara.transform.position +
            rotacionBase * posicionLocalTutorialManual;

        rootParaUbicar.position = posicionMundoFallback;

        if (hacerQueTutorialMireAlJugador)
        {
            RotarTutorialHaciaJugador();
        }
        else
        {
            Quaternion rotacionMundoFallback =
                rotacionBase * Quaternion.Euler(rotacionLocalTutorialEuler);

            if (invertirFrenteTutorial)
            {
                rotacionMundoFallback *= Quaternion.Euler(0f, 180f, 0f);
            }

            rootParaUbicar.rotation = rotacionMundoFallback;
        }

        tutorialYaFueUbicadoInicialmente = true;
        DebugLog("Tutorial ubicado usando Camera.main como fallback.");
    }

    private Vector3 ObtenerPuntoBaseParaMirar()
    {
        PrepararPuntosMiradaTutorial();

        bool vistaMandoExpandida = mandoController != null
            ? mandoController.EstaExpandidoVisualmente()
            : barraExpandidaActual;

        if (usarRootComoPuntoMiradaEstableSiempre)
        {
            return AplicarOffsetVerticalMiradaExpandida(ObtenerPosicionRootTutorialSegura(), vistaMandoExpandida);
        }

        // Los puntos hijos cambian al contraer o expandir la barra. Durante el
        // agarre usamos el root estable para evitar que la rotacion mueva el panel.
        if (tutorialAgarradoPocket &&
            usarRootComoPuntoMiradaMientrasAgarrado &&
            !usarPuntosMiradaMientrasAgarrado)
        {
            return AplicarOffsetVerticalMiradaExpandida(ObtenerPosicionRootTutorialSegura(), vistaMandoExpandida);
        }

        if (restaurandoDesdePocket && usarRootComoPuntoMiradaAlRestaurarPocket)
        {
            return AplicarOffsetVerticalMiradaExpandida(ObtenerPosicionRootTutorialSegura(), vistaMandoExpandida);
        }

        bool puedeUsarPuntoMirada = usarPuntosMiradaSegunMando &&
                                    (!tutorialAgarradoPocket || usarPuntosMiradaMientrasAgarrado);

        if (puedeUsarPuntoMirada)
        {
            if (vistaMandoExpandida &&
                (puntoMiradaExpandido == null || puntoMiradaExpandido == puntoMiradaContraido) &&
                IntentarObtenerCentroVisualExpandido(out Vector3 centroExpandido) &&
                PuntoMiradaEsSeguro(centroExpandido))
            {
                return AplicarOffsetVerticalMiradaExpandida(centroExpandido, true);
            }

            Transform punto = ObtenerTransformPuntoMiradaActual(vistaMandoExpandida);

            if (PuntoMiradaEsSeguro(punto))
            {
                return AplicarOffsetVerticalMiradaExpandida(punto.position, vistaMandoExpandida);
            }
        }

        if (usarRootComoPuntoMiradaMientrasAgarrado && tutorialAgarradoPocket)
        {
            return AplicarOffsetVerticalMiradaExpandida(ObtenerPosicionRootTutorialSegura(), vistaMandoExpandida);
        }

        return AplicarOffsetVerticalMiradaExpandida(ObtenerPosicionRootTutorialSegura(), vistaMandoExpandida);
    }

    private Vector3 AplicarOffsetVerticalMiradaExpandida(Vector3 puntoBase, bool vistaExpandida)
    {
        if (!vistaExpandida)
        {
            return puntoBase;
        }

        return puntoBase + Vector3.up * offsetVerticalMiradaExpandido;
    }

    private bool PuntoMiradaEsSeguro(Transform punto)
    {
        if (punto == null)
        {
            return false;
        }

        return PuntoMiradaEsSeguro(punto.position);
    }

    private bool PuntoMiradaEsSeguro(Vector3 posicionPunto)
    {

        if (!validarPuntoMiradaAntesDeUsarlo)
        {
            return true;
        }

        if (!Vector3EsFinito(posicionPunto))
        {
            return false;
        }

        Vector3 posicionRoot = ObtenerPosicionRootTutorialSegura();
        float distanciaRoot = Vector3.Distance(posicionRoot, posicionPunto);

        if (distanciaRoot > Mathf.Max(0.05f, distanciaMaximaPuntoMiradaDesdeRoot))
        {
            DebugLog("TUTORIAL: Punto de mirada ignorado por estar demasiado lejos del root. Distancia: " + distanciaRoot.ToString("F2"));
            return false;
        }

        if (cabezaJugador != null)
        {
            float distanciaCabeza = Vector3.Distance(cabezaJugador.position, posicionPunto);

            if (distanciaCabeza < Mathf.Max(0.02f, distanciaMinimaPuntoMiradaACabeza))
            {
                DebugLog("TUTORIAL: Punto de mirada ignorado por quedar demasiado cerca de la cabeza.");
                return false;
            }
        }

        return true;
    }

    private bool IntentarObtenerCentroVisualExpandido(out Vector3 puntoMundo)
    {
        puntoMundo = Vector3.zero;

        if (panelRoot == null || tutorialMainPanel == null)
        {
            return false;
        }

        Bounds boundsPrincipal = RectTransformUtility.CalculateRelativeRectTransformBounds(
            panelRoot,
            tutorialMainPanel
        );

        float minX = boundsPrincipal.min.x;
        float maxX = boundsPrincipal.max.x;

        RectTransform panelMando = mandoController != null ? mandoController.panelRoot : null;
        if (panelMando != null)
        {
            float anchoVisible = Mathf.Max(panelMando.rect.width, mandoController.anchoVisible);
            Vector3 extremoLocalA = new Vector3(-panelMando.pivot.x * anchoVisible, 0f, 0f);
            Vector3 extremoLocalB = new Vector3((1f - panelMando.pivot.x) * anchoVisible, 0f, 0f);
            Vector3 extremoA = panelRoot.InverseTransformPoint(panelMando.TransformPoint(extremoLocalA));
            Vector3 extremoB = panelRoot.InverseTransformPoint(panelMando.TransformPoint(extremoLocalB));

            minX = Mathf.Min(minX, extremoA.x, extremoB.x);
            maxX = Mathf.Max(maxX, extremoA.x, extremoB.x);
        }

        Vector3 centroLocal = boundsPrincipal.center;
        centroLocal.x = (minX + maxX) * 0.5f;
        puntoMundo = panelRoot.TransformPoint(centroLocal);

        return Vector3EsFinito(puntoMundo);
    }

    private bool Vector3EsFinito(Vector3 v)
    {
        return float.IsFinite(v.x) && float.IsFinite(v.y) && float.IsFinite(v.z);
    }

    private Transform ObtenerTransformPuntoMiradaActual(bool vistaMandoExpandida)
    {
        if (!usarPuntosMiradaSegunMando)
        {
            return null;
        }

        if (vistaMandoExpandida)
        {
            return puntoMiradaExpandido != null
                ? puntoMiradaExpandido
                : (puntoMiradaContraido != null ? puntoMiradaContraido : rootParaUbicar);
        }

        return puntoMiradaContraido != null ? puntoMiradaContraido : rootParaUbicar;
    }

    private Vector3 ObtenerPosicionRootTutorialSegura()
    {
        if (rootParaUbicar != null)
        {
            return rootParaUbicar.position;
        }

        if (panelRoot != null)
        {
            return panelRoot.position;
        }

        return transform.position;
    }

    private void RotarTutorialHaciaJugador()
    {
        ActualizarEstadoAgarrePocketPorPolling();

        if (DebePausarMiradaPorAgarrePocket())
        {
            return;
        }

        if (!hacerQueTutorialMireAlJugador)
        {
            return;
        }

        if (rootParaUbicar == null)
        {
            rootParaUbicar = panelRoot != null ? panelRoot.transform : transform;
        }

        if (cabezaJugador == null && Camera.main != null)
        {
            cabezaJugador = Camera.main.transform;
        }

        if (cabezaJugador == null || rootParaUbicar == null)
        {
            return;
        }

        Vector3 puntoBaseMirada = ObtenerPuntoBaseParaMirar();
        Vector3 direccion = puntoBaseMirada - cabezaJugador.position;

        if (soloRotacionYTutorial)
        {
            direccion.y = 0f;
        }

        if (direccion.sqrMagnitude < 0.001f)
        {
            return;
        }

        if (!AlgoLabPanelFacing.TryGetStableRotation(
                direccion,
                soloRotacionYTutorial,
                Quaternion.Euler(rotacionLocalTutorialEuler),
                invertirFrenteTutorial,
                out Quaternion rotacionObjetivo))
        {
            return;
        }

        rootParaUbicar.rotation = rotacionObjetivo;
    }

    private void RotarTutorialHaciaJugadorSuave()
    {
        ActualizarEstadoAgarrePocketPorPolling();

        if (DebePausarMiradaPorAgarrePocket())
        {
            return;
        }

        if (!hacerQueTutorialMireAlJugador)
        {
            return;
        }

        if (rootParaUbicar == null)
        {
            rootParaUbicar = panelRoot != null ? panelRoot.transform : transform;
        }

        if (cabezaJugador == null && Camera.main != null)
        {
            cabezaJugador = Camera.main.transform;
        }

        if (cabezaJugador == null || rootParaUbicar == null)
        {
            return;
        }

        Vector3 puntoBaseMirada = ObtenerPuntoBaseParaMirar();
        Vector3 direccion = puntoBaseMirada - cabezaJugador.position;

        if (soloRotacionYTutorial)
        {
            direccion.y = 0f;
        }

        if (direccion.sqrMagnitude < 0.001f)
        {
            return;
        }

        if (!AlgoLabPanelFacing.TryGetStableRotation(
                direccion,
                soloRotacionYTutorial,
                Quaternion.Euler(rotacionLocalTutorialEuler),
                invertirFrenteTutorial,
                out Quaternion rotacionObjetivo))
        {
            return;
        }

        rootParaUbicar.rotation = Quaternion.Slerp(
            rootParaUbicar.rotation,
            rotacionObjetivo,
            Time.unscaledDeltaTime * suavizadoMirarJugador
        );
    }

    [ContextMenu("Iniciar Tutorial")]
    public void IniciarTutorial()
    {
        ActivarTutorialVisual();

        PrepararReferencias();
        DetenerRutinasTransitoriasTutorial();
        PrepararIndicadoresGatillos();

        bool esPrimeraUbicacion = !tutorialYaFueUbicadoInicialmente;
        UbicarTutorialEnPuntoManualSiCorresponde();

        CapturarEstadosIniciales();

        tutorialActivo = true;
        tutorialFinalizado = false;
        cerrandoTutorialPorBotonIniciar = false;
        tutorialVisibleRestauradoDesdePocket = false;
        tiempoTutorial = 0f;
        ultimoTiempoBotonA = -999f;

        ReiniciarEstadoInteractivo();
        ReiniciarEventos();
        ReiniciarPanelVisual();

        AplicarEstadoInicialImagenesPanelesTutorial();

        AplicarEstadoInicialBarraAlIniciarTutorial();

        if (ocultarCirculosAlIniciarTutorial)
        {
            OcultarTodosLosIndicadoresGatillosInstantaneo();
        }

        OrdenarEventos();

        if (esPrimeraUbicacion)
        {
            rutinaAsegurarUbicacionInicial = StartCoroutine(
                AsegurarUbicacionInicialDesdeManualSpawner()
            );
        }

        DebugLog("Tutorial iniciado: " + nombreTutorial);
    }

    private void DetenerRutinasTransitoriasTutorial()
    {
        if (rutinaAsegurarUbicacionInicial != null)
        {
            StopCoroutine(rutinaAsegurarUbicacionInicial);
            rutinaAsegurarUbicacionInicial = null;
        }

        if (rutinaPanel != null)
        {
            StopCoroutine(rutinaPanel);
            rutinaPanel = null;
        }

        if (rutinaRevelarVideo != null)
        {
            StopCoroutine(rutinaRevelarVideo);
            rutinaRevelarVideo = null;
        }

        if (rutinaEsperarFinVideoActual != null)
        {
            StopCoroutine(rutinaEsperarFinVideoActual);
            rutinaEsperarFinVideoActual = null;
        }

        esperandoFinVideoActual = false;
        tiempoRealInicioEsperaAccion = -999f;
        tiempoRealInicioEsperaVideo = -999f;

        DetenerAudioActual();
        DetenerVideoActual();
    }

    [ContextMenu("Tutorial - permitir reubicar una vez")]
    public void PermitirReubicarTutorialUnaVez()
    {
        tutorialYaFueUbicadoInicialmente = false;
    }

    /// <summary>
    /// Prepara el tutorial para un inicio solicitado por el flujo del juego.
    /// Si estaba guardado en el panel de opciones, elimina su mini card, limpia
    /// el estado pocket y reactiva el panel real antes de reproducirlo.
    /// </summary>
    public void PrepararInicioAutomaticoExterno()
    {
        BuscarPanelOpcionesManagerSiCorresponde();

        if (panelOpcionesManager != null)
        {
            GameObject objetoTutorial = rootParaUbicar != null
                ? rootParaUbicar.gameObject
                : gameObject;

            panelOpcionesManager.PrepararPanelesDeObjetoParaControlTutorial(
                objetoTutorial,
                Mathf.Max(5f, tiempoBloquearAutoRegistroObjetoTutorial),
                true
            );
        }

        CancelarAgarreTutorialPocketSiQuedoPegado(false);
        tutorialGuardadoEnPanelOpciones = false;
        tutorialOmitidoDesdePanelOpciones = false;
        avisoTutorialOmitidoDesdePanelOpcionesConsumido = true;
        tutorialVisibleRestauradoDesdePocket = false;
        ActivarTutorialVisual();
    }

    private IEnumerator AsegurarUbicacionInicialConTrackingActualizado()
    {
        for (int frame = 0; frame < 45; frame++)
        {
            yield return null;

            if (!tutorialActivo || tutorialFinalizado || tutorialAgarradoPocket)
            {
                break;
            }

            PrepararGrabHandleTutorialPocket();
            if (grabHandleTutorialPocket != null && grabHandleTutorialPocket.EstaAgarrando)
            {
                break;
            }

            if (frame == 1 || frame == 5 || frame == 15 || frame == 30 || frame == 44)
            {
                IntentarUbicarTutorialFrenteALaCabeza(frame != 44);
            }
        }

        rutinaAsegurarUbicacionInicial = null;
    }

    private IEnumerator AsegurarUbicacionInicialDesdeManualSpawner()
    {
        for (int frame = 0; frame < 31; frame++)
        {
            yield return null;

            if (!tutorialActivo || tutorialFinalizado || tutorialAgarradoPocket)
            {
                break;
            }

            PrepararGrabHandleTutorialPocket();
            if (grabHandleTutorialPocket != null && grabHandleTutorialPocket.EstaAgarrando)
            {
                break;
            }

            if (frame == 1 || frame == 5 || frame == 15 || frame == 30)
            {
                UbicarTutorialEnPuntoManual();
            }
        }

        rutinaAsegurarUbicacionInicial = null;
    }

    private void OrdenarEventos()
    {
        if (eventos == null)
        {
            eventos = new List<EventoTutorial>();
        }

        eventos.Sort((a, b) =>
        {
            if (a == null && b == null) return 0;
            if (a == null) return 1;
            if (b == null) return -1;

            int comparacionTiempo = a.tiempo.CompareTo(b.tiempo);

            if (comparacionTiempo != 0)
            {
                return comparacionTiempo;
            }

            return a.orden.CompareTo(b.orden);
        });
    }

    private void ReiniciarEventos()
    {
        if (eventos == null)
        {
            eventos = new List<EventoTutorial>();
            return;
        }

        for (int i = 0; i < eventos.Count; i++)
        {
            if (eventos[i] != null)
            {
                eventos[i].ejecutado = false;
                eventos[i].indicadorGatilloMostrado = false;
            }
        }
    }

    private void EjecutarEventosPendientes()
    {
        if (eventos == null)
        {
            return;
        }

        for (int i = 0; i < eventos.Count; i++)
        {
            EventoTutorial evento = eventos[i];

            if (evento == null || evento.ejecutado)
            {
                continue;
            }

            if (tiempoTutorial >= evento.tiempo)
            {
                evento.ejecutado = true;

                try
                {
                    EjecutarEvento(evento);
                }
                catch (System.Exception ex)
                {
                    Debug.LogException(ex, this);
                    DebugLog("TUTORIAL: evento omitido por excepcion para evitar bloqueo.");
                }

                if (esperandoAccionInteractiva || esperandoFinVideoActual)
                {
                    break;
                }
            }
        }
    }

    private void EjecutarEvento(EventoTutorial evento)
    {
        switch (evento.tipoEvento)
        {
            case TipoEventoTutorial.MostrarPanel:
                MostrarPanel();
                break;

            case TipoEventoTutorial.RevelarVideo:
                RevelarVideo();
                break;

            case TipoEventoTutorial.CerrarPanel:
                CerrarPanel();
                break;

            case TipoEventoTutorial.CambiarInstruccion:
                CambiarInstruccion(evento.texto);
                break;

            case TipoEventoTutorial.OcultarInstruccion:
                OcultarInstruccion();
                break;

            case TipoEventoTutorial.ReproducirAudioClip:
                ReproducirAudioClip(ObtenerAudioClipParaEvento(evento));
                break;

            case TipoEventoTutorial.DetenerAudioActual:
                DetenerAudioActual();
                break;

            case TipoEventoTutorial.SilenciarTutorial:
                SilenciarTutorial();
                break;

            case TipoEventoTutorial.ActivarSonidoTutorial:
                ActivarSonidoTutorial();
                break;

            case TipoEventoTutorial.AlternarSilencioTutorial:
                AlternarSilencioTutorial();
                break;

            case TipoEventoTutorial.ReproducirVideoClip:
                ReproducirVideoClip(
                    evento.videoClip,
                    evento.repetirVideo,
                    evento.duracionReproduccionVideo,
                    evento.reiniciarVideoDesdeInicio
                );
                break;

            case TipoEventoTutorial.DetenerVideoActual:
                DetenerVideoActual();
                break;

            case TipoEventoTutorial.PausarVideoActual:
                PausarVideoActual();
                break;

            case TipoEventoTutorial.ReanudarVideoActual:
                ReanudarVideoActual();
                break;

            case TipoEventoTutorial.MostrarPanelMando:
                if (mandoController != null) mandoController.MostrarPanelMando();
                AjustarBarraInferior(true);
                break;

            case TipoEventoTutorial.OcultarPanelMando:
                if (mandoController != null) mandoController.OcultarPanelMando();
                AjustarBarraInferior(false);
                break;

            case TipoEventoTutorial.ColapsarMando:
                if (mandoController != null) mandoController.ColapsarMando();
                AjustarBarraInferior(false);
                break;

            case TipoEventoTutorial.ExpandirMando:
                if (mandoController != null) mandoController.ExpandirMando();
                AjustarBarraInferior(true);
                break;

            case TipoEventoTutorial.CambiarMandoLateral:
                if (mandoController != null) mandoController.CambiarMandoLateral();
                break;

            case TipoEventoTutorial.CambiarMandoFrontal:
                if (mandoController != null) mandoController.CambiarMandoFrontal();
                break;

            case TipoEventoTutorial.CambiarMandoLateralConTransicion:
                if (mandoController != null) mandoController.CambiarMandoLateralConTransicion();
                break;

            case TipoEventoTutorial.CambiarMandoFrontalConTransicion:
                if (mandoController != null) mandoController.CambiarMandoFrontalConTransicion();
                break;

            case TipoEventoTutorial.MostrarMandoIdle:
                if (mandoController != null) mandoController.MostrarMandoIdle();
                break;

            case TipoEventoTutorial.PresionarGatilloPrincipal:
                MostrarCirculoGatilloPrincipalSmooth();
                if (mandoController != null) mandoController.PresionarGatilloPrincipal();
                break;

            case TipoEventoTutorial.SoltarGatilloPrincipal:
                if (mandoController != null) mandoController.SoltarGatilloPrincipal();
                OcultarCirculoGatilloPrincipalDespues();
                break;

            case TipoEventoTutorial.PresionarGatilloSecundario:
                MostrarCirculoGatilloSecundarioSmooth();
                if (mandoController != null) mandoController.PresionarGatilloSecundario();
                break;

            case TipoEventoTutorial.SoltarGatilloSecundario:
                if (mandoController != null) mandoController.SoltarGatilloSecundario();
                OcultarCirculoGatilloSecundarioDespues();
                break;

            case TipoEventoTutorial.ForzarMantenerGatilloPrincipal:
                MostrarCirculoGatilloPrincipalSmooth();
                if (mandoController != null) mandoController.ForzarMantenerGatilloPrincipal();
                break;

            case TipoEventoTutorial.ForzarMantenerGatilloSecundario:
                MostrarCirculoGatilloSecundarioSmooth();
                if (mandoController != null) mandoController.ForzarMantenerGatilloSecundario();
                break;

            case TipoEventoTutorial.PresionarBotonA:
                if (mandoController != null) mandoController.PresionarBotonA();
                break;

            case TipoEventoTutorial.PresionarBotonB:
                if (mandoController != null) mandoController.PresionarBotonB();
                break;

            case TipoEventoTutorial.MoverPalanca:
                if (mandoController != null) mandoController.MoverPalanca();
                break;

            case TipoEventoTutorial.SoltarPalanca:
                if (mandoController != null) mandoController.SoltarPalanca();
                break;

            case TipoEventoTutorial.OmitirTutorial:
                OmitirTutorial();
                break;

            case TipoEventoTutorial.FinalizarTutorial:
                FinalizarTutorial();
                break;

            case TipoEventoTutorial.IniciarPractica:
                IniciarPractica();
                break;

            case TipoEventoTutorial.ContinuarAplicacion:
                ContinuarAplicacion();
                break;

            case TipoEventoTutorial.EsperarAccionInteractiva:
                IniciarEsperaAccionInteractiva(evento);
                break;

            case TipoEventoTutorial.EsperarFinVideoActual:
                IniciarEsperaFinVideoActual(evento);
                break;

            case TipoEventoTutorial.OcultarIndicadoresGatillos:
                OcultarTodosLosIndicadoresGatillosInstantaneo();
                break;

            case TipoEventoTutorial.ActivarObjeto:
                ProtegerObjetoTutorialDeAutoRegistro(evento.objeto, true);
                if (evento.objeto != null) evento.objeto.SetActive(true);
                break;

            case TipoEventoTutorial.DesactivarObjeto:
                ProtegerObjetoTutorialDeAutoRegistro(evento.objeto, false);
                if (evento.objeto != null) evento.objeto.SetActive(false);
                break;

            case TipoEventoTutorial.CambiarImagen:
                CambiarImagen(evento.imagen);
                break;

            case TipoEventoTutorial.EjecutarUnityEvent:
                if (evento.unityEvent != null) evento.unityEvent.Invoke();
                break;

            case TipoEventoTutorial.MostrarImagenPanelPrincipal:
                MostrarImagenPanelPrincipal();
                break;

            case TipoEventoTutorial.OcultarImagenPanelPrincipal:
                OcultarImagenPanelPrincipal();
                break;

            case TipoEventoTutorial.MostrarImagenPanelDiagramas:
                MostrarImagenPanelDiagramas();
                break;

            case TipoEventoTutorial.OcultarImagenPanelDiagramas:
                OcultarImagenPanelDiagramas();
                break;

            case TipoEventoTutorial.MostrarImagenPanelIA:
                MostrarImagenPanelIA();
                break;

            case TipoEventoTutorial.OcultarImagenPanelIA:
                OcultarImagenPanelIA();
                break;

            case TipoEventoTutorial.OcultarImagenesPanelesTutorial:
                OcultarImagenesPanelesTutorial();
                break;

            case TipoEventoTutorial.HabilitarPanelOpciones:
            case TipoEventoTutorial.HabilitarArco:
                HabilitarPanelOpciones();
                break;

            case TipoEventoTutorial.DeshabilitarPanelOpciones:
            case TipoEventoTutorial.DeshabilitarArco:
                DeshabilitarPanelOpciones();
                break;
        }
    }

    private void IniciarEsperaAccionInteractiva(EventoTutorial evento)
    {
        if (!permitirTutorialInteractivo || evento == null)
        {
            return;
        }

        if (evento.accionEsperada == AccionTutorialInteractiva.Ninguna)
        {
            DebugLog("TUTORIAL: espera interactiva sin accion asignada. Se continua por seguridad.");
            AvanzarTimelineDespuesDeEspera();
            return;
        }

        esperandoAccionInteractiva = true;
        accionInteractivaActual = evento.accionEsperada;
        eventoInteractivoActual = evento;
        tiempoRealInicioEsperaAccion = Time.unscaledTime;

        accionPanelAgarrado = false;
        accionPanelMovido = false;
        accionPanelSoltado = false;
        accionPanelMetidoEnArco = false;
        accionPanelSacadoDelArco = false;
        secuenciaPanelMeterYSacarDelArco = false;
        secuenciaPanelSacarYMeterDelArco = false;

        if (!string.IsNullOrWhiteSpace(evento.texto))
        {
            CambiarInstruccion(evento.texto);
        }
        else if (!string.IsNullOrWhiteSpace(textoEsperandoAccion))
        {
            CambiarInstruccion(textoEsperandoAccion);
        }

        DebugLog("TUTORIAL PAUSADO. Esperando acción: " + accionInteractivaActual);
    }

    private void CancelarAgarreTutorialPocketSiQuedoPegado(bool notificarSoltado)
    {
        PrepararGrabHandleTutorialPocket();

        if (grabHandleTutorialPocket != null && grabHandleTutorialPocket.EstaAgarrando)
        {
            grabHandleTutorialPocket.CancelarAgarreForzadoDesdeExterno(notificarSoltado);
        }

        tutorialAgarradoPocket = false;
    }

    public void NotificarAgarreTutorialPocketIniciado()
    {
        tutorialAgarradoPocket = true;
        AsegurarMiradaMientrasAgarraPocket();
        DetenerAnimacionBarraPorAgarrePocket();
        DebugLog("TUTORIAL: agarre pocket iniciado. Se estabiliza barra y mantiene mirada.");
    }

    public void NotificarAgarreTutorialPocketSoltado()
    {
        tutorialAgarradoPocket = false;
        barraPendienteAplicarCuandoSuelte = false;

        AplicarEstadoVisualBarraYMiradaActual(true, true, true);

        DebugLog("TUTORIAL: agarre pocket soltado. Se reaplicó barra y mirada.");
    }

    public void NotificarTutorialRestauradoDesdePocket()
    {
        CancelarAgarreTutorialPocketSiQuedoPegado(false);

        // IMPORTANTE:
        // No mover la posición aquí. El PocketManager ya restauró [TUTORIAL_SYSTEM]
        // exactamente donde soltaste la mini card.
        restaurandoDesdePocket = true;
        tutorialAgarradoPocket = false;

        tutorialPasoPorPocket = true;
        bool veniaDesdePanelOpciones = tutorialGuardadoEnPanelOpciones || tutorialOmitidoDesdePanelOpciones;
        tutorialGuardadoEnPanelOpciones = false;
        tutorialVisibleRestauradoDesdePocket = true;

        PrepararReemplazoAudio21SiCorresponde(veniaDesdePanelOpciones);

        if (EstaEsperandoAccionPanelOpcionesInteractiva() &&
            !accionPanelMetidoEnArco &&
            accionInteractivaActual == AccionTutorialInteractiva.MeterYSacarPanelDelArco)
        {
            accionPanelMetidoEnArco = true;
        }

        RegistrarAccionPanelEnArco(false);

        if (noReubicarDespuesDeSalirDelPocket)
        {
            tutorialYaFueUbicadoInicialmente = true;
        }

        PrepararReferencias();
        PrepararPuntosMiradaTutorial();
        PrepararBarraInferior();

        if (mantenerEstadoBarraAlRestaurarDesdePocket)
        {
            AplicarEstadoVisualBarraYMiradaActual(true, true, true);
        }
        else if (hacerQueTutorialMireAlJugador)
        {
            RotarTutorialHaciaJugador();
            ReaplicarEstadoVisualTutorialSiguienteFrame();
        }

        restaurandoDesdePocket = false;

        if (veniaDesdePanelOpciones && reiniciarTutorialSiempreAlSacarDelPanelOpciones)
        {
            ConsumirAvisoSalidaPanelOpcionesSinAudioInmediato();
            ProgramarReinicioTutorialDesdePanelOpciones();
        }
        else
        {
            ProcesarSacarTutorialOmitidoDesdePanelOpcionesSiCorresponde();
        }

        DebugLog("TUTORIAL: restaurado desde pocket. Se conservó posición de la card, estado de barra y mirada.");
    }

    public void EstabilizarPoseRestauradaDesdePocket(Vector3 posicionAnclaMundo)
    {
        if (!Vector3EsFinito(posicionAnclaMundo))
        {
            return;
        }

        // El root se usa solo durante el instante de restauracion. Despues, la
        // mirada debe volver al pivote contraido o expandido que corresponda.
        usarRootComoPuntoMiradaEstableSiempre = false;
        usarRootComoPuntoMiradaAlRestaurarPocket = true;
        usarRootComoPuntoMiradaMientrasAgarrado = true;
        usarPuntosMiradaMientrasAgarrado = true;
        pausarMiradaMientrasAgarraPocket = false;

        if (rutinaEstabilizarPoseRestauradaPocket != null)
        {
            StopCoroutine(rutinaEstabilizarPoseRestauradaPocket);
            rutinaEstabilizarPoseRestauradaPocket = null;
        }

        DebugLog("TUTORIAL: estado interno estabilizado sin modificar la posicion de [TUTORIAL_SYSTEM].");
    }

    private void ConsumirAvisoSalidaPanelOpcionesSinAudioInmediato()
    {
        bool debeNotificar = tutorialOmitidoDesdePanelOpciones &&
                             !avisoTutorialOmitidoDesdePanelOpcionesConsumido;

        tutorialOmitidoDesdePanelOpciones = false;
        avisoTutorialOmitidoDesdePanelOpcionesConsumido = true;

        if (debeNotificar)
        {
            alSacarTutorialOmitidoDesdePanelOpciones?.Invoke();
        }
    }

    private void ProgramarReinicioTutorialDesdePanelOpciones()
    {
        if (rutinaReiniciarDesdePanelOpciones != null)
        {
            StopCoroutine(rutinaReiniciarDesdePanelOpciones);
        }

        rutinaReiniciarDesdePanelOpciones =
            StartCoroutine(ReiniciarTutorialDesdePanelOpcionesSiguienteFrame());
    }

    private IEnumerator ReiniciarTutorialDesdePanelOpcionesSiguienteFrame()
    {
        yield return null;

        if (!isActiveAndEnabled || rootParaUbicar == null || !rootParaUbicar.gameObject.activeInHierarchy)
        {
            rutinaReiniciarDesdePanelOpciones = null;
            yield break;
        }

        tutorialYaFueUbicadoInicialmente = true;
        tutorialActivo = false;
        tutorialFinalizado = false;
        IniciarTutorial();

        restaurandoDesdePocket = true;
        RotarTutorialHaciaJugador();
        restaurandoDesdePocket = false;
        AplicarEstadoVisualBarraYMiradaActual(true, true, true);

        rutinaReiniciarDesdePanelOpciones = null;
        DebugLog("TUTORIAL: reiniciado desde el primer evento al salir del panel de opciones.");
    }

    public void NotificarTutorialGuardadoEnPocket()
    {
        CancelarAgarreTutorialPocketSiQuedoPegado(false);
        tutorialAgarradoPocket = false;

        tutorialPasoPorPocket = true;
        tutorialGuardadoEnPanelOpciones = true;
        tutorialVisibleRestauradoDesdePocket = false;

        RegistrarAccionPanelEnArco(true);

        if (noReubicarDespuesDeSalirDelPocket)
        {
            tutorialYaFueUbicadoInicialmente = true;
        }

        if (rutinaBarraInferior != null)
        {
            StopCoroutine(rutinaBarraInferior);
            rutinaBarraInferior = null;
        }

        barraPendienteAplicarCuandoSuelte = false;

        DebugLog("TUTORIAL: guardado en pocket. Estado de agarre limpiado sin cambiar posición.");

        if (omitirTutorialAlGuardarEnPanelOpciones &&
            tutorialActivo &&
            !tutorialFinalizado &&
            !EstaEsperandoAccionPanelOpcionesInteractiva())
        {
            tutorialOmitidoDesdePanelOpciones = true;
            avisoTutorialOmitidoDesdePanelOpcionesConsumido = false;
            OmitirTutorial();
        }
    }

    public void NotificarPanelGuardadoEnPanelOpciones(AlgoLabPocketPanelItem panel)
    {
        RegistrarAccionPanelEnArco(true, panel);
    }

    public void NotificarPanelRestauradoDesdePanelOpciones(AlgoLabPocketPanelItem panel)
    {
        RegistrarAccionPanelEnArco(false, panel);
    }

    public void NotificarPanelAgarrado(AlgoLabPanelGrabHandle panel)
    {
        if (EsGrabHandleDelTutorialPocket(panel))
        {
            tutorialAgarradoPocket = true;
            AsegurarMiradaMientrasAgarraPocket();
            DetenerAnimacionBarraPorAgarrePocket();
        }

        if (!DebeProcesarAccionInteractiva(panel))
        {
            return;
        }

        accionPanelAgarrado = true;

        DebugLog("TUTORIAL: panel agarrado.");

        RevisarSiAccionInteractivaCompletada();
    }

    public void NotificarPanelMovido(AlgoLabPanelGrabHandle panel)
    {
        if (EsGrabHandleDelTutorialPocket(panel))
        {
            tutorialAgarradoPocket = true;
            AsegurarMiradaMientrasAgarraPocket();
        }

        if (!DebeProcesarAccionInteractiva(panel))
        {
            return;
        }

        accionPanelMovido = true;

        DebugLog("TUTORIAL: panel movido.");

        RevisarSiAccionInteractivaCompletada();
    }

    public void NotificarPanelSoltado(AlgoLabPanelGrabHandle panel)
    {
        if (EsGrabHandleDelTutorialPocket(panel))
        {
            tutorialAgarradoPocket = false;

            if (barraPendienteAplicarCuandoSuelte || mantenerEstadoBarraAlRestaurarDesdePocket)
            {
                barraPendienteAplicarCuandoSuelte = false;
                AplicarEstadoVisualBarraYMiradaActual(true, true, true);
            }
            else if (hacerQueTutorialMireAlJugador)
            {
                RotarTutorialHaciaJugador();
            }
        }

        if (!DebeProcesarAccionInteractiva(panel))
        {
            return;
        }

        accionPanelSoltado = true;

        DebugLog("TUTORIAL: panel soltado.");

        RevisarSiAccionInteractivaCompletada();
    }

    public bool NotificarBotonUIClicado(Button boton)
    {
        if (boton == null)
        {
            return false;
        }

        if (!tutorialActivo || tutorialFinalizado)
        {
            return false;
        }

        bool esBotonIniciar = EsBotonIniciarTutorial(boton);

        bool esperaSeleccion =
            esperandoAccionInteractiva &&
            (accionInteractivaActual == AccionTutorialInteractiva.Ninguna ||
             accionInteractivaActual == AccionTutorialInteractiva.SeleccionarBotonNoIniciar);

        if (esperaSeleccion)
        {
            if (esBotonIniciar)
            {
                DebugLog("TUTORIAL: click en Iniciar ignorado durante la espera de seleccion.");
                return true;
            }

            if (!completarSeleccionSiPresionaBotonNoIniciar)
            {
                return true;
            }

            if (!string.IsNullOrWhiteSpace(textoFelicitacionBotonNoIniciar))
            {
                CambiarInstruccion(textoFelicitacionBotonNoIniciar);
            }

            DebugLog("TUTORIAL: boton valido seleccionado: " + ObtenerTextoBotonTutorial(boton));

            CompletarAccionInteractiva();
            return true;
        }

        if (esBotonIniciar && cerrarTutorialSiPresionaBotonIniciar)
        {
            DebugLog("TUTORIAL: se presionó Iniciar. Se oculta el tutorial.");

            OcultarTutorialPorBotonIniciar();
            return false;
        }

        if (!completarSeleccionSiPresionaBotonNoIniciar)
        {
            return false;
        }

        if (!esperandoAccionInteractiva)
        {
            return false;
        }

        return false;
    }

    public void OcultarTutorialPorBotonIniciar()
    {
        if (cerrandoTutorialPorBotonIniciar)
        {
            return;
        }

        cerrandoTutorialPorBotonIniciar = true;

        ReiniciarEstadoInteractivo();
        OcultarTodosLosIndicadoresGatillosInstantaneo();

        DetenerAudioActual();
        DetenerVideoActual();

        if (mandoController != null)
        {
            mandoController.OcultarPanelMando();
        }

        if (alPresionarBotonIniciarDuranteTutorial != null)
        {
            alPresionarBotonIniciarDuranteTutorial.Invoke();
        }

        if (tutorialActivo && !tutorialFinalizado)
        {
            OmitirTutorial();
            cerrandoTutorialPorBotonIniciar = false;
            return;
        }

        CerrarPanel();
    }

    private bool EsBotonIniciarTutorial(Button boton)
    {
        if (boton == null)
        {
            return false;
        }

        string textoCompleto = (
            boton.name + " " +
            boton.gameObject.name + " " +
            ObtenerTextoBotonTutorial(boton)
        ).ToLower();

        if (palabrasBotonIniciar == null || palabrasBotonIniciar.Length == 0)
        {
            return textoCompleto.Contains("iniciar") ||
                   textoCompleto.Contains("start") ||
                   textoCompleto.Contains("comenzar") ||
                   textoCompleto.Contains("empezar");
        }

        for (int i = 0; i < palabrasBotonIniciar.Length; i++)
        {
            string palabra = palabrasBotonIniciar[i];

            if (string.IsNullOrWhiteSpace(palabra))
            {
                continue;
            }

            if (textoCompleto.Contains(palabra.Trim().ToLower()))
            {
                return true;
            }
        }

        return false;
    }

    private string ObtenerTextoBotonTutorial(Button boton)
    {
        if (boton == null)
        {
            return "";
        }

        TMP_Text tmp = boton.GetComponentInChildren<TMP_Text>(true);

        if (tmp != null)
        {
            return tmp.text.Trim();
        }

        Text textoNormal = boton.GetComponentInChildren<Text>(true);

        if (textoNormal != null)
        {
            return textoNormal.text.Trim();
        }

        return boton.name.Trim();
    }

    private bool EsGrabHandleDelTutorialPocket(AlgoLabPanelGrabHandle panel)
    {
        if (panel == null)
        {
            return false;
        }

        PrepararGrabHandleTutorialPocket();

        if (grabHandleTutorialPocket != null && panel == grabHandleTutorialPocket)
        {
            return true;
        }

        if (barraInferior != null)
        {
            Transform panelTransform = panel.transform;

            if (panelTransform == barraInferior ||
                panelTransform.IsChildOf(barraInferior) ||
                barraInferior.IsChildOf(panelTransform))
            {
                return true;
            }
        }

        if (rootParaUbicar != null && panel.panelRoot == rootParaUbicar)
        {
            return true;
        }

        return false;
    }

    private bool DebeProcesarAccionInteractiva(AlgoLabPanelGrabHandle panel)
    {
        if (!tutorialActivo || tutorialFinalizado)
        {
            return false;
        }

        if (!permitirTutorialInteractivo)
        {
            return false;
        }

        if (!esperandoAccionInteractiva)
        {
            return false;
        }

        if (eventoInteractivoActual == null)
        {
            return false;
        }

        if (eventoInteractivoActual.panelEsperado != null &&
            eventoInteractivoActual.panelEsperado != panel)
        {
            return false;
        }

        return true;
    }

    private bool DebeProcesarAccionInteractivaPanelOpciones()
    {
        if (!tutorialActivo || tutorialFinalizado)
        {
            return false;
        }

        if (!permitirTutorialInteractivo || !esperandoAccionInteractiva || eventoInteractivoActual == null)
        {
            return false;
        }

        if (!EstaEsperandoAccionPanelOpcionesInteractiva())
        {
            return false;
        }

        if (eventoInteractivoActual.panelEsperado != null &&
            !EsGrabHandleDelTutorialPocket(eventoInteractivoActual.panelEsperado))
        {
            return false;
        }

        return true;
    }

    private bool DebeProcesarAccionInteractivaPanelOpciones(AlgoLabPocketPanelItem panel)
    {
        if (!tutorialActivo || tutorialFinalizado)
        {
            return false;
        }

        if (!permitirTutorialInteractivo || !esperandoAccionInteractiva || eventoInteractivoActual == null)
        {
            return false;
        }

        if (!EstaEsperandoAccionPanelOpcionesInteractiva())
        {
            return false;
        }

        if (panel == null || !panel.puedeGuardarse || panel.esPanelPrincipal)
        {
            return false;
        }

        if (eventoInteractivoActual.panelEsperado != null &&
            !PanelPocketCoincideConGrabHandle(panel, eventoInteractivoActual.panelEsperado))
        {
            return false;
        }

        return true;
    }

    private bool PanelPocketCoincideConGrabHandle(AlgoLabPocketPanelItem panel, AlgoLabPanelGrabHandle grabHandle)
    {
        if (panel == null || grabHandle == null)
        {
            return false;
        }

        Transform rootPanel = panel.ObtenerPanelRoot();
        Transform rootGrab = grabHandle.panelRoot != null ? grabHandle.panelRoot : grabHandle.transform;

        if (rootPanel == null || rootGrab == null)
        {
            return false;
        }

        if (rootPanel == rootGrab)
        {
            return true;
        }

        Transform grabTransform = grabHandle.transform;
        return grabTransform != null &&
               (grabTransform == rootPanel || grabTransform.IsChildOf(rootPanel));
    }

    private bool EstaEsperandoAccionPanelOpcionesInteractiva()
    {
        if (!esperandoAccionInteractiva)
        {
            return false;
        }

        return accionInteractivaActual == AccionTutorialInteractiva.MeterPanelEnArco ||
               accionInteractivaActual == AccionTutorialInteractiva.SacarPanelDelArco ||
               accionInteractivaActual == AccionTutorialInteractiva.MeterYSacarPanelDelArco ||
               accionInteractivaActual == AccionTutorialInteractiva.SacarYMeterPanelDelArco;
    }

    private void RegistrarAccionPanelEnArco(bool metidoEnArco)
    {
        RegistrarAccionPanelEnArco(metidoEnArco, null);
    }

    private void RegistrarAccionPanelEnArco(bool metidoEnArco, AlgoLabPocketPanelItem panel)
    {
        bool puedeProcesar = panel != null
            ? DebeProcesarAccionInteractivaPanelOpciones(panel)
            : DebeProcesarAccionInteractivaPanelOpciones();

        if (!puedeProcesar)
        {
            return;
        }

        if (metidoEnArco)
        {
            accionPanelMetidoEnArco = true;

            if (accionPanelSacadoDelArco)
            {
                secuenciaPanelSacarYMeterDelArco = true;
            }

            DebugLog("TUTORIAL: panel metido en panel de opciones.");
        }
        else
        {
            accionPanelSacadoDelArco = true;

            if (accionPanelMetidoEnArco)
            {
                secuenciaPanelMeterYSacarDelArco = true;
            }

            DebugLog("TUTORIAL: panel sacado del panel de opciones.");
        }

        RevisarSiAccionInteractivaCompletada();
    }

    private void RevisarSiAccionInteractivaCompletada()
    {
        if (!esperandoAccionInteractiva)
        {
            return;
        }

        bool completado = false;

        switch (accionInteractivaActual)
        {
            case AccionTutorialInteractiva.AgarrarPanel:
                completado = accionPanelAgarrado;
                break;

            case AccionTutorialInteractiva.MoverPanel:
                completado = accionPanelMovido;
                break;

            case AccionTutorialInteractiva.SoltarPanel:
                completado = accionPanelSoltado;
                break;

            case AccionTutorialInteractiva.AgarrarMoverSoltarPanel:
                completado =
                    accionPanelAgarrado &&
                    accionPanelMovido &&
                    accionPanelSoltado;
                break;

            case AccionTutorialInteractiva.SeleccionarBotonNoIniciar:
                // Esta acción se completa desde NotificarBotonUIClicado().
                completado = false;
                break;

            case AccionTutorialInteractiva.MeterPanelEnArco:
                completado = accionPanelMetidoEnArco;
                break;

            case AccionTutorialInteractiva.SacarPanelDelArco:
                completado = accionPanelSacadoDelArco;
                break;

            case AccionTutorialInteractiva.MeterYSacarPanelDelArco:
                completado = secuenciaPanelMeterYSacarDelArco;
                break;

            case AccionTutorialInteractiva.SacarYMeterPanelDelArco:
                completado = secuenciaPanelSacarYMeterDelArco;
                break;
        }

        if (completado)
        {
            CompletarAccionInteractiva();
        }
    }

    public void CompletarAccionInteractiva()
    {
        if (!esperandoAccionInteractiva)
        {
            return;
        }

        DebugLog("TUTORIAL: acción interactiva completada.");

        AccionTutorialInteractiva accionCompletada = accionInteractivaActual;

        esperandoAccionInteractiva = false;
        accionInteractivaActual = AccionTutorialInteractiva.Ninguna;
        eventoInteractivoActual = null;
        tiempoRealInicioEsperaAccion = -999f;

        accionPanelAgarrado = false;
        accionPanelMovido = false;
        accionPanelSoltado = false;
        accionPanelMetidoEnArco = false;
        accionPanelSacadoDelArco = false;
        secuenciaPanelMeterYSacarDelArco = false;
        secuenciaPanelSacarYMeterDelArco = false;

        if (ocultarIndicadoresAlCompletarInteraccion)
        {
            OcultarTodosLosIndicadoresGatillosInstantaneo();
        }

        if (detenerVideoAlCompletarSeleccionBotonNoIniciar &&
            accionCompletada == AccionTutorialInteractiva.SeleccionarBotonNoIniciar)
        {
            PausarVideoActual();
        }

        if (esperarFinVideoActualAlCompletarAccion &&
            accionCompletada != AccionTutorialInteractiva.SeleccionarBotonNoIniciar &&
            tutorialVideoPlayer != null &&
            tutorialVideoPlayer.clip != null)
        {
            IniciarRutinaEsperaFinVideoActual(true);
            return;
        }

        AvanzarTimelineDespuesDeEspera();
    }

    private void IniciarEsperaFinVideoActual(EventoTutorial evento)
    {
        if (evento != null && !string.IsNullOrWhiteSpace(evento.texto))
        {
            CambiarInstruccion(evento.texto);
        }

        IniciarRutinaEsperaFinVideoActual(true);
    }

    private void IniciarRutinaEsperaFinVideoActual(bool avanzarAlTerminar)
    {
        if (rutinaEsperarFinVideoActual != null)
        {
            StopCoroutine(rutinaEsperarFinVideoActual);
            rutinaEsperarFinVideoActual = null;
        }

        generacionEsperaVideo++;
        int esperaActual = generacionEsperaVideo;
        int videoActual = generacionVideo;
        VideoClip clipActual = tutorialVideoPlayer != null ? tutorialVideoPlayer.clip : null;

        rutinaEsperarFinVideoActual = StartCoroutine(
            EsperarFinVideoActualRutina(
                avanzarAlTerminar,
                esperaActual,
                videoActual,
                clipActual
            )
        );
    }

    private IEnumerator EsperarFinVideoActualRutina(
        bool avanzarAlTerminar,
        int esperaEsperada,
        int videoEsperado,
        VideoClip clipEsperado
    )
    {
        esperandoFinVideoActual = true;
        tiempoRealInicioEsperaVideo = Time.unscaledTime;

        if (tutorialVideoPlayer != null && clipEsperado != null)
        {
            if (quitarLoopVideoActualAlCompletarAccion)
            {
                tutorialVideoPlayer.isLooping = false;
            }

            float tiempoEsperandoInicio = 0f;
            float tiempoLimiteInicio = Mathf.Max(
                0.1f,
                tiempoMaximoEsperarInicioVideo,
                tiempoMaximoPrepararVideo
            );

            while (tutorialVideoPlayer != null &&
                   generacionEsperaVideo == esperaEsperada &&
                   generacionVideo == videoEsperado &&
                   tutorialVideoPlayer.clip == clipEsperado &&
                   !tutorialVideoPlayer.isPlaying &&
                   tiempoEsperandoInicio < tiempoLimiteInicio)
            {
                tiempoEsperandoInicio += Time.unscaledDeltaTime;
                yield return null;
            }

            float tiempoEsperandoFin = 0f;
            float tiempoMaximoFin = ObtenerTiempoMaximoEsperaFinVideoActual();

            while (tutorialVideoPlayer != null &&
                   generacionEsperaVideo == esperaEsperada &&
                   generacionVideo == videoEsperado &&
                   tutorialVideoPlayer.clip == clipEsperado &&
                   tutorialVideoPlayer.isPlaying &&
                   tiempoEsperandoFin < tiempoMaximoFin)
            {
                tiempoEsperandoFin += Time.unscaledDeltaTime;
                yield return null;
            }

            bool videoSigueSiendoElEsperado =
                tutorialVideoPlayer != null &&
                generacionVideo == videoEsperado &&
                tutorialVideoPlayer.clip == clipEsperado;

            if (videoSigueSiendoElEsperado &&
                tutorialVideoPlayer.isPlaying)
            {
                DebugLog("TUTORIAL: video detenido por seguridad para evitar bloqueo.");
            }

            if (detenerVideoActualDespuesDeEsperar && videoSigueSiendoElEsperado)
            {
                tutorialVideoPlayer.Stop();
            }
        }

        if (generacionEsperaVideo != esperaEsperada)
        {
            yield break;
        }

        esperandoFinVideoActual = false;
        rutinaEsperarFinVideoActual = null;
        tiempoRealInicioEsperaVideo = -999f;

        if (avanzarAlTerminar)
        {
            AvanzarTimelineDespuesDeEspera();
        }
    }

    private float ObtenerTiempoMaximoEsperaFinVideoActual()
    {
        float maximo = Mathf.Max(1f, tiempoMaximoEsperaFinVideo);

        if (tutorialVideoPlayer != null &&
            tutorialVideoPlayer.clip != null &&
            tutorialVideoPlayer.clip.length > 0.01)
        {
            maximo = Mathf.Max(
                maximo,
                (float)tutorialVideoPlayer.clip.length + Mathf.Max(0f, margenExtraEsperaFinVideo)
            );
        }

        return maximo;
    }

    private void AvanzarTimelineDespuesDeEspera()
    {
        if (continuarAutomaticamenteAlCompletarAccion)
        {
            tiempoTutorial += Mathf.Max(0.01f, avanceTiempoAlCompletarAccion);
        }
    }

    private void ReiniciarEstadoInteractivo()
    {
        generacionEsperaVideo++;
        esperandoAccionInteractiva = false;
        esperandoFinVideoActual = false;
        tiempoRealInicioEsperaAccion = -999f;
        tiempoRealInicioEsperaVideo = -999f;

        if (rutinaEsperarFinVideoActual != null)
        {
            StopCoroutine(rutinaEsperarFinVideoActual);
            rutinaEsperarFinVideoActual = null;
        }

        accionInteractivaActual = AccionTutorialInteractiva.Ninguna;
        eventoInteractivoActual = null;

        accionPanelAgarrado = false;
        accionPanelMovido = false;
        accionPanelSoltado = false;
        accionPanelMetidoEnArco = false;
        accionPanelSacadoDelArco = false;
        secuenciaPanelMeterYSacarDelArco = false;
        secuenciaPanelSacarYMeterDelArco = false;
    }

    private void RevisarIndicadoresGatillosAntesDeTiempo()
    {
        if (!usarIndicadoresGatillos)
        {
            return;
        }

        if (eventos == null || eventos.Count == 0)
        {
            return;
        }

        for (int i = 0; i < eventos.Count; i++)
        {
            EventoTutorial evento = eventos[i];

            if (evento == null || evento.ejecutado || evento.indicadorGatilloMostrado)
            {
                continue;
            }

            bool esPresionarPrincipal =
                evento.tipoEvento == TipoEventoTutorial.PresionarGatilloPrincipal ||
                evento.tipoEvento == TipoEventoTutorial.ForzarMantenerGatilloPrincipal;

            bool esPresionarSecundario =
                evento.tipoEvento == TipoEventoTutorial.PresionarGatilloSecundario ||
                evento.tipoEvento == TipoEventoTutorial.ForzarMantenerGatilloSecundario;

            if (!esPresionarPrincipal && !esPresionarSecundario)
            {
                continue;
            }

            float tiempoRestante = evento.tiempo - tiempoTutorial;

            if (tiempoRestante < 0f)
            {
                continue;
            }

            if (tiempoRestante <= segundosAntesDePresionarGatillo)
            {
                evento.indicadorGatilloMostrado = true;

                if (esPresionarPrincipal)
                {
                    MostrarCirculoGatilloPrincipalSmooth();
                }
                else if (esPresionarSecundario)
                {
                    MostrarCirculoGatilloSecundarioSmooth();
                }
            }
        }
    }

    private void MostrarCirculoGatilloPrincipalSmooth()
    {
        if (!usarIndicadoresGatillos || circuloGatilloPrincipal == null)
        {
            return;
        }

        if (rutinaOcultarCirculoPrincipalDespues != null)
        {
            StopCoroutine(rutinaOcultarCirculoPrincipalDespues);
            rutinaOcultarCirculoPrincipalDespues = null;
        }

        if (rutinaCirculoPrincipal != null)
        {
            StopCoroutine(rutinaCirculoPrincipal);
        }

        rutinaCirculoPrincipal = StartCoroutine(
            AnimarCirculoAlphaRutina(
                circuloGatilloPrincipal,
                canvasGroupCirculoPrincipal,
                escalaOriginalCirculoPrincipal,
                true
            )
        );
    }

    private void MostrarCirculoGatilloSecundarioSmooth()
    {
        if (!usarIndicadoresGatillos || circuloGatilloSecundario == null)
        {
            return;
        }

        if (rutinaOcultarCirculoSecundarioDespues != null)
        {
            StopCoroutine(rutinaOcultarCirculoSecundarioDespues);
            rutinaOcultarCirculoSecundarioDespues = null;
        }

        if (rutinaCirculoSecundario != null)
        {
            StopCoroutine(rutinaCirculoSecundario);
        }

        rutinaCirculoSecundario = StartCoroutine(
            AnimarCirculoAlphaRutina(
                circuloGatilloSecundario,
                canvasGroupCirculoSecundario,
                escalaOriginalCirculoSecundario,
                true
            )
        );
    }

    private void OcultarCirculoGatilloPrincipalDespues()
    {
        if (!usarIndicadoresGatillos || circuloGatilloPrincipal == null)
        {
            return;
        }

        if (rutinaOcultarCirculoPrincipalDespues != null)
        {
            StopCoroutine(rutinaOcultarCirculoPrincipalDespues);
        }

        rutinaOcultarCirculoPrincipalDespues = StartCoroutine(
            OcultarCirculoDespuesRutina(true)
        );
    }

    private void OcultarCirculoGatilloSecundarioDespues()
    {
        if (!usarIndicadoresGatillos || circuloGatilloSecundario == null)
        {
            return;
        }

        if (rutinaOcultarCirculoSecundarioDespues != null)
        {
            StopCoroutine(rutinaOcultarCirculoSecundarioDespues);
        }

        rutinaOcultarCirculoSecundarioDespues = StartCoroutine(
            OcultarCirculoDespuesRutina(false)
        );
    }

    private IEnumerator OcultarCirculoDespuesRutina(bool principal)
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, segundosDespuesDeSoltarGatillo));

        if (principal)
        {
            if (rutinaCirculoPrincipal != null)
            {
                StopCoroutine(rutinaCirculoPrincipal);
            }

            rutinaCirculoPrincipal = StartCoroutine(
                AnimarCirculoAlphaRutina(
                    circuloGatilloPrincipal,
                    canvasGroupCirculoPrincipal,
                    escalaOriginalCirculoPrincipal,
                    false
                )
            );

            rutinaOcultarCirculoPrincipalDespues = null;
        }
        else
        {
            if (rutinaCirculoSecundario != null)
            {
                StopCoroutine(rutinaCirculoSecundario);
            }

            rutinaCirculoSecundario = StartCoroutine(
                AnimarCirculoAlphaRutina(
                    circuloGatilloSecundario,
                    canvasGroupCirculoSecundario,
                    escalaOriginalCirculoSecundario,
                    false
                )
            );

            rutinaOcultarCirculoSecundarioDespues = null;
        }
    }

    private IEnumerator AnimarCirculoAlphaRutina(
        GameObject circulo,
        CanvasGroup canvasGroup,
        Vector3 escalaOriginal,
        bool mostrar
    )
    {
        if (circulo == null)
        {
            yield break;
        }

        RectTransform rect = circulo.GetComponent<RectTransform>();

        if (rect == null)
        {
            yield break;
        }

        if (mantenerEscalaOriginalCirculos)
        {
            rect.localScale = escalaOriginal;
        }

        if (mostrar)
        {
            circulo.SetActive(true);
        }

        float duracion = mostrar
            ? Mathf.Max(0.01f, duracionAparecerCirculo)
            : Mathf.Max(0.01f, duracionDesaparecerCirculo);

        float alphaInicio = canvasGroup != null ? canvasGroup.alpha : (mostrar ? 0f : 1f);
        float alphaFinal = mostrar ? 1f : 0f;

        if (mostrar && canvasGroup != null && canvasGroup.alpha <= 0.001f)
        {
            alphaInicio = 0f;
            canvasGroup.alpha = 0f;
        }

        float tiempo = 0f;

        while (tiempo < duracion)
        {
            tiempo += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(tiempo / duracion);
            float smooth = Mathf.SmoothStep(0f, 1f, t);

            if (mantenerEscalaOriginalCirculos)
            {
                rect.localScale = escalaOriginal;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = Mathf.Lerp(alphaInicio, alphaFinal, smooth);
            }

            yield return null;
        }

        if (mantenerEscalaOriginalCirculos)
        {
            rect.localScale = escalaOriginal;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = alphaFinal;
        }

        if (!mostrar)
        {
            circulo.SetActive(false);
        }
    }

    private void OcultarTodosLosIndicadoresGatillosInstantaneo()
    {
        DetenerRutinasCirculos();

        OcultarIndicadorInstantaneo(
            circuloGatilloPrincipal,
            canvasGroupCirculoPrincipal,
            escalaOriginalCirculoPrincipal
        );

        OcultarIndicadorInstantaneo(
            circuloGatilloSecundario,
            canvasGroupCirculoSecundario,
            escalaOriginalCirculoSecundario
        );
    }

    private void OcultarIndicadorInstantaneo(
        GameObject circulo,
        CanvasGroup canvasGroup,
        Vector3 escalaOriginal
    )
    {
        if (circulo == null)
        {
            return;
        }

        RectTransform rect = circulo.GetComponent<RectTransform>();

        if (rect != null && mantenerEscalaOriginalCirculos)
        {
            rect.localScale = escalaOriginal;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        circulo.SetActive(false);
    }

    private void DetenerRutinasCirculos()
    {
        if (rutinaCirculoPrincipal != null)
        {
            StopCoroutine(rutinaCirculoPrincipal);
            rutinaCirculoPrincipal = null;
        }

        if (rutinaCirculoSecundario != null)
        {
            StopCoroutine(rutinaCirculoSecundario);
            rutinaCirculoSecundario = null;
        }

        if (rutinaOcultarCirculoPrincipalDespues != null)
        {
            StopCoroutine(rutinaOcultarCirculoPrincipalDespues);
            rutinaOcultarCirculoPrincipalDespues = null;
        }

        if (rutinaOcultarCirculoSecundarioDespues != null)
        {
            StopCoroutine(rutinaOcultarCirculoSecundarioDespues);
            rutinaOcultarCirculoSecundarioDespues = null;
        }
    }


    private void AplicarEstadoInicialImagenesPanelesTutorial()
    {
        if (!ocultarImagenesPanelesAlIniciarTutorial)
        {
            if (mostrarImagenPanelPrincipalAlIniciarTutorial)
            {
                SetPanelImagenActiva(imagenPanelPrincipal, true);
            }

            return;
        }

        SetPanelImagenActiva(imagenPanelDiagramas, false);
        SetPanelImagenActiva(imagenPanelIA, false);

        SetPanelImagenActiva(
            imagenPanelPrincipal,
            mostrarImagenPanelPrincipalAlIniciarTutorial
        );
    }

    public void MostrarImagenPanelPrincipal()
    {
        PrepararMostrarImagenEstaticaPanel();
        SetPanelImagenActiva(imagenPanelPrincipal, true);
    }

    public void OcultarImagenPanelPrincipal()
    {
        SetPanelImagenActiva(imagenPanelPrincipal, false);
    }

    public void MostrarImagenPanelDiagramas()
    {
        PrepararMostrarImagenEstaticaPanel();
        SetPanelImagenActiva(imagenPanelDiagramas, true);
    }

    public void OcultarImagenPanelDiagramas()
    {
        SetPanelImagenActiva(imagenPanelDiagramas, false);
    }

    public void MostrarImagenPanelIA()
    {
        PrepararMostrarImagenEstaticaPanel();
        SetPanelImagenActiva(imagenPanelIA, true);
    }

    public void OcultarImagenPanelIA()
    {
        SetPanelImagenActiva(imagenPanelIA, false);
    }

    public void OcultarImagenesPanelesTutorial()
    {
        SetPanelImagenActiva(imagenPanelPrincipal, false);
        SetPanelImagenActiva(imagenPanelDiagramas, false);
        SetPanelImagenActiva(imagenPanelIA, false);
    }

    private void OcultarImagenesPanelesTutorialSiCorresponde()
    {
        if (!ocultarImagenesPanelesAlCerrarTutorial)
        {
            return;
        }

        OcultarImagenesPanelesTutorial();
    }

    private void SetPanelImagenActiva(GameObject panelImagen, bool activo)
    {
        if (panelImagen == null)
        {
            return;
        }

        if (panelImagen.activeSelf != activo)
        {
            panelImagen.SetActive(activo);
        }
    }

    private void PrepararMostrarImagenEstaticaPanel()
    {
        if (!detenerVideoAlMostrarImagenPanel)
        {
            return;
        }

        DetenerVideoActualYLimpiarRender();
    }

    private void ActivarTutorialVisual()
    {
        if (activarPadresAlIniciarTutorial)
        {
            List<Transform> jerarquia = new List<Transform>();
            Transform actual = transform;

            while (actual != null)
            {
                jerarquia.Add(actual);
                actual = actual.parent;
            }

            for (int i = jerarquia.Count - 1; i >= 0; i--)
            {
                if (jerarquia[i] != null && !jerarquia[i].gameObject.activeSelf)
                {
                    jerarquia[i].gameObject.SetActive(true);
                }
            }
        }
        else if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        if (panelRoot != null && !panelRoot.gameObject.activeSelf)
        {
            panelRoot.gameObject.SetActive(true);
        }
    }

    private void DesactivarTutorialVisualSiCorresponde()
    {
        if (!desactivarGameObjectCuandoEstaOculto)
        {
            return;
        }

        gameObject.SetActive(false);
    }

    public void MostrarPanel()
    {
        if (rutinaPanel != null)
        {
            StopCoroutine(rutinaPanel);
        }

        ActivarTutorialVisual();
        rutinaPanel = StartCoroutine(MostrarPanelRutina());
    }

    private IEnumerator MostrarPanelRutina()
    {
        if (panelRoot == null)
        {
            rutinaPanel = null;
            yield break;
        }

        panelRoot.localScale = escalaOriginalPanel * escalaInicialPanel;

        float tiempo = 0f;
        float duracion = Mathf.Max(0.01f, duracionMostrarPanel);

        while (tiempo < duracion)
        {
            tiempo += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(tiempo / duracion);
            float smooth = Mathf.SmoothStep(0f, 1f, t);

            panelRoot.localScale = Vector3.Lerp(
                escalaOriginalPanel * escalaInicialPanel,
                escalaVisiblePanel,
                smooth
            );

            yield return null;
        }

        panelRoot.localScale = escalaVisiblePanel;
        rutinaPanel = null;
    }

    public void CerrarPanel()
    {
        if (rutinaPanel != null)
        {
            StopCoroutine(rutinaPanel);
        }

        rutinaPanel = StartCoroutine(CerrarPanelRutina());
    }

    private IEnumerator CerrarPanelRutina()
    {
        ReiniciarEstadoInteractivo();
        AjustarBarraInferior(false, true);
        OcultarTodosLosIndicadoresGatillosInstantaneo();
        OcultarImagenesPanelesTutorialSiCorresponde();

        DetenerAudioActual();
        DetenerVideoActual();

        if (mandoController != null)
        {
            mandoController.OcultarPanelMando();
        }

        if (panelRoot == null)
        {
            tutorialActivo = false;
            tutorialFinalizado = true;
            cerrandoTutorialPorBotonIniciar = false;
            tutorialVisibleRestauradoDesdePocket = false;
            rutinaPanel = null;

            if (desactivarDespuesDeCerrarPanel)
            {
                DesactivarTutorialVisualSiCorresponde();
            }

            yield break;
        }

        Vector3 escalaInicio = panelRoot.localScale;
        Vector3 escalaFinal = escalaOriginalPanel * escalaInicialPanel;

        float tiempo = 0f;
        float duracion = Mathf.Max(0.01f, duracionCerrarPanel);

        while (tiempo < duracion)
        {
            tiempo += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(tiempo / duracion);
            float smooth = Mathf.SmoothStep(0f, 1f, t);

            panelRoot.localScale = Vector3.Lerp(escalaInicio, escalaFinal, smooth);

            yield return null;
        }

        panelRoot.localScale = escalaFinal;

        ReiniciarPanelVisual();

        tutorialActivo = false;
        tutorialFinalizado = true;
        cerrandoTutorialPorBotonIniciar = false;
        tutorialVisibleRestauradoDesdePocket = false;

        rutinaPanel = null;

        if (desactivarDespuesDeCerrarPanel)
        {
            DesactivarTutorialVisualSiCorresponde();
        }
    }

    public void OcultarPanelInstantaneo()
    {
        ReiniciarEstadoInteractivo();
        AjustarBarraInferior(false, true);
        OcultarTodosLosIndicadoresGatillosInstantaneo();
        OcultarImagenesPanelesTutorialSiCorresponde();

        DetenerAudioActual();
        DetenerVideoActual();

        if (panelRoot != null)
        {
            panelRoot.localScale = escalaOriginalPanel * escalaInicialPanel;
        }

        if (mandoController != null)
        {
            mandoController.OcultarInstantaneo();
        }

        tutorialActivo = false;
        tutorialFinalizado = true;

        ReiniciarPanelVisual();

        DesactivarTutorialVisualSiCorresponde();
    }

    private void ReiniciarPanelVisual()
    {
        if (introBlackPanel != null)
        {
            introBlackPanel.gameObject.SetActive(true);
            introBlackPanel.anchoredPosition = posicionInicialIntroBlackPanel;
        }

        if (tituloTutorialRect != null)
        {
            tituloTutorialRect.gameObject.SetActive(true);
            tituloTutorialRect.anchoredPosition = posicionInicialTitulo;
        }

        if (tituloTutorialText != null)
        {
            tituloTutorialText.gameObject.SetActive(true);
            tituloTutorialText.text = nombreTutorial;
        }
    }

    public void RevelarVideo()
    {
        if (rutinaRevelarVideo != null)
        {
            StopCoroutine(rutinaRevelarVideo);
        }

        rutinaRevelarVideo = StartCoroutine(RevelarVideoRutina());
    }

    private IEnumerator RevelarVideoRutina()
    {
        Vector2 inicioIntro = introBlackPanel != null
            ? introBlackPanel.anchoredPosition
            : Vector2.zero;

        Vector2 destinoIntro = inicioIntro;

        if (introBlackPanel != null)
        {
            introBlackPanel.gameObject.SetActive(true);

            float salidaY = introBlackPanel.rect.height + margenSalidaIntro;
            destinoIntro = posicionInicialIntroBlackPanel + new Vector2(0f, salidaY);
        }

        Vector2 inicioTitulo = tituloTutorialRect != null
            ? tituloTutorialRect.anchoredPosition
            : Vector2.zero;

        Vector2 destinoTitulo = inicioTitulo;

        if (tituloTutorialRect != null)
        {
            tituloTutorialRect.gameObject.SetActive(true);

            if (ocultarTituloAlRevelar)
            {
                if (calcularSalidaTituloAutomaticamente)
                {
                    float altoReferencia = 0f;

                    if (tutorialMainPanel != null)
                    {
                        altoReferencia = tutorialMainPanel.rect.height;
                    }
                    else if (introBlackPanel != null)
                    {
                        altoReferencia = introBlackPanel.rect.height;
                    }
                    else
                    {
                        altoReferencia = Mathf.Abs(tituloYFuera);
                    }

                    destinoTitulo = posicionInicialTitulo +
                                    Vector2.up * (altoReferencia + margenSalidaTitulo);
                }
                else
                {
                    destinoTitulo = new Vector2(
                        posicionInicialTitulo.x,
                        tituloYFuera
                    );
                }
            }
            else
            {
                destinoTitulo = posicionTituloArriba;
            }
        }

        float tiempo = 0f;
        float duracion = Mathf.Max(0.01f, duracionRevelarVideo);

        while (tiempo < duracion)
        {
            tiempo += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(tiempo / duracion);
            float smooth = Mathf.SmoothStep(0f, 1f, t);

            if (introBlackPanel != null)
            {
                introBlackPanel.anchoredPosition = Vector2.Lerp(
                    inicioIntro,
                    destinoIntro,
                    smooth
                );
            }

            if (tituloTutorialRect != null)
            {
                tituloTutorialRect.anchoredPosition = Vector2.Lerp(
                    inicioTitulo,
                    destinoTitulo,
                    smooth
                );
            }

            yield return null;
        }

        if (introBlackPanel != null)
        {
            introBlackPanel.anchoredPosition = destinoIntro;
        }

        if (tituloTutorialRect != null)
        {
            tituloTutorialRect.anchoredPosition = destinoTitulo;

            if (ocultarTituloAlRevelar &&
                desactivarTituloAlTerminarRevelado)
            {
                tituloTutorialRect.gameObject.SetActive(false);
            }
        }

        rutinaRevelarVideo = null;
    }

    public void CambiarInstruccion(string texto)
    {
        if (instruccionesText != null)
        {
            instruccionesText.gameObject.SetActive(true);
            instruccionesText.text = texto;
        }
    }

    public void OcultarInstruccion()
    {
        if (instruccionesText != null)
        {
            instruccionesText.gameObject.SetActive(false);
        }
    }

    private AudioClip ObtenerAudioClipParaEvento(EventoTutorial evento)
    {
        if (!DebeReemplazarAudioElemento2PorAudio21(evento))
        {
            return evento != null ? evento.audioClip : null;
        }

        reemplazarProximoAudioElemento2PorAudio21 = false;

        if (audio21VolverIntentarlo == null)
        {
            DebugLog("Tutorial: audio 21 no asignado. Se usa el audio original del evento.");
            return evento.audioClip;
        }

        DebugLog("Tutorial: se reemplaza el audio del elemento " + indiceEventoAudioReemplazablePanelOpciones + " por audio 21.");
        return audio21VolverIntentarlo;
    }

    private bool DebeReemplazarAudioElemento2PorAudio21(EventoTutorial evento)
    {
        if (!reemplazarAudioElemento2PorAudio21AlVolverDesdePanelOpciones ||
            !reemplazarProximoAudioElemento2PorAudio21 ||
            evento == null ||
            evento.tipoEvento != TipoEventoTutorial.ReproducirAudioClip ||
            eventos == null)
        {
            return false;
        }

        int indice = eventos.IndexOf(evento);

        if (indice != indiceEventoAudioReemplazablePanelOpciones)
        {
            return false;
        }

        return true;
    }

    public void ReproducirAudioClip(AudioClip clip)
    {
        PrepararReferencias();

        if (clip == null)
        {
            DebugLog("Audio tutorial no reproducido: clip vacio.");
            return;
        }

        if (audioSource == null)
        {
            DebugLog("Audio tutorial no reproducido: falta AudioSource.");
            return;
        }

        if (!audioSource.enabled)
        {
            audioSource.enabled = true;
        }

        audioSource.playOnAwake = false;
        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.mute = estaSilenciado;
        audioSource.Play();

        DebugLog("Audio tutorial: " + clip.name);
    }

    public void DetenerAudioActual()
    {
        if (audioSource != null)
        {
            audioSource.Stop();
            audioSource.clip = null;
        }
    }

    public void SilenciarTutorial()
    {
        estaSilenciado = true;

        if (audioSource != null)
        {
            audioSource.mute = true;
        }

        ActualizarIconoMute();
    }

    public void ActivarSonidoTutorial()
    {
        estaSilenciado = false;

        if (audioSource != null)
        {
            audioSource.mute = false;
        }

        ActualizarIconoMute();
    }

    public void AlternarSilencioTutorial()
    {
        if (estaSilenciado)
        {
            ActivarSonidoTutorial();
        }
        else
        {
            SilenciarTutorial();
        }
    }

    private void ActualizarIconoMute()
    {
        if (muteIconImage != null)
        {
            if (estaSilenciado && iconoSonidoSilenciado != null)
            {
                muteIconImage.sprite = iconoSonidoSilenciado;
            }
            else if (!estaSilenciado && iconoSonidoActivo != null)
            {
                muteIconImage.sprite = iconoSonidoActivo;
            }
        }

        if (muteButtonText != null)
        {
            muteButtonText.text = estaSilenciado
                ? textoSonidoSilenciado
                : textoSonidoActivo;
        }
    }

    public void ReproducirVideoClip(
        VideoClip clip,
        bool repetir,
        float duracionReproduccion,
        bool reiniciarDesdeInicio
    )
    {
        PrepararReferencias();

        if (tutorialVideoPlayer == null || clip == null)
        {
            DebugLog("Video tutorial no reproducido: falta VideoPlayer o clip.");
            return;
        }

        if (ocultarImagenesPanelesAlReproducirVideo)
        {
            OcultarImagenesPanelesTutorial();
        }

        int videoActual = IniciarNuevaGeneracionVideo();

        tutorialVideoPlayer.Stop();
        tutorialVideoPlayer.clip = clip;
        tutorialVideoPlayer.isLooping = repetir;

        if (reiniciarDesdeInicio)
        {
            tutorialVideoPlayer.time = 0;
            tutorialVideoPlayer.frame = 0;
        }

        tutorialVideoPlayer.Prepare();

        rutinaPrepararVideo = StartCoroutine(
            ReproducirVideoPreparadoRutina(clip, duracionReproduccion, videoActual)
        );

        DebugLog("Video tutorial: " + clip.name);
    }

    private IEnumerator ReproducirVideoPreparadoRutina(
        VideoClip clipEsperado,
        float duracionReproduccion,
        int videoEsperado
    )
    {
        float tiempoPreparando = 0f;

        while (tutorialVideoPlayer != null &&
               generacionVideo == videoEsperado &&
               tutorialVideoPlayer.clip == clipEsperado &&
               !tutorialVideoPlayer.isPrepared &&
               tiempoPreparando < Mathf.Max(0.1f, tiempoMaximoPrepararVideo))
        {
            tiempoPreparando += Time.unscaledDeltaTime;
            yield return null;
        }

        if (tutorialVideoPlayer == null ||
            generacionVideo != videoEsperado ||
            tutorialVideoPlayer.clip != clipEsperado)
        {
            yield break;
        }

        if (!tutorialVideoPlayer.isPrepared)
        {
            DebugLog("TUTORIAL: video no preparado a tiempo. Se omite reproduccion para evitar bloqueo.");
            rutinaPrepararVideo = null;
            yield break;
        }

        MostrarRenderVideoEnRawImage();
        tutorialVideoPlayer.Play();
        MostrarRenderVideoEnRawImage();
        rutinaPrepararVideo = null;

        if (duracionReproduccion > 0f)
        {
            rutinaVideoDuracion = StartCoroutine(
                DetenerVideoDespuesDeTiempo(duracionReproduccion, videoEsperado)
            );
        }
        else if (!tutorialVideoPlayer.isLooping && restaurarUltimaImagenEstaticaAlTerminarVideo)
        {
            rutinaRestaurarImagenAlTerminarVideo = StartCoroutine(
                RestaurarImagenAlTerminarVideoRutina(clipEsperado, videoEsperado)
            );
        }
    }

    private IEnumerator DetenerVideoDespuesDeTiempo(float duracion, int videoEsperado)
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, duracion));

        if (generacionVideo == videoEsperado)
        {
            rutinaVideoDuracion = null;
            DetenerVideoActual();
        }
    }

    private IEnumerator RestaurarImagenAlTerminarVideoRutina(
        VideoClip clipEsperado,
        int videoEsperado
    )
    {
        float tiempoEsperandoInicio = 0f;

        while (tutorialVideoPlayer != null &&
               generacionVideo == videoEsperado &&
               tutorialVideoPlayer.clip == clipEsperado &&
               !tutorialVideoPlayer.isPlaying &&
               tiempoEsperandoInicio < Mathf.Max(0.1f, tiempoMaximoEsperarInicioVideo))
        {
            tiempoEsperandoInicio += Time.unscaledDeltaTime;
            yield return null;
        }

        while (tutorialVideoPlayer != null &&
               generacionVideo == videoEsperado &&
               tutorialVideoPlayer.clip == clipEsperado &&
               tutorialVideoPlayer.isPlaying)
        {
            yield return null;
        }

        if (tutorialVideoPlayer != null &&
            generacionVideo == videoEsperado &&
            tutorialVideoPlayer.clip == clipEsperado)
        {
            tutorialVideoPlayer.Pause();
            RestaurarUltimaImagenEstaticaSiCorresponde();
            rutinaRestaurarImagenAlTerminarVideo = null;
        }
    }

    private int IniciarNuevaGeneracionVideo()
    {
        generacionVideo++;
        CancelarRutinasReproduccionVideo();
        return generacionVideo;
    }

    private void CancelarRutinasReproduccionVideo()
    {
        if (rutinaPrepararVideo != null)
        {
            StopCoroutine(rutinaPrepararVideo);
            rutinaPrepararVideo = null;
        }

        if (rutinaVideoDuracion != null)
        {
            StopCoroutine(rutinaVideoDuracion);
            rutinaVideoDuracion = null;
        }

        if (rutinaRestaurarImagenAlTerminarVideo != null)
        {
            StopCoroutine(rutinaRestaurarImagenAlTerminarVideo);
            rutinaRestaurarImagenAlTerminarVideo = null;
        }
    }

    public void DetenerVideoActual()
    {
        IniciarNuevaGeneracionVideo();

        if (tutorialVideoPlayer != null)
        {
            if (pausarVideoAlDetenerParaConservarFrame &&
                tutorialVideoPlayer.clip != null &&
                (tutorialVideoPlayer.isPlaying || tutorialVideoPlayer.isPrepared))
            {
                tutorialVideoPlayer.Pause();
            }
            else
            {
                tutorialVideoPlayer.Stop();
            }
        }

        if (limpiarRenderAlDetenerVideoActual)
        {
            LimpiarRenderVideoActual();
        }
    }

    private void DetenerVideoActualYLimpiarRender()
    {
        DetenerVideoActual();

        if (tutorialVideoPlayer != null)
        {
            tutorialVideoPlayer.Stop();
            tutorialVideoPlayer.clip = null;
        }

        LimpiarRenderVideoActual();
    }

    private void MostrarRenderVideoEnRawImage()
    {
        if (videoRawImage == null || tutorialRenderTexture == null)
        {
            return;
        }

        videoRawImage.gameObject.SetActive(true);
        videoRawImage.enabled = true;
        videoRawImage.texture = tutorialRenderTexture;
    }

    private void LimpiarRenderVideoActual()
    {
        if (videoRawImage != null &&
            (tutorialRenderTexture == null || videoRawImage.texture == tutorialRenderTexture))
        {
            videoRawImage.texture = null;
        }
    }

    private void RestaurarUltimaImagenEstaticaSiCorresponde()
    {
        if (!restaurarUltimaImagenEstaticaAlTerminarVideo ||
            ultimaImagenEstaticaTutorial == null ||
            videoRawImage == null)
        {
            return;
        }

        videoRawImage.gameObject.SetActive(true);
        videoRawImage.enabled = true;
        videoRawImage.texture = ultimaImagenEstaticaTutorial;
    }

    public void PausarVideoActual()
    {
        IniciarNuevaGeneracionVideo();

        if (tutorialVideoPlayer != null &&
            tutorialVideoPlayer.clip != null &&
            (tutorialVideoPlayer.isPlaying || tutorialVideoPlayer.isPrepared))
        {
            tutorialVideoPlayer.Pause();
        }
    }

    public void ReanudarVideoActual()
    {
        if (tutorialVideoPlayer == null ||
            tutorialVideoPlayer.clip == null ||
            tutorialVideoPlayer.isPlaying)
        {
            return;
        }

        int videoActual = IniciarNuevaGeneracionVideo();

        if (tutorialVideoPlayer.isPrepared)
        {
            tutorialVideoPlayer.Play();
            MostrarRenderVideoEnRawImage();
        }
        else
        {
            VideoClip clip = tutorialVideoPlayer.clip;
            tutorialVideoPlayer.Prepare();
            rutinaPrepararVideo = StartCoroutine(
                ReproducirVideoPreparadoRutina(clip, 0f, videoActual)
            );
        }
    }

    public void CambiarImagen(Texture imagen)
    {
        if (imagen == null || videoRawImage == null)
        {
            return;
        }

        if (detenerVideoAlMostrarImagenPanel)
        {
            DetenerVideoActualYLimpiarRender();
        }

        OcultarImagenesPanelesTutorial();
        ultimaImagenEstaticaTutorial = imagen;
        videoRawImage.gameObject.SetActive(true);
        videoRawImage.enabled = true;
        videoRawImage.texture = imagen;
    }

    private void BuscarPanelOpcionesManagerSiCorresponde()
    {
        if (!autoBuscarPanelOpcionesManager || panelOpcionesManager != null)
        {
            return;
        }

        panelOpcionesManager = AlgoLabPanelPocketManager.Instance;

        if (panelOpcionesManager == null)
        {
            panelOpcionesManager = FindFirstObjectByType<AlgoLabPanelPocketManager>(FindObjectsInactive.Include);
        }
    }

    private void BuscarGameAccessControllerSiCorresponde()
    {
        if (!activarPanelesDespuesDelTutorialPorCodigo || gameAccessController != null)
        {
            return;
        }

        gameAccessController = FindFirstObjectByType<AlgoLabGameAccessController>(FindObjectsInactive.Include);
    }

    private void AsegurarSalidaTutorial(bool mostrarPanelOpcionesTemporalmente)
    {
        if (activarPanelesDespuesDelTutorialPorCodigo)
        {
            BuscarGameAccessControllerSiCorresponde();

            if (gameAccessController != null)
            {
                gameAccessController.ActivarPanelesDespuesDelTutorial();
            }
        }

        if (spawnManager == null)
        {
            spawnManager = AlgoLabManualPanelSpawnManager.Instance;
        }

        if (spawnManager == null)
        {
            spawnManager = FindFirstObjectByType<AlgoLabManualPanelSpawnManager>(
                FindObjectsInactive.Include
            );
        }

        // Los paneles ya activos reciben su pose definitiva en este mismo frame.
        // Así la salida por doble A no depende de búsquedas o actualizaciones posteriores.
        if (spawnManager != null)
        {
            spawnManager.ReubicarPaneles();
        }

        if (!asegurarPanelOpcionesAlSalirDelTutorial)
        {
            return;
        }

        BuscarPanelOpcionesManagerSiCorresponde();

        if (panelOpcionesManager != null)
        {
            panelOpcionesManager.HabilitarPanelOpcionesTrasTutorial(mostrarPanelOpcionesTemporalmente);
        }
        else
        {
            HabilitarPanelOpciones();
        }
    }

    private void ProtegerObjetoTutorialDeAutoRegistro(GameObject objeto, bool activarPanelReal)
    {
        if (objeto == null)
        {
            return;
        }

        BuscarPanelOpcionesManagerSiCorresponde();

        if (panelOpcionesManager == null)
        {
            return;
        }

        panelOpcionesManager.PrepararPanelesDeObjetoParaControlTutorial(
            objeto,
            tiempoBloquearAutoRegistroObjetoTutorial,
            activarPanelReal
        );
    }

    private void PrepararReemplazoAudio21SiCorresponde(bool veniaDesdePanelOpciones)
    {
        if (!veniaDesdePanelOpciones ||
            !reemplazarAudioElemento2PorAudio21AlVolverDesdePanelOpciones)
        {
            return;
        }

        if (!tutorialVistoAlMenosUnaVez &&
            !tutorialOmitidoAlMenosUnaVez &&
            !tutorialOmitidoDesdePanelOpciones)
        {
            return;
        }

#if UNITY_EDITOR
        AutoAsignarAudio21VolverIntentarloEditor();
#endif
        reemplazarProximoAudioElemento2PorAudio21 = true;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        AutoAsignarAudio21VolverIntentarloEditor();
    }

    private void AutoAsignarAudio21VolverIntentarloEditor()
    {
        if (audio21VolverIntentarlo != null)
        {
            return;
        }

        audio21VolverIntentarlo = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(
            "Assets/__Algolab/_TutorialSystem/audios/tutorialPrincipioAudioArco/22volverIntentarlo.mp3"
        );
    }
#endif

    public void HabilitarPanelOpciones()
    {
        BuscarPanelOpcionesManagerSiCorresponde();

        if (panelOpcionesManager != null)
        {
            panelOpcionesManager.HabilitarPanelOpciones();
        }
    }

    public void DeshabilitarPanelOpciones()
    {
        BuscarPanelOpcionesManagerSiCorresponde();

        if (panelOpcionesManager != null)
        {
            panelOpcionesManager.DeshabilitarPanelOpciones();
        }
    }

    public void HabilitarArco()
    {
        HabilitarPanelOpciones();
    }

    public void DeshabilitarArco()
    {
        DeshabilitarPanelOpciones();
    }

    private bool GuardarTutorialEnPanelOpcionesPorOmitir()
    {
        if (!guardarTutorialEnPanelOpcionesAlOmitir)
        {
            return false;
        }

        BuscarPanelOpcionesManagerSiCorresponde();

        if (panelOpcionesManager == null)
        {
            return false;
        }

        AlgoLabPocketPanelItem item = GetComponent<AlgoLabPocketPanelItem>();

        if (item == null)
        {
            item = GetComponentInParent<AlgoLabPocketPanelItem>(true);
        }

        if (item == null && rootParaUbicar != null)
        {
            item = rootParaUbicar.GetComponentInChildren<AlgoLabPocketPanelItem>(true);
        }

        if (item == null)
        {
            return false;
        }

        tutorialGuardadoEnPanelOpciones = true;
        tutorialOmitidoDesdePanelOpciones = true;
        avisoTutorialOmitidoDesdePanelOpcionesConsumido = false;
        tutorialVisibleRestauradoDesdePocket = false;

        return panelOpcionesManager.RegistrarPanelEnOpciones(item, true, false);
    }

    private void ProcesarSacarTutorialOmitidoDesdePanelOpcionesSiCorresponde()
    {
        if (!tutorialOmitidoDesdePanelOpciones || avisoTutorialOmitidoDesdePanelOpcionesConsumido)
        {
            return;
        }

        avisoTutorialOmitidoDesdePanelOpcionesConsumido = true;
        tutorialOmitidoDesdePanelOpciones = false;

        if (alSacarTutorialOmitidoDesdePanelOpciones != null)
        {
            alSacarTutorialOmitidoDesdePanelOpciones.Invoke();
        }

        if (!string.IsNullOrWhiteSpace(textoAlSacarTutorialOmitidoDesdePanelOpciones))
        {
            CambiarInstruccion(textoAlSacarTutorialOmitidoDesdePanelOpciones);
        }

        AudioClip audioSalida = audioAlSacarTutorialOmitidoDesdePanelOpciones;

        if (audioSalida == null &&
            !repetirTutorialAlSacarTutorialOmitidoDesdePanelOpciones &&
            reemplazarProximoAudioElemento2PorAudio21 &&
            audio21VolverIntentarlo != null)
        {
            audioSalida = audio21VolverIntentarlo;
        }

        if (audioSalida != null)
        {
            ReproducirAudioClip(audioSalida);
        }

        if (repetirTutorialAlSacarTutorialOmitidoDesdePanelOpciones)
        {
            StartCoroutine(RepetirTutorialDesdePanelOpcionesSiguienteFrame());
        }
    }

    private IEnumerator RepetirTutorialDesdePanelOpcionesSiguienteFrame()
    {
        yield return null;

        tutorialActivo = false;
        tutorialFinalizado = false;
        IniciarTutorial();
    }

    public void RegistrarBotonA()
    {
        if (!permitirOmitirConDobleA || !tutorialActivo || tutorialFinalizado)
        {
            return;
        }

        float tiempoActual = Time.unscaledTime;
        float diferencia = tiempoActual - ultimoTiempoBotonA;

        if (diferencia >= 0f && diferencia <= tiempoMaximoDobleA)
        {
            OmitirTutorial();
            ultimoTiempoBotonA = -999f;
            return;
        }

        // Si ya pasaron mas de los segundos configurados, esta pulsacion
        // cuenta como la primera de un nuevo intento.
        ultimoTiempoBotonA = tiempoActual;

        DebugLog(
            "Tutorial: primera pulsacion de A para omitir. Presiona A otra vez antes de " +
            tiempoMaximoDobleA.ToString("F1") +
            " segundos."
        );
    }

    public void OmitirTutorial()
    {
        if (tutorialFinalizado)
        {
            return;
        }

        DebugLog("Tutorial omitido.");
        tutorialOmitidoAlMenosUnaVez = true;

        ReiniciarEstadoInteractivo();
        OcultarTodosLosIndicadoresGatillosInstantaneo();

        DetenerAudioActual();
        DetenerVideoActual();

        if (mandoController != null)
        {
            mandoController.OcultarPanelMando();
        }

        tutorialActivo = false;
        tutorialFinalizado = true;
        tutorialVisibleRestauradoDesdePocket = false;
        ultimoTiempoBotonA = -999f;

        // Primero libera el juego y posiciona sus paneles; guardar/cerrar el tutorial
        // no debe retrasar la aparición del resto de la interfaz.
        AsegurarSalidaTutorial(true);

        if (habilitarPanelOpcionesAlOmitirTutorial)
        {
            HabilitarPanelOpciones();
        }

        if (alOmitirTutorial != null)
        {
            alOmitirTutorial.Invoke();
        }

        if (iniciarPracticaAlFinalizar)
        {
            IniciarPractica();
        }
        else
        {
            ContinuarAplicacion();
        }

        bool tutorialGuardadoEnOpciones =
            tutorialGuardadoEnPanelOpciones ||
            GuardarTutorialEnPanelOpcionesPorOmitir();

        if (!tutorialGuardadoEnOpciones)
        {
            CerrarPanel();
        }
    }

    public void FinalizarTutorial()
    {
        if (tutorialFinalizado)
        {
            return;
        }

        DebugLog("Tutorial finalizado.");
        tutorialVistoAlMenosUnaVez = true;

        ReiniciarEstadoInteractivo();
        OcultarTodosLosIndicadoresGatillosInstantaneo();

        tutorialActivo = false;
        tutorialFinalizado = true;
        tutorialVisibleRestauradoDesdePocket = false;
        ultimoTiempoBotonA = -999f;

        if (habilitarPanelOpcionesAlFinalizarTutorial)
        {
            HabilitarPanelOpciones();
        }

        if (alFinalizarTutorial != null)
        {
            alFinalizarTutorial.Invoke();
        }

        if (iniciarPracticaAlFinalizar)
        {
            IniciarPractica();
        }
        else
        {
            ContinuarAplicacion();
        }

        CerrarPanel();
        AsegurarSalidaTutorial(false);
    }

    public void IniciarPractica()
    {
        if (alIniciarPractica != null)
        {
            alIniciarPractica.Invoke();
        }
    }

    public void ContinuarAplicacion()
    {
        if (alContinuarAplicacion != null)
        {
            alContinuarAplicacion.Invoke();
        }
    }

    private void OnDrawGizmos()
    {
        if (!dibujarGizmoTutorialSiempre)
        {
            return;
        }

        DibujarGizmoTutorial();
        DibujarGizmosPuntosMirada();
    }

    private void OnDrawGizmosSelected()
    {
        DibujarGizmoTutorial();
        DibujarGizmosPuntosMirada();
    }

    private void DibujarGizmoTutorial()
    {
        if (!ubicarTutorialEnPuntoManual)
        {
            return;
        }

        if (spawnManager == null)
        {
            spawnManager = AlgoLabManualPanelSpawnManager.Instance;
        }

        Transform referencia = null;

        if (spawnManager != null)
        {
            referencia = spawnManager.referenciaManual != null
                ? spawnManager.referenciaManual
                : spawnManager.transform;
        }

        if (referencia == null)
        {
            referencia = transform;
        }

        Vector3 posicionLocal;

        if (usarPuntoPropioTutorial)
        {
            posicionLocal = posicionLocalTutorialPropia;
        }
        else if (spawnManager != null && usarPosicionObjetoFrontalDelManager)
        {
            posicionLocal = spawnManager.posicionLocalObjetoFrontal + offsetLocalTutorialDesdeObjetoFrontal;
        }
        else
        {
            posicionLocal = posicionLocalTutorialManual;
        }

        Vector3 posicionMundo = referencia.TransformPoint(posicionLocal);

        Gizmos.color = colorGizmoTutorial;
        Gizmos.DrawWireCube(posicionMundo, Vector3.one * tamanoGizmoTutorial);
        Gizmos.DrawWireSphere(posicionMundo, tamanoGizmoTutorial * 0.45f);

        if (dibujarLineaDesdeReferenciaTutorial)
        {
            Gizmos.DrawLine(referencia.position, posicionMundo);
        }
    }

    private void DibujarGizmosPuntosMirada()
    {
        if (!dibujarGizmosPuntosMirada)
        {
            return;
        }

        if (puntoMiradaContraido != null)
        {
            Gizmos.color = colorGizmoMiradaContraida;
            Gizmos.DrawWireSphere(
                puntoMiradaContraido.position,
                tamanoGizmoPuntoMirada
            );

            Gizmos.DrawLine(
                puntoMiradaContraido.position + Vector3.left * tamanoGizmoPuntoMirada,
                puntoMiradaContraido.position + Vector3.right * tamanoGizmoPuntoMirada
            );

            Gizmos.DrawLine(
                puntoMiradaContraido.position + Vector3.down * tamanoGizmoPuntoMirada,
                puntoMiradaContraido.position + Vector3.up * tamanoGizmoPuntoMirada
            );
        }

        if (puntoMiradaExpandido != null)
        {
            Gizmos.color = colorGizmoMiradaExpandida;
            Gizmos.DrawWireSphere(
                puntoMiradaExpandido.position,
                tamanoGizmoPuntoMirada
            );

            Gizmos.DrawLine(
                puntoMiradaExpandido.position + Vector3.left * tamanoGizmoPuntoMirada,
                puntoMiradaExpandido.position + Vector3.right * tamanoGizmoPuntoMirada
            );

            Gizmos.DrawLine(
                puntoMiradaExpandido.position + Vector3.down * tamanoGizmoPuntoMirada,
                puntoMiradaExpandido.position + Vector3.up * tamanoGizmoPuntoMirada
            );
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
