using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class AlgoLabClassDiagramController : MonoBehaviour
{
    public enum ModoDiagrama
    {
        DictadoTema,
        Practica
    }

    [Header("Modo")]
    public ModoDiagrama modoActual = ModoDiagrama.DictadoTema;

    [Header("Modo práctica")]
    public bool mostrarSoloObjetoPractica = true;

    [Tooltip("Objeto educativo que se debe mostrar en práctica.")]
    public AlgoLabObjetoEducativo objetoPracticaActivo;

    [Tooltip("Si el objeto de práctica llega vacío, lo busca automáticamente.")]
    public bool buscarObjetoPracticaSiEsNull = true;

    [Tooltip("Nombre de clase que debe buscar en práctica.")]
    public string nombreClasePreferidaPractica = "Vehiculo";

    public string[] clasesSinZonasClasificacion =
        new string[] { "Robot" };

    [Tooltip("Clases que no deben mostrarse en práctica si se hace búsqueda automática.")]
    public string[] clasesIgnoradasEnPractica = new string[] { "Puerta" };

    [Header("Referencias")]
    public Camera camaraReferencia;
    public RectTransform cardsContainer;
    public TMP_Text emptyStateText;
    public AlgoLabClassDiagramCardUI cardPrefab;

    [Header("Detección")]
    public bool mostrarSoloObjetosEnPantalla = false;
    public float distanciaMaxima = 20f;

    [Header("Actualización")]
    public bool actualizarAutomaticamente = true;
    [Tooltip("Cada cuántos segundos el panel vuelve a buscar objetos spawneados. Para tu caso déjalo en 5.")]
    public float intervaloActualizacion = 5f;

    [Header("Filtro en modo tema")]
    [Tooltip("Si está activo, en modo tema solo muestra objetos que tengan nombre de clase y al menos un atributo o método.")]
    public bool mostrarSoloObjetosConDatosEnTema = true;

    [Tooltip("Si está activo, ignora objetos educativos vacíos para que no aparezcan diagramas sin datos.")]
    public bool ignorarObjetosSinClaseEnTema = true;

    [Header("Distribución")]
    public Vector2 posicionInicial = new Vector2(35f, -35f);
    public float separacionX = 340f;
    public float separacionY = 300f;
    public int columnas = 2;

    [Header("Debug")]
    public bool mostrarDebug = true;

    private float tiempoSiguienteActualizacion;

    private readonly Dictionary<AlgoLabObjetoEducativo, AlgoLabClassDiagramCardUI> tarjetas =
        new Dictionary<AlgoLabObjetoEducativo, AlgoLabClassDiagramCardUI>();

    private readonly Dictionary<AlgoLabObjetoEducativo, HashSet<string>> atributosPractica =
        new Dictionary<AlgoLabObjetoEducativo, HashSet<string>>();

    private readonly Dictionary<AlgoLabObjetoEducativo, HashSet<string>> metodosPractica =
        new Dictionary<AlgoLabObjetoEducativo, HashSet<string>>();

    private void Start()
    {
        if (camaraReferencia == null)
        {
            camaraReferencia = Camera.main;
        }

        OcultarPlantillaSiEstaEnEscena();
        RefrescarDiagramas();
    }

    private void Update()
    {
        if (!actualizarAutomaticamente)
        {
            return;
        }

        if (Time.time >= tiempoSiguienteActualizacion)
        {
            tiempoSiguienteActualizacion = Time.time + intervaloActualizacion;
            RefrescarDiagramas();
        }
    }

    public void RefrescarDiagramas()
    {
        if (cardsContainer == null || cardPrefab == null)
        {
            Debug.LogWarning("Faltan referencias en AlgoLabClassDiagramController.");
            return;
        }

        OcultarPlantillaSiEstaEnEscena();

        List<AlgoLabObjetoEducativo> objetosValidos = ObtenerObjetosValidos();

        if (emptyStateText != null)
        {
            emptyStateText.gameObject.SetActive(objetosValidos.Count == 0);
        }

        List<AlgoLabObjetoEducativo> objetosParaEliminar = tarjetas.Keys
            .Where(obj => obj == null || !objetosValidos.Contains(obj))
            .ToList();

        foreach (AlgoLabObjetoEducativo objeto in objetosParaEliminar)
        {
            if (tarjetas.ContainsKey(objeto) && tarjetas[objeto] != null)
            {
                OcultarYDestruir(tarjetas[objeto].gameObject);
            }

            tarjetas.Remove(objeto);
        }

        LimpiarTarjetasHuerfanas();

        for (int i = 0; i < objetosValidos.Count; i++)
        {
            AlgoLabObjetoEducativo objeto = objetosValidos[i];

            if (objeto == null)
            {
                continue;
            }

            if (!tarjetas.ContainsKey(objeto))
            {
                AlgoLabClassDiagramCardUI nuevaTarjeta =
                    Instantiate(cardPrefab, cardsContainer);

                nuevaTarjeta.gameObject.SetActive(true);
                nuevaTarjeta.name = "ClassCard_" + ObtenerNombreClase(objeto);

                // Se posiciona solo cuando nace. Luego el usuario puede mover la tarjeta
                // y el refresco cada 5 segundos NO la devuelve al lugar original.
                PosicionarTarjeta(nuevaTarjeta.GetComponent<RectTransform>(), i);

                tarjetas.Add(objeto, nuevaTarjeta);

                nuevaTarjeta.MostrarConAnimacion();
            }

            ActualizarTarjeta(objeto, tarjetas[objeto]);
        }

        ForzarZonasClasificacionActivas(
            DebeMostrarZonasClasificacion()
        );
    }

    private List<AlgoLabObjetoEducativo> ObtenerObjetosValidos()
    {
        List<AlgoLabObjetoEducativo> objetosValidos = new List<AlgoLabObjetoEducativo>();

        if (modoActual == ModoDiagrama.Practica && mostrarSoloObjetoPractica)
        {
            if ((objetoPracticaActivo == null || !objetoPracticaActivo.gameObject.activeInHierarchy) &&
                buscarObjetoPracticaSiEsNull)
            {
                objetoPracticaActivo = BuscarObjetoPracticaAutomatico();
            }

            if (objetoPracticaActivo != null && objetoPracticaActivo.gameObject.activeInHierarchy)
            {
                objetosValidos.Add(objetoPracticaActivo);

                if (mostrarDebug)
                {
                    Debug.Log("Modo práctica usando objeto: " + ObtenerNombreClase(objetoPracticaActivo));
                }
            }
            else
            {
                if (mostrarDebug)
                {
                    Debug.LogWarning("Modo práctica activo, pero no se encontró objeto de práctica.");
                }
            }

            return objetosValidos;
        }

        AlgoLabObjetoEducativo[] objetos =
            FindObjectsByType<AlgoLabObjetoEducativo>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );

        foreach (AlgoLabObjetoEducativo objeto in objetos)
        {
            if (objeto == null || !objeto.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (modoActual == ModoDiagrama.DictadoTema && !ObjetoValidoParaTema(objeto))
            {
                continue;
            }

            if (mostrarSoloObjetosEnPantalla && !ObjetoEstaVisible(objeto))
            {
                continue;
            }

            objetosValidos.Add(objeto);
        }

        return objetosValidos;
    }

    private bool ObjetoValidoParaTema(AlgoLabObjetoEducativo objeto)
    {
        if (objeto == null)
        {
            return false;
        }

        bool tieneClase = !string.IsNullOrWhiteSpace(objeto.nombreClase);
        bool tieneAtributos = objeto.atributos != null && objeto.atributos.Any(a => !string.IsNullOrWhiteSpace(a));
        bool tieneMetodos = objeto.metodos != null && objeto.metodos.Any(m => !string.IsNullOrWhiteSpace(m));

        if (objeto.forzarVisibleEnDiagramaTema && tieneClase)
        {
            return true;
        }

        if (ignorarObjetosSinClaseEnTema && !tieneClase)
        {
            return false;
        }

        if (mostrarSoloObjetosConDatosEnTema && !tieneAtributos && !tieneMetodos)
        {
            return false;
        }

        return true;
    }

    private AlgoLabObjetoEducativo BuscarObjetoPracticaAutomatico()
    {
        AlgoLabObjetoEducativo[] objetos =
            FindObjectsByType<AlgoLabObjetoEducativo>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );

        if (objetos == null || objetos.Length == 0)
        {
            Debug.LogWarning("No se encontró ningún AlgoLabObjetoEducativo activo.");
            return null;
        }

        string nombrePreferido = Normalizar(nombreClasePreferidaPractica);

        foreach (AlgoLabObjetoEducativo objeto in objetos)
        {
            if (objeto == null)
            {
                continue;
            }

            string nombreClase = Normalizar(objeto.nombreClase);
            string nombreObjeto = Normalizar(objeto.nombreObjeto);
            string nombreGameObject = Normalizar(objeto.gameObject.name);

            if (nombreClase == nombrePreferido ||
                nombreObjeto == nombrePreferido ||
                nombreGameObject.Contains(nombrePreferido))
            {
                Debug.Log("Objeto de práctica encontrado: " + ObtenerNombreClase(objeto));
                return objeto;
            }
        }

        foreach (AlgoLabObjetoEducativo objeto in objetos)
        {
            if (objeto == null)
            {
                continue;
            }

            if (!EsClaseIgnoradaEnPractica(objeto))
            {
                Debug.Log("Objeto de práctica encontrado por fallback: " + ObtenerNombreClase(objeto));
                return objeto;
            }
        }

        return objetos[0];
    }

    private bool EsClaseIgnoradaEnPractica(AlgoLabObjetoEducativo objeto)
    {
        if (objeto == null || clasesIgnoradasEnPractica == null)
        {
            return false;
        }

        string nombreClase = Normalizar(objeto.nombreClase);
        string nombreObjeto = Normalizar(objeto.nombreObjeto);
        string nombreGameObject = Normalizar(objeto.gameObject.name);

        for (int i = 0; i < clasesIgnoradasEnPractica.Length; i++)
        {
            string ignorada = Normalizar(clasesIgnoradasEnPractica[i]);

            if (string.IsNullOrWhiteSpace(ignorada))
            {
                continue;
            }

            if (nombreClase == ignorada ||
                nombreObjeto == ignorada ||
                nombreGameObject.Contains(ignorada))
            {
                return true;
            }
        }

        return false;
    }

    private void ActualizarTarjeta(AlgoLabObjetoEducativo objeto, AlgoLabClassDiagramCardUI tarjeta)
    {
        if (objeto == null || tarjeta == null)
        {
            return;
        }

        string nombreClase = ObtenerNombreClase(objeto);

        if (modoActual == ModoDiagrama.DictadoTema)
        {
            tarjeta.ConfigurarDictado(nombreClase, objeto.atributos, objeto.metodos);
            tarjeta.SetZonasClasificacionActivas(false);
        }
        else
        {
            if (!atributosPractica.ContainsKey(objeto))
            {
                atributosPractica[objeto] = new HashSet<string>();
            }

            if (!metodosPractica.ContainsKey(objeto))
            {
                metodosPractica[objeto] = new HashSet<string>();
            }

            tarjeta.ConfigurarPractica(
                nombreClase,
                atributosPractica[objeto],
                metodosPractica[objeto]
            );

            tarjeta.SetZonasClasificacionActivas(
                DebeMostrarZonasClasificacion()
            );
        }
    }

    public void RegistrarAtributoEncontrado(AlgoLabObjetoEducativo objeto, string atributo)
    {
        if (objeto == null)
        {
            objeto = objetoPracticaActivo;
        }

        if (objeto == null || string.IsNullOrWhiteSpace(atributo))
        {
            Debug.LogWarning("No se pudo registrar atributo porque el objeto o atributo está vacío.");
            return;
        }

        string atributoLimpio = LimpiarNombreElemento(atributo);

        modoActual = ModoDiagrama.Practica;
        objetoPracticaActivo = objeto;

        if (!atributosPractica.ContainsKey(objeto))
        {
            atributosPractica[objeto] = new HashSet<string>();
        }

        atributosPractica[objeto].Add(atributoLimpio);

        Debug.Log("ATRIBUTO AGREGADO AL DIAGRAMA: " + atributoLimpio);

        RefrescarDiagramas();
        ForzarZonasClasificacionActivas(
            DebeMostrarZonasClasificacion()
        );
    }

    public void RegistrarMetodoEncontrado(AlgoLabObjetoEducativo objeto, string metodo)
    {
        if (objeto == null)
        {
            objeto = objetoPracticaActivo;
        }

        if (objeto == null || string.IsNullOrWhiteSpace(metodo))
        {
            Debug.LogWarning("No se pudo registrar método porque el objeto o método está vacío.");
            return;
        }

        string metodoLimpio = LimpiarNombreElemento(metodo);

        modoActual = ModoDiagrama.Practica;
        objetoPracticaActivo = objeto;

        if (!metodosPractica.ContainsKey(objeto))
        {
            metodosPractica[objeto] = new HashSet<string>();
        }

        metodosPractica[objeto].Add(metodoLimpio);

        Debug.Log("MÉTODO AGREGADO AL DIAGRAMA: " + metodoLimpio);

        RefrescarDiagramas();
        ForzarZonasClasificacionActivas(
            DebeMostrarZonasClasificacion()
        );
    }

    private string LimpiarNombreElemento(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return "";
        }

        return texto
            .Replace("()", "")
            .Replace(":", "")
            .Trim();
    }

    private string ObtenerNombreClase(AlgoLabObjetoEducativo objeto)
    {
        if (objeto == null)
        {
            return "Clase";
        }

        return string.IsNullOrWhiteSpace(objeto.nombreClase)
            ? objeto.name
            : objeto.nombreClase;
    }

    private string Normalizar(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return "";
        }

        return texto.Trim().ToLower();
    }

    private void PosicionarTarjeta(RectTransform rectTransform, int index)
    {
        if (rectTransform == null)
        {
            return;
        }

        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(0f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f);

        int columnasSeguras = Mathf.Max(1, columnas);
        int columna = index % columnasSeguras;
        int fila = index / columnasSeguras;

        float x = posicionInicial.x + columna * separacionX;
        float y = posicionInicial.y - fila * separacionY;

        rectTransform.anchoredPosition = new Vector2(x, y);
    }

    private bool ObjetoEstaVisible(AlgoLabObjetoEducativo objeto)
    {
        if (camaraReferencia == null)
        {
            return true;
        }

        float distancia = Vector3.Distance(
            camaraReferencia.transform.position,
            objeto.transform.position
        );

        if (distancia > distanciaMaxima)
        {
            return false;
        }

        Renderer[] renderers = objeto.GetComponentsInChildren<Renderer>();

        if (renderers == null || renderers.Length == 0)
        {
            return true;
        }

        Plane[] planos = GeometryUtility.CalculateFrustumPlanes(camaraReferencia);

        foreach (Renderer renderer in renderers)
        {
            if (renderer != null && GeometryUtility.TestPlanesAABB(planos, renderer.bounds))
            {
                return true;
            }
        }

        return false;
    }

    public void CambiarAModoDictado()
    {
        modoActual = ModoDiagrama.DictadoTema;
        objetoPracticaActivo = null;

        atributosPractica.Clear();
        metodosPractica.Clear();

        LimpiarTarjetasInstanciadas();

        RefrescarDiagramas();
        ForzarZonasClasificacionActivas(false);

        Debug.Log("Diagrama cambiado a MODO DICTADO / TEMA.");
    }

    public void CambiarAModoDictadoTema()
    {
        CambiarAModoDictado();
    }

    public void CambiarAModoPractica()
    {
        modoActual = ModoDiagrama.Practica;

        atributosPractica.Clear();
        metodosPractica.Clear();

        if (objetoPracticaActivo == null && buscarObjetoPracticaSiEsNull)
        {
            objetoPracticaActivo = BuscarObjetoPracticaAutomatico();
        }

        LimpiarTarjetasInstanciadas();

        RefrescarDiagramas();
        ForzarZonasClasificacionActivas(
            DebeMostrarZonasClasificacion()
        );

        Debug.Log("Diagrama cambiado a MODO PRÁCTICA.");
    }

    public void CambiarAModoPracticaConObjeto(AlgoLabObjetoEducativo objetoPractica)
    {
        modoActual = ModoDiagrama.Practica;

        if (objetoPractica != null)
        {
            objetoPracticaActivo = objetoPractica;
        }
        else if (buscarObjetoPracticaSiEsNull)
        {
            objetoPracticaActivo = BuscarObjetoPracticaAutomatico();
        }

        atributosPractica.Clear();
        metodosPractica.Clear();

        LimpiarTarjetasInstanciadas();

        RefrescarDiagramas();
        ForzarZonasClasificacionActivas(
            DebeMostrarZonasClasificacion()
        );

        if (objetoPracticaActivo != null)
        {
            Debug.Log("Diagrama cambiado a MODO PRÁCTICA SOLO CON OBJETO: " + ObtenerNombreClase(objetoPracticaActivo));
        }
        else
        {
            Debug.LogWarning("Diagrama cambiado a práctica, pero no se encontró objeto activo.");
        }
    }

    public void LimpiarTarjetasInstanciadas()
    {
        foreach (KeyValuePair<AlgoLabObjetoEducativo, AlgoLabClassDiagramCardUI> item in tarjetas)
        {
            if (item.Value != null)
            {
                OcultarYDestruir(item.Value.gameObject);
            }
        }

        tarjetas.Clear();

        if (cardsContainer != null)
        {
            for (int i = cardsContainer.childCount - 1; i >= 0; i--)
            {
                Transform hijo = cardsContainer.GetChild(i);

                if (hijo == null)
                {
                    continue;
                }

                if (EsPlantillaCardPrefab(hijo.gameObject))
                {
                    hijo.gameObject.SetActive(false);
                    continue;
                }

                AlgoLabClassDiagramCardUI tarjeta = hijo.GetComponent<AlgoLabClassDiagramCardUI>();

                if (tarjeta != null || hijo.name.ToLower().Contains("classcard"))
                {
                    OcultarYDestruir(hijo.gameObject);
                }
            }
        }

        Debug.Log("Todas las tarjetas del diagrama fueron limpiadas.");
    }

    private void LimpiarTarjetasHuerfanas()
    {
        if (cardsContainer == null)
        {
            return;
        }

        for (int i = cardsContainer.childCount - 1; i >= 0; i--)
        {
            Transform hijo = cardsContainer.GetChild(i);

            if (hijo == null)
            {
                continue;
            }

            if (EsPlantillaCardPrefab(hijo.gameObject))
            {
                hijo.gameObject.SetActive(false);
                continue;
            }

            AlgoLabClassDiagramCardUI tarjeta = hijo.GetComponent<AlgoLabClassDiagramCardUI>();

            if (tarjeta == null)
            {
                continue;
            }

            bool estaRegistrada = tarjetas.ContainsValue(tarjeta);

            if (!estaRegistrada)
            {
                OcultarYDestruir(hijo.gameObject);
            }
        }
    }

    private bool EsPlantillaCardPrefab(GameObject objeto)
    {
        if (objeto == null || cardPrefab == null)
        {
            return false;
        }

        return objeto == cardPrefab.gameObject;
    }

    private void OcultarPlantillaSiEstaEnEscena()
    {
        if (cardPrefab == null)
        {
            return;
        }

        if (cardPrefab.gameObject.scene.IsValid())
        {
            cardPrefab.gameObject.SetActive(false);
        }
    }

    private void OcultarYDestruir(GameObject objeto)
    {
        if (objeto == null)
        {
            return;
        }

        objeto.SetActive(false);

        if (Application.isPlaying)
        {
            Destroy(objeto);
        }
        else
        {
            DestroyImmediate(objeto);
        }
    }

    public void ForzarZonasClasificacionActivas(bool activas)
    {
        AlgoLabClassDiagramCardUI[] tarjetasEnEscena =
            FindObjectsByType<AlgoLabClassDiagramCardUI>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        for (int i = 0; i < tarjetasEnEscena.Length; i++)
        {
            if (tarjetasEnEscena[i] == null)
            {
                continue;
            }

            if (EsPlantillaCardPrefab(tarjetasEnEscena[i].gameObject))
            {
                continue;
            }

            tarjetasEnEscena[i].SetZonasClasificacionActivas(activas);
        }

        Debug.Log("Zonas de clasificación activas: " + activas);
    }

    private bool DebeMostrarZonasClasificacion()
    {
        if (modoActual != ModoDiagrama.Practica)
            return false;
        if (objetoPracticaActivo == null)
            return true;

        string claseActual = Normalizar(
            ObtenerNombreClase(objetoPracticaActivo)
        );
        if (claseActual == "robot")
            return false;
        if (clasesSinZonasClasificacion == null)
            return true;
        for (int i = 0; i < clasesSinZonasClasificacion.Length; i++)
        {
            if (claseActual == Normalizar(
                    clasesSinZonasClasificacion[i]))
            {
                return false;
            }
        }
        return true;
    }

    public AlgoLabClassDiagramCardUI ObtenerTarjetaPorNombreClase(string nombreClase)
    {
        RefrescarDiagramas();

        if (string.IsNullOrWhiteSpace(nombreClase))
        {
            return null;
        }

        foreach (KeyValuePair<AlgoLabObjetoEducativo, AlgoLabClassDiagramCardUI> item in tarjetas)
        {
            if (item.Key == null || item.Value == null)
            {
                continue;
            }

            string nombreObjeto = ObtenerNombreClase(item.Key);

            if (nombreObjeto.Trim().ToLower() == nombreClase.Trim().ToLower())
            {
                return item.Value;
            }
        }

        return null;
    }

    public AlgoLabClassDiagramCardUI ObtenerTarjetaPorObjeto(
        AlgoLabObjetoEducativo objeto)
    {
        if (objeto == null)
        {
            return null;
        }

        AlgoLabClassDiagramCardUI tarjeta;
        return tarjetas.TryGetValue(objeto, out tarjeta) ? tarjeta : null;
    }

    public void ResaltarClase(string nombreClase, float duracion = 1f)
    {
        AlgoLabClassDiagramCardUI tarjeta = ObtenerTarjetaPorNombreClase(nombreClase);

        if (tarjeta != null)
        {
            tarjeta.ResaltarClase(duracion);
        }
    }

    public void ResaltarAtributos(string nombreClase, float duracion = 1f)
    {
        AlgoLabClassDiagramCardUI tarjeta = ObtenerTarjetaPorNombreClase(nombreClase);

        if (tarjeta != null)
        {
            tarjeta.ResaltarAtributos(duracion);
        }
    }

    public void ResaltarMetodos(string nombreClase, float duracion = 1f)
    {
        AlgoLabClassDiagramCardUI tarjeta = ObtenerTarjetaPorNombreClase(nombreClase);

        if (tarjeta != null)
        {
            tarjeta.ResaltarMetodos(duracion);
        }
    }

    public void MantenerAtributo(string nombreClase, string atributo)
    {
        AlgoLabClassDiagramCardUI tarjeta = ObtenerTarjetaPorNombreClase(nombreClase);

        if (tarjeta != null)
        {
            tarjeta.MantenerAtributo(atributo);
        }
    }

    public void MantenerAtributoConColor(
        string nombreClase,
        string atributo,
        Color color)
    {
        AlgoLabClassDiagramCardUI tarjeta =
            ObtenerTarjetaPorNombreClase(nombreClase);

        if (tarjeta != null)
            tarjeta.MantenerAtributoConColor(atributo, color);
    }

    public void MantenerMetodo(string nombreClase, string metodo)
    {
        AlgoLabClassDiagramCardUI tarjeta = ObtenerTarjetaPorNombreClase(nombreClase);

        if (tarjeta != null)
        {
            tarjeta.MantenerMetodo(metodo);
        }
    }

    public void MantenerMetodoConColor(
        string nombreClase,
        string metodo,
        Color color)
    {
        AlgoLabClassDiagramCardUI tarjeta =
            ObtenerTarjetaPorNombreClase(nombreClase);

        if (tarjeta != null)
            tarjeta.MantenerMetodoConColor(metodo, color);
    }

    public void LimpiarResaltado(string nombreClase)
    {
        AlgoLabClassDiagramCardUI tarjeta = ObtenerTarjetaPorNombreClase(nombreClase);

        if (tarjeta != null)
        {
            tarjeta.LimpiarResaltado();
        }
    }

    public void LimpiarTodosLosResaltados()
    {
        foreach (KeyValuePair<AlgoLabObjetoEducativo, AlgoLabClassDiagramCardUI> item in tarjetas)
        {
            if (item.Value != null)
            {
                item.Value.LimpiarResaltado();
            }
        }
    }
}
