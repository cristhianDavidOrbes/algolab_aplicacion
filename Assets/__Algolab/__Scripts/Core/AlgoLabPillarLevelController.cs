using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Flujo ligero para los cuatro niveles de pilares de POO. El contenido se muestra
/// en el panel de progreso y deja el avance bajo control del usuario.
/// </summary>
public class AlgoLabPillarLevelController : MonoBehaviour
{
    [Serializable]
    public class PilarNivel
    {
        public int numeroNivel;
        public string nombre;
        [TextArea(3, 8)] public string explicacion;
        [TextArea(3, 8)] public string reto;
    }

    [Header("Niveles de pilares")]
    public List<PilarNivel> niveles = new List<PilarNivel>();

    [Header("Tema visual de Encapsulamiento (nivel 3)")]
    public AlgoLabEncapsulationThemeController encapsulationThemeController;

    [Header("Tema visual de Abstraccion (nivel 4)")]
    public AlgoLabAbstractionThemeController abstractionThemeController;

    public UnityEvent OnTemaTerminado = new UnityEvent();

    [Header("Guia de practica de Encapsulamiento (nivel 3)")]
    [Tooltip("Secuencia preparada para video, narraciones o la guia provisional de texto del nivel 3.")]
    public AlgoLabPracticeTutorialSequence tutorialPracticaNivel3;

    [Header("Practica interactiva de Encapsulamiento (nivel 3)")]
    public string recursoPracticaRobot = "Level3/AlgoLabRobotPractice";
    public AlgoLabManualPanelSpawnManager manualSpawner;

    public bool mostrarDebug = true;

    private int nivelActual = -1;
    private bool temaActivo;
    private bool practicaActiva;
    private Coroutine rutinaIniciarRobot;
    private AlgoLabEncapsulationRobotPractice practicaRobotActiva;

    public int NivelActual => nivelActual;
    public bool TemaActivo => temaActivo;
    public bool PracticaActiva => practicaActiva;
    public bool UsaPracticaInteractiva(int numeroNivelReal) => numeroNivelReal == 3;

    private void Awake()
    {
        AsegurarNivelesPorDefecto();
        ResolverTemaEncapsulamiento();
        ResolverTemaAbstraccion();
        ResolverTutorialPracticaNivel3();
        ConectarTemaEncapsulamiento();
        ConectarTemaAbstraccion();
    }

    private void OnEnable()
    {
        ResolverTemaEncapsulamiento();
        ResolverTemaAbstraccion();
        ResolverTutorialPracticaNivel3();
        ConectarTemaEncapsulamiento();
        ConectarTemaAbstraccion();
    }

    private void OnDisable()
    {
        DesconectarTemaEncapsulamiento();
        DesconectarTemaAbstraccion();
    }

    public void AsegurarNivelesPorDefecto()
    {
        if (niveles == null)
        {
            niveles = new List<PilarNivel>();
        }

        if (niveles.Count > 0)
        {
            for (int i = 0; i < niveles.Count; i++)
            {
                if (niveles[i] != null && niveles[i].numeroNivel == 3)
                {
                    niveles[i].reto =
                        "Repara el Robot usando solo sus metodos publicos. Protege energia, temperatura, encendido y averia: intentar modificar directamente un atributo privado resta puntos.";
                    break;
                }
            }
            return;
        }

        niveles.Add(new PilarNivel
        {
            numeroNivel = 3,
            nombre = "Encapsulamiento",
            explicacion = "Una clase protege su estado interno y expone solo operaciones seguras. Los datos privados no se manipulan directamente: se validan mediante métodos públicos.",
            reto = "Explica qué datos deben ser privados en CuentaSegura y qué métodos públicos pueden depositar, retirar o consultar el saldo."
        });

        niveles[niveles.Count - 1].reto =
            "Repara el Robot usando solo sus metodos publicos. Protege energia, temperatura, encendido y averia: intentar modificar directamente un atributo privado resta puntos.";

        niveles.Add(new PilarNivel
        {
            numeroNivel = 4,
            nombre = "Abstracción",
            explicacion = "La abstracción muestra lo esencial y oculta detalles que no necesita quien utiliza el objeto. Una interfaz simple permite usar una clase sin conocer toda su implementación.",
            reto = "Separa las operaciones esenciales de Vehículo de los detalles internos necesarios para realizarlas."
        });

        niveles.Add(new PilarNivel
        {
            numeroNivel = 5,
            nombre = "Herencia",
            explicacion = "Una clase hija reutiliza la estructura de una clase padre y puede especializarla. Así Carro, Moto y Camión comparten lo que corresponde a Vehículo.",
            reto = "Clasifica qué atributos y métodos pertenecen a Vehículo y cuáles son específicos de Carro, Moto o Camión."
        });

        niveles.Add(new PilarNivel
        {
            numeroNivel = 6,
            nombre = "Polimorfismo",
            explicacion = "Una misma operación puede producir un comportamiento distinto según el objeto que la ejecuta. Una referencia común permite trabajar con varias implementaciones.",
            reto = "Relaciona acelerar() con la implementación de Carro, Moto o Camión y explica por qué cambia la respuesta."
        });
    }

