using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Conecta el tema de Encapsulamiento con el objeto frontal del ManualSpawner.
/// </summary>
public class AlgoLabEncapsulationThemeController : MonoBehaviour
{
    [Header("Referencias")]
    public AlgoLabManualPanelSpawnManager spawnManager;
    public GameObject themeVisualPrefab;

    [Header("Spawn")]
    public Vector3 spawnScale = Vector3.one;
    public float maximumConnectWait = 4f;

    [Header("Eventos")]
    public UnityEvent OnThemeFinished = new UnityEvent();

    [Header("Debug")]
    public bool showDebug;

    private AlgoLabEncapsulationThemeVisual activeVisual;
    private Coroutine connectRoutine;
    private int connectionGeneration;

    public bool IsThemeRunning => activeVisual != null && activeVisual.IsPlaying;

    [ContextMenu("Iniciar tema de Encapsulamiento")]
    public void StartTheme()
    {
        StopTheme();
        ResolveSpawnManager();

        if (spawnManager == null)
        {
            Debug.LogError("ENCAPSULAMIENTO: no se encontro ManualPanelSpawnManager.");
            return;
        }

        if (themeVisualPrefab == null)
        {
            Debug.LogError("ENCAPSULAMIENTO: falta el prefab visual del tema.");
            return;
        }

        GameObject previousObject = spawnManager.ObjetoFrontalActual;
        spawnManager.CambiarObjetoFrontalDesdePrefabConEscala(themeVisualPrefab, spawnScale);

        int myGeneration = ++connectionGeneration;
        connectRoutine = StartCoroutine(ConnectSpawnedVisual(previousObject, myGeneration));
    }

    public void StopTheme()
    {
        connectionGeneration++;

        if (connectRoutine != null)
        {
            StopCoroutine(connectRoutine);
            connectRoutine = null;
        }

        DisconnectActiveVisual(true);
    }

    private void OnDisable()
    {
        StopTheme();
    }

    private IEnumerator ConnectSpawnedVisual(GameObject previousObject, int myGeneration)
    {
        float elapsed = 0f;
        AlgoLabEncapsulationThemeVisual foundVisual = null;

        while (elapsed < maximumConnectWait && myGeneration == connectionGeneration)
        {
            elapsed += Time.unscaledDeltaTime;
            GameObject candidate = spawnManager != null ? spawnManager.ObjetoFrontalActual : null;

            if (candidate != null && candidate != previousObject)
            {
                foundVisual = candidate.GetComponentInChildren<AlgoLabEncapsulationThemeVisual>(true);
                if (foundVisual != null)
                {
                    break;
                }
            }

            yield return null;
        }

        connectRoutine = null;

        if (myGeneration != connectionGeneration)
        {
            yield break;
        }

        if (foundVisual == null)
        {
            Debug.LogError("ENCAPSULAMIENTO: no se pudo conectar el visual frontal spawneado.");
            yield break;
        }

        activeVisual = foundVisual;
        activeVisual.OnSequenceFinished.RemoveListener(HandleSequenceFinished);
        activeVisual.OnSequenceFinished.AddListener(HandleSequenceFinished);
        activeVisual.PlaySequence();

        if (showDebug)
        {
            Debug.Log("ENCAPSULAMIENTO: visual frontal conectado; secuencia iniciada.");
        }
    }

    private void HandleSequenceFinished()
    {
        if (activeVisual != null)
        {
            activeVisual.OnSequenceFinished.RemoveListener(HandleSequenceFinished);
        }

        if (showDebug)
        {
            Debug.Log("ENCAPSULAMIENTO: controlador recibio el final de audio 10.");
        }

        OnThemeFinished.Invoke();
    }

    private void DisconnectActiveVisual(bool stopAudio)
    {
        if (activeVisual == null)
        {
            return;
        }

        activeVisual.OnSequenceFinished.RemoveListener(HandleSequenceFinished);
        if (stopAudio)
        {
            activeVisual.StopSequence();
        }
        activeVisual = null;
    }

    private void ResolveSpawnManager()
    {
        if (spawnManager == null)
        {
            spawnManager = AlgoLabManualPanelSpawnManager.Instance;
        }

        if (spawnManager == null)
        {
            spawnManager = FindFirstObjectByType<AlgoLabManualPanelSpawnManager>(FindObjectsInactive.Include);
        }
    }
}
