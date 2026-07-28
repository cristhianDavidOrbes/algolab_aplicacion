using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class AlgoLabCarPracticeController : MonoBehaviour
{
    [Header("Referencias")]
    public AlgoLabManualPanelSpawnManager spawnManager;
    public AlgoLabClassDiagramController diagramController;
    public AlgoLabProgressPanel progressPanel;

    [Header("Prefab práctica")]
    public GameObject carPracticePrefab;
    public bool usarEscalaManual = true;
    public Vector3 escalaManual = new Vector3(0.25f, 0.25f, 0.25f);

    [Header("Alineación de spawn")]
    public bool alinearPorAnchorSpawn = true;

    [Header("Audio explicación práctica")]
    public AudioSource audioSource;
    public AudioClip audioInstruccionesPractica;
    [Header("Tutorial multimedia de la practica")]
    public AlgoLabPracticeTutorialSequence tutorialMultimedia;


    [Header("Audio final práctica")]
    public AudioClip audioFelicitacion;
    public AudioClip audioPerdida;

    [Header("Mensajes finales")]
    [TextArea(2, 4)]
    public string mensajeFelicitacion =
        "¡Felicitaciones! Completaste la práctica del nivel 1.\nYa puedes continuar al nivel 2 para entender mejor qué es un objeto.";

    [TextArea(2, 4)]
    public string mensajePerdida =
        "Tiempo agotado. No completaste la práctica del nivel 1, pero puedes volver a intentarlo.";

    [Header("Tiempo")]
    public float tiempoPractica = 80f;

    [Header("Totales de respaldo")]
    public int totalAtributosRespaldo = 3;
    public int totalMetodosRespaldo = 4;

    [Header("Error")]
    public float tiempoColorIncorrecto = 1f;

    [Header("Puntaje y guardado")]
    public int numeroNivelReal = 1;
    public int puntosMenosPorErrorEtiqueta = 10;
    public bool guardarProgresoAlCompletar = true;

    [Tooltip("Normalmente déjalo desactivado. Si se activa, también guarda cuando pierde con puntaje 0.")]
    public bool guardarIntentoFallido = false;

    [Header("UI progreso opcional")]
    public TMP_Text textoProgresoPractica;

    [Header("Debug")]
    public bool mostrarDebug = true;

    private GameObject practicaActual;
    private AlgoLabObjetoEducativo objetoEducativo;

    private readonly List<AlgoLabPracticeLabel> etiquetas =
        new List<AlgoLabPracticeLabel>();

    private AlgoLabPracticeLabel etiquetaSeleccionada;

    private readonly HashSet<string> atributosEncontrados =
        new HashSet<string>();

    private readonly HashSet<string> metodosEncontrados =
        new HashSet<string>();

    private int totalAtributos;
    private int totalMetodos;

    private float tiempoRestante;
    private bool practicaActiva;
    private bool practicaConectada;
    private bool practicaTerminada;

    private int penalizacionPuntaje = 0;
    private int erroresEtiqueta = 0;
    private int intentosPractica = 0;

    private Coroutine rutinaAudio;
    private Coroutine rutinaPractica;
    private Coroutine rutinaError;
    private Coroutine rutinaSpawn;
    private Coroutine rutinaAlinear;
    private Coroutine rutinaConectar;
    private Coroutine rutinaZonas;

    private bool explicacionCanceladaPorCambioFlujo = false;

    private void Start()
    {
        if (spawnManager == null)
        {
            spawnManager = AlgoLabManualPanelSpawnManager.Instance;
        }

        if (diagramController == null)
        {
            diagramController = FindFirstObjectByType<AlgoLabClassDiagramController>();
        }

        if (progressPanel == null)
        {
            progressPanel = FindFirstObjectByType<AlgoLabProgressPanel>();
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (tutorialMultimedia == null)
        {
            tutorialMultimedia = GetComponent<AlgoLabPracticeTutorialSequence>();
        }
    }

    private void OnDisable()
    {
        CancelarOperacionesInterrumpibles();
    }

    [ContextMenu("Iniciar explicación práctica")]
    public void IniciarExplicacionPractica()
    {
        if (!isActiveAndEnabled)
        {
            Debug.LogWarning("No se puede iniciar la explicación con el controlador desactivado.");
            return;
        }

        // IMPORTANTE:
        // El FlowStateManager puede cancelar una explicación anterior cuando cambia de flujo.
        // Si no limpiamos esta bandera aquí, la nueva explicación termina, pero NO muestra
        // el botón Iniciar práctica.
        explicacionCanceladaPorCambioFlujo = false;

        if (rutinaAudio != null)
        {
            StopCoroutine(rutinaAudio);
        }

        rutinaAudio = StartCoroutine(FlujoExplicacionPractica());
    }

    private IEnumerator FlujoExplicacionPractica()
    {
        practicaActiva = false;
        practicaTerminada = false;

        if (tutorialMultimedia != null && tutorialMultimedia.PuedeReproducir)
        {
            bool tutorialTerminado = false;
            tutorialMultimedia.Reproducir(() => tutorialTerminado = true);

            yield return new WaitUntil(() =>
                tutorialTerminado || explicacionCanceladaPorCambioFlujo
            );

            if (explicacionCanceladaPorCambioFlujo)
            {
                tutorialMultimedia.Detener(false);
            }
        }
        else if (audioSource != null && audioInstruccionesPractica != null)
        {
            audioSource.Stop();
            audioSource.clip = audioInstruccionesPractica;
            audioSource.Play();
            yield return new WaitWhile(() => audioSource != null && audioSource.isPlaying);
        }

        if (explicacionCanceladaPorCambioFlujo)
        {
            rutinaAudio = null;
            yield break;
        }

        if (progressPanel != null)
        {
            progressPanel.MostrarBotonIniciarPracticaDespuesDeAudio();
        }

        rutinaAudio = null;
    }

    public void CancelarExplicacionPracticaPorCambioDeFlujo()
    {
        explicacionCanceladaPorCambioFlujo = true;

        if (rutinaAudio != null)
        {
            StopCoroutine(rutinaAudio);
            rutinaAudio = null;
        }

        if (tutorialMultimedia != null)
        {
            tutorialMultimedia.Detener(false);
        }

        if (audioSource != null)
        {
            audioSource.Stop();
        }
    }

    [ContextMenu("Iniciar práctica desde botón")]
    public void IniciarPracticaDesdeBoton()
    {
        if (!isActiveAndEnabled)
        {
            Debug.LogWarning("No se puede iniciar la práctica con el controlador desactivado.");
            return;
        }

        if (rutinaSpawn != null)
        {
            StopCoroutine(rutinaSpawn);
        }

        rutinaSpawn = StartCoroutine(FlujoIniciarPracticaDesdeBoton());
    }

    private IEnumerator FlujoIniciarPracticaDesdeBoton()
    {
        practicaActiva = false;
        practicaConectada = false;
        practicaTerminada = false;

        SpawnearPractica();

        float tiempo = 0f;

        while (!practicaConectada && tiempo < 4f)
        {
            tiempo += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!practicaConectada)
        {
            Debug.LogError("No se pudo conectar la práctica spawneada.");
            rutinaSpawn = null;
            yield break;
        }

        ComenzarPractica();

        rutinaSpawn = null;
    }

    [ContextMenu("Spawnear práctica")]
    public void SpawnearPractica()
    {
        if (spawnManager == null)
        {
            Debug.LogError("No hay ManualPanelSpawnManager asignado.");
            return;
        }

        if (carPracticePrefab == null)
        {
            Debug.LogError("No hay prefab de práctica del carro asignado.");
            return;
        }

        if (usarEscalaManual)
        {
            spawnManager.CambiarObjetoFrontalDesdePrefabConEscala(
                carPracticePrefab,
                escalaManual
            );
        }
        else
        {
            spawnManager.CambiarObjetoFrontalDesdePrefab(carPracticePrefab);
        }

        if (rutinaConectar != null)
            StopCoroutine(rutinaConectar);

        rutinaConectar = StartCoroutine(ConectarPracticaSpawneada());
    }

    private IEnumerator ConectarPracticaSpawneada()
    {
        yield return null;

        GameObject objetoSpawneado = null;
        float tiempo = 0f;

        while (tiempo < 3f)
        {
            tiempo += Time.unscaledDeltaTime;

            if (spawnManager != null && spawnManager.ObjetoFrontalActual != null)
            {
                objetoSpawneado = spawnManager.ObjetoFrontalActual;
                break;
            }

            yield return null;
        }

        if (objetoSpawneado == null)
        {
            Debug.LogError("No se encontró el objeto de práctica spawneado.");
            rutinaConectar = null;
            yield break;
        }

        practicaActual = objetoSpawneado;

        if (alinearPorAnchorSpawn)
        {
            if (rutinaAlinear != null)
            {
                StopCoroutine(rutinaAlinear);
            }

            rutinaAlinear = StartCoroutine(AlinearObjetoConAnchorSpawnDuranteAnimacion());
        }

        objetoEducativo = practicaActual.GetComponentInChildren<AlgoLabObjetoEducativo>(true);

        if (objetoEducativo == null)
        {
            Debug.LogError("El prefab del carro no tiene AlgoLabObjetoEducativo.");
            rutinaConectar = null;
            yield break;
        }

        RecolectarEtiquetasDeLaPractica(true);

        atributosEncontrados.Clear();
        metodosEncontrados.Clear();
        etiquetaSeleccionada = null;

        if (diagramController != null)
        {
            diagramController.CambiarAModoPracticaConObjeto(objetoEducativo);
            IniciarActivacionZonasClasificacion();
        }

        tiempoRestante = tiempoPractica;
        penalizacionPuntaje = 0;
        erroresEtiqueta = 0;

        ActualizarProgresoUI();

        practicaConectada = true;
        rutinaConectar = null;

        if (mostrarDebug)
        {
            Debug.Log(
                "Práctica conectada. Etiquetas: " + etiquetas.Count +
                " | Atributos: " + totalAtributos +
                " | Métodos: " + totalMetodos
            );
        }
    }

    private void RecolectarEtiquetasDeLaPractica(bool ocultarAlRecolectar)
    {
        etiquetas.Clear();

        if (practicaActual == null)
        {
            Debug.LogWarning("No se pueden recolectar etiquetas porque practicaActual es NULL.");
            RecalcularTotalesSeguro();
            return;
        }

        AlgoLabPracticeLabel[] etiquetasEncontradas =
            practicaActual.GetComponentsInChildren<AlgoLabPracticeLabel>(true);

        etiquetas.AddRange(etiquetasEncontradas);

        for (int i = 0; i < etiquetas.Count; i++)
        {
            if (etiquetas[i] == null)
            {
                continue;
            }

            etiquetas[i].Inicializar(this);

            if (ocultarAlRecolectar)
            {
                etiquetas[i].gameObject.SetActive(false);
            }
        }

        RecalcularTotalesSeguro();

        if (mostrarDebug)
        {
            Debug.Log(
                "Etiquetas recolectadas: " + etiquetas.Count +
                " | Total atributos: " + totalAtributos +
                " | Total métodos: " + totalMetodos
            );
        }
    }

    private void RecalcularTotalesSeguro()
    {
        int atributos = 0;
        int metodos = 0;

        if (practicaActual != null)
        {
            AlgoLabPracticeLabel[] etiquetasActuales =
                practicaActual.GetComponentsInChildren<AlgoLabPracticeLabel>(true);

            if (etiquetasActuales != null && etiquetasActuales.Length > 0)
            {
                etiquetas.Clear();

                for (int i = 0; i < etiquetasActuales.Length; i++)
                {
                    if (etiquetasActuales[i] == null)
                    {
                        continue;
                    }

                    etiquetas.Add(etiquetasActuales[i]);

                    if (etiquetasActuales[i].tipoCorrecto == AlgoLabPracticeLabel.TipoElemento.Atributo)
                    {
                        atributos++;
                    }
                    else
                    {
                        metodos++;
                    }
                }
            }
        }

        if (atributos == 0 && metodos == 0)
        {
            AlgoLabPracticeLabel[] etiquetasEscena =
                FindObjectsByType<AlgoLabPracticeLabel>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None
                );

            if (etiquetasEscena != null && etiquetasEscena.Length > 0)
            {
                etiquetas.Clear();

                for (int i = 0; i < etiquetasEscena.Length; i++)
                {
                    if (etiquetasEscena[i] == null)
                    {
                        continue;
                    }

                    etiquetas.Add(etiquetasEscena[i]);

                    if (etiquetasEscena[i].tipoCorrecto == AlgoLabPracticeLabel.TipoElemento.Atributo)
                    {
                        atributos++;
                    }
                    else
                    {
                        metodos++;
                    }
                }

                if (mostrarDebug)
                {
                    Debug.LogWarning(
                        "Se usó búsqueda global de etiquetas. Atributos: " +
                        atributos + " | Métodos: " + metodos
                    );
                }
            }
        }

        if (atributos == 0 && metodos == 0 && objetoEducativo != null)
        {
            if (objetoEducativo.atributos != null)
            {
                atributos = objetoEducativo.atributos.Length;
            }

            if (objetoEducativo.metodos != null)
            {
                metodos = objetoEducativo.metodos.Length;
            }

            if (mostrarDebug)
            {
                Debug.LogWarning(
                    "Se usaron los datos de AlgoLabObjetoEducativo. Atributos: " +
                    atributos + " | Métodos: " + metodos
                );
            }
        }

        if (atributos == 0)
        {
            atributos = totalAtributosRespaldo;
        }

        if (metodos == 0)
        {
            metodos = totalMetodosRespaldo;
        }

        totalAtributos = atributos;
        totalMetodos = metodos;
    }

    private IEnumerator ActivarZonasClasificacionDespuesDeCrearDiagrama()
    {
        yield return null;
        yield return null;
        yield return new WaitForSecondsRealtime(0.1f);

        if (diagramController != null)
        {
            diagramController.ForzarZonasClasificacionActivas(true);
        }

        ConectarZonasClasificacion();

        if (mostrarDebug)
        {
            Debug.Log("Zonas de clasificación forzadas y conectadas desde práctica.");
        }

        rutinaZonas = null;
    }

    private void IniciarActivacionZonasClasificacion()
    {
        if (rutinaZonas != null)
            StopCoroutine(rutinaZonas);

        if (isActiveAndEnabled)
            rutinaZonas = StartCoroutine(ActivarZonasClasificacionDespuesDeCrearDiagrama());
    }

    private void ConectarZonasClasificacion()
    {
        AlgoLabPracticeClassificationZone[] zonas =
            FindObjectsByType<AlgoLabPracticeClassificationZone>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        for (int i = 0; i < zonas.Length; i++)
        {
            if (zonas[i] == null)
            {
                continue;
            }

            zonas[i].SetController(this);

            if (mostrarDebug)
            {
                Debug.Log("Zona conectada al controller: " + zonas[i].name);
            }
        }
    }

    private IEnumerator AlinearObjetoConAnchorSpawnDuranteAnimacion()
    {
        float duracion = 0.6f;

        if (spawnManager != null)
        {
            duracion = spawnManager.duracionAparecerObjeto + 0.15f;
        }

        float tiempo = 0f;

        while (tiempo < duracion)
        {
            tiempo += Time.unscaledDeltaTime;
            AlinearObjetoConAnchorSpawn();
            yield return null;
        }

        AlinearObjetoConAnchorSpawn();

        rutinaAlinear = null;
    }

    private void AlinearObjetoConAnchorSpawn()
    {
        if (practicaActual == null)
        {
            return;
        }

        AlgoLabPracticeSpawnPoint spawnPoint =
            practicaActual.GetComponentInChildren<AlgoLabPracticeSpawnPoint>(true);

        if (spawnPoint == null)
        {
            Debug.LogWarning("El prefab no tiene AlgoLabPracticeSpawnPoint. Se usará el pivot del prefab.");
            return;
        }

        Transform anchor = spawnPoint.ObtenerAnchor();

        if (anchor == null)
        {
            Debug.LogWarning("No hay spawnAnchor asignado en AlgoLabPracticeSpawnPoint.");
            return;
        }

        Vector3 posicionDestino = practicaActual.transform.position;
        Vector3 diferencia = posicionDestino - anchor.position;

        practicaActual.transform.position += diferencia;
    }

    [ContextMenu("Comenzar práctica")]
    public void ComenzarPractica()
    {
        if (practicaActual == null)
        {
            Debug.LogWarning("No hay práctica spawneada.");
            return;
        }

        practicaTerminada = false;

        atributosEncontrados.Clear();
        metodosEncontrados.Clear();
        etiquetaSeleccionada = null;

        practicaActiva = true;
        tiempoRestante = tiempoPractica;

        penalizacionPuntaje = 0;
        erroresEtiqueta = 0;
        intentosPractica++;

        RecolectarEtiquetasDeLaPractica(false);
        RecalcularTotalesSeguro();

        for (int i = 0; i < etiquetas.Count; i++)
        {
            if (etiquetas[i] == null)
            {
                continue;
            }

            etiquetas[i].gameObject.SetActive(true);
            etiquetas[i].AplicarEstadoNormal();
        }

        if (diagramController != null)
        {
            diagramController.CambiarAModoPracticaConObjeto(objetoEducativo);
            diagramController.ForzarZonasClasificacionActivas(true);
            ConectarZonasClasificacion();
            IniciarActivacionZonasClasificacion();
        }

        if (progressPanel != null)
        {
            progressPanel.MarcarPracticaEnCursoDesdeControlador();
        }

        if (rutinaPractica != null)
        {
            StopCoroutine(rutinaPractica);
        }

        rutinaPractica = StartCoroutine(RutinaTemporizador());

        ActualizarProgresoUI();

        Debug.Log("Práctica iniciada.");
    }

    private IEnumerator RutinaTemporizador()
    {
        while (practicaActiva && tiempoRestante > 0f)
        {
            tiempoRestante -= Time.deltaTime;
            ActualizarProgresoUI();
            yield return null;
        }

        if (practicaActiva)
        {
            TerminarPractica(false);
        }
    }

    public void SeleccionarEtiqueta(AlgoLabPracticeLabel etiqueta)
    {
        if (!practicaActiva || practicaTerminada || etiqueta == null)
        {
            return;
        }

        if (etiqueta.ClasificadaCorrectamente)
        {
            return;
        }

        if (etiquetaSeleccionada != null)
        {
            etiquetaSeleccionada.SetSeleccionada(false);
        }

        etiquetaSeleccionada = etiqueta;
        etiquetaSeleccionada.SetSeleccionada(true);

        Debug.Log("Etiqueta seleccionada: " + etiqueta.nombreElemento);
    }

    public void ClasificarEtiquetaSeleccionada(AlgoLabPracticeLabel.TipoElemento tipoElegido)
    {
        if (!practicaActiva || practicaTerminada)
        {
            Debug.LogWarning("No se puede clasificar porque la práctica no está activa.");
            return;
        }

        if (etiquetaSeleccionada == null)
        {
            Debug.Log("Primero selecciona una etiqueta del carro.");
            return;
        }

        Debug.Log(
            "Clasificando etiqueta: " +
            etiquetaSeleccionada.nombreElemento +
            " | Elegido: " + tipoElegido +
            " | Correcto: " + etiquetaSeleccionada.tipoCorrecto
        );

        if (etiquetaSeleccionada.tipoCorrecto == tipoElegido)
        {
            ClasificacionCorrecta(etiquetaSeleccionada, tipoElegido);
        }
        else
        {
            ClasificacionIncorrecta(etiquetaSeleccionada);
        }
    }

    public void ClasificarComoAtributo()
    {
        ClasificarEtiquetaSeleccionada(AlgoLabPracticeLabel.TipoElemento.Atributo);
    }

    public void ClasificarComoMetodo()
    {
        ClasificarEtiquetaSeleccionada(AlgoLabPracticeLabel.TipoElemento.Metodo);
    }

    private void ClasificacionCorrecta(
        AlgoLabPracticeLabel etiqueta,
        AlgoLabPracticeLabel.TipoElemento tipo)
    {
        if (etiqueta == null)
        {
            return;
        }

        etiqueta.MarcarCorrecto();

        string nombre = etiqueta.nombreElemento;

        if (tipo == AlgoLabPracticeLabel.TipoElemento.Atributo)
        {
            atributosEncontrados.Add(nombre);

            if (diagramController != null)
            {
                diagramController.RegistrarAtributoEncontrado(objetoEducativo, nombre);
                diagramController.ForzarZonasClasificacionActivas(true);
                ConectarZonasClasificacion();
            }
        }
        else
        {
            metodosEncontrados.Add(nombre);

            if (diagramController != null)
            {
                diagramController.RegistrarMetodoEncontrado(objetoEducativo, nombre);
                diagramController.ForzarZonasClasificacionActivas(true);
                ConectarZonasClasificacion();
            }
        }

        etiquetaSeleccionada = null;

        RecalcularTotalesSeguro();
        ActualizarProgresoUI();

        if (PracticaCompletada())
        {
            TerminarPractica(true);
        }
    }

    private void ClasificacionIncorrecta(AlgoLabPracticeLabel etiqueta)
    {
        if (etiqueta == null)
        {
            return;
        }

        RegistrarErrorPuntajeNivel1();

        if (rutinaError != null)
        {
            StopCoroutine(rutinaError);
        }

        rutinaError = StartCoroutine(MostrarErrorTemporal(etiqueta));
    }

    private void RegistrarErrorPuntajeNivel1()
    {
        erroresEtiqueta++;
        penalizacionPuntaje += puntosMenosPorErrorEtiqueta;

        Debug.Log(
            "PUNTAJE NIVEL 1: error de etiqueta. -" +
            puntosMenosPorErrorEtiqueta +
            " | Errores: " + erroresEtiqueta +
            " | Penalización total: " + penalizacionPuntaje
        );

        ActualizarProgresoUI();
    }

    private IEnumerator MostrarErrorTemporal(AlgoLabPracticeLabel etiqueta)
    {
        etiqueta.MarcarIncorrectoTemporal();

        yield return new WaitForSeconds(tiempoColorIncorrecto);

        if (etiqueta != null && !etiqueta.ClasificadaCorrectamente)
        {
            etiqueta.SetSeleccionada(etiqueta == etiquetaSeleccionada);
        }

        rutinaError = null;
    }

    private bool PracticaCompletada()
    {
        RecalcularTotalesSeguro();

        bool atributosCompletos = atributosEncontrados.Count >= totalAtributos;
        bool metodosCompletos = metodosEncontrados.Count >= totalMetodos;

        return atributosCompletos && metodosCompletos;
    }

    private void ActualizarProgresoUI()
    {
        RecalcularTotalesSeguro();

        int puntajeActual = CalcularPuntajeFinalNivel1();

        string texto =
            "Atributos: " + atributosEncontrados.Count + "/" + totalAtributos +
            "\nMétodos: " + metodosEncontrados.Count + "/" + totalMetodos +
            "\nPuntaje: " + puntajeActual;

        if (erroresEtiqueta > 0)
        {
            texto += "\nErrores: " + erroresEtiqueta + " (-" + penalizacionPuntaje + ")";
        }

        if (textoProgresoPractica != null)
        {
            textoProgresoPractica.text = texto;
        }

        if (progressPanel != null)
        {
            if (progressPanel.descriptionOrTaskText != null)
            {
                progressPanel.descriptionOrTaskText.text = texto;
            }

            if (progressPanel.timerText != null)
            {
                progressPanel.timerText.gameObject.SetActive(true);
                progressPanel.timerText.text = FormatearTiempo(tiempoRestante);
            }
        }
    }

    private string FormatearTiempo(float segundos)
    {
        segundos = Mathf.Max(0f, segundos);

        int minutos = Mathf.FloorToInt(segundos / 60f);
        int seg = Mathf.FloorToInt(segundos % 60f);

        return minutos.ToString("00") + ":" + seg.ToString("00");
    }

    public void TerminarPractica(bool completada)
    {
        if (practicaTerminada)
        {
            return;
        }

        practicaTerminada = true;
        practicaActiva = false;

        if (rutinaPractica != null)
        {
            StopCoroutine(rutinaPractica);
            rutinaPractica = null;
        }

        if (rutinaError != null)
        {
            StopCoroutine(rutinaError);
            rutinaError = null;
        }

        for (int i = 0; i < etiquetas.Count; i++)
        {
            if (etiquetas[i] != null)
            {
                etiquetas[i].SetSeleccionada(false);
            }
        }

        if (diagramController != null)
        {
            diagramController.ForzarZonasClasificacionActivas(false);
        }

        if (completada)
        {
            MostrarResultadoGanador();
        }
        else
        {
            MostrarResultadoPerdedor();
        }
    }

    private void CancelarOperacionesInterrumpibles()
    {
        explicacionCanceladaPorCambioFlujo = true;
        practicaActiva = false;
        practicaConectada = false;
        practicaTerminada = true;

        DetenerRutina(ref rutinaAudio);
        DetenerRutina(ref rutinaPractica);
        DetenerRutina(ref rutinaError);
        DetenerRutina(ref rutinaSpawn);
        DetenerRutina(ref rutinaAlinear);
        DetenerRutina(ref rutinaConectar);
        DetenerRutina(ref rutinaZonas);

        if (audioSource != null)
            audioSource.Stop();

        if (etiquetaSeleccionada != null && !etiquetaSeleccionada.ClasificadaCorrectamente)
            etiquetaSeleccionada.SetSeleccionada(false);

        etiquetaSeleccionada = null;

        if (diagramController != null)
            diagramController.ForzarZonasClasificacionActivas(false);
    }

    private void DetenerRutina(ref Coroutine rutina)
    {
        if (rutina == null)
            return;

        StopCoroutine(rutina);
        rutina = null;
    }

    private void MostrarResultadoGanador()
    {
        int puntajeFinal = CalcularPuntajeFinalNivel1();

        if (progressPanel != null)
        {
            progressPanel.TerminarPracticaActual();

            if (progressPanel.descriptionOrTaskText != null)
            {
                progressPanel.descriptionOrTaskText.text =
                    mensajeFelicitacion +
                    "\n\nPuntaje obtenido: " + puntajeFinal +
                    "\nTiempo restante: " + Mathf.CeilToInt(Mathf.Max(0f, tiempoRestante)) +
                    "\nPenalización: -" + penalizacionPuntaje;
            }

            if (progressPanel.timerText != null)
            {
                progressPanel.timerText.gameObject.SetActive(true);
                progressPanel.timerText.text = "Completado";
            }
        }

        if (audioSource != null && audioFelicitacion != null)
        {
            audioSource.Stop();
            audioSource.clip = audioFelicitacion;
            audioSource.Play();
        }

        GuardarProgresoNivel1(true);

        Debug.Log(
            "Práctica completada. Ganaste. Puntaje final: " +
            puntajeFinal +
            " | Tiempo restante: " + tiempoRestante +
            " | Penalización: " + penalizacionPuntaje
        );
    }

    private void MostrarResultadoPerdedor()
    {
        if (progressPanel != null)
        {
            progressPanel.MarcarPracticaPerdidaDesdeControlador();

            if (progressPanel.descriptionOrTaskText != null)
            {
                progressPanel.descriptionOrTaskText.text =
                    mensajePerdida +
                    "\n\nPuntaje obtenido: 0" +
                    "\nErrores cometidos: " + erroresEtiqueta;
            }

            if (progressPanel.timerText != null)
            {
                progressPanel.timerText.gameObject.SetActive(true);
                progressPanel.timerText.text = "00:00";
            }
        }

        if (audioSource != null && audioPerdida != null)
        {
            audioSource.Stop();
            audioSource.clip = audioPerdida;
            audioSource.Play();
        }

        GuardarProgresoNivel1(false);

        Debug.Log("Tiempo agotado. Perdiste la práctica.");
    }

    private int CalcularPuntajeFinalNivel1()
    {
        int tiempo = Mathf.CeilToInt(Mathf.Max(0f, tiempoRestante));
        int puntaje = tiempo - penalizacionPuntaje;

        return Mathf.Max(0, puntaje);
    }

    private void GuardarProgresoNivel1(bool completada)
    {
        if (!guardarProgresoAlCompletar)
        {
            return;
        }

        if (!completada && !guardarIntentoFallido)
        {
            return;
        }

        int puntajeFinal = completada ? CalcularPuntajeFinalNivel1() : 0;
        int tiempoEntero = Mathf.CeilToInt(Mathf.Max(0f, tiempoRestante));

        if (AlgoLabProgressSaver.Instance != null)
        {
            AlgoLabProgressSaver.Instance.GuardarProgresoSiAplica(
                numeroNivelReal,
                completada,
                puntajeFinal,
                tiempoEntero,
                Mathf.Max(1, intentosPractica)
            );
        }
        else
        {
            Debug.LogWarning("PUNTAJE NIVEL 1: no existe AlgoLabProgressSaver en la escena.");
        }

        Debug.Log(
            "PUNTAJE NIVEL 1: completada=" + completada +
            " | tiempo=" + tiempoEntero +
            " | penalización=" + penalizacionPuntaje +
            " | errores=" + erroresEtiqueta +
            " | puntajeFinal=" + puntajeFinal +
            " | intentos=" + intentosPractica
        );
    }
}