    public bool EsNivelPilar(int numeroNivelReal)
    {
        return numeroNivelReal >= 3 && numeroNivelReal <= 6;
    }

    public PilarNivel ObtenerNivel(int numeroNivelReal)
    {
        AsegurarNivelesPorDefecto();

        for (int i = 0; i < niveles.Count; i++)
        {
            if (niveles[i] != null && niveles[i].numeroNivel == numeroNivelReal)
            {
                return niveles[i];
            }
        }

        return null;
    }

    public string ObtenerTextoTema(int numeroNivelReal)
    {
        PilarNivel nivel = ObtenerNivel(numeroNivelReal);
        return nivel == null ? "Tema de POO" : nivel.explicacion;
    }

    public string ObtenerTextoPractica(int numeroNivelReal)
    {
        PilarNivel nivel = ObtenerNivel(numeroNivelReal);
        return nivel == null ? "Completa el reto del pilar." : nivel.reto;
    }

    public void IniciarTema(int numeroNivelReal)
    {
        if (!EsNivelPilar(numeroNivelReal))
        {
            return;
        }

        nivelActual = numeroNivelReal;
        temaActivo = true;
        practicaActiva = false;

        if (numeroNivelReal == 3 && encapsulationThemeController != null)
        {
            encapsulationThemeController.StartTheme();
        }
        else if (numeroNivelReal == 4 && abstractionThemeController != null)
        {
            abstractionThemeController.StartTheme();
        }

        DebugPilar("tema iniciado");
    }

    public void TerminarTema()
    {
        if (!temaActivo)
        {
            return;
        }

        temaActivo = false;

        if (nivelActual == 3 && encapsulationThemeController != null)
        {
            encapsulationThemeController.StopTheme();
        }
        else if (nivelActual == 4 && abstractionThemeController != null)
        {
            abstractionThemeController.StopTheme();
        }

        DebugPilar("tema terminado");
    }

    public void PrepararPractica(int numeroNivelReal)
    {
        if (!EsNivelPilar(numeroNivelReal))
        {
            return;
        }

        nivelActual = numeroNivelReal;
        temaActivo = false;
        practicaActiva = false;
        DebugPilar("reto preparado");
    }

    public bool ReproducirTutorialPracticaSiDisponible(
        int numeroNivelReal,
        UnityAction alTerminar)
    {
        if (numeroNivelReal != 3)
        {
            return false;
        }

        ResolverTutorialPracticaNivel3();
        if (tutorialPracticaNivel3 == null || !tutorialPracticaNivel3.PuedeReproducir)
        {
            return false;
        }

        tutorialPracticaNivel3.Reproducir(alTerminar);
        DebugPilar("guia de practica iniciada");
        return true;
    }

    public void IniciarPractica(int numeroNivelReal)
    {
        if (!EsNivelPilar(numeroNivelReal))
        {
            return;
        }

        nivelActual = numeroNivelReal;
        temaActivo = false;
        practicaActiva = true;

        if (numeroNivelReal == 3)
        {
            if (rutinaIniciarRobot != null)
                StopCoroutine(rutinaIniciarRobot);
            rutinaIniciarRobot = StartCoroutine(IniciarPracticaRobotRutina());
        }

        DebugPilar("reto iniciado");
    }

    public bool CompletarPractica(int numeroNivelReal)
    {
        if (!EsNivelPilar(numeroNivelReal) || !practicaActiva)
        {
            return false;
        }

        practicaActiva = false;
        DebugPilar("reto completado");
        return true;
    }

    public void DetenerFlujo()
    {
        if (rutinaIniciarRobot != null)
        {
            StopCoroutine(rutinaIniciarRobot);
            rutinaIniciarRobot = null;
        }

        if (tutorialPracticaNivel3 != null)
        {
            tutorialPracticaNivel3.Detener(false);
        }

        if (encapsulationThemeController != null)
        {
            encapsulationThemeController.StopTheme();
        }
        if (abstractionThemeController != null)
        {
            abstractionThemeController.StopTheme();
        }

        temaActivo = false;
        practicaActiva = false;
        nivelActual = -1;
    }

