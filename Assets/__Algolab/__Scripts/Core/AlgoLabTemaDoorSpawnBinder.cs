using System.Collections;
using UnityEngine;

public class AlgoLabTemaDoorSpawnBinder : MonoBehaviour
{
    [Header("Referencias")]
    public AlgoLabManualPanelSpawnManager spawnManager;
    public AlgoLabTemaPOOController temaController;

    [Header("Prefab de puerta tema")]
    public GameObject puertaTemaPrefab;

    [Header("Spawn")]
    public bool usarEscalaManual = true;
    public Vector3 escalaManual = new Vector3(0.25f, 0.25f, 0.25f);

    [Header("Inicio")]
    public bool spawnearAlIniciar = false;
    public bool reproducirTemaDespuesSpawn = false;

    [Header("Debug")]
    public bool mostrarDebug = false;

    private Coroutine rutinaConexion;
    private int generacionConexion;

    private void Start()
    {
        if (spawnManager == null)
        {
            spawnManager = AlgoLabManualPanelSpawnManager.Instance;
        }

        if (temaController == null)
        {
            temaController = FindFirstObjectByType<AlgoLabTemaPOOController>();
        }

        if (spawnearAlIniciar)
        {
            SpawnearPuertaTema();
        }
    }

    private void OnDisable()
    {
        generacionConexion++;

        if (rutinaConexion != null)
        {
            StopCoroutine(rutinaConexion);
            rutinaConexion = null;
        }
    }

    [ContextMenu("Spawnear puerta tema")]
    public void SpawnearPuertaTema()
    {
        SpawnearPuertaTemaInterno(reproducirTemaDespuesSpawn);
    }

    private void SpawnearPuertaTemaInterno(bool iniciarTemaAlConectar)
    {
        if (spawnManager == null)
        {
            Debug.LogError("No hay ManualPanelSpawnManager asignado.");
            return;
        }

        if (puertaTemaPrefab == null)
        {
            Debug.LogError("No hay prefab de PuertaTema asignado.");
            return;
        }

        if (!isActiveAndEnabled)
        {
            Debug.LogWarning("No se puede spawnear la puerta con el binder desactivado.");
            return;
        }

        GameObject objetoAnterior = spawnManager.ObjetoFrontalActual;

        if (usarEscalaManual)
        {
            spawnManager.CambiarObjetoFrontalDesdePrefabConEscala(
                puertaTemaPrefab,
                escalaManual
            );
        }
        else
        {
            spawnManager.CambiarObjetoFrontalDesdePrefab(puertaTemaPrefab);
        }

        if (rutinaConexion != null)
            StopCoroutine(rutinaConexion);

        int miGeneracion = ++generacionConexion;
        rutinaConexion = StartCoroutine(
            ConectarPuertaSpawneada(objetoAnterior, iniciarTemaAlConectar, miGeneracion)
        );
    }

    private IEnumerator ConectarPuertaSpawneada(
        GameObject objetoAnterior,
        bool iniciarTemaAlConectar,
        int miGeneracion)
    {
        yield return null;

        GameObject objetoSpawneado = null;
        AlgoLabThemeDoorController puertaController = null;

        float tiempo = 0f;

        while (tiempo < 3f && miGeneracion == generacionConexion)
        {
            tiempo += Time.unscaledDeltaTime;

            GameObject candidato = spawnManager != null
                ? spawnManager.ObjetoFrontalActual
                : null;

            if (candidato != null && candidato != objetoAnterior)
            {
                puertaController =
                    candidato.GetComponentInChildren<AlgoLabThemeDoorController>(true);

                if (puertaController != null)
                {
                    objetoSpawneado = candidato;
                    break;
                }
            }

            yield return null;
        }

        if (miGeneracion != generacionConexion)
            yield break;

        if (objetoSpawneado == null)
        {
            Debug.LogError("No se encontró la puerta spawneada.");
            rutinaConexion = null;
            yield break;
        }

        if (temaController != null)
        {
            temaController.AsignarPuertaController(puertaController);

            // La puerta aparece durante el tema, después del refresco normal
            // del panel. Forzamos uno ahora para que su clase Puerta, sus
            // atributos y sus métodos se vean en el mismo instante.
            AlgoLabClassDiagramModeManager modo = temaController.diagramModeManager;
            if (modo != null && modo.classDiagramController != null)
            {
                modo.classDiagramController.RefrescarDiagramas();
            }
        }

        if (mostrarDebug)
        {
            Debug.Log("Puerta tema conectada al controlador del tema.");
        }

        if (iniciarTemaAlConectar && temaController != null)
        {
            temaController.ReproducirTema();
        }

        rutinaConexion = null;
    }

    [ContextMenu("Reubicar puerta al frente")]
    public void ReubicarPuertaAlFrente()
    {
        if (spawnManager == null)
        {
            spawnManager = AlgoLabManualPanelSpawnManager.Instance;
        }

        if (spawnManager != null)
        {
            spawnManager.ReubicarObjetoFrontal();
        }
    }

    [ContextMenu("Iniciar tema con puerta")]
    public void IniciarTemaConPuerta()
    {
        SpawnearPuertaTemaInterno(true);
    }
}
