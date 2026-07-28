using System.Collections;
using UnityEngine;

public class AlgoLabLevel02GarageController : MonoBehaviour
{
    public enum EstadoVehiculo
    {
        Nuevo,
        Seminuevo,
        Usado
    }

    [Header("Manual Spawn")]
    public AlgoLabManualPanelSpawnManager spawnManager;
    public bool usarManualSpawn = true;
    public bool actualizarReferenciaAntesDeSpawnear = false;

    [Tooltip("Ajuste extra desde Posicion Local Objeto Frontal del ManualPanelSpawnManager.")]
    public Vector3 offsetLocalGarageDesdeObjetoFrontal = new Vector3(0f, -0.45f, 0.15f);

    public Vector3 rotacionLocalGarageEuler = Vector3.zero;

    [Header("Root del garaje")]
    public Transform garageRoot;

    [Header("Visibilidad del garaje")]
    [Tooltip("Si está activo, el garaje inicia oculto y solo aparece cuando se prepara la práctica del nivel 2.")]
    public bool ocultarGarageAlIniciar = true;

    [Tooltip("Si está activo, el garaje aparece con animación smooth al iniciar la práctica.")]
    public bool aparecerGarageConSmooth = true;

    [Tooltip("Escala inicial de la animación. No cambia el tamaño final real del garaje.")]
    public float escalaInicialAparicionGarage = 0.05f;

    [Tooltip("Duración de la aparición del garaje.")]
    public float duracionAparicionGarage = 0.45f;

    [Tooltip("Si está activo, al limpiar/reintentar la práctica se vuelve a ocultar el garaje.")]
    public bool ocultarGarageAlLimpiarVehiculos = true;

    [Tooltip("Si está activo, al ocultar el garaje se usa smooth.")]
    public bool ocultarGarageConSmooth = true;

    public float duracionOcultarGarage = 0.25f;

    [Tooltip("No tocar normalmente. Esta escala se captura una sola vez al iniciar y será el tamaño real final del garaje.")]
    public Vector3 escalaRealGarage = Vector3.one;

    [Header("Referencia del usuario si NO usas Manual Spawn")]
    public Transform referenciaUsuario;
    public bool buscarCamaraPrincipal = true;

    [Header("Posición del garaje frente al usuario si NO usas Manual Spawn")]
    public bool moverGarageAlIniciarPractica = true;
    public Vector3 offsetDesdeUsuario = new Vector3(0f, -0.45f, 1.4f);
    public Vector3 rotacionExtraGarageEuler = Vector3.zero;
    public bool ignorarInclinacionCabeza = true;
    public bool usarMovimientoSmoothGarage = true;
    public float duracionMovimientoGarage = 0.5f;

    [Header("Referencias del garaje")]
    public Transform puertaGaraje;
    public Transform carSpawnPoint;
    public Transform carExitPoint;
    public Transform carsRoot;

    [Header("Prefab vehículo")]
    public GameObject prefabVehiculo;

    [Header("Puerta")]
    [Tooltip("Movimiento local de la puerta al abrir. Se limita para que no suba demasiado.")]
    public Vector3 desplazamientoPuertaAbierta = new Vector3(0f, 0.06f, 0f);

    [Tooltip("Actívalo para que la puerta nunca se vaya demasiado arriba aunque el valor Y esté alto.")]
    public bool limitarAperturaPuerta = true;

    [Tooltip("Altura máxima real que puede subir la puerta. Si sigue muy alta, baja este valor.")]
    public float aperturaMaximaPuertaY = 0.08f;

    public float duracionMovimientoPuerta = 0.55f;

    [Tooltip("La puerta se cierra cuando el vehículo se aleja del punto de spawn.")]
    public bool cerrarCuandoVehiculoSalga = true;

    [Tooltip("Distancia desde CarSpawnPoint para considerar que el vehículo ya salió del garaje.")]
    public float distanciaSalidaDesdeSpawn = 0.45f;

    [Tooltip("Tiempo de espera después de detectar que el vehículo salió para cerrar la puerta.")]
    public float esperaAntesDeCerrarPuerta = 0.35f;

    [Header("Configuración vehículo")]
    public Vector3 escalaVehiculo = new Vector3(0.08f, 0.08f, 0.08f);
    public bool usarEscalaManual = true;
    public string tagObjeto = "Objeto";

    [Header("Comportamiento del vehículo")]
    public bool moverAlSoltar = true;
    public bool moverSiMetodoAcelerar = true;
    public bool mantenerQuietoHastaSoltar = true;

    [Header("Rigidbody")]
    public bool asegurarRigidbody = true;