    private IEnumerator IniciarPracticaRobotRutina()
    {
        if (manualSpawner == null)
        {
            manualSpawner = AlgoLabManualPanelSpawnManager.Instance;
            if (manualSpawner == null)
            {
                manualSpawner = FindFirstObjectByType<AlgoLabManualPanelSpawnManager>(
                    FindObjectsInactive.Include
                );
            }
        }

        GameObject prefab = Resources.Load<GameObject>(recursoPracticaRobot);
        if (prefab != null && manualSpawner != null)
        {
            manualSpawner.CambiarObjetoFrontalDesdePrefab(prefab);

            float espera = 0f;
            while (espera < 8f)
            {
                espera += Time.unscaledDeltaTime;
                GameObject actual = manualSpawner.ObjetoFrontalActual;
                if (actual != null)
                {
                    practicaRobotActiva =
                        actual.GetComponent<AlgoLabEncapsulationRobotPractice>();
                    if (practicaRobotActiva != null)
                        break;
                }
                yield return null;
            }
        }
        else
        {
            GameObject instance = prefab != null
                ? Instantiate(prefab)
                : new GameObject("AlgoLabRobotPractice_Runtime");
            practicaRobotActiva =
                instance.GetComponent<AlgoLabEncapsulationRobotPractice>();
            if (practicaRobotActiva == null)
                practicaRobotActiva = instance.AddComponent<AlgoLabEncapsulationRobotPractice>();

            Camera camera = Camera.main;
            if (camera != null)
            {
                Vector3 forward = Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up).normalized;
                if (forward.sqrMagnitude < 0.001f)
                    forward = camera.transform.forward;
                instance.transform.position =
                    camera.transform.position + forward * 1.9f + Vector3.down * 0.12f;
                instance.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
            }

            if (manualSpawner != null)
                manualSpawner.RegistrarObjetoFrontalExternoParaAlturaDinamica(instance);
        }

        if (practicaRobotActiva != null)
        {
            practicaRobotActiva.IniciarPractica();
            DebugPilar("practica interactiva del robot lista");
        }
        else
        {
            Debug.LogError("PILARES POO: no se pudo crear la practica interactiva del robot.");
        }

        rutinaIniciarRobot = null;
    }

    public bool TemaUsaSecuenciaAutomatica(int numeroNivelReal)
    {
        if (numeroNivelReal == 3)
        {
            return encapsulationThemeController != null &&
                   encapsulationThemeController.themeVisualPrefab != null;
        }

        if (numeroNivelReal == 4)
        {
            return abstractionThemeController != null &&
                   abstractionThemeController.themeVisualPrefab != null;
        }

        return false;
    }

    private void ResolverTemaEncapsulamiento()
    {
        if (encapsulationThemeController == null)
        {
            encapsulationThemeController = FindFirstObjectByType<AlgoLabEncapsulationThemeController>(
                FindObjectsInactive.Include
            );
        }
    }

    private void ResolverTemaAbstraccion()
    {
        if (abstractionThemeController == null)
        {
            abstractionThemeController = FindFirstObjectByType<AlgoLabAbstractionThemeController>(
                FindObjectsInactive.Include
            );
        }
    }

    private void ResolverTutorialPracticaNivel3()
    {
        if (tutorialPracticaNivel3 == null)
        {
            tutorialPracticaNivel3 = GetComponent<AlgoLabPracticeTutorialSequence>();
        }

        if (tutorialPracticaNivel3 == null && Application.isPlaying)
        {
            tutorialPracticaNivel3 = gameObject.AddComponent<AlgoLabPracticeTutorialSequence>();
        }

        if (tutorialPracticaNivel3 == null)
        {
            return;
        }

        tutorialPracticaNivel3.tipoPractica =
            AlgoLabPracticeTutorialSequence.TipoPractica.Nivel3Encapsulamiento;

        if (tutorialPracticaNivel3.tutorialPanel == null)
        {
            tutorialPracticaNivel3.tutorialPanel =
                FindFirstObjectByType<AlgoLabTutorialPanelController>(FindObjectsInactive.Include);
        }
    }

    private void ConectarTemaEncapsulamiento()
    {
        if (encapsulationThemeController == null)
        {
            return;
        }

        encapsulationThemeController.OnThemeFinished.RemoveListener(OnEncapsulationThemeFinished);
        encapsulationThemeController.OnThemeFinished.AddListener(OnEncapsulationThemeFinished);
    }

    private void DesconectarTemaEncapsulamiento()
    {
        if (encapsulationThemeController != null)
        {
            encapsulationThemeController.OnThemeFinished.RemoveListener(OnEncapsulationThemeFinished);
        }
    }

    private void ConectarTemaAbstraccion()
    {
        if (abstractionThemeController == null)
        {
            return;
        }

        abstractionThemeController.OnThemeFinished.RemoveListener(OnAbstractionThemeFinished);
        abstractionThemeController.OnThemeFinished.AddListener(OnAbstractionThemeFinished);
    }

    private void DesconectarTemaAbstraccion()
    {
        if (abstractionThemeController != null)
        {
            abstractionThemeController.OnThemeFinished.RemoveListener(OnAbstractionThemeFinished);
        }
    }

    private void OnEncapsulationThemeFinished()
    {
        if (!temaActivo || nivelActual != 3)
        {
            return;
        }

        DebugPilar("secuencia de tema terminada");
        OnTemaTerminado.Invoke();
    }

    private void OnAbstractionThemeFinished()
    {
        if (!temaActivo || nivelActual != 4)
        {
            return;
        }

        DebugPilar("secuencia de tema de abstraccion terminada");
        OnTemaTerminado.Invoke();
    }

    private void DebugPilar(string mensaje)
    {
        if (mostrarDebug)
        {
            Debug.Log("PILARES POO: nivel " + nivelActual + " | " + mensaje);
        }
    }
}
