using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AlgoLabClassDiagramCardUI : MonoBehaviour
{
    public enum TipoResaltado
    {
        Ninguno,
        Clase,
        Atributos,
        Metodos,
        AtributoEspecifico,
        MetodoEspecifico
    }

    [Header("Referencias UI")]
    public TMP_Text textoNombreClase;
    public TMP_Text textoAtributos;
    public TMP_Text textoMetodos;

    [Header("Títulos visibles antes de clasificar")]
    [Tooltip("Texto TMP que dice Atributos. Se muestra en práctica mientras no haya atributos clasificados.")]
    public TMP_Text tituloAtributos;

    [Tooltip("Texto TMP que dice Métodos. Se muestra en práctica mientras no haya métodos clasificados.")]
    public TMP_Text tituloMetodos;

    public bool buscarTitulosAutomaticamente = true;
    public bool mostrarTitulosVaciosEnPractica = true;

    [Tooltip("En práctica, mantiene TextoAtributos y TextoMetodos apagados hasta que el usuario clasifique al menos un atributo o método correcto.")]
    public bool ocultarTextosVaciosEnPractica = true;
    public string textoTituloAtributos = "Atributos";
    public string textoTituloMetodos = "Métodos";

    [Header("Líneas del diagrama")]
    public RectTransform linea1;
    public RectTransform linea2;
    public bool buscarLineasAutomaticamente = true;

    [Header("Layout interno automático")]
    [Tooltip("Corrige la posición de textos y líneas para que Linea1 no se monte encima de Vehiculo y para que Metodos no se salgan del cuadro.")]
    public bool reajustarLayoutInterno = true;

    [Tooltip("Recomendado activado. Limita márgenes grandes para que la tarjeta se adapte al contenido y no deje espacios enormes.")]
    public bool forzarLayoutCompactoSeguro = true;

    public float margenSuperiorLayout = 24f;
    public float separacionTituloLinea = 16f;
    public float separacionLineaContenido = 16f;
    public float separacionEntreSecciones = 16f;
    public float alturaMinimaLineaTexto = 24f;
    public float margenInferiorLayout = 24f;

    [Header("Zonas de clasificación práctica")]
    public GameObject zonaAtributos;
    public GameObject zonaMetodos;
    public bool buscarZonasAutomaticamente = true;

    [Header("Fondos opcionales para resaltar")]
    public Image fondoClase;
    public Image fondoAtributos;
    public Image fondoMetodos;

    [Header("Tamaño automático")]
    public float anchoMinimo = 220f;
    public float anchoMaximo = 420f;
    public float paddingExtra = 70f;
    public float altoMinimo = 180f;
    public float altoExtra = 65f;

    [Header("Animación de aparición")]
    public bool aparecerConSmooth = true;
    public float duracionAparicion = 0.35f;
    public float escalaInicialAparicion = 0.85f;

    [Header("Resaltado")]
    public Color colorResaltado = new Color(1f, 0.82f, 0.15f, 0.45f);
    public Color colorResaltadoTMP = new Color(1f, 0.82f, 0.15f, 0.55f);
    public Color colorFondoNormal = new Color(1f, 1f, 1f, 0f);
    public float duracionCambioColor = 0.18f;
    public bool resaltarSoloSignosAcceso = false;

    private RectTransform rectTransform;
    private LayoutElement layoutElement;
    private CanvasGroup canvasGroup;

    private Vector3 escalaOriginal;

    private List<string> atributosActuales = new List<string>();
    private List<string> metodosActuales = new List<string>();
    private string nombreClaseActual = "";
    private bool modoPracticaActual = false;

    private TipoResaltado tipoResaltadoActual = TipoResaltado.Ninguno;
    private string elementoResaltadoActual = "";

    private Coroutine rutinaAparicion;
    private Coroutine rutinaFondos;
    private Coroutine rutinaTemporal;
    private Outline contornoContexto;
    private Color colorNombreClaseOriginal;
    private bool colorNombreClaseCapturado;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        layoutElement = GetComponent<LayoutElement>();
        canvasGroup = GetComponent<CanvasGroup>();

        if (layoutElement == null)
        {
            layoutElement = gameObject.AddComponent<LayoutElement>();
        }

        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        escalaOriginal = transform.localScale;

        if (buscarZonasAutomaticamente)
        {
            BuscarZonasClasificacion();
        }

        BuscarTitulosYLineasSiHaceFalta();
        SetZonasClasificacionActivas(false);

        PrepararTextos();
        if (textoNombreClase != null)
        {
            colorNombreClaseOriginal = textoNombreClase.color;
            colorNombreClaseCapturado = true;
        }
        AplicarTitulosDePractica();
        AplicarFondosInmediato(colorFondoNormal);
    }

    public void ConfigurarContornoContexto(Color color, bool activo = true)
    {
        if (!activo && contornoContexto == null)
        {
            if (textoNombreClase != null && colorNombreClaseCapturado)
            {
                textoNombreClase.color = colorNombreClaseOriginal;
            }
            return;
        }

        if (contornoContexto == null)
        {
            contornoContexto = GetComponent<Outline>();
            if (contornoContexto == null)
            {
                contornoContexto = gameObject.AddComponent<Outline>();
            }
        }

        contornoContexto.effectColor = color;
        contornoContexto.effectDistance = new Vector2(5f, -5f);
        contornoContexto.useGraphicAlpha = false;
        contornoContexto.enabled = activo;

        if (textoNombreClase != null)
        {
            if (!colorNombreClaseCapturado)
            {
                colorNombreClaseOriginal = textoNombreClase.color;
                colorNombreClaseCapturado = true;
            }

            textoNombreClase.color =
                activo ? new Color(color.r, color.g, color.b, 1f)
                : colorNombreClaseOriginal;
        }
    }

    private void OnDisable()
    {
        DetenerRutinasVisuales();

        tipoResaltadoActual = TipoResaltado.Ninguno;
        elementoResaltadoActual = "";
        AplicarTextoConResaltado();
        AplicarFondosResaltadoInmediato();

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;

        ConfigurarContornoContexto(Color.clear, false);
        transform.localScale = escalaOriginal;
    }

    private void PrepararTextos()
    {
        if (textoNombreClase != null)
        {
            textoNombreClase.richText = true;
        }

        if (textoAtributos != null)
        {
            textoAtributos.richText = true;
        }

        if (textoMetodos != null)
        {
            textoMetodos.richText = true;
        }

        if (tituloAtributos != null)
        {
            tituloAtributos.richText = true;
        }

        if (tituloMetodos != null)
        {
            tituloMetodos.richText = true;
        }
    }

    private void BuscarTitulosYLineasSiHaceFalta()
    {
        if (buscarTitulosAutomaticamente)
        {
            if (tituloAtributos == null)
            {
                Transform t = transform.Find("TituloAtributo");

                if (t == null)
                {
                    t = transform.Find("TituloAtributos");
                }

                if (t != null)
                {
                    tituloAtributos = t.GetComponent<TMP_Text>();
                }
            }

            if (tituloMetodos == null)
            {
                Transform t = transform.Find("TituloMetodo");

                if (t == null)
                {
                    t = transform.Find("TituloMetodos");
                }

                if (t != null)
                {
                    tituloMetodos = t.GetComponent<TMP_Text>();
                }
            }
        }

        if (buscarLineasAutomaticamente)
        {
            if (linea1 == null)
            {
                Transform t = transform.Find("Linea1");

                if (t != null)
                {
                    linea1 = t.GetComponent<RectTransform>();
                }
            }

            if (linea2 == null)
            {
                Transform t = transform.Find("Linea2");

                if (t != null)
                {
                    linea2 = t.GetComponent<RectTransform>();
                }
            }
        }
    }

    private void BuscarZonasClasificacion()
    {
        if (zonaAtributos == null)
        {
            Transform zona = transform.Find("ZonaAtributos");

            if (zona != null)
            {
                zonaAtributos = zona.gameObject;
            }
        }

        if (zonaMetodos == null)
        {
            Transform zona = transform.Find("ZonaMetodos");

            if (zona != null)
            {
                zonaMetodos = zona.gameObject;
            }
        }
    }

    public void SetZonasClasificacionActivas(bool activas)
    {
        if (buscarZonasAutomaticamente)
        {
            BuscarZonasClasificacion();
        }

        BuscarTitulosYLineasSiHaceFalta();

        if (zonaAtributos != null)
        {
            zonaAtributos.SetActive(activas);
        }

        if (zonaMetodos != null)
        {
            zonaMetodos.SetActive(activas);
        }
    }

    public void MostrarConAnimacion()
    {
        if (!aparecerConSmooth || !isActiveAndEnabled)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }

            transform.localScale = escalaOriginal;
            return;
        }

        if (rutinaAparicion != null)
        {
            StopCoroutine(rutinaAparicion);
        }

        rutinaAparicion = StartCoroutine(AparecerSmooth());
    }

    private IEnumerator AparecerSmooth()
    {
        float tiempo = 0f;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }

        transform.localScale = escalaOriginal * escalaInicialAparicion;

        while (tiempo < duracionAparicion)
        {
            tiempo += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(tiempo / duracionAparicion);
            float smooth = Mathf.SmoothStep(0f, 1f, t);

            if (canvasGroup != null)
            {
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, smooth);
            }

            transform.localScale = Vector3.Lerp(
                escalaOriginal * escalaInicialAparicion,
                escalaOriginal,
                smooth
            );

            yield return null;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }

        transform.localScale = escalaOriginal;
        rutinaAparicion = null;
    }

    public void ConfigurarDictado(string nombreClase, IEnumerable<string> atributos, IEnumerable<string> metodos)
    {
        modoPracticaActual = false;
        nombreClaseActual = nombreClase;

        atributosActuales = atributos != null
            ? atributos.Where(e => !string.IsNullOrWhiteSpace(e)).ToList()
            : new List<string>();

        metodosActuales = metodos != null
            ? metodos.Where(e => !string.IsNullOrWhiteSpace(e)).ToList()
            : new List<string>();

        AplicarTextoConResaltado();
        AjustarTamano();
    }

    public void ConfigurarPractica(string nombreClase, IEnumerable<string> atributosVisibles, IEnumerable<string> metodosVisibles)
    {
        modoPracticaActual = true;
        nombreClaseActual = nombreClase;

        atributosActuales = atributosVisibles != null
            ? atributosVisibles.Where(e => !string.IsNullOrWhiteSpace(e)).ToList()
            : new List<string>();

        metodosActuales = metodosVisibles != null
            ? metodosVisibles.Where(e => !string.IsNullOrWhiteSpace(e)).ToList()
            : new List<string>();

        AplicarTextoConResaltado();
        AjustarTamano();
    }

    public void ConfigurarResaltadoSoloSignos(bool activo)
    {
        resaltarSoloSignosAcceso = activo;
        AplicarTextoConResaltado();
        ActualizarFondosResaltado();
    }

    private void AplicarTextoConResaltado()
    {
        if (textoNombreClase != null)
        {
            textoNombreClase.text = CrearTextoClase(nombreClaseActual);
        }

        if (textoAtributos != null)
        {
            textoAtributos.text = ConstruirTexto(
                atributosActuales,
                "- ",
                TipoResaltado.Atributos,
                TipoResaltado.AtributoEspecifico
            );
        }

        if (textoMetodos != null)
        {
            textoMetodos.text = ConstruirTexto(
                metodosActuales,
                "+ ",
                TipoResaltado.Metodos,
                TipoResaltado.MetodoEspecifico
            );
        }

        AplicarTitulosDePractica();
    }

    private void AplicarTitulosDePractica()
    {
        BuscarTitulosYLineasSiHaceFalta();

        bool mostrarTituloAtributos =
            modoPracticaActual &&
            mostrarTitulosVaciosEnPractica &&
            (atributosActuales == null || atributosActuales.Count == 0);

        bool mostrarTituloMetodos =
            modoPracticaActual &&
            mostrarTitulosVaciosEnPractica &&
            (metodosActuales == null || metodosActuales.Count == 0);

        if (tituloAtributos != null)
        {
            if (!string.IsNullOrWhiteSpace(textoTituloAtributos))
            {
                tituloAtributos.text = textoTituloAtributos;
            }

            tituloAtributos.gameObject.SetActive(mostrarTituloAtributos);
        }

        if (tituloMetodos != null)
        {
            if (!string.IsNullOrWhiteSpace(textoTituloMetodos))
            {
                tituloMetodos.text = textoTituloMetodos;
            }

            tituloMetodos.gameObject.SetActive(mostrarTituloMetodos);
        }

        bool mostrarTextoAtributos =
            !modoPracticaActual ||
            !ocultarTextosVaciosEnPractica ||
            (atributosActuales != null && atributosActuales.Count > 0);

        bool mostrarTextoMetodos =
            !modoPracticaActual ||
            !ocultarTextosVaciosEnPractica ||
            (metodosActuales != null && metodosActuales.Count > 0);

        if (textoAtributos != null)
        {
            textoAtributos.gameObject.SetActive(mostrarTextoAtributos);
        }

        if (textoMetodos != null)
        {
            textoMetodos.gameObject.SetActive(mostrarTextoMetodos);
        }
    }

    private string CrearTextoClase(string nombreClase)
    {
        if (tipoResaltadoActual == TipoResaltado.Clase)
        {
            return MarcarTexto(nombreClase);
        }

        return nombreClase;
    }

    private string ConstruirTexto(
        IEnumerable<string> elementos,
        string prefijo,
        TipoResaltado tipoSeccion,
        TipoResaltado tipoEspecifico)
    {
        if (elementos == null)
        {
            return "";
        }

        List<string> lista = elementos
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .ToList();

        if (lista.Count == 0)
        {
            return "";
        }

        List<string> lineas = new List<string>();

        foreach (string elemento in lista)
        {
            string contenido = QuitarPrefijoAccesoDuplicado(
                elemento,
                prefijo
            );
            bool resaltarTodaSeccion = tipoResaltadoActual == tipoSeccion;
            bool resaltarElemento =
                tipoResaltadoActual == tipoEspecifico &&
                CoincideElemento(contenido, elementoResaltadoActual);

            string linea;
            if (resaltarSoloSignosAcceso)
            {
                bool resaltado =
                    resaltarTodaSeccion || resaltarElemento;
                if (resaltado)
                {
                    // Al marcar una fila se resalta el signo y su nombre para
                    // que sea evidente que atributo o metodo esta activo. El
                    // texto negro mantiene contraste en amarillo, verde y rojo.
                    string signo = string.IsNullOrWhiteSpace(prefijo)
                        ? ""
                        : prefijo.Trim();
                    linea = MarcarTexto(signo + " " + contenido);
                }
                else
                {
                    linea = CrearPrefijoAcceso(prefijo, false) + contenido;
                }
            }
            else
            {
                linea = prefijo + contenido;
                if (resaltarTodaSeccion || resaltarElemento)
                {
                    linea = MarcarTexto(linea);
                }
            }

            lineas.Add(linea);
        }

        // Evita que TMP contabilice una cuarta linea vacia. Esa linea extra
        // reducia el espacio util y hacia que el separador tapara el ultimo
        // atributo (por ejemplo, "- precio").
        return string.Join("\n", lineas);
    }

    private static string QuitarPrefijoAccesoDuplicado(
        string elemento,
        string prefijo)
    {
        string contenido = elemento != null ? elemento.Trim() : "";
        string signo = string.IsNullOrWhiteSpace(prefijo)
            ? ""
            : prefijo.Trim();
        if (!string.IsNullOrEmpty(signo) &&
            contenido.StartsWith(signo))
        {
            contenido = contenido.Substring(signo.Length).TrimStart();
        }
        return contenido;
    }

    private string CrearPrefijoAcceso(string prefijo, bool resaltado)
    {
        string signo = string.IsNullOrWhiteSpace(prefijo) ? "" : prefijo.Trim();
        string colorHex = signo == "-" ? "FF6B73" : "63D9A6";
        string signoConColor = "<color=#" + colorHex + ">" + signo + "</color>";
        if (resaltado)
        {
            signoConColor = MarcarTexto(signoConColor);
        }

        return signoConColor + " ";
    }

    private bool CoincideElemento(string textoCompleto, string textoBuscado)
    {
        if (string.IsNullOrWhiteSpace(textoCompleto) || string.IsNullOrWhiteSpace(textoBuscado))
        {
            return false;
        }

        string completo = NormalizarTexto(textoCompleto);
        string buscado = NormalizarTexto(textoBuscado);

        return completo.Contains(buscado) || buscado.Contains(completo);
    }

    private string NormalizarTexto(string texto)
    {
        return texto
            .ToLower()
            .Replace("()", "")
            .Replace(":", " ")
            .Trim();
    }

    private string MarcarTexto(string texto)
    {
        Color marcaTransparente = colorResaltadoTMP;
        // En Quest el submesh de <mark> puede quedar delante de los glifos.
        // Un fondo deliberadamente translúcido conserva el color de estado
        // sin tapar las letras negras aunque el orden de dibujo cambie.
        marcaTransparente.a = 0.34f;
        string colorHex = ColorUtility.ToHtmlStringRGBA(
            marcaTransparente
        );
        return "<color=#000000FF><b><mark=#" + colorHex + ">" +
               texto + "</mark></b></color>";
    }

    public void ResaltarClase(float duracion = 1f)
    {
        ResaltarTemporal(TipoResaltado.Clase, "", duracion);
    }

    public void ResaltarAtributos(float duracion = 1f)
    {
        ResaltarTemporal(TipoResaltado.Atributos, "", duracion);
    }

    public void ResaltarMetodos(float duracion = 1f)
    {
        ResaltarTemporal(TipoResaltado.Metodos, "", duracion);
    }

    public void MantenerAtributo(string atributo)
    {
        SetResaltado(TipoResaltado.AtributoEspecifico, atributo);
    }

    public void MantenerAtributoConColor(
        string atributo,
        Color colorMarca)
    {
        ConfigurarColorMarca(colorMarca);
        SetResaltado(TipoResaltado.AtributoEspecifico, atributo);
    }

    public void MantenerMetodo(string metodo)
    {
        SetResaltado(TipoResaltado.MetodoEspecifico, metodo);
    }

    public void MantenerMetodoConColor(
        string metodo,
        Color colorMarca)
    {
        ConfigurarColorMarca(colorMarca);
        SetResaltado(TipoResaltado.MetodoEspecifico, metodo);
    }

    public void ResaltarAtributoPorTiempo(string atributo, float duracion)
    {
        ResaltarTemporal(TipoResaltado.AtributoEspecifico, atributo, duracion);
    }

    public void ResaltarMetodoPorTiempo(string metodo, float duracion)
    {
        ResaltarTemporal(TipoResaltado.MetodoEspecifico, metodo, duracion);
    }

    public void LimpiarResaltado()
    {
        if (rutinaTemporal != null)
        {
            StopCoroutine(rutinaTemporal);
            rutinaTemporal = null;
        }

        SetResaltado(TipoResaltado.Ninguno, "");
    }

    private void ConfigurarColorMarca(Color color)
    {
        // Los colores de estado llegan normalmente con alfa 1. En Android
        // eso convertía el resaltado en un rectángulo opaco que ocultaba
        // por completo batería, temperatura, estado, apagar, etc.
        color.a = 0.34f;
        colorResaltadoTMP = color;
        colorResaltado = color;
    }

    private void ResaltarTemporal(TipoResaltado tipo, string elemento, float duracion)
    {
        if (rutinaTemporal != null)
        {
            StopCoroutine(rutinaTemporal);
            rutinaTemporal = null;
        }

        SetResaltado(tipo, elemento);

        if (duracion > 0f)
        {
            rutinaTemporal = StartCoroutine(LimpiarDespuesDe(duracion));
        }
    }

    private IEnumerator LimpiarDespuesDe(float duracion)
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, duracion));
        SetResaltado(TipoResaltado.Ninguno, "");
        rutinaTemporal = null;
    }

    private void SetResaltado(TipoResaltado tipo, string elemento)
    {
        tipoResaltadoActual = tipo;
        elementoResaltadoActual = elemento;

        AplicarTextoConResaltado();
        ActualizarFondosResaltado();
    }

    private void ActualizarFondosResaltado()
    {
        Color colorClase = tipoResaltadoActual == TipoResaltado.Clase
            ? colorResaltado
            : colorFondoNormal;

        bool resaltarFondoAtributos = !resaltarSoloSignosAcceso &&
            (tipoResaltadoActual == TipoResaltado.Atributos ||
             tipoResaltadoActual == TipoResaltado.AtributoEspecifico);
        Color colorAtributos = resaltarFondoAtributos ? colorResaltado : colorFondoNormal;

        bool resaltarFondoMetodos = !resaltarSoloSignosAcceso &&
            (tipoResaltadoActual == TipoResaltado.Metodos ||
             tipoResaltadoActual == TipoResaltado.MetodoEspecifico);
        Color colorMetodos = resaltarFondoMetodos ? colorResaltado : colorFondoNormal;

        if (!isActiveAndEnabled)
        {
            AplicarFondosInmediato(colorClase, colorAtributos, colorMetodos);
            return;
        }

        if (rutinaFondos != null)
        {
            StopCoroutine(rutinaFondos);
        }

        rutinaFondos = StartCoroutine(AnimarFondos(colorClase, colorAtributos, colorMetodos));
    }

    private IEnumerator AnimarFondos(Color colorClase, Color colorAtributos, Color colorMetodos)
    {
        float tiempo = 0f;

        Color inicioClase = fondoClase != null ? fondoClase.color : colorFondoNormal;
        Color inicioAtributos = fondoAtributos != null ? fondoAtributos.color : colorFondoNormal;
        Color inicioMetodos = fondoMetodos != null ? fondoMetodos.color : colorFondoNormal;

        while (tiempo < duracionCambioColor)
        {
            tiempo += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(tiempo / duracionCambioColor);
            float smooth = Mathf.SmoothStep(0f, 1f, t);

            if (fondoClase != null)
            {
                fondoClase.color = Color.Lerp(inicioClase, colorClase, smooth);
            }

            if (fondoAtributos != null)
            {
                fondoAtributos.color = Color.Lerp(inicioAtributos, colorAtributos, smooth);
            }

            if (fondoMetodos != null)
            {
                fondoMetodos.color = Color.Lerp(inicioMetodos, colorMetodos, smooth);
            }

            yield return null;
        }

        if (fondoClase != null)
        {
            fondoClase.color = colorClase;
        }

        if (fondoAtributos != null)
        {
            fondoAtributos.color = colorAtributos;
        }

        if (fondoMetodos != null)
        {
            fondoMetodos.color = colorMetodos;
        }

        rutinaFondos = null;
    }

    private void AplicarFondosInmediato(Color color)
    {
        AplicarFondosInmediato(color, color, color);
    }

    private void AplicarFondosInmediato(Color colorClase, Color colorAtributos, Color colorMetodos)
    {
        if (fondoClase != null)
        {
            fondoClase.color = colorClase;
        }

        if (fondoAtributos != null)
        {
            fondoAtributos.color = colorAtributos;
        }

        if (fondoMetodos != null)
        {
            fondoMetodos.color = colorMetodos;
        }
    }

    private void AplicarFondosResaltadoInmediato()
    {
        Color colorClase = tipoResaltadoActual == TipoResaltado.Clase
            ? colorResaltado
            : colorFondoNormal;

        bool resaltarFondoAtributos = !resaltarSoloSignosAcceso &&
            (tipoResaltadoActual == TipoResaltado.Atributos ||
             tipoResaltadoActual == TipoResaltado.AtributoEspecifico);
        Color colorAtributos = resaltarFondoAtributos ? colorResaltado : colorFondoNormal;

        bool resaltarFondoMetodos = !resaltarSoloSignosAcceso &&
            (tipoResaltadoActual == TipoResaltado.Metodos ||
             tipoResaltadoActual == TipoResaltado.MetodoEspecifico);
        Color colorMetodos = resaltarFondoMetodos ? colorResaltado : colorFondoNormal;

        AplicarFondosInmediato(colorClase, colorAtributos, colorMetodos);
    }

    private void DetenerRutinasVisuales()
    {
        if (rutinaAparicion != null)
        {
            StopCoroutine(rutinaAparicion);
            rutinaAparicion = null;
        }

        if (rutinaFondos != null)
        {
            StopCoroutine(rutinaFondos);
            rutinaFondos = null;
        }

        if (rutinaTemporal != null)
        {
            StopCoroutine(rutinaTemporal);
            rutinaTemporal = null;
        }
    }

    private void AjustarTamano()
    {
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }

        if (layoutElement == null)
        {
            layoutElement = GetComponent<LayoutElement>();
        }

        BuscarTitulosYLineasSiHaceFalta();
        AplicarTitulosDePractica();

        float anchoTitulo = textoNombreClase != null
            ? textoNombreClase.GetPreferredValues(textoNombreClase.text).x
            : 0f;

        float anchoTituloAtributos = EstaTMPActivo(tituloAtributos)
            ? tituloAtributos.GetPreferredValues(tituloAtributos.text).x
            : 0f;

        float anchoTituloMetodos = EstaTMPActivo(tituloMetodos)
            ? tituloMetodos.GetPreferredValues(tituloMetodos.text).x
            : 0f;

        float anchoAtributos = EstaTMPActivo(textoAtributos)
            ? textoAtributos.GetPreferredValues(textoAtributos.text).x
            : 0f;

        float anchoMetodos = EstaTMPActivo(textoMetodos)
            ? textoMetodos.GetPreferredValues(textoMetodos.text).x
            : 0f;

        float anchoFinal = Mathf.Max(
            anchoTitulo,
            anchoTituloAtributos,
            anchoTituloMetodos,
            anchoAtributos,
            anchoMetodos
        ) + paddingExtra;

        anchoFinal = Mathf.Clamp(anchoFinal, anchoMinimo, anchoMaximo);

        float margenSuperior = forzarLayoutCompactoSeguro ? Mathf.Clamp(margenSuperiorLayout, 6f, 14f) : margenSuperiorLayout;
        float sepTituloLinea = forzarLayoutCompactoSeguro ? Mathf.Clamp(separacionTituloLinea, 3f, 7f) : separacionTituloLinea;
        float sepLineaContenido = forzarLayoutCompactoSeguro ? Mathf.Clamp(separacionLineaContenido, 3f, 7f) : separacionLineaContenido;
        float sepSecciones = forzarLayoutCompactoSeguro ? Mathf.Clamp(separacionEntreSecciones, 4f, 10f) : separacionEntreSecciones;
        float altoMinLinea = forzarLayoutCompactoSeguro ? Mathf.Clamp(alturaMinimaLineaTexto, 16f, 24f) : alturaMinimaLineaTexto;
        float margenInferior = forzarLayoutCompactoSeguro ? Mathf.Clamp(margenInferiorLayout, 6f, 14f) : margenInferiorLayout;

        float altoTitulo = Mathf.Max(altoMinLinea, ObtenerAltoTMP(textoNombreClase, anchoFinal));
        float altoTituloAtributos = EstaTMPActivo(tituloAtributos)
            ? Mathf.Max(altoMinLinea, ObtenerAltoTMP(tituloAtributos, anchoFinal))
            : 0f;
        float altoAtributos = Mathf.Max(0f, ObtenerAltoTMP(textoAtributos, anchoFinal));
        float altoTituloMetodos = EstaTMPActivo(tituloMetodos)
            ? Mathf.Max(altoMinLinea, ObtenerAltoTMP(tituloMetodos, anchoFinal))
            : 0f;
        float altoMetodos = Mathf.Max(0f, ObtenerAltoTMP(textoMetodos, anchoFinal));

        float altoAtributosTotal = AltoSeccion(altoTituloAtributos, altoAtributos, altoMinLinea);
        float altoMetodosTotal = AltoSeccion(altoTituloMetodos, altoMetodos, altoMinLinea);

        float altoContenido =
            margenSuperior +
            altoTitulo +
            sepTituloLinea +
            sepLineaContenido +
            altoAtributosTotal +
            sepSecciones +
            sepLineaContenido +
            altoMetodosTotal +
            margenInferior;

        int filasAdicionales = Mathf.Max(
            0,
            (atributosActuales != null ? atributosActuales.Count : 0) - 2
        ) + Mathf.Max(
            0,
            (metodosActuales != null ? metodosActuales.Count : 0) - 2
        );
        float margenFilasAdicionales = filasAdicionales * 10f;
        float altoFinal = Mathf.Max(
            altoMinimo,
            altoContenido + margenFilasAdicionales
        );

        layoutElement.preferredWidth = anchoFinal;
        layoutElement.preferredHeight = altoFinal;
        layoutElement.minWidth = anchoFinal;
        layoutElement.minHeight = altoFinal;
        layoutElement.flexibleWidth = 0;
        layoutElement.flexibleHeight = 0;

        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, anchoFinal);
        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, altoFinal);

        if (reajustarLayoutInterno)
        {
            ReajustarLayoutInterno(
                altoFinal,
                anchoFinal,
                margenSuperior,
                sepTituloLinea,
                sepLineaContenido,
                sepSecciones,
                altoMinLinea
            );
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
    }

    private float AltoSeccion(float altoTituloSeccion, float altoContenidoSeccion, float altoMinLinea)
    {
        float resultado = 0f;

        if (altoTituloSeccion > 0f)
        {
            resultado += altoTituloSeccion;
        }

        if (altoContenidoSeccion > 0f)
        {
            if (resultado > 0f)
            {
                resultado += 2f;
            }

            resultado += Mathf.Max(altoMinLinea, altoContenidoSeccion);
        }

        if (resultado <= 0f)
        {
            resultado = altoMinLinea;
        }

        return resultado;
    }

    private float ObtenerAltoTMP(TMP_Text texto, float ancho)
    {
        if (texto == null || !texto.gameObject.activeSelf || string.IsNullOrWhiteSpace(texto.text))
        {
            return 0f;
        }

        return texto.GetPreferredValues(texto.text, ancho, 1000).y;
    }

    private bool EstaTMPActivo(TMP_Text texto)
    {
        return texto != null && texto.gameObject.activeSelf && !string.IsNullOrWhiteSpace(texto.text);
    }

    private void ReajustarLayoutInterno(
        float altoFinal,
        float anchoFinal,
        float margenSuperior,
        float sepTituloLinea,
        float sepLineaContenido,
        float sepSecciones,
        float altoMinLinea)
    {
        float y = -margenSuperior;
        float anchoTexto = Mathf.Max(10f, anchoFinal - paddingExtra * 0.55f);

        y = PosicionarTMP(textoNombreClase, y, anchoTexto, altoMinLinea);
        y -= sepTituloLinea;

        PosicionarLinea(linea1, y, anchoFinal);
        y -= sepLineaContenido;

        bool hayTituloAtributos = EstaTMPActivo(tituloAtributos);
        bool hayTextoAtributos = EstaTMPActivo(textoAtributos);

        if (hayTituloAtributos)
        {
            y = PosicionarTMP(tituloAtributos, y, anchoTexto, altoMinLinea);
        }

        if (hayTextoAtributos)
        {
            y = PosicionarTMP(textoAtributos, y, anchoTexto, altoMinLinea);
        }
        else if (!hayTituloAtributos)
        {
            y -= altoMinLinea;
        }

        y -= sepSecciones;

        PosicionarLinea(linea2, y, anchoFinal);
        y -= sepLineaContenido;

        bool hayTituloMetodos = EstaTMPActivo(tituloMetodos);
        bool hayTextoMetodos = EstaTMPActivo(textoMetodos);

        if (hayTituloMetodos)
        {
            y = PosicionarTMP(tituloMetodos, y, anchoTexto, altoMinLinea);
        }

        if (hayTextoMetodos)
        {
            y = PosicionarTMP(textoMetodos, y, anchoTexto, altoMinLinea);
        }
    }

    private float PosicionarTMP(TMP_Text texto, float y, float ancho, float altoMinLinea)
    {
        if (texto == null || !texto.gameObject.activeSelf)
        {
            return y;
        }

        RectTransform rt = texto.GetComponent<RectTransform>();

        if (rt == null)
        {
            return y;
        }

        float alto = Mathf.Max(altoMinLinea, ObtenerAltoTMP(texto, ancho));
        float altoConMargen = alto + 8f;

        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, ancho);
        rt.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Vertical,
            altoConMargen
        );

        // El prefab usa un VerticalLayoutGroup. Si no se actualiza tambien
        // el LayoutElement, el grupo vuelve a imponer los 80 px originales
        // y Linea2 termina atravesando el tercer atributo.
        LayoutElement textLayout = texto.GetComponent<LayoutElement>();
        if (textLayout != null)
        {
            textLayout.minHeight = altoConMargen;
            textLayout.preferredHeight = altoConMargen;
            textLayout.flexibleHeight = 0f;
        }

        Vector2 pos = rt.anchoredPosition;
        pos.y = y;
        rt.anchoredPosition = pos;

        return y - altoConMargen;
    }

    private void PosicionarLinea(RectTransform linea, float y, float anchoFinal)
    {
        if (linea == null)
        {
            return;
        }

        Vector2 pos = linea.anchoredPosition;
        pos.y = y;
        linea.anchoredPosition = pos;

        linea.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Horizontal,
            Mathf.Max(20f, anchoFinal - paddingExtra * 0.35f)
        );
    }
}