    [Tooltip("Desactivado para que el vehículo no caiga apenas aparece dentro del garaje.")]
    public bool usarGravedadAlCrear = false;

    public float linearDampingAlCrear = 3.5f;
    public float angularDampingAlCrear = 4.5f;

    [Header("Limpieza de vehículos")]
    [Tooltip("Si está activo, al reiniciar la práctica también busca y elimina vehículos que hayan salido del CarsRoot.")]
    public bool limpiarVehiculosFueraDeCarsRoot = true;

    [Header("Debug")]
    public bool mostrarDebug = true;

    private Vector3 posicionPuertaCerradaLocal;
    private Vector3 posicionPuertaAbiertaLocal;

    private bool escalaRealGarageCapturada = false;
    private bool garageVisible = true;
    private bool garageVisibleObjetivo = true;

    private int vehiculosCreados;
    private bool creandoVehiculo;
    private int generacionPuerta;

    private Coroutine rutinaPuerta;
    private Coroutine rutinaCerrarCuandoSale;
    private Coroutine rutinaVisibilidadGarage;
    private Coroutine rutinaCrearVehiculo;

    private void Awake()
    {
        BuscarReferenciasAutomaticas();
        CapturarEscalaRealGarageUnaVez();
        InicializarPuerta();
    }

    private void Start()
    {
        if (ocultarGarageAlIniciar)
        {
            OcultarGarageInstantaneo();
        }
    }

    private void OnDisable()
    {
        DetenerOperacionesInterrumpibles();
    }

    private void OnValidate()
    {
        if (limitarAperturaPuerta)
        {
            desplazamientoPuertaAbierta.y = Mathf.Clamp(
                desplazamientoPuertaAbierta.y,
                -aperturaMaximaPuertaY,
                aperturaMaximaPuertaY
            );
        }

        escalaInicialAparicionGarage = Mathf.Clamp(escalaInicialAparicionGarage, 0f, 1f);
        duracionAparicionGarage = Mathf.Max(0.01f, duracionAparicionGarage);
        duracionOcultarGarage = Mathf.Max(0.01f, duracionOcultarGarage);
    }

    private void BuscarReferenciasAutomaticas()
    {
        if (garageRoot == null)
        {
            garageRoot = transform.parent != null ? transform.parent : transform;
        }

        if (spawnManager == null)
        {
            spawnManager = AlgoLabManualPanelSpawnManager.Instance;
        }

        if (referenciaUsuario == null && buscarCamaraPrincipal && Camera.main != null)
        {
            referenciaUsuario = Camera.main.transform;
        }

        if (carSpawnPoint == null)
        {
            carSpawnPoint = BuscarHijoPorNombre(garageRoot, "CarSpawnPoint");
        }

        if (carExitPoint == null)
        {
            carExitPoint = BuscarHijoPorNombre(garageRoot, "CarExitPoint");
        }

        if (carsRoot == null)
        {
            carsRoot = BuscarHijoPorNombre(garageRoot, "CarsRoot");
        }

        if (puertaGaraje == null)
        {
            puertaGaraje = BuscarHijoPorNombre(garageRoot, "puerta");

            if (puertaGaraje == null)
            {
                puertaGaraje = BuscarHijoPorNombre(garageRoot, "garage");
            }
        }
    }

    private Transform BuscarHijoPorNombre(Transform raiz, string nombre)
    {
        if (raiz == null)
        {
            return null;
        }

        if (raiz.name.ToLower() == nombre.ToLower())
        {
            return raiz;
        }

        for (int i = 0; i < raiz.childCount; i++)
        {
            Transform encontrado = BuscarHijoPorNombre(raiz.GetChild(i), nombre);

            if (encontrado != null)
            {
                return encontrado;
            }
        }

        return null;
    }

    private void CapturarEscalaRealGarageUnaVez()
    {
        if (escalaRealGarageCapturada)
        {
            return;
        }

        if (garageRoot == null)
        {
            return;
        }

        if (garageRoot.localScale.sqrMagnitude > 0.0001f)
        {
            escalaRealGarage = garageRoot.localScale;
        }
        else if (escalaRealGarage.sqrMagnitude <= 0.0001f)
        {
            escalaRealGarage = Vector3.one;
        }

        escalaRealGarageCapturada = true;

        DebugLog("GARAGE NIVEL 2: escala real capturada = " + escalaRealGarage);
    }

