using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class AlgoLabTutorialControllerAnimationPanel : MonoBehaviour
{
    public enum VistaMando
    {
        Lateral,
        Frontal
    }

    public enum EstadoGatillo
    {
        Ninguno,
        Principal,
        Secundario
    }

    [Header("Panel")]
    public RectTransform panelRoot;
    public RawImage controllerRawImage;

    [Header("Tamaño del panel")]
    public float anchoVisible = 200f;
    public float anchoOculto = 0f;

    [Header("Secuencia PNG")]
    [Tooltip("Ruta dentro de Resources. Ejemplo: Assets/_Algolab/_TutorialSystem/Resources/sin fondo")]
    public string carpetaResourcesFrames = "sin fondo";

    [Tooltip("Si está activo, carga todos los PNG automáticamente desde Resources.")]
    public bool cargarFramesAutomaticamente = true;

    [Tooltip("Lista de frames cargados. Normalmente no tienes que llenarla a mano.")]
    public List<Texture2D> frames = new List<Texture2D>();

    [Header("Rangos de animación")]
    public int frameIdle = 1;

    [Header("Gatillo principal")]
    public int principalPresionarInicio = 1;
    public int principalPresionarFin = 40;
    public int principalMantener = 40;
    public int principalSoltarInicio = 40;
    public int principalSoltarFin = 70;

    [Header("Gatillo secundario")]
    public int secundarioPresionarInicio = 70;
    public int secundarioPresionarFin = 90;
    public int secundarioMantener = 90;
    public int secundarioSoltarInicio = 90;
    public int secundarioSoltarFin = 111;

    [Header("Velocidad")]
    [Tooltip("Frames por segundo de la secuencia PNG.")]
    public float framesPorSegundo = 30f;

    [Header("Vista frontal opcional")]
    public Texture frontalIdleTexture;

    [Header("Animación panel")]
    public bool ocultarAlIniciar = true;
    public float duracionMostrar = 0.25f;
    public float duracionOcultar = 0.25f;

    [Header("Transición de vista")]
    public float duracionColapsar = 0.18f;
    public float duracionExpandir = 0.18f;

    [Header("Debug")]
    public bool mostrarDebug = true;

    private VistaMando vistaActual = VistaMando.Lateral;
    private EstadoGatillo gatilloActual = EstadoGatillo.Ninguno;

    private bool soltarPrincipalPendiente;
    private bool soltarSecundarioPendiente;
    private bool animacionPresionarEnCurso;

    private Coroutine rutinaPanel;
    private Coroutine rutinaVista;
    private Coroutine rutinaSecuencia;

    private void Awake()
    {
        PrepararReferencias();
        CargarFramesSiHaceFalta();
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        rutinaPanel = null;
        rutinaVista = null;
        rutinaSecuencia = null;
        animacionPresionarEnCurso = false;
        soltarPrincipalPendiente = false;
        soltarSecundarioPendiente = false;
    }

    private void Start()
    {
        PrepararReferencias();
        CargarFramesSiHaceFalta();

        if (ocultarAlIniciar)
        {
            OcultarInstantaneo();
        }
        else
        {
            MostrarInstantaneo();
        }

        MostrarMandoIdle();
    }

    private void PrepararReferencias()
    {
        if (panelRoot == null)
        {
            panelRoot = GetComponent<RectTransform>();
        }

        if (controllerRawImage == null)
        {
            controllerRawImage = GetComponentInChildren<RawImage>(true);
        }
    }

    private void CargarFramesSiHaceFalta()
    {
        if (!cargarFramesAutomaticamente)
        {
            return;
        }

        if (frames != null && frames.Count > 0)
        {
            return;
        }

        Texture2D[] framesCargados =
            Resources.LoadAll<Texture2D>(carpetaResourcesFrames);

        if (framesCargados == null || framesCargados.Length == 0)
        {
            Debug.LogWarning(
                "Mando tutorial: no se encontraron PNG en Resources/" +
                carpetaResourcesFrames +
                ". Revisa que la carpeta sea " +
                "Assets/_Algolab/_TutorialSystem/Resources/sin fondo"
            );

            return;
        }

        frames = framesCargados
            .OrderBy(t => ExtraerNumeroFrame(t.name))
            .ToList();

        DebugLog("Mando tutorial: frames cargados = " + frames.Count);
    }

    private int ExtraerNumeroFrame(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            return 0;
        }

        string numeros = "";

        for (int i = nombre.Length - 1; i >= 0; i--)
        {
            char caracter = nombre[i];

            if (char.IsDigit(caracter))
            {
                numeros = caracter + numeros;
            }
            else if (numeros.Length > 0)
            {
                break;
            }
        }

        if (int.TryParse(numeros, out int resultado))
        {
            return resultado;
        }

        return 0;
    }

    public void MostrarPanelMando()
    {
        PrepararReferencias();
        CargarFramesSiHaceFalta();

        if (rutinaPanel != null)
        {
            StopCoroutine(rutinaPanel);
        }

        gameObject.SetActive(true);

        rutinaPanel = StartCoroutine(
            AnimarAnchoPanel(anchoVisible, duracionMostrar)
        );
    }

    public void OcultarPanelMando()
    {
        PrepararReferencias();

        if (rutinaPanel != null)
        {
            StopCoroutine(rutinaPanel);
        }

        rutinaPanel = StartCoroutine(OcultarPanelRutina());
    }

    private IEnumerator OcultarPanelRutina()
    {
        yield return AnimarAnchoPanel(
            anchoOculto,
            duracionOcultar
        );

        DetenerSecuencia();

        rutinaPanel = null;
    }

    public void MostrarInstantaneo()
    {
        PrepararReferencias();

        gameObject.SetActive(true);
        AplicarAnchoPanel(anchoVisible);
    }

    public void OcultarInstantaneo()
    {
        PrepararReferencias();

        AplicarAnchoPanel(anchoOculto);
        DetenerSecuencia();
    }

    public bool EstaExpandidoVisualmente()
    {
        PrepararReferencias();

        if (panelRoot == null)
        {
            return false;
        }

        float anchoActual = panelRoot.rect.width;
        float distanciaExpandido = Mathf.Abs(anchoActual - anchoVisible);
        float distanciaContraido = Mathf.Abs(anchoActual - anchoOculto);
        return distanciaExpandido < distanciaContraido;
    }

    public void ColapsarMando()
    {
        PrepararReferencias();

        if (rutinaVista != null)
        {
            StopCoroutine(rutinaVista);
        }

        rutinaVista = StartCoroutine(ColapsarRutina());
    }

    public void ExpandirMando()
    {
        PrepararReferencias();

        if (rutinaVista != null)
        {
            StopCoroutine(rutinaVista);
        }

        rutinaVista = StartCoroutine(ExpandirRutina());
    }

    private IEnumerator ColapsarRutina()
    {
        yield return AnimarAnchoPanel(
            anchoOculto,
            duracionColapsar
        );

        rutinaVista = null;
    }

    private IEnumerator ExpandirRutina()
    {
        yield return AnimarAnchoPanel(
            anchoVisible,
            duracionExpandir
        );

        rutinaVista = null;
    }

    private IEnumerator AnimarAnchoPanel(
        float anchoDestino,
        float duracion
    )
    {
        if (panelRoot == null)
        {
            yield break;
        }

        float anchoInicio = panelRoot.rect.width;
        float tiempo = 0f;
        float duracionSegura = Mathf.Max(0.01f, duracion);

        while (tiempo < duracionSegura)
        {
            tiempo += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(
                tiempo / duracionSegura
            );

            float smooth = Mathf.SmoothStep(
                0f,
                1f,
                t
            );

            float anchoActual = Mathf.Lerp(
                anchoInicio,
                anchoDestino,
                smooth
            );

            AplicarAnchoPanel(anchoActual);

            yield return null;
        }

        AplicarAnchoPanel(anchoDestino);
    }

    private void AplicarAnchoPanel(float nuevoAncho)
    {
        if (panelRoot == null)
        {
            return;
        }

        panelRoot.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Horizontal,
            nuevoAncho
        );
    }

    public void CambiarMandoLateral()
    {
        vistaActual = VistaMando.Lateral;

        MostrarMandoIdle();
    }

    public void CambiarMandoFrontal()
    {
        vistaActual = VistaMando.Frontal;
        gatilloActual = EstadoGatillo.Ninguno;

        soltarPrincipalPendiente = false;
        soltarSecundarioPendiente = false;
        animacionPresionarEnCurso = false;

        DetenerSecuencia();

        if (controllerRawImage != null)
        {
            if (frontalIdleTexture != null)
            {
                controllerRawImage.texture =
                    frontalIdleTexture;
            }
            else
            {
                MostrarFrame(frameIdle);
            }
        }

        DebugLog("Mando tutorial: vista frontal.");
    }

    public void CambiarMandoFrontalConTransicion()
    {
        if (rutinaVista != null)
        {
            StopCoroutine(rutinaVista);
        }

        rutinaVista = StartCoroutine(
            CambiarVistaConTransicion(
                VistaMando.Frontal
            )
        );
    }

    public void CambiarMandoLateralConTransicion()
    {
        if (rutinaVista != null)
        {
            StopCoroutine(rutinaVista);
        }

        rutinaVista = StartCoroutine(
            CambiarVistaConTransicion(
                VistaMando.Lateral
            )
        );
    }

    private IEnumerator CambiarVistaConTransicion(
        VistaMando nuevaVista
    )
    {
        yield return AnimarAnchoPanel(
            anchoOculto,
            duracionColapsar
        );

        if (nuevaVista == VistaMando.Frontal)
        {
            CambiarMandoFrontal();
        }
        else
        {
            CambiarMandoLateral();
        }

        yield return AnimarAnchoPanel(
            anchoVisible,
            duracionExpandir
        );

        rutinaVista = null;
    }

    public void MostrarMandoIdle()
    {
        DetenerSecuencia();

        gatilloActual = EstadoGatillo.Ninguno;
        soltarPrincipalPendiente = false;
        soltarSecundarioPendiente = false;
        animacionPresionarEnCurso = false;

        if (controllerRawImage == null)
        {
            return;
        }

        /*
         * Esta comprobación utiliza vistaActual.
         * Por eso desaparece la advertencia CS0414.
         */
        if (vistaActual == VistaMando.Frontal)
        {
            if (frontalIdleTexture != null)
            {
                controllerRawImage.texture =
                    frontalIdleTexture;
            }
            else
            {
                MostrarFrame(frameIdle);
            }

            DebugLog(
                "Mando tutorial: idle frontal."
            );

            return;
        }

        MostrarFrame(frameIdle);

        DebugLog(
            "Mando tutorial: idle lateral, frame " +
            frameIdle
        );
    }

    public void PresionarGatilloPrincipal()
    {
        vistaActual = VistaMando.Lateral;
        gatilloActual = EstadoGatillo.Principal;

        soltarPrincipalPendiente = false;
        soltarSecundarioPendiente = false;

        ReproducirPresionar(
            principalPresionarInicio,
            principalPresionarFin,
            principalMantener,
            EstadoGatillo.Principal
        );
    }

    public void SoltarGatilloPrincipal()
    {
        if (gatilloActual != EstadoGatillo.Principal)
        {
            return;
        }

        if (animacionPresionarEnCurso)
        {
            soltarPrincipalPendiente = true;
            return;
        }

        ReproducirSoltar(
            principalSoltarInicio,
            principalSoltarFin,
            EstadoGatillo.Principal
        );
    }

    public void PresionarGatilloSecundario()
    {
        vistaActual = VistaMando.Lateral;
        gatilloActual = EstadoGatillo.Secundario;

        soltarPrincipalPendiente = false;
        soltarSecundarioPendiente = false;

        ReproducirPresionar(
            secundarioPresionarInicio,
            secundarioPresionarFin,
            secundarioMantener,
            EstadoGatillo.Secundario
        );
    }

    public void SoltarGatilloSecundario()
    {
        if (gatilloActual != EstadoGatillo.Secundario)
        {
            return;
        }

        if (animacionPresionarEnCurso)
        {
            soltarSecundarioPendiente = true;
            return;
        }

        ReproducirSoltar(
            secundarioSoltarInicio,
            secundarioSoltarFin,
            EstadoGatillo.Secundario
        );
    }

    public void ForzarMantenerGatilloPrincipal()
    {
        vistaActual = VistaMando.Lateral;
        gatilloActual = EstadoGatillo.Principal;

        soltarPrincipalPendiente = false;
        soltarSecundarioPendiente = false;
        animacionPresionarEnCurso = false;

        DetenerSecuencia();
        MostrarFrame(principalMantener);

        DebugLog(
            "Mando tutorial: mantener gatillo principal frame " +
            principalMantener
        );
    }

    public void ForzarMantenerGatilloSecundario()
    {
        vistaActual = VistaMando.Lateral;
        gatilloActual = EstadoGatillo.Secundario;

        soltarPrincipalPendiente = false;
        soltarSecundarioPendiente = false;
        animacionPresionarEnCurso = false;

        DetenerSecuencia();
        MostrarFrame(secundarioMantener);

        DebugLog(
            "Mando tutorial: mantener gatillo secundario frame " +
            secundarioMantener
        );
    }

    private void ReproducirPresionar(
        int inicio,
        int fin,
        int mantener,
        EstadoGatillo gatillo
    )
    {
        PrepararReferencias();
        CargarFramesSiHaceFalta();

        if (rutinaSecuencia != null)
        {
            StopCoroutine(rutinaSecuencia);
        }

        rutinaSecuencia = StartCoroutine(
            ReproducirPresionarRutina(
                inicio,
                fin,
                mantener,
                gatillo
            )
        );
    }

    private IEnumerator ReproducirPresionarRutina(
        int inicio,
        int fin,
        int mantener,
        EstadoGatillo gatillo
    )
    {
        animacionPresionarEnCurso = true;

        yield return ReproducirRangoRutina(
            inicio,
            fin
        );

        animacionPresionarEnCurso = false;

        bool debeSoltar =
            gatillo == EstadoGatillo.Principal
                ? soltarPrincipalPendiente
                : soltarSecundarioPendiente;

        if (debeSoltar)
        {
            if (gatillo == EstadoGatillo.Principal)
            {
                soltarPrincipalPendiente = false;

                ReproducirSoltar(
                    principalSoltarInicio,
                    principalSoltarFin,
                    EstadoGatillo.Principal
                );
            }
            else
            {
                soltarSecundarioPendiente = false;

                ReproducirSoltar(
                    secundarioSoltarInicio,
                    secundarioSoltarFin,
                    EstadoGatillo.Secundario
                );
            }

            yield break;
        }

        MostrarFrame(mantener);

        rutinaSecuencia = null;
    }

    private void ReproducirSoltar(
        int inicio,
        int fin,
        EstadoGatillo gatillo
    )
    {
        PrepararReferencias();
        CargarFramesSiHaceFalta();

        if (rutinaSecuencia != null)
        {
            StopCoroutine(rutinaSecuencia);
        }

        rutinaSecuencia = StartCoroutine(
            ReproducirSoltarRutina(
                inicio,
                fin,
                gatillo
            )
        );
    }

    private IEnumerator ReproducirSoltarRutina(
        int inicio,
        int fin,
        EstadoGatillo gatillo
    )
    {
        yield return ReproducirRangoRutina(
            inicio,
            fin
        );

        MostrarMandoIdle();

        rutinaSecuencia = null;
    }

    private IEnumerator ReproducirRangoRutina(
        int inicio,
        int fin
    )
    {
        if (frames == null || frames.Count == 0)
        {
            CargarFramesSiHaceFalta();
        }

        if (frames == null || frames.Count == 0)
        {
            yield break;
        }

        int inicioSeguro = Mathf.Clamp(
            inicio,
            1,
            frames.Count
        );

        int finSeguro = Mathf.Clamp(
            fin,
            1,
            frames.Count
        );

        float espera =
            1f / Mathf.Max(1f, framesPorSegundo);

        if (inicioSeguro <= finSeguro)
        {
            for (
                int i = inicioSeguro;
                i <= finSeguro;
                i++
            )
            {
                MostrarFrame(i);

                yield return new WaitForSecondsRealtime(
                    espera
                );
            }
        }
        else
        {
            for (
                int i = inicioSeguro;
                i >= finSeguro;
                i--
            )
            {
                MostrarFrame(i);

                yield return new WaitForSecondsRealtime(
                    espera
                );
            }
        }
    }

    private void MostrarFrame(int numeroFrame)
    {
        PrepararReferencias();

        if (controllerRawImage == null)
        {
            return;
        }

        if (frames == null || frames.Count == 0)
        {
            CargarFramesSiHaceFalta();
        }

        if (frames == null || frames.Count == 0)
        {
            return;
        }

        int indice = Mathf.Clamp(
            numeroFrame - 1,
            0,
            frames.Count - 1
        );

        Texture2D frame = frames[indice];

        if (frame != null)
        {
            controllerRawImage.texture = frame;
        }
    }

    private void DetenerSecuencia()
    {
        if (rutinaSecuencia != null)
        {
            StopCoroutine(rutinaSecuencia);
            rutinaSecuencia = null;
        }
    }

    public void PresionarBotonA()
    {
        CambiarMandoFrontal();

        DebugLog(
            "Mando tutorial: presionar botón A. " +
            "Pendiente agregar animación específica."
        );
    }

    public void PresionarBotonB()
    {
        CambiarMandoFrontal();

        DebugLog(
            "Mando tutorial: presionar botón B. " +
            "Pendiente agregar animación específica."
        );
    }

    public void MoverPalanca()
    {
        CambiarMandoFrontal();

        DebugLog(
            "Mando tutorial: mover palanca. " +
            "Pendiente agregar animación específica."
        );
    }

    public void SoltarPalanca()
    {
        CambiarMandoFrontal();

        DebugLog(
            "Mando tutorial: soltar palanca."
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
