using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AlgoLabLevel02VehicleObject : MonoBehaviour
{
    [Header("Datos")]
    public AlgoLabLevel02GarageController.EstadoVehiculo estadoVehiculo =
        AlgoLabLevel02GarageController.EstadoVehiculo.Nuevo;

    public Color colorVehiculo = Color.white;
    public string metodoSeleccionado = "encender()";

    [Header("Movimiento")]
    public float velocidad = 0.8f;
    public float velocidadAcelerado = 1.4f;
    public float velocidadRotacion = 4f;
    public float tiempoRetroceso = 0.7f;
    public float tiempoGiro = 0.8f;
    public float distanciaRaycastObstaculo = 0.45f;

    [Header("Movimiento hacia punto señalado")]
    public bool permitirMovimientoPorDestino = true;
    public float velocidadDestino = 1.1f;
    public float velocidadRotacionDestino = 360f;
    public float distanciaLlegadaDestino = 0.25f;
    public bool enderezarSiEstaVolteadoAlRecibirDestino = true;
    public float alturaRaycastEnderezar = 1.5f;
    public float alturaExtraAlEnderezar = 0.12f;

    [Header("Condición para conducir")]
    public bool conducirSoloSiEstaDerecho = true;
    public bool conducirSoloSiTocaSuelo = true;
    public float minimoDotVerticalParaConducir = 0.65f;
    public float distanciaRaycastSuelo = 0.18f;
    public float tiempoEstableAntesDeConducir = 0.35f;
    public LayerMask capasSuelo = ~0;

    [Header("Partículas ruedas")]
    public bool usarParticulasRuedas = true;
    public bool buscarParticulasAutomaticamente = true;

    [Tooltip("Arrastra aquí FX_DirtSplatter_L y FX_DirtSplatter_R.")]
    public ParticleSystem[] particulasRuedas;

    public bool forzarLoopParticulasRuedas = true;
    public bool limpiarParticulasAlDetener = true;

    [Header("Fragilidad por impacto")]
    public bool evaluarImpactoSoloDespuesDeSoltar = true;
    public float tiempoProteccionInicial = 0.35f;

    public float impactoMaximoSeminuevo = 6f;
    public float impactoMaximoUsado = 3f;

    public bool seminuevoExplotaConImpactoFuerte = true;
    public bool usadoExplotaConImpactoFuerte = true;

    [Header("Daño por altura de caída")]
    public bool destruirPorAlturaDeCaida = true;

    [Tooltip("Altura mínima para que un vehículo seminuevo explote al caer.")]
    public float alturaMinimaCaidaSeminuevo = 0.65f;

    [Tooltip("Altura mínima para que un vehículo usado explote al caer.")]
    public float alturaMinimaCaidaUsado = 0.3f;

    [Tooltip("Tiempo mínimo después de soltar para empezar a evaluar caída.")]
    public float tiempoMinimoAntesDeEvaluarCaida = 0.25f;

    [Header("Explosión")]
    [Tooltip("Si tienes un prefab aparte de explosión, arrástralo aquí.")]
    public GameObject prefabExplosion;

    [Tooltip("Si la explosión está dentro del prefab del carro, arrastra aquí FX_Explosion_Smoke.")]
    public ParticleSystem particulaExplosion;

    [Tooltip("Tiempo que queda viva la explosión después de destruir el carro.")]
    public float tiempoDestruirParticulaExplosion = 2.5f;

    public bool destruirAlExplotar = true;
    public float tiempoAntesDeDestruir = 0.15f;

    [Header("Debug")]
    public bool mostrarDebug = true;

    [Header("Práctica nivel 2")]
    [Tooltip("Si está vacío, se busca automáticamente en la escena.")]
    public AlgoLabLevel02PracticeController practiceController;

    [Tooltip("Busca automáticamente el controlador de práctica del nivel 2 si no está asignado.")]
    public bool buscarPracticeControllerAutomaticamente = true;

    [Tooltip("Si está activo, cuando el vehículo se daña o explota se reporta a la práctica para aplicar penalización.")]
    public bool notificarDanoOExplosionAPractica = true;

    private Rigidbody rb;
    private SimpleMRGrabbable grabbable;
    private Light luzMotor;

    private bool moverAlSoltar = true;
    private bool moverSiMetodoAcelerar = true;

    private bool estaAgarrado = false;
    private bool fueSoltado = false;
    private bool permisoParaConducir = false;
    private bool metodoEjecutado = false;
    private bool movimientoActivado = false;

    private bool movimientoPorDestinoActivo = false;
    private Vector3 destinoOrdenado;
    private RigidbodyConstraints restriccionesAntesDestino;
    private bool restriccionesDestinoGuardadas = false;

    private bool vehiculoDañado = false;
    private bool vehiculoExplotado = false;
    private bool particulasRuedasActivas = false;
    private bool danoOExplosionNotificadaAPractica = false;

    private float tiempoEstableEnSuelo = 0f;
    private float tiempoCreacion = 0f;

    private float alturaMaximaDesdeSoltado = 0f;
    private float tiempoUltimoSoltado = 0f;
    private bool siguiendoCaida = false;
    private bool yaEvaluoCaidaActual = false;

    private Coroutine rutinaManiobra;
    private AlgoLabLevel02GarageController garageController;

    public void Configurar(
        AlgoLabLevel02GarageController controller,
        AlgoLabLevel02GarageController.EstadoVehiculo estado,
        Color color,
        string metodo,
        Transform exitPoint,
        bool mantenerQuietoHastaSoltar,
        bool permitirMoverAlSoltar,
        bool permitirMoverSiAcelera
    )
    {
        garageController = controller;

        estadoVehiculo = estado;
        colorVehiculo = color;
        metodoSeleccionado = metodo;

        moverAlSoltar = permitirMoverAlSoltar;
        moverSiMetodoAcelerar = permitirMoverSiAcelera;

        rb = GetComponent<Rigidbody>();

        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        grabbable = GetComponent<SimpleMRGrabbable>();

        if (grabbable == null)
        {
            grabbable = gameObject.AddComponent<SimpleMRGrabbable>();
        }

        grabbable.OnGrabStarted -= NotificarAgarrado;
        grabbable.OnGrabEnded -= NotificarSoltado;
        grabbable.OnGrabStarted += NotificarAgarrado;
        grabbable.OnGrabEnded += NotificarSoltado;

        rb.useGravity = false;
        rb.isKinematic = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.linearDamping = 0.2f;
        rb.angularDamping = 0.2f;

        tiempoCreacion = Time.time;
        BuscarPracticeControllerSiHaceFalta();

        CrearLuzMotorSiNoExiste();
        BuscarParticulasSiEsNecesario();
        PrepararParticulas();

        estaAgarrado = false;
        fueSoltado = false;
        permisoParaConducir = false;
        metodoEjecutado = false;
        movimientoActivado = false;
        movimientoPorDestinoActivo = false;
        RestaurarRestriccionesDespuesDeDestino();
        vehiculoDañado = false;
        vehiculoExplotado = false;
        particulasRuedasActivas = false;
        danoOExplosionNotificadaAPractica = false;
        tiempoEstableEnSuelo = 0f;

        siguiendoCaida = false;
        yaEvaluoCaidaActual = false;
        alturaMaximaDesdeSoltado = transform.position.y;

        DebugLog("VEHICULO NIVEL 2: configurado. Estado: " + estadoVehiculo + " Método: " + metodoSeleccionado);
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        grabbable = GetComponent<SimpleMRGrabbable>();

        tiempoCreacion = Time.time;

        BuscarPracticeControllerSiHaceFalta();
        BuscarParticulasSiEsNecesario();
        PrepararParticulas();
    }

    private void OnDestroy()
    {
        if (grabbable != null)
        {
            grabbable.OnGrabStarted -= NotificarAgarrado;
            grabbable.OnGrabEnded -= NotificarSoltado;
        }
    }

    private void OnDisable()
    {
        if (rutinaManiobra != null)
        {
            StopCoroutine(rutinaManiobra);
            rutinaManiobra = null;
        }

        permisoParaConducir = false;
        movimientoActivado = false;
        movimientoPorDestinoActivo = false;
        RestaurarRestriccionesDespuesDeDestino();
        DetenerParticulasRuedas();

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (luzMotor != null)
            luzMotor.enabled = false;
    }

    private void Update()
    {
        if (EstaAgarrado())
        {
            DetenerParticulasRuedas();
            return;
        }

        ActualizarDañoPorCaida();
        RevisarCondicionesParaConducir();
        ActualizarParticulasRuedas();
    }

    private void FixedUpdate()
    {
        if (EstaAgarrado())
        {
            DetenerParticulasRuedas();
            return;
        }

        if (vehiculoDañado || vehiculoExplotado)
        {
            movimientoPorDestinoActivo = false;
            RestaurarRestriccionesDespuesDeDestino();
            DetenerParticulasRuedas();
            return;
        }

        if (movimientoPorDestinoActivo)
        {
            MoverHaciaDestinoOrdenado();
            return;
        }

        if (!movimientoActivado)
        {
            DetenerParticulasRuedas();
            return;
        }

        if (!PuedeConducirAhora())
        {
            DetenerMovimientoHorizontal();
            movimientoActivado = false;
            metodoEjecutado = false;
            tiempoEstableEnSuelo = 0f;
            DetenerParticulasRuedas();
            return;
        }

        ActivarParticulasRuedas();
        MoverVehiculoLibre();
    }

    private bool EstaAgarrado()
    {
        return estaAgarrado || (grabbable != null && grabbable.IsGrabbed);
    }

    public void NotificarAgarrado()
    {
        if (estaAgarrado)
        {
            return;
        }

        estaAgarrado = true;
        RestaurarRestriccionesDespuesDeDestino();
        permisoParaConducir = false;
        metodoEjecutado = false;
        movimientoActivado = false;
        movimientoPorDestinoActivo = false;
        tiempoEstableEnSuelo = 0f;

        siguiendoCaida = false;
        yaEvaluoCaidaActual = false;

        DetenerParticulasRuedas();

        if (rutinaManiobra != null)
        {
            StopCoroutine(rutinaManiobra);
            rutinaManiobra = null;
        }

        if (rb != null)
        {
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        DebugLog("VEHICULO NIVEL 2: agarrado. Partículas apagadas.");
    }

    public void NotificarSoltado()
    {
        // SimpleMRGrabbable mantiene un evento tipado y una notificación legacy.
        // Cuando ambos están activos esta función puede llegar dos veces en el
        // mismo soltado; la segunda no debe reiniciar caída ni movimiento.
        if (!estaAgarrado && fueSoltado)
        {
            return;
        }

        estaAgarrado = false;
        RestaurarRestriccionesDespuesDeDestino();
        fueSoltado = true;
        permisoParaConducir = true;
        movimientoActivado = false;
        movimientoPorDestinoActivo = false;
        metodoEjecutado = false;
        tiempoEstableEnSuelo = 0f;

        DetenerParticulasRuedas();

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.linearDamping = 0.2f;
            rb.angularDamping = 0.2f;
        }

        IniciarSeguimientoCaida();

        DebugLog("VEHICULO NIVEL 2: soltado. Evaluando caída.");
    }

    public void OrdenarMoverADestino(Vector3 destino)
    {
        if (!permitirMovimientoPorDestino)
        {
            return;
        }

        if (vehiculoDañado || vehiculoExplotado)
        {
            return;
        }

        if (EstaAgarrado())
        {
            return;
        }

        // El prefab usa una protección que lo congela dentro del garaje hasta el
        // primer agarre. Una orden consciente del usuario también es una salida
        // válida del estado de spawn y debe permitir moverlo inmediatamente.
        if (grabbable != null)
        {
            grabbable.PrepararParaMovimientoProgramatico();
        }

        destinoOrdenado = destino;
        movimientoPorDestinoActivo = true;

        movimientoActivado = false;
        permisoParaConducir = false;
        metodoEjecutado = true;
        tiempoEstableEnSuelo = 0f;

        if (rutinaManiobra != null)
        {
            StopCoroutine(rutinaManiobra);
            rutinaManiobra = null;
        }

        if (enderezarSiEstaVolteadoAlRecibirDestino && !EstaDerecho())
        {
            EnderezarVehiculoHaciaDestino(destinoOrdenado);
        }

        if (rb != null)
        {
            GuardarYAplicarRestriccionesDeDestino();
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        DebugLog("VEHICULO NIVEL 2: destino recibido = " + destinoOrdenado);
    }

    private void MoverHaciaDestinoOrdenado()
    {
        if (rb == null)
        {
            movimientoPorDestinoActivo = false;
            ReanudarMovimientoNormalDespuesDeDestino();
            return;
        }

        if (EstaAgarrado() || vehiculoDañado || vehiculoExplotado)
        {
            movimientoPorDestinoActivo = false;
            RestaurarRestriccionesDespuesDeDestino();
            DetenerParticulasRuedas();
            return;
        }

        if (!EstaDerecho())
        {
            EnderezarVehiculoHaciaDestino(destinoOrdenado);
            return;
        }

        if (!EstaTocandoSuelo())
        {
            DetenerParticulasRuedas();
            return;
        }

        Vector3 posicionActual = rb.position;

        Vector3 destinoPlano = new Vector3(
            destinoOrdenado.x,
            posicionActual.y,
            destinoOrdenado.z
        );

        Vector3 direccion = destinoPlano - posicionActual;
        direccion.y = 0f;

        float distancia = direccion.magnitude;

        if (distancia <= distanciaLlegadaDestino)
        {
            movimientoPorDestinoActivo = false;
            DetenerParticulasRuedas();

            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            DebugLog("VEHICULO NIVEL 2: llegó al destino señalado. Reanudando movimiento normal.");

            ReanudarMovimientoNormalDespuesDeDestino();
            return;
        }

        Vector3 direccionNormalizada = direccion.normalized;

        Quaternion rotacionObjetivo = Quaternion.LookRotation(
            direccionNormalizada,
            Vector3.up
        );

        Quaternion nuevaRotacion = Quaternion.RotateTowards(
            rb.rotation,
            rotacionObjetivo,
            velocidadRotacionDestino * Time.fixedDeltaTime
        );

        rb.MoveRotation(nuevaRotacion);

        Vector3 nuevaPosicion =
            rb.position + direccionNormalizada * velocidadDestino * Time.fixedDeltaTime;

        rb.MovePosition(nuevaPosicion);

        ActivarParticulasRuedas();
    }

    private void ReanudarMovimientoNormalDespuesDeDestino()
    {
        RestaurarRestriccionesDespuesDeDestino();

        if (vehiculoDañado || vehiculoExplotado)
        {
            return;
        }

        if (EstaAgarrado())
        {
            return;
        }

        movimientoPorDestinoActivo = false;
        movimientoActivado = false;

        permisoParaConducir = true;
        metodoEjecutado = false;
        tiempoEstableEnSuelo = 0f;

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        DebugLog("VEHICULO NIVEL 2: movimiento normal reactivado después del destino.");
    }

    private void EnderezarVehiculoHaciaDestino(Vector3 destino)
    {
        Vector3 direccion = destino - transform.position;
        direccion.y = 0f;

        if (direccion.sqrMagnitude < 0.001f)
        {
            direccion = transform.forward;
            direccion.y = 0f;
        }

        if (direccion.sqrMagnitude < 0.001f)
        {
            direccion = Vector3.forward;
        }

        direccion.Normalize();

        Quaternion rotacionCorrecta = Quaternion.LookRotation(direccion, Vector3.up);
        Vector3 posicionCorrecta = transform.position;

        if (Physics.Raycast(
                transform.position + Vector3.up * alturaRaycastEnderezar,
                Vector3.down,
                out RaycastHit hit,
                alturaRaycastEnderezar * 3f,
                capasSuelo,
                QueryTriggerInteraction.Ignore
            ))
        {
            float offsetRaizHastaBase = CalcularOffsetRaizHastaBaseVehiculo();
            posicionCorrecta = hit.point +
                               Vector3.up * (offsetRaizHastaBase + alturaExtraAlEnderezar);
        }
        else
        {
            posicionCorrecta += Vector3.up * alturaExtraAlEnderezar;
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.position = posicionCorrecta;
            rb.rotation = rotacionCorrecta;
        }
        else
        {
            transform.position = posicionCorrecta;
            transform.rotation = rotacionCorrecta;
        }

        DebugLog("VEHICULO NIVEL 2: vehículo enderezado para ir al destino.");
    }

    private float CalcularOffsetRaizHastaBaseVehiculo()
    {
        if (!IntentarObtenerBoundsFisicos(out Bounds bounds))
        {
            return 0.15f;
        }

        return Mathf.Max(transform.position.y - bounds.min.y, 0.15f);
    }

    private void GuardarYAplicarRestriccionesDeDestino()
    {
        if (rb == null)
        {
            return;
        }

        if (!restriccionesDestinoGuardadas)
        {
            restriccionesAntesDestino = rb.constraints;
            restriccionesDestinoGuardadas = true;
        }

        // El usuario está dando una orden horizontal. Mantener libres la altura
        // y el giro Y, pero evitar que el contacto con el piso vuelque el modelo
        // y cancele silenciosamente el desplazamiento dirigido.
        rb.constraints = restriccionesAntesDestino |
                         RigidbodyConstraints.FreezeRotationX |
                         RigidbodyConstraints.FreezeRotationZ;
    }

    private void RestaurarRestriccionesDespuesDeDestino()
    {
        if (!restriccionesDestinoGuardadas)
        {
            return;
        }

        if (rb != null)
        {
            rb.constraints = restriccionesAntesDestino;
        }

        restriccionesDestinoGuardadas = false;
    }

    private void IniciarSeguimientoCaida()
    {
        if (!destruirPorAlturaDeCaida)
        {
            return;
        }

        alturaMaximaDesdeSoltado = transform.position.y;
        tiempoUltimoSoltado = Time.time;
        siguiendoCaida = true;
        yaEvaluoCaidaActual = false;
    }

    private void ActualizarDañoPorCaida()
    {
        if (!destruirPorAlturaDeCaida)
        {
            return;
        }

        if (!siguiendoCaida || yaEvaluoCaidaActual)
        {
            return;
        }

        if (vehiculoDañado || vehiculoExplotado)
        {
            return;
        }

        if (EstaAgarrado())
        {
            return;
        }

        if (estadoVehiculo == AlgoLabLevel02GarageController.EstadoVehiculo.Nuevo)
        {
            return;
        }

        if (Time.time - tiempoUltimoSoltado < tiempoMinimoAntesDeEvaluarCaida)
        {
            return;
        }

        if (transform.position.y > alturaMaximaDesdeSoltado)
        {
            alturaMaximaDesdeSoltado = transform.position.y;
        }

        if (!EstaTocandoSuelo())
        {
            return;
        }

        float alturaCaida = alturaMaximaDesdeSoltado - transform.position.y;

        if (alturaCaida <= 0f)
        {
            return;
        }

        yaEvaluoCaidaActual = true;
        siguiendoCaida = false;

        EvaluarDañoPorAltura(alturaCaida);
    }

    private void EvaluarDañoPorAltura(float alturaCaida)
    {
        if (estadoVehiculo == AlgoLabLevel02GarageController.EstadoVehiculo.Seminuevo)
        {
            if (alturaCaida >= alturaMinimaCaidaSeminuevo)
            {
                if (seminuevoExplotaConImpactoFuerte)
                {
                    ExplotarVehiculo("Vehículo seminuevo explotó por caída desde altura: " + alturaCaida.ToString("F2"));
                }
                else
                {
                    DañarVehiculo("Vehículo seminuevo se dañó por caída desde altura: " + alturaCaida.ToString("F2"));
                }
            }

            return;
        }

        if (estadoVehiculo == AlgoLabLevel02GarageController.EstadoVehiculo.Usado)
        {
            if (alturaCaida >= alturaMinimaCaidaUsado)
            {
                if (usadoExplotaConImpactoFuerte)
                {
                    ExplotarVehiculo("Vehículo usado explotó por caída desde altura: " + alturaCaida.ToString("F2"));
                }
                else
                {
                    DañarVehiculo("Vehículo usado se dañó por caída desde altura: " + alturaCaida.ToString("F2"));
                }
            }
        }
    }

    private void RevisarCondicionesParaConducir()
    {
        if (!permisoParaConducir || metodoEjecutado || vehiculoDañado || vehiculoExplotado)
        {
            return;
        }

        if (!PuedeConducirAhora())
        {
            tiempoEstableEnSuelo = 0f;
            DetenerParticulasRuedas();
            return;
        }

        tiempoEstableEnSuelo += Time.deltaTime;

        if (tiempoEstableEnSuelo >= tiempoEstableAntesDeConducir)
        {
            metodoEjecutado = true;
            EjecutarMetodoSeleccionado();
        }
    }

    private bool PuedeConducirAhora()
    {
        if (EstaAgarrado())
        {
            return false;
        }

        if (vehiculoDañado || vehiculoExplotado)
        {
            return false;
        }

        if (conducirSoloSiEstaDerecho && !EstaDerecho())
        {
            return false;
        }

        if (conducirSoloSiTocaSuelo && !EstaTocandoSuelo())
        {
            return false;
        }

        return true;
    }

    private bool EstaDerecho()
    {
        float dot = Vector3.Dot(transform.up, Vector3.up);
        return dot >= minimoDotVerticalParaConducir;
    }

    private bool EstaTocandoSuelo()
    {
        Vector3 origen = transform.position + Vector3.up * 0.05f;
        float distancia = distanciaRaycastSuelo;

        // Los vehículos usan varios colliders. Elegir solo el primero es
        // inestable: en algunos prefabs ese collider pertenece a una pieza
        // elevada y el auto parece estar en el aire aunque sus ruedas/carcasa
        // sí estén apoyadas. Los límites combinados siempre alcanzan la base
        // física completa del vehículo.
        if (IntentarObtenerBoundsFisicos(out Bounds bounds))
        {
            origen = bounds.center;
            distancia = Mathf.Max(distanciaRaycastSuelo, bounds.extents.y + 0.18f);
        }

        RaycastHit[] hits = Physics.RaycastAll(
            origen,
            Vector3.down,
            distancia,
            capasSuelo,
            QueryTriggerInteraction.Ignore
        );

        if (hits == null || hits.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];

            if (hit.collider == null)
            {
                continue;
            }

            if (hit.collider.transform == transform || hit.collider.transform.IsChildOf(transform))
            {
                continue;
            }

            return true;
        }

        return false;
    }

    private bool IntentarObtenerBoundsFisicos(out Bounds bounds)
    {
        bounds = new Bounds(transform.position, Vector3.zero);
        Collider[] colliders = GetComponentsInChildren<Collider>(false);
        bool encontro = false;

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider colliderVehiculo = colliders[i];
            if (colliderVehiculo == null ||
                !colliderVehiculo.enabled ||
                colliderVehiculo.isTrigger ||
                !colliderVehiculo.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (!encontro)
            {
                bounds = colliderVehiculo.bounds;
                encontro = true;
            }
            else
            {
                bounds.Encapsulate(colliderVehiculo.bounds);
            }
        }

        return encontro;
    }

    private void EjecutarMetodoSeleccionado()
    {
        if (EstaAgarrado())
        {
            return;
        }

        string metodo = Normalizar(metodoSeleccionado);

        if (metodo.Contains("encender"))
        {
            Encender();

            if (moverAlSoltar)
            {
                ActivarMovimientoLibre(velocidad);
            }
        }
        else if (metodo.Contains("acelerar"))
        {
            Acelerar();
        }
        else if (metodo.Contains("frenar"))
        {
            Frenar();
        }
        else if (metodo.Contains("apagar"))
        {
            Apagar();
        }
    }

    public void Encender()
    {
        CrearLuzMotorSiNoExiste();

        if (luzMotor != null)
        {
            luzMotor.enabled = true;
            luzMotor.color = colorVehiculo;
        }

        DebugLog("VEHICULO NIVEL 2: encender()");
    }

    public void Acelerar()
    {
        Encender();

        if (moverSiMetodoAcelerar)
        {
            ActivarMovimientoLibre(velocidadAcelerado);
        }

        DebugLog("VEHICULO NIVEL 2: acelerar()");
    }

    public void Frenar()
    {
        Encender();

        if (rutinaManiobra != null)
        {
            StopCoroutine(rutinaManiobra);
        }

        rutinaManiobra = StartCoroutine(MoverCortoYFrenar());

        DebugLog("VEHICULO NIVEL 2: frenar()");
    }

    public void Apagar()
    {
        movimientoActivado = false;
        movimientoPorDestinoActivo = false;
        RestaurarRestriccionesDespuesDeDestino();
        DetenerParticulasRuedas();

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (luzMotor != null)
        {
            luzMotor.enabled = false;
        }

        DebugLog("VEHICULO NIVEL 2: apagar()");
    }

    private void ActivarMovimientoLibre(float nuevaVelocidad)
    {
        if (!PuedeConducirAhora())
        {
            movimientoActivado = false;
            DetenerParticulasRuedas();
            return;
        }

        velocidad = nuevaVelocidad;
        movimientoActivado = true;
        movimientoPorDestinoActivo = false;
        RestaurarRestriccionesDespuesDeDestino();

        if (rb != null)
        {
            rb.useGravity = true;
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        ActivarParticulasRuedas();

        DebugLog("VEHICULO NIVEL 2: movimiento activado.");
    }

    private void MoverVehiculoLibre()
    {
        if (rb == null)
        {
            transform.position += transform.forward * velocidad * Time.fixedDeltaTime;
            return;
        }

        bool hayObstaculo = Physics.Raycast(
            transform.position + Vector3.up * 0.15f,
            transform.forward,
            distanciaRaycastObstaculo,
            capasSuelo,
            QueryTriggerInteraction.Ignore
        );

        if (hayObstaculo && rutinaManiobra == null)
        {
            rutinaManiobra = StartCoroutine(EsquivarObstaculo());
            return;
        }

        Vector3 nuevaPosicion =
            rb.position + transform.forward * velocidad * Time.fixedDeltaTime;

        rb.MovePosition(nuevaPosicion);
    }

    private void DetenerMovimientoHorizontal()
    {
        movimientoActivado = false;
        movimientoPorDestinoActivo = false;
        RestaurarRestriccionesDespuesDeDestino();
        DetenerParticulasRuedas();

        if (rb == null)
        {
            return;
        }

        Vector3 velocidadActual = rb.linearVelocity;
        rb.linearVelocity = new Vector3(0f, velocidadActual.y, 0f);
        rb.angularVelocity = Vector3.zero;
    }

    private IEnumerator EsquivarObstaculo()
    {
        movimientoActivado = false;
        movimientoPorDestinoActivo = false;
        RestaurarRestriccionesDespuesDeDestino();
        DetenerParticulasRuedas();

        float tiempo = 0f;

        while (tiempo < tiempoRetroceso)
        {
            tiempo += Time.deltaTime;

            if (rb != null)
            {
                Vector3 nuevaPos =
                    rb.position - transform.forward * velocidad * 0.5f * Time.deltaTime;

                rb.MovePosition(nuevaPos);
            }

            yield return null;
        }

        tiempo = 0f;

        while (tiempo < tiempoGiro)
        {
            tiempo += Time.deltaTime;

            Quaternion giro =
                Quaternion.Euler(0f, velocidadRotacion * 30f * Time.deltaTime, 0f);

            if (rb != null)
            {
                rb.MoveRotation(rb.rotation * giro);
            }

            yield return null;
        }

        rutinaManiobra = null;

        if (PuedeConducirAhora())
        {
            movimientoActivado = true;
            ActivarParticulasRuedas();
        }
    }

    private IEnumerator MoverCortoYFrenar()
    {
        movimientoActivado = false;

        float duracion = 0.45f;
        float tiempo = 0f;

        while (tiempo < duracion)
        {
            tiempo += Time.deltaTime;

            if (rb != null && PuedeConducirAhora())
            {
                ActivarParticulasRuedas();

                Vector3 nuevaPos =
                    rb.position + transform.forward * velocidad * 0.35f * Time.deltaTime;

                rb.MovePosition(nuevaPos);
            }
            else
            {
                DetenerParticulasRuedas();
            }

            yield return null;
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        movimientoActivado = false;
        rutinaManiobra = null;
        DetenerParticulasRuedas();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (vehiculoExplotado || vehiculoDañado)
        {
            return;
        }

        if (Time.time - tiempoCreacion < tiempoProteccionInicial)
        {
            return;
        }

        if (evaluarImpactoSoloDespuesDeSoltar && !fueSoltado)
        {
            return;
        }

        if (EstaAgarrado())
        {
            return;
        }

        float impacto = collision.relativeVelocity.magnitude;

        if (estadoVehiculo == AlgoLabLevel02GarageController.EstadoVehiculo.Nuevo)
        {
            return;
        }

        if (estadoVehiculo == AlgoLabLevel02GarageController.EstadoVehiculo.Seminuevo)
        {
            if (impacto >= impactoMaximoSeminuevo)
            {
                if (seminuevoExplotaConImpactoFuerte)
                {
                    ExplotarVehiculo("Vehículo seminuevo explotó por impacto: " + impacto);
                }
                else
                {
                    DañarVehiculo("Vehículo seminuevo se dañó por impacto: " + impacto);
                }
            }

            return;
        }

        if (estadoVehiculo == AlgoLabLevel02GarageController.EstadoVehiculo.Usado)
        {
            if (impacto >= impactoMaximoUsado)
            {
                if (usadoExplotaConImpactoFuerte)
                {
                    ExplotarVehiculo("Vehículo usado explotó por impacto: " + impacto);
                }
                else
                {
                    DañarVehiculo("Vehículo usado se dañó por impacto: " + impacto);
                }
            }
        }
    }

    private void DañarVehiculo(string razon)
    {
        if (vehiculoDañado || vehiculoExplotado)
        {
            return;
        }

        vehiculoDañado = true;
        NotificarDanoOExplosionAPractica(razon);

        movimientoActivado = false;
        movimientoPorDestinoActivo = false;
        RestaurarRestriccionesDespuesDeDestino();
        DetenerParticulasRuedas();

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            Material[] materiales = renderers[i].materials;

            for (int j = 0; j < materiales.Length; j++)
            {
                if (materiales[j] == null)
                {
                    continue;
                }

                if (materiales[j].HasProperty("_BaseColor"))
                {
                    materiales[j].SetColor("_BaseColor", Color.gray);
                }
                else if (materiales[j].HasProperty("_Color"))
                {
                    materiales[j].SetColor("_Color", Color.gray);
                }
            }
        }

        Debug.LogWarning("VEHICULO NIVEL 2: " + razon);
    }

    private void ExplotarVehiculo(string razon)
    {
        if (vehiculoExplotado)
        {
            return;
        }

        vehiculoExplotado = true;
        NotificarDanoOExplosionAPractica(razon);

        movimientoActivado = false;
        movimientoPorDestinoActivo = false;
        RestaurarRestriccionesDespuesDeDestino();
        siguiendoCaida = false;

        DetenerParticulasRuedas();

        if (rutinaManiobra != null)
        {
            StopCoroutine(rutinaManiobra);
            rutinaManiobra = null;
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.useGravity = false;
            rb.isKinematic = true;
        }

        ReproducirExplosionIndependiente();

        Debug.LogWarning("VEHICULO NIVEL 2: " + razon);

        if (destruirAlExplotar)
        {
            Destroy(gameObject, tiempoAntesDeDestruir);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private void ReproducirExplosionIndependiente()
    {
        Vector3 posicionExplosion = ObtenerPosicionExplosion();

        if (prefabExplosion != null)
        {
            GameObject explosionObj = Instantiate(
                prefabExplosion,
                posicionExplosion,
                Quaternion.identity
            );

            explosionObj.name = "FX_Explosion_Runtime";
            explosionObj.SetActive(true);

            ReproducirTodasLasParticulas(explosionObj);

            Destroy(explosionObj, tiempoDestruirParticulaExplosion);
            return;
        }

        if (particulaExplosion != null)
        {
            GameObject explosionObj = Instantiate(
                particulaExplosion.gameObject,
                posicionExplosion,
                Quaternion.identity
            );

            explosionObj.name = "FX_Explosion_Runtime";
            explosionObj.SetActive(true);

            ReproducirTodasLasParticulas(explosionObj);

            Destroy(explosionObj, tiempoDestruirParticulaExplosion);
            return;
        }

        CrearExplosionSimple();
    }

    private Vector3 ObtenerPosicionExplosion()
    {
        Collider col = GetComponentInChildren<Collider>();

        if (col != null)
        {
            return col.bounds.center;
        }

        return transform.position;
    }

    private void ReproducirTodasLasParticulas(GameObject root)
    {
        if (root == null)
        {
            return;
        }

        ParticleSystem[] particulas = root.GetComponentsInChildren<ParticleSystem>(true);

        for (int i = 0; i < particulas.Length; i++)
        {
            ParticleSystem ps = particulas[i];

            if (ps == null)
            {
                continue;
            }

            ps.gameObject.SetActive(true);

            ParticleSystem.MainModule main = ps.main;
            main.loop = false;
            main.playOnAwake = false;

            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Play(true);
        }
    }

    private void CrearExplosionSimple()
    {
        GameObject explosion = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        explosion.name = "ExplosionSimple";
        explosion.transform.position = ObtenerPosicionExplosion();
        explosion.transform.localScale = Vector3.one * 0.35f;

        Renderer r = explosion.GetComponent<Renderer>();

        if (r != null)
        {
            r.material.color = Color.red;
        }

        Light luz = explosion.AddComponent<Light>();
        luz.type = LightType.Point;
        luz.range = 2f;
        luz.intensity = 4f;
        luz.color = Color.red;

        Destroy(explosion, 0.5f);
    }

    private void BuscarPracticeControllerSiHaceFalta()
    {
        if (!buscarPracticeControllerAutomaticamente)
        {
            return;
        }

        if (practiceController != null)
        {
            return;
        }

        practiceController = FindFirstObjectByType<AlgoLabLevel02PracticeController>();
    }

    private void NotificarDanoOExplosionAPractica(string razon)
    {
        if (!notificarDanoOExplosionAPractica)
        {
            return;
        }

        if (danoOExplosionNotificadaAPractica)
        {
            return;
        }

        BuscarPracticeControllerSiHaceFalta();

        if (practiceController == null)
        {
            DebugLog(
                "VEHICULO NIVEL 2: no se encontró PracticeController para reportar daño/explosión. Razón: " +
                razon
            );
            return;
        }

        danoOExplosionNotificadaAPractica = true;
        practiceController.RegistrarVehiculoDestruidoPorPractica();

        DebugLog(
            "VEHICULO NIVEL 2: daño/explosión reportada a la práctica. Razón: " +
            razon
        );
    }

    private void BuscarParticulasSiEsNecesario()
    {
        if (!buscarParticulasAutomaticamente)
        {
            return;
        }

        ParticleSystem[] todas = GetComponentsInChildren<ParticleSystem>(true);

        if ((particulasRuedas == null || particulasRuedas.Length == 0) && todas.Length > 0)
        {
            List<ParticleSystem> ruedasEncontradas = new List<ParticleSystem>();

            for (int i = 0; i < todas.Length; i++)
            {
                ParticleSystem ps = todas[i];

                if (ps == null)
                {
                    continue;
                }

                string nombre = ps.name.ToLower();

                bool esExplosion =
                    nombre.Contains("explosion") ||
                    nombre.Contains("explosión") ||
                    nombre.Contains("smoke_explosion");

                bool esRueda =
                    nombre.Contains("dirt") ||
                    nombre.Contains("splatter") ||
                    nombre.Contains("rueda") ||
                    nombre.Contains("wheel") ||
                    nombre.Contains("polvo");

                if (esRueda && !esExplosion)
                {
                    ruedasEncontradas.Add(ps);
                }
            }

            particulasRuedas = ruedasEncontradas.ToArray();
        }

        if (particulaExplosion == null && todas.Length > 0)
        {
            for (int i = 0; i < todas.Length; i++)
            {
                ParticleSystem ps = todas[i];

                if (ps == null)
                {
                    continue;
                }

                string nombre = ps.name.ToLower();

                if (nombre.Contains("explosion") ||
                    nombre.Contains("explosión") ||
                    nombre.Contains("smoke"))
                {
                    particulaExplosion = ps;
                    break;
                }
            }
        }
    }

    private void PrepararParticulas()
    {
        PrepararParticulasRuedas();
        PrepararParticulaExplosion();
    }

    private void PrepararParticulasRuedas()
    {
        if (particulasRuedas == null)
        {
            return;
        }

        for (int i = 0; i < particulasRuedas.Length; i++)
        {
            ParticleSystem ps = particulasRuedas[i];

            if (ps == null)
            {
                continue;
            }

            if (forzarLoopParticulasRuedas)
            {
                ParticleSystem.MainModule main = ps.main;
                main.loop = true;
                main.playOnAwake = false;
            }

            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        particulasRuedasActivas = false;
    }

    private void PrepararParticulaExplosion()
    {
        if (particulaExplosion == null)
        {
            return;
        }

        ParticleSystem.MainModule main = particulaExplosion.main;
        main.loop = false;
        main.playOnAwake = false;

        particulaExplosion.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        particulaExplosion.gameObject.SetActive(false);
    }

    private void ActualizarParticulasRuedas()
    {
        bool debeActivarPorMetodo =
            movimientoActivado &&
            PuedeConducirAhora();

        bool debeActivarPorDestino =
            movimientoPorDestinoActivo &&
            PuedeMostrarParticulasPorDestino();

        bool debeActivar =
            usarParticulasRuedas &&
            (debeActivarPorMetodo || debeActivarPorDestino) &&
            !EstaAgarrado() &&
            !vehiculoDañado &&
            !vehiculoExplotado;

        if (debeActivar)
        {
            ActivarParticulasRuedas();
        }
        else
        {
            DetenerParticulasRuedas();
        }
    }

    private bool PuedeMostrarParticulasPorDestino()
    {
        if (!movimientoPorDestinoActivo)
        {
            return false;
        }

        if (EstaAgarrado() || vehiculoDañado || vehiculoExplotado)
        {
            return false;
        }

        if (!EstaDerecho())
        {
            return false;
        }

        if (!EstaTocandoSuelo())
        {
            return false;
        }

        return true;
    }

    private void ActivarParticulasRuedas()
    {
        if (!usarParticulasRuedas)
        {
            return;
        }

        if (particulasRuedas == null || particulasRuedas.Length == 0)
        {
            return;
        }

        if (particulasRuedasActivas)
        {
            return;
        }

        for (int i = 0; i < particulasRuedas.Length; i++)
        {
            ParticleSystem ps = particulasRuedas[i];

            if (ps == null)
            {
                continue;
            }

            ps.gameObject.SetActive(true);

            if (!ps.isPlaying)
            {
                ps.Play(true);
            }
        }

        particulasRuedasActivas = true;
    }

    private void DetenerParticulasRuedas()
    {
        if (particulasRuedas == null || particulasRuedas.Length == 0)
        {
            particulasRuedasActivas = false;
            return;
        }

        if (!particulasRuedasActivas && !limpiarParticulasAlDetener)
        {
            return;
        }

        for (int i = 0; i < particulasRuedas.Length; i++)
        {
            ParticleSystem ps = particulasRuedas[i];

            if (ps == null)
            {
                continue;
            }

            if (limpiarParticulasAlDetener)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            else
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        particulasRuedasActivas = false;
    }

    private void CrearLuzMotorSiNoExiste()
    {
        if (luzMotor != null)
        {
            return;
        }

        GameObject luzObj = new GameObject("LuzMetodoVehiculo");
        luzObj.transform.SetParent(transform, false);
        luzObj.transform.localPosition = new Vector3(0f, 0.4f, 0.6f);

        luzMotor = luzObj.AddComponent<Light>();
        luzMotor.type = LightType.Point;
        luzMotor.range = 1.5f;
        luzMotor.intensity = 1.5f;
        luzMotor.color = colorVehiculo;
        luzMotor.enabled = false;
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