    [ContextMenu("Capturar escala actual como tamaño real")]
    public void CapturarEscalaActualComoTamanoReal()
    {
        BuscarReferenciasAutomaticas();

        if (garageRoot == null)
        {
            Debug.LogWarning("GARAGE NIVEL 2: no hay garageRoot para capturar escala.");
            return;
        }

        if (garageRoot.localScale.sqrMagnitude <= 0.0001f)
        {
            Debug.LogWarning("GARAGE NIVEL 2: no se puede capturar escala porque el garaje está oculto en escala 0.");
            return;
        }

        escalaRealGarage = garageRoot.localScale;
        escalaRealGarageCapturada = true;

        DebugLog("GARAGE NIVEL 2: escala actual guardada como tamaño real = " + escalaRealGarage);
    }

    private void InicializarPuerta()
    {
        if (puertaGaraje == null)
        {
            return;
        }

        posicionPuertaCerradaLocal = puertaGaraje.localPosition;
        posicionPuertaAbiertaLocal = posicionPuertaCerradaLocal + ObtenerDesplazamientoPuertaSeguro();
    }

    private Vector3 ObtenerDesplazamientoPuertaSeguro()
    {
        Vector3 desplazamiento = desplazamientoPuertaAbierta;

        if (limitarAperturaPuerta)
        {
            desplazamiento.y = Mathf.Clamp(
                desplazamiento.y,
                -aperturaMaximaPuertaY,
                aperturaMaximaPuertaY
            );
        }

        return desplazamiento;
    }

    [ContextMenu("Preparar garage para práctica")]
    public void PrepararGarageParaPractica()
    {
        BuscarReferenciasAutomaticas();
        CapturarEscalaRealGarageUnaVez();
        InicializarPuerta();

        if (usarManualSpawn && actualizarReferenciaAntesDeSpawnear && spawnManager != null)
        {
            spawnManager.ActualizarReferenciaDesdeCabeza();
        }

        if (garageRoot == null)
        {
            Debug.LogError("GARAGE NIVEL 2: falta asignar Garage Root.");
            return;
        }

        if (usarManualSpawn && spawnManager == null)
        {
            Debug.LogError("GARAGE NIVEL 2: usarManualSpawn está activo, pero no existe ManualPanelSpawnManager.");
            return;
        }

        if (!usarManualSpawn && referenciaUsuario == null)
        {
            Debug.LogError("GARAGE NIVEL 2: falta asignar Referencia Usuario o Main Camera.");
            return;
        }

        Vector3 posicionDestino = moverGarageAlIniciarPractica
            ? CalcularPosicionGarage()
            : garageRoot.position;

        Quaternion rotacionDestino = moverGarageAlIniciarPractica
            ? CalcularRotacionGarage()
            : garageRoot.rotation;

        MostrarGarage(posicionDestino, rotacionDestino);
        CerrarPuertaInstantanea();

        DebugLog("GARAGE NIVEL 2: garaje preparado y mostrado para práctica.");
    }

    private void MostrarGarage(Vector3 posicionDestino, Quaternion rotacionDestino)
    {
        if (garageRoot == null)
        {
            return;
        }

        DetenerRutinaVisibilidadGarage();
        garageVisibleObjetivo = true;

        garageRoot.gameObject.SetActive(true);

        if (aparecerGarageConSmooth && Application.isPlaying)
        {
            rutinaVisibilidadGarage = StartCoroutine(
                MostrarGarageSmooth(posicionDestino, rotacionDestino)
            );
        }
        else
        {
            garageRoot.SetPositionAndRotation(posicionDestino, rotacionDestino);
            garageRoot.localScale = escalaRealGarage;
            garageVisible = true;
        }
    }

    private IEnumerator MostrarGarageSmooth(Vector3 posicionDestino, Quaternion rotacionDestino)
    {
        if (garageRoot == null)
        {
            yield break;
        }

        Vector3 posicionInicio = garageRoot.position;
        Quaternion rotacionInicio = garageRoot.rotation;

        if (!garageVisible || garageRoot.localScale.sqrMagnitude < 0.0001f)
        {
            posicionInicio = posicionDestino;
            rotacionInicio = rotacionDestino;
            garageRoot.SetPositionAndRotation(posicionDestino, rotacionDestino);
            garageRoot.localScale = escalaRealGarage * escalaInicialAparicionGarage;
        }

        Vector3 escalaInicio = garageRoot.localScale;
        Vector3 escalaDestino = escalaRealGarage;

        float duracion = Mathf.Max(duracionAparicionGarage, 0.01f);
        float tiempo = 0f;

        while (tiempo < duracion)
        {
            tiempo += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(tiempo / duracion);
            float smooth = Mathf.SmoothStep(0f, 1f, t);

            if (garageRoot != null)
            {
                garageRoot.position = Vector3.Lerp(posicionInicio, posicionDestino, smooth);
                garageRoot.rotation = Quaternion.Slerp(rotacionInicio, rotacionDestino, smooth);
                garageRoot.localScale = Vector3.Lerp(escalaInicio, escalaDestino, smooth);
            }

            yield return null;
        }

        if (garageRoot != null)
        {
            garageRoot.SetPositionAndRotation(posicionDestino, rotacionDestino);
            garageRoot.localScale = escalaDestino;
        }

        garageVisible = true;
        rutinaVisibilidadGarage = null;
    }

