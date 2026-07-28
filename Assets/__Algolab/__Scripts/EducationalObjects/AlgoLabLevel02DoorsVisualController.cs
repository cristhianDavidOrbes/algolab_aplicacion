using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AlgoLabLevel02DoorsVisualController : MonoBehaviour
{
    [System.Serializable]
    public class PuertaTemaDatos
    {
        [Header("Identificación")]
        public string nombreObjeto = "Puerta";
        public string nombreClase = "Puerta";

        [Header("Modelo y color")]
        public int indiceVariante = 0;
        public Color colorPuerta = Color.white;
        public bool abrirAlCrear = false;

        [Header("Datos POO")]
        [TextArea(2, 4)]
        public string descripcionObjeto = "Objeto creado a partir de la clase Puerta.";

        public string[] atributos = new string[]
        {
            "color : texto",
            "modelo : texto",
            "estado : texto"
        };

        public string[] metodos = new string[]
        {
            "abrir()",
            "cerrar()"
        };
    }

    [System.Serializable]
    public class ColorPuertaOpcion
    {
        public string nombreColor = "cafe";
        public Color color = new Color(0.45f, 0.25f, 0.10f, 1f);
    }

    [Header("Manual Spawn")]
    public AlgoLabManualPanelSpawnManager spawnManager;
    public bool usarManualSpawn = true;
    public bool actualizarReferenciaAntesDeSpawnear = false;

    [Header("Plano de la clase")]
    [Tooltip("Aquí va el prefab PlanoClasePuerta.")]
    public GameObject planoClasePuertaPrefab;

    [Tooltip("Padre opcional donde se guardará el plano instanciado.")]
    public Transform planoRoot;

    [Tooltip("Escala del plano.")]
    public Vector3 escalaPlanoClase = new Vector3(0.006f, 0.006f, 0.006f);

    [Tooltip("Offset propio del plano desde Posicion Local Objeto Frontal.")]
    public Vector3 offsetLocalPlanoDesdeObjetoFrontal = new Vector3(0.2f, 0.45f, 0f);

    [Tooltip("Rotación extra del plano respecto a la referencia del spawn.")]
    public Vector3 rotacionLocalPlanoEuler = Vector3.zero;

    [Header("Movimiento runtime del plano")]
    public float pasoMovimientoPlano = 0.05f;
    public float duracionMovimientoPlano = 0.25f;

    [Header("Prefab puerta")]
    public GameObject puertaTemaPrefab;

    [Header("Root donde se guardan las puertas")]
    public Transform doorObjectsRoot;

    [Header("Grupo padre de puertas")]
    public string nombreGrupoPuertas = "Level02_DoorGroupRuntime";
    public Vector3 offsetLocalGrupoDesdeObjetoFrontal = new Vector3(0f, -0.25f, 0f);
    public Vector3 rotacionLocalGrupoEuler = Vector3.zero;
    public Vector3 escalaGrupo = Vector3.one;

    [Header("Distribución interna")]
    public float separacionHorizontal = 0.4f;
    public Vector3 escalaPuerta = new Vector3(0.15f, 0.15f, 0.15f);

    [Header("Aparición")]
    public bool aparecerConAnimacion = true;
    public bool crearPuertasUnaPorUna = true;
    public float duracionAparicion = 0.35f;
    public float escalaInicialPuerta = 0.05f;
    public float tiempoEntrePuertas = 0.25f;

    [Header("Movimiento smooth del grupo")]
    public float pasoMovimientoGrupo = 0.05f;
    public float duracionMovimientoGrupo = 0.25f;

    [Header("Spawns antiguos opcionales")]
    [Tooltip("Déjalo apagado si quieres que las puertas salgan centradas dentro del grupo padre.")]
    public bool usarSpawnsIndividualesAntiguos = false;
    public Transform doorSpawn01;
    public Transform doorSpawn02;
    public Transform doorSpawn03;

    [Header("Tiempos de demostración")]
    public float tiempoEntreAcciones = 1f;
    public float tiempoEntreDiferencias = 0.6f;
    public float duracionPulsoCambio = 0.35f;
    public float escalaPulsoCambio = 1.18f;

    [Header("Aleatoriedad inicial")]
    public bool generarDatosAleatorios = true;
    public bool usarSeedFijo = false;
    public int seedFijo = 12345;

    [Tooltip("Dejar en 3 si el prefab solo tiene variantes 0, 1 y 2.")]
    public int cantidadVariantesDisponibles = 3;

    [Header("Tags")]
    public string tagObjeto = "Objeto";

    [Header("Colores aleatorios")]
    public List<ColorPuertaOpcion> coloresDisponibles = new List<ColorPuertaOpcion>()
    {
        new ColorPuertaOpcion()
        {
            nombreColor = "cafe",
            color = new Color(0.45f, 0.25f, 0.10f, 1f)
        },
        new ColorPuertaOpcion()
        {
            nombreColor = "azul",
            color = Color.blue
        },
        new ColorPuertaOpcion()
        {
            nombreColor = "roja",
            color = Color.red
        },
        new ColorPuertaOpcion()
        {
            nombreColor = "verde",
            color = Color.green
        },
        new ColorPuertaOpcion()
        {
            nombreColor = "amarilla",
            color = Color.yellow
        },
        new ColorPuertaOpcion()
        {
            nombreColor = "naranja",
            color = new Color(1f, 0.45f, 0.08f, 1f)
        },
        new ColorPuertaOpcion()
        {
            nombreColor = "blanca",
            color = Color.white
        }
    };

    [Header("Modelos aleatorios")]
    public string[] modelosDisponibles = new string[]
    {
        "madera",
        "moderna",
        "seguridad",
        "clasica",
        "metalica"
    };

    [Header("Estados aleatorios")]
    public string[] estadosDisponibles = new string[]
    {
        "cerrada",
        "abierta",
        "bloqueada"
    };

    [Header("Acciones aleatorias")]
    public bool generarMetodosAleatorios = true;
    public int cantidadMetodosPorPuerta = 2;

    public string[] accionesDisponibles = new string[]
    {
        "abrir()",
        "cerrar()",
        "bloquear()",
        "desbloquear()"
    };

    [Header("Puertas del tema si NO son aleatorias")]
    public List<PuertaTemaDatos> puertasTema = new List<PuertaTemaDatos>()
    {
        new PuertaTemaDatos()
        {
            nombreObjeto = "Puerta Cafe",
            nombreClase = "Puerta",
            indiceVariante = 0,
            colorPuerta = new Color(0.45f, 0.25f, 0.10f, 1f),
            abrirAlCrear = false,
            descripcionObjeto = "Primera puerta creada desde la clase Puerta.",
            atributos = new string[]
            {
                "color : cafe",
                "modelo : madera",
                "estado : cerrada"
            },
            metodos = new string[]
            {
                "abrir()",
                "cerrar()"
            }
        },
        new PuertaTemaDatos()
        {
            nombreObjeto = "Puerta Azul",
            nombreClase = "Puerta",
            indiceVariante = 1,
            colorPuerta = Color.blue,
            abrirAlCrear = true,
            descripcionObjeto = "Segunda puerta creada desde la misma clase Puerta.",
            atributos = new string[]
            {
                "color : azul",
                "modelo : moderna",
                "estado : abierta"
            },
            metodos = new string[]
            {
                "abrir()",
                "cerrar()"
            }
        },
        new PuertaTemaDatos()
        {
            nombreObjeto = "Puerta Roja",
            nombreClase = "Puerta",
            indiceVariante = 2,
            colorPuerta = Color.red,
            abrirAlCrear = false,
            descripcionObjeto = "Tercera puerta creada desde la misma clase Puerta.",
            atributos = new string[]
            {
                "color : roja",
                "modelo : seguridad",
                "estado : cerrada"
            },
            metodos = new string[]
            {
                "abrir()",
                "cerrar()"
            }
        }
    };

    [Header("Diagrama opcional")]
    public AlgoLabClassDiagramController diagramController;

    [Header("Inicio")]
    public bool ocultarTodoAlIniciar = true;
    public bool ocultarPuertasAlIniciar = true;

    [Header("Debug")]
    public bool mostrarDebug = true;

    [Header("Diagrama de clase en modo tema")]
    [Tooltip("Si está activo, después de spawnear las puertas se fuerza el diagrama a modo tema para que muestre atributos y métodos de cada instancia.")]
    public bool refrescarDiagramaTemaDespuesDeCrearPuertas = true;

    private GameObject planoActual;
    private GameObject grupoPuertasActual;

    private readonly List<GameObject> puertasInstanciadas = new List<GameObject>();
    private readonly List<AlgoLabThemeDoorController> controllersInstanciados = new List<AlgoLabThemeDoorController>();
    private readonly List<PuertaTemaDatos> datosRuntime = new List<PuertaTemaDatos>();

    private Coroutine rutinaSpawn;
    private Coroutine rutinaIndependencia;
    private Coroutine rutinaMoverGrupo;
    private Coroutine rutinaDiferencias;
    private Coroutine rutinaMoverPlano;

    private bool animandoEntrada = false;
    private bool animandoCambio = false;

    private float forzarLayoutHasta = -1f;

    private void Start()
    {
        PrepararReferencias();

        if (ocultarTodoAlIniciar)
        {
            OcultarTodo();
        }
        else if (ocultarPuertasAlIniciar)
        {
            LimpiarPuertas();
        }
    }

    private void OnDisable()
    {
        DetenerRutinasInterrumpibles();
    }

    private void LateUpdate()
    {
        if (Time.unscaledTime <= forzarLayoutHasta)
        {
            MantenerPosiciones();

            if (!animandoEntrada && !animandoCambio)
            {
                ForzarEscalasFinales();
            }
        }
    }

    private void PrepararReferencias()
    {
        if (spawnManager == null)
        {
            spawnManager = AlgoLabManualPanelSpawnManager.Instance;
        }

        if (diagramController == null)
        {
            diagramController = FindFirstObjectByType<AlgoLabClassDiagramController>();
        }
    }

    private Transform ObtenerReferenciaSpawn()
    {
        PrepararReferencias();

        if (spawnManager == null)
        {
            return transform;
        }

        if (spawnManager.referenciaManual != null)
        {
            return spawnManager.referenciaManual;
        }

        return spawnManager.transform;
    }

    private Vector3 ObtenerPosicionDesdeObjetoFrontal(Vector3 offsetLocalPropio)
    {
        Transform referencia = ObtenerReferenciaSpawn();

        if (spawnManager == null)
        {
            return referencia.TransformPoint(offsetLocalPropio);
        }

        Vector3 posicionLocalFinal =
            spawnManager.posicionLocalObjetoFrontal + offsetLocalPropio;

        return referencia.TransformPoint(posicionLocalFinal);
    }

    private Quaternion ObtenerRotacionDesdeSpawn(Vector3 rotacionLocalEuler)
    {
        Transform referencia = ObtenerReferenciaSpawn();
        return referencia.rotation * Quaternion.Euler(rotacionLocalEuler);
    }

    private Vector3 ObtenerPosicionGrupoDesdeManualSpawn()
    {
        return ObtenerPosicionDesdeObjetoFrontal(offsetLocalGrupoDesdeObjetoFrontal);
    }

    private Quaternion ObtenerRotacionGrupoDesdeManualSpawn()
    {
        return ObtenerRotacionDesdeSpawn(rotacionLocalGrupoEuler);
    }

    private void ActualizarReferenciaSiCorresponde()
    {
        PrepararReferencias();

        if (actualizarReferenciaAntesDeSpawnear && spawnManager != null)
        {
            spawnManager.ActualizarReferenciaDesdeCabeza();
        }
    }

    [ContextMenu("Ocultar todo")]
    public void OcultarTodo()
    {
        LimpiarPlanoClasePuerta();
        LimpiarPuertas();
    }

    [ContextMenu("Mostrar plano clase puerta")]
    public void MostrarPlanoClasePuerta()
    {
        PrepararReferencias();

        if (planoClasePuertaPrefab == null)
        {
            Debug.LogWarning("DOORS: no hay Plano Clase Puerta Prefab asignado.");
            return;
        }

        ActualizarReferenciaSiCorresponde();
        LimpiarPlanoClasePuerta();

        Vector3 posicion = ObtenerPosicionDesdeObjetoFrontal(offsetLocalPlanoDesdeObjetoFrontal);
        Quaternion rotacion = ObtenerRotacionDesdeSpawn(rotacionLocalPlanoEuler);

        planoActual = Instantiate(
            planoClasePuertaPrefab,
            posicion,
            rotacion,
            planoRoot != null ? planoRoot : null
        );

        planoActual.name = "PlanoClasePuerta_Runtime";
        planoActual.transform.localScale = escalaPlanoClase;
        planoActual.SetActive(true);

        DebugLog(
            "DOORS: PlanoClasePuerta spawneado. Offset: " +
            offsetLocalPlanoDesdeObjetoFrontal +
            " | Posición mundo: " +
            posicion
        );
    }

    [ContextMenu("Ocultar plano clase puerta")]
    public void OcultarPlanoClasePuerta()
    {
        if (planoActual != null)
        {
            planoActual.SetActive(false);
        }
    }

    [ContextMenu("Mostrar plano actual")]
    public void MostrarPlanoActual()
    {
        if (planoActual != null)
        {
            planoActual.SetActive(true);
        }
    }

    [ContextMenu("Limpiar plano clase puerta")]
    public void LimpiarPlanoClasePuerta()
    {
        if (rutinaMoverPlano != null)
        {
            StopCoroutine(rutinaMoverPlano);
            rutinaMoverPlano = null;
        }

        if (planoActual == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(planoActual);
        }
        else
        {
            DestroyImmediate(planoActual);
        }

        planoActual = null;
    }

    public void MoverPlanoXMas()
    {
        MoverPlano(new Vector3(pasoMovimientoPlano, 0f, 0f));
    }

    public void MoverPlanoXMenos()
    {
        MoverPlano(new Vector3(-pasoMovimientoPlano, 0f, 0f));
    }

    public void MoverPlanoYMas()
    {
        MoverPlano(new Vector3(0f, pasoMovimientoPlano, 0f));
    }

    public void MoverPlanoYMenos()
    {
        MoverPlano(new Vector3(0f, -pasoMovimientoPlano, 0f));
    }

    public void MoverPlanoZMas()
    {
        MoverPlano(new Vector3(0f, 0f, pasoMovimientoPlano));
    }

    public void MoverPlanoZMenos()
    {
        MoverPlano(new Vector3(0f, 0f, -pasoMovimientoPlano));
    }

    private void MoverPlano(Vector3 deltaLocal)
    {
        offsetLocalPlanoDesdeObjetoFrontal += deltaLocal;
        ReubicarPlanoConSmooth();
    }

    [ContextMenu("Reubicar plano con smooth")]
    public void ReubicarPlanoConSmooth()
    {
        if (planoActual == null)
        {
            return;
        }

        Vector3 posicionDestino =
            ObtenerPosicionDesdeObjetoFrontal(offsetLocalPlanoDesdeObjetoFrontal);

        Quaternion rotacionDestino =
            ObtenerRotacionDesdeSpawn(rotacionLocalPlanoEuler);

        if (rutinaMoverPlano != null)
        {
            StopCoroutine(rutinaMoverPlano);
        }

        rutinaMoverPlano = StartCoroutine(
            MoverPlanoSmooth(
                planoActual.transform,
                posicionDestino,
                rotacionDestino,
                duracionMovimientoPlano
            )
        );
    }

    private IEnumerator MoverPlanoSmooth(
        Transform objeto,
        Vector3 posicionDestino,
        Quaternion rotacionDestino,
        float duracion)
    {
        yield return MoverObjetoSmooth(objeto, posicionDestino, rotacionDestino, duracion);
        rutinaMoverPlano = null;
    }

    [ContextMenu("Mostrar plano y crear tres puertas")]
    public void MostrarPlanoYCrearTresPuertas()
    {
        MostrarPlanoClasePuerta();
        MostrarTresPuertasHorizontal();
    }

    [ContextMenu("Spawnear tres puertas")]
    public void SpawnearTresPuertas()
    {
        MostrarTresPuertasHorizontal();
    }

    [ContextMenu("Mostrar tres puertas horizontal")]
    public void MostrarTresPuertasHorizontal()
    {
        if (rutinaSpawn != null)
        {
            StopCoroutine(rutinaSpawn);
            rutinaSpawn = null;
        }

        rutinaSpawn = StartCoroutine(MostrarTresPuertasRutina());
    }

    private IEnumerator MostrarTresPuertasRutina()
    {
        DebugLog("DOORS: iniciando creación de 3 puertas.");

        LimpiarPuertasInterno(false);

        if (!ValidarConfiguracionBase())
        {
            rutinaSpawn = null;
            yield break;
        }

        ActualizarReferenciaSiCorresponde();
        PrepararAleatoriedad();
        PrepararDatosRuntime();

        if (datosRuntime.Count < 3)
        {
            Debug.LogError("DOORS: no hay datos suficientes para crear 3 puertas.");
            rutinaSpawn = null;
            yield break;
        }

        CrearGrupoPadrePuertas();

        if (grupoPuertasActual == null)
        {
            Debug.LogError("DOORS: no se pudo crear el grupo padre.");
            rutinaSpawn = null;
            yield break;
        }

        CrearTresPuertasIniciales();

        if (puertasInstanciadas.Count < 3)
        {
            Debug.LogError("DOORS: se esperaban 3 puertas, pero se crearon " + puertasInstanciadas.Count);
        }

        MantenerPosiciones();

        if (aparecerConAnimacion)
        {
            yield return AnimarPuertasEntrada();
        }
        else
        {
            ActivarTodasLasPuertasFinal();
        }

        MantenerLayoutFinal();
        ActivarForzadoLayoutTemporal();
        RefrescarDiagrama();

        DebugLog("DOORS: spawn finalizado. Puertas creadas: " + puertasInstanciadas.Count);

        rutinaSpawn = null;
    }

    private bool ValidarConfiguracionBase()
    {
        PrepararReferencias();

        if (puertaTemaPrefab == null)
        {
            Debug.LogError("DOORS: falta asignar Puerta Tema Prefab.");
            return false;
        }

        if (usarManualSpawn && spawnManager == null)
        {
            Debug.LogError("DOORS: usarManualSpawn está activo pero no hay ManualPanelSpawnManager.");
            return false;
        }

        return true;
    }

    private void PrepararAleatoriedad()
    {
        if (!generarDatosAleatorios)
        {
            return;
        }

        if (usarSeedFijo)
        {
            Random.InitState(seedFijo);
        }
        else
        {
            int seed = (int)(System.DateTime.Now.Ticks % int.MaxValue);
            Random.InitState(seed);
        }
    }

    private void PrepararDatosRuntime()
    {
        datosRuntime.Clear();

        if (generarDatosAleatorios)
        {
            for (int i = 0; i < 3; i++)
            {
                datosRuntime.Add(GenerarDatosAleatoriosPuerta(i));
            }

            return;
        }

        if (puertasTema == null || puertasTema.Count < 3)
        {
            Debug.LogError("DOORS: debes tener 3 puertas configuradas en Puertas Tema.");
            return;
        }

        datosRuntime.Add(puertasTema[0]);
        datosRuntime.Add(puertasTema[1]);
        datosRuntime.Add(puertasTema[2]);
    }

    private PuertaTemaDatos GenerarDatosAleatoriosPuerta(int index)
    {
        PuertaTemaDatos datos = new PuertaTemaDatos();

        datos.nombreObjeto = "Puerta Objeto " + (index + 1);
        datos.nombreClase = "Puerta";
        datos.descripcionObjeto =
            "Objeto creado desde la clase Puerta. Cada puerta tiene valores propios y funciona de forma independiente.";

        ColorPuertaOpcion colorElegido = ObtenerColorAleatorio();

        string modelo = ObtenerTextoAleatorio(modelosDisponibles, "madera");
        string estado = ObtenerTextoAleatorio(estadosDisponibles, "cerrada");

        int maxVariantes = Mathf.Max(1, cantidadVariantesDisponibles);

        datos.indiceVariante = Mathf.Clamp(index, 0, maxVariantes - 1);
        datos.colorPuerta = colorElegido.color;
        datos.abrirAlCrear = estado == "abierta";

        datos.atributos = new string[]
        {
            "color : " + colorElegido.nombreColor,
            "modelo : " + modelo,
            "estado : " + estado
        };

        datos.metodos = ObtenerMetodosParaPuerta();

        return datos;
    }

    private ColorPuertaOpcion ObtenerColorAleatorio()
    {
        if (coloresDisponibles == null || coloresDisponibles.Count == 0)
        {
            return new ColorPuertaOpcion()
            {
                nombreColor = "blanca",
                color = Color.white
            };
        }

        return coloresDisponibles[Random.Range(0, coloresDisponibles.Count)];
    }

    private string ObtenerTextoAleatorio(string[] opciones, string defecto)
    {
        if (opciones == null || opciones.Length == 0)
        {
            return defecto;
        }

        return opciones[Random.Range(0, opciones.Length)];
    }

    private string[] ObtenerMetodosParaPuerta()
    {
        if (!generarMetodosAleatorios)
        {
            return new string[]
            {
                "abrir()",
                "cerrar()"
            };
        }

        if (accionesDisponibles == null || accionesDisponibles.Length == 0)
        {
            return new string[]
            {
                "abrir()",
                "cerrar()"
            };
        }

        List<string> disponibles = new List<string>(accionesDisponibles);
        List<string> seleccionados = new List<string>();

        int cantidad = Mathf.Clamp(cantidadMetodosPorPuerta, 1, disponibles.Count);

        for (int i = 0; i < cantidad; i++)
        {
            int indice = Random.Range(0, disponibles.Count);
            seleccionados.Add(disponibles[indice]);
            disponibles.RemoveAt(indice);
        }

        return seleccionados.ToArray();
    }

    private void CrearGrupoPadrePuertas()
    {
        Vector3 posicionGrupo = ObtenerPosicionGrupoDesdeManualSpawn();
        Quaternion rotacionGrupo = ObtenerRotacionGrupoDesdeManualSpawn();

        grupoPuertasActual = new GameObject(nombreGrupoPuertas);
        AsignarTagSeguro(grupoPuertasActual, tagObjeto);

        grupoPuertasActual.transform.SetPositionAndRotation(posicionGrupo, rotacionGrupo);
        grupoPuertasActual.transform.localScale = escalaGrupo;

        if (doorObjectsRoot != null)
        {
            grupoPuertasActual.transform.SetParent(doorObjectsRoot, true);
        }

        grupoPuertasActual.SetActive(true);
    }

    private void CrearTresPuertasIniciales()
    {
        puertasInstanciadas.Clear();
        controllersInstanciados.Clear();

        for (int i = 0; i < 3; i++)
        {
            GameObject puerta = CrearPuertaEnGrupo(i, datosRuntime[i]);

            if (puerta == null)
            {
                Debug.LogError("DOORS: puerta " + (i + 1) + " no se pudo crear.");
                continue;
            }

            puertasInstanciadas.Add(puerta);
            RegistrarController(puerta);

            if (aparecerConAnimacion)
            {
                puerta.SetActive(false);
                puerta.transform.localScale = escalaPuerta * escalaInicialPuerta;
            }
            else
            {
                puerta.SetActive(true);
                puerta.transform.localScale = escalaPuerta;
            }

            DebugLog("DOORS: puerta " + (i + 1) + " creada.");
        }

        RefrescarDiagramaTemaSiCorresponde();
    }

    private void RefrescarDiagramaTemaSiCorresponde()
    {
        if (!refrescarDiagramaTemaDespuesDeCrearPuertas)
        {
            return;
        }

        if (diagramController == null)
        {
            diagramController = FindFirstObjectByType<AlgoLabClassDiagramController>();
        }

        if (diagramController == null)
        {
            return;
        }

        diagramController.CambiarAModoDictadoTema();
        diagramController.RefrescarDiagramas();
    }

    private GameObject CrearPuertaEnGrupo(int index, PuertaTemaDatos datos)
    {
        if (grupoPuertasActual == null)
        {
            Debug.LogError("DOORS: no existe el grupo padre de puertas.");
            return null;
        }

        if (puertaTemaPrefab == null)
        {
            Debug.LogError("DOORS: falta asignar Puerta Tema Prefab.");
            return null;
        }

        GameObject nuevaPuerta = null;

        try
        {
            nuevaPuerta = Instantiate(puertaTemaPrefab, grupoPuertasActual.transform);
        }
        catch (System.Exception e)
        {
            Debug.LogError("DOORS: error al instanciar puerta " + (index + 1) + ": " + e.Message);
            return null;
        }

        if (nuevaPuerta == null)
        {
            Debug.LogError("DOORS: Instantiate devolvió null en puerta " + (index + 1));
            return null;
        }

        nuevaPuerta.name = datos != null ? datos.nombreObjeto : "Puerta Objeto " + (index + 1);
        nuevaPuerta.SetActive(true);

        AsignarTagSeguro(nuevaPuerta, tagObjeto);

        if (usarSpawnsIndividualesAntiguos)
        {
            Transform spawnAntiguo = ObtenerSpawnAntiguo(index);

            if (spawnAntiguo != null)
            {
                nuevaPuerta.transform.SetPositionAndRotation(spawnAntiguo.position, spawnAntiguo.rotation);
                nuevaPuerta.transform.SetParent(grupoPuertasActual.transform, true);
            }
            else
            {
                AplicarLayoutPuerta(nuevaPuerta.transform, index, true);
            }
        }
        else
        {
            AplicarLayoutPuerta(nuevaPuerta.transform, index, true);
        }

        ConfigurarPuerta(nuevaPuerta, datos);
        AplicarLayoutPuerta(nuevaPuerta.transform, index, true);

        return nuevaPuerta;
    }

    private Transform ObtenerSpawnAntiguo(int index)
    {
        if (index == 0)
        {
            return doorSpawn01;
        }

        if (index == 1)
        {
            return doorSpawn02;
        }

        return doorSpawn03;
    }

    private void AplicarLayoutPuerta(Transform puerta, int index, bool forzarEscala)
    {
        if (puerta == null)
        {
            return;
        }

        AplicarPosicionPuerta(puerta, index);

        if (forzarEscala)
        {
            puerta.localScale = escalaPuerta;
        }
    }

    private void AplicarPosicionPuerta(Transform puerta, int index)
    {
        if (puerta == null)
        {
            return;
        }

        if (usarSpawnsIndividualesAntiguos)
        {
            return;
        }

        float posicionX = (index - 1) * separacionHorizontal;

        puerta.localPosition = new Vector3(posicionX, 0f, 0f);
        puerta.localRotation = Quaternion.identity;
    }

    private IEnumerator AnimarPuertasEntrada()
    {
        animandoEntrada = true;
        animandoCambio = false;
        forzarLayoutHasta = -1f;

        if (crearPuertasUnaPorUna)
        {
            for (int i = 0; i < puertasInstanciadas.Count; i++)
            {
                GameObject puerta = puertasInstanciadas[i];

                if (puerta == null)
                {
                    continue;
                }

                puerta.SetActive(true);
                yield return AnimarEscalaPuerta(puerta.transform, i);

                yield return new WaitForSecondsRealtime(Mathf.Max(0f, tiempoEntrePuertas));
            }
        }
        else
        {
            for (int i = 0; i < puertasInstanciadas.Count; i++)
            {
                if (puertasInstanciadas[i] != null)
                {
                    puertasInstanciadas[i].SetActive(true);
                }
            }

            float tiempo = 0f;

            while (tiempo < duracionAparicion)
            {
                tiempo += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(tiempo / duracionAparicion);
                float smooth = Mathf.SmoothStep(0f, 1f, t);

                for (int i = 0; i < puertasInstanciadas.Count; i++)
                {
                    if (puertasInstanciadas[i] != null)
                    {
                        AplicarPosicionPuerta(puertasInstanciadas[i].transform, i);
                        puertasInstanciadas[i].transform.localScale =
                            Vector3.Lerp(escalaPuerta * escalaInicialPuerta, escalaPuerta, smooth);
                    }
                }

                yield return null;
            }
        }

        animandoEntrada = false;

        MantenerLayoutFinal();
    }

    private IEnumerator AnimarEscalaPuerta(Transform puerta, int index)
    {
        if (puerta == null)
        {
            yield break;
        }

        Vector3 escalaInicio = escalaPuerta * escalaInicialPuerta;
        Vector3 escalaFinal = escalaPuerta;

        puerta.localScale = escalaInicio;

        float tiempo = 0f;

        while (tiempo < duracionAparicion)
        {
            tiempo += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(tiempo / duracionAparicion);
            float smooth = Mathf.SmoothStep(0f, 1f, t);

            AplicarPosicionPuerta(puerta, index);
            puerta.localScale = Vector3.Lerp(escalaInicio, escalaFinal, smooth);

            yield return null;
        }

        AplicarLayoutPuerta(puerta, index, true);
    }

    private void ActivarTodasLasPuertasFinal()
    {
        for (int i = 0; i < puertasInstanciadas.Count; i++)
        {
            if (puertasInstanciadas[i] != null)
            {
                puertasInstanciadas[i].SetActive(true);
                AplicarLayoutPuerta(puertasInstanciadas[i].transform, i, true);
            }
        }
    }

    private void ConfigurarPuerta(GameObject puerta, PuertaTemaDatos datos)
    {
        if (puerta == null || datos == null)
        {
            return;
        }

        AlgoLabThemeDoorController controller =
            puerta.GetComponentInChildren<AlgoLabThemeDoorController>(true);

        if (controller != null)
        {
            int varianteSegura = datos.indiceVariante;

            if (cantidadVariantesDisponibles > 0)
            {
                varianteSegura = Mathf.Clamp(
                    datos.indiceVariante,
                    0,
                    cantidadVariantesDisponibles - 1
                );
            }

            try
            {
                controller.CambiarVariante(varianteSegura);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning(
                    "DOORS: no se pudo cambiar variante en " +
                    puerta.name +
                    ". Variante: " +
                    varianteSegura +
                    ". Error: " +
                    e.Message
                );
            }

            try
            {
                controller.CambiarColor(datos.colorPuerta);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning(
                    "DOORS: no se pudo cambiar color en " +
                    puerta.name +
                    ". Error: " +
                    e.Message
                );

                AplicarColorFallback(puerta, datos.colorPuerta);
            }

            try
            {
                if (datos.abrirAlCrear)
                {
                    controller.AbrirPuerta();
                }
                else
                {
                    controller.CerrarPuerta();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning(
                    "DOORS: no se pudo abrir/cerrar " +
                    puerta.name +
                    ". Error: " +
                    e.Message
                );
            }
        }
        else
        {
            Debug.LogWarning("DOORS: la puerta no tiene AlgoLabThemeDoorController.");
            AplicarColorFallback(puerta, datos.colorPuerta);
        }

        try
        {
            ConfigurarObjetoEducativo(puerta, datos);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning(
                "DOORS: no se pudo configurar ObjetoEducativo en " +
                puerta.name +
                ". Error: " +
                e.Message
            );
        }
    }

    private void AplicarColorFallback(GameObject objeto, Color color)
    {
        if (objeto == null)
        {
            return;
        }

        Renderer[] renderers = objeto.GetComponentsInChildren<Renderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer rendererActual = renderers[i];

            if (rendererActual == null)
            {
                continue;
            }

            Material[] materiales = rendererActual.materials;

            for (int j = 0; j < materiales.Length; j++)
            {
                Material material = materiales[j];

                if (material == null)
                {
                    continue;
                }

                if (material.HasProperty("_BaseColor"))
                {
                    material.SetColor("_BaseColor", color);
                }
                else if (material.HasProperty("_Color"))
                {
                    material.SetColor("_Color", color);
                }
                else
                {
                    material.color = color;
                }
            }
        }
    }

    private void ConfigurarObjetoEducativo(GameObject puerta, PuertaTemaDatos datos)
    {
        if (puerta == null || datos == null)
        {
            return;
        }

        AlgoLabObjetoEducativo objetoEducativo =
            puerta.GetComponentInChildren<AlgoLabObjetoEducativo>(true);

        if (objetoEducativo == null)
        {
            objetoEducativo = puerta.AddComponent<AlgoLabObjetoEducativo>();
        }

        objetoEducativo.nombreObjeto = datos.nombreObjeto;
        objetoEducativo.descripcionObjeto = datos.descripcionObjeto;
        objetoEducativo.nombreClase = datos.nombreClase;
        objetoEducativo.atributos = datos.atributos;
        objetoEducativo.metodos = datos.metodos;
    }

    private void RegistrarController(GameObject puerta)
    {
        if (puerta == null)
        {
            return;
        }

        AlgoLabThemeDoorController controller =
            puerta.GetComponentInChildren<AlgoLabThemeDoorController>(true);

        if (controller != null && !controllersInstanciados.Contains(controller))
        {
            controllersInstanciados.Add(controller);
        }
    }

    [ContextMenu("Mostrar diferencias de atributos y acciones")]
    public void MostrarDiferenciasAtributosYAcciones()
    {
        if (rutinaDiferencias != null)
        {
            StopCoroutine(rutinaDiferencias);
        }

        rutinaDiferencias = StartCoroutine(MostrarDiferenciasAtributosYAccionesRutina());
    }

    private IEnumerator MostrarDiferenciasAtributosYAccionesRutina()
    {
        if (puertasInstanciadas.Count < 3)
        {
            Debug.LogWarning("DOORS: primero debes mostrar las tres puertas.");
            rutinaDiferencias = null;
            yield break;
        }

        animandoCambio = true;

        for (int i = 0; i < puertasInstanciadas.Count; i++)
        {
            GameObject puerta = puertasInstanciadas[i];

            if (puerta == null)
            {
                continue;
            }

            PuertaTemaDatos datosCambio = CrearDatosDemostracionDiferencias(i);

            if (i < datosRuntime.Count)
            {
                datosRuntime[i] = datosCambio;
            }

            ConfigurarPuerta(puerta, datosCambio);
            AplicarLayoutPuerta(puerta.transform, i, true);
            RefrescarDiagrama();

            yield return PulsoPuerta(puerta.transform, i);

            DebugLog("DOORS: puerta " + (i + 1) + " cambió características y acciones.");

            yield return new WaitForSecondsRealtime(Mathf.Max(0f, tiempoEntreDiferencias));
        }

        animandoCambio = false;

        MantenerLayoutFinal();
        ActivarForzadoLayoutTemporal();

        rutinaDiferencias = null;
    }

    private PuertaTemaDatos CrearDatosDemostracionDiferencias(int index)
    {
        PuertaTemaDatos datos = new PuertaTemaDatos();

        datos.nombreClase = "Puerta";
        datos.nombreObjeto = "Puerta Objeto " + (index + 1);
        datos.descripcionObjeto =
            "Objeto creado desde la clase Puerta. Sus características y acciones son propias.";

        if (index == 0)
        {
            datos.indiceVariante = 0;
            datos.colorPuerta = Color.yellow;
            datos.abrirAlCrear = true;
            datos.atributos = new string[]
            {
                "color : amarilla",
                "modelo : moderna",
                "estado : abierta"
            };
            datos.metodos = new string[]
            {
                "abrir()",
                "cerrar()"
            };
        }
        else if (index == 1)
        {
            datos.indiceVariante = 1;
            datos.colorPuerta = Color.green;
            datos.abrirAlCrear = false;
            datos.atributos = new string[]
            {
                "color : verde",
                "modelo : metalica",
                "estado : bloqueada"
            };
            datos.metodos = new string[]
            {
                "bloquear()",
                "desbloquear()"
            };
        }
        else
        {
            datos.indiceVariante = 2;
            datos.colorPuerta = Color.blue;
            datos.abrirAlCrear = true;
            datos.atributos = new string[]
            {
                "color : azul",
                "modelo : seguridad",
                "estado : abierta"
            };
            datos.metodos = new string[]
            {
                "abrir()",
                "cerrar()"
            };
        }

        return datos;
    }

    private IEnumerator PulsoPuerta(Transform puerta, int index)
    {
        if (puerta == null)
        {
            yield break;
        }

        Vector3 escalaNormal = escalaPuerta;
        Vector3 escalaGrande = escalaPuerta * escalaPulsoCambio;

        float mitad = duracionPulsoCambio * 0.5f;
        float tiempo = 0f;

        while (tiempo < mitad)
        {
            tiempo += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(tiempo / mitad);
            float smooth = Mathf.SmoothStep(0f, 1f, t);

            AplicarPosicionPuerta(puerta, index);
            puerta.localScale = Vector3.Lerp(escalaNormal, escalaGrande, smooth);

            yield return null;
        }

        tiempo = 0f;

        while (tiempo < mitad)
        {
            tiempo += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(tiempo / mitad);
            float smooth = Mathf.SmoothStep(0f, 1f, t);

            AplicarPosicionPuerta(puerta, index);
            puerta.localScale = Vector3.Lerp(escalaGrande, escalaNormal, smooth);

            yield return null;
        }

        AplicarLayoutPuerta(puerta, index, true);
    }

    [ContextMenu("Demostrar independencia")]
    public void DemostrarIndependenciaObjetos()
    {
        if (rutinaIndependencia != null)
        {
            StopCoroutine(rutinaIndependencia);
        }

        rutinaIndependencia = StartCoroutine(DemostrarIndependenciaRutina());
    }

    private IEnumerator DemostrarIndependenciaRutina()
    {
        if (controllersInstanciados.Count < 3 || puertasInstanciadas.Count < 3)
        {
            Debug.LogWarning("DOORS: primero debes mostrar las tres puertas.");
            rutinaIndependencia = null;
            yield break;
        }

        CambiarPuertaIndependiente(0, Color.yellow, true, "amarilla", "abierta");

        yield return new WaitForSecondsRealtime(Mathf.Max(0f, tiempoEntreAcciones));

        CambiarPuertaIndependiente(1, Color.green, false, "verde", "cerrada");

        yield return new WaitForSecondsRealtime(Mathf.Max(0f, tiempoEntreAcciones));

        CambiarPuertaIndependiente(2, Color.blue, true, "azul", "abierta");

        MantenerLayoutFinal();

        DebugLog("DOORS: cada puerta cambió de forma independiente.");

        rutinaIndependencia = null;
    }

    private void CambiarPuertaIndependiente(
        int index,
        Color nuevoColor,
        bool abrir,
        string textoColor,
        string textoEstado)
    {
        if (index < 0 || index >= controllersInstanciados.Count)
        {
            return;
        }

        AlgoLabThemeDoorController controller = controllersInstanciados[index];

        if (controller == null)
        {
            return;
        }

        try
        {
            controller.CambiarColor(nuevoColor);

            if (abrir)
            {
                controller.AbrirPuerta();
            }
            else
            {
                controller.CerrarPuerta();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("DOORS: error cambiando puerta independiente: " + e.Message);
        }

        ActualizarAtributosPuerta(index, textoColor, textoEstado);
        RefrescarDiagrama();
        MantenerLayoutFinal();
        ActivarForzadoLayoutTemporal();
    }

    private void ActualizarAtributosPuerta(int index, string nuevoColor, string nuevoEstado)
    {
        if (index < 0 || index >= puertasInstanciadas.Count)
        {
            return;
        }

        GameObject puerta = puertasInstanciadas[index];

        if (puerta == null)
        {
            return;
        }

        AlgoLabObjetoEducativo objetoEducativo =
            puerta.GetComponentInChildren<AlgoLabObjetoEducativo>(true);

        if (objetoEducativo == null)
        {
            return;
        }

        string modeloActual = ObtenerModeloDesdeAtributos(objetoEducativo.atributos);

        objetoEducativo.atributos = new string[]
        {
            "color : " + nuevoColor,
            "modelo : " + modeloActual,
            "estado : " + nuevoEstado
        };
    }

    private string ObtenerModeloDesdeAtributos(string[] atributos)
    {
        if (atributos == null)
        {
            return "modelo";
        }

        for (int i = 0; i < atributos.Length; i++)
        {
            string atributo = atributos[i];

            if (!string.IsNullOrEmpty(atributo) &&
                atributo.ToLower().Contains("modelo"))
            {
                string[] partes = atributo.Split(':');

                if (partes.Length > 1)
                {
                    return partes[1].Trim();
                }
            }
        }

        return "modelo";
    }

    public void MoverGrupoXMas()
    {
        MoverGrupo(new Vector3(pasoMovimientoGrupo, 0f, 0f));
    }

    public void MoverGrupoXMenos()
    {
        MoverGrupo(new Vector3(-pasoMovimientoGrupo, 0f, 0f));
    }

    public void MoverGrupoYMas()
    {
        MoverGrupo(new Vector3(0f, pasoMovimientoGrupo, 0f));
    }

    public void MoverGrupoYMenos()
    {
        MoverGrupo(new Vector3(0f, -pasoMovimientoGrupo, 0f));
    }

    public void MoverGrupoZMas()
    {
        MoverGrupo(new Vector3(0f, 0f, pasoMovimientoGrupo));
    }

    public void MoverGrupoZMenos()
    {
        MoverGrupo(new Vector3(0f, 0f, -pasoMovimientoGrupo));
    }

    public void MoverGrupoPuertasXMas()
    {
        MoverGrupoXMas();
    }

    public void MoverGrupoPuertasXMenos()
    {
        MoverGrupoXMenos();
    }

    public void MoverGrupoPuertasYMas()
    {
        MoverGrupoYMas();
    }

    public void MoverGrupoPuertasYMenos()
    {
        MoverGrupoYMenos();
    }

    public void MoverGrupoPuertasZMas()
    {
        MoverGrupoZMas();
    }

    public void MoverGrupoPuertasZMenos()
    {
        MoverGrupoZMenos();
    }

    private void MoverGrupo(Vector3 deltaLocal)
    {
        offsetLocalGrupoDesdeObjetoFrontal += deltaLocal;
        ReubicarGrupoConSmooth();
    }

    [ContextMenu("Reubicar grupo con smooth")]
    public void ReubicarGrupoConSmooth()
    {
        if (grupoPuertasActual == null)
        {
            return;
        }

        Vector3 posicionDestino = ObtenerPosicionGrupoDesdeManualSpawn();
        Quaternion rotacionDestino = ObtenerRotacionGrupoDesdeManualSpawn();

        if (rutinaMoverGrupo != null)
        {
            StopCoroutine(rutinaMoverGrupo);
        }

        rutinaMoverGrupo = StartCoroutine(
            MoverGrupoSmooth(posicionDestino, rotacionDestino)
        );
    }

    public void ReubicarGrupoPuertasConSmooth()
    {
        ReubicarGrupoConSmooth();
    }

    private IEnumerator MoverGrupoSmooth(Vector3 posicionDestino, Quaternion rotacionDestino)
    {
        if (grupoPuertasActual == null)
        {
            rutinaMoverGrupo = null;
            yield break;
        }

        Transform grupo = grupoPuertasActual.transform;

        Vector3 posicionInicial = grupo.position;
        Quaternion rotacionInicial = grupo.rotation;

        float tiempo = 0f;

        while (tiempo < duracionMovimientoGrupo)
        {
            tiempo += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(tiempo / duracionMovimientoGrupo);
            float smooth = Mathf.SmoothStep(0f, 1f, t);

            if (grupo != null)
            {
                grupo.position = Vector3.Lerp(posicionInicial, posicionDestino, smooth);
                grupo.rotation = Quaternion.Slerp(rotacionInicial, rotacionDestino, smooth);
                grupo.localScale = escalaGrupo;
            }

            MantenerPosiciones();

            yield return null;
        }

        if (grupo != null)
        {
            grupo.position = posicionDestino;
            grupo.rotation = rotacionDestino;
            grupo.localScale = escalaGrupo;
        }

        MantenerLayoutFinal();
        ActivarForzadoLayoutTemporal();

        rutinaMoverGrupo = null;
    }

    private IEnumerator MoverObjetoSmooth(
        Transform objeto,
        Vector3 posicionDestino,
        Quaternion rotacionDestino,
        float duracion)
    {
        if (objeto == null)
        {
            yield break;
        }

        Vector3 posicionInicial = objeto.position;
        Quaternion rotacionInicial = objeto.rotation;

        float tiempo = 0f;

        while (tiempo < duracion)
        {
            tiempo += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(tiempo / duracion);
            float smooth = Mathf.SmoothStep(0f, 1f, t);

            if (objeto != null)
            {
                objeto.position = Vector3.Lerp(posicionInicial, posicionDestino, smooth);
                objeto.rotation = Quaternion.Slerp(rotacionInicial, rotacionDestino, smooth);
            }

            yield return null;
        }

        if (objeto != null)
        {
            objeto.position = posicionDestino;
            objeto.rotation = rotacionDestino;
        }
    }

    private void MantenerPosiciones()
    {
        if (grupoPuertasActual != null)
        {
            grupoPuertasActual.transform.localScale = escalaGrupo;
        }

        for (int i = 0; i < puertasInstanciadas.Count; i++)
        {
            if (puertasInstanciadas[i] != null)
            {
                AplicarPosicionPuerta(puertasInstanciadas[i].transform, i);
            }
        }
    }

    private void ForzarEscalasFinales()
    {
        for (int i = 0; i < puertasInstanciadas.Count; i++)
        {
            if (puertasInstanciadas[i] != null)
            {
                puertasInstanciadas[i].transform.localScale = escalaPuerta;
                puertasInstanciadas[i].SetActive(true);
            }
        }
    }

    private void MantenerLayoutFinal()
    {
        MantenerPosiciones();
        ForzarEscalasFinales();
    }

    private void ActivarForzadoLayoutTemporal()
    {
        forzarLayoutHasta = Time.unscaledTime + 2f;
    }

    [ContextMenu("Ocultar puertas")]
    public void OcultarPuertas()
    {
        if (grupoPuertasActual != null)
        {
            grupoPuertasActual.SetActive(false);
        }
    }

    [ContextMenu("Mostrar puertas instanciadas")]
    public void MostrarPuertasInstanciadas()
    {
        if (grupoPuertasActual != null)
        {
            grupoPuertasActual.SetActive(true);
            MantenerLayoutFinal();
            ActivarForzadoLayoutTemporal();
        }
    }

    [ContextMenu("Limpiar puertas")]
    public void LimpiarPuertas()
    {
        LimpiarPuertasInterno(true);
    }

    private void LimpiarPuertasInterno(bool detenerSpawn)
    {
        if (detenerSpawn && rutinaSpawn != null)
        {
            StopCoroutine(rutinaSpawn);
            rutinaSpawn = null;
        }

        if (rutinaIndependencia != null)
        {
            StopCoroutine(rutinaIndependencia);
            rutinaIndependencia = null;
        }

        if (rutinaMoverGrupo != null)
        {
            StopCoroutine(rutinaMoverGrupo);
            rutinaMoverGrupo = null;
        }

        if (rutinaDiferencias != null)
        {
            StopCoroutine(rutinaDiferencias);
            rutinaDiferencias = null;
        }

        animandoEntrada = false;
        animandoCambio = false;

        if (grupoPuertasActual != null)
        {
            grupoPuertasActual.SetActive(false);

            if (Application.isPlaying)
            {
                Destroy(grupoPuertasActual);
            }
            else
            {
                DestroyImmediate(grupoPuertasActual);
            }
        }
        else
        {
            for (int i = puertasInstanciadas.Count - 1; i >= 0; i--)
            {
                if (puertasInstanciadas[i] != null)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(puertasInstanciadas[i]);
                    }
                    else
                    {
                        DestroyImmediate(puertasInstanciadas[i]);
                    }
                }
            }
        }

        grupoPuertasActual = null;

        puertasInstanciadas.Clear();
        controllersInstanciados.Clear();
        datosRuntime.Clear();

        forzarLayoutHasta = -1f;

        RefrescarDiagrama();
    }

    private void DetenerRutinasInterrumpibles()
    {
        if (rutinaSpawn != null)
        {
            StopCoroutine(rutinaSpawn);
            rutinaSpawn = null;
        }

        if (rutinaIndependencia != null)
        {
            StopCoroutine(rutinaIndependencia);
            rutinaIndependencia = null;
        }

        if (rutinaMoverGrupo != null)
        {
            StopCoroutine(rutinaMoverGrupo);
            rutinaMoverGrupo = null;
        }

        if (rutinaDiferencias != null)
        {
            StopCoroutine(rutinaDiferencias);
            rutinaDiferencias = null;
        }

        if (rutinaMoverPlano != null)
        {
            StopCoroutine(rutinaMoverPlano);
            rutinaMoverPlano = null;
        }

        animandoEntrada = false;
        animandoCambio = false;
        forzarLayoutHasta = -1f;

        if (grupoPuertasActual != null)
            MantenerLayoutFinal();
    }

    private void RefrescarDiagrama()
    {
        if (diagramController != null)
        {
            diagramController.RefrescarDiagramas();
        }
    }

    private void AsignarTagSeguro(GameObject obj, string tag)
    {
        if (obj == null || string.IsNullOrEmpty(tag))
        {
            return;
        }

        try
        {
            obj.tag = tag;
        }
        catch
        {
            DebugLog("DOORS: no existe el tag en Unity: " + tag);
        }
    }

    private void DebugLog(string mensaje)
    {
        if (mostrarDebug)
        {
            Debug.Log(mensaje);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Transform referencia = transform;
        Vector3 baseLocal = Vector3.zero;

        if (spawnManager != null)
        {
            referencia = spawnManager.referenciaManual != null
                ? spawnManager.referenciaManual
                : spawnManager.transform;

            baseLocal = spawnManager.posicionLocalObjetoFrontal;
        }

        Vector3 basePos = referencia.TransformPoint(baseLocal);
        Vector3 grupoPos = referencia.TransformPoint(baseLocal + offsetLocalGrupoDesdeObjetoFrontal);
        Vector3 planoPos = referencia.TransformPoint(baseLocal + offsetLocalPlanoDesdeObjetoFrontal);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireCube(basePos, Vector3.one * 0.2f);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(planoPos, Vector3.one * 0.18f);
        Gizmos.DrawLine(basePos, planoPos);

        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(grupoPos, Vector3.one * 0.25f);
        Gizmos.DrawLine(basePos, grupoPos);
    }
}
