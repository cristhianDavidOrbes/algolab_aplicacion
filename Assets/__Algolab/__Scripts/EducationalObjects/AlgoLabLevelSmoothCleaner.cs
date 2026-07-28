using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AlgoLabLevelSmoothCleaner : MonoBehaviour
{
    [Header("Tags")]
    public string tagPanel = "Panel";
    public string tagObjeto = "Objeto";

    [Header("Spawn Manager")]
    public AlgoLabManualPanelSpawnManager spawnManager;

    [Header("Animación smooth")]
    public float duracionDesaparicion = 0.35f;
    public float escalaFinalMultiplicador = 0.05f;

    [Tooltip("Si está activo destruye los objetos. Si está apagado solo los desactiva.")]
    public bool destruirAlFinal = true;

    [Header("Debug")]
    public bool mostrarDebug = true;

    private Coroutine rutinaLimpieza;
    private readonly List<GameObject> objetosEnLimpieza = new List<GameObject>();
    private readonly Dictionary<GameObject, Vector3> escalasOriginales =
        new Dictionary<GameObject, Vector3>();

    private void Awake()
    {
        if (spawnManager == null)
        {
            spawnManager = AlgoLabManualPanelSpawnManager.Instance;
        }
    }

    private void OnDisable()
    {
        if (rutinaLimpieza == null)
            return;

        StopCoroutine(rutinaLimpieza);
        rutinaLimpieza = null;
        FinalizarObjetosEnLimpieza();
    }

    [ContextMenu("Limpiar objetos con smooth")]
    public void LimpiarObjetosConSmooth()
    {
        if (rutinaLimpieza != null)
        {
            StopCoroutine(rutinaLimpieza);
            rutinaLimpieza = null;
        }

        PrepararObjetosEnLimpieza(ObtenerObjetosParaLimpiar());

        if (objetosEnLimpieza.Count == 0)
        {
            DebugLog("Cleaner: no hay objetos válidos para limpiar.");
            return;
        }

        if (!isActiveAndEnabled || duracionDesaparicion <= 0f)
        {
            FinalizarObjetosEnLimpieza();
            return;
        }

        rutinaLimpieza = StartCoroutine(LimpiarRutina());
    }

    private IEnumerator LimpiarRutina()
    {
        if (mostrarDebug)
        {
            Debug.Log("Cleaner: objetos encontrados para limpiar: " + objetosEnLimpieza.Count);
        }

        float duracion = Mathf.Max(0.01f, duracionDesaparicion);
        float multiplicador = Mathf.Max(0f, escalaFinalMultiplicador);
        float tiempo = 0f;

        while (tiempo < duracion)
        {
            tiempo += Time.unscaledDeltaTime;
            float progreso = Mathf.Clamp01(tiempo / duracion);
            float smooth = Mathf.SmoothStep(0f, 1f, progreso);

            for (int i = 0; i < objetosEnLimpieza.Count; i++)
            {
                GameObject obj = objetosEnLimpieza[i];

                if (obj == null || !escalasOriginales.TryGetValue(obj, out Vector3 escalaInicial))
                    continue;

                obj.transform.localScale = Vector3.Lerp(
                    escalaInicial,
                    escalaInicial * multiplicador,
                    smooth
                );
            }

            yield return null;
        }

        FinalizarObjetosEnLimpieza();
        rutinaLimpieza = null;

        if (mostrarDebug)
        {
            Debug.Log("Cleaner: limpieza terminada.");
        }
    }

    private List<GameObject> ObtenerObjetosParaLimpiar()
    {
        HashSet<GameObject> resultado = new HashSet<GameObject>();

        Transform[] todos = FindObjectsByType<Transform>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < todos.Length; i++)
        {
            Transform actual = todos[i];

            if (actual == null)
            {
                continue;
            }

            GameObject obj = actual.gameObject;

            if (!TieneTagSeguro(obj, tagObjeto))
            {
                continue;
            }

            if (TienePadrePanel(actual))
            {
                continue;
            }

            GameObject rootObjeto = ObtenerRootObjeto(actual);

            if (rootObjeto != null &&
                rootObjeto != gameObject &&
                !transform.IsChildOf(rootObjeto.transform) &&
                !TienePadrePanel(rootObjeto.transform))
            {
                resultado.Add(rootObjeto);
            }
        }

        if (spawnManager == null)
        {
            spawnManager = AlgoLabManualPanelSpawnManager.Instance;
        }

        if (spawnManager != null && spawnManager.ObjetoFrontalActual != null)
        {
            GameObject frontal = spawnManager.ObjetoFrontalActual;

            if (frontal != null &&
                !TieneTagSeguro(frontal, tagPanel) &&
                !TienePadrePanel(frontal.transform))
            {
                resultado.Add(frontal);
            }
        }

        return new List<GameObject>(resultado);
    }

    private GameObject ObtenerRootObjeto(Transform objeto)
    {
        if (objeto == null)
        {
            return null;
        }

        Transform actual = objeto;
        GameObject rootObjeto = objeto.gameObject;

        while (actual.parent != null)
        {
            Transform padre = actual.parent;

            if (TieneTagSeguro(padre.gameObject, tagPanel))
            {
                return null;
            }

            if (TieneTagSeguro(padre.gameObject, tagObjeto))
            {
                rootObjeto = padre.gameObject;
            }

            actual = padre;
        }

        return rootObjeto;
    }

    private bool TienePadrePanel(Transform objeto)
    {
        if (objeto == null)
        {
            return false;
        }

        Transform actual = objeto;

        while (actual != null)
        {
            if (TieneTagSeguro(actual.gameObject, tagPanel))
            {
                return true;
            }

            actual = actual.parent;
        }

        return false;
    }

    private bool TieneTagSeguro(GameObject obj, string tagBuscado)
    {
        if (obj == null || string.IsNullOrWhiteSpace(tagBuscado))
        {
            return false;
        }

        try
        {
            return obj.CompareTag(tagBuscado);
        }
        catch
        {
            return false;
        }
    }

    private void PrepararObjetosEnLimpieza(List<GameObject> objetos)
    {
        if (objetos == null)
        {
            return;
        }

        for (int i = 0; i < objetos.Count; i++)
        {
            GameObject obj = objetos[i];

            if (obj == null || obj == gameObject || transform.IsChildOf(obj.transform))
                continue;

            if (!objetosEnLimpieza.Contains(obj))
                objetosEnLimpieza.Add(obj);

            if (!escalasOriginales.ContainsKey(obj))
                escalasOriginales[obj] = obj.transform.localScale;
        }
    }

    private void FinalizarObjetosEnLimpieza()
    {
        for (int i = objetosEnLimpieza.Count - 1; i >= 0; i--)
        {
            GameObject obj = objetosEnLimpieza[i];

            if (obj == null)
                continue;

            escalasOriginales.TryGetValue(obj, out Vector3 escalaOriginal);

            if (destruirAlFinal)
            {
                if (Application.isPlaying)
                    Destroy(obj);
                else
                    DestroyImmediate(obj);
            }
            else
            {
                obj.transform.localScale = escalaOriginal;
                obj.SetActive(false);
            }
        }

        objetosEnLimpieza.Clear();
        escalasOriginales.Clear();
    }

    [ContextMenu("Limpiar objetos inmediato")]
    public void LimpiarObjetosInmediato()
    {
        if (rutinaLimpieza != null)
        {
            StopCoroutine(rutinaLimpieza);
            rutinaLimpieza = null;
        }

        PrepararObjetosEnLimpieza(ObtenerObjetosParaLimpiar());
        FinalizarObjetosEnLimpieza();

        if (mostrarDebug)
        {
            Debug.Log("Cleaner: limpieza inmediata terminada.");
        }
    }

    private void DebugLog(string mensaje)
    {
        if (mostrarDebug)
            Debug.Log(mensaje);
    }
}