    [ContextMenu("Ocultar garage")]
    public void OcultarGarage()
    {
        if (garageRoot == null)
        {
            return;
        }

        DetenerRutinaVisibilidadGarage();
        garageVisibleObjetivo = false;

        if (ocultarGarageConSmooth && Application.isPlaying)
        {
            rutinaVisibilidadGarage = StartCoroutine(OcultarGarageSmooth());
        }
        else
        {
            OcultarGarageInstantaneo();
        }
    }

    public void OcultarGarageInstantaneo()
    {
        if (garageRoot == null)
        {
            return;
        }

        DetenerRutinaVisibilidadGarage();

        garageRoot.localScale = Vector3.zero;
        garageVisible = false;
        garageVisibleObjetivo = false;
    }

    private IEnumerator OcultarGarageSmooth()
    {
        if (garageRoot == null)
        {
            yield break;
        }

        Vector3 escalaInicio = garageRoot.localScale;
        Vector3 escalaDestino = Vector3.zero;

        float duracion = Mathf.Max(duracionOcultarGarage, 0.01f);
        float tiempo = 0f;

        while (tiempo < duracion)
        {
            tiempo += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(tiempo / duracion);
            float smooth = Mathf.SmoothStep(0f, 1f, t);

            if (garageRoot != null)
            {
                garageRoot.localScale = Vector3.Lerp(escalaInicio, escalaDestino, smooth);
            }

            yield return null;
        }

        if (garageRoot != null)
        {
            garageRoot.localScale = Vector3.zero;
        }

        garageVisible = false;
        rutinaVisibilidadGarage = null;
    }

    private void DetenerRutinaVisibilidadGarage()
    {
        if (rutinaVisibilidadGarage != null)
        {
            StopCoroutine(rutinaVisibilidadGarage);
            rutinaVisibilidadGarage = null;
        }
    }

    private Vector3 CalcularPosicionGarage()
    {
        if (usarManualSpawn && spawnManager != null)
        {
            Transform referencia = spawnManager.referenciaManual != null
                ? spawnManager.referenciaManual
                : spawnManager.transform;

            Vector3 posicionLocalFinal =
                spawnManager.posicionLocalObjetoFrontal + offsetLocalGarageDesdeObjetoFrontal;

            return referencia.TransformPoint(posicionLocalFinal);
        }

        Vector3 forward = referenciaUsuario.forward;
        Vector3 right = referenciaUsuario.right;
        Vector3 up = Vector3.up;

        if (ignorarInclinacionCabeza)
        {
            forward.y = 0f;
            right.y = 0f;

            if (forward.sqrMagnitude < 0.001f)
            {
                forward = Vector3.forward;
            }

            forward.Normalize();
            right.Normalize();
        }

        return referenciaUsuario.position +
               right * offsetDesdeUsuario.x +
               up * offsetDesdeUsuario.y +
               forward * offsetDesdeUsuario.z;
    }

    private Quaternion CalcularRotacionGarage()
    {
        if (usarManualSpawn && spawnManager != null)
        {
            Transform referencia = spawnManager.referenciaManual != null
                ? spawnManager.referenciaManual
                : spawnManager.transform;

            return referencia.rotation * Quaternion.Euler(rotacionLocalGarageEuler);
        }

        Vector3 forward = referenciaUsuario.forward;

        if (ignorarInclinacionCabeza)
        {
            forward.y = 0f;

            if (forward.sqrMagnitude < 0.001f)
            {
                forward = Vector3.forward;
            }

            forward.Normalize();
        }

        Quaternion rotacionBase = Quaternion.LookRotation(forward, Vector3.up);
        return rotacionBase * Quaternion.Euler(rotacionExtraGarageEuler);
    }

    public void CrearVehiculoDesdeModoObjeto(AlgoLabClassDiagramModeManager.DatosObjetoModo datos)
    {
        if (datos == null)
        {
            Debug.LogWarning("GARAGE NIVEL 2: no llegaron datos desde el modo objeto.");
            return;
        }

        string colorTexto = ObtenerValorSeguro(datos, "color", "rojo");
        string modelo = ObtenerValorSeguro(datos, "modelo", "2024");
        string carcasa = ObtenerValorSeguro(datos, "carcasa", "Hatchback");
        string estadoTexto = ObtenerValorSeguro(datos, "estado", "nuevo");
        string metodo = datos.metodoSeleccionado;

        if (string.IsNullOrWhiteSpace(metodo))
        {
            metodo = "encender()";
        }

        Color color = ConvertirColor(colorTexto);
        EstadoVehiculo estado = ConvertirEstado(estadoTexto);

        CrearVehiculoDesdeGarage(
            prefabVehiculo,
            color,
            estado,
            modelo,
            carcasa,
            metodo
        );
    }

