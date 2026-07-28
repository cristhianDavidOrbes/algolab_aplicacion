using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class SimpleMRGrabbable : MonoBehaviour
{
    public enum ReleaseMode
    {
        FloatInPlace,
        Physics
    }

    public enum PerfilUso
    {
        Practica2FisicaOriginal,
        Practica2SpawnSeguroSinColisionInicial,
        Practica1FlotanteSinColision,
        Personalizado
    }

    [Header("Perfil")]
    [Tooltip("Practica2FisicaOriginal conserva el comportamiento antiguo. Practica1FlotanteSinColision flota y no colisiona al soltar.")]
    public PerfilUso perfilUso = PerfilUso.Practica2FisicaOriginal;

    [Header("Release Settings")]
    public ReleaseMode releaseMode = ReleaseMode.Physics;
    public bool useGravityOnRelease = true;

    [Header("Grab Settings")]
    [Tooltip("Déjalo desactivado para práctica 2 si necesitas la física antigua. Actívalo para objetos flotantes que sigan mejor la mano.")]
    public bool ponerKinematicMientrasEstaAgarrado = false;

    [Tooltip("Detecta agarre cuando el objeto cambia de padre. Útil para hand tracking o sistemas que parentan el objeto al agarrarlo.")]
    public bool detectarAgarrePorCambioDePadre = true;

    [Header("Flotar / bloquear solo al soltar")]
    [Tooltip("Si está activo, al soltar se queda flotando donde el usuario lo dejó.")]
    public bool mantenerFlotandoAlSoltar = false;

    [Tooltip("Si está activo, la posición solo se bloquea después de que el usuario agarre y suelte el objeto. No bloquea al spawnear.")]
    public bool bloquearSoloDespuesDeSoltar = true;

    [Tooltip("Permite que otros scripts como el ManualPanelSpawnManager muevan el objeto antes de la primera soltada.")]
    public bool permitirMovimientoExternoAntesDeSoltar = true;

    [Tooltip("Guarda la posición mundo al soltar y la fuerza mientras no esté agarrado.")]
    public bool bloquearPosicionMundoAlSoltar = false;

    [Tooltip("Actívalo si también quieres congelar la rotación exacta al soltar.")]
    public bool bloquearRotacionMundoAlSoltar = false;

    [Tooltip("Cuando no está agarrado queda kinematic para que no caiga ni sea empujado. Úsalo en práctica 1, no en práctica 2.")]
    public bool hacerKinematicCuandoNoAgarrado = false;

    [Tooltip("Congela el Rigidbody cuando no está agarrado. Úsalo en práctica 1, no en práctica 2.")]
    public bool congelarRigidbodyCuandoNoAgarrado = false;

    [Tooltip("Borra velocidad lineal y angular para evitar caídas o empujones raros.")]
    public bool limpiarVelocidadesCuandoNoAgarrado = true;

    [Tooltip("Al agarrar se libera la posición bloqueada para que el usuario lo pueda mover.")]
    public bool desbloquearAlSerAgarrado = true;

    [Header("Colisiones")]
    [Tooltip("Si está activo, el objeto evita colisión física con paredes, piso, paneles u otros objetos sólidos.")]
    public bool sinColisionFisica = false;

    [Header("Practica 2 - spawn seguro")]
    [Tooltip("Si está activo, al aparecer el objeto NO colisiona con nada hasta que el usuario lo agarre por primera vez. Después vuelve a la física normal de práctica 2.")]
    public bool sinColisionInicialHastaPrimerAgarre = false;

    [Tooltip("Si está activo, mientras espera el primer agarre queda quieto, sin gravedad y sin velocidad. Esto evita que salga volando al spawnear dentro del garage.")]
    public bool congelarMientrasEsperaPrimerAgarre = true;

    [Tooltip("Si está activo, no colisiona solamente cuando NO está agarrado. Recomendado para práctica 1.")]
    public bool sinColisionSoloCuandoNoAgarrado = true;

    [Tooltip("NO recomendado. Si está activo, el collider se vuelve Trigger y algunos sistemas de grab dejan de detectarlo.")]
    public bool usarTriggerParaNoColisionar = false;

    [Tooltip("Recomendado activado. Mantiene el collider normal para que el sistema de agarre lo pueda detectar.")]
    public bool mantenerColliderNormalParaAgarre = true;

    [Tooltip("Si está activo, también aplica la configuración a los colliders hijos del objeto.")]
    public bool incluirCollidersHijos = true;

    [Tooltip("Si está activo, cuando el objeto no colisiona se desactiva la gravedad para que no caiga atravesando el piso.")]
    public bool desactivarGravedadCuandoNoColisiona = true;

    [Tooltip("Usa Physics.IgnoreCollision contra objetos sólidos. Evita choque físico sin convertir tu collider en Trigger.")]
    public bool ignorarColisionesSolidasDeEscena = true;

    [Tooltip("Si está activo, no ignora colliders Trigger. Muchos sistemas de manos/agarre usan triggers para detectar objetos.")]
    public bool noIgnorarTriggers = true;

    [Tooltip("Si está activo, no ignora colliders cuyo nombre parezca de mano, control, rayo, cursor o interactor.")]
    public bool noIgnorarCollidersDeControladoresYManos = true;

    [Tooltip("Palabras usadas para NO ignorar colliders de controladores, manos, rayos, cursores o interactores.")]
    public string[] palabrasNombreNoIgnorar = new string[]
    {
        "Hand", "Controller", "Grab", "Ray", "Cursor", "OVR", "Touch",
        "Interactor", "Pointer", "Left", "Right", "HandGrab", "Direct"
    };

    [Tooltip("Cada cuántos segundos se refresca la lista de colliders ignorados. Útil si aparecen objetos nuevos en runtime.")]
    public float intervaloRefrescarIgnorados = 0.5f;

    [Header("Compatibilidad antigua")]
    [Tooltip("Activado por defecto para conservar la lógica antigua de práctica 2 que usa NotificarAgarrado/NotificarSoltado.")]
    public bool usarSendMessageLegacy = true;

    [Header("Float Physics Settings")]
    public float floatLinearDamping = 3.5f;
    public float floatAngularDamping = 4.5f;

    [Header("Physics Release Settings")]
    public float physicsLinearDamping = 0.2f;
    public float physicsAngularDamping = 0.2f;

    [Header("Lanzamiento con impulso")]
    [Tooltip("En modo Physics conserva la velocidad de la mano al soltar el objeto.")]
    public bool conservarImpulsoAlSoltar = true;

    [Min(0f)] public float multiplicadorImpulsoLineal = 1f;
    [Min(0f)] public float multiplicadorImpulsoAngular = 1f;
    [Min(0.1f)] public float velocidadLinealMaximaAlSoltar = 8f;
    [Min(0.1f)] public float velocidadAngularMaximaAlSoltar = 20f;

    [Tooltip("Suaviza pequeñas variaciones del seguimiento sin eliminar lanzamientos rápidos.")]
    [Min(0f)] public float suavizadoMuestreoLanzamiento = 18f;

    [Header("Debug")]
    public bool mostrarDebug = true;

    public Rigidbody Rigidbody { get; private set; }
    public bool IsGrabbed { get; private set; }
    public bool YaFueSoltadoPorUsuario => yaFueSoltadoPorUsuario;

    public event Action OnGrabStarted;
    public event Action OnGrabEnded;

    private Transform padreInicial;
    private bool agarradoPorCambioDePadre = false;

    private Collider[] misColliders;
    private readonly Dictionary<Collider, bool> triggerOriginalPorCollider = new Dictionary<Collider, bool>();
    private readonly List<ParColliderIgnorado> colisionesIgnoradas = new List<ParColliderIgnorado>();
    private readonly HashSet<ulong> clavesIgnoradasPropias = new HashSet<ulong>();
    private float proximoRefrescoIgnorados;

    private static readonly Dictionary<ulong, EstadoColisionIgnorada> colisionesIgnoradasCompartidas =
        new Dictionary<ulong, EstadoColisionIgnorada>();
    private static Collider[] collidersEscenaCache = Array.Empty<Collider>();
    private static float proximoRefrescoCollidersEscena;

    private bool yaFueSoltadoPorUsuario = false;
    private bool yaFueAgarradoPorPrimeraVez = false;
    private bool posicionBloqueadaValida = false;
    private Vector3 posicionBloqueadaMundo;
    private Quaternion rotacionBloqueadaMundo;
    private Vector3 ultimaPosicionMuestreo;
    private Quaternion ultimaRotacionMuestreo;
    private float ultimoTiempoMuestreo;
    private Vector3 velocidadLinealMuestreada;
    private Vector3 velocidadAngularMuestreada;
    private bool muestreoLanzamientoValido;

    private struct ParColliderIgnorado
    {
        public Collider mio;
        public Collider otro;
        public ulong clave;
    }

    private struct EstadoColisionIgnorada
    {
        public int referencias;
        public bool ignoradaAntesDeAlgoLab;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ReiniciarCacheEstatico()
    {
        colisionesIgnoradasCompartidas.Clear();
        collidersEscenaCache = Array.Empty<Collider>();
        proximoRefrescoCollidersEscena = 0f;
    }

    private void Reset()
    {
        AplicarPresetDesdePerfil();
    }

    private void Awake()
    {
        Rigidbody = GetComponent<Rigidbody>();
        padreInicial = transform.parent;

        ActualizarMisColliders();
        ConfigurarRigidbodyInicial();
        AplicarEstadoColisiones();
    }

    private void OnEnable()
    {
        ActualizarMisColliders();
        AplicarEstadoColisiones();
    }

    private void OnDisable()
    {
        IsGrabbed = false;
        agarradoPorCambioDePadre = false;
        RestaurarColisionesIgnoradas();
        RestaurarTriggersOriginales();
    }

    private void OnDestroy()
    {
        RestaurarColisionesIgnoradas();
        RestaurarTriggersOriginales();
    }

    private void Update()
    {
        DetectarCambioDePadre();

        if ((sinColisionFisica || sinColisionInicialHastaPrimerAgarre) &&
            Time.unscaledTime >= proximoRefrescoIgnorados)
        {
            proximoRefrescoIgnorados =
                Time.unscaledTime + Mathf.Max(0.05f, intervaloRefrescarIgnorados);
            AplicarEstadoColisiones();
        }

        ReforzarEstadoNoAgarrado();
    }

    private void FixedUpdate()
    {
        ReforzarEstadoNoAgarrado();
    }

    private void LateUpdate()
    {
        ActualizarMuestreoLanzamiento();
        ReforzarEstadoNoAgarrado();
    }

    [ContextMenu("Aplicar preset desde perfil")]
    public void AplicarPresetDesdePerfil()
    {
        switch (perfilUso)
        {
            case PerfilUso.Practica2FisicaOriginal:
                releaseMode = ReleaseMode.Physics;
                useGravityOnRelease = true;
                ponerKinematicMientrasEstaAgarrado = false;
                detectarAgarrePorCambioDePadre = true;

                mantenerFlotandoAlSoltar = false;
                bloquearSoloDespuesDeSoltar = true;
                permitirMovimientoExternoAntesDeSoltar = true;
                bloquearPosicionMundoAlSoltar = false;
                bloquearRotacionMundoAlSoltar = false;
                hacerKinematicCuandoNoAgarrado = false;
                congelarRigidbodyCuandoNoAgarrado = false;
                limpiarVelocidadesCuandoNoAgarrado = true;

                sinColisionFisica = false;
                sinColisionInicialHastaPrimerAgarre = false;
                congelarMientrasEsperaPrimerAgarre = false;
                sinColisionSoloCuandoNoAgarrado = true;
                usarTriggerParaNoColisionar = false;
                mantenerColliderNormalParaAgarre = true;
                ignorarColisionesSolidasDeEscena = false;
                usarSendMessageLegacy = true;
                break;

            case PerfilUso.Practica2SpawnSeguroSinColisionInicial:
                releaseMode = ReleaseMode.Physics;
                useGravityOnRelease = true;
                ponerKinematicMientrasEstaAgarrado = false;
                detectarAgarrePorCambioDePadre = true;

                mantenerFlotandoAlSoltar = false;
                bloquearSoloDespuesDeSoltar = true;
                permitirMovimientoExternoAntesDeSoltar = true;
                bloquearPosicionMundoAlSoltar = false;
                bloquearRotacionMundoAlSoltar = false;
                hacerKinematicCuandoNoAgarrado = false;
                congelarRigidbodyCuandoNoAgarrado = false;
                limpiarVelocidadesCuandoNoAgarrado = true;

                // Solo evita colisiones mientras el carro acaba de spawnear
                // y todavía no fue agarrado por el usuario.
                sinColisionFisica = false;
                sinColisionInicialHastaPrimerAgarre = true;
                congelarMientrasEsperaPrimerAgarre = true;
                sinColisionSoloCuandoNoAgarrado = true;
                usarTriggerParaNoColisionar = false;
                mantenerColliderNormalParaAgarre = true;
                ignorarColisionesSolidasDeEscena = true;
                desactivarGravedadCuandoNoColisiona = true;
                usarSendMessageLegacy = true;
                break;

            case PerfilUso.Practica1FlotanteSinColision:
                releaseMode = ReleaseMode.FloatInPlace;
                useGravityOnRelease = false;
                ponerKinematicMientrasEstaAgarrado = true;
                detectarAgarrePorCambioDePadre = true;

                mantenerFlotandoAlSoltar = true;
                bloquearSoloDespuesDeSoltar = true;
                permitirMovimientoExternoAntesDeSoltar = true;
                bloquearPosicionMundoAlSoltar = true;
                bloquearRotacionMundoAlSoltar = false;
                hacerKinematicCuandoNoAgarrado = true;
                congelarRigidbodyCuandoNoAgarrado = true;
                limpiarVelocidadesCuandoNoAgarrado = true;

                sinColisionFisica = true;
                sinColisionInicialHastaPrimerAgarre = false;
                congelarMientrasEsperaPrimerAgarre = false;
                sinColisionSoloCuandoNoAgarrado = true;
                usarTriggerParaNoColisionar = false;
                mantenerColliderNormalParaAgarre = true;
                ignorarColisionesSolidasDeEscena = true;
                usarSendMessageLegacy = true;
                break;
        }
    }

    private void ActualizarMisColliders()
    {
        if (incluirCollidersHijos)
        {
            misColliders = GetComponentsInChildren<Collider>(true);
        }
        else
        {
            Collider colliderPrincipal = GetComponent<Collider>();
            misColliders = colliderPrincipal != null
                ? new Collider[] { colliderPrincipal }
                : Array.Empty<Collider>();
        }

        for (int i = 0; i < misColliders.Length; i++)
        {
            Collider col = misColliders[i];

            if (col == null)
            {
                continue;
            }

            if (!triggerOriginalPorCollider.ContainsKey(col))
            {
                triggerOriginalPorCollider.Add(col, col.isTrigger);
            }
        }
    }

    private void ConfigurarRigidbodyInicial()
    {
        if (Rigidbody == null)
        {
            Rigidbody = GetComponent<Rigidbody>();
        }

        Rigidbody.isKinematic = false;
        Rigidbody.useGravity = false;
        Rigidbody.linearVelocity = Vector3.zero;
        Rigidbody.angularVelocity = Vector3.zero;
        Rigidbody.linearDamping = 0f;
        Rigidbody.angularDamping = 0f;
        Rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        Rigidbody.isKinematic = hacerKinematicCuandoNoAgarrado;
        Rigidbody.constraints = congelarRigidbodyCuandoNoAgarrado
            ? RigidbodyConstraints.FreezeAll
            : RigidbodyConstraints.None;
    }

    private void DetectarCambioDePadre()
    {
        if (!detectarAgarrePorCambioDePadre)
        {
            return;
        }

        bool ahoraTieneOtroPadre = transform.parent != padreInicial;

        if (ahoraTieneOtroPadre && !agarradoPorCambioDePadre)
        {
            agarradoPorCambioDePadre = true;
            BeginGrab();
        }
        else if (!ahoraTieneOtroPadre && agarradoPorCambioDePadre)
        {
            agarradoPorCambioDePadre = false;
            EndGrab();
        }
    }

    public void BeginGrab()
    {
        if (IsGrabbed)
        {
            return;
        }

        IsGrabbed = true;
        yaFueAgarradoPorPrimeraVez = true;

        if (Rigidbody == null)
        {
            Rigidbody = GetComponent<Rigidbody>();
        }

        if (desbloquearAlSerAgarrado)
        {
            posicionBloqueadaValida = false;
        }

        Rigidbody.constraints = RigidbodyConstraints.None;
        Rigidbody.useGravity = false;
        bool kinematicDuranteAgarre =
            ponerKinematicMientrasEstaAgarrado;
        if (Rigidbody.isKinematic && !kinematicDuranteAgarre)
            Rigidbody.isKinematic = false;
        if (!Rigidbody.isKinematic)
        {
            Rigidbody.linearVelocity = Vector3.zero;
            Rigidbody.angularVelocity = Vector3.zero;
        }
        Rigidbody.linearDamping = 0f;
        Rigidbody.angularDamping = 0f;
        Rigidbody.isKinematic = kinematicDuranteAgarre;

        ReiniciarMuestreoLanzamiento();

        AplicarEstadoColisiones();

        if (usarSendMessageLegacy)
        {
            SendMessage("NotificarAgarrado", SendMessageOptions.DontRequireReceiver);
        }

        OnGrabStarted?.Invoke();

        DebugLog("SimpleMRGrabbable: agarrado.");
    }

    public void EndGrab()
    {
        if (!IsGrabbed)
        {
            return;
        }

        ActualizarMuestreoLanzamiento();
        Vector3 impulsoLineal = velocidadLinealMuestreada;
        Vector3 impulsoAngular = velocidadAngularMuestreada;
        bool aplicarImpulso = DebeAplicarImpulsoDeLanzamiento();

        IsGrabbed = false;
        yaFueSoltadoPorUsuario = true;

        if (Rigidbody == null)
        {
            Rigidbody = GetComponent<Rigidbody>();
        }

        if (mantenerFlotandoAlSoltar || releaseMode == ReleaseMode.FloatInPlace)
        {
            BloquearPosicionActualComoSoltada();
        }

        AplicarModoDeSoltado();
        AplicarEstadoColisiones();
        ReforzarEstadoNoAgarrado();

        if (usarSendMessageLegacy)
        {
            SendMessage("NotificarSoltado", SendMessageOptions.DontRequireReceiver);
        }

        OnGrabEnded?.Invoke();

        if (aplicarImpulso)
        {
            AplicarImpulsoDeLanzamiento(impulsoLineal, impulsoAngular);
        }

        DebugLog("SimpleMRGrabbable: soltado.");
    }

    private void BloquearPosicionActualComoSoltada()
    {
        posicionBloqueadaMundo = transform.position;
        rotacionBloqueadaMundo = transform.rotation;
        posicionBloqueadaValida = true;
    }

    private void AplicarModoDeSoltado()
    {
        if (Rigidbody == null)
        {
            return;
        }

        if (mantenerFlotandoAlSoltar || releaseMode == ReleaseMode.FloatInPlace)
        {
            Rigidbody.useGravity = false;
            Rigidbody.linearVelocity = Vector3.zero;
            Rigidbody.angularVelocity = Vector3.zero;
            Rigidbody.linearDamping = floatLinearDamping;
            Rigidbody.angularDamping = floatAngularDamping;
            Rigidbody.isKinematic = hacerKinematicCuandoNoAgarrado;
            Rigidbody.constraints = congelarRigidbodyCuandoNoAgarrado
                ? RigidbodyConstraints.FreezeAll
                : RigidbodyConstraints.None;
            return;
        }

        Rigidbody.isKinematic = false;
        Rigidbody.constraints = RigidbodyConstraints.None;
        Rigidbody.useGravity = useGravityOnRelease;
        Rigidbody.linearDamping = physicsLinearDamping;
        Rigidbody.angularDamping = physicsAngularDamping;
    }

    private void ReiniciarMuestreoLanzamiento()
    {
        ultimaPosicionMuestreo = transform.position;
        ultimaRotacionMuestreo = transform.rotation;
        ultimoTiempoMuestreo = Time.unscaledTime;
        velocidadLinealMuestreada = Vector3.zero;
        velocidadAngularMuestreada = Vector3.zero;
        muestreoLanzamientoValido = true;
    }

    private void ActualizarMuestreoLanzamiento()
    {
        if (!IsGrabbed)
        {
            return;
        }

        if (!muestreoLanzamientoValido)
        {
            ReiniciarMuestreoLanzamiento();
            return;
        }

        float ahora = Time.unscaledTime;
        float deltaTiempo = ahora - ultimoTiempoMuestreo;
        if (deltaTiempo < 0.001f)
        {
            return;
        }

        Vector3 posicionActual = transform.position;
        Quaternion rotacionActual = transform.rotation;
        Vector3 velocidadLinealInstantanea =
            (posicionActual - ultimaPosicionMuestreo) / deltaTiempo;

        Quaternion deltaRotacion = rotacionActual * Quaternion.Inverse(ultimaRotacionMuestreo);
        deltaRotacion.ToAngleAxis(out float anguloGrados, out Vector3 eje);
        if (anguloGrados > 180f)
        {
            anguloGrados -= 360f;
        }

        Vector3 velocidadAngularInstantanea = Vector3.zero;
        if (eje.sqrMagnitude > 0.0001f && EsVectorFinito(eje))
        {
            velocidadAngularInstantanea =
                eje.normalized * (anguloGrados * Mathf.Deg2Rad / deltaTiempo);
        }

        float factor = suavizadoMuestreoLanzamiento <= 0f
            ? 1f
            : 1f - Mathf.Exp(-suavizadoMuestreoLanzamiento * deltaTiempo);

        if (EsVectorFinito(velocidadLinealInstantanea))
        {
            velocidadLinealMuestreada = Vector3.Lerp(
                velocidadLinealMuestreada,
                Vector3.ClampMagnitude(
                    velocidadLinealInstantanea,
                    velocidadLinealMaximaAlSoltar * 1.5f
                ),
                factor
            );
        }

        if (EsVectorFinito(velocidadAngularInstantanea))
        {
            velocidadAngularMuestreada = Vector3.Lerp(
                velocidadAngularMuestreada,
                Vector3.ClampMagnitude(
                    velocidadAngularInstantanea,
                    velocidadAngularMaximaAlSoltar * 1.5f
                ),
                factor
            );
        }

        ultimaPosicionMuestreo = posicionActual;
        ultimaRotacionMuestreo = rotacionActual;
        ultimoTiempoMuestreo = ahora;
    }

    private bool DebeAplicarImpulsoDeLanzamiento()
    {
        return conservarImpulsoAlSoltar &&
               muestreoLanzamientoValido &&
               releaseMode == ReleaseMode.Physics &&
               !mantenerFlotandoAlSoltar;
    }

    private void AplicarImpulsoDeLanzamiento(
        Vector3 velocidadLineal,
        Vector3 velocidadAngular)
    {
        if (Rigidbody == null)
        {
            return;
        }

        Rigidbody.isKinematic = false;
        Rigidbody.constraints = RigidbodyConstraints.None;
        Rigidbody.useGravity = useGravityOnRelease;
        Rigidbody.linearVelocity = Vector3.ClampMagnitude(
            velocidadLineal * multiplicadorImpulsoLineal,
            velocidadLinealMaximaAlSoltar
        );
        Rigidbody.angularVelocity = Vector3.ClampMagnitude(
            velocidadAngular * multiplicadorImpulsoAngular,
            velocidadAngularMaximaAlSoltar
        );
    }

    private static bool EsVectorFinito(Vector3 valor)
    {
        return !float.IsNaN(valor.x) && !float.IsInfinity(valor.x) &&
               !float.IsNaN(valor.y) && !float.IsInfinity(valor.y) &&
               !float.IsNaN(valor.z) && !float.IsInfinity(valor.z);
    }

    private void ReforzarEstadoNoAgarrado()
    {
        if (IsGrabbed)
        {
            return;
        }

        if (Rigidbody == null)
        {
            Rigidbody = GetComponent<Rigidbody>();
        }

        bool esperandoPrimerAgarre = EstaEsperandoPrimerAgarreSinColision();

        // Caso especial para práctica 2:
        // El carro acaba de spawnear dentro del garage. Mientras el usuario todavía
        // no lo agarra por primera vez, no debe chocar ni salir volando.
        // Pero después del primer agarre vuelve a la física normal de práctica 2.
        if (esperandoPrimerAgarre && congelarMientrasEsperaPrimerAgarre)
        {
            if (Rigidbody != null)
            {
                Rigidbody.useGravity = false;
                if (!Rigidbody.isKinematic)
                {
                    Rigidbody.linearVelocity = Vector3.zero;
                    Rigidbody.angularVelocity = Vector3.zero;
                }
                Rigidbody.isKinematic = true;
                Rigidbody.constraints = RigidbodyConstraints.FreezeAll;
            }

            return;
        }

        bool debeForzarFlotar = mantenerFlotandoAlSoltar || releaseMode == ReleaseMode.FloatInPlace;

        if (!debeForzarFlotar && !hacerKinematicCuandoNoAgarrado && !congelarRigidbodyCuandoNoAgarrado)
        {
            return;
        }

        if (Rigidbody != null)
        {
            if (debeForzarFlotar)
            {
                Rigidbody.useGravity = false;
            }

            if (limpiarVelocidadesCuandoNoAgarrado &&
                !Rigidbody.isKinematic)
            {
                Rigidbody.linearVelocity = Vector3.zero;
                Rigidbody.angularVelocity = Vector3.zero;
            }

            Rigidbody.isKinematic = hacerKinematicCuandoNoAgarrado;
            Rigidbody.constraints = congelarRigidbodyCuandoNoAgarrado
                ? RigidbodyConstraints.FreezeAll
                : RigidbodyConstraints.None;
        }

        if (DebeBloquearTransform())
        {
            transform.position = posicionBloqueadaMundo;

            if (bloquearRotacionMundoAlSoltar)
            {
                transform.rotation = rotacionBloqueadaMundo;
            }
        }
    }

    private bool DebeBloquearTransform()
    {
        if (!bloquearPosicionMundoAlSoltar || !posicionBloqueadaValida)
        {
            return false;
        }

        if (bloquearSoloDespuesDeSoltar && !yaFueSoltadoPorUsuario)
        {
            return false;
        }

        if (permitirMovimientoExternoAntesDeSoltar && !yaFueSoltadoPorUsuario)
        {
            return false;
        }

        return true;
    }

    private bool EstaEsperandoPrimerAgarreSinColision()
    {
        return sinColisionInicialHastaPrimerAgarre &&
               !yaFueAgarradoPorPrimeraVez &&
               !IsGrabbed;
    }

    private void AplicarEstadoColisiones()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        bool debeEstarSinColision =
            EstaEsperandoPrimerAgarreSinColision() ||
            (sinColisionFisica && (!sinColisionSoloCuandoNoAgarrado || !IsGrabbed));

        if (!debeEstarSinColision)
        {
            RestaurarColisionesIgnoradas();
            RestaurarTriggersOriginales();
            return;
        }

        if (Rigidbody != null && desactivarGravedadCuandoNoColisiona)
        {
            Rigidbody.useGravity = false;
            if (!Rigidbody.isKinematic)
            {
                Rigidbody.linearVelocity = Vector3.zero;
                Rigidbody.angularVelocity = Vector3.zero;
            }
        }

        ActualizarMisColliders();

        bool usarTriggerReal = usarTriggerParaNoColisionar && !mantenerColliderNormalParaAgarre;

        for (int i = 0; i < misColliders.Length; i++)
        {
            Collider col = misColliders[i];

            if (!ColliderPerteneceAEscenaValida(col))
            {
                continue;
            }

            if (usarTriggerReal)
            {
                col.isTrigger = true;
            }
            else
            {
                col.isTrigger = triggerOriginalPorCollider.ContainsKey(col)
                    ? triggerOriginalPorCollider[col]
                    : false;
            }
        }

        if (!usarTriggerReal && ignorarColisionesSolidasDeEscena)
        {
            IgnorarColisionesSolidasDeEscena();
        }
    }

    private void IgnorarColisionesSolidasDeEscena()
    {
        if (!Application.isPlaying || misColliders == null || misColliders.Length == 0)
        {
            return;
        }

        Collider[] todosLosColliders = ObtenerCollidersEscenaCache();

        for (int i = 0; i < misColliders.Length; i++)
        {
            Collider miCollider = misColliders[i];

            if (!ColliderPerteneceAEscenaValida(miCollider))
            {
                continue;
            }

            for (int j = 0; j < todosLosColliders.Length; j++)
            {
                Collider otro = todosLosColliders[j];

                if (!DebeIgnorarCollider(miCollider, otro))
                {
                    continue;
                }

                AdquirirColisionIgnorada(miCollider, otro);
            }
        }
    }

    private static Collider[] ObtenerCollidersEscenaCache()
    {
        if (collidersEscenaCache.Length == 0 ||
            Time.unscaledTime >= proximoRefrescoCollidersEscena)
        {
            collidersEscenaCache = FindObjectsByType<Collider>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );
            proximoRefrescoCollidersEscena = Time.unscaledTime + 0.5f;
        }

        return collidersEscenaCache;
    }

    private bool DebeIgnorarCollider(Collider miCollider, Collider otro)
    {
        if (!ColliderPerteneceAEscenaValida(miCollider) || !ColliderPerteneceAEscenaValida(otro))
        {
            return false;
        }

        if (miCollider == otro)
        {
            return false;
        }

        if (EsColliderPropio(otro))
        {
            return false;
        }

        if (otro.transform == transform || otro.transform.IsChildOf(transform))
        {
            return false;
        }

        if (noIgnorarTriggers && otro.isTrigger)
        {
            return false;
        }

        if (noIgnorarCollidersDeControladoresYManos && PareceColliderDeAgarre(otro))
        {
            return false;
        }

        return true;
    }

    private bool ColliderPerteneceAEscenaValida(Collider col)
    {
        if (col == null || col.gameObject == null)
        {
            return false;
        }

        return col.gameObject.scene.IsValid() && col.gameObject.scene.isLoaded;
    }

    private bool EsColliderPropio(Collider col)
    {
        if (col == null || misColliders == null)
        {
            return false;
        }

        for (int i = 0; i < misColliders.Length; i++)
        {
            if (misColliders[i] == col)
            {
                return true;
            }
        }

        return false;
    }

    private bool PareceColliderDeAgarre(Collider col)
    {
        if (col == null)
        {
            return false;
        }

        string nombre = col.name;
        string nombreObjeto = col.gameObject.name;
        string nombrePadre = col.transform.parent != null ? col.transform.parent.name : string.Empty;
        string etiqueta = col.gameObject.tag;

        for (int i = 0; i < palabrasNombreNoIgnorar.Length; i++)
        {
            string palabra = palabrasNombreNoIgnorar[i];

            if (string.IsNullOrWhiteSpace(palabra))
            {
                continue;
            }

            if (Contiene(nombre, palabra) ||
                Contiene(nombreObjeto, palabra) ||
                Contiene(nombrePadre, palabra) ||
                Contiene(etiqueta, palabra))
            {
                return true;
            }
        }

        return false;
    }

    private bool Contiene(string texto, string palabra)
    {
        if (string.IsNullOrEmpty(texto) || string.IsNullOrEmpty(palabra))
        {
            return false;
        }

        return texto.IndexOf(palabra, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private void AdquirirColisionIgnorada(Collider mio, Collider otro)
    {
        if (!ColliderPerteneceAEscenaValida(mio) || !ColliderPerteneceAEscenaValida(otro))
        {
            return;
        }

        ulong clave = ObtenerClaveColliders(mio, otro);

        if (!clavesIgnoradasPropias.Add(clave))
        {
            return;
        }

        if (colisionesIgnoradasCompartidas.TryGetValue(clave, out EstadoColisionIgnorada estado))
        {
            estado.referencias++;
            colisionesIgnoradasCompartidas[clave] = estado;
        }
        else
        {
            estado = new EstadoColisionIgnorada
            {
                referencias = 1,
                ignoradaAntesDeAlgoLab = Physics.GetIgnoreCollision(mio, otro)
            };
            colisionesIgnoradasCompartidas.Add(clave, estado);

            if (!estado.ignoradaAntesDeAlgoLab)
            {
                Physics.IgnoreCollision(mio, otro, true);
            }
        }

        colisionesIgnoradas.Add(new ParColliderIgnorado
        {
            mio = mio,
            otro = otro,
            clave = clave
        });
    }

    private static ulong ObtenerClaveColliders(Collider primero, Collider segundo)
    {
        uint a = unchecked((uint)primero.GetInstanceID());
        uint b = unchecked((uint)segundo.GetInstanceID());
        uint menor = Math.Min(a, b);
        uint mayor = Math.Max(a, b);
        return ((ulong)menor << 32) | mayor;
    }

    private void RestaurarColisionesIgnoradas()
    {
        for (int i = 0; i < colisionesIgnoradas.Count; i++)
        {
            ParColliderIgnorado par = colisionesIgnoradas[i];

            if (!colisionesIgnoradasCompartidas.TryGetValue(
                    par.clave,
                    out EstadoColisionIgnorada estado))
            {
                continue;
            }

            estado.referencias--;

            if (estado.referencias > 0)
            {
                colisionesIgnoradasCompartidas[par.clave] = estado;
                continue;
            }

            colisionesIgnoradasCompartidas.Remove(par.clave);

            if (!estado.ignoradaAntesDeAlgoLab &&
                ColliderPerteneceAEscenaValida(par.mio) &&
                ColliderPerteneceAEscenaValida(par.otro))
            {
                Physics.IgnoreCollision(par.mio, par.otro, false);
            }
        }

        colisionesIgnoradas.Clear();
        clavesIgnoradasPropias.Clear();
    }

    private void RestaurarTriggersOriginales()
    {
        foreach (KeyValuePair<Collider, bool> kvp in triggerOriginalPorCollider)
        {
            if (ColliderPerteneceAEscenaValida(kvp.Key))
            {
                kvp.Key.isTrigger = kvp.Value;
            }
        }
    }

    public void ForcePhysicsRelease()
    {
        if (IsGrabbed)
        {
            ActualizarMuestreoLanzamiento();
        }

        Vector3 impulsoLineal = velocidadLinealMuestreada;
        Vector3 impulsoAngular = velocidadAngularMuestreada;
        bool aplicarImpulso = DebeAplicarImpulsoDeLanzamiento();

        IsGrabbed = false;
        agarradoPorCambioDePadre = false;
        yaFueSoltadoPorUsuario = true;
        posicionBloqueadaValida = false;

        if (Rigidbody == null)
        {
            Rigidbody = GetComponent<Rigidbody>();
        }

        Rigidbody.isKinematic = false;
        Rigidbody.constraints = RigidbodyConstraints.None;
        Rigidbody.useGravity = true;
        Rigidbody.linearVelocity = Vector3.zero;
        Rigidbody.angularVelocity = Vector3.zero;
        Rigidbody.linearDamping = physicsLinearDamping;
        Rigidbody.angularDamping = physicsAngularDamping;

        AplicarEstadoColisiones();

        if (usarSendMessageLegacy)
        {
            SendMessage("NotificarSoltado", SendMessageOptions.DontRequireReceiver);
        }

        OnGrabEnded?.Invoke();

        if (aplicarImpulso)
        {
            AplicarImpulsoDeLanzamiento(impulsoLineal, impulsoAngular);
        }

        DebugLog("SimpleMRGrabbable: soltado forzado con física.");
    }

    [ContextMenu("Bloquear posición actual")]
    public void BloquearPosicionActual()
    {
        yaFueSoltadoPorUsuario = true;
        BloquearPosicionActualComoSoltada();
        ReforzarEstadoNoAgarrado();
    }

    [ContextMenu("Permitir movimiento externo")]
    public void PermitirMovimientoExterno()
    {
        posicionBloqueadaValida = false;
        yaFueSoltadoPorUsuario = false;
    }

    /// <summary>
    /// Libera únicamente la protección de spawn inicial para que un sistema del
    /// juego pueda mover el objeto sin exigir un agarre manual previo. Conserva
    /// el comportamiento normal de agarre y soltado para interacciones futuras.
    /// </summary>
    public void PrepararParaMovimientoProgramatico()
    {
        yaFueAgarradoPorPrimeraVez = true;
        posicionBloqueadaValida = false;

        if (Rigidbody == null)
        {
            Rigidbody = GetComponent<Rigidbody>();
        }

        if (Rigidbody != null && !IsGrabbed)
        {
            Rigidbody.constraints = RigidbodyConstraints.None;
            Rigidbody.isKinematic = false;
            Rigidbody.linearVelocity = Vector3.zero;
            Rigidbody.angularVelocity = Vector3.zero;
        }

        AplicarEstadoColisiones();
    }

    public void Grab()
    {
        BeginGrab();
    }

    public void Release()
    {
        EndGrab();
    }

    public void OnGrab()
    {
        BeginGrab();
    }

    public void OnRelease()
    {
        EndGrab();
    }

    public void NotificarGrab()
    {
        BeginGrab();
    }

    public void NotificarRelease()
    {
        EndGrab();
    }

    private void DebugLog(string mensaje)
    {
        if (mostrarDebug)
        {
            Debug.Log(mensaje);
        }
    }
}