    public void CrearVehiculosDesdeModoObjeto(AlgoLabClassDiagramModeManager.DatosObjetoModo datos)
    {
        CrearVehiculoDesdeModoObjeto(datos);
    }

    public void CrearVehiculoDesdeGarage(
        GameObject prefab,
        Color color,
        EstadoVehiculo estado,
        string modelo,
        string carcasa,
        string metodo
    )
    {
        if (creandoVehiculo)
        {
            Debug.LogWarning("GARAGE NIVEL 2: ya se está creando un vehículo.");
            return;
        }

        if (prefab == null)
        {
            Debug.LogError("GARAGE NIVEL 2: falta asignar el prefab del vehículo.");
            return;
        }

        if (carSpawnPoint == null)
        {
            Debug.LogError("GARAGE NIVEL 2: falta asignar CarSpawnPoint.");
            return;
        }

        if (!garageVisibleObjetivo)
        {
            Vector3 posicionDestino = moverGarageAlIniciarPractica
                ? CalcularPosicionGarage()
                : garageRoot.position;

            Quaternion rotacionDestino = moverGarageAlIniciarPractica
                ? CalcularRotacionGarage()
                : garageRoot.rotation;

            MostrarGarage(posicionDestino, rotacionDestino);
        }

        if (!isActiveAndEnabled)
        {
            Debug.LogWarning("GARAGE NIVEL 2: no se puede crear un vehículo con el controlador desactivado.");
            return;
        }

        rutinaCrearVehiculo = StartCoroutine(
            FlujoCrearVehiculo(
                prefab,
                color,
                estado,
                modelo,
                carcasa,
                metodo
            )
        );
    }

    private IEnumerator FlujoCrearVehiculo(
        GameObject prefab,
        Color color,
        EstadoVehiculo estado,
        string modelo,
        string carcasa,
        string metodo
    )
    {
        creandoVehiculo = true;

        try
        {
            if (rutinaPuerta != null)
            {
                StopCoroutine(rutinaPuerta);
                rutinaPuerta = null;
            }

            yield return AbrirPuertaRutina();

            GameObject carro = Instantiate(
                prefab,
                carSpawnPoint.position,
                carSpawnPoint.rotation,
                carsRoot != null ? carsRoot : null
            );

            vehiculosCreados++;

            carro.name =
                "Vehiculo_" +
                vehiculosCreados +
                "_" +
                carcasa +
                "_" +
                estado;

            AsignarTagSeguro(carro, tagObjeto);

            if (usarEscalaManual)
                AplicarEscalaMundo(carro.transform, escalaVehiculo);

            AplicarCarcasaVisual(carro, carcasa);
            AplicarColor(carro, color);
            ConfigurarObjetoEducativo(carro, color, modelo, carcasa, estado, metodo);
            PrepararRigidbody(carro);
            PrepararComportamientoVehiculo(carro, estado, color, metodo);

            if (cerrarCuandoVehiculoSalga)
            {
                if (rutinaCerrarCuandoSale != null)
                    StopCoroutine(rutinaCerrarCuandoSale);

                rutinaCerrarCuandoSale = StartCoroutine(CerrarPuertaCuandoVehiculoSalga(carro.transform));
            }

            DebugLog(
                "GARAGE NIVEL 2: vehículo creado dentro del garaje." +
                "\nModelo: " + modelo +
                "\nCarcasa: " + carcasa +
                "\nEstado: " + estado +
                "\nMétodo: " + metodo
            );
        }
        finally
        {
            creandoVehiculo = false;
            rutinaCrearVehiculo = null;
        }
    }

    private IEnumerator CerrarPuertaCuandoVehiculoSalga(Transform vehiculo)
    {
        if (vehiculo == null || carSpawnPoint == null)
        {
            rutinaCerrarCuandoSale = null;
            yield break;
        }

        while (vehiculo != null)
        {
            float distanciaDesdeSpawn = Vector3.Distance(
                vehiculo.position,
                carSpawnPoint.position
            );

            if (distanciaDesdeSpawn >= distanciaSalidaDesdeSpawn)
            {
                break;
            }

            yield return null;
        }

        yield return new WaitForSecondsRealtime(Mathf.Max(0f, esperaAntesDeCerrarPuerta));

        CerrarPuerta();

        rutinaCerrarCuandoSale = null;
    }

    private void AplicarCarcasaVisual(GameObject carro, string carcasa)
    {
        if (carro == null)
        {
            return;
        }

        string buscada = Normalizar(carcasa);

        string[] posibles =
        {
            "hatchback",
            "pickup",
            "towtruck",
            "police"
        };

        bool encontro = false;

        for (int i = 0; i < posibles.Length; i++)
        {
            Transform hijo = BuscarHijoPorNombre(carro.transform, posibles[i]);

            if (hijo != null)
            {
                bool activar = Normalizar(hijo.name) == buscada;
                hijo.gameObject.SetActive(activar);

                if (activar)
                {
                    encontro = true;
                }
            }
        }

        if (!encontro)
        {
            DebugLog("GARAGE NIVEL 2: no se encontró carcasa " + carcasa + ". Se deja el prefab como está.");
        }
    }

    private void PrepararRigidbody(GameObject carro)
    {
        if (carro == null || !asegurarRigidbody)
        {
            return;
        }

        Rigidbody rb = carro.GetComponent<Rigidbody>();

        if (rb == null)
        {
            rb = carro.AddComponent<Rigidbody>();
        }

        rb.useGravity = usarGravedadAlCrear;
        rb.isKinematic = false;
        rb.linearDamping = linearDampingAlCrear;
        rb.angularDamping = angularDampingAlCrear;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    private void PrepararComportamientoVehiculo(
        GameObject carro,
        EstadoVehiculo estado,
        Color color,
        string metodo
    )
    {
        if (carro == null)
        {
            return;
        }

        AlgoLabLevel02VehicleObject vehiculo =
            carro.GetComponent<AlgoLabLevel02VehicleObject>();

        if (vehiculo == null)
        {
            vehiculo = carro.AddComponent<AlgoLabLevel02VehicleObject>();
        }

        vehiculo.Configurar(
            this,
            estado,
            color,
            metodo,
            carExitPoint,
            mantenerQuietoHastaSoltar,
            moverAlSoltar,
            moverSiMetodoAcelerar
        );
    }

    [ContextMenu("Abrir puerta")]
    public void AbrirPuerta()
    {
        if (rutinaPuerta != null)
        {
            StopCoroutine(rutinaPuerta);
        }

        rutinaPuerta = StartCoroutine(AbrirPuertaRutina());
    }

    [ContextMenu("Cerrar puerta")]
    public void CerrarPuerta()
    {
        if (rutinaPuerta != null)
        {
            StopCoroutine(rutinaPuerta);
        }

        rutinaPuerta = StartCoroutine(CerrarPuertaRutina());
    }

    private IEnumerator AbrirPuertaRutina()
    {
        int miGeneracion = ++generacionPuerta;
        yield return MoverPuerta(true, miGeneracion);

        if (miGeneracion == generacionPuerta)
            rutinaPuerta = null;
    }

    private IEnumerator CerrarPuertaRutina()
    {
        int miGeneracion = ++generacionPuerta;
        yield return MoverPuerta(false, miGeneracion);

        if (miGeneracion == generacionPuerta)
            rutinaPuerta = null;
    }

    private IEnumerator MoverPuerta(bool abrir, int miGeneracion)
    {
        if (puertaGaraje == null)
        {
            yield break;
        }

        posicionPuertaAbiertaLocal = posicionPuertaCerradaLocal + ObtenerDesplazamientoPuertaSeguro();

        Vector3 inicio = puertaGaraje.localPosition;
        Vector3 destino = abrir
            ? posicionPuertaAbiertaLocal
            : posicionPuertaCerradaLocal;

        float tiempo = 0f;

        float duracion = Mathf.Max(0.01f, duracionMovimientoPuerta);

        while (tiempo < duracion && miGeneracion == generacionPuerta)
        {
            tiempo += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(tiempo / duracion);
            float smooth = Mathf.SmoothStep(0f, 1f, t);

            puertaGaraje.localPosition = Vector3.Lerp(inicio, destino, smooth);

            yield return null;
        }

        if (miGeneracion == generacionPuerta && puertaGaraje != null)
            puertaGaraje.localPosition = destino;
    }

    private void CerrarPuertaInstantanea()
    {
        if (puertaGaraje != null)
        {
            puertaGaraje.localPosition = posicionPuertaCerradaLocal;
        }
    }

    [ContextMenu("Limpiar vehículos creados")]
    public void LimpiarVehiculosCreados()
    {
        BuscarReferenciasAutomaticas();
        InicializarPuerta();

        if (rutinaCerrarCuandoSale != null)
        {
            StopCoroutine(rutinaCerrarCuandoSale);
            rutinaCerrarCuandoSale = null;
        }

        if (rutinaPuerta != null)
        {
            StopCoroutine(rutinaPuerta);
            rutinaPuerta = null;
        }

        creandoVehiculo = false;

        if (rutinaCrearVehiculo != null)
        {
            StopCoroutine(rutinaCrearVehiculo);
            rutinaCrearVehiculo = null;
        }

        int eliminados = 0;

        eliminados += LimpiarVehiculosDentroDeCarsRoot();

        if (limpiarVehiculosFueraDeCarsRoot)
        {
            eliminados += LimpiarVehiculosSueltosEnEscena();
        }

        vehiculosCreados = 0;

        CerrarPuertaInstantanea();

        if (ocultarGarageAlLimpiarVehiculos)
        {
            OcultarGarage();
        }

        DebugLog("GARAGE NIVEL 2: vehículos limpiados para reintento. Total eliminados: " + eliminados);
    }

    private void DetenerOperacionesInterrumpibles()
    {
        generacionPuerta++;

        if (rutinaCrearVehiculo != null)
        {
            StopCoroutine(rutinaCrearVehiculo);
            rutinaCrearVehiculo = null;
        }

        if (rutinaPuerta != null)
        {
            StopCoroutine(rutinaPuerta);
            rutinaPuerta = null;
        }

        if (rutinaCerrarCuandoSale != null)
        {
            StopCoroutine(rutinaCerrarCuandoSale);
            rutinaCerrarCuandoSale = null;
        }

        DetenerRutinaVisibilidadGarage();
        creandoVehiculo = false;
        CerrarPuertaInstantanea();

        if (garageRoot != null)
        {
            garageRoot.localScale = garageVisibleObjetivo
                ? escalaRealGarage
                : Vector3.zero;
        }

        garageVisible = garageVisibleObjetivo;
    }

    public void LimpiarGarageParaReintento()
    {
        LimpiarVehiculosCreados();
    }

    public void LimpiarTodosLosVehiculos()
    {
        LimpiarVehiculosCreados();
    }

    private int LimpiarVehiculosDentroDeCarsRoot()
    {
        int eliminados = 0;

        if (carsRoot == null)
        {
            return eliminados;
        }

        for (int i = carsRoot.childCount - 1; i >= 0; i--)
        {
            Transform hijo = carsRoot.GetChild(i);

            if (hijo == null)
            {
                continue;
            }

            if (EsVehiculoCreadoPorGarage(hijo.gameObject))
            {
                DestruirObjetoSeguro(hijo.gameObject);
                eliminados++;
            }
        }

        return eliminados;
    }

    private int LimpiarVehiculosSueltosEnEscena()
    {
        int eliminados = 0;

        AlgoLabLevel02VehicleObject[] vehiculos =
            FindObjectsByType<AlgoLabLevel02VehicleObject>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        for (int i = 0; i < vehiculos.Length; i++)
        {
            AlgoLabLevel02VehicleObject vehiculo = vehiculos[i];

            if (vehiculo == null)
            {
                continue;
            }

            GameObject obj = vehiculo.gameObject;

            if (obj == null)
            {
                continue;
            }

            if (!obj.scene.IsValid() || !obj.scene.isLoaded)
            {
                continue;
            }

            if (carsRoot != null && obj.transform.IsChildOf(carsRoot))
            {
                continue;
            }

            if (EsVehiculoCreadoPorGarage(obj))
            {
                DestruirObjetoSeguro(obj);
                eliminados++;
            }
        }

        return eliminados;
    }

    private bool EsVehiculoCreadoPorGarage(GameObject obj)
    {
        if (obj == null)
        {
            return false;
        }

        if (obj.GetComponentInChildren<AlgoLabLevel02VehicleObject>(true) != null)
        {
            return true;
        }

        string nombre = obj.name.ToLower();

        return nombre.Contains("vehiculo_") ||
               nombre.Contains("vehículo_") ||
               nombre.Contains("vehicle_");
    }

    private void DestruirObjetoSeguro(GameObject obj)
    {
        if (obj == null)
        {
            return;
        }

        obj.SetActive(false);

        if (Application.isPlaying)
        {
            Destroy(obj);
        }
        else
        {
            DestroyImmediate(obj);
        }
    }

    private void AplicarColor(GameObject objeto, Color color)
    {
        if (objeto == null)
        {
            return;
        }

        Renderer[] renderers = objeto.GetComponentsInChildren<Renderer>(false);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer rendererActual = renderers[i];

            if (rendererActual == null || DebeIgnorarRenderer(rendererActual))
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

            rendererActual.materials = materiales;
        }
    }

    private bool DebeIgnorarRenderer(Renderer rendererActual)
    {
        string nombre = Normalizar(rendererActual.name);

        return nombre.Contains("wheel") ||
               nombre.Contains("tire") ||
               nombre.Contains("llanta") ||
               nombre.Contains("rueda") ||
               nombre.Contains("glass") ||
               nombre.Contains("window") ||
               nombre.Contains("vidrio");
    }

    private void ConfigurarObjetoEducativo(
        GameObject carro,
        Color color,
        string modelo,
        string carcasa,
        EstadoVehiculo estado,
        string metodo
    )
    {
        if (carro == null)
        {
            return;
        }

        AlgoLabObjetoEducativo objetoEducativo =
            carro.GetComponentInChildren<AlgoLabObjetoEducativo>(true);

        if (objetoEducativo == null)
        {
            objetoEducativo = carro.AddComponent<AlgoLabObjetoEducativo>();
        }

        objetoEducativo.nombreObjeto = carro.name;
        objetoEducativo.nombreClase = "Vehículo";

        objetoEducativo.descripcionObjeto =
            "Vehículo creado en el modo práctica a partir de la clase Vehículo.";

        objetoEducativo.atributos = new string[]
        {
            "color : " + ObtenerNombreColor(color),
            "modelo : " + modelo,
            "carcasa : " + carcasa,
            "estado : " + estado.ToString().ToLower()
        };

        objetoEducativo.metodos = new string[]
        {
            metodo
        };
    }

    private void AplicarEscalaMundo(Transform target, Vector3 escalaMundo)
    {
        if (target == null)
        {
            return;
        }

        Transform parent = target.parent;

        if (parent == null)
        {
            target.localScale = escalaMundo;
            return;
        }

        Vector3 parentScale = parent.lossyScale;

        target.localScale = new Vector3(
            DividirSeguro(escalaMundo.x, parentScale.x),
            DividirSeguro(escalaMundo.y, parentScale.y),
            DividirSeguro(escalaMundo.z, parentScale.z)
        );
    }

    private string ObtenerValorSeguro(
        AlgoLabClassDiagramModeManager.DatosObjetoModo datos,
        string atributo,
        string valorDefecto
    )
    {
        string valor = datos.ObtenerValorAtributo(atributo);

        if (string.IsNullOrWhiteSpace(valor))
        {
            return valorDefecto;
        }

        return valor.Trim();
    }

    private EstadoVehiculo ConvertirEstado(string estadoTexto)
    {
        string estado = Normalizar(estadoTexto);

        if (estado == "seminuevo" ||
            estado == "semi-nuevo" ||
            estado == "semiusado" ||
            estado == "semi-usado")
        {
            return EstadoVehiculo.Seminuevo;
        }

        if (estado == "usado")
        {
            return EstadoVehiculo.Usado;
        }

        return EstadoVehiculo.Nuevo;
    }

    private Color ConvertirColor(string colorTexto)
    {
        string color = Normalizar(colorTexto);

        if (color == "rojo")
        {
            return Color.red;
        }

        if (color == "azul")
        {
            return Color.blue;
        }

        if (color == "negro")
        {
            return Color.black;
        }

        if (color == "blanco")
        {
            return Color.white;
        }

        if (color == "amarillo")
        {
            return Color.yellow;
        }

        if (color == "verde")
        {
            return Color.green;
        }

        return Color.white;
    }

    private string ObtenerNombreColor(Color color)
    {
        if (color == Color.red)
        {
            return "rojo";
        }

        if (color == Color.blue)
        {
            return "azul";
        }

        if (color == Color.black)
        {
            return "negro";
        }

        if (color == Color.white)
        {
            return "blanco";
        }

        if (color == Color.yellow)
        {
            return "amarillo";
        }

        if (color == Color.green)
        {
            return "verde";
        }

        return "personalizado";
    }

    private float DividirSeguro(float valor, float divisor)
    {
        if (Mathf.Abs(divisor) < 0.0001f)
        {
            return valor;
        }

        return valor / divisor;
    }

    private void AsignarTagSeguro(GameObject obj, string tag)
    {
        if (obj == null || string.IsNullOrWhiteSpace(tag))
        {
            return;
        }

        try
        {
            obj.tag = tag;
        }
        catch
        {
            DebugLog("GARAGE NIVEL 2: no existe el tag en Unity: " + tag);
        }
    }

    private string Normalizar(string texto)
    {
        if (texto == null)
        {
            return "";
        }

        return texto.Trim().ToLower();
    }

    private void DebugLog(string mensaje)
    {
        if (mostrarDebug)
        {
            Debug.Log(mensaje);
        }
    }
}
